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
using UnityEngine;
using Verse;

namespace BetterTradeColors.Patches
{
    // TODO color settings in Mod Menu

    public class TransferableUIUtilityPatches
    {
        [HarmonyPatch(typeof(TransferableUIUtility), nameof(TransferableUIUtility.DrawTransferableInfo))]
        public static class Patch_DrawTransferableInfo
        {
            private const float hpPercentBreakPoint = 0.5f;

            public static void Prefix(Transferable trad, ref Color labelColor)
            {
                if (!trad.IsThing || trad.AnyThing == null)
                    return;

                Thing thing = trad.AnyThing;


                if (thing is Apparel apparel && apparel.WornByCorpse)
                {
                    labelColor = new Color(0.7f, 0.3f, 0.3f); // Dim Red/Brown
                    return;
                }

                if (thing is Apparel && thing.def.useHitPoints && thing.MaxHitPoints > 0)
                {
                    float hpPercent = (float)thing.HitPoints / thing.MaxHitPoints;
                    if (hpPercent < hpPercentBreakPoint)
                    {
                        labelColor = new Color(0.85f, 0.2f, 0.75f); // Vibrant Magenta / Purple
                        return;
                    }
                }

                if (thing.TryGetQuality(out QualityCategory qc))
                {
                    switch (qc)
                    {
                        case QualityCategory.Awful:
                            labelColor = new Color(0.4f, 0.4f, 0.4f); // Dark Grey
                            break;
                        case QualityCategory.Poor:
                            labelColor = new Color(0.6f, 0.6f, 0.6f); // Light Grey
                            break;
                        case QualityCategory.Normal:
                            labelColor = Color.white;                 // White
                            break;
                        case QualityCategory.Good:
                            labelColor = new Color(0.4f, 0.8f, 0.4f); // Soft Green
                            break;
                        case QualityCategory.Excellent:
                            labelColor = new Color(0.8f, 0.8f, 0.2f); // Yellow / Gold
                            break;
                        case QualityCategory.Masterwork:
                            labelColor = new Color(1f, 0.6f, 0.2f);   // True Orange
                            break;
                        case QualityCategory.Legendary:
                            labelColor = new Color(0.2f, 0.8f, 1f);   // Cyan / Light Blue
                            break;
                        default:
                            labelColor = Color.white;                 // White
                            break;
                    }
                }
            }
        }
    }
}