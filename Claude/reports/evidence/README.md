# 検証の証拠

iPhone AR 先行検証（S1〜S5）と Quest（Q0〜）の証拠を置く場所。Unity のメニュー **OrdinalScale > iOS AR** の「2. 設定を検証」「3. iOS開発ビルド」、**OrdinalScale > Quest MR** の「4. 設定を検証」「5. Android開発ビルド」はここにテキストを自動保存する（`YYYYMMDD-HHMMSS_S1_….txt`、`…_Q0_….txt`）。

- 画像・動画は `S<番号>_<内容>.png/.mov` の名前で置く。30MB を超える動画はリポジトリに入れず、保存場所だけを表に書く。
- **実行した人・日時・端末が分かるものだけ**を証拠にする。未実施の行は「未実施」のまま残す。

## S1 記録表（開発者が記入）

| 項目 | 記入欄 |
| --- | --- |
| 実施日時・実施者 | 未実施 |
| Mac（機種・macOS版） | 未実施 |
| Unity の開き方（Hub から追加できたか／コマンドで直接開いたか） | 未実施 |
| Unity 版 | 未実施（設定検証テキストに自動記録） |
| Xcode 版 | 未実施（設定検証テキストに自動記録） |
| パッケージ版（Input System / URP / AR Foundation / Apple ARKit / XR Management） | 未実施（設定検証テキストに自動記録） |
| iPhone 機種・iOS 版 | 未実施（オーバーレイ3行目） |
| 設定検証の NG 件数（ファイル名） | 未実施 |
| Unity ビルド結果・所要時間（ファイル名） | 未実施 |
| Xcode で導入・起動できたか（エラーがあれば全文） | 未実施 |
| 正常系：Session / Camera / Tracking / 平面数 / Placement / FPS | 未実施（`S1_ok_tracking.png`） |
| カメラ権限なし：表示された理由 | 未実施（`S1_denied_camera.png`） |
| 追跡未準備：表示された理由 | 未実施（`S1_initializing.png`） |
| 平面未検出：表示された理由 | 未実施（`S1_no_plane.png`） |
| 日本語の案内文が表示されたか | 未実施 |
| 気づいた問題・所要時間の実績 | 未実施 |

- GPT 側の受入判定は `GPT/verification/` の記録票に転記される（読み取り専用）。ここには Claude が再現・判定に使う一次資料を置く。

## S2 記録表（開発者が記入。S1 合格後）

| 項目 | 記入欄 |
| --- | --- |
| 実施日時・実施者・コミット | 未実施 |
| Editor（5-2）：Placed / Moved / TargetNotOnPlane / TargetNotHorizontal の結果 | 未実施（`S2_editor.png`） |
| iPhone #1 平面が出る前のタップ | 未実施（`S2_blocked_noplane.png`） |
| iPhone #2 床への配置 | 未実施（`S2_placed.png`） |
| iPhone #3 再配置で1体のまま移動 | 未実施（`S2_moved.png`） |
| iPhone #4 平面の外のタップ | 未実施（`S2_blocked_offplane.png`） |
| iPhone #5 追跡不安定時のタップ | 未実施（`S2_blocked_tracking.png`） |
| 誤って配置された（動いた）ことがあったか | 未実施 |
| Xcode コンソールの `[OrdinalScale][S2]` 行 | 未実施（`S2_xcode_log.txt`） |
| 敵の大きさ・色・浮き沈みなど気づいた点 | 未実施 |

## Q0 記録表（開発者が記入。Quest 実機は不要）

手順は `Claude/docs/quest-xr-setup.md` 2章。Editor と Android ビルドの結果は**準備の証拠**で、Quest 実機での合格を意味しない。

| 項目 | 記入欄 |
| --- | --- |
| 実施日時・実施者・コミット | 未実施 |
| Mac／PC（機種・OS版） | 未実施 |
| Android Build Support の追加にかかった時間 | 未実施 |
| パッケージ追加後の版（openxr / meta-openxr / compositionlayers） | 未実施（設定検証テキストに自動記録） |
| コンパイルエラーの有無（あれば全文） | 未実施 |
| Project Validation（Android）に残った警告・エラー | 未実施 |
| 設定検証の NG 件数（ファイル名） | 未実施 |
| Editor 再生：敵が正面2mに立つ／刃が追従／刃先速度が変わる／Game ビュー外で途切れ | 未実施（`Q0_editor.png`） |
| Android ビルド結果・所要時間・APKサイズ（ファイル名） | 未実施 |
| iPhone 経路の回帰（iOS 設定検証の NG 件数） | 未実施 |
| 気づいた問題・所要時間の実績 | 未実施 |

## Q1 記録表（開発者が記入。Editor の代替操作。Quest 実機は不要）

手順は `Claude/docs/quest-xr-setup.md` 2-3「Q1 の確認」。しきい値・換算係数は仮の値のまま行う。Editor での区別の確認で、Quest での操作感の合格ではない。

| 項目 | 記入欄 |
| --- | --- |
| 実施日時・実施者・コミット | 未実施 |
| コンパイルエラーの有無（あれば全文） | 未実施 |
| EditMode テストの件数と結果（XML のファイル名） | 未実施（136件の見込み） |
| メニュー3で作り直した後のメニュー4の NG 件数（ファイル名） | 未実施 |
| 1 空振り：swings / whiffs / hits | 未実施 |
| 2 ゆっくり接触：slow contact / hits | 未実施 |
| 3 有効な命中：hits、敵が光ったか | 未実施 |
| 4a 触れ続け（命中後）：hits / held | 未実施 |
| 4b 押し当てたまま振る：hits / slow contact | 未実施 |
| 5 1振りでの再接触：hits / same-swing contact | 未実施 |
| 6 別の振り：hits / swings | 未実施 |
| 7 しきい値の変更が反映されたか | 未実施 |
| 意図と違う判定になった操作（あれば具体的に） | 未実施 |
| Console の `[OrdinalScale][Q1]` 行 | 未実施（`Q1_editor_log.txt`） |

## Q2 記録表（開発者が記入。Editor。Quest 実機は不要）

手順は `Claude/docs/quest-xr-setup.md` 2-3「Q2 の確認」。撃破に必要な有効命中数は初期10（試遊で調整する値）。Editor での確認で、Quest での表示・終了の合格ではない。

| 項目 | 記入欄 |
| --- | --- |
| 実施日時・実施者・コミット | 未実施 |
| コンパイルエラーの有無（あれば全文） | 未実施 |
| EditMode テストの件数と結果（XML のファイル名） | 未実施（153件の見込み） |
| メニュー3で作り直した後のメニュー4の NG 件数（ファイル名） | 未実施 |
| Q2-1 9回：enemy HP / state | 未実施 |
| Q2-2 空振り・押し当て・再接触で HP が変わらないか | 未実施 |
| Q2-3 10回目で撃破：VICTORY・2択が出たか、敵が消えたか | 未実施 |
| Q2-4 撃破後の攻撃が無効か | 未実施 |
| Q2-5 再挑戦：HP 10/10・敵の再配置・剣の回数0 | 未実施 |
| Q2-6 再挑戦後に10回で再び撃破 | 未実施 |
| Q2-7 終了：ログ・QUIT REQUESTED、二重に出ないか | 未実施 |
| Q2-8 撃破前に2択が出ないか | 未実施 |
| 気づいた問題 | 未実施 |
| Console の `[OrdinalScale][Q2]` 行 | 未実施（`Q2_editor_log.txt`） |

## Q3 記録表（開発者が記入。Editor。Quest 実機は不要）

手順は `Claude/docs/quest-xr-setup.md` 2-3「Q3 の確認」。値はすべて仮。Editor では回避を試せない。

| 項目 | 記入欄 |
| --- | --- |
| 実施日時・実施者・コミット | 未実施 |
| コンパイルエラーの有無（あれば全文） | 未実施 |
| EditMode テストの件数と結果（XML のファイル名） | 未実施（175件の見込み） |
| メニュー3で作り直した後のメニュー4の NG 件数（ファイル名） | 未実施 |
| Q3-1 接近と停止（約1.2m、安全範囲の外へ出ない） | 未実施 |
| Q3-2 予告と攻撃、プレイヤーHPの減少 | 未実施 |
| Q3-3 敗北：DEFEAT・2択、剣が無効 | 未実施 |
| Q3-4 敗北から再挑戦：双方HP・敵の再配置 | 未実施 |
| Q3-5 勝利：撃破後に敵の攻撃が来ない | 未実施 |
| Q3-6 勝利から再挑戦：プレイヤーHPも戻る | 未実施 |
| Q3-7 敗北から終了 | 未実施 |
| 気づいた問題 | 未実施 |
