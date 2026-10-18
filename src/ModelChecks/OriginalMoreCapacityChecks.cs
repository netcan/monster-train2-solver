using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalMoreCapacityChecks
{
    private static CombatContext Context(RoomCapacityState[] rooms, CombatRelicState[] owners,
        BattleResourceState? raw = null, UnityRng? rng = null, int selected = 0, int turn = 0)
    {
        raw ??= new(3, 0, 5, 10, 10, "pyre", 0, 80, 0, 0, [], [], []);
        UnityRng state = rng ?? UnityRng.Seed(593);
        return new(new([], [], [], state, 0, []), state, 0, 1, raw.MaxHandSize, relics: owners,
            resources: raw, roomCapacities: rooms, queryFrame: new(turn: turn, selectedRoomIndex: selected),
            energyState: new(raw.MaxEnergy, 0, 0, "MonsterTurn", true));
    }
    internal static void Run()
    {
        var effect = new RelicResourceEffect(0, RelicSingleFloorCapacityModel.Effect, 2, 0, 2, false, paramFloat: 1f);
        var owner = new CombatRelicState(RelicSingleFloorCapacityModel.DataId, RelicSingleFloorCapacityModel.AssetKey,
            [RelicSingleFloorCapacityModel.Effect], resourceEffects: [effect], disallowedInPlacementPhase: false);
        RoomCapacityState[] rooms = Enumerable.Range(0, 4).Select(index => new RoomCapacityState(index, 5, 6, index == 3, 0, [])).ToArray();
        CombatContext input = Context(rooms, [owner]); string parent = JsonSerializer.Serialize(input, ModelJson.Options);
        Parallel.For(0, 16, _ => {
            RoomCapacityResult result = RelicSingleFloorCapacityModel.Apply(input, 0);
            Require(result.Supported && result.Context!.RoomCapacities!.All(room => room.PlayerMaximum == (room.RoomIndex == 1 ? 7 : 5) && room.EnemyMaximum == 6) &&
                result.Context.QueryFrame!.SelectedRoomIndex == 1 && result.Context.BattleRng.Equals(input.BattleRng),
                "Single-floor capacity must only add two friendly slots on floor one: " + result.UnsupportedReason);
        });
        foreach (int turn in new[] { 1, 2, 5 }) Require(ReferenceEquals(RelicSingleFloorCapacityModel.Apply(input, turn).Context, input), "Capacity repeated after deployment.");
        Require(ReferenceEquals(RelicSingleFloorCapacityModel.Apply(input, 0, preview: true).Context, input), "Capacity ran in preview.");
        var missing = Context(rooms.Where(room => room.RoomIndex != 1).ToArray(), [owner]);
        Require(!RelicSingleFloorCapacityModel.Test(missing, 0) && ReferenceEquals(RelicSingleFloorCapacityModel.Apply(missing, 0).Context, missing), "Missing target room must not apply.");
        var absent = Context(rooms, []);
        Require(ReferenceEquals(RelicSingleFloorCapacityModel.Apply(absent, 0).Context, absent), "Removed owner must not apply.");
        foreach (int maximum in new[] { 29, 30 }) {
            var bounded = Context(rooms.Select(room => new RoomCapacityState(room.RoomIndex, maximum, 6, room.IsPyre, 0, [])).ToArray(), [owner]);
            Require(RelicSingleFloorCapacityModel.Apply(bounded, 0).Context!.RoomCapacities![1].PlayerMaximum == 30, "Native capacity upper bound differs.");
        }
        Require(RelicModel.Validate([new CombatRelicState("other", RelicSingleFloorCapacityModel.AssetKey,
            [RelicSingleFloorCapacityModel.Effect], resourceEffects: [effect], disallowedInPlacementPhase: false)]) != null, "Unknown capacity owners require calibration.");
        Require(JsonSerializer.Serialize(input, ModelJson.Options) == parent, "Capacity branches mutated their parent.");
        Console.WriteLine("MORE-CAPACITY-PURE PASS: exact owner, middle-floor-only +2, later-turn/preview/missing-room/removed-owner controls, capacity bound and immutable parallel branches.");
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 223 && root.GetProperty("OriginalMoreCapacityPrepared").GetBoolean() &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending", "OriginalMoreCapacityCaptureFailures", "OriginalMoreCapacityMismatches", "OriginalMoreCapacityUnsupported", "OriginalMoreCapacityPending" }
                .All(name => root.GetProperty(name).GetInt32() == 0), "Native MoreCapacity acceptance failed.");
        int positive = 0, gated = 0, applications = 0;
        foreach (FixtureValue row in root.GetProperty("OriginalMoreCapacityRecords").EnumerateArray()) {
            FixtureValue before = row.GetProperty("Before"), after = row.GetProperty("After");
            Require(row.GetProperty("Completed").GetBoolean(), "Capacity observation did not finish.");
            RoomCapacityState[] rooms = before.GetProperty("Rooms").Deserialize<RoomCapacityState[]>()!;
            var input = Context(rooms, before.GetProperty("Relics").Deserialize<CombatRelicState[]>()!,
                before.GetProperty("Resources").Deserialize<BattleResourceState>()!, before.GetProperty("BattleRng").Deserialize<UnityRng>(),
                before.GetProperty("SelectedRoom").GetInt32(), before.GetProperty("Turn").GetInt32());
            if (row.GetProperty("Kind").GetString() == "Test") {
                bool expected = RelicSingleFloorCapacityModel.Test(input, before.GetProperty("Turn").GetInt32());
                Require(row.GetProperty("Result").GetBoolean() == expected, "Native capacity timing Test differs.");
                Equal(before.ToObjectGraph(), after.ToObjectGraph(), "Test changed native state");
                if (expected) positive++; else gated++;
                Require(row.GetProperty("Notifications").GetInt32() == 0, "Test notified the relic.");
            } else {
                applications++;
                Require(rooms[1].PlayerMaximum == 5 && row.GetProperty("Notifications").GetInt32() == 1,
                    "Missing natural 5-to-7 application or native manager notification.");
                string parent = JsonSerializer.Serialize(input, ModelJson.Options);
                Parallel.For(0, 16, _ => {
                    RoomCapacityResult result = RelicSingleFloorCapacityModel.Apply(input, before.GetProperty("Turn").GetInt32());
                    Require(result.Supported, result.UnsupportedReason ?? "Capacity failed.");
                    Equal(result.Context!.RoomCapacities, after.GetProperty("Rooms").Deserialize<RoomCapacityState[]>(), "room capacities");
                    Require(result.Context.QueryFrame!.SelectedRoomIndex == after.GetProperty("SelectedRoom").GetInt32(), "Selected floor differs.");
                });
                Require(parent == JsonSerializer.Serialize(input, ModelJson.Options), "Native capacity replay mutated input.");
                foreach (string name in new[] { "Resources", "Relics", "BattleRng", "Turn", "Preview" }) Equal(before.GetProperty(name).ToObjectGraph(), after.GetProperty(name).ToObjectGraph(), "Apply changed " + name);
            }
        }
        Require(positive == 1 && applications == 1 && gated >= 1, "Missing natural opening callback or later-turn controls.");
        int[] cards = root.GetProperty("OriginalMoreCapacity").GetProperty("CardIds").EnumerateArray().Select(item => item.GetInt32()).ToArray();
        FixtureValue[] plays = root.GetProperty("Actions").EnumerateArray().Where(row => row.GetProperty("ScenarioAction").GetBoolean()).ToArray();
        Require(cards.Length == 8 && plays.Length == 7 && plays.Select(row => row.GetProperty("Action").GetProperty("CardInstanceId").GetInt32()).SequenceEqual(cards.Take(7)) &&
            plays.All(row => row.GetProperty("Action").GetProperty("RoomIndex").GetInt32() == 1), "Missing ordered seven middle-floor deployment plays.");
        BattleTurnState filled = plays[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatUnit[] units = filled.Spawn.Train.Rooms.Single(room => room.RoomIndex == 1).Units.Where(unit => unit.Team == CombatTeam.Player).ToArray();
        Require(units.Length == 7 && units.All(unit => unit.Size == 1) && filled.Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 1).PlayerMaximum == 7,
            "The native middle floor is not filled by seven size-one Monsters.");
        Require(!BattleActionModel.EnumerateSupportedPlays(filled).Any(action => action.CardInstanceId == cards[7] && action.RoomIndex == 1), "An eighth size-one Monster incorrectly fits the full floor.");
        Console.WriteLine($"ORIGINAL-MORE-CAPACITY-NATIVE PASS: one natural 5->7 middle-floor callback, one notification, {gated} later-turn gates, seven actual size-one deployments, eighth-unit rejection, full projection and immutable parallel replay.");
    }
    private static void Equal<T>(T a, T b, string label)
    {
        string predicted = JsonSerializer.Serialize(a, ModelJson.Options), actual = JsonSerializer.Serialize(b, ModelJson.Options);
        Require(predicted == actual, "MoreCapacity " + label + " differs: " + ModelJson.Difference(predicted, actual));
    }
    private static void Require(bool condition, string error) { if (!condition) throw new InvalidOperationException(error); }
}
