using System;
using System.Collections.Generic;
using MonsterTrain2Poju.Model;

internal static class ForgePointsChecks
{
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
        CombatRelicState relic = new("extra-forge", "ExtraForge", [ForgePointsModel.AdditionalForgeRelic]);
        Require(ForgePointsModel.Validate(Context(3, relics: [relic])) != null,
            "An unmodeled relic Forge Points bonus was silently accepted.");
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
        Console.WriteLine("FORGE-POINTS-CHECKS PASS: signed adjustment, floor, dynamic scaling, Pyre gate, explicit relic rejection, range RNG and action-state synchronization.");
    }

    private static void Require(bool pass, string message)
    { if (!pass) throw new InvalidOperationException(message); }
}
