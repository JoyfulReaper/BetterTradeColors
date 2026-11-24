using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace BetterTradeColors.Patches;

internal enum RecordStanding { Unachieved, Holder, BelowRecord }

internal sealed class ColonistRecordComparison
{
    private static readonly AccessTools.FieldRef<Pawn_RecordsTracker, DefMap<RecordDef, float>> StoredValues =
        AccessTools.DeclaredField(typeof(Pawn_RecordsTracker), "records")?.FieldType == typeof(DefMap<RecordDef, float>)
            ? AccessTools.FieldRefAccess<Pawn_RecordsTracker, DefMap<RecordDef, float>>("records") : null;

    private readonly System.WeakReference<World> _world = new System.WeakReference<World>(null);
    private readonly HashSet<int> _members = new HashSet<int>();
    private float[] _maxima = Array.Empty<float>();
    private int _frame;

    // The native finder walks active maps, caravans, and transport holders.
    // Its returned list is scratch storage owned by RimWorld.
    internal static List<Pawn> FindCandidates() =>
        PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists_NoLodgers;

    internal static bool Eligible(Pawn pawn) =>
        pawn != null && pawn.thingIDNumber >= 0 && pawn.records != null && pawn.health != null &&
        pawn.def?.race != null && !pawn.Dead && !pawn.IsSlave && !pawn.IsPrisoner && pawn.IsFreeColonist;

    internal void Refresh(World world, int frame, int recordCount, Func<List<Pawn>> findCandidates)
    {
        if (StoredValues == null || world == null)
            return;

        if (_frame == frame && _maxima.Length == recordCount && _world.TryGetTarget(out World previous) && ReferenceEquals(previous, world))
            return;

        _members.Clear();
        if (_maxima.Length != recordCount)
            _maxima = new float[recordCount];
        else
            Array.Clear(_maxima, 0, _maxima.Length);

        foreach (Pawn pawn in findCandidates())
        {
            if (!Eligible(pawn))
                continue;

            DefMap<RecordDef, float> values = StoredValues(pawn.records);
            if (values == null || !_members.Add(pawn.thingIDNumber))
                continue;

            int count = Math.Min(recordCount, values.Count);
            for (int i = 0; i < count; i++)
            {
                float value = values[i];
                if (value > _maxima[i] && !float.IsInfinity(value))
                    _maxima[i] = value;
            }
        }

        _frame = frame;
        _world.SetTarget(world);
    }

    internal bool Contains(Pawn pawn) => pawn != null && _members.Contains(pawn.thingIDNumber);

    internal RecordStanding? Standing(Pawn pawn, RecordDef record)
    {
        if (record == null || record.index >= _maxima.Length || !Contains(pawn) || pawn.records == null)
            return null;

        DefMap<RecordDef, float> values = StoredValues(pawn.records);
        if (values == null || record.index >= values.Count)
            return null;

        // GetValue rounds integer and time records. Compare the stored floats
        // and leave vanilla's rounded numbers or formatted durations on screen.
        float value = values[record.index];
        if (float.IsNaN(value) || float.IsInfinity(value))
            return null;

        float maximum = _maxima[record.index];
        if (maximum <= 0f && value <= 0f)
            return RecordStanding.Unachieved;
        return value >= maximum ? RecordStanding.Holder : RecordStanding.BelowRecord;
    }
}
