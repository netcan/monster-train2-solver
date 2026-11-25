using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalDiscountedFlickerChecks
{
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 1 &&
            root.GetProperty("Boundary").GetString() == "OriginalDiscountedFlicker" &&
            root.GetProperty("GameVersion").GetString() == Game231FixtureCatalog.Version &&
            root.GetProperty("GameModuleMvid").GetString() == Game231FixtureCatalog.Module,
            "Expected the original 2.3.1 discounted Flicker receipt.");
        var before = root.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = root.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = root.GetProperty("Action").Deserialize<PlayCardAction>()!;
        int visibleDeck = root.GetProperty("VisibleDeck").GetInt32(), paid = root.GetProperty("PaidCost").GetInt32();
        int copyId = root.GetProperty("CopyId").GetInt32();
        var prior = before.Spawn.Train.Context!;
        var next = actual.Spawn.Train.Context!;
        var source = prior.FindCard(action.CardInstanceId)!;
        var played = next.FindCard(source.InstanceId)!;
        var copy = next.FindCard(copyId)!;
        var discount = source.Temporary.Upgrades.Single(RelicFirstCardCostModel.IsDiscountUpgrade);
        Require(visibleDeck == prior.Resources!.VisibleDeckCount && visibleDeck >= 15 && prior.Cards.Draw.Count >= 15 &&
            before.Spawn.Turn == 1 && source.DataId == "3d1a2e1b-7751-4b2c-a549-77d03db715c9" &&
            discount.Stats.Cost == -(visibleDeck / 15) && discount.Stats.XCost == visibleDeck / 15 &&
            source.Permanent.Upgrades is [{ DataId: CardCopyOnPlayModel.UpgradeId }] &&
            root.GetProperty("OriginalCost").GetInt32() == 0 && paid == 0 && before.Energy - actual.Energy == paid,
            "Expected the existing merchant upgrade and the active native first-spell discount on Flicker.");
        Require(FirstSpellWaiting(prior) && !FirstSpellWaiting(next) && prior.Cards.Hand.Any(card => card.InstanceId == source.InstanceId) &&
            !next.Cards.Hand.Any(card => card.InstanceId == source.InstanceId) && prior.FindCard(copyId) == null &&
            next.Cards.Hand.Any(card => card.InstanceId == copyId) && copy.DataId == source.DataId &&
            !played.Temporary.Upgrades.Any(RelicFirstCardCostModel.IsDiscountUpgrade) &&
            !copy.Temporary.Upgrades.Any(RelicFirstCardCostModel.IsDiscountUpgrade) &&
            CardBranchMaskModel.Resolve(copy).Traits.SequenceEqual(["CardTraitEphemeral"]),
            "The native cast must spend the first-Spell slot and leave one distinct Ephemeral copy without the discount.");

        string parent = Json(before);
        BattleActionResult replay = BattleActionModel.PlayCard(before, action);
        Require(replay.Supported, "Original discounted Flicker was rejected: " + replay.Reason);
        var frontier = BattleActionModel.EnumerateActions(before, source.InstanceId, includeAbilities: false);
        var candidate = frontier.SupportedCandidates.Single(item => Json(item.Action) == Json(action));
        string expected = Json(actual);
        string? difference = ModelJson.Difference(expected, Json(replay.State));
        Require(difference == null && Json(candidate.Result.State) == expected && Json(before) == parent,
            "Discounted Flicker direct or frontier full-state/RNG replay differed: " + difference);

        var alteredDiscount = discount.WithStats(new CardStatModifier(damage: 1, cost: discount.Stats.Cost, xCost: discount.Stats.XCost));
        var altered = source.WithModifiers(source.Permanent, new CardModifiers(source.Temporary.Offsets,
            [alteredDiscount], source.Temporary.PersistentHealth, source.Temporary.ExternalInteractions));
        Require(CardCopyOnPlayModel.Validate(altered)?.Contains("discard-removing", StringComparison.Ordinal) == true,
            "An altered discard-removing copy modifier escaped the payload guard.");
        Console.WriteLine($"ORIGINAL-DISCOUNTED-FLICKER PASS: draw={prior.Cards.Draw.Count} owned={visibleDeck} paid={paid} copy={copyId}, direct and frontier full state/RNG.");
    }

    private static bool FirstSpellWaiting(CombatContext context) => context.Relics!
        .Single(owner => owner.DataId == RelicFirstCardCostModel.DataId).CardTypeCosts!.Single().WaitingForFirstCard == true;
    private static string Json<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
