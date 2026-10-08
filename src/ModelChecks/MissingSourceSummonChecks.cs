using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class MissingSourceSummonChecks
{
    internal static void Native(FixtureValue fixture)
    {
        PoolSummonChecks.Native(fixture);
        string scenario = fixture.GetProperty("ModifierScenario").GetString()!;
        var births = fixture.GetProperty("UnitBirths").EnumerateArray().ToArray();
        var missing = births.Where(sample => sample.GetProperty("SpawnerCardId").GetInt32() == 0).ToArray();
        var setups = fixture.GetProperty("FreshSpawners").EnumerateArray().ToArray();
        var upgrades = fixture.GetProperty("SpawnUpgrades").EnumerateArray().ToArray();
        var globals = fixture.GetProperty("GlobalStandbyChecks").EnumerateArray().ToArray();
        Require(missing.Length >= 2 && globals.Length > 0 && fixture.GetProperty("DetachedCardClones").GetArrayLength() == 0,
            "Missing-source births or global Standby checks were not observed.");
        Require(upgrades.Count(sample => sample.GetProperty("SpawnerCardId").GetInt32() == 0) == missing.Length &&
            upgrades.Where(sample => sample.GetProperty("SpawnerCardId").GetInt32() == 0).All(sample => !sample.GetProperty("SourceAdded").GetBoolean()),
            "Missing-source extra upgrades allocated or wrote a source card.");
        if (scenario.Contains("no-primary")) Require(setups.Length == 0, "An all-missing pool created a fallback card.");
        else Require(setups.Length > 0 && upgrades.Any(sample => sample.GetProperty("SourceAdded").GetBoolean()),
            "Mixed present/missing fallbacks were not exercised.");
        bool firstMissing = false;
        foreach (var sample in fixture.GetProperty("Actions").EnumerateArray())
        {
            var before = sample.GetProperty("Before").Deserialize<BattleTurnState>()!;
            var action = sample.GetProperty("Action").Deserialize<PlayCardAction>()!;
            var card = before.Spawn.Train.Context!.FindCard(action.CardInstanceId)!;
            var rule = before.PlayRules!.Cards.Single(item => item.DataId == card.DataId);
            if (rule.Summon?.Pool.Count is not > 0) continue;
            VerifyUnknownLookup(before, rule, action);
            var after = sample.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            var oldIds = before.Spawn.Train.Rooms[action.RoomIndex].Units.Select(unit => unit.Id).ToHashSet();
            var born = after.Spawn.Train.Rooms[action.RoomIndex].Units.Where(unit => !oldIds.Contains(unit.Id)).OrderBy(unit => unit.Id).ToArray();
            for (int index = 0; index < born.Length; index++)
                Require((born[index].Status("cardless")?.Stacks > 0) == (index > 0 || born[index].SpawnerCardId == 0),
                    "Source absence failed to mark the first birth cardless.");
            firstMissing |= born[0].SpawnerCardId == 0;
            Require(after.Spawn.Train.Context!.NextCardId - before.Spawn.Train.Context.NextCardId == born.Count(unit => unit.SpawnerCardId > 0),
                "Absent fallbacks consumed card identities or fresh sources failed to allocate them.");
            Require(after.OtherPiles.Any(pile => pile.UnitConditions?.Any(binding => binding.CardId == card.InstanceId &&
                binding.HostUnitId == born[0].Id) == true), "The paid card lost its first-host Standby binding.");
        }
        if (scenario.Contains("no-primary")) Require(firstMissing, "The first all-missing birth did not remain source-free.");
        if (scenario.EndsWith("deaths", StringComparison.Ordinal))
        {
            var zeroIds = missing.Select(sample => sample.GetProperty("UnitId").GetInt32()).ToHashSet();
            Require(globals.Any(sample => sample.GetProperty("Before").Deserialize<CombatContext>()!.OtherPiles!
                .Any(pile => pile.UnitConditions?.Any(binding => binding.Ready && zeroIds.Contains(binding.HostUnitId)) == true)),
                "No dead source-free host retained an original paid card until the global check.");
        }
        Verify();
        Parallel.For(0, 32, _ => Verify());
        void Verify()
        {
            foreach (var sample in setups) FreshSummonChecks.VerifySetup(sample);
            foreach (var sample in globals) FreshSummonChecks.VerifyGlobal(sample);
            foreach (var sample in missing)
            {
                var before = sample.GetProperty("Before").Deserialize<RoomCombatState>()!;
                var after = sample.GetProperty("After").Deserialize<RoomCombatState>()!;
                var born = after.Units.Single(unit => unit.Id == sample.GetProperty("UnitId").GetInt32());
                Require(sample.GetProperty("IsCardless").GetBoolean() && born.SpawnerCardId == 0 &&
                    born.StatusImmunities.Contains("endless") && after.Context!.FindCard(0) == null &&
                    after.Context.NextCardId == before.Context!.NextCardId, "Source-free birth lost its marker/immunity or created a card.");
                Require(born.Modifiers!.Upgrades.All(upgrade => upgrade.AssetKey is not
                    ("PojuMultiSummonPermanent" or "PojuMultiSummonTemporary" or "PojuMultiSummonExcludedPermanent" or "PojuMultiSummonExcludedTemporary")),
                    "Source-free birth inherited paid-card upgrades.");
            }
        }
        Console.WriteLine($"NATIVE-MISSING-SOURCE-SUMMON-CHECKS PASS: {missing.Length} source-free births, {setups.Length} fresh sources, {globals.Length} global checks, exact cardless/RNG/upgrades/identities/bindings and 32 branches.");
    }
    private static void VerifyUnknownLookup(BattleTurnState before, CardPlayRule rule, PlayCardAction action)
    {
        var summon = rule.Summon!;
        var unknown = new UnitSummonRule(summon.Count, summon.Creation, summon.CardlessStatus, summon.Upgrade,
            summon.IgnoreCardUpgrades, summon.FallbackCreation, summon.Additional == null ? null :
                new UnitSummonChoice(summon.Additional.Unit, summon.Additional.FallbackCreation),
            summon.Pool.Select(choice => new UnitSummonChoice(choice.Unit, choice.FallbackCreation)).ToArray(),
            summon.NativeBaseSize, summon.TriggersPaidRally);
        var changed = new CardPlayRule(rule.DataId, rule.AssetKey, rule.Cost, rule.Effect, rule.Destination,
            rule.SpawnUnit, rule.ExternalInteractions, rule.Effects, rule.UpgradeInteractions, rule.HandDiscardInteractions,
            rule.HandConsumeInteractions, rule.CostType, rule.Equipment, rule.Ability, unknown);
        var rules = new BattlePlayRules(before.PlayRules!.Rooms,
            before.PlayRules.Cards.Select(item => item.DataId == rule.DataId ? changed : item).ToArray(), before.PlayRules.StatusRules);
        var uncaptured = new BattleTurnState(before.Spawn, before.Energy, before.EnergyPerTurn, before.DrawPerTurn,
            before.ForgePoints, before.DragonsHoard, before.MoonPhase, before.RngStreams, before.OtherPiles,
            before.ExternalInteractions, rules, before.BattlePreviewEnabled, before.UiRngIsolated, before.CanonicalDecisionReferences);
        string parent = JsonSerializer.Serialize(uncaptured);
        var result = BattleActionModel.PlayCard(uncaptured, action);
        Require(!result.Supported && result.Rejection == ActionRejection.Unsupported && JsonSerializer.Serialize(uncaptured) == parent,
            "Uncaptured fallback absence became a supported child or mutated its parent.");
    }
    private static void Require(bool condition, string error) { if (!condition) throw new InvalidOperationException(error); }
}
