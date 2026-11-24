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

Implemented in the shared source. The room overlay and gizmo hover panel use EnvironmentStatsDrawer.DoRoomInfo. The selected-building summary uses Gizmo_RoomStats.GizmoOnGUI, and the Alt cell inspector uses CellInspectorDrawer.DrawMapInspector. The patches format only the stage description in those methods and use their existing scores and stage lookups. Separate beauty-at-cursor displays, room role names, and raw score columns keep their vanilla formatting.

Room.GetStat reuses the room's stat map until vanilla marks the room dirty. RoomStatDef.GetScoreStage chooses the last stage whose minimum score is met, with stage zero as the fallback. The installed Core definitions have 10 Impressiveness stages, 8 Beauty stages, 5 Cleanliness stages, 7 Space stages, and 9 Wealth stages. The displays rebuild their stage strings as they draw. The mod leaves the game's definitions, localized stage labels, room data, and stat cache untouched.

The independent Room Quality controls start off. Colors select the nearest of seven palette entries by stage position, with endpoints mapped directly and a single stage mapped to the middle. The palette represents definition order. For example, Sterile is the last Cleanliness stage, but the palette does not decide whether a sterile room is preferable for a particular use. Visible custom stats with stages follow the same rule. Stage-less stats receive no decoration. Numbering is 1-based and uses each definition's actual stage count.

To fit compact rows, the mod reuses Phase 2 truncation and preserves the complete indicator where it fits. This can shorten the description, including the inline raw score in the gizmo or cell inspector. Existing precolored descriptions retain their color. Another mod that replaces the same UI code can prevent the display patch from matching. Mods that insert their own stage numbers can also duplicate the indicator. The design was developed independently of the Workshop mod. [Room Quality Colors on Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3407965871)

The 1.7 standalone checks exercise the game's stage lookup with thresholds from the installed definitions and verify that all three display patches find their intended sites. The isolated 1.6 Release build compiles against the 1.6 references. Run the rendering, settings, and mod-interoperability checks in game before distributing an updated binary. See [BUILDING.md](../BUILDING.md).

## Phase 4 · Optional colonist records coloring

Implemented in the shared source. The native Records tab formats integers and floats with its existing numeric format and formats time records as periods. Pawn_RecordsTracker stores all three as floats indexed by RecordDef, with zero-filled defaults. Its public accessor rounds time and integers, so the comparison reads raw slots while leaving the game's displayed values, names, hitboxes, tooltips, and scroll layout intact. Vanilla caches the sorted definition list and scroll state, and it reads the record values again while drawing.

The comparison uses RimWorld's map, caravan, and travelling-transporter finder for living free colonists without quest lodgers. It also excludes slaves explicitly because some secure slaves satisfy the game's colonist predicate. The finder includes pawns in map-held containers and its supported transport, including unspawned occupants of the travelling gravship in 1.7. Dead pawns, prisoners, visitors, animals, hostile and other non-player pawns, and temporary or off-map world pawns outside those sources are excluded. Colonists remain eligible while downed or in a mental break if their faction and location still qualify. A pawn outside the comparison population keeps vanilla coloring.

Equal positive maxima share the holder color. A finite value below a positive maximum gets the below-record color. A record with no positive value is unachieved, even in a one-colonist game. Non-finite values and missing trackers or slots contribute no maximum. Colors describe numerical standing, including records such as damage received. Defaults are green for holders, yellow for lower records, and inherited vanilla text for unachieved values. The option starts off and has its own customizable palette and reset.

RecordsCardUtility.DrawRecordsCard scopes the comparison and restores its render context in a finalizer. A transpiler in its private DrawRecord method decorates only the value label. An unfamiliar row layout skips decoration. Existing fully colored values retain their color. A Records tab replacement or another mod patching the same value cell can override or bypass this behavior. The implementation was developed independently. [Better Records Tab (Continued) on Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=2359778562)

The enabled tab builds maxima once per rendered frame. Multiple UI passes reuse the snapshot, and the next frame sees membership and value changes even while the info card pauses game ticks. Changing worlds invalidates the snapshot on use. It retains one numeric maximum per registered record and eligible pawn IDs, with a weak world reference. It holds no pawn or tracker references after collection and schedules no background work. The standalone benchmark isolates comparison cost from map-holder traversal, GUI drawing, and text allocation. See [BUILDING.md](../BUILDING.md).

## Phase 5 · Accessibility and colorblind-friendly presets

Evaluate contrast on vanilla backgrounds and palettes for common color-vision deficiencies. Decide how preset selection interacts with custom colors and whether additional visual cues are needed.

## Phase 6 · Additional optional coloring

Explore skill levels, medical condition severity, mood, food freshness, and other item condition indicators. Evaluate each idea for readability, compatibility, and runtime cost before selecting any for implementation.

## Phase 7 · Final verification

Check both supported RimWorld versions, the load-folder and dependency layout, UI-mod interoperability, settings migration, and runtime performance in large trade and inventory views. Complete in-game verification before preparing release artifacts.
