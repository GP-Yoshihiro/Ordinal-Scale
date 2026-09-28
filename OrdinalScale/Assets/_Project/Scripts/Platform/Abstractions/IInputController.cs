using UnityEngine;

namespace OrdinalScale.Platform
{
    /// <summary>
    /// 攻撃入力の抽象。Editor ではマウス、Quest ではハンドトラッキング/コントローラ、
    /// ARグラスではスマホコントローラ等、デバイスごとの入力を「照準レイ＋攻撃の瞬間」に正規化する。
    /// </summary>
    public interface IInputController
    {
        /// <summary>現在の照準レイ（ワールド座標）。照準UIの描画に使う。</summary>
        Ray AimRay { get; }

        /// <summary>
        /// このフレームに発生した攻撃入力を1件取り出す。無ければ false。
        /// 呼び出し側（Gameplay層）が Update で1回ポーリングする前提。
        /// </summary>
        bool TryConsumeAttack(out AttackInput attack);
    }
}
