using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class OuterBossRelentlessPhaseModel
    {
        internal static OuterBossPhaseResult Apply(OuterBossPhaseState source, bool preCombat)
        {
            OuterBossPhaseResult Fail(string reason) => new OuterBossPhaseResult(null, reason);
            BattleTurnState battle = source.Battle; OuterBossProgress boss = source.Boss;
            TrainCombatState train = battle.Spawn.Train;
            if (boss.AssetKey != "Boss_Level8_AllyBuffer" || boss.Phase is not ("Casting" or "Relentless") ||
                boss.Actions.Count != 1 || boss.Actions[0].Count != 1 || !boss.Actions[0][0].Empty ||
                boss.DestroyActions.Count != 1 || !boss.DestroyActions[0].DestroyRoom || battle.PlayRules == null ||
                train.Context?.SpawnPoints == null || boss.Controller.InitialAction != null)
                return Fail("Outer boss relentless phase requires the calibrated empty-action definition without an initial latch.");
            string? error = TrainCombatModel.Validate(train);
            if (error != null) return Fail(error);
            CombatUnit? actor = train.Rooms.SelectMany(room => room.Units).FirstOrDefault(unit => unit.Id == boss.UnitId);
            if (actor?.BumpRules?.OuterBoss != true || actor.Health <= 0 || actor.DeathState?.IsDestroyed == true)
                return Fail("Missing live outer boss at the relentless phase boundary.");
            if (actor.BumpRules.RelentlessAnchorSlots == null)
                return Fail("Missing native relentless anchor layout.");
            if (actor.BumpRules.PairedBossUnitId != 0 || train.Movement.Any(rule => rule.CompanionBoss))
                return Fail("Paired companion and outer-boss phases require native integration calibration.");
            RoomPlayRule[] rules = battle.PlayRules.Rooms.ToArray();
            int room = boss.Room, selectedRoom = battle.SelectedRoom;
            string phase = boss.Phase;
            BossActionControllerProgress controller = boss.Controller;
            OuterBossActionProgress action = boss.DestroyActions[0];
            IReadOnlyList<int> visits = boss.VisitedRooms;
            BattleRngStream? random = battle.RngStreams.FirstOrDefault(stream => stream.Name == "BossActions");
            if (random == null) return Fail("Missing BossActions random stream for the relentless phase.");
            UnityRng rng = random.State;
            var pending = new List<RoomCombatModel.QueuedCharacterTrigger>();
            bool changed = false;
            void SetActor(CombatUnit value, CombatContext context)
            {
                train = CardSpellModel.WithContext(new TrainCombatState(train.Rooms.Select(floor =>
                    new RoomCombatState(floor.RoomIndex, floor.Deployment, floor.Units.Select(unit => unit.Id == value.Id ? value : unit).ToArray(),
                        floor.ExternalInteractions, context, floor.Preview, floor.ActiveCompanionBossIds)).ToArray(),
                    train.Movement, train.EnemySlotsPerRoom, context), context);
            }
            void Select(int floor)
            {
                selectedRoom = floor;
                if (train.Context!.QueryFrame?.SelectedRoomIndex != null)
                    train = CardSpellModel.WithContext(train, train.Context.WithQueryFrame(train.Context.QueryFrame.With(selectedRoomIndex: floor)));
            }
            if (preCombat)
            {
                if (phase == "Casting")
                {
                    if (battle.Spawn.Phase < battle.Spawn.Waves.Count) return Fail("Native spawn queue is not empty.");
                    CombatStatus? relentless = train.Context!.StatusRules.FirstOrDefault(status => status.Id == "relentless");
                    if (relentless == null) return Fail("Missing captured relentless status definition.");
                    RoomCombatState scope = train.Rooms.Single(floor => floor.Units.Any(unit => unit.Id == boss.UnitId));
                    RoomCombatResult added = StatusApplicationModel.Apply(scope, boss.UnitId, relentless.WithStacks(1));
                    if (!added.Supported) return Fail(added.UnsupportedReason!);
                    actor = CompanionBossModel.RemoveTriggers(added.State!.Units.Single(unit => unit.Id == boss.UnitId))
                        .WithOuterBossPhase(boss.BaseCanAttack && (boss.ValidAttackPhases & 2) != 0, true);
                    SetActor(actor, added.State.Context!); pending.AddRange(added.PendingCallbacks);
                    phase = "Relentless"; changed = true;
                }
                BossActionControllerResult decided = BossActionControllerModel.Apply(controller, "DecideNext", true);
                if (!decided.Supported) return Fail(decided.UnsupportedReason!);
                controller = decided.State!;
                if (controller.NextAction?.Group != controller.Groups.Count || controller.NextAction.Action != 0)
                    return Fail("Relentless controller did not select its room-destruction action.");
                bool[] enabled = rules.Where(rule => !rule.IsPyre).Select(rule => rule.Enabled).ToArray();
                bool[] occupied = new bool[enabled.Length];
                BossActionTargetResult target = BossActionTargetModel.Setup(new BossActionTargetState(action.Behavior,
                    enabled, occupied, occupied, visits, false, room, boss.MovementSpeed, action.TargetRoom,
                    action.PostActionRoom, action.SetupAsInitialAction, rng));
                if (!target.Supported) return Fail(target.UnsupportedReason!);
                visits = target.VisitedRooms; rng = target.Rng;
                action = new OuterBossActionProgress(action.Behavior, action.TargetTeam, target.TargetRoom,
                    target.PostActionRoom, target.SetupAsInitialAction, action.DestroyRoom, action.Empty);
                Select(target.TargetRoom);
                if (target.TargetRoom != room)
                {
                    BumpResult moved = BumpModel.Apply(train, new[] { boss.UnitId }, target.TargetRoom - room,
                        rules, 0, pending);
                    if (!moved.Supported) return Fail(moved.UnsupportedReason!);
                    if (moved.Outcome != RoomOutcome.Exchanged) return Fail("Terminal relentless movement requires native phase calibration.");
                    train = moved.State!; rules = moved.RoomRules.ToArray(); pending = moved.PendingCallbacks.ToList();
                    room = target.TargetRoom; changed = true;
                    TrainCombatResult updated = EnchantmentWorldModel.UpdateAll(train, pending.Add);
                    if (!updated.Supported) return Fail(updated.UnsupportedReason!);
                    train = updated.State!;
                }
                else if (changed)
                {
                    RoomCombatState scope = train.Rooms.Single(floor => floor.RoomIndex == room);
                    BattleSpawnPointResult anchored = BattleSpawnPointModel.Apply(train.Context!.SpawnPoints!, scope, "Set",
                        CombatTeam.Enemy, boss.UnitId, target: new SpawnPointReference(room, CombatTeam.Enemy, 0, outerBossAnchor: true));
                    if (!anchored.Supported) return Fail(anchored.Error!);
                    train = CardSpellModel.WithContext(train, train.Context.WithSpawnPoints(anchored.State!));
                }
            }
            else
            {
                if (controller.NextAction?.Group != controller.Groups.Count || controller.NextAction.Action != 0)
                    return Fail("Relentless post-combat phase has no prepared destruction action.");
                Select(room);
                int end = Math.Max(room, Math.Min(train.Rooms.Count - 1, room + boss.MovementSpeed));
                for (int floor = room; floor < end; floor++)
                {
                    rules = rules.Select(rule => rule.RoomIndex == floor ? rule.WithEnabled(false) : rule).ToArray();
                    TrainCombatResult destroyed = TrainCombatModel.DestroyAscendedRoom(train, floor);
                    if (!destroyed.Supported) return Fail(destroyed.UnsupportedReason!);
                    if (destroyed.Outcome != RoomOutcome.Exchanged) return Fail("Terminal room-destruction callbacks require native calibration.");
                    train = destroyed.State!; changed = true;
                }
                BossActionControllerResult performed = BossActionControllerModel.Apply(controller, "Perform");
                if (!performed.Supported) return Fail(performed.UnsupportedReason!);
                controller = performed.State!;
            }
            TrainCombatResult settled = TrainCombatModel.ApplyCharacterQueue(train, pending);
            if (!settled.Supported) return Fail(settled.UnsupportedReason!);
            if (settled.Outcome != RoomOutcome.Exchanged) return Fail("Terminal outer-boss phase callbacks require native calibration.");
            // The Instant protocol advances a frame during boss movement. Room
            // destruction can finish in the same frame; its new Unity destructors
            // and previously queued ones still wait for a later frame boundary.
            train = changed && battle.CanonicalDecisionReferences ? TrainCombatModel.CompleteFrameRemovals(settled.State!,
                preCombat ? battle.Spawn.PendingDestroyedUnitIds : Array.Empty<int>()) :
                EnchantmentWorldModel.Rebase(settled.State!);
            BossActionGroupProgress destroyGroup = controller.DestroyGroup;
            controller = new BossActionControllerProgress(controller.Groups, new BossActionGroupProgress(destroyGroup.Repeats,
                destroyGroup.ActionIndex, destroyGroup.CurrentAction, new[] { rules.Any(rule => !rule.IsPyre && rule.Enabled) }, true),
                controller.GroupIndex, controller.CurrentGroup, controller.InitialAction);
            var progress = new OuterBossProgress(boss.UnitId, boss.AssetKey, phase, boss.ValidAttackPhases, boss.BaseCanAttack,
                room, boss.MovementSpeed, visits, controller, boss.Actions, new[] { action });
            EnemySpawnState old = battle.Spawn;
            var spawn = new EnemySpawnState(train, old.Waves, old.SelectedGroups, old.Phase, old.Looping, old.Rng,
                old.NextUnitId, old.Treasures, old.TreasuresRemaining, old.TreasureEnabled, old.FirstTreasureTurn,
                old.FirstTreasureRoom, old.Turn, old.ExternalInteractions, old.CanonicalDecisionReferences, old.PendingDestroyedUnitIds);
            var streams = battle.RngStreams.Select(stream => new BattleRngStream(stream.Name, stream.Seed,
                stream.Name == "BossActions" ? rng : stream.Name == "Battle" ? train.Context!.BattleRng : stream.State)).ToArray();
            return new OuterBossPhaseResult(new OuterBossPhaseState(new BattleTurnState(spawn, battle.Energy, battle.EnergyPerTurn,
                battle.DrawPerTurn, battle.ForgePoints, battle.DragonsHoard, battle.MoonPhase, streams, battle.OtherPiles,
                battle.ExternalInteractions, battle.PlayRules.WithRooms(rules), battle.BattlePreviewEnabled, battle.UiRngIsolated,
                battle.CanonicalDecisionReferences, battle.CanonicalPhysicalReferences, selectedRoom, battle.MoonPhaseTrackingActive), progress));
        }
    }
}
