using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace BetterTradeColors.Patches;

[HarmonyPatch]
internal static class RoomQualityPatches
{
    private static readonly MethodInfo GetStage = AccessTools.Method(typeof(RoomStatDef), nameof(RoomStatDef.GetScoreStage));
    private static readonly FieldInfo StageLabel = AccessTools.Field(typeof(RoomStatScoreStage), nameof(RoomStatScoreStage.label));
    private static readonly MethodInfo Capitalize = AccessTools.Method(typeof(GenText), nameof(GenText.CapitalizeFirst), new[] { typeof(string) });
    private static readonly MethodInfo Label = AccessTools.Method(typeof(Widgets), nameof(Widgets.Label), new[] { typeof(Rect), typeof(string) });
    private static readonly MethodInfo Truncate = AccessTools.Method(typeof(GenText), nameof(GenText.Truncate),
        new[] { typeof(string), typeof(float), typeof(Dictionary<string, string>) });
    private static readonly MethodInfo DrawRow = AccessTools.DeclaredMethod(typeof(CellInspectorDrawer), "DrawRow",
        new[] { typeof(string), typeof(string) });

    private static IEnumerable<MethodBase> TargetMethods()
    {
        foreach (MethodBase method in new[]
        {
            AccessTools.DeclaredMethod(typeof(EnvironmentStatsDrawer), nameof(EnvironmentStatsDrawer.DoRoomInfo),
                new[] { typeof(Room), typeof(float).MakeByRefType(), typeof(Rect) }),
            AccessTools.DeclaredMethod(typeof(CellInspectorDrawer), "DrawMapInspector", Type.EmptyTypes),
            AccessTools.DeclaredMethod(typeof(Gizmo_RoomStats), nameof(Gizmo_RoomStats.GizmoOnGUI),
                new[] { typeof(Vector2), typeof(float), typeof(GizmoRenderParms) })
        })
        {
            if (method != null)
                yield return method;
            else
                Log.Warning("[BetterTradeColors] A room quality display method could not be found.");
        }
    }

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase original)
    {
        var code = instructions.ToList();
        bool overlay = original.DeclaringType == typeof(EnvironmentStatsDrawer);
        bool gizmo = original.DeclaringType == typeof(Gizmo_RoomStats);
        MethodInfo draw = overlay ? Label : gizmo ? Truncate : DrawRow;
        var sites = new List<(int Lookup, int Field, int Capitalize, int Draw)>();
        for (int lookup = 0; lookup < code.Count; lookup++)
        {
            if (!code[lookup].Calls(GetStage))
                continue;

            int field = code.FindIndex(lookup + 1, instruction => instruction.LoadsField(StageLabel) || instruction.Calls(GetStage));
            if (field < 0 || !code[field].LoadsField(StageLabel))
                return Unrecognized(code, original);

            int next = field + 1;
            int capitalize = next < code.Count && code[next].Calls(Capitalize) ? next : -1;
            int row = code.FindIndex(next, instruction => instruction.Calls(draw) || instruction.Calls(GetStage));
            if (row < 0 || !code[row].Calls(draw) || code[lookup].blocks.Count != 0 ||
                code[field].blocks.Count != 0 || code[row].blocks.Count != 0)
                return Unrecognized(code, original);

            sites.Add((lookup, field, capitalize, row));
        }

        if (sites.Count != (gizmo ? 2 : 1))
            return Unrecognized(code, original);

        // Capture the definition already on the stack. Leave the game's lookup and score intact.
        LocalBuilder score = generator.DeclareLocal(typeof(float));
        LocalBuilder statDef = generator.DeclareLocal(typeof(RoomStatDef));
        LocalBuilder indicator = generator.DeclareLocal(typeof(string));
        MethodInfo format = AccessTools.Method(typeof(RoomQualityPatches), nameof(FormatStage));
        MethodInfo render = AccessTools.Method(typeof(RoomQualityPatches), overlay ? nameof(DrawOverlayStage)
            : gizmo ? nameof(TruncateGizmoStage) : nameof(FitCellStage));
        foreach (var site in sites.AsEnumerable().Reverse())
        {
            if (overlay || gizmo)
            {
                code[site.Draw].operand = render;
                InsertBefore(code, site.Draw, new CodeInstruction(OpCodes.Ldloc, indicator));
            }
            else
            {
                InsertBefore(code, site.Draw, new CodeInstruction(OpCodes.Ldloc, indicator),
                    new CodeInstruction(OpCodes.Call, render));
            }

            if (site.Capitalize >= 0)
            {
                code[site.Capitalize].opcode = OpCodes.Nop;
                code[site.Capitalize].operand = null;
            }

            code[site.Field].opcode = OpCodes.Call;
            code[site.Field].operand = format;
            InsertBefore(code, site.Field, new CodeInstruction(OpCodes.Ldloc, statDef),
                new CodeInstruction(site.Capitalize >= 0 ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0), new CodeInstruction(OpCodes.Ldloca, indicator));
            InsertBefore(code, site.Lookup, new CodeInstruction(OpCodes.Stloc, score),
                new CodeInstruction(OpCodes.Dup), new CodeInstruction(OpCodes.Stloc, statDef),
                new CodeInstruction(OpCodes.Ldloc, score));
        }
        return code;
    }

    private static List<CodeInstruction> Unrecognized(List<CodeInstruction> code, MethodBase original)
    {
        Log.Warning($"[BetterTradeColors] Skipped room quality formatting for {original.DeclaringType.Name}.{original.Name}: unknown IL layout.");
        return code;
    }

    private static void InsertBefore(List<CodeInstruction> code, int index, params CodeInstruction[] inserted)
    {
        inserted[0].MoveLabelsFrom(code[index]);
        code.InsertRange(index, inserted);
    }

    internal static string FormatStage(RoomStatScoreStage stage, RoomStatDef stat, bool capitalize, out string indicator)
    {
        indicator = null;
        string label = stage?.label ?? "";
        if (capitalize)
            label = label.CapitalizeFirst();

        var settings = BetterTradeColorsMod.Settings;
        if (settings?.RoomFormattingEnabled != true || label.Length == 0 || stat?.scoreStages == null)
            return label;

        int index = stat.scoreStages.IndexOf(stage);
        return FormatDescription(label, index, stat.scoreStages.Count, out indicator);
    }

    internal static string FormatDescription(string label, int index, int count, out string indicator)
    {
        var settings = BetterTradeColorsMod.Settings;
        indicator = null;
        if (settings?.RoomFormattingEnabled != true || string.IsNullOrEmpty(label) || index < 0 || index >= count)
            return label;

        indicator = settings.ShowRoomStage ? " " + "BTC_RoomStageIndicator".Translate(index + 1, count).RawText : null;
        if (indicator != null && label.EndsWith(indicator, StringComparison.Ordinal))
        {
            if (!settings.ColorRoomQuality)
                return label;
            label = label.Substring(0, label.Length - indicator.Length);
        }

        if (settings.ColorRoomQuality)
            label = LabelRichText.ApplyColor(label, settings.RoomQualityTag(index, count), stackCount: 1);
        return string.Concat(label, indicator);
    }

    private static bool Enabled => BetterTradeColorsMod.Settings?.RoomFormattingEnabled == true;

    private static void DrawOverlayStage(Rect rect, string text, string indicator)
    {
        if (Enabled && !string.IsNullOrEmpty(text))
            text = FitStage(text, rect.width, indicator);
        Widgets.Label(rect, text);
    }

    private static string TruncateGizmoStage(string text, float width, Dictionary<string, string> cache, string indicator) =>
        Enabled && !string.IsNullOrEmpty(text) ? FitStage(text, width, indicator) : text.Truncate(width, cache);

    private static string FitCellStage(string text, string indicator)
    {
        // Match DrawRow's info column and the vanilla LabelEllipses inset.
        const float widthAfterVanillaInset = 170f - 13f;
        return Enabled && !string.IsNullOrEmpty(text) && (indicator != null || text.IndexOf('<') >= 0)
            ? FitStage(text, widthAfterVanillaInset, indicator) : text;
    }

    internal static string FitStage(string text, float width, string indicator)
    {
        string shortened = LabelRichText.Truncate(text, width);
        if (indicator == null || shortened == text || shortened.IndexOf(indicator, StringComparison.Ordinal) >= 0)
            return shortened;

        int suffix = text.LastIndexOf(indicator, StringComparison.Ordinal);
        if (suffix < 0)
            return shortened;

        // When vanilla's tail would hide the stage number, shorten the description
        // first and keep the complete indicator if it fits the column by itself.
        string description = text.Substring(0, suffix);
        float available = width - Text.CalcSize(indicator).x;
        if (available < 0f)
        {
            string number = indicator.TrimStart();
            return Text.CalcSize(number).x <= width ? number : LabelRichText.Truncate(description, width);
        }
        string prefix = LabelRichText.Truncate(description, available);
        return prefix.Length == 0 ? indicator.TrimStart() : string.Concat(prefix, indicator);
    }
}
