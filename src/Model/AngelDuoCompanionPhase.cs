using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class AngelDuoCompanionPhaseModel
    {
        internal static BattleTurnResult Apply(BattleTurnState source, bool preCombat)
        {
            EnemySpawnState old = source.Spawn;
            if (!preCombat || old.Train.Context?.QueryFrame?.ForcedRelentless == null || old.Phase < old.Waves.Count)
                return new BattleTurnResult(source, RoomOutcome.Exchanged);
            CombatUnit? companion = old.Train.Rooms.SelectMany(room => room.Units).FirstOrDefault(unit =>
                unit.AssetKey == "Boss_Level6_AngelDuo_Savage_Variant2_SlayAttack" && unit.Health > 0);
            if (companion == null || companion.Status("relentless") != null)
                return new BattleTurnResult(source, RoomOutcome.Exchanged);
            BattleTurnResult Fail(string reason) => new BattleTurnResult(null, RoomOutcome.Unsupported, reason);
            if (old.QueuedOuterBoss != null || old.Turn == 0)
                return Fail("Companion Relentless before original outer-boss creation requires separate calibration.");
            int origin = old.Train.Rooms.Single(room => room.Units.Any(unit => unit.Id == companion.Id)).RoomIndex;
            TrainCombatResult moved = TrainCombatModel.Ascend(old.Train, companion.Id, forceLoop: true);
            if (!moved.Supported) return Fail(moved.UnsupportedReason!);
            if (moved.Outcome is not (RoomOutcome.Exchanged or RoomOutcome.Cleared)) return Fail("Terminal companion Relentless ascension requires separate calibration.");
            TrainCombatState train = moved.State!;
            int destination = train.Rooms.Single(room => room.Units.Any(unit => unit.Id == companion.Id)).RoomIndex;
            if (destination < origin && train.Context!.QueryFrame?.SelectedRoomIndex != null)
                train = CardSpellModel.WithContext(train, train.Context.WithQueryFrame(
                    train.Context.QueryFrame.With(selectedRoomIndex: origin)));
            CombatStatus? relentless = train.Context!.StatusRules.FirstOrDefault(status => status.Id == "relentless");
            if (relentless == null) return Fail("Missing original companion Relentless status definition.");
            RoomCombatState scope = train.Rooms.Single(room => room.Units.Any(unit => unit.Id == companion.Id));
            RoomCombatResult added = StatusApplicationModel.Apply(scope, companion.Id, relentless.WithStacks(1));
            if (!added.Supported) return Fail(added.UnsupportedReason!);
            RoomCombatState changed = added.State!.WithUnits(added.State.Units.Select(unit => unit.Id == companion.Id
                ? CompanionBossModel.RemoveTriggers(unit) : unit).ToArray());
            train = CardSpellModel.WithContext(new TrainCombatState(train.Rooms.Select(room => room.RoomIndex == changed.RoomIndex
                ? changed : room).ToArray(), train.Movement, train.EnemySlotsPerRoom, changed.Context), changed.Context!);
            train = EnchantmentWorldModel.Rebase(train);
            TrainCombatResult settled = TrainCombatModel.ApplyCharacterQueue(train, added.PendingCallbacks.ToList());
            if (!settled.Supported) return Fail(settled.UnsupportedReason!);
            if (settled.Outcome != RoomOutcome.Exchanged) return Fail("Terminal companion status callbacks require separate calibration.");
            train = origin != destination && source.CanonicalDecisionReferences
                ? TrainCombatModel.CompleteFrameRemovals(settled.State!, old.PendingDestroyedUnitIds)
                : settled.State!;
            // The original Angel Duo retains its outer CharacterState reference
            // after death. Native therefore keeps DoAscension's selected room.
            var spawn = new EnemySpawnState(train, old.Waves, old.SelectedGroups, old.Phase, old.Looping, old.Rng,
                old.NextUnitId, old.Treasures, old.TreasuresRemaining, old.TreasureEnabled, old.FirstTreasureTurn,
                old.FirstTreasureRoom, old.Turn, old.ExternalInteractions, old.CanonicalDecisionReferences,
                old.PendingDestroyedUnitIds, old.OuterBoss, old.QueuedOuterBoss, old.QueuedTrueFinalBoss, old.TrueFinalBoss);
            return new BattleTurnResult(new BattleTurnState(spawn, source.Energy, source.EnergyPerTurn, source.DrawPerTurn,
                source.ForgePoints, source.DragonsHoard, source.MoonPhase, source.RngStreams.Select(stream =>
                    new BattleRngStream(stream.Name, stream.Seed, stream.Name == "Battle" ? train.Context!.BattleRng : stream.State)).ToArray(),
                source.OtherPiles, source.ExternalInteractions, source.PlayRules, source.BattlePreviewEnabled,
                source.UiRngIsolated, source.CanonicalDecisionReferences, source.CanonicalPhysicalReferences,
                train.Context!.QueryFrame?.SelectedRoomIndex ?? source.SelectedRoom, source.MoonPhaseTrackingActive), RoomOutcome.Exchanged);
        }
    }
}
