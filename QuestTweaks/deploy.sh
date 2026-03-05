#!/bin/bash
# Build and deploy QuestTweaks to 7 Days to Die Mods folder
set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
GAME_MODS="/d/SteamLibrary/steamapps/common/7 Days To Die/Mods"
MOD_DIR="$GAME_MODS/QuestTweaks"

cd "$SCRIPT_DIR"

echo "[QuestTweaks] Building..."
dotnet build -c Release

echo "[QuestTweaks] Deploying to $MOD_DIR"
mkdir -p "$MOD_DIR"

cp -f "$SCRIPT_DIR/ModInfo.xml" "$MOD_DIR/"
cp -f "$SCRIPT_DIR/bin/QuestTweaks.dll" "$MOD_DIR/"

echo "[QuestTweaks] Deployed successfully."
echo "  $MOD_DIR/ModInfo.xml"
echo "  $MOD_DIR/QuestTweaks.dll"
echo ""
echo "Remember: Launch the game with EAC disabled."
