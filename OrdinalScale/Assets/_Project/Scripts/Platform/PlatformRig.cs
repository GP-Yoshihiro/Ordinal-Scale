using UnityEngine;

namespace OrdinalScale.Platform
{
    /// <summary>
    /// シーン上のプラットフォーム実装への窓口。子階層にあるプロバイダ実装を解決して公開する。
    /// ゲームロジックは具体クラスではなく、このリグ経由でインターフェースを取得する。
    /// デバイスごとにリグのプレハブ（Editor / Quest / XREAL）を差し替えて切り替える。
    /// 同じ種類の実装が複数ある場合（Quest シーンのコントローラ用と Editor 用など）は、
    /// 有効な GameObject 上のものを優先する。切り替えは XRRuntimeSwitch が PlatformRig より先に行う。
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class PlatformRig : MonoBehaviour
    {
        public IARSpatialProvider Spatial { get; private set; }
        public IPointerInput Pointer { get; private set; }

        /// <summary>剣の姿勢の入力源。iPhone のシーンには無い（剣操作をしない）ので null になりうる。</summary>
        public ISwordPoseSource Sword { get; private set; }

        private void Awake()
        {
            Spatial = Resolve<IARSpatialProvider>();
            Pointer = Resolve<IPointerInput>();
            Sword = Resolve<ISwordPoseSource>();

            if (Spatial == null) Debug.LogError($"[{nameof(PlatformRig)}] {nameof(IARSpatialProvider)} の実装が子階層に見つかりません。", this);
            // S1 の状態確認だけの構成など、指す・選ぶ入力を使わない場合もあるため警告に留める
            if (Pointer == null) Debug.LogWarning($"[{nameof(PlatformRig)}] {nameof(IPointerInput)} の実装が子階層にありません（タップ・クリック操作なしで動作）。", this);
        }

        private T Resolve<T>() where T : class
        {
            // 有効なものを優先し、無ければ従来どおり無効な GameObject も含めて探す（iPhone シーンの既存の挙動を保つ）
            return GetComponentInChildren<T>(false) ?? GetComponentInChildren<T>(true);
        }
    }
}
