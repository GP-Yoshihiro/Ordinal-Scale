# Quest Q0 実機スモーク記録票

状態：**未実施**。2026-10-09にQuestを使用可能になったとの連絡を受け、[Q0下書きPR #7](https://github.com/GP-Yoshihiro/Ordinal-Scale/pull/7)の実機確認用に作成。これは[戦闘全体の受入記録票](Quest_実機受入記録票.md)を代替しない。剣の命中、敵の反撃、勝敗はQ0の対象外。

## 実施条件

| 項目 | 記録 |
| --- | --- |
| 実施日・担当 | 未記入 |
| Quest機種・OS版 | USB表示はQuest 3。OS版は未確認 |
| Mac接続・開発者モード・USBデバッグ許可 | 2026-10-09にMacのUSB機器として認識。`adb devices -l`は端末なし。ユーザーは開発者モード・USBデバッグ許可の状態を未把握。端末側の確認が必要 |
| APK導入方法と権限 | 未確認 |
| Unity版・PR／コミット・APKファイル名 | Unity 6000.5.10f1。[PR #7](https://github.com/GP-Yoshihiro/Ordinal-Scale/pull/7)の`ce2c886`までを反映。ローカルAPKは`~/dev/Ordinal-Scale/OrdinalScale/Builds/Android/OrdinalScale_Quest_dev.apk`（GitHubには含めない）。SHA-256：`77707ed4843a36a678608b14b3083b6708fe832e3134a9bb2f3ceedf0b3e19c1` |
| テスト場所・周囲の安全範囲 | 未記入 |
| Android設定検証・Editor再生・APKビルドの結果 | Unity 6000.5.10f1のAndroid Build Support・SDK/NDK・OpenJDKは導入済み。Q0設定検証はNG 0件（`20261010-114851_Q0_Quest設定検証.txt`）。Editor再生でDemonLord2の出現を確認。マウスでの剣の動きは未確認。初回APKビルドは共通画面方向がPortraitでOpenXR事前検証に失敗（`20261009-105104_Q0_Androidビルド.txt`）。Claudeの自動復元修正を取り込んだ再ビルドは成功（`20261010-114925_Q0_Androidビルド.txt`、エラー0、警告2、75.8 MB）。ビルド中だけLandscape Leftとなり、終了後にPortraitへ復元。実機への導入・起動は未実施 |
| APK構成・署名の静的確認 | 2026-10-10にUnity付属の`aapt`と`apksigner`で確認。Application IDは`com.gpyoshihiro.ordinalscale`、CPUは`arm64-v8a`、OpenXR権限・頭部追跡・`com.oculus.feature.PASSTHROUGH`の宣言あり。Android Debug証明書によるAPK署名の検証は成功。実機での機能動作は未判定 |

## Q0だけの確認順

安全な立ち位置を決め、境界設定が有効な状態で、最初は歩き回らずに確認する。失敗時は画面表示、Unity／Androidログ、再現手順を残す。開発版APKが導入できない場合は、どの段階で止まったかを記録する。

開発者モードが有効になったら、[Meta公式のデバイス設定手順](https://developers.meta.com/vr/documentation/native/android/mobile-device-setup/)に沿ってQuest内のUSBデバッグ許可を確認する。その後MacのUnity付属ADBで`adb devices -l`を実行し、端末が`device`と表示された場合にだけ上記APKを`adb install -r`で導入する。学校管理端末で許可画面や導入権限が出ない場合は、端末管理者へ確認する。

| 順 | 確認内容 | 結果（合格／不合格／未実施） | 証拠・再現手順 |
| --- | --- | --- | --- |
| 1 | APKを導入し、アプリが起動する | 未実施 | |
| 2 | 現実の部屋がパススルーで見え、表示が破綻しない | 未実施 | |
| 3 | 頭を左右に動かしても表示が追跡される | 未実施 | |
| 4 | 正面の敵1体または仮モデルが、説明どおりの位置に現れる。床との高さ・距離も記録する | 未実施 | |
| 5 | コントローラを動かすと剣の姿勢表示が追従し、停止時に刃先速度が下がる | 未実施 | |
| 6 | 起動し直しても同じ手順で再現できる | 未実施 | |

## 判定と次の作業

- Q0の結果：未判定。
- 不具合の優先度・再現条件・担当：未記入。
- Q1（振って敵の体に触れた時だけ1振り1命中）への影響：未判定。
- 戦闘全体のQ-1〜Q-6を合格と記すには、別途[実機受入記録票](Quest_実機受入記録票.md)で勝敗まで確認する。
