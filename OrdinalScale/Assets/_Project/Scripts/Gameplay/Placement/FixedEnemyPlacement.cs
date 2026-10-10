using System;
using OrdinalScale.Core.Spatial;
using OrdinalScale.Gameplay.Combat;
using OrdinalScale.Platform;
using UnityEngine;

namespace OrdinalScale.Gameplay.Placement
{
    /// <summary>
    /// Quest Q0：追跡が安定したら、プレイヤーの正面 distance(m) の床に敵を1体置く（タップ操作なし）。
    /// 部屋のスキャンを使わない最小の配置方法で、Space Setup・シーン権限なしで Q-1（パススルー越しに敵1体）を確かめられる。
    /// 床は IARSpatialProvider.TryGetFloorHeight（Quest では追跡原点 Floor）、向きはプレイヤーの方。
    /// 置き直しは Reposition()（Q2 の再挑戦から呼ぶ想定）。
    /// </summary>
    public sealed class FixedEnemyPlacement : MonoBehaviour
    {
        [SerializeField] private PlatformRig rig;
        [Tooltip("敵の見た目。未設定時はローカルのDemonLord2を使い、見つからなければ仮モデルを作る。")]
        [SerializeField] private GameObject enemyPrefab = null;
        [Tooltip("正面のこの距離（m）に置く。剣（約0.9m）が届かず、歩いて近づける距離にする。実機で安全範囲に合わせて調整する。")]
        [SerializeField, Range(1f, 4f)] private float distance = 2f;
        [Tooltip("追跡が安定してから置くまでの待ち時間（秒）。起動直後の頭の位置で置かないため。")]
        [SerializeField] private float settleSeconds = 1f;
        [SerializeField] private Color placeholderBodyColor = new Color(1f, 0.45f, 0.1f);
        [SerializeField] private Color placeholderFaceColor = new Color(0.1f, 0.9f, 1f);

        private GameObject _enemy;
        private bool _placed;
        private float _readySince = -1f;

        /// <summary>敵を置いた（置き直した）とき。引数は足元の位置。</summary>
        public event Action<Vector3> Placed;

        /// <summary>配置済みの敵。未配置なら null。</summary>
        public GameObject Enemy => _placed ? _enemy : null;

        private void Reset()
        {
            rig = FindAnyObjectByType<PlatformRig>();
        }

        private void Awake()
        {
            if (rig == null) rig = FindAnyObjectByType<PlatformRig>();
            if (rig == null) Debug.LogError($"[{nameof(FixedEnemyPlacement)}] シーンに PlatformRig がありません。", this);
        }

        private void Update()
        {
            if (_placed || rig == null || rig.Spatial == null) return;

            var spatial = rig.Spatial;
            if (!spatial.IsReady)
            {
                _readySince = -1f;
                return;
            }

            if (_readySince < 0f) _readySince = Time.time;
            if (Time.time - _readySince < settleSeconds) return;

            TryPlace(spatial);
        }

        /// <summary>次の安定したフレームで、その時の正面に置き直す。</summary>
        public void Reposition()
        {
            _placed = false;
            _readySince = -1f;
        }

        private void TryPlace(IARSpatialProvider spatial)
        {
            if (!spatial.TryGetFloorHeight(out var floorY)) return;

            var head = spatial.HeadPose;
            var forward = head.rotation * Vector3.forward;
            // 真下・真上を見ている間は正面が決まらないので、次のフレームで再試行する
            if (!PlacementMath.TryPointInFront(head.position.x, head.position.z, forward.x, forward.z, distance,
                    out var x, out var z)) return;

            var position = new Vector3(x, floorY, z);
            var yaw = PlacementMath.TryYawDegreesToFace(x, z, head.position.x, head.position.z, out var facing) ? facing : 0f;

            if (_enemy == null) _enemy = CreateEnemy();
            _enemy.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            _enemy.SetActive(true);
            _placed = true;

            Debug.Log($"[OrdinalScale][Q0] 敵を正面に配置: dist={distance:0.00}m floorY={floorY:0.00} " +
                      $"head=({head.position.x:0.00},{head.position.y:0.00},{head.position.z:0.00}) model={_enemy.name}", this);
            Placed?.Invoke(position);
        }

        private GameObject CreateEnemy()
        {
            // 公開リポジトリへ再配布できないモデルはローカルの Resources にだけ置く（無いクローンでは仮モデル）
            var prefab = enemyPrefab != null ? enemyPrefab : Resources.Load<GameObject>("DemonLord2");
            var enemy = prefab != null
                ? Instantiate(prefab)
                : EnemyPlaceholder.Create(placeholderBodyColor, placeholderFaceColor);

            // iPhone の配置と同じく、敵自身が環境へのレイの対象にならないようにする。
            // 剣の判定は物理レイヤーを使わず EnemyHitVolume の寸法で計算するので、レイヤーは剣に影響しない
            EnemyPlaceholder.SetLayerRecursively(enemy, EnemyPlaceholder.IgnoreRaycastLayer);

            // 剣の当たり判定（体全体で1つのカプセル）。仮モデルは寸法が分かっているのでそのまま、Fab モデルは表示範囲から概算（仮）
            var volume = enemy.GetComponent<EnemyHitVolume>();
            if (volume == null) volume = enemy.AddComponent<EnemyHitVolume>();
            if (prefab == null)
            {
                volume.Configure(EnemyPlaceholder.BodyHeight, EnemyPlaceholder.BodyDiameter * 0.5f);
            }
            else if (EnemyHitVolume.TryEstimate(enemy, out var h, out var r))
            {
                volume.Configure(h, r);
            }

            Debug.Log($"[OrdinalScale][Q1] 敵の当たり判定: 高さ{volume.Height:0.00}m 半径{volume.Radius:0.00}m（{(prefab == null ? "仮モデル" : "モデルの表示範囲から概算・仮")}）", this);
            return enemy;
        }
    }
}
