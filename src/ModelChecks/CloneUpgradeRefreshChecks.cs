using MonsterTrain2Poju.Fixtures;
using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class CloneUpgradeRefreshChecks
{
    internal static void Run()
    {
        CardUpgradeModifier Upgrade(string id, int damage, int heal, int? damageBase = null, int? healBase = null) =>
            new(id, id, new(damage: damage, health: 11, heal: heal), [], false, false, false, 0, 0, [],
                cloneDamageBase: damageBase, cloneHealBase: healBase);
        var reset = Upgrade("reset", 8, 8, 1, 2);
        var protectedUpgrade = Upgrade("protected", 9, 7);
        var temporary = Upgrade("temporary", 9, 9, 3, 4);
        var optional = Upgrade("optional", 8, 9, 5, 6);
        var pending = Upgrade("pending", 10, 10, 1, 2);
        var sourceCard = new CardInstanceState(1, "source", new(new(damage: 7), [reset, protectedUpgrade], 0, []),
            new(new(heal: 6), [temporary], 0, []), 0, 0, 0, []);
        var rng = UnityRng.Seed(86);
        var context = new CombatContext(new([new(1, "source")], [], [], rng, 0, []), rng, 0, 2, 10,
            statistics: BattleStatistics.Empty(deckCards: [1]).TrackCards([1]), cardInstances: [sourceCard],
            nextAddedTemporaryUpgrades: [pending], otherPiles: []);
        var creation = new CardCreationRule("clone", CardModifiers.Empty(), null, []);
        var rule = new CardGenerationRule("HandPile", 1, [creation], copyModifiers: true, upgrade: optional);
        string parent = JsonSerializer.Serialize(context);
        var result = CardGenerationModel.Apply(context, rule, 1);
        var clone = result.Context!.CardInstances!.Single(card => card.InstanceId == 2);
        Require(result.Supported && clone.Permanent.Upgrades[0].Stats.Damage == 1 && clone.Permanent.Upgrades[0].Stats.Heal == 2 &&
            clone.Permanent.Upgrades[0].Stats.Health == 11 && clone.Permanent.Upgrades[1].Stats.Damage == 9 &&
            clone.Permanent.Upgrades[1].Stats.Heal == 7 && clone.Permanent.Offsets.Damage == 7 && clone.Temporary.Offsets.Heal == 6 &&
            clone.Temporary.Upgrades.Select(upgrade => upgrade.Stats.Damage).SequenceEqual([5, 3, 10]) &&
            clone.Temporary.Upgrades.Select(upgrade => upgrade.Stats.Heal).SequenceEqual([6, 4, 10]),
            "Clone refresh lost base damage/heal, protected values, other statistics, offsets or native pending-upgrade order.");
        var ordinary = CardGenerationModel.Apply(context, new("HandPile", 1, [creation], upgrade: optional), 1);
        Require(ordinary.Context!.CardInstances!.Single(card => card.InstanceId == 2).Temporary.Upgrades[0].Stats.Damage == 8,
            "Clone-only refresh ran during ordinary generation.");
        var ignoreTemp = CardGenerationModel.Apply(context, new("HandPile", 1, [creation], copyModifiers: true,
            ignoreTemporaryModifiers: true, upgrade: optional), 1);
        Require(ignoreTemp.Context!.CardInstances!.Single(card => card.InstanceId == 2).Temporary.Upgrades.Select(upgrade => upgrade.Stats.Heal)
            .SequenceEqual([6, 10]), "Ignoring source temporary upgrades skipped the destination's clone refresh.");
        var scaled = UnitUpgradeScalingModel.ApplyTrait(context, new(new("AnyCharacter"), "Damage", 3), 1, reset);
        Require(scaled.Supported && scaled.Upgrade!.Stats.Damage == 11 && scaled.Upgrade.CloneDamageBase == 1 && scaled.Upgrade.CloneHealBase == 2,
            "Scaling a unit upgrade discarded its immutable clone refresh definition.");
        var missingSource = CardGenerationModel.Apply(context, new("HandPile", 1, [creation], copyModifiers: true, upgrade: optional), 99);
        Require(missingSource.Context!.CardInstances!.Single(card => card.InstanceId == 2).Temporary.Upgrades[0].Stats.Damage == 8,
            "Missing clone source refreshed upgrades despite native early return.");
        var unsupported = new CardCreationRule("magic", CardModifiers.Empty(), null, ["Generated card trait setup/callback CardTraitMagicPowerMultiplier"]);
        Require(!CardGenerationModel.Apply(context, new("HandPile", 1, [unsupported], copyModifiers: true), 1).Supported,
            "Unsupported magic multiplier produced a clone search child.");
        Parallel.For(0, 32, _ => Require(JsonSerializer.Serialize(CardGenerationModel.Apply(context, rule, 1)) == JsonSerializer.Serialize(result),
            "Parallel clone refresh differs."));
        Require(JsonSerializer.Serialize(context) == parent, "Clone refresh changed source upgrades.");
        Console.WriteLine("CLONE-UPGRADE-REFRESH-CHECKS PASS: positive damage/heal bases, protected/anonymous values, other statistics/offsets, temporary/optional/pending order, no-copy/missing-source gates, retained scaling descriptors and 32 parallel branches.");
    }

    internal static void Native(FixtureValue fixture)
    {
        if (!fixture.TryGetProperty("ModifierScenario", out FixtureValue scenario) || scenario.GetString() != "clone-upgrade-refresh") return;
        int records = 0, copies = 0, damage = 0, heal = 0, protectedValues = 0;
        foreach (FixtureValue item in fixture.GetProperty("CardGenerations").EnumerateArray())
        {
            CombatContext before = item.GetProperty("Before").Deserialize<CombatContext>()!;
            CombatContext actual = item.GetProperty("Actual").Deserialize<CombatContext>()!;
            CardGenerationRule rule = item.GetProperty("Rule").Deserialize<CardGenerationRule>()!;
            int sourceId = item.GetProperty("SourceCardId").GetInt32();
            var predicted = CardGenerationModel.Apply(before, rule, sourceId);
            Require(predicted.Supported && item.GetProperty("Difference").ValueKind == FixtureKind.Null &&
                ModelJson.Difference(JsonSerializer.Serialize(predicted.Context), JsonSerializer.Serialize(actual)) == null,
                "Independent native clone refresh differs.");
            records++;
            if (!rule.CopyModifiers) continue;
            CardInstanceState source = before.CardInstances!.Single(card => card.InstanceId == sourceId);
            Require(JsonSerializer.Serialize(actual.CardInstances!.Single(card => card.InstanceId == sourceId)) == JsonSerializer.Serialize(source),
                "Native clone refresh modified its source.");
            foreach (CardInstanceState clone in actual.CardInstances!.Where(card => card.InstanceId >= before.NextCardId))
            {
                copies++;
                foreach (CardUpgradeModifier upgrade in source.Permanent.Upgrades.Concat(source.Temporary.Upgrades))
                {
                    CardUpgradeModifier copied = clone.Permanent.Upgrades.Concat(clone.Temporary.Upgrades).First(other => other.DataId == upgrade.DataId);
                    if (upgrade.CloneDamageBase.HasValue && upgrade.Stats.Damage != upgrade.CloneDamageBase)
                    { Require(copied.Stats.Damage == upgrade.CloneDamageBase, "Native copied damage did not reset to its data base."); damage++; }
                    if (upgrade.CloneHealBase.HasValue && upgrade.Stats.Heal != upgrade.CloneHealBase)
                    { Require(copied.Stats.Heal == upgrade.CloneHealBase, "Native copied heal did not reset to its data base."); heal++; }
                    if (upgrade.AssetKey == "PojuCloneProtectedDamage")
                    { Require(upgrade.CloneDamageBase == null && upgrade.Stats.Damage == 9 && copied.Stats.Damage == 9,
                        "Native non-magic-scaled copied damage was reset."); protectedValues++; }
                }
            }
        }
        Require(records > 0 && copies >= 2 && damage >= 2 && heal >= 2 && protectedValues >= 2,
            "Native clone refresh lacks both copied damage/heal resets and the non-magic-scaling exception.");
        Console.WriteLine($"NATIVE-CLONE-UPGRADE-REFRESH-CHECKS PASS: {records} exact generations, {copies} copies, {damage} damage resets, {heal} heal resets and {protectedValues} preserved non-magic-scaled upgrades; full contexts and unchanged sources.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
