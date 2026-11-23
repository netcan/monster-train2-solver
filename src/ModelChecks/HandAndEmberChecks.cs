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

    private static void Replay(FixtureValue root, bool combined)
    {
        Require(root.GetProperty("Boundary").GetString() == "OriginalHandAndEmberNewRun" &&
            root.GetProperty("GameVersion").GetString() == Game231FixtureCatalog.Version &&
            root.GetProperty("GameModuleMvid").GetString() == Game231FixtureCatalog.Module &&
            root.GetProperty("RunType").GetString() == (combined ? "Custom" : "Class") &&
            root.GetProperty("Mode").GetString() == (combined ? "Combined" : "Stock"), "Missing original paired start.");

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
        BattleTurnResult last = BattleTurnModel.EndTurn(state);
        Require(last.State != null && last.Outcome == RoomOutcome.Exchanged, "Ordinary EndTurn: " + last.UnsupportedReason);
        RecordedBattleChecks.RecordedBoundary(last.State!, Decision("SecondDraw"), false, "Second draw");

        static int[] Hand(BattleTurnState state) =>
            state.Spawn.Train.Context!.Cards.Hand.Select(card => card.InstanceId).ToArray();
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
