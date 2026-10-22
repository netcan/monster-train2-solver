using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TerminalPurgeCopyChecks
{
    internal static void Boundary(FixtureValue root)
    {
        Require(root.GetProperty("CopyOnPlayTerminalPurge").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing settled native terminal purge-copy boundary.");
        FixtureValue[] rows = root.GetProperty("Actions").EnumerateArray().ToArray();
        FixtureValue row = rows.Last();
        BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
        CombatContext prior = before.Spawn.Train.Context!, context = actual.Spawn.Train.Context!;
        CardInstanceState card = prior.FindCard(action.CardInstanceId)!;
        bool consume = root.GetProperty("CopyOnPlayTerminalConsume").GetBoolean();
        Require(card.DataId == "3d1a2e1b-7751-4b2c-a549-77d03db715c9" && CardCopyOnPlayModel.SupportsTerminalPurge(card) &&
            CardBranchMaskModel.Resolve(card).Traits.Contains("CardTraitExhaustState") == consume,
            "Missing original temporary purge-copy/Consume control.");
        CardInstanceState extraCallback = card.WithMaskDescriptor(card.MaskDescriptor!.WithBaseTraits(
            new[] { new CardTraitValue("CardTraitSelfPurge", "CardTraitSelfPurge", 0, 0, false, 0) }));
        Require(!CardCopyOnPlayModel.SupportsTerminalPurge(card.WithModifiers(card.Permanent, CardModifiers.Empty())) &&
            !CardCopyOnPlayModel.SupportsTerminalPurge(extraCallback),
            "Terminal purge protection accepted a missing original upgrade or an extra discard callback.");
        BattleTurnState created = rows[^2].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(created.Spawn.Train.Context!.Cards.Hand.Any(token => token.InstanceId == card.InstanceId) &&
            created.Spawn.Train.Context.NextCardId == card.InstanceId + 1 &&
            rows[^2].GetProperty("ActualOutcome").Deserialize<RoomOutcome>() == RoomOutcome.Exchanged &&
            before.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit => unit.IsBoss == true && unit.Health > 0),
            "The original must create the played copy before a separate nonterminal/terminal cast.");
        Require(context.NextCardId == prior.NextCardId && context.Cards.Hand.Count == 0 && context.Cards.Discard.Count == 0 &&
            context.OtherPiles!.Single(pile => pile.Name == "Purged").Cards.Count(token => token.InstanceId == card.InstanceId) == 2 &&
            context.OtherPiles.Where(pile => pile.Name is "Standby" or "Exhausted").All(pile =>
                !pile.Cards.Any(token => token.InstanceId == card.InstanceId)),
            "Terminal Ephemeral must purge twice without recursive creation or retained Consume routing.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        void Replay()
        {
            BattleActionResult result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && result.Outcome == RoomOutcome.BattleWon, result.Reason ?? "Purge copy did not win.");
            string? difference = ModelJson.Difference(BattleTurnChecks.Comparable(result.State!, true), BattleTurnChecks.Comparable(actual, true));
            Require(difference == null, "Complete native terminal purge-copy state/RNG differs: " + difference);
        }
        Replay(); Parallel.For(0, 16, _ => Replay());
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Terminal purge-copy branch mutated its parent.");
        Console.WriteLine($"TERMINAL-PURGE-COPY-BOUNDARY PASS consume={consume} generatedThenPlayed=true duplicatePurge=true completeStateAndRng=true immutableBranches=16");
    }
    internal static void Native(FixtureValue root)
    {
        Boundary(root);
        foreach (string counter in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(counter).GetInt32() == 0, "Nonzero terminal purge-copy " + counter);
        RecordedBattleChecks.Native(root);
        EnemySpawningChecks.Native(root);
        Require(root.GetProperty("Turns").GetArrayLength() >= 4, "Missing retained original spell turn boundaries.");
        Console.WriteLine("TERMINAL-PURGE-COPY PASS completeNativeBattle=true");
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
