using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class BattleRngStream
    {
        public string Name { get; }
        public int Seed { get; }
        public UnityRng State { get; }
        public BattleRngStream(string name, int seed, UnityRng state) { Name = name; Seed = seed; State = state; }
    }
    public sealed class CardPileState
    {
        public string Name { get; }
        public IReadOnlyList<CardToken> Cards { get; }
        public IReadOnlyList<int>? EntrySlots { get; }
        public IReadOnlyList<int>? FreeSlots { get; }
        public IReadOnlyList<EquipmentStandbyCondition>? EquipmentConditions { get; }
        public IReadOnlyList<UnitStandbyCondition>? UnitConditions { get; }
        public IReadOnlyList<CardExhaustStandbyCondition>? ExhaustConditions { get; }
        public CardPileState(string name, IReadOnlyList<CardToken> cards, IReadOnlyList<int>? entrySlots = null,
            IReadOnlyList<int>? freeSlots = null, IReadOnlyList<EquipmentStandbyCondition>? equipmentConditions = null,
            IReadOnlyList<UnitStandbyCondition>? unitConditions = null, IReadOnlyList<CardExhaustStandbyCondition>? exhaustConditions = null)
        {
            Name = name; Cards = Array.AsReadOnly(cards.ToArray());
            EntrySlots = entrySlots == null ? null : Array.AsReadOnly(entrySlots.ToArray());
            FreeSlots = freeSlots == null ? null : Array.AsReadOnly(freeSlots.ToArray());
            EquipmentConditions = equipmentConditions == null ? null : Array.AsReadOnly(equipmentConditions
                .OrderBy(condition => Array.FindIndex(cards.ToArray(), card => card.InstanceId == condition.CardId)).ToArray());
            UnitConditions = unitConditions == null ? null : Array.AsReadOnly(unitConditions
                .OrderBy(condition => Array.FindIndex(cards.ToArray(), card => card.InstanceId == condition.CardId)).ToArray());
            ExhaustConditions = exhaustConditions == null ? null : Array.AsReadOnly(exhaustConditions
                .OrderBy(condition => Array.FindIndex(cards.ToArray(), card => card.InstanceId == condition.CardId)).ToArray());
        }
    }
    public sealed class BattleTurnState
    {
        public EnemySpawnState Spawn { get; }
        public int Energy { get; }
        public int EnergyPerTurn { get; }
        public int DrawPerTurn { get; }
        public int ForgePoints { get; }
        public int DragonsHoard { get; }
        public string MoonPhase { get; }
        public bool? MoonPhaseTrackingActive { get; }
        public IReadOnlyList<BattleRngStream> RngStreams { get; }
        public IReadOnlyList<CardPileState> OtherPiles { get; }
        public IReadOnlyList<string> ExternalInteractions { get; }
        public BattlePlayRules? PlayRules { get; }
        public bool BattlePreviewEnabled { get; }
        public bool UiRngIsolated { get; }
        public bool CanonicalDecisionReferences { get; }
        public bool CanonicalPhysicalReferences { get; }
        public int SelectedRoom { get; }
        public BattleTurnState(EnemySpawnState spawn, int energy, int energyPerTurn, int drawPerTurn,
            int forgePoints, int dragonsHoard, string moonPhase, IReadOnlyList<BattleRngStream> rngStreams,
            IReadOnlyList<CardPileState> otherPiles, IReadOnlyList<string> externalInteractions, BattlePlayRules? playRules = null,
            bool battlePreviewEnabled = false, bool uiRngIsolated = false, bool canonicalDecisionReferences = false,
            bool canonicalPhysicalReferences = false, int selectedRoom = -1, bool? moonPhaseTrackingActive = null)
        {
            Spawn = spawn; Energy = energy; EnergyPerTurn = energyPerTurn; DrawPerTurn = drawPerTurn;
            ForgePoints = forgePoints; DragonsHoard = dragonsHoard; MoonPhase = moonPhase;
            MoonPhaseTrackingActive = moonPhaseTrackingActive;
            RngStreams = Array.AsReadOnly(rngStreams.ToArray()); OtherPiles = Array.AsReadOnly(otherPiles.ToArray());
            ExternalInteractions = Array.AsReadOnly(externalInteractions.ToArray());
            PlayRules = playRules;
            BattlePreviewEnabled = battlePreviewEnabled;
            UiRngIsolated = uiRngIsolated;
            CanonicalDecisionReferences = canonicalDecisionReferences;
            CanonicalPhysicalReferences = canonicalPhysicalReferences;
            SelectedRoom = selectedRoom;
        }
    }
    public sealed class BattleTurnResult
    {
        public BattleTurnState? State { get; }
        public RoomOutcome Outcome { get; }
        public string? UnsupportedReason { get; }
        public bool Supported => State != null;
        internal BattleTurnResult(BattleTurnState? state, RoomOutcome outcome, string? unsupportedReason = null)
        { State = state; Outcome = outcome; UnsupportedReason = unsupportedReason; }
    }

    public static class BattleTurnModel
    {
        internal static bool IsDeferredPersistentDiscard(string interaction) =>
            interaction is "CardTraitPersistent Discard callback" or "CardTraitPersistent discard routing" or "CardTraitFreeze discard routing";

        internal static IReadOnlyList<string> ActionInteractions(BattleTurnState source) =>
            source.ExternalInteractions.Where(interaction => !IsDeferredPersistentDiscard(interaction)).ToArray();

        internal static IReadOnlyList<string> AfterActionInteractions(BattleTurnState source, CombatContext context)
        {
            if (!source.ExternalInteractions.Any(IsDeferredPersistentDiscard)) return source.ExternalInteractions;
            foreach (CardToken token in context.Cards.Hand)
            {
                CardInstanceState? instance = context.FindCard(token.InstanceId);
                if (instance?.MaskDescriptor == null) return source.ExternalInteractions;
                try
                {
                    if (CardBranchMaskModel.Resolve(instance).Traits.Any(trait => trait is "CardTraitPersistent" or "CardTraitFreeze"))
                        return source.ExternalInteractions;
                }
                catch (InvalidOperationException)
                {
                    return source.ExternalInteractions;
                }
            }
            return ActionInteractions(source);
        }

        // Room changes refresh the native combat preview before the selected action.
        // Older captures without a selected room remain explicitly unknown.
        public static BattleTurnResult SelectRoom(BattleTurnState source, int roomIndex)
        {
            if (source.PlayRules == null || source.Spawn.Train.Context == null)
                return Unsupported("Missing room-selection rules or battle context.");
            if (!source.PlayRules.Rooms.Any(room => room.RoomIndex == roomIndex))
                return Unsupported("Selected room is not present in the captured rules.");
            TrainCombatState train = source.Spawn.Train;
            int? framedRoom = train.Context!.QueryFrame?.SelectedRoomIndex;
            int currentRoom = framedRoom ?? source.SelectedRoom;
            if (source.SelectedRoom == roomIndex && (!framedRoom.HasValue || framedRoom == roomIndex))
                return new BattleTurnResult(source, RoomOutcome.Exchanged);
            IReadOnlyList<string> actionInteractions = ActionInteractions(source);
            if (actionInteractions.Count > 0)
                return Unsupported(string.Join("; ", actionInteractions));
            if (train.Rooms.Any(room => room.ExternalInteractions.Count > 0))
                return Unsupported(string.Join("; ", train.Rooms.SelectMany(room => room.ExternalInteractions).Distinct()));
            string? relicError = RelicModel.Validate(train.Context);
            if (relicError != null) return Unsupported(relicError);
            if (currentRoom >= 0 && !source.PlayRules.Rooms.Any(room => room.RoomIndex == currentRoom))
                return Unsupported("Current selected room is not present in the captured rules.");

            // Native callbacks can focus a different room before the next decision.
            // The captured live frame is authoritative when the outer snapshot lags.
            bool selectionChanged = currentRoom != roomIndex;
            if (framedRoom.HasValue)
                train = CardSpellModel.WithContext(train, train.Context!.WithQueryFrame(train.Context.QueryFrame!.With(selectedRoomIndex: roomIndex)));
            if (selectionChanged && currentRoom >= 0 && source.BattlePreviewEnabled)
            {
                TrainCombatResult preview = BattlePreviewModel.Refresh(train, source.PlayRules);
                if (!preview.Supported) return Unsupported(preview.UnsupportedReason!);
                train = preview.State!;
            }
            EnemySpawnState old = source.Spawn;
            var spawn = new EnemySpawnState(train, old.Waves, old.SelectedGroups, old.Phase, old.Looping,
                old.Rng, train.Context?.NextUnitId ?? old.NextUnitId, old.Treasures, old.TreasuresRemaining,
                old.TreasureEnabled, old.FirstTreasureTurn, old.FirstTreasureRoom, old.Turn, old.ExternalInteractions,
                old.CanonicalDecisionReferences, old.PendingDestroyedUnitIds, old.OuterBoss, old.QueuedOuterBoss, old.QueuedTrueFinalBoss, old.TrueFinalBoss);
            BattleRngStream[] streams = source.RngStreams.Select(stream => new BattleRngStream(stream.Name, stream.Seed,
                stream.Name == "Battle" ? train.Context!.BattleRng : stream.State)).ToArray();
            return new BattleTurnResult(new BattleTurnState(spawn, source.Energy, source.EnergyPerTurn,
                source.DrawPerTurn, source.ForgePoints, source.DragonsHoard, source.MoonPhase, streams,
                source.OtherPiles, source.ExternalInteractions, source.PlayRules, source.BattlePreviewEnabled,
                source.UiRngIsolated, source.CanonicalDecisionReferences, source.CanonicalPhysicalReferences, roomIndex,
                source.MoonPhaseTrackingActive),
                RoomOutcome.Exchanged);
        }

        // One quiet player decision point through EndTurn to the next decision or terminal battle.
        public static BattleTurnResult EndTurn(BattleTurnState source)
        {
            IReadOnlyList<string> turnInteractions = ActionInteractions(source);
            if (turnInteractions.Count > 0) return Unsupported(string.Join("; ", turnInteractions));
            if (source.Spawn.Train.Context == null) return Unsupported("Missing shared battle context.");
            string? identityError = UnitIdentityModel.Validate(source.Spawn.Train.Context,
                source.Spawn.Train.Rooms.SelectMany(room => room.Units), source.Spawn.NextUnitId);
            if (identityError != null) return Unsupported(identityError);
            string? frameError = StatisticQueryFrame.ValidateDecision(source);
            if (frameError != null) return Unsupported(frameError);
            foreach (CardPileState pile in source.OtherPiles)
            {
                string? pileError = CardPileModel.Validate(pile);
                if (pileError != null) return Unsupported(pileError);
            }
            if (source.MoonPhase != "Full" && source.MoonPhase != "New") return Unsupported("Unmodeled moon phase.");
            string? membershipError = CardPileModel.ValidateMembership(source.Spawn.Train.Context.Cards, source.OtherPiles,
                source.Spawn.Train.Context.NextCardId);
            if (membershipError != null) return Unsupported(membershipError);
            int[] spawners = source.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .Where(unit => unit.SpawnerCardId > 0).Select(unit => unit.SpawnerCardId).ToArray();
            if (spawners.Distinct().Count() != spawners.Length) return Unsupported("Shared unit spawner cards are not modeled.");
            var standbyCards = new HashSet<int>(source.OtherPiles.Where(pile => pile.Name == "Standby")
                .SelectMany(pile => pile.Cards).Select(card => card.InstanceId));
            CombatContext birthContext = source.Spawn.Train.Context;
            if (source.Spawn.Train.Rooms.SelectMany(room => room.Units).Where(unit => unit.SpawnerCardId > 0)
                .Any(unit => !standbyCards.Contains(unit.SpawnerCardId) &&
                    !source.OtherPiles.Any(pile => pile.Name == "Purged" && pile.Cards.Any(card => card.InstanceId == unit.SpawnerCardId)) &&
                    !((unit.Status("cardless")?.Stacks > 0 || unit.Modifiers?.IsClone == true ||
                    source.OtherPiles.Any(pile => pile.UnitConditions?.Any(binding => binding.HostUnitId == unit.Id) == true)) &&
                    birthContext.CardInstances != null && !birthContext.CardInstances.Any(card => card.InstanceId == unit.SpawnerCardId) &&
                    birthContext.CardRegistry?.Any(card => card.InstanceId == unit.SpawnerCardId) == true)))
                return Unsupported("A living unit's spawner card is missing from standby.");
            if (source.RngStreams.Select(stream => stream.Name).Distinct().Count() != source.RngStreams.Count)
                return Unsupported("Duplicate gameplay RNG streams.");
            BattleRngStream? spawningStream = source.RngStreams.FirstOrDefault(stream => stream.Name == "Spawning");
            if (spawningStream == null) return Unsupported("Missing spawning seed.");
            EnemySpawnState spawn = source.Spawn;
            BattlePlayRules? outputPlayRules = source.PlayRules;
            UnityRng? outerBossRng = source.RngStreams.FirstOrDefault(stream => stream.Name == "BossActions")?.State;
            string? uiRangeError = CardSpellModel.UnisolatedUiRangeReason(source);
            if (uiRangeError != null) return Unsupported(uiRangeError);
            CardPileState[] otherPiles = source.OtherPiles.ToArray();
            TrainCombatState train = spawn.Train;
            CombatContext context = train.Context!.WithOtherPiles(otherPiles);
            if (source.MoonPhaseTrackingActive.HasValue && context.QueryFrame != null)
                context = context.WithQueryFrame(context.QueryFrame.With(
                    moonPhaseTrackingActive: source.MoonPhaseTrackingActive));
            train = WithContext(train, context, spawn.Turn == 0);
            int drawPerTurn = source.DrawPerTurn - UnitRoomResourceModel.Amount(source.Spawn.Train, UnitRoomResourceModel.Draw);
            if (context.Resources != null)
            {
                RelicResourceResult resourceDraw = RelicResourceModel.GetDrawCount(context);
                if (!resourceDraw.Supported) return Unsupported(resourceDraw.UnsupportedReason!);
                context = resourceDraw.Context!; drawPerTurn = resourceDraw.Value;
            }
            context = EnergyModel.SetPhase(context, "HeroTurn");
            train = WithContext(train, context, spawn.Turn == 0);
            foreach (CombatTeam team in new[] { CombatTeam.Player, CombatTeam.Enemy })
            {
                TrainCombatResult triggers = TrainCombatModel.EndTurnPreHandDiscard(train, team);
                if (!triggers.Supported) return Unsupported(triggers.UnsupportedReason!);
                if (!RouteDeadUnits(train, triggers)) return Unsupported("Missing pre-discard unit death or card routing.");
                train = triggers.State!;
                spawn = WithTrain(spawn, train, spawn.Turn, spawn.Rng);
                if (Terminal(triggers.Outcome)) return Finish(triggers.Outcome, source.Energy, source.MoonPhase);
            }
            context = train.Context!;
            RelicCheaperSpellUpgradeResult endTurnRelics = RelicCheaperSpellUpgradeModel.EndTurnPreHandDiscard(context);
            if (!endTurnRelics.Supported) return Unsupported(endTurnRelics.UnsupportedReason!);
            context = endTurnRelics.Context;
            RelicMoonPhaseHandUpgradeResult moonPhaseHandUpgrade = RelicMoonPhaseHandUpgradeModel.Apply(context,
                source.MoonPhase == "New" ? 1 : source.MoonPhase == "Full" ? 2 : 0,
                spawn.Turn == 0, context.IsolatedBattlePreview == true);
            if (!moonPhaseHandUpgrade.Supported) return Unsupported(moonPhaseHandUpgrade.UnsupportedReason!);
            context = moonPhaseHandUpgrade.Context;
            RelicMoonPhaseHandUpgradeResult randomFreeze = RelicRandomFreezeModel.Apply(context,
                spawn.Turn == 0, context.IsolatedBattlePreview == true);
            if (!randomFreeze.Supported) return Unsupported(randomFreeze.UnsupportedReason!);
            context = randomFreeze.Context;
            train = WithContext(train, context, spawn.Turn == 0);
            spawn = WithTrain(spawn, train, spawn.Turn, spawn.Rng);
            if (outputPlayRules != null)
            {
                var unplayed = context.Cards.Hand.Select((card, handIndex) =>
                {
                    CardPlayRule? rule = outputPlayRules.Cards.FirstOrDefault(item => item.DataId == card.DataId);
                    return (Card: card, HandIndex: handIndex, Rule: rule);
                }).Where(item => item.Rule?.OnUnplayedNegativeEffects.Count > 0)
                    .OrderBy(item => item.Rule!.UnplayedNegativeOrder.HasValue ? 0 : 1)
                    .ThenBy(item => item.Rule!.UnplayedNegativeOrder ?? item.HandIndex)
                    .ThenBy(item => item.HandIndex).ToArray();
                foreach (var item in unplayed)
                {
                    CardInstanceState? instance = context.FindCard(item.Card.InstanceId);
                    if (instance == null) return Unsupported("Unplayed card trigger has no captured card instance.");
                    CardPlayRule resolved = CardModifierModel.Resolve(item.Rule!, instance);
                    if (resolved.HasUnsupportedUnplayedTriggers || resolved.OnUnplayedNegativeEffects.Count == 0)
                        return Unsupported("Unmodeled unplayed card trigger: " + item.Card.DataId);
                    int triggerRoom = source.SelectedRoom >= 0 ? source.SelectedRoom :
                        outputPlayRules.Rooms.FirstOrDefault(room => room.IsPyre)?.RoomIndex ?? -1;
                    if (triggerRoom < 0) return Unsupported("Unplayed Pyre damage requires a captured selected room.");
                    TrainSpellResult spell = CardSpellModel.ApplyForCardPlay(train, triggerRoom,
                        resolved.OnUnplayedNegativeEffects, 0, item.Card.InstanceId, outputPlayRules, otherPiles,
                        onUnplayedNegativeEffects: true);
                    if (!spell.Supported) return Unsupported(spell.UnsupportedReason!);
                    train = spell.State!;
                    RoomOutcome triggerOutcome = spell.Outcome;
                    outputPlayRules = spell.PlayRules ?? outputPlayRules;
                    otherPiles = spell.OtherPiles?.ToArray() ?? train.Context?.OtherPiles?.ToArray() ?? otherPiles;
                    train = WithContext(train, train.Context!.WithOtherPiles(otherPiles), spawn.Turn == 0);
                    spawn = WithTrain(spawn, train, spawn.Turn, spawn.Rng);
                    if (spell.PendingCallbacks.Count > 0)
                    {
                        TrainCombatResult completed = TrainCombatModel.ApplyCharacterQueue(train, spell.PendingCallbacks.ToList());
                        if (!completed.Supported) return Unsupported(completed.UnsupportedReason!);
                        if (!RouteDeadUnits(train, completed)) return Unsupported("Unplayed card trigger death requires missing card routing.");
                        train = completed.State!;
                        otherPiles = train.Context!.OtherPiles?.ToArray() ?? otherPiles;
                        train = WithContext(train, train.Context!.WithOtherPiles(otherPiles), spawn.Turn == 0);
                        spawn = WithTrain(spawn, train, spawn.Turn, spawn.Rng);
                        if (completed.Outcome != RoomOutcome.Exchanged) triggerOutcome = completed.Outcome;
                    }
                    context = train.Context!;
                    if (Terminal(triggerOutcome)) return Finish(triggerOutcome, source.Energy, source.MoonPhase);
                }
            }
            context = train.Context!;
            CardGenerationResult unplayedConversion = RelicPerkeoModel.ApplyUnplayed(context);
            if (!unplayedConversion.Supported) return Unsupported(unplayedConversion.UnsupportedReason!);
            context = unplayedConversion.Context!;
            otherPiles = context.OtherPiles?.ToArray() ?? otherPiles;
            train = WithContext(train, context, spawn.Turn == 0);
            spawn = WithTrain(spawn, train, spawn.Turn, spawn.Rng);
            var ephemeralCardIds = new List<int>();
            var persistentCardIds = new List<int>();
            var infusionCardIds = new List<int>();
            foreach (CardToken card in context.Cards.Hand)
            {
                CardInstanceState? instance = context.FindCard(card.InstanceId);
                if (instance?.MaskDescriptor == null)
                {
                    if (source.ExternalInteractions.Any(IsDeferredPersistentDiscard))
                        return Unsupported("Persistent discard requires captured hand traits.");
                    continue;
                }
                try
                {
                    IReadOnlyList<string> traits = CardBranchMaskModel.Resolve(instance).Traits;
                    if (traits.Contains("CardTraitEphemeral", StringComparer.Ordinal))
                        ephemeralCardIds.Add(card.InstanceId);
                    if (traits.Contains("CardTraitPersistent", StringComparer.Ordinal) || traits.Contains("CardTraitFreeze", StringComparer.Ordinal) &&
                        !traits.Contains("CardTraitEphemeral", StringComparer.Ordinal))
                        persistentCardIds.Add(card.InstanceId);
                    if (traits.Contains("CardTraitInfusion", StringComparer.Ordinal))
                        infusionCardIds.Add(card.InstanceId);
                }
                catch (InvalidOperationException error) { return Unsupported(error.Message); }
            }
            CardCycleResult discard = CardCycleModel.DiscardHand(context.Cards, ephemeralCardIds, persistentCardIds, infusionCardIds);
            if (!discard.Supported) return Unsupported(discard.UnsupportedReason!);
            BattleStatistics? statistics = context.Statistics;
            var retainedIds = new HashSet<int>(persistentCardIds.Concat(infusionCardIds));
            foreach (CardToken card in context.Cards.Hand.Reverse().Where(card => !retainedIds.Contains(card.InstanceId)))
            {
                RelicCheaperSpellUpgradeResult discarded = RelicCheaperSpellUpgradeModel.CardDiscarded(context, card.InstanceId);
                if (!discarded.Supported) return Unsupported(discarded.UnsupportedReason!);
                context = discarded.Context;
                RelicCheaperSpellUpgradeResult randomDiscarded = RelicRandomHandUpgradeModel.Discarded(context, card.InstanceId);
                if (!randomDiscarded.Supported) return Unsupported(randomDiscarded.UnsupportedReason!);
                context = randomDiscarded.Context;
                var suppression = RelicSuppressExhaustModel.Discarded(context, out _, preview: context.IsolatedBattlePreview == true);
                if (!suppression.Supported) return Unsupported(suppression.UnsupportedReason!);
                context = suppression.Context;
            }
            train = WithContext(train, context, spawn.Turn == 0);
            foreach (CardToken card in context.Cards.Hand.Reverse().Where(card => !retainedIds.Contains(card.InstanceId)))
                statistics = statistics?.Increment(card.InstanceId, "TimesDiscarded");
            foreach (CardToken card in discard.PurgedCards)
            {
                if (statistics?.DeckCards?.Contains(card.InstanceId) == true && context.Resources != null)
                    context = context.WithResources(context.Resources.WithVisibleDeckCount(context.Resources.VisibleDeckCount - 1));
                statistics = statistics?.RemoveDeckCard(card.InstanceId);
            }
            bool conserveEnergy = context.Relics?.Any(relic => relic.EffectTypes.Contains(RelicResourceModel.ConserveEnergyEndOfTurn)) == true;
            context = context.WithStatistics(conserveEnergy ? statistics :
                statistics?.WithEndTurnEnergy(context.EnergyState == null ? source.Energy : context.QueryFrame!.Energy!.Value));
            var discardedIds = new HashSet<int>(context.Cards.Hand.Where(card => !retainedIds.Contains(card.InstanceId))
                .Select(card => card.InstanceId));
            context = context.WithCardInstances(context.CardInstances?.Select(card => discardedIds.Contains(card.InstanceId)
                ? card.OnDiscard(false, triggersOnUnplayed: CardRetainModel.TriggersOnUnplayed(card,
                    outputPlayRules?.Cards.FirstOrDefault(rule => rule.DataId == card.DataId))) : card).ToArray());
            foreach (int discardedId in discardedIds) context = ConduitModel.Discarded(context, discardedId);
            CardCycleState cards = discard.State!;
            if (discard.PurgedCards.Count > 0)
            {
                int purgedIndex = Array.FindIndex(otherPiles, pile => pile.Name == "Purged");
                if (purgedIndex < 0) return Unsupported("Missing purged card pile.");
                otherPiles[purgedIndex] = new CardPileState("Purged",
                    otherPiles[purgedIndex].Cards.Concat(discard.PurgedCards).ToArray());
                context = context.WithOtherPiles(otherPiles);
            }
            if (spawn.Turn == 0)
            {
                ShuffleResult<CardToken> shuffled = cards.Rng.Shuffle(cards.Draw.Concat(cards.Discard).ToArray());
                cards = new CardCycleState(cards.Hand, shuffled.Items, Array.Empty<CardToken>(), shuffled.State,
                    cards.DrawModifier, cards.ExternalInteractions, cards.BonusDraw, cards.NextDrawUpgrades, cards.NextUpgradeReferenceId, cards.DeckDrawDisabledThisTurn);
            }
            context = WithCards(context, cards);
            if (discardedIds.Count > 0) context = UnitStandbyModel.ReturnReady(CardExhaustModel.ReturnReady(context));
            // Native conservation sees hand callbacks and the previous EndTurnEnergy
            // statistic, then stores this turn's remaining energy before removal.
            if (conserveEnergy)
            {
                if (context.QueryFrame?.Energy == null) return Unsupported("Missing end-of-turn conservation energy.");
                int remaining = context.QueryFrame.Energy.Value;
                RelicResourceResult removal = RelicResourceModel.GetEnergyToRemoveAtTurnEnd(context, remaining, placement: spawn.Turn == 0);
                if (!removal.Supported) return Unsupported(removal.UnsupportedReason!);
                context = removal.Context!;
                context = context.WithStatistics(context.Statistics?.WithEndTurnEnergy(remaining));
                context = context.WithQueryFrame(context.QueryFrame!.With(energy: remaining - removal.Value));
            }
            else context = context.WithQueryFrame(context.QueryFrame?.With(energy: 0));
            context = EnergyModel.SetPhase(context, "Combat");
            train = WithContext(train, context, spawn.Turn == 0);
            TrainCombatResult combat = TrainCombatModel.ResolveCombat(train);
            if (!combat.Supported) return Unsupported(combat.UnsupportedReason!);
            if (!RouteDeadUnits(train, combat)) return Unsupported("Missing unit death event or card routing.");
            train = combat.State!;
            spawn = WithTrain(spawn, train, spawn.Turn, spawn.Rng);
            if (Terminal(combat.Outcome)) return Finish(combat.Outcome, 0, source.MoonPhase);
            train = WithContext(train, EnergyModel.SetPhase(train.Context!, "HeroTurn"), spawn.Turn == 0);
            TrainCombatResult ascended = TrainCombatModel.Ascend(train);
            if (!ascended.Supported) return Unsupported(ascended.UnsupportedReason!);
            if (!RouteDeadUnits(train, ascended)) return Unsupported("Missing unit death event or card routing.");
            if (outputPlayRules != null && ascended.DestroyedRoomIndexes.Count > 0)
            {
                outputPlayRules = outputPlayRules.WithRooms(outputPlayRules.Rooms.Select(room =>
                    ascended.DestroyedRoomIndexes.Contains(room.RoomIndex) ? room.WithEnabled(false) : room).ToArray());
            }
            train = ascended.State!;
            spawn = WithTrain(spawn, train, spawn.Turn, spawn.Rng);
            if (Terminal(ascended.Outcome)) return Finish(ascended.Outcome, 0, source.MoonPhase);
            train = WithContext(train, RoomAbilityModel.ChangeCooldown(EnergyModel.SetPhase(train.Context!, "EndMonsterTurn")), spawn.Turn == 0);
            spawn = WithTrain(spawn, train, spawn.Turn, spawn.Rng);
            if (spawn.Turn > 0)
            {
                EnemySpawnResult spawned = EnemySpawningModel.Spawn(spawn, true);
                if (!spawned.Supported) return Unsupported(spawned.UnsupportedReason!);
                spawn = spawned.State!; train = spawn.Train;
                if (Terminal(spawned.Outcome)) return Finish(spawned.Outcome, 0, source.MoonPhase);
            }
            string moon = source.MoonPhase == "Full" ? "New" : "Full";
            bool hellforgedMoonAdvanced = spawn.OuterBoss is { AssetKey: "Boss_Level9_Entropy_Top_V2", Phase: "Relentless" };
            if (hellforgedMoonAdvanced)
            {
                // Native EndMonsterTurn advances the moon before the potentially
                // terminal Hellforged post action, while the turn number is old.
                MoonPhaseTransitionResult advanced = MoonPhaseTransitionModel.Apply(train,
                    new CardActionEffect("AdvanceMoonPhase", "Room", 1, false, false, []), playerActionOnly: false);
                if (!advanced.Supported) return Unsupported(advanced.UnsupportedReason!);
                train = advanced.State!;
            }
            RoomOutcome bossPhaseOutcome = RoomOutcome.Exchanged;
            string? postBossError = ResolveOuterBoss(false, hellforgedMoonAdvanced ? moon : source.MoonPhase);
            if (postBossError != null) return Unsupported(postBossError);
            if (Terminal(bossPhaseOutcome)) return Finish(bossPhaseOutcome, 0, moon);
            context = train.Context!;
            int turn = checked(spawn.Turn + 1);
            if (hellforgedMoonAdvanced)
                train = WithContext(train, context.WithQueryFrame(context.QueryFrame?.With(turn: turn)), false);
            else if (source.MoonPhaseTrackingActive.HasValue)
            {
                train = WithContext(train, context.WithQueryFrame(context.QueryFrame?.With(
                    turn: turn, moonPhaseTrackingActive: context.QueryFrame?.MoonPhaseTrackingActive ?? source.MoonPhaseTrackingActive)), false);
                MoonPhaseTransitionResult advanced = MoonPhaseTransitionModel.Apply(train,
                    new CardActionEffect("AdvanceMoonPhase", "Room", 1, false, false, []),
                    playerActionOnly: false);
                if (!advanced.Supported) return Unsupported(advanced.UnsupportedReason!);
                train = advanced.State!;
            }
            else
            {
                train = WithContext(train, context.WithQueryFrame(context.QueryFrame?.With(
                    turn: turn, moonPhase: moon == "New" ? 1 : 2)), false);
                if (train.Context!.Resources != null)
                {
                    RelicResourceResult moonEnergy = RelicResourceModel.ApplyMoonPhaseEnergy(train.Context, moon,
                        preview: train.Rooms.Any(room => room.Preview));
                    if (!moonEnergy.Supported) return Unsupported(moonEnergy.UnsupportedReason!);
                    train = WithContext(train, moonEnergy.Context!, false);
                }
            }
            context = train.Context!;
            train = WithContext(train, RelicSpawnStatusModel.EndDuration(RelicSpawnStatusModel.EndDuration(
                context.WithStatistics(context.Statistics?.NextTurn(context.Gold)), "PreviousTurn"), "ThisTurn"), false);
            train = TrainCombatModel.ProcessRemovals(train);
            // RunCombat settles room order after incrementing the turn counter,
            // before the next turn's initial spawn and Spawning RNG reset.
            TrainCombatResult turnOrder = EnchantmentWorldModel.UpdateAll(train);
            if (!turnOrder.Supported) return Unsupported(turnOrder.UnsupportedReason!);
            train = turnOrder.State!;
            // Initial enemies appear at the start of turn one, before that turn resets Spawning RNG.
            if (turn == 1)
            {
                spawn = WithTrain(spawn, train, turn, spawn.Rng);
                EnemySpawnResult initial = EnemySpawningModel.Spawn(spawn, false);
                if (!initial.Supported) return Unsupported(initial.UnsupportedReason!);
                spawn = initial.State!; train = spawn.Train;
                if (Terminal(initial.Outcome)) return Finish(initial.Outcome, 0, moon);
                if (spawn.QueuedOuterBoss != null)
                {
                    string? birthError = ResolveOuterBossBirth(moon);
                    if (birthError != null) return Unsupported(birthError);
                }
                if (spawn.QueuedTrueFinalBoss != null)
                {
                    var streams = source.RngStreams.Select(stream => new BattleRngStream(stream.Name, stream.Seed,
                        stream.Name == "Battle" ? train.Context!.BattleRng : stream.Name == "CardDraw" ? train.Context!.Cards.Rng :
                        stream.Name == "Spawning" ? spawn.Rng : stream.State)).ToArray();
                    var boundary = new BattleTurnState(WithTrain(spawn, train, turn, spawn.Rng), train.Context!.QueryFrame?.Energy ?? 0,
                        source.EnergyPerTurn, source.DrawPerTurn, train.Context.QueryFrame?.ForgePoints ?? source.ForgePoints,
                        source.DragonsHoard, moon, streams, otherPiles, source.ExternalInteractions, outputPlayRules,
                        source.BattlePreviewEnabled, source.UiRngIsolated, source.CanonicalDecisionReferences,
                        source.CanonicalPhysicalReferences, train.Context.QueryFrame?.SelectedRoomIndex ?? source.SelectedRoom,
                        source.MoonPhaseTrackingActive);
                    BattleTurnResult born = TrueFinalBossSpawningModel.ApplyQueued(boundary);
                    if (!born.Supported) return Unsupported(born.UnsupportedReason!);
                    spawn = born.State!.Spawn; train = spawn.Train;
                    otherPiles = born.State.OtherPiles.ToArray(); outputPlayRules = born.State.PlayRules;
                }
                TrainCombatResult initialOrder = EnchantmentWorldModel.UpdateAll(train);
                if (!initialOrder.Supported) return Unsupported(initialOrder.UnsupportedReason!);
                train = initialOrder.State!;
                if (train.Context!.Relics?.Any(relic => relic.EffectTypes.Any(type =>
                    RelicResourceModel.IsOpeningCorruption(type) || type == RelicResourceModel.StartCombatEnergy || type == RelicOpeningSpawnModel.Effect || type == RelicOpeningDeckSpawnModel.Effect)) == true)
                {
                    TrainCombatResult opening = RelicResourceModel.ApplyOpeningCorruption(train, definitions: source.PlayRules);
                    if (!opening.Supported) return Unsupported(opening.UnsupportedReason!);
                    train = opening.State!;
                }
            }
            spawn = WithTrain(spawn, train, turn, UnityRng.Seed(unchecked(spawningStream.Seed + turn)));
            train = WithContext(train, EnergyModel.SetPhase(train.Context!, "PreCombat"), false);
            // Native queues each team's PreCombat after spawning/rollover, before energy and draw.
            foreach (CombatTeam team in new[] { CombatTeam.Player, CombatTeam.Enemy })
            {
                TrainCombatResult triggers = TrainCombatModel.PreCombat(train, team);
                if (!triggers.Supported) return Unsupported(triggers.UnsupportedReason!);
                if (!RouteDeadUnits(train, triggers)) return Unsupported("Pre-combat deaths require standby card routing.");
                train = triggers.State!;
                spawn = WithTrain(spawn, train, turn, spawn.Rng);
                if (Terminal(triggers.Outcome)) return Finish(triggers.Outcome, 0, moon);
                if (team == CombatTeam.Enemy && spawn.OuterBoss != null && outputPlayRules?.Rooms.All(room => room.IsPyre || !room.Enabled) == true)
                {
                    var streams = source.RngStreams.Select(stream => new BattleRngStream(stream.Name, stream.Seed,
                        stream.Name == "BossActions" && outerBossRng.HasValue ? outerBossRng.Value :
                        stream.Name == "Battle" ? train.Context!.BattleRng : stream.Name == "CardDraw" ? train.Context!.Cards.Rng :
                        stream.Name == "Spawning" ? spawn.Rng : stream.State)).ToArray();
                    var boundary = new BattleTurnState(spawn, train.Context!.QueryFrame?.Energy ?? 0, source.EnergyPerTurn,
                        source.DrawPerTurn, train.Context.QueryFrame?.ForgePoints ?? source.ForgePoints, source.DragonsHoard,
                        moon, streams, otherPiles, source.ExternalInteractions, outputPlayRules, source.BattlePreviewEnabled,
                        source.UiRngIsolated, source.CanonicalDecisionReferences, source.CanonicalPhysicalReferences,
                        train.Context.QueryFrame?.SelectedRoomIndex ?? source.SelectedRoom, source.MoonPhaseTrackingActive);
                    OuterBossPyreResult tower = OuterBossPyreModel.Apply(new OuterBossPhaseState(boundary, spawn.OuterBoss));
                    if (!tower.Supported) return Unsupported(tower.UnsupportedReason!);
                    if (!RouteDeadUnits(train, new TrainCombatResult(tower.State!.Spawn.Train, tower.Outcome, Array.Empty<RoomCombatResult>())))
                        return Unsupported("Outer boss Pyre combat requires native death/card routing.");
                    spawn = tower.State.Spawn; train = spawn.Train; outputPlayRules = tower.State.PlayRules;
                    if (Terminal(tower.Outcome)) return Finish(tower.Outcome, 0, moon);
                    return Unsupported("Nonterminal outer-boss Pyre combat requires native continuation calibration.");
                }
            }
            context = train.Context!;
            if (context.Cards.DeckDrawDisabledThisTurn.HasValue)
                context = context.WithCards(context.Cards.WithDrawDisabled(false));
            RelicFirstCardCostResult sequenceStart = RelicCardTypeSequenceModel.StartTurn(context);
            if (!sequenceStart.Supported) return Unsupported(sequenceStart.UnsupportedReason!);
            context = sequenceStart.Context;
            RelicFirstCardCostResult firstCardCostBeforeDraw = RelicFirstCardCostModel.StartTurnBeforeDraw(context,
                source.PlayRules);
            if (!firstCardCostBeforeDraw.Supported) return Unsupported(firstCardCostBeforeDraw.UnsupportedReason!);
            context = firstCardCostBeforeDraw.Context;
            if (train.Movement.Any(rule => rule.CompanionBoss))
            {
                train = WithContext(train, EnergyModel.SetPhase(context, "BossActionPreCombat"), false);
                spawn = WithTrain(spawn, train, turn, spawn.Rng);
                TrainCombatResult bossAction = CompanionBossModel.Resolve(spawn, true);
                if (!bossAction.Supported) return Unsupported(bossAction.UnsupportedReason!);
                if (!RouteDeadUnits(train, bossAction)) return Unsupported("Companion movement requires native death/card routing.");
                train = bossAction.State!; spawn = WithTrain(spawn, train, turn, spawn.Rng);
                if (Terminal(bossAction.Outcome)) return Finish(bossAction.Outcome, 0, moon);
                context = train.Context!;
            }
            string? preBossError = ResolveOuterBoss(true, moon);
            if (preBossError != null) return Unsupported(preBossError);
            context = train.Context!;
            if (context.Resources != null)
            {
                RelicResourceResult resourceEnergy = RelicResourceModel.StartTurn(context,
                    unitRoomEnergy: UnitRoomResourceModel.Amount(train, UnitRoomResourceModel.Energy));
                if (!resourceEnergy.Supported) return Unsupported(resourceEnergy.UnsupportedReason!);
                context = resourceEnergy.Context!;
            }
            else context = EnergyModel.StartTurn(context, source.EnergyPerTurn + UnitRoomResourceModel.Amount(train, UnitRoomResourceModel.Energy));
            train = WithContext(train, context, false);
            RelicSoloUnitUpgradeResult soloUnitUpgrades = RelicSoloUnitUpgradeModel.Apply(train);
            if (!soloUnitUpgrades.Supported) return Unsupported(soloUnitUpgrades.UnsupportedReason!);
            train = soloUnitUpgrades.State!;
            int startTurnRoom = train.Context!.QueryFrame?.SelectedRoomIndex ?? source.SelectedRoom;
            RoomCapacityResult singleFloorCapacity = RelicSingleFloorCapacityModel.Apply(train.Context!, turn);
            if (!singleFloorCapacity.Supported) return Unsupported(singleFloorCapacity.UnsupportedReason!);
            train = WithContext(train, singleFloorCapacity.Context!, false);
            RelicStartTurnHealResult startTurnHealing = RelicStartTurnHealModel.Apply(train, turn, startTurnRoom);
            if (!startTurnHealing.Supported) return Unsupported(startTurnHealing.UnsupportedReason!);
            train = startTurnHealing.State!;
            RelicStartTurnStatusResult startTurnStatus = RelicStartTurnStatusModel.Apply(train, outputPlayRules?.Rooms, turn);
            if (!startTurnStatus.Supported) return Unsupported(startTurnStatus.UnsupportedReason!);
            train = startTurnStatus.State!;
            spawn = WithTrain(spawn, train, turn, spawn.Rng);
            context = train.Context!;
            context = UnitStandbyModel.ReturnReady(context);
            context = EquipmentModel.ReturnUnattached(context, new HashSet<int>(train.Rooms.SelectMany(room => room.Units).Select(unit => unit.Id)));
            train = WithContext(train, context, false);
            if (context.Relics?.Any(owner => owner.EffectTypes.Contains(RelicResourceModel.PileStartTurn)) == true)
            {
                TrainSpellResult pileStart = RelicPileStartTurnModel.Apply(train, outputPlayRules);
                if (!pileStart.Supported) return Unsupported(pileStart.UnsupportedReason!);
                train = pileStart.State!;
                context = train.Context!;
                spawn = WithTrain(spawn, train, turn, spawn.Rng);
            }
            if (context.Relics?.Any(owner => owner.EffectTypes.Contains(RelicTurnDrawModel.FullHealth)) == true)
            {
                var refresh = RelicTurnDrawModel.BeforeDraw(context, train.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre).Health);
                if (!refresh.Supported) return Unsupported(refresh.UnsupportedReason!);
                var count = RelicResourceModel.GetDrawCount(refresh.Context!);
                if (!count.Supported) return Unsupported(count.UnsupportedReason!);
                context = count.Context!; drawPerTurn = count.Value;
            }
            // Deployment is turn zero. Native DrawOpeningHand runs on turn one:
            TypedDrawResult held = CardRetainModel.PreDrawHand(context, outputPlayRules, opening: turn == 1);
            if (!held.Supported) return Unsupported(held.UnsupportedReason!);
            context = held.Context!;
            // Opening-hand trait callbacks run after Holdover's ordinary pre-draw.
            // each Intrinsic card leaves the deck before the ordinary hand draw.
            if (turn == 1)
            {
                foreach (CardToken token in context.Cards.Draw.ToArray())
                {
                    CardInstanceState? intrinsic = context.FindCard(token.InstanceId);
                    if (intrinsic?.MaskDescriptor == null ||
                        !CardBranchMaskModel.Resolve(intrinsic).Traits.Contains("CardTraitIntrinsicState")) continue;
                    TypedDrawResult opening = NextDrawUpgradeModel.DrawSpecific(context, token.InstanceId, 0, source.PlayRules);
                    if (!opening.Supported) return Unsupported(opening.UnsupportedReason!);
                    context = opening.Context!;
                }
            }
            int roomDrawCount = Math.Max(0, unchecked(drawPerTurn + UnitRoomResourceModel.Amount(train, UnitRoomResourceModel.Draw)));
            if (context.OtherPiles != null) otherPiles = context.OtherPiles.ToArray();
            if (RelicCorruptedAngelsModel.Enabled(context))
            {
                TypedDrawResult interleaved = RelicCorruptedAngelsModel.DrawHand(context, roomDrawCount, source.PlayRules);
                if (!interleaved.Supported) return Unsupported(interleaved.UnsupportedReason!);
                context = interleaved.Context!;
            }
            else
            {
                CardCycleResult drawn = CardCycleModel.DrawHand(context.Cards, roomDrawCount, context.MaxHandSize);
                if (!drawn.Supported) return Unsupported(drawn.UnsupportedReason!);
                var existingHand = new HashSet<int>(context.Cards.Hand.Select(card => card.InstanceId));
                statistics = context.Statistics;
                foreach (CardToken card in drawn.State!.Hand.Reverse().Where(card => !existingHand.Contains(card.InstanceId)))
                    statistics = statistics?.Increment(card.InstanceId, "TimesDrawn");
                context = context.WithStatistics(statistics);
                context = WithCards(context, drawn.State!);
                // Draw upgrades precede ICardDrawnRelicEffect and the subsequent
                // card-added-to-hand notification in CardManager.DrawSpecificCard.
                context = BonusDrawModel.ApplyUpgrades(context, drawn, source.PlayRules, out string? bonusError);
                if (bonusError != null) return Unsupported(bonusError);
                foreach (CardToken card in drawn.State!.Hand.Reverse().Where(card => !existingHand.Contains(card.InstanceId)))
                {
                    context = RelicExpensiveCardUpgradeModel.Drawn(context, new[] { card.InstanceId }, out string? drawCallbackError);
                    if (drawCallbackError != null) return Unsupported(drawCallbackError);
                    RelicFirstCardCostResult cardAdded = RelicFirstCardCostModel.ApplyOnCardAdded(context, card.InstanceId,
                        source.PlayRules);
                    if (!cardAdded.Supported) return Unsupported(cardAdded.UnsupportedReason!);
                    context = cardAdded.Context;
                }
            }
            train = WithContext(train, context, false);
            if (turn == 1 && context.Relics?.Any(owner => owner.EffectTypes.Contains(RelicTurnDrawModel.Opening)) == true)
            {
                var openingDraw = RelicTurnDrawModel.PostOpening(context, definitions: source.PlayRules);
                if (!openingDraw.Supported) return Unsupported(openingDraw.UnsupportedReason!);
                context = openingDraw.Context!; train = WithContext(train, context, false);
            }
            if (turn == 1 && context.Relics?.Any(owner => owner.EffectTypes.Contains(RelicJudgesScalesModel.Effect)) == true)
            {
                RelicJudgesScalesResult replacements = RelicJudgesScalesModel.Apply(context, source.PlayRules);
                if (!replacements.Supported) return Unsupported(replacements.UnsupportedReason!);
                context = replacements.Context!; train = WithContext(train, context, false);
            }
            if (turn == 1 && train.Context!.Relics?.Any(relic => relic.EffectTypes.Any(RelicResourceModel.IsOpeningCorruption)) == true)
            {
                TrainCombatResult opening = RelicResourceModel.ApplyOpeningCorruption(train, postDraw: true);
                if (!opening.Supported) return Unsupported(opening.UnsupportedReason!);
                train = opening.State!;
            }
            if (train.Context!.Relics?.Any(relic => relic.EffectTypes.Any(RelicDeckTurnDrawModel.IsAfterDraw)) == true)
            {
                var generated = RelicDeckTurnDrawModel.ApplyAfterDraw(train.Context, outputPlayRules);
                if (!generated.Supported) return Unsupported(generated.UnsupportedReason!);
                train = WithContext(train, generated.Context!, false);
            }
            ScorchStatusResult scorched = ScorchStatusModel.StartTurn(train);
            if (!scorched.Supported) return Unsupported(scorched.UnsupportedReason!);
            train = scorched.State!;
            // Native drains queued room support effects after drawing and
            // turn-begin statuses. Initial enemy focus can leave an Eclipse
            // floor after the global phase refresh; retained spells must then
            // understand the final selected room, not their pre-spawn floor.
            CombatContext refreshedHand = CardMoonUpgradeModel.RefreshHand(train.Context!, out string? handMoonError);
            if (handMoonError != null) return Unsupported(handMoonError);
            train = WithContext(train, refreshedHand, false);
            spawn = WithTrain(spawn, train, turn, spawn.Rng);
            return Finish(RoomOutcome.Exchanged, train.Context!.QueryFrame?.Energy ?? source.EnergyPerTurn, moon);

            BattleTurnResult Finish(RoomOutcome outcome, int energy, string phase)
            {
                CombatContext settled = spawn.Train.Context!;
                if (settled.EnergyState != null)
                    energy = settled.QueryFrame!.Energy!.Value;
                spawn = WithTrain(spawn, CardSpellModel.WithContext(spawn.Train,
                    settled.WithQueryFrame(settled.QueryFrame?.With(energy: energy,
                        runningCombat: outcome != RoomOutcome.BattleWon && outcome != RoomOutcome.PlayerDefeated))),
                    spawn.Turn, spawn.Rng);
                if (source.BattlePreviewEnabled && !Terminal(outcome))
                {
                    TrainCombatResult preview = BattlePreviewModel.Refresh(spawn.Train, outputPlayRules);
                    if (!preview.Supported) return Unsupported(preview.UnsupportedReason!);
                    spawn = WithTrain(spawn, preview.State!, spawn.Turn, spawn.Rng);
                }
                if (source.CanonicalDecisionReferences)
                    spawn = WithTrain(spawn, TrainCombatModel.CompleteFrameRemovals(spawn.Train), spawn.Turn, spawn.Rng);
                CombatContext finalContext = spawn.Train.Context!;
                // Native decision captures refresh room rules after all turn callbacks,
                // including PreCombat capacity changes and terminal transitions.
                if (finalContext.RoomCapacities != null)
                    outputPlayRules = outputPlayRules?.WithCapacities(finalContext.RoomCapacities);
                BattleRngStream[] streams = source.RngStreams.Select(stream => new BattleRngStream(stream.Name, stream.Seed,
                    stream.Name == "Battle" ? finalContext.BattleRng : stream.Name == "CardDraw" ? finalContext.Cards.Rng :
                    stream.Name == "Spawning" ? spawn.Rng : stream.Name == "BossActions" && outerBossRng.HasValue ? outerBossRng.Value : stream.State)).ToArray();
                CardPileState[] piles = Terminal(outcome) && outcome != RoomOutcome.Stalemate &&
                    finalContext.KillCamActivated != false
                    ? otherPiles.Select(CardPileModel.Clear).ToArray()
                    : otherPiles;
                spawn = WithTrain(spawn, CardSpellModel.WithContext(spawn.Train, finalContext.WithOtherPiles(piles)),
                    spawn.Turn, spawn.Rng);
                int selectedRoom = finalContext.QueryFrame?.SelectedRoomIndex ?? source.SelectedRoom;
                if (finalContext.QueryFrame?.SelectedRoomIndex == null && selectedRoom >= 0)
                {
                    var priorRooms = source.Spawn.Train.Rooms.SelectMany(room => room.Units.Select(unit => (unit.Id, room.RoomIndex)))
                        .ToDictionary(item => item.Id, item => item.RoomIndex);
                    int latestChangedUnit = -1;
                    foreach (RoomCombatState room in spawn.Train.Rooms)
                    foreach (CombatUnit unit in room.Units)
                    {
                        if ((!priorRooms.TryGetValue(unit.Id, out int priorRoom) || priorRoom != room.RoomIndex) &&
                            unit.Id > latestChangedUnit)
                        {
                            latestChangedUnit = unit.Id;
                            selectedRoom = room.RoomIndex;
                        }
                    }
                }
                return new BattleTurnResult(new BattleTurnState(spawn, energy, source.EnergyPerTurn,
                    Math.Max(0, drawPerTurn + UnitRoomResourceModel.Amount(spawn.Train, UnitRoomResourceModel.Draw)),
                    finalContext.QueryFrame?.ForgePoints ?? source.ForgePoints, source.DragonsHoard, phase, streams, piles, source.ExternalInteractions, outputPlayRules,
                    source.BattlePreviewEnabled, source.UiRngIsolated, source.CanonicalDecisionReferences, source.CanonicalPhysicalReferences,
                    selectedRoom, finalContext.QueryFrame?.MoonPhaseTrackingActive ?? source.MoonPhaseTrackingActive), outcome);
            }

            string? ResolveOuterBossBirth(string moonPhase)
            {
                if (outputPlayRules == null || !outerBossRng.HasValue) return "Missing outer-boss birth rules or random stream.";
                var streams = source.RngStreams.Select(stream => new BattleRngStream(stream.Name, stream.Seed,
                    stream.Name == "BossActions" ? outerBossRng.Value : stream.Name == "Battle" ? train.Context!.BattleRng :
                    stream.Name == "CardDraw" ? train.Context!.Cards.Rng : stream.Name == "Spawning" ? spawn.Rng : stream.State)).ToArray();
                var boundary = new BattleTurnState(spawn, train.Context!.QueryFrame?.Energy ?? 0, source.EnergyPerTurn,
                    source.DrawPerTurn, train.Context.QueryFrame?.ForgePoints ?? source.ForgePoints, source.DragonsHoard,
                    moonPhase, streams, otherPiles, source.ExternalInteractions, outputPlayRules, source.BattlePreviewEnabled,
                    source.UiRngIsolated, source.CanonicalDecisionReferences, source.CanonicalPhysicalReferences,
                    train.Context.QueryFrame?.SelectedRoomIndex ?? source.SelectedRoom, source.MoonPhaseTrackingActive);
                OuterBossInstantiationResult created = OuterBossInstantiationModel.Apply(new OuterBossInstantiationState(boundary, spawn.QueuedOuterBoss!));
                if (!created.Supported) return created.UnsupportedReason;
                OuterBossPhaseResult placed = OuterBossBirthModel.Apply(created.State!);
                if (!placed.Supported) return placed.UnsupportedReason;
                spawn = placed.State!.Battle.Spawn; train = spawn.Train;
                outputPlayRules = placed.State.Battle.PlayRules;
                otherPiles = placed.State.Battle.OtherPiles.ToArray();
                outerBossRng = placed.State.Battle.RngStreams.Single(stream => stream.Name == "BossActions").State;
                return null;
            }

            string? ResolveOuterBoss(bool preCombat, string moonPhase)
            {
                if (spawn.OuterBoss == null) return null;
                if (outputPlayRules == null || !outerBossRng.HasValue) return "Missing outer-boss room rules or random stream.";
                train = WithContext(train, EnergyModel.SetPhase(train.Context!, preCombat ? "BossActionPreCombat" : "BossActionPostCombat"), spawn.Turn == 0);
                spawn = WithTrain(spawn, train, spawn.Turn, spawn.Rng);
                var streams = source.RngStreams.Select(stream => new BattleRngStream(stream.Name, stream.Seed,
                    stream.Name == "BossActions" ? outerBossRng.Value : stream.Name == "Battle" ? train.Context!.BattleRng :
                    stream.Name == "CardDraw" ? train.Context!.Cards.Rng : stream.Name == "Spawning" ? spawn.Rng : stream.State)).ToArray();
                var boundary = new BattleTurnState(spawn, train.Context!.QueryFrame?.Energy ?? 0, source.EnergyPerTurn,
                    source.DrawPerTurn, train.Context.QueryFrame?.ForgePoints ?? source.ForgePoints, source.DragonsHoard,
                    moonPhase, streams, otherPiles, source.ExternalInteractions, outputPlayRules, source.BattlePreviewEnabled,
                    source.UiRngIsolated, source.CanonicalDecisionReferences, source.CanonicalPhysicalReferences,
                    train.Context.QueryFrame?.SelectedRoomIndex ?? source.SelectedRoom, source.MoonPhaseTrackingActive);
                OuterBossPhaseResult action = OuterBossPhaseModel.Apply(new OuterBossPhaseState(boundary, spawn.OuterBoss!, spawn.TrueFinalBoss), preCombat);
                if (!action.Supported) return action.UnsupportedReason;
                bossPhaseOutcome = action.Outcome;
                TrainCombatState prior = train;
                spawn = action.State!.Battle.Spawn; train = spawn.Train;
                if (!RouteDeadUnits(prior, new TrainCombatResult(train, RoomOutcome.Exchanged, Array.Empty<RoomCombatResult>())))
                    return "Outer boss action requires native unit-death card routing.";
                outputPlayRules = action.State.Battle.PlayRules;
                outerBossRng = action.State.Battle.RngStreams.Single(stream => stream.Name == "BossActions").State;
                return null;
            }

            bool RouteDeadUnits(TrainCombatState before, TrainCombatResult result)
            {
                if (result.State!.Context!.OtherPiles != null)
                {
                    otherPiles = result.State.Context.OtherPiles.ToArray();
                    return true;
                }
                var alive = new HashSet<int>(result.State!.Rooms.SelectMany(room => room.Units).Select(unit => unit.Id));
                var dead = before.Rooms.SelectMany(room => room.Units)
                    .Where(unit => unit.Team == CombatTeam.Player && unit.SpawnerCardId > 0 && !alive.Contains(unit.Id))
                    .ToDictionary(unit => unit.Id);
                if (dead.Count == 0) return true;
                int[] orderedDeaths = result.RoomResults.SelectMany(room => room.Events)
                    .Where(item => item.Kind == "Death" || item.Kind == "Despawn").Select(item => item.Target)
                    .Where(dead.ContainsKey).Distinct().ToArray();
                if (orderedDeaths.Length != dead.Count) return false;
                int[] deadCards = orderedDeaths.Select(id => dead[id].SpawnerCardId).ToArray();
                var standby = otherPiles.FirstOrDefault(pile => pile.Name == "Standby");
                var exhausted = otherPiles.FirstOrDefault(pile => pile.Name == "Exhausted")?.Cards.ToList();
                if (standby == null || exhausted == null) return false;
                foreach (int cardId in deadCards)
                {
                    CardToken? card = standby.Cards.FirstOrDefault(item => item.InstanceId == cardId);
                    if (card == null) return false;
                    standby = CardPileModel.Remove(standby, cardId); exhausted.Add(card);
                }
                otherPiles = otherPiles.Select(pile => pile.Name == "Standby" ? standby :
                    pile.Name == "Exhausted" ? new CardPileState(pile.Name, exhausted) : pile).ToArray();
                return true;
            }
        }
        private static CombatContext WithCards(CombatContext context, CardCycleState cards) => new CombatContext(cards,
            context.BattleRng, context.Gold, context.NextCardId, context.MaxHandSize, context.StatusRules, context.Statistics, context.CardInstances,
            context.CardRegistry, context.AllScenarioBossesDead, context.NextAddedTemporaryUpgrades, context.OtherPiles, context.QueryFrame, context.KillCamActivated, context.MagicPower, context.IsolatedBattlePreview, context.EnergyState, context.RoomCapacities, context.AbilityCardCache, context.LastAbilityActivatorUnitId, context.PermanentlyDisabledAbilities, context.LastSpawnedUnitId, context.NextUnitId, context.SpawnPoints, context.SummonCatalog, context.Enchantments, context.PurifyBlockedTriggers, context.Relics, context.TriggerCounts, context.Resources, context.MixState, context.ConduitRetained, context.LifeLinks);
        private static TrainCombatState WithContext(TrainCombatState train, CombatContext context, bool deployment) =>
            new TrainCombatState(train.Rooms.Select(room => new RoomCombatState(room.RoomIndex, deployment, room.Units,
                room.ExternalInteractions, context, room.Preview, room.ActiveCompanionBossIds)).ToArray(),
                train.Movement, train.EnemySlotsPerRoom, context);
        private static EnemySpawnState WithTrain(EnemySpawnState spawn, TrainCombatState train, int turn, UnityRng rng) =>
            new EnemySpawnState(train, spawn.Waves, spawn.SelectedGroups, spawn.Phase, spawn.Looping, rng, train.Context?.NextUnitId ?? spawn.NextUnitId,
                spawn.Treasures, spawn.TreasuresRemaining, spawn.TreasureEnabled, spawn.FirstTreasureTurn,
                spawn.FirstTreasureRoom, turn, spawn.ExternalInteractions, spawn.CanonicalDecisionReferences, spawn.PendingDestroyedUnitIds,
                spawn.OuterBoss is { AssetKey: "Boss_Level9_Entropy_Top_V2" } && !train.Rooms.Any(room => room.Units.Any(unit => unit.Id == spawn.OuterBoss.UnitId))
                    ? null : spawn.OuterBoss, spawn.QueuedOuterBoss,
                spawn.QueuedTrueFinalBoss, spawn.TrueFinalBoss);
        private static bool Terminal(RoomOutcome outcome) => outcome == RoomOutcome.BattleWon ||
            outcome == RoomOutcome.PlayerDefeated || outcome == RoomOutcome.Stalemate;
        private static BattleTurnResult Unsupported(string reason) => new BattleTurnResult(null, RoomOutcome.Unsupported, reason);
    }
}
