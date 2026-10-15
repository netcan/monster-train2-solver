using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RelicOpeningCorruptionChecks
{
    private static readonly string[] Ids = ["3a28026e-e44b-4456-b535-7134e7525240", "a66188b9-845d-475e-b6e6-7ea5335ec00b", "a528a1b2-0d14-4fc5-9b8f-4b1f23722422"];
    private static readonly string[] Types = [RelicResourceModel.MaxCorruptionStart, RelicResourceModel.PermanentCorruptionStart, RelicResourceModel.CorruptionPostOpeningDraw];
    private static readonly string[] Keys = ["IncreaseMaxCorruption", "UnremovableCorruptionTop", "CorruptionBattleStart"];
    private static CombatRelicState[] Owners() => Enumerable.Range(0, 3).Select(index =>
        new CombatRelicState(Ids[index], Keys[index], [Types[index]], isCovenant: false, disallowedInPlacementPhase: false,
            resourceEffects: [new(0, Types[index], index == 2 ? 2 : 1, 0, index == 2 ? 2 : 1, false, paramFloat: index == 1 ? 2 : 0)])).ToArray();

    private static TrainCombatState Train(RoomCapacityState[] rooms, CombatRelicState[] relics, BattleResourceState? raw = null, int energy = 0)
    {
        var rng = UnityRng.Seed(29);
        var context = new CombatContext(new([], [], [], rng, 0, []), rng, 0, 1, raw?.MaxHandSize ?? 10,
            statistics: BattleStatistics.Empty(deckCards: []), queryFrame: new(energy: energy, runningCombat: true),
            energyState: new(raw?.MaxEnergy ?? 99, 0, 0, "MonsterTurn", true), roomCapacities: rooms, relics: relics,
            resources: raw ?? new(3, 1, 5, 99, 10, "pyre", 20, 80, 0, 0, [], [], []));
        return new(rooms.Select(room => new RoomCombatState(room.RoomIndex, true, [], [], context)).ToArray(), [], 7, context);
    }
    internal static void Run()
    {
        var source = Train(Enumerable.Range(0, 4).Select(index => new RoomCapacityState(index, 5, 5, index == 3, 0, [], 4, 0)).ToArray(), Owners());
        string frozen = JsonSerializer.Serialize(source);
        var start = RelicResourceModel.ApplyOpeningCorruption(source, placement: true);
        var drawn = RelicResourceModel.ApplyOpeningCorruption(start.State!, postDraw: true, placement: true);
        Require(start.Supported && drawn.Supported && drawn.State!.Context!.RoomCapacities!.All(room =>
            room.MaxCorruption == 5 && room.PermanentCorruption == (room.RoomIndex == 2 ? 1 : 0) &&
            room.Corruption == (room.RoomIndex == 2 ? 3 : 2)), "Original opening corruption order or room scope differs.");
        Require(RelicsUnchanged(start.State!, Owners()) && frozen == JsonSerializer.Serialize(source) &&
            JsonSerializer.Serialize(RelicResourceModel.ApplyOpeningCorruption(source, placement: false).State!.Context!.RoomCapacities) ==
                JsonSerializer.Serialize(source.Context!.RoomCapacities) &&
            JsonSerializer.Serialize(RelicResourceModel.ApplyOpeningCorruption(source, placement: true, preview: true).State!.Context!.RoomCapacities) ==
                JsonSerializer.Serialize(source.Context.RoomCapacities), "Opening phase/preview/notification guards or source isolation differ.");
        foreach (string id in Ids)
        {
            var ablatedSource = Train(source.Context.RoomCapacities!.ToArray(), Owners().Where(owner => owner.DataId != id).ToArray());
            var ablatedStart = RelicResourceModel.ApplyOpeningCorruption(ablatedSource, placement: true);
            var ablated = RelicResourceModel.ApplyOpeningCorruption(ablatedStart.State!, placement: true, postDraw: true);
            Require(ablated.Supported && JsonSerializer.Serialize(ablated.State!.Context!.RoomCapacities) !=
                JsonSerializer.Serialize(drawn.State!.Context!.RoomCapacities), "Original opening owner ablation did not change its affected room values.");
        }
        Console.WriteLine("RELIC-OPENING-CORRUPTION-CHECKS PASS: original owner order, all-room max, selected-floor permanent minimum, post-draw addition, phase/preview guards and immutable owner ablations.");
    }
    private static bool RelicsUnchanged(TrainCombatState state, CombatRelicState[] owners) => JsonSerializer.Serialize(state.Context!.Relics) == JsonSerializer.Serialize(owners);
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("ModifierScenario").GetString() == "original-opening-corruption-relics" &&
            root.GetProperty("GameVersion").GetString() == "2.2.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }.All(name => root.GetProperty(name).GetInt32() == 0),
            "Opening corruption battle failed acceptance.");
        var rows = root.GetProperty("OpeningCorruptionOperations").EnumerateArray().ToArray();
        Require(rows.Length == 3, "Missing three natural opening corruption callbacks.");
        for (int index = 0; index < rows.Length; index++)
        {
            var row = rows[index]; var before = row.GetProperty("Before"); var after = row.GetProperty("After");
            var relics = before.GetProperty("Relics").Deserialize<CombatRelicState[]>()!;
            Require(row.GetProperty("OwnerId").GetString() == Ids[index] && row.GetProperty("Type").GetString() == Types[index] &&
                before.GetProperty("Placement").GetBoolean() && !before.GetProperty("Preview").GetBoolean() &&
                before.GetProperty("Turn").GetInt32() == 0 && before.GetProperty("UnitCorruptionTriggers").GetInt32() == 0,
                "Native callback order, phase or unit-callback precondition differs.");
            Equal(relics.Where(owner => Ids.Contains(owner.DataId)).ToArray(), Owners(), "unchanged original owner metadata");
            var source = Train(before.GetProperty("Rooms").Deserialize<RoomCapacityState[]>()!,
                relics.Where(owner => owner.DataId == Ids[index]).ToArray(), before.GetProperty("Resources").Deserialize<BattleResourceState>()!,
                before.GetProperty("Energy").GetInt32());
            string frozen = JsonSerializer.Serialize(source);
            var predicted = RelicResourceModel.ApplyOpeningCorruption(source, postDraw: index == 2, placement: true);
            Require(predicted.Supported, "Native opening corruption callback unsupported: " + predicted.UnsupportedReason);
            Equal(predicted.State!.Context!.RoomCapacities, after.GetProperty("Rooms").Deserialize<RoomCapacityState[]>(), "individual callback room states");
            Require(row.GetProperty("Notifications").GetInt32() == (index == 1 ? 1 : 0) &&
                before.GetProperty("Relics").ContentEquals(after.GetProperty("Relics")) &&
                before.GetProperty("Resources").ContentEquals(after.GetProperty("Resources")) &&
                before.GetProperty("Energy").GetInt32() == after.GetProperty("Energy").GetInt32() &&
                frozen == JsonSerializer.Serialize(source), "Opening callback notification/resource preservation differs.");
        }
        var initial = rows[0].GetProperty("Before");
        var train = Train(initial.GetProperty("Rooms").Deserialize<RoomCapacityState[]>()!,
            initial.GetProperty("Relics").Deserialize<CombatRelicState[]>()!, initial.GetProperty("Resources").Deserialize<BattleResourceState>()!);
        var wholeStart = RelicResourceModel.ApplyOpeningCorruption(train, placement: true);
        Equal(wholeStart.State!.Context!.RoomCapacities, rows[1].GetProperty("After").GetProperty("Rooms").Deserialize<RoomCapacityState[]>(), "ordered start-battle manager chain");
        var wholeDraw = RelicResourceModel.ApplyOpeningCorruption(wholeStart.State!, postDraw: true, placement: true);
        Equal(wholeDraw.State!.Context!.RoomCapacities, rows[2].GetProperty("After").GetProperty("Rooms").Deserialize<RoomCapacityState[]>(), "start-battle then post-opening-draw chain");
        foreach (var row in root.GetProperty("Turns").EnumerateArray())
        {
            var before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            var after = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            Equal(before.Spawn.Train.Context!.RoomCapacities, after.Spawn.Train.Context!.RoomCapacities, "no repeated opening effects on later turns");
        }
        Console.WriteLine("ORIGINAL-OPENING-CORRUPTION-NATIVE PASS: three unchanged owners, ordered natural start/post-draw callbacks, exact all-room values and permanent floor, notification semantics, ablations and no later repeats.");
    }
    private static void Equal<T>(T left, T right, string label) => Require(JsonSerializer.Serialize(left) == JsonSerializer.Serialize(right), "Native opening corruption " + label + " differs.");
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
