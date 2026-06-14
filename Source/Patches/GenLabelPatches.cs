/**BSD 2-Clause License

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
using UnityEngine;
using Verse;

[HarmonyPatch(typeof(GenLabel), nameof(GenLabel.ThingLabel),
    new[] { typeof(Thing), typeof(int), typeof(bool), typeof(bool) })]
public static class Patch_GenLabel_ThingLabel
{
    private const float hpPercentBreakPoint = 0.5f;

    public static void Postfix(Thing t, int stackCount, ref string __result)
    {
        if (t == null || string.IsNullOrEmpty(__result))
            return;

        Color? color = GetColorForThing(t);

        if (color.HasValue)
        {
            // Use hex conversion safely
            string hex = ColorUtility.ToHtmlStringRGB(color.Value);
            __result = $"<color=#{hex}>{__result}</color>";
        }
    }

    private static Color? GetColorForThing(Thing t)
    {
        // Tainted Apparel
        if (t is Apparel apparel && apparel.WornByCorpse)
            return new Color(0.7f, 0.3f, 0.3f);

        // Durability
        if (t.def.useHitPoints && t.MaxHitPoints > 0 &&
           ((float)t.HitPoints / t.MaxHitPoints) < hpPercentBreakPoint)
            return new Color(0.85f, 0.2f, 0.75f);

        // Quality
        if (t.TryGetQuality(out QualityCategory qc) && qc != QualityCategory.Normal)
        {
            return qc switch
            {
                QualityCategory.Awful => new Color(0.4f, 0.4f, 0.4f),
                QualityCategory.Poor => new Color(0.6f, 0.6f, 0.6f),
                QualityCategory.Good => new Color(0.4f, 0.8f, 0.4f),
                QualityCategory.Excellent => new Color(0.8f, 0.8f, 0.2f),
                QualityCategory.Masterwork => new Color(1f, 0.6f, 0.2f),
                QualityCategory.Legendary => new Color(0.2f, 0.8f, 1f),
                _ => (Color?)null
            };
        }

        return null;
    }
}