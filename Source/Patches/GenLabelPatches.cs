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
using System.Collections.Generic;

namespace BetterTradeColors
{
    // =========================================================================
    // Inject colors into the core string generation
    // =========================================================================
    [HarmonyPatch(typeof(GenLabel), "NewThingLabel", new[] { typeof(Thing), typeof(int), typeof(bool), typeof(bool) })]
    public static class Patch_GenLabel_NewThingLabel
    {
        private const float hpPercentBreakPoint = 0.5f;

        public static void Postfix(Thing t, int stackCount, ref string __result)
        {
            if (t == null || string.IsNullOrEmpty(__result))
                return;

            Color targetColor = Color.white;
            bool shouldColor = false;

            // Tainted Apparel Check
            if (t is Apparel apparel && apparel.WornByCorpse)
            {
                targetColor = new Color(0.7f, 0.3f, 0.3f); // Dim Red / Brown
                shouldColor = true;
            }
            // Durability Check
            else if (t is Apparel && t.def.useHitPoints && t.MaxHitPoints > 0 && ((float)t.HitPoints / t.MaxHitPoints) < hpPercentBreakPoint)
            {
                targetColor = new Color(0.85f, 0.2f, 0.75f); // Vibrant Magenta / Purple
                shouldColor = true;
            }
            // Quality Tiers
            else if (t.TryGetQuality(out QualityCategory qc) && qc != QualityCategory.Normal)
            {
                shouldColor = true;
                if (qc == QualityCategory.Awful) targetColor = new Color(0.4f, 0.4f, 0.4f); // Dark Grey
                else if (qc == QualityCategory.Poor) targetColor = new Color(0.6f, 0.6f, 0.6f); // Light Grey
                else if (qc == QualityCategory.Good) targetColor = new Color(0.4f, 0.8f, 0.4f); // Soft Green
                else if (qc == QualityCategory.Excellent) targetColor = new Color(0.8f, 0.8f, 0.2f); // Yellow / Gold
                else if (qc == QualityCategory.Masterwork) targetColor = new Color(1f, 0.6f, 0.2f);   // True Orange
                else if (qc == QualityCategory.Legendary) targetColor = new Color(0.2f, 0.8f, 1f);   // Cyan / Light Blue
                else shouldColor = false;
            }

            if (shouldColor)
            {
                Color32 c = targetColor;
                string hexColor = $"{c.r:X2}{c.g:X2}{c.b:X2}";

                if (stackCount > 1)
                {
                    string countSuffix = " x" + stackCount.ToStringCached();
                    if (__result.EndsWith(countSuffix))
                    {
                        string cleanLabel = __result.Substring(0, __result.Length - countSuffix.Length);
                        __result = $"<color=#{hexColor}>{cleanLabel}</color>{countSuffix}";
                        return;
                    }
                }

                __result = $"<color=#{hexColor}>{__result}</color>";
            }
        }
    }
}