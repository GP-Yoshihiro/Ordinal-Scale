using OrdinalScale.Core.Spatial;
using UnityEngine;

namespace OrdinalScale.Platform
{
    /// <summary>
    /// 空間認識の抽象。AR Foundation (iPhone) / Meta XR / NRSDK (XREAL) / Editor シミュレータの差をここで吸収する。
    /// ゲームロジックはこのインターフェースだけを見て、敵の配置位置や頭部姿勢を取得する。
    /// </summary>
    public interface IARSpatialProvider
    {
        /// <summary>トラッキング・空間認識の準備が整ったか。false の間は敵を配置しない。</summary>
        bool IsReady { get; }

        /// <summary>
        /// カメラ許可・追跡段階・検出平面数のスナップショット。配置可否は Core の PlacementGate で判定する。
        /// 利用者に「なぜ置けないか」を示すため、IsReady より細かい理由を返す。
        /// </summary>
        SpatialStatus Status { get; }

        /// <summary>プレイヤー頭部（HMD / グラス / Editorカメラ）のワールド姿勢。</summary>
        Pose HeadPose { get; }

        /// <summary>床の高さ（ワールドY）。未検出なら false。</summary>
        bool TryGetFloorHeight(out float floorY);

        /// <summary>
        /// 現実環境（検出済みの平面・メッシュ）に対するレイキャスト。敵の配置先探索に使う。
        /// 検出済みの面の範囲外（未検出の空間）には当たらないこと。当たった面の種類も返す。
        /// </summary>
        bool TryRaycastEnvironment(Ray ray, float maxDistance, out EnvironmentHit hit);
    }
}
