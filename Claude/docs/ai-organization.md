# AI組織と連携の進め方

## 体制

```
                 開発者（ユーザー）
            作業時間・実機・最終判断を持つ
               ▲                 ▲
   計画・進捗の相談 │                 │ 実装依頼・Editor確認結果
               ▼                 ▼
      GPT（PM / 進行管理）  ── 依頼文 ──▶  Claude（開発統括 / テックリード）
      GPT/ フォルダ          ◀── 報告書 ──   Claude/reports/
      何を・いつ・どの順で                    どう実装するか・技術判断
                                              │ 委任（.claude/agents/）
                     ┌────────────────────────┼────────────────────────┐
                     ▼                        ▼                        ▼
              code-generator            asset-manager               qa-tester
           C#実装＋Coreテスト     パッケージ/SDK/アセット/     レビュー・テスト追加・
                                  マテリアル設定手順           確認チェックリスト
```

| 役割 | 担当 | 成果物 | 置き場所 |
| --- | --- | --- | --- |
| PM | GPT | 週次計画・WBS・進捗管理 | `GPT/` |
| テックリード | Claude | 設計・技術判断・統合・PM報告 | `Claude/docs/`, `Claude/reports/` |
| Code Generation | サブエージェント `code-generator` | C#、テスト | `OrdinalScale/Assets/_Project/` |
| Asset Manager | サブエージェント `asset-manager` | 導入手順・版の調査結果 | `Claude/docs/` |
| QA | サブエージェント `qa-tester` | 判定・指摘・確認手順 | 報告内、`Tests/` |

## 1サイクルの流れ

1. **GPT → 開発者 → Claude**：週次計画の「Claudeへ渡す依頼」を開発者がClaudeに渡す。
2. **Claude**：`ordinal-techlead` スキルの手順でタスクを分解し、必要に応じてサブエージェントへ委任する。
3. **Claude（＋qa-tester）**：自動テストを通し、Editorで開発者が見るべき確認手順を作る。
4. **開発者**：ローカルのUnity Editorで確認手順を実行し、結果（OK / NG・Consoleのエラー）をClaudeに返す。
5. **Claude → 開発者 → GPT**：`Claude/reports/` の報告書（完了したこと／次の一手／詰まっていること／残り時間）をGPTに渡す。GPTが計画を更新する。

Claudeは `GPT/` を読むが書き換えない（`.claude/settings.json` で編集を禁止済み）。

## 環境で使えるもの・使えないもの

| 項目 | クラウドのClaude | 開発者のPC |
| --- | --- | --- |
| Core層（純C#）のビルド・テスト | ○ `dotnet test tools/CoreTests` | ○ Unity Test Runner |
| Unity依存コードのコンパイル | × | ○ |
| シーン・プレハブ作成 | ×（手順書で渡す） | ○ |
| Quest / ARグラス実機 | × | ○（学校のQuest、購入後のXREAL） |

## 推奨プラグイン（Claude Code）

claude.ai のプラグインカタログから、次を有効化すると専門知識つきのスキルが使える。

| プラグイン | 使いどころ |
| --- | --- |
| **Unity**（Unity Technologies公式） | URP・uGUI/UI Toolkit・3D物理衝突・パッケージ管理・Unity CLI |
| **Meta VR**（Meta公式） | Meta XR Core/Interaction SDK、MRUK（空間認識）、パススルー、XR Simulator、実機デバッグ。Quest実機週の前に有効化推奨 |
| **unity-perf** | ARグラス向け軽量化（STEP 3以降）、ゼロGC、コードレビュー |

## ローカル開発で追加を検討するもの

- **Unity MCP サーバー**（例：Unity公式/コミュニティのMCP連携）：開発者PCのClaude CodeからEditorの操作・Consoleログ取得ができるようになり、「Editor確認待ち」の往復が減る。導入時は `asset-manager` に版と手順を調べさせる。
- **Git LFS**：モデル・テクスチャを入れ始める前に設定する（`.fbx`, `.png`, `.psd`, `.wav` など）。
