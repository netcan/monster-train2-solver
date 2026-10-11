using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class EndlessReturnChecks
{
    internal static void Run()
    {
        Check(handSize: 9);
        Check(handSize: 10);
        Console.WriteLine("ENDLESS-RETURN-CHECKS PASS: unconditional draw-pile return, pile cleanup, death statistics and immutable branches.");
    }

    internal static PlayCardAction? Choose(BattleTurnState source, int targetCardId)
    {
        var plays = BattleActionModel.EnumerateSupportedPlays(source);
        CardInstanceState? card = source.Spawn.Train.Context!.CardInstances?.FirstOrDefault(item => item.InstanceId == targetCardId)
            ?? source.Spawn.Train.Context.CardRegistry?.FirstOrDefault(item => item.InstanceId == targetCardId);
        if (card?.PlayCount == 0)
        {
            PlayCardAction? target = plays.FirstOrDefault(action => action.CardInstanceId == targetCardId && action.RoomIndex == 0);
            if (target != null) return target;
        }
        PlayCardAction? fallback = BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
        return fallback?.CardInstanceId == targetCardId ? plays.FirstOrDefault(action => action.CardInstanceId != targetCardId) : fallback;
    }

    internal static void Native(FixtureValue fixture)
    {
        if (!fixture.TryGetProperty("ModifierScenario", out FixtureValue scenario) || scenario.GetString() != "original-endless-return") return;
        int targetCardId = fixture.GetProperty("EndlessReturnCardId").GetInt32();
        Require(fixture.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            fixture.GetProperty("NativeWon").GetBoolean() && fixture.GetProperty("CaptureFailures").GetInt32() == 0 &&
            fixture.GetProperty("Mismatches").GetInt32() == 0 && fixture.GetProperty("Unsupported").GetInt32() == 0 &&
            fixture.GetProperty("Pending").GetInt32() == 0,
            "The Endless return archive is not a complete native battle with a clean differential.");

        FixtureValue[] actions = fixture.GetProperty("Actions").EnumerateArray().ToArray();
        Require(actions.Count(entry => entry.GetProperty("Action").GetProperty("CardInstanceId").GetInt32() == targetCardId) == 1,
            "The authored Endless carrier was not played exactly once.");
        var deaths = new HashSet<int>();
        foreach (FixtureValue stage in fixture.GetProperty("Stages").EnumerateArray())
        {
            RoomCombatState before = stage.GetProperty("Before").Deserialize<RoomCombatState>()!;
            CombatUnit? unit = before.Units.SingleOrDefault(item => item.SpawnerCardId == targetCardId &&
                item.Statuses.Any(status => status.Id == "endless" && status.Stacks > 0));
            if (unit == null) continue;
            RoomCombatState actual = stage.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            if (actual.Units.Any(item => item.Id == unit.Id)) continue;
            CombatContext actualContext = actual.Context!;
            CardInstanceState? beforeCard = before.Context!.CardInstances?.SingleOrDefault(item => item.InstanceId == targetCardId);
            CardInstanceState? afterCard = actualContext.CardInstances?.SingleOrDefault(item => item.InstanceId == targetCardId);
            Require(actualContext.Cards.Draw.Any(item => item.InstanceId == targetCardId) &&
                !actualContext.Cards.Hand.Any(item => item.InstanceId == targetCardId) &&
                actualContext.OtherPiles!.Where(pile => pile.Name is "Standby" or "Exhausted" or "Eaten")
                    .All(pile => pile.Cards.All(item => item.InstanceId != targetCardId)) &&
                actualContext.Statistics!.Value(targetCardId, "TimesExhausted") ==
                    before.Context.Statistics!.Value(targetCardId, "TimesExhausted") &&
                beforeCard?.Temporary.Upgrades.Any(upgrade => upgrade.DataId == "c62d9641-8948-4c00-b67d-60f0e728c1c1") == true &&
                afterCard?.Temporary.Upgrades.Any(upgrade => upgrade.DataId == "c62d9641-8948-4c00-b67d-60f0e728c1c1") == true,
                "Native Endless death did not return its spawner to draw while preserving the temporary relic upgrade and exhaustion statistics.");
            deaths.Add(unit.Id);
        }
        Require(deaths.Count == 1, "The authored Endless carrier had no unique native death witness.");
        Console.WriteLine("NATIVE-ENDLESS-RETURN-CHECKS PASS: original More Rares carrier returned to draw with temporary upgrade retained and no exhaustion.");
    }

    private static void Check(int handSize)
    {
        const int cardId = 7;
        const int unitId = 11;
        UnityRng rng = UnityRng.Seed(42);
        CardToken spawner = new(cardId, "endless-unit");
        CardToken[] hand = Enumerable.Range(20, handSize).Select(id => new CardToken(id, "hand-card")).ToArray();
        CardToken[] draw = [new CardToken(30, "draw-card")];
        CardPileState standby = new("Standby", [spawner], [cardId], [], unitConditions: [new(cardId, unitId)]);
        CombatContext context = new(new CardCycleState(hand, draw, [], rng, 0, []), rng, 0, 31, 10,
            statistics: BattleStatistics.Empty().TrackCards([cardId]),
            otherPiles: [standby, new("Exhausted", []), new("Eaten", [])]);
        CombatUnit enemy = new(1, "enemy", CombatTeam.Enemy, 1, 20, 20, true, false, false, []);
        CombatUnit endless = new(unitId, "endless-unit", CombatTeam.Player, 0, 1, 1, false, false, false,
            [new CombatStatus("endless", 1)], spawnerCardId: cardId, size: 1);
        RoomCombatState root = new(0, false, [enemy, endless], [], context);
        string parent = JsonSerializer.Serialize(root);

        RoomCombatResult result = RoomCombatModel.Exchange(root);
        Require(result.Supported, result.UnsupportedReason ?? "Endless death routing was unsupported.");
        CombatContext actual = result.State!.Context!;
        IReadOnlyList<CardPileState> piles = actual.OtherPiles!;
        Require(result.State.Units.All(unit => unit.Id != unitId), "The killed Endless unit remained in the room.");
        Require(piles.Single(pile => pile.Name == "Standby").Cards.All(card => card.InstanceId != cardId) &&
            piles.Single(pile => pile.Name == "Exhausted").Cards.All(card => card.InstanceId != cardId) &&
            piles.Single(pile => pile.Name == "Eaten").Cards.All(card => card.InstanceId != cardId),
            "The Endless spawner remained in a unit-routing pile.");
        Require(actual.Statistics!.Value(cardId, "TimesExhausted") == 0,
            "An Endless death incremented TimesExhausted.");
        Require(actual.Cards.Hand.Select(card => card.InstanceId).SequenceEqual(hand.Select(card => card.InstanceId)) &&
            actual.Cards.Draw.Select(card => card.InstanceId).SequenceEqual([30, cardId]),
            "An Endless spawner did not append to the draw pile regardless of available hand space.");
        Require(JsonSerializer.Serialize(root) == parent, "Endless routing mutated its parent branch.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
