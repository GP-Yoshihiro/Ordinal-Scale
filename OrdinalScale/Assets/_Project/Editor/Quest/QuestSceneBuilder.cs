using OrdinalScale.Gameplay.Battle;
using OrdinalScale.Gameplay.Combat;
using OrdinalScale.Gameplay.Placement;
using OrdinalScale.Platform;
using OrdinalScale.Platform.EditorSim;
using OrdinalScale.Platform.XR;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;
using InputTrackedPoseDriver = UnityEngine.InputSystem.XR.TrackedPoseDriver;

namespace OrdinalScale.EditorTools
{
    /// <summary>
    /// Quest 用シーン（Quest_MR.unity）をコードで組み立てて保存する。シーンの YAML を手で書かないための道具。
    /// iPhone のシーン（iPhone_AR.unity）とビルド対象一覧には触れない。
    ///
    /// 構成：
    ///   AR Session                       … ARSession（OpenXR: Meta のセッション）
    ///   XR Origin (Quest)                … XROrigin（追跡原点 Floor）
    ///     Camera Offset
    ///       Main Camera                  … 背景 = 単色・透明（パススルー）、TrackedPoseDriver、ARCameraManager（有効でパススルー）
    ///   Directional Light
    ///   PlatformRig_Quest                … PlatformRig ＋ XRRuntimeSwitch
    ///     Spatial                        … XRHeadSpatialProvider（XR・Editor 共通）
    ///     XR / Sword (Controller)        … XRControllerSwordPoseSource（XR のときだけ有効）
    ///     Editor / Sword (Mouse)         … EditorSwordPoseSource（XR が無いときだけ有効）
    ///   Battle                           … FixedEnemyPlacement、SwordPoseDebugView、SwordHitDetector（Q1：命中の判定と表示）、
    ///                                      BattleController・BattleView（Q2：HP・撃破・終了／再挑戦）
    /// 剣の調整値アセット（Assets/_Project/Settings/SwordTuning.asset、仮の値）が無ければ作って割り当てる。
    /// </summary>
    internal static class QuestSceneBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/Quest_MR.unity";
        public const string TuningAssetPath = "Assets/_Project/Settings/SwordTuning.asset";

        public static bool Build(bool overwrite)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null && !overwrite)
            {
                Debug.LogWarning($"[OrdinalScale] {ScenePath} は既にあります。作り直す場合は上書きを選んでください。");
                return false;
            }

            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // パススルーを隠さないよう、スカイボックスを使わない（Meta の Passthrough 導入手順）
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.55f);

            new GameObject("AR Session", typeof(ARSession));

            var light = new GameObject("Directional Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // --- XR Origin ---
            var originGo = new GameObject("XR Origin (Quest)");
            var offsetGo = new GameObject("Camera Offset");
            offsetGo.transform.SetParent(originGo.transform, false);

            var cameraGo = new GameObject("Main Camera") { tag = "MainCamera" };
            cameraGo.transform.SetParent(offsetGo.transform, false);
            var camera = cameraGo.AddComponent<Camera>();
            // パススルーはカメラ出力の後ろに合成されるため、背景は単色・アルファ0にする（Unity OpenXR: Meta の要件）
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 50f;
            cameraGo.AddComponent<AudioListener>();

            var poseDriver = cameraGo.AddComponent<InputTrackedPoseDriver>();
            poseDriver.positionInput = new InputActionProperty(new InputAction("Head Position", InputActionType.Value,
                "<XRHMD>/centerEyePosition", expectedControlType: "Vector3"));
            poseDriver.rotationInput = new InputActionProperty(new InputAction("Head Rotation", InputActionType.Value,
                "<XRHMD>/centerEyeRotation", expectedControlType: "Quaternion"));
            poseDriver.trackingStateInput = new InputActionProperty(new InputAction("Head Tracking State", InputActionType.Value,
                "<XRHMD>/trackingState", expectedControlType: "Integer"));

            // ARCameraManager が有効な間、Meta Quest: Camera (Passthrough) 機能がパススルーを表示する
            var cameraManager = cameraGo.AddComponent<ARCameraManager>();

            var origin = originGo.AddComponent<XROrigin>();
            origin.Origin = originGo;
            origin.CameraFloorOffsetObject = offsetGo;
            origin.Camera = camera;
            origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;

            // --- PlatformRig ---
            var rigGo = new GameObject("PlatformRig_Quest");
            rigGo.AddComponent<PlatformRig>();
            var runtimeSwitch = rigGo.AddComponent<XRRuntimeSwitch>();

            var spatialGo = new GameObject("Spatial");
            spatialGo.transform.SetParent(rigGo.transform, false);
            var spatial = spatialGo.AddComponent<XRHeadSpatialProvider>();

            var xrGo = new GameObject("XR");
            xrGo.transform.SetParent(rigGo.transform, false);
            var controllerSwordGo = new GameObject("Sword (Controller)");
            controllerSwordGo.transform.SetParent(xrGo.transform, false);
            var controllerSword = controllerSwordGo.AddComponent<XRControllerSwordPoseSource>();

            var editorGo = new GameObject("Editor");
            editorGo.transform.SetParent(rigGo.transform, false);
            var mouseSwordGo = new GameObject("Sword (Mouse)");
            mouseSwordGo.transform.SetParent(editorGo.transform, false);
            var mouseSword = mouseSwordGo.AddComponent<EditorSwordPoseSource>();

            SetObject(spatial, "headCamera", camera);
            SetObject(spatial, "floorReference", originGo.transform);
            SetObject(controllerSword, "trackingSpace", offsetGo.transform);
            SetObject(mouseSword, "viewCamera", camera);
            SetObject(runtimeSwitch, "editorCamera", cameraGo.transform);
            SetArray(runtimeSwitch, "xrOnlyObjects", xrGo);
            SetArray(runtimeSwitch, "editorOnlyObjects", editorGo);
            SetArray(runtimeSwitch, "xrOnlyBehaviours", origin, poseDriver, cameraManager);

            // --- 戦闘（Q0 は配置と剣の姿勢表示まで） ---
            var battleGo = new GameObject("Battle");
            var placement = battleGo.AddComponent<FixedEnemyPlacement>();
            var swordView = battleGo.AddComponent<SwordPoseDebugView>();
            var rig = rigGo.GetComponent<PlatformRig>();
            SetObject(placement, "rig", rig);
            SetObject(swordView, "rig", rig);

            // Q1：剣の命中判定（Core）を接続し、命中・不命中をログと色で示す
            var hitDetector = battleGo.AddComponent<SwordHitDetector>();
            SetObject(hitDetector, "rig", rig);
            SetObject(hitDetector, "placement", placement);
            SetObject(hitDetector, "bladeView", swordView);
            SetObject(hitDetector, "tuning", EnsureTuningAsset());

            // Q2：有効な命中を敵の HP へ接続し、撃破・勝利表示・「終了」「再挑戦」を通す
            var battleView = battleGo.AddComponent<BattleView>();
            var battle = battleGo.AddComponent<BattleController>();
            SetObject(battle, "placement", placement);
            SetObject(battle, "detector", hitDetector);
            SetObject(battle, "view", battleView);

            var saved = EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log(saved
                ? $"[OrdinalScale] Quest シーンを作成しました: {ScenePath}（ビルド対象一覧には追加していません。APK ビルドはメニュー 5 がこのシーンを指定します）"
                : $"[OrdinalScale] Quest シーンの保存に失敗しました: {ScenePath}");
            return saved;
        }

        /// <summary>剣の調整値アセットを読み込む。無ければ既定値（仮の値）で作る。既にあれば値は変えない。</summary>
        public static SwordTuningAsset EnsureTuningAsset()
        {
            var asset = AssetDatabase.LoadAssetAtPath<SwordTuningAsset>(TuningAssetPath);
            if (asset != null) return asset;

            asset = ScriptableObject.CreateInstance<SwordTuningAsset>();
            AssetDatabase.CreateAsset(asset, TuningAssetPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[OrdinalScale] 剣の調整値アセットを作成しました（仮の値）: {TuningAssetPath}");
            return asset;
        }

        private static void SetObject(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogError($"[OrdinalScale] {target.GetType().Name}.{field} が見つかりません（フィールド名の変更に追従してください）。");
                return;
            }

            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetArray(Object target, string field, params Object[] values)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null || !prop.isArray)
            {
                Debug.LogError($"[OrdinalScale] {target.GetType().Name}.{field} が配列として見つかりません。");
                return;
            }

            prop.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>シーンの必須構成を確かめる（検証メニューから呼ぶ）。開いていなければ追加で開いて閉じる。</summary>
        public static void Inspect(EvidenceReport r)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                r.Check("Quest シーン", false, $"{ScenePath} がありません（メニュー 3 で作成）");
                return;
            }

            var scene = SceneManager.GetSceneByPath(ScenePath);
            var openedHere = !scene.isLoaded;
            if (openedHere) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            try
            {
                r.Check("Quest シーン", true, ScenePath);

                XROrigin origin = null;
                ARSession session = null;
                PlatformRig rig = null;
                FixedEnemyPlacement placement = null;
                SwordHitDetector hitDetector = null;
                BattleController battle = null;
                foreach (var root in scene.GetRootGameObjects())
                {
                    if (origin == null) origin = root.GetComponentInChildren<XROrigin>(true);
                    if (session == null) session = root.GetComponentInChildren<ARSession>(true);
                    if (rig == null) rig = root.GetComponentInChildren<PlatformRig>(true);
                    if (placement == null) placement = root.GetComponentInChildren<FixedEnemyPlacement>(true);
                    if (hitDetector == null) hitDetector = root.GetComponentInChildren<SwordHitDetector>(true);
                    if (battle == null) battle = root.GetComponentInChildren<BattleController>(true);
                }

                r.Check("AR Session", session != null, session != null ? "あり" : "なし");
                r.Check("XR Origin（追跡原点 Floor）", origin != null && origin.RequestedTrackingOriginMode == XROrigin.TrackingOriginMode.Floor,
                    origin != null ? origin.RequestedTrackingOriginMode.ToString() : "なし");

                var cam = origin != null ? origin.Camera : null;
                r.Check("カメラ背景（単色・アルファ0）", cam != null && cam.clearFlags == CameraClearFlags.SolidColor && cam.backgroundColor.a == 0f,
                    cam != null ? $"{cam.clearFlags} alpha={cam.backgroundColor.a}" : "カメラなし");
                r.Check("ARCameraManager（パススルー）", cam != null && cam.GetComponent<ARCameraManager>() != null,
                    cam != null && cam.GetComponent<ARCameraManager>() != null ? "あり" : "なし");
                r.Check("TrackedPoseDriver（頭部）", cam != null && cam.GetComponent<InputTrackedPoseDriver>() != null,
                    cam != null && cam.GetComponent<InputTrackedPoseDriver>() != null ? "あり" : "なし");

                var swordSources = rig != null ? rig.GetComponentsInChildren<ISwordPoseSource>(true).Length : 0;
                r.Check("PlatformRig と剣の入力源（コントローラ・マウス）", rig != null && swordSources >= 2, $"rig={(rig != null ? "あり" : "なし")} 剣の入力源={swordSources}");
                r.Check("固定配置（FixedEnemyPlacement）", placement != null, placement != null ? "あり" : "なし");

                // Q1：命中判定の接続と調整値（値は仮。受入時に固定して記録する）
                var tuningAsset = hitDetector != null ? new SerializedObject(hitDetector).FindProperty("tuning")?.objectReferenceValue as SwordTuningAsset : null;
                r.Check("剣の命中判定（SwordHitDetector）", hitDetector != null, hitDetector != null ? "あり" : "なし（メニュー3でシーンを作り直す）");
                var battleHits = battle != null ? new SerializedObject(battle).FindProperty("hitsToDefeat") : null;
                r.Check("戦闘の進行（BattleController：HP・撃破・終了／再挑戦）", battle != null && battle.GetComponent<BattleView>() != null,
                    battle != null ? $"あり（撃破に必要な有効命中 {(battleHits != null ? battleHits.intValue.ToString() : "?")}・試遊で調整する値）" : "なし（メニュー3でシーンを作り直す）");
                r.Check("剣の調整値アセット", tuningAsset != null, tuningAsset != null ? AssetDatabase.GetAssetPath(tuningAsset) : "未設定（Core の既定値で動く）");
                if (tuningAsset != null)
                {
                    r.Info("剣の調整値（仮の値・Quest 実機で未確認）",
                        tuningAsset.TryCreate(out var t, out var error) ? t.ToString() : $"不正: {error}");
                }
            }
            finally
            {
                if (openedHere) EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
