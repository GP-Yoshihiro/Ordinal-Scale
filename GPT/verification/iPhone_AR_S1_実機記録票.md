# iPhone AR S1 実機記録票

状態：**未実施**。Unity実装は[ドラフトPR #1](https://github.com/GP-Yoshihiro/Ordinal-Scale/pull/1)で共有済み。MacとiPhoneではまだ確認していない。[Claudeの手順書](https://github.com/GP-Yoshihiro/Ordinal-Scale/blob/claude/awesome-knuth-jx7l51/Claude/docs/iphone-ar-setup.md)のパート1〜4に沿って実施し、[承認済み設計](../requirements/iPhone_AR_先行検証_設計案.md)のS1（iPhone 15 Proでの起動、カメラ映像、AR追跡状態）を判定する。結果は[受入記録](iPhone_AR_受入記録.md)へ転記する。

## 実施情報

| 項目 | 記録 |
| --- | --- |
| 実施日時・実施者 | 未記入 |
| Unityプロジェクトのブランチ・コミット | 未記入 |
| Unity Editor・iOS Build Supportの版 | 未記入 |
| AR Foundation・ARKit等のパッケージ版 | 未記入 |
| Xcode・iOSの版 | 未記入 |
| 使用機器 | iPhone 15 Pro |
| ビルドログ・端末画面の保存先 | 未記入 |

## 確認手順と結果

| 順序 | 実施する確認 | 結果・証拠 |
| --- | --- | --- |
| 1 | Claudeの手順に従い、共有されたコミットのUnityプロジェクトをMac上のEditorで開く。コンパイルエラーの有無を記録する | 未実施 |
| 2 | iOS向けにビルドし、XcodeでiPhone 15 Proへ導入する。ビルド・署名・導入の成否とログを記録する | 未実施 |
| 3 | 端末でアプリを起動する。カメラ権限の表示と選択結果を記録する | 未実施 |
| 4 | 実際のカメラ映像が見えるかを確認し、画面の証拠を残す | 未実施 |
| 5 | AR追跡の状態が画面またはログで判別できるかを確認し、表示内容を記録する | 未実施 |
| 6 | エラーが出た場合は文言、操作、再現手順、ログの保存先を記録する | 未実施 |

**S1の判定**：未実施。端末で起動し、カメラ映像とAR追跡状態の両方を確認できた場合に限り「合格」とする。Editor起動、APIスタブでのコンパイル、iOSビルドだけでは合格にしない。

**次への引継ぎ**：S1合格後、S2の床面検出・敵配置へ進む。S1で分かったカメラ権限・追跡状態の問題はS2の前提条件として共有する。
