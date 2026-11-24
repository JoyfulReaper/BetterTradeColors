using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using BetterTradeColors;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

internal static partial class Program
{
    private const string Blue = "<color=#33CCFF>";
    private const string Close = "</color>";
    private static readonly Regex Markup = new(@"</?(?:color(?:=[^>]+)?|b|i|size(?:=[^>]+)?)>",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static int _measurements;
    private static int _vanillaMeasurements;
    private static int _vanillaTaggedMeasurements;
    private static int _textFrame;
    private static PrefsData _textPrefs;

    private static void SetUpTextChecks(Harmony harmony)
    {
        LanguageDatabase.activeLanguage = new LoadedLanguage("English")
        {
            info = new LanguageInfo { canBeTiny = false }
        };
        _textPrefs = new PrefsData { uiScale = 1f };
        AccessTools.Field(typeof(Prefs), "data").SetValue(null, _textPrefs);
        harmony.Patch(AccessTools.Method(typeof(GUIStyle), nameof(GUIStyle.CalcSize), new[] { typeof(GUIContent) }),
            prefix: new HarmonyMethod(typeof(Program), nameof(MeasureTextForTest)));
        harmony.Patch(AccessTools.PropertyGetter(typeof(Time), nameof(Time.frameCount)),
            prefix: new HarmonyMethod(typeof(Program), nameof(FrameForTest)));

        string sample = Blue + "test item" + Close;
        int before = _measurements;
        string vanilla = GenText.Truncate(sample, 7f, null);
        _vanillaMeasurements = _measurements - before;
        before = _measurements;
        string tagged = GenText.Truncate(new TaggedString(sample), 7f, null).RawText;
        _vanillaTaggedMeasurements = _measurements - before;
        string clamped = Text.ClampTextWithEllipsis(new Rect(0, 0, 20, 20), sample);
        Console.WriteLine($"VANILLA (simulated): string={vanilla}, tagged={tagged}, clamp={clamped}");
        Check(vanilla == Blue + "test..." && tagged == "test..." &&
            clamped == Blue + "test...", "Unpatched 1.7 width truncators reproduce loss of markup");
    }

    private static bool FrameForTest(ref int __result)
    {
        __result = _textFrame;
        return false;
    }

    // Deterministic fixture for decisions and allocations. It does not render text.
    private static bool MeasureTextForTest(GUIContent content, ref Vector2 __result)
    {
        _measurements++;
        string text = content.text ?? "";
        float width = 0f;
        float size = 1f;
        bool bold = false;
        var sizes = new Stack<float>();
        int start = 0;
        foreach (Match match in Markup.Matches(text))
        {
            width += new StringInfo(text.Substring(start, match.Index - start)).LengthInTextElements * size * (bold ? 2f : 1f);
            if (match.Value.StartsWith("<size=", StringComparison.OrdinalIgnoreCase))
            {
                sizes.Push(size);
                size = float.Parse(match.Value.AsSpan(6, match.Length - 7), CultureInfo.InvariantCulture) / 10f;
            }
            else if (match.Value.Equals("</size>", StringComparison.OrdinalIgnoreCase) && sizes.Count > 0)
                size = sizes.Pop();
            else if (match.Value.Equals("<b>", StringComparison.OrdinalIgnoreCase))
                bold = true;
            else if (match.Value.Equals("</b>", StringComparison.OrdinalIgnoreCase))
                bold = false;
            start = match.Index + match.Length;
        }

        width += new StringInfo(text.Substring(start)).LengthInTextElements * size * (bold ? 2f : 1f);
        __result = new Vector2(width * (Text.Font == GameFont.Medium ? 2f : 1f) * _textPrefs.uiScale, 1f);
        return false;
    }

    private static void VerifyRichText(ThingWithComps thing)
    {
        string label = "test item";
        string input = Blue + label + Close;
        var tagged = new TaggedString(input);
        var plainCache = new Dictionary<string, string>();
        Check(GenText.Truncate("plain label", 6f, plainCache) == "pla..." &&
            plainCache["plain label"] == "pla..." && GenText.Truncate("plain label", 80f, plainCache) == "pla...", "Plain text uses vanilla truncation and caller cache");
        Check(GenText.Truncate((string)null, 0f, null) == null &&
            GenText.Truncate("", -1f, null) == "" &&
            GenText.Truncate(default(TaggedString), 0f, null).RawText == null &&
            GenText.Truncate(TaggedString.Empty, 0f, null).RawText == "", "Null and empty inputs pass through safely");
        Check(ReferenceEquals(GenText.Truncate(input, 9f, null), input) &&
            GenText.Truncate(tagged, 9f, null).RawText == input, "Both overloads preserve untruncated markup at exact width");
        Check(GenText.Truncate(input, 7f, null) == Blue + "test" + Close + "..." &&
            GenText.Truncate(tagged, 7f, null).RawText == Blue + "test" + Close + "...",
            "Both overloads produce a balanced colored prefix");
        Check(Text.ClampTextWithEllipsis(new Rect(0, 0, 20, 20), input) == Blue + "test" + Close + "...",
            "Cell inspector clamp preserves the vanilla inset and closes color");
        Check(GenText.Truncate(input, 0f, null) == "" && GenText.Truncate(input, 2f, null) == "" &&
            GenText.Truncate(input, 3f, null) == "...", "Narrow columns return only what fits");

        string mixed = Blue + "<b><i>abcdef</i></b>ABCDEF" + Close;
        Check(GenText.Truncate(mixed, 10f, null) == Blue + "<b><i>abc</i></b>" + Close + "...",
            "Nested bold and italic survive and contribute to measurement");
        Check(GenText.Truncate(Blue + "<size=20>abcd</size>abcd" + Close, 10f, null) ==
            Blue + "<size=20>abc</size>" + Close + "...", "Size markup contributes to measured width");
        string adjacent = "<color=red>red</color><COLOR=blue>blues</COLOR>";
        Check(GenText.Truncate(adjacent, 7f, null) == "<color=red>red</color><COLOR=blue>b</color>...",
            "Adjacent spans preserve preceding colors and close the active span");
        Check(GenText.Truncate(Blue + "<color=red>abcdefghi</color> tail" + Close, 7f, null) ==
            Blue + "<color=red>abcd</color>" + Close + "...", "Nested colors close in reverse order");
        Check(GenText.Truncate(Blue + "test</color", 100f, null) == Blue + "test" + Close &&
            GenText.Truncate("test</color><color=", 100f, null) == "test" &&
            GenText.Truncate(Blue + "<b>test</b", 100f, null) == Blue + "<b>test</b>" + Close, "Incomplete and unmatched supported tags are repaired");
        Check(GenText.Truncate(new TaggedString(Blue + "(*Name)Ab(/Name)abcdef" + Close), 5f, null).RawText ==
            Blue + "(*Name)Ab(/Name)" + Close + "...", "TaggedString preserves complete semantic spans");
        Check(GenText.Truncate(new TaggedString(Blue + "(*Name)Ab(/Name)abcdef" + Close), 4f, null).RawText == "...",
            "TaggedString never cuts inside a semantic span");
        string unicode = Blue + "e\u0301👩🏽‍🔧漢字🙂123" + Close;
        Check(GenText.Truncate(unicode, 5f, null) == Blue + "e\u0301👩🏽‍🔧" + Close + "...", "Unicode cuts preserve grapheme clusters");

        SetQuality(thing, QualityCategory.Legendary);
        Check(ColorLabel(thing, input) == input && ColorLabel(thing, adjacent) == adjacent,
            "Repeated application and fully precolored names avoid redundant wrappers");
        string partiallyColored = "<b>test</b> <color=red>item</color>";
        Check(ColorLabel(thing, partiallyColored) == Blue + partiallyColored + Close,
            "Existing bold and local color overrides are preserved");
        string stackLabel = GenLabel.ThingLabel(thing, 75, false, false);
        Check(stackLabel == input + " x75" && GenText.Truncate(stackLabel, 100f, null) == stackLabel
            && GenText.Truncate(stackLabel, 8f, null) == Blue + "test " + Close + "...",
            "GenLabel preserves the original uncolored stack suffix and normal tail truncation");
        string repeatedStack = "</color>" + Blue + "test item x75";
        Patch_GenLabel_ThingLabel.Postfix(thing, 75, ref repeatedStack);
        Check(repeatedStack == stackLabel, "Counted labels keep same-length repairs and avoid a second wrapper");
        var stack = new ThingWithComps
        {
            def = new ThingDef { label = label, useHitPoints = true, stackLimit = 75 },
            HitPoints = 10,
            stackCount = 75
        };
        Check(stack.Label == "<color=#D933BF>test item</color> x75",
            "Thing.Label also appends its stack count outside the color");

        var caller = new Dictionary<string, string> { [input] = "stale caller result" };
        var taggedCaller = new Dictionary<string, TaggedString> { [input] = new TaggedString("stale tagged result") };
        _textFrame++;
        int before = _measurements;
        string narrow = GenText.Truncate(input, 6f, caller);
        int cold = _measurements - before;
        Check(cold > 0 && narrow == Blue + "tes" + Close + "..." &&
            ReferenceEquals(narrow, GenText.Truncate(input, 6f, null)) && _measurements == before + cold,
            "Same-frame cache hit reuses balanced output without measurement");
        Check(GenText.Truncate(input, 8f, caller) == Blue + "test " + Close + "..." &&
            GenText.Truncate(tagged, 6f, taggedCaller).RawText == narrow &&
            caller[input] == "stale caller result" && taggedCaller[input].RawText == "stale tagged result",
            "Colored callers use width-aware results and retain ownership of their dictionaries");
        Text.Font = GameFont.Medium;
        Check(GenText.Truncate(input, 8f, caller) == Blue + "t" + Close + "...", "Cache distinguishes font changes");
        Text.Font = GameFont.Small;
        _textPrefs.uiScale = 2f;
        Check(GenText.Truncate(input, 8f, caller) == Blue + "t" + Close + "...", "Cache distinguishes UI scale changes");
        _textPrefs.uiScale = 1f;
        before = _measurements;
        _textFrame++;
        Check(GenText.Truncate(input, 6f, caller) == narrow && _measurements > before, "Cached measurements expire at the next frame");

        BetterTradeColorsMod.Settings.TrySetColor((int)QualityCategory.Legendary, "#A1B2C3");
        string updated = GenLabel.ThingLabel(thing, 1, false, false);
        Check(GenText.Truncate(updated, 6f, caller) == "<color=#A1B2C3>tes</color>...",
            "Settings edits apply without clearing vanilla or caller caches");
        BetterTradeColorsMod.Settings.ResetDefaults();

        const int iterations = 20000;
        _textFrame++;
        before = _measurements;
        GenText.Truncate(input, 7f, null);
        int sampleMeasurements = _measurements - before;
        before = _measurements;
        var watch = Stopwatch.StartNew();
        long bytesBefore = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < iterations; i++)
            GenText.Truncate(input, 7f, null);
        watch.Stop();
        long bytes = GC.GetAllocatedBytesForCurrentThread() - bytesBefore;
        Check(_measurements == before, "Warm truncation loop performs zero simulated measurements");
        Console.WriteLine($"PERF (simulated): {iterations:N0} warm calls, {watch.Elapsed.TotalMilliseconds:F2} ms, {bytes:N0} bytes. Cold sample: {sampleMeasurements} measurements. Vanilla sample: {_vanillaMeasurements} (string), {_vanillaTaggedMeasurements} (tagged).");
    }

    private static void SetQuality(ThingWithComps thing, QualityCategory quality) =>
        AccessTools.Field(typeof(CompQuality), "qualityInt").SetValue(thing.compQuality, quality);
}
