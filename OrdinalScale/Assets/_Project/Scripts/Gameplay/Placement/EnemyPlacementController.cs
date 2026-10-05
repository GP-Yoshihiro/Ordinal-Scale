using System;
using OrdinalScale.Core.Spatial;
using OrdinalScale.Platform;
using UnityEngine;

namespace OrdinalScale.Gameplay.Placement
{
    /// <summary>
    /// S2：検出した水平面をタップ（Editor ではクリック）して敵を1体置く。
    /// 置けるかどうかは Core の EnemyPlacementSession（内部で PlacementGate）が決め、
    /// 抑止したときは敵を置かず・動かさず、理由を Attempted イベントとログで知らせる。
    /// デバイス差は PlatformRig の IARSpatialProvider / IPointerInput で吸収するので、Editor と iPhone で同じ処理が動く。
    /// </summary>
    public sealed class EnemyPlacementController : MonoBehaviour
    {
        [SerializeField] private PlatformRig rig;
        [Tooltip("敵の見た目。未設定時はローカルのDemonLord2を使い、見つからなければ仮モデルを作る。")]
        [SerializeField] private GameObject enemyPrefab = null;
        [Tooltip("タップ位置からこの距離（m）より遠い面には置かない。")]
        [SerializeField] private float maxPlacementDistance = 8f;
        [SerializeField] private Color placeholderBodyColor = new Color(1f, 0.45f, 0.1f);
        [SerializeField] private Color placeholderFaceColor = new Color(0.1f, 0.9f, 1f);

        private readonly EnemyPlacementSession _session = new EnemyPlacementSession();
        private GameObject _enemy;

        /// <summary>配置操作のたびに発火する（配置・移動・抑止のすべて）。</summary>
        public event Action<PlacementAttemptResult> Attempted;

        public EnemyPlacementSession Session => _session;

        /// <summary>配置済みの敵。未配置なら null。</summary>
        public GameObject Enemy => _session.HasEnemy ? _enemy : null;

        private void Reset()
        {
            rig = FindAnyObjectByType<PlatformRig>();
        }

        private void Awake()
        {
            if (rig == null) rig = FindAnyObjectByType<PlatformRig>();
            if (rig == null) Debug.LogError($"[{nameof(EnemyPlacementController)}] シーンに PlatformRig がありません。", this);
        }

        private void Update()
        {
            if (rig == null) return;

            var pointer = rig.Pointer;
            var spatial = rig.Spatial;
            if (pointer == null || spatial == null) return;
            if (!pointer.TryConsumeSelect(out var ray)) return;

            HandleSelect(spatial, ray);
        }

        /// <summary>敵を取り除く（検証のやり直し用）。次の有効なタップで新しく置かれる。</summary>
        public void RemoveEnemy()
        {
            _session.RemoveEnemy();
            if (_enemy != null) _enemy.SetActive(false);
        }

        private void HandleSelect(IARSpatialProvider spatial, Ray ray)
        {
            // 状態はタップした瞬間のものを使う（後から追跡が戻っても、その操作は抑止のまま）
            var status = spatial.Status;
            var hasHit = spatial.TryRaycastEnvironment(ray, maxPlacementDistance, out var hit);
            var result = _session.Attempt(status, hasHit ? hit.Surface : SurfaceKind.None);

            if (!result.IsBlocked) PlaceEnemyAt(hit.Pose.position, spatial.HeadPose.position);

            // Xcode のコンソールにも出るので、実機での証拠（操作番号・結果・理由・状態）として残せる
            Debug.Log($"[OrdinalScale][S2] {PlacementMessages.Describe(result)} | camera={status.CameraPermission} " +
                      $"tracking={status.TrackingPhase} planes={status.HorizontalPlaneCount} " +
                      $"hit={(hasHit ? hit.Surface.ToString() : "none")} dist={(hasHit ? hit.Distance : -1f):0.00}", this);

            Attempted?.Invoke(result);
        }

        private void PlaceEnemyAt(Vector3 position, Vector3 headPosition)
        {
            if (_enemy == null)
            {
                // 公開リポジトリへ再配布できないモデルは、ローカルの Resources に置いてビルドへ含める。
                var prefab = enemyPrefab != null ? enemyPrefab : Resources.Load<GameObject>("DemonLord2");
                _enemy = prefab != null
                    ? Instantiate(prefab)
                    : EnemyPlaceholder.Create(placeholderBodyColor, placeholderFaceColor);

                // 敵自身が配置先の面として拾われないようにする（Editor の物理レイキャスト対策）。
                // 剣の当たり判定を作る段階で専用レイヤーへ移す
                EnemyPlaceholder.SetLayerRecursively(_enemy, EnemyPlaceholder.IgnoreRaycastLayer);
            }

            // 敵はプレイヤーの方を向いて立つ（体は常に直立。面の傾きには合わせない）
            var yaw = PlacementMath.TryYawDegreesToFace(position.x, position.z, headPosition.x, headPosition.z, out var facing)
                ? facing
                : _enemy.transform.eulerAngles.y;

            _enemy.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            _enemy.SetActive(true);
        }
    }
}
