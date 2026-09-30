# 検証の証拠

iPhone AR 先行検証（S1〜S5）の証拠を置く場所。Unity のメニュー **OrdinalScale > iOS AR** の「2. 設定を検証」「3. iOS開発ビルド」はここにテキストを自動保存する（`YYYYMMDD-HHMMSS_S1_….txt`）。

- 画像・動画は `S<番号>_<内容>.png/.mov` の名前で置く。30MB を超える動画はリポジトリに入れず、保存場所だけを表に書く。
- **実行した人・日時・端末が分かるものだけ**を証拠にする。未実施の行は「未実施」のまま残す。

## S1 記録表（開発者が記入）

| 項目 | 記入欄 |
| --- | --- |
| 実施日時・実施者 | 未実施 |
| Mac（機種・macOS版） | 未実施 |
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
