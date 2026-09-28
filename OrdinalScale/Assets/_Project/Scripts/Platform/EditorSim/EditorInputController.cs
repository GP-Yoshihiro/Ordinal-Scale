using UnityEngine;
#if OS_HAS_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace OrdinalScale.Platform.EditorSim
{
    /// <summary>
    /// Editor 用の攻撃入力。マウス位置からカメラのレイを飛ばし、左クリックで攻撃する。
    /// Quest ではハンド/コントローラの照準レイに置き換わるため、ゲーム側は「レイ＋攻撃の瞬間」だけに依存させる。
    /// Input System パッケージ（推奨）と旧 Input Manager の両方に対応。
    /// </summary>
    public sealed class EditorInputController : MonoBehaviour, IInputController
    {
        [SerializeField] private Camera aimCamera;

        private bool _attackPending;
        private Ray _aimRay;

        public Ray AimRay => _aimRay;

        private void Reset()
        {
            aimCamera = Camera.main;
        }

        private void Awake()
        {
            if (aimCamera == null) aimCamera = Camera.main;
        }

        private void Update()
        {
            if (aimCamera == null) return;

            if (!TryReadPointer(out var screenPos, out var pressedThisFrame)) return;

            _aimRay = aimCamera.ScreenPointToRay(screenPos);
            if (pressedThisFrame) _attackPending = true;
        }

        public bool TryConsumeAttack(out AttackInput attack)
        {
            if (!_attackPending)
            {
                attack = default;
                return false;
            }

            _attackPending = false;
            attack = new AttackInput(AttackKind.Shot, _aimRay);
            return true;
        }

        private static bool TryReadPointer(out Vector2 screenPos, out bool pressedThisFrame)
        {
#if OS_HAS_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
            var mouse = Mouse.current;
            if (mouse == null)
            {
                screenPos = default;
                pressedThisFrame = false;
                return false;
            }

            screenPos = mouse.position.ReadValue();
            pressedThisFrame = mouse.leftButton.wasPressedThisFrame;
            return true;
#elif ENABLE_LEGACY_INPUT_MANAGER
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
