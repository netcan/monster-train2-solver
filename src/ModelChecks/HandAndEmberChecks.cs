using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class HandAndEmberChecks
{
    internal static void Pair(FixtureValue combined, FixtureValue stock)
    {
        foreach (string field in new[] { "Seed", "ClassId", "SubclassId", "Scenario", "BaseEnergy", "BaseDraw", "MaxEnergy", "PaidCard" })
            Require(combined.GetProperty(field).ContentEquals(stock.GetProperty(field)), "Changed native control " + field + ".");
        Replay(combined, combined: true);
        Replay(stock, combined: false);
        Console.WriteLine("NATIVE-HAND-AND-EMBER-REPLAY PASS: two original starts, two paid actions, four EndTurns and full native successors and RNG.");
    }

    internal static void Ephemeral(FixtureValue root)
    {
        Replay(root, combined: true, ephemeral: true);
        Console.WriteLine("NATIVE-HAND-EPHEMERAL-REPLAY PASS: original HandDoesntDiscard, native buffer-only Ephemeral cleanup, ordinary retention and complete EndTurn successor.");
    }

    private static void Replay(FixtureValue root, bool combined, bool ephemeral = false)
    {
        Require(root.GetProperty("Boundary").GetString() == "OriginalHandAndEmberNewRun" &&
            root.GetProperty("GameVersion").GetString() == Game231FixtureCatalog.Version &&
            root.GetProperty("GameModuleMvid").GetString() == Game231FixtureCatalog.Module &&
            root.GetProperty("RunType").GetString() == (combined ? "Custom" : "Class") &&
            root.GetProperty("Mode").GetString() == (ephemeral ? "Ephemeral" : combined ? "Combined" : "Stock"), "Missing original paired start.");

        FixtureValue Decision(string name) => root.GetProperty(name).GetProperty("Decision");
        var deployment = Decision("Deployment").Deserialize<BattleTurnState>()!;
        var after = Decision("AfterPlay").Deserialize<BattleTurnState>()!;
        var second = Decision("SecondDraw").Deserialize<BattleTurnState>()!;
        var owners = deployment.Spawn.Train.Context!.Relics!;
        Require(owners.Any(RelicHandAndEmberModel.OriginalHand) == combined &&
            RelicHandAndEmberModel.SupportsEnergyOwner(owners) == combined, "Unexpected native owners or missing ConserveEmber acquisition.");

        var input = root.GetProperty("Action").Deserialize<PlayCardAction>()!;
        int paid = root.GetProperty("Paid").GetInt32();
        int baseEnergy = root.GetProperty("BaseEnergy").GetInt32();
        int draw = root.GetProperty("BaseDraw").GetInt32() - (combined ? 2 : 0);
        int[] retained = Hand(after), finalHand = Hand(second);
        Require(paid > 0 && finalHand.Length == draw + (combined ? retained.Length : 0) &&
            (combined ? finalHand.Skip(draw).SequenceEqual(retained) : !finalHand.Intersect(retained).Any()) &&
            second.Energy == (combined ? after.Energy : baseEnergy), "Native retained cards, draw count or carry differs.");
        string[] discardPhases = root.GetProperty("Phases").EnumerateArray().Where(row =>
            row.GetProperty("Kind").GetString() == "DiscardHand").Select(row => row.GetProperty("Turn").GetInt32().ToString()).ToArray();
        Require(discardPhases.SequenceEqual(combined ? ["0"] : ["0", "1"]), "Unexpected native discard timing.");

        BattleTurnResult first = BattleTurnModel.EndTurn(deployment);
        Require(first.State != null && first.Outcome == RoomOutcome.Exchanged, "Initial EndTurn: " + first.UnsupportedReason);
        BattleTurnState state = RecordedBattleChecks.RecordedBoundary(first.State!, Decision("FirstDraw"), false, "First draw");
        state = RecordedBattleChecks.RecordedBoundary(state, Decision("BeforePlay"), false, "Before paid action");
        Require(state.Energy - after.Energy == paid && Hand(state).Contains(input.CardInstanceId), "Changed original paid input or price.");
        BattleActionResult played = BattleActionModel.PlayCard(state, input);
        Require(played.State != null && played.Outcome == RoomOutcome.Exchanged, "Paid action: " + played.Reason);
        state = RecordedBattleChecks.RecordedBoundary(played.State!, Decision("AfterPlay"), false, "After paid action");
        if (ephemeral) state = DonorBoundary(root, state, second);
        BattleTurnResult last = BattleTurnModel.EndTurn(state);
        Require(last.State != null && last.Outcome == RoomOutcome.Exchanged, "Ordinary EndTurn: " + last.UnsupportedReason);
        RecordedBattleChecks.RecordedBoundary(last.State!, Decision("SecondDraw"), false, "Second draw");

        static int[] Hand(BattleTurnState state) =>
            state.Spawn.Train.Context!.Cards.Hand.Select(card => card.InstanceId).ToArray();
    }

    private static BattleTurnState DonorBoundary(FixtureValue root, BattleTurnState afterPlay, BattleTurnState second)
    {
        FixtureValue movement = root.GetProperty("Cleanup");
        int cardId = movement.GetProperty("CardId").GetInt32();
        foreach (string field in new[] { "Forced", "HandBefore", "BufferAfter" })
            Require(movement.GetProperty(field).GetBoolean(), "Missing native cleanup flag " + field + ".");
        foreach (string field in new[] { "WasPlayed", "HandDiscarded", "BufferBefore", "HandAfter", "DiscardAfter", "PurgedAfter" })
            Require(!movement.GetProperty(field).GetBoolean(), "Unexpected native cleanup flag " + field + ".");
        Require(root.GetProperty("DonorDiscardCalls").GetInt32() == 0 && root.GetProperty("DonorPurgeCalls").GetInt32() == 0,
            "Native removal entered an ordinary discard or purge callback.");

        var boundary = root.GetProperty("BeforeCleanup").GetProperty("Decision").Deserialize<BattleTurnState>()!;
        CombatContext prior = afterPlay.Spawn.Train.Context!, before = boundary.Spawn.Train.Context!, after = second.Spawn.Train.Context!;
        Require(RelicHandAndEmberModel.OriginalEphemeral(before.FindCard(cardId)) &&
            before.Cards.Hand.Single(card => card.InstanceId == cardId).DataId == RelicHandAndEmberModel.EphemeralCardId &&
            Tokens(before.Cards.Hand.Where(card => card.InstanceId != cardId)).SequenceEqual(Tokens(prior.Cards.Hand)) &&
            Tokens(before.Cards.Draw).SequenceEqual(Tokens(prior.Cards.Draw)) &&
            Tokens(before.Cards.Discard).SequenceEqual(Tokens(prior.Cards.Discard)) &&
            before.Statistics!.DeckCards!.SequenceEqual(prior.Statistics!.DeckCards!) && !before.Statistics.DeckCards!.Contains(cardId) &&
            boundary.Energy == afterPlay.Energy && prior.NextCardId == cardId && before.NextCardId == cardId + 1,
            "Expected one combat-only donor insertion and the original hand, deck and paid resource state.");
        Require(Pile(second, "DiscardBuffer").SequenceEqual(Pile(boundary, "DiscardBuffer").Append(cardId)) &&
            Pile(second, "Purged").SequenceEqual(Pile(boundary, "Purged")) &&
            !after.Cards.Hand.Concat(after.Cards.Draw).Concat(after.Cards.Discard).Any(card => card.InstanceId == cardId) &&
            after.Statistics!.Value(cardId, "TimesDiscarded", "ThisBattle") == before.Statistics.Value(cardId, "TimesDiscarded", "ThisBattle"),
            "Native donor did not stay exclusively in the discard buffer, or recorded a full discard.");

        return boundary;

        static IEnumerable<int> Pile(BattleTurnState state, string name) =>
            state.OtherPiles.Single(pile => pile.Name == name).Cards.Select(card => card.InstanceId);

        static IEnumerable<(int InstanceId, string DataId)> Tokens(IEnumerable<CardToken> cards) =>
            cards.Select(card => (card.InstanceId, card.DataId));
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
