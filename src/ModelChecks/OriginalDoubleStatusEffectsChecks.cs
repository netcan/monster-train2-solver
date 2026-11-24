using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalDoubleStatusEffectsChecks
{
    internal static void Native(FixtureValue witness)
    {
        FixtureValue owner = witness.GetProperty("Owner");
        Require(witness.GetProperty("Boundary").GetString() == "OriginalDoubleStatusEffects" &&
            witness.GetProperty("GameModuleMvid").GetString() == Game231FixtureCatalog.Module &&
            owner.GetProperty("Id").GetString() == RelicStatusStackModel.DoubleStatusEffectsId &&
            owner.GetProperty("Percent").GetInt32() == 200 && owner.GetProperty("Conditions").GetInt32() == 0 &&
            owner.GetProperty("CanApplyInPreviewMode").GetBoolean(),
            "Expected the pinned original double-status mutator.");
        var before = witness.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var action = witness.GetProperty("Action").Deserialize<PlayCardAction>()!;
        CombatContext context = before.Spawn.Train.Context!;
        Require(context.FindCard(action.CardInstanceId)?.DataId == "ec0980cf-9f88-4d38-9704-b89042f2f625",
            "Expected an original SpikePotion cast.");

        foreach (FixtureValue row in witness.GetProperty("QueryControls").EnumerateArray())
        {
            int actual = row.GetProperty("Actual").GetInt32();
            int predicted = RelicStatusStackModel.GetStacks(context.Relics, row.GetProperty("Status").GetString()!,
                row.GetProperty("Incoming").GetInt32(), row.GetProperty("HasRecipient").GetBoolean() ? CombatTeam.Player : null,
                hasStatus: false, fromPermanentUpgrade: row.GetProperty("Permanent").GetBoolean(),
                deferred: row.GetProperty("Deferred").GetBoolean(), fromEffectType: row.GetProperty("EffectType").GetString(),
                includeAdders: row.GetProperty("IncludeAdders").GetBoolean());
            Require(predicted == actual, "Original status multiplier query differs.");
        }
        FixtureValue[] queries = witness.GetProperty("StackQueries").EnumerateArray().ToArray();
        FixtureValue application = witness.GetProperty("Applications").EnumerateArray().Single();
        Require(queries.Length == 2 && queries.All(row => row.GetProperty("Incoming").GetInt32() == 10 &&
                row.GetProperty("Actual").GetInt32() == 20) &&
            queries[0].GetProperty("Order").GetInt32() < application.GetProperty("Order").GetInt32() &&
            application.GetProperty("Order").GetInt32() < queries[1].GetProperty("Order").GetInt32() &&
            queries[1].GetProperty("Order").GetInt32() < application.GetProperty("AfterOrder").GetInt32() &&
            application.GetProperty("AllowModification").GetBoolean() &&
            application.GetProperty("BeforeStacks").GetInt32() == 0 && application.GetProperty("AfterStacks").GetInt32() == 20,
            "Expected the original target test, paid application, recipient modifier and 20 applied Spikes.");
        BattleActionResult played = BattleActionModel.PlayCard(before, action);
        Require(played.Supported && played.State!.Energy == before.Energy - 1,
            played.Reason ?? "The original paid spell must consume one Ember.");
        RecordedBattleChecks.RecordedBoundary(played.State!, witness.GetProperty("After"), terminal: false,
            "Original double-status paid successor", trackPreviewPosition: true);
        FixtureValue[] removals = witness.GetProperty("RemovalControls").EnumerateArray().ToArray();
        Require(removals.Length == 4, "Expected four original removal controls.");
        foreach (FixtureValue row in removals)
        {
            RoomCombatState room = row.GetProperty("Before").Deserialize<RoomCombatState>()!;
            RoomCombatState actual = row.GetProperty("After").Deserialize<RoomCombatState>()!;
            int targetId = row.GetProperty("ActorId").GetInt32();
            string status = row.GetProperty("Status").GetString()!;
            var removal = StatusRemovalModel.Remove(room, targetId, status, row.GetProperty("Requested").GetInt32(),
                fromEffectType: row.GetProperty("EffectType").GetString(),
                allowStackModifiers: row.GetProperty("AllowModification").GetBoolean(),
                fromCardUpgrade: row.GetProperty("FromCardUpgrade").GetBoolean());
            int Expected(RoomCombatState floor) => floor.Units.Single(unit => unit.Id == targetId).Status(status)?.Stacks ?? 0;
            Require(removal.Supported && Expected(removal.State!) == Expected(actual) &&
                Expected(actual) == row.GetProperty("AfterStacks").GetInt32() && Expected(room) == row.GetProperty("BeforeStacks").GetInt32(),
                removal.UnsupportedReason ?? $"Original removal {row.GetProperty("Label").GetString()} differs.");
        }
        Console.WriteLine("NATIVE-DOUBLE-STATUS PASS: ordered queries, paid 10 to 20 Spikes with complete successor and RNG, four removal controls.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
