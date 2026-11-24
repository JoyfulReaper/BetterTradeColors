using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Xml;
using BetterTradeColors;
using BetterTradeColors.Patches;
using BetterTradeColors.Settings;
using HarmonyLib;
using RimWorld;
using Verse;

internal static partial class Program
{
    // Resolve installed dependencies without copying game or Harmony DLLs into the mod.
    public static int Main(string[] args)
    {
        if (args.Length != 2)
        {
            Console.Error.WriteLine("Usage: CompatibilityChecks <RimWorldDir> <HarmonyDir>");
            return 2;
        }

        AssemblyLoadContext.Default.Resolving += (context, name) =>
        {
            foreach (string directory in new[] { args[0], Path.Combine(args[1], "1.7", "Assemblies") })
            {
                string path = Path.GetFullPath(Path.Combine(directory, name.Name + ".dll"));
                if (File.Exists(path))
                    return context.LoadFromAssemblyPath(path);
            }
            return null;
        };

        return RunChecks(args[0]);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int RunChecks(string rimWorldDir)
    {
        const string owner = "BetterTradeColors.CompatibilityChecks";
        BetterTradeColorsMod.Settings = new BetterTradeColorsSettings();
        var harmony = new Harmony(owner);
        try
        {
            // Supply HP before patching labels so the standalone CLR cannot inline
            // the game's stat lookup, which requires a running game's databases.
            harmony.Patch(AccessTools.PropertyGetter(typeof(Thing), nameof(Thing.MaxHitPoints)),
                prefix: new HarmonyMethod(typeof(Program), nameof(MaxHitPoints)));
            SetUpTextChecks(harmony);
            harmony.PatchAll(typeof(BetterTradeColorsMod).Assembly);
            var labelMethod = AccessTools.Method(typeof(GenLabel), nameof(GenLabel.ThingLabel),
                new[] { typeof(Thing), typeof(int), typeof(bool), typeof(bool) });
            var truncateMethod = AccessTools.Method(typeof(GenText), nameof(GenText.Truncate),
                new[] { typeof(string), typeof(float), typeof(Dictionary<string, string>) });
            Check(Harmony.GetPatchInfo(labelMethod).Postfixes.Any(p => p.owner == owner), "ThingLabel patch applied");
            Check(Harmony.GetPatchInfo(truncateMethod).Prefixes.Any(p => p.owner == owner), "Truncate patch applied");

            var thing = new ThingWithComps { def = new ThingDef { label = "test item", useHitPoints = false } };
            Check(GenLabel.ThingLabel(thing, 1, false, false) == "test item", "Uncolored label passes through");
            var quality = new CompQuality { parent = thing };
            thing.compQuality = quality;
            string[] colors = { "#666666", "#999999", null, "#66CC66", "#CCCC33", "#FF9933", "#33CCFF" };
            for (int i = 0; i < colors.Length; i++)
            {
                AccessTools.Field(typeof(CompQuality), "qualityInt").SetValue(quality, (QualityCategory)i);
                string expected = colors[i] == null ? "test item" : $"<color={colors[i]}>test item</color>";
                Check(GenLabel.ThingLabel(thing, 1, false, false) == expected, $"Quality tier {(QualityCategory)i}");
            }

            thing.def.useHitPoints = true;
            thing.HitPoints = 49;
            Check(GenLabel.ThingLabel(thing, 1, false, false) == "<color=#D933BF>test item</color>", "Damage overrides quality below 50%");
            thing.HitPoints = 50;
            Check(GenLabel.ThingLabel(thing, 1, false, false) == "<color=#33CCFF>test item</color>", "50% HP retains quality color");

            var apparel = new Apparel { def = thing.def, HitPoints = 1, WornByCorpse = true };
            string label = "tainted item";
            Patch_GenLabel_ThingLabel.Postfix(apparel, 1, ref label);
            Check(label == "<color=#B24C4C>tainted item</color>", "Taint overrides damage");

            VerifyColorSettings(thing, apparel);
            VerifySettingsXml();

            VerifyRichText(thing);
            VerifyRoomQuality(rimWorldDir, owner);
            VerifyLoadFolders();
            Console.WriteLine("Compatibility checks passed. In-game rendering still requires verification.");
            return 0;
        }
        finally
        {
            harmony.UnpatchAll(owner);
        }
    }

    private static void VerifyColorSettings(ThingWithComps thing, Apparel apparel)
    {
        var settings = BetterTradeColorsMod.Settings;
        apparel.compQuality = thing.compQuality;
        thing.HitPoints = 49;

        settings.ColorTainted = false;
        Check(ColorLabel(apparel) == "<color=#D933BF>test item</color>", "Disabling taint falls through to damage");
        settings.ColorDamaged = false;
        Check(ColorLabel(apparel) == "<color=#33CCFF>test item</color>", "Disabling damage falls through to quality");
        settings.ColorQuality = false;
        Check(ColorLabel(apparel) == "test item", "All three toggles can be disabled");
        settings.ColorTainted = true;
        Check(ColorLabel(apparel) == "<color=#B24C4C>test item</color>", "Taint works with damage and quality disabled");
        settings.ColorTainted = false;
        settings.ColorDamaged = true;
        Check(ColorLabel(apparel) == "<color=#D933BF>test item</color>", "Damage works with taint and quality disabled");

        settings.DamageThreshold = 75;
        thing.HitPoints = 74;
        Check(GenLabel.ThingLabel(thing, 1, false, false) == "<color=#D933BF>test item</color>", "Custom threshold colors non-apparel below the boundary");
        thing.HitPoints = 75;
        Check(GenLabel.ThingLabel(thing, 1, false, false) == "test item", "Custom threshold excludes its exact boundary");
        thing.HitPoints = 76;
        Check(ColorLabel(thing) == "test item", "HP above the threshold is uncolored");
        thing.HitPoints = 0;
        settings.DamageThreshold = -1;
        Check(settings.DamageThreshold == 0 && ColorLabel(thing) == "test item", "Zero threshold leaves zero HP uncolored");
        settings.DamageThreshold = 120;
        thing.HitPoints = 99;
        Check(settings.DamageThreshold == 100 && ColorLabel(thing) == "<color=#D933BF>test item</color>", "100% threshold colors 99% HP");
        thing.HitPoints = 100;
        Check(ColorLabel(thing) == "test item", "100% threshold excludes full HP");

        settings.ColorQuality = true;
        string originalTag = settings.QualityTag(QualityCategory.Legendary);
        Check(settings.TrySetColor((int)QualityCategory.Legendary, "  badeed  "), "Hex colors accept lowercase and an optional hash");
        string customTag = settings.QualityTag(QualityCategory.Legendary);
        Check(customTag == "<color=#BADEED>" && !ReferenceEquals(originalTag, customTag), "Editing a color refreshes its cached tag");
        Check(ReferenceEquals(customTag, settings.QualityTag(QualityCategory.Legendary)), "Lookups reuse the cached tag");
        Check(settings.TrySetColor((int)QualityCategory.Legendary, "#badeed")
            && ReferenceEquals(customTag, settings.QualityTag(QualityCategory.Legendary)), "Equivalent edits reuse the cached tag");
        Check(GenLabel.ThingLabel(thing, 1, false, false) == "<color=#BADEED>test item</color>", "Live palette applies above vanilla's cached label");
        Check(!settings.TrySetColor((int)QualityCategory.Legendary, "#00ZZ99")
            && !settings.TrySetColor((int)QualityCategory.Legendary, "#00AA99FF")
            && !settings.TrySetColor((int)QualityCategory.Legendary, "#12345\0")
            && !settings.TrySetColor((int)QualityCategory.Legendary, null)
            && ReferenceEquals(customTag, settings.QualityTag(QualityCategory.Legendary)), "Malformed colors leave the valid cached tag intact");

        Check(settings.TrySetColor(BetterTradeColorsSettings.TaintedColor, "#102030")
            && settings.TrySetColor(BetterTradeColorsSettings.DamagedColor, "#ABCDEF"), "Both condition colors can be set");
        settings.ColorTainted = true;
        Check(ColorLabel(apparel) == "<color=#102030>test item</color>", "Custom taint overrides custom damage");
        settings.ColorTainted = false;
        Check(ColorLabel(apparel) == "<color=#ABCDEF>test item</color>", "Custom damage overrides quality");

        FieldInfo qualityField = AccessTools.Field(typeof(CompQuality), "qualityInt");
        qualityField.SetValue(thing.compQuality, QualityCategory.Normal);
        Check(settings.TrySetColor((int)QualityCategory.Normal, "#00AB12")
            && ColorLabel(thing) == "<color=#00AB12>test item</color>", "Normal accepts a custom color");
        Check(ColorLabel(new ThingWithComps { def = thing.def, HitPoints = 100 }) == "test item", "Normal color requires actual item quality");
        Check(settings.TrySetColor((int)QualityCategory.Normal, "")
            && ColorLabel(thing) == "test item", "Clearing Normal restores vanilla text");
        qualityField.SetValue(thing.compQuality, (QualityCategory)255);
        Check(GenLabel.ThingLabel(thing, 1, false, false) == "test item", "Unsupported quality passes through");
        Check(ColorLabel(null) == "test item" && ColorLabel(new ThingWithComps()) == "test item"
            && ColorLabel(thing, null) == null && ColorLabel(thing, "") == "", "Missing inputs pass through the postfix");

        settings.ColorQuality = settings.ColorTainted = settings.ColorDamaged = false;
        settings.ResetDefaults();
        Check(settings.ColorQuality && settings.ColorTainted && settings.ColorDamaged && settings.DamageThreshold == 50,
            "Reset restores toggles and threshold");
        Check(settings.QualityTag(QualityCategory.Normal) == null
            && settings.QualityTag(QualityCategory.Legendary) == "<color=#33CCFF>"
            && settings.TaintedTag == "<color=#B24C4C>" && settings.DamagedTag == "<color=#D933BF>", "Reset restores original color tags");
    }

    private static string ColorLabel(Thing thing, string label = "test item")
    {
        Patch_GenLabel_ThingLabel.Postfix(thing, 1, ref label);
        return label;
    }

    private static void VerifySettingsXml()
    {
        string path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "<SettingsBlock><ModSettings><Version>0.3.0</Version></ModSettings></SettingsBlock>");
            var settings = ReadSettings(path);
            Check(settings.ColorQuality && settings.ColorTainted && settings.ColorDamaged
                && settings.DamageThreshold == 50 && settings.QualityTag(QualityCategory.Normal) == null
                && settings.QualityTag(QualityCategory.Awful) == "<color=#666666>" && !settings.RoomFormattingEnabled, "Legacy settings load with the previous default behavior");

            File.WriteAllText(path, """
                <SettingsBlock>
                  <ModSettings>
                    <ColorQuality>invalid</ColorQuality>
                    <ColorTainted>False</ColorTainted>
                    <DamageThreshold>not an integer</DamageThreshold>
                    <AwfulColor>#bad</AwfulColor>
                    <ColorRoomQuality>broken</ColorRoomQuality>
                    <ShowRoomStage>invalid</ShowRoomStage>
                    <RoomColor1>#bad</RoomColor1>
                    <RoomColor7>  a1b2c3  </RoomColor7>
                    <NormalColor>  11aa22  </NormalColor>
                  </ModSettings>
                </SettingsBlock>
                """);
            settings = ReadSettings(path);
            Check(settings.ColorQuality && !settings.ColorTainted && settings.ColorDamaged && settings.DamageThreshold == 50
                && settings.QualityTag(QualityCategory.Awful) == "<color=#666666>"
                && settings.QualityTag(QualityCategory.Normal) == "<color=#11AA22>" && !settings.RoomFormattingEnabled
                && settings.RoomQualityTag(0, 2) == "<color=#666666>" && settings.RoomQualityTag(1, 2) == "<color=#A1B2C3>",
                "Malformed saved values fall back and valid colors normalize");

            File.WriteAllText(path, "<SettingsBlock><ModSettings><DamageThreshold>120</DamageThreshold></ModSettings></SettingsBlock>");
            Check(ReadSettings(path).DamageThreshold == 100, "Saved out-of-range threshold is clamped");

            settings.ColorQuality = false;
            settings.ColorDamaged = false;
            settings.DamageThreshold = 64;
            settings.ColorRoomQuality = true;
            settings.ShowRoomStage = true;
            settings.TrySetRoomColor(0, "#123abc");
            settings.TrySetColor(BetterTradeColorsSettings.DamagedColor, "011223");
            Scribe.saver.InitSaving(path, "SettingsBlock");
            try
            {
                Scribe_Deep.Look(ref settings, "ModSettings");
            }
            finally
            {
                Scribe.saver.FinalizeSaving();
            }
            var reloaded = ReadSettings(path);
            Check(!reloaded.ColorQuality && !reloaded.ColorTainted && !reloaded.ColorDamaged && reloaded.DamageThreshold == 64
                && reloaded.DamagedTag == "<color=#011223>" && reloaded.QualityTag(QualityCategory.Normal) == "<color=#11AA22>",
                "Scribe round trip restores toggles, threshold, and cached colors");
            Check(reloaded.ColorRoomQuality && reloaded.ShowRoomStage && reloaded.RoomQualityTag(0, 2) == "<color=#123ABC>"
                && reloaded.RoomQualityTag(1, 2) == "<color=#A1B2C3>", "Scribe round trip restores both room toggles and room palette");
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static BetterTradeColorsSettings ReadSettings(string path)
    {
        BetterTradeColorsSettings settings = null;
        Scribe.loader.InitLoading(path);
        try
        {
            Scribe_Deep.Look(ref settings, "ModSettings");
        }
        finally
        {
            Scribe.loader.FinalizeLoading();
        }
        return settings;
    }

    private static bool MaxHitPoints(ref int __result)
    {
        __result = 100;
        return false;
    }

    private static void VerifyLoadFolders()
    {
        var document = new XmlDocument();
        document.Load("LoadFolders.xml");
        var folders = new ModLoadFolders();
        folders.LoadDataFromXmlCustom(document.DocumentElement);
        foreach (string version in new[] { "1.6", "1.7" })
        {
            var paths = folders.FoldersForVersion(version);
            string localizationPath = paths.Select(folder =>
                Path.Combine(folder.folderName, "Languages", "English", "Keyed", "BetterTradeColors.xml")).Single(File.Exists);
            var localization = new XmlDocument();
            localization.Load(localizationPath);
            Check(localization.DocumentElement.Name == "LanguageData" &&
                localization.DocumentElement["BTC_ResetDefaults"] != null
                && localization.DocumentElement["BTC_RoomStageIndicator"] != null, $"{version} load folders include shared localization");

            var assemblies = paths.Select(folder => Path.Combine(folder.folderName, "Assemblies"))
                .Where(Directory.Exists).SelectMany(folder => Directory.GetFiles(folder, "*.dll")).ToArray();
            Check(assemblies.Length == 1 && Path.GetFileName(assemblies[0]) == "BetterTradeColors.dll",
                $"{version} loads exactly one mod assembly");
            using var stream = File.OpenRead(assemblies[0]);
            using var pe = new PEReader(stream);
            var metadata = pe.GetMetadataReader();
            var references = metadata.AssemblyReferences.Select(handle => metadata.GetAssemblyReference(handle)).ToArray();
            bool is17 = version == "1.7";
            string engineAssembly = is17 ? "UnityStubs" : "UnityEngine.CoreModule";
            string runtimeAssembly = is17 ? "System.Runtime" : "mscorlib";
            Check(references.Any(reference => metadata.GetString(reference.Name) == engineAssembly)
                && references.Any(reference => metadata.GetString(reference.Name) == runtimeAssembly),
                $"{version} ships the correct engine and runtime references");
            Check(references.Single(reference => metadata.GetString(reference.Name) == "0Harmony").Version.Major == (is17 ? 3 : 2),
                $"{version} ships the correct Harmony reference");
        }
    }

    private static void Check(bool passed, string message)
    {
        if (!passed)
            throw new InvalidOperationException("FAIL: " + message);
        Console.WriteLine("PASS: " + message);
    }
}
