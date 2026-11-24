using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BetterTradeColors.Settings;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace BetterTradeColors.Patches;

[HarmonyPatch(typeof(RecordsCardUtility), nameof(RecordsCardUtility.DrawRecordsCard), new[] { typeof(Rect), typeof(Pawn) })]
internal static class ColonistRecordsCardPatch
{
    private static readonly ColonistRecordComparison Comparison = new ColonistRecordComparison();
    internal static ColonistRecordComparison Active { get; private set; }

    private static bool Prefix(Pawn pawn, out ColonistRecordComparison __state)
    {
        __state = Active;
        Active = null;
        if (BetterTradeColorsMod.Settings?.ColorColonistRecords != true)
            return true;

        // An incomplete pawn has no values the original renderer can read.
        if (pawn?.records == null)
            return false;

        if (Current.ProgramState == ProgramState.Playing && Current.Game?.World is World world &&
            ColonistRecordComparison.Eligible(pawn))
        {
            Comparison.Refresh(world, Time.frameCount, DefDatabase<RecordDef>.DefCount, ColonistRecordComparison.FindCandidates);
            if (Comparison.Contains(pawn))
                Active = Comparison;
        }
        return true;
    }

    private static void Finalizer(ColonistRecordComparison __state) => Active = __state;
}

[HarmonyPatch]
internal static class ColonistRecordValuePatch
{
    private static MethodInfo ValueRow => AccessTools.DeclaredMethod(typeof(RecordsCardUtility), "DrawRecord",
        new[] { typeof(float), typeof(float), typeof(float), typeof(RecordDef), typeof(Pawn) });

    private static bool Prepare()
    {
        if (ValueRow != null)
            return true;
        Log.Warning("[BetterTradeColors] The colonist record row method could not be found.");
        return false;
    }

    private static MethodBase TargetMethod() => ValueRow;

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var code = instructions.ToList();
        MethodInfo label = AccessTools.Method(typeof(Widgets), nameof(Widgets.Label), new[] { typeof(Rect), typeof(string) });
        MethodInfo taggedLabel = AccessTools.Method(typeof(Widgets), nameof(Widgets.Label), new[] { typeof(Rect), typeof(TaggedString) });
        int name = code.FindIndex(instruction => instruction.Calls(taggedLabel) || instruction.Calls(label));
        int value = name < 0 ? -1 : code.FindIndex(name + 1, instruction => instruction.Calls(label));
        if (value < 0 || code.FindIndex(value + 1, instruction => instruction.Calls(label)) >= 0 || code[value].blocks.Count != 0)
        {
            Log.Warning("[BetterTradeColors] Skipped colonist record coloring because the row layout changed.");
            return code;
        }

        var record = new CodeInstruction(OpCodes.Ldarg_3).MoveLabelsFrom(code[value]);
        code[value].operand = AccessTools.Method(typeof(ColonistRecordValuePatch), nameof(DrawValue));
        code.InsertRange(value, new[] { record, new CodeInstruction(OpCodes.Ldarg_S, (byte)4) });
        return code;
    }

    private static void DrawValue(Rect rect, string value, RecordDef record, Pawn pawn)
    {
        if (ColonistRecordsCardPatch.Active is ColonistRecordComparison comparison)
            value = ColorValue(value, comparison.Standing(pawn, record));
        Widgets.Label(rect, value);
    }

    internal static string ColorValue(string value, RecordStanding? standing)
    {
        var settings = BetterTradeColorsMod.Settings;
        if (settings?.ColorColonistRecords != true || string.IsNullOrEmpty(value))
            return value;

        string tag = standing switch
        {
            RecordStanding.Holder => settings.RecordTag(BetterTradeColorsSettings.RecordHolderColor),
            RecordStanding.BelowRecord => settings.RecordTag(BetterTradeColorsSettings.NonRecordHolderColor),
            RecordStanding.Unachieved => settings.RecordTag(BetterTradeColorsSettings.UnachievedRecordColor),
            _ => null
        };
        return tag == null ? value : LabelRichText.ApplyColor(value, tag, stackCount: 1);
    }
}
