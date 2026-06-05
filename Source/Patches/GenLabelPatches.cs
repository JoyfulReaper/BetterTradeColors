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


using RimWorld;
using UnityEngine;
using Verse;

namespace BetterTradeColors
{
    public static class Patch_GenLabel_NewThingLabel
    {
        private const float hpPercentBreakPoint = 0.5f;

        public static void Postfix(Thing t, int stackCount, ref string __result)
        {
            if (t == null || string.IsNullOrEmpty(__result))
                return;

            Color targetColor = Color.white;
            bool shouldColor = false;

            //Tainted Check
            if (t is Apparel apparel && apparel.WornByCorpse)
            {
                targetColor = new Color(0.7f, 0.3f, 0.3f); // Dim Red / Brown
                shouldColor = true;
            }
            // Durability Check (Only Applies to Apparel)
            else if (t is Apparel && t.def.useHitPoints && t.MaxHitPoints > 0)
            {
                float hpPercent = (float)t.HitPoints / t.MaxHitPoints;
                if (hpPercent < hpPercentBreakPoint)
                {
                    targetColor = new Color(0.85f, 0.2f, 0.75f); // Vibrant Magenta / Purple
                    shouldColor = true;
                }
            }
            // Quality Tiers
            else if (t.TryGetQuality(out QualityCategory qc))
            {
                switch (qc)
                {
                    case QualityCategory.Awful:
                        targetColor = new Color(0.4f, 0.4f, 0.4f); // Dark Grey
                        shouldColor = true;
                        break;
                    case QualityCategory.Poor:
                        targetColor = new Color(0.6f, 0.6f, 0.6f); // Light Grey
                        shouldColor = true;
                        break;
                    case QualityCategory.Normal:
                        // Leave normal items white to keep UI balanced
                        break;
                    case QualityCategory.Good:
                        targetColor = new Color(0.4f, 0.8f, 0.4f); // Soft Green
                        shouldColor = true;
                        break;
                    case QualityCategory.Excellent:
                        targetColor = new Color(0.8f, 0.8f, 0.2f); // Yellow / Gold
                        shouldColor = true;
                        break;
                    case QualityCategory.Masterwork:
                        targetColor = new Color(1f, 0.6f, 0.2f);   // True Orange
                        shouldColor = true;
                        break;
                    case QualityCategory.Legendary:
                        targetColor = new Color(0.2f, 0.8f, 1f);   // Cyan / Light Blue
                        shouldColor = true;
                        break;
                }
            }

            // Apply targeted rich text formatting if color rules are met
            if (shouldColor)
            {
                string hexColor = $"{Mathf.RoundToInt(targetColor.r * 255f):X2}{Mathf.RoundToInt(targetColor.g * 255f):X2}{Mathf.RoundToInt(targetColor.b * 255f):X2}";

                if (stackCount > 1)
                {
                    // Calculate the exact suffix string length added by vanilla: " x" + stackCount
                    string countSuffix = " x" + stackCount.ToStringCached();

                    if (__result.EndsWith(countSuffix))
                    {
                        // Strip the suffix, colorize only the item label, and append the uncolored suffix back on
                        string cleanLabel = __result.Substring(0, __result.Length - countSuffix.Length);
                        __result = $"<color=#{hexColor}>{cleanLabel}</color>{countSuffix}";
                        return;
                    }
                }

                // If stack size is 1, wrap the entire string normally
                __result = $"<color=#{hexColor}>{__result}</color>";
            }
        }
    }
}