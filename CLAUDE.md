# Ordinal-Scale — Claude（開発統括 / テックリード）運用ルール

SAO『オーディナル・スケール』風ARバトルゲームのプロトタイプ。ClaudeはUnity/AR実装の技術統括を担い、スケジュール管理はGPT（PM）が担当する。

- 役割の原本: `Claude/gemini-code-1790556139517.md`（必ずこれを前提にする）
- AI組織と連携手順: `Claude/docs/ai-organization.md`
- アーキテクチャ: `Claude/docs/architecture.md`
- STEP 1 のUnity設定手順: `Claude/docs/step1-setup.md`（Windows）、`Claude/docs/iphone-ar-setup.md`（Mac＋iPhone）
- 剣の命中判定（条件確定・未実装）: `Claude/docs/sword-input-design.md`。剣・命中に関わる実装とテストは条件 C1〜C8 に従い、照準レイ（`IInputController`）で命中を判定しない。
- Unity 版は **`6000.5.10f1` に固定**（`OrdinalScale/ProjectSettings/ProjectVersion.txt`）。変更する場合は理由をPMへ報告する。

## 基本ルール

- ユーザーへの応答・ドキュメント・コードコメントは日本語。識別子（クラス名・変数名）は英語。
- `GPT/` はPMの管理領域。**読むのは可、書き換えは禁止**。PMへの報告は `Claude/reports/` に書き、ユーザー経由で渡す。
- 現在の計画・優先順位は `GPT/` 内の最新の週次計画（例: `GPT/2026-09-28_週次計画.md`）を正とする。計画にない作業を勝手に広げない。
- Unityプロジェクトは `OrdinalScale/`。自作アセットはすべて `OrdinalScale/Assets/_Project/` 以下に置く。

## リポジトリ構成

```
CLAUDE.md                 … このファイル
.claude/agents/           … サブエージェント（code-generator / asset-manager / qa-tester）
.claude/skills/           … 作業手順スキル（ordinal-techlead / unity-csharp）
.claude/hooks/            … クラウドセッション起動時に .NET SDK を用意
Claude/                   … Claude側の役割定義・設計書・PM向け報告（reports/evidence/ に実機・Editorの証拠）
GPT/                      … PM（GPT）の領域（書き換え禁止）
OrdinalScale/             … Unityプロジェクト
tools/CoreTests/          … Unityなしで Core 層をビルド・テストする .NET プロジェクト
```

## レイヤー規約（詳細は architecture.md）

| アセンブリ | 置き場所 | 依存してよいもの |
| --- | --- | --- |
| `OrdinalScale.Core` | `Scripts/Core` | なし（**UnityEngine禁止**、純C#） |
| `OrdinalScale.Platform` | `Scripts/Platform` | Core, UnityEngine（SDK非依存の抽象・Editor実装） |
| `OrdinalScale.Platform.ARFoundation` | `Scripts/Platform/ARFoundation` | Core, Platform, AR Foundation（導入時のみコンパイル） |
| `OrdinalScale.Editor` | `Editor/` | Editor専用ツール（iOS設定の適用・検証・ビルド） |
| `OrdinalScale.Gameplay`（予定） | `Scripts/Gameplay` | Core, Platform の**インターフェースのみ** |

- Gameplay / UI から Meta XR SDK・NRSDK の型を直接参照しない。必要なら `Platform/Abstractions` にインターフェースを足す。
- ゲームルール（HP・ダメージ・状態遷移）は Core に書き、`tools/CoreTests` でテストする。

## 検証コマンド

```bash
dotnet test tools/CoreTests                                   # Core 層のユニットテスト
dotnet format whitespace tools/CoreTests/CoreTests.csproj --verify-no-changes   # 整形チェック
```

- Core のテストは Unity の Test Runner（EditMode）でも同じファイルが動く。
- **クラウド環境では Unity Editor を起動できない。** Platform / Gameplay 層（UnityEngine依存）のコンパイルと動作は、開発者のローカルEditorでの確認が必要。報告では「Editorで確認済み」と「未確認」を必ず分けて書く。
- C# は Unity 6 に合わせて C# 9 まで（`LangVersion 9.0`）。

## Git

- `.meta` ファイルは必ずコミットする（Unityが生成したもの）。`Library/` `Temp/` などは `OrdinalScale/.gitignore` で除外済み。
- シーン・プレハブの YAML を手で書き換えない。Editor での作成手順を文書で渡す。
