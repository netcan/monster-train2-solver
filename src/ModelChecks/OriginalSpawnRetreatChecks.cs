using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalSpawnRetreatChecks
{
    private static CombatRelicState Owner() => new(RelicSpawnRetreatModel.DataId, RelicSpawnRetreatModel.AssetKey,
        [RelicAfterSpawnChanceStatusModel.Effect], isCovenant: false, disallowedInPlacementPhase: false,
        afterSpawnChanceRules: [new(0, 100, 1, "Heroes", true, true, "SubtypesData_None", RelicSpawnRetreatModel.ExcludedSubtypes, 0, true, 1, true, [],
            "CardEffectFloorRearrange", "BackInRoom", "Heroes", "Spell", 0, 0, 0, false, false, false, [], new bool[4])]);
    internal static void Run()
    {
        var rng = UnityRng.Seed(269);
        var context = new CombatContext(new([], [], [], rng, 0, []), rng, 0, 1, 10, [], relics: [Owner()]);
        CombatUnit Unit(int id, CombatTeam team, params CombatStatus[] statuses) => new(id, "unit" + id, team, 1, 20, 20, true, false, false, statuses);
        var root = new RoomCombatState(0, false, [Unit(1, CombatTeam.Enemy), Unit(2, CombatTeam.Enemy), Unit(3, CombatTeam.Player)], [], context);
        Require(RelicModel.Validate(context) == null, "Retreat definition rejected.");
        string parent = JsonSerializer.Serialize(root, ModelJson.Options);
        RoomCombatResult moved = RelicSpawnRetreatModel.Apply(root, 1, "AfterSpawnEnchant", true);
        Require(moved.Supported, moved.UnsupportedReason!);
        Require(moved.State!.Units.Where(unit => unit.Team == CombatTeam.Enemy).Select(unit => unit.Id).SequenceEqual([2, 1]) &&
            Flags(moved.State.Context!).SequenceEqual([true, false, false, false]), "Back-to-front movement or room flag differs.");
        Equal(RelicSpawnRetreatModel.Apply(moved.State, 2, "AfterSpawnEnchant", true).State, moved.State, "same-room gate");
        RoomCombatResult raw = RelicSpawnRetreatModel.Apply(moved.State, 2, "OnSpawn", false);
        Require(raw.Supported && raw.State!.Units.Where(unit => unit.Team == CombatTeam.Enemy).Select(unit => unit.Id).SequenceEqual([1, 2]),
            "Raw Apply incorrectly reused Test or the room gate.");
        var reset = moved.State.WithContext(RelicSpawnRetreatModel.Reset(moved.State.Context!));
        RoomCombatResult again = RelicSpawnRetreatModel.Apply(reset, 2, "AfterSpawnEnchant", true);
        Require(again.Supported && again.State!.Units.Where(unit => unit.Team == CombatTeam.Enemy).Select(unit => unit.Id).SequenceEqual([1, 2]),
            "Combat-end reset did not restore the same room's eligibility.");
        CombatRelicState owner = root.Context!.Relics!.Single();
        Require(!RelicSpawnRetreatModel.Test(root, root.Units[0], "OnSpawn", owner) &&
            !RelicSpawnRetreatModel.Test(root, root.Units[2], "AfterSpawnEnchant", owner) &&
            !RelicSpawnRetreatModel.Test(root, Unit(4, CombatTeam.Enemy, new CombatStatus("untouchable", 1)), "AfterSpawnEnchant", owner),
            "Trigger, team or untouchable control failed.");
        foreach (string subtype in RelicSpawnRetreatModel.ExcludedSubtypes)
            Require(!RelicSpawnRetreatModel.Test(root, new CombatUnit(5, "excluded", CombatTeam.Enemy, 1, 20, 20, true, false, false, [],
                subtypes: [subtype]), "AfterSpawnEnchant", owner), "Original excluded subtype accepted: " + subtype);
        var preview = new RoomCombatState(0, false, root.Units, [], context, preview: true);
        Require(RelicSpawnRetreatModel.Apply(preview, 1, "AfterSpawnEnchant", true).Supported, "100% retreat preview rejected.");
        var absent = root.WithContext(root.Context.WithRelics([]));
        Equal(RelicSpawnRetreatModel.Apply(absent, 1, "AfterSpawnEnchant", true).State, absent, "owner ablation");
        Parallel.For(0, 16, _ => Equal(RelicSpawnRetreatModel.Apply(root, 1, "AfterSpawnEnchant", true).State, moved.State, "parallel retreat"));
        Require(JsonSerializer.Serialize(root, ModelJson.Options) == parent, "Retreat mutated its parent.");
        Console.WriteLine("SPAWN-RETREAT-PURE PASS: unchanged definition, back-to-front movement, per-room gate, raw bypass, combat reset, controls, preview and 16 immutable branches.");
    }
    private static IReadOnlyList<bool> Flags(CombatContext context) => context.Relics!.Single(owner => owner.DataId == RelicSpawnRetreatModel.DataId)
        .AfterSpawnChanceRules!.Single().TriggeredInRooms!;
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 239 && root.GetProperty("OriginalSpawnRetreatPrepared").GetBoolean() &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Incomplete retreat battle.");
        foreach (string field in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending", "OriginalSpawnRetreatCaptureFailures",
            "OriginalSpawnRetreatMismatches", "OriginalSpawnRetreatUnsupported", "OriginalSpawnRetreatPending" })
            Require(root.GetProperty(field).GetInt32() == 0, "Nonzero " + field);
        int tests = 0, applies = 0, resets = 0, moves = 0, usedResets = 0;
        bool repeatControl = false, triggerControl = false, teamControl = false, subtypeControl = false;
        var turns = new HashSet<int>();
        foreach (var row in root.GetProperty("RelicSpawnRetreatRecords").EnumerateArray())
        {
            string kind = row.GetProperty("Kind").GetString()!, trigger = row.GetProperty("Trigger").GetString()!;
            int actorId = row.GetProperty("ActorId").GetInt32();
            TrainCombatState before = row.GetProperty("Before").Deserialize<TrainCombatState>()!, actual = row.GetProperty("Actual").Deserialize<TrainCombatState>()!;
            RoomCombatState? room = before.Rooms.FirstOrDefault(item => item.Units.Any(unit => unit.Id == actorId));
            CombatUnit? actor = room?.Units.FirstOrDefault(unit => unit.Id == actorId);
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            void Replay()
            {
                if (kind == "Test")
                {
                    bool test = RelicSpawnRetreatModel.Test(room, actor, trigger, before.Context!.Relics!.Single(owner => owner.DataId == RelicSpawnRetreatModel.DataId));
                    Require(test == row.GetProperty("Returned").GetBoolean(), "Retreat Test result differs."); Equal(before, actual, "Test state");
                }
                else if (kind == "Reset") Equal(CardSpellModel.WithContext(before, RelicSpawnRetreatModel.Reset(before.Context!)), actual, "combat reset");
                else
                {
                    var result = RelicSpawnRetreatModel.Apply(before, actorId, trigger, false);
                    Require(result.Supported, result.UnsupportedReason!); Equal(result.State, actual, "raw retreat");
                }
            }
            Replay(); Parallel.For(0, 16, _ => Replay());
            Require(JsonSerializer.Serialize(before, ModelJson.Options) == parent, "Native retreat replay mutated parent.");
            if (kind == "Test")
            {
                tests++; bool returned = row.GetProperty("Returned").GetBoolean();
                subtypeControl |= trigger == "AfterSpawnEnchant" && actor?.Team == CombatTeam.Enemy &&
                    actor.Subtypes.Any(RelicSpawnRetreatModel.ExcludedSubtypes.Contains) && !Flags(before.Context!)[room!.RoomIndex] && !returned;
                if (returned) turns.Add(before.Context!.QueryFrame!.Turn!.Value);
                if (row.GetProperty("Control").GetBoolean())
                {
                    bool excluded = actor?.Subtypes.Any(RelicSpawnRetreatModel.ExcludedSubtypes.Contains) == true;
                    repeatControl |= trigger == "AfterSpawnEnchant" && actor?.Team == CombatTeam.Enemy && !excluded && Flags(before.Context!)[room!.RoomIndex] && !returned;
                    triggerControl |= trigger == "OnSpawn" && actor?.Team == CombatTeam.Enemy && !excluded && !Flags(before.Context!)[room!.RoomIndex] && !returned;
                    teamControl |= trigger == "AfterSpawnEnchant" && actor?.Team == CombatTeam.Player && !Flags(before.Context!)[room!.RoomIndex] && !returned;
                }
            }
            if (kind == "Apply")
            {
                applies++;
                Require(Flags(actual.Context!)[room!.RoomIndex], "Raw Apply did not set its room flag.");
                int[] Order(TrainCombatState state) => state.Rooms.Single(item => item.RoomIndex == room.RoomIndex).Units
                    .Where(unit => unit.Team == CombatTeam.Enemy).Select(unit => unit.Id).ToArray();
                if (!Order(before).SequenceEqual(Order(actual))) moves++;
            }
            if (kind == "Reset")
            {
                resets++; if (Flags(before.Context!).Any(value => value)) usedResets++;
                Require(Flags(actual.Context!).All(value => !value), "Combat reset did not clear all four flags.");
            }
        }
        Require(tests > 0 && applies >= 2 && moves > 0 && resets >= 2 && usedResets >= 2 && turns.Count >= 2 &&
            repeatControl && triggerControl && teamControl && subtypeControl && root.GetProperty("Actions").GetArrayLength() == 2,
            "Missing actual movement, same-floor reset, repeated-room or independent controls.");
        Console.WriteLine($"ORIGINAL-SPAWN-RETREAT-NATIVE PASS: {tests} Tests, {applies} applications, {moves} formation changes, {resets} combat resets, no RNG draws and 16 immutable branches.");
    }
    private static void Equal<T>(T expected, T actual, string label)
    { string a = JsonSerializer.Serialize(expected, ModelJson.Options), b = JsonSerializer.Serialize(actual, ModelJson.Options); Require(a == b, label + ": " + ModelJson.Difference(a, b)); }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
