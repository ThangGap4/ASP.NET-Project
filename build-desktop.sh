#!/usr/bin/env bash
# Build self-contained Desktop app
# Usage: ./build-desktop.sh [linux-x64|win-x64|osx-x64]
# Default: linux-x64

set -e

RID=${1:-linux-x64}
OUTPUT="./publish/desktop-${RID}"

echo "==> Building QuizAI.Desktop for ${RID}..."
dotnet publish QuizAI.Desktop/QuizAI.Desktop.csproj \
  -c Release \
  -r "$RID" \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -o "$OUTPUT"

echo ""
echo "==> Done! Output: $OUTPUT"
ls -lh "$OUTPUT/QuizAI.Desktop"* 2>/dev/null || ls -lh "$OUTPUT" | head -5
