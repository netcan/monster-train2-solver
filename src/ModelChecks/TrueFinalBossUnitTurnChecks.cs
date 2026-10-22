using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TrueFinalBossUnitTurnChecks
{
    internal static void Native(FixtureValue root)
    {
        void Require(bool value, string message) => TrueFinalBossChecks.Require(value, message);
        FixtureValue[] rows = root.GetProperty("UnitTurns").EnumerateArray().ToArray();
        Require(rows.Length == 29 && root.GetProperty("OriginalTrueFinalBossRoute").GetProperty("Scenario").GetString() == "Level9TrueFinalBoss_V2",
            "Missing original true-final-boss native unit turns.");
        int pyreAttacks = 0, dominionAttacks = 0, linkedHealthWrites = 0;
        var inputs = new List<RoomCombatState>(); var expected = new List<string>();
        foreach (FixtureValue row in rows)
        {
            RoomCombatState before = row.GetProperty("Before").Deserialize<RoomCombatState>()!;
            RoomCombatState actual = row.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            int actorId = row.GetProperty("ActorId").GetInt32();
            CombatUnit actor = before.Units.Single(unit => unit.Id == actorId);
            Require(!before.Preview && (inputs.Count == 0 ? before.Context?.LifeLinks == null && before.Units.All(unit => unit.IsPyre) : before.Context?.LifeLinks?.Actors.Count == 5) &&
                (actor.IsPyre || TrueFinalBossChecks.Assets.Contains(actor.AssetKey)), "Native unit turn omitted its original physical actor or canonical links.");
            string parent = JsonSerializer.Serialize(before, ModelJson.Options), target = JsonSerializer.Serialize(actual, ModelJson.Options);
            RoomCombatResult result = RoomCombatModel.ApplyUnitTurn(before, actorId);
            string? difference = result.Supported ? ModelJson.Difference(JsonSerializer.Serialize(result.State, ModelJson.Options), target) : result.UnsupportedReason;
            Require(result.Supported && difference == null && JsonSerializer.Serialize(before, ModelJson.Options) == parent,
                "Native true-final-boss unit turn differs for " + actor.AssetKey + ": " + difference);
            int[] nativeTargets = row.GetProperty("AttackedTargetIds").Deserialize<int[]>()!;
            int[] modelTargets = result.Events.Where(entry => entry.Kind == "Attack" && entry.Actor == actorId).Select(entry => entry.Target).ToArray();
            Require(nativeTargets.SequenceEqual(modelTargets), "Native unit attack target sequence differs.");
            if (actor.IsPyre && nativeTargets.Length > 0) pyreAttacks++;
            if (actor.AssetKey == TrueFinalBossChecks.Assets[0] && nativeTargets.Length > 0) dominionAttacks++;
            linkedHealthWrites += before.Context!.LifeLinks?.Actors.Count(link => actual.Context!.LifeLinks!.Actors.Single(next => next.Id == link.Id).Health != link.Health) ?? 0;
            inputs.Add(before); expected.Add(target);
        }
        Require(pyreAttacks >= 4 && dominionAttacks >= 4 && linkedHealthWrites >= 4,
            "Missing actual Pyre/Dominion attack and health-change witnesses.");
        Parallel.For(0, 64, index =>
        {
            int selected = index % rows.Length;
            RoomCombatResult result = RoomCombatModel.ApplyUnitTurn(inputs[selected], rows[selected].GetProperty("ActorId").GetInt32());
            Require(result.Supported && JsonSerializer.Serialize(result.State, ModelJson.Options) == expected[selected], "Concurrent native boss attack diverged.");
        });
        TrueFinalBossTrainChecks.RequirePreviewGuarded(root.GetProperty("PreCombats")[2].GetProperty("Before").Deserialize<TrainCombatState>()!);
        Console.WriteLine($"TRUE-FINAL-BOSS-UNIT-TURNS PASS: {rows.Length} actual native turns, Pyre attacks={pyreAttacks}, Dominion attacks={dominionAttacks}, linked health writes={linkedHealthWrites}; full room/context/RNG and target sequence equality; 64 immutable concurrent branches; full battle remains guarded.");
    }
}
