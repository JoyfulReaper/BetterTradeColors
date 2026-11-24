# Building Better Trade Colors

Build from the repository root. The source project defaults to RimWorld 1.7.

| Game version | Target | Game references | Harmony | Loaded assembly folder |
| --- | --- | --- | --- | --- |
| 1.7 | .NET 10 (`net10.0`) | `Assembly-CSharp.dll`, `UnityStubs.dll` in the game directory | 3, from `1.7/Assemblies` | `1.7/Assemblies` |
| 1.6 | .NET Framework 4.8 (`net48`) | `Assembly-CSharp.dll` and UnityEngine CoreModule, IMGUIModule, and TextRenderingModule in `RimWorldWin64_Data/Managed` | 2, from `1.6/Assemblies` | `Common/Assemblies` |

## Requirements

- .NET 10 SDK, or a newer SDK with the .NET 10 targeting pack.
- A RimWorld installation for the version being built.
- The [Harmony mod](https://github.com/pardeike/HarmonyRimWorld), including its assemblies for that game version. Enable Harmony before Better Trade Colors.
- For a 1.6 rebuild, the .NET Framework 4.8 developer/targeting pack.

The project references installed assemblies directly. It adds no NuGet dependencies to the mod. The decompiled reference repositories are used for API inspection only; they are not compiled or distributed.

## RimWorld 1.7

```powershell
dotnet build Source/BetterTradeColors.csproj -c Release
```

The default paths match the original developer's Steam library. Override both directories for another installation:

```powershell
dotnet build Source/BetterTradeColors.csproj -c Release `
  -p:RimWorldDir="D:/SteamLibrary/steamapps/common/RimWorld" `
  -p:HarmonyDir="D:/SteamLibrary/steamapps/workshop/content/294100/2009463077"
```

`HarmonyDir` is the Harmony mod's root directory, not its `Assemblies` subdirectory. The build writes `1.7/Assemblies/BetterTradeColors.dll` directly. All installed game and Harmony references have `Private=false` to prevent redistribution.

The settings UI uses RimWorld's `Widgets` and `Listing_Standard`, with geometry and colors from `UnityStubs.dll`. The native widget APIs also compile on 1.6 with its Unity assemblies. Harmony registers the existing patch targets by passing the mod assembly explicitly. Both patched overloads retain their original signatures in 1.7:

- `RimWorld.GenLabel.ThingLabel(Thing, int, bool, bool)`
- `Verse.GenText.Truncate(string, float, Dictionary<string, string>)`

## RimWorld 1.6

The shipped `Common/Assemblies/BetterTradeColors.dll` remains unchanged from version 0.2.0. `LoadFolders.xml` loads only `Common` for 1.6 and only `1.7` for 1.7, avoiding duplicate or incompatible assemblies.

To compile the shared source against a separate 1.6 installation and keep the shipped DLL in place:

```powershell
dotnet build Source/BetterTradeColors.csproj -c Release `
  -p:RimWorldVersion=1.6 `
  -p:RimWorldDir="D:/Games/RimWorld-1.6" `
  -p:HarmonyDir="D:/SteamLibrary/steamapps/workshop/content/294100/2009463077" `
  -p:OutputPath="../Tests/obj/Phase1Release16/"
```

`RimWorldManagedDir` can override the 1.6 managed-reference directory directly. For an experimental 1.6 in-game test, use a separate staging copy of the mod. Put the rebuilt DLL under its `Common/Assemblies` folder and copy `1.7/Languages` to its `Common/Languages` folder so the 1.6 load mapping finds the new strings.

## Compatibility checks

The standalone check program patches the installed 1.7 game assembly using its matching Harmony mod. It uses temporary settings files and in-memory fixtures. Build and run from the repository root with an isolated output directory so the checks exercise the new assembly without replacing either packaged DLL.

```powershell
dotnet build Tests/CompatibilityChecks.csproj -c Release `
  -p:OutputPath="../Tests/obj/Phase1Release17/"
dotnet Tests/obj/Phase1Release17/CompatibilityChecks.dll `
  "H:/SteamLibrary/steamapps/common/RimWorld" `
  "H:/SteamLibrary/steamapps/workshop/content/294100/2009463077"
```

For other installations, pass `RimWorldDir` and `HarmonyDir` as build properties and supply those directories as the two runtime arguments.

The checks exercise default colors, toggles and priority, threshold boundaries for apparel and other items, custom and Normal colors, cached tag reuse and refresh, defaults and validation, Scribe round trips, and missing or unsupported quality input. They also exercise the existing truncation patch and inspect the packaged DLLs and load folders.

## Phase 1 verification

- **1.7 Release build and standalone checks:** Passed against RimWorld **1.7.5014 rev1195** and Harmony 3. The Release build completed with zero warnings and errors.
- **1.6 Release compile:** Passed with zero warnings and errors against [Krafs.Rimworld.Ref 1.6.4871](https://www.nuget.org/packages/Krafs.Rimworld.Ref/1.6.4871), the .NET Framework 4.8 targeting pack, and installed Harmony 2. The compile used existing local build-only reference assemblies. In-game parity remains unverified.
- **Packaging:** The Release outputs are in ignored `Tests/obj` folders. Each version still selects one mod DLL. The existing packaged 1.6 and 1.7 DLLs were preserved.

## Manual in-game checklist

Use the 1.7 Release DLL from `Tests/obj/Phase1Release17` in a staging copy of the mod. Include `1.7/Languages` and enable Harmony.

1. Open the settings from the main menu and from a loaded colony. At normal and smaller window sizes or higher UI scale, scroll to every color and Reset. Check field focus, swatches, labels, and clipping.
2. Verify the defaults with a fresh settings file and with a pre-Phase 1 file. Check colored and uncolored quality tiers, correct taint → damage → quality priority, and unchanged stack display and sorting.
3. Toggle each feature on and off. Test tainted, damaged apparel with quality and damaged non-apparel items. Check 49%, 50%, and the exact boundary of a custom threshold.
4. Edit a quality color and both condition colors. Set a Normal color, then clear it. Check live swatches, rejection of malformed hex, and updated labels after closing settings and returning to trade, caravan, inventory, and inspect views.
5. Restart the game and verify persistence. Reset defaults, close the window, and restart again to confirm the original colors and toggles persist.
6. Check narrow columns and labels containing mixed rich text with the listed UI mods. Record truncation or stale-label issues for Phase 2.
7. If testing the experimental 1.6 build, repeat these checks in a separate 1.6 installation before considering packaging it.

Standalone checks use stub font measurement. In-game layout, rich-text rendering, and third-party UI behavior still require manual validation. See [docs/PLAN.md](docs/PLAN.md) for the truncation and cache work deferred to Phase 2.
