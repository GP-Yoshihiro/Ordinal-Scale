# Quest 3／3S：XR 方式と Q0 のセットアップ手順

状態：**Q0 実装済み・Editor／Android ビルド未確認**（2026-10-09、Claude）。
対象：`GPT/plans/2026-10-09_Quest戦闘開発.md` の Q0、依頼 `GPT/requests/2026-10-09_Claude_Quest開発着手.md`。

## 1. 採用した方式

| 項目 | 採用 | 版 | 理由 |
| --- | --- | --- | --- |
| XR ランタイム | Unity **OpenXR Plugin** | `com.unity.xr.openxr` **1.17.1** | Unity マニュアルで Unity 6000.5 向けに「released」とされる版。Oculus XR Plugin は Meta が非推奨・削除予定としている |
| Quest 対応 | **Unity OpenXR: Meta** | `com.unity.xr.meta-openxr` **2.5.1** | Unity 6000.5 向けの released 版。依存は OpenXR 1.15.1 以上・AR Foundation 6.5.0 以上で、既存の AR Foundation 6.5.1 とそのまま組み合わせられる（パッケージの package.json で確認） |
| パススルー | AR Foundation の `ARCameraManager` ＋ OpenXR 機能「Meta Quest: Camera (Passthrough)」 | ― | カメラ背景を単色・アルファ0にし、`ARCameraManager` を有効にするだけで表示される。Meta XR Core SDK（`OVRManager`・`OVRPassthroughLayer`）は不要 |
| 頭部 | `XROrigin`（追跡原点 Floor）＋ TrackedPoseDriver（Input System） | AR Foundation／Core Utils／Input System は既存 | iPhone と同じ部品 |
| コントローラ（剣） | `UnityEngine.XR.InputDevices` のグリップ姿勢（Oculus Touch／Meta Quest Touch Plus の各プロファイル） | 組み込みモジュール | 追加パッケージなし。XR パッケージが無くてもコンパイルできる |
| 部屋の認識・配置 | **使わない**。起動後、追跡が安定したら正面 2m の床（追跡原点 Floor の高さ）に敵を置く | ― | Space Setup の部屋データと `USE_SCENE` 権限が不要で、Q-1 を最小の前提で確かめられる（下の 4 章） |

採らなかった案：

- **Meta XR Core SDK（OVRCameraRig／OVRManager）**：パススルーの公式手順はこちらが前提。ただし Meta 独自の SDK を Platform 層以外へ持ち込みやすく、iPhone の AR Foundation 経路と部品を共有できない。Meta のレジストリ利用（規約同意）も増える。OpenXR: Meta で不足が出たら Q4 で再検討する。
- **Meta XR Simulator**：Meta XR Core SDK が前提のため Q0 では使わない。Editor での代替は 3 章の「XR なし再生」。Q4 で必要なら XR Interaction Toolkit の XR Device Simulator を候補にする。

参照：[Unity と OpenXR の互換性（Meta）](https://developers.meta.com/vr/documentation/unity/unity-and-openxr-compatibility/)、[Passthrough 導入（Meta）](https://developers.meta.com/vr/documentation/unity/unity-passthrough-gs/)、[OpenXR: Meta の Camera 機能（Unity）](https://docs.unity3d.com/Packages/com.unity.xr.meta-openxr@2.5/manual/features/camera.html)、[Unity 6000.5 の meta-openxr 版](https://docs.unity3d.com/6000.5/Documentation/Manual/com.unity.xr.meta-openxr.html)、[Unity 6000.5 の openxr 版](https://docs.unity3d.com/6000.5/Documentation/Manual/com.unity.xr.openxr.html)

## 2. 開発者の手順（Unity 6000.5.10f1）

所要の目安：初回 60〜90分（Android モジュールのダウンロードと IL2CPP の初回ビルドが大半）。

### 2-1. 準備（1回だけ）

1. Unity Hub → Installs → 6000.5.10f1 → Add modules で **Android Build Support**（OpenJDK、Android SDK & NDK Tools を含める）を追加する。
2. `git pull` で `main`（または本 PR のブランチ）を取得し、Unity Hub から `OrdinalScale/` を開く。

### 2-2. メニュー `OrdinalScale > Quest MR`

| # | メニュー | すること | 結果の確認 |
| --- | --- | --- | --- |
| 1 | XRパッケージを追加（OpenXR・Meta） | Package Manager で `com.unity.xr.openxr@1.17.1` と `com.unity.xr.meta-openxr@2.5.1` を追加（依存で `com.unity.xr.compositionlayers` も入る） | Console に「導入済み」が並ぶ。再コンパイル後に 2〜5 のメニューが現れる |
| 2 | Android・OpenXR設定を適用 | Android：IL2CPP／ARM64／最小 API 32／Vulkan のみ。XR Plug-in Management の Android タブに OpenXR を割り当て、Meta Quest 機能グループ、Touch／Touch Plus の入力プロファイル、Meta Quest: Session／Camera (Passthrough) を有効化。**iOS の設定・ビルド対象一覧・縦持ち固定には触れない** | Console の要約。プロジェクト検証（Project Settings > XR Plug-in Management > Project Validation）の Android に赤が残っていないか |
| ― | File > Build Profiles で **Android に Switch Platform** | ― | ― |
| 3 | Questシーンを作成 | `Assets/_Project/Scenes/Quest_MR.unity` をコードで組み立てて保存（構成は `QuestSceneBuilder` の冒頭コメント）。ビルド対象一覧には追加しない | シーンが開き、Hierarchy に `XR Origin (Quest)`・`PlatformRig_Quest`・`Battle` がある |
| 4 | 設定を検証（証拠を保存） | Unity 版・Android モジュール・パッケージ版・Android 設定・XR ローダー・OpenXR 機能・iPhone 経路の回帰・シーン構成を OK/NG で記録 | `Claude/reports/evidence/YYYYMMDD-HHMMSS_Q0_Quest設定検証.txt` の NG が 0 |
| 5 | Android開発ビルド（APK） | Quest シーンだけを指定して `Builds/Android/OrdinalScale_Quest_dev.apk` を作る（Unity 既定のデバッグ鍵）。**ビルドの間だけ画面の向きを Landscape Left にし、終わったら（失敗しても）元の Portrait に戻す**（2-5） | `…_Q0_Androidビルド.txt` の「ビルド結果」と「ビルド後の画面の向き（元の値に復元）」が OK |

コマンドラインでビルドする場合（2〜3 を済ませた後。Mac の例）：

```bash
"/Applications/Unity/Hub/Editor/6000.5.10f1/Unity.app/Contents/MacOS/Unity" -batchmode \
  -projectPath OrdinalScale -buildTarget Android \
  -executeMethod OrdinalScale.EditorTools.QuestMRSetup.BuildApkFromCommandLine \
  -logFile build_android.log
echo $?   # 0 = 成功
```

### 2-3. Editor での確認（実機なし）

**Q1 の変更を取り込んだら、まずメニュー3「Questシーンを作成」で `Quest_MR.unity` を作り直す**（`SwordHitDetector` が追加され、`Assets/_Project/Settings/SwordTuning.asset` が作られる）。続けてメニュー4で NG 0 を確認する。`Quest_MR.unity` を開いて再生すると、XR が動いていないので `XRRuntimeSwitch` が Editor の代替入力（マウス）に切り替える。Game ビューは 16:9 などで、Free Aspect でも可。

#### Q0 の確認

| 見ること | 期待 |
| --- | --- |
| Console | `[OrdinalScale][Q0] 実行環境: XRなし（Editorの代替入力）`、`[OrdinalScale][Q1] 剣の調整値（仮の値・Quest 実機で未確認）: start=1.5m/s …` |
| 約1秒後 | 正面 2m の床に敵（DemonLord2 があればそのモデル、無ければオレンジのカプセル）が立ち、こちらを向く。Console に `[OrdinalScale][Q0] 敵を正面に配置 …` と `[OrdinalScale][Q1] 敵の当たり判定: 高さ… 半径…` |
| 左ボタンを押さずにマウスを動かす | 剣は出ない |
| 左ボタンでドラッグ | 押している間だけ水色の棒（刃 0.9m）が出て、マウスの動きに合わせて動く。離すと消える |
| 背景 | 黒（パススルー用に透明なので Editor では黒く見えるのが正しい） |
| Scene ビュー | 敵の周りに黄色の線のカプセル（剣の当たり判定） |

#### Q1 の確認：命中と不命中の区別

画面左上の表示（英字）に、刃先の速さ・振りの状態・回数（hits／swings／whiffs／slow contact／same-swing contact／held）と直近の出来事が出る。Console には日本語で `[OrdinalScale][Q1] …` が出る。**各場面の前に左上の「Reset counters」を押す。**

刃先の速さ(m/s) ＝ マウスの速さ(画面の高さ/秒) × 換算係数（`Sword (Mouse)` の `Meters Per Screen Height`、既定 1.0・仮）。既定では、マウスを**画面の高さぶん 0.5秒以内**に動かすと振り（開始 1.5m/s 以上・仮）、**2秒以上かけて**動かすとゆっくり接触になる。敵の胴は画面中央付近（刃先の平面は正面2m）。押した瞬間は刃先がマウスの下に出るが、既定の係数ではその後の刃先の動きはマウスより小さい（約4割）ため、刃先がマウスから遅れて見えるのは仕様。

| # | 場面 | 操作 | 期待（回数） | 期待（ログ・見た目） |
| --- | --- | --- | --- | --- |
| 1 | 空振り | 敵の頭より上で押し、左から右へ素早く払って離す | swings 1・whiffs 1・hits 0 | `振り#1 終了 … 結果=空振り（接触なし）`。刃は振りの間だけ黄色 |
| 2 | ゆっくり接触 | 敵の胸の左横で押し、ゆっくり（2秒以上）胴の中央まで動かして離す | slow contact 1・swings 0・hits 0 | `不命中：ゆっくり接触（振り中でない）`。刃が青くなる。敵は光らない |
| 3 | 有効な命中 | 胸の左横で押し、右へ素早く払って離す | hits 1・swings 1 | `命中 振り#1 …`、`結果=命中`。敵が白く光り、刃が赤くなる |
| 4a | 触れ続け（命中後） | 3と同じく素早く払い、胴の中央で止めて0.5秒以上押したまま、ゆっくり抜いて離す | hits 1・held 1 | 命中は1回だけ。抜けたとき `接触終了：Nフレーム触れ続け（追加の命中なし）・この接触で命中済み` |
| 4b | 押し当てたまま振る | 胴の中央で押し（押した瞬間に接触）、胴から出ない幅で左右に素早く揺らしてから離す | hits 0・slow contact 1・swings 1以上 | `不命中：ゆっくり接触`、`結果=接触したが命中なし（触れ続け）` |
| 5 | 1振りでの再接触 | 胸の左横で押し、右へ素早く払って**止めずに**左へ戻して離す | hits 1・same-swing contact 1・swings 1 | 2回目の接触で `不命中：同じ振り#1で再接触（1振り1命中）`。刃が紫になる |
| 6 | 別の振り | 右へ素早く払い、**0.3秒ほど止めてから**（押したまま）左へ素早く払う。または一度離して押し直して払う | hits 2・swings 2 | `命中 振り#1`、`命中 振り#2` |
| 7 | しきい値の調整 | 再生中に `Assets/_Project/Settings/SwordTuning.asset` の `Swing Start Speed` を 3.0 にし、3と同じ速さで払う | 以前は命中した速さで slow contact になる | `剣の調整値…を変更: start=3m/s …`（変更時に回数は初期化）。確認後 1.5 に戻す |

結果は `Claude/reports/evidence/README.md` の Q1 記録表に書き、Console の `[OrdinalScale][Q1]` 行を `Q1_editor_log.txt` として保存する。
ここでの合否は **Editor の代替操作での区別**の確認で、Quest のコントローラでの「斬った／押し当てた」の感覚やしきい値の妥当性は確認できない（5章）。

#### Q2 の確認：HP・撃破・終了／再挑戦（E3・E4）

**Q2 の変更を取り込んだら、メニュー3で `Quest_MR.unity` をもう一度作り直す**（`BattleController`・`BattleView` が追加される）。メニュー4で `戦闘の進行（BattleController…）` が OK になることを確認してから再生する。

画面右上に `Q2 battle  round N`・`enemy HP 現在/最大`・`valid hits`・`state`（FIGHTING／VICTORY）が出る。敵の頭上には赤い HP バーが出て、命中のたびに右から短くなる。Console には `[OrdinalScale][Q2] …` が日本語で出る。撃破に必要な有効命中数は `Battle` の `BattleController` の `Hits To Defeat`（初期10・試遊で調整する値。再生中の変更は次の再生から反映）。

| # | 場面 | 操作 | 期待 |
| --- | --- | --- | --- |
| Q2-1 | 9回では撃破しない | Q1 の場面3（有効な命中）を9回。振りの間は毎回0.3秒ほど止める | `enemy HP 1/10`・`valid hits 9`・`state FIGHTING`。HP バーが残り1割。`敵HP 9/10 … 敵HP 1/10` のログが9行 |
| Q2-2 | 空振り・押し当てでは減らない | 続けて Q1 の場面1（空振り）と場面2（ゆっくり接触）、4b（押し当てたまま振る）、5（1振りでの再接触の2回目）を数回ずつ | HP は 1/10 のまま。`[OrdinalScale][Q2] 敵HP` のログが増えない（Q1 のログ・回数は増える） |
| Q2-3 | 10回目で撃破 | 有効な命中をもう1回 | `敵HP 0/10`、`撃破：有効命中 10 回（ラウンド 1）…`。敵が光ってから約0.4秒で消え、`VICTORY` の文字（敵がいた位置の上）と、画面中央に `VICTORY` と「終了 / Quit」「再挑戦 / Retry」のボタン。`state VICTORY` |
| Q2-4 | 撃破後は攻撃無効 | 撃破後に敵がいた場所で速く払う | 左上の Q1 の表示が消える（剣の判定が止まっている）。HP・`valid hits` は変わらない。刃は水色のまま（黄・赤にならない） |
| Q2-5 | 再挑戦 | 「再挑戦 / Retry」を押す | `再挑戦：ラウンド 2。敵・HP（10/10）・剣の判定を初期化`。約1秒後に敵が正面に再び立ち、HP バーが満タン、左上の Q1 の回数が0。`round 2`・`state FIGHTING` |
| Q2-6 | 再挑戦後も同じ回数で撃破 | 有効な命中を10回 | 9回目まで撃破せず、10回目で `撃破：有効命中 10 回（ラウンド 2）` |
| Q2-7 | 終了（Editor） | 撃破後に「終了 / Quit」を押す | `終了要求を受け付けました（Editor ではアプリを閉じない…）`。右上に `QUIT REQUESTED`、中央に `Quit requested …`。再生は止まらない。もう一度押してもログは増えない |
| Q2-8 | 撃破前は2択が出ない | 再生直後（撃破前） | 中央の2択は出ない |

結果は `Claude/reports/evidence/README.md` の Q2 記録表に書き、Console の `[OrdinalScale][Q2]` 行を `Q2_editor_log.txt` として保存する。**Quest 実機での表示（HP バー・VICTORY の文字）、`Application.Quit` でアプリが閉じるか、ヘッドセット内からの2択の操作は未確認**（2択のコントローラ操作は Q4）。

### 2-4. コミットしてよいもの・いけないもの

- コミットする：`Packages/manifest.json`・`Packages/packages-lock.json`、`ProjectSettings/*.asset`、`Assets/XR/` 配下（OpenXR の設定アセットを含む）、`Assets/_Project/Scenes/Quest_MR.unity`、新しく生成された `.meta`、`Claude/reports/evidence/` の検証・ビルドのテキスト。
- コミットしない：`Builds/`（APK）、キーストア、`Assets/LocalLicensed/`（Fab 原本。`.gitignore` 済み）、`UserSettings/`。

### 2-5. 画面の向き（iPhone の縦持ちと Quest の Landscape Left）

`PlayerSettings.defaultInterfaceOrientation`（Player 設定の Default Orientation）は **Android と iOS で共有の1つの値**。iPhone 用のメニュー（`OrdinalScale > iOS AR > 1`）は Portrait に固定し、OpenXR の Meta Quest Support は Landscape Left 以外をビルド前の検証でエラーにする（`Meta Quest HMDs only support Landscape Left orientation.`、com.unity.xr.openxr 1.17.1 の `MetaQuestFeature`）。2026-10-09 に開発者の Mac で、メニュー5の初回ビルドがこのエラーで失敗した。

対応（`Editor/Quest/QuestBuildOrientation.cs`）：

- 保存しておく値は **Portrait のまま**（iPhone の経路を壊さない）。
- メニュー5と `BuildApkFromCommandLine` は、`BuildPipeline.BuildPlayer` の直前に Landscape Left にし、成功・失敗・例外のいずれでも直後に元の値へ戻して `ProjectSettings` を保存し直す。
- ビルド中に Unity が落ちた場合に備え、変更前の値を `Library/OrdinalScale_QuestOrientationBackup.txt` に控え、次に Editor を開いたとき自動で戻す（`Library/` はコミットされない）。
- メニュー4の設定検証は「保存値が Portrait」「一時変更が残っていない」を確認する。
- **File > Build Profiles から直接 Android をビルドすると、この切り替えが働かず同じエラーで失敗する**。Quest の APK は必ずメニュー5かコマンドラインで作る。
- Project Validation の「Fix」で Landscape Left に直さない（iPhone 用の値が変わる）。直してしまった場合は `OrdinalScale > iOS AR > 1` で Portrait に戻す。

## 3. コードの構成（Q0 で追加）

```
Core/Combat/BladeSample           刃の1フレーム分の観測値（純C#）。刃先の速さ TryTipSpeed（Q1 の振り判定の入力）
Core/Spatial/PlacementMath        TryPointInFront（正面 d m の床の点）を追加
Platform/Abstractions/ISwordPoseSource, BladePose   剣の姿勢の境界（BladePose.ToSample() で Core へ渡す）
Platform/XR/XRHeadSpatialProvider        頭部・床・床へのレイ（IARSpatialProvider）
Platform/XR/XRControllerSwordPoseSource  コントローラのグリップ姿勢 → 刃
Platform/XR/XRRuntimeSwitch              XR あり／なしで入力を切り替え（PlatformRig より先に実行）
Platform/EditorSim/EditorSwordPoseSource マウス → 刃（C7 の最小版。Q1 でドラッグ操作と速度換算を追加）
Platform/PlatformRig                     Sword を追加。有効な GameObject 上の実装を優先して解決
Gameplay/Placement/FixedEnemyPlacement   正面固定の配置
Gameplay/Combat/SwordPoseDebugView       刃の表示と速さのログ（命中判定はしない）
Editor/QuestPackageInstaller             パッケージ追加（常にコンパイル）
Editor/Quest/*                           設定適用・シーン作成・検証・APK ビルド（XR パッケージ導入後にコンパイル）
```

`OrdinalScale.Editor.Quest` は `OS_HAS_ARFOUNDATION`・`OS_HAS_OPENXR`・`OS_HAS_XR_MANAGEMENT` がそろったときだけコンパイルされる。iPhone 用の `OrdinalScale.Editor` は変更していない。

## 4. 配置方法の提案（Q-1「配置方法・許容するずれは技術検証待ち」への回答案）

| 案 | 前提 | 長所 | 短所 | 判断 |
| --- | --- | --- | --- | --- |
| **A. 正面固定（採用）** | 追跡原点 Floor | 権限・部屋のスキャン不要。起動するだけで敵が出る | 家具や壁と重なりうる。床の高さは Quest のガーディアン設定に依存 | Q0〜Q3 はこれで進める |
| B. 床の平面検出に配置 | Space Setup 済み＋`USE_SCENE` 権限 | 現実の床に正確に立つ | 端末ごとに部屋の登録が必要。学校の Quest で設定できるか未定 | 実機で A の床ずれが目立つ場合に Q4 で追加 |
| C. コントローラで指して置く | B と同じ | 置き場所を選べる | B と同じ | Q4 以降の候補 |

A は製品の範囲を変えない（Q-1 は「敵1体がパススルーの現実空間に表示される」で、配置方法は技術側の提案待ち）。安全範囲（Q-6）は現地でガーディアン境界を設定し、敵の距離 2m（`FixedEnemyPlacement.distance`）を範囲に合わせて調整する。

## 5. 実機でしか確認できないこと（Q0 の範囲）

- 起動して OpenXR が Quest 3／3S で初期化されるか、パススルーが表示されるか（背景が黒いままなら URP の設定を確認）
- 床の高さ（追跡原点 Floor）と敵の足元が一致して見えるか、2m が安全範囲に収まるか
- 剣の判定（Q1）：振り開始・終了の速さのしきい値（1.5／0.6 m/s・仮）が「斬った」「押し当てた」の感覚に合うか、速い振りの取りこぼし、追跡の途切れでの誤命中、敵が光る反応が見えるか（`Claude/docs/sword-input-design.md` 6章）
- 戦闘（Q2）：敵の頭上の HP バーと `VICTORY` の文字がパススルー越しに見えるか、「終了」で `Application.Quit` によりアプリが閉じるか（Editor ではログのみ）。2択はヘッドセット内から選べない（画面ボタンは Editor 専用、コントローラでの選択は Q4）
- コントローラの追跡と刃の向き（`bladeLocalEuler` の初期値 0,0,0 で「握った拳から前へ」伸びるか）
- フレームレート（URP のポストプロセス・HDR は iPhone と共有のため未調整）
- 導入方法：開発者モードの有効化（Meta の開発者登録と規約同意が必要）と `adb install`、または学校の管理方法。いずれもユーザー判断
