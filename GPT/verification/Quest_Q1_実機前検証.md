# Quest Q1 剣判定の実機前検証

状態：**Unity自動テスト・設定検証は合格。Editor手操作とQuest実機は未実施**（2026-10-10）。製品条件は[仕様書](../requirements/仕様書_ドラフト.md)のE2・Q-2、実装順は[Quest戦闘計画](../plans/2026-10-09_Quest戦闘開発.md)のQ1に従う。

| 項目 | 結果・証拠 |
| --- | --- |
| 対象 | [PR #8](https://github.com/GP-Yoshihiro/Ordinal-Scale/pull/8)の`7d530d2`まで。Unityで再生成した`Quest_MR.unity`・`SwordTuning.asset`と検証証拠を反映済み。PRは下書き、基点は[PR #7](https://github.com/GP-Yoshihiro/Ordinal-Scale/pull/7) |
| 環境 | MacのUnity 6000.5.10f1。検証コピーは`~/dev/Ordinal-Scale/OrdinalScale` |
| コンパイル | 初回はEditor用asmdefのCore参照不足で失敗。Claudeが`e8fb74a`で修正し、以後コンパイル成功 |
| EditMode | 初回131/136件。テストの状態が試験間で残る問題をClaudeが`e694393`で修正後、**136/136件成功**。`/private/tmp/ordinal_q1_gameplay_editmode_20261010_final.xml`を`Claude/reports/evidence/20261010-121200_Q1_Unity_EditMode.xml`へ保存 |
| Quest用シーン | `Quest_MR.unity`をQ1部品込みで再生成。`SwordTuning.asset`も作成。Unityバッチ実行中だけ既存の確認ダイアログを迂回する一時変更を使い、実行後にソースを元へ戻した |
| 設定検証 | NG **0件**。`SwordHitDetector`と調整値アセットを確認。`Claude/reports/evidence/20261010-121717_Q0_Quest設定検証.txt` |
| Android APK | Q1内容の開発版を**ビルド成功**（エラー0・警告2、94.9 MB）。ビルド中のみLandscape Leftで、終了後Portraitへ復元。`Claude/reports/evidence/20261010-122726_Q0_Androidビルド.txt` |
| APK静的確認 | SHA-256：`52d49e6a9ff3b010f48a236699fd9c275c0cabe59379d97c3d6cdf52bdbeea8b`。`arm64-v8a`、OpenXR、頭部追跡、Passthrough宣言を確認し、v2署名検証は成功。`Claude/reports/evidence/20261010-122726_Q1_APK静的検査.txt`。APK本体はGitHubへ含めない |

Unityの自動テストは、空振り、ゆっくり押し当て、触れ続け、1振り中の再接触、別の振りなどの判定を検証する。Editorの画面で剣をドラッグして色・ログ・モデル反応を目視する手順は[Claudeの手順書](../../Claude/docs/quest-xr-setup.md)2-3節にあるが、今回のMacの画面操作接続が不安定なため未実施。Questコントローラの速度しきい値、刃の長さ・向き、追跡抜け、モデルの当たり判定と見た目の一致も実機まで未判定とする。

次はQ2の敵HP・勝利、Q3の敗北、Q4のQuest入力統合を端末なしで進める。開発者モードとUSBデバッグが整うまではAPK導入を行わない。
