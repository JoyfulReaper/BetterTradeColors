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

The settings UI uses RimWorld's `Widgets` and `Listing_Standard`, with geometry and colors from `UnityStubs.dll`. The native widget APIs also compile on 1.6 with its Unity assemblies. Harmony registers the existing patch targets by passing the mod assembly explicitly. The shared source uses these patch targets:

- `RimWorld.GenLabel.ThingLabel(Thing, int, bool, bool)`
- `Verse.GenText.Truncate(string, float, Dictionary<string, string>)`
- `Verse.GenText.Truncate(TaggedString, float, Dictionary<string, TaggedString>)`
- `Verse.Text.ClampTextWithEllipsis(Rect, string)`

## RimWorld 1.6

The shipped `Common/Assemblies/BetterTradeColors.dll` remains unchanged from version 0.2.0. `LoadFolders.xml` selects the `Common` assembly for 1.6 and the `1.7` assembly for 1.7. Both versions also load the localization-only `Shared` folder.

To compile the shared source against a separate 1.6 installation and keep the shipped DLL in place:

```powershell
dotnet build Source/BetterTradeColors.csproj -c Release `
  -p:RimWorldVersion=1.6 `
  -p:RimWorldDir="D:/Games/RimWorld-1.6" `
  -p:HarmonyDir="D:/SteamLibrary/steamapps/workshop/content/294100/2009463077" `
  -p:OutputPath="../Tests/obj/Phase2Release16/"
```

`RimWorldManagedDir` can override the 1.6 managed-reference directory directly. For an experimental 1.6 in-game test, use a separate staging copy of the mod. Put the rebuilt DLL under its `Common/Assemblies` folder and include `Shared/Languages`. Future 1.6 packages can use that same shared localization without copying or maintaining a second set of strings.

## Compatibility checks

The standalone check program patches the installed 1.7 game assembly using its matching Harmony mod. It uses temporary settings files and in-memory fixtures. Build and run from the repository root with an isolated output directory so the checks exercise the new assembly without replacing either packaged DLL.

```powershell
dotnet build Tests/CompatibilityChecks.csproj -c Release `
  -p:OutputPath="../Tests/obj/Phase2Release17/"
dotnet Tests/obj/Phase2Release17/CompatibilityChecks.dll `
  "H:/SteamLibrary/steamapps/common/RimWorld" `
  "H:/SteamLibrary/steamapps/workshop/content/294100/2009463077"
```

For other installations, pass `RimWorldDir` and `HarmonyDir` as build properties and supply those directories as the two runtime arguments.

The checks cover Phase 1 color selection and Scribe persistence. They also compare the unpatched and patched width truncators, exercise nesting, adjacent colors, stack counts, narrow or empty input, Unicode, caller dictionaries, and cache isolation across widths, fonts, UI scales, and settings edits. Packaging checks verify both versions find the shared localization and exactly one mod assembly.

## Phase 2 verification

- **1.7 Release build and standalone checks:** Passed against RimWorld **1.7.5014 rev1195** and Harmony 3 with zero warnings and errors.
- **1.6 Release compile:** Passed with zero warnings and errors against local Krafs.Rimworld.Ref 1.6.4871, the .NET Framework 4.8 targeting pack, and installed Harmony 2. Runtime behavior in 1.6 still needs verification.
- **Artifacts:** Use the mod DLL in the ignored Phase 2 output directory for your target version. Both packaged DLLs were preserved.

The standalone runner simulates font metrics and counts measurements without launching RimWorld. For the sample in the check suite, the new cold path measures fewer candidates than either vanilla overload. The warm loop measures allocations and elapsed time for 20,000 same-frame calls. Real cache hit rates and native renderer costs depend on the views used and still need in-game profiling.

## Manual in-game checklist

Use the 1.7 Release DLL from Tests/obj/Phase2Release17 in a staging copy. Include Shared/Languages and enable Harmony.

1. In a loaded colony, check inspect titles and the cell inspector with long item labels. Resize the window or change UI scale to make columns narrow. Verify the ellipsis fits, active formatting ends at the label, and adjacent UI text uses its usual color.
2. Check trade, caravan, inventory, and inspect screens with stacked items. Verify counts stay uncolored when the count fits, and that counts and sorting remain correct.
3. Check a UI mod or test definition with nested bold, italic, size, and adjacent color spans. Verify text widths, original local color overrides, and Unicode glyphs with actual game fonts. Try the same label in different font sizes and column widths.
4. In a loaded colony, change a color and a toggle, then revisit the same item. Verify updated labels and swatches, then check persistence and Reset after restarting. Check small settings layouts and original priority, threshold, and non-apparel damage behavior.
5. Profile repeated narrow labels in a dense UI and watch for allocation spikes or an increased layout cost. Compare Harmony-only and the staged build under the same UI scale and font.
6. Repeat the rendering and settings checks for the experimental 1.6 build. Treat the 1.6 compile and all simulated metrics as separate from in-game verification.

See [docs/PLAN.md](docs/PLAN.md) for the source findings and remaining limitations.
