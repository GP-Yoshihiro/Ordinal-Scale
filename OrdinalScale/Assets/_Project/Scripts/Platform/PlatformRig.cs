using UnityEngine;

namespace OrdinalScale.Platform
{
    /// <summary>
    /// シーン上のプラットフォーム実装への窓口。子階層にあるプロバイダ実装を解決して公開する。
    /// ゲームロジックは具体クラスではなく、このリグ経由でインターフェースを取得する。
    /// デバイスごとにリグのプレハブ（Editor / Quest / XREAL）を差し替えて切り替える。
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class PlatformRig : MonoBehaviour
    {
        public IARSpatialProvider Spatial { get; private set; }
        public IPointerInput Pointer { get; private set; }

        private void Awake()
        {
            Spatial = GetComponentInChildren<IARSpatialProvider>(true);
            Pointer = GetComponentInChildren<IPointerInput>(true);

            if (Spatial == null) Debug.LogError($"[{nameof(PlatformRig)}] {nameof(IARSpatialProvider)} の実装が子階層に見つかりません。", this);
            // S1 の状態確認だけの構成など、指す・選ぶ入力を使わない場合もあるため警告に留める
            if (Pointer == null) Debug.LogWarning($"[{nameof(PlatformRig)}] {nameof(IPointerInput)} の実装が子階層にありません（タップ・クリック操作なしで動作）。", this);
        }
    }
}
