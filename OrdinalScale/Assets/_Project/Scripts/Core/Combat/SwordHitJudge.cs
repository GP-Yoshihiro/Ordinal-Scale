using System.Collections.Generic;

namespace OrdinalScale.Core.Combat
{
    /// <summary>1フレーム・1体分の命中判定の結果。命中でない場合は理由を返す（ログ・テスト用）。</summary>
    public enum HitVerdict
    {
        /// <summary>命中（この振りでこの敵に1回だけ）。</summary>
        Hit,
        /// <summary>刃が体に触れていない（空振りを含む。C4）。</summary>
        NoContact,
        /// <summary>前のフレームから触れ続けている。接触の「開始」ではない（C3）。</summary>
        ContactContinued,
        /// <summary>接触は始まったが、振り中ではない（ゆっくり押し当てた。C2）。</summary>
        NotSwinging,
        /// <summary>この振りで既にこの敵に命中している（C5）。</summary>
        AlreadyHitThisSwing,
    }

    /// <summary>
    /// 命中の規則（Claude/docs/sword-input-design.md 3.2）。次の3つが同じフレームでそろったときだけ命中：
    /// 1. その敵との接触が「前フレームなし → 今フレームあり」に変わった（C3）
    /// 2. このフレームが振り中（C1・C2）
    /// 3. この振りでまだその敵に当てていない（C5）
    /// 敵は整数の ID で区別する。1フレームに1回 BeginFrame を呼び、続けて各敵について Evaluate を呼ぶ。
    /// </summary>
    public sealed class SwordHitJudge
    {
        private readonly Dictionary<int, bool> _wasTouching = new Dictionary<int, bool>();
        private readonly HashSet<int> _hitThisSwing = new HashSet<int>();
        private SwingState _swing;
        private int _hitSetSwingId;

        /// <summary>これまでの命中の合計（全ての敵）。</summary>
        public int TotalHits { get; private set; }

        /// <summary>このフレームの振りの状態を受け取る。振りが変わったら「この振りで当てた敵」を空にする。</summary>
        public void BeginFrame(in SwingState swing)
        {
            _swing = swing;
            if (swing.SwingId != _hitSetSwingId)
            {
                _hitThisSwing.Clear();
                _hitSetSwingId = swing.SwingId;
            }
        }

        /// <summary>敵 targetId に、このフレーム刃が触れているか（touching）を渡して命中を判定する。</summary>
        public HitVerdict Evaluate(int targetId, bool touching)
        {
            _wasTouching.TryGetValue(targetId, out var wasTouching);
            _wasTouching[targetId] = touching;

            if (!touching) return HitVerdict.NoContact;
            if (wasTouching) return HitVerdict.ContactContinued;
            if (!_swing.IsSwinging) return HitVerdict.NotSwinging;
            if (_hitThisSwing.Contains(targetId)) return HitVerdict.AlreadyHitThisSwing;

            _hitThisSwing.Add(targetId);
            TotalHits++;
            return HitVerdict.Hit;
        }

        /// <summary>敵が消えた・置き直されたときに、その敵の接触の記録を消す。</summary>
        public void Forget(int targetId)
        {
            _wasTouching.Remove(targetId);
            _hitThisSwing.Remove(targetId);
        }

        /// <summary>すべての記録を初期化する（再挑戦など）。</summary>
        public void Reset()
        {
            _wasTouching.Clear();
            _hitThisSwing.Clear();
            _swing = default;
            _hitSetSwingId = 0;
            TotalHits = 0;
        }
    }
}
