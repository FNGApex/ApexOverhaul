#!/usr/bin/env bash
set -euo pipefail

# ---------------------------------------------------------------------------
# deploy_mac.sh — Build NetworkCrafting and install it into 7 Days to Die
# Usage:
#   ./deploy_mac.sh           # Release build (default)
#   ./deploy_mac.sh debug     # Debug build
# ---------------------------------------------------------------------------

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
MOD_NAME="NetworkCrafting"
CONFIG="${1:-Release}"

GAME_DIR="$HOME/Library/Application Support/Steam/steamapps/common/7 Days To Die"
MODS_DIR="$GAME_DIR/Mods"
DEST="$MODS_DIR/$MOD_NAME"

echo "==> Building $MOD_NAME ($CONFIG)…"
dotnet build "$SCRIPT_DIR/$MOD_NAME.csproj" -c "$CONFIG" --nologo -v minimal

echo "==> Installing to: $DEST"
mkdir -p "$DEST/Harmony"
mkdir -p "$DEST/Config"

# DLL
cp "$SCRIPT_DIR/Harmony/$MOD_NAME.dll" "$DEST/Harmony/$MOD_NAME.dll"

# Mod manifest
cp "$SCRIPT_DIR/ModInfo.xml" "$DEST/ModInfo.xml"

# All Config files (xml, txt, etc.)
cp "$SCRIPT_DIR/Config/"* "$DEST/Config/"

echo "==> Done."
echo "    Installed: $DEST"
echo "    DLL size:  $(du -sh "$DEST/Harmony/$MOD_NAME.dll" | cut -f1)"
