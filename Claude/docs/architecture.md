# Unityプロジェクト ベース設計

## 設計の狙い

1. **同じゲームロジックを Editor / iPhone / Quest / XREAL で動かす。** デバイス固有のSDKは Platform 層に閉じ込め、ゲーム側はインターフェースだけを見る。
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
│  Spatial/            │  │  EditorSim / ARFoundation(iPhone) /    │
│   PlacementGate      │  │  MetaQuest(予定) / Xreal(予定)          │
│  （今後: 振り判定等） │  │  PlatformRig（実装の解決窓口）          │
└──────────────────────┘  └───────────────────────────────────────┘

┌──────────────────────────────────────────────┐
│ Editor ツール（OrdinalScale.Editor）          │  iOS 設定の適用・検証・ビルド（Editor専用）
└──────────────────────────────────────────────┘
```

依存の向きは上から下のみ。Core は何にも依存しない。

| アセンブリ | 置き場所 | コンパイルされる条件 |
| --- | --- | --- |
| `OrdinalScale.Core` | `Scripts/Core` | 常に（UnityEngine 参照なし） |
| `OrdinalScale.Platform` | `Scripts/Platform`（Abstractions / EditorSim / PlatformRig） | 常に |
| `OrdinalScale.Platform.ARFoundation` | `Scripts/Platform/ARFoundation` | AR Foundation 導入時のみ（`OS_HAS_ARFOUNDATION`） |
| `OrdinalScale.Editor` | `Editor/` | Editor のみ |

Unity 版は `6000.5.10f1` に固定（`ProjectSettings/ProjectVersion.txt`）。

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
          ARFoundation/             AR Foundation 実装（iPhone。Quest で使えるかは要検証）
          MetaQuest/                （Quest実機週に追加）
          Xreal/                    （STEP 2で追加）
        Gameplay/                   敵・攻撃・ゲーム進行
        UI/                         SAO風HUD
        Bootstrap/                  シーン起動・デバイス別リグの生成
      Editor/                       iOS設定の適用・検証・ビルドのメニュー
      Tests/EditMode/Core/          Core のNUnitテスト
      Scenes/  Prefabs/  Materials/  Shaders/  Art/
      Settings/                     URPアセット等
    ThirdParty/                     Asset Store等（導入時に作成）
  Packages/  ProjectSettings/       Unityが生成（コミットする）
tools/CoreTests/                    Unityなしで Core をテストする .NET プロジェクト
```

## 抽象化レイヤー

### `IARSpatialProvider` — 空間認識

| メンバー | 役割 | Editor | iPhone（AR Foundation＋ARKit） | Quest（予定） | XREAL（予定） |
| --- | --- | --- | --- | --- | --- |
| `IsReady` | 追跡が正常か | カメラがあれば true | `ARSession.state == SessionTracking` | MRUK または AR Foundation | NRSDK トラッキング開始 |
| `Status` | 置けない理由の元データ（許可・追跡段階・水平面数） | 常に許可済み・水平面1枚 | カメラ許可の問い合わせ結果＋セッション状態＋検出平面 | 同左 | 同左 |
| `HeadPose` | 頭部姿勢 | Main Camera | XR Origin のカメラ | CenterEyeAnchor | NRSDK のヘッド姿勢 |
| `TryGetFloorHeight` | 床の高さ | 固定値（0m） | 最も低い上向き水平面 | MRUK の床アンカー | 平面検出 |
| `TryRaycastEnvironment` | 現実環境へのレイ | シーンのコライダー | 検出平面の境界内へのレイキャスト | MRUK の部屋メッシュ | 平面/メッシュ |

`Status` は Core の `PlacementGate` に渡し、「権限 → 対応端末 → 追跡 → 平面 → 選んだ位置」の順で配置できない理由を1つに決める（iPhone 実行計画の失敗条件表に対応）。

**Quest で AR Foundation を使う選択肢**：Unity OpenXR: Meta パッケージは Quest 3 向けに AR Foundation の平面・レイキャスト・アンカーを提供している。これを採ると iPhone の `ARFoundationSpatialProvider` と配置処理を Quest でも使い回せる。Meta XR SDK（MRUK）を使う案との比較は、11月18日より前に机上で行い、実機で確定する（未確認）。

### `IInputController` — 攻撃入力（**戦闘には使わない。改名予定**）

> 剣の命中条件は確定済み（振っている最中に体に触れた時だけ、押し当ては不命中、1振り1命中、体全体が同じ当たり判定、Editor はマウスドラッグで代替）。
> このインターフェースでは表現できないため、戦闘の命中は新設する `ISwordPoseSource` と Core の `SwingDetector`／`SwordHitJudge` で判定する。
> 本インターフェースは「指す・選ぶ」用の `IPointerInput` に改名し（S2 の配置タップ実装時）、メニュー操作や配置にだけ使う。
> 詳細と条件番号 C1〜C8 は [sword-input-design.md](sword-input-design.md)。

デバイスごとの入力を **「照準レイ」＋「攻撃の瞬間」** に正規化する。

| デバイス | 照準レイ | 攻撃の瞬間 |
| --- | --- | --- |
| Editor | マウス位置からのカメラレイ | 左クリック |
| Quest（予定） | 手のポインターポーズ / コントローラ | ピンチ / トリガー |
| XREAL（予定） | 頭部レイ or スマホコントローラ | タップ |

ゲーム側は `TryConsumeAttack` を Update で1回ポーリングする。イベント方式にしないのは、処理順を Gameplay 側で固定し、テストや再現をしやすくするため。

### `PlatformRig` — 実装の切り替え

デバイスごとに「リグ」（`PlatformRig_Editor` / `PlatformRig_iPhone` / `PlatformRig_Quest` / `PlatformRig_Xreal`）を作り、子に各プロバイダ実装を置く。シーンには1つだけ置き、ゲーム側は `PlatformRig.Spatial` / `PlatformRig.Input` を使う。将来はビルドターゲットに応じて Bootstrap がリグを生成する。

## STEP 1 最小デモの流れ（旧WBS 3〜5。攻撃部分は剣の設計で置き換える予定）

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
