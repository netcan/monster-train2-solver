using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public static class UnitAbilityModel
    {
        public static BattleActionResult Activate(BattleTurnState source, PlayCardAction action)
        {
            TrainCombatState train = source.Spawn.Train;
            CombatContext? context = train.Context;
            if (context == null || source.PlayRules == null) return Unsupported("Missing ability battle context or definitions.");
            if (source.ExternalInteractions.Count > 0 || source.Spawn.ExternalInteractions.Count > 0)
                return Unsupported(string.Join("; ", source.ExternalInteractions.Concat(source.Spawn.ExternalInteractions)));
            if (source.PlayRules.Cards.Select(rule => rule.DataId).Distinct().Count() != source.PlayRules.Cards.Count)
                return Unsupported("Duplicate ability play definitions.");
            if (action.PlayerPosition != -1 || action.TargetUnitId != 0) return Illegal("An ability supplies its activator as self, without a card drop target.");
            RoomCombatState? actorRoom = train.Rooms.FirstOrDefault(room => room.Units.Any(unit => unit.Id == action.ActivatorUnitId));
            CombatUnit? actor = actorRoom?.Units.FirstOrDefault(unit => unit.Id == action.ActivatorUnitId);
            if (actor == null || actor.Health <= 0 || actor.Ability?.HasAbility != true) return Illegal("The selected unit has no live ability.");
            if (actor.Ability.Resolving || actor.Status("silenced") != null || actor.Status("muted") != null)
                return Illegal("The unit ability is disabled.");
            string? error = TrainCombatModel.Validate(train) ?? StatisticQueryFrame.ValidateDecision(source);
            if (error != null) return Unsupported(error);
            if (source.Spawn.NextUnitId <= 0 || train.Rooms.SelectMany(room => room.Units)
                .Any(unit => unit.Id <= 0 || unit.Id >= source.Spawn.NextUnitId || unit.Size < 0))
                return Unsupported("Invalid ability unit identity allocation or size.");
            if (actor.Status("cooldown") != null) return Illegal("The unit ability is cooling down.");
            if (actorRoom!.Deployment || source.Spawn.Turn == 0) return Illegal("Unit abilities cannot activate during deployment.");
            CardPlayRule? rule = source.PlayRules.Cards.FirstOrDefault(item => item.DataId == actor.Ability.DataId);
            if (rule?.Ability?.Kind != "Unit") return Unsupported("Missing unit ability definition or availability metadata.");
            if (!rule.Ability.CanPlayWhenHandFull && context.Cards.Hand.Count >= context.MaxHandSize) return Illegal("The ability requires hand space.");
            if (!rule.Ability.CanTargetOtherFloors && action.RoomIndex != actorRoom.RoomIndex) return Illegal("The ability cannot target another floor.");
            RoomPlayRule? selectedRule = source.PlayRules.Rooms.FirstOrDefault(room => room.RoomIndex == action.RoomIndex);
            if (selectedRule == null || !selectedRule.Enabled || train.Rooms.All(room => room.RoomIndex != action.RoomIndex))
                return Illegal("The selected ability room does not exist or is disabled.");
            if (rule.Effect != "Spell") return Unsupported("Unit ability effect pipeline " + rule.Effect + " is not modeled.");
            if (rule.ExternalInteractions.Count > 0) return Unsupported(string.Join("; ", rule.ExternalInteractions));
            if (actor.Ability.CardCreation == null) return Unsupported("Missing ability card creation state.");
            AbilityCardResult cache = AbilityCardModel.Get(context, actor.Ability.CardCreation);
            if (!cache.Supported) return Unsupported(cache.UnsupportedReason!);
            if (cache.Card!.InstanceId != action.CardInstanceId) return Illegal("The action does not use this unit's cached ability card.");
            context = cache.Context!.WithOtherPiles(source.OtherPiles);
            rule = CardModifierModel.Resolve(rule, cache.Card);
            if (rule.ExternalInteractions.Count > 0) return Unsupported(string.Join("; ", rule.ExternalInteractions));
            if (rule.CostType != "Default" && rule.CostType != "ConsumeRemainingEnergy" || rule.Cost < 0 || source.Energy < 0)
                return Unsupported("Unmodeled ability cost type or negative cost/energy.");
            int paid = rule.CostType == "ConsumeRemainingEnergy" ? source.Energy : rule.Cost;
            if (source.Energy < paid) return Illegal("Insufficient energy for the unit ability.");
            error = CardSpellModel.UnisolatedUiRangeReason(source) ?? CardPileModel.ValidateMembership(context.Cards, source.OtherPiles, context.NextCardId);
            if (error != null) return Unsupported(error);
            train = CardSpellModel.WithContext(train, context);
            SpellCastCheck cast = CardSpellModel.TestPlay(train, action.RoomIndex, rule.Effects, 0, source.PlayRules, actor.Id);
            if (!cast.Supported) return Unsupported(cast.UnsupportedReason!);
            if (!cast.CanPlay) return Illegal("The ability failed its casting test.");
            context = context.WithBattleRng(cast.BattleRngAfterTests!.Value)
                .WithStatistics(context.Statistics?.WithPlayedCost(cache.Card.InstanceId, paid))
                .WithCard(cache.Card.WithPlayedCost(paid)).WithAbilityActivator(actor.Id)
                .WithQueryFrame(context.QueryFrame?.With(energy: source.Energy - paid));
            train = Resolving(CardSpellModel.WithContext(train, context), actor.Id, true);
            RoomOutcome outcome = RoomOutcome.Exchanged;
            // Only MonsterManager (the player's units) receives the pre-own callback.
            if (actor.Team == CombatTeam.Player)
            {
                TrainCombatResult before = OwnTrigger(train, actor.Id, "OnPreOwnAbilityActivated");
                if (!before.Supported) return Unsupported(before.UnsupportedReason!);
                train = before.State!; if (Terminal(before.Outcome)) outcome = before.Outcome;
            }
            TrainSpellResult applied = CardSpellModel.Apply(train, action.RoomIndex, rule.Effects, 0, cache.Card.InstanceId,
                source.PlayRules, train.Context!.OtherPiles ?? source.OtherPiles, actor.Id);
            if (!applied.Supported) return Unsupported(applied.UnsupportedReason!);
            train = applied.State!; if (Terminal(applied.Outcome)) outcome = applied.Outcome;
            context = train.Context!;
            // Cached cards enter global played history, but never discard or increment local play count.
            BattleStatistics? statistics = context.LiveStatistics?.Increment(cache.Card.InstanceId, "TimesPlayed", requireTrackedCard: true)
                .WithPlayedCost(cache.Card.InstanceId, null);
            context = context.WithStatistics(statistics).AfterCardEffects();
            train = Resolving(CardSpellModel.WithContext(train, context), actor.Id, false);
            TrainCombatResult after = OwnTrigger(train, actor.Id, "OnOwnAbilityActivated");
            if (!after.Supported) return Unsupported(after.UnsupportedReason!);
            train = after.State!; if (Terminal(after.Outcome)) outcome = after.Outcome;
            context = train.Context!;
            context = EquipmentModel.ReturnUnattached(context, new HashSet<int>(train.Rooms.SelectMany(room => room.Units).Select(unit => unit.Id)));
            if (Terminal(outcome)) context = context.WithQueryFrame(context.QueryFrame?.With(runningCombat: false));
            train = CardSpellModel.WithContext(train, context);
            if (source.BattlePreviewEnabled && !Terminal(outcome))
            {
                TrainCombatResult preview = BattlePreviewModel.Refresh(train);
                if (!preview.Supported) return Unsupported(preview.UnsupportedReason!);
                train = preview.State!; context = train.Context!;
            }
            if (source.CanonicalDecisionReferences) train = TrainCombatModel.ProcessRemovals(train);
            EnemySpawnState old = source.Spawn;
            var spawn = new EnemySpawnState(train, old.Waves, old.SelectedGroups, old.Phase, old.Looping, old.Rng, old.NextUnitId,
                old.Treasures, old.TreasuresRemaining, old.TreasureEnabled, old.FirstTreasureTurn, old.FirstTreasureRoom,
                old.Turn, old.ExternalInteractions, old.CanonicalDecisionReferences);
            return new BattleActionResult(new BattleTurnState(spawn, context.QueryFrame?.Energy ?? source.Energy - paid,
                source.EnergyPerTurn, source.DrawPerTurn, source.ForgePoints, source.DragonsHoard, source.MoonPhase,
                source.RngStreams.Select(stream => new BattleRngStream(stream.Name, stream.Seed,
                    stream.Name == "Battle" ? context.BattleRng : stream.Name == "CardDraw" ? context.Cards.Rng : stream.State)).ToArray(),
                context.OtherPiles ?? source.OtherPiles, source.ExternalInteractions, source.PlayRules, source.BattlePreviewEnabled,
                source.UiRngIsolated, source.CanonicalDecisionReferences), outcome: outcome);
        }

        public static IReadOnlyList<PlayCardAction> EnumerateSupportedActivations(BattleTurnState source)
        {
            var actions = new List<PlayCardAction>();
            CombatContext? context = source.Spawn.Train.Context;
            if (context == null || source.PlayRules == null) return actions.AsReadOnly();
            foreach (RoomCombatState room in source.Spawn.Train.Rooms)
            foreach (CombatUnit actor in room.Units.Where(unit => unit.Team == CombatTeam.Player).OrderBy(unit => unit.Id))
            {
                if (actor.Ability?.HasAbility != true || actor.Ability.CardCreation == null) continue;
                AbilityCardResult cached = AbilityCardModel.Get(context, actor.Ability.CardCreation);
                if (!cached.Supported) continue;
                foreach (RoomPlayRule target in source.PlayRules.Rooms.OrderBy(target => target.RoomIndex == room.RoomIndex ? 0 : 1))
                {
                    var action = new PlayCardAction(cached.Card!.InstanceId, target.RoomIndex, activatorUnitId: actor.Id);
                    if (Activate(source, action).Supported) actions.Add(action);
                }
            }
            return actions.AsReadOnly();
        }
        public static PlayCardAction? ChooseAbilityThenCards(BattleTurnState source) =>
            EnumerateSupportedActivations(source).FirstOrDefault() ?? BattleActionModel.ChooseUnitSpellAndJunkPlay(source);

        private static TrainCombatState Resolving(TrainCombatState train, int id, bool value) => new TrainCombatState(train.Rooms
            .Select(room => new RoomCombatState(room.RoomIndex, room.Deployment, room.Units.Select(unit => unit.Id == id && unit.Ability != null
                ? AbilityCooldownModel.Copy(unit, unit.Ability.WithResolving(value)) : unit).ToArray(), room.ExternalInteractions,
                train.Context, room.Preview)).ToArray(), train.Movement, train.EnemySlotsPerRoom, train.Context);
        private static TrainCombatResult OwnTrigger(TrainCombatState train, int id, string kind)
        {
            RoomCombatState? room = train.Rooms.FirstOrDefault(item => item.Units.Any(unit => unit.Id == id && unit.Health > 0));
            return room == null ? new TrainCombatResult(train, RoomOutcome.Exchanged, Array.Empty<RoomCombatResult>()) :
                TrainCombatModel.ApplyCharacterQueue(train, new List<RoomCombatModel.QueuedCharacterTrigger> {
                    new RoomCombatModel.QueuedCharacterTrigger(room.RoomIndex, room.Units.First(unit => unit.Id == id), kind) });
        }
        private static bool Terminal(RoomOutcome outcome) => outcome == RoomOutcome.BattleWon || outcome == RoomOutcome.PlayerDefeated;
        private static BattleActionResult Unsupported(string error) => new BattleActionResult(null, ActionRejection.Unsupported, error);
        private static BattleActionResult Illegal(string error) => new BattleActionResult(null, ActionRejection.Illegal, error);
    }
}
