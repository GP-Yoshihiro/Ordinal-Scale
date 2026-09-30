# iPhone AR 先行検証：MacBook＋iPhone 作業手順（順序1〜3／S1・S2）

対象：開発者がMacBook（M2 / 8GB）とiPhone 15 Proで行う作業。GPTの[実行計画](../../GPT/plans/2026-09-29_iPhone_AR先行検証.md)の**順序1（共通プロジェクトのEditor確認）**、**順序2（iOSビルドと実機での初回起動＝S1）**、**順序3（床への敵の配置＝S2）**をカバーする。**S2（パート5）は S1 に合格してから行う。**

- 今週は **Mac 1台で完結** させる。Unity `6000.5.10f1`・iOS Build Support・Xcode `26.1.1` がすでにMacに入っているため、Windows機への同じ版の導入は来週以降でよい。
- クラウド側のClaudeはUnityもiPhoneも動かせない。以下の「確認」はすべて**開発者が実行して初めて確認済み**になる。
- 作業中に出たエラーは、Consoleの全文（またはスクリーンショット）をClaudeに渡す。

| パート | 内容 | 目安 |
| --- | --- | ---: |
| 1 | プロジェクト取得・Unityで開く・パッケージ・URP・テスト・Editorシーン再生 | 60〜75分 |
| 2 | iOS設定・AR用シーン作成・設定検証・iOSビルド | 60〜90分 |
| 3 | Xcodeで署名・iPhoneへ導入・初回起動・S1の証拠取得 | 45〜75分 |
| 4 | 証拠のコミット | 10分 |
| 5 | S2：Editor でクリック配置を確認 → iPhone でタップ配置・抑止の確認 | 60〜100分（S1 合格後） |

初回ビルドとXcodeでの「実機の準備」に時間がかかるため、パート2〜3は**連続した2〜3時間**を確保するとよい。

---

## パート1：共通Unityプロジェクトを開いてEditorで確認する（順序1）

### 1-1. プロジェクトを取得する

```bash
mkdir -p ~/dev && cd ~/dev
git clone -b claude/awesome-knuth-jx7l51 https://github.com/GP-Yoshihiro/Ordinal-Scale.git
# すでに clone 済みなら:
#   cd ~/dev/Ordinal-Scale && git fetch origin && git switch claude/awesome-knuth-jx7l51 && git pull
```

パスに日本語・空白を含めない（`~/dev/Ordinal-Scale` を推奨）。

### 1-2. Unity で開く

1. メモリ節約のため、ブラウザのタブ・Xcode・その他のアプリを閉じる。
2. Unity Hub →「Add」→「Add project from disk」→ `~/dev/Ordinal-Scale/OrdinalScale` を選ぶ。
3. 一覧に **6000.5.10f1** と出ることを確認して開く。版の変更（Upgrade）を促されたら**キャンセル**する（版はチームで固定）。
4. 初回は `Library/`・`ProjectSettings/`・`Packages/` が自動生成される（10〜20分）。

### 1-3. パッケージを入れる（Window > Package Manager > Unity Registry）

表の順に、**Package Managerが既定で選ぶ版**をそのままInstallする。版の数字は後の検証メニューが自動で記録するので、メモは不要。

| パッケージ | 用途 | 備考 |
| --- | --- | --- |
| Input System | Editor・iPhone・Questの入力 | 「新しい入力バックエンドを有効にして再起動」は **Yes** |
| Universal RP | 描画パイプライン（全端末共通） | 既に入っていれば不要 |
| AR Foundation | AR機能の共通層（平面・レイキャスト等） | XR Plug-in Management・XR Core Utilities も自動で入る |
| Apple ARKit XR Plugin | iPhoneのARKit接続 | |
| Test Framework | ユニットテスト | 既に入っていれば不要 |

### 1-4. URP を設定する

1. Projectウィンドウで `Assets/_Project/Settings` を右クリック → Create > Rendering > **URP Asset (with Universal Renderer)**。名前は `OS_URP` にする（`OS_URP` と `OS_URP_Renderer` の2つができる）。
2. Edit > Project Settings > **Graphics** > Default Render Pipeline に `OS_URP` を設定。
3. `OS_URP_Renderer` を選び、Inspector 下部の **Add Renderer Feature** → **AR Background Renderer Feature** を追加。
   ※これが無いと iPhone でカメラ映像が映らず、画面が黒くなる。

### 1-5. Editor での確認（順序1の完了条件）

1. **コンパイル**：Console（Window > General > Console）に赤いエラーが0件。
2. **テスト**：Window > General > Test Runner > **EditMode** > Run All → **48件すべて緑**（Health・PlacementGate・SurfaceClassifier・EnemyPlacementSession・PlacementMessages・PlacementMath）。
3. **Editor用シーン**：[step1-setup.md の A-5 手順3](step1-setup.md) の `Step1_EditorDemo` シーンを作って Play → Console に赤いエラーが出ない。
4. Console・Test Runner の画面をスクリーンショットで保存（`⌘+Shift+4`）。

ここまでで一度コミットする（→ パート4の手順）。

---

## パート2：iPhone 用の設定とビルド（順序2の前半）

### 2-1. iOS に切り替える

File > **Build Profiles** → 左の一覧で **iOS** を選び → **Switch Platform**（数分）。

### 2-2. ARKit を有効にする

Edit > Project Settings > **XR Plug-in Management** → **iOSタブ（iPhoneのアイコン）** → **Apple ARKit** にチェック。「Initialize XR on Startup」はオンのまま。
続けて XR Plug-in Management > **Project Validation** を開き、iOS の項目に警告があれば **Fix All**。

### 2-3. AR 用シーンを作る（約10分）

1. File > New Scene → **Basic (URP)**（一覧に無ければ Basic / Empty でもよい。Empty の場合は GameObject > Light > Directional Light を足す）→ `Assets/_Project/Scenes/iPhone_AR.unity` として保存（S1〜S4 で同じシーンを使う）。
2. Hierarchy にある **Main Camera を削除**する（次の XR Origin が専用のカメラを持つため）。Directional Light は残す。
3. Hierarchy で右クリック → **XR > AR Session**。
4. Hierarchy で右クリック → **XR > XR Origin (Mobile AR)**。
5. `XR Origin` を選び、Add Component で次の2つを追加する。
   - **AR Plane Manager**：Detection Mode を **Horizontal** にする
   - **AR Raycast Manager**
6. 平面の見える化（S2 以降でも使う）：Hierarchy で右クリック → **XR > AR Default Plane** → できたオブジェクトを `Assets/_Project/Prefabs` にドラッグしてプレハブ化（名前 `ARPlane_Debug`）→ Hierarchy からは削除 → AR Plane Manager の **Plane Prefab** にそのプレハブを設定。
7. 空の GameObject を作り、名前を `PlatformRig_iPhone` にして **Platform Rig** を追加。
   - 子に空の GameObject `Spatial` を作り **AR Foundation Spatial Provider** を追加（XR Origin・Plane Manager・Raycast Manager は自動で入る。空欄なら手でドラッグ）。
   - 子に空の GameObject `StatusOverlay` を作り **AR Status Overlay** を追加（Spatial 欄が空ならドラッグ）。
8. `⌘+S` で保存。

### 2-4. 設定を適用して検証する

1. メニュー **OrdinalScale > iOS AR > 1. Player設定を適用**
   Bundle ID（`com.gpyoshihiro.ordinalscale`）、カメラ使用目的の文言、縦画面固定、ビルド対象シーンの登録を行う。
2. メニュー **OrdinalScale > iOS AR > 2. 設定を検証（証拠を保存）**
   Console に `[OK]` / `[NG]` の一覧が出て、`Claude/reports/evidence/` にテキストで保存される（クリップボードにもコピー）。
   **NG が 0 件になるまで**直して再実行する。よくある NG と直し方：

| NG の項目 | 直し方 |
| --- | --- |
| Package … 未導入 | 1-3 のパッケージを入れる |
| URP アセット 未設定 | 1-4 の手順2 |
| AR Background Renderer Feature 未追加 | 1-4 の手順3 |
| XR Plug-in (iOS) Apple ARKit | 2-2 |
| ビルドターゲット | 2-1 |
| ビルド対象の先頭シーン | シーンを保存してから「1. Player設定を適用」を再実行 |

### 2-5. iOS ビルド（Xcode プロジェクトの出力）

1. メモリ確保のため、他のアプリを閉じる。
2. メニュー **OrdinalScale > iOS AR > 3. iOS開発ビルドを出力（Xcodeプロジェクト）**
   `OrdinalScale/Builds/iOS/` に出力される（初回10〜20分）。結果は `Claude/reports/evidence/` に保存され、成功すると Finder が開く。
3. 失敗したら Console の最初の赤いエラーを控える（Unity のログ全体は `~/Library/Logs/Unity/Editor.log`）。

---

## パート3：iPhone への導入と初回起動（順序2の後半＝S1）

### 3-1. iPhone の準備（初回のみ）

1. iPhone を USB-C ケーブルで Mac につなぎ、iPhone 側で「このコンピュータを信頼」→ パスコード入力。
2. Xcode を一度起動して iPhone を認識させた後、iPhone の **設定 > プライバシーとセキュリティ > デベロッパモード** をオン → 再起動 → 「オンにする」を確認。
   （項目が出ない場合は、Xcode 起動中にケーブルを抜き差しする）

### 3-2. Xcode で署名して実行

1. **Unity Editor を終了**してから（8GB のメモリを Xcode に回すため）、`~/dev/Ordinal-Scale/OrdinalScale/Builds/iOS/Unity-iPhone.xcodeproj` を開く。
2. Xcode > Settings > **Accounts** に Apple ID を追加（無料の Personal Team でよい）。
3. 左のナビゲータで `Unity-iPhone` プロジェクト → TARGETS の **Unity-iPhone** → **Signing & Capabilities**：
   - **Automatically manage signing** にチェック
   - **Team** に自分の Personal Team を選ぶ
   - Bundle Identifier が他と重複してエラーになる場合は `com.<自分の名前>.ordinalscale` などに変え、同じ値を Unity の Player Settings にも入れて記録する
4. 画面上部の実行先で自分の iPhone を選び、**▶（⌘R）**。
   初回は「Preparing iPhone for development」などで 5〜15 分待つことがある。
5. iPhone に「信頼されていないデベロッパ」と出たら：**設定 > 一般 > VPNとデバイス管理** → 自分の Apple ID → **信頼**。もう一度 ▶。

無料の Personal Team で入れたアプリは **7日で起動できなくなる**。その場合は Xcode から ▶ で入れ直す。

### 3-3. S1 の確認（正常系）

1. アプリが起動し、**カメラの使用許可**を聞かれたら「許可」。
2. 画面上部の検証用オーバーレイで次を確認する。

| 項目 | 期待する表示 |
| --- | --- |
| 1〜3行目 | アプリ版・`Unity 6000.5.10f1`・iOS版・`Device iPhone16,1`（iPhone 15 Pro の機種ID） |
| Session | 数秒で `SessionTracking` / reason `None` |
| Camera | `Granted` |
| Tracking | `Tracking` |
| Horizontal planes | 床をゆっくり映すと 1 以上に増える（床に半透明の平面が表示される） |
| Placement | 平面が見つかると `READY` |
| FPS | 30〜60 程度（値を記録） |

3. **証拠**：上の状態で**スクリーンショット**（サイドボタン＋音量上）。さらに端末を動かして平面が増える様子を **20秒ほど画面収録**（コントロールセンター > 画面収録）。

### 3-4. S1 の確認（失敗時の扱い）

実行計画の「主な失敗条件」のうち、S1で確認できるものを試す。それぞれスクリーンショットを1枚残す。

| 試すこと | 操作 | 期待する表示 |
| --- | --- | --- |
| カメラ権限なし | iPhone の 設定 > OrdinalScale > カメラ をオフ → アプリを上スワイプで終了 → 再起動 | Camera `Denied`、Placement `BLOCKED CameraPermissionDenied` |
| 追跡未準備 | 起動直後にカメラを指で覆う | Tracking `Initializing` または `Limited`、Placement `BLOCKED …` |
| 平面未検出 | 追跡できた状態で天井や無地の壁だけを映す（床を映す前） | Horizontal planes `0`、Placement `BLOCKED NoPlaneDetected` |

確認後はカメラ許可をオンに戻す。
オーバーレイの日本語の案内文が表示されない（空白・豆腐）場合も、英語の理由コードで判定できる。表示されなかったことは記録する。

---

## パート4：証拠を残してコミットする

1. iPhone のスクリーンショット・画面収録を AirDrop で Mac に送り、`Claude/reports/evidence/` に次の名前で置く（動画は容量が大きいので **30MB を超えるものはリポジトリに入れず**、置き場所だけを記録）。
   - `S1_ok_tracking.png`、`S1_denied_camera.png`、`S1_initializing.png`、`S1_no_plane.png`、`S1_planes.mov`（任意）
2. [evidence/README.md](../reports/evidence/README.md) の **S1記録表** に結果を書き込む。
3. Unity が作ったファイルもまとめてコミットする。

```bash
cd ~/dev/Ordinal-Scale
git add OrdinalScale/Assets OrdinalScale/Packages OrdinalScale/ProjectSettings Claude/reports/evidence
git status   # Library/ や Builds/ が含まれていないことを確認
git commit -m "S1: iPhone AR 初回起動の確認結果と Unity 生成ファイルを追加"
git push origin claude/awesome-knuth-jx7l51
```

push 後に Claude に「S1 の証拠を push した」と伝える。Claude が証拠を読んで S1 の判定と報告書の更新を行う。

---

## パート5：S2 敵の仮モデルを床に置く（順序3。**S1 合格後**）

S2 の完了条件（GPT 設計案）：検出した水平面を選ぶと敵の仮モデル1体が置かれ、未検出時には誤って配置されない。
クラウドの Claude は Unity・iPhone を動かせないため、以下はすべて**未検証**の手順。詰まったら Console / Xcode のログ全文を Claude に渡す。

### 5-1. 最新のコードを取り込む

```bash
cd ~/dev/Ordinal-Scale && git pull origin claude/awesome-knuth-jx7l51
```

Unity に戻ると再コンパイルが走る。Console に赤いエラーが無いこと、Test Runner（EditMode）が **48件すべて緑** であることを確認する。

### 5-2. Editor で先に確かめる（約20分）

`Step1_EditorDemo` シーンで、実機なしに配置の判定を確認する。

1. `PlatformRig_Editor` の子 `Pointer` に **Screen Pointer Input** があることを確認（無ければ追加し、Pointer Camera に Main Camera を設定）。
2. 空の GameObject `EnemyPlacement` を作り、**Enemy Placement Controller** と **Placement Feedback View** を追加（Rig・Controller 欄が空ならドラッグ）。
3. 検証用の壁：3D Object > Cube を作り、Position (0, 1, 4)・Scale (4, 2, 0.2) にする。
4. Play して次を確かめる（画面下部に結果、Console に `[OrdinalScale][S2]` の行が出る）。

| 操作 | 期待する結果 |
| --- | --- |
| 床（Plane）をクリック | `Placed`。オレンジのカプセルが立ち、水色の目印がカメラの方を向く |
| 床の別の場所をクリック | `Moved`。敵は1体のまま移動する |
| 空（何もない所）をクリック | `Blocked TargetNotOnPlane`。敵は動かない |
| 壁（Cube の側面）をクリック | `Blocked TargetNotHorizontal`。敵は動かない |
| 敵のカプセルをクリック | 敵の後ろの床に当たれば `Moved`、何も無ければ `TargetNotOnPlane`（敵自身には置かれない） |

5. Game ビューのスクリーンショットを `Claude/reports/evidence/S2_editor.png` として保存。

### 5-3. iPhone 用シーンに部品を足す（約10分）

`iPhone_AR` シーンを開き、次を足して保存する。

1. `PlatformRig_iPhone` の子に空の GameObject `Pointer` を作り、**Screen Pointer Input** を追加。Pointer Camera に `XR Origin > Camera Offset > Main Camera` をドラッグ。
2. 空の GameObject `EnemyPlacement` を作り、**Enemy Placement Controller** と **Placement Feedback View** を追加。
3. `XR Origin` の **AR Plane Manager** の Detection Mode が **Horizontal** のままであること（壁は検出しない）。

その後、パート2-4（設定の適用・検証）→ 2-5（iOS ビルド）→ パート3-2（Xcode で ▶）をもう一度行う。

### 5-4. iPhone での確認（約30〜45分）

周囲に人や障害物が無い、安全な場所で行う。各項目でスクリーンショットを1枚撮り、全体を1本の画面収録にしてもよい。
Xcode 下部のコンソールに `[OrdinalScale][S2]` で始まる行が出るので、最後にまとめてコピーして保存する（`S2_xcode_log.txt`）。

| # | 操作 | 期待する結果（画面下部の表示） | 証拠の名前 |
| --- | --- | --- | --- |
| 1 | 起動直後、平面が出る前に画面をタップ | `Blocked NoPlaneDetected` または `TrackingInitializing`。敵は出ない | `S2_blocked_noplane.png` |
| 2 | 床をゆっくり映し、表示された平面の上をタップ | `Placed`。敵が床に立ち、こちらを向く | `S2_placed.png` |
| 3 | 別の床の位置をタップ | `Moved`。敵は1体のまま | `S2_moved.png` |
| 4 | 平面が表示されていない所（遠くの壁・空中・平面の外側）をタップ | `Blocked TargetNotOnPlane`。敵は元の位置のまま | `S2_blocked_offplane.png` |
| 5 | カメラを指で覆った直後にタップ | `Blocked TrackingLimited` など追跡の理由。敵は動かない | `S2_blocked_tracking.png` |
| 6 | （任意）机の天板など床以外の水平面をタップ | `Placed`/`Moved`（上向きの水平面なら置ける仕様） | `S2_table.png` |

判定の目安：#2・#3 で敵が1体だけ置かれ、#1・#4・#5 で**一度も誤って置かれない（動かない）**こと。
敵の大きさ・色・床からの浮き沈み・ずれが気になった場合は、気づいた内容を記録する（S3・S4 で扱う）。

### 5-5. 記録してコミット

[evidence/README.md](../reports/evidence/README.md) の **S2 記録表** に結果を書き、パート4と同じ手順でコミット・push する。

---

## 詰まったとき

| 症状 | 原因の候補と対処 |
| --- | --- |
| Hub がプロジェクトを開けない | `/Applications/Unity/Hub/Editor/6000.5.10f1/Unity.app/Contents/MacOS/Unity -projectPath ~/dev/Ordinal-Scale/OrdinalScale` で直接開く |
| 画面が真っ黒でオーバーレイだけ出る | AR Background Renderer Feature 未追加（1-4）。検証メニューで NG になっているはず |
| Session が `Unsupported` のまま | XR Plug-in Management の iOS タブで Apple ARKit が未チェック（2-2） |
| ビルドで camera usage description のエラー | メニュー「1. Player設定を適用」を再実行 |
| Xcode の署名エラー | Team 未選択／Bundle ID 重複（3-2 の手順3） |
| Xcode のビルドがメモリ不足で極端に遅い | Unity を終了し、Xcode 以外を閉じてから再ビルド |
| タップしても何も表示されない（S2） | `Pointer` の Screen Pointer Input が無い、または Pointer Camera が空。`EnemyPlacement` に Enemy Placement Controller と Placement Feedback View があるか確認 |
| 敵が表示されず Console に Shader / Material のエラー（S2） | 仮モデルの色付けが URP で失敗している可能性。エラー全文を Claude に渡す |
| Unity 6000.5.10f1 と Xcode 26.1.1 の組み合わせでビルドできない | 未確認の組み合わせ。エラー全文を Claude に渡す（版の変更はClaudeが判断してPMに報告する） |
