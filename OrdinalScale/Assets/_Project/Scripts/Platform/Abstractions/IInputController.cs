using UnityEngine;

namespace OrdinalScale.Platform
{
    /// <summary>
    /// 「照準レイ＋入力の瞬間」の抽象。Editor ではマウス、iPhone では画面タップ等に対応させる。
    /// 注意：剣の命中判定には使わない。剣は「振っている最中に刃が体に触れた時だけ・1振り1命中」が確定条件のため、
    /// ISwordPoseSource（新設予定）と Core の振り判定で扱う（Claude/docs/sword-input-design.md）。
    /// 本インターフェースは S2 の配置タップ実装時に IPointerInput（指す・選ぶ）へ改名する予定。
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
