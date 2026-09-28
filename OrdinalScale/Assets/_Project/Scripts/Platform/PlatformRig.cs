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
            if (Input == null) Debug.LogError($"[{nameof(PlatformRig)}] {nameof(IInputController)} の実装が子階層に見つかりません。", this);
        }
    }
}
