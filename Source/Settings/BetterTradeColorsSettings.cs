/**
BSD 2-Clause License

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

using UnityEngine;
using Verse;

namespace BetterTradeColors.Settings
{
    public class BetterTradeColorsSettings : ModSettings
    {
        private string _version = "0.0.5";
        private bool _canSaveSettings = false;
        private bool _colorTainted = true;

        public readonly string MessagePrefix = "[BetterTradeColors]";

        private static Vector2 _scrollPos = Vector2.zero;
        private const int _buttonCount = 5; // TODO: Just so the message is visible

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
            listing.Label($"If save file access is not allowed settings will not persist.", 24f);
            listing.Gap(12f);
            listing.CheckboxLabeled("Allow Access to save file", ref _canSaveSettings);
            listing.Gap(12f);

            listing.End();
            Widgets.EndScrollView();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            if (_canSaveSettings)
            {
                Scribe_Values.Look(ref _version, "Version", Version);
            }
        }
    }
}
