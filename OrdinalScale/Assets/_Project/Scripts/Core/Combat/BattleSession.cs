using System;
using System.Collections.Generic;

namespace OrdinalScale.Core.Combat
{
    /// <summary>戦闘の状態。</summary>
    public enum BattleState
    {
        /// <summary>戦闘中。剣の有効な命中で敵の HP が、敵の攻撃の命中でプレイヤーの HP が減る。</summary>
        Fighting,
        /// <summary>敵を撃破した。双方の攻撃は無効で、「終了」「再挑戦」を選べる。</summary>
        Victory,
        /// <summary>プレイヤーの HP が0になった（Q3）。双方の攻撃は無効で、「終了」「再挑戦」を選べる。</summary>
        Defeat,
    }

    /// <summary>決着後に選べる操作（E4・Q-5）。</summary>
    public enum BattleChoice
    {
        /// <summary>アプリを閉じる（Editor では終了要求をログで確認する）。</summary>
        Quit,
        /// <summary>敵・HP・剣の判定を初期化してもう一度戦う。</summary>
        Retry,
    }

    /// <summary>RegisterValidHit の結果。</summary>
    public enum BattleHitResult
    {
        /// <summary>戦闘中でないため無視した（撃破後の攻撃は無効）。</summary>
        IgnoredNotFighting,
        /// <summary>HP が1減った（まだ生存）。</summary>
        Damaged,
        /// <summary>この命中で撃破した。</summary>
        Defeated,
    }

    /// <summary>RegisterEnemyAttackHit の結果（Q3）。</summary>
    public enum PlayerHitResult
    {
        /// <summary>戦闘中でないため無視した（決着後の敵の攻撃は無効）。</summary>
        IgnoredNotFighting,
        /// <summary>プレイヤーの HP が1減った（まだ戦える）。</summary>
        Damaged,
        /// <summary>この攻撃でプレイヤーが敗北した。</summary>
        PlayerDefeated,
    }

    /// <summary>
    /// 敵1体との戦闘の進行（Q2：E3・E4、Q-4・Q-5。Q3：Q-3 の敗北と、敗北後の2択）。UnityEngine に依存しない。
    /// - 剣の**有効な命中だけ**を RegisterValidHit で受け取り、敵の Health を1ずつ減らす（空振り・押し当ては呼ばれないので減らない）。
    /// - HP が0になったら Victory。以後の命中は無視し、「終了」「再挑戦」を選べる。
    /// - 再挑戦で敵の HP を全快にして Fighting に戻す（剣の判定と敵の配置の初期化は Gameplay 側が Retried を受けて行う）。
    /// - 終了は要求を記録して QuitRequested を発火するだけ（アプリを閉じるのは Gameplay 側）。
    /// - 敵の攻撃が当たったら RegisterEnemyAttackHit でプレイヤーの Health を1ずつ減らし、0で Defeat（Q3）。
    /// - 決着（Victory／Defeat）の後は双方の攻撃を無視し、どちらの結末からも「終了」「再挑戦」を選べる。
    ///   再挑戦では敵とプレイヤーの両方の HP を全快にする。
    /// 必要な有効命中数（既定10）とプレイヤーの HP（既定5）は試遊で調整できる値（仮）で、受入時に固定して記録する。
    /// </summary>
    public sealed class BattleSession
    {
        public const int DefaultHitsToDefeat = 10;

        /// <summary>プレイヤーの HP の既定値（敵の攻撃5回で敗北。仮の値・試遊で調整）。</summary>
        public const int DefaultPlayerMaxHp = 5;

        private static readonly BattleChoice[] NoChoices = new BattleChoice[0];
        private static readonly BattleChoice[] EndChoices = { BattleChoice.Quit, BattleChoice.Retry };

        public BattleSession(int hitsToDefeat = DefaultHitsToDefeat, int playerMaxHp = DefaultPlayerMaxHp)
        {
            if (hitsToDefeat < 1) throw new ArgumentOutOfRangeException(nameof(hitsToDefeat), hitsToDefeat, "Must be at least 1.");
            if (playerMaxHp < 1) throw new ArgumentOutOfRangeException(nameof(playerMaxHp), playerMaxHp, "Must be at least 1.");
            HitsToDefeat = hitsToDefeat;
            // 1命中1ダメージなので、HP の最大値＝撃破に必要な有効命中数
            EnemyHealth = new Health(hitsToDefeat);
            PlayerHealth = new Health(playerMaxHp);
            Round = 1;
        }

        /// <summary>撃破に必要な有効命中数（＝敵の最大 HP）。</summary>
        public int HitsToDefeat { get; }

        /// <summary>敵の HP（既存の Core.Combat.Health）。</summary>
        public Health EnemyHealth { get; }

        /// <summary>プレイヤーの HP（Q3）。敵の攻撃1回で1減る。</summary>
        public Health PlayerHealth { get; }

        public BattleState State { get; private set; } = BattleState.Fighting;

        /// <summary>何回目の戦闘か（再挑戦で増える）。</summary>
        public int Round { get; private set; }

        /// <summary>このラウンドで敵の HP を減らした有効命中の数。</summary>
        public int ValidHits { get; private set; }

        /// <summary>撃破後に無視した命中の数（攻撃が無効になっていることの確認用）。</summary>
        public int IgnoredHits { get; private set; }

        /// <summary>このラウンドでプレイヤーに当たった敵の攻撃の数。</summary>
        public int EnemyAttackHits { get; private set; }

        /// <summary>決着後に無視した敵の攻撃の数。</summary>
        public int IgnoredEnemyAttacks { get; private set; }

        /// <summary>終了が要求されたか。</summary>
        public bool QuitRequested { get; private set; }

        /// <summary>剣の命中を受け付けるか。</summary>
        public bool AcceptsAttacks => State == BattleState.Fighting;

        /// <summary>今選べる操作。戦闘中は空、決着後は「終了」「再挑戦」。</summary>
        public IReadOnlyList<BattleChoice> AvailableChoices => State == BattleState.Fighting ? NoChoices : EndChoices;

        /// <summary>敵の HP が減ったとき。引数は (残り HP, 最大 HP)。</summary>
        public event Action<int, int> EnemyDamaged;

        /// <summary>撃破したとき（1ラウンドに1度）。</summary>
        public event Action Victory;

        /// <summary>プレイヤーの HP が減ったとき。引数は (残り HP, 最大 HP)。</summary>
        public event Action<int, int> PlayerDamaged;

        /// <summary>プレイヤーが敗北したとき（1ラウンドに1度）。</summary>
        public event Action Defeat;

        /// <summary>再挑戦で初期化したとき。引数は新しいラウンド番号。</summary>
        public event Action<int> Retried;

        /// <summary>終了が要求されたとき（1度だけ）。</summary>
        public event Action QuitRequestedEvent;

        /// <summary>剣の有効な命中を1回受け取る。</summary>
        public BattleHitResult RegisterValidHit()
        {
            if (State != BattleState.Fighting)
            {
                IgnoredHits++;
                return BattleHitResult.IgnoredNotFighting;
            }

            var result = EnemyHealth.ApplyDamage(1);
            if (result == DamageResult.Ignored)
            {
                // 戦闘中に HP が0のことは無いが、念のため撃破扱いにはしない
                IgnoredHits++;
                return BattleHitResult.IgnoredNotFighting;
            }

            ValidHits++;
            EnemyDamaged?.Invoke(EnemyHealth.Current, EnemyHealth.Max);

            if (result != DamageResult.Killed) return BattleHitResult.Damaged;

            State = BattleState.Victory;
            Victory?.Invoke();
            return BattleHitResult.Defeated;
        }

        /// <summary>敵の攻撃がプレイヤーに当たったことを1回受け取る（Q3）。</summary>
        public PlayerHitResult RegisterEnemyAttackHit()
        {
            if (State != BattleState.Fighting)
            {
                IgnoredEnemyAttacks++;
                return PlayerHitResult.IgnoredNotFighting;
            }

            var result = PlayerHealth.ApplyDamage(1);
            if (result == DamageResult.Ignored)
            {
                IgnoredEnemyAttacks++;
                return PlayerHitResult.IgnoredNotFighting;
            }

            EnemyAttackHits++;
            PlayerDamaged?.Invoke(PlayerHealth.Current, PlayerHealth.Max);

            if (result != DamageResult.Killed) return PlayerHitResult.Damaged;

            State = BattleState.Defeat;
            Defeat?.Invoke();
            return PlayerHitResult.PlayerDefeated;
        }

        /// <summary>決着後の選択を行う。選べない状態（戦闘中など）なら false。</summary>
        public bool Choose(BattleChoice choice)
        {
            if (State == BattleState.Fighting) return false;

            switch (choice)
            {
                case BattleChoice.Retry:
                    // 双方の戦闘状態を初期化（Q-5）
                    EnemyHealth.Reset();
                    PlayerHealth.Reset();
                    State = BattleState.Fighting;
                    ValidHits = 0;
                    IgnoredHits = 0;
                    EnemyAttackHits = 0;
                    IgnoredEnemyAttacks = 0;
                    Round++;
                    Retried?.Invoke(Round);
                    return true;
                case BattleChoice.Quit:
                    if (QuitRequested) return true;
                    QuitRequested = true;
                    QuitRequestedEvent?.Invoke();
                    return true;
                default:
                    return false;
            }
        }
    }
}
