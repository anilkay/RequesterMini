#!/usr/bin/env bash
# drive.sh — launch and drive the RequesterMini Avalonia GUI on macOS.
#
# The app is a native Avalonia desktop window; there is no headless mode.
# We drive it by: reading the accessibility tree for element coordinates
# (`inspect`), posting real HID click/type events at those coordinates
# (uidriver, built from uidriver.c), and grabbing PNGs with screencapture.
#
# Requires: macOS, .NET 10 SDK, clang (Command Line Tools), and the
# controlling terminal must hold Accessibility + Screen-Recording
# permission (System Settings › Privacy & Security).
#
# Usage:
#   drive.sh build            compile uidriver (auto-run by other cmds)
#   drive.sh launch           `dotnet run` the app in the background
#   drive.sh inspect          list named UI elements + their point coords
#   drive.sh focus            bring the app window to the front
#   drive.sh click X Y        click at screen point X,Y
#   drive.sh type "STR"       type a UTF-8 string into the focused control
#   drive.sh selectall        Cmd+A
#   drive.sh delete           Backspace
#   drive.sh shot PATH.png    screenshot the whole screen to PATH
#   drive.sh quit             kill the running app
set -euo pipefail
SKILL_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BIN="$SKILL_DIR/uidriver"
APP="RequesterMini"
# Repo root is three levels up from .claude/skills/run-requestermini/.
REPO="$(cd "$SKILL_DIR/../../.." && pwd)"

build_driver() {
  if [ ! -x "$BIN" ] || [ "$SKILL_DIR/uidriver.c" -nt "$BIN" ]; then
    clang "$SKILL_DIR/uidriver.c" -o "$BIN" -framework ApplicationServices
  fi
}

focus() {
  osascript -e "tell application \"System Events\" to tell process \"$APP\" to set frontmost to true"
}

cmd="${1:-}"; shift || true
case "$cmd" in
  build) build_driver ;;
  launch)
    ( cd "$REPO" && exec dotnet run --project src/RequesterMini ) >/tmp/requestermini.log 2>&1 &
    echo "launching ($!); log: /tmp/requestermini.log"
    # Wait for the *window* (not just the process) — the AX window appears a
    # beat after the process starts, and inspect/click fail until it exists.
    for _ in $(seq 1 60); do
      if osascript -e "tell application \"System Events\" to tell process \"$APP\" to get name of window 1" >/dev/null 2>&1; then
        echo "up"; exit 0
      fi
      sleep 1
    done
    echo "did not start; see /tmp/requestermini.log" >&2; exit 1
    ;;
  inspect)
    # NOTE: `entire contents of window 1` is unreliable on Avalonia — it
    # returns an empty list because AX peers are built lazily and the flat
    # enumeration doesn't force them. We descend the tree by hand instead.
    osascript <<'EOF'
on walk(el, depth, acc)
  if depth > 30 then return acc
  tell application "System Events"
    set kids to {}
    try
      set kids to UI elements of el
    end try
    repeat with k in kids
      set cl to ""
      set nm to ""
      set px to ""
      try
        set cl to class of k as text
      end try
      try
        set nm to name of k as text
      end try
      try
        set p to position of k
        set s to size of k
        set px to (item 1 of p as text) & "," & (item 2 of p as text) & " " & (item 1 of s as text) & "x" & (item 2 of s as text)
      end try
      if (nm is not "") or (cl is in {"radio button", "pop up button", "text field", "button", "tab"}) then
        set end of acc to cl & " | " & nm & " | " & px
      end if
      set acc to my walk(k, depth + 1, acc)
    end repeat
  end tell
  return acc
end walk

tell application "System Events" to tell process "RequesterMini"
  set res to my walk(window 1, 0, {})
end tell
set text item delimiters to linefeed
return res as text
EOF
    ;;
  focus) focus ;;
  click) build_driver; focus; sleep 0.3; "$BIN" click "$1" "$2" ;;
  type)  build_driver; focus; "$BIN" type "$1" ;;
  selectall) build_driver; "$BIN" selectall ;;
  delete)    build_driver; "$BIN" delete ;;
  shot)  screencapture -x -o "$1"; echo "$1" ;;
  quit)  pkill -x "$APP" 2>/dev/null || true; echo "quit" ;;
  *)
    echo "usage: drive.sh {build|launch|inspect|focus|click X Y|type STR|selectall|delete|shot PATH|quit}" >&2
    exit 1 ;;
esac
