using OrdinalScale.Core.Spatial;
using UnityEngine;
using UnityEngine.XR;

namespace OrdinalScale.Platform.XR
{
    /// <summary>
    /// Quest（OpenXR）用の空間認識。Q0 では部屋のスキャン（平面・メッシュ）を使わず、
    /// XR Origin を「床の高さ」に置く追跡原点（Floor）を床とみなす。
    /// - 頭部姿勢：XR Origin 配下の Main Camera（TrackedPoseDriver が動かす）
    /// - 床の高さ：floorReference（XR Origin のルート）のワールドY
    /// - 環境へのレイ：その高さの無限の水平面（上向き）とだけ交差する
    /// 平面検出（Space Setup の部屋データと com.oculus.permission.USE_SCENE が必要）は Q4 以降で必要になったら追加する。
    /// UnityEngine.XR（組み込みモジュール）だけを使うので、XR パッケージが未導入でもコンパイルでき、
    /// XR が動いていない Editor ではカメラの位置をそのまま頭部とみなす。
    /// </summary>
    public sealed class XRHeadSpatialProvider : MonoBehaviour, IARSpatialProvider
    {
        [SerializeField] private Camera headCamera;
        [Tooltip("床の高さの基準。XR Origin（追跡原点 Floor）のルートを指定する。")]
        [SerializeField] private Transform floorReference;

        private InputDevice _head;

        public bool IsReady => headCamera != null && floorReference != null && CurrentPhase() == TrackingPhase.Tracking;

        // Quest のパススルー表示はカメラ画像をアプリへ渡さないため、実行時のカメラ許可は不要（Granted 扱い）。
        // 床は追跡原点から分かるので「水平面1枚」とみなす。
        public SpatialStatus Status => new SpatialStatus(CameraPermission.Granted, CurrentPhase(), horizontalPlaneCount: 1);

        public Pose HeadPose => headCamera != null
            ? new Pose(headCamera.transform.position, headCamera.transform.rotation)
            : Pose.identity;

        private void Reset()
        {
            headCamera = Camera.main;
        }

        private void Awake()
        {
            if (headCamera == null) headCamera = Camera.main;
        }

        public bool TryGetFloorHeight(out float floorY)
        {
            if (floorReference == null)
            {
                floorY = 0f;
                return false;
            }

            floorY = floorReference.position.y;
            return true;
        }

        public bool TryRaycastEnvironment(Ray ray, float maxDistance, out EnvironmentHit hit)
        {
            hit = default;
            if (!TryGetFloorHeight(out var floorY)) return false;

            // 下向きのレイだけが床に当たる
            var dirY = ray.direction.y;
            if (dirY > -1e-4f) return false;

            var distance = (floorY - ray.origin.y) / dirY;
            if (distance < 0f || distance > maxDistance) return false;

            var point = ray.origin + ray.direction * distance;
            var forward = Vector3.ProjectOnPlane(ray.direction, Vector3.up);
            if (forward.sqrMagnitude < 1e-6f) forward = Vector3.forward;

            hit = new EnvironmentHit(new Pose(point, Quaternion.LookRotation(forward.normalized, Vector3.up)),
                SurfaceKind.HorizontalUp, distance);
            return true;
        }

        private TrackingPhase CurrentPhase()
        {
            if (headCamera == null) return TrackingPhase.Initializing;

            // XR が動いていない（Editor の再生など）ときはカメラを頭とみなし、常に追跡中とする
            if (!XRSettings.isDeviceActive) return TrackingPhase.Tracking;

            if (!_head.isValid) _head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            if (!_head.isValid) return TrackingPhase.Initializing;

            return _head.TryGetFeatureValue(CommonUsages.isTracked, out var tracked) && tracked
                ? TrackingPhase.Tracking
                : TrackingPhase.Limited;
        }
    }
}
