# Tree Position Fix

A RimWorld 1.6 mod that shifts tree rendering from the bottom edge to the center of their cell.

## The Problem

In vanilla RimWorld, trees are drawn aligned to the bottom of their tile. This is barely noticeable in dense forests, but stands out when placing trees deliberately — along paths, in parks, or around buildings.

## The Fix

This mod nudges tree rendering upward so trees sit centered on their tile. Placement ghost and shadows are adjusted to match. Only trees are affected — grass, bushes, and other plants remain unchanged.

## Installation

1. Subscribe to [Harmony](https://steamcommunity.com/workshop/filedetails/?id=2009463077) on Steam
2. Subscribe to this mod on Steam (or copy to `RimWorld/Mods/`)
3. Enable in mod list — load after Harmony and Core

Safe to add or remove mid-save.

## Building from Source

1. Open `Source/CenterTrees/CenterTrees.csproj` in Visual Studio
2. Update `<RimWorldDir>` and `<HarmonyDir>` paths to match your system
3. Build (Release mode)
4. DLL is copied to `Assemblies/` automatically

## Technical Details

- Harmony Prefix patch on `Plant.Print()`
- XML patch adds `drawOffset` to `TreeBase` for placement ghost
- Pure visual — no gameplay changes

## Compatibility

- RimWorld 1.6
- Works with most mods
- May conflict with mods that also patch `Plant.Print()`

## License

[MIT](LICENSE)
