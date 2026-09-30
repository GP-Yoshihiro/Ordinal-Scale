# STEP 1 環境構築手順（Windows メイン機）

> **9月30日更新**：今週（9/28〜10/4）は iPhone AR 先行検証のため、**MacBook での手順 [iphone-ar-setup.md](iphone-ar-setup.md) を優先**する。Windows 機は同じ Unity 版を入れて使えるようにしておく（今週は任意）。

Quest 実機は 11月18日以降に使える。それまでは Part A（Editor）と iPhone で進め、Part B は実機を使う週の前に行う。

---

## Part A：Editor で動かすまで（目安 45〜60分）

### A-1. Unity のインストール

1. Unity Hub をインストールし、**Unity `6000.5.10f1`** を入れる（Hub の Install Editor > Archive、またはリリースアーカイブから同じ版を選ぶ）。Mac と同じ版にそろえないと、同じプロジェクトを行き来できない。
2. モジュールで **Android Build Support（OpenJDK / Android SDK & NDK Tools を含む）** にもチェックを入れる。今週は使わないが、Quest の週に入れ直す手間を省くため。
3. IDE は Visual Studio 2022（Game development with Unity）か JetBrains Rider。

> 版を `6000.5.10f1` に固定する理由：iOS ビルドを行う MacBook に、この版と iOS Build Support がすでに入っているため（8GB の Mac で別の版を入れ直す時間を省く）。
> 注意：`6000.5` 系の今後の更新提供状況は未確認。Quest 作業に入る前に、Meta 向けパッケージの対応状況と合わせて版の更新要否を判断する。

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
| **AR Foundation** / **Apple ARKit XR Plugin** | iPhone の AR 経路（共通プロジェクトなので Windows でも入れる） | Mac で先に入れて push 済みなら、pull すれば自動で入る |

URP の有効化（Mac で設定・push 済みなら不要）：
1. `Assets/_Project/Settings` で右クリック → Create > Rendering > **URP Asset (with Universal Renderer)**（名前 `OS_URP`）。
2. Edit > Project Settings > **Graphics** の Default Render Pipeline に設定。
3. `OS_URP_Renderer` に **AR Background Renderer Feature** を追加（iPhone のカメラ映像表示に必要）。

### A-4. Player Settings

Edit > Project Settings > Player > Other Settings：
- **Active Input Handling**：`Input System Package (New)`（`Both` でも動く。スクリプトは両対応済み）
- **Color Space**：Linear（URP既定）

### A-5. 動作確認（ここまでの完了条件）

1. **コンパイル**：Console にエラーが出ていないこと。
2. **テスト**：Window > General > **Test Runner** > EditMode > Run All → **17件が緑**（`HealthTests` 7件＋`PlacementGateTests` 10件）。
3. **シーン作成**：`Assets/_Project/Scenes` に `Step1_EditorDemo` シーンを作り、次を配置して保存。
   - `Main Camera`：Position (0, 1.6, 0)（目の高さ）
   - `Floor`：3D Object > Plane、Position (0, 0, 0)
   - 空のGameObject `PlatformRig_Editor` に **PlatformRig** を追加
     - 子 `Spatial` に **EditorSpatialProvider**（Head Camera に Main Camera）
     - 子 `Pointer` に **ScreenPointerInput**（Pointer Camera に Main Camera）
4. **再生**：Play して Console にエラー（特に「〜の実装が子階層に見つかりません」）が出ないこと。
5. 生成された `.meta`・`ProjectSettings/`・`Packages/` と、作ったシーンをコミットして push。

問題が出たら Console のエラー全文をClaudeに渡す。

---

## Part B：Quest 実機を使う週の前に（詳細は実機週に更新）

1. Meta Horizon アプリで開発者モードを有効化（学校の端末の管理者に可否を確認）。
2. Meta XR All-in-One SDK を導入し、Project Setup Tool の「Fix All」を適用。
3. ビルドターゲットを Android に切り替え、Quest 用 `PlatformRig_Quest`（空間認識＋コントローラの剣入力）を実装して差し替え。
4. 確認項目：実機起動／パススルー表示／敵の空間固定／コントローラを剣として振った命中判定（[sword-input-design.md](sword-input-design.md)）。

空間認識を Meta XR SDK（MRUK）で作るか、iPhone と同じ AR Foundation（Unity OpenXR: Meta 経由）で作るかは、11月18日より前に比較して決める。

SDK の版・導入方法は、実機週の直前に `asset-manager` サブエージェントで最新の公式情報を確認してから確定する。

---

## MacBook（M2 / 8GB）の使い方

今週は iOS ビルドのためのメイン機として使う（手順は [iphone-ar-setup.md](iphone-ar-setup.md)）。メモリが少ないため、Unity と Xcode を同時に開かない、ブラウザを閉じる、などで負荷を下げる。`dotnet test tools/CoreTests` は Mac でも動く（.NET 8 SDK が必要）。
