#!/usr/bin/env bash
# 実機確認の前に Mac で1回だけ行う Unity 側の確認を、まとめて実行する（Claude/docs/quest-device-test.md 6章）。
#
#   tools/quest/prepare_apk.sh            EditMode テスト → 設定検証＋APK ビルド → APK の静的検査
#   tools/quest/prepare_apk.sh --no-tests テストを飛ばす（テストだけ別に済ませた場合）
#
# 前提：Unity Editor でこのプロジェクトを開いていないこと（バッチ実行は同じプロジェクトを同時に開けない）。
# Unity は環境変数 UNITY → Unity Hub の既定の場所（6000.5.10f1）の順に探す。
# 証拠（テスト結果 XML・設定検証とビルドの結果・静的検査）は Claude/reports/evidence/ に保存する。
# Unity の全ログは OrdinalScale/Builds/UnityLogs/（.gitignore 済み）に置き、APK と同じく GitHub に入れない。

set -u

UNITY_VERSION="6000.5.10f1"
PACKAGE="com.gpyoshihiro.ordinalscale"
REPO_ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
PROJECT="$REPO_ROOT/OrdinalScale"
APK="$PROJECT/Builds/Android/OrdinalScale_Quest_dev.apk"
LOG_DIR="$PROJECT/Builds/UnityLogs"
EVIDENCE_DIR="$REPO_ROOT/Claude/reports/evidence"

die() { echo "エラー: $*" >&2; exit 1; }

find_unity() {
    if [ -n "${UNITY:-}" ]; then
        [ -x "$UNITY" ] || die "UNITY=$UNITY は実行できません"
        echo "$UNITY"; return
    fi
    local c
    for c in \
        "/Applications/Unity/Hub/Editor/$UNITY_VERSION/Unity.app/Contents/MacOS/Unity" \
        "$HOME/Unity/Hub/Editor/$UNITY_VERSION/Editor/Unity"; do
        if [ -x "$c" ]; then echo "$c"; return; fi
    done
    die "Unity $UNITY_VERSION が見つかりません。UNITY=<Unity 本体のパス> を指定してください"
}

# Unity 付属の Android 一式（SDK・OpenJDK）の場所
android_player_dir() {
    local dir
    dir="$(cd "$(dirname "$UNITY_BIN")" && pwd)"
    case "$dir" in
        */Unity.app/Contents/MacOS) echo "${dir%/Unity.app/Contents/MacOS}/PlaybackEngines/AndroidPlayer" ;;
        *) echo "$dir/Data/PlaybackEngines/AndroidPlayer" ;;
    esac
}

# Unity 付属の Android SDK の build-tools から、いちばん新しい版のツールを返す
sdk_tool() {
    ls -d "$(android_player_dir)"/SDK/build-tools/*/"$1" 2>/dev/null | sort | tail -1
}

sha256_of() {
    if command -v shasum >/dev/null 2>&1; then shasum -a 256 "$1" | awk '{print $1}'
    else sha256sum "$1" | awk '{print $1}'; fi
}

run_tests=1
case "${1:-}" in
    "") ;;
    --no-tests) run_tests=0 ;;
    *) sed -n '2,11p' "$0" | sed 's/^# \{0,1\}//'; exit 1 ;;
esac

UNITY_BIN="$(find_unity)" || exit 1
[ -d "$PROJECT/Assets" ] || die "Unity プロジェクトがありません: $PROJECT"

stamp="$(date +%Y%m%d-%H%M%S)"
mkdir -p "$LOG_DIR" "$EVIDENCE_DIR"
echo "Unity: $UNITY_BIN"
echo "コミット: $(git -C "$REPO_ROOT" rev-parse --short HEAD)（$(git -C "$REPO_ROOT" rev-parse --abbrev-ref HEAD)）"

# 1. EditMode テスト（結果 XML を証拠にする）
if [ "$run_tests" = 1 ]; then
    xml="$EVIDENCE_DIR/${stamp}_Q5_Unity_EditMode.xml"
    echo "--- 1/3 EditMode テスト（数分かかる）"
    "$UNITY_BIN" -batchmode -nographics -projectPath "$PROJECT" -runTests -testPlatform EditMode \
        -testResults "$xml" -logFile "$LOG_DIR/${stamp}_editmode.log"
    code=$?
    if [ -f "$xml" ]; then
        summary="$(grep -o '<test-run [^>]*>' "$xml" | head -1 | grep -oE '(total|passed|failed|skipped)="[0-9]+"' | tr '\n' ' ')"
        echo "結果: $summary（$xml）"
    fi
    case "$code" in
        0) echo "OK: テストはすべて成功" ;;
        2) die "失敗したテストがあります。XML の failed の項目を Claude へ渡してください" ;;
        *) tail -40 "$LOG_DIR/${stamp}_editmode.log" >&2
           die "Unity がテストを実行できませんでした（終了コード $code）。Unity Editor でこのプロジェクトを開いていれば閉じて再実行。そうでなければ上のログ（コンパイルエラーなど）を Claude へ渡してください" ;;
    esac
fi

# 2. 設定検証＋APK ビルド（メニュー4・5 と同じ。結果のテキストは Unity が evidence に保存する）
echo "--- 2/3 設定検証と APK ビルド（初回は10分以上かかることがある）"
"$UNITY_BIN" -batchmode -projectPath "$PROJECT" -buildTarget Android \
    -executeMethod OrdinalScale.EditorTools.QuestMRSetup.BuildApkFromCommandLine \
    -logFile "$LOG_DIR/${stamp}_build.log"
code=$?
grep -F "[OrdinalScale] Quest 設定検証" "$LOG_DIR/${stamp}_build.log" | head -1
if [ "$code" -ne 0 ] || [ ! -f "$APK" ]; then
    grep -E "error|Error|NG" "$LOG_DIR/${stamp}_build.log" | head -20 >&2
    die "APK のビルドに失敗しました（終了コード $code）。evidence の最新の …_Q0_Androidビルド.txt と上の行を Claude へ渡してください"
fi
echo "OK: APK を作りました（$APK）"

# 3. APK の静的検査（以前の Q1・Q4 と同じ項目）
echo "--- 3/3 APK の静的検査"
report="$EVIDENCE_DIR/${stamp}_Q5_APK静的検査.txt"
# apksigner は java を使う。JAVA_HOME が無ければ Unity 付属の OpenJDK を使う
if [ -z "${JAVA_HOME:-}" ] && [ -x "$(android_player_dir)/OpenJDK/bin/java" ]; then
    export JAVA_HOME="$(android_player_dir)/OpenJDK"
    export PATH="$JAVA_HOME/bin:$PATH"
fi
aapt="$(sdk_tool aapt)"
apksigner="$(sdk_tool apksigner)"
ng=0
{
    echo "Quest Q5 APK 静的検査"
    echo "日時: $stamp／コミット: $(git -C "$REPO_ROOT" rev-parse --short HEAD)"
    echo "対象: OrdinalScale/Builds/Android/OrdinalScale_Quest_dev.apk（GitHub に入れない）"
    echo "SHA-256: $(sha256_of "$APK")"
    echo "サイズ: $(du -h "$APK" | awk '{print $1}')"
    if [ -n "$aapt" ]; then
        badging="$("$aapt" dump badging "$APK" 2>/dev/null)"
        manifest="$("$aapt" dump xmltree "$APK" AndroidManifest.xml 2>/dev/null)"
        check() { if printf '%s\n' "$2" | grep -q "$3"; then echo "OK  $1"; else echo "NG  $1"; ng=$((ng + 1)); fi; }
        check "package=$PACKAGE" "$badging" "package: name='$PACKAGE'"
        check "native-code=arm64-v8a" "$badging" "native-code: 'arm64-v8a'"
        check "起動できる Activity がある" "$badging" "launchable-activity"
        check "OpenXR の権限（org.khronos.openxr.permission.OPENXR）" "$manifest" "org.khronos.openxr.permission.OPENXR"
        check "パススルーの宣言（com.oculus.feature.PASSTHROUGH）" "$manifest" "com.oculus.feature.PASSTHROUGH"
        echo "起動する Activity: $(printf '%s\n' "$badging" | sed -n "s/^launchable-activity: name='\([^']*\)'.*/\1/p" | head -1)"
    else
        echo "NG  aapt が見つからないため構成を確認できない"; ng=$((ng + 1))
    fi
    if [ -n "$apksigner" ]; then
        if "$apksigner" verify "$APK" >/dev/null 2>&1; then echo "OK  署名の検証（apksigner verify）"; else echo "NG  署名の検証（apksigner verify）"; ng=$((ng + 1)); fi
    else
        echo "NG  apksigner が見つからないため署名を確認できない"; ng=$((ng + 1))
    fi
    echo "NG の件数: $ng"
} > "$report"
cat "$report"
[ "$ng" -eq 0 ] || die "静的検査に NG があります（$report）"

echo
echo "準備完了。次は tools/quest/quest_device.sh check → install（手順書 3章）"
echo "コミットするもの：Claude/reports/evidence/ の今回のファイル、新しくできた .meta（git status で確認）。APK と Builds/ はコミットしない"
