using OrdinalScale.Platform;
using UnityEngine;

namespace OrdinalScale.Gameplay.Placement
{
    /// <summary>iPhone先行検証用。配置したFabモデルの待機・攻撃アニメーションを確認する。</summary>
    public sealed class EnemyAnimationPreview : MonoBehaviour
    {
        [SerializeField] private EnemyPlacementController placement;
        [SerializeField] private string idleState = "Base Layer.Idle1";
        [SerializeField] private string attackState = "Base Layer.Attack1";

        private GameObject _observedEnemy;
        private Animator _animator;
        private GUIStyle _buttonStyle;
        private ScreenPointerInput _screenPointer;

        private void Awake()
        {
            if (placement == null) placement = FindAnyObjectByType<EnemyPlacementController>();
            _screenPointer = FindAnyObjectByType<ScreenPointerInput>();
            if (_screenPointer != null)
                _screenPointer.AddSelectionExclusion(IsAttackButtonPointer);
        }

        private void OnDestroy()
        {
            if (_screenPointer != null)
                _screenPointer.RemoveSelectionExclusion(IsAttackButtonPointer);
        }

        private void Update()
        {
            var enemy = placement != null ? placement.Enemy : null;
            if (enemy != _observedEnemy)
            {
                _observedEnemy = enemy;
                _animator = enemy != null ? enemy.GetComponentInChildren<Animator>(true) : null;
                if (_animator != null)
                {
                    // Controllerの初期状態に依存せず、配置直後は待機を表示する。
                    _animator.applyRootMotion = false;
                    _animator.Play(idleState, 0, 0f);
                    Debug.Log($"[OrdinalScale][S3] モデル表示・待機再生: {enemy.name}", this);
                }
            }

            if (_animator == null || !_animator.isActiveAndEnabled) return;
            var state = _animator.GetCurrentAnimatorStateInfo(0);
            if ((state.IsName(idleState) || state.IsName(attackState)) && state.normalizedTime >= 1f)
            {
                // クリップのLoop設定に依存せず待機へ戻す。
                _animator.Play(idleState, 0, 0f);
            }
        }

        private void OnGUI()
        {
            if (_animator == null || !_animator.isActiveAndEnabled) return;
            if (_buttonStyle == null)
            {
                _buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 16 };
            }

            var scale = PreviewScale();
            var saved = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            var screenRect = AttackButtonScreenRect(scale);
            var guiRect = new Rect(screenRect.x / scale,
                (Screen.height - screenRect.yMax) / scale,
                screenRect.width / scale, screenRect.height / scale);
            if (GUI.Button(guiRect, "Attack preview", _buttonStyle))
            {
                _animator.Play(attackState, 0, 0f);
                Debug.Log($"[OrdinalScale][S3] 攻撃アニメ再生: {attackState}", this);
            }
            GUI.matrix = saved;
        }

        private bool IsAttackButtonPointer(Vector2 screenPosition)
        {
            return _animator != null && _animator.isActiveAndEnabled &&
                   AttackButtonScreenRect(PreviewScale()).Contains(screenPosition);
        }

        private static float PreviewScale()
        {
            return Mathf.Max(1f, Screen.dpi > 0f ? Screen.dpi / 160f : Screen.height / 800f);
        }

        private static Rect AttackButtonScreenRect(float scale)
        {
            var safe = Screen.safeArea;
            return new Rect(safe.xMax - 148f * scale, safe.yMin + 8f * scale,
                140f * scale, 46f * scale);
        }
    }
}
