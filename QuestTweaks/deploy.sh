#!/usr/bin/env bash
set -euo pipefail

# ---------------------------------------------------------------------------
# deploy.sh — Build QuestTweaks and install it into 7 Days to Die
# Usage:
#   ./deploy.sh           # Release build (default)
#   ./deploy.sh debug     # Debug build
# ---------------------------------------------------------------------------

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
MOD_NAME="QuestTweaks"
CONFIG="${1:-Release}"

# Detect OS and set game directory
case "$(uname -s)" in
  Darwin)
    GAME_DIR="$HOME/Library/Application Support/Steam/steamapps/common/7 Days To Die"
    ;;
  MINGW*|MSYS*|CYGWIN*|Linux)
    # Git Bash / MSYS2 on Windows, or WSL
    if [ -d "/d/SteamLibrary/steamapps/common/7 Days To Die" ]; then
      GAME_DIR="/d/SteamLibrary/steamapps/common/7 Days To Die"
    elif [ -d "/c/Program Files (x86)/Steam/steamapps/common/7 Days To Die" ]; then
      GAME_DIR="/c/Program Files (x86)/Steam/steamapps/common/7 Days To Die"
    else
      echo "ERROR: Could not find 7 Days To Die install directory." >&2
      echo "Set GAME_DIR env variable and re-run." >&2
      exit 1
    fi
    ;;
  *)
    echo "ERROR: Unsupported OS '$(uname -s)'. Set GAME_DIR env variable and re-run." >&2
    exit 1
    ;;
esac

# Allow override via env variable
GAME_DIR="${GAME_DIR}"
MODS_DIR="$GAME_DIR/Mods"
DEST="$MODS_DIR/$MOD_NAME"

echo "==> Building $MOD_NAME ($CONFIG)…"
dotnet build "$SCRIPT_DIR/$MOD_NAME.csproj" -c "$CONFIG" --nologo -v minimal

echo "==> Installing to: $DEST"
mkdir -p "$DEST"

# DLL goes in mod root — game loader scans the folder directly
cp -f "$SCRIPT_DIR/bin/$MOD_NAME.dll" "$DEST/$MOD_NAME.dll"

# Mod manifest
cp -f "$SCRIPT_DIR/ModInfo.xml" "$DEST/ModInfo.xml"

# Patches folder (XML patches)
if [ -d "$SCRIPT_DIR/Patches" ]; then
  mkdir -p "$DEST/Patches"
  cp -f "$SCRIPT_DIR/Patches/"* "$DEST/Patches/"
fi

echo "==> Done."
echo "    Installed: $DEST"
echo "    DLL size:  $(du -sh "$DEST/$MOD_NAME.dll" | cut -f1)"
echo ""
echo "Remember: Launch the game with EAC disabled."
