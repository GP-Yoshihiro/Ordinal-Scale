using OrdinalScale.Core.Combat;
using OrdinalScale.Gameplay.Combat;
using OrdinalScale.Gameplay.Placement;
using UnityEngine;

namespace OrdinalScale.Gameplay.Battle
{
    /// <summary>
    /// Q2：剣の有効な命中を敵の HP（Core の BattleSession／Health）へ接続し、撃破・勝利表示・「終了」「再挑戦」を通す（仕様 E3・E4）。
    /// - SwordHitDetector の出来事のうち「命中」だけを数える。空振り・ゆっくり接触・同じ振りでの再接触は HP を減らさない。
    /// - HP が0になったら撃破：剣の判定を止め（攻撃無効）、敵を消して「VICTORY」と2択を出す。
    /// - 再挑戦：敵の HP を全快、剣の判定と回数を初期化、敵を正面に置き直す。
    /// - 終了：実機は Application.Quit、Editor はログで終了要求を確認（AppQuit）。
    /// 2択の操作は Editor の画面ボタン。Quest のコントローラで選ぶ方法は Q4 で追加する（今はヘッドセット内から選べない）。
    /// 撃破に必要な有効命中数（初期10）は試遊で調整する値で、受入時に固定して記録する。
    /// </summary>
    [DefaultExecutionOrder(150)]
    public sealed class BattleController : MonoBehaviour
    {
        [SerializeField] private FixedEnemyPlacement placement;
        [SerializeField] private SwordHitDetector detector;
        [SerializeField] private BattleView view;
        [Tooltip("撃破に必要な有効命中数（＝敵の最大HP、1命中1ダメージ）。初期10。試遊で調整し、受入時は固定して記録する。再生中の変更は次の再生から反映。")]
        [SerializeField, Min(1)] private int hitsToDefeat = BattleSession.DefaultHitsToDefeat;
        [Tooltip("撃破してから敵を消すまでの時間（秒）。最後の命中の光が見えるようにする。")]
        [SerializeField, Min(0f)] private float defeatHideDelay = 0.4f;
        [Tooltip("画面右上に HP と状態、決着後に2択を出す（Editor 用。Quest のヘッドセット内には出ない）。")]
        [SerializeField] private bool showHud = true;

        private BattleSession _session;
        private float _hideAt = -1f;
        private Vector3 _enemyPosition;
        private GUIStyle _hudStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _buttonStyle;
        private BattleChoice? _pendingChoice;

        public BattleSession Session => _session;

        private void Reset()
        {
            placement = FindAnyObjectByType<FixedEnemyPlacement>();
            detector = FindAnyObjectByType<SwordHitDetector>();
            view = FindAnyObjectByType<BattleView>();
        }

        private void Awake()
        {
            if (placement == null) placement = FindAnyObjectByType<FixedEnemyPlacement>();
            if (detector == null) detector = FindAnyObjectByType<SwordHitDetector>();
            if (view == null) view = FindAnyObjectByType<BattleView>();
            if (placement == null || detector == null)
                Debug.LogError($"[{nameof(BattleController)}] FixedEnemyPlacement または SwordHitDetector がありません。", this);

            _session = new BattleSession(hitsToDefeat);
            _session.Victory += OnVictory;
            _session.Retried += OnRetried;
            _session.QuitRequestedEvent += OnQuitRequested;
            Debug.Log($"[OrdinalScale][Q2] 戦闘開始：撃破に必要な有効命中 {_session.HitsToDefeat}（試遊で調整する値）・1命中1ダメージ", this);
        }

        private void OnEnable()
        {
            if (detector != null) detector.Struck += OnStruck;
            if (placement != null) placement.Placed += OnEnemyPlaced;
        }

        private void OnDisable()
        {
            if (detector != null) detector.Struck -= OnStruck;
            if (placement != null) placement.Placed -= OnEnemyPlaced;
        }

        private void Update()
        {
            // 画面ボタンの選択は OnGUI の途中で状態を変えず（IMGUI の配置が崩れるため）、次の Update で処理する
            if (_pendingChoice.HasValue)
            {
                var choice = _pendingChoice.Value;
                _pendingChoice = null;
                Choose(choice);
            }

            if (_hideAt >= 0f && Time.time >= _hideAt)
            {
                _hideAt = -1f;
                var enemy = placement != null ? placement.Enemy : null;
                if (enemy != null) enemy.SetActive(false);
                if (view != null) view.HideHealthBar();
            }
        }

        /// <summary>決着後の選択（Editor の画面ボタン、Q4 ではコントローラの UI から呼ぶ）。</summary>
        public void Choose(BattleChoice choice)
        {
            if (!_session.Choose(choice))
                Debug.LogWarning($"[OrdinalScale][Q2] 今は「{Label(choice)}」を選べません（状態 {_session.State}）", this);
        }

        private void OnStruck(StrikeEvent e)
        {
            if (e.Kind != StrikeEventKind.Hit) return;

            var result = _session.RegisterValidHit();
            var hp = _session.EnemyHealth;
            switch (result)
            {
                case BattleHitResult.Damaged:
                case BattleHitResult.Defeated:
                    if (view != null) view.SetHealth(hp.Current, hp.Max);
                    Debug.Log($"[OrdinalScale][Q2] 敵HP {hp.Current}/{hp.Max}（有効命中 {_session.ValidHits}・振り#{e.SwingId}）", this);
                    break;
                case BattleHitResult.IgnoredNotFighting:
                    Debug.Log($"[OrdinalScale][Q2] 撃破後の命中を無視（攻撃は無効） HP {hp.Current}/{hp.Max}", this);
                    break;
            }
        }

        private void OnEnemyPlaced(Vector3 position)
        {
            _enemyPosition = position;
            var enemy = placement.Enemy;
            if (view == null || enemy == null) return;

            var volume = enemy.GetComponent<EnemyHitVolume>();
            view.Bind(enemy.transform, volume != null ? volume.Height : EnemyPlaceholder.BodyHeight);
            view.SetHealth(_session.EnemyHealth.Current, _session.EnemyHealth.Max);
        }

        private void OnVictory()
        {
            // 撃破後は攻撃を無効化：剣の判定そのものを止める（BattleSession も撃破後の命中を無視する）
            if (detector != null) detector.enabled = false;
            var enemy = placement != null ? placement.Enemy : null;
            if (enemy != null) _enemyPosition = enemy.transform.position;
            _hideAt = Time.time + defeatHideDelay;
            if (view != null) view.ShowVictory(true, _enemyPosition);
            Debug.Log($"[OrdinalScale][Q2] 撃破：有効命中 {_session.ValidHits} 回（ラウンド {_session.Round}）。「終了」「再挑戦」を選べます", this);
        }

        private void OnRetried(int round)
        {
            _hideAt = -1f;
            if (view != null) view.ShowVictory(false, _enemyPosition);
            if (detector != null)
            {
                detector.ResetCounters();
                detector.enabled = true;
            }

            // 次の安定したフレームで、その時の正面に敵を置き直す（FixedEnemyPlacement が SetActive(true) にする）
            if (placement != null) placement.Reposition();
            Debug.Log($"[OrdinalScale][Q2] 再挑戦：ラウンド {round}。敵・HP（{_session.EnemyHealth.Current}/{_session.EnemyHealth.Max}）・剣の判定を初期化", this);
        }

        private void OnQuitRequested()
        {
            AppQuit.Request(this);
        }

        private void OnGUI()
        {
            if (!showHud || _session == null) return;
            if (_hudStyle == null)
            {
                _hudStyle = new GUIStyle(GUI.skin.label) { fontSize = 15 };
                _titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 32, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
                _buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 20 };
            }

            // 右上：HP と状態（英字。ログは日本語）
            var hp = _session.EnemyHealth;
            GUILayout.BeginArea(new Rect(Screen.width - 330f, 10f, 320f, 110f), GUI.skin.box);
            GUILayout.Label($"Q2 battle   round {_session.Round}", _hudStyle);
            GUILayout.Label($"enemy HP {hp.Current}/{hp.Max}   valid hits {_session.ValidHits}", _hudStyle);
            GUILayout.Label($"state {_session.State.ToString().ToUpperInvariant()}{(_session.QuitRequested ? "   QUIT REQUESTED" : "")}", _hudStyle);
            GUILayout.EndArea();

            if (_session.State == BattleState.Fighting) return;

            // 中央：決着と2択（E4）。日本語が出ない環境でも読めるよう英字を併記
            var w = 420f;
            var h = 190f;
            GUILayout.BeginArea(new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h), GUI.skin.box);
            GUILayout.Label("VICTORY", _titleStyle);
            GUILayout.Space(10f);
            foreach (var choice in _session.AvailableChoices)
            {
                if (GUILayout.Button(Label(choice), _buttonStyle, GUILayout.Height(48f))) _pendingChoice = choice;
            }

            if (_session.QuitRequested) GUILayout.Label("Quit requested (Editor keeps running; see Console)", _hudStyle);
            GUILayout.EndArea();
        }

        private static string Label(BattleChoice choice)
        {
            return choice == BattleChoice.Quit ? "終了 / Quit" : "再挑戦 / Retry";
        }
    }
}
