using System.Collections;
using System.Collections.Generic;
using OrdinalScale.Core.Spatial;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace OrdinalScale.Platform.ARFoundation
{
    /// <summary>
    /// AR Foundation による空間認識の実装。iPhone（Apple ARKit XR Plug-in）で使う。
    /// AR Foundation は Quest 向けにも Unity OpenXR: Meta 経由のプロバイダがあるため、
    /// 将来 Quest でこの実装を使い回せるかは 11月18日以降の実機で判断する（未確認）。
    /// </summary>
    public sealed class ARFoundationSpatialProvider : MonoBehaviour, IARSpatialProvider
    {
        [SerializeField] private XROrigin origin;
        [Tooltip("XR Origin に追加した ARPlaneManager。未設定だと平面数は常に0になる。")]
        [SerializeField] private ARPlaneManager planeManager;
        [Tooltip("XR Origin に追加した ARRaycastManager。未設定だと環境へのレイキャストは常に失敗する。")]
        [SerializeField] private ARRaycastManager raycastManager;

        // Raycast の結果バッファ。毎回 new しないよう使い回す
        private static readonly List<ARRaycastHit> s_Hits = new List<ARRaycastHit>();

        private CameraPermission _cameraPermission = CameraPermission.Unknown;

        public bool IsReady => origin != null && ARSession.state == ARSessionState.SessionTracking;

        public SpatialStatus Status => new SpatialStatus(
            _cameraPermission,
            ToTrackingPhase(ARSession.state, ARSession.notTrackingReason),
            CountHorizontalPlanes());

        public Pose HeadPose
        {
            get
            {
                var cam = origin != null ? origin.Camera : null;
                return cam != null ? new Pose(cam.transform.position, cam.transform.rotation) : Pose.identity;
            }
        }

        public ARPlaneManager PlaneManager => planeManager;

        private void Reset()
        {
            ResolveReferences();
        }

        private void Awake()
        {
            ResolveReferences();
            if (origin == null) Debug.LogError($"[{nameof(ARFoundationSpatialProvider)}] シーンに XR Origin がありません。GameObject > XR > XR Origin (Mobile AR) を追加してください。", this);
        }

        private IEnumerator Start()
        {
            // iOS ではここで OS の許可ダイアログが出る（ARKit のセッション開始でも同じ許可が求められる）。
            // 拒否されたかどうかを画面に出せるよう、結果を明示的に記録する。
            yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
            _cameraPermission = Application.HasUserAuthorization(UserAuthorization.WebCam)
                ? CameraPermission.Granted
                : CameraPermission.Denied;
        }

        public bool TryGetFloorHeight(out float floorY)
        {
            // 最も低い上向き水平面を床とみなす（机や椅子の天板は床より高い）
            floorY = 0f;
            if (planeManager == null) return false;

            var found = false;
            foreach (var plane in planeManager.trackables)
            {
                if (!IsUsableHorizontalPlane(plane)) continue;

                var y = plane.transform.position.y;
                if (!found || y < floorY)
                {
                    floorY = y;
                    found = true;
                }
            }

            return found;
        }

        public bool TryRaycastEnvironment(Ray ray, float maxDistance, out EnvironmentHit hit)
        {
            // 平面の境界内だけを対象にする。境界外（無限平面扱い）に当てると「未検出の場所」に置けてしまうため。
            // 結果は近い順に並ぶので、最も手前の面を「タップした面」とみなす
            if (raycastManager != null
                && raycastManager.Raycast(ray, s_Hits, TrackableType.PlaneWithinPolygon)
                && s_Hits[0].distance <= maxDistance)
            {
                var first = s_Hits[0];
                var plane = first.trackable as ARPlane;
                var surface = plane != null && plane.subsumedBy == null ? ToSurfaceKind(plane.alignment) : SurfaceKind.None;
                hit = new EnvironmentHit(first.pose, surface, first.distance);
                return true;
            }

            hit = default;
            return false;
        }

        /// <summary>AR Foundation の平面の向きを、デバイス共通の面の種類へ写像する。</summary>
        public static SurfaceKind ToSurfaceKind(PlaneAlignment alignment)
        {
            switch (alignment)
            {
                case PlaneAlignment.HorizontalUp: return SurfaceKind.HorizontalUp;
                case PlaneAlignment.HorizontalDown: return SurfaceKind.HorizontalDown;
                case PlaneAlignment.Vertical: return SurfaceKind.Vertical;
                default: return SurfaceKind.Other;
            }
        }

        /// <summary>AR Foundation のセッション状態を、デバイス共通の追跡段階へ写像する。</summary>
        public static TrackingPhase ToTrackingPhase(ARSessionState state, NotTrackingReason reason)
        {
            switch (state)
            {
                case ARSessionState.Unsupported:
                    return TrackingPhase.Unsupported;
                case ARSessionState.SessionTracking:
                    return TrackingPhase.Tracking;
                case ARSessionState.SessionInitializing:
                    // AR Foundation は ARKit の「limited」も SessionInitializing として報告するため、理由で区別する
                    switch (reason)
                    {
                        case NotTrackingReason.None:
                        case NotTrackingReason.Initializing:
                            return TrackingPhase.Initializing;
                        case NotTrackingReason.Unsupported:
                            return TrackingPhase.Unsupported;
                        default:
                            return TrackingPhase.Limited;
                    }
                default:
                    // None / CheckingAvailability / NeedsInstall / Installing / Ready
                    return TrackingPhase.Initializing;
            }
        }

        private int CountHorizontalPlanes()
        {
            if (planeManager == null) return 0;

            var count = 0;
            foreach (var plane in planeManager.trackables)
            {
                if (IsUsableHorizontalPlane(plane)) count++;
            }

            return count;
        }

        private static bool IsUsableHorizontalPlane(ARPlane plane)
        {
            // 他の平面に統合された古い平面と、追跡が切れている平面は数えない
            return plane.alignment == PlaneAlignment.HorizontalUp
                && plane.subsumedBy == null
                && plane.trackingState == TrackingState.Tracking;
        }

        private void ResolveReferences()
        {
            if (origin == null) origin = FindAnyObjectByType<XROrigin>();
            if (origin == null) return;

            if (planeManager == null) planeManager = origin.GetComponent<ARPlaneManager>();
            if (raycastManager == null) raycastManager = origin.GetComponent<ARRaycastManager>();
        }
    }
}
