using System.Collections.Generic;

namespace OrdinalScale.Core.Combat
{
    /// <summary>剣の操作で起きた出来事の種類。Editor・Quest で「なぜ当たった／当たらなかったか」を区別して記録するため。</summary>
    public enum StrikeEventKind
    {
        /// <summary>振りが始まった。</summary>
        SwingStarted,
        /// <summary>振りが終わった（結果は StrikeEvent.Outcome）。</summary>
        SwingEnded,
        /// <summary>有効な命中（振り中に接触が始まり、この振りで初めて）。</summary>
        Hit,
        /// <summary>接触が始まったが振り中ではない（ゆっくり接触・押し当て。C2）。</summary>
        SlowContact,
        /// <summary>同じ振りの中で、離れてから再び接触した（追加の命中なし。C5）。</summary>
        RepeatContact,
        /// <summary>接触が終わった（触れていたフレーム数を記録。2フレーム以上は「触れ続け」。C3）。</summary>
        ContactEnded,
    }

    /// <summary>振りの結果。</summary>
    public enum SwingOutcome
    {
        None,
        /// <summary>この振りで命中した。</summary>
        Hit,
        /// <summary>振りの間、一度も体に触れなかった（空振り。C4）。</summary>
        Whiff,
        /// <summary>振りの間に触れていたが命中しなかった（押し当てたまま振った＝触れ続け。C2・C3）。</summary>
        ContactWithoutHit,
    }

    /// <summary>1件の出来事。</summary>
    public readonly struct StrikeEvent
    {
        public StrikeEventKind Kind { get; }

        /// <summary>関係する振りの通し番号（振りの外なら直近の番号）。</summary>
        public int SwingId { get; }

        /// <summary>そのフレームの刃先の速さ（m/s）。SwingEnded では振りの間の最大の速さ。</summary>
        public float Speed { get; }

        /// <summary>SwingEnded のときの結果。それ以外は None。</summary>
        public SwingOutcome Outcome { get; }

        /// <summary>SwingEnded のときの終わり方（減速・追跡の途切れなど）。</summary>
        public SwingBreak Break { get; }

        /// <summary>ContactEnded のとき、触れていたフレーム数。</summary>
        public int ContactFrames { get; }

        /// <summary>ContactEnded のとき、その接触で命中していたか。</summary>
        public bool ContactHadHit { get; }

        public StrikeEvent(StrikeEventKind kind, int swingId, float speed,
            SwingOutcome outcome = SwingOutcome.None, SwingBreak swingBreak = SwingBreak.None,
            int contactFrames = 0, bool contactHadHit = false)
        {
            Kind = kind;
            SwingId = swingId;
            Speed = speed;
            Outcome = outcome;
            Break = swingBreak;
            ContactFrames = contactFrames;
            ContactHadHit = contactHadHit;
        }
    }

    /// <summary>
    /// SwordStrikeTracker の毎フレームの結果（振りの状態と、1体の敵への判定）から出来事を取り出し、回数を数える。
    /// 判定そのものは変えない（命中かどうかは SwordHitJudge が決める）。ログ・画面表示・Editor での検証用。
    /// 1フレームに1回 Process を呼ぶ。出来事は渡したリストに追加する（毎フレームの割り当てを避けるため）。
    /// </summary>
    public sealed class StrikeEventRecorder
    {
        private bool _touching;
        private int _contactFrames;
        private bool _contactHadHit;

        private bool _swinging;
        private float _swingPeak;
        private bool _swingTouched;
        private bool _swingHit;

        public int Swings { get; private set; }
        public int Hits { get; private set; }
        public int Whiffs { get; private set; }
        public int SlowContacts { get; private set; }
        public int RepeatContacts { get; private set; }

        /// <summary>2フレーム以上続いた接触の数（触れ続け）。</summary>
        public int HeldContacts { get; private set; }

        /// <summary>このフレームの出来事を output に追加し、追加した件数を返す。</summary>
        public int Process(in SwingState swing, HitVerdict verdict, List<StrikeEvent> output)
        {
            var before = output.Count;

            if (swing.Phase == SwingPhase.Started)
            {
                _swinging = true;
                _swingPeak = swing.TipSpeed;
                _swingTouched = false;
                _swingHit = false;
                Swings++;
                output.Add(new StrikeEvent(StrikeEventKind.SwingStarted, swing.SwingId, swing.TipSpeed));
            }
            else if (swing.IsSwinging && swing.TipSpeed > _swingPeak)
            {
                _swingPeak = swing.TipSpeed;
            }

            var touching = verdict != HitVerdict.NoContact;
            if (touching && !_touching)
            {
                _touching = true;
                _contactFrames = 1;
                _contactHadHit = verdict == HitVerdict.Hit;

                switch (verdict)
                {
                    case HitVerdict.Hit:
                        Hits++;
                        output.Add(new StrikeEvent(StrikeEventKind.Hit, swing.SwingId, swing.TipSpeed));
                        break;
                    case HitVerdict.NotSwinging:
                        SlowContacts++;
                        output.Add(new StrikeEvent(StrikeEventKind.SlowContact, swing.SwingId, swing.TipSpeed));
                        break;
                    case HitVerdict.AlreadyHitThisSwing:
                        RepeatContacts++;
                        output.Add(new StrikeEvent(StrikeEventKind.RepeatContact, swing.SwingId, swing.TipSpeed));
                        break;
                }
            }
            else if (touching)
            {
                _contactFrames++;
            }
            else if (_touching)
            {
                _touching = false;
                if (_contactFrames > 1) HeldContacts++;
                output.Add(new StrikeEvent(StrikeEventKind.ContactEnded, swing.SwingId, swing.TipSpeed,
                    contactFrames: _contactFrames, contactHadHit: _contactHadHit));
            }

            if (_swinging && swing.IsSwinging && touching)
            {
                _swingTouched = true;
                if (verdict == HitVerdict.Hit) _swingHit = true;
            }

            if (swing.Phase == SwingPhase.Ended && _swinging)
            {
                _swinging = false;
                var outcome = _swingHit ? SwingOutcome.Hit : _swingTouched ? SwingOutcome.ContactWithoutHit : SwingOutcome.Whiff;
                if (outcome == SwingOutcome.Whiff) Whiffs++;
                output.Add(new StrikeEvent(StrikeEventKind.SwingEnded, swing.SwingId, _swingPeak, outcome, swing.Break));
            }

            return output.Count - before;
        }

        /// <summary>回数と途中の状態を初期化する（検証のやり直し・再挑戦）。</summary>
        public void Reset()
        {
            _touching = false;
            _contactFrames = 0;
            _contactHadHit = false;
            _swinging = false;
            _swingPeak = 0f;
            _swingTouched = false;
            _swingHit = false;
            Swings = 0;
            Hits = 0;
            Whiffs = 0;
            SlowContacts = 0;
            RepeatContacts = 0;
            HeldContacts = 0;
        }
    }

    /// <summary>出来事の文言。ログは日本語、画面表示は既定フォントで読める英字の短い符号。</summary>
    public static class StrikeMessages
    {
        public static string Describe(in StrikeEvent e)
        {
            switch (e.Kind)
            {
                case StrikeEventKind.SwingStarted:
                    return $"振り#{e.SwingId} 開始 刃先{e.Speed:0.00}m/s";
                case StrikeEventKind.SwingEnded:
                    return $"振り#{e.SwingId} 終了 最大{e.Speed:0.00}m/s 結果={DescribeOutcome(e.Outcome)}（{DescribeBreak(e.Break)}）";
                case StrikeEventKind.Hit:
                    return $"命中 振り#{e.SwingId} 刃先{e.Speed:0.00}m/s";
                case StrikeEventKind.SlowContact:
                    return $"不命中：ゆっくり接触（振り中でない） 刃先{e.Speed:0.00}m/s";
                case StrikeEventKind.RepeatContact:
                    return $"不命中：同じ振り#{e.SwingId}で再接触（1振り1命中）";
                case StrikeEventKind.ContactEnded:
                    return e.ContactFrames > 1
                        ? $"接触終了：{e.ContactFrames}フレーム触れ続け（追加の命中なし）{(e.ContactHadHit ? "・この接触で命中済み" : "")}"
                        : $"接触終了：1フレーム{(e.ContactHadHit ? "・命中" : "")}";
                default:
                    return e.Kind.ToString();
            }
        }

        public static string ShortCode(in StrikeEvent e)
        {
            switch (e.Kind)
            {
                case StrikeEventKind.SwingStarted: return $"SWING#{e.SwingId} START {e.Speed:0.0}m/s";
                case StrikeEventKind.SwingEnded: return $"SWING#{e.SwingId} END peak {e.Speed:0.0}m/s {e.Outcome.ToString().ToUpperInvariant()}";
                case StrikeEventKind.Hit: return $"HIT (swing#{e.SwingId}) {e.Speed:0.0}m/s";
                case StrikeEventKind.SlowContact: return $"NO HIT: SLOW CONTACT {e.Speed:0.0}m/s";
                case StrikeEventKind.RepeatContact: return $"NO HIT: SAME SWING #{e.SwingId}";
                case StrikeEventKind.ContactEnded: return $"CONTACT END {e.ContactFrames}f{(e.ContactHadHit ? " (hit)" : "")}";
                default: return e.Kind.ToString();
            }
        }

        private static string DescribeOutcome(SwingOutcome o)
        {
            switch (o)
            {
                case SwingOutcome.Hit: return "命中";
                case SwingOutcome.Whiff: return "空振り（接触なし）";
                case SwingOutcome.ContactWithoutHit: return "接触したが命中なし（触れ続け）";
                default: return "なし";
            }
        }

        private static string DescribeBreak(SwingBreak b)
        {
            switch (b)
            {
                case SwingBreak.SlowedDown: return "減速";
                case SwingBreak.TrackingLost: return "追跡の途切れ・剣を下ろした";
                case SwingBreak.Jump: return "位置の飛び";
                case SwingBreak.Gap: return "フレームの間隔の空き";
                default: return "-";
            }
        }
    }
}
