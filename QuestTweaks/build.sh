#!/bin/bash
# Build QuestTweaks mod
set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
cd "$SCRIPT_DIR"

echo "[QuestTweaks] Building..."
dotnet build -c Release

echo "[QuestTweaks] Build complete. DLL at: bin/QuestTweaks.dll"
