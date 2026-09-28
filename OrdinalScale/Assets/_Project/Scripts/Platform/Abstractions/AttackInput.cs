using UnityEngine;

namespace OrdinalScale.Platform
{
    /// <summary>攻撃の種別。STEP 1 では Shot（照準レイ方向への射撃）のみ使う。</summary>
    public enum AttackKind
    {
        Shot,
    }

    /// <summary>デバイス非依存に正規化した1回分の攻撃入力。</summary>
    public readonly struct AttackInput
    {
        public AttackKind Kind { get; }
        public Ray Ray { get; }

        public AttackInput(AttackKind kind, Ray ray)
        {
            Kind = kind;
            Ray = ray;
        }
    }
}
