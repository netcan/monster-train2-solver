using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class ConserveEmberChecks
{
    private const string DataId = "3e200d1f-7ba9-4a6e-a09d-5625f25b01ac";

    internal static void Run()
    {
        var once = new RelicConditionState(new("AnyCardPlayed"), true, 6, 1, false);
        CombatContext root = Context([Owner("once", conditions: [once])]);
        string parent = Serialize(root);
        RelicResourceResult saved = Query(root, 8);
        Require(saved.Value == 0 && saved.Events.Single().Notified && saved.Events.Single().Before == 0 &&
            saved.Events.Single().After == 8 && saved.Context!.QueryFrame!.Energy == 8 &&
            saved.Context.Relics![0].ResourceEffects![0].Conditions[0].DurationTriggerCount == 1 &&
            Query(saved.Context, 8).Value == 8 && Serialize(root) == parent,
            "Conserve-all failed to record its first positive contribution, repeated a once condition or changed its parent.");
        RelicResourceResult zero = Query(root, 0);
        Require(zero.Value == 0 && !zero.Events.Single().Notified &&
            !zero.Context!.Relics![0].ResourceEffects![0].Conditions[0].Triggered,
            "Zero remaining energy consumed a conservation condition.");
        RelicResourceResult preview = Query(root, 8, preview: true);
        Require(preview.Value == 0 && !preview.Events.Single().Notified &&
            !preview.Context!.Relics![0].ResourceEffects![0].Conditions[0].Triggered,
            "Native preview either stopped conserving or recorded a screen-center notification.");
        Require(Query(Context([Owner("placement", disallowed: true)]), 8, placement: true).Value == 8 &&
            Query(Context([Owner("placement", disallowed: true)]), 8).Value == 0 &&
            Query(Context([]), 8).Value == 8,
            "Placement restrictions or the no-owner removal path differ from native.");
        RelicResourceResult duplicate = Query(Context([Owner("first", conditions: [once]), Owner("second", conditions: [once])]), 8);
        Require(duplicate.Value == 0 && duplicate.Events.Select(item => item.Notified).SequenceEqual([true, false]) &&
            !duplicate.Context!.Relics![1].ResourceEffects![0].Conditions[0].Triggered,
            "A second conserve-all effect notified without increasing the conserved amount.");
        var failed = new RelicConditionState(new("EnergyRemainingEndOfTurn"), false, 2, 20, true);
        Require(Query(Context([Owner("failed", conditions: [failed])]), 8).Value == 8,
            "A failed conservation condition retained energy.");

        foreach (CombatRelicState malformed in new[]
        {
            new CombatRelicState("missing", "missing", [RelicResourceModel.ConserveEnergyEndOfTurn]),
            new CombatRelicState("wrong", "wrong", [RelicResourceModel.ConserveEnergyEndOfTurn], disallowedInPlacementPhase: false,
                resourceEffects: [new(0, RelicResourceModel.ConserveEnergyEndOfTurn, 1, 0, 1, false)]),
            new CombatRelicState("flat", "flat", ["RelicEffectConserveEnergyEndOfTurnFlat"]),
            new CombatRelicState("start", "start", ["RelicEffectConserveEnergyStartOfTurn"]),
            Owner("condition", conditions: [new(new("NotCaptured"), false, 2, 1, true)])
        })
            Require(!RelicResourceModel.GetEnergyToRemoveAtTurnEnd(Context([malformed]), 8).Supported,
                "Missing metadata or an unmodeled conservation variant/condition became a guessed result.");

        foreach (bool terminal in new[] { false, true })
        {
            BattleTurnState decision = Decision(8, terminal);
            string original = Serialize(decision);
            BattleTurnResult banked = BattleTurnModel.EndTurn(decision);
            Require(banked.Supported && banked.Outcome == (terminal ? RoomOutcome.BattleWon : RoomOutcome.Exchanged) &&
                banked.State!.Energy == (terminal ? 8 : 11) &&
                banked.State.Spawn.Train.Context!.Statistics!.EnergyRemainingEndOfTurn == (terminal ? 8 : 0),
                "EndTurn lost conserved energy during combat, terminal settlement or the next-turn income: " + banked.UnsupportedReason);
            BattleActionResult paid = BattleActionModel.PlayCard(decision, new(10, 0));
            Require(paid.Supported && paid.State!.Energy == 6, "The paid branch did not spend its native card cost: " + paid.Reason);
            BattleTurnResult spent = BattleTurnModel.EndTurn(paid.State!);
            Require(spent.Supported && spent.State!.Energy == (terminal ? 6 : 9) && Serialize(decision) == original,
                "The paid EndTurn branch discarded retained energy or mutated the parent: " + spent.UnsupportedReason);
            BattleTurnResult ordinary = BattleTurnModel.EndTurn(Decision(8, terminal, relics: []));
            Require(ordinary.Supported && ordinary.State!.Energy == (terminal ? 0 : 3),
                "Adding conservation changed the existing no-owner EndTurn behavior.");
        }
        BattleTurnResult capped = BattleTurnModel.EndTurn(Decision(98, false));
        Require(capped.Supported && capped.State!.Energy == 99, "Retained energy bypassed the native next-turn maximum.");
        var previous = new RelicConditionState(new("EnergyRemainingEndOfTurn"), false, 2, 2, true);
        BattleTurnResult priorStatistic = BattleTurnModel.EndTurn(Decision(8, true, [Owner("prior", conditions: [previous])]));
        Require(priorStatistic.Supported && priorStatistic.State!.Energy == 8 &&
            priorStatistic.State.Spawn.Train.Context!.Statistics!.EnergyRemainingEndOfTurn == 8,
            "Conservation conditions read the new EndTurnEnergy statistic instead of the previous native value.");
        RelicResourceResult rollover = RelicResourceModel.StartTurn(Context([Owner()], energy: 98, next: 2, every: 1));
        Require(rollover.Supported && rollover.Value == 99 && rollover.Context!.EnergyState!.NextTurn == 0 &&
            rollover.Context.EnergyState.EveryTurn == 1, "Conservation lost the pending-energy ledger or its rollover/cap.");
        string expected = Serialize(saved);
        Parallel.For(0, 32, _ => Require(Serialize(Query(root, 8)) == expected && Serialize(Query(root, 0)) == Serialize(zero),
            "Parallel conservation branches diverged."));
        Require(Serialize(root) == parent && saved.Context!.BattleRng.Equals(root.BattleRng),
            "Conservation branches mutated their source or consumed gameplay RNG.");
        Console.WriteLine("CONSERVE-EMBER-CHECKS PASS: native conserve-all, zero/duplicate notifications, preview/placement/condition guards, prior end-turn statistic order, paid/banked and terminal branches, cap99 and immutable parallel state.");
    }

    internal static PlayCardAction? Choose(BattleTurnState source)
    {
        if (source.Spawn.Turn < 2 || source.Energy <= 1 || source.PlayRules == null) return null;
        return BattleActionModel.ChooseUnitSpellAndJunkPlay(source, card =>
        {
            CardPlayRule? rule = source.PlayRules.Cards.FirstOrDefault(item => item.DataId == card.DataId);
            return rule != null && rule.CostType == "Default" && rule.Cost > 0 && rule.Cost < source.Energy;
        });
    }

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("ModifierScenario").GetString() == "original-conserve-ember-relic" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }.All(name => root.GetProperty(name).GetInt32() == 0),
            "Original ConserveEmber battle is not strictly accepted and terminal.");
        FixtureValue[] removals = root.GetProperty("EnergyConservationQueries").EnumerateArray().ToArray();
        FixtureValue[] turns = root.GetProperty("Turns").EnumerateArray().ToArray();
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(turns.Length >= 3 && actions.Length > 0 && removals.Length >= turns.Length,
            "ConserveEmber lacks native accumulation, paid-card or energy-removal witnesses.");
        foreach (FixtureValue row in removals)
        {
            int remaining = row.GetProperty("RemainingPlayerEnergy").GetInt32();
            bool preview = row.GetProperty("Preview").GetBoolean();
            Require(row.GetProperty("Completed").GetBoolean() && row.GetProperty("RemovedEnergy").GetInt32() == 0 &&
                row.GetProperty("PlayerEnergyBefore").GetInt32() == remaining && row.GetProperty("PlayerEnergyAfter").GetInt32() == remaining &&
                row.GetProperty("Notifications").GetInt32() == (!preview && remaining > 0 ? 1 : 0),
                "Native conserve-all query changed energy or returned/notified an unexpected result.");
        }
        foreach (FixtureValue row in turns)
        {
            BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            BattleTurnState actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            CombatContext context = before.Spawn.Train.Context!;
            CombatRelicState owner = context.Relics!.Single(item => item.DataId == DataId);
            Require(owner.AssetKey == "ConserveEmber" && owner.DisallowedInPlacementPhase == false &&
                owner.EffectTypes.SequenceEqual([RelicResourceModel.ConserveEnergyEndOfTurn]) && owner.ResourceEffects?.Count == 1,
                "Original ConserveEmber identity/effect order is missing or changed.");
            RelicResourceEffect effect = owner.ResourceEffects![0];
            Require(effect.EffectIndex == 0 && effect.Type == RelicResourceModel.ConserveEnergyEndOfTurn && effect.ParamInt == 0 &&
                effect.ParamInt2 == 0 && effect.Amount == 0 && !effect.CanApplyInPreviewMode && effect.Conditions.Count == 0,
                "Original ConserveEmber payload or conditions changed.");
            FixtureValue native = removals.Single(item => !item.GetProperty("Preview").GetBoolean() &&
                item.GetProperty("Turn").GetInt32() == before.Spawn.Turn);
            int remaining = native.GetProperty("RemainingPlayerEnergy").GetInt32();
            RelicResourceResult removal = Query(context, remaining, placement: before.Spawn.Turn == 0);
            Require(removal.Value == 0 && removal.Events.Count(item => item.Notified) == native.GetProperty("Notifications").GetInt32(),
                "Model conservation query differs from its native call.");
            BattleTurnResult replay = BattleTurnModel.EndTurn(before);
            bool terminal = (RoomOutcome)row.GetProperty("ActualOutcome").GetInt32() == RoomOutcome.BattleWon;
            Require(replay.Supported && BattleTurnChecks.Comparable(replay.State!, terminal) == BattleTurnChecks.Comparable(actual, terminal),
                "ConserveEmber EndTurn differs from native: " + replay.UnsupportedReason);
            Require(actual.Energy == (terminal ? remaining : Math.Min(context.Resources!.MaxEnergy,
                remaining + context.Resources.BaseEnergy + context.EnergyState!.NextTurn + context.EnergyState.EveryTurn)),
                "Conserved energy was not retained through terminal combat or the native next-turn income.");
        }
        Require(turns.Take(2).All(row => row.GetProperty("Actual").Deserialize<BattleTurnState>()!.Energy >
                row.GetProperty("Before").Deserialize<BattleTurnState>()!.Energy) &&
            removals.Any(row => row.GetProperty("Placement").GetBoolean()),
            "Unspent deployment/ordinary energy did not accumulate in native turns.");
        foreach (FixtureValue row in actions)
        {
            BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            BattleTurnState actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            Require(before.Spawn.Turn >= 2 && before.Energy > actual.Energy && actual.Energy > 0,
                "The state-based conservation policy did not play positive-cost cards while retaining energy.");
        }
        BattleTurnState branch = actions[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        PlayCardAction play = actions[0].GetProperty("Action").Deserialize<PlayCardAction>()!;
        string parent = Serialize(branch);
        BattleActionResult paid = BattleActionModel.PlayCard(branch, play);
        BattleTurnResult bank = BattleTurnModel.EndTurn(branch);
        Require(paid.Supported && bank.Supported, "A captured native conserve decision cannot branch.");
        BattleTurnResult spend = BattleTurnModel.EndTurn(paid.State!);
        Require(spend.Supported && bank.State!.Energy > spend.State!.Energy && Serialize(branch) == parent,
            "Paid/banked branches lost distinct retained energy or mutated their captured source.");
        Console.WriteLine($"ORIGINAL-CONSERVE-EMBER-CHECKS PASS: {removals.Length} native conservation queries, {turns.Length} complete turns, {actions.Length} paid plays, retained terminal energy and independent paid/banked branches.");
    }

    private static CombatRelicState Owner(string id = DataId, bool disallowed = false, IReadOnlyList<RelicConditionState>? conditions = null) =>
        new(id, id == DataId ? "ConserveEmber" : id, [RelicResourceModel.ConserveEnergyEndOfTurn], disallowedInPlacementPhase: disallowed,
            resourceEffects: [new(0, RelicResourceModel.ConserveEnergyEndOfTurn, 0, 0, 0, false, conditions: conditions)]);

    private static CombatContext Context(IReadOnlyList<CombatRelicState> relics, int energy = 8, int next = 0, int every = 0) =>
        new(new([], [], [], UnityRng.Seed(439), 0, []), UnityRng.Seed(439), 0, 1, 10,
            statistics: BattleStatistics.Empty(deckCards: []).WithEndTurnEnergy(2), otherPiles: [],
            queryFrame: new(energy: energy, runningCombat: true, turn: 1), energyState: new(99, next, every, "MonsterTurn", true),
            relics: relics, resources: new(3, 1, 5, 99, 10, "pyre", 45, 80, 0, 0, [30], [20], []));

    private static BattleTurnState Decision(int energy, bool terminal, IReadOnlyList<CombatRelicState>? relics = null)
    {
        var rng = UnityRng.Seed(439);
        CardPileState[] piles = [new("Standby", []), new("Exhausted", []), new("Purged", []), new("Eaten", [])];
        var card = CardInstanceState.Empty(10, "paid");
        int turn = terminal ? 1 : 0;
        var context = new CombatContext(new([new(10, "paid")], [], [], rng, 0, []), rng, 0, 11, 10,
            statistics: BattleStatistics.Empty(deckCards: [10]).WithEndTurnEnergy(2), cardInstances: [card], cardRegistry: [card], otherPiles: piles,
            queryFrame: new(energy: energy, runningCombat: true, turn: turn, forgePoints: 0, dragonsHoard: 0, moonPhase: 1, pyreResurrectionCount: 0),
            energyState: new(99, 0, 0, "MonsterTurn", true), relics: relics ?? [Owner()],
            resources: new(3, 1, 5, 99, 10, "pyre", 45, 80, 0, 0, [30], [20], []));
        var actor = new CombatUnit(1, "actor", CombatTeam.Player, 10, 50, 50, true, false, false, []);
        var pyre = new CombatUnit(2, "pyre", CombatTeam.Player, 45, 80, 80, true, true, false, []);
        var boss = new CombatUnit(3, "boss", CombatTeam.Enemy, 0, 1, 1, true, false, true, [], isBoss: true);
        var train = new TrainCombatState([new(0, false, terminal ? [boss, actor] : [actor], [], context),
            new(1, false, [pyre], [], context)], [], 7, context);
        var spawn = new EnemySpawnState(train, [new([new([])])], [-1], 0, false, rng, 4, [], 0, false, 0, 0, turn, []);
        var rules = new BattlePlayRules([new(0, 5, 7, true, false, false), new(1, 0, 7, true, true, true)],
            [new("paid", "paid", 2, "Null", "Discard", null, [], cardType: "Spell", isAnyAbility: false)]);
        return new BattleTurnState(spawn, energy, 3, 5, 0, 0, "New", [new("Spawning", 439, rng)], piles, [], rules);
    }

    private static RelicResourceResult Query(CombatContext source, int remaining, bool placement = false, bool preview = false)
    {
        RelicResourceResult result = RelicResourceModel.GetEnergyToRemoveAtTurnEnd(source, remaining, placement, preview);
        Require(result.Supported, result.UnsupportedReason ?? "Conservation query failed.");
        return result;
    }
    private static string Serialize(object? value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
