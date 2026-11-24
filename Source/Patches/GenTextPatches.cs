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
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace BetterTradeColors.Patches;

[HarmonyPatch(typeof(GenText), nameof(GenText.Truncate), new[] { typeof(string), typeof(float), typeof(Dictionary<string, string>) })]
public static class Patch_GenText_Truncate
{
    public static bool Prefix(string str, float width, ref string __result)
    {
        if (!string.IsNullOrEmpty(str) && !LabelRichText.HasColorMarkup(str))
            return true;

        __result = string.IsNullOrEmpty(str) ? str : LabelRichText.Truncate(str, width);
        return false;
    }
}

[HarmonyPatch(typeof(GenText), nameof(GenText.Truncate), new[] { typeof(TaggedString), typeof(float), typeof(Dictionary<string, TaggedString>) })]
internal static class Patch_GenText_TruncateTaggedString
{
    private static bool Prefix(TaggedString str, float width, ref TaggedString __result)
    {
        string text = str.RawText;
        if (!string.IsNullOrEmpty(text) && !LabelRichText.HasColorMarkup(text))
            return true;

        __result = string.IsNullOrEmpty(text) ? str : new TaggedString(LabelRichText.Truncate(text, width, tagged: true));
        return false;
    }
}

[HarmonyPatch(typeof(Text), nameof(Text.ClampTextWithEllipsis))]
internal static class Patch_Text_ClampTextWithEllipsis
{
    private static bool Prefix(Rect rect, string text, ref string __result)
    {
        // Keep the same inset as the vanilla clamp.
        return Patch_GenText_Truncate.Prefix(text, rect.width - 13f, ref __result);
    }
}
