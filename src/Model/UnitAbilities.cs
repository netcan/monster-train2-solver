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
            string? identityError = UnitIdentityModel.Validate(context, train.Rooms.SelectMany(room => room.Units), source.Spawn.NextUnitId);
            if (identityError != null) return Unsupported(identityError);
            if (source.ExternalInteractions.Count > 0 || source.Spawn.ExternalInteractions.Count > 0)
                return Unsupported(string.Join("; ", source.ExternalInteractions.Concat(source.Spawn.ExternalInteractions)));
            if (source.PlayRules.Cards.Select(rule => rule.DataId).Distinct().Count() != source.PlayRules.Cards.Count)
                return Unsupported("Duplicate ability play definitions.");
            if (action.PlayerPosition != -1 || action.TargetUnitId != 0)
                return Illegal("A unit ability does not accept a summon position or card unit target.");
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
            if (RelicModel.AbilitiesTriggerIncant(context) && (rule.CardType != "Spell" || rule.IsAnyAbility != true))
                return Unsupported("Unit ability Incant requires captured Spell/ability classification.");
            if (actor.Ability.CardCreation == null) return Unsupported("Missing ability card creation state.");
            AbilityCardResult cache = AbilityCardModel.Get(context, actor.Ability.CardCreation);
            if (!cache.Supported) return Unsupported(cache.UnsupportedReason!);
            if (cache.Card!.InstanceId != action.CardInstanceId) return Illegal("The action does not use this unit's cached ability card.");
            context = cache.Context!.WithOtherPiles(source.OtherPiles);
            rule = CardModifierModel.Resolve(rule, cache.Card);
            if (rule.ExternalInteractions.Count > 0) return Unsupported(string.Join("; ", rule.ExternalInteractions));
            bool hasTeleport = rule.Effects.Any(effect => effect.Type == "AbilityTeleport");
            if (action.TargetSpawnPointIndex != -1 && !hasTeleport)
                return Illegal("This unit ability does not accept a destination spawn point.");
            if (rule.CostType != "Default" && rule.CostType != "ConsumeRemainingEnergy" || rule.Cost < 0 || source.Energy < 0)
                return Unsupported("Unmodeled ability cost type or negative cost/energy.");
            int effectiveCost = rule.Cost;
            if (rule.CardType == "Spell")
                effectiveCost = (int)Math.Clamp((long)rule.Cost + RoomCapacityModel.SpellCostDelta(context, action.RoomIndex), 0L, 99L);
            int paid = rule.CostType == "ConsumeRemainingEnergy" ? source.Energy : effectiveCost;
            error = CardSpellModel.UnisolatedUiRangeReason(source) ?? CardPileModel.ValidateMembership(context.Cards, source.OtherPiles, context.NextCardId);
            if (error != null) return Unsupported(error);
            train = CardSpellModel.WithContext(train, context);
            SpellCastCheck cast = CardSpellModel.TestPlay(train, action.RoomIndex, rule.Effects, 0, source.PlayRules,
                actor.Id, action.TargetSpawnPointIndex);
            if (!cast.Supported) return Unsupported(cast.UnsupportedReason!);
            if (!cast.CanPlay) return Illegal("The ability failed its casting test.");
            RoomCombatState? selectedRoomState = train.Rooms.FirstOrDefault(room => room.RoomIndex == action.RoomIndex);
            if (selectedRoomState == null) return Illegal("The selected ability room is missing from train state.");
            RelicPyreCostPlan pyreCost = RelicPayCardCostWithPyreHealthModel.Plan(train, effectiveCost,
                source.Energy, rule.CostType, source.Spawn.Turn,
                context.IsolatedBattlePreview == true || selectedRoomState.Preview);
            if (!pyreCost.Supported) return Unsupported(pyreCost.UnsupportedReason!);
            if (!pyreCost.CanPay) return Illegal("Insufficient energy for the unit ability.");
            bool paidWithPyre = pyreCost.PaidWithPyre;
            int energyPaid = paidWithPyre ? 0 : paid;
            context = pyreCost.State.Context!.WithBattleRng(cast.BattleRngAfterTests!.Value);
            if (!paidWithPyre)
                context = context.WithStatistics(context.Statistics?.WithPlayedCost(cache.Card.InstanceId, paid))
                    .WithCard(cache.Card.WithPlayedCost(paid));
            context = context.WithAbilityActivator(actor.Id)
                .WithQueryFrame(context.QueryFrame?.With(energy: source.Energy - energyPaid));
            train = Resolving(CardSpellModel.WithContext(pyreCost.State, context), actor.Id, true);
            RoomOutcome outcome = RoomOutcome.Exchanged;
            if (pyreCost.Callbacks.Count > 0)
            {
                TrainCombatResult paidCallbacks = RelicPayCardCostWithPyreHealthModel.DrainCallbacks(train, pyreCost);
                if (!paidCallbacks.Supported) return Unsupported(paidCallbacks.UnsupportedReason!);
                train = paidCallbacks.State!;
                if (Terminal(paidCallbacks.Outcome)) outcome = paidCallbacks.Outcome;
            }
            // Only MonsterManager (the player's units) receives the pre-own callback.
            if (actor.Team == CombatTeam.Player)
            {
                TrainCombatResult before = OwnTrigger(train, actor.Id, "OnPreOwnAbilityActivated");
                if (!before.Supported) return Unsupported(before.UnsupportedReason!);
                train = before.State!; if (Terminal(before.Outcome)) outcome = before.Outcome;
            }
            // CombatManager caches the selected room after PreOwn, before effects.
            int[] cachedUnits = train.Rooms.Single(room => room.RoomIndex == action.RoomIndex).Units
                .Where(unit => unit.Health > 0 && unit.DeathState?.IsDestroyed != true && unit.IsSpawning != true)
                .Select(unit => unit.Id).ToArray();
            if (train.Context!.FindCard(cache.Card.InstanceId)?.PlayedRoomUnitIds != null)
                train = CardSpellModel.WithContext(train, train.Context.WithCard(
                    train.Context.FindCard(cache.Card.InstanceId)!.WithPlayedRoomUnits(cachedUnits)));
            TrainSpellResult applied = CardSpellModel.ApplyForCardPlay(train, action.RoomIndex, rule.Effects, 0, cache.Card.InstanceId,
                source.PlayRules, train.Context!.OtherPiles ?? source.OtherPiles, actor.Id, action.TargetSpawnPointIndex);
            if (!applied.Supported) return Unsupported(applied.UnsupportedReason!);
            train = applied.State!; if (Terminal(applied.Outcome)) outcome = applied.Outcome;
            context = train.Context!;
            // Cached cards enter global played history, but never discard or increment local play count.
            BattleStatistics? statistics = context.LiveStatistics?.Increment(cache.Card.InstanceId, "TimesPlayed", requireTrackedCard: true);
            context = context.WithStatistics(statistics).AfterCardEffects();
            train = CardSpellModel.WithContext(train, context);
            foreach (CombatTeam team in new[] { CombatTeam.Player, CombatTeam.Enemy })
            {
                TrainCombatResult after = CardPlayedTriggerModel.Ability(train, team, cachedUnits, actor.Id,
                    team == CombatTeam.Player ? applied.PendingCallbacks : null);
                if (!after.Supported) return Unsupported(after.UnsupportedReason!);
                train = after.State!; if (Terminal(after.Outcome)) outcome = after.Outcome;
            }
            // Native clears resolving and played-cost queries after both team queues.
            train = Resolving(train, actor.Id, false);
            context = train.Context!;
            context = context.WithStatistics(context.Statistics?.WithPlayedCost(cache.Card.InstanceId, null));
            context = EquipmentModel.ReturnUnattached(context, new HashSet<int>(train.Rooms.SelectMany(room => room.Units).Select(unit => unit.Id)));
            if (Terminal(outcome)) context = context.WithQueryFrame(context.QueryFrame?.With(runningCombat: false));
            train = CardSpellModel.WithContext(train, context);
            if (source.BattlePreviewEnabled && !Terminal(outcome))
            {
                TrainCombatResult preview = BattlePreviewModel.Refresh(train);
                if (!preview.Supported) return Unsupported(preview.UnsupportedReason!);
                train = preview.State!; context = train.Context!;
            }
            if (source.CanonicalPhysicalReferences) train = TrainCombatModel.CompleteFrameRemovals(train);
            else if (source.CanonicalDecisionReferences) train = TrainCombatModel.ProcessRemovals(train);
            EnemySpawnState old = source.Spawn;
            var spawn = new EnemySpawnState(train, old.Waves, old.SelectedGroups, old.Phase, old.Looping, old.Rng, train.Context?.NextUnitId ?? old.NextUnitId,
                old.Treasures, old.TreasuresRemaining, old.TreasureEnabled, old.FirstTreasureTurn, old.FirstTreasureRoom,
                old.Turn, old.ExternalInteractions, old.CanonicalDecisionReferences);
            return new BattleActionResult(new BattleTurnState(spawn, context.QueryFrame?.Energy ?? source.Energy - energyPaid,
                source.EnergyPerTurn, source.DrawPerTurn, context.QueryFrame?.ForgePoints ?? source.ForgePoints, source.DragonsHoard,
                context.QueryFrame?.MoonPhase == 1 ? "New" : context.QueryFrame?.MoonPhase == 2 ? "Full" : source.MoonPhase,
                source.RngStreams.Select(stream => new BattleRngStream(stream.Name, stream.Seed,
                    stream.Name == "Battle" ? context.BattleRng : stream.Name == "CardDraw" ? context.Cards.Rng : stream.State)).ToArray(),
                context.OtherPiles ?? source.OtherPiles, source.ExternalInteractions, source.PlayRules, source.BattlePreviewEnabled,
                source.UiRngIsolated, source.CanonicalDecisionReferences, source.CanonicalPhysicalReferences,
                train.Context?.QueryFrame?.SelectedRoomIndex ?? source.SelectedRoom,
                source.MoonPhaseTrackingActive), outcome: outcome);
        }

        public static IReadOnlyList<PlayCardAction> EnumerateSupportedActivations(BattleTurnState source) =>
            EnumerateActivations(source).SupportedActions;

        internal static BattleActionEnumeration EnumerateActivations(BattleTurnState source)
        {
            var actions = new List<SupportedActionCandidate>();
            var unsupported = new List<UnsupportedActionAttempt>();
            CombatContext? context = source.Spawn.Train.Context;
            if (context == null || source.PlayRules == null)
            {
                unsupported.Add(new UnsupportedActionAttempt(null, "Missing ability play definitions or battle context."));
                return new BattleActionEnumeration(actions, unsupported);
            }
            foreach (RoomCombatState room in source.Spawn.Train.Rooms)
            foreach (CombatUnit actor in room.Units.Where(unit => unit.Team == CombatTeam.Player).OrderBy(unit => unit.Id))
            {
                if (actor.Ability?.HasAbility != true) continue;
                if (actor.Ability.CardCreation == null)
                {
                    unsupported.Add(new UnsupportedActionAttempt(null,
                        "Unit " + actor.Id + " has an ability without captured card-creation rules."));
                    continue;
                }
                AbilityCardResult cached = AbilityCardModel.Get(context, actor.Ability.CardCreation);
                if (!cached.Supported)
                {
                    unsupported.Add(new UnsupportedActionAttempt(null, cached.UnsupportedReason ??
                        "Unit " + actor.Id + " ability card could not be captured."));
                    continue;
                }
                CardPlayRule? abilityRule = source.PlayRules.Cards.FirstOrDefault(item => item.DataId == actor.Ability.DataId);
                bool hasTeleport = abilityRule != null && CardModifierModel.Resolve(abilityRule, cached.Card!).Effects
                    .Any(effect => effect.Type == "AbilityTeleport");
                foreach (RoomPlayRule target in source.PlayRules.Rooms.OrderBy(target => target.RoomIndex == room.RoomIndex ? 0 : 1))
                {
                    int targetCount = hasTeleport ? context.SpawnPoints?.Group(target.RoomIndex, CombatTeam.Player)?.GroupCount ?? 0 : 1;
                    if (hasTeleport && targetCount == 0)
                    {
                        unsupported.Add(new UnsupportedActionAttempt(null,
                            "Unit " + actor.Id + " teleport has no captured destination spawn-point group."));
                        continue;
                    }
                    for (int targetIndex = 0; targetIndex < targetCount; targetIndex++)
                    {
                        var action = new PlayCardAction(cached.Card!.InstanceId, target.RoomIndex,
                            activatorUnitId: actor.Id, targetSpawnPointIndex: hasTeleport ? targetIndex : -1);
                        BattleActionResult attempt = BattleActionModel.PlayCard(source, action);
                        if (attempt.Supported) actions.Add(new SupportedActionCandidate(action, attempt));
                        else if (attempt.Rejection == ActionRejection.Unsupported)
                            unsupported.Add(new UnsupportedActionAttempt(action, attempt.Reason ?? "Unsupported unit ability action."));
                    }
                }
            }
            return new BattleActionEnumeration(actions, unsupported);
        }
        public static PlayCardAction? ChooseAbilityThenCards(BattleTurnState source) =>
            EnumerateSupportedActivations(source).FirstOrDefault() ?? BattleActionModel.ChooseUnitSpellAndJunkPlay(source);

        public static PlayCardAction? ChooseTeleportCalibrationAction(BattleTurnState source)
        {
            IReadOnlyList<PlayCardAction> actions = EnumerateSupportedActivations(source);
            BattleSpawnPoints? points = source.Spawn.Train.Context?.SpawnPoints;
            int lastActivator = source.Spawn.Train.Context?.LastAbilityActivatorUnitId ?? 0;
            SpawnPointReference? lastPosition = lastActivator > 0
                ? points?.Units.SingleOrDefault(unit => unit.UnitId == lastActivator)?.Current : null;
            if (lastActivator == 0)
            {
                foreach (PlayCardAction action in actions)
                {
                    RoomCombatState? actorRoom = source.Spawn.Train.Rooms.FirstOrDefault(room =>
                        room.Units.Any(unit => unit.Id == action.ActivatorUnitId));
                    SpawnPointReference? before = points?.Units.SingleOrDefault(unit => unit.UnitId == action.ActivatorUnitId)?.Current;
                    if (actorRoom == null || before == null || actorRoom.RoomIndex != action.RoomIndex) continue;
                    BattleActionResult result = BattleActionModel.PlayCard(source, action);
                    SpawnPointReference? after = result.State?.Spawn.Train.Context?.SpawnPoints?.Units
                        .SingleOrDefault(unit => unit.UnitId == action.ActivatorUnitId)?.Current;
                    if (result.Supported && after != null && after.RoomIndex == before.RoomIndex && after.Index != before.Index)
                        return action;
                }
            }
            else if (lastPosition?.RoomIndex == 0)
            {
                foreach (PlayCardAction action in actions)
                {
                    RoomCombatState? actorRoom = source.Spawn.Train.Rooms.FirstOrDefault(room =>
                        room.Units.Any(unit => unit.Id == action.ActivatorUnitId));
                    if (actorRoom == null || actorRoom.RoomIndex == action.RoomIndex) continue;
                    BattleActionResult result = BattleActionModel.PlayCard(source, action);
                    SpawnPointReference? after = result.State?.Spawn.Train.Context?.SpawnPoints?.Units
                        .SingleOrDefault(unit => unit.UnitId == action.ActivatorUnitId)?.Current;
                    if (result.Supported && after?.RoomIndex == action.RoomIndex && after.Index == action.TargetSpawnPointIndex)
                        return action;
                }
            }
            return ChooseAbilityThenCards(source);
        }

        internal static TrainCombatState Resolving(TrainCombatState train, int id, bool value) => new TrainCombatState(train.Rooms
            .Select(room => new RoomCombatState(room.RoomIndex, room.Deployment, room.Units.Select(unit => unit.Id == id && unit.Ability != null
                ? AbilityCooldownModel.Copy(unit, AbilityLifecycleModel.Normalize(unit.Ability.WithResolving(value))) : unit).ToArray(), room.ExternalInteractions,
                train.Context, room.Preview, room.ActiveCompanionBossIds)).ToArray(), train.Movement, train.EnemySlotsPerRoom, train.Context);
        private static TrainCombatResult OwnTrigger(TrainCombatState train, int id, string kind,
            IReadOnlyList<RoomCombatModel.QueuedCharacterTrigger>? preceding = null)
        {
            RoomCombatState? room = train.Rooms.FirstOrDefault(item => item.Units.Any(unit => unit.Id == id && unit.Health > 0));
            var queue = preceding?.ToList() ?? new List<RoomCombatModel.QueuedCharacterTrigger>();
            if (room != null) queue.Add(new RoomCombatModel.QueuedCharacterTrigger(room.RoomIndex, room.Units.First(unit => unit.Id == id), kind));
            return queue.Count == 0 ? new TrainCombatResult(train, RoomOutcome.Exchanged, Array.Empty<RoomCombatResult>()) :
                TrainCombatModel.ApplyCharacterQueue(train, queue);
        }
        private static bool Terminal(RoomOutcome outcome) => outcome == RoomOutcome.BattleWon || outcome == RoomOutcome.PlayerDefeated;
        private static BattleActionResult Unsupported(string error) => new BattleActionResult(null, ActionRejection.Unsupported, error);
        private static BattleActionResult Illegal(string error) => new BattleActionResult(null, ActionRejection.Illegal, error);
    }
}
