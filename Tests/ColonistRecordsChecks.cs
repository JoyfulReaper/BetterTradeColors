using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using BetterTradeColors;
using BetterTradeColors.Patches;
using BetterTradeColors.Settings;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

internal static partial class Program
{
    private static readonly Faction PlayerRecordFaction = new Faction { def = new FactionDef { isPlayer = true } };
    private static readonly Faction OtherRecordFaction = new Faction { def = new FactionDef() };
    private static readonly FieldInfo RecordValues = AccessTools.Field(typeof(Pawn_RecordsTracker), "records");

    private static void VerifyColonistRecords(Harmony harmony)
    {
        var card = AccessTools.DeclaredMethod(typeof(RecordsCardUtility), nameof(RecordsCardUtility.DrawRecordsCard));
        var row = AccessTools.DeclaredMethod(typeof(RecordsCardUtility), "DrawRecord");
        var drawValue = AccessTools.DeclaredMethod(typeof(ColonistRecordValuePatch), "DrawValue");
        Check(Harmony.GetPatchInfo(card).Prefixes.Any(p => p.owner == harmony.Id)
            && Harmony.GetPatchInfo(card).Finalizers.Any(p => p.owner == harmony.Id)
            && PatchProcessor.GetCurrentInstructions(row).Count(i => i.Calls(drawValue)) == 1,
            "Records card scopes comparisons and the private row patches only its value label");

        var settings = BetterTradeColorsMod.Settings;
        Check("BTC_ColonistRecordsSettings".Translate().RawText == "Colonist Records"
            && !settings.ColorColonistRecords && ColonistRecordValuePatch.ColorValue("23", RecordStanding.Holder) == "23",
            "Colonist Records settings are localized and disabled by default");
        settings.ColorColonistRecords = true;
        Check(ColonistRecordValuePatch.ColorValue("23", RecordStanding.Holder) == "<color=#66CC66>23</color>"
            && ColonistRecordValuePatch.ColorValue("23", RecordStanding.BelowRecord) == "<color=#CCCC33>23</color>"
            && ColonistRecordValuePatch.ColorValue("0", RecordStanding.Unachieved) == "0",
            "Default record palette colors values and inherits the unachieved color");
        Check(settings.TrySetColor(BetterTradeColorsSettings.UnachievedRecordColor, "123abc")
            && !settings.TrySetColor(BetterTradeColorsSettings.UnachievedRecordColor, "#oops")
            && ColonistRecordValuePatch.ColorValue("0", RecordStanding.Unachieved) == "<color=#123ABC>0</color>"
            && settings.TrySetColor(BetterTradeColorsSettings.UnachievedRecordColor, "")
            && ColonistRecordValuePatch.ColorValue("0", RecordStanding.Unachieved) == "0"
            && settings.TrySetColor(BetterTradeColorsSettings.RecordHolderColor, "#010203"),
            "Record colors validate, refresh immediately, and can inherit vanilla");
        string styled = ColonistRecordValuePatch.ColorValue("<b>23</b>", RecordStanding.Holder);
        Check(styled == "<color=#010203><b>23</b></color>"
            && ColonistRecordValuePatch.ColorValue(styled, RecordStanding.Holder) == styled
            && ColonistRecordValuePatch.ColorValue("<color=red>23</color>", RecordStanding.Holder) == "<color=red>23</color>",
            "Record value formatting preserves rich text without duplicate color wrappers");
        // Exercises the enabled card guard without initializing the GUI.
        RecordsCardUtility.DrawRecordsCard(default, null);
        RecordsCardUtility.DrawRecordsCard(default, new Pawn());
        Check(ColonistRecordsCardPatch.Active == null, "Missing pawns and trackers leave no active comparison");

        try
        {
            VerifyRecordComparisons(harmony);
            BenchmarkRecords(15, 40);
            BenchmarkRecords(100, 80);
            BenchmarkRecords(400, 160);
        }
        finally
        {
            DefDatabase<RecordDef>.Clear();
        }

        settings.ColorRoomQuality = true;
        settings.TrySetColor((int)QualityCategory.Legendary, "#123456");
        settings.ResetRecordsDefaults();
        Check(!settings.ColorColonistRecords && settings.ColorRoomQuality
            && settings.QualityTag(QualityCategory.Legendary) == "<color=#123456>"
            && settings.RecordTag(BetterTradeColorsSettings.RecordHolderColor) == "<color=#66CC66>"
            && settings.RecordTag(BetterTradeColorsSettings.UnachievedRecordColor) == null,
            "Records reset restores only records defaults");
        settings.ResetRoomDefaults();
        settings.ResetDefaults();
    }

    private static void VerifyRecordComparisons(Harmony harmony)
    {
        RecordDef[] records = CreateRecordDefs(4);
        Pawn first = CreateRecordPawn(1);
        Pawn second = CreateRecordPawn(2);
        Pawn third = CreateRecordPawn(3);
        Pawn dead = CreateRecordPawn(4);
        Pawn outsider = CreateRecordPawn(5, OtherRecordFaction);
        Pawn noTracker = CreateRecordPawn(6);
        Pawn prisoner = CreateRecordPawn(7);
        Pawn slave = CreateRecordPawn(8);
        Pawn animal = CreateRecordPawn(9);
        Pawn visitor = CreateRecordPawn(10, OtherRecordFaction);

        AccessTools.Field(typeof(Pawn_HealthTracker), "healthState").SetValue(dead.health, PawnHealthState.Dead);
        noTracker.records = null;
        prisoner.guest = new Pawn_GuestTracker(prisoner) { guestStatusInt = GuestStatus.Prisoner };
        AccessTools.Field(typeof(Pawn_GuestTracker), "hostFactionInt").SetValue(prisoner.guest, PlayerRecordFaction);
        slave.guest = new Pawn_GuestTracker(slave) { guestStatusInt = GuestStatus.Slave };
        animal.def.race.intelligence = Intelligence.Animal;
        visitor.guest = new Pawn_GuestTracker(visitor);
        AccessTools.Field(typeof(Pawn_GuestTracker), "hostFactionInt").SetValue(visitor.guest, PlayerRecordFaction);

        SetRecord(first, records[0], 801.2f);
        SetRecord(second, records[0], 801.4f);
        SetRecord(first, records[1], 12f);
        SetRecord(second, records[1], 12f);
        first.records.AddTo(records[2], 15.384f);
        second.records.AddTo(records[2], 15.381f);
        SetRecord(third, records[2], float.PositiveInfinity);
        SetRecord(third, records[1], float.NaN);
        SetRecord(third, records[3], float.NegativeInfinity);
        SetRecord(second, records[3], -4f);
        foreach (Pawn ineligible in new[] { dead, outsider, prisoner, slave, animal, visitor })
            SetRecord(ineligible, records[2], 99999f);

        var world = NewRecordWorld();
        var snapshot = new ColonistRecordComparison();
        var candidates = new List<Pawn> { first, second, third, dead, outsider, noTracker, prisoner, slave, animal, visitor, null, first };
        int enumerations = 0;
        List<Pawn> GetCandidates()
        {
            enumerations++;
            return candidates;
        }
        snapshot.Refresh(world, 1, records.Length, GetCandidates);

        Check(first.records.GetAsInt(records[0]) == second.records.GetAsInt(records[0])
            && snapshot.Standing(first, records[0]) == RecordStanding.BelowRecord
            && snapshot.Standing(second, records[0]) == RecordStanding.Holder, "Time rankings use stored ticks before vanilla rounding and formatting");
        Check(snapshot.Standing(first, records[1]) == RecordStanding.Holder
            && snapshot.Standing(second, records[1]) == RecordStanding.Holder, "Equal positive integer records share the record");
        Check(first.records.GetValue(records[2]).ToString("0.##") == second.records.GetValue(records[2]).ToString("0.##")
            && snapshot.Standing(first, records[2]) == RecordStanding.Holder
            && snapshot.Standing(second, records[2]) == RecordStanding.BelowRecord, "Custom float records compare raw values before display rounding");
        Check(snapshot.Standing(first, records[3]) == RecordStanding.Unachieved
            && snapshot.Standing(second, records[3]) == RecordStanding.Unachieved, "Missing zero and negative values never create a positive record");
        Check(snapshot.Standing(third, records[1]) == null && snapshot.Standing(third, records[2]) == null
            && snapshot.Standing(third, records[3]) == null, "Non-finite values receive vanilla coloring and do not contribute to maxima");
        Check(new[] { dead, outsider, noTracker, prisoner, slave, animal, visitor, null }.All(p => snapshot.Standing(p, records[2]) == null),
            "Dead, unaffiliated, imprisoned, enslaved, animal, and incomplete pawns have no standings");

        SetRecord(second, records[2], 22f);
        snapshot.Refresh(world, 1, records.Length, GetCandidates);
        Check(enumerations == 1 && snapshot.Standing(first, records[2]) == RecordStanding.Holder,
            "Repeated UI events reuse one population and maxima per frame");
        snapshot.Refresh(world, 2, records.Length, GetCandidates);
        Check(enumerations == 2 && snapshot.Standing(first, records[2]) == RecordStanding.BelowRecord,
            "Next frame reflects record changes even with no tick advance");
        candidates.Clear();
        candidates.Add(first);
        snapshot.Refresh(world, 3, records.Length, GetCandidates);
        Check(snapshot.Standing(first, records[2]) == RecordStanding.Holder && snapshot.Standing(first, records[3]) == RecordStanding.Unachieved
            && snapshot.Standing(second, records[2]) == null, "Membership changes and single-colonist records refresh together");
        candidates.Clear();
        snapshot.Refresh(NewRecordWorld(), 3, records.Length, GetCandidates);
        Check(enumerations == 4 && snapshot.Standing(first, records[2]) == null,
            "A different world invalidates even within the same frame and an empty population yields no standings");

        VerifyNativeRecordPopulation(harmony, first, second, third);
    }

    private static void VerifyNativeRecordPopulation(Harmony harmony, Pawn first, Pawn second, Pawn third)
    {
        var originalGame = Current.Game;
        var originalState = Current.ProgramState;
        var mapContents = AccessTools.PropertyGetter(typeof(MapPawns), nameof(MapPawns.AllPawnsUnspawned));
        harmony.Patch(mapContents, prefix: new HarmonyMethod(typeof(Program), nameof(EmptyRecordMapContents)));
        try
        {
            var world = NewRecordWorld();
            var game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            game.World = world;
            var maps = new List<Map>();
            foreach (Pawn pawn in new[] { first, second })
            {
                var map = (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));
                map.mapPawns = new MapPawns(map);
                ((List<Pawn>)map.mapPawns.AllPawnsSpawned).Add(pawn);
                maps.Add(map);
            }
            AccessTools.Field(typeof(Game), "maps").SetValue(game, maps);
            AccessTools.Field(typeof(Game), "questManager").SetValue(game, new QuestManager());
            Current.Game = game;
            Current.ProgramState = ProgramState.Playing;
            var caravan = (Caravan)RuntimeHelpers.GetUninitializedObject(typeof(Caravan));
            caravan.pawns = new ThingOwner<Pawn>(caravan);
            caravan.PawnsListForReading.Add(third);
            world.worldObjects.Caravans.Add(caravan);

            Pawn passenger = CreateRecordPawn(11);
            var transport = new TravellingTransporters();
            var contents = new ActiveTransporterInfo(transport);
            ((ThingOwner<Thing>)contents.innerContainer).InnerListForReading.Add(passenger);
            var transporters = (List<ActiveTransporterInfo>)AccessTools.Field(typeof(TravellingTransporters), "transporters").GetValue(transport);
            transporters.Add(contents);
            world.worldObjects.TravellingTransporters.Add(transport);

            List<Pawn> found = ColonistRecordComparison.FindCandidates();
            Check(found.Contains(first) && found.Contains(second) && found.Contains(third) && found.Contains(passenger),
                "Native colonist finder combines two active maps, a caravan, and travelling transport");

            var enter = AccessTools.DeclaredMethod(typeof(ColonistRecordsCardPatch), "Prefix");
            var exit = AccessTools.DeclaredMethod(typeof(ColonistRecordsCardPatch), "Finalizer");
            object[] state = { first, null };
            var settings = BetterTradeColorsMod.Settings;
            bool wasEnabled = settings.ColorColonistRecords;
            try
            {
                bool drawsCard = (bool)enter.Invoke(null, state);
                Check(drawsCard && ColonistRecordsCardPatch.Active?.Contains(passenger) == true,
                    "An enabled Records card activates the native comparison population");
                exit.Invoke(null, new[] { state[1] });
                Check(ColonistRecordsCardPatch.Active == null, "Card finalizer restores its previous render context");
                state[0] = CreateRecordPawn(12);
                drawsCard = (bool)enter.Invoke(null, state);
                Check(drawsCard && ColonistRecordsCardPatch.Active == null,
                    "An out-of-population pawn retains the original card without a comparison");
                exit.Invoke(null, new[] { state[1] });

                settings.ColorColonistRecords = false;
                state[0] = first;
                drawsCard = (bool)enter.Invoke(null, state);
                Check(drawsCard && ColonistRecordsCardPatch.Active == null,
                    "A disabled Records card keeps its vanilla path for an eligible pawn");
            }
            finally
            {
                settings.ColorColonistRecords = wasEnabled;
                exit.Invoke(null, new object[] { null });
            }
        }
        finally
        {
            Current.Game = originalGame;
            Current.ProgramState = originalState;
            harmony.Unpatch(mapContents, HarmonyPatchType.Prefix, harmony.Id);
        }
    }

    private static readonly List<Pawn> NoContainedPawns = new List<Pawn>();

    private static bool EmptyRecordMapContents(ref List<Pawn> __result)
    {
        __result = NoContainedPawns;
        return false;
    }

    private static World NewRecordWorld()
    {
        var world = (World)RuntimeHelpers.GetUninitializedObject(typeof(World));
        world.worldObjects = new WorldObjectsHolder();
        return world;
    }

    private static RecordDef[] CreateRecordDefs(int count)
    {
        DefDatabase<RecordDef>.Clear();
        var records = new RecordDef[count];
        for (int i = 0; i < count; i++)
        {
            records[i] = new RecordDef { defName = "BTC_CustomRecord" + i, type = i == 0 ? RecordType.Time : i == 1 ? RecordType.Int : RecordType.Float };
            DefDatabase<RecordDef>.Add(records[i]);
        }
        return records;
    }

    private static Pawn CreateRecordPawn(int id, Faction faction = null)
    {
        var pawn = new Pawn
        {
            def = new ThingDef { category = ThingCategory.Pawn, race = new RaceProperties { intelligence = Intelligence.Humanlike } },
            thingIDNumber = id
        };
        pawn.SetFactionDirect(faction ?? PlayerRecordFaction);
        pawn.health = new Pawn_HealthTracker(pawn);
        pawn.records = new Pawn_RecordsTracker(pawn);
        return pawn;
    }

    private static void SetRecord(Pawn pawn, RecordDef record, float value) =>
        ((DefMap<RecordDef, float>)RecordValues.GetValue(pawn.records))[record] = value;

    private static void BenchmarkRecords(int pawnCount, int recordCount)
    {
        const int Frames = 120;
        const int GuiPasses = 3;
        var records = CreateRecordDefs(recordCount);
        var pawns = Enumerable.Range(1, pawnCount).Select(id => CreateRecordPawn(id)).ToList();
        Pawn selected = pawns[0];
        for (int i = 0; i < pawnCount; i++)
        {
            var values = (DefMap<RecordDef, float>)RecordValues.GetValue(pawns[i].records);
            for (int j = 0; j < recordCount; j++)
                values[j] = (i + j) % 12;
        }
        var world = NewRecordWorld();
        var snapshot = new ColonistRecordComparison();
        int enumerations = 0;
        List<Pawn> Population()
        {
            enumerations++;
            return pawns;
        }
        Func<List<Pawn>> population = Population;
        // Warm the same lookup paths before timing steady-state refreshes.
        for (int frame = -Frames; frame < 0; frame++)
        {
            snapshot.Refresh(world, frame, recordCount, population);
            foreach (RecordDef record in records)
                snapshot.Standing(selected, record);
        }
        enumerations = 0;
        int classified = 0;
        long start = Stopwatch.GetTimestamp();
        long bytesBefore = GC.GetAllocatedBytesForCurrentThread();
        for (int frame = 1; frame <= Frames; frame++)
        {
            for (int pass = 0; pass < GuiPasses; pass++)
            {
                snapshot.Refresh(world, frame, recordCount, population);
                foreach (RecordDef record in records)
                    if (snapshot.Standing(selected, record).HasValue)
                        classified++;
            }
        }
        long allocations = GC.GetAllocatedBytesForCurrentThread() - bytesBefore;
        double ms = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        Check(enumerations == Frames && classified == Frames * GuiPasses * recordCount, "Benchmark reuses one comparison population per frame");
        // Populated fixture maps perform one read per definition per pawn per refresh,
        // plus the selected pawn's value per row. Rendering and string formatting are excluded.
        long reads = (long)enumerations * pawnCount * recordCount + classified;
        Console.WriteLine($"RECORD PERF (simulated): {pawnCount} pawns x {recordCount} defs, {Frames} frames x {GuiPasses} passes, " +
            $"{enumerations} enumerations, {reads:N0} raw lookups, {allocations:N0} bytes, {ms:F2} ms.");
    }
}
