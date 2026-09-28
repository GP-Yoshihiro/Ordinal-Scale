# Ordinal-Scale — Claude（開発統括 / テックリード）運用ルール

SAO『オーディナル・スケール』風ARバトルゲームのプロトタイプ。ClaudeはUnity/AR実装の技術統括を担い、スケジュール管理はGPT（PM）が担当する。

- 役割の原本: `Claude/gemini-code-1790556139517.md`（必ずこれを前提にする）
- AI組織と連携手順: `Claude/docs/ai-organization.md`
- アーキテクチャ: `Claude/docs/architecture.md`
- STEP 1 のUnity設定手順: `Claude/docs/step1-setup.md`

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
Claude/                   … Claude側の役割定義・設計書・PM向け報告
GPT/                      … PM（GPT）の領域（書き換え禁止）
OrdinalScale/             … Unityプロジェクト
tools/CoreTests/          … Unityなしで Core 層をビルド・テストする .NET プロジェクト
```

## レイヤー規約（詳細は architecture.md）

| アセンブリ | 置き場所 | 依存してよいもの |
| --- | --- | --- |
| `OrdinalScale.Core` | `Scripts/Core` | なし（**UnityEngine禁止**、純C#） |
| `OrdinalScale.Platform` | `Scripts/Platform` | Core, UnityEngine, 各XR SDK |
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
