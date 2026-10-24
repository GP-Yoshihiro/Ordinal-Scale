#!/usr/bin/env bash
# Quest 実機検証（Q5／Q6）の補助：端末の確認、APK の導入、起動、ログの採取と集計。
# 手順は Claude/docs/quest-device-test.md。macOS 標準の bash 3.2 でも動くように書いている。
#
#   tools/quest/quest_device.sh check              端末・開発者モード・USB デバッグ許可の確認
#   tools/quest/quest_device.sh install [APK]      SHA-256 を表示して adb install -r
#   tools/quest/quest_device.sh run <ラベル> [--practice]
#                                                  ログを消去 → アプリを起動し直す → Ctrl-C までログ採取 → 集計
#                                                  --practice：剣の測定モード（敵は動かず攻撃せず、HP を減らさない。受入には使わない）
#   tools/quest/quest_device.sh summary <ログ>     採取済みログの集計だけを表示
#   tools/quest/quest_device.sh running            アプリのプロセスが残っているか（「終了」の確認）
#   tools/quest/quest_device.sh stop               アプリを強制停止
#
# adb は環境変数 ADB → PATH → Unity 6000.5.10f1 付属の SDK の順に探す。
# 生ログ（全行）は OrdinalScale/Builds/DeviceLogs/（.gitignore 済み）に置き、GitHub に入れない。
# Claude/reports/evidence/ には [OrdinalScale] の行・エラーの行と集計だけを保存する（端末のシリアル番号は保存しない）。

set -u

PACKAGE="com.gpyoshihiro.ordinalscale"
PRACTICE_EXTRA="os_sword_practice"   # LaunchOptions.SwordPractice と同じ文字列
UNITY_VERSION="6000.5.10f1"
REPO_ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
DEFAULT_APK="$REPO_ROOT/OrdinalScale/Builds/Android/OrdinalScale_Quest_dev.apk"
RAW_DIR="$REPO_ROOT/OrdinalScale/Builds/DeviceLogs"
EVIDENCE_DIR="$REPO_ROOT/Claude/reports/evidence"
TAG="[OrdinalScale]"
# 証拠に残す行：アプリのログと、Unity のエラー・例外、Java／ネイティブのクラッシュ
KEEP_PATTERN='\[OrdinalScale\]|E/Unity|Exception|E/AndroidRuntime|F/libc|E/DEBUG|FATAL'

die() { echo "エラー: $*" >&2; exit 1; }

find_adb() {
    if [ -n "${ADB:-}" ]; then
        [ -x "$ADB" ] || die "ADB=$ADB は実行できません"
        echo "$ADB"; return
    fi
    if command -v adb >/dev/null 2>&1; then command -v adb; return; fi
    local c
    for c in \
        "/Applications/Unity/Hub/Editor/$UNITY_VERSION/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb" \
        "$HOME/Unity/Hub/Editor/$UNITY_VERSION/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb" \
        "/c/Program Files/Unity/Hub/Editor/$UNITY_VERSION/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb.exe"; do
        if [ -x "$c" ]; then echo "$c"; return; fi
    done
    die "adb が見つかりません。Unity Hub で $UNITY_VERSION に Android Build Support（SDK & NDK Tools）を追加するか、ADB=<adb のパス> を指定してください"
}

sha256_of() {
    if command -v shasum >/dev/null 2>&1; then shasum -a 256 "$1" | awk '{print $1}'
    else sha256sum "$1" | awk '{print $1}'; fi
}

# 接続状態を1語で返す：device / unauthorized / offline / none / many
device_state() {
    local lines n
    lines="$("$ADB_BIN" devices | sed '1d' | awk 'NF >= 2 {print $2}')"
    n="$(printf '%s\n' "$lines" | grep -c . || true)"
    if [ "$n" -eq 0 ]; then echo none
    elif [ "$n" -gt 1 ]; then echo many
    else echo "$lines"; fi
}

require_device() {
    local s
    s="$(device_state)"
    [ "$s" = "device" ] || die "端末が使える状態ではありません（状態: $s）。先に「$0 check」を実行してください"
}

prop() { "$ADB_BIN" shell getprop "$1" 2>/dev/null | tr -d '\r'; }

cmd_check() {
    echo "adb: $ADB_BIN"
    "$ADB_BIN" version | head -1
    echo "--- adb devices -l"
    "$ADB_BIN" devices -l
    case "$(device_state)" in
        device)
            echo "--- 端末（記録票の「Quest機種・OS版」に転記）"
            echo "機種: $(prop ro.product.manufacturer) $(prop ro.product.model)（$(prop ro.product.device)）"
            echo "OS: build=$(prop ro.build.display.id) incremental=$(prop ro.build.version.incremental) Android=$(prop ro.build.version.release)"
            echo "電池: $("$ADB_BIN" shell dumpsys battery 2>/dev/null | tr -d '\r' | awk -F': ' '/level/ {print $2 "%"; exit}')"
            if "$ADB_BIN" shell pm path "$PACKAGE" 2>/dev/null | grep -q package:; then
                echo "アプリ: 導入済み（$PACKAGE）"
            else
                echo "アプリ: 未導入（次は「$0 install」）"
            fi
            echo "OK: 端末は使える状態です"
            ;;
        unauthorized)
            echo "NG: USB デバッグが未許可。ヘッドセットを装着し「USBデバッグを許可しますか」で「許可」（常に許可にチェック）を選び、もう一度 check を実行"
            exit 2 ;;
        offline)
            echo "NG: 端末が offline。ケーブルを挿し直すか「$ADB_BIN kill-server」の後にもう一度 check"
            exit 2 ;;
        many)
            echo "NG: 端末が2台以上つながっています。1台だけにするか ANDROID_SERIAL=<シリアル> を指定"
            exit 2 ;;
        *)
            echo "NG: 端末が見えません。Quest の開発者モード（スマホの Meta Horizon アプリ > デバイス > 開発者モード）と USB ケーブル（データ通信対応）を確認"
            exit 2 ;;
    esac
}

cmd_install() {
    local apk="${1:-$DEFAULT_APK}"
    [ -f "$apk" ] || die "APK がありません: $apk（Unity のメニュー OrdinalScale > Quest MR > 5 で作る）"
    require_device
    echo "APK: $apk"
    echo "SHA-256: $(sha256_of "$apk")（記録票の「APK識別子」に転記）"
    echo "サイズ: $(du -h "$apk" | awk '{print $1}')"
    "$ADB_BIN" install -r "$apk" || die "adb install に失敗しました。表示されたエラー（INSTALL_FAILED_…）を記録票に残してください"
    echo "OK: 導入しました"
}

# $1 = 1 なら剣の測定モードで起動。-S で動いているアプリを止めてから起動する（起動オプションを確実に渡すため）
start_app() {
    local practice="$1" activity
    activity="$("$ADB_BIN" shell cmd package resolve-activity --brief -c android.intent.category.LAUNCHER "$PACKAGE" 2>/dev/null | tr -d '\r' | tail -1)"
    case "$activity" in
        */*)
            if [ "$practice" = 1 ]; then
                "$ADB_BIN" shell am start -S -n "$activity" --ez "$PRACTICE_EXTRA" true >/dev/null || return 1
            else
                "$ADB_BIN" shell am start -S -n "$activity" >/dev/null || return 1
            fi ;;
        *)
            [ "$practice" = 1 ] && { echo "起動する Activity が分からないため、測定モードの起動オプションを渡せません" >&2; return 1; }
            "$ADB_BIN" shell am force-stop "$PACKAGE"
            "$ADB_BIN" shell monkey -p "$PACKAGE" -c android.intent.category.LAUNCHER 1 >/dev/null 2>&1 || return 1 ;;
    esac
    echo "起動: ${activity:-$PACKAGE}$([ "$practice" = 1 ] && echo "（剣の測定モード）")。ヘッドセットを装着して確認"
}

cmd_run() {
    local label="${1:-}" practice=0
    [ -n "$label" ] || die "ラベルを指定してください（例: $0 run q0-smoke）"
    case "${2:-}" in
        "") ;;
        --practice) practice=1 ;;
        *) die "不明な指定: $2（使えるのは --practice だけ）" ;;
    esac
    case "$label" in *[!A-Za-z0-9_-]*) die "ラベルは英数字・-・_ だけにしてください: $label" ;; esac
    require_device
    "$ADB_BIN" shell pm path "$PACKAGE" 2>/dev/null | grep -q package: || die "アプリが未導入です（$0 install）"

    local stamp raw filtered
    stamp="$(date +%Y%m%d-%H%M%S)"
    mkdir -p "$RAW_DIR" "$EVIDENCE_DIR"
    raw="$RAW_DIR/${stamp}_Q5_${label}_logcat_full.txt"
    filtered="$EVIDENCE_DIR/${stamp}_Q5_${label}_ログ.txt"

    "$ADB_BIN" logcat -c
    "$ADB_BIN" logcat -v time Unity:V AndroidRuntime:E DEBUG:E libc:F '*:S' > "$raw" &
    local logcat_pid=$!

    # 非対話の bash では背景ジョブが SIGINT を受けないので、trap で明示的に止める
    trap "kill $logcat_pid 2>/dev/null" INT TERM
    if ! start_app "$practice"; then
        kill "$logcat_pid" 2>/dev/null
        die "アプリを起動できませんでした"
    fi
    echo "ログ採取中（[OrdinalScale] とエラーの行をここに表示）… 確認が終わったら Ctrl-C（アプリは止めない）"
    # Ctrl-C で表示が止まったら logcat も止める（端末が外れて logcat が先に終わっても Ctrl-C まで待つ）
    tail -n +1 -f "$raw" | grep --line-buffered -E "$KEEP_PATTERN"
    kill "$logcat_pid" 2>/dev/null
    wait "$logcat_pid" 2>/dev/null
    trap - INT TERM

    {
        echo "# Quest 実機ログ（[OrdinalScale] の行とエラー・クラッシュの行のみ）"
        echo "# ラベル: $label／採取開始: $stamp／パッケージ: $PACKAGE／剣の測定モード: $([ "$practice" = 1 ] && echo あり || echo なし)"
        echo "# 機種: $(prop ro.product.model)／OS: $(prop ro.build.display.id)"
        echo "# 生ログ（GitHub に入れない）: OrdinalScale/Builds/DeviceLogs/$(basename "$raw")"
        echo
        summarize "$raw"
        echo
        echo "## ログ"
        grep -E "$KEEP_PATTERN" "$raw" || echo "（該当する行なし）"
    } > "$filtered"

    echo
    summarize "$raw"
    echo
    echo "保存: $filtered"
    echo "生ログ: $raw"
}

# 1行目が見つかったものを表示（無ければ「なし」）
first_line() {
    local s
    s="$(grep -F "$1" "$2" | head -1 | sed 's/.*\[OrdinalScale\]/[OrdinalScale]/')"
    echo "${s:-なし}"
}

count() { grep -F -c "$1" "$2" || true; }

# 「最大1.23m/s」などから数値を取り出して 件数・最小・最大・一覧 を出す
speeds() {
    grep -F "$1" "$3" | sed -n "s/.*$2\([0-9][0-9.]*\)m\/s.*/\1/p" | awk '
        { v[NR] = $1; if (NR == 1 || $1 < mn) mn = $1; if (NR == 1 || $1 > mx) mx = $1 }
        END {
            if (NR == 0) { print "なし"; exit }
            s = ""; for (i = 1; i <= NR && i <= 30; i++) s = s (i > 1 ? " " : "") v[i]
            if (NR > 30) s = s " …"
            printf "%d件 最小%.2f 最大%.2f m/s [%s]\n", NR, mn, mx, s
        }'
}

summarize() {
    [ -f "$1" ] || die "ログがありません: $1"
    # logcat の行（「10-24 12:34:56.789 I/Unity …」）だけを数える。保存済みの証拠ファイルの集計欄を数え直さないため
    local f
    f="$(mktemp)"
    grep -E '^[0-9]{2}-[0-9]{2} [0-9]{2}:[0-9]{2}:' "$1" > "$f" || true
    echo "## 集計（記録票へ転記する数。Editor の結果からは推定しない）"
    echo "- 実行環境: $(first_line '実行環境:' "$f")"
    echo "- 敵の配置: $(first_line '敵を正面に配置' "$f")"
    echo "- 剣の調整値: $(first_line '剣の調整値' "$f")"
    echo "- 剣の測定モード: $(first_line '[Q5] 剣の測定モード' "$f")（測定モードの命中 $(count '測定モードの命中' "$f") 回）"
    echo "- 剣の表示/非表示の切り替え: $(count '[Q0] 剣: ' "$f") 回（うち非表示 $(count '[Q0] 剣: 非表示' "$f")）"
    echo "- 剣（Q1）: 命中 $(count '[Q1] 命中 振り#' "$f")／振り $(count '終了 最大' "$f")／空振り $(count '結果=空振り' "$f")／ゆっくり接触 $(count '不命中：ゆっくり接触' "$f")／同じ振りで再接触 $(count '不命中：同じ振り' "$f")／触れ続け $(count '触れ続け（追加の命中なし）' "$f")"
    echo "- 振りの終わり方: 減速 $(count '（減速）' "$f")／追跡の途切れ $(count '（追跡の途切れ' "$f")／位置の飛び $(count '（位置の飛び）' "$f")／フレームの空き $(count '（フレームの間隔の空き）' "$f")"
    echo "- 振りの最大刃先速度: $(speeds '終了 最大' '最大' "$f")"
    echo "- ゆっくり接触の刃先速度: $(speeds '不命中：ゆっくり接触' '刃先' "$f")"
    echo "- 命中時の刃先速度: $(speeds '[Q1] 命中 振り#' '刃先' "$f")"
    echo "- 敵HP（Q2）: 減少 $(count '[Q2] 敵HP' "$f") 回／撃破 $(count '[Q2] 撃破' "$f")／決着後の命中を無視 $(count '決着後の命中を無視' "$f")"
    echo "- 敵の行動（Q3）: 予備動作 $(count '敵の攻撃の予備動作' "$f")／被弾 $(count '敵の攻撃が命中' "$f")／回避 $(count '敵の攻撃を回避' "$f")／敗北 $(count '[Q3] 敗北' "$f")"
    echo "- 2択（Q4）: パネル表示 $(count '2択パネルを表示' "$f")／再挑戦を選択 $(count '2択：再挑戦を選択' "$f")／終了を選択 $(count '2択：終了を選択' "$f")／再挑戦の初期化 $(count '[Q3] 再挑戦' "$f")／Application.Quit $(count 'Application.Quit を呼びます' "$f")"
    echo "- エラー: Unity のエラー/例外 $(grep -E -c 'E/Unity|Exception' "$f" || true) 行／クラッシュ（Java・ネイティブ） $(grep -E -c 'E/AndroidRuntime|F/libc|E/DEBUG|FATAL' "$f" || true) 行"
    rm -f "$f"
}

cmd_running() {
    require_device
    local pid
    pid="$("$ADB_BIN" shell pidof "$PACKAGE" 2>/dev/null | tr -d '\r')"
    if [ -n "$pid" ]; then echo "実行中（pid $pid）"; else echo "停止（プロセスなし）"; fi
}

cmd_stop() {
    require_device
    "$ADB_BIN" shell am force-stop "$PACKAGE"
    echo "停止しました: $PACKAGE"
}

sub="${1:-}"
[ $# -gt 0 ] && shift
case "$sub" in
    summary) [ $# -gt 0 ] || die "ログのパスを指定してください"; summarize "$1" ;;
    check) ADB_BIN="$(find_adb)" || exit 1; cmd_check ;;
    install) ADB_BIN="$(find_adb)" || exit 1; cmd_install "$@" ;;
    run) ADB_BIN="$(find_adb)" || exit 1; cmd_run "$@" ;;
    running) ADB_BIN="$(find_adb)" || exit 1; cmd_running ;;
    stop) ADB_BIN="$(find_adb)" || exit 1; cmd_stop ;;
    *) sed -n '2,17p' "$0" | sed 's/^# \{0,1\}//'; [ -z "$sub" ] || exit 1 ;;
esac
