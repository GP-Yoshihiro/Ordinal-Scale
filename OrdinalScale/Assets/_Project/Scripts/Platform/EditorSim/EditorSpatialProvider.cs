using UnityEngine;

namespace OrdinalScale.Platform.EditorSim
{
    /// <summary>
    /// 実機なしで動かすための空間認識シミュレータ。
    /// 頭部＝指定カメラ、床＝固定の高さ、環境＝Physics コライダー（シーンに置いた仮の床・壁）として扱う。
    /// </summary>
    public sealed class EditorSpatialProvider : MonoBehaviour, IARSpatialProvider
    {
        [SerializeField] private Camera headCamera;
        [SerializeField] private float floorY = 0f;
        [SerializeField] private LayerMask environmentLayers = ~0;

        public bool IsReady => headCamera != null;

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

        public bool TryGetFloorHeight(out float y)
        {
            y = floorY;
            return true;
        }

        public bool TryRaycastEnvironment(Ray ray, float maxDistance, out Pose hitPose)
        {
            if (Physics.Raycast(ray, out var hit, maxDistance, environmentLayers, QueryTriggerInteraction.Ignore))
            {
                // 面に沿った前方向。レイが面に垂直だと射影が0になるので、その場合は任意の接線を使う。
                var forward = Vector3.ProjectOnPlane(ray.direction, hit.normal);
                if (forward.sqrMagnitude < 1e-6f) forward = Vector3.Cross(hit.normal, Vector3.right);
                if (forward.sqrMagnitude < 1e-6f) forward = Vector3.Cross(hit.normal, Vector3.forward);

                hitPose = new Pose(hit.point, Quaternion.LookRotation(forward.normalized, hit.normal));
                return true;
            }

            hitPose = default;
            return false;
        }
    }
}
