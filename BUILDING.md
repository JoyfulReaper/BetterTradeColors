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

The room quality patches transpile three presentation methods. They use the RoomStatDef, score, and stage already used by each view. No room or stat caches are scanned or refreshed by the mod.

- EnvironmentStatsDrawer.DoRoomInfo(Room, ref float, Rect) covers the room overlay and the gizmo hover panel. The patch also fits the stage description to its fixed-width column.
- Gizmo_RoomStats.GizmoOnGUI(Vector2, float, GizmoRenderParms) covers the selected-building room summary, including the Impressiveness title.
- CellInspectorDrawer.DrawMapInspector() covers only the room-stat row in the Alt inspector, and uses the width and inset of vanilla's info column.

The transpiler checks the expected display call sites and logs a warning if a changed method cannot safely be patched. A competing UI replacement or localization mod that appends its own numbers can change the result. Precolored stage labels keep their existing color. Phase 3 caches the seven palette tags and reuses the Phase 2 layout cache. Additional measurements can occur when space must be reserved for a complete stage indicator.

The Records tab uses two presentation hooks:

- RecordsCardUtility.DrawRecordsCard(Rect, Pawn) establishes the active comparison only when records coloring is enabled and a qualifying pawn is viewed in a loaded game. Its finalizer restores the previous render context, including on exceptions.
- RecordsCardUtility.DrawRecord(float, float, float, RecordDef, Pawn) redirects only the value-cell label call. The transpiler checks for the two vanilla label sites and leaves an unrecognized layout intact. A mod that replaces the Records panel or patches its value display can bypass or override the coloring.

The snapshot traverses the game's free-colonist finder once per open-card frame. Native enumeration includes maps and their held pawns, caravans, and supported transport. The comparison filters for eligible living colonists and reads stored record values directly for precise rankings. It reuses the maximum array and the pawn ID set, without holding pawn references. A short game-tick window would stick while the info card pauses the game. Frame-based reuse handles repeat GUI passes, changes while paused, and world switches. A new snapshot costs one read per stored slot per eligible pawn. Each value cell then reads just the selected pawn's value and reuses a cached color tag.

## RimWorld 1.6

The shipped `Common/Assemblies/BetterTradeColors.dll` remains unchanged from version 0.2.0. `LoadFolders.xml` selects the `Common` assembly for 1.6 and the `1.7` assembly for 1.7. Both versions also load the localization-only `Shared` folder.

To compile the shared source against a separate 1.6 installation and keep the shipped DLL in place:

```powershell
dotnet build Source/BetterTradeColors.csproj -c Release `
  -p:RimWorldVersion=1.6 `
  -p:RimWorldDir="D:/Games/RimWorld-1.6" `
  -p:HarmonyDir="D:/SteamLibrary/steamapps/workshop/content/294100/2009463077" `
  -p:OutputPath="../Tests/obj/Phase4Release16/"
```

`RimWorldManagedDir` can override the 1.6 managed-reference directory directly. For an experimental 1.6 in-game test, use a separate staging copy of the mod. Put the rebuilt DLL under its `Common/Assemblies` folder and include `Shared/Languages`. Future 1.6 packages can use that same shared localization without copying or maintaining a second set of strings.

## Compatibility checks

The standalone check program patches the installed 1.7 game assembly using its matching Harmony mod. It uses temporary settings files and in-memory fixtures. Build and run from the repository root with an isolated output directory so the checks exercise the new assembly without replacing either packaged DLL.

```powershell
dotnet build Tests/CompatibilityChecks.csproj -c Release `
  -p:OutputPath="../Tests/obj/Phase4Release17/"
dotnet Tests/obj/Phase4Release17/CompatibilityChecks.dll `
  "H:/SteamLibrary/steamapps/common/RimWorld" `
  "H:/SteamLibrary/steamapps/workshop/content/294100/2009463077"
```

For other installations, pass `RimWorldDir` and `HarmonyDir` as build properties and supply those directories as the two runtime arguments.

The checks cover Phase 1 color selection and Scribe persistence. They also compare the unpatched and patched width truncators, exercise nesting, adjacent colors, stack counts, narrow or empty input, Unicode, caller dictionaries, and cache isolation across widths, fonts, UI scales, and settings edits. Packaging checks verify both versions find the shared localization and exactly one mod assembly.

Phase 3 also checks the installed room-stage boundaries, injected display sites, the independent options and resets, shared translations, Scribe persistence and invalid input, custom stats, and truncation with a reserved stage indicator.

The Phase 4 standalone checks exercise the two Records tab hooks, raw and formatted value differences, ties, non-finite values, world and membership changes, native map, caravan, and travelling transport enumeration with fixtures, and persisted records settings. The benchmark runs each colony size for 120 frames with three simulated UI passes per frame. The printed allocation and timing figures cover warmed snapshot refreshes and comparisons. They exclude native map-holder traversal, formatting, and rendering.

One local .NET 10 run with tiered compilation disabled (DOTNET_TieredCompilation=0) produced these totals. Each fixture enumerated its population once per frame. Raw lookup counts are derived from the populated fixture sizes and checked row counts.

| Colonists | Records | Enumerations | Raw lookups | Allocated bytes | Time |
| ---: | ---: | ---: | ---: | ---: | ---: |
| 15 | 40 | 120 | 86,400 | 0 | 0.31 ms |
| 100 | 80 | 120 | 988,800 | 0 | 1.60 ms |
| 400 | 160 | 120 | 7,737,600 | 0 | 10.11 ms |

## Phase 4 verification

- **1.7 Release and standalone checks:** Passed against RimWorld 1.7.5014 rev1195 and Harmony 3.
- **1.6 Release compile:** Passed against local Krafs.Rimworld.Ref 1.6.4871 and Harmony 2. Runtime patch matching and rendering are unverified.
- **Artifacts:** Both Phase 4 outputs are isolated under Tests/obj. The packaged DLLs were preserved.

## Phase 3 verification

- **1.7 Release and standalone checks:** Passed against RimWorld 1.7.5014 rev1195 and Harmony 3.
- **1.6 Release compile:** Passed against local Krafs.Rimworld.Ref 1.6.4871 and Harmony 2. Actual 1.6 display patch matching and rendering remain unverified.
- **Artifacts:** Both Phase 3 outputs are isolated under Tests/obj. Both packaged DLLs were preserved.

## Phase 2 verification

- **1.7 Release build and standalone checks:** Passed against RimWorld **1.7.5014 rev1195** and Harmony 3 with zero warnings and errors.
- **1.6 Release compile:** Passed with zero warnings and errors against local Krafs.Rimworld.Ref 1.6.4871, the .NET Framework 4.8 targeting pack, and installed Harmony 2. Runtime behavior in 1.6 still needs verification.
- **Artifacts:** Use the mod DLL in the ignored Phase 2 output directory for your target version. Both packaged DLLs were preserved.

The standalone runner simulates font metrics and counts measurements without launching RimWorld. For the sample in the check suite, the new cold path measures fewer candidates than either vanilla overload. The warm loop measures allocations and elapsed time for 20,000 same-frame calls. Real cache hit rates and native renderer costs depend on the views used and still need in-game profiling.

## Manual in-game checklist

Use the 1.7 Release DLL from Tests/obj/Phase4Release17 in a staging copy. Include Shared/Languages and enable Harmony.

1. In a loaded colony, check inspect titles and the cell inspector with long item labels. Resize the window or change UI scale to make columns narrow. Verify the ellipsis fits, active formatting ends at the label, and adjacent UI text uses its usual color.
2. Check trade, caravan, inventory, and inspect screens with stacked items. Verify counts stay uncolored when the count fits, and that counts and sorting remain correct.
3. Check a UI mod or test definition with nested bold, italic, size, and adjacent color spans. Verify text widths, original local color overrides, and Unicode glyphs with actual game fonts. Try the same label in different font sizes and column widths.
4. In a loaded colony, change a color and a toggle, then revisit the same item. Verify updated labels and swatches, then check persistence and Reset after restarting. Check small settings layouts and original priority, threshold, and non-apparel damage behavior.
5. Profile repeated narrow labels in a dense UI and watch for allocation spikes or an increased layout cost. Compare Harmony-only and the staged build under the same UI scale and font.
6. Compare both room options off, colors only, numbers only, and both on. Use the room stats overlay, an eligible selected building and its hover panel, and the Alt cell inspector. Verify current/max stages and the original room name and score formatting. Include clean/sterile rooms, rooms at different impressiveness or wealth stages, and a staged custom room statistic if available.
7. Narrow the windows or raise UI scale and try a longer localization. Verify descriptions shorten cleanly, the complete indicator stays visible when it fits, and colors stop at the end of the description. Watch for competing UI mods that already color or number room descriptions.
8. Edit the room palette, close settings, restart, and check both toggles and swatches. Reset the room settings and verify item colors and thresholds stay as configured. Repeat the rendering and settings checks for the experimental 1.6 build.

9. Open the Records tab for a lone colonist and then for a colony with positive ties, lower records, and all-zero records. Check integer, decimal, and duration formatting, labels, scrolling, tooltips, and mixed rich text with coloring off and on.
10. Compare a colony split across maps, a caravan, and travelling transport. Check that slaves, prisoners, guests, and pawn previews outside the active colony keep their usual display. Launch or land a transport, switch saves, and reopen the card after changing a record.
11. Customize and reset the records colors, including a blank unachieved color, and restart to verify persistence. Test a modded record and a Records tab UI mod. Profile open and closed Records tabs in a larger colony. On the separate 1.6 build, confirm the hooks match and repeat the display checks.

Standalone tests verify the transformed methods, formatting, and comparison logic with simulated text metrics and in-memory game data. Rendering still requires a running game.

See [docs/PLAN.md](docs/PLAN.md) for the source findings and remaining limitations.
