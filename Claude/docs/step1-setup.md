# STEP 1 環境構築手順（Windows メイン機）

今週はQuest実機を使えないため、**Part A（Editorのみ）だけで完了**。Part B は実機が使える週の前に行う。

---

## Part A：Editor で動かすまで（目安 45〜60分）

### A-1. Unity のインストール

1. Unity Hub をインストールし、**Unity 6 の LTS 版**（Hub の「インストール」で *LTS* と表示されている最新のもの）を入れる。
2. モジュールで **Android Build Support（OpenJDK / Android SDK & NDK Tools を含む）** にもチェックを入れる。今週は使わないが、Quest の週に入れ直す手間を省くため。
3. IDE は Visual Studio 2022（Game development with Unity）か JetBrains Rider。

> Unity 6 LTS を選ぶ理由：Meta XR SDK・AR Foundation・URP の現行版がそろって対応しており、長期サポートでSTEP 4まで版を上げずに進められるため。

### A-2. プロジェクトを開く

1. このリポジトリを `git clone` する（`C:\dev\Ordinal-Scale` など、日本語・空白を含まないパス）。
2. Unity Hub →「追加」→「ディスクから追加」→ `Ordinal-Scale\OrdinalScale` フォルダを選ぶ。
3. エディターバージョンを聞かれたら A-1 で入れた版を選んで開く。
   初回は `Library/`・`ProjectSettings/`・`Packages/manifest.json`・各 `.meta` が自動生成される（数分かかる）。
   Hub がフォルダを追加できない場合は、コマンドで直接開く：
   `"C:\Program Files\Unity\Hub\Editor\<版>\Editor\Unity.exe" -projectPath C:\dev\Ordinal-Scale\OrdinalScale`

### A-3. パッケージの導入（Window > Package Manager > Unity Registry）

| パッケージ | 用途 | 備考 |
| --- | --- | --- |
| **Input System** | Editorのマウス入力、後のQuest入力 | 導入時の「新しい入力バックエンドを有効化して再起動」は **Yes** |
| **Universal RP** | Quest/グラス向け描画の標準 | 後から移行すると手間なので最初から入れる |
| **Test Framework** | ユニットテスト | 既に入っていれば不要 |

URP の有効化：
1. `Assets/_Project/Settings` で右クリック → Create > Rendering > **URP Asset (with Universal Renderer)**。
2. Edit > Project Settings > **Graphics** の Default Render Pipeline と、**Quality** の各レベルの Render Pipeline Asset に、作ったアセットを設定。

### A-4. Player Settings

Edit > Project Settings > Player > Other Settings：
- **Active Input Handling**：`Input System Package (New)`（`Both` でも動く。スクリプトは両対応済み）
- **Color Space**：Linear（URP既定）

### A-5. 動作確認（ここまでの完了条件）

1. **コンパイル**：Console にエラーが出ていないこと。
2. **テスト**：Window > General > **Test Runner** > EditMode > Run All → `HealthTests` の **7件が緑**。
3. **シーン作成**：`Assets/_Project/Scenes` に `Step1_EditorDemo` シーンを作り、次を配置して保存。
   - `Main Camera`：Position (0, 1.6, 0)（目の高さ）
   - `Floor`：3D Object > Plane、Position (0, 0, 0)
   - 空のGameObject `PlatformRig_Editor` に **PlatformRig** を追加
     - 子 `Spatial` に **EditorSpatialProvider**（Head Camera に Main Camera）
     - 子 `Input` に **EditorInputController**（Aim Camera に Main Camera）
4. **再生**：Play して Console にエラー（特に「〜の実装が子階層に見つかりません」）が出ないこと。
5. 生成された `.meta`・`ProjectSettings/`・`Packages/` と、作ったシーンをコミットして push。

問題が出たら Console のエラー全文をClaudeに渡す。

---

## Part B：Quest 実機を使う週の前に（詳細は実機週に更新）

1. Meta Horizon アプリで開発者モードを有効化（学校の端末の管理者に可否を確認）。
2. Meta XR All-in-One SDK を導入し、Project Setup Tool の「Fix All」を適用。
3. ビルドターゲットを Android に切り替え、Quest 用 `PlatformRig_Quest`（MRUK＋ハンド入力）を実装して差し替え。
4. 確認項目：実機起動／パススルー表示／敵の空間固定／ハンドでの攻撃入力。

SDK の版・導入方法は、実機週の直前に `asset-manager` サブエージェントで最新の公式情報を確認してから確定する。

---

## MacBook（M2 / 8GB）の使い方

メモリが少ないため、Unity での作業は Core のテスト・スクリプト編集・軽いシーン確認に留める。`dotnet test tools/CoreTests` は Mac でも動く（.NET 8 SDK が必要）。
