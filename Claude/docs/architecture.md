# Unityプロジェクト ベース設計

## 設計の狙い

1. **同じゲームロジックを Editor / Quest / XREAL で動かす。** デバイス固有のSDKは Platform 層に閉じ込め、ゲーム側はインターフェースだけを見る。
2. **実機がない週でも前に進める。** すべての抽象に Editor 用の実装を用意し、マウス＋キーボードで同じ流れを再現できるようにする。
3. **ゲームルールはUnityなしでテストする。** HP・ダメージ・状態遷移は純C#の Core 層に置き、`dotnet test` と Unity Test Runner の両方で同じテストを回す。

## レイヤー構成

```
┌──────────────────────────────────────────────┐
│ UI / Gameplay（MonoBehaviour）                │  敵の表示・命中反応・HPバー・再開始
│   OrdinalScale.Gameplay                       │
└──────────┬──────────────────────┬────────────┘
           │ 使う                  │ 使う（インターフェースのみ）
┌──────────▼──────────┐  ┌────────▼──────────────────────────────┐
│ Core（純C#）         │  │ Platform                               │
│ OrdinalScale.Core    │  │  Abstractions: IARSpatialProvider,     │
│  Combat/Health       │  │                IInputController        │
│  （今後: 敵の状態遷移、│  │  EditorSim / MetaQuest(予定) / Xreal(予定) │
│   ダメージ計算など）  │  │  PlatformRig（実装の解決窓口）          │
└──────────────────────┘  └───────────────────────────────────────┘
```

依存の向きは上から下のみ。Core は何にも依存しない。

## フォルダ構成

```
OrdinalScale/                       Unityプロジェクトのルート
  Assets/
    _Project/                       自作物はすべてここ（サードパーティと混ぜない）
      Scripts/
        Core/                       純C#（UnityEngine禁止）
        Platform/
          Abstractions/             デバイス抽象インターフェース
          EditorSim/                Editor用シミュレータ実装
          MetaQuest/                （Quest実機週に追加）
          Xreal/                    （STEP 2で追加）
        Gameplay/                   敵・攻撃・ゲーム進行
        UI/                         SAO風HUD
        Bootstrap/                  シーン起動・デバイス別リグの生成
      Tests/EditMode/Core/          Core のNUnitテスト
      Scenes/  Prefabs/  Materials/  Shaders/  Art/
      Settings/                     URPアセット等
    ThirdParty/                     Asset Store等（導入時に作成）
  Packages/  ProjectSettings/       Unityが生成（コミットする）
tools/CoreTests/                    Unityなしで Core をテストする .NET プロジェクト
```

## 抽象化レイヤー

### `IARSpatialProvider` — 空間認識

| メンバー | 役割 | Editor | Quest（予定） | XREAL（予定） |
| --- | --- | --- | --- | --- |
| `IsReady` | 配置してよいか | カメラがあれば true | MRUK のシーン読込完了 | NRSDK トラッキング開始 |
| `HeadPose` | 頭部姿勢 | Main Camera | CenterEyeAnchor | NRSDK のヘッド姿勢 |
| `TryGetFloorHeight` | 床の高さ | 固定値（0m） | MRUK の床アンカー | 平面検出 |
| `TryRaycastEnvironment` | 現実環境へのレイ | シーンのコライダー | MRUK の部屋メッシュ | 平面/メッシュ |

### `IInputController` — 攻撃入力

デバイスごとの入力を **「照準レイ」＋「攻撃の瞬間」** に正規化する。

| デバイス | 照準レイ | 攻撃の瞬間 |
| --- | --- | --- |
| Editor | マウス位置からのカメラレイ | 左クリック |
| Quest（予定） | 手のポインターポーズ / コントローラ | ピンチ / トリガー |
| XREAL（予定） | 頭部レイ or スマホコントローラ | タップ |

ゲーム側は `TryConsumeAttack` を Update で1回ポーリングする。イベント方式にしないのは、処理順を Gameplay 側で固定し、テストや再現をしやすくするため。

### `PlatformRig` — 実装の切り替え

デバイスごとに「リグ」プレハブ（`PlatformRig_Editor` / `PlatformRig_Quest` / `PlatformRig_Xreal`）を作り、子に各プロバイダ実装を置く。シーンには1つだけ置き、ゲーム側は `PlatformRig.Spatial` / `PlatformRig.Input` を使う。将来はビルドターゲットに応じて Bootstrap がリグを生成する。

## STEP 1 最小デモの流れ（WBS 3〜5 で実装）

```
EditorInputController ──AttackInput(ray)──▶ AttackController（Gameplay）
                                              │ Physics.Raycast（敵レイヤー）
                                              ▼
                                        EnemyHitbox → Enemy（Health を保持）
                                              │ Health.ApplyDamage()
                              ┌───────────────┼──────────────────┐
                        Damaged イベント    Died イベント      Revived イベント
                              ▼               ▼                  ▼
                      命中フラッシュ/HPバー  撃破演出・再開始待ち   再配置・表示復帰
```

予定クラス（Gameplay）：`EnemySpawner`（IARSpatialProvider から配置位置を決める）、`Enemy`（Core の `Health` を保持）、`EnemyView`（命中反応・HPバー）、`AttackController`（入力→レイキャスト→ダメージ）、`BattleLoop`（撃破→再開始）。

## SAO風UIの方針（STEP 3 で本格化、今から守ること）

- 光学シースルーのARグラスでは黒は透明になる。UIは明るい色＋加算合成＋太めの輪郭で作る。
- FOV 50°前後を想定し、重要情報は視野中心から±20°以内、HPバー等は上端寄りに置く。Quest（FOV約100°）で見やすくてもグラスでははみ出すので、UIは頭部固定ではなく「視線前方に遅れて追従」させる設計にする。
