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
        public CardPileState(string name, IReadOnlyList<CardToken> cards, IReadOnlyList<int>? entrySlots = null,
            IReadOnlyList<int>? freeSlots = null, IReadOnlyList<EquipmentStandbyCondition>? equipmentConditions = null)
        {
            Name = name; Cards = Array.AsReadOnly(cards.ToArray());
            EntrySlots = entrySlots == null ? null : Array.AsReadOnly(entrySlots.ToArray());
            FreeSlots = freeSlots == null ? null : Array.AsReadOnly(freeSlots.ToArray());
            EquipmentConditions = equipmentConditions == null ? null : Array.AsReadOnly(equipmentConditions
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
        public BattleTurnState(EnemySpawnState spawn, int energy, int energyPerTurn, int drawPerTurn,
            int forgePoints, int dragonsHoard, string moonPhase, IReadOnlyList<BattleRngStream> rngStreams,
            IReadOnlyList<CardPileState> otherPiles, IReadOnlyList<string> externalInteractions, BattlePlayRules? playRules = null,
            bool battlePreviewEnabled = false, bool uiRngIsolated = false, bool canonicalDecisionReferences = false)
        {
            Spawn = spawn; Energy = energy; EnergyPerTurn = energyPerTurn; DrawPerTurn = drawPerTurn;
            ForgePoints = forgePoints; DragonsHoard = dragonsHoard; MoonPhase = moonPhase;
            RngStreams = Array.AsReadOnly(rngStreams.ToArray()); OtherPiles = Array.AsReadOnly(otherPiles.ToArray());
            ExternalInteractions = Array.AsReadOnly(externalInteractions.ToArray());
            PlayRules = playRules;
            BattlePreviewEnabled = battlePreviewEnabled;
            UiRngIsolated = uiRngIsolated;
            CanonicalDecisionReferences = canonicalDecisionReferences;
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
        // One quiet player decision point through EndTurn to the next decision or terminal battle.
        public static BattleTurnResult EndTurn(BattleTurnState source)
        {
            if (source.ExternalInteractions.Count > 0) return Unsupported(string.Join("; ", source.ExternalInteractions));
            if (source.Spawn.Train.Context == null) return Unsupported("Missing shared battle context.");
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
            if (spawners.Any(cardId => !standbyCards.Contains(cardId)))
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
            train = WithContext(train, context.WithStatistics(context.Statistics?.NextTurn(context.Gold)), false);
            train = TrainCombatModel.ProcessRemovals(train);
            int turn = checked(spawn.Turn + 1);
            train = WithContext(train, train.Context!.WithQueryFrame(train.Context.QueryFrame?.With(
                turn: turn, moonPhase: moon == "New" ? 1 : 2)), false);
            // Initial enemies appear at the start of turn one, before that turn resets Spawning RNG.
            if (turn == 1)
            {
                spawn = WithTrain(spawn, train, turn, spawn.Rng);
                EnemySpawnResult initial = EnemySpawningModel.Spawn(spawn, false);
                if (!initial.Supported) return Unsupported(initial.UnsupportedReason!);
                spawn = initial.State!; train = spawn.Train;
                if (Terminal(initial.Outcome)) return Finish(initial.Outcome, 0, moon);
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
                    spawn = WithTrain(spawn, TrainCombatModel.ProcessRemovals(spawn.Train), spawn.Turn, spawn.Rng);
                CombatContext finalContext = spawn.Train.Context!;
                BattleRngStream[] streams = source.RngStreams.Select(stream => new BattleRngStream(stream.Name, stream.Seed,
                    stream.Name == "Battle" ? finalContext.BattleRng : stream.Name == "CardDraw" ? finalContext.Cards.Rng :
                    stream.Name == "Spawning" ? spawn.Rng : stream.State)).ToArray();
                CardPileState[] piles = Terminal(outcome) && outcome != RoomOutcome.Stalemate
                    ? otherPiles.Select(CardPileModel.Clear).ToArray()
                    : otherPiles;
                spawn = WithTrain(spawn, CardSpellModel.WithContext(spawn.Train, finalContext.WithOtherPiles(piles)),
                    spawn.Turn, spawn.Rng);
                return new BattleTurnResult(new BattleTurnState(spawn, energy, source.EnergyPerTurn, source.DrawPerTurn,
                    source.ForgePoints, source.DragonsHoard, phase, streams, piles, source.ExternalInteractions, source.PlayRules,
                    source.BattlePreviewEnabled, source.UiRngIsolated, source.CanonicalDecisionReferences), outcome);
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
            context.CardRegistry, context.AllScenarioBossesDead, context.NextAddedTemporaryUpgrades, context.OtherPiles, context.QueryFrame, context.KillCamActivated, context.MagicPower, context.IsolatedBattlePreview, context.EnergyState, context.RoomCapacities);
        private static TrainCombatState WithContext(TrainCombatState train, CombatContext context, bool deployment) =>
            new TrainCombatState(train.Rooms.Select(room => new RoomCombatState(room.RoomIndex, deployment, room.Units,
                room.ExternalInteractions, context)).ToArray(), train.Movement, train.EnemySlotsPerRoom, context);
        private static EnemySpawnState WithTrain(EnemySpawnState spawn, TrainCombatState train, int turn, UnityRng rng) =>
            new EnemySpawnState(train, spawn.Waves, spawn.SelectedGroups, spawn.Phase, spawn.Looping, rng, spawn.NextUnitId,
                spawn.Treasures, spawn.TreasuresRemaining, spawn.TreasureEnabled, spawn.FirstTreasureTurn,
                spawn.FirstTreasureRoom, turn, spawn.ExternalInteractions, spawn.CanonicalDecisionReferences, spawn.PendingDestroyedUnitIds);
        private static bool Terminal(RoomOutcome outcome) => outcome == RoomOutcome.BattleWon ||
            outcome == RoomOutcome.PlayerDefeated || outcome == RoomOutcome.Stalemate;
        private static BattleTurnResult Unsupported(string reason) => new BattleTurnResult(null, RoomOutcome.Unsupported, reason);
    }
}
