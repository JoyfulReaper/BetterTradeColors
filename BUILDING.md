# Building Better Trade Colors

Build from the repository root. The source project defaults to RimWorld 1.7.

| Game version | Target | Game references | Harmony | Loaded assembly folder |
| --- | --- | --- | --- | --- |
| 1.7 | .NET 10 (`net10.0`) | `Assembly-CSharp.dll`, `UnityStubs.dll` in the game directory | 3, from `1.7/Assemblies` | `1.7/Assemblies` |
| 1.6 | .NET Framework 4.8 (`net48`) | `Assembly-CSharp.dll`, `UnityEngine.CoreModule.dll` in `RimWorldWin64_Data/Managed` | 2, from `1.6/Assemblies` | `Common/Assemblies` |

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

The mod's `UnityEngine.Rect` and `UnityEngine.Vector2` uses remain supported by `UnityStubs.dll`. RimWorld's own `Mod`, `Widgets`, and `Listing_Standard` interfaces still use these types. No native Waylith API calls, direct `Waylith.dll` reference, or production Harmony patch changes are needed. Both patched overloads retain their original signatures in 1.7:

- `RimWorld.GenLabel.ThingLabel(Thing, int, bool, bool)`
- `Verse.GenText.Truncate(string, float, Dictionary<string, string>)`

## RimWorld 1.6

The shipped `Common/Assemblies/BetterTradeColors.dll` remains unchanged from version 0.2.0. `LoadFolders.xml` loads only `Common` for 1.6 and only `1.7` for 1.7, avoiding duplicate or incompatible assemblies.

To rebuild 1.6 from the shared source, point to a separate 1.6 installation:

```powershell
dotnet build Source/BetterTradeColors.csproj -c Release `
  -p:RimWorldVersion=1.6 `
  -p:RimWorldDir="D:/Games/RimWorld-1.6" `
  -p:HarmonyDir="D:/SteamLibrary/steamapps/workshop/content/294100/2009463077"
```

This replaces the DLL in `Common/Assemblies`. To verify without replacing the shipped binary, also set `-p:OutputPath="C:/temp/BetterTradeColors-1.6/"`. `RimWorldManagedDir` can override the 1.6 managed-reference directory directly.

## Compatibility checks

The standalone check program uses the installed 1.7 game and Harmony assemblies. It adds no testing packages and does not launch the game or change player settings. Run from the repository root:

```powershell
dotnet build Tests/CompatibilityChecks.csproj -c Release
dotnet Tests/bin/Release/net10.0/CompatibilityChecks.dll `
  "H:/SteamLibrary/steamapps/common/RimWorld" `
  "H:/SteamLibrary/steamapps/workshop/content/294100/2009463077"
```

For other installations, pass the same `RimWorldDir` and `HarmonyDir` build properties as above, and supply those directories as the two runtime arguments.

The checks apply both production Harmony patches, exercise patched item labels across all quality tiers and the damage threshold, verify taint priority, check truncation pass-through/cache behavior and untruncated colored text, and parse the load-folder mapping with RimWorld's own loader. They also inspect the shipped DLLs' runtime, engine, and Harmony references. The fixture supplies maximum HP without loading the game's stat database and uses stub font measurement; it does not validate actual rendered text or a running colony.

Migration validation:

- 1.7 Release build: passed with no warnings or errors against installed RimWorld **1.7.5005 rev1082** and Harmony 3.
- Standalone compatibility checks: passed.
- 1.6 Release build: passed with no warnings or errors against [Krafs.Rimworld.Ref 1.6.4871](https://www.nuget.org/packages/Krafs.Rimworld.Ref/1.6.4871) and installed Harmony 2, targeting .NET Framework 4.8. These temporary build-only reference assemblies were restored under ignored `Tests/obj`; they are not a project or runtime dependency. The shipped 1.6 DLL was preserved.
- Packaging: each supported game version selects exactly one mod DLL. No game, engine, Harmony, test, or reference-package DLLs belong in the release mod folders.

## In-game verification still required

The game has not been launched for this migration. Check a new and existing save, the mod settings window, trade/caravan and inspect labels, narrow text columns, stack counts, and the UI mods listed in the README. Compilation and standalone checks do not establish full Waylith runtime compatibility or compatibility with updated third-party mods.

The existing truncation patch still delegates to vanilla `TaggedString.Truncate`. The 1.7 source shortens raw rich text when space is limited, so closing-tag integrity in narrow columns remains a concern. This migration preserves that algorithm; the standalone checks do not certify tag balancing during truncation. No gameplay, color priority, durability threshold, or save-data changes were made.
