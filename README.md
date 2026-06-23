# [BTC-CORE: SYSTEMS ARCHITECTURE MANUAL]

**PROTOCOL ID:** BTC-COLOR-01  
**CODENAME:** Better Trade Colors  
**SYSTEM STATUS:** OPERATIONAL  
**MAINTAINER:** K. GIVLER (ADMIN)  

---

## 1.0 SYSTEM OVERVIEW

**Better Trade Colors** is an optimization and visual parsing utility designed for the *RimWorld* user interface. The system implements a global, high-performance color-coding pipeline that modifies item labels based on asset quality, durability, and condition metrics.

In the standard execution environment, rendering colored text fields often relies on heavy string manipulation and dynamic regular expression (regex) parsing. Under large late-game rendering loads—such as scrolling through massive trade manifests—this legacy approach introduces significant frame-time spikes and micro-stuttering.

This utility intercepts the text-rendering pipeline, replacing complex string operations with an ultra-optimized, zero-allocation array indexing execution path.

## 2.0 PERFORMANCE VALIDATION

Analytical assessments performed via *Dubs Performance Analyzer* confirm the efficiency of the array indexing architecture under active scrolling loads:

* **UI CPU Time Reduction:** ~66% reduction in total average UI CPU processing time compared to legacy iteration methods like Quality Colors.
* **Execution Velocity:** Operational throughput is approximately **3x faster** under peak loading conditions.
* **Memory Footprint:** Bypasses dynamic color-to-hex conversion routines, eliminating frame-time variance caused by garbage collection allocations.

## 3.0 MATERIAL PARSING SELECTION Matrix

The parsing routine automatically evaluates item metadata and applies specific rich-text color layers according to the following matrix:

| Targeted State | Applied Visual Metric | Operational Purpose |
| --- | --- | --- |
| **Quality Tier** | Full spectrum: Greyscale (`Awful`) to Cyan (`Legendary`) | Immediate item value classification. |
| **Degradation** | Vibrant Magenta flag | Critical alert for assets falling below 50% durability. |
| **Contamination** | Dim Red / Brown flag | Isolation marking for stripped (tainted) apparel to prevent accidental liquidation. |

* **Stack Isolation (Stack-Aware):** Color application routines isolate the item definition string exclusively. Numerical multipliers (e.g., `x75`) bypass modification to ensure total compatibility with inventory calculations and sorting frameworks.
* **Dimensional Constraint (Smart Truncation):** Employs a cache-aware text truncation routine to prevent modified string layers from causing UI clipping or boundary breaches.

## 4.0 TECHNICAL SPECIFICATIONS & ARCHITECTURE

The underlying pipeline represents an aggressive optimization refactor over legacy UI modification methods:

* **Pipeline Footprint:** Replaces a legacy 7-patch string stripping implementation with exactly **2 highly optimized Harmony patches**.
* **Data Retrieval:** Fetches rich-text configuration tags directly via static array pointer indices rather than parsing dynamic hex values at runtime.
* **Environment Stability:** 100% volatile runtime logic. The utility introduces no persistent data structures to the simulation state, ensuring it is entirely safe to initialize or decouple mid-game.

## 5.0 COMPATIBILITY & DEPLOYMENT MATRIX

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
