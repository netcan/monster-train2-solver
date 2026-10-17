using System;
using System.Linq;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RelicMonsterSpellTargetingChecks
{
    private const string SpellId = "monster-spread-spell";
    private const string NativeWitnessSpellId = "d9042b99-f328-4ae9-8995-1bb242a970c4";
    private const string Effect = "RelicEffectMonsterSpellSingleToTeamTargeting";

    internal static void Run()
    {
        var condition = new RelicConditionState(new CardStatisticQuery("SubtypeInDeck", "ThisTurn"),
            true, 6, 1, false);
        var relic = new CombatRelicState(RelicMonsterSpellTargetingModel.RelicId,
            RelicMonsterSpellTargetingModel.RelicKey, [Effect], monsterSpellTargetingModifiers: [
                new RelicMonsterSpellTargetingModifier(0, [condition])]);
        Require(RelicModel.Validate([relic]) == null, "The exact native Monster spell-targeting relic was rejected.");

        UnityRng rng = UnityRng.Seed(404);
        var descriptor = new CardMaskDescriptor(new CardMaskDefinition(SpellId, "Spell", "Common", false,
            false, false, [], [], [], [], null, false, 0, 0, false, false, false), 0, [], []);
        var card = new CardInstanceState(1, SpellId, CardModifiers.Empty(), CardModifiers.Empty(), 0, 0, 0, [],
            maskDescriptor: descriptor);
        var damage = new CardActionEffect("Damage", "DropTargetCharacter", 1, false, true, []);
        var rule = new CardPlayRule(SpellId, "MonsterSpreadSpell", 0, "Spell", "Discard", null, [], [damage],
            cardType: "Spell", isAnyAbility: false);
        var definitions = new BattlePlayRules([new RoomPlayRule(0, 5, 5, true, false, false, 5),
            new RoomPlayRule(1, 0, 0, true, false, true, 5)], [rule]);

        CombatUnit Unit(int id, CombatTeam team, int health, params CombatStatus[] statuses) =>
            new(id, "monster-spread-unit-" + id, team, 0, health, health, true, false, false, statuses, size: 1);
        CombatContext Context()
        {
            var cards = new CardCycleState([new CardToken(1, SpellId)], [], [], rng, 0, []);
            return new CombatContext(cards, rng, 0, 2, 10, cardInstances: [card], cardRegistry: [card], relics: [relic]);
        }
        TrainCombatState Train(CombatContext context, params CombatUnit[] units) =>
            new([new RoomCombatState(0, false, units, [], context),
                new RoomCombatState(1, false, [new CombatUnit(99, "monster-spread-pyre", CombatTeam.Player, 1,
                    80, 80, true, true, false, [])], [], context)], [], 5, context);

        CombatUnit[] roster = [Unit(20, CombatTeam.Player, 8), Unit(21, CombatTeam.Player, 8),
            Unit(22, CombatTeam.Player, 8, new CombatStatus("untouchable", 1)), Unit(23, CombatTeam.Player, 8),
            Unit(30, CombatTeam.Enemy, 8)];
        CombatContext root = Context();
        TrainCombatState before = Train(root, roster);
        RelicMonsterSpellTargetingResult first = RelicMonsterSpellTargetingModel.Apply(before, 0, 0,
            rule, card.InstanceId, 0, [20], CombatTeam.Player, definitions, 1);
        Require(first.Supported && first.CopiedTargets == 2 && first.State.Rooms.Single(room => room.RoomIndex == 0).Units.Single(unit => unit.Id == 20).Health == 8 &&
            first.State.Rooms.Single(room => room.RoomIndex == 0).Units.Single(unit => unit.Id == 21).Health == 7 &&
            first.State.Rooms.Single(room => room.RoomIndex == 0).Units.Single(unit => unit.Id == 22).Health == 8 &&
            first.State.Rooms.Single(room => room.RoomIndex == 0).Units.Single(unit => unit.Id == 23).Health == 7 &&
            first.State.Context!.Relics![0].MonsterSpellTargetingModifiers![0].Conditions[0].Triggered,
            "The copied spell did not hit only remaining touchable Monster teammates and record its per-turn trigger: supported=" +
            first.Supported + ", copied=" + first.CopiedTargets + ", hp=" + string.Join(",", first.State.Rooms.Single(room => room.RoomIndex == 0).Units.Select(unit => unit.Id + ":" + unit.Health)) +
            ", recorded=" + first.State.Context!.Relics![0].MonsterSpellTargetingModifiers![0].Conditions[0].Triggered +
            ", reason=" + first.UnsupportedReason);

        RelicMonsterSpellTargetingResult sameTurn = RelicMonsterSpellTargetingModel.Apply(first.State, 0, 0,
            rule, card.InstanceId, 0, [20], CombatTeam.Player, definitions, 1);
        Require(sameTurn.Supported && sameTurn.CopiedTargets == 0 &&
            sameTurn.State.Rooms.Single(room => room.RoomIndex == 0).Units.Single(unit => unit.Id == 21).Health == 7,
            "The relic repeated its copied effects after its once-per-turn condition was recorded.");

        CombatContext resetContext = RelicConditionModel.EndDuration(first.State.Context!, "ThisTurn");
        TrainCombatState reset = Train(resetContext, first.State.Rooms.Single(room => room.RoomIndex == 0).Units.ToArray());
        RelicMonsterSpellTargetingResult nextTurn = RelicMonsterSpellTargetingModel.Apply(reset, 0, 0,
            rule, card.InstanceId, 0, [20], CombatTeam.Player, definitions, 2);
        Require(nextTurn.Supported && nextTurn.CopiedTargets == 2 &&
            nextTurn.State.Rooms.Single(room => room.RoomIndex == 0).Units.Single(unit => unit.Id == 21).Health == 6,
            "The ThisTurn condition did not reset for a later turn.");

        CardActionEffect enemyDamage = new("Damage", "DropTargetCharacter", 1, true, false, []);
        CardPlayRule enemyRule = new(SpellId, "EnemyControl", 0, "Spell", "Discard", null, [], [enemyDamage],
            cardType: "Spell", isAnyAbility: false);
        RelicMonsterSpellTargetingResult wrongTeam = RelicMonsterSpellTargetingModel.Apply(before, 0, 0,
            enemyRule, card.InstanceId, 0, [30], CombatTeam.Enemy, definitions, 1);
        Require(wrongTeam.Supported && wrongTeam.CopiedTargets == 0 &&
            wrongTeam.State.Context!.Relics![0].MonsterSpellTargetingModifiers![0].Conditions[0].DurationTriggerCount == 0,
            "A Heroes-targeting control incorrectly triggered the Monster spell relic.");

        CombatUnit[] alone = [Unit(20, CombatTeam.Player, 8)];
        RelicMonsterSpellTargetingResult noTeammates = RelicMonsterSpellTargetingModel.Apply(Train(Context(), alone),
            0, 0, rule, card.InstanceId, 0, [20], CombatTeam.Player, definitions, 1);
        Require(noTeammates.Supported && noTeammates.CopiedTargets == 0 &&
            noTeammates.State.Context!.Relics![0].MonsterSpellTargetingModifiers![0].Conditions[0].DurationTriggerCount == 0,
            "The relic consumed its per-turn trigger without any other Monster targets.");

        Require(relic.WithStatusMagnitudes([]).MonsterSpellTargetingModifiers?.Single().Conditions[0].Triggered == false,
            "Updating another relic payload discarded its Monster spell-targeting definition.");
        CombatRelicState missing = new(RelicMonsterSpellTargetingModel.RelicId,
            RelicMonsterSpellTargetingModel.RelicKey, [Effect]);
        Require(RelicModel.Validate([missing]) != null, "An uncaptured Monster spell-targeting condition passed validation.");
        Console.WriteLine("RELIC-MONSTER-SPELL-TARGETING-CHECKS PASS: direct Monster target, remaining teammate fan-out, Untouchable/original-target gates, once-per-turn reset, no-target and wrong-team controls.");
    }

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 188 &&
            root.GetProperty("OriginalMonsterSpellTargetingPrepared").GetBoolean() &&
            root.GetProperty("ModifierScenario").GetString() == "original-monster-spell-targeting" &&
            root.GetProperty("NativeWon").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }
                .All(name => root.GetProperty(name).GetInt32() == 0) &&
            root.GetProperty("Actions").EnumerateArray().All(entry => entry.GetProperty("Difference").ValueKind == FixtureKind.Null),
            "The original SingleToTeamMonsterSpellTargeting battle is incomplete or has native/model failures.");

        FixtureValue[] scenarioActions = root.GetProperty("Actions").EnumerateArray()
            .Where(entry => entry.GetProperty("ScenarioAction").GetBoolean()).ToArray();
        Require(scenarioActions.Length == 1, "The native Monster spell witness must be played exactly once.");
        FixtureValue entry = scenarioActions[0];
        BattleTurnState before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = entry.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction action = entry.GetProperty("Action").Deserialize<PlayCardAction>()!;
        CombatContext source = before.Spawn.Train.Context!;
        CombatContext result = actual.Spawn.Train.Context!;
        CombatUnit? originalTarget = before.Spawn.Train.Rooms.Single(room => room.RoomIndex == action.RoomIndex).Units
            .SingleOrDefault(unit => unit.Id == action.TargetUnitId);
        CardInstanceState? played = source.FindCard(action.CardInstanceId);
        CardPlayRule? playedRule = before.PlayRules!.Cards.SingleOrDefault(rule => rule.DataId == played?.DataId);
        CombatRelicState owner = source.Relics!.Single(relic => relic.DataId == RelicMonsterSpellTargetingModel.RelicId);
        RelicMonsterSpellTargetingModifier modifier = owner.MonsterSpellTargetingModifiers!.Single();
        RelicConditionState beforeCondition = modifier.Conditions.Single();
        RelicConditionState afterCondition = result.Relics!.Single(relic => relic.DataId == owner.DataId)
            .MonsterSpellTargetingModifiers!.Single().Conditions.Single();
        if (originalTarget == null || played == null || played.MaskDescriptor == null || playedRule == null)
            throw new InvalidOperationException("The native witness lost its target, owned-card mask or card-play rule.");
        Require(action.TargetUnitId > 0 && originalTarget.Team == CombatTeam.Player && !originalTarget.IsPyre &&
            played.DataId == NativeWitnessSpellId && played.MaskDescriptor.Definition.DataId == played.DataId &&
            played.MaskDescriptor.Definition.CardType == "Spell" && played.MaskDescriptor.BaseTraits.Count == 0 &&
            played.Permanent.Upgrades.Count == 0 && played.Temporary.Upgrades.Count == 0 && played.CanBeCopied == true &&
            playedRule.Effect == "Spell" && playedRule.CardType == "Spell" && playedRule.IsAnyAbility == false &&
            playedRule.Ability == null && playedRule.Effects.Count == 1 && playedRule.OnCastEffects.Count == 0 &&
            playedRule.Effects[0].Type == "Damage" && playedRule.Effects[0].Target == "DropTargetCharacter" &&
            playedRule.Effects[0].Value == 1 && !playedRule.Effects[0].AllowEnemy && playedRule.Effects[0].AllowPlayer &&
            before.PlayRules.Rooms.Single(room => room.RoomIndex == action.RoomIndex).IsPyre == false,
            "The native action is not the supported direct, fixed one-damage Monster-target Spell path.");
        Require(owner.AssetKey == RelicMonsterSpellTargetingModel.RelicKey && owner.EffectTypes.SequenceEqual([Effect]) &&
            modifier.EffectIndex == 0 && beforeCondition.Query.Type == "SubtypeInDeck" &&
            beforeCondition.Query.Duration == "ThisTurn" && beforeCondition.TrackTriggerCount &&
            beforeCondition.Comparator == 6 && beforeCondition.Value == 1 && !beforeCondition.AllowMultiple &&
            !beforeCondition.Triggered && beforeCondition.DurationTriggerCount == 0 &&
            afterCondition.Triggered && afterCondition.DurationTriggerCount == 1 &&
            RelicModel.Validate([owner]) == null,
            "The captured original owner or its once-per-turn native condition changed.");

        RoomCombatState beforeRoom = before.Spawn.Train.Rooms.Single(room => room.RoomIndex == action.RoomIndex);
        RoomCombatState afterRoom = actual.Spawn.Train.Rooms.Single(room => room.RoomIndex == action.RoomIndex);
        int[] copiedTargets = beforeRoom.Units.Where(unit => unit.Team == CombatTeam.Player && !unit.IsPyre &&
            unit.Health > 0 && unit.Id != action.TargetUnitId && unit.Status("untouchable") == null &&
            unit.DeathState?.IsBeingRemoved != true && unit.DeathState?.IsDestroyed != true &&
            unit.DeathState?.IsDespawned != true).Select(unit => unit.Id).ToArray();
        Require(copiedTargets.Length > 0 && originalTarget.Status("untouchable") == null,
            "The native witness did not have a legal original Monster target and a remaining teammate.");

        var hitIds = copiedTargets.Append(action.TargetUnitId).ToHashSet();
        foreach (CombatUnit prior in beforeRoom.Units)
        {
            CombatUnit after = afterRoom.Units.Single(unit => unit.Id == prior.Id);
            if (!hitIds.Contains(prior.Id))
            {
                Require(after.Health == prior.Health,
                    "The direct/copy spell damaged an ineligible teammate, Untouchable, or Heroes unit " + prior.Id + ".");
                continue;
            }
            if (prior.Status("armor")?.Stacks > 0 || prior.Status("spell shield")?.Stacks > 0 ||
                prior.Status("damage shield")?.Stacks > 0 || prior.Status("titanskin")?.Stacks > 0)
                continue;
            Require(after.Health == prior.Health - 1,
                "The original/copy spell did not apply exactly one damage to eligible Monster " + prior.Id + ".");
        }
        Console.WriteLine("ORIGINAL-MONSTER-SPELL-TARGETING NATIVE PASS: unchanged owner, one direct Monster Spell, " +
            copiedTargets.Length + " eligible teammate copies, original/Untouchable/team controls and zero error counters.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
