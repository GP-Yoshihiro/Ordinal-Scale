using OrdinalScale.Core.Combat;
using OrdinalScale.Platform;
using UnityEngine;

namespace OrdinalScale.Gameplay.Combat
{
    /// <summary>
    /// Q0：ISwordPoseSource から届く刃の姿勢を見える棒として表示し、刃先の速さをログと画面に出す。
    /// 剣の入力が Gameplay まで届いていることと、Q1 の振り判定へ渡す値（BladeSample）を確かめるための表示で、命中判定はしない。
    /// </summary>
    public sealed class SwordPoseDebugView : MonoBehaviour
    {
        [SerializeField] private PlatformRig rig;
        [SerializeField] private Color bladeColor = new Color(0.3f, 0.95f, 1f);
        [Tooltip("刃の太さ（m）。")]
        [SerializeField] private float bladeThickness = 0.02f;
        [Tooltip("刃先の速さをログに出す間隔（秒）。Quest では adb logcat で読む。")]
        [SerializeField] private float logIntervalSeconds = 2f;

        private Transform _blade;
        private BladeSample _previous;
        private bool _hasPrevious;
        private bool _wasTracked;
        private float _tipSpeed;
        private float _maxTipSpeedSinceLog;
        private float _nextLogTime;
        private GUIStyle _labelStyle;

        /// <summary>最新の刃先の速さ（m/s）。追跡外なら 0。</summary>
        public float TipSpeed => _tipSpeed;

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
                Debug.Log($"[OrdinalScale][Q0] 剣の追跡: {(tracked ? "開始" : "途切れ")}", this);
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
            if (!_wasTracked) return;
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
            // 表示専用。命中判定は Q1 で前後フレームの掃引により行うので、物理コライダーは持たせない
            Destroy(go.GetComponent<Collider>());
            var r = go.GetComponent<Renderer>();
            if (r != null) r.material.color = bladeColor;
            go.SetActive(false);
            return go.transform;
        }
    }
}
