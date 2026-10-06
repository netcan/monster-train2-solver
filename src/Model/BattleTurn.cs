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
        public CardPileState(string name, IReadOnlyList<CardToken> cards)
        { Name = name; Cards = Array.AsReadOnly(cards.ToArray()); }
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
        public BattleTurnState(EnemySpawnState spawn, int energy, int energyPerTurn, int drawPerTurn,
            int forgePoints, int dragonsHoard, string moonPhase, IReadOnlyList<BattleRngStream> rngStreams,
            IReadOnlyList<CardPileState> otherPiles, IReadOnlyList<string> externalInteractions, BattlePlayRules? playRules = null,
            bool battlePreviewEnabled = false)
        {
            Spawn = spawn; Energy = energy; EnergyPerTurn = energyPerTurn; DrawPerTurn = drawPerTurn;
            ForgePoints = forgePoints; DragonsHoard = dragonsHoard; MoonPhase = moonPhase;
            RngStreams = Array.AsReadOnly(rngStreams.ToArray()); OtherPiles = Array.AsReadOnly(otherPiles.ToArray());
            ExternalInteractions = Array.AsReadOnly(externalInteractions.ToArray());
            PlayRules = playRules;
            BattlePreviewEnabled = battlePreviewEnabled;
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
            if (source.MoonPhase != "Full" && source.MoonPhase != "New") return Unsupported("Unmodeled moon phase.");
            CardToken[] allCards = source.Spawn.Train.Context.Cards.Hand.Concat(source.Spawn.Train.Context.Cards.Draw)
                .Concat(source.Spawn.Train.Context.Cards.Discard).Concat(source.OtherPiles.SelectMany(pile => pile.Cards)).ToArray();
            if (allCards.Select(card => card.InstanceId).Distinct().Count() != allCards.Length ||
                allCards.Any(card => card.InstanceId >= source.Spawn.Train.Context.NextCardId))
                return Unsupported("Invalid card identity allocation or duplicate pile membership.");
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
            CardPileState[] otherPiles = source.OtherPiles.ToArray();
            TrainCombatState train = spawn.Train;
            CombatContext context = train.Context!;
            CardCycleResult discard = CardCycleModel.DiscardHand(context.Cards);
            if (!discard.Supported) return Unsupported(discard.UnsupportedReason!);
            BattleStatistics? statistics = context.Statistics;
            foreach (CardToken card in context.Cards.Hand.Reverse()) statistics = statistics?.Increment(card.InstanceId, "TimesDiscarded");
            context = context.WithStatistics(statistics?.WithEndTurnEnergy(source.Energy));
            var discardedIds = new HashSet<int>(context.Cards.Hand.Select(card => card.InstanceId));
            context = context.WithCardInstances(context.CardInstances?.Select(card => discardedIds.Contains(card.InstanceId)
                ? card.OnDiscard(false) : card).ToArray());
            CardCycleState cards = discard.State!;
            if (spawn.Turn == 0)
            {
                ShuffleResult<CardToken> shuffled = cards.Rng.Shuffle(cards.Draw.Concat(cards.Discard).ToArray());
                cards = new CardCycleState(cards.Hand, shuffled.Items, Array.Empty<CardToken>(), shuffled.State,
                    cards.DrawModifier, cards.ExternalInteractions);
            }
            context = WithCards(context, cards);
            train = WithContext(train, context, spawn.Turn == 0);
            TrainCombatResult combat = TrainCombatModel.ResolveCombat(train);
            if (!combat.Supported) return Unsupported(combat.UnsupportedReason!);
            if (!RouteDeadUnits(train, combat)) return Unsupported("Missing unit death event or card routing.");
            train = combat.State!;
            spawn = WithTrain(spawn, train, spawn.Turn, spawn.Rng);
            if (Terminal(combat.Outcome)) return Finish(combat.Outcome, 0, source.MoonPhase);
            TrainCombatResult ascended = TrainCombatModel.Ascend(train);
            if (!ascended.Supported) return Unsupported(ascended.UnsupportedReason!);
            if (!RouteDeadUnits(train, ascended)) return Unsupported("Missing unit death event or card routing.");
            train = ascended.State!;
            spawn = WithTrain(spawn, train, spawn.Turn, spawn.Rng);
            if (Terminal(ascended.Outcome)) return Finish(ascended.Outcome, 0, source.MoonPhase);
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
            int turn = checked(spawn.Turn + 1);
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
            context = train.Context!;
            CardCycleResult drawn = CardCycleModel.DrawHand(context.Cards, source.DrawPerTurn, context.MaxHandSize);
            if (!drawn.Supported) return Unsupported(drawn.UnsupportedReason!);
            var existingHand = new HashSet<int>(context.Cards.Hand.Select(card => card.InstanceId));
            statistics = context.Statistics;
            foreach (CardToken card in drawn.State!.Hand.Reverse().Where(card => !existingHand.Contains(card.InstanceId)))
                statistics = statistics?.Increment(card.InstanceId, "TimesDrawn");
            context = context.WithStatistics(statistics);
            context = WithCards(context, drawn.State!);
            train = WithContext(train, context, false);
            spawn = WithTrain(spawn, train, turn, spawn.Rng);
            return Finish(RoomOutcome.Exchanged, source.EnergyPerTurn, moon);

            BattleTurnResult Finish(RoomOutcome outcome, int energy, string phase)
            {
                if (source.BattlePreviewEnabled && !Terminal(outcome))
                {
                    TrainCombatResult preview = BattlePreviewModel.Refresh(spawn.Train);
                    if (!preview.Supported) return Unsupported(preview.UnsupportedReason!);
                    spawn = WithTrain(spawn, preview.State!, spawn.Turn, spawn.Rng);
                }
                CombatContext finalContext = spawn.Train.Context!;
                BattleRngStream[] streams = source.RngStreams.Select(stream => new BattleRngStream(stream.Name, stream.Seed,
                    stream.Name == "Battle" ? finalContext.BattleRng : stream.Name == "CardDraw" ? finalContext.Cards.Rng :
                    stream.Name == "Spawning" ? spawn.Rng : stream.State)).ToArray();
                CardPileState[] piles = Terminal(outcome) && outcome != RoomOutcome.Stalemate
                    ? otherPiles.Select(pile => new CardPileState(pile.Name, Array.Empty<CardToken>())).ToArray()
                    : otherPiles;
                return new BattleTurnResult(new BattleTurnState(spawn, energy, source.EnergyPerTurn, source.DrawPerTurn,
                    source.ForgePoints, source.DragonsHoard, phase, streams, piles, source.ExternalInteractions, source.PlayRules,
                    source.BattlePreviewEnabled), outcome);
            }

            bool RouteDeadUnits(TrainCombatState before, TrainCombatResult result)
            {
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
                var standby = otherPiles.FirstOrDefault(pile => pile.Name == "Standby")?.Cards.ToList();
                var exhausted = otherPiles.FirstOrDefault(pile => pile.Name == "Exhausted")?.Cards.ToList();
                if (standby == null || exhausted == null) return false;
                foreach (int cardId in deadCards)
                {
                    CardToken? card = standby.FirstOrDefault(item => item.InstanceId == cardId);
                    if (card == null) return false;
                    standby.Remove(card); exhausted.Add(card);
                }
                otherPiles = otherPiles.Select(pile => pile.Name == "Standby" ? new CardPileState(pile.Name, standby) :
                    pile.Name == "Exhausted" ? new CardPileState(pile.Name, exhausted) : pile).ToArray();
                return true;
            }
        }
        private static CombatContext WithCards(CombatContext context, CardCycleState cards) => new CombatContext(cards,
            context.BattleRng, context.Gold, context.NextCardId, context.MaxHandSize, context.StatusRules, context.Statistics, context.CardInstances,
            context.CardRegistry, context.AllScenarioBossesDead);
        private static TrainCombatState WithContext(TrainCombatState train, CombatContext context, bool deployment) =>
            new TrainCombatState(train.Rooms.Select(room => new RoomCombatState(room.RoomIndex, deployment, room.Units,
                room.ExternalInteractions, context)).ToArray(), train.Movement, train.EnemySlotsPerRoom, context);
        private static EnemySpawnState WithTrain(EnemySpawnState spawn, TrainCombatState train, int turn, UnityRng rng) =>
            new EnemySpawnState(train, spawn.Waves, spawn.SelectedGroups, spawn.Phase, spawn.Looping, rng, spawn.NextUnitId,
                spawn.Treasures, spawn.TreasuresRemaining, spawn.TreasureEnabled, spawn.FirstTreasureTurn,
                spawn.FirstTreasureRoom, turn, spawn.ExternalInteractions);
        private static bool Terminal(RoomOutcome outcome) => outcome == RoomOutcome.BattleWon ||
            outcome == RoomOutcome.PlayerDefeated || outcome == RoomOutcome.Stalemate;
        private static BattleTurnResult Unsupported(string reason) => new BattleTurnResult(null, RoomOutcome.Unsupported, reason);
    }
}
