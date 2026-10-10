using System;

namespace OrdinalScale.Core.Combat
{
    /// <summary>
    /// 空間に置いた長方形のパネル（決着後の「終了」「再挑戦」の選択用。Q4）。UnityEngine に依存しない。
    /// 中心・右方向・上方向（いずれも単位ベクトル、右と上は直交）と半分の幅・高さで表す。
    /// </summary>
    public readonly struct ChoicePanel
    {
        public float CenterX { get; }
        public float CenterY { get; }
        public float CenterZ { get; }
        public float RightX { get; }
        public float RightY { get; }
        public float RightZ { get; }
        public float UpX { get; }
        public float UpY { get; }
        public float UpZ { get; }
        public float HalfWidth { get; }
        public float HalfHeight { get; }

        public ChoicePanel(float cx, float cy, float cz, float rx, float ry, float rz, float ux, float uy, float uz,
            float halfWidth, float halfHeight)
        {
            if (!(halfWidth > 0f) || !(halfHeight > 0f)) throw new ArgumentOutOfRangeException(nameof(halfWidth), "Panel size must be positive.");
            CenterX = cx;
            CenterY = cy;
            CenterZ = cz;
            RightX = rx;
            RightY = ry;
            RightZ = rz;
            UpX = ux;
            UpY = uy;
            UpZ = uz;
            HalfWidth = halfWidth;
            HalfHeight = halfHeight;
        }

        /// <summary>
        /// レイ（始点 o・向き d）がパネルの長方形に当たるか。当たれば始点からの距離を返す。
        /// 裏側からでも当たる（表裏で区別しない）。maxDistance より遠い・後ろ向きなら false。
        /// </summary>
        public bool TryHit(float ox, float oy, float oz, float dx, float dy, float dz, float maxDistance, out float distance)
        {
            distance = 0f;
            // 法線 = 右 × 上
            var nx = RightY * UpZ - RightZ * UpY;
            var ny = RightZ * UpX - RightX * UpZ;
            var nz = RightX * UpY - RightY * UpX;
            var denom = dx * nx + dy * ny + dz * nz;
            if (Math.Abs(denom) < 1e-6f) return false;

            var t = ((CenterX - ox) * nx + (CenterY - oy) * ny + (CenterZ - oz) * nz) / denom;
            if (t < 0f || t > maxDistance) return false;

            var px = ox + dx * t - CenterX;
            var py = oy + dy * t - CenterY;
            var pz = oz + dz * t - CenterZ;
            var u = px * RightX + py * RightY + pz * RightZ;
            var v = px * UpX + py * UpY + pz * UpZ;
            if (Math.Abs(u) > HalfWidth || Math.Abs(v) > HalfHeight) return false;

            distance = t;
            return true;
        }
    }

    /// <summary>
    /// 決着後の2択をヘッドセット内で選ぶ規則（Q4、仕様 E4・Q-5 の「操作UIの方式」への提案）。
    /// 剣先（コントローラの向き）をパネルに向けてトリガーを押したら選ぶ。誤操作を防ぐため：
    /// - パネルを出してから ArmSeconds の間は選べない（剣を振っている途中のトリガーで決まらない）
    /// - パネルを出す前から押していたトリガーは数えない（押し始めの瞬間だけを選択とする：呼び出し側が IPointerInput で渡す）
    /// - 1回選んだら、次に表示し直すまで選ばない
    /// </summary>
    public sealed class ChoiceSelector
    {
        public const double DefaultArmSeconds = 0.6;

        private double _shownAt = -1;
        private bool _decided;

        public ChoiceSelector(double armSeconds = DefaultArmSeconds)
        {
            if (!(armSeconds >= 0)) throw new ArgumentOutOfRangeException(nameof(armSeconds));
            ArmSeconds = armSeconds;
        }

        public double ArmSeconds { get; }

        public bool IsShown => _shownAt >= 0;

        /// <summary>パネルを出した（時刻 now）。</summary>
        public void Show(double now)
        {
            _shownAt = now;
            _decided = false;
        }

        /// <summary>パネルを消した。</summary>
        public void Hide()
        {
            _shownAt = -1;
            _decided = false;
        }

        /// <summary>選べる状態か（表示中・待ち時間が過ぎた・まだ選んでいない）。</summary>
        public bool IsArmed(double now) => IsShown && !_decided && now - _shownAt >= ArmSeconds - 1e-9;

        /// <summary>
        /// 1フレーム分の入力。selectPressed はトリガーの押し始め、hitIndex は押した瞬間にレイが当たっていたパネル（無ければ -1）。
        /// 選んだらそのパネル番号、選ばなければ -1。
        /// </summary>
        public int Update(double now, bool selectPressed, int hitIndex)
        {
            if (!selectPressed || hitIndex < 0 || !IsArmed(now)) return -1;
            _decided = true;
            return hitIndex;
        }
    }
}
