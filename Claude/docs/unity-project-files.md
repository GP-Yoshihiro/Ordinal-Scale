# Unity プロジェクトの構成ファイル（Packages / ProjectSettings）

`OrdinalScale/` を Unity Hub から直接開ける Unity プロジェクトにするため、次のファイルをリポジトリに置いている（2026-09-30 追加）。

| ファイル | 内容 | 出所 |
| --- | --- | --- |
| `Packages/manifest.json` | 使うパッケージと版（下表）＋Unity 組み込みモジュール一式 | 下記サンプルの `manifest.json` から必要なものだけを抜粋。Test Framework のみ追加 |
| `ProjectSettings/ProjectVersion.txt` | `6000.5.10f1`（リビジョン `3bd4f66ad299`） | リビジョンは Unity の公式ダウンロード URL の値で確認 |
| `ProjectSettings/ProjectSettings.asset` | Player 設定 | 下記サンプルのファイルを複製し、下の「変更した値」だけを変更 |
| `ProjectSettings/EditorSettings.asset` | メタファイル表示・テキスト形式での保存（Git 向け） | 下記サンプルのファイルをそのまま複製 |

それ以外の設定ファイル（Graphics・Quality・Tag・Physics など）は置いていない。初回に Unity が既定値で作るので、生成されたものを開発者がコミットする。

## 出所

- Unity-Technologies/arfoundation-samples の `6.5` ブランチ、コミット `bfdc662`（2026-09-03、Unity `6000.5.0b7` で保存されたもの）
- ライセンス：Unity Companion License（Unity を使うプロジェクトでの利用を認めるもの）
- 選んだ理由：AR Foundation と Apple ARKit を iOS で使う、Unity 公式のプロジェクトで、同じ `6000.5` 系の Unity が書き出した設定だから。クラウド環境では Unity を起動できず、設定ファイルを Unity に生成させられないため、手書きではなく公式の生成物を使った。

## パッケージの版

| パッケージ | 版 | 備考 |
| --- | --- | --- |
| com.unity.inputsystem | 1.20.0 | 要求 Unity 6000.0 以上（公開ミラーの package.json で確認） |
| com.unity.render-pipelines.universal | 17.5.0 | Unity 本体に固定された版に置き換わる場合がある |
| com.unity.ugui | 2.5.0 | 同上 |
| com.unity.xr.arfoundation | 6.5.1 | 要求 Unity 6000.0 以上 |
| com.unity.xr.arkit | 6.5.1 | 同上 |
| com.unity.test-framework | 1.4.6 | サンプルには無いため追加。公開済みの最新版（要求 Unity 2019.4 以上）。Unity 本体に固定された版があればそちらに置き換わる |
| com.unity.ide.visualstudio / com.unity.ide.rider | 2.0.28 / 3.0.40 | エディタ連携 |

初回に開いたときに生成される `Packages/packages-lock.json` をコミットして、実際に解決された版を固定する。

## `ProjectSettings.asset` で変更した値

サンプル固有の値だけを変えた。構造やその他の値には手を入れていない。

| 項目 | サンプル | 変更後 |
| --- | --- | --- |
| productGUID | サンプルの値 | 新しく生成した値（プロジェクトの識別子が重ならないように） |
| companyName / productName / projectName | Unity Technologies / AR Foundation Samples / Template_3D | GP-Yoshihiro / OrdinalScale / OrdinalScale |
| applicationIdentifier（Android・Standalone・iPhone） | com.unity.arfoundation.samples | com.gpyoshihiro.ordinalscale |
| appleEnableAutomaticSigning | 0 | 1（Xcode の自動署名を使うため） |
| cameraUsageDescription | Camera required for AR | Uses the camera to show enemies in your real surroundings. |
| locationUsageDescription | Required for Geo Anchors | 空（位置情報は使わない） |
| scriptingDefineSymbols | ポストプロセス等のサンプル用定義 | 空（ARKit の定義はプラグインが自動で付ける） |

そのまま使う主な値：Active Input Handling = Input System Package (New)、iOS 最小バージョン 15.0、Color Space = Linear。

## 未確認

- この構成で Unity Hub 3.21.0 が「Add project from disk」でプロジェクトとして認識するか（Hub の判定条件は公開されておらず、クラウドでは確認できない）。認識しない場合はコマンドで直接開く（`iphone-ar-setup.md` 1-2 手順4）。
- `6000.5.0b7` で保存された設定を `6000.5.10f1` で開いたときの自動更新の有無。
- パッケージが上表の版で解決されるか。

## Quest 用パッケージ（2026-10-09 追加予定・未反映）

Quest 3／3S（Q0）で次の2つを追加する。`manifest.json` を手で書き換えず、Unity のメニュー **OrdinalScale > Quest MR > 1. XRパッケージを追加** から Package Manager に追加させ、生成された `manifest.json` と `packages-lock.json` をコミットする（このリポジトリにはまだ入っていない）。

| パッケージ | 版 | 備考 |
| --- | --- | --- |
| com.unity.xr.openxr | 1.17.1 | Unity 6000.5 向け released 版（Unity マニュアル）。依存：XR Management 4.4.0、Input System 1.6.3、Core Utils 2.3.0 以上 |
| com.unity.xr.meta-openxr | 2.5.1 | Unity 6000.5 向け released 版。依存：OpenXR 1.15.1、AR Foundation 6.5.0、Composition Layers 2.4.0、Core Utils 2.5.1 以上 |

選定理由とセットアップ手順は [quest-xr-setup.md](quest-xr-setup.md)。Android の Player 設定（IL2CPP・ARM64・最小 API 32・Vulkan）も同じ手順のメニュー 2 が Unity の API で書き込むので、`ProjectSettings.asset` は手で編集しない。
