# Better Trade Colors development plan

This development cycle follows the RimWorld 1.7 migration and in-game testing of Better Trade Colors 0.3.0.

## Phase 1 · Configurable item coloring

Implemented in the shared source. The 1.7 settings panel provides independent quality, taint, and damage toggles, a whole-percent damage threshold, nine RGB color fields with live swatches, and a default reset. Normal quality starts uncolored. The original palette, 50% exclusive cutoff, and taint → damage → quality priority remain the defaults. Damage applies to any item that uses hit points and has positive maximum HP.

The native swatches and text fields accept arbitrary RGB without a dependency on a game-specific picker dialog. Scribe stores the settings in the mod config file and supplies defaults for missing or malformed values. The label postfix uses nine cached tag slots and reads the current settings on every call. It runs after vanilla caches the undecorated label, so palette changes take effect on the next label request. The Phase 2 truncation cache sees a different key when a color changes. Mods that retain whole decorated labels may need to refresh their view.

Remaining work is manual verification of the settings layout, rendered colors, persistence through the game UI, and compatibility across trade and inventory views. The 1.6 release DLL stays at 0.2.0 while 1.6 in-game verification is pending. See [BUILDING.md](../BUILDING.md).

## Phase 2 · Rich text, truncation, caching, and compatibility

Implemented in the shared source. The investigation followed the current 1.7 code and the installed renderer:

- RimWorld's Thing overload caches vanilla labels before Harmony runs. A settings change can therefore reuse the same vanilla label and apply the current palette. Trade and one-way transfer labels request the item name again when drawn. A mod that retains a decorated string may still need to refresh its view.
- RimWorld's Text.CalcSize strips angle-bracket tags and Verse semantic markers before measuring. Widgets.Label sends strings to the GUI label style. The TaggedString overload resolves Verse markers through ColoredText.Resolve first. TaggedString.RawText is the stored string, while its implicit conversion to string strips the markup.
- The UnityStubs label style forwards text to the 1.7 ImGui backend. Inspection of the installed backend confirmed that measurement and drawing both call TextLayout.Layout and that its rich-text parser recognizes color, bold, italic, and size tags. Measuring the raw renderer markup through the label style therefore accounts for formatting such as size that vanilla's stripped-width measurement misses.
- The string truncator and cell-inspector clamp remove raw UTF-16 units and can leave an open color span or partial markup. The TaggedString truncator also walks raw text and can discard all formatting through its implicit conversion. The Phase 1 patch delegates to that overload. Standalone fixtures reproduce the failures without Better Trade Colors patches installed, so colored text still needs targeted handling.
- InspectPaneUtility keeps a caller-owned dictionary and clears it on reset. Both truncation overloads key their entries by text alone even when the width or font changes. Plain inputs keep that vanilla behavior. Colored inputs use a separate 256-slot, frame-scoped cache keyed by full text, width, game font, UI scale, and whether Verse markers are present. The patches leave caller dictionaries intact.

The width truncators preserve supported enclosing and adjacent spans, close active tags before the ellipsis, and cut at text-element boundaries. The existing standalone string overload, the TaggedString overload, and the cell-inspector clamp share one implementation. The item label postfix avoids adding an outer color when existing spans already cover the whole label. Local color overrides and other supported formatting survive. The explicit GenLabel stack suffix now sits outside the mod's color span, matching Thing.Label. RimWorld's normal tail truncation can still omit a count that does not fit.

Verse semantic spans stay intact and use vanilla's stripped-width estimate. Height-based truncation, unsupported markup syntax, third-party caches of complete labels, and rendered behavior at unusual UI scales are outside the standalone guarantee. The two version mappings share one localization-only folder and still select one version-specific DLL each.

Both Release targets compile and the 1.7 standalone checks pass. Manual checks still need to cover real text measurement and rendering, UI mods, and the staged 1.6 build. See [BUILDING.md](../BUILDING.md).

## Phase 3 · Optional room quality coloring

Workshop inspiration colors room characteristics according to quality and displays the current and maximum quality tiers alongside the labels. Decide which vanilla room views and metrics to support after inspecting the 1.7 UI. Make room coloring independently optional. [Room Quality Colors on Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3407965871)

## Phase 4 · Optional colonist records coloring

Workshop inspiration colors records on the selected colonist's Records tab based on comparison with other colonists. Green marks a high record, yellow a lower record, and white a record nobody has achieved. Decide how to handle ties, zero values, and comparison scope before choosing patch targets. Make records coloring independently optional. [Better Records Tab (Continued) on Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=2359778562)

## Phase 5 · Accessibility and colorblind-friendly presets

Evaluate contrast on vanilla backgrounds and palettes for common color-vision deficiencies. Decide how preset selection interacts with custom colors and whether additional visual cues are needed.

## Phase 6 · Additional optional coloring

Explore skill levels, medical condition severity, mood, food freshness, and other item condition indicators. Evaluate each idea for readability, compatibility, and runtime cost before selecting any for implementation.

## Phase 7 · Final verification

Check both supported RimWorld versions, the load-folder and dependency layout, UI-mod interoperability, settings migration, and runtime performance in large trade and inventory views. Complete in-game verification before preparing release artifacts.
