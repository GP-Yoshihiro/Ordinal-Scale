using UnityEngine;

namespace OrdinalScale.Gameplay.Placement
{
    /// <summary>
    /// 敵の仮モデルを実行時に組み立てる。プレハブやマテリアルのアセットを増やさずに
    /// Editor・iPhone・Quest で同じ見た目を使うため（S4 で表示設定を調整できる形に置き換える予定）。
    /// 原点は足元。体の当たり判定はカプセル1つだけ（剣の命中条件 C6：体全体が同じ当たり判定）。
    /// </summary>
    public static class EnemyPlaceholder
    {
        public const float BodyHeight = 1.6f;
        public const float BodyDiameter = 0.5f;

        /// <summary>Unity 組み込みの「Ignore Raycast」レイヤー番号。</summary>
        public const int IgnoreRaycastLayer = 2;

        public static GameObject Create(Color bodyColor, Color faceColor)
        {
            var root = new GameObject("EnemyPlaceholder");

            // Unity のカプセルは高さ2m・直径1m。足元原点になるよう中心を半分の高さに上げる
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, BodyHeight * 0.5f, 0f);
            body.transform.localScale = new Vector3(BodyDiameter, BodyHeight * 0.5f, BodyDiameter);
            SetColor(body, bodyColor);

            // 正面（+Z）の目の高さに目印を付け、敵がプレイヤーの方を向いているか画面で確かめられるようにする
            var face = GameObject.CreatePrimitive(PrimitiveType.Cube);
            face.name = "FaceMarker";
            face.transform.SetParent(root.transform, false);
            face.transform.localPosition = new Vector3(0f, BodyHeight * 0.85f, BodyDiameter * 0.5f);
            face.transform.localScale = new Vector3(0.25f, 0.08f, 0.1f);
            Object.Destroy(face.GetComponent<Collider>());
            SetColor(face, faceColor);

            return root;
        }

        public static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            var t = go.transform;
            for (var i = 0; i < t.childCount; i++) SetLayerRecursively(t.GetChild(i).gameObject, layer);
        }

        private static void SetColor(GameObject go, Color color)
        {
            // 描画パイプライン既定のマテリアル（URP では Lit）を複製して色だけ変える
            var r = go.GetComponent<Renderer>();
            if (r != null) r.material.color = color;
        }
    }
}
