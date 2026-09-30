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
        public IInputController Input { get; private set; }

        private void Awake()
        {
            Spatial = GetComponentInChildren<IARSpatialProvider>(true);
            Input = GetComponentInChildren<IInputController>(true);

            if (Spatial == null) Debug.LogError($"[{nameof(PlatformRig)}] {nameof(IARSpatialProvider)} の実装が子階層に見つかりません。", this);
            // iPhone先行検証のように攻撃入力を使わない構成もあるため、入力の欠落は警告に留める
            if (Input == null) Debug.LogWarning($"[{nameof(PlatformRig)}] {nameof(IInputController)} の実装が子階層にありません（攻撃入力なしで動作）。", this);
        }
    }
}
