using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace BetterTradeColors.Patches
{
    [HarmonyPatch(typeof(GenLabel), nameof(GenLabel.ThingLabel), new[] { typeof(Thing), typeof(int), typeof(bool) })]
    public static class Patch_GenLabel_ThingLabel
    {
        private const float hpPercentBreakPoint = 0.5f;

        public static void Postfix(Thing t, ref string __result)
        {
            if (t == null || __result.NullOrEmpty())
                return;

            Color targetColor = Color.white;
            bool shouldColor = false;

            // Tainted Check
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

            //Quality Tiers
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
                        // Leave normal items white to match vanilla style
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

            // Wrap the final string in rich text if a match was found
            if (shouldColor)
            {
                __result = ColorText(__result, targetColor);
            }
        }

        private static string ColorText(string text, Color color)
        {
            return $"<color=#{Mathf.RoundToInt(color.r * 255f):X2}{Mathf.RoundToInt(color.g * 255f):X2}{Mathf.RoundToInt(color.b * 255f):X2}>{text}</color>";
        }
    }
}