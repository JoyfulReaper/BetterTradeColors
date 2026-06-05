using System;
using UnityEngine;
using Verse;

namespace BetterTradeColors.Settings
{
    public class BetterTradeColorsSettings : ModSettings
    {
        private string _version = "0.0.1";
        private static Vector2 _scrollPos = Vector2.zero;
        private const int _buttonCount = 0;

        public string Version => _version;

        public void DoSettingsWindowContents(Rect inRect)
        {
            float calculatedHeight = (_buttonCount * 35f) + 50f;
            Rect viewRect = new Rect(0f, 0f, inRect.width - 20f, calculatedHeight);

            Widgets.BeginScrollView(inRect, ref _scrollPos, viewRect);

            Listing_Standard listing = new Listing_Standard();
            listing.Begin(viewRect);
            listing.Label($"Version {Version}", 24f);
            listing.Gap(12f);

            listing.Label("General Settings", 24f);
            listing.Gap(12f);
            listing.Label("No Settings YET....", 24f);
            listing.Gap(12f);

            listing.End();
            Widgets.EndScrollView();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref _version, "Version", Version);
        }
    }

}
