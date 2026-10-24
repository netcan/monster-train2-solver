using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RelentlessPreviewCooldownChecks
{
    internal static void Native(FixtureValue fixture)
    {
        RandomCampaignChecks.Input(fixture, 1862997127, 0, 2, "Pyreborne", "Banished");
        RollingAdviceChecks.ObservedSuccessors(fixture);
        TrainCombatChecks.Native(fixture);
        FixtureValue[] rows = fixture.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        Require(rows.Length == 44 && fixture.GetProperty("NativeWon").ValueKind == FixtureKind.Null,
            "This calibration must remain an incomplete 44-input native observation.");
        BattleTurnState root = rows[^1].GetProperty("Root").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(root.Spawn.Turn == 7 && actual.Spawn.Turn == 8 &&
            root.Spawn.Train.Context!.FindCard(4)!.PlayedRoomUnitIds!.SequenceEqual([2, 24, 48]) &&
            actual.Spawn.Train.Context!.FindCard(4)!.PlayedRoomUnitIds!.SequenceEqual([2, 8, 24]) &&
            actual.Spawn.Train.Context.FindCard(4)!.RawPlayedRoomUnitIds!.SequenceEqual([2, 8, 24]) &&
            actual.Spawn.Train.Context.EnergyState!.Phase == "MonsterTurn",
            "The native phase, ascending boss or surviving weak references changed.");
        Require(fixture.GetProperty("PreviewAttackDiagnostic").EnumerateArray().Any(sample =>
            sample.TryGetProperty("Event", out FixtureValue kind) && kind.GetString() == "RoomCache" &&
            sample.GetProperty("Turn").GetInt32() == 8 && sample.GetProperty("Preview").GetBoolean() &&
            sample.GetProperty("Actors").EnumerateArray().Select(actor => actor.GetProperty("Id").GetInt32())
                .Order().SequenceEqual([2, 8, 24])), "Missing the directly observed native preview cache callback.");

        // Only vary the native combat phase. The same visible relentless room
        // must tick cooldown during the player preview and preserve it in combat.
        string original = BattleTurnChecks.Comparable(actual);
        CombatContext context = actual.Spawn.Train.Context;
        CardInstanceState card = context.FindCard(4)!;
        context = context.WithCard(card.WithRoomCacheState([2, 24], [2, 24]));
        foreach (string phase in new[] { "MonsterTurn", "Combat" })
        {
            TrainCombatState train = CardSpellModel.WithContext(actual.Spawn.Train, EnergyModel.SetPhase(context, phase));
            TrainCombatResult preview = BattlePreviewModel.Refresh(train, actual.PlayRules);
            Require(preview.Supported, preview.UnsupportedReason ?? "Phase preview rejected.");
            Require(preview.State!.Context!.FindCard(4)!.PlayedRoomUnitIds!.Contains(8) == (phase == "MonsterTurn"),
                "Cooldown availability callbacks lost their native phase boundary.");
        }
        Require(BattleTurnChecks.Comparable(actual) == original, "Preview control mutated the native parent.");
        Console.WriteLine("RELENTLESS-PREVIEW-COOLDOWN PASS: 44 native successors, direct preview callback, player/combat phase controls and immutable parent.");
    }
    private static void Require(bool value, string message)
    { if (!value) throw new InvalidOperationException(message); }
}
