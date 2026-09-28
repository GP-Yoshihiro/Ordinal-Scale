using UnityEngine;

namespace OrdinalScale.Platform
{
    /// <summary>
    /// 空間認識の抽象。Meta XR (MRUK) / NRSDK (XREAL) / Editor シミュレータの差をここで吸収する。
    /// ゲームロジックはこのインターフェースだけを見て、敵の配置位置や頭部姿勢を取得する。
    /// </summary>
    public interface IARSpatialProvider
    {
        /// <summary>トラッキング・空間認識の準備が整ったか。false の間は敵を配置しない。</summary>
        bool IsReady { get; }

        /// <summary>プレイヤー頭部（HMD / グラス / Editorカメラ）のワールド姿勢。</summary>
        Pose HeadPose { get; }

        /// <summary>床の高さ（ワールドY）。未検出なら false。</summary>
        bool TryGetFloorHeight(out float floorY);

        /// <summary>現実環境（床・壁・家具のメッシュや平面）に対するレイキャスト。敵の配置先探索に使う。</summary>
        bool TryRaycastEnvironment(Ray ray, float maxDistance, out Pose hitPose);
    }
}
