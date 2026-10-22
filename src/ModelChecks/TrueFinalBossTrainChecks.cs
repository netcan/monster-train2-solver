using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TrueFinalBossTrainChecks
{
    internal static void Native(FixtureValue root)
    {
        void Require(bool value, string message) => TrueFinalBossChecks.Require(value, message);
        FixtureValue[] rows = root.GetProperty("TrainPhases").EnumerateArray().ToArray();
        bool multi = root.GetProperty("Policy").GetString() == "units-and-junk";
        Require(rows.Length == (multi ? 11 : 10), "Missing native true-final-boss train phases.");
        int combat = 0, ascend = 0;
        var inputs = new List<TrainCombatState>(); var parents = new List<string>(); var expected = new List<string>();
        foreach (FixtureValue row in rows)
        {
            TrainCombatState before = row.GetProperty("Before").Deserialize<TrainCombatState>()!;
            TrainCombatState actual = row.GetProperty("Actual").Deserialize<TrainCombatState>()!;
            string kind = row.GetProperty("Kind").GetString()!;
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            TrainCombatResult result = kind == "Combat" ? TrainCombatModel.ResolveCombat(before) : TrainCombatModel.Ascend(before);
            string? difference = result.Supported ? ModelJson.Difference(Comparable(result.State!, kind),
                Comparable(actual, kind)) : result.UnsupportedReason;
            Require(result.Supported && difference == null && JsonSerializer.Serialize(before, ModelJson.Options) == parent,
                "Native true-final-boss full train phase differs at " + row.GetProperty("Index").GetInt32() + " " + kind + ": " + difference);
            if (kind == "Combat") combat++; else ascend++;
            inputs.Add(before); parents.Add(parent); expected.Add(Comparable(actual, kind));
        }
        Require(combat == (multi ? 6 : 5) && ascend == 5 && inputs.Count(state => state.Context?.LifeLinks != null) == (multi ? 9 : 8),
            "Missing deployment, all-five-actor combat or ordinary-wave ascension witnesses.");
        Parallel.For(0, 64, index =>
        {
            int selected = index % rows.Length;
            string kind = rows[selected].GetProperty("Kind").GetString()!;
            TrainCombatResult branch = kind == "Combat" ? TrainCombatModel.ResolveCombat(inputs[selected]) : TrainCombatModel.Ascend(inputs[selected]);
            Require(branch.Supported && Comparable(branch.State!, kind) == expected[selected] &&
                JsonSerializer.Serialize(inputs[selected], ModelJson.Options) == parents[selected], "Concurrent true-final-boss train phase changed its parent or result.");
        });
        RequirePreviewGuarded(inputs[2]);
        var ally = new CombatUnit(1000, "uncalibrated-ally", CombatTeam.Player, 1, 10, 10, true, false, false, []);
        TrainCombatState allied = EnchantmentWorldModel.Rebase(new TrainCombatState(inputs[2].Rooms.Select(room => room.RoomIndex == 0 ?
            room.WithUnits(room.Units.Concat(new[] { ally }).ToArray()) : room).ToArray(), inputs[2].Movement, inputs[2].EnemySlotsPerRoom, inputs[2].Context));
        Require(!TrainCombatModel.ResolveCombat(allied).Supported, "No-allied-unit train proof must not admit uncalibrated friendly callbacks.");
        Console.WriteLine($"TRUE-FINAL-BOSS-TRAIN PASS: {combat} native train combats and {ascend} ascents; units, physical points, shared context/cards/RNG and 64 immutable concurrent branches match; unprepared preview and uncalibrated allies remain guarded.");
    }

    internal static void RequirePreviewGuarded(TrainCombatState state)
    {
        var preview = new TrainCombatState(state.Rooms.Select(room => new RoomCombatState(room.RoomIndex, room.Deployment,
            room.Units, room.ExternalInteractions, state.Context, true, room.ActiveCompanionBossIds)).ToArray(), state.Movement, state.EnemySlotsPerRoom, state.Context);
        TrueFinalBossChecks.Require(!TrainCombatModel.ResolveCombat(preview).Supported, "True-final-boss preview without prepared health copies must remain guarded.");
    }

    internal static string Comparable(TrainCombatState state, string kind)
    {
        JsonObject value = JsonSerializer.SerializeToNode(state, ModelJson.Options)!.AsObject();
        // Room snapshots carry a redundant capture-time context; the authoritative
        // shared context is the train's. Preserve every other room/train field.
        foreach (JsonNode? room in value["Rooms"]!.AsArray()) room!.AsObject().Remove("Context");
        value["Context"] = JsonSerializer.SerializeToNode(BattleTurnChecks.ComparableContext(state.Context), ModelJson.Options);
        // The native Ascend coroutine can finish the next draw before returning.
        if (kind == "Ascend") (value["Context"]?["Statistics"] as JsonObject)?.Remove("StoredCards");
        return value.ToJsonString();
    }
}
