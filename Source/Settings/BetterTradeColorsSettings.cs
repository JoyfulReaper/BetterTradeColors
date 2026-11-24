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

using System;
using System.Globalization;
using RimWorld;
using UnityEngine;
using Verse;

namespace BetterTradeColors.Settings
{
    public class BetterTradeColorsSettings : ModSettings
    {
        private const int DefaultDamageThreshold = 50;
        private const int QualityCount = 7;
        internal const int TaintedColor = QualityCount;
        internal const int DamagedColor = QualityCount + 1;
        private const int RoomPaletteStart = DamagedColor + 1;
        internal const int RoomPaletteSize = 7;

        private static readonly string[] ColorKeys =
        {
            "AwfulColor", "PoorColor", "NormalColor", "GoodColor", "ExcellentColor",
            "MasterworkColor", "LegendaryColor", "TaintedColor", "DamagedColor",
            "RoomColor1", "RoomColor2", "RoomColor3", "RoomColor4", "RoomColor5", "RoomColor6", "RoomColor7"
        };

        private static readonly string[] DefaultColors =
        {
            "#666666", "#999999", "", "#66CC66", "#CCCC33", "#FF9933", "#33CCFF", "#B24C4C", "#D933BF",
            "#666666", "#999999", "#BFBFBF", "#66CC66", "#CCCC33", "#FF9933", "#33CCFF"
        };

        private readonly string[] _colors = new string[DefaultColors.Length];
        private readonly string[] _colorTags = new string[DefaultColors.Length];
        private readonly Color[] _colorPreviews = new Color[DefaultColors.Length];
        private readonly string[] _colorInputs = new string[DefaultColors.Length];
        private readonly bool[] _invalidColorInputs = new bool[DefaultColors.Length];
        private Listing_Standard _listing;
        private Vector2 _scrollPosition;
        private float _contentHeight;
        private int _damageThreshold;

        internal bool ColorQuality;
        internal bool ColorTainted;
        internal bool ColorDamaged;
        internal bool ColorRoomQuality;
        internal bool ShowRoomStage;
        internal bool RoomFormattingEnabled => ColorRoomQuality || ShowRoomStage;

        internal int DamageThreshold
        {
            get => _damageThreshold;
            set => _damageThreshold = Math.Min(100, Math.Max(0, value));
        }

        internal string TaintedTag => _colorTags[TaintedColor];
        internal string DamagedTag => _colorTags[DamagedColor];

        public string Version => "0.4.0";
        public readonly string MessagePrefix = "[BetterTradeColors]";

        public BetterTradeColorsSettings()
        {
            ResetDefaults();
            ResetRoomDefaults();
        }

        internal string QualityTag(QualityCategory quality)
        {
            int index = (int)quality;
            return (uint)index < QualityCount ? _colorTags[index] : null;
        }

        internal void ResetDefaults()
        {
            ColorQuality = ColorTainted = ColorDamaged = true;
            DamageThreshold = DefaultDamageThreshold;
            ResetPalette(0, RoomPaletteStart);
        }

        internal void ResetRoomDefaults()
        {
            ColorRoomQuality = ShowRoomStage = false;
            ResetPalette(RoomPaletteStart, RoomPaletteSize);
        }

        private void ResetPalette(int start, int count)
        {
            for (int i = start; i < start + count; i++)
                TrySetColor(i, DefaultColors[i]);
            ClearColorInputs(start, count);
        }

        internal void ClearColorInputs() => ClearColorInputs(0, _colors.Length);

        private void ClearColorInputs(int start, int count)
        {
            Array.Clear(_colorInputs, start, count);
            Array.Clear(_invalidColorInputs, start, count);
        }

        internal string RoomQualityTag(int stage, int stageCount)
        {
            if (stage < 0 || stage >= stageCount)
                return null;

            int last = stageCount - 1;
            int slot = last == 0 ? RoomPaletteSize / 2 : (int)(((long)stage * (RoomPaletteSize - 1) + last / 2) / last);
            return _colorTags[RoomPaletteStart + slot];
        }

        internal bool TrySetRoomColor(int slot, string input) =>
            (uint)slot < RoomPaletteSize && TrySetColor(RoomPaletteStart + slot, input);

        internal bool TrySetColor(int index, string input)
        {
            string hex = input?.Trim() ?? "";
            if (index == (int)QualityCategory.Normal && hex.Length == 0)
            {
                _colors[index] = "";
                _colorTags[index] = null;
                return true;
            }

            if (hex.Length > 0 && hex[0] == '#')
                hex = hex.Substring(1);

            if (hex.Length != 6 || hex.IndexOf('\0') >= 0 ||
                !uint.TryParse(hex, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out uint rgb))
                return false;

            string canonical = "#" + hex.ToUpperInvariant();
            if (canonical != _colors[index])
            {
                _colors[index] = canonical;
                _colorTags[index] = "<color=" + canonical + ">";
                _colorPreviews[index] = new Color((rgb >> 16) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f);
            }
            return true;
        }

        public void DoSettingsWindowContents(Rect inRect)
        {
            float width = inRect.width - 20f;
            if (width <= 40f || inRect.height <= 0f)
                return;

            var viewRect = new Rect(0f, 0f, width, Math.Max(inRect.height, _contentHeight));
            _listing ??= new Listing_Standard { maxOneColumn = true };
            Widgets.BeginScrollView(inRect, ref _scrollPosition, viewRect);
            _listing.Begin(viewRect);
            _listing.Label("BTC_SettingsVersion".Translate(Version));
            _listing.Gap();

            _listing.Label("BTC_ItemSettings".Translate());
            float labelWidth = (width - 28f) / width;
            _listing.CheckboxLabeled("BTC_ColorQuality".Translate(), ref ColorQuality, labelPct: labelWidth);
            _listing.CheckboxLabeled("BTC_ColorTainted".Translate(), ref ColorTainted, labelPct: labelWidth);
            _listing.CheckboxLabeled("BTC_ColorDamaged".Translate(), ref ColorDamaged, labelPct: labelWidth);
            _listing.Gap();
            _listing.Label("BTC_DamageThreshold".Translate(DamageThreshold));
            DamageThreshold = (int)Widgets.HorizontalSlider(_listing.GetRect(22f), DamageThreshold, 0f, 100f, roundTo: 1f);

            _listing.Gap();
            _listing.Label("BTC_ColorHelp".Translate());
            _listing.Gap(6f);
            for (int i = 0; i < QualityCount; i++)
                DrawColorRow(i, ((QualityCategory)i).GetLabel().CapitalizeFirst());
            DrawColorRow(TaintedColor, "BTC_TaintedColor".Translate());
            DrawColorRow(DamagedColor, "BTC_DamagedColor".Translate());

            _listing.Gap();
            string resetLabel = "BTC_ResetDefaults".Translate();
            if (Widgets.ButtonText(_listing.GetRect(Math.Max(30f, Text.CalcHeight(resetLabel, width - 12f) + 8f)), resetLabel))
                ResetDefaults();
            _listing.GapLine();
            _listing.Label("BTC_RoomQualitySettings".Translate());
            _listing.CheckboxLabeled("BTC_ColorRoomQuality".Translate(), ref ColorRoomQuality, labelPct: labelWidth);
            _listing.CheckboxLabeled("BTC_ShowRoomStage".Translate(), ref ShowRoomStage, labelPct: labelWidth);
            _listing.Gap();
            _listing.Label("BTC_RoomColorHelp".Translate());
            _listing.Gap(6f);
            for (int i = 0; i < RoomPaletteSize; i++)
                DrawColorRow(RoomPaletteStart + i, "BTC_RoomPaletteLevel".Translate(i + 1, RoomPaletteSize));

            _listing.Gap();
            string resetRoomLabel = "BTC_ResetRoomDefaults".Translate();
            if (Widgets.ButtonText(_listing.GetRect(Math.Max(30f, Text.CalcHeight(resetRoomLabel, width - 12f) + 8f)), resetRoomLabel))
                ResetRoomDefaults();
            _contentHeight = _listing.CurHeight;
            _listing.End();
            Widgets.EndScrollView();
        }

        private void DrawColorRow(int index, string label)
        {
            const float fieldWidth = 100f;
            const float controlHeight = 26f;
            const float swatchSize = 20f;
            const float gap = 8f;
            float width = _listing.ColumnWidth;
            bool stacked = width < 260f;
            float labelWidth = stacked ? width : width - fieldWidth - swatchSize - 2f * gap;
            float labelHeight = Text.CalcHeight(label, labelWidth);
            Rect row = _listing.GetRect(stacked ? labelHeight + controlHeight : Math.Max(controlHeight, labelHeight));
            Widgets.Label(new Rect(row.x, row.y, labelWidth, labelHeight), label);

            float fieldX = stacked ? row.x : row.xMax - fieldWidth - swatchSize - gap;
            float fieldY = stacked ? row.y + labelHeight : row.y;
            float actualFieldWidth = Math.Min(fieldWidth, width - swatchSize - gap);
            var field = new Rect(fieldX, fieldY, actualFieldWidth, controlHeight);
            string previousInput = _colorInputs[index] ?? _colors[index];
            string input = Widgets.TextField(field, previousInput, 7);
            if (input != previousInput)
            {
                _colorInputs[index] = input;
                _invalidColorInputs[index] = !TrySetColor(index, input);
            }

            if (_invalidColorInputs[index])
                TooltipHandler.TipRegion(field, "BTC_InvalidColor".Translate());
            if (_colorTags[index] != null)
                Widgets.DrawBoxSolidWithOutline(new Rect(field.xMax + gap, fieldY + 3f, swatchSize, swatchSize),
                    _colorPreviews[index], Color.gray);
            _listing.Gap(4f);
        }

        public override void ExposeData()
        {
            LookToggle(ref ColorQuality, "ColorQuality");
            LookToggle(ref ColorTainted, "ColorTainted");
            LookToggle(ref ColorDamaged, "ColorDamaged");
            LookToggle(ref ColorRoomQuality, "ColorRoomQuality", defaultValue: false);
            LookToggle(ref ShowRoomStage, "ShowRoomStage", defaultValue: false);
            string threshold = _damageThreshold.ToString(CultureInfo.InvariantCulture);
            Scribe_Values.Look(ref threshold, "DamageThreshold", DefaultDamageThreshold.ToString(CultureInfo.InvariantCulture));

            bool loading = Scribe.mode == LoadSaveMode.LoadingVars;
            for (int i = 0; i < _colors.Length; i++)
            {
                string color = _colors[i];
                Scribe_Values.Look(ref color, ColorKeys[i], DefaultColors[i]);
                if (loading && !TrySetColor(i, color))
                    TrySetColor(i, DefaultColors[i]);
            }

            if (loading)
            {
                DamageThreshold = int.TryParse(threshold, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
                    ? value : DefaultDamageThreshold;
                ClearColorInputs();
            }
        }

        private static void LookToggle(ref bool enabled, string key, bool defaultValue = true)
        {
            string value = enabled.ToString();
            Scribe_Values.Look(ref value, key, defaultValue.ToString());
            if (Scribe.mode == LoadSaveMode.LoadingVars)
                enabled = bool.TryParse(value, out bool parsed) ? parsed : defaultValue;
        }
    }
}
