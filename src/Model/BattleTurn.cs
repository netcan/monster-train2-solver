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
        public CardPileState(string name, IReadOnlyList<CardToken> cards, IReadOnlyList<int>? entrySlots = null,
            IReadOnlyList<int>? freeSlots = null, IReadOnlyList<EquipmentStandbyCondition>? equipmentConditions = null,
            IReadOnlyList<UnitStandbyCondition>? unitConditions = null)
        {
            Name = name; Cards = Array.AsReadOnly(cards.ToArray());
            EntrySlots = entrySlots == null ? null : Array.AsReadOnly(entrySlots.ToArray());
            FreeSlots = freeSlots == null ? null : Array.AsReadOnly(freeSlots.ToArray());
            EquipmentConditions = equipmentConditions == null ? null : Array.AsReadOnly(equipmentConditions
                .OrderBy(condition => Array.FindIndex(cards.ToArray(), card => card.InstanceId == condition.CardId)).ToArray());
            UnitConditions = unitConditions == null ? null : Array.AsReadOnly(unitConditions
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
            bool canonicalPhysicalReferences = false, int selectedRoom = -1)
        {
            Spawn = spawn; Energy = energy; EnergyPerTurn = energyPerTurn; DrawPerTurn = drawPerTurn;
            ForgePoints = forgePoints; DragonsHoard = dragonsHoard; MoonPhase = moonPhase;
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
        // Room changes refresh the native combat preview before the selected action.
        // Older captures without a selected room remain explicitly unknown.
        public static BattleTurnResult SelectRoom(BattleTurnState source, int roomIndex)
        {
            if (source.PlayRules == null || source.Spawn.Train.Context == null)
                return Unsupported("Missing room-selection rules or battle context.");
            if (!source.PlayRules.Rooms.Any(room => room.RoomIndex == roomIndex))
                return Unsupported("Selected room is not present in the captured rules.");
            if (source.SelectedRoom == roomIndex)
                return new BattleTurnResult(source, RoomOutcome.Exchanged);
            if (source.ExternalInteractions.Count > 0)
                return Unsupported(string.Join("; ", source.ExternalInteractions));
            TrainCombatState train = source.Spawn.Train;
            if (train.Rooms.Any(room => room.ExternalInteractions.Count > 0))
                return Unsupported(string.Join("; ", train.Rooms.SelectMany(room => room.ExternalInteractions).Distinct()));
            string? relicError = RelicModel.Validate(train.Context);
            if (relicError != null) return Unsupported(relicError);
            if (source.SelectedRoom >= 0 && !source.PlayRules.Rooms.Any(room => room.RoomIndex == source.SelectedRoom))
                return Unsupported("Current selected room is not present in the captured rules.");

            if (source.SelectedRoom >= 0 && source.BattlePreviewEnabled)
            {
                TrainCombatResult preview = BattlePreviewModel.Refresh(train);
                if (!preview.Supported) return Unsupported(preview.UnsupportedReason!);
                train = preview.State!;
            }
            EnemySpawnState old = source.Spawn;
            var spawn = new EnemySpawnState(train, old.Waves, old.SelectedGroups, old.Phase, old.Looping,
                old.Rng, train.Context?.NextUnitId ?? old.NextUnitId, old.Treasures, old.TreasuresRemaining,
                old.TreasureEnabled, old.FirstTreasureTurn, old.FirstTreasureRoom, old.Turn, old.ExternalInteractions,
                old.CanonicalDecisionReferences, old.PendingDestroyedUnitIds);
            BattleRngStream[] streams = source.RngStreams.Select(stream => new BattleRngStream(stream.Name, stream.Seed,
                stream.Name == "Battle" ? train.Context!.BattleRng : stream.State)).ToArray();
            return new BattleTurnResult(new BattleTurnState(spawn, source.Energy, source.EnergyPerTurn,
                source.DrawPerTurn, source.ForgePoints, source.DragonsHoard, source.MoonPhase, streams,
                source.OtherPiles, source.ExternalInteractions, source.PlayRules, source.BattlePreviewEnabled,
                source.UiRngIsolated, source.CanonicalDecisionReferences, source.CanonicalPhysicalReferences, roomIndex),
                RoomOutcome.Exchanged);
        }

        // One quiet player decision point through EndTurn to the next decision or terminal battle.
        public static BattleTurnResult EndTurn(BattleTurnState source)
        {
            if (source.ExternalInteractions.Count > 0) return Unsupported(string.Join("; ", source.ExternalInteractions));
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
                .Any(unit => !standbyCards.Contains(unit.SpawnerCardId) && !((unit.Status("cardless")?.Stacks > 0 || unit.Modifiers?.IsClone == true ||
                    source.OtherPiles.Any(pile => pile.UnitConditions?.Any(binding => binding.HostUnitId == unit.Id) == true)) &&
                    birthContext.CardInstances != null && !birthContext.CardInstances.Any(card => card.InstanceId == unit.SpawnerCardId) &&
                    birthContext.CardRegistry?.Any(card => card.InstanceId == unit.SpawnerCardId) == true)))
                return Unsupported("A living unit's spawner card is missing from standby.");
            if (source.RngStreams.Select(stream => stream.Name).Distinct().Count() != source.RngStreams.Count)
                return Unsupported("Duplicate gameplay RNG streams.");
            BattleRngStream? spawningStream = source.RngStreams.FirstOrDefault(stream => stream.Name == "Spawning");
            if (spawningStream == null) return Unsupported("Missing spawning seed.");
            EnemySpawnState spawn = source.Spawn;
            string? uiRangeError = CardSpellModel.UnisolatedUiRangeReason(source);
            if (uiRangeError != null) return Unsupported(uiRangeError);
            CardPileState[] otherPiles = source.OtherPiles.ToArray();
            TrainCombatState train = spawn.Train;
            CombatContext context = train.Context!.WithOtherPiles(otherPiles);
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
            CardCycleResult discard = CardCycleModel.DiscardHand(context.Cards);
            if (!discard.Supported) return Unsupported(discard.UnsupportedReason!);
            BattleStatistics? statistics = context.Statistics;
            foreach (CardToken card in context.Cards.Hand.Reverse()) statistics = statistics?.Increment(card.InstanceId, "TimesDiscarded");
            context = context.WithStatistics(statistics?.WithEndTurnEnergy(context.EnergyState == null ? source.Energy : context.QueryFrame!.Energy!.Value));
            var discardedIds = new HashSet<int>(context.Cards.Hand.Select(card => card.InstanceId));
            context = context.WithCardInstances(context.CardInstances?.Select(card => discardedIds.Contains(card.InstanceId)
                ? card.OnDiscard(false) : card).ToArray());
            CardCycleState cards = discard.State!;
            if (spawn.Turn == 0)
            {
                ShuffleResult<CardToken> shuffled = cards.Rng.Shuffle(cards.Draw.Concat(cards.Discard).ToArray());
                cards = new CardCycleState(cards.Hand, shuffled.Items, Array.Empty<CardToken>(), shuffled.State,
                    cards.DrawModifier, cards.ExternalInteractions, cards.BonusDraw);
            }
            context = WithCards(context, cards);
            if (discardedIds.Count > 0) context = UnitStandbyModel.ReturnReady(context);
            // Hand callbacks precede the native energy removal; combat sees zero energy.
            context = context.WithQueryFrame(context.QueryFrame?.With(energy: 0));
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
            train = ascended.State!;
            spawn = WithTrain(spawn, train, spawn.Turn, spawn.Rng);
            if (Terminal(ascended.Outcome)) return Finish(ascended.Outcome, 0, source.MoonPhase);
            train = WithContext(train, EnergyModel.SetPhase(train.Context!, "EndMonsterTurn"), spawn.Turn == 0);
            spawn = WithTrain(spawn, train, spawn.Turn, spawn.Rng);
            if (spawn.Turn > 0)
            {
                EnemySpawnResult spawned = EnemySpawningModel.Spawn(spawn, true);
                if (!spawned.Supported) return Unsupported(spawned.UnsupportedReason!);
                spawn = spawned.State!; train = spawn.Train;
                if (Terminal(spawned.Outcome)) return Finish(spawned.Outcome, 0, source.MoonPhase);
            }
            string moon = source.MoonPhase == "Full" ? "New" : "Full";
            context = train.Context!;
            train = WithContext(train, RelicSpawnStatusModel.EndDuration(RelicSpawnStatusModel.EndDuration(context.WithStatistics(context.Statistics?.NextTurn(context.Gold)), "PreviousTurn"), "ThisTurn"), false);
            train = TrainCombatModel.ProcessRemovals(train);
            int turn = checked(spawn.Turn + 1);
            train = WithContext(train, train.Context!.WithQueryFrame(train.Context.QueryFrame?.With(
                turn: turn, moonPhase: moon == "New" ? 1 : 2)), false);
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
                TrainCombatResult initialOrder = EnchantmentWorldModel.UpdateAll(train);
                if (!initialOrder.Supported) return Unsupported(initialOrder.UnsupportedReason!);
                train = initialOrder.State!;
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
            }
            context = train.Context!;
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
            context = EnergyModel.StartTurn(context, source.EnergyPerTurn);
            context = UnitStandbyModel.ReturnReady(context);
            context = EquipmentModel.ReturnUnattached(context, new HashSet<int>(train.Rooms.SelectMany(room => room.Units).Select(unit => unit.Id)));
            if (context.OtherPiles != null) otherPiles = context.OtherPiles.ToArray();
            CardCycleResult drawn = CardCycleModel.DrawHand(context.Cards, source.DrawPerTurn, context.MaxHandSize);
            if (!drawn.Supported) return Unsupported(drawn.UnsupportedReason!);
            var existingHand = new HashSet<int>(context.Cards.Hand.Select(card => card.InstanceId));
            statistics = context.Statistics;
            foreach (CardToken card in drawn.State!.Hand.Reverse().Where(card => !existingHand.Contains(card.InstanceId)))
                statistics = statistics?.Increment(card.InstanceId, "TimesDrawn");
            context = context.WithStatistics(statistics);
            context = WithCards(context, drawn.State!);
            context = BonusDrawModel.ApplyUpgrades(context, drawn, source.PlayRules, out string? bonusError);
            if (bonusError != null) return Unsupported(bonusError);
            train = WithContext(train, context, false);
            spawn = WithTrain(spawn, train, turn, spawn.Rng);
            return Finish(RoomOutcome.Exchanged, source.EnergyPerTurn, moon);

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
                    TrainCombatResult preview = BattlePreviewModel.Refresh(spawn.Train);
                    if (!preview.Supported) return Unsupported(preview.UnsupportedReason!);
                    spawn = WithTrain(spawn, preview.State!, spawn.Turn, spawn.Rng);
                }
                if (source.CanonicalDecisionReferences)
                    spawn = WithTrain(spawn, TrainCombatModel.CompleteFrameRemovals(spawn.Train), spawn.Turn, spawn.Rng);
                CombatContext finalContext = spawn.Train.Context!;
                BattleRngStream[] streams = source.RngStreams.Select(stream => new BattleRngStream(stream.Name, stream.Seed,
                    stream.Name == "Battle" ? finalContext.BattleRng : stream.Name == "CardDraw" ? finalContext.Cards.Rng :
                    stream.Name == "Spawning" ? spawn.Rng : stream.State)).ToArray();
                CardPileState[] piles = Terminal(outcome) && outcome != RoomOutcome.Stalemate
                    ? otherPiles.Select(CardPileModel.Clear).ToArray()
                    : otherPiles;
                spawn = WithTrain(spawn, CardSpellModel.WithContext(spawn.Train, finalContext.WithOtherPiles(piles)),
                    spawn.Turn, spawn.Rng);
                int selectedRoom = source.SelectedRoom;
                if (selectedRoom >= 0)
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
                return new BattleTurnResult(new BattleTurnState(spawn, energy, source.EnergyPerTurn, source.DrawPerTurn,
                    source.ForgePoints, source.DragonsHoard, phase, streams, piles, source.ExternalInteractions, source.PlayRules,
                    source.BattlePreviewEnabled, source.UiRngIsolated, source.CanonicalDecisionReferences, source.CanonicalPhysicalReferences,
                    selectedRoom), outcome);
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
            context.CardRegistry, context.AllScenarioBossesDead, context.NextAddedTemporaryUpgrades, context.OtherPiles, context.QueryFrame, context.KillCamActivated, context.MagicPower, context.IsolatedBattlePreview, context.EnergyState, context.RoomCapacities, context.AbilityCardCache, context.LastAbilityActivatorUnitId, context.PermanentlyDisabledAbilities, context.LastSpawnedUnitId, context.NextUnitId, context.SpawnPoints, context.SummonCatalog, context.Enchantments, context.PurifyBlockedTriggers, context.Relics, context.TriggerCounts);
        private static TrainCombatState WithContext(TrainCombatState train, CombatContext context, bool deployment) =>
            new TrainCombatState(train.Rooms.Select(room => new RoomCombatState(room.RoomIndex, deployment, room.Units,
                room.ExternalInteractions, context)).ToArray(), train.Movement, train.EnemySlotsPerRoom, context);
        private static EnemySpawnState WithTrain(EnemySpawnState spawn, TrainCombatState train, int turn, UnityRng rng) =>
            new EnemySpawnState(train, spawn.Waves, spawn.SelectedGroups, spawn.Phase, spawn.Looping, rng, train.Context?.NextUnitId ?? spawn.NextUnitId,
                spawn.Treasures, spawn.TreasuresRemaining, spawn.TreasureEnabled, spawn.FirstTreasureTurn,
                spawn.FirstTreasureRoom, turn, spawn.ExternalInteractions, spawn.CanonicalDecisionReferences, spawn.PendingDestroyedUnitIds);
        private static bool Terminal(RoomOutcome outcome) => outcome == RoomOutcome.BattleWon ||
            outcome == RoomOutcome.PlayerDefeated || outcome == RoomOutcome.Stalemate;
        private static BattleTurnResult Unsupported(string reason) => new BattleTurnResult(null, RoomOutcome.Unsupported, reason);
    }
}
