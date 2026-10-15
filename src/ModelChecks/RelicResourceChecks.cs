using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class RelicResourceChecks
{
    internal static void Run()
    {
        RelicForgeConservationChecks.Run();
        RelicOpeningCorruptionChecks.Run();
        var once = new RelicConditionState(new("AnyCardPlayed"), true, 6, 1, false);
        var firstCombat = Context([Rule("first", RelicResourceModel.StartCombatEnergy, 2, disallowed: true, conditions: [once])], energy: 9);
        string firstFrozen = Serialize(firstCombat);
        var firstBonus = RelicResourceModel.ApplyEnergyTiming(firstCombat, startOfCombat: true);
        Require(firstBonus.Supported && firstBonus.Value == 10 && !firstBonus.Events.Single().Notified &&
            !firstBonus.Context!.Relics![0].ResourceEffects![0].Conditions[0].Triggered &&
            Serialize(firstCombat) == firstFrozen &&
            RelicResourceModel.ApplyEnergyTiming(firstCombat, placement: true, startOfCombat: true).Value == 9 &&
            RelicResourceModel.ApplyEnergyTiming(firstCombat, preview: true, startOfCombat: true).Value == 9 &&
            RelicResourceModel.StartTurn(firstCombat).Events.Count == 0,
            "Start-combat energy ignored caps, lifecycle/preview gates, no-notify semantics or input isolation.");
        Require(RelicResourceModel.ApplyEnergyTiming(Context([Rule("deployment-only", RelicResourceModel.StartCombatEnergy, 2)]), startOfCombat: true).Value == 0 &&
            RelicResourceModel.ApplyEnergyTiming(Context([Rule("deployment-only", RelicResourceModel.StartCombatEnergy, 2)]), placement: true, startOfCombat: true).Value == 2 &&
            RelicResourceModel.ApplyEnergyTiming(Context([Rule("first-loss", RelicResourceModel.StartCombatEnergy, -5, disallowed: true)], energy: 2), startOfCombat: true).Value == 0,
            "Start-of-battle placement equality gate or signed energy removal differs.");
        CombatContext ordered = Context([Rule("up", RelicResourceModel.Energy, 3), Rule("down", RelicResourceModel.Energy, -3)], energy: 9);
        RelicResourceResult capped = Timing(ordered);
        Require(capped.Value == 7 && capped.Events.Select(item => (item.Before, item.After)).SequenceEqual([(9, 10), (10, 7)]) &&
            Timing(Context(ordered.Relics!.Reverse().ToArray(), energy: 9)).Value == 9 &&
            GetEnergy(ordered).Value == 0,
            "Ordered native energy timing was replaced by a summed resource query or clamped only once.");
        Require(GetEnergy(Context([Rule("negative", RelicResourceModel.Energy, -3)])).Value == 0 &&
            GetEnergy(Context([Rule("negative", RelicResourceModel.DeploymentEnergy, -3)]), deployment: true).Value == -3,
            "Per-turn query lower clamp or signed deployment query differs.");

        CombatContext limited = Context([Rule("once", RelicResourceModel.Energy, 1, conditions: [once])]);
        string limitedParent = Serialize(limited);
        Require(GetEnergy(limited).Value == 1 && GetEnergy(limited).Value == 1 && Serialize(limited) == limitedParent,
            "A resource value query notified or recorded its outer conditions.");
        RelicResourceResult fired = Timing(limited);
        RelicConditionState recorded = fired.Context!.Relics![0].ResourceEffects![0].Conditions[0];
        Require(fired.Value == 1 && fired.Events.Single().Notified && recorded.Triggered && recorded.DurationTriggerCount == 1 &&
            Timing(fired.Context).Value == 1 && GetEnergy(fired.Context).Value == 0,
            "Energy timing failed to notify/record, or repeated a consumed once condition.");
        CombatContext reset = RelicConditionModel.EndDuration(fired.Context, "ThisTurn");
        Require(Timing(reset).Value == 2 && !reset.Relics![0].ResourceEffects![0].Conditions[0].Triggered &&
            ReferenceEquals(reset.Resources, limited.Resources), "Resource duration rollover lost conditions or raw inputs.");
        RelicResourceResult deployed = Timing(Context([Rule("deploy-once", RelicResourceModel.DeploymentEnergy, 1,
            conditions: [once])]), deployment: true);
        Require(deployed.Value == 1 && !deployed.Events.Single().Notified &&
            !deployed.Context!.Relics![0].ResourceEffects![0].Conditions[0].Triggered &&
            Timing(deployed.Context, deployment: true).Value == 2,
            "Deployment energy acquired the ordinary effect's notification/record behavior.");
        Require(Timing(Context([Rule("preview-block", RelicResourceModel.Energy, 2)]), preview: true).Value == 0 &&
            Timing(Context([Rule("preview-allow", RelicResourceModel.Energy, 2, canPreview: true)]), preview: true).Value == 2 &&
            Timing(Context([Rule("placement-block", RelicResourceModel.Energy, 2, disallowed: true)]), placement: true).Value == 0 &&
            GetEnergy(Context([Rule("placement-query", RelicResourceModel.Energy, 2, disallowed: true)])).Value == 2,
            "Resource preview, timing placement or ungated value-query semantics differ.");
        var spent = once.Recorded(0);
        Require(Timing(Context([Rule("spent", RelicResourceModel.Energy, 2, conditions: [spent])])).Value == 0 &&
            Timing(Context([Rule("zero", RelicResourceModel.Energy, 0, conditions: [once])])).Events.Count == 0,
            "Failed conditions or a zero timing amount still applied/notified.");
        Require(!RelicResourceModel.ApplyEnergyTiming(Context([Rule("min", RelicResourceModel.Energy, int.MinValue)])).Supported,
            "The native Math.Abs(Int32.MinValue) removal failure became a guessed energy state.");

        CombatRelicState[] incomes = [Rule("regular", RelicResourceModel.Energy, 4), Rule("deployment", RelicResourceModel.DeploymentEnergy, 4)];
        CombatContext start = Context(incomes, next: 2, every: 1);
        RelicResourceResult regular = Start(start);
        RelicResourceResult deployment = Start(start, deployment: true);
        Require(regular.Value == 10 && regular.Context!.EnergyState!.NextTurn == 0 && regular.Context.EnergyState.EveryTurn == 1 &&
            deployment.Value == 10 && deployment.Events.Select(item => (item.Before, item.After)).SequenceEqual([(7, 10), (10, 10)]) &&
            Start(regular.Context.WithQueryFrame(regular.Context.QueryFrame!.With(energy: 0))).Value == 8,
            "Base/deployment income, one-shot ledger reset, every-turn ledger or ordered relic timing differs.");
        Require(Start(Context([], raw: Raw(deploymentExtra: -5)), deployment: true).Value == -2 &&
            Start(Context([], next: -5)).Value == 0,
            "Deployment incorrectly lower-clamped signed AddEnergy, or ordinary nonpositive income was added.");

        RelicResourceEffect fullMoon = new(0, RelicResourceModel.MoonPhaseEnergy, 1, 2, 1, false, conditions: [once]);
        CombatContext moonSource = Context([Owner("full-moon", fullMoon)]);
        RelicResourceResult skippedNew = RelicResourceModel.ApplyMoonPhaseEnergy(moonSource, "New");
        RelicResourceResult gainedFull = RelicResourceModel.ApplyMoonPhaseEnergy(skippedNew.Context!, "Full");
        RelicResourceResult duplicateFull = RelicResourceModel.ApplyMoonPhaseEnergy(gainedFull.Context!, "Full");
        RelicResourceResult nextTurnFull = RelicResourceModel.ApplyMoonPhaseEnergy(
            RelicConditionModel.EndDuration(duplicateFull.Context!, "ThisTurn"), "Full");
        Require(skippedNew.Supported && skippedNew.Value == 0 && skippedNew.Events.Count == 0 &&
            gainedFull.Supported && gainedFull.Value == 1 && gainedFull.Events.Single().Notified &&
            gainedFull.Events.Single().Before == 0 && gainedFull.Events.Single().After == 1 &&
            gainedFull.Context!.Relics![0].ResourceEffects![0].Conditions[0].Triggered &&
            gainedFull.Context.Relics[0].ResourceEffects![0].Conditions[0].DurationTriggerCount == 1 &&
            duplicateFull.Supported && duplicateFull.Value == 1 && duplicateFull.Events.Count == 0 &&
            nextTurnFull.Supported && nextTurnFull.Value == 2 && nextTurnFull.Events.Count == 1 &&
            GetEnergy(moonSource).Value == 0,
            "Moon-phase energy did not honor phase masks, once-per-turn trigger recording, next-turn reset or the separate static energy query.");
        RelicResourceEffect newMoonLoss = new(0, RelicResourceModel.MoonPhaseEnergy, -2, 1, -2, false);
        RelicResourceResult removedMoonEnergy = RelicResourceModel.ApplyMoonPhaseEnergy(
            Context([Owner("new-moon", newMoonLoss)], energy: 1), "New");
        RelicResourceResult cappedMoonEnergy = RelicResourceModel.ApplyMoonPhaseEnergy(
            Context([Owner("full-moon-cap", new(0, RelicResourceModel.MoonPhaseEnergy, 3, 2, 3, false))], energy: 9), "Full");
        Require(removedMoonEnergy.Supported && removedMoonEnergy.Value == 0 &&
            cappedMoonEnergy.Supported && cappedMoonEnergy.Value == 10 && cappedMoonEnergy.Events.Single().After == 10 &&
            !RelicResourceModel.ApplyMoonPhaseEnergy(Context([Owner("preview-moon",
                new(0, RelicResourceModel.MoonPhaseEnergy, 1, 2, 1, false))]), "Full", preview: true).Events.Any(),
            "Moon-phase energy ignored native zero/max clamps or preview restrictions.");

        CombatRelicState[] mutations = [Rule("draw-later", RelicResourceModel.Draw, 2, order: 1),
            Rule("draw-earlier", RelicResourceModel.Draw, 1, order: 0, conditions: [spent], disallowed: true),
            Rule("draw-not-installed", RelicResourceModel.Draw, 99)];
        CombatContext drawRoot = Context(mutations);
        Require(Draw(drawRoot).Value == 8 && ReferenceEquals(Draw(drawRoot).Context, drawRoot),
            "MutableRules re-evaluated outer conditions, lost registrations or applied an unregistered mutation.");
        CombatContext highDraw = Context([Rule("draw-cap", RelicResourceModel.Draw, 6, order: 0)]);
        CardToken[] deck = Enumerable.Range(1, 15).Select(id => new CardToken(id, "card")).ToArray();
        var pending = new CardCycleState([], deck, [], UnityRng.Seed(13), -5, [], new BonusDrawState([], []));
        CardCycleResult hand = CardCycleModel.DrawHand(pending, Draw(highDraw).Value, highDraw.MaxHandSize);
        Require(hand.Supported && hand.State!.Hand.Count == 5 && hand.State.DrawModifier == 0 &&
            Draw(Context([Rule("draw-floor", RelicResourceModel.Draw, -50, order: 0)])).Value == 1,
            "Save's draw clamp was merged with pending draw modifiers or the pending ledger failed to reset.");
        var floatRule = new RelicResourceEffect(0, RelicResourceModel.Draw, 16777217, 0, 16777216, false,
            drawValue: (float)16777217, drawMutationOrder: 0);
        Require(RelicResourceModel.GetDrawCount(Context([Owner("float", floatRule)])).Supported &&
            !RelicResourceModel.GetDrawCount(Context([Owner("bad-float", new(0, RelicResourceModel.Draw,
                16777217, 0, 16777217, false, drawValue: (float)16777217, drawMutationOrder: 0))])).Supported,
            "Draw initialization skipped native single-precision conversion before integer rounding.");

        var attack = Owner("attack", new(0, RelicResourceModel.PyreAttack, 999, 1, 10, false));
        var health = Owner("health", new(0, RelicResourceModel.PyreHealth, 999, 1, 20, false, conditions: [spent]));
        CombatContext pyreRoot = Context([attack, health]);
        RelicPyreResourceResult born = RelicResourceModel.InitializePyre(pyreRoot, 90);
        Require(born.Supported && born.Attack == 30 && born.Health == 90 && born.MaxHealth == 100 &&
            RelicResourceModel.GetPyreMaxHealth(pyreRoot).Value == 100,
            "Pyre initialization used ParamInt instead of its initialized upgrade index/cache or evaluated max-HP outer conditions.");
        RelicPyreResourceResult skippedAcquire = RelicResourceModel.Acquire(pyreRoot, 1, 90);
        RelicPyreResourceResult acquired = RelicResourceModel.Acquire(Context([attack, Rule("heal", RelicResourceModel.PyreHealth, 20, index: 1)]), 1, 90);
        Require(skippedAcquire.Supported && skippedAcquire.Health == 90 && acquired.Supported && acquired.Health == 100 &&
            acquired.MaxHealth == 100 && !acquired.Context!.Relics![1].ResourceEffects![0].Conditions.Any() &&
            Start(acquired.Context).Context!.Resources!.BasePyreMaxHealth == 80,
            "Acquire failed its condition/cap or reapplied persistent Pyre health during the future turn.");
        var blockedAttack = Owner("required", new(0, RelicResourceModel.PyreAttack, 0, 1, 10, false,
            requiredMutatorId: "required", conditions: [once]));
        var blockedHealth = Owner("required-hp", new(0, RelicResourceModel.PyreHealth, 0, 1, 20, false, requiredMutatorId: "required"));
        RelicResourceResult unmatched = RelicResourceModel.GetPyreAttack(Context([blockedAttack]), notify: true);
        Require(unmatched.Supported && unmatched.Value == 0 && !unmatched.Context!.Relics![0].ResourceEffects![0].Conditions[0].Triggered &&
            RelicResourceModel.GetPyreMaxHealth(Context([blockedHealth])).Value == 80 &&
            RelicResourceModel.GetPyreAttack(Context([blockedAttack], raw: Raw(mutators: ["required"]))).Value == 30,
            "Native required-mutator failure failed to replace attack with zero, notified despite returning early, or added health.");

        CombatContext withoutOwners = pyreRoot.WithRelics([]);
        CombatUnit cachedPyre = new(1, "pyre", CombatTeam.Player, born.Attack, born.Health, born.MaxHealth,
            true, true, false, [], modifiers: new(born.Attack, 0, 0, 1, 1, true, false, [], damageBuffFromStatusEffects: 0));
        RoomCombatResult preserved = RoomCombatModel.Resolve(new(1, false, [cachedPyre], [], withoutOwners));
        Require(preserved.Supported && preserved.State!.Units[0].BaseAttack == 30 && preserved.State.Units[0].MaxHealth == 100 &&
            RelicResourceModel.InitializePyre(withoutOwners, 90).Attack == 20,
            "Removing an initialization owner retroactively rewrote an existing native Pyre cache.");
        CombatContext savedModifier = Context([], raw: Raw(modifiedAttack: 2));
        Require(RelicResourceModel.GetPyreAttack(savedModifier).Supported &&
            !RoomCombatModel.Resolve(new(1, false, [cachedPyre], [], savedModifier)).Supported,
            "Pure birth queries enabled an unmodeled live saved Pyre attack modifier.");

        Guards();
        CheckCopiesAndPlay();
        string parent = Serialize(ordered), drawParent = Serialize(drawRoot), pyreParent = Serialize(pyreRoot);
        string expected = Serialize(capped);
        Parallel.For(0, 32, _ =>
        {
            Require(Serialize(Timing(ordered)) == expected && Draw(drawRoot).Value == 8 &&
                RelicResourceModel.InitializePyre(pyreRoot, 90).Attack == 30 &&
                Start(start, deployment: true).Value == 10, "Parallel resource initialization/timing/draw branches diverged.");
        });
        Require(Serialize(ordered) == parent && Serialize(drawRoot) == drawParent && Serialize(pyreRoot) == pyreParent &&
            capped.Context!.BattleRng.Equals(ordered.BattleRng), "Resource branches mutated parents or consumed gameplay RNG.");
        Console.WriteLine("RELIC-RESOURCE-CHECKS PASS: ordered timing caps, signed query/deployment income, preview/placement/conditions/notification, ledger rollover, installed float draw rules and separate clamps, upgrade-table Pyre initialization/acquisition/cache lifetimes, metadata/copy guards, complete card play and 32 immutable branches.");
    }

    private static void Guards()
    {
        var absent = new CombatRelicState("absent", "absent", [RelicResourceModel.Energy]);
        var unexpected = new CombatRelicState("unexpected", "unexpected", [], resourceEffects: [new(0, RelicResourceModel.Energy, 1, 0, 1, false)]);
        var wrongType = new CombatRelicState("wrong", "wrong", [RelicResourceModel.Energy], disallowedInPlacementPhase: false,
            resourceEffects: [new(0, RelicResourceModel.DeploymentEnergy, 1, 0, 1, false)]);
        foreach (CombatRelicState relic in new[] { absent, unexpected, wrongType })
            Require(!RelicResourceModel.GetEnergyModification(Context([relic])).Supported, "Malformed resource metadata became a guessed result.");
        Require(!RelicResourceModel.GetEnergyModification(Context([Rule("amount", RelicResourceModel.Energy, 1, amount: 2)])).Supported &&
            !RelicResourceModel.GetPyreAttack(Context([Owner("bad-cache", new(0, RelicResourceModel.PyreAttack, 10, 1, 999, false))])).Supported &&
            !RelicResourceModel.GetDrawCount(Context([Rule("dup-a", RelicResourceModel.Draw, 1, order: 0),
                Rule("dup-b", RelicResourceModel.Draw, 1, order: 0)])).Supported &&
            !RelicResourceModel.GetEnergyModification(new(new([], [], [], UnityRng.Seed(13), 0, []), UnityRng.Seed(13), 0, 1, 10,
                relics: [Rule("missing-input", RelicResourceModel.Energy, 1)])).Supported,
            "Mismatched initialized payload, duplicate registration or missing raw resource inputs were accepted.");
        Require(!RelicResourceModel.GetEnergyModification(Context([new("unknown", "unknown", ["UnknownResourceEffect"])] )).Supported,
            "Adding resources bypassed the unknown native owner/effect guard.");
        Require(!RelicResourceModel.ApplyMoonPhaseEnergy(Context([new("bad-moon-mask", "bad-moon-mask",
                [RelicResourceModel.MoonPhaseEnergy], disallowedInPlacementPhase: false,
                resourceEffects: [new(0, RelicResourceModel.MoonPhaseEnergy, 1, 8, 1, false)])]), "Full").Supported &&
            !RelicResourceModel.ApplyMoonPhaseEnergy(Context([Owner("moon-minimum", new(0,
                RelicResourceModel.MoonPhaseEnergy, int.MinValue, 1, int.MinValue, false))]), "New").Supported &&
            !RelicResourceModel.ApplyMoonPhaseEnergy(Context([]), "None").Supported,
            "Invalid moon masks, minimum-int removal or unsupported phase transitions became guessed results.");
        CombatRelicState badCondition = Rule("bad-query", RelicResourceModel.Energy, 1,
            conditions: [new(new("NotCaptured"), false, 2, 0, true)]);
        Require(!RelicResourceModel.GetEnergyModification(Context([badCondition])).Supported &&
            !RelicResourceModel.ApplyEnergyTiming(Context([badCondition])).Supported,
            "Unsupported outer resource conditions produced a partial child state.");
    }

    private static void CheckCopiesAndPlay()
    {
        CombatContext source = Context([Rule("energy", RelicResourceModel.Energy, 1)]);
        CombatRelicState owner = source.Relics![0];
        foreach (CombatRelicState copy in new[] { owner.WithSpawnStatuses([]), owner.WithCardModifiers([]), owner.WithTriggerAdditions([]),
            owner.WithStatusStackAdditions([]), owner.WithStatusMagnitudes([]), owner.WithResourceEffects(owner.ResourceEffects!) })
            Require(copy.ResourceEffects!.Single().Amount == 1, "Another relic family discarded resource metadata.");
        foreach (CombatContext copy in new[] { source.WithGold(5), source.WithCards(source.Cards), source.WithRelics(source.Relics),
            source.WithEnergyState(source.EnergyState!), source.WithBattleRng(source.BattleRng), source.WithStatistics(source.Statistics),
            source.AfterCardEffects(), source.WithOtherPiles([]), source.WithQueryFrame(source.QueryFrame) })
            Require(ReferenceEquals(copy.Resources, source.Resources), "A shared runtime ledger copy discarded raw resource inputs.");
        var rng = UnityRng.Seed(13);
        var card = CardInstanceState.Empty(1, "free");
        CardPileState[] piles = [new("Standby", []), new("Exhausted", []), new("Purged", []), new("DiscardBuffer", [])];
        var context = new CombatContext(new([new(1, "free")], [], [], rng, 0, []), rng, 0, 2, 10,
            statistics: BattleStatistics.Empty(deckCards: [1]), cardInstances: [card], cardRegistry: [card], otherPiles: piles,
            queryFrame: new(energy: 3, runningCombat: true, turn: 1, forgePoints: 0, dragonsHoard: 0, moonPhase: 1, pyreResurrectionCount: 0),
            energyState: new(10, 2, 1, "MonsterTurn", true), relics: source.Relics, resources: source.Resources);
        var pyre = new CombatUnit(1, "pyre", CombatTeam.Player, 20, 80, 80, true, true, false, []);
        var train = new TrainCombatState([new(0, false, [], [], context), new(1, false, [pyre], [], context)], [], 7, context);
        var spawn = new EnemySpawnState(train, [new EnemyWave([new EnemyGroup([])])], [-1], 0, false, rng, 2, [], 0, false, 2, 0, 1, []);
        var root = new BattleTurnState(spawn, 3, 3, 5, 0, 0, "New", [new("Spawning", 13, rng)], piles, [],
            new([new(0, 5, 7, true, false, false), new(1, 0, 7, true, true, true)],
                [new("free", "free", 0, "Null", "Discard", null, [], cardType: "Spell", isAnyAbility: false)]), selectedRoom: 0);
        string parent = Serialize(root);
        BattleActionResult played = BattleActionModel.PlayCard(root, new(1, 0));
        Require(played.Supported && ReferenceEquals(played.State!.Spawn.Train.Context!.Resources, context.Resources) &&
            played.State.Spawn.Train.Context.EnergyState!.NextTurn == 2 && played.State.Spawn.Train.Context.EnergyState.EveryTurn == 1 &&
            played.State.Spawn.Train.Context.Cards.Discard.Single().InstanceId == 1 && Serialize(root) == parent,
            "A complete card action lost resource metadata or pending energy while routing its card.");
    }

    private static BattleResourceState Raw(int deploymentExtra = 1, IReadOnlyList<string>? mutators = null, int modifiedAttack = 0) =>
        new(3, deploymentExtra, 5, 10, 10, "pyre", 20, 80, 0, modifiedAttack, [5, 10], [10, 20], mutators ?? []);
    private static CombatRelicState Owner(string id, RelicResourceEffect rule, bool disallowed = false) =>
        new(id, id, [rule.Type], disallowedInPlacementPhase: disallowed, resourceEffects: [rule]);
    private static CombatRelicState Rule(string id, string type, int value, int index = 0, bool canPreview = false,
        IReadOnlyList<RelicConditionState>? conditions = null, bool disallowed = false, int? order = null, int? amount = null) =>
        Owner(id, new(0, type, value, index, amount ?? value, canPreview, conditions: conditions,
            drawValue: type == RelicResourceModel.Draw ? (float?)value : null, drawMutationOrder: order), disallowed);
    private static CombatContext Context(IReadOnlyList<CombatRelicState> relics, int energy = 0, int next = 0, int every = 0,
        BattleResourceState? raw = null) => new(new([], [], [], UnityRng.Seed(13), 0, []), UnityRng.Seed(13), 0, 1, 10,
            statistics: BattleStatistics.Empty(deckCards: []), otherPiles: [], queryFrame: new(energy: energy, runningCombat: true, turn: 1),
            energyState: new(10, next, every, "MonsterTurn", true), relics: relics, resources: raw ?? Raw());
    private static RelicResourceResult Timing(CombatContext source, bool deployment = false, bool preview = false, bool placement = false)
    { RelicResourceResult result = RelicResourceModel.ApplyEnergyTiming(source, deployment, preview, placement); Require(result.Supported, result.UnsupportedReason ?? "Resource timing failed."); return result; }
    private static RelicResourceResult Start(CombatContext source, bool deployment = false)
    { RelicResourceResult result = RelicResourceModel.StartTurn(source, deployment); Require(result.Supported, result.UnsupportedReason ?? "Resource turn failed."); return result; }
    private static RelicResourceResult GetEnergy(CombatContext source, bool deployment = false)
    { RelicResourceResult result = RelicResourceModel.GetEnergyModification(source, deployment); Require(result.Supported, result.UnsupportedReason ?? "Resource query failed."); return result; }
    private static RelicResourceResult Draw(CombatContext source)
    { RelicResourceResult result = RelicResourceModel.GetDrawCount(source); Require(result.Supported, result.UnsupportedReason ?? "Draw mutation query failed."); return result; }
    private static string Serialize(object? value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
