# Better Trade Colors

Instantly parse your inventory at a glance. **Better Trade Colors** adds intelligent, global color-coding to item labels based on quality, durability, and condition.

Designed from the ground up for modern performance, this mod replaces heavy string manipulation and dynamic regex parsing with an ultra-optimized, zero-allocation array indexing pipeline. Expect raw performance, even when scrolling through massive late-game trade manifests.

---

## 📊 Performance Benchmark
We have achieved a **~66% reduction in total average UI CPU time** compared to legacy color mods, making this mod a staggering **3x faster** under active load testing.

*(Analysis performed under active scrolling load using Dubs Performance Analyzer)*

---

## 🎨 What is colored?
* **Quality:** Intuitive color spectrum from Greyscale (Awful) to Cyan (Legendary).
* **Condition:** Items below 50% durability are highlighted in **Vibrant Magenta**.
* **Tainted:** Stripped apparel is marked in **Dim Red/Brown** to prevent accidental sales.

## ⚙️ Why choose this mod?
* **Unrivaled Performance:** ~66% reduction in total average UI CPU time. Smooth rendering with zero micro-stuttering or frame drops while scrolling.
* **Near Zero-Allocation Architecture:** Replaces the legacy method of messy, multi-layered string stripping (7 distinct patches) with just 2 highly optimized Harmony patches. By bypassing dynamic color-to-hex conversions and pulling rich-text tags via direct array indexing, frame-time spikes are entirely eliminated.
* **Smart Parsing:** Uses cache-aware text truncation to prevent UI clipping.
* **Stack-Aware:** Item names are colored, but quantities (e.g., "x75") remain standard text, keeping your sorting/calculation mods perfectly functional.
* **100% Safe:** Zero impact on save files. Safe to add or remove mid-game.

## 🤝 Compatibility
Works natively with:
* LWM's Deep Storage
* Trade UI Revised
* RimHUD

**Load Order:** Place anywhere in your **UI** section, ideally above your general performance section.

## 🔗 Links
* **[Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3738935469)**
* **[Source Code](https://github.com/JoyfulReaper/BetterTradeColors)**

## License
BSD 2-Clause

---
*Disclaimer: Portions of this mod were developed with AI assistance.*
