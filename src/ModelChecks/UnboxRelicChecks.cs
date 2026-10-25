using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class UnboxRelicChecks
{
    private const string Reason = "Unboxing with relic modifiers after copied upgrades requires native calibration.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason);
        Require(rows.Length == 30, "Wrong native unboxing prefix.");
        BattleTurnState before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatContext context = before.Spawn.Train.Context!;
        Require(rows[^1].GetProperty("Action").GetProperty("CardInstanceId").GetInt32() == 19 &&
            context.NextCardId == 32 && context.FindCard(19)!.Permanent.BoxedCards!.Count == 3,
            "Missing three-card native box.");
        CombatRelicState owner = context.Relics!.Single(relic => relic.DataId == RelicExpensiveCardUpgradeModel.DataId);
        Require(RelicModel.Validate([owner]) == null, "Changed expensive-card relic signature.");
        CheckCards(actual.Spawn.Train.Context!);
        PlayCardAction action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 8, _ =>
        {
            BattleActionResult result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported, result.Reason ?? "Unsupported unboxing.");
            Require(NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Parallel unboxing differs from native state/RNG.");
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Unboxing mutated its parent.");
        CopiedCostControls(context);
        if (replay is FixtureValue second)
        {
            NativeBranchObservationChecks.Replay(second, Reason);
            NativeBranchObservationChecks.Pair(source, second);
            CheckCards(second.GetProperty("Rows").EnumerateArray().Last().GetProperty("Actual")
                .Deserialize<BattleTurnState>()!.Spawn.Train.Context!);
        }
        OriginalExpensiveCardUpgradeChecks.Run();
        Console.WriteLine("UNBOX-RELIC PASS:30 native inputs/full state/RNG; copied Corrupt, reduced cost, Consume and Juice retained; original expensive-card relic validated; eight immutable parallel branches; independentPair=" + replay.HasValue);
    }

    // Metamorphic controls compose the captured box template with the separately
    // native-calibrated 2/3 cost threshold. They are not additional native casts.
    private static void CopiedCostControls(CombatContext context)
    {
        BoxedCardState boxed = context.FindCard(19)!.Permanent.BoxedCards![2];
        CardCreationRule definition = boxed.Creation;
        var creation = new CardCreationRule(definition.DataId, CardModifiers.Empty(), null, [], maskDescriptor: definition.MaskDescriptor);
        foreach (int costOffset in new[] { 1, 2 })
        {
            var permanent = new CardModifiers(new CardStatModifier(cost: costOffset), boxed.Permanent.Upgrades, 0, []);
            var temporary = new CardModifiers(new CardStatModifier(damage: 2), [], 0, []);
            var template = new CardInstanceState(0, definition.DataId, permanent, temporary, 0, 0, 0, [],
                maskDescriptor: definition.MaskDescriptor);
            CardGenerationResult result = CardGenerationModel.ApplyCachedCopy(context, creation, template);
            Require(result.Supported, result.UnsupportedReason ?? "Copied-cost control unsupported.");
            CardInstanceState card = result.Context!.FindCard(context.NextCardId)!;
            Require(card.Permanent.Offsets.Cost == costOffset && card.Temporary.Offsets.Damage == 2 &&
                RelicExpensiveCardUpgradeModel.HasUpgrade(card) == (costOffset == 2),
                "Relic eligibility did not use copied cost or temporary modifiers were reset.");
        }
    }

    private static void CheckCards(CombatContext context)
    {
        Require(context.NextCardId == 35 && context.Cards.Hand.Take(3).Select(card => card.InstanceId).SequenceEqual([34, 33, 32]),
            "Wrong shuffled card identities/hand order.");
        Require(context.FindCard(32)!.DataId == "c8b39244-83c1-4eb3-b474-8898226d4280" &&
            context.FindCard(33)!.DataId == "5ae81798-157e-4fcb-b4e6-822ff40843b0" &&
            context.FindCard(34)!.DataId == "c8b39244-83c1-4eb3-b474-8898226d4280", "Wrong boxed templates.");
        Require(context.FindCard(32)!.Permanent.Upgrades.Select(upgrade => upgrade.AssetKey).SequenceEqual(["TraitCorrupt"]) &&
            context.FindCard(33)!.Permanent.Upgrades.Count == 0 &&
            context.FindCard(34)!.Permanent.Upgrades.Select(upgrade => upgrade.AssetKey)
                .SequenceEqual(["TraitCorrupt", "ReduceCostAddConsume", "TraitJuice"]), "Copied upgrades differ.");
        Require(Enumerable.Range(32, 3).All(id => context.FindCard(id)!.Temporary.Upgrades.Count == 0),
            "Below-threshold boxed cards unexpectedly gained Permafrost.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
