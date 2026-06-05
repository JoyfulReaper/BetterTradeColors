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

                if (thing.def.useHitPoints && thing.MaxHitPoints > 0)
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