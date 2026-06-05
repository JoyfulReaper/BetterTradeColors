using HarmonyLib;
using System.Collections.Generic;
using Verse;

namespace BetterTradeColors.Patches
{
    // =========================================================================
    // Make the global string truncator RichText-aware
    // =========================================================================
    [HarmonyPatch(typeof(GenText), nameof(GenText.Truncate), new[] { typeof(string), typeof(float), typeof(Dictionary<string, string>) })]
    public static class Patch_GenText_Truncate
    {
        public static bool Prefix(string str, float width, Dictionary<string, string> cache, ref string __result)
        {
            if (string.IsNullOrEmpty(str))
                return true;

            if (str.IndexOf("<color=", System.StringComparison.Ordinal) == -1)
                return true;

            if (cache != null && cache.TryGetValue(str, out __result))
            {
                return false;
            }

            TaggedString tagged = new TaggedString(str);
            TaggedString safelyTruncated = tagged.Truncate(width, null);

            __result = safelyTruncated.RawText;

            if (cache != null)
            {
                cache[str] = __result;
            }

            return false;
        }
    }
}
