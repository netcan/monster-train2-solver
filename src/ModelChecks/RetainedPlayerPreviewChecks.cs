using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RetainedPlayerPreviewChecks
{
    internal const string Reason = "Retained troop upgrade cleanup differs after the ninth end turn.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason, Historical);
        Require(rows.Length == 39 && rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Missing natural 39-input preview cleanup witness.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(before.Spawn.Turn == 8 && actual.Spawn.Turn == 9, "Wrong natural turn boundary.");
        var world = actual.Spawn.Train.Context!.Enchantments!;
        var canceled = world.RetainedUnits.Where(actor => actor.Unit.Id is 48 or 50).ToArray();
        Require(canceled.Length == 2 && canceled.All(actor => actor.Preview && actor.RoomIndex == -1 &&
            actor.Unit is { AssetKey: "MonsterFunguy", Health: 4, BaseAttack: 3, DeathState.IsDestroyed: true,
                DeathState.HasStatisticsListener: false, DeathState.StatisticsListenerOnce: false } &&
            actor.Unit.Modifiers!.Upgrades.Count == 0 && actor.Unit.Status("horde")?.Stacks == 1 &&
            actor.Unit.Status("fullmoon")?.Stacks == 1), "Preview cancellation restored numeric state or kept lists/listeners.");
        var living = canceled[0].Unit.WithDeathState(new UnitDeathState(false, false, true,
            statisticsListenerOnce: true, isDespawned: false, isDestroyed: false));
        var preview = new EnchantmentRetainedUnit(living, 0, true);
        Require(PreviewBirthModel.CancelPlayer(preview).Unit.DeathState?.IsDestroyed == true &&
            ReferenceEquals(PreviewBirthModel.CancelPlayer(new EnchantmentRetainedUnit(living, 0, false)).Unit, living),
            "Preview cancellation changed a primary player actor.");
        var enemyNode = JsonSerializer.SerializeToNode(living, ModelJson.Options)!;
        enemyNode["Team"] = (int)CombatTeam.Enemy;
        var enemy = new EnchantmentRetainedUnit(enemyNode.Deserialize<CombatUnit>(ModelJson.Options)!, 0, true);
        Require(ReferenceEquals(PreviewBirthModel.CancelPlayer(enemy), enemy) &&
            ReferenceEquals(PreviewBirthModel.CancelPlayer(canceled[0]), canceled[0]), "Enemy preview/reset or idempotent destruction changed.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            var result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual) &&
                parent == JsonSerializer.Serialize(before, ModelJson.Options), "Preview cleanup changed native successor/RNG or mutated its parent.");
        });
        if (replay.HasValue)
        {
            NativeBranchObservationChecks.Replay(replay.Value, Reason);
            NativeBranchObservationChecks.Pair(source, replay.Value, originalPredictionPresent: true);
        }
        Console.WriteLine("RETAINED-PLAYER-PREVIEW PASS:39 native inputs/full state/RNG, two earlier temporary player births removed without death/primary restore, " +
            "four immutable branches and primary/enemy/idempotence controls; independentPair=" + replay.HasValue);
    }
    private static void Historical(BattleTurnState historical, BattleTurnState actual)
    {
        var context = historical.Spawn.Train.Context!;
        var world = context.Enchantments!;
        var old = world.RetainedUnits.Where(actor => actor.Unit.Id is 48 or 50).ToArray();
        Require(old.Length == 2 && old.All(actor => actor.Preview && actor.RoomIndex == 0 && actor.Unit.Health == 4 &&
            actor.Unit.DeathState?.IsDestroyed == false && actor.Unit.Modifiers!.Upgrades is
                [{ DataId: "118977af-2756-44ff-aa49-ac31ef21fabc", AssetKey: "InfectoryEffect_Upgrade" }]),
            "Original native mismatch no longer proves retained preview cleanup.");
        var expectedKey = JsonNode.Parse(NativeBranchObservationChecks.Key(historical))!;
        var actualKey = JsonNode.Parse(NativeBranchObservationChecks.Key(actual))!;
        var expectedActors = expectedKey["Context"]!["Enchantments"]!["RetainedUnits"]!.AsArray();
        var actualActors = actualKey["Context"]!["Enchantments"]!["RetainedUnits"]!.AsArray();
        for (int index = 0; index < expectedActors.Count; index++)
            if (expectedActors[index]!["Unit"]!["Id"]!.GetValue<int>() is 48 or 50)
                expectedActors[index] = actualActors[index]!.DeepClone();
        Require(JsonNode.DeepEquals(expectedKey, actualKey), "Historical mismatch includes changes outside the two canceled preview actors.");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
