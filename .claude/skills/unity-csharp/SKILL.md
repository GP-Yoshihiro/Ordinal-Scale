---
name: unity-csharp
description: Ordinal-Scale の Unity C# コードを書く・直す・レビューするときの規約。アセンブリ分割（Core/Platform/Gameplay）、XR SDK の抽象化、命名、テストの置き場所、ARグラス向けのパフォーマンス注意点。C# スクリプトや asmdef を追加・変更するときは必ず参照する。
---

# Unity C# 規約（Ordinal-Scale）

## 配置とアセンブリ

```
OrdinalScale/Assets/_Project/
  Scripts/Core/        OrdinalScale.Core        純C#。noEngineReferences=true
  Scripts/Platform/    OrdinalScale.Platform    Abstractions/ にインターフェース、デバイス別実装はサブフォルダ
    Abstractions/        IARSpatialProvider, IInputController, AttackInput
    EditorSim/           Editor 用シミュレータ実装
    MetaQuest/  (予定)   Meta XR SDK 実装。asmdef を分け、defineConstraints で SDK 有無を切り替える
    Xreal/      (予定)   NRSDK 実装。同上
  Scripts/Gameplay/    OrdinalScale.Gameplay（予定） 敵・攻撃・HP表示などの MonoBehaviour
  Scripts/UI/          SAO風HUD
  Scripts/Bootstrap/   シーン起動処理
  Tests/EditMode/Core/ Core の NUnit テスト（tools/CoreTests からも実行される）
```

- 新しいスクリプトは既存 asmdef の配下に置く。新 asmdef を作るのは SDK 依存を隔離するときだけ。
- asmdef の参照は GUID ではなく **アセンブリ名** で書く（クラウドで .meta が作れないため）。
- 任意パッケージへの依存は `versionDefines`（例: `OS_HAS_INPUT_SYSTEM`）で囲み、未導入でもコンパイルが通るようにする。

## 抽象化の原則

- Gameplay は `PlatformRig` から `IARSpatialProvider` / `IInputController` を受け取り、具体クラスを知らない。
- デバイス差は「照準レイ＋攻撃の瞬間」「頭部姿勢」「床・環境へのレイキャスト」に正規化する。新しい能力（手の位置、平面検出など）が必要ならインターフェースを増やし、Editor 実装も同時に用意する（Editor で再現できない機能を作らない）。
- ゲームルール（HP・ダメージ・クールダウン・状態遷移）は Core の純C#クラスに置き、MonoBehaviour はそれを保持してイベントで見た目を更新するだけにする。

## 書き方

- 名前空間: `OrdinalScale.<層>[.<機能>]`（例: `OrdinalScale.Core.Combat`）
- `[SerializeField] private` フィールドは camelCase、それ以外の private フィールドは `_camelCase`、公開はプロパティ。
- `Reset()` / `Awake()` で `Camera.main` 等の既定値を補う。`Awake` で自身の初期化、`Start` で他オブジェクト参照、購読は `OnEnable`/`OnDisable` で対にする。
- C# 9 まで（record 型、file-scoped namespace、global using は不可）。
- コメントは日本語で「なぜそうするか」を書く。

## パフォーマンス（ARグラス前提）

- `Update` 内で `new`（参照型）、LINQ、`foreach` over interface、文字列連結、`GetComponent`、`Camera.main` の毎回呼び出しをしない。
- 物理レイキャストは `LayerMask` を必ず指定し、`QueryTriggerInteraction` を明示する。

## 検証

```bash
dotnet test tools/CoreTests
dotnet format whitespace tools/CoreTests/CoreTests.csproj --verify-no-changes
```

Unity 依存コードはクラウドでコンパイルできない。変更したら「Editor で確認すること」を報告に必ず書く。
