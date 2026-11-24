using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using Verse;

namespace BetterTradeColors.Patches;

internal static class LabelRichText
{
    private const string Ellipsis = "...";
    private const string CloseColor = "</color>";
    private const int CacheCapacity = 256;
    private const int MaxCachedLength = 1024;

    private enum Format { None, Color, Bold, Italic, Size }

    private static readonly (
        (string Text, float Width, GameFont Font, float Scale, bool Semantic) Key,
        string Result)[] Cache = new (
        (string, float, GameFont, float, bool), string)[CacheCapacity];
    private static int _cacheFrame = -1;

    internal static bool HasColorMarkup(string text) =>
        text.IndexOf('<') >= 0 && (text.IndexOf("<color=", StringComparison.OrdinalIgnoreCase) >= 0 ||
        text.IndexOf("</color", StringComparison.OrdinalIgnoreCase) >= 0);

    internal static string ApplyColor(string text, string tag, int stackCount)
    {
        string count = stackCount > 1 ? stackCount.ToStringCached() : null;
        int labelLength = text.Length;
        if (count != null)
        {
            int suffix = text.Length - count.Length - 2;
            if (suffix >= 0 && Matches(text, suffix, " x") && text.EndsWith(count, StringComparison.Ordinal))
                labelLength = suffix;
        }

        if (text.IndexOf('<') < 0 && labelLength == text.Length)
            return string.Concat(tag, text, CloseColor);

        string originalLabel = labelLength == text.Length ? text : text.Substring(0, labelLength);
        string label = Balance(originalLabel, out bool fullyColored);
        if (labelLength == text.Length)
            return fullyColored ? label : string.Concat(tag, label, CloseColor);
        if (fullyColored && ReferenceEquals(label, originalLabel))
            return text;

        var result = new StringBuilder(text.Length + tag.Length + CloseColor.Length);
        if (!fullyColored)
            result.Append(tag);
        result.Append(label);
        if (!fullyColored)
            result.Append(CloseColor);
        return result.Append(text, labelLength, text.Length - labelLength).ToString();
    }

    internal static string Truncate(string text, float width, bool tagged = false)
    {
        if (!(width > 0f))
            return "";

        bool semantic = tagged && text.IndexOf("(*", StringComparison.Ordinal) >= 0;
        if (text.Length > MaxCachedLength)
            return TruncateUncached(text, width, semantic);

        // Colored callers' dictionaries have no width or typography in their keys.
        // Restrict reuse to a frame so replaced fonts or layouts cannot outlive the pass.
        int frame = Time.frameCount;
        if (frame != _cacheFrame)
        {
            Array.Clear(Cache, 0, Cache.Length);
            _cacheFrame = frame;
        }

        var key = (text, width, Text.Font, Prefs.UIScale, semantic);
        int index = key.GetHashCode() & (CacheCapacity - 1);
        ref var entry = ref Cache[index];
        if (entry.Key.Equals(key))
            return entry.Result;

        string result = TruncateUncached(text, width, semantic);
        entry = (key, result);
        return result;
    }

    private static string TruncateUncached(string text, float width, bool semantic)
    {
        text = Balance(text, out _, semantic);
        GUIStyle style = Text.CurFontStyle;
        var content = new GUIContent(text);
        if (Measure(style, content, semantic) <= width)
            return text;

        content.text = Ellipsis;
        if (Measure(style, content, semantic) > width)
            return "";

        var visible = new StringBuilder(text.Length);
        var cuts = new List<(int End, int Scope)>(text.Length);
        var scopes = new List<(Format Format, int Parent)>(4);
        int scope = -1;
        for (int i = 0; i < text.Length;)
        {
            Format format = ReadFormat(text, i, out int end, out bool closing, out _);
            if (format != Format.None)
            {
                if (closing)
                    scope = scopes[scope].Parent;
                else
                {
                    scopes.Add((format, scope));
                    scope = scopes.Count - 1;
                }
                i = end;
                continue;
            }

            // A RimWorld semantic span must reach ColoredText.Resolve intact.
            if (semantic && TryReadSemanticSpan(text, i, out end))
            {
                visible.Append('\uFFFC');
                i = end;
            }
            else
            {
                visible.Append(text[i++]);
            }
            cuts.Add((i, scope));
        }

        string best = Ellipsis;
        int[] elements = StringInfo.ParseCombiningCharacters(visible.ToString());
        int low = 1;
        int high = elements.Length;
        var candidate = new StringBuilder(text.Length + Ellipsis.Length);
        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            int next = middle == elements.Length ? cuts.Count : elements[middle];
            var cut = cuts[next - 1];
            candidate.Clear();
            candidate.Append(text, 0, cut.End);
            for (int open = cut.Scope; open >= 0; open = scopes[open].Parent)
                candidate.Append(ClosingTag(scopes[open].Format));

            // Measure every candidate with the same ellipsis style.
            content.text = candidate.Append(Ellipsis).ToString();
            if (Measure(style, content, semantic) <= width)
            {
                best = content.text;
                low = middle + 1;
            }
            else
            {
                high = middle - 1;
            }
        }
        return best;
    }

    private static float Measure(GUIStyle style, GUIContent content, bool semantic)
    {
        // Text.CalcSize strips markup. The label style can measure renderer tags
        // directly. Keep the game's stripped-width estimate for unresolved Verse tags.
        return semantic ? Text.CalcSize(content.text).x : style.CalcSize(content).x;
    }

    private static string Balance(string text, out bool fullyColored, bool semantic = false)
    {
        fullyColored = true;
        List<Format> open = null;
        int colorDepth = 0;
        StringBuilder repaired = null;
        int copied = 0;
        for (int i = 0; i < text.Length;)
        {
            if (semantic && TryReadSemanticSpan(text, i, out int spanEnd))
            {
                if (colorDepth == 0)
                    fullyColored = false;
                i = spanEnd;
                continue;
            }

            Format format = ReadFormat(text, i, out int end, out bool closing, out bool complete);
            if (format == Format.None)
            {
                if (colorDepth == 0 && !char.IsWhiteSpace(text[i]))
                    fullyColored = false;
                i++;
                continue;
            }

            if (!complete || (closing && (open == null || open.Count == 0 || open[open.Count - 1] != format)))
            {
                repaired ??= new StringBuilder(text.Length);
                repaired.Append(text, copied, i - copied);
                copied = end;
            }
            else if (closing)
            {
                open.RemoveAt(open.Count - 1);
                if (format == Format.Color)
                    colorDepth--;
            }
            else
            {
                (open ??= new List<Format>(4)).Add(format);
                if (format == Format.Color)
                    colorDepth++;
            }
            i = end;
        }

        int openCount = open?.Count ?? 0;
        if (repaired == null && openCount == 0)
            return text;
        repaired ??= new StringBuilder(text.Length + CloseColor.Length);
        repaired.Append(text, copied, text.Length - copied);
        for (int i = openCount - 1; i >= 0; i--)
            repaired.Append(ClosingTag(open[i]));
        return repaired.ToString();
    }

    private static Format ReadFormat(string text, int start, out int end, out bool closing, out bool complete)
    {
        end = start;
        closing = false;
        complete = false;
        if (text[start] != '<')
            return Format.None;

        Format format;
        if (Matches(text, start, "<color=") || MatchesTagName(text, start, "</color"))
            format = Format.Color;
        else if (Matches(text, start, "<size=") || MatchesTagName(text, start, "</size"))
            format = Format.Size;
        else if (MatchesTagName(text, start, "<b") || MatchesTagName(text, start, "</b"))
            format = Format.Bold;
        else if (MatchesTagName(text, start, "<i") || MatchesTagName(text, start, "</i"))
            format = Format.Italic;
        else
            return Format.None;

        closing = text[start + 1] == '/';
        end = start + 1;
        while (end < text.Length && text[end] != '<' && text[end] != '>')
            end++;
        if (end < text.Length && text[end] == '>')
        {
            end++;
            complete = closing
                ? end - start == ClosingTag(format).Length
                : format == Format.Bold || format == Format.Italic || end - start > (format == Format.Color ? 8 : 7);
        }
        return format;
    }

    private static bool TryReadSemanticSpan(string text, int start, out int end)
    {
        end = start;
        if (!Matches(text, start, "(*"))
            return false;
        int headerEnd = text.IndexOf(')', start + 2);
        if (headerEnd < 0)
            return false;
        int argument = text.IndexOf('=', start + 2, headerEnd - start - 2);
        int nameLength = (argument < 0 ? headerEnd : argument) - (start + 2);
        if (nameLength == 0)
            return false;

        int closer = text.IndexOf("(/", headerEnd + 1, StringComparison.Ordinal);
        if (closer < 0 || closer + 2 + nameLength >= text.Length || text[closer + 2 + nameLength] != ')' ||
            string.CompareOrdinal(text, start + 2, text, closer + 2, nameLength) != 0)
            return false;
        end = closer + 3 + nameLength;
        return true;
    }

    private static bool MatchesTagName(string text, int start, string name)
    {
        int end = start + name.Length;
        return Matches(text, start, name) && (end == text.Length || text[end] == '>' || text[end] == '<');
    }

    private static bool Matches(string text, int start, string value) =>
        start <= text.Length - value.Length &&
        string.Compare(text, start, value, 0, value.Length, StringComparison.OrdinalIgnoreCase) == 0;

    private static string ClosingTag(Format format) => format switch
    {
        Format.Color => CloseColor,
        Format.Bold => "</b>",
        Format.Italic => "</i>",
        Format.Size => "</size>",
        _ => throw new ArgumentOutOfRangeException(nameof(format))
    };
}
