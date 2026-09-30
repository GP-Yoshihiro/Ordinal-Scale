using System;
using System.Text;
using OrdinalScale.Core.Spatial;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace OrdinalScale.Platform.ARFoundation
{
    /// <summary>
    /// iPhone 先行検証（S1）の証拠用オーバーレイ。
    /// 端末・OS・Unity版、カメラ許可、ARセッション状態、検出平面数、配置可否とその理由、FPS、時刻を画面に出し、
    /// スクリーンショット1枚で「いつ・どの環境で・どういう状態だったか」を残せるようにする。
    /// UI 素材やシーン設定を増やさないため IMGUI で描く（検証用。製品UIには使わない）。
    /// </summary>
    public sealed class ARStatusOverlay : MonoBehaviour
    {
        [SerializeField] private ARFoundationSpatialProvider spatial;
        [SerializeField] private bool visible = true;
        [Tooltip("文字の大きさの倍率。端末の画面密度に合わせた自動拡大に掛け合わされる。")]
        [SerializeField, Range(0.5f, 3f)] private float uiScale = 1f;
        [Tooltip("表示テキストの更新間隔（秒）。毎フレーム文字列を作らないため。")]
        [SerializeField] private float refreshInterval = 0.25f;

        private readonly StringBuilder _sb = new StringBuilder(512);
        private string _staticInfo;
        private readonly GUIContent _content = new GUIContent(string.Empty);
        private float _nextRefresh;
        private float _smoothedFps;
        private GUIStyle _style;

        private void Reset()
        {
            spatial = FindAnyObjectByType<ARFoundationSpatialProvider>();
        }

        private void Awake()
        {
            if (spatial == null) spatial = FindAnyObjectByType<ARFoundationSpatialProvider>();
            _staticInfo =
                $"Ordinal-Scale iPhone AR S1  app {Application.version}\n" +
                $"Unity {Application.unityVersion} | {SystemInfo.operatingSystem}\n" +
                $"Device {SystemInfo.deviceModel}";
        }

        private void Update()
        {
            var dt = Time.unscaledDeltaTime;
            if (dt > 0f) _smoothedFps = _smoothedFps <= 0f ? 1f / dt : Mathf.Lerp(_smoothedFps, 1f / dt, 0.1f);

            if (Time.unscaledTime < _nextRefresh) return;
            _nextRefresh = Time.unscaledTime + refreshInterval;
            _content.text = BuildText();
        }

        private string BuildText()
        {
            _sb.Clear();
            _sb.AppendLine(_staticInfo);
            _sb.Append("Session: ").Append(ARSession.state).Append(" / reason: ").Append(ARSession.notTrackingReason).AppendLine();

            if (spatial == null)
            {
                _sb.AppendLine("Spatial provider: NOT FOUND");
            }
            else
            {
                var status = spatial.Status;
                var reason = PlacementGate.Evaluate(status);
                _sb.Append("Camera: ").Append(status.CameraPermission).AppendLine();
                _sb.Append("Tracking: ").Append(status.TrackingPhase).AppendLine();
                _sb.Append("Horizontal planes: ").Append(status.HorizontalPlaneCount).AppendLine();
                _sb.Append("Placement: ")
                    .Append(reason == PlacementBlockReason.None ? "READY" : "BLOCKED " + reason)
                    .AppendLine();
                if (reason != PlacementBlockReason.None) _sb.Append("  -> ").AppendLine(Hint(reason));
            }

            _sb.Append("FPS: ").Append(_smoothedFps.ToString("0.0")).AppendLine();
            _sb.Append("Time: ").Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            return _sb.ToString();
        }

        /// <summary>利用者が次にすべきことの案内。日本語が表示できない場合に備え、英語の理由コードも併記している。</summary>
        private static string Hint(PlacementBlockReason reason)
        {
            switch (reason)
            {
                case PlacementBlockReason.CameraPermissionDenied: return "設定アプリ > OrdinalScale > カメラ をオンにして再起動";
                case PlacementBlockReason.TrackingUnsupported: return "この端末ではAR追跡を使えません";
                case PlacementBlockReason.TrackingInitializing: return "AR追跡を準備中です。端末をゆっくり動かしてください";
                case PlacementBlockReason.TrackingLimited: return "追跡が不安定です（暗い・模様が少ない・動きが速い）";
                case PlacementBlockReason.NoPlaneDetected: return "床をゆっくり映して平面を検出させてください";
                case PlacementBlockReason.TargetNotOnPlane: return "検出された平面の上を選んでください";
                default: return string.Empty;
            }
        }

        private void OnGUI()
        {
            if (!visible) return;

            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.box)
                {
                    alignment = TextAnchor.UpperLeft,
                    fontSize = 14,
                    wordWrap = true,
                    richText = false,
                };
                _style.normal.textColor = Color.white;
            }

            // 高密度ディスプレイ（iPhone は約460dpi）でも読める大きさにする。dpi が取れない環境は画面高で近似
            var dpiScale = Screen.dpi > 0f ? Screen.dpi / 160f : Screen.height / 800f;
            var scale = Mathf.Max(1f, dpiScale) * uiScale;
            var saved = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

            // ノッチ／Dynamic Island を避けるため、セーフエリア内の上部に描く（IMGUI は左上原点）
            var safe = Screen.safeArea;
            var x = safe.xMin / scale + 8f;
            var y = (Screen.height - safe.yMax) / scale + 8f;
            var width = safe.width / scale - 16f;
            var height = _style.CalcHeight(_content, width);
            GUI.Box(new Rect(x, y, width, height), _content, _style);

            GUI.matrix = saved;
        }
    }
}
