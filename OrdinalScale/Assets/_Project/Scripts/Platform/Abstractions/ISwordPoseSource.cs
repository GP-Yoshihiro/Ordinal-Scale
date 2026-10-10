namespace OrdinalScale.Platform
{
    /// <summary>
    /// 剣の姿勢の入力源。Quest ではコントローラのグリップ姿勢、Editor ではマウスから作る。
    /// 戦闘の命中は IPointerInput（指す・選ぶ）ではなく、この姿勢の時系列と Core の振り判定で決める
    /// （確定条件 C1〜C8、Claude/docs/sword-input-design.md）。
    /// ゲーム側は Update で1フレーム1回呼び出し、得た姿勢を BladePose.ToSample() で Core へ渡す。
    /// </summary>
    public interface ISwordPoseSource
    {
        /// <summary>
        /// 現在フレームの刃の姿勢を取得する。追跡できていなければ false を返し、
        /// pose には IsTracked = false で現在時刻だけを入れる（追跡の途切れを Core 側で区別するため）。
        /// </summary>
        bool TryGetBladePose(out BladePose pose);
    }
}
