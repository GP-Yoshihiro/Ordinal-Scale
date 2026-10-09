using UnityEngine;
#if OS_HAS_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace OrdinalScale.Platform.EditorSim
{
    /// <summary>
    /// Editor で剣の姿勢を作る代替入力（条件 C7）。マウス位置へのカメラレイ上、手前 gripDistance(m) を手元とし、
    /// 刃はレイの奥向きに伸びる。マウスを速く動かすと刃先が速く動く（＝振り）、ゆっくり動かすと押し当てになる。
    /// Q0 では姿勢の受け渡しを確かめる最小実装。Q1 でドラッグ中だけ剣を出す操作と、画面速度→刃先速度の換算を加える。
    /// </summary>
    public sealed class EditorSwordPoseSource : MonoBehaviour, ISwordPoseSource
    {
        [SerializeField] private Camera viewCamera;
        [Tooltip("カメラから手元までの距離（m）。")]
        [SerializeField] private float gripDistance = 0.45f;
        [Tooltip("刃の長さ（m）。Quest 側と同じ仮の値。")]
        [SerializeField] private float bladeLength = 0.9f;

        private void Reset()
        {
            viewCamera = Camera.main;
        }

        private void Awake()
        {
            if (viewCamera == null) viewCamera = Camera.main;
        }

        public bool TryGetBladePose(out BladePose pose)
        {
            var now = Time.timeAsDouble;
            if (viewCamera == null || !TryReadMouse(out var screenPosition))
            {
                pose = BladePose.Untracked(now);
                return false;
            }

            var ray = viewCamera.ScreenPointToRay(screenPosition);
            var grip = ray.origin + ray.direction * gripDistance;
            var rotation = Quaternion.LookRotation(ray.direction, viewCamera.transform.up);
            pose = BladePose.FromGrip(grip, rotation, Quaternion.identity, bladeLength, now);
            return true;
        }

        private static bool TryReadMouse(out Vector2 screenPosition)
        {
#if OS_HAS_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
            var mouse = Mouse.current;
            screenPosition = mouse != null ? mouse.position.ReadValue() : default;
            return mouse != null && IsInsideScreen(screenPosition);
#elif ENABLE_LEGACY_INPUT_MANAGER
            screenPosition = UnityEngine.Input.mousePosition;
            return IsInsideScreen(screenPosition);
#else
            screenPosition = default;
            return false;
#endif
        }

        // Game ビューの外にマウスがあるときは「追跡していない」扱いにする（追跡の途切れの再現にも使える）
        private static bool IsInsideScreen(Vector2 p)
        {
            return p.x >= 0f && p.y >= 0f && p.x <= Screen.width && p.y <= Screen.height;
        }
    }
}
