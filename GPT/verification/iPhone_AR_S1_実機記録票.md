# iPhone AR S1 実機記録票

状態：**未実施**。Unity実装は[ドラフトPR #1](https://github.com/GP-Yoshihiro/Ordinal-Scale/pull/1)で共有済み。MacとiPhoneではまだ確認していない。[Claudeの手順書](https://github.com/GP-Yoshihiro/Ordinal-Scale/blob/claude/awesome-knuth-jx7l51/Claude/docs/iphone-ar-setup.md)のパート1〜4に沿って実施し、[承認済み設計](../requirements/iPhone_AR_先行検証_設計案.md)のS1（iPhone 15 Proでの起動、カメラ映像、AR追跡状態）を判定する。結果は[受入記録](iPhone_AR_受入記録.md)へ転記する。

## 接続・環境の事前確認（2026-09-30）

- Xcode 26.1.1はインストール済み。ただし既定の開発者ディレクトリはCommand Line Toolsであり、Xcodeビルド時はXcode本体を選ぶ必要がある。
- Unity 6000.5.10f1とiOS Build Supportはインストール済み。
- iPhone 15 Pro（iOS 26.6.2）はXcodeのCoreDeviceから有線接続・ペアリング済みとして認識され、デベロッパモードも有効。個体識別子は記録しない。
- `~/dev/Ordinal-Scale` に検証用の作業コピーを作成済み。Unity Editorの起動・コンパイル・iOSビルド・端末起動は未実施。

## 2026-09-30 中断時点の引継ぎ

- ユーザーの今回の検証に限る許可を受け、`~/dev/Ordinal-Scale` にClaude作業ブランチを取得した。検証用コピーのHEADは`db509d8`。
- Unity Hubは元のコピーを「Unityプロジェクトが見つかりません」と判定した。共有ブランチには`Packages/manifest.json`がなく、検証用コピーに最小のマニフェストを試験追加したが、Hubの判定は変わらなかった。このファイルはローカルの未追跡変更であり、共有ブランチには反映していない。
- Unity Hubで同じUnity版の基本テンプレートを取得し、設定のひな形を生成するため`UnitySkeleton`の作成を開始した。ただしフォルダの生成は確認できず、Unity Editor初回起動の利用規約ダイアログが表示された時点で停止した。規約への同意は行っていない。
- Xcodeの既定開発者ディレクトリはCommand Line Toolsのため、コマンドから実機を確認するときは`DEVELOPER_DIR=/Applications/Xcode.app/Contents/Developer`を指定した。Xcodeアプリ内の署名設定は未確認。
- 次回はUnityの利用規約についてユーザーの判断を確認した後、Hubに認識されるプロジェクトの初期設定を整え、手順書パート1のEditorコンパイルから再開する。S1の判定は引き続き**未実施**。

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
