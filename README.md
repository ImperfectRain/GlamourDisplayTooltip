# Glamour Display Tooltip

Tiny Dalamud experiment for people who keep hovering gear and thinking, "yeah, but what does it look like?"

Hold the configured modifier while an item tooltip is open and the plugin shows a separate preview panel beside it. Right now it can use:

- the game's item icon as a dependable fallback
- Eorzea Collection images when they have been cached or found
- the game's native Try On render target for a more direct in-game preview

This is still early and a bit scrappy in the honest way. The hard part is not drawing the window, it is reliably mapping XIV item names to Eorzea Collection's gearset images without a nice public API. The plugin has a debug window for that reason.

## Commands

- `/gdt` opens the main preview window
- `/gdt config` opens settings

## Settings

The settings window lets you pick the preview source, preview size, and activation key. The default virtual key is `16`, which treats Shift, Ctrl, or Alt as the modifier.

There are also helper buttons for:

- downloading/caching Eorzea Collection images
- watching the EC download progress
- copying a debug report for a broken item lookup

## Eorzea Collection Notes

EC images are best effort. The plugin tries to build an index from Eorzea Collection gearset pages, then uses the real CDN paths from that index rather than blindly guessing folders. If an item still fails:

1. Hover the item.
2. Open `Debug output`.
3. Press `Refresh`.
4. Press `Copy`.
5. Paste the report into the issue/conversation.

That report includes the item row, slot data, normalized EC names, slug candidates, cache paths, indexed gearset matches, and fallback CDN candidates.

## Building

Requirements are the usual Dalamud plugin basics:

- XIVLauncher and Dalamud installed
- Dalamud dev files available through XIVLauncher
- .NET SDK compatible with `Dalamud.NET.Sdk/15.0.0`

Build with:

```powershell
dotnet build -p:Platform=x64
```

The dev DLL will be at:

```text
GlamourDisplayTooltip/bin/x64/Debug/GlamourDisplayTooltip.dll
```

Add that DLL to Dalamud's dev plugin locations, then enable it from the plugin installer dev tools.

## Status

The plugin is usable, but not polished. EC image lookup and bulk caching are the main moving parts that still need real-world reports. Native Try On preview is intentionally using the game's own render target instead of a custom model renderer, because the custom route got messy fast and was not worth pretending otherwise.
