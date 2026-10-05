using OrdinalScale.Core.Spatial;
using UnityEngine;

namespace OrdinalScale.Gameplay.Placement
{
    /// <summary>
    /// S2 の配置結果を画面下部に出す検証用表示。直近の結果（配置・移動・抑止とその理由）を数秒表示し、
    /// 配置・抑止の回数は常に表示する。スクリーンショット1枚で操作と結果を対応づけるため。
    /// IMGUI で描く（検証用。製品UIには使わない）。
    /// </summary>
    public sealed class PlacementFeedbackView : MonoBehaviour
    {
        [SerializeField] private EnemyPlacementController controller;
        [Tooltip("直近の結果を表示し続ける秒数。")]
        [SerializeField] private float showSeconds = 4f;
        [SerializeField, Range(0.5f, 3f)] private float uiScale = 1f;

        private readonly GUIContent _message = new GUIContent(string.Empty);
        private readonly GUIContent _counts = new GUIContent(string.Empty);
        private bool _lastBlocked;
        private float _hideAt;
        private GUIStyle _style;

        private void Reset()
        {
            controller = FindAnyObjectByType<EnemyPlacementController>();
        }

        private void Awake()
        {
            if (controller == null) controller = FindAnyObjectByType<EnemyPlacementController>();
            UpdateCounts();
        }

        private void OnEnable()
        {
            if (controller != null) controller.Attempted += OnAttempted;
        }

        private void OnDisable()
        {
            if (controller != null) controller.Attempted -= OnAttempted;
        }

        private void OnAttempted(PlacementAttemptResult result)
        {
            _message.text = PlacementMessages.Describe(result);
            _lastBlocked = result.IsBlocked;
            _hideAt = Time.unscaledTime + showSeconds;
            UpdateCounts();
        }

        private void UpdateCounts()
        {
            if (controller == null)
            {
                _counts.text = "S2 placement: controller NOT FOUND";
                return;
            }

            var s = controller.Session;
            _counts.text = $"S2 placement  enemy: {(s.HasEnemy ? "yes" : "no")}  placed/moved: {s.PlacedCount}  blocked: {s.BlockedCount}";
        }

        private void OnGUI()
        {
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.box)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 15,
                    wordWrap = true,
                };
            }

            var dpiScale = Screen.dpi > 0f ? Screen.dpi / 160f : Screen.height / 800f;
            var scale = Mathf.Max(1f, dpiScale) * uiScale;
            var saved = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

            // セーフエリアの下端（ホームインジケータの上）から積み上げる。IMGUI は左上原点
            var safe = Screen.safeArea;
            var x = safe.xMin / scale + 8f;
            var width = safe.width / scale - 16f;
            var bottom = (Screen.height - safe.yMin) / scale - 8f;

            var countsHeight = _style.CalcHeight(_counts, width);
            bottom -= countsHeight;
            _style.normal.textColor = Color.white;
            GUI.Box(new Rect(x, bottom, width, countsHeight), _counts, _style);

            if (Time.unscaledTime < _hideAt)
            {
                var messageHeight = _style.CalcHeight(_message, width);
                bottom -= messageHeight + 4f;
                // 抑止は橙、配置・移動は緑で、遠目の画面収録でも結果を区別できるようにする
                _style.normal.textColor = _lastBlocked ? new Color(1f, 0.6f, 0.2f) : new Color(0.4f, 1f, 0.5f);
                GUI.Box(new Rect(x, bottom, width, messageHeight), _message, _style);
            }

            GUI.matrix = saved;
        }
    }
}
