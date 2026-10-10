using UnityEngine;
using UnityEngine.XR;

namespace OrdinalScale.Platform.XR
{
    /// <summary>
    /// 1つの Quest シーンを、XR が動いているとき（Quest 実機）と動いていないとき（Editor の再生）の両方で使えるようにする。
    /// XR が無ければ XR 専用の入力（コントローラ）を止めて Editor 用の代替入力（マウス）を有効にし、
    /// カメラを目の高さに置く。PlatformRig（-1000）より先に動かし、リグが有効な実装を拾えるようにする。
    /// </summary>
    [DefaultExecutionOrder(-1100)]
    public sealed class XRRuntimeSwitch : MonoBehaviour
    {
        [Tooltip("XR が動いているときだけ使う GameObject（コントローラの剣など）。")]
        [SerializeField] private GameObject[] xrOnlyObjects = new GameObject[0];
        [Tooltip("XR が動いているときだけ有効にするコンポーネント（XR Origin、TrackedPoseDriver、ARCameraManager など）。")]
        [SerializeField] private Behaviour[] xrOnlyBehaviours = new Behaviour[0];
        [Tooltip("XR が動いていないときだけ使う GameObject（マウスの剣など）。")]
        [SerializeField] private GameObject[] editorOnlyObjects = new GameObject[0];
        [Tooltip("XR が無いときに目の高さへ置くカメラ。")]
        [SerializeField] private Transform editorCamera;
        [Tooltip("XR が無いときのカメラの高さ（床からのm）。")]
        [SerializeField] private float editorEyeHeight = 1.6f;

        /// <summary>このシーンが XR（Quest 実機）で動いているか。</summary>
        public bool IsXRActive { get; private set; }

        private void Awake()
        {
            // XR Plug-in Management の「起動時に初期化」はシーンの読み込みより前に終わるので、Awake の時点で判定できる
            IsXRActive = XRSettings.isDeviceActive;

            foreach (var go in xrOnlyObjects)
                if (go != null) go.SetActive(IsXRActive);
            foreach (var behaviour in xrOnlyBehaviours)
                if (behaviour != null) behaviour.enabled = IsXRActive;
            foreach (var go in editorOnlyObjects)
                if (go != null) go.SetActive(!IsXRActive);

            if (!IsXRActive && editorCamera != null)
            {
                editorCamera.localPosition = new Vector3(0f, editorEyeHeight, 0f);
                editorCamera.localRotation = Quaternion.identity;
            }

            Debug.Log($"[OrdinalScale][Q0] 実行環境: {(IsXRActive ? $"XR ({XRSettings.loadedDeviceName})" : "XRなし（Editorの代替入力）")}", this);
        }
    }
}
