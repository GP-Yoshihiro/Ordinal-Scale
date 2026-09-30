using UnityEngine;

namespace OrdinalScale.Platform
{
    /// <summary>
    /// 「指す・選ぶ」入力の抽象。Editor ではマウスクリック、iPhone では画面タップ、
    /// Quest ではコントローラのレイ（メニュー操作）に対応させる。敵の配置（S2）やメニュー選択に使う。
    /// 注意：剣の命中判定には使わない。剣は「振っている最中に刃が体に触れた時だけ・1振り1命中」が確定条件のため、
    /// ISwordPoseSource（新設予定）と Core の振り判定で扱う（Claude/docs/sword-input-design.md）。
    /// </summary>
    public interface IPointerInput
    {
        /// <summary>現在の指し示しレイ（ワールド座標）。照準やホバー表示に使う。</summary>
        Ray PointerRay { get; }

        /// <summary>
        /// このフレームに発生した「選ぶ」操作（クリック・タップ）を1件取り出す。無ければ false。
        /// 呼び出し側が Update で1回ポーリングする前提（処理順を呼び出し側で固定し、再現しやすくするため）。
        /// </summary>
        bool TryConsumeSelect(out Ray selectRay);
    }
}
