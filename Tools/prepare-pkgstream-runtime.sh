#!/usr/bin/env bash
set -euo pipefail

TARGET="$1"
OUT="$2"
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
PKG="$ROOT/PKGStream"
NODE_VERSION="${PKGSTREAM_NODE_VERSION:-22.23.3}"
CACHE="$ROOT/.pkgstream-build-cache"
WORK="$CACHE/$TARGET"

mkdir -p "$WORK" "$OUT/PKGStream"
rm -rf "$OUT/PKGStream"
mkdir -p "$OUT/PKGStream"

case "$TARGET" in
  win-x64)   NODE_OS=win;    NODE_ARCH=x64;      EXT=zip ;;
  win-x86)   NODE_OS=win;    NODE_ARCH=x86;      EXT=zip ;;
  win-arm)   NODE_OS=win;    NODE_ARCH=x86;      EXT=zip ;;
  win-arm64) NODE_OS=win;    NODE_ARCH=arm64;    EXT=zip ;;
  linux-x64) NODE_OS=linux;  NODE_ARCH=x64;      EXT=tar.xz ;;
  linux-arm) NODE_OS=linux;  NODE_ARCH=armv7l;   EXT=tar.xz ;;
  linux-arm64) NODE_OS=linux; NODE_ARCH=arm64;   EXT=tar.xz ;;
  osx-x64)   NODE_OS=darwin; NODE_ARCH=x64;      EXT=tar.gz ;;
  osx-arm64) NODE_OS=darwin; NODE_ARCH=arm64;    EXT=tar.gz ;;
  *) echo "Unsupported PKGStream runtime target: $TARGET" >&2; exit 1 ;;
esac

ARCHIVE="node-v$NODE_VERSION-$NODE_OS-$NODE_ARCH.$EXT"
URL="https://nodejs.org/dist/v$NODE_VERSION/$ARCHIVE"
ARCHIVE_PATH="$WORK/$ARCHIVE"

if [ ! -f "$ARCHIVE_PATH" ]; then
  curl -fL --retry 3 --retry-delay 1 "$URL" -o "$ARCHIVE_PATH"
fi

rm -rf "$WORK/extracted"
mkdir -p "$WORK/extracted"

if [ "$EXT" = "zip" ]; then
  unzip -q "$ARCHIVE_PATH" -d "$WORK/extracted"
else
  tar -xf "$ARCHIVE_PATH" -C "$WORK/extracted"
fi

NODE_BIN="$(find "$WORK/extracted" -type f \( -name node -o -name node.exe \) | head -n 1)"
if [ -z "$NODE_BIN" ]; then
  echo "Node binary not found in $ARCHIVE" >&2
  exit 1
fi

mkdir -p "$OUT/PKGStream/node"
cp "$NODE_BIN" "$OUT/PKGStream/node/$(basename "$NODE_BIN")"
chmod +x "$OUT/PKGStream/node/$(basename "$NODE_BIN")" 2>/dev/null || true

if [ ! -d "$PKG/node_modules/@mary/rar" ]; then
  (cd "$PKG" && npm install --omit=dev)
fi

cp "$PKG/package.json" "$OUT/PKGStream/"
cp "$PKG/.npmrc" "$OUT/PKGStream/" 2>/dev/null || true
cp -R "$PKG/src" "$OUT/PKGStream/"
cp -R "$PKG/node_modules" "$OUT/PKGStream/"

rm -rf "$OUT/PKGStream/test"
