# [BTC-CORE: SYSTEMS ARCHITECTURE MANUAL]

**PROTOCOL ID:** BTC-COLOR-01  
**CODENAME:** Better Trade Colors  
**SYSTEM STATUS:** OPERATIONAL  
**MAINTAINER:** K. GIVLER (ADMIN)  

---

## 1.0 SYSTEM OVERVIEW

**Better Trade Colors** is an optimization and visual parsing utility designed for the *RimWorld* user interface. The system implements a global, high-performance color-coding pipeline that modifies item labels based on asset quality, durability, and condition metrics.

In the standard execution environment, rendering colored text fields often relies on heavy string manipulation and dynamic regular expression (regex) parsing. Under large late-game rendering loads—such as scrolling through massive trade manifests—this legacy approach introduces significant frame-time spikes and micro-stuttering.

The label patch uses cached color tags and needs one string allocation for a typical unformatted label that receives a color.

## 2.0 PERFORMANCE VALIDATION

Earlier Dubs Performance Analyzer measurements compared the item-coloring path with legacy implementations. Phase 2 standalone results and their measurement limits are documented in [BUILDING.md](BUILDING.md).

* **UI CPU Time Reduction:** ~66% reduction in total average UI CPU processing time compared to legacy iteration methods like Quality Colors.
* **Execution Velocity:** Operational throughput is approximately **3x faster** under peak loading conditions.
* **Color tag reuse:** Hex conversion happens when the palette changes.

## 3.0 MATERIAL PARSING SELECTION Matrix

The parsing routine automatically evaluates item metadata and applies specific rich-text color layers according to the following matrix:

| Targeted State | Applied Visual Metric | Operational Purpose |
| --- | --- | --- |
| **Quality Tier** | Full spectrum: Greyscale (`Awful`) to Cyan (`Legendary`) | Immediate item value classification. |
| **Degradation** | Vibrant Magenta flag | Critical alert for assets falling below 50% durability. |
| **Contamination** | Dim Red / Brown flag | Isolation marking for stripped (tainted) apparel to prevent accidental liquidation. |

* **Stack Counts:** RimWorld generates the count, and the mod keeps it outside the item color. Narrow columns can still truncate the end of a label.
* **Truncation:** Colored labels keep their formatting when they fit. In narrower columns the mod closes active tags before the ellipsis. Plain text uses vanilla truncation.

### Configuring item colors

In the current development build for RimWorld 1.7, open **Options → Mod Settings → Better Trade Colors**. Quality, taint, and damage coloring each have their own toggle. Taint takes priority over damage, which takes priority over quality. Damage coloring applies below the selected durability percentage, including to non-apparel items that use hit points. The default cutoff is 50%.

Enter six RGB hex digits for any color, such as `#33CCFF`. The swatch shows the last valid value. Leave **Normal** blank to use the game's text color. Changes apply when item labels are next generated, and RimWorld saves them when the settings window closes. **Reset to defaults** restores the original palette, toggles, and threshold.

### Room quality

Under **Options → Mod Settings → Better Trade Colors → Room Quality**, turn on room quality colors, numerical stage indicators, or both. The options start off. Both appear in the room-stat overlay and its building gizmo hover panel, the selected-building room summary, and the Alt cell inspector.

Impressiveness, beauty, cleanliness, spaciousness, wealth, and visible custom stats with stage definitions use their own thresholds. An indicator like **Very impressive (7/10)** shows the current stage and the total stages for that statistic. The seven-color palette runs from the first defined stage to the last. You can edit the colors with live swatches and reset the room settings independently of item coloring.

## 4.0 TECHNICAL SPECIFICATIONS & ARCHITECTURE

The underlying pipeline represents an aggressive optimization refactor over legacy UI modification methods:

* **Harmony patches:** Item coloring uses the Thing label overload. Colored text uses the width-based string and TaggedString truncators and the cell inspector clamp.
* **Data Retrieval:** Reuses cached rich-text tags and updates each tag when its configured color changes.
* **Configuration Storage:** RimWorld saves preferences in the mod settings file. Colony saves contain no Better Trade Colors settings.

## 5.0 COMPATIBILITY & DEPLOYMENT MATRIX

Supports **RimWorld 1.6 and 1.7**. Version 1.7 uses a separate .NET 10 assembly with Harmony 3; version 1.6 retains its 0.2.0 assembly and settings behavior. See [BUILDING.md](BUILDING.md) for build and verification details and [docs/PLAN.md](docs/PLAN.md) for the development roadmap.

### 5.1 Verified Environments

The framework features native interoperability with the following user interface configurations:

* *LWM's Deep Storage*
* *Trade UI Revised*
* *RimHUD*

### 5.2 Boot Sequencing (Load Order)

For optimal initialization, place the module within the **UI** subdivision of your initialization order, positioned vertically above generalized performance configuration modules.

## 6.0 SYSTEM ACCESS NODES

* **[Distribution Interface (Steam Workshop)](https://steamcommunity.com/sharedfiles/filedetails/?id=3738935469)**
* **[Source Code Repository (GitHub)](https://github.com/JoyfulReaper/BetterTradeColors)**

---

## 7.0 LEGAL & COMPLIANCE

**DISTRIBUTION NOTICE:** This framework is distributed under the terms of the BSD 2-Clause License.  
**TECHNICAL FOOTNOTE:** Portions of this system architecture were developed utilizing artificial intelligence generation protocols.

---

**[BTC-CORE: SYSTEMS ARCHITECTURE MANUAL]**
