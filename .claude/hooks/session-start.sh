#!/bin/bash
# Claude Code on the web 用: OrdinalScale.Core（純C#）をビルド・テストできるよう .NET SDK を用意する。
# Unity Editor はクラウドでは動かないため、Unity 依存コードの検証は開発者のローカル Editor で行う。
set -euo pipefail

if [ "${CLAUDE_CODE_REMOTE:-}" != "true" ]; then
  exit 0
fi

if ! command -v dotnet >/dev/null 2>&1; then
  # dot.net のインストールスクリプトはプロキシで遮断されるため Ubuntu 公式アーカイブから入れる
  export DEBIAN_FRONTEND=noninteractive
  apt-get update -qq || true
  apt-get install -y -qq dotnet-sdk-8.0 >/dev/null
fi

export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1
{
  echo 'export DOTNET_CLI_TELEMETRY_OPTOUT=1'
  echo 'export DOTNET_NOLOGO=1'
} >> "${CLAUDE_ENV_FILE:-/dev/null}"

dotnet restore "$CLAUDE_PROJECT_DIR/tools/CoreTests/CoreTests.csproj" --nologo -v q
