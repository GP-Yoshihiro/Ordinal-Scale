using System;

namespace OrdinalScale.Core.Combat
{
    /// <summary>ダメージ適用の結果。</summary>
    public enum DamageResult
    {
        /// <summary>既に撃破済み、またはダメージ量が0以下のため何も起きなかった。</summary>
        Ignored,
        /// <summary>HPが減ったが、まだ生存している。</summary>
        Damaged,
        /// <summary>このダメージでHPが0になった。</summary>
        Killed,
    }

    /// <summary>
    /// HP管理。UnityEngineに依存しない純C#クラスで、Quest / ARグラス / Editor の全プラットフォームで共通に使う。
    /// 表示や演出は Gameplay 層がイベントを購読して行う。
    /// </summary>
    public sealed class Health
    {
        public int Max { get; }
        public int Current { get; private set; }
        public bool IsDead => Current <= 0;

        /// <summary>ダメージを受けたとき（撃破時も含む）。引数は (実際に減った量, 残りHP)。</summary>
        public event Action<int, int> Damaged;

        /// <summary>HPが0になったとき。1回の撃破につき1度だけ発火する。</summary>
        public event Action Died;

        /// <summary>Reset() で全快したとき。</summary>
        public event Action Revived;

        public Health(int max)
        {
            if (max <= 0) throw new ArgumentOutOfRangeException(nameof(max), max, "Max HP must be positive.");
            Max = max;
            Current = max;
        }

        public DamageResult ApplyDamage(int amount)
        {
            if (IsDead || amount <= 0) return DamageResult.Ignored;

            var dealt = Math.Min(amount, Current);
            Current -= dealt;
            Damaged?.Invoke(dealt, Current);

            if (!IsDead) return DamageResult.Damaged;

            Died?.Invoke();
            return DamageResult.Killed;
        }

        /// <summary>全快させる（撃破後の再開始用）。</summary>
        public void Reset()
        {
            Current = Max;
            Revived?.Invoke();
        }
    }
}
