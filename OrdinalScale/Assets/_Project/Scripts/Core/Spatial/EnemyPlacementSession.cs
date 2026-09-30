namespace OrdinalScale.Core.Spatial
{
    /// <summary>配置操作1回分の結果の種類。</summary>
    public enum PlacementOutcome
    {
        /// <summary>まだ敵がいない状態から、新しく配置した。</summary>
        Placed,
        /// <summary>すでにいる敵を、選んだ位置へ移した（敵は常に1体）。</summary>
        Moved,
        /// <summary>配置を抑止した。敵は（いれば）元の位置に残る。</summary>
        Blocked,
    }

    /// <summary>配置操作1回分の結果。</summary>
    public readonly struct PlacementAttemptResult
    {
        public PlacementOutcome Outcome { get; }
        public PlacementBlockReason Reason { get; }

        /// <summary>このセッションで何回目の操作か（1始まり）。証拠のログと画面の対応づけに使う。</summary>
        public int AttemptNumber { get; }

        public bool IsBlocked => Outcome == PlacementOutcome.Blocked;

        public PlacementAttemptResult(PlacementOutcome outcome, PlacementBlockReason reason, int attemptNumber)
        {
            Outcome = outcome;
            Reason = reason;
            AttemptNumber = attemptNumber;
        }
    }

    /// <summary>
    /// 敵1体の配置状態を管理する（S2）。配置可否は PlacementGate で判定し、
    /// 抑止した操作では既存の敵を動かさない。位置や見た目は Unity 側が結果を見て反映する。
    /// </summary>
    public sealed class EnemyPlacementSession
    {
        public bool HasEnemy { get; private set; }
        public int AttemptCount { get; private set; }
        public int PlacedCount { get; private set; }
        public int BlockedCount { get; private set; }

        /// <summary>最後の操作の結果。操作がまだ無ければ AttemptNumber が 0。</summary>
        public PlacementAttemptResult LastResult { get; private set; }

        /// <summary>
        /// 選んだ位置への配置を試みる。
        /// </summary>
        /// <param name="status">操作した瞬間の空間認識の状態。</param>
        /// <param name="targetSurface">選んだ位置にレイを飛ばして当たった面の種類（当たらなければ None）。</param>
        public PlacementAttemptResult Attempt(in SpatialStatus status, SurfaceKind targetSurface)
        {
            AttemptCount++;

            var reason = PlacementGate.EvaluateTarget(status, targetSurface);
            PlacementAttemptResult result;
            if (reason != PlacementBlockReason.None)
            {
                BlockedCount++;
                result = new PlacementAttemptResult(PlacementOutcome.Blocked, reason, AttemptCount);
            }
            else
            {
                PlacedCount++;
                result = new PlacementAttemptResult(HasEnemy ? PlacementOutcome.Moved : PlacementOutcome.Placed, PlacementBlockReason.None, AttemptCount);
                HasEnemy = true;
            }

            LastResult = result;
            return result;
        }

        /// <summary>敵を取り除く（再挑戦・検証のやり直し用）。操作回数の記録は残す。</summary>
        public void RemoveEnemy()
        {
            HasEnemy = false;
        }
    }
}
