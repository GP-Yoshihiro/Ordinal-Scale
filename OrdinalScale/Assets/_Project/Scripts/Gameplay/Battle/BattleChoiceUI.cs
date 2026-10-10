using OrdinalScale.Core.Combat;
using OrdinalScale.Platform;
using UnityEngine;

namespace OrdinalScale.Gameplay.Battle
{
    /// <summary>
    /// Q4：決着後の「終了」「再挑戦」をヘッドセット内で選ぶ最小の UI（仕様 E4・Q-5 の操作方式の提案）。
    /// - 勝敗が決まったら、その時のプレイヤーの正面 1m・目の少し下に、大きなパネルを2枚（左＝終了、右＝再挑戦）出す。
    ///   以後は動かさない（頭を動かしても狙いやすいように固定）。
    /// - 剣先（コントローラの向き）をパネルに向けると明るくなり、先に小さな白い点が出る。トリガーの押し始めで決定。
    /// - 誤操作防止：表示から0.6秒は選べない、表示前から押していたトリガーは数えない、1回選んだら表示し直すまで選ばない（Core の ChoiceSelector）。
    /// - Editor ではマウスのクリック（ScreenPointerInput）でも同じパネルを選べる。画面上寄りのボタン（BattleController）も
    ///   TryChooseFromScreenButton を通るので、待ち時間と1回だけの制限をパネルと共有する。
    /// 入力は PlatformRig.Pointer（IPointerInput）だけを見る。Quest 実機での見え方・選びやすさは未確認。
    /// </summary>
    [DefaultExecutionOrder(160)]
    public sealed class BattleChoiceUI : MonoBehaviour
    {
        private const int QuitIndex = 0;
        private const int RetryIndex = 1;

        [SerializeField] private BattleController battle;
        [SerializeField] private PlatformRig rig;
        [Tooltip("プレイヤーからパネルまでの距離（m）。")]
        [SerializeField, Min(0.4f)] private float distance = 1.0f;
        [Tooltip("目の高さからパネルの中心までの下げ幅（m）。")]
        [SerializeField] private float belowEye = 0.2f;
        [SerializeField, Min(0.1f)] private float panelWidth = 0.5f;
        [SerializeField, Min(0.1f)] private float panelHeight = 0.3f;
        [Tooltip("2枚のパネルの中心の間隔（m）。")]
        [SerializeField, Min(0.2f)] private float spacing = 0.64f;
        [Tooltip("レイが届く距離（m）。")]
        [SerializeField, Min(0.5f)] private float maxRayDistance = 4f;
        [SerializeField] private Color quitColor = new Color(0.45f, 0.45f, 0.5f);
        [SerializeField] private Color retryColor = new Color(0.2f, 0.6f, 0.35f);
        [SerializeField] private Color hoverTint = new Color(1f, 1f, 1f);

        private readonly ChoiceSelector _selector = new ChoiceSelector();
        private readonly ChoicePanel[] _panels = new ChoicePanel[2];
        private readonly Transform[] _panelTransforms = new Transform[2];
        private readonly Material[] _panelMaterials = new Material[2];
        private Transform _root;
        private Transform _reticle;
        private TextMesh _header;
        private int _hover = -1;

        /// <summary>今選べるか（表示中・待ち時間が過ぎた・まだ選んでいない）。画面ボタンの有効・無効に使う。</summary>
        public bool IsArmed => _selector.IsArmed(Time.timeAsDouble);

        /// <summary>
        /// Editor の画面ボタンからの選択。パネルと同じ ChoiceSelector を通すので、表示から0.6秒の待ちと1回だけの制限を共有する。
        /// 選べなかったら false（パネルが出ていない・待ち時間中・選択済み）。
        /// </summary>
        public bool TryChooseFromScreenButton(BattleChoice choice)
        {
            if (!_selector.IsShown) return false;
            return Decide(_selector.Update(Time.timeAsDouble, true, choice == BattleChoice.Quit ? QuitIndex : RetryIndex), "画面ボタン");
        }

        private void Reset()
        {
            battle = FindAnyObjectByType<BattleController>();
            rig = FindAnyObjectByType<PlatformRig>();
        }

        private void Awake()
        {
            if (battle == null) battle = FindAnyObjectByType<BattleController>();
            if (rig == null) rig = FindAnyObjectByType<PlatformRig>();
            Build();
        }

        private void Update()
        {
            var session = battle != null ? battle.Session : null;
            var pointer = rig != null ? rig.Pointer : null;
            if (session == null) return;

            var decided = session.State != BattleState.Fighting;
            if (decided && !_selector.IsShown) ShowPanels(session.State == BattleState.Victory);
            else if (!decided && _selector.IsShown) HidePanels();

            if (!_selector.IsShown)
            {
                // 表示前の押し始めは捨てる（表示した瞬間に決まらないように）
                if (pointer != null) pointer.TryConsumeSelect(out _);
                return;
            }

            if (pointer == null) return;

            _hover = HitIndex(pointer.PointerRay, out var hoverPoint);
            UpdateHighlight(hoverPoint);

            var selectPressed = pointer.TryConsumeSelect(out var selectRay);
            var hit = selectPressed ? HitIndex(selectRay, out _) : -1;
            Decide(_selector.Update(Time.timeAsDouble, selectPressed, hit), "剣先で指してトリガー／Editor はクリック");
        }

        private bool Decide(int chosen, string source)
        {
            if (chosen < 0) return false;

            var choice = chosen == QuitIndex ? BattleChoice.Quit : BattleChoice.Retry;
            Debug.Log($"[OrdinalScale][Q4] 2択：{(choice == BattleChoice.Quit ? "終了" : "再挑戦")}を選択（{source}）", this);
            battle.Choose(choice);
            return true;
        }

        private void ShowPanels(bool victory)
        {
            var head = rig != null && rig.Spatial != null ? rig.Spatial.HeadPose : new Pose(Vector3.up * 1.6f, Quaternion.identity);
            var forward = head.rotation * Vector3.forward;
            forward.y = 0f;
            forward = forward.sqrMagnitude > 1e-4f ? forward.normalized : Vector3.forward;
            var right = Vector3.Cross(Vector3.up, forward).normalized;
            var center = head.position + forward * distance + Vector3.down * belowEye;

            _root.SetPositionAndRotation(center, Quaternion.LookRotation(forward, Vector3.up));
            _header.text = victory ? "VICTORY" : "DEFEAT";
            for (var i = 0; i < 2; i++)
            {
                var offset = (i == QuitIndex ? -0.5f : 0.5f) * spacing;
                var c = center + right * offset;
                _panels[i] = new ChoicePanel(c.x, c.y, c.z, right.x, right.y, right.z, 0f, 1f, 0f, panelWidth * 0.5f, panelHeight * 0.5f);
            }

            _root.gameObject.SetActive(true);
            _selector.Show(Time.timeAsDouble);
            Debug.Log("[OrdinalScale][Q4] 2択パネルを表示（左=終了・右=再挑戦。剣先で指してトリガー。表示から0.6秒後に選べる）", this);
        }

        private void HidePanels()
        {
            _selector.Hide();
            _root.gameObject.SetActive(false);
            _hover = -1;
        }

        private int HitIndex(Ray ray, out Vector3 point)
        {
            point = default;
            var best = -1;
            var bestDistance = float.MaxValue;
            for (var i = 0; i < 2; i++)
            {
                if (_panels[i].TryHit(ray.origin.x, ray.origin.y, ray.origin.z, ray.direction.x, ray.direction.y, ray.direction.z,
                        maxRayDistance, out var d) && d < bestDistance)
                {
                    best = i;
                    bestDistance = d;
                }
            }

            if (best >= 0) point = ray.origin + ray.direction * bestDistance;
            return best;
        }

        private void UpdateHighlight(Vector3 hoverPoint)
        {
            var armed = _selector.IsArmed(Time.timeAsDouble);
            for (var i = 0; i < 2; i++)
            {
                var baseColor = i == QuitIndex ? quitColor : retryColor;
                // 選べる前は暗く、指している間は明るく
                var c = !armed ? baseColor * 0.5f : i == _hover ? Color.Lerp(baseColor, hoverTint, 0.45f) : baseColor;
                c.a = 1f;
                _panelMaterials[i].color = c;
                _panelTransforms[i].localScale = new Vector3(panelWidth, panelHeight, 0.01f) * (i == _hover && armed ? 1.06f : 1f);
            }

            _reticle.gameObject.SetActive(_hover >= 0);
            if (_hover >= 0) _reticle.position = hoverPoint;
        }

        private void Build()
        {
            _root = new GameObject("ChoicePanels").transform;
            _root.SetParent(transform, false);
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            _header = CreateLabel("Header", _root, font, new Vector3(0f, panelHeight * 0.5f + 0.09f, 0f), 0.006f);
            for (var i = 0; i < 2; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = i == QuitIndex ? "QuitPanel" : "RetryPanel";
                go.transform.SetParent(_root, false);
                go.transform.localPosition = new Vector3((i == QuitIndex ? -0.5f : 0.5f) * spacing, 0f, 0f);
                go.transform.localScale = new Vector3(panelWidth, panelHeight, 0.01f);
                // 選択は Core の ChoicePanel で計算するので物理コライダーは使わない
                Destroy(go.GetComponent<Collider>());
                _panelMaterials[i] = go.GetComponent<Renderer>().material;
                _panelTransforms[i] = go.transform;

                // 日本語が出ない場合に備え英字を併記（URP でのレガシーフォントの表示は未確認）
                var label = CreateLabel("Label", _root, font, go.transform.localPosition + new Vector3(0f, 0f, -0.012f), 0.0045f);
                label.text = i == QuitIndex ? "QUIT\n終了" : "RETRY\n再挑戦";
            }

            var reticle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            reticle.name = "Reticle";
            reticle.transform.SetParent(transform, false);
            reticle.transform.localScale = Vector3.one * 0.025f;
            Destroy(reticle.GetComponent<Collider>());
            reticle.GetComponent<Renderer>().material.color = Color.white;
            _reticle = reticle.transform;
            reticle.SetActive(false);

            _root.gameObject.SetActive(false);
        }

        private static TextMesh CreateLabel(string name, Transform parent, Font font, Vector3 localPosition, float characterSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            var t = go.AddComponent<TextMesh>();
            t.anchor = TextAnchor.MiddleCenter;
            t.alignment = TextAlignment.Center;
            t.fontSize = 96;
            t.characterSize = characterSize;
            t.color = Color.white;
            if (font != null)
            {
                t.font = font;
                go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            }

            return t;
        }
    }
}
