using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TroopCombatBirthChecks
{
    private const string Reason = "Train combat floor 1: Combined troop upgrades and enchantment source ordering require native calibration.";
    internal static void Native(FixtureValue source, string counterexamplePath, FixtureValue? replay = null)
    {
        using var rawDocument = ModelJson.ReadFixture(counterexamplePath);
        var raw = rawDocument.RootElement;
        Require(raw.GetProperty("NativeWon").ValueKind == FixtureKind.Null && raw.GetProperty("Mismatches").GetInt32() == 2 &&
            raw.GetProperty("Unsupported").GetInt32() == 4, "Preserve the original aborted counterexample and its rejection counts.");
        RoomCombatChecks.NativeObservedRooms(counterexamplePath);
        TrainCombatChecks.Native(raw);
        var frame = raw.GetProperty("Stages")[47].GetProperty("Before").Deserialize<RoomCombatState>()!;
        Require(ReferenceEquals(frame.Context, PreviewBirthModel.CompleteRemoteCombatBirths(frame.Context!, 1)),
            "Same-floor frame completed deferred removals before its observed wait.");
        var frameWorld = frame.Context!.Enchantments!;
        var withoutOwner = frame.Context.WithEnchantments(new EnchantmentWorld(frameWorld.Rooms, frameWorld.Movement,
            frameWorld.EnemySlotsPerRoom, frameWorld.RetainedUnits.Where(actor => actor.Unit.Id != 6).ToArray(),
            frameWorld.EnchanterIds, frameWorld.AllowUpdates, frameWorld.Updating, frameWorld.Preview, frameWorld.TestRng,
            frameWorld.AutomaticLifecycle, frameWorld.TemporaryPreviewUnitIds));
        var unrelated = PreviewBirthModel.CompleteRemoteCombatBirths(withoutOwner, 0);
        Require(unrelated.Enchantments!.RetainedUnits.Single(actor => actor.Unit.Id == 41).Unit.DeathState?.IsDestroyed == false,
            "Plain Infectory preview birth was cancelled without the calibrated troop-owner retirement.");
        var rows = NativeBranchObservationChecks.Replay(source, Reason);
        Require(rows.Length == 32 && rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Missing thirty-two-input native combat prefix.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(before.Spawn.Turn == 5 && actual.Spawn.Turn == 6 &&
            before.Spawn.Train.Context!.Enchantments!.EnchanterIds.SequenceEqual([13, 16, 28]), "Lost source turnover combat boundary.");
        var updates = rows[^1].GetProperty("TroopUpdates").EnumerateArray().ToArray();
        var primary = updates.Single(update => !update.GetProperty("Before").GetProperty("Preview").GetBoolean() &&
            update.GetProperty("Before").GetProperty("Cache").GetArrayLength() == 0 &&
            update.GetProperty("After").GetProperty("Cache").Deserialize<int[]>()!.SequenceEqual([40]));
        var first = primary.GetProperty("Before").GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == 40);
        var last = primary.GetProperty("After").GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == 40);
        Require(primary.GetProperty("Before").GetProperty("Room").GetInt32() == 1 && first.GetProperty("Attack").GetInt32() == 3 &&
            last.GetProperty("Attack").GetInt32() == 5 && first.GetProperty("Trample").GetInt32() == 0 &&
            last.GetProperty("Trample").GetInt32() == 1 && last.GetProperty("Horde").GetInt32() == 1,
            "Native real combat birth did not add exactly one upgrade/status before committing its cache.");
        Require(updates.Any(update => !update.GetProperty("Before").GetProperty("Preview").GetBoolean() &&
            update.GetProperty("Before").GetProperty("Cache").Deserialize<int[]>()!.SequenceEqual([40]) &&
            update.GetProperty("After").GetProperty("Cache").GetArrayLength() == 0), "Native dead target was not pruned from primary cache.");
        var world = actual.Spawn.Train.Context!.Enchantments!;
        var birth = world.RetainedUnits.Single(actor => actor.Unit.Id == 40);
        var owner = world.RetainedUnits.Single(actor => actor.Unit.Id == 6);
        Require(birth.Unit.DeathState?.IsDestroyed == true && birth.Unit.Health == 0 && birth.Unit.Attack == 5 &&
            birth.Unit.Status("trample")?.Stacks == 1 && birth.Unit.Modifiers!.Upgrades.Count == 0 &&
            owner.Unit.DeathState?.IsDestroyed == true && actual.Spawn.Train.Rooms[1].Units.Any(unit =>
                unit.Id == 42 && unit.AssetKey == "MonsterFunguy" && unit.Status("horde")?.Stacks == 2 && unit.Status("trample") == null) &&
            new[] { 16, 28 }.All(id => world.RetainedUnits.Any(actor => actor.Unit.Id == id &&
                actor.Unit.DeathState?.IsDestroyed == true && actor.Unit.Health == 0)),
            "Lost actual birth death, later owner death or the unupgraded post-owner troop.");
        BattleTurnState current = source.GetProperty("Request").GetProperty("Root").Deserialize<BattleTurnState>()!;
        foreach (var row in rows)
        {
            current = BattlePlannerChecks.Replay(current, [new BattleSearchStep(row.GetProperty("Action").Deserialize<PlayCardAction>(),
                row.GetProperty("IsEndTurn").GetBoolean())], out var outcome);
            Require(outcome == RoomOutcome.Exchanged && NativeBranchObservationChecks.Key(current) ==
                NativeBranchObservationChecks.Key(row.GetProperty("Actual").Deserialize<BattleTurnState>()!), "Owned real combat birth prefix differs.");
        }
        string immutable = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            var result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual) &&
                immutable == JsonSerializer.Serialize(before, ModelJson.Options), "Combat birth changed its parent, state or RNG.");
        });
        if (replay.HasValue)
        {
            Native(replay.Value, counterexamplePath);
            NativeBranchObservationChecks.Pair(source, replay.Value);
        }
        Console.WriteLine("TROOP-COMBAT-BIRTH PASS:32 native inputs/continuous state/RNG, raw room/train stages, primary3-to5 attack and committed/pruned cache, " +
            "real birth and owner death, later unupgraded troop, four immutable branches; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
