# Steam Input for Kerbal Space Program

Kerbal Space Program ships with a Steam controller plugin that switches the controller between six
layouts (Steam calls them *action sets*): Menu, Flight, Docking, Editor, Map and EVA. Since KSP 1.6 it
picks the wrong one in maneuver mode (it falls back to the menu layout, see
[KSP issue #22165](https://bugs.kerbalspaceprogram.com/issues/22165)), and it knows nothing of the
features added since, such as EVA construction.

This mod replaces that plugin. It keeps the same six action sets, so it works with any layout made for
KSP in Steam, and only fixes which one is active:

| What you are doing | Action set |
|---|---|
| Main menu, space center, its buildings, pause menu | Menu |
| Flying (maneuver mode included), IVA in a seat | Flight |
| Docking mode | Docking |
| VAB, SPH, mission builder | Editor |
| Map view (from flight, docking or EVA), tracking station | Map |
| EVA, EVA construction, FreeIVA out of the seat | EVA |

Replacing the stock plugin also brings back joystick support in the Steam version of the game.

**How this was made.** This mod was written with Claude (Anthropic), then reviewed and validated by a
human. I say it upfront because work done with an AI deserves a closer look, and because some will
prefer to stop here. The code is small: one class per detected situation, under `Src/ContextDaemons/`.

## Installation

1. Extract the archive into `GameData/` of your KSP install: you get `GameData/KSPModSteamInput/`.
2. Start KSP from Steam.
3. In Steam, open the controller settings of Kerbal Space Program and pick a layout. A layout for the
   Steam Controller (2025) is built in
   [KSP-Util-SteamInputConfig](https://github.com/lhervier/KSP-Util-SteamInputConfig).

There is nothing to copy into Steam's folders.

If you used **SteamInputExtended** before, remove it from `GameData/`, and remove the
`game_actions_220200.vdf` file it asked you to put in Steam's `controller_config` folder: it hides the
stock action sets.

## Settings

`GameData/KSPModSteamInput/settings.cfg` sets the log level (`Info` by default; `Debug` or `Trace`
detail the detected situations in `KSP.log`, on lines starting with `[KSPModSteamInput]`).

## Building

Requires the .NET SDK, and `KSPDIR` set to a KSP install (for the game assemblies).

```
build.bat      (Windows)
./build.sh     (Linux)
```

The archive is written to `Release/KSPModSteamInput.zip`; `install.bat` / `install.sh` extract it into
`$KSPDIR/GameData`.

## License

MIT — see `LICENSE`.
