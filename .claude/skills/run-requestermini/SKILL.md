---
name: run-requestermini
description: Build, launch, and drive the RequesterMini desktop app (Avalonia HTTP client) on macOS. Use to run, start, screenshot, or interact with the GUI — click tabs, type into the request body, verify UI behavior like live JSON validation.
---

# Run RequesterMini

RequesterMini is a native **Avalonia + ReactiveUI desktop GUI** (.NET 10).
There is no headless mode and its accessibility peers ignore AppleScript
AXPress, so we drive it with **real HID events**: read element coordinates
from the AX tree, post mouse/keyboard events with a tiny CoreGraphics tool
(`uidriver.c`), and grab PNGs with `screencapture`. All of that is wrapped
by **`drive.sh`** — the one entry point you use.

Paths below are relative to the repo root (`<unit>/`). The driver lives at
`.claude/skills/run-requestermini/`.

> **macOS only.** This app is verified on macOS (darwin). The controlling
> terminal must hold **Accessibility** and **Screen Recording** permission
> (System Settings › Privacy & Security) — without them clicks/typing are
> silently dropped and screenshots come back black.

## Prerequisites

- macOS with the **.NET 10 SDK** (`dotnet --version` → `10.0.101` verified).
- **clang** (Xcode Command Line Tools) to build `uidriver`.
  `drive.sh` compiles it on first use; nothing to install manually.
- No `cliclick`, no Python `Quartz`, no Swift needed (see Gotchas — those
  paths were tried and don't work here).

## Build

```bash
dotnet build RequesterMini.slnx      # whole solution; ~2s incremental
```

Warnings are expected and benign: `IL2026` (ReactiveUI reflection under
trimming) and `NU1903` (Tmds.DBus.Protocol advisory). Build still succeeds.

## Run — agent path (drive.sh)

This is the path to use. Launch, inspect for coordinates, then click/type.

```bash
D=.claude/skills/run-requestermini/drive.sh

$D launch                     # dotnet run in background; waits until window is up
$D inspect                    # list named UI elements + their point coords
$D click 727 225              # click at a screen point
$D type '{"a": 1 "b": 2}'     # type UTF-8 into the focused control (layout-independent)
$D selectall                  # Cmd+A (e.g. to clear a field before retyping)
$D delete                     # Backspace
$D shot /tmp/shot.png         # screenshot whole screen -> PNG, then Read it
$D quit                       # kill the app
```

**`inspect` is how you get coordinates** — never hardcode them, the window
can move. It prints `class | name | X,Y WxH`. Click the **center** of a
target: `X + W/2`, `Y + H/2`. Coordinates are screen points, the same units
`uidriver click` consumes, so feed them straight through.

Key elements (names as of this writing):
- Tabs are `radio button`s: `Params`, `Headers`, `Body`.
- Body-type selector: the `pop up button` at ~`471,251` (defaults to `Json`).
- Request-body editor: the `text field` at ~`471,293 820x158`.

### Verified end-to-end flow (live JSON validation)

This exact sequence was run to verify the skill (invalid then valid JSON):

```bash
D=.claude/skills/run-requestermini/drive.sh
$D launch
$D click 727 225                 # Body tab (radio "Body" 686,201 82x48 -> center)
sleep 0.5
$D click 881 372                 # request-body editor (text field 471,293 820x158)
sleep 0.3
$D type '{"a": 1 "b": 2}'        # invalid JSON (missing comma)
sleep 1.2                        # WAIT > 0.4s: validation is debounced 400ms
$D shot /tmp/invalid.png
# -> red border on the editor + "⚠ Invalid JSON — Line 1, Col 9: '"' is
#    invalid after a value. Expected either ',', '}', or ']'."
$D selectall; $D delete
$D type '{"a": 1, "b": 2}'       # valid -> error clears, border back to normal
sleep 1.2
$D shot /tmp/valid.png
$D quit
```

**Always `Read` the screenshot you take.** A black frame = missing Screen
Recording permission, not a passing run.

## Run — human path

```bash
dotnet run --project src/RequesterMini
```

Opens the window and blocks until you Ctrl-C. Fine for eyeballing, but you
can't drive it this way — use `drive.sh` for any scripted interaction.

## Test

```bash
dotnet test RequesterMini.slnx                                   # all projects
dotnet test RequesterMini.slnx --filter "FullyQualifiedName~Foo"  # one test
```

Unit tests are a sanity check; they do not exercise the GUI. Reusable logic
lives in the `src/*` class libraries with paired `tests/*.Tests` projects.

## Gotchas

- **AppleScript AXPress does nothing to Avalonia controls.** `click e` on a
  `radio button` (a tab) via System Events returns success but doesn't switch
  the tab. You must post a real mouse click at coordinates — that's the whole
  reason `uidriver.c` exists.
- **`entire contents of window 1` returns an empty list.** Avalonia builds AX
  peers lazily and the flat enumeration doesn't force them (`count of entire
  contents` → 0 while `count of groups` → 1). `drive.sh inspect` works around
  this by **recursively descending** `UI elements` by hand. Don't switch it
  back to `entire contents`.
- **AX coordinates are already screen points** — same space CoreGraphics
  mouse events use. No Retina/pixel (÷2 or ×scale) conversion needed; the
  `position`/`size` you read go straight into `uidriver click`.
- **Validation is debounced 400 ms.** After typing, `sleep 1.2` before you
  screenshot or the error text won't have rendered yet (the app throttles the
  `Body`/`SelectedBodyType` observable by 400ms before parsing).
- **`type` uses `CGEventKeyboardSetUnicodeString`** — it inserts literal
  characters regardless of keyboard layout, so `{`, `"`, `:` come out right
  without worrying about Turkish/US layouts or dead keys.
- The app auto-loads a saved request on launch (GET jsonplaceholder), so the
  history list and a prior response may already be populated. Not a bug.

## Troubleshooting

- **`inspect` prints nothing / clicks land nowhere** → the window isn't
  frontmost or AX isn't ready. `drive.sh focus`, `sleep 2`, retry. Every
  click/type command already calls `focus` first.
- **Screenshot is all black** → grant the terminal **Screen Recording** in
  System Settings › Privacy & Security, then restart the terminal.
- **Clicks/typing do nothing but no error** → grant **Accessibility** to the
  same terminal, restart it.
- **`swiftc` fails with `redefinition of module 'SwiftBridging'` / SDK
  mismatch** → don't use Swift; the driver is C compiled with `clang … 
  -framework ApplicationServices`, which works with the Command Line Tools.
- **App won't start** → check `/tmp/requestermini.log` (where `drive.sh
  launch` redirects output).
