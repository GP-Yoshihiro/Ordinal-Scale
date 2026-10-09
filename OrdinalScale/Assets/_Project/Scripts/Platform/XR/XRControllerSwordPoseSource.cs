using UnityEngine;
using UnityEngine.XR;

namespace OrdinalScale.Platform.XR
{
    /// <summary>
    /// Quest（OpenXR）のコントローラを剣として扱う。グリップ姿勢（Oculus Touch Controller Profile の devicePosition /
    /// deviceRotation）から刃の線分を作り、ISwordPoseSource として Gameplay へ渡す。
    /// UnityEngine.XR.InputDevices（組み込みモジュール）を使うので、XR パッケージ未導入でもコンパイルできる。
    /// 刃の長さ・角度は仮の値で、Quest 実機で調整する（条件 C8）。
    /// </summary>
    public sealed class XRControllerSwordPoseSource : MonoBehaviour, ISwordPoseSource
    {
        public enum Hand
        {
            Right,
            Left,
        }

        [Tooltip("XR Origin の Camera Offset。コントローラの位置は追跡空間の座標で届くため、これでワールド座標に直す。")]
        [SerializeField] private Transform trackingSpace;
        [SerializeField] private Hand hand = Hand.Right;
        [Tooltip("刃の長さ（m）。仮の値。")]
        [SerializeField] private float bladeLength = 0.9f;
        [Tooltip("グリップの向きに対する刃の角度（度）。(0,0,0) で握りこぶしの筒の向き（グリップ姿勢の +Z）に刃が伸びる。実機で調整する。")]
        [SerializeField] private Vector3 bladeLocalEuler = Vector3.zero;

        private InputDevice _device;

        private XRNode Node => hand == Hand.Right ? XRNode.RightHand : XRNode.LeftHand;

        public bool TryGetBladePose(out BladePose pose)
        {
            var now = Time.timeAsDouble;
            if (!_device.isValid) _device = InputDevices.GetDeviceAtXRNode(Node);

            if (_device.isValid
                && _device.TryGetFeatureValue(CommonUsages.isTracked, out var tracked) && tracked
                && _device.TryGetFeatureValue(CommonUsages.devicePosition, out var localPosition)
                && _device.TryGetFeatureValue(CommonUsages.deviceRotation, out var localRotation))
            {
                var position = trackingSpace != null ? trackingSpace.TransformPoint(localPosition) : localPosition;
                var rotation = trackingSpace != null ? trackingSpace.rotation * localRotation : localRotation;
                pose = BladePose.FromGrip(position, rotation, Quaternion.Euler(bladeLocalEuler), bladeLength, now);
                return true;
            }

            pose = BladePose.Untracked(now);
            return false;
        }
    }
}
