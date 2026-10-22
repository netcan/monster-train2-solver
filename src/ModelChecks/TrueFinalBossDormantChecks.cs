using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;
using System.Text.Json;

internal static class TrueFinalBossDormantChecks
{
    internal static void Native(FixtureValue root)
    {
        void Require(bool value, string message) => TrueFinalBossChecks.Require(value, message);
        FixtureValue control = root.GetProperty("TrueFinalBossDormantControl");
        int savagery = control.GetProperty("SavageryId").GetInt32();
        BattleTurnState input = control.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState controlled = control.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        LinkedBossHealthResult mutation = LinkedBossHealthModel.Apply(input.Spawn.Train, savagery, "DebuffHP", 2749);
        Require(mutation.Supported, "Native dormant health control rejected: " + mutation.UnsupportedReason);
        TrainCombatState prepared = mutation.State!;
        int frontId = control.GetProperty("FrontId").GetInt32(), frontHealth = control.GetProperty("FrontHealth").GetInt32();
        if (frontId > 0)
            prepared = EnchantmentWorldModel.Rebase(new TrainCombatState(prepared.Rooms.Select(room => room.WithUnits(room.Units.Select(unit =>
                unit.Id == frontId ? unit.WithHealth(frontHealth) : unit).ToArray())).ToArray(), prepared.Movement, prepared.EnemySlotsPerRoom, prepared.Context));
        Require(TrueFinalBossTrainChecks.Comparable(prepared, "Control") == TrueFinalBossTrainChecks.Comparable(controlled.Spawn.Train, "Control"),
            "Native controlled preparation changed more than the recorded HP debuffs.");
        Require(input.Spawn.Train.Context!.LifeLinks!.Actors.Single(actor => actor.Id == savagery).Health == 2750 &&
            controlled.Spawn.Train.Context!.LifeLinks!.Actors.Single(actor => actor.Id == savagery).Health == 1 &&
            JsonSerializer.Serialize(input.RngStreams) == JsonSerializer.Serialize(controlled.RngStreams) &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }.All(key => root.GetProperty(key).GetInt32() == 0),
            "Missing settled, RNG-neutral native one-HP Savagery control.");
        var cases = new List<(RoomCombatState Before, int Actor, string Expected)>();
        int dormantTurns = 0, trampleHits = 0, bypassHits = 0;
        foreach (FixtureValue row in root.GetProperty("UnitTurns").EnumerateArray())
        {
            RoomCombatState before = row.GetProperty("Before").Deserialize<RoomCombatState>()!;
            if (before.Context?.LifeLinks?.Actors.Any(actor => actor.Id == savagery && actor.Health == 1) != true) continue;
            int actor = row.GetProperty("ActorId").GetInt32();
            RoomCombatResult result = RoomCombatModel.ApplyUnitTurn(before, actor);
            string expected = JsonSerializer.Serialize(row.GetProperty("Actual").Deserialize<RoomCombatState>(), ModelJson.Options);
            string? difference = result.Supported ? ModelJson.Difference(JsonSerializer.Serialize(result.State, ModelJson.Options), expected) : result.UnsupportedReason;
            Require(result.Supported && difference == null, "Native dormant unit turn differs for actor " + actor + ": " + difference);
            Require(row.GetProperty("AttackedTargetIds").Deserialize<int[]>()!.SequenceEqual(result.Events
                .Where(hit => hit.Kind == "Attack" && hit.Actor == actor).Select(hit => hit.Target)), "Dormant direct attack targets differ.");
            if (before.Units.Any(unit => unit.Id == savagery) && before.Units.Single(unit => unit.Id == actor).Team == CombatTeam.Player)
            {
                int[] targets = row.GetProperty("AttackedTargetIds").Deserialize<int[]>()!;
                Require(!targets.Contains(savagery), "Normal player attacks must skip one-HP Savagery.");
                bypassHits += targets.Count(target => before.Units.Single(unit => unit.Id == target).BumpRules?.OuterBoss == true);
            }
            if (row.TryGetProperty("TrampleTargetIds", out FixtureValue trample))
            {
                Require(trample.Deserialize<int[]>()!.SequenceEqual(result.Events.Where(hit => hit.Kind == "Trample" && hit.Actor == actor)
                    .Select(hit => hit.Target)), "Dormant trample targets differ.");
                trampleHits += trample.GetArrayLength();
            }
            if (actor == savagery) dormantTurns++;
            cases.Add((before, actor, expected));
        }
        Require(dormantTurns >= 4, "Missing repeated native one-HP Savagery attacks.");
        if (frontId == 0) Require(bypassHits >= 2, "Missing actual player attacks bypassing dormant Savagery for the outer body.");
        if (frontId > 0) Require(frontHealth == 5 && trampleHits == 1, "Five-HP front must witness one native trample hit.");
        Parallel.For(0, 64, index =>
        {
            var sample = cases[index % cases.Count];
            string parent = JsonSerializer.Serialize(sample.Before, ModelJson.Options);
            RoomCombatResult result = RoomCombatModel.ApplyUnitTurn(sample.Before, sample.Actor);
            Require(result.Supported && JsonSerializer.Serialize(result.State, ModelJson.Options) == sample.Expected &&
                JsonSerializer.Serialize(sample.Before, ModelJson.Options) == parent, "Dormant attack branch changed input or result.");
        });
        TrueFinalBossTrainChecks.Native(root);
        TrueFinalBossPreviewChecks.Native(root, prefixStart: 1);
        FixtureValue[] turns = root.GetProperty("Turns").EnumerateArray().Skip(1).ToArray();
        BattleTurnState start = turns[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        Parallel.For(0, 16, _ =>
        {
            BattleTurnState current = start;
            string parent = JsonSerializer.Serialize(start, ModelJson.Options);
            foreach (FixtureValue row in turns)
            {
                BattleTurnResult result = BattleTurnModel.EndTurn(current);
                BattleTurnState actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
                bool terminal = result.Outcome is RoomOutcome.PlayerDefeated or RoomOutcome.BattleWon;
                string? difference = result.Supported ? ModelJson.Difference(BattleTurnChecks.Comparable(result.State!, terminal),
                    BattleTurnChecks.Comparable(actual, terminal)) : result.UnsupportedReason;
                Require(result.Supported && result.Outcome == (RoomOutcome)row.GetProperty("ActualOutcome").GetInt32() && difference == null,
                    "Independent controlled dormant chain differs: " + difference);
                current = result.State!;
            }
            Require(JsonSerializer.Serialize(start, ModelJson.Options) == parent, "Controlled full loss mutated its root.");
        });
        Console.WriteLine($"TRUE-FINAL-BOSS-DORMANT PASS: {cases.Count} native attack turns, {dormantTurns} one-HP Savagery turns, {trampleHits} trample hits, preview/train equality, 64 immutable attack branches and sixteen independent chains from the controlled boundary. Controlled HP, no natural route or victory claim.");
    }
}
