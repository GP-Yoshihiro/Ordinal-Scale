using UnityEngine;
using UnityEngine.XR;

namespace OrdinalScale.Platform.XR
{
    /// <summary>
    /// Quest（OpenXR）のコントローラで「指す・選ぶ」（IPointerInput、Q4）。決着後の2択パネルを選ぶのに使う。
    /// - 指し示しレイ：グリップ姿勢の向き（剣と同じ向き）。剣先をパネルに向ければよい。
    /// - 選ぶ：トリガー（CommonUsages.triggerButton）の押し始め。押しっぱなしは1回だけ。
    /// 剣の命中判定には使わない（剣は ISwordPoseSource と Core の振り判定）。
    /// UnityEngine.XR（組み込みモジュール）だけを使うので XR パッケージ未導入でもコンパイルできる。Quest 実機では未確認。
    /// </summary>
    public sealed class XRControllerPointerInput : MonoBehaviour, IPointerInput
    {
        [Tooltip("XR Origin の Camera Offset。コントローラの位置は追跡空間の座標で届くため、これでワールド座標に直す。")]
        [SerializeField] private Transform trackingSpace;
        [SerializeField] private XRControllerSwordPoseSource.Hand hand = XRControllerSwordPoseSource.Hand.Right;
        [Tooltip("グリップの向きに対するレイの角度（度）。剣（XRControllerSwordPoseSource の bladeLocalEuler）と合わせる。")]
        [SerializeField] private Vector3 rayLocalEuler = Vector3.zero;

        private InputDevice _device;
        private bool _wasPressed;
        private bool _selectPending;
        private Ray _pointerRay;
        private Ray _selectRay;

        public Ray PointerRay => _pointerRay;

        private XRNode Node => hand == XRControllerSwordPoseSource.Hand.Right ? XRNode.RightHand : XRNode.LeftHand;

        private void Update()
        {
            if (!_device.isValid) _device = InputDevices.GetDeviceAtXRNode(Node);
            if (!_device.isValid
                || !_device.TryGetFeatureValue(CommonUsages.isTracked, out var tracked) || !tracked
                || !_device.TryGetFeatureValue(CommonUsages.devicePosition, out var localPosition)
                || !_device.TryGetFeatureValue(CommonUsages.deviceRotation, out var localRotation))
            {
                // 追跡していない間の押し始めは数えない（復帰した瞬間に誤って決まらないように）
                _wasPressed = true;
                return;
            }

            var position = trackingSpace != null ? trackingSpace.TransformPoint(localPosition) : localPosition;
            var rotation = (trackingSpace != null ? trackingSpace.rotation * localRotation : localRotation) * Quaternion.Euler(rayLocalEuler);
            _pointerRay = new Ray(position, rotation * Vector3.forward);

            var pressed = _device.TryGetFeatureValue(CommonUsages.triggerButton, out var trigger) && trigger;
            if (pressed && !_wasPressed)
            {
                _selectPending = true;
                _selectRay = _pointerRay;
            }

            _wasPressed = pressed;
        }

        public bool TryConsumeSelect(out Ray selectRay)
        {
            selectRay = _selectRay;
            if (!_selectPending) return false;
            _selectPending = false;
            return true;
        }
    }
}
