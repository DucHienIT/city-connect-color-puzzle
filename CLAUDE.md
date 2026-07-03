# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

A Unity 6 (`6000.3.9f1`) mobile puzzle game — Flow Free–style "connect same-colored dots" with a city theme (working title **"Tiny Town Roads"**, namespace `TinyTownRoads`). Built on URP (2D renderer) + the new Input System. The design document is [docs/GAME_SPEC.md](docs/GAME_SPEC.md) (Vietnamese); its checklist items 1–13 are implemented, 14–16 (rewarded ads, IAP, device builds) are not.

Work happens on the `dev` branch; `main` is the PR target.

## Architecture

All game code is under `Assets/_Project/Scripts/`. The core principle: **everything is built at runtime from code** — no prefabs and no image/audio assets. To play: open `Assets/_Project/Scenes/Game.unity` (first scene in Build Settings) and press Play.

- `Gameplay/GameBootstrap.cs` — the only component placed in the scene. Creates the EventSystem, AudioManager, UIController and GameManager in `Awake`.
- **Pure-logic core, no MonoBehaviours** (`Core/`): `GridModel` (immutable board), `PathManager` (path state + all drawing rules from spec §2.2; views subscribe to its `PathChanged` event), `Solver` (backtracking, counts solutions for uniqueness validation and powers hints), `LevelGenerator` (random-walk grid fill), `LevelData`/`LevelLoader` (JSON via `JsonUtility` from `Resources/Levels`).
- **Views/input** (`Gameplay/`): `GridView` (board + world↔grid conversion, cell size 1, board centered at origin), `NodeView` (procedural "building"), `PathRenderer` (pooled sprite segments), `InputController` → `PathDrawer` (pointer drags → L-stepped path edits; a "move" = a completed drag that changed any path), `GameManager` (level lifecycle, undo stack of pre-drag snapshots, win → stars → save).
- **UI** (`UI/`): `UIFactory` builds uGUI from code — legacy `Text` with `LegacyRuntime.ttf`, **deliberately not TMP** (avoids TMP resource import); `SpriteFactory` generates all sprites (rounded rects, circles, stars) as white textures tinted per use. Screens are plain classes toggled by `UIController`.
- **Systems**: `SaveSystem` (PlayerPrefs, keys prefixed `ttr_`; stars per levelId, daily hint allowance), `AudioManager` (all SFX/music synthesized with `AudioClip.Create` — draw blips rise in pitch with path length).
- Stars: 3 = moves ≤ pair count, 2 = within +2, else 1. A level unlocks when the previous one has ≥1 star.

## Levels

JSON files `Assets/_Project/Resources/Levels/level_NNN.json` (24 shipped, 5x5→9x9, all `requireFullCoverage: true`, all solver-verified unique-solution). Generate/validate via **Tools → Tiny Town Roads → Level Generator** in the editor.

**Hard-won generation parameters** (unique solutions are vanishingly rare otherwise): cap random-walk path length and use many pairs — 5x5/6x6 uncapped, 7x7 → maxLen 9 with 6–9 pairs, 8x8 → maxLen 7 with 8–11 pairs, 9x9 → maxLen 7 with 10–12 pairs **and 2 obstacles** (obstacles break symmetry; without them 9x9 uniqueness is nearly unreachable). If generation fails, vary the seed before loosening parameters.

## Development Workflow

Unity Editor–driven; no standalone build/test CLI. Options:

- **Unity MCP** (`com.coplaydev.unity-mcp`) is installed — when connected, use it (see `unity-mcp-skill`) to edit scenes, run tests, and read console output instead of hand-editing `.unity` YAML.
- **Fast compile check without Unity** (works while the editor is open; catches all C# errors — reference list comes from the Unity-generated csproj):
  ```bash
  UNITY="/c/Program Files/Unity/Hub/Editor/6000.3.9f1/Editor/Data"
  grep -oE '<HintPath>[^<]+</HintPath>' Assembly-CSharp.csproj | sed 's/<[^>]*>//g' > /tmp/refs.txt
  REFS=""; while IFS= read -r p; do REFS="$REFS -r:\"$p\""; done < /tmp/refs.txt
  SRC=$(find Assets/_Project/Scripts -name "*.cs" ! -path "*/Editor/*" | sed 's/.*/"&"/' | tr '\n' ' ')
  eval "\"$UNITY/NetCoreRuntime/dotnet.exe\" \"$UNITY/DotNetSdkRoslyn/csc.dll\" -nologo -target:library \
    -nostdlib -noconfig -out:/tmp/Runtime.dll $REFS $SRC"
  ```
  (Editor scripts: add `-define:UNITY_EDITOR -r:"$UNITY/Managed/UnityEditor.dll" -r:/tmp/Runtime.dll`.)
- Batch mode (editor must be closed): `Unity.exe -batchmode -projectPath . -runTests -testPlatform EditMode -testResults results.xml -quit`.

Every asset file needs its paired `.meta`. When creating files outside the editor, either let Unity generate metas on next focus, or write them by hand — but `GameBootstrap.cs` (`b0075712a9c14e0f8a3d2c4b1e5f6a01`) and `Game.unity` (`c1186823b0d25f1a9b4e3d5c2f6a7b02`) have **fixed GUIDs referenced by the scene/build settings — never regenerate those two**.

## Third-Party Assets (do not modify)

- **DOTween / DOTween Pro** (`Assets/Plugins/Demigiant/`) — used for all animations (node pulses, star pops, popup scale, `DOVirtual.DelayedCall`).
- **Toony Colors Pro 2** (`Assets/JMO Assets/`) — stylized shading, currently unused by the game.
- **Layer Lab GUI Pro-CasualGame** (`Assets/Layer Lab/`) — UI kit, currently unused (UI is procedural); candidate for a future art pass.

## Input

`activeInputHandler` is **new Input System only** — never use legacy `UnityEngine.Input`. Gameplay reads `Pointer.current` directly (`InputController`); UI uses `InputSystemUIInputModule` (created by bootstrap). Legal note from the spec: don't copy names/art/branding from any existing app.
