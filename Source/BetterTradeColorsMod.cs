using BetterTradeColors.Settings;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace BetterTradeColors
{
    public class BetterTradeColorsMod : Mod
    {
        public static BetterTradeColorsSettings Settings;

        public BetterTradeColorsMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<BetterTradeColorsSettings>();
            var harmony = new Harmony("com.kylegivler.bettertradecolors");
            harmony.PatchAll();

            Log.Message($"[BetterTradeColors {Settings.Version}]: Harmony patches applied successfully.");
        }

        public override string SettingsCategory()
        {
            return "Better Trade Colors";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            //Settings.DoSettingsWindowContents(inRect);
            base.DoSettingsWindowContents(inRect);
        }
    }
}
