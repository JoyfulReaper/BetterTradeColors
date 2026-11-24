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


using BetterTradeColors;
using BetterTradeColors.Settings;
using HarmonyLib;
using RimWorld;
using Verse;

[HarmonyPatch(typeof(GenLabel), nameof(GenLabel.ThingLabel),
    new[] { typeof(Thing), typeof(int), typeof(bool), typeof(bool) })]
public static class Patch_GenLabel_ThingLabel
{
    public static void Postfix(Thing t, int stackCount, ref string __result)
    {
        BetterTradeColorsSettings settings = BetterTradeColorsMod.Settings;
        if (settings == null || t?.def == null || string.IsNullOrEmpty(__result))
            return;

        string tag = GetTagForThing(t, settings);
        if (tag != null)
            __result = string.Concat(tag, __result, "</color>");
    }

    private static string GetTagForThing(Thing t, BetterTradeColorsSettings settings)
    {
        if (settings.ColorTainted && t is Apparel apparel && apparel.WornByCorpse)
            return settings.TaintedTag;

        if (settings.ColorDamaged && t.def.useHitPoints)
        {
            int maxHitPoints = t.MaxHitPoints;
            if (maxHitPoints > 0 && (long)t.HitPoints * 100 < (long)maxHitPoints * settings.DamageThreshold)
                return settings.DamagedTag;
        }

        return settings.ColorQuality && t.TryGetQuality(out QualityCategory quality)
            ? settings.QualityTag(quality)
            : null;
    }
}
