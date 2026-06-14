/**
BSD 2-Clause License

Copyright (c) 2026, Kyle Givler

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 **/


using HarmonyLib;
using RimWorld;
using Verse;

[HarmonyPatch(typeof(GenLabel), nameof(GenLabel.ThingLabel),
    new[] { typeof(Thing), typeof(int), typeof(bool), typeof(bool) })]
public static class Patch_GenLabel_ThingLabel
{
    private const float hpPercentBreakPoint = 0.5f;

    private const string TagTainted = "<color=#B24C4C>"; // 0.7, 0.3, 0.3
    private const string TagDamaged = "<color=#D933BF>"; // 0.85, 0.2, 0.75

    // QualityCategory is an enum that ranges from 0 (Awful) to 6 (Legendary).
    // We can cast the enum directly to an integer to pull the string instantly.
    private static readonly string[] QualityTags = new string[]
    {
        "<color=#666666>", // 0: Awful      (0.4, 0.4, 0.4)
        "<color=#999999>", // 1: Poor       (0.6, 0.6, 0.6)
        null,              // 2: Normal     (We skip normal)
        "<color=#66CC66>", // 3: Good       (0.4, 0.8, 0.4)
        "<color=#CCCC33>", // 4: Excellent  (0.8, 0.8, 0.2)
        "<color=#FF9933>", // 5: Masterwork (1.0, 0.6, 0.2)
        "<color=#33CCFF>"  // 6: Legendary  (0.2, 0.8, 1.0)
    };

    public static void Postfix(Thing t, int stackCount, ref string __result)
    {
        if (t == null || string.IsNullOrEmpty(__result))
            return;

        string tag = GetTagForThing(t);

        if (tag != null)
        {
            // Only one string allocation happens here!
            __result = $"{tag}{__result}</color>";
        }
    }

    private static string GetTagForThing(Thing t)
    {
        // Tainted Apparel
        if (t is Apparel apparel && apparel.WornByCorpse)
            return TagTainted;

        // Durability
        if (t.def.useHitPoints && t.MaxHitPoints > 0 &&
           ((float)t.HitPoints / t.MaxHitPoints) < hpPercentBreakPoint)
            return TagDamaged;

        // Quality
        if (t.TryGetQuality(out QualityCategory qc) && qc != QualityCategory.Normal)
        {
            return QualityTags[(int)qc];
        }

        return null;
    }
}