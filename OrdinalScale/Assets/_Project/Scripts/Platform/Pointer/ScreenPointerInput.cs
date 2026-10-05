using UnityEngine;
#if OS_HAS_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace OrdinalScale.Platform
{
    /// <summary>
    /// 画面上の位置から「指す・選ぶ」入力を作る。Editor ではマウス左クリック、iPhone では画面タップ（指1本目の押し始め）。
    /// 画面座標をカメラのレイに変換するので、Editor と iPhone で同じ配置処理を使える。
    /// Input System パッケージ（推奨）と旧 Input Manager の両方に対応。
    /// </summary>
    public sealed class ScreenPointerInput : MonoBehaviour, IPointerInput
    {
        [Tooltip("画面座標をレイに変換するカメラ。iPhone では XR Origin の Main Camera（MainCamera タグ付き）。")]
        [SerializeField] private Camera pointerCamera;

        private bool _selectPending;
        private Ray _selectRay;
        private Ray _pointerRay;

        public Ray PointerRay => _pointerRay;

        private void Reset()
        {
            pointerCamera = Camera.main;
        }

        private void Awake()
        {
            if (pointerCamera == null) pointerCamera = Camera.main;
        }

        private void Update()
        {
            if (pointerCamera == null) return;
            if (!TryReadPointer(out var screenPos, out var pressedThisFrame)) return;

            _pointerRay = pointerCamera.ScreenPointToRay(screenPos);
            if (!pressedThisFrame) return;

            // 同じフレームに複数回押されても1件として扱う（呼び出し側は1フレーム1回ポーリング）
            _selectPending = true;
            _selectRay = _pointerRay;
        }

        public bool TryConsumeSelect(out Ray selectRay)
        {
            selectRay = _selectRay;
            if (!_selectPending) return false;

            _selectPending = false;
            return true;
        }

        private static bool TryReadPointer(out Vector2 screenPos, out bool pressedThisFrame)
        {
#if OS_HAS_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
            // タッチ端末（iPhone）を優先し、無ければマウス（Editor）を使う
            var touchscreen = Touchscreen.current;
            if (touchscreen != null)
            {
                var touch = touchscreen.primaryTouch;
                if (touch.press.isPressed || touch.press.wasPressedThisFrame)
                {
                    screenPos = touch.position.ReadValue();
                    pressedThisFrame = touch.press.wasPressedThisFrame;
                    return true;
                }
            }

            var mouse = Mouse.current;
            if (mouse != null)
            {
                screenPos = mouse.position.ReadValue();
                pressedThisFrame = mouse.leftButton.wasPressedThisFrame;
                return true;
            }

            screenPos = default;
            pressedThisFrame = false;
            return false;
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (UnityEngine.Input.touchCount > 0)
            {
                var touch = UnityEngine.Input.GetTouch(0);
                screenPos = touch.position;
                pressedThisFrame = touch.phase == UnityEngine.TouchPhase.Began;
                return true;
            }

            screenPos = UnityEngine.Input.mousePosition;
            pressedThisFrame = UnityEngine.Input.GetMouseButtonDown(0);
            return true;
#else
            screenPos = default;
            pressedThisFrame = false;
            return false;
#endif
        }
    }
}
