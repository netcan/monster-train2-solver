using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalFullCostReductionChecks
{
    internal static void Native(FixtureValue fixture, FixtureValue? randomHandFixture = null)
    {
        Require(fixture.GetProperty("Schema").GetInt32() == 1 &&
            fixture.GetProperty("Boundary").GetString() == "OriginalFullCostReduction" &&
            fixture.GetProperty("GameVersion").GetString() == Game231FixtureCatalog.Version &&
            fixture.GetProperty("GameModuleMvid").GetString() == Game231FixtureCatalog.Module,
            "Expected the original full-cost-reduction binary observation.");
        FixtureValue definition = fixture.GetProperty("Definition");
        Require(definition.ContentEquals(fixture.GetProperty("DefinitionAfter")) &&
            definition.GetProperty("FecundityOwner").GetString() == "d968e210-0d44-47c9-b783-f0c90dfe757b" &&
            definition.GetProperty("FreeOwner").GetString() == "128dd519-611c-4999-b7ac-e63ea027b1fa",
            "The original owner definitions changed.");
        var original = definition.GetProperty("Fecundity").Deserialize<CardUpgradeModifier>()!;
        var free = definition.GetProperty("FullReduction").Deserialize<CardUpgradeModifier>()!;
        Require(original.Lifecycle != null && CardFullCostReductionModel.OriginalTraitUpgrade(original.DataId, original.AssetKey, original.Lifecycle) &&
            free.DataId == CardFullCostReductionModel.FreeUpgradeId && free.Stats.Cost == -99,
            "Expected the original Fecundity and FreeFromBlessing payloads.");

        FixtureValue[] cases = fixture.GetProperty("Cases").EnumerateArray().ToArray();
        Require(cases.Select(row => row.GetProperty("Role").GetString()).SequenceEqual(new[] { "Neither", "Target", "ExplicitSource" }),
            "The transfer cases did not cover both native trait-owner roles and a control.");
        foreach (FixtureValue row in cases)
        {
            string role = row.GetProperty("Role").GetString()!;
            int targetId = row.GetProperty("TargetId").GetInt32();
            int sourceId = row.GetProperty("SourceId").GetInt32();
            var before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            var actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            var context = before.Spawn.Train.Context!;
            CardInstanceState target = context.FindCard(targetId)!;
            CardInstanceState? explicitSource = sourceId == 0 ? null : context.FindCard(sourceId);
            Require(context.Cards.Hand.Any(card => card.InstanceId == targetId) &&
                (role == "ExplicitSource" ? explicitSource != null && explicitSource.InstanceId != targetId : sourceId == 0),
                "Wrong observed card relationship.");
            var mask = target.MaskDescriptor!;
            int beforeCost = CardHandCostModel.Resolve(context, target, mask.BaseCost, mask.Definition.CardType);
            const string steward = "d14a50f3-728d-43e1-87f0-ef1b013f6678";
            Require(target.DataId == steward && beforeCost > 0 && beforeCost < 99 &&
                row.GetProperty("NativeCostBefore").GetInt32() == beforeCost && row.GetProperty("NativeCostAfter").GetInt32() == 0,
                "Original native paid cost was not reproduced.");

            CardUpgradeModifier predicted = CardFullCostReductionModel.Resolve(context, target, explicitSource, free, out string? error);
            var native = row.GetProperty("Applied").Deserialize<CardUpgradeModifier>()!;
            CardInstanceState after = actual.Spawn.Train.Context!.FindCard(targetId)!;
            CardUpgradeModifier installed = after.Temporary.Upgrades.Single(upgrade => upgrade.DataId == free.DataId);
            Require(error == null && predicted.Stats.Cost == (role == "Neither" ? -99 : -beforeCost) &&
                Serialize(predicted) == Serialize(native) && installed.Stats.Cost == predicted.Stats.Cost &&
                Serialize(target.Permanent) == Serialize(after.Permanent) &&
                before.Energy == actual.Energy && Serialize(before.RngStreams) == Serialize(actual.RngStreams),
                "Original " + role + " callback changed its payload, registry, Energy or RNG.");
            if (role == "Target" && randomHandFixture is FixtureValue originalHandUpgrade)
                ReplayOriginalRelic(context, target, installed, originalHandUpgrade);
        }
        Console.WriteLine("ORIGINAL-FULL-COST-REDUCTION PASS: native control, target trait, explicit source trait.");
    }

    private static void ReplayOriginalRelic(CombatContext context, CardInstanceState target, CardUpgradeModifier expected,
        FixtureValue randomHandFixture)
    {
        // Reuse the independently captured state immediately after the native random selection.
        var relicState = randomHandFixture.GetProperty("RelicRandomHandUpgradeRecords").EnumerateArray()
            .First(row => row.GetProperty("Phase").GetString() == "ApplyPlay").GetProperty("Before").Deserialize<TrainCombatState>()!;
        CombatRelicState owner = relicState.Context!.Relics!.Single(relic => relic.DataId == RelicRandomHandUpgradeModel.DataId);
        RelicCardModifier originalEffect = owner.CardModifiers!.Single();
        RelicRandomHandUpgrade originalRule = originalEffect.RandomHandUpgrade!;
        var selected = originalRule.WithState(new[] { target.InstanceId }, Array.Empty<int>());
        owner = owner.WithCardModifiers(new[] { new RelicCardModifier(originalEffect.EffectIndex, null, null, false,
            originalEffect.Conditions, randomHandUpgrade: selected) });
        CombatContext before = context.WithRelics(context.Relics!.Append(owner).ToArray());
        string parent = Serialize(before);
        Require(RelicModel.Validate(before.Relics) == null, "Expected the original random-hand-upgrade relic definition.");
        var after = RelicRandomHandUpgradeModel.Apply(before);
        Require(after.Supported, "The regular original relic application rejected the native upgrade: " + after.UnsupportedReason);
        var installed = after.Context.FindCard(target.InstanceId)!.Temporary.Upgrades.Single(upgrade => upgrade.DataId == expected.DataId);
        string actual = Serialize(installed); string native = Serialize(expected);
        Require(actual == native && parent == Serialize(before),
            "The regular original relic application disagreed with its native upgrade: " + ModelJson.Difference(native, actual));
    }

    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
