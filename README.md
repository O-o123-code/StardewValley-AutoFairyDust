# Auto Fairy Dust

Automatically applies Fairy Dust to processing machines connected via floor path networks. No more manual clicking — just place chests with Fairy Dust nearby and let the mod handle the rest.

If you've used Automate, this will feel very familiar. Configure connectors, rate limits, and visualize your network with the built-in overlay.

## Features

- Automatically applies Fairy Dust to supported machines (Casks, Preserves Jars, Kegs, Crystalariums, etc.)
- Connect machines using floor paths — all vanilla floors supported (Wood Floor, Stone Floor, Crystal Path, etc.)
- Customizable rate limit — control how many Fairy Dusts are used per second
- Per-machine cooldown to prevent over-consumption
- Optional HUD message when Fairy Dust is applied
- Built-in overlay (configurable hotkey) to visualize connections and network groups
- Custom connector items — add any floor/path via config
- Works inside buildings (sheds, barns, coops, etc.)
- Generic Mod Config Menu support (optional)
- Also includes built-in Chinese (Simplified) translation

## Requirements

- Stardew Valley 1.6+
- SMAPI 4.1.0 or later
- Generic Mod Config Menu — optional

## Install

1. Install SMAPI 4.1.0 or later
2. Unzip into `Stardew Valley/Mods/AutoFairyDust/`
3. Launch the game via SMAPI
4. Configure via `config.json` or Generic Mod Config Menu (optional)

## Config

| Setting | Default | Description |
|---|---|---|
| `Enabled` | `true` | Enable or disable automation |
| `MaxPerSecond` | `10` | Max Fairy Dust uses per second |
| `MachineCooldownSeconds` | `1.0` | Cooldown between applications on the same machine |
| `ShowHudMessage` | `true` | Show HUD message when Fairy Dust is applied |
| `ToggleOverlay` | `K` | Hotkey to toggle the connection overlay |
| `ConnectorNames` | `[]` | Floor paths that connect machines (add item IDs via config or GMCM) |

## Known Issues

If Automate is installed, it has its own built-in Fairy Dust automation. With both mods enabled, Fairy Dust consumption may be higher than expected. If you disable this mod but still see Fairy Dust being consumed, it's Automate's built-in feature. To disable it, set `MinMinutesForFairyDust` to `99999999` in Automate's `config.json`.

A warning about this is also shown in the in-game console log and in the mod's config description.

## Build

```bash
dotnet build -c Release
```

Output is in `bin/Release/net8.0/`.

## License

MIT
