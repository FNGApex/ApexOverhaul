#!/usr/bin/env bash
set -euo pipefail

# ---------------------------------------------------------------------------
# build.sh — Build QuestTweaks (compile only, no deploy)
# Usage:
#   ./build.sh           # Release build (default)
#   ./build.sh debug     # Debug build
# ---------------------------------------------------------------------------

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
MOD_NAME="QuestTweaks"
CONFIG="${1:-Release}"

echo "==> Building $MOD_NAME ($CONFIG)…"
dotnet build "$SCRIPT_DIR/$MOD_NAME.csproj" -c "$CONFIG" --nologo -v minimal

echo "==> Done. DLL at: $SCRIPT_DIR/bin/$MOD_NAME.dll"
