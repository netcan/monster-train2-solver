using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalPerkeoChecks
{
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 245 && root.GetProperty("OriginalPerkeoPrepared").GetBoolean() &&
            root.GetProperty("ModifierScenario").GetString() == "original-perkeo" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Incomplete original Perkeo witness.");
        foreach (string field in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending", "OriginalPerkeoCaptureFailures",
            "OriginalPerkeoMismatches", "OriginalPerkeoUnsupported", "OriginalPerkeoPending" })
            Require(root.GetProperty(field).GetInt32() == 0, "Nonzero " + field);
        FixtureValue[] generation = root.GetProperty("RelicStartBattleGeneration").EnumerateArray().ToArray();
        Require(generation.Length == 1, "Expected one natural Perkeo generation.");
        FixtureValue row = generation[0];
        CombatContext before = row.GetProperty("Before").Deserialize<CombatContext>()!, actual = row.GetProperty("Actual").Deserialize<CombatContext>()!;
        Require(row.GetProperty("OwnerId").GetString() == RelicPerkeoModel.DataId && row.GetProperty("EffectIndex").GetInt32() == 0 &&
            row.GetProperty("Completed").GetBoolean(), "Wrong native generation callback.");
        CombatRelicState owner = before.Relics!.Single(item => item.DataId == RelicPerkeoModel.DataId);
        RelicResourceEffect effect = owner.ResourceEffects!.Single();
        Require(RelicModel.Validate([owner]) == null && RelicPerkeoModel.Validate(owner, effect) == null, "Changed original Perkeo owner.");
        CardToken[] added = row.GetProperty("GeneratedCards").Deserialize<CardToken[]>()!;
        Require(added.Length == 1 && added[0].DataId == RelicPerkeoModel.SourceCardId &&
            actual.Cards.Draw.Any(card => card.InstanceId == added[0].InstanceId) &&
            actual.Resources!.VisibleDeckCount == before.Resources!.VisibleDeckCount + 1 &&
            actual.Statistics!.DeckCards!.Contains(added[0].InstanceId) &&
            actual.FindCard(added[0].InstanceId)!.Permanent.Upgrades.Any(RelicPerkeoModel.IsConversionUpgrade) &&
            !actual.FindCard(added[0].InstanceId)!.Temporary.Upgrades.Any(upgrade => upgrade.DataId == RelicPerkeoModel.UpgradeId),
            "Perkeo did not permanently add its upgraded original card to the random draw pile.");
        Branches(before, () =>
        {
            CardGenerationResult result = RelicCardGenerationModel.ApplyStartOfBattle(before, owner.DataId, 0, 0);
            Require(result.Supported, result.UnsupportedReason ?? "Unsupported Perkeo generation.");
            Equal(result.Context, actual, "generation complete context"); Equal(result.AddedCards, added, "allocated card");
        });
        Require(!RelicCardGenerationModel.ApplyStartOfBattle(before, "changed-owner", 0, 0).Supported &&
            !RelicCardGenerationModel.ApplyStartOfBattle(before, owner.DataId, 1, 0).Supported &&
            !RelicCardGenerationModel.ApplyStartOfBattle(before, owner.DataId, 0, 1).Supported,
            "Perkeo owner/effect/pool guards are missing.");

        FixtureValue[] conversions = root.GetProperty("RelicPerkeoConversions").EnumerateArray().ToArray();
        Require(conversions.Length == 1, "Expected one actual unplayed conversion.");
        foreach (FixtureValue conversion in conversions)
        {
            CombatContext source = conversion.GetProperty("Before").Deserialize<CombatContext>()!, target = conversion.GetProperty("Actual").Deserialize<CombatContext>()!;
            int cardId = conversion.GetProperty("CardId").GetInt32();
            CardCreationRule creation = conversion.GetProperty("Creation").Deserialize<CardCreationRule>()!;
            Require(cardId == added[0].InstanceId && conversion.GetProperty("Count").GetInt32() == 1 &&
                conversion.GetProperty("Destination").GetString() == "HandPile" && conversion.GetProperty("Completed").GetBoolean() &&
                creation.DataId == RelicPerkeoModel.DestinationCardId, "Changed original conversion consumer.");
            Branches(source, () =>
            {
                CardGenerationResult result = RelicPerkeoModel.Convert(source, cardId, creation);
                Require(result.Supported, result.UnsupportedReason ?? "Conversion unsupported.");
                Equal(result.Context, target, "conversion complete context");
            });
            CardToken born = target.Cards.Hand.Single(card => card.InstanceId == source.NextCardId);
            Require(born.DataId == RelicPerkeoModel.DestinationCardId && target.NextCardId == source.NextCardId + 1 &&
                target.OtherPiles!.Single(pile => pile.Name == "Purged").Cards.Any(card => card.InstanceId == cardId) &&
                !target.Cards.Hand.Any(card => card.InstanceId == cardId) && !target.Statistics!.DeckCards!.Contains(cardId) &&
                target.Statistics.DeckCards.Contains(born.InstanceId) && target.Resources!.VisibleDeckCount == source.Resources!.VisibleDeckCount &&
                target.FindCard(born.InstanceId)!.Permanent.Upgrades.All(upgrade => upgrade.DataId != RelicPerkeoModel.UpgradeId) &&
                source.BattleRng.Equals(target.BattleRng) && source.Cards.Rng.Equals(target.Cards.Rng),
                "Conversion purge/permanent ownership/fresh card/RNG boundaries differ.");
            CardInstanceState card = source.FindCard(cardId)!;
            CardModifiers removed = new(card.Permanent.Offsets, card.Permanent.Upgrades.Where(upgrade => upgrade.DataId != RelicPerkeoModel.UpgradeId).ToArray(),
                card.Permanent.PersistentHealth, card.Permanent.ExternalInteractions);
            CombatContext ablated = source.WithCard(card.WithModifiers(removed, card.Temporary));
            Require(!RelicPerkeoModel.Convert(ablated, cardId, creation).Supported, "Conversion accepted an absent original upgrade.");
        }
        int controls = 0, phases = 0;
        foreach (FixtureValue phase in root.GetProperty("RelicPerkeoUnplayed").EnumerateArray())
        {
            phases++;
            CombatContext source = phase.GetProperty("Before").Deserialize<CombatContext>()!, target = phase.GetProperty("Actual").Deserialize<CombatContext>()!;
            Require(phase.GetProperty("Completed").GetBoolean(), "Pending unplayed phase.");
            CardGenerationResult result = RelicPerkeoModel.ApplyUnplayed(source);
            Require(result.Supported, result.UnsupportedReason ?? "Unplayed phase unsupported."); Equal(result.Context, target, "complete unplayed phase");
            foreach (int id in phase.GetProperty("UnupgradedControls").Deserialize<int[]>()!)
            {
                controls++;
                Require(source.FindCard(id)!.DataId == RelicPerkeoModel.SourceCardId && !RelicPerkeoModel.HasConversion(source.FindCard(id)) &&
                    target.Cards.Hand.Any(card => card.InstanceId == id) && target.Statistics!.DeckCards!.Contains(id),
                    "Unupgraded original card converted or disappeared during unplayed dispatch.");
                Equal(source.FindCard(id), target.FindCard(id), "unupgraded original control");
            }
        }
        Require(phases > 0 && controls > 0, "Missing actual unplayed negative controls.");
        Console.WriteLine($"ORIGINAL-PERKEO-NATIVE PASS: unchanged one-card original pool, permanent random-pile generation/upgrade, actual unplayed conversion, exact permanent replacement/purge/piles/statistics/RNG, {phases} unplayed phases/{controls} unupgraded controls and 16 immutable branches.");
    }
    private static void Branches(CombatContext source, Action check)
    {
        string parent = JsonSerializer.Serialize(source, ModelJson.Options); check(); Parallel.For(0, 16, _ => check());
        Require(parent == JsonSerializer.Serialize(source, ModelJson.Options), "Perkeo mutated its parent.");
    }
    private static void Equal<T>(T predicted, T actual, string label)
    {
        string? difference = ModelJson.Difference(JsonSerializer.Serialize(predicted, ModelJson.Options), JsonSerializer.Serialize(actual, ModelJson.Options));
        Require(difference == null, label + ": " + difference);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
