using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using BetterTradeColors;
using BetterTradeColors.Patches;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

internal static partial class Program
{
    private static void VerifyRoomQuality(string rimWorldDir, string owner)
    {
        VerifyRoomPatch(typeof(EnvironmentStatsDrawer), nameof(EnvironmentStatsDrawer.DoRoomInfo), 1, owner);
        VerifyRoomPatch(typeof(CellInspectorDrawer), "DrawMapInspector", 1, owner);
        VerifyRoomPatch(typeof(Gizmo_RoomStats), nameof(Gizmo_RoomStats.GizmoOnGUI), 2, owner);

        XDocument localization = XDocument.Load("Shared/Languages/English/Keyed/BetterTradeColors.xml");
        foreach (XElement entry in localization.Root!.Elements())
            LanguageDatabase.activeLanguage.keyedReplacements[entry.Name.LocalName] =
                new LoadedLanguage.KeyedReplacement { key = entry.Name.LocalName, value = entry.Value };
        // The standalone fixture provides the actual keyed strings without loading game assets.
        AccessTools.Field(typeof(LoadedLanguage), "dataIsLoaded").SetValue(LanguageDatabase.activeLanguage, true);
        Check("BTC_RoomQualitySettings".Translate().RawText == "Room Quality"
            && "BTC_RoomPaletteLevel".Translate(1, 7).RawText == "Palette level 1 of 7", "Room settings use shared localization");

        XDocument roomStats = XDocument.Load(Path.Combine(rimWorldDir, "Data", "Core", "Defs", "Rooms", "RoomStats.xml"));
        var coreStats = roomStats.Root!.Elements("RoomStatDef")
            .Where(def => def.Element("scoreStages") != null)
            .Select(def => new RoomStatDef
            {
                defName = (string)def.Element("defName")!,
                scoreStages = def.Element("scoreStages")!.Elements("li").Select(stage => new RoomStatScoreStage
                {
                    label = (string)stage.Element("label")!,
                    minScore = stage.Element("minScore") is { } min
                        ? float.Parse(min.Value, CultureInfo.InvariantCulture) : float.MinValue
                }).ToList()
            }).ToArray();

        var settings = BetterTradeColorsMod.Settings;
        Check(!settings.RoomFormattingEnabled, "Room colors and numbers are both off by default");
        var impressive = coreStats.Single(stat => stat.defName == "Impressiveness");
        const float TestScore = 85f;
        var example = impressive.GetScoreStage(TestScore);
        string exampleLabel = example.label.CapitalizeFirst();
        Check(RoomQualityPatches.FormatStage(example, impressive, true, out _) == exampleLabel
            && RoomQualityPatches.FormatStage(example, impressive, false, out _) == example.label,
            "Disabled formatting retains each view's vanilla capitalization");

        settings.ShowRoomStage = true;
        string number = $" ({impressive.GetScoreStageIndex(TestScore) + 1}/{impressive.scoreStages.Count})";
        Check(RoomQualityPatches.FormatStage(example, impressive, true, out _) == exampleLabel + number,
            "Numbers can be enabled without colors");
        settings.ShowRoomStage = false;
        settings.ColorRoomQuality = true;
        Check(RoomQualityPatches.FormatStage(example, impressive, true, out _) == $"<color=#CCCC33>{exampleLabel}</color>",
            "Color can be enabled without numbers and uses the nearest relative palette slot");
        settings.ShowRoomStage = true;

        foreach (string name in new[] { "Impressiveness", "Beauty", "Cleanliness", "Space", "Wealth" })
        {
            var stat = coreStats.Single(stat => stat.defName == name);
            int count = stat.scoreStages.Count;
            bool boundariesMatch = Enumerable.Range(1, count - 1).All(i =>
                stat.GetScoreStageIndex(MathF.BitDecrement(stat.scoreStages[i].minScore)) == i - 1
                && ReferenceEquals(stat.GetScoreStage(stat.scoreStages[i].minScore), stat.scoreStages[i]));
            Check(boundariesMatch, $"Actual {name} thresholds select all {count} stages inclusively");
            Check(RoomQualityPatches.FormatStage(stat.GetScoreStage(float.MinValue), stat, false, out _)
                    == $"<color=#666666>{stat.scoreStages[0].label}</color> (1/{count})"
                && RoomQualityPatches.FormatStage(stat.GetScoreStage(float.MaxValue), stat, false, out _)
                    == $"<color=#33CCFF>{stat.scoreStages[count - 1].label}</color> ({count}/{count})",
                $"{name} maps its own {count} stages to the room palette endpoints");
        }

        var customStage = new RoomStatScoreStage { label = "<i>very clean</i>" };
        var custom = new RoomStatDef { scoreStages = [customStage] };
        string formatted = RoomQualityPatches.FormatStage(custom.GetScoreStage(0f), custom, false, out string indicator);
        Check(formatted == "<color=#66CC66><i>very clean</i></color> (1/1)"
            && RoomQualityPatches.FormatDescription(formatted, 0, 1, out _) == formatted,
            "Single custom stage uses the midpoint and repeated formatting is stable");
        Check(GenText.Truncate(formatted, 7f, null) == "<color=#66CC66><i>very</i></color>..."
            && Text.ClampTextWithEllipsis(new Rect(0, 0, 20, 20), formatted) == "<color=#66CC66><i>very</i></color>...",
            "Room and cell inspector truncation close mixed formatting");
        Check(RoomQualityPatches.FitStage(formatted + " (2.5)", 13f, indicator) ==
            "<color=#66CC66><i>very</i></color>... (1/1)"
            && RoomQualityPatches.FitStage(formatted, 5f, indicator) == "(1/1)"
            && RoomQualityPatches.FitStage(formatted, 2f, indicator) == "",
            "Narrow columns prioritize a complete indicator when it fits");
        customStage.label = "<color=red>very clean</color>";
        Check(RoomQualityPatches.FormatStage(customStage, custom, false, out _) == "<color=red>very clean</color> (1/1)",
            "Precolored room descriptions keep their existing color without nesting");
        var noStages = new RoomStatDef();
        var emptyStages = new RoomStatDef { scoreStages = [] };
        Check(RoomQualityPatches.FormatStage(noStages.GetScoreStage(0f), noStages, true, out _) == ""
            && RoomQualityPatches.FormatStage(emptyStages.GetScoreStage(0f), emptyStages, true, out _) == ""
            && RoomQualityPatches.FormatStage(customStage, null, false, out _) == customStage.label
            && RoomQualityPatches.FormatDescription("orphan stage", -1, 1, out _) == "orphan stage",
            "Missing, empty, and unmatched stage definitions receive no new formatting");

        Check(settings.TrySetRoomColor(3, "a1b2c3") && !settings.TrySetRoomColor(3, "#bad")
            && settings.RoomQualityTag(0, 1) == "<color=#A1B2C3>", "Room color edits update cached tags and reject invalid input");
        settings.ResetDefaults();
        Check(settings.RoomFormattingEnabled && settings.RoomQualityTag(0, 1) == "<color=#A1B2C3>",
            "Item reset preserves room settings");
        settings.TrySetColor((int)QualityCategory.Legendary, "#123456");
        settings.ResetRoomDefaults();
        Check(!settings.RoomFormattingEnabled && settings.RoomQualityTag(0, 1) == "<color=#66CC66>"
            && settings.QualityTag(QualityCategory.Legendary) == "<color=#123456>",
            "Room reset restores defaults and preserves the item palette");
        settings.ResetDefaults();
    }

    private static void VerifyRoomPatch(Type type, string methodName, int expectedSites, string owner)
    {
        MethodBase original = AccessTools.DeclaredMethod(type, methodName);
        MethodInfo format = AccessTools.Method(typeof(RoomQualityPatches), nameof(RoomQualityPatches.FormatStage));
        int sites = PatchProcessor.GetCurrentInstructions(original).Count(instruction => instruction.Calls(format));
        Check(Harmony.GetPatchInfo(original).Transpilers.Any(patch => patch.owner == owner) && sites == expectedSites,
            $"{type.Name}.{methodName} patches all {expectedSites} vanilla stage labels");
    }
}
