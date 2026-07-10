#!/usr/bin/env bash
# Roll back Puzzle Signal.unity to the last committed version (git).
# For editor backup restore, use Unity menu:
#   Who Wired This / Signal Calibration / Restore Puzzle Signal Pre-V2 Wiring Backup
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SCENE="Assets/Scenes/Game/Puzzle Signal.unity"

cd "$ROOT"

if [[ ! -f "$SCENE" ]]; then
  echo "Scene not found: $SCENE" >&2
  exit 1
fi

echo "This will discard local changes to:"
echo "  $SCENE"
echo
read -r -p "Continue? [y/N] " confirm
if [[ ! "$confirm" =~ ^[Yy]$ ]]; then
  echo "Aborted."
  exit 0
fi

git checkout -- "$SCENE"
echo "Restored $SCENE from git HEAD."
