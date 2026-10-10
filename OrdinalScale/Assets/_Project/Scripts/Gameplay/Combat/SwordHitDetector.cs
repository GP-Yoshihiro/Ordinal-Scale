using System;
using System.Collections.Generic;
using OrdinalScale.Core.Combat;
using OrdinalScale.Gameplay.Placement;
using OrdinalScale.Platform;
using UnityEngine;

namespace OrdinalScale.Gameplay.Combat
{
    /// <summary>
    /// Q1：剣の姿勢（ISwordPoseSource）を Core の命中判定（SwordStrikeTracker）へ渡し、敵の体への有効な命中・不命中を
    /// ログと最小限の見た目の反応で示す。判定の規則は Core にあり、ここは毎フレームの受け渡しと表示だけを行う。
    ///
    /// - 有効な命中：敵が白く光り、刃が赤くなる。ログ「命中」。
    /// - ゆっくり接触：刃が青くなる。ログ「不命中：ゆっくり接触」。
    /// - 同じ振りでの再接触：刃が紫になる。ログ「不命中：同じ振りで再接触」。
    /// - 触れ続け：接触が終わったときにログ「接触終了：Nフレーム触れ続け」。
    /// - 空振り：振りが終わったときにログ「結果=空振り」。
    /// - 振り中は刃が黄色。
    ///
    /// 体力の減少・撃破・勝敗は Q2 の範囲（ここでは命中を数えるだけ）。
    /// 調整値は SwordTuningAsset（仮の値）。再生中に変えると判定の途中状態と回数を初期化して新しい値で続ける。
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class SwordHitDetector : MonoBehaviour
    {
        private const int RecentCapacity = 6;

        [SerializeField] private PlatformRig rig;
        [SerializeField] private FixedEnemyPlacement placement;
        [Tooltip("判定の調整値（仮の値）。未設定なら Core の既定値（同じく仮）を使う。")]
        [SerializeField] private SwordTuningAsset tuning;
        [Tooltip("刃の表示。色で振り中・命中などを示す。")]
        [SerializeField] private SwordPoseDebugView bladeView;

        [Header("見た目の反応（表示のみ。判定には影響しない）")]
        [SerializeField] private Color swingingColor = new Color(1f, 0.9f, 0.2f);
        [SerializeField] private Color hitColor = new Color(1f, 0.2f, 0.2f);
        [SerializeField] private Color slowContactColor = new Color(0.3f, 0.45f, 1f);
        [SerializeField] private Color repeatContactColor = new Color(0.75f, 0.35f, 1f);
        [SerializeField] private Color enemyFlashColor = Color.white;
        [Tooltip("命中・不命中の色を保つ時間（秒）。")]
        [SerializeField, Min(0.02f)] private float flashSeconds = 0.2f;

        [Header("記録")]
        [Tooltip("振りの開始もログに出す（既定は終了だけ）。")]
        [SerializeField] private bool logSwingStart = false;
        [Tooltip("画面左上に状態と回数を出す（Editor 用。Quest のヘッドセット内には出ない）。")]
        [SerializeField] private bool showHud = true;

        private readonly StrikeEventRecorder _recorder = new StrikeEventRecorder();
        private readonly List<StrikeEvent> _events = new List<StrikeEvent>(8);
        private readonly string[] _recent = new string[RecentCapacity];
        private int _recentCount;
        private int _recentNext;

        private SwordStrikeTracker _tracker;
        private SwordTuning _activeTuning;
        private int _appliedRevision = int.MinValue;
        private bool _hasAppliedAsset;

        private GameObject _target;
        private int _targetId;
        private int _nextTargetId = 1;
        private EnemyHitVolume _volume;
        private Renderer[] _targetRenderers;
        private MaterialPropertyBlock _flashBlock;
        private bool _enemyFlashing;
        private float _enemyFlashUntil;
        private Color _bladeFlashColor;
        private float _bladeFlashUntil;

        private SwingState _lastSwing;
        private GUIStyle _hudStyle;

        /// <summary>有効な命中の回数（Q2 で体力に接続する）。</summary>
        public int Hits => _recorder.Hits;

        public StrikeEventRecorder Recorder => _recorder;

        /// <summary>
        /// 剣の出来事（命中・ゆっくり接触・空振りの振り終了など）が起きたとき。Q2 の BattleController は Kind == Hit だけを HP に数える。
        /// 判定は変えず、記録した出来事をそのまま渡す。
        /// </summary>
        public event Action<StrikeEvent> Struck;

        private void Reset()
        {
            rig = FindAnyObjectByType<PlatformRig>();
            placement = FindAnyObjectByType<FixedEnemyPlacement>();
            bladeView = FindAnyObjectByType<SwordPoseDebugView>();
        }

        private void Awake()
        {
            if (rig == null) rig = FindAnyObjectByType<PlatformRig>();
            if (placement == null) placement = FindAnyObjectByType<FixedEnemyPlacement>();
            if (bladeView == null) bladeView = FindAnyObjectByType<SwordPoseDebugView>();
            if (rig == null) Debug.LogError($"[{nameof(SwordHitDetector)}] シーンに PlatformRig がありません。", this);
            _flashBlock = new MaterialPropertyBlock();
            ApplyTuning(force: true);
        }

        private void OnEnable()
        {
            if (placement != null) placement.Placed += OnEnemyPlaced;
        }

        private void OnDisable()
        {
            if (placement != null) placement.Placed -= OnEnemyPlaced;
            EndEnemyFlash();
            // 判定を止めた（撃破後など）ときに、命中の色が残らないようにする
            if (bladeView != null) bladeView.SetTint(bladeView.BaseColor);
        }

        private void Update()
        {
            ApplyTuning(force: false);

            var source = rig != null ? rig.Sword : null;
            if (source == null || _tracker == null) return;

            source.TryGetBladePose(out var pose);
            var swing = _tracker.Step(pose.ToSample());
            _lastSwing = swing;

            var verdict = HitVerdict.NoContact;
            AcquireTarget();
            if (_volume != null && _target.activeInHierarchy) verdict = _tracker.Resolve(_targetId, _volume.ToCapsule());

            _events.Clear();
            _recorder.Process(swing, verdict, _events);
            for (var i = 0; i < _events.Count; i++) Handle(_events[i]);

            UpdateVisuals(swing);
        }

        /// <summary>回数と判定の途中状態を初期化する（検証のやり直し）。</summary>
        public void ResetCounters()
        {
            _tracker?.Reset();
            _recorder.Reset();
            _recentCount = 0;
            _recentNext = 0;
            Debug.Log("[OrdinalScale][Q1] 剣の判定と回数を初期化しました。", this);
        }

        private void ApplyTuning(bool force)
        {
            if (!force)
            {
                if (tuning == null || (_hasAppliedAsset && tuning.Revision == _appliedRevision)) return;
            }

            SwordTuning next;
            string source;
            if (tuning == null)
            {
                next = SwordTuning.Default;
                source = "Core の既定値";
                _hasAppliedAsset = false;
            }
            else
            {
                _appliedRevision = tuning.Revision;
                _hasAppliedAsset = true;
                if (!tuning.TryCreate(out next, out var error))
                {
                    Debug.LogError($"[OrdinalScale][Q1] 剣の調整値が不正なため、前の値のまま判定します: {error}", tuning);
                    if (_tracker != null) return;
                    next = SwordTuning.Default;
                }

                source = tuning.name;
            }

            var changed = _tracker != null;
            _activeTuning = next;
            _tracker = new SwordStrikeTracker(next);
            _recorder.Reset();
            if (_target != null) _target = null; // 次のフレームで取り直す（接触の記録は新しい判定器に無い）
            Debug.Log($"[OrdinalScale][Q1] 剣の調整値（仮の値・Quest 実機で未確認）{(changed ? "を変更" : "")}: {next}（{source}）", this);
        }

        private void OnEnemyPlaced(Vector3 position)
        {
            // 置き直した敵に、前の位置での「触れ続け」を引き継がない
            if (_target != null) _tracker?.Forget(_targetId);
            _target = null;
        }

        private void AcquireTarget()
        {
            var enemy = placement != null ? placement.Enemy : null;
            if (enemy == _target) return;

            if (_target != null) _tracker.Forget(_targetId);
            EndEnemyFlash();
            _target = enemy;
            _volume = null;
            _targetRenderers = null;
            if (enemy == null) return;

            _targetId = _nextTargetId++;
            _volume = enemy.GetComponent<EnemyHitVolume>();
            if (_volume == null)
            {
                // FixedEnemyPlacement 以外で置かれた敵の場合は仮モデルの寸法を使う
                _volume = enemy.AddComponent<EnemyHitVolume>();
                _volume.Configure(EnemyPlaceholder.BodyHeight, EnemyPlaceholder.BodyDiameter * 0.5f);
            }

            _targetRenderers = enemy.GetComponentsInChildren<Renderer>(true);
        }

        private void Handle(in StrikeEvent e)
        {
            PushRecent(StrikeMessages.ShortCode(e));
            Struck?.Invoke(e);

            if (e.Kind != StrikeEventKind.SwingStarted || logSwingStart)
            {
                Debug.Log($"[OrdinalScale][Q1] {StrikeMessages.Describe(e)} | 命中{_recorder.Hits} 振り{_recorder.Swings} " +
                          $"空振り{_recorder.Whiffs} ゆっくり接触{_recorder.SlowContacts} 同じ振りで再接触{_recorder.RepeatContacts} 触れ続け{_recorder.HeldContacts}", this);
            }

            switch (e.Kind)
            {
                case StrikeEventKind.Hit:
                    FlashBlade(hitColor);
                    _enemyFlashUntil = Time.time + flashSeconds;
                    break;
                case StrikeEventKind.SlowContact:
                    FlashBlade(slowContactColor);
                    break;
                case StrikeEventKind.RepeatContact:
                    FlashBlade(repeatContactColor);
                    break;
            }
        }

        private void FlashBlade(Color color)
        {
            _bladeFlashColor = color;
            _bladeFlashUntil = Time.time + flashSeconds;
        }

        private void UpdateVisuals(in SwingState swing)
        {
            if (bladeView != null)
            {
                var color = Time.time < _bladeFlashUntil ? _bladeFlashColor
                    : swing.IsSwinging ? swingingColor
                    : bladeView.BaseColor;
                bladeView.SetTint(color);
            }

            var shouldFlash = _targetRenderers != null && Time.time < _enemyFlashUntil;
            if (shouldFlash && !_enemyFlashing)
            {
                // URP Lit は _BaseColor、旧シェーダーは _Color。マテリアル自体は変えない（Fab の原本マテリアルを汚さない）
                _flashBlock.Clear();
                _flashBlock.SetColor("_BaseColor", enemyFlashColor);
                _flashBlock.SetColor("_Color", enemyFlashColor);
                for (var i = 0; i < _targetRenderers.Length; i++)
                    if (_targetRenderers[i] != null) _targetRenderers[i].SetPropertyBlock(_flashBlock);
                _enemyFlashing = true;
            }
            else if (!shouldFlash && _enemyFlashing)
            {
                EndEnemyFlash();
            }
        }

        private void EndEnemyFlash()
        {
            if (!_enemyFlashing) return;
            if (_targetRenderers != null)
            {
                for (var i = 0; i < _targetRenderers.Length; i++)
                    if (_targetRenderers[i] != null) _targetRenderers[i].SetPropertyBlock(null);
            }

            _enemyFlashing = false;
        }

        private void PushRecent(string line)
        {
            _recent[_recentNext] = line;
            _recentNext = (_recentNext + 1) % RecentCapacity;
            if (_recentCount < RecentCapacity) _recentCount++;
        }

        private void OnGUI()
        {
            if (!showHud || _activeTuning == null) return;
            if (_hudStyle == null) _hudStyle = new GUIStyle(GUI.skin.label) { fontSize = 15 };

            // 既定フォントに日本語が無い環境でも読めるよう英字で出す（ログは日本語）
            GUILayout.BeginArea(new Rect(10f, 10f, 520f, 330f), GUI.skin.box);
            GUILayout.Label("Q1 sword  (thresholds are PROVISIONAL, not tuned on Quest)", _hudStyle);
            GUILayout.Label($"tip {_lastSwing.TipSpeed:0.00} m/s   start >= {_activeTuning.SwingStartSpeed:0.00}   end < {_activeTuning.SwingEndSpeed:0.00}", _hudStyle);
            GUILayout.Label($"state {(_lastSwing.IsSwinging ? "SWINGING" : "idle")}   swing #{_lastSwing.SwingId}   target {(_volume != null ? "yes" : "none")}", _hudStyle);
            GUILayout.Label($"hits {_recorder.Hits}   swings {_recorder.Swings}   whiffs {_recorder.Whiffs}", _hudStyle);
            GUILayout.Label($"slow contact {_recorder.SlowContacts}   same-swing contact {_recorder.RepeatContacts}   held {_recorder.HeldContacts}", _hudStyle);
            for (var i = 0; i < _recentCount; i++)
            {
                var index = (_recentNext - 1 - i + RecentCapacity) % RecentCapacity;
                GUILayout.Label($"  {_recent[index]}", _hudStyle);
            }

            if (GUILayout.Button("Reset counters", GUILayout.Width(160f))) ResetCounters();
            GUILayout.EndArea();
        }
    }
}
