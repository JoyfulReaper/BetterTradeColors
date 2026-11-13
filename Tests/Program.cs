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
using HarmonyLib;
using RimWorld;
using Verse;

internal static class Program
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

        return RunChecks();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int RunChecks()
    {
        const string owner = "BetterTradeColors.CompatibilityChecks";
        var harmony = new Harmony(owner);
        try
        {
            // Supply HP before patching labels so the standalone CLR cannot inline
            // the game's stat lookup, which requires a running game's databases.
            harmony.Patch(AccessTools.PropertyGetter(typeof(Thing), nameof(Thing.MaxHitPoints)),
                prefix: new HarmonyMethod(typeof(Program), nameof(MaxHitPoints)));
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

            string input = "<color=#33CCFF>test item</color>";
            // Disable tiny fonts in the fixture so Text does not need player preferences.
            LanguageDatabase.activeLanguage = new LoadedLanguage("English")
            {
                info = new LanguageInfo { canBeTiny = false }
            };
            Check(GenText.Truncate(input, float.MaxValue, null) == input, "Uncached text that fits keeps its color tags");
            var cache = new Dictionary<string, string> { [input] = "cached result" };
            Check(GenText.Truncate(input, 10f, cache) == "cached result", "Truncation cache is honored");
            string result = null;
            Check(Patch_GenText_Truncate.Prefix("plain label", 100f, null, ref result), "Plain text uses vanilla truncation");
            Check(Patch_GenText_Truncate.Prefix(null, 100f, null, ref result), "Null text uses vanilla truncation");

            VerifyLoadFolders();
            Console.WriteLine("Compatibility checks passed. In-game rendering still requires verification.");
            return 0;
        }
        finally
        {
            harmony.UnpatchAll(owner);
        }
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
            var assemblies = paths.SelectMany(folder => Directory.GetFiles(
                Path.Combine(folder.folderName, "Assemblies"), "*.dll")).ToArray();
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
