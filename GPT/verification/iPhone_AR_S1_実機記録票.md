# iPhone AR S1 実機記録票

状態：**S1実機判定は未実施**。Unity Editorの起動・EditModeテスト48件、iOS向け設定検証NG 0件、UnityのiOSビルド、Xcodeの署名なしビルドを確認した。iPhoneへの導入・起動・カメラ映像・AR追跡は未確認。[Claudeの手順書](https://github.com/GP-Yoshihiro/Ordinal-Scale/blob/claude/awesome-knuth-jx7l51/Claude/docs/iphone-ar-setup.md)のパート3から再開し、[承認済み設計](../requirements/iPhone_AR_先行検証_設計案.md)のS1を判定する。結果は[受入記録](iPhone_AR_受入記録.md)へ転記する。

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

## 2026-09-30 再開後の切り分け

- Claudeに、共有ブランチのUnityプロジェクトをHubから直接開ける構成へ修正し、手順書を更新するよう依頼した。依頼は届いたが、現時点で修正結果は未受領。
- 今回の検証用コピーに限り、Unity 6000.5.10f1付属の3Dテンプレートから`Packages/manifest.json`と`ProjectSettings`一式を追加した。いずれも未追跡のローカル変更で、共有ブランチには反映していない。
- 追加後もUnity Hubのフォルダ選択画面で「開く」が無効のまま。Hubにプロジェクトとして登録できたことは**未確認**。Unity Editorの初回利用規約への同意はユーザーの判断待ちで、操作していない。
- Editorでのコンパイル、iOSビルド、iPhone実機起動は引き続き**未実施**。S1の合否は判定しない。

## 2026-09-30 Unity Editorでの確認

- ユーザーの許可を受けてUnity Editor Software Termsに同意し、Editor 6000.5.10f1を起動した。
- Claudeの修正コミット`e8c660f`を検証用コピーへ取り込んだ。Unity Hub 3.21.0は`OrdinalScale`を認識して一覧へ登録でき、Editorでも開けた。試験用に追加した旧設定は`~/dev/Ordinal-Scale/verification-backup/`へ退避した。
- 初回インポート後のUnity Consoleは情報3件、警告1件、赤いエラー0件。AR Foundation・ARKitを含むパッケージが読み込まれ、EditModeテスト48件が一覧に表示された。
- Unity 6000.5.10f1のバッチモードでEditModeテストを実行し、`~/dev/Ordinal-Scale/OrdinalScale/Logs/EditModeResults.xml`で**48件成功、失敗0件**を確認した。最初の`-quit`付き試行は結果XMLが作られなかったため、終了指定を外して再実行した結果である。
- S1用シーン、URPのAR背景設定、iOSビルド、iPhone実機起動は未実施。**S1の合否は未判定**。

## 2026-10-01 実機テスト直前までの準備

- 検証用コピーの`codex/iphone-ar-prep`ブランチで、Claudeの`e8c660f`を基に`iPhone_AR`シーン、水平面の可視化プレハブ、URPのAR Background Renderer Feature、iOSのARKitローダー、Unity生成の`.meta`・ProjectSettingsを追加した。コミットは[`906d630`](https://github.com/GP-Yoshihiro/Ordinal-Scale/commit/906d630)で、GitHubへpush済み。一時的な生成スクリプトと`Builds/`、`Library/`、退避フォルダはコミットに含めていない。
- Unityの設定検証は**NG 0件**。Unity 6000.5.10f1、URP 17.5.0、AR Foundation／ARKit 6.5.1、XR Management 4.6.1、先頭シーンとカメラ使用目的を確認。証拠はコミット内の`Claude/reports/evidence/20261001-001104_S1_設定検証.txt`。
- UnityのiOS開発ビルドは**Succeeded、エラー0・警告0、17分34秒**。Xcodeプロジェクトは`~/dev/Ordinal-Scale/OrdinalScale/Builds/iOS/Unity-iPhone.xcodeproj`に生成。証拠はコミット内の`Claude/reports/evidence/20261001-002933_S1_iOSビルド.txt`。
- Xcode 26.1.1で生成プロジェクトを汎用iOS向けに**署名なしビルド成功**。`OrdinalScale.app`の出力を確認した。Unity生成コードの非推奨APIとApp Store向けアイコン等の警告はあったが、ビルドを止めるエラーはなかった。この結果はiPhoneでの起動を保証しない。
- Macの有効なコード署名IDは0件で、Xcodeプロジェクトの`DEVELOPMENT_TEAM`は空欄。ユーザーがXcodeにApple IDを設定し、Teamを選んだ後、実機向けビルド・導入・起動を行う。ユーザー就寝中の指示に従い、端末への導入とアプリ起動は行っていない。

## 2026-10-05 実機検証の再開

- iPhone 15 ProはUSB接続・ペアリング済みで、デベロッパーモードは有効。Macに有効な開発用署名IDが1件あることを確認した。10月1日の「0件」は当時の状態である。
- Xcodeの「Apple Accounts」は未登録。署名付きビルドは`No Accounts`と対象Bundle IDのプロビジョニングプロファイル不在で失敗した。本人によるApple IDログインが必要。検証用Xcodeプロジェクトには既存証明書のTeamを設定済みだが、生成物なのでUnity再ビルド時には設定し直す。
- 実機を指定したビルドでは、iPhoneのロック中に開発用ディスクイメージをマウントできず、Xcodeが端末を利用可能と判定しなかった。ユーザーへロック解除とUSB接続維持を依頼した。
- 署名付きビルドのログは検証コピーの`OrdinalScale/Logs/IOSGenericSignedBuild.log`と`IOSDeviceBuild.log`。端末へのインストール・起動・S1の観察は引き続き**未実施**。

## 実施情報

| 項目 | 記録 |
| --- | --- |
| 実施日時・実施者 | 未記入 |
| Unityプロジェクトのブランチ・コミット | `codex/iphone-ar-prep`・`906d630`（実機準備） |
| Unity Editor・iOS Build Supportの版 | 6000.5.10f1・同版のiOS Build Support |
| AR Foundation・ARKit等のパッケージ版 | AR Foundation 6.5.1、ARKit 6.5.1、URP 17.5.0、XR Management 4.6.1 |
| Xcode・iOSの版 | Xcode 26.1.1、接続端末iOS 26.6.2。端末上の起動版は未確認 |
| 使用機器 | iPhone 15 Pro |
| ビルドログ・端末画面の保存先 | Unityの証拠は`Claude/reports/evidence/`、ローカルのUnityログは`~/dev/Ordinal-Scale/OrdinalScale/Logs/`。端末画面は未取得 |

## 確認手順と結果

| 順序 | 実施する確認 | 結果・証拠 |
| --- | --- | --- |
| 1 | Claudeの手順に従い、共有されたコミットのUnityプロジェクトをMac上のEditorで開く。コンパイルエラーの有無を記録する | 実施済み。Console赤エラー0件、EditModeテスト48件成功 |
| 2 | iOS向けにビルドし、XcodeでiPhone 15 Proへ導入する。ビルド・署名・導入の成否とログを記録する | UnityビルドとXcode署名なしビルドは成功。署名・端末導入は未実施 |
| 3 | 端末でアプリを起動する。カメラ権限の表示と選択結果を記録する | 未実施 |
| 4 | 実際のカメラ映像が見えるかを確認し、画面の証拠を残す | 未実施 |
| 5 | AR追跡の状態が画面またはログで判別できるかを確認し、表示内容を記録する | 未実施 |
| 6 | エラーが出た場合は文言、操作、再現手順、ログの保存先を記録する | 未実施 |

**S1の判定**：未実施。端末で起動し、カメラ映像とAR追跡状態の両方を確認できた場合に限り「合格」とする。Editor起動、APIスタブでのコンパイル、iOSビルドだけでは合格にしない。

**次への引継ぎ**：S1合格後、S2の床面検出・敵配置へ進む。S1で分かったカメラ権限・追跡状態の問題はS2の前提条件として共有する。
