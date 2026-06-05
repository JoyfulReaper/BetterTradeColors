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
