using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class BattleBeliefWorld
    {
        public BattleTurnState State { get; }
        public double Weight { get; }
        public RoomOutcome Outcome { get; }
        public BattleBeliefWorld(BattleTurnState state, double weight, RoomOutcome outcome = RoomOutcome.Exchanged)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            if (double.IsNaN(weight) || double.IsInfinity(weight) || weight <= 0) throw new ArgumentOutOfRangeException(nameof(weight));
            Weight = weight; Outcome = outcome;
        }
    }
    public sealed class BattleBelief
    {
        public IReadOnlyList<BattleBeliefWorld> Worlds { get; }
        public bool ExhaustiveDistribution { get; }
        public string Uncertainty { get; }
        public BattleBelief(IReadOnlyList<BattleBeliefWorld> worlds, bool exhaustiveDistribution, string uncertainty)
        {
            if (worlds == null || worlds.Count == 0) throw new ArgumentException("A belief needs positive-weight hypotheses.", nameof(worlds));
            double total = worlds.Sum(world => world.Weight);
            if (double.IsInfinity(total)) throw new ArgumentOutOfRangeException(nameof(worlds));
            Worlds = Array.AsReadOnly(worlds.Select(world => new BattleBeliefWorld(world.State, world.Weight / total, world.Outcome)).ToArray());
            ExhaustiveDistribution = exhaustiveDistribution; Uncertainty = uncertainty;
        }
    }
    public static class DrawOrderBelief
    {
        // Uniform prior over current hidden draw order. This does not add an
        // uncertainty model for future reshuffles, combat RNG or enemy waves.
        // Samples start from a canonical pool, never the captured hidden order.
        public static BattleBelief Create(BattleTurnState source, int maxWorlds = 8, int seed = 424242)
        {
            if (maxWorlds < 1 || maxWorlds > 4096) throw new ArgumentOutOfRangeException(nameof(maxWorlds));
            var context = source.Spawn.Train.Context ?? throw new ArgumentException("Missing battle card context.", nameof(source));
            var pool = context.Cards.Draw.OrderBy(card => card.InstanceId).ToArray();
            if (pool.Select(card => card.InstanceId).Distinct().Count() != pool.Length) throw new ArgumentException("Duplicate draw-pile identities.");
            long permutations = 1;
            for (int index = 2; index <= pool.Length && permutations <= maxWorlds; index++) permutations *= index;
            bool exhaustive = permutations <= maxWorlds;
            var orders = new List<CardToken[]>();
            if (exhaustive) Permute(0);
            else
            {
                var seen = new HashSet<string>(StringComparer.Ordinal); var rng = UnityRng.Seed(seed);
                for (int attempt = 0; orders.Count < maxWorlds && attempt < maxWorlds * 64; attempt++)
                {
                    var shuffled = rng.Shuffle(pool); rng = shuffled.State;
                    string key = string.Join(",", shuffled.Items.Select(card => card.InstanceId));
                    if (seen.Add(key)) orders.Add(shuffled.Items.ToArray());
                }
            }
            return new BattleBelief(orders.Select(order => new BattleBeliefWorld(WithDrawOrder(source, order), 1)).ToArray(),
                exhaustive, "CurrentDrawOrderUniformPrior");
            void Permute(int offset)
            {
                if (offset == pool.Length) { orders.Add(pool.ToArray()); return; }
                for (int index = offset; index < pool.Length; index++)
                {
                    (pool[offset], pool[index]) = (pool[index], pool[offset]);
                    Permute(offset + 1);
                    (pool[offset], pool[index]) = (pool[index], pool[offset]);
                }
            }
        }
        internal static BattleTurnState WithDrawOrder(BattleTurnState source, IReadOnlyList<CardToken> order)
        {
            var spawn = source.Spawn; var context = spawn.Train.Context!; var cards = context.Cards;
            var changed = context.WithCards(new CardCycleState(cards.Hand, order, cards.Discard, cards.Rng, cards.DrawModifier,
                cards.ExternalInteractions, cards.BonusDraw, cards.NextDrawUpgrades, cards.NextUpgradeReferenceId, cards.DeckDrawDisabledThisTurn));
            var train = CardSpellModel.WithContext(spawn.Train, changed);
            var next = new EnemySpawnState(train, spawn.Waves, spawn.SelectedGroups, spawn.Phase, spawn.Looping, spawn.Rng,
                spawn.NextUnitId, spawn.Treasures, spawn.TreasuresRemaining, spawn.TreasureEnabled, spawn.FirstTreasureTurn,
                spawn.FirstTreasureRoom, spawn.Turn, spawn.ExternalInteractions, spawn.CanonicalDecisionReferences,
                spawn.PendingDestroyedUnitIds, spawn.OuterBoss, spawn.QueuedOuterBoss, spawn.QueuedTrueFinalBoss, spawn.TrueFinalBoss);
            return new BattleTurnState(next, source.Energy, source.EnergyPerTurn, source.DrawPerTurn, source.ForgePoints,
                source.DragonsHoard, source.MoonPhase, source.RngStreams, source.OtherPiles, source.ExternalInteractions,
                source.PlayRules, source.BattlePreviewEnabled, source.UiRngIsolated, source.CanonicalDecisionReferences,
                source.CanonicalPhysicalReferences, source.SelectedRoom, source.MoonPhaseTrackingActive);
        }
    }
    public sealed class BattleObservationKeys : IDisposable
    {
        private readonly BattleStateKeys keys = new BattleStateKeys();
        // Public card/room state and known pile contents, with hidden draw order
        // and all RNG words omitted. No action may depend on the hypothesis ID.
        public string Key(BattleTurnState state, RoomOutcome outcome = RoomOutcome.Exchanged)
        {
            var context = state.Spawn.Train.Context ?? throw new ArgumentException("Missing observable context.");
            return keys.SnapshotKey(new {
                Outcome = outcome, state.Spawn.Turn, state.Energy, state.EnergyPerTurn, state.DrawPerTurn,
                state.ForgePoints, state.DragonsHoard, state.MoonPhase, state.SelectedRoom, context.Gold,
                Rooms = state.Spawn.Train.Rooms.Select(room => new { room.RoomIndex, room.Deployment, room.Units }).ToArray(),
                Hand = context.Cards.Hand.Select(card => context.FindCard(card.InstanceId)).ToArray(),
                DrawPool = context.Cards.Draw.OrderBy(card => card.InstanceId).Select(card => context.FindCard(card.InstanceId)).ToArray(),
                Discard = context.Cards.Discard.Select(card => context.FindCard(card.InstanceId)).ToArray(),
                state.OtherPiles, context.MaxHandSize, context.Cards.DrawModifier, context.Cards.DeckDrawDisabledThisTurn
            });
        }
        public void Dispose() => keys.Dispose();
    }
}
