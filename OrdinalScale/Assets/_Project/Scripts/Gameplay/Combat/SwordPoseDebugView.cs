using OrdinalScale.Core.Combat;
using OrdinalScale.Platform;
using UnityEngine;

namespace OrdinalScale.Gameplay.Combat
{
    /// <summary>
    /// ISwordPoseSource から届く刃の姿勢を見える棒として表示し、刃先の速さを定期的にログへ出す。命中判定はしない（SwordHitDetector が行う）。
    /// Editor ではドラッグ中だけ、Quest ではコントローラを追跡している間だけ刃が見える。
    /// 刃の色は SwordHitDetector が SetTint で変える（振り中・命中など）。Quest のヘッドセット内でも見える最小限の反応。
    /// </summary>
    public sealed class SwordPoseDebugView : MonoBehaviour
    {
        [SerializeField] private PlatformRig rig;
        [SerializeField] private Color bladeColor = new Color(0.3f, 0.95f, 1f);
        [Tooltip("刃の太さ（m）。")]
        [SerializeField] private float bladeThickness = 0.02f;
        [Tooltip("刃先の速さをログに出す間隔（秒）。Quest では adb logcat で読む。")]
        [SerializeField] private float logIntervalSeconds = 2f;
        [Tooltip("画面左上に刃先の速さを出す（SwordHitDetector の表示と重なるため既定は出さない）。")]
        [SerializeField] private bool showSpeedLabel = false;

        private Transform _blade;
        private Material _bladeMaterial;
        private BladeSample _previous;
        private bool _hasPrevious;
        private bool _wasTracked;
        private float _tipSpeed;
        private float _maxTipSpeedSinceLog;
        private float _nextLogTime;
        private GUIStyle _labelStyle;

        /// <summary>最新の刃先の速さ（m/s）。追跡外なら 0。</summary>
        public float TipSpeed => _tipSpeed;

        /// <summary>既定の刃の色。</summary>
        public Color BaseColor => bladeColor;

        /// <summary>刃の色を変える（SwordHitDetector から呼ぶ）。</summary>
        public void SetTint(Color color)
        {
            if (_bladeMaterial != null) _bladeMaterial.color = color;
        }

        private void Reset()
        {
            rig = FindAnyObjectByType<PlatformRig>();
        }

        private void Awake()
        {
            if (rig == null) rig = FindAnyObjectByType<PlatformRig>();
            _blade = CreateBladeVisual();
        }

        private void Update()
        {
            var source = rig != null ? rig.Sword : null;
            if (source == null)
            {
                SetVisible(false);
                return;
            }

            var tracked = source.TryGetBladePose(out var pose);
            var sample = pose.ToSample();

            _tipSpeed = _hasPrevious && BladeSample.TryTipSpeed(_previous, sample, out var speed) ? speed : 0f;
            if (_tipSpeed > _maxTipSpeedSinceLog) _maxTipSpeedSinceLog = _tipSpeed;
            _previous = sample;
            _hasPrevious = true;

            if (tracked != _wasTracked)
            {
                Debug.Log($"[OrdinalScale][Q0] 剣: {(tracked ? "表示（追跡中・ドラッグ中）" : "非表示（追跡の途切れ・ドラッグ終了）")}", this);
                _wasTracked = tracked;
            }

            if (tracked && Time.time >= _nextLogTime)
            {
                Debug.Log($"[OrdinalScale][Q0] 剣 tip=({pose.Tip.x:0.00},{pose.Tip.y:0.00},{pose.Tip.z:0.00}) " +
                          $"len={sample.Length:0.00}m 刃先最大速度={_maxTipSpeedSinceLog:0.00}m/s（直近{logIntervalSeconds:0}秒）", this);
                _maxTipSpeedSinceLog = 0f;
                _nextLogTime = Time.time + logIntervalSeconds;
            }

            SetVisible(tracked);
            if (!tracked) return;

            var axis = pose.Tip - pose.Hilt;
            _blade.SetPositionAndRotation((pose.Hilt + pose.Tip) * 0.5f, Quaternion.FromToRotation(Vector3.up, axis));
            // Unity の円柱は高さ2m・直径1m
            _blade.localScale = new Vector3(bladeThickness, axis.magnitude * 0.5f, bladeThickness);
        }

        private void OnGUI()
        {
            // Editor の確認用（Quest のヘッドセット内には表示されない）
            if (!showSpeedLabel || !_wasTracked) return;
            if (_labelStyle == null) _labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 18 };
            // 既定フォントに日本語が無い環境でも読めるよう英字で出す
            GUI.Label(new Rect(12f, 12f, 420f, 28f), $"Blade tip speed {_tipSpeed:0.00} m/s", _labelStyle);
        }

        private void SetVisible(bool visible)
        {
            if (_blade != null && _blade.gameObject.activeSelf != visible) _blade.gameObject.SetActive(visible);
        }

        private Transform CreateBladeVisual()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "BladeDebug";
            go.transform.SetParent(transform, false);
            // 表示専用。命中判定は Core の掃引で行うので、物理コライダーは持たせない
            Destroy(go.GetComponent<Collider>());
            var r = go.GetComponent<Renderer>();
            if (r != null)
            {
                _bladeMaterial = r.material;
                _bladeMaterial.color = bladeColor;
            }

            go.SetActive(false);
            return go.transform;
        }
    }
}
