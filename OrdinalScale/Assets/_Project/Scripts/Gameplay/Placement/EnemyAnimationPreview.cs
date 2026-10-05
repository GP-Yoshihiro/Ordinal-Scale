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

        private void Awake()
        {
            if (placement == null) placement = FindAnyObjectByType<EnemyPlacementController>();
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
                    // 攻撃が初期状態の配布コントローラでも、配置直後は待機を表示する。
                    _animator.applyRootMotion = false;
                    _animator.Play(idleState, 0, 0f);
                    Debug.Log($"[OrdinalScale][S3] モデル表示・待機再生: {enemy.name}", this);
                }
            }

            if (_animator == null || !_animator.isActiveAndEnabled) return;
            var state = _animator.GetCurrentAnimatorStateInfo(0);
            if ((state.IsName(idleState) || state.IsName(attackState)) && state.normalizedTime >= 1f)
            {
                // 配布クリップのLoop設定に依存せず待機へ戻す。
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

            var scale = Mathf.Max(1f, Screen.dpi > 0f ? Screen.dpi / 160f : Screen.height / 800f);
            var saved = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            var safe = Screen.safeArea;
            const float width = 140f;
            const float height = 46f;
            var x = safe.xMax / scale - width - 8f;
            var y = (Screen.height - safe.yMin) / scale - height - 8f;
            if (GUI.Button(new Rect(x, y, width, height), "Attack preview", _buttonStyle))
            {
                _animator.Play(attackState, 0, 0f);
                Debug.Log($"[OrdinalScale][S3] 攻撃アニメ再生: {attackState}", this);
            }
            GUI.matrix = saved;
        }
    }
}
