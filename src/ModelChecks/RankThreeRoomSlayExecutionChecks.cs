using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RankThreeRoomSlayExecutionChecks
{
    internal static void Native(string path, string? replayPath = null)
    {
        using var source = ModelJson.ReadFixture(path);
        Check(path, source.RootElement);
        if (replayPath != null)
        {
            using var replay = ModelJson.ReadFixture(replayPath);
            Check(replayPath, replay.RootElement);
            Require(source.RootElement.GetProperty("NativeRunInput").ContentEquals(replay.RootElement.GetProperty("NativeRunInput")),
                "Independent room slay process changed its original campaign input.");
            foreach (string field in new[] { "Actions", "Turns" })
            {
                var first = source.RootElement.GetProperty(field).EnumerateArray().ToArray();
                var second = replay.RootElement.GetProperty(field).EnumerateArray().ToArray();
                Require(first.Length == second.Length, "Independent room slay input count differs.");
                for (int index = 0; index < first.Length; index++)
                foreach (string state in new[] { "Before", "Actual" })
                    Require(NativeBranchObservationChecks.Key(first[index].GetProperty(state).Deserialize<BattleTurnState>()!) ==
                        NativeBranchObservationChecks.Key(second[index].GetProperty(state).Deserialize<BattleTurnState>()!),
                        $"Independent room slay {field}/{state} differs at{index}.");
            }
        }
        Console.WriteLine("RANK-THREE-ROOM-SLAY-EXECUTION PASS: saved60ATK/25HP champion, native three slays raise ally regen9to15, complete room/train/action/EndTurn state and RNG, four immutable exchanges and owned full chains; controlled combat, not campaign victory; independentPair=" + (replayPath != null));
    }

    private static void Check(string path, FixtureValue fixture)
    {
        Require(fixture.GetProperty("ModifierScenario").GetString() == "rank-three-room-slay-execution" &&
            fixture.GetProperty("RankThreeRoomSlayPrepared").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }.All(key => fixture.GetProperty(key).GetInt32() == 0),
            "Missing complete explicitly controlled native room slay witness.");
        RoomCombatChecks.NativeRoomOnly(path);
        TrainCombatChecks.Native(fixture);
        BattleActionChecks.Native(fixture);
        BattleTurnChecks.Native(fixture);
        var actions = fixture.GetProperty("Actions").EnumerateArray().ToArray();
        Require(actions.Length == 2 && fixture.GetProperty("Turns").GetArrayLength() == 11 &&
            !fixture.GetProperty("NativeWon").GetBoolean(), "Preserve two deployments and the complete11-EndTurn controlled defeat.");
        var deployed = actions[1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var champion = deployed.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == 3);
        Require(champion is { AssetKey: "MonsterChampionUnderlegion1", Attack: 60, Health: 25 } &&
            champion.Triggers.Where(trigger => trigger.Kind == "OnKill").SelectMany(trigger => trigger.Effects).Single().Action is
                { Type: "AdjustAllStatusEffects", Value: 2, Target: "Room", StatusAdjustmentFilter: 1, AllowPlayer: true, AllowEnemy: false },
            "Saved rank-three champion payload changed.");
        var exchange = fixture.GetProperty("Stages").EnumerateArray().Single(row => row.GetProperty("Index").GetInt32() == 19);
        Require(exchange.GetProperty("Kind").GetString() == "Exchange" && exchange.GetProperty("Turn").GetInt32() == 2,
            "Missing original second-turn slay exchange.");
        var before = exchange.GetProperty("Before").Deserialize<RoomCombatState>()!;
        var actual = exchange.GetProperty("Actual").Deserialize<RoomCombatState>()!;
        Require(before.Units.Single(unit => unit.Id == 2).Status("regen")?.Stacks == 9 &&
            actual.Units.Single(unit => unit.Id == 2).Status("regen")?.Stacks == 15 &&
            new[] { 7, 8, 9 }.All(id => before.Units.Any(unit => unit.Id == id && unit.Team == CombatTeam.Enemy) &&
                actual.Units.All(unit => unit.Id != id)), "Native ally did not gain six regeneration stacks after three enemy deaths.");
        string immutable = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            var result = RoomCombatModel.Exchange(before);
            Require(result.Supported && result.Events.Count(item => item.Kind == "Death" && item.Actor == 3) == 3 &&
                JsonSerializer.Serialize(result.State!.Units, ModelJson.Options) == JsonSerializer.Serialize(actual.Units, ModelJson.Options) &&
                JsonSerializer.Serialize(BattleTurnChecks.ComparableContext(result.State.Context!), ModelJson.Options) ==
                    JsonSerializer.Serialize(BattleTurnChecks.ComparableContext(actual.Context!), ModelJson.Options),
                "Independent slay exchange differs from full native successor/RNG.");
        });
        Require(immutable == JsonSerializer.Serialize(before, ModelJson.Options), "Slay exchange mutated its parent.");
        var root = actions[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        string parent = NativeBranchObservationChecks.Key(root);
        Parallel.For(0, 4, _ =>
        {
            var current = root;
            foreach (var row in actions)
            {
                Require(NativeBranchObservationChecks.Key(current) ==
                    NativeBranchObservationChecks.Key(row.GetProperty("Before").Deserialize<BattleTurnState>()!), "Owned deployment root differs.");
                var result = BattleActionModel.PlayCard(current, row.GetProperty("Action").Deserialize<PlayCardAction>()!);
                Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) ==
                    NativeBranchObservationChecks.Key(row.GetProperty("Actual").Deserialize<BattleTurnState>()!), "Owned deployment successor differs.");
                current = result.State!;
            }
            Require(NativeBranchObservationChecks.Key(current) ==
                NativeBranchObservationChecks.Key(fixture.GetProperty("Turns")[0].GetProperty("Before").Deserialize<BattleTurnState>()!),
                "Owned deployments do not join the first native EndTurn.");
            BattleTurnChecks.RunIndependentChain(current, fixture.GetProperty("Turns"), compareRoomEnabled: true);
        });
        Require(parent == NativeBranchObservationChecks.Key(root), "Owned slay battle mutated its root.");
    }

    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
