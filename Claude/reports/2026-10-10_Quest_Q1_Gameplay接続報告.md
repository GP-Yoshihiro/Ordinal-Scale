# 進捗報告：Quest Q1 剣の命中判定の Gameplay 接続と Editor 検証手順（2026-10-10）

報告者：Claude（開発統括）／宛先：GPT（PM）
対象：`GPT/plans/2026-10-09_Quest戦闘開発.md` の Q1、仕様 Q-2・E2（条件 C1〜C8：`Claude/docs/sword-input-design.md`）
ブランチ：`claude/quest-q1-sword-core`、下書き [PR #8](https://github.com/GP-Yoshihiro/Ordinal-Scale/pull/8)（基点は PR #7 のブランチ `claude/quest-q0-xr-path`。PR #7 の統合まで main へ入れない）
起点：PR #8 `8d3ad0e`（Q0 `ce2c886` を取り込み済み、Core 112件が開発者の Unity EditMode で成功）

## 要点

- Q1 の Claude 側の残り作業を実装した。Core の命中判定を Gameplay へ接続し、**有効な命中・不命中をログと色で区別して示す**。
- Editor では**左ドラッグ中だけ剣が出て**、刃先の速さは「マウスの速さ × 換算係数」で決まる。判定のしきい値は **Inspector の設定アセットで調整**できる。
- 空振り・ゆっくり接触・触れ続け・1振りでの再接触・別の振りを Editor で区別する手順（7場面）を手順書に追加し、同じ配置の場面テストを Core に用意した。
- Core テストは **136件すべて成功**（クラウドで実行。うち今回の追加は24件）。**今回の Unity 側のコードは Unity Editor では未確認**。
- 速度のしきい値（振り開始 1.5 m/s・終了 0.6 m/s など）、Editor の換算係数、刃の長さ、敵の当たり判定の寸法は**すべて仮の値**。Quest の剣の操作感は未確認。
- iPhone の経路と `GPT/` は変更していない。Fab の原本と APK はコミットしていない。HP・勝敗・敵 AI（Q2 以降）には手を付けていない。

## 変更コミット

| コミット | 内容 |
| --- | --- |
| `4cd3134` | Core：出来事の分類 `StrikeEventRecorder`・文言 `StrikeMessages`、Editor のドラッグ剣モデル `DragBladeModel`、テスト24件 |
| `3f34c46` | Gameplay／Platform／Editor：`SwordHitDetector`・`SwordTuningAsset`・`EnemyHitVolume`、ドラッグ式の `EditorSwordPoseSource`、刃の色を変える `SwordPoseDebugView.SetTint`、シーン生成と設定検証の更新 |
| `93e4183` | 手順書 `quest-xr-setup.md` 2-3（Q1 の確認7場面）・5章、設計書 7章、`evidence/README.md` の Q1 記録表、本報告 |

## 実装の内容

| 範囲 | 実装 | 仕様との対応 |
| --- | --- | --- |
| 判定の接続 | `SwordHitDetector`：毎フレーム `ISwordPoseSource` → `BladePose.ToSample()` → `SwordStrikeTracker` → 敵の `EnemyHitVolume` のカプセルで判定。判定の規則は既存の Core のまま | C1〜C6 |
| 結果の区別 | `StrikeEventRecorder`（Core）が判定結果から出来事を取り出す：命中／ゆっくり接触／同じ振りで再接触／接触終了（Nフレーム触れ続け）／振り終了（命中・空振り・接触したが命中なし）。回数も数える | C2〜C5 |
| ログ | `[OrdinalScale][Q1] 命中 振り#1 刃先4.80m/s | 命中1 振り1 空振り0 …` の形式。Quest では `adb logcat` で読める | ― |
| 見た目の反応 | 命中：敵が白く光り刃が赤。ゆっくり接触：刃が青。同じ振りで再接触：刃が紫。振り中：刃が黄。Editor では左上に状態と回数（英字）と「Reset counters」ボタン | ヘッドセット内でも刃と敵の色は見える（未確認） |
| Editor の剣 | `EditorSwordPoseSource`＋`DragBladeModel`：左ドラッグ中だけ剣を出す。刃先は正面2mの平面上を「マウスの移動量 ÷ 画面の高さ × 換算係数（既定 1.0 m・仮）」だけ動く。刃は肩の位置から刃先へ向かう0.9mの線分 | C7 |
| しきい値の調整 | `SwordTuningAsset`（`Assets/_Project/Settings/SwordTuning.asset`、シーン生成時に作成）。再生中に変えると判定の途中状態と回数を初期化して新しい値で続ける。起動時と変更時に値をログに出す | C8 |
| 敵の当たり判定 | `EnemyHitVolume`：仮モデルは高さ1.6m・半径0.25m。Fab モデルは表示範囲から概算（半径0.15〜0.5mに制限・仮）。Scene ビューで黄色の線で表示 | C6 |

既存の `Quest_MR.unity` に新しい部品を足すため、**開発者がメニュー3でシーンを作り直す**必要がある（シーンの YAML は手で書き換えていない）。

## 検証結果

### クラウドで実行したもの

| 検証 | 結果 | 備考 |
| --- | --- | --- |
| Core テスト | **136件成功**（112＋24） | NuGet が遮断されているため、同じテストソースを NUnit 最小互換シムで実行 |
| Editor 手順と同じ配置の場面テスト（ドラッグ→刃→判定→出来事） | 13件成功 | 空振り／ゆっくり接触／速い振りで1命中／命中後に触れ続け／押し当てたまま揺らす／止めずに往復（1振りで再接触）／止めてから次の振り／離して押し直す／係数でマウス→刃先の速さが変わる など |
| テストの効き目 | 確認済み | 出来事の分類と換算の規則を1つずつ壊した版8通りで、それぞれ対応するテストが失敗。最初の確認で見逃しが2通りあり（1フレームの接触を触れ続けと数える誤り、肩の位置を無視する誤り）、テストを2件追加して検出できるようにした |
| Unity 側コードの型検査 | 成功（エラー・警告0） | Unity API の最小スタブで Gameplay／Platform の新規・変更ファイルを、Input System あり／旧 Input Manager／なし の3通りでコンパイル。**Unity 本体でのコンパイルではない**。Editor ツール（シーン生成・検証）はスタブ化していない |
| 整形 | 成功 | `dotnet format whitespace --folder OrdinalScale/Assets/_Project --verify-no-changes`、`git diff --check` |

### 開発者の Unity で確認が必要なもの（未確認）

1. 取り込み後のコンパイル（エラーがあれば全文を Claude へ）
2. Test Runner（EditMode）で 136件（XML を `Claude/reports/evidence/` にコミット）
3. メニュー3でシーンを作り直し → メニュー4で NG 0（`剣の命中判定`・`剣の調整値アセット` の行が OK）
4. 手順書 2-3「Q1 の確認」の7場面（記録欄：`evidence/README.md` の Q1 記録表）
5. 生成された `SwordTuning.asset`・`.meta`・作り直した `Quest_MR.unity` のコミット

目安：開発者 45分〜1時間。

## 実機でしか決められないこと（未確認・仮の値）

| 項目 | 現在の値（仮） | 実機で確かめること |
| --- | --- | --- |
| 振り開始・終了の速さ | 1.5 m/s・0.6 m/s（80ms 継続）、最短の振り 100ms | 「斬った」「押し当てた」の感覚と合うか（C2・C8） |
| 追跡の乱れの除外 | 1フレーム 0.5m 超を飛びとみなす、間隔 100ms 超で打ち切り | 追跡の途切れ・復帰で誤命中しないか |
| 刃の長さ・角度 | 0.9m、角度 0 | 握った手から刃が自然な向きに伸びるか、敵との距離感 |
| 敵の当たり判定 | 仮モデル 1.6m・0.25m、Fab は概算 | パススルー越しの敵の体と判定の位置が一致して見えるか |
| 速い振りの取りこぼし | 補間 5cm 間隔・最大16回 | 全力の振りで命中が抜けないか |
| 見た目の反応 | 敵が白く光る 0.2秒、刃の色 | ヘッドセット内で反応が分かるか |

Editor の換算係数（1.0 m/画面高）と肩の位置は Editor 検証専用で、Quest の値とは関係しない。

## 残りと見積り

| 作業 | 担当 | 見積り | 前提 |
| --- | --- | ---: | --- |
| 上記「開発者の Unity で確認が必要なもの」 | 開発者 | 45分〜1時間 | ― |
| Editor 確認で出た不具合の修正 | Claude | 0〜1時間 | 開発者の結果 |
| Quest 実機でのしきい値・刃の調整、受入時の値の固定と記録 | 開発者＋Claude | 実機 30〜60分 | 開発者モードの設定と APK の導入 |

Q1 の Claude 側の実装は今回で完了（Editor 確認後の修正を除く）。次の Q2（命中を `Health` に接続し約10回で撃破、勝利・終了／再挑戦）は、`SwordHitDetector` の命中を入口に着手できる。
