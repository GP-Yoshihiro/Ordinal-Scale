using OrdinalScale.Core.Combat;
using UnityEngine;

namespace OrdinalScale.Gameplay.Combat
{
    /// <summary>
    /// 敵の体の当たり判定（縦のカプセル1つ。条件 C6：部位差なし）。原点は敵の足元。
    /// 剣の判定は物理エンジンを使わず、この寸法から Core の BodyCapsule を作って計算する。
    /// 仮モデルは EnemyPlaceholder と同じ寸法（高さ1.6m・半径0.25m）。Fab モデルは表示範囲から概算する（仮。Inspector で調整可）。
    /// Scene ビューでは黄色の線でカプセルを表示する。
    /// </summary>
    public sealed class EnemyHitVolume : MonoBehaviour
    {
        [Tooltip("全高（m）。半球部分を含む。")]
        [SerializeField, Min(0.1f)] private float height = 1.6f;
        [Tooltip("半径（m）。")]
        [SerializeField, Min(0.05f)] private float radius = 0.25f;

        public float Height => height;
        public float Radius => radius;

        public void Configure(float bodyHeight, float bodyRadius)
        {
            radius = Mathf.Max(0.05f, bodyRadius);
            height = Mathf.Max(bodyHeight, radius * 2f);
        }

        /// <summary>現在の位置（足元）でのカプセル。回転・拡縮は使わない（縦のカプセルなので向きに依存しない）。</summary>
        public BodyCapsule ToCapsule()
        {
            var p = transform.position;
            var r = Mathf.Max(0.05f, radius);
            return new BodyCapsule(p.x, p.y, p.z, Mathf.Max(height, r * 2f), r);
        }

        /// <summary>
        /// モデルの表示範囲（Renderer の境界）から寸法を概算する。足元原点・直立を前提にする。
        /// 腕を広げた姿勢などで幅が過大にならないよう、半径は 0.15〜0.5m に収める（仮）。
        /// </summary>
        public static bool TryEstimate(GameObject model, out float bodyHeight, out float bodyRadius)
        {
            bodyHeight = 0f;
            bodyRadius = 0f;
            var renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return false;

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

            var basePosition = model.transform.position;
            bodyHeight = bounds.max.y - basePosition.y;
            bodyRadius = Mathf.Clamp(Mathf.Min(bounds.size.x, bounds.size.z) * 0.5f, 0.15f, 0.5f);
            return bodyHeight > 0.1f;
        }

        private void OnDrawGizmos()
        {
            var p = transform.position;
            var r = Mathf.Max(0.05f, radius);
            var bottom = p + Vector3.up * r;
            var top = p + Vector3.up * (Mathf.Max(height, r * 2f) - r);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(bottom, r);
            Gizmos.DrawWireSphere(top, r);
            Gizmos.DrawLine(bottom + Vector3.right * r, top + Vector3.right * r);
            Gizmos.DrawLine(bottom - Vector3.right * r, top - Vector3.right * r);
            Gizmos.DrawLine(bottom + Vector3.forward * r, top + Vector3.forward * r);
            Gizmos.DrawLine(bottom - Vector3.forward * r, top - Vector3.forward * r);
        }
    }
}
