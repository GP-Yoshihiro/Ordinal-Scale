using UnityEngine;

namespace OrdinalScale.Gameplay.Battle
{
    /// <summary>
    /// 戦闘の最小限の表示（E3：HP の減少が見える）。プレハブやマテリアルのアセットを増やさず、実行時にプリミティブで作る。
    /// - 敵の頭上の HP バー（背景＋赤い残量。左詰めで減る）。常にカメラの方を向く。
    /// - 撃破時の「VICTORY」文字（敵がいた位置の上）。
    /// どちらもワールド空間に置くので、Editor の Game ビューと Quest のヘッドセット内の両方に出る（Quest では未確認）。
    /// 「終了」「再挑戦」の操作は Q2 では Editor の画面ボタン（BattleController）。Quest のコントローラでの選択は Q4。
    /// </summary>
    public sealed class BattleView : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float barWidth = 0.6f;
        [SerializeField, Min(0.01f)] private float barHeight = 0.06f;
        [Tooltip("体の上端からバーまでの高さ（m）。")]
        [SerializeField, Min(0f)] private float barAboveHead = 0.2f;
        [SerializeField] private Color barBackColor = new Color(0.12f, 0.12f, 0.12f);
        [SerializeField] private Color barFillColor = new Color(0.95f, 0.2f, 0.2f);
        [SerializeField] private Color victoryColor = new Color(1f, 0.85f, 0.2f);
        [Tooltip("VICTORY の文字の高さ（床からの m）。")]
        [SerializeField, Min(0.2f)] private float victoryHeight = 1.4f;

        private Transform _barRoot;
        private Transform _fill;
        private TextMesh _victory;
        private Transform _target;
        private float _targetHeight = 1.6f;
        private float _ratio = 1f;
        private Vector3 _victoryAnchor;
        private Camera _camera;

        private void Awake()
        {
            _camera = Camera.main;
            _barRoot = new GameObject("EnemyHealthBar").transform;
            _barRoot.SetParent(transform, false);
            CreateQuad("Back", _barRoot, barBackColor, new Vector3(barWidth, barHeight, 0.005f), Vector3.zero);
            _fill = CreateQuad("Fill", _barRoot, barFillColor, new Vector3(barWidth, barHeight * 0.75f, 0.006f), new Vector3(0f, 0f, -0.006f));
            _barRoot.gameObject.SetActive(false);

            var label = new GameObject("VictoryLabel");
            label.transform.SetParent(transform, false);
            _victory = label.AddComponent<TextMesh>();
            _victory.text = "VICTORY";
            _victory.anchor = TextAnchor.MiddleCenter;
            _victory.alignment = TextAlignment.Center;
            _victory.fontSize = 96;
            _victory.characterSize = 0.012f;
            _victory.color = victoryColor;
            // Unity 6 の組み込みフォント。見つからなくても HP バーと Editor の画面表示は動く
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null)
            {
                _victory.font = font;
                label.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            }

            label.SetActive(false);
        }

        /// <summary>HP バーを付ける敵と、その体の高さ。</summary>
        public void Bind(Transform enemy, float bodyHeight)
        {
            _target = enemy;
            _targetHeight = bodyHeight;
            _barRoot.gameObject.SetActive(enemy != null);
        }

        public void SetHealth(int current, int max)
        {
            _ratio = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
            if (_fill == null) return;
            // 左端を固定して右から減らす
            _fill.localScale = new Vector3(barWidth * _ratio, barHeight * 0.75f, 0.006f);
            _fill.localPosition = new Vector3(-(1f - _ratio) * barWidth * 0.5f, 0f, -0.006f);
            _fill.gameObject.SetActive(_ratio > 0f);
        }

        public void HideHealthBar()
        {
            _barRoot.gameObject.SetActive(false);
        }

        /// <summary>撃破の表示。position は敵の足元。</summary>
        public void ShowVictory(bool visible, Vector3 position)
        {
            _victoryAnchor = position + Vector3.up * victoryHeight;
            _victory.gameObject.SetActive(visible);
        }

        private void LateUpdate()
        {
            if (_camera == null) _camera = Camera.main;
            if (_camera == null) return;
            var cam = _camera.transform.position;

            if (_barRoot.gameObject.activeSelf && _target != null)
            {
                if (!_target.gameObject.activeInHierarchy)
                {
                    _barRoot.gameObject.SetActive(false);
                }
                else
                {
                    var p = _target.position + Vector3.up * (_targetHeight + barAboveHead);
                    _barRoot.SetPositionAndRotation(p, Billboard(p, cam));
                }
            }

            if (_victory.gameObject.activeSelf)
            {
                _victory.transform.SetPositionAndRotation(_victoryAnchor, Billboard(_victoryAnchor, cam));
            }
        }

        // 表示物の +Z をカメラと反対に向ける（文字が正しく読める向き）。水平だけ回し、傾けない
        private static Quaternion Billboard(Vector3 position, Vector3 camera)
        {
            var away = position - camera;
            away.y = 0f;
            return away.sqrMagnitude > 1e-6f ? Quaternion.LookRotation(away.normalized, Vector3.up) : Quaternion.identity;
        }

        private static Transform CreateQuad(string name, Transform parent, Color color, Vector3 scale, Vector3 localPosition)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localScale = scale;
            go.transform.localPosition = localPosition;
            // 表示専用。剣の判定は物理を使わないが、余計な当たり判定は持たせない
            Destroy(go.GetComponent<Collider>());
            var r = go.GetComponent<Renderer>();
            if (r != null) r.material.color = color;
            return go.transform;
        }
    }
}
