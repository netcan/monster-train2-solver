using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TerminalCopyOnPlayChecks
{
    internal static void Boundary(FixtureValue root)
    {
        Require(root.GetProperty("CopyOnPlayTerminal").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing settled native terminal copy boundary.");
        FixtureValue row = root.GetProperty("Actions").EnumerateArray().Last();
        BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
        CardInstanceState card = before.Spawn.Train.Context!.FindCard(action.CardInstanceId)!;
        Require(card.DataId == "3d1a2e1b-7751-4b2c-a549-77d03db715c9" && CardCopyOnPlayModel.Present(card) &&
            card.MaskDescriptor!.TemporaryTraits.Any(trait => trait.RuntimeType == "CardTraitFreeze"), "Missing original frozen copy carrier.");
        int nextId = before.Spawn.Train.Context.NextCardId;
        CombatContext context = actual.Spawn.Train.Context!;
        CardInstanceState copy = context.FindCard(nextId)!;
        bool consume = root.GetProperty("CopyOnPlayTerminalConsume").GetBoolean();
        Require(CardBranchMaskModel.Resolve(card).Traits.Contains("CardTraitExhaustState") == consume &&
            CardBranchMaskModel.Resolve(copy).Traits.Contains("CardTraitExhaustState") == consume,
            "Terminal original/copy Consume control differs.");
        Require(context.NextCardId == nextId + 1 && context.Cards.Hand.Count == 1 && context.Cards.Hand[0].InstanceId == nextId &&
            copy.DataId == card.DataId && !CardCopyOnPlayModel.Present(copy) &&
            copy.Temporary.Upgrades.Any(upgrade => upgrade.AssetKey == "CopyOnPlayTraitUpgrade") &&
            CardBranchMaskModel.Resolve(copy).Traits.Contains("CardTraitEphemeral") &&
            !CardBranchMaskModel.Resolve(copy).Traits.Contains("CardTraitFreeze") &&
            context.FindCard(card.InstanceId)!.MaskDescriptor!.TemporaryTraits.All(trait => trait.RuntimeType != "CardTraitFreeze"),
            "Native terminal copy identity, purge upgrade, exclusion or prior freeze removal differs.");
        Require(consume ? context.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Count(token => token.InstanceId == card.InstanceId) == 1 &&
            !context.Cards.Discard.Any(token => token.InstanceId == card.InstanceId) :
            context.Cards.Discard.Count(token => token.InstanceId == card.InstanceId) == 1, "Terminal original discard/Consume routing differs.");
        foreach (FixtureValue turn in root.GetProperty("Turns").EnumerateArray())
        {
            CombatContext retained = turn.GetProperty("Actual").Deserialize<BattleTurnState>()!.Spawn.Train.Context!;
            Require(retained.Cards.Hand.Any(token => token.InstanceId == card.InstanceId) &&
                retained.FindCard(card.InstanceId)!.MaskDescriptor!.TemporaryTraits.Any(trait => trait.RuntimeType == "CardTraitFreeze"),
                "The unplayed copy carrier lost its original Freeze before the native boss was born.");
        }
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        void Replay()
        {
            BattleActionResult result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && result.Outcome == RoomOutcome.BattleWon, result.Reason ?? "Terminal copy did not win.");
            string? difference = ModelJson.Difference(BattleTurnChecks.Comparable(result.State!, true), BattleTurnChecks.Comparable(actual, true));
            Require(difference == null, "Complete native terminal copy state/RNG differs: " + difference);
        }
        Replay(); Parallel.For(0, 16, _ => Replay());
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Terminal copy branch mutated its parent.");
        Console.WriteLine($"TERMINAL-COPY-BOUNDARY PASS consume={consume} originalFrozenFlicker=true originalCopyUpgrade=true postVictoryHandCopy=true completeStateAndRng=true immutableBranches=16");
    }
    internal static void Native(FixtureValue root)
    {
        Boundary(root);
        foreach (string counter in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(counter).GetInt32() == 0, "Nonzero terminal copy " + counter);
        RecordedBattleChecks.Native(root);
        EnemySpawningChecks.Native(root);
        Require(root.GetProperty("Turns").GetArrayLength() >= 4, "Missing retained frozen spell turn boundaries.");
        Console.WriteLine("TERMINAL-COPY-ON-PLAY PASS completeNativeBattle=true");
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
