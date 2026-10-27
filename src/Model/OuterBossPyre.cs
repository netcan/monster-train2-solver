using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class OuterBossPyreResult
    {
        public BattleTurnState? State { get; }
        public RoomOutcome Outcome { get; }
        public string? UnsupportedReason { get; }
        public bool Supported => State != null;
        internal OuterBossPyreResult(BattleTurnState? state, RoomOutcome outcome, string? reason = null)
        { State = state; Outcome = outcome; UnsupportedReason = reason; }
    }
    public static class OuterBossPyreModel
    {
        public static OuterBossPyreResult Apply(OuterBossPhaseState source)
        {
            OuterBossPyreResult Fail(string reason) => new OuterBossPyreResult(null, RoomOutcome.Unsupported, reason);
            BattleTurnState battle = source.Battle; OuterBossProgress boss = source.Boss;
            TrainCombatState train = battle.Spawn.Train;
            int pyre = train.Rooms.Count - 1;
            CombatUnit? actor = train.Rooms.SelectMany(room => room.Units).FirstOrDefault(unit => unit.Id == boss.UnitId);
            bool originalDuoPair = OuterBossDefinitions.IsAngelDuo(boss.AssetKey) && actor != null &&
                train.Context?.QueryFrame?.ForcedRelentless != null && train.Rooms.SelectMany(room => room.Units)
                    .Any(unit => unit.Id == actor.BumpRules?.PairedBossUnitId && unit.Health > 0 &&
                        unit.AssetKey == OuterBossDefinitions.AngelDuoCompanion(boss.AssetKey));
            if (!OuterBossDefinitions.Supports(boss.AssetKey) || boss.Phase != "Relentless" || boss.Room != pyre - 1 ||
                actor?.BumpRules?.OuterBoss != true || actor.BumpRules.PairedBossUnitId != 0 && !originalDuoPair || actor.Health <= 0 ||
                actor.Status("rooted") != null || actor.Status("immobile") != null || train.Context?.SpawnPoints == null ||
                battle.PlayRules == null || battle.PlayRules.Rooms.Any(room => !room.IsPyre && room.Enabled))
                return Fail("Outer-boss Pyre entry requires the calibrated unpaired relentless boss above three destroyed floors.");
            if (train.Context.QueryFrame?.SelectedRoomIndex != null)
                train = CardSpellModel.WithContext(train, train.Context.WithQueryFrame(train.Context.QueryFrame.With(selectedRoomIndex: pyre)));
            // HeroManager.DoBossTowerAttack moves the boss directly, without
            // HandleRoomUnitOrderPossiblyChanged. Its departure-room attachment
            // upgrades remain registered through terminal tower combat.
            BumpResult moved = BumpModel.Apply(train, new[] { boss.UnitId }, 1, battle.PlayRules.Rooms,
                0, null, outerBossActionMovement: true, synchronizeRoomAttachments: false);
            if (!moved.Supported) return Fail(moved.UnsupportedReason!);
            if (moved.Outcome != RoomOutcome.Exchanged) return Fail("Terminal outer-boss Pyre-entry movement requires separate calibration.");
            train = moved.State!;
            if (!train.Rooms[pyre].Units.Any(unit => unit.Id == boss.UnitId)) return Fail("Outer boss did not reach its Pyre anchor.");
            // Animated native movement advances the frame before tower combat;
            // destructors from the destroyed floors finish before new combat deaths.
            if (battle.CanonicalDecisionReferences)
                train = TrainCombatModel.CompleteFrameRemovals(train, battle.Spawn.PendingDestroyedUnitIds);
            TrainCombatResult combat = TrainCombatModel.ResolvePyreCombat(train);
            if (!combat.Supported) return Fail(combat.UnsupportedReason!);
            train = combat.State!;
            if (combat.Outcome is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated)
            {
                CombatContext terminal = train.Context!;
                terminal = terminal.WithQueryFrame(terminal.QueryFrame?.With(runningCombat: false))
                    .WithOtherPiles((terminal.OtherPiles ?? battle.OtherPiles).Select(CardPileModel.Clear).ToArray());
                train = CardSpellModel.WithContext(train, terminal);
            }
            if (battle.CanonicalDecisionReferences) train = TrainCombatModel.CompleteFrameRemovals(train);
            EnemySpawnState old = battle.Spawn;
            var spawn = new EnemySpawnState(train, old.Waves, old.SelectedGroups, old.Phase, old.Looping, old.Rng,
                train.Context?.NextUnitId ?? old.NextUnitId, old.Treasures, old.TreasuresRemaining, old.TreasureEnabled,
                old.FirstTreasureTurn, old.FirstTreasureRoom, old.Turn, old.ExternalInteractions,
                old.CanonicalDecisionReferences, old.PendingDestroyedUnitIds, old.OuterBoss,
                queuedTrueFinalBoss: old.QueuedTrueFinalBoss, trueFinalBoss: old.TrueFinalBoss);
            var streams = battle.RngStreams.Select(stream => new BattleRngStream(stream.Name, stream.Seed,
                stream.Name == "Battle" ? train.Context!.BattleRng : stream.Name == "CardDraw" ? train.Context!.Cards.Rng : stream.State)).ToArray();
            return new OuterBossPyreResult(new BattleTurnState(spawn, battle.Energy, battle.EnergyPerTurn, battle.DrawPerTurn,
                train.Context!.QueryFrame?.ForgePoints ?? battle.ForgePoints, battle.DragonsHoard, battle.MoonPhase, streams,
                train.Context.OtherPiles ?? battle.OtherPiles, battle.ExternalInteractions, battle.PlayRules,
                battle.BattlePreviewEnabled, battle.UiRngIsolated, battle.CanonicalDecisionReferences,
                battle.CanonicalPhysicalReferences, pyre, battle.MoonPhaseTrackingActive), combat.Outcome);
        }
    }
}
