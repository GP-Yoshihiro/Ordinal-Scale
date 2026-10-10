using OrdinalScale.Core.Combat;
using OrdinalScale.Core.Spatial;
using OrdinalScale.Gameplay.Combat;
using OrdinalScale.Gameplay.Placement;
using OrdinalScale.Platform;
using UnityEngine;

namespace OrdinalScale.Gameplay.Battle
{
    /// <summary>
    /// 敵1体との戦闘の進行（Q2：E3・E4。Q3：Q-3 の敵の移動・反撃とプレイヤーの敗北、Q-5 の両結末からの2択）。
    /// - 剣：SwordHitDetector の出来事のうち「命中」だけを敵の HP に数える（空振り・ゆっくり接触・再接触は数えない）。
    /// - 敵：Core の EnemyBrain が、配置位置から安全範囲（既定半径1m）の内側でプレイヤーへ近づき、攻撃距離で止まって
    ///   予備動作（頭上に赤い予告）→ 攻撃。攻撃の瞬間にプレイヤーが届く距離にいれば当たり、プレイヤーの HP が1減る。
    /// - 決着：敵の HP 0 で勝利（敵を消す）、プレイヤーの HP 0 で敗北（敵はその場で止まる）。どちらも剣の判定と敵の行動を止め、
    ///   「VICTORY」／「DEFEAT」と「終了」「再挑戦」を出す。
    /// - 再挑戦：敵とプレイヤーの HP、剣の判定、敵の行動を初期化し、敵を正面に置き直す。終了：実機は Application.Quit、Editor はログ。
    /// 2択の操作は Editor の画面ボタン（Quest のコントローラでの選択は Q4）。数値はすべて仮の値で、受入時に固定して記録する。
    /// </summary>
    [DefaultExecutionOrder(150)]
    public sealed class BattleController : MonoBehaviour
    {
        [SerializeField] private FixedEnemyPlacement placement;
        [SerializeField] private SwordHitDetector detector;
        [SerializeField] private BattleView view;
        [Tooltip("プレイヤーの頭の位置を得るリグ（Q3）。")]
        [SerializeField] private PlatformRig rig;

        [Header("勝敗（仮の値。試遊で調整し、受入時に固定）")]
        [Tooltip("撃破に必要な有効命中数（＝敵の最大HP、1命中1ダメージ）。初期10。再生中の変更は次の再生から反映。")]
        [SerializeField, Min(1)] private int hitsToDefeat = BattleSession.DefaultHitsToDefeat;
        [Tooltip("プレイヤーの HP（敵の攻撃1回で1減る）。初期5。再生中の変更は次の再生から反映。")]
        [SerializeField, Min(1)] private int playerMaxHp = BattleSession.DefaultPlayerMaxHp;

        [Header("敵の行動（仮の値。実機の場所と安全範囲に合わせて調整）")]
        [SerializeField, Min(0f)] private float enemyMoveSpeed = 0.5f;
        [Tooltip("この距離（m）まで近づいたら止まって攻撃の予備動作に入る。")]
        [SerializeField, Min(0.3f)] private float enemyAttackRange = 1.2f;
        [Tooltip("攻撃の瞬間にプレイヤーの頭がこの距離（m）以内なら当たる。攻撃距離以上にする。")]
        [SerializeField, Min(0.3f)] private float enemyStrikeReach = 1.5f;
        [Tooltip("安全範囲：配置した位置からこの半径（m）の外へは出ない。")]
        [SerializeField, Min(0f)] private float enemyLeashRadius = 1.0f;
        [SerializeField, Min(0f)] private float enemyWindUpSeconds = 0.8f;
        [SerializeField, Min(0f)] private float enemyRecoverSeconds = 2.0f;
        [Tooltip("配置・再挑戦から動き出すまでの時間（秒）。")]
        [SerializeField, Min(0f)] private float enemyStartDelaySeconds = 1.5f;
        [Tooltip("攻撃の予備動作で再生する Animator の状態（Fab モデル用。無ければ再生しない）。")]
        [SerializeField] private string attackState = "Base Layer.Attack1";
        [SerializeField] private string idleState = "Base Layer.Idle1";

        [Header("表示")]
        [Tooltip("撃破してから敵を消すまでの時間（秒）。最後の命中の光が見えるようにする。")]
        [SerializeField, Min(0f)] private float defeatHideDelay = 0.4f;
        [Tooltip("画面右上に HP と状態、決着後に2択を出す（Editor 用。Quest のヘッドセット内には出ない）。")]
        [SerializeField] private bool showHud = true;

        private BattleSession _session;
        private EnemyBrain _brain;
        private bool _brainReady;
        private Animator _enemyAnimator;
        private int _attackHash;
        private int _idleHash;
        private float _hideAt = -1f;
        private float _playerHitFlashUntil = -1f;
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
            rig = FindAnyObjectByType<PlatformRig>();
        }

        private void Awake()
        {
            if (placement == null) placement = FindAnyObjectByType<FixedEnemyPlacement>();
            if (detector == null) detector = FindAnyObjectByType<SwordHitDetector>();
            if (view == null) view = FindAnyObjectByType<BattleView>();
            if (rig == null) rig = FindAnyObjectByType<PlatformRig>();
            if (placement == null || detector == null)
                Debug.LogError($"[{nameof(BattleController)}] FixedEnemyPlacement または SwordHitDetector がありません。", this);

            _session = new BattleSession(hitsToDefeat, playerMaxHp);
            _session.Victory += OnVictory;
            _session.Defeat += OnDefeat;
            _session.Retried += OnRetried;
            _session.QuitRequestedEvent += OnQuitRequested;

            var settings = CreateBrainSettings();
            _brain = new EnemyBrain(settings);
            _attackHash = Animator.StringToHash(attackState);
            _idleHash = Animator.StringToHash(idleState);
            Debug.Log($"[OrdinalScale][Q2] 戦闘開始：撃破に必要な有効命中 {_session.HitsToDefeat}・プレイヤーHP {_session.PlayerHealth.Max}（仮の値）", this);
            Debug.Log($"[OrdinalScale][Q3] 敵の行動（仮の値・Quest 実機で未確認）: {settings}", this);
        }

        private EnemyBrainSettings CreateBrainSettings()
        {
            try
            {
                return new EnemyBrainSettings(enemyMoveSpeed, enemyAttackRange, Mathf.Max(enemyStrikeReach, enemyAttackRange),
                    enemyLeashRadius, enemyWindUpSeconds, enemyRecoverSeconds, enemyStartDelaySeconds);
            }
            catch (System.ArgumentOutOfRangeException e)
            {
                Debug.LogError($"[OrdinalScale][Q3] 敵の行動の値が不正なため既定値を使います: {e.Message}", this);
                return EnemyBrainSettings.Default;
            }
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
                var hidden = placement != null ? placement.Enemy : null;
                if (hidden != null) hidden.SetActive(false);
                if (view != null) view.HideHealthBar();
            }

            UpdateEnemy();
        }

        private void UpdateEnemy()
        {
            if (!_brainReady || _session.State != BattleState.Fighting) return;
            var enemy = placement != null ? placement.Enemy : null;
            if (enemy == null || !enemy.activeInHierarchy || rig == null || rig.Spatial == null) return;

            var head = rig.Spatial.HeadPose.position;
            var step = _brain.Step(Time.deltaTime, head.x, head.z);

            // 足元の高さは変えず、水平だけ動かす。常にプレイヤーの方を向く
            var t = enemy.transform;
            var position = new Vector3(step.X, t.position.y, step.Z);
            var yaw = PlacementMath.TryYawDegreesToFace(step.X, step.Z, head.x, head.z, out var facing) ? facing : t.eulerAngles.y;
            t.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));

            if (step.WindUpStarted)
            {
                if (view != null) view.ShowAttackWarning(true);
                PlayAnimation(_attackHash);
                Debug.Log($"[OrdinalScale][Q3] 敵の攻撃の予備動作（{_brain.Settings.WindUpSeconds:0.0}秒後に攻撃。下がれば避けられる）", this);
            }

            if (step.StrikeLanded || step.StrikeMissed)
            {
                if (view != null) view.ShowAttackWarning(false);
                if (step.StrikeLanded) OnEnemyStrikeLanded();
                else Debug.Log($"[OrdinalScale][Q3] 敵の攻撃を回避（プレイヤーHP {_session.PlayerHealth.Current}/{_session.PlayerHealth.Max}）", this);
            }

            if (step.Phase == EnemyPhase.Approaching && _brain.Strikes > 0) PlayAnimation(_idleHash, onlyIfDifferent: true);
        }

        private void OnEnemyStrikeLanded()
        {
            var result = _session.RegisterEnemyAttackHit();
            var hp = _session.PlayerHealth;
            if (result == PlayerHitResult.IgnoredNotFighting) return;

            _playerHitFlashUntil = Time.time + 0.4f;
            if (view != null) view.SetPlayerHealth(hp.Current, hp.Max);
            Debug.Log($"[OrdinalScale][Q3] 敵の攻撃が命中：プレイヤーHP {hp.Current}/{hp.Max}（被弾 {_session.EnemyAttackHits}）", this);
        }

        private void PlayAnimation(int stateHash, bool onlyIfDifferent = false)
        {
            if (_enemyAnimator == null || !_enemyAnimator.isActiveAndEnabled || !_enemyAnimator.HasState(0, stateHash)) return;
            if (onlyIfDifferent && _enemyAnimator.GetCurrentAnimatorStateInfo(0).fullPathHash == stateHash) return;
            _enemyAnimator.applyRootMotion = false;
            _enemyAnimator.Play(stateHash, 0, 0f);
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
                    Debug.Log($"[OrdinalScale][Q2] 決着後の命中を無視（攻撃は無効） HP {hp.Current}/{hp.Max}", this);
                    break;
            }
        }

        private void OnEnemyPlaced(Vector3 position)
        {
            _enemyPosition = position;
            // 配置した位置を安全範囲の中心にする（再挑戦では置き直した位置）
            _brain.Reset(position.x, position.z);
            _brainReady = true;

            var enemy = placement.Enemy;
            _enemyAnimator = enemy != null ? enemy.GetComponentInChildren<Animator>(true) : null;
            if (view == null || enemy == null) return;

            var volume = enemy.GetComponent<EnemyHitVolume>();
            view.Bind(enemy.transform, volume != null ? volume.Height : EnemyPlaceholder.BodyHeight);
            view.SetHealth(_session.EnemyHealth.Current, _session.EnemyHealth.Max);
            view.SetPlayerHealth(_session.PlayerHealth.Current, _session.PlayerHealth.Max);
            view.ShowAttackWarning(false);
        }

        private void OnVictory()
        {
            StopCombat();
            var enemy = placement != null ? placement.Enemy : null;
            if (enemy != null) _enemyPosition = enemy.transform.position;
            _hideAt = Time.time + defeatHideDelay;
            if (view != null) view.ShowResult(true, true, _enemyPosition);
            Debug.Log($"[OrdinalScale][Q2] 撃破：有効命中 {_session.ValidHits} 回（ラウンド {_session.Round}）。「終了」「再挑戦」を選べます", this);
        }

        private void OnDefeat()
        {
            StopCombat();
            var enemy = placement != null ? placement.Enemy : null;
            if (enemy != null) _enemyPosition = enemy.transform.position;
            PlayAnimation(_idleHash);
            if (view != null) view.ShowResult(true, false, _enemyPosition);
            Debug.Log($"[OrdinalScale][Q3] 敗北：被弾 {_session.EnemyAttackHits} 回・敵HP {_session.EnemyHealth.Current}/{_session.EnemyHealth.Max}（ラウンド {_session.Round}）。「終了」「再挑戦」を選べます", this);
        }

        /// <summary>決着したら双方の攻撃を止める（剣の判定を止め、敵の行動も止める。BattleSession も決着後の攻撃を無視する）。</summary>
        private void StopCombat()
        {
            if (detector != null) detector.enabled = false;
            _brainReady = false;
            if (view != null) view.ShowAttackWarning(false);
        }

        private void OnRetried(int round)
        {
            _hideAt = -1f;
            _brainReady = false; // 置き直した位置で OnEnemyPlaced が初期化する
            if (view != null)
            {
                view.ShowResult(false, true, _enemyPosition);
                view.ShowAttackWarning(false);
            }

            if (detector != null)
            {
                detector.ResetCounters();
                detector.enabled = true;
            }

            // 次の安定したフレームで、その時の正面に敵を置き直す（FixedEnemyPlacement が SetActive(true) にする）
            if (placement != null) placement.Reposition();
            Debug.Log($"[OrdinalScale][Q3] 再挑戦：ラウンド {round}。敵HP {_session.EnemyHealth.Current}/{_session.EnemyHealth.Max}・" +
                      $"プレイヤーHP {_session.PlayerHealth.Current}/{_session.PlayerHealth.Max}・剣の判定・敵の行動を初期化", this);
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
            var php = _session.PlayerHealth;
            GUILayout.BeginArea(new Rect(Screen.width - 330f, 10f, 320f, 150f), GUI.skin.box);
            GUILayout.Label($"Q3 battle   round {_session.Round}", _hudStyle);
            GUILayout.Label($"enemy HP {hp.Current}/{hp.Max}   valid hits {_session.ValidHits}", _hudStyle);
            GUILayout.Label($"player HP {php.Current}/{php.Max}   enemy hits {_session.EnemyAttackHits}" +
                            (Time.time < _playerHitFlashUntil ? "   << HIT!" : ""), _hudStyle);
            GUILayout.Label($"enemy {(_brainReady ? _brain.Phase.ToString().ToUpperInvariant() : "-")}", _hudStyle);
            GUILayout.Label($"state {_session.State.ToString().ToUpperInvariant()}{(_session.QuitRequested ? "   QUIT REQUESTED" : "")}", _hudStyle);
            GUILayout.EndArea();

            if (_session.State == BattleState.Fighting) return;

            // 中央：決着と2択（E4・Q-5）。日本語が出ない環境でも読めるよう英字を併記
            var w = 420f;
            var h = 190f;
            // Q4 のヘッドセット内パネル（画面の中央より下に見える）と重ならないよう、画面の上寄りに出す
            GUILayout.BeginArea(new Rect((Screen.width - w) * 0.5f, Screen.height * 0.05f, w, h), GUI.skin.box);
            GUILayout.Label(_session.State == BattleState.Victory ? "VICTORY" : "DEFEAT", _titleStyle);
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
