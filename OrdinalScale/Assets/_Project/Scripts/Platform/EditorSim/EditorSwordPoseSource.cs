using OrdinalScale.Core.Combat;
using UnityEngine;
#if OS_HAS_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace OrdinalScale.Platform.EditorSim
{
    /// <summary>
    /// Editor で剣の姿勢を作る代替入力（条件 C7）。**マウスの左ボタンでドラッグしている間だけ**剣が出る。
    /// 刃先の速さ(m/s) = マウスの速さ(画面の高さ/秒) × metersPerScreenHeight（Core の DragBladeModel）。
    /// 押した瞬間、刃先はマウスの下にある「カメラ正面 tipDistance(m) の平面」上の点に置かれ、以後はマウスの移動量に比例して動く
    /// （係数によってはマウスの位置と刃先が少しずれる）。刃は肩の位置から刃先へ向かう長さ bladeLength の線分。
    /// 係数・距離・長さはすべて Editor 検証用の仮の値で、Quest の操作感とは別物。
    /// 同じフレームに何度呼ばれても同じ姿勢を返す（刃の表示と命中判定の両方が呼ぶため）。
    /// </summary>
    public sealed class EditorSwordPoseSource : MonoBehaviour, ISwordPoseSource
    {
        [SerializeField] private Camera viewCamera;
        [Tooltip("マウスを画面の高さ分動かしたときに刃先が動く距離（m）。刃先の速さ＝マウスの速さ（画面高/秒）×この値。仮の値。")]
        [SerializeField, Min(0.05f)] private float metersPerScreenHeight = 1f;
        [Tooltip("刃先が動く平面のカメラからの距離（m）。敵の固定配置の距離（FixedEnemyPlacement、既定2m）に合わせると、画面中央付近で体に触れる。")]
        [SerializeField, Min(0.3f)] private float tipDistance = 2f;
        [Tooltip("刃の長さ（m）。Quest 側と同じ仮の値。")]
        [SerializeField, Min(0.1f)] private float bladeLength = 0.9f;
        [Tooltip("刃の根元の向きを決める肩の位置（カメラから見た m。右・上・前）。")]
        [SerializeField] private Vector3 shoulderOffset = new Vector3(0.2f, -0.35f, 0.1f);

        private DragBladeModel _model;
        private int _cachedFrame = -1;
        private bool _cachedTracked;
        private BladePose _cachedPose;
        private float _mouseSpeedScreenHeights;
        private Vector2 _lastMouse;
        private double _lastMouseTime;

        /// <summary>直近のマウスの速さ（画面の高さ/秒）。ドラッグしていなければ0。</summary>
        public float MouseSpeedScreenHeightsPerSecond => _mouseSpeedScreenHeights;

        public float MetersPerScreenHeight => metersPerScreenHeight;

        private void Reset()
        {
            viewCamera = Camera.main;
        }

        private void Awake()
        {
            if (viewCamera == null) viewCamera = Camera.main;
            RebuildModel();
        }

        private void OnValidate()
        {
            // 再生中に Inspector で係数を変えたら、次のドラッグから反映する
            if (Application.isPlaying) RebuildModel();
        }

        private void RebuildModel()
        {
            _model = new DragBladeModel(metersPerScreenHeight, bladeLength, shoulderOffset.x, shoulderOffset.y, shoulderOffset.z);
        }

        public bool TryGetBladePose(out BladePose pose)
        {
            if (_cachedFrame != Time.frameCount)
            {
                _cachedFrame = Time.frameCount;
                _cachedTracked = Evaluate(out _cachedPose);
            }

            pose = _cachedPose;
            return _cachedTracked;
        }

        private bool Evaluate(out BladePose pose)
        {
            var now = Time.timeAsDouble;
            if (viewCamera == null || _model == null || !TryReadMouse(out var mouse, out var held) || Screen.height <= 0)
            {
                EndDrag();
                pose = BladePose.Untracked(now);
                return false;
            }

            if (!held)
            {
                EndDrag();
                pose = BladePose.Untracked(now);
                return false;
            }

            var camTransform = viewCamera.transform;
            if (!_model.IsDragging)
            {
                // 押した瞬間：マウスの下にある、カメラ正面 tipDistance の平面上の点から始める
                var ray = viewCamera.ScreenPointToRay(mouse);
                var localDir = camTransform.InverseTransformDirection(ray.direction);
                var scale = localDir.z > 1e-3f ? tipDistance / localDir.z : tipDistance;
                _model.Begin(localDir.x * scale, localDir.y * scale, tipDistance, mouse.x, mouse.y);
                _lastMouse = mouse;
                _lastMouseTime = now;
                _mouseSpeedScreenHeights = 0f;
            }
            else
            {
                _model.Move(mouse.x, mouse.y, Screen.height);
                var dt = now - _lastMouseTime;
                if (dt > 1e-4)
                {
                    _mouseSpeedScreenHeights = (float)((mouse - _lastMouse).magnitude / Screen.height / dt);
                    _lastMouse = mouse;
                    _lastMouseTime = now;
                }
            }

            _model.TryGetLocalBlade(out var hx, out var hy, out var hz, out var tx, out var ty, out var tz);
            var hilt = camTransform.TransformPoint(new Vector3(hx, hy, hz));
            var tip = camTransform.TransformPoint(new Vector3(tx, ty, tz));
            var axis = tip - hilt;
            var rotation = axis.sqrMagnitude > 1e-8f ? Quaternion.LookRotation(axis, camTransform.up) : camTransform.rotation;
            pose = new BladePose(hilt, tip, rotation, now, true);
            return true;
        }

        private void EndDrag()
        {
            _model?.End();
            _mouseSpeedScreenHeights = 0f;
        }

        private static bool TryReadMouse(out Vector2 screenPosition, out bool leftHeld)
        {
#if OS_HAS_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
            var mouse = Mouse.current;
            screenPosition = mouse != null ? mouse.position.ReadValue() : default;
            leftHeld = mouse != null && mouse.leftButton.isPressed;
            return mouse != null && IsInsideScreen(screenPosition);
#elif ENABLE_LEGACY_INPUT_MANAGER
            screenPosition = UnityEngine.Input.mousePosition;
            leftHeld = UnityEngine.Input.GetMouseButton(0);
            return IsInsideScreen(screenPosition);
#else
            screenPosition = default;
            leftHeld = false;
            return false;
#endif
        }

        // Game ビューの外にマウスが出たら剣を下ろす（追跡の途切れの再現にも使える）
        private static bool IsInsideScreen(Vector2 p)
        {
            return p.x >= 0f && p.y >= 0f && p.x <= Screen.width && p.y <= Screen.height;
        }
    }
}
