using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalPartFactoryChecks
{
    private const string DataId = "623ce968-aae5-4b07-b0c8-071f315031b7";

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("ModifierScenario").GetString() == "original-part-factory" &&
            root.GetProperty("GameVersion").GetString() == "2.2.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Pending" }.All(name => root.GetProperty(name).GetInt32() == 0),
            "Original PartFactory battle failed acceptance.");
        FixtureValue[] rows = root.GetProperty("RelicStartBattleGeneration").EnumerateArray().ToArray();
        Require(rows.Length == 1, "Expected one natural PartFactory start-of-battle callback.");
        FixtureValue row = rows[0];
        Require(row.GetProperty("Completed").GetBoolean() && row.GetProperty("OwnerId").GetString() == DataId &&
            row.GetProperty("EffectIndex").GetInt32() == 0, "PartFactory native callback identity or completion differs.");
        CombatContext before = row.GetProperty("Before").Deserialize<CombatContext>()!;
        CombatContext actual = row.GetProperty("Actual").Deserialize<CombatContext>()!;
        CombatRelicState owner = before.Relics!.Single(item => item.DataId == DataId);
        RelicResourceEffect effect = owner.ResourceEffects!.Single(item => item.EffectIndex == 0);
        CardGenerationRule rule = effect.CardGeneration!;
        Require(owner.AssetKey == "PartFactory" && owner.IsCovenant == false && owner.DisallowedInPlacementPhase == false &&
            owner.EffectTypes.SequenceEqual([RelicCardGenerationModel.StartOfBattle]) && effect.Type == RelicCardGenerationModel.StartOfBattle &&
            effect.Conditions.Count == 0 && effect.RequiredMutatorId == null && rule.Destination == "HandPile" && rule.Count == 1 &&
            rule.Pool.Count > 0 && rule.Permanent == false && rule.Upgrade == null && !rule.UpgradeIsPermanent &&
            !rule.DeterministicPoolSelection && rule.SelectedPoolIndex == null && rule.ExternalInteractions.Count == 0,
            "PartFactory original owner or card-pool definition changed.");
        Require(RelicResourceModel.Validate(before) == null, "Captured PartFactory resource metadata is invalid.");
        CardToken[] generated = row.GetProperty("GeneratedCards").Deserialize<CardToken[]>()!;
        Require(generated.Length == 1 && !before.CardRegistry!.Any(card => card.InstanceId == generated[0].InstanceId) &&
            actual.Cards.Hand.Any(card => card.InstanceId == generated[0].InstanceId && card.DataId == generated[0].DataId),
            "Native PartFactory did not add exactly one newly owned card to the hand.");
        int[] matches = rule.Pool.Select((card, index) => (card, index)).Where(item => item.card.DataId == generated[0].DataId)
            .Select(item => item.index).ToArray();
        Require(matches.Length > 0, "The native selected card is absent from the captured PartFactory pool.");
        Require(matches.All(index => RelicCardGenerationModel.IsPartFactoryGraftedCard(rule.Pool[index])),
            "PartFactory's selected weighted entries no longer expose the captured Grafted unit-card boundary.");
        Require(!root.GetProperty("Actions").EnumerateArray().Any(action =>
            action.GetProperty("Action").GetProperty("CardInstanceId").GetInt32() == generated[0].InstanceId),
            "The native battle played the generated Grafted card; that play path is outside PartFactory coverage.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        foreach (int candidateIndex in matches)
        {
            CardGenerationResult predicted = RelicCardGenerationModel.ApplyStartOfBattle(before, DataId, 0, candidateIndex);
            Require(predicted.Supported, "PartFactory card generation unsupported: " + predicted.UnsupportedReason);
            string? difference = ModelJson.Difference(JsonSerializer.Serialize(predicted.Context, ModelJson.Options),
                JsonSerializer.Serialize(actual, ModelJson.Options));
            bool addedCardsMatch = predicted.AddedCards.Count == generated.Length && predicted.AddedCards.Zip(generated,
                (left, right) => left.InstanceId == right.InstanceId && left.DataId == right.DataId).All(match => match);
            Require(difference == null && addedCardsMatch,
                "Native PartFactory card generation differs for a matching pool entry: " + difference);
        }
        Require(before.BattleRng.Equals(actual.BattleRng) && before.Cards.Rng.Equals(actual.Cards.Rng) &&
            parent == JsonSerializer.Serialize(before, ModelJson.Options),
            "PartFactory changed a modeled battle/card RNG stream or mutated its input.");
        Console.WriteLine($"PART-FACTORY-CONTEXT-CHECK complete generated-card projection matched; unrelated battle unsupported count={root.GetProperty("Unsupported").GetInt32()}.");
        Require(root.GetProperty("Unsupported").GetInt32() == 0, "Original PartFactory battle contains unsupported transitions.");
        Console.WriteLine($"NATIVE-PART-FACTORY PASS: unchanged owner; native SetupRun-selected card {generated[0].DataId} added to hand; all {matches.Length} weighted entries for that card produce the captured complete context.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
