using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class ForcedRelentlessProgress
    {
        public int Turn { get; }
        public int WaveCount { get; }
        public int SpawnPhase { get; }
        public int RemainingTurns { get; }
        public ForcedRelentlessProgress(int turn, int waveCount, int spawnPhase, int remainingTurns)
        { Turn = turn; WaveCount = waveCount; SpawnPhase = spawnPhase; RemainingTurns = remainingTurns; }
    }

    // The native global transition only. Companion death and subsequent boss
    // phases still require their own complete combat integration.
    public static class ForcedRelentlessModel
    {
        public static ForcedRelentlessProgress Apply(ForcedRelentlessProgress source)
        {
            if (source.WaveCount <= 0 || source.Turn < 0 || source.SpawnPhase < 0 || source.RemainingTurns < 0)
                throw new ArgumentOutOfRangeException(nameof(source));
            int final = source.WaveCount - 1;
            return source.Turn >= final ? source : new ForcedRelentlessProgress(final, source.WaveCount,
                final, final - source.Turn);
        }

        internal static BattleTurnState ApplyBattle(BattleTurnState source, ForcedRelentlessProgress before)
        {
            if (source.Spawn.Turn != before.Turn || source.Spawn.Phase != before.SpawnPhase ||
                source.Spawn.Waves.Count != before.WaveCount)
                throw new ArgumentException("Forced Relentless requires matching native turn and wave inputs.");
            ForcedRelentlessProgress after = Apply(before);
            if (ReferenceEquals(after, before)) return source;
            EnemySpawnState old = source.Spawn;
            CombatContext context = old.Train.Context!;
            if (context.QueryFrame != null) context = context.WithQueryFrame(context.QueryFrame.With(turn: after.Turn));
            TrainCombatState train = CardSpellModel.WithContext(new TrainCombatState(old.Train.Rooms.Select(room =>
                new RoomCombatState(room.RoomIndex, after.Turn == 0, room.Units, room.ExternalInteractions,
                    room.Context, room.Preview, room.ActiveCompanionBossIds)).ToArray(), old.Train.Movement,
                old.Train.EnemySlotsPerRoom, old.Train.Context), context);
            train = EnchantmentWorldModel.Rebase(train);
            var spawn = new EnemySpawnState(train, old.Waves, old.SelectedGroups, after.SpawnPhase, old.Looping, old.Rng,
                old.NextUnitId, old.Treasures, old.TreasuresRemaining, old.TreasureEnabled, old.FirstTreasureTurn,
                old.FirstTreasureRoom, after.Turn, old.ExternalInteractions, old.CanonicalDecisionReferences,
                old.PendingDestroyedUnitIds, old.OuterBoss, old.QueuedOuterBoss, old.QueuedTrueFinalBoss, old.TrueFinalBoss);
            return new BattleTurnState(spawn, source.Energy, source.EnergyPerTurn, source.DrawPerTurn, source.ForgePoints,
                source.DragonsHoard, source.MoonPhase, source.RngStreams, source.OtherPiles, source.ExternalInteractions,
                source.PlayRules, source.BattlePreviewEnabled, source.UiRngIsolated, source.CanonicalDecisionReferences,
                source.CanonicalPhysicalReferences, source.SelectedRoom, source.MoonPhaseTrackingActive);
        }
    }
}
