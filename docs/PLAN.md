# Better Trade Colors development plan

This development cycle follows the RimWorld 1.7 migration and in-game testing of Better Trade Colors 0.3.0.

## Phase 1 · Configurable item coloring

Implemented in the shared source. The 1.7 settings panel provides independent quality, taint, and damage toggles, a whole-percent damage threshold, nine RGB color fields with live swatches, and a default reset. Normal quality starts uncolored. The original palette, 50% exclusive cutoff, and taint → damage → quality priority remain the defaults. Damage applies to any item that uses hit points and has positive maximum HP.

The native swatches and text fields accept arbitrary RGB without a dependency on a game-specific picker dialog. Scribe stores the settings in the mod config file and supplies defaults for missing or malformed values. The label postfix uses nine cached tag slots and reads the current settings on every call. It runs after vanilla caches the undecorated label, so palette changes take effect on the next label request. Truncation caches keyed by the input string see a different key when a color changes. Mods that retain whole decorated labels may need to refresh their view.

Remaining work is manual verification of the settings layout, rendered colors, persistence through the game UI, and compatibility across trade and inventory views. The 1.6 release DLL stays at 0.2.0 while 1.6 in-game verification is pending. See [BUILDING.md](../BUILDING.md).

## Phase 2 · Rich text, truncation, caching, and compatibility

Audit narrow columns and mixed markup, including labels that already contain rich text. The current patch delegates to vanilla `TaggedString.Truncate`, which can cut the raw string inside a tag or drop a closing tag. The caller-provided truncation cache uses text alone as its key. Check both the callers' cache lifetime and the effect of reusing entries at different widths and font sizes. Compare the measured string with its actual rendering, including ellipses and stack counts, and exercise compatibility with UI mods that truncate or cache item labels.

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
