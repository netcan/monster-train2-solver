using System;
using System.Collections.Generic;
using System.Linq;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class ForgePointsChecks
{
    internal static void Native(FixtureValue root, bool required = false)
    {
        bool eligible = root.TryGetProperty("ModifierScenario", out FixtureValue scenario) && scenario.GetString() == "forge-points";
        Require(eligible || !required, "Fixture lacks the Forge Points relic battle.");
        if (!eligible) return;
        Require(root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("Mismatches").GetInt32() == 0 &&
            root.GetProperty("CaptureFailures").GetInt32() == 0 && root.GetProperty("Unsupported").GetInt32() == 0 &&
            root.GetProperty("Pending").GetInt32() == 0, "Forge Points relic battle did not pass strict native acceptance.");

        const string relicId = "4e0f1e86-f122-4b20-82cf-7c997e4ee84a";
        BattleTurnState? beforeAction = null;
        bool changedInBattle = false;
        foreach (FixtureValue action in root.GetProperty("Actions").EnumerateArray())
        {
            BattleTurnState before = action.GetProperty("Before").Deserialize<BattleTurnState>()!;
            if (before.Spawn.Train.Context?.Relics?.Any(relic => relic.DataId == relicId) != true) continue;
            beforeAction ??= before;
            BattleTurnState actual = action.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            changedInBattle |= before.ForgePoints != actual.ForgePoints;
        }
        Require(beforeAction?.Spawn.Train.Context?.Relics?.SingleOrDefault(relic => relic.DataId == relicId) is CombatRelicState owner,
            "The unchanged HerzalsAnvil owner was absent from the native battle.");
        CombatRelicState relicOwner = beforeAction!.Spawn.Train.Context!.Relics!.Single(relic => relic.DataId == relicId);
        RelicForgePointModifier modifier = relicOwner.ForgePointModifiers!.Single();
        Require(relicOwner.AssetKey == "HerzalsAnvil" && relicOwner.EffectTypes.SequenceEqual([ForgePointsModel.AdditionalForgeRelic]) &&
            modifier.EffectIndex == 0 && modifier.AdditionalPoints == 1 && modifier.ConditionCount == 0 &&
            relicOwner.DisallowedInPlacementPhase == false && changedInBattle,
            "The original Forge Points effect lacked its unmodified payload or a live battle witness.");

        CombatContext context = beforeAction.Spawn.Train.Context;
        ForgePointsResult withOwner = ForgePointsModel.Apply(context, 1);
        CombatContext withoutOwner = CopyWithRelics(context, context.Relics!.Where(relic => relic.DataId != relicId).ToArray());
        ForgePointsResult ablated = ForgePointsModel.Apply(withoutOwner, 1);
        Require(withOwner.Supported && ablated.Supported && withOwner.Amount - ablated.Amount == 1,
            "Removing HerzalsAnvil did not remove exactly one Forge Point from a positive native effect.");
        Console.WriteLine("NATIVE-FORGE-POINTS-RELIC PASS: unchanged HerzalsAnvil, positive effect in battle, exact +1 ablation and zero differential failures.");
    }

    internal static void Run()
    {
        var rng = UnityRng.Seed(318);
        CardPileState[] piles = [new("Standby", []), new("Exhausted", []), new("DiscardBuffer", []), new("Eaten", []), new("Purged", [])];
        BattleStatistics statistics = new([], [], [], [], [], [], [], 0, 0, 0, 0, 0, [1], [1]);
        CardInstanceState sourceCard = new(1, "forge", CardModifiers.Empty(), CardModifiers.Empty(), 0, 0, 0, [],
            forgeCountScalingTraits: [new(new CardStatisticQuery("ForgePoints"), 2)]);
        CombatContext Context(int forgePoints, IReadOnlyList<CardInstanceState>? cards = null, IReadOnlyList<CombatRelicState>? relics = null) =>
            new(new([new(1, "forge")], [], [], rng, 0, []), rng, 0, 2, 10, statistics: statistics,
                cardInstances: cards ?? [sourceCard], otherPiles: piles,
                queryFrame: new(2, true, 2, forgePoints, 0, 1, 0), relics: relics);

        CombatContext root = Context(3);
        ForgePointsResult scaled = ForgePointsModel.Apply(root, 4, 1);
        Require(scaled.Supported && scaled.Amount == 13 && root.QueryFrame!.ForgePoints == 3,
            "Forge count scaling did not read the current statistic or preserve its parent.");
        ForgePointsResult rescaled = ForgePointsModel.Apply(scaled.Context, 4, 1);
        Require(rescaled.Supported && rescaled.Amount == 43,
            "Forge count scaling was not reevaluated against the updated query frame.");
        ForgePointsResult reduced = ForgePointsModel.Apply(Context(3), -8);
        Require(reduced.Supported && reduced.Amount == 0, "Negative forge point changes did not clamp at zero.");
        CombatRelicState relic = new("extra-forge", "ExtraForge", [ForgePointsModel.AdditionalForgeRelic],
            disallowedInPlacementPhase: true,
            forgePointModifiers: [new(0, 2, false)]);
        ForgePointsResult withRelic = ForgePointsModel.Apply(Context(3, relics: [relic]), 4, 1);
        Require(ForgePointsModel.Validate(Context(3, relics: [relic])) == null && withRelic.Supported && withRelic.Amount == 15,
            "The original relic bonus did not precede card Forge-count scaling.");
        Require(ForgePointsModel.Apply(Context(3, relics: [relic]), 1, placement: true).Amount == 4 &&
            ForgePointsModel.Apply(Context(3, relics: [relic]), 1, preview: true).Amount == 4 &&
            ForgePointsModel.Apply(Context(3, relics: [relic]), -1).Amount == 2,
            "Placement, preview or nonpositive Forge adjustments applied the relic bonus.");
        var forgeCondition = new RelicConditionState(new("TurnCount"), false, 2, 2, true);
        CombatRelicState conditionalRelic = new("conditional-forge", "ConditionalForge", [ForgePointsModel.AdditionalForgeRelic],
            disallowedInPlacementPhase: false, forgePointModifiers: [new(0, 1, true, [forgeCondition])]);
        ForgePointsResult conditioned = ForgePointsModel.Apply(Context(3, [new(1, "forge", CardModifiers.Empty(), CardModifiers.Empty(), 0, 0, 0, [],
            forgeCountScalingTraits: Array.Empty<ScalingForgeCountTrait>())], [conditionalRelic]), 1);
        ForgePointsResult consumed = ForgePointsModel.Apply(conditioned.Context, 1);
        Require(conditioned.Supported && conditioned.Amount == 5 && consumed.Supported && consumed.Amount == 6 &&
            conditioned.Context!.Relics![0].ForgePointModifiers![0].Conditions[0].ValueAtLastTrigger == 2,
            "Conditional Forge Points bonus did not record immutable native condition state.");
        Require(ForgePointsModel.Test(true) && !ForgePointsModel.Test(false) && !ForgePointsModel.Test(null),
            "Forge point effect ignored its native Pyre-health gate.");

        CardActionEffect rangedEffect = new(ForgePointsModel.Effect, "Room", 0, false, false, [],
            tests: new(true, false, false, false, true), range: new(-2, 5, .75f));
        CardInstanceState actionCard = new(1, "forge", CardModifiers.Empty(), CardModifiers.Empty(), 0, 0, 0, [],
            forgeCountScalingTraits: Array.Empty<ScalingForgeCountTrait>());
        CombatContext actionContext = Context(3, [actionCard]);
        var enemy = new CombatUnit(11, "enemy", CombatTeam.Enemy, 0, 50, 50, false, false, false, []);
        var pyre = new CombatUnit(12, "pyre", CombatTeam.Player, 45, 80, 80, true, true, false, []);
        var train = new TrainCombatState([new(0, false, [enemy], [], actionContext), new(1, false, [pyre], [], actionContext)], [], 7, actionContext);
        var spawn = new EnemySpawnState(train, [new([new([])]), new([new([])]), new([new([])])], [-1, -1, -1],
            0, false, rng, 13, [], 0, false, 0, 0, 2, []);
        var rules = new BattlePlayRules([new(0, 5, 7, true, false, false)],
            [new("forge", "forge", 1, "Spell", "Discard", null, [], [rangedEffect])]);
        var state = new BattleTurnState(spawn, 2, 3, 5, 3, 0, "New", [new("Spawning", 318, rng)], piles, [], rules);
        RngDraw sample = rangedEffect.Range!.Sample(rng);
        BattleActionResult played = BattleActionModel.PlayCard(state, new(1, 0));
        Require(played.Supported && played.State!.ForgePoints == Math.Max(0, 3 + sample.Value) &&
            played.State.Spawn.Train.Context!.QueryFrame!.ForgePoints == played.State.ForgePoints &&
            played.State.Spawn.Train.Context.BattleRng.Equals(sample.State),
            "Forge spell range, RNG or decision-state synchronization differs: " + played.Reason);
        Console.WriteLine("FORGE-POINTS-CHECKS PASS: signed adjustment, floor, dynamic scaling, ordered relic bonus, placement/preview/positive gates, condition recording, Pyre gate, range RNG and action-state synchronization.");
    }

    private static void Require(bool pass, string message)
    { if (!pass) throw new InvalidOperationException(message); }

    private static CombatContext CopyWithRelics(CombatContext source, IReadOnlyList<CombatRelicState> relics) =>
        new(source.Cards, source.BattleRng, source.Gold, source.NextCardId, source.MaxHandSize,
            statusRules: source.StatusRules, statistics: source.Statistics, cardInstances: source.CardInstances,
            cardRegistry: source.CardRegistry, allScenarioBossesDead: source.AllScenarioBossesDead,
            nextAddedTemporaryUpgrades: source.NextAddedTemporaryUpgrades, otherPiles: source.OtherPiles,
            queryFrame: source.QueryFrame, killCamActivated: source.KillCamActivated, magicPower: source.MagicPower,
            isolatedBattlePreview: source.IsolatedBattlePreview, energyState: source.EnergyState,
            roomCapacities: source.RoomCapacities, abilityCardCache: source.AbilityCardCache,
            lastAbilityActivatorUnitId: source.LastAbilityActivatorUnitId,
            permanentlyDisabledAbilities: source.PermanentlyDisabledAbilities, lastSpawnedUnitId: source.LastSpawnedUnitId,
            nextUnitId: source.NextUnitId, spawnPoints: source.SpawnPoints, summonCatalog: source.SummonCatalog,
            enchantments: source.Enchantments, purifyBlockedTriggers: source.PurifyBlockedTriggers,
            relics: relics, triggerCounts: source.TriggerCounts, resources: source.Resources);
}
