using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class OuterBossActionProgress
    {
        public string Behavior { get; }
        public CombatTeam TargetTeam { get; }
        public int TargetRoom { get; }
        public int PostActionRoom { get; }
        public bool SetupAsInitialAction { get; }
        public bool DestroyRoom { get; }
        public bool Empty { get; }
        public OuterBossActionProgress(string behavior, CombatTeam targetTeam, int targetRoom, int postActionRoom,
            bool setupAsInitialAction, bool destroyRoom, bool empty)
        {
            Behavior = behavior; TargetTeam = targetTeam; TargetRoom = targetRoom; PostActionRoom = postActionRoom;
            SetupAsInitialAction = setupAsInitialAction; DestroyRoom = destroyRoom; Empty = empty;
        }
    }
    public sealed class OuterBossProgress
    {
        public int UnitId { get; }
        public string AssetKey { get; }
        public string Phase { get; }
        public int ValidAttackPhases { get; }
        public bool BaseCanAttack { get; }
        public int Room { get; }
        public int MovementSpeed { get; }
        public IReadOnlyList<int> VisitedRooms { get; }
        public BossActionControllerProgress Controller { get; }
        public IReadOnlyList<IReadOnlyList<OuterBossActionProgress>> Actions { get; }
        public IReadOnlyList<OuterBossActionProgress> DestroyActions { get; }
        public OuterBossProgress(int unitId, string assetKey, string phase, int validAttackPhases, bool baseCanAttack,
            int room, int movementSpeed, IReadOnlyList<int> visitedRooms, BossActionControllerProgress controller,
            IReadOnlyList<IReadOnlyList<OuterBossActionProgress>> actions, IReadOnlyList<OuterBossActionProgress> destroyActions)
        {
            UnitId = unitId; AssetKey = assetKey; Phase = phase; ValidAttackPhases = validAttackPhases;
            BaseCanAttack = baseCanAttack; Room = room; MovementSpeed = movementSpeed;
            VisitedRooms = Array.AsReadOnly(visitedRooms.ToArray()); Controller = controller;
            Actions = Array.AsReadOnly(actions.Select(group => (IReadOnlyList<OuterBossActionProgress>)Array.AsReadOnly(group.ToArray())).ToArray());
            DestroyActions = Array.AsReadOnly(destroyActions.ToArray());
        }
        internal OuterBossProgress WithRoom(int room) => room == Room ? this : new OuterBossProgress(UnitId, AssetKey,
            Phase, ValidAttackPhases, BaseCanAttack, room, MovementSpeed, VisitedRooms, Controller, Actions, DestroyActions);
        internal OuterBossProgress WithIdentity(int id) => new OuterBossProgress(id, AssetKey,
            Phase, ValidAttackPhases, BaseCanAttack, Room, MovementSpeed, VisitedRooms, Controller, Actions, DestroyActions);
    }
    public sealed class OuterBossPhaseState
    {
        public BattleTurnState Battle { get; }
        public OuterBossProgress Boss { get; }
        public TrueFinalBossPhaseFlags? TrueFinalBoss { get; }
        public bool ShouldSerializeTrueFinalBoss() => TrueFinalBoss != null;
        public OuterBossPhaseState(BattleTurnState battle, OuterBossProgress boss, TrueFinalBossPhaseFlags? trueFinalBoss = null)
        { Battle = battle; Boss = boss; TrueFinalBoss = trueFinalBoss; }
    }
    public sealed class OuterBossPhaseResult
    {
        public OuterBossPhaseState? State { get; }
        public string? UnsupportedReason { get; }
        public bool Supported => State != null;
        public RoomOutcome Outcome { get; }
        internal OuterBossPhaseResult(OuterBossPhaseState? state, string? error = null, RoomOutcome outcome = RoomOutcome.Exchanged)
        { State = state; UnsupportedReason = error; Outcome = outcome; }
    }
    public static class OuterBossPhaseModel
    {
        // Natural casting captures and explicit native queue controls calibrate
        // these phase boundaries independently of whole-EndTurn integration.
        public static OuterBossPhaseResult Apply(OuterBossPhaseState source, bool preCombat)
        {
            if (source.Battle.Spawn.Train.Context?.QueryFrame?.ForcedRelentless != null)
            {
                BattleTurnResult companion = AngelDuoCompanionPhaseModel.Apply(source.Battle, preCombat);
                if (!companion.Supported) return new OuterBossPhaseResult(null, companion.UnsupportedReason);
                source = new OuterBossPhaseState(companion.State!, source.Boss, source.TrueFinalBoss);
            }
            if (source.Boss.AssetKey == "Boss_Level9_Entropy_Top_V2") return TrueFinalBossPhaseModel.Apply(source, preCombat);
            if (source.Boss.Phase == "Relentless" || preCombat && source.Battle.Spawn.Phase >= source.Battle.Spawn.Waves.Count)
                return OuterBossRelentlessPhaseModel.Apply(source, preCombat);
            return Casting(source, preCombat);
        }
        private static OuterBossPhaseResult Casting(OuterBossPhaseState source, bool preCombat)
        {
            OuterBossPhaseResult Fail(string reason) => new OuterBossPhaseResult(null, reason);
            BattleTurnState battle = source.Battle; OuterBossProgress boss = source.Boss;
            TrainCombatState train = battle.Spawn.Train;
            if (!OuterBossDefinitions.Supports(boss.AssetKey) || boss.Phase != "Casting" ||
                boss.Actions.Count != boss.Controller.Groups.Count || boss.Actions.Count != 1 ||
                boss.Actions[0].Count != 1 || !boss.Actions[0][0].Empty || boss.DestroyActions.Count != 1 ||
                battle.PlayRules == null || train.Context?.SpawnPoints == null)
                return Fail("Outer boss phase requires the calibrated empty-action casting definition and physical room rules.");
            if (!preCombat) return new OuterBossPhaseResult(source);
            string? error = TrainCombatModel.Validate(train);
            if (error != null) return Fail(error);
            CombatUnit? actor = train.Rooms.SelectMany(room => room.Units).FirstOrDefault(unit => unit.Id == boss.UnitId);
            if (actor?.BumpRules?.OuterBoss != true || actor.Health <= 0 || actor.DeathState?.IsDestroyed == true ||
                boss.Controller.Groups[0].Executable.Count != 1 || !boss.Controller.Groups[0].Executable[0])
                return Fail("Outer boss casting actor or action executability is missing.");
            BossActionControllerResult decided = BossActionControllerModel.Apply(boss.Controller, "DecideNext");
            if (!decided.Supported) return Fail(decided.UnsupportedReason!);
            BossActionReference? next = decided.State!.NextAction;
            if (next == null || next.Group != 0 || next.Action != 0)
                return Fail("Casting controller did not select the calibrated empty action.");
            OuterBossActionProgress action = boss.Actions[0][0];
            RoomPlayRule[] rules = battle.PlayRules.Rooms.ToArray();
            BattleRngStream? bossRng = battle.RngStreams.FirstOrDefault(stream => stream.Name == "BossActions");
            if (bossRng == null) return Fail("Missing native BossActions random stream.");
            bool[] Occupied(CombatTeam team) => rules.Where(rule => !rule.IsPyre).Select(rule =>
                train.Rooms.First(room => room.RoomIndex == rule.RoomIndex).Units.Any(unit => unit.Team == team &&
                    unit.BumpRules?.OuterBoss != true && unit.Health > 0 && unit.DeathState?.IsDestroyed != true)).ToArray();
            BossActionTargetResult target = BossActionTargetModel.Setup(new BossActionTargetState(action.Behavior,
                rules.Where(rule => !rule.IsPyre).Select(rule => rule.Enabled).ToArray(), Occupied(CombatTeam.Player),
                Occupied(action.TargetTeam), boss.VisitedRooms, false, boss.Room, boss.MovementSpeed,
                action.TargetRoom, action.PostActionRoom, action.SetupAsInitialAction, bossRng.State));
            if (!target.Supported) return Fail(target.UnsupportedReason!);
            int room = boss.Room;
            if (train.Context.QueryFrame?.SelectedRoomIndex != null)
                train = CardSpellModel.WithContext(train, train.Context.WithQueryFrame(train.Context.QueryFrame.With(selectedRoomIndex: target.TargetRoom)));
            bool Move(int destination)
            {
                if (destination == room) return true;
                if (actor.Status("immobile") != null || actor.Status("rooted") != null)
                { error = "Native action movement of an immobilized outer boss is not calibrated."; return false; }
                BumpResult moved = BumpModel.Apply(train, new[] { boss.UnitId }, destination - room, rules,
                    0, null, outerBossActionMovement: true);
                if (!moved.Supported) { error = moved.UnsupportedReason; return false; }
                if (moved.Outcome != RoomOutcome.Exchanged)
                { error = "Terminal outer-boss action movement requires native phase calibration."; return false; }
                train = moved.State!; rules = moved.RoomRules.ToArray(); room = destination;
                // HandleBossMovement calls HandleRoomUnitOrderPossiblyChanged even
                // when no movement callback fires. That refreshes the moving aura.
                var pending = new List<RoomCombatModel.QueuedCharacterTrigger>();
                TrainCombatResult updated = EnchantmentWorldModel.UpdateAll(train, pending.Add);
                if (!updated.Supported) { error = updated.UnsupportedReason; return false; }
                TrainCombatResult settled = TrainCombatModel.ApplyCharacterQueue(updated.State!, pending);
                if (!settled.Supported) { error = settled.UnsupportedReason; return false; }
                train = settled.State!;
                return true;
            }
            if (!Move(target.TargetRoom) || !Move(target.PostActionRoom)) return Fail(error!);
            train = room != boss.Room && battle.CanonicalDecisionReferences
                ? TrainCombatModel.CompleteFrameRemovals(train, battle.Spawn.PendingDestroyedUnitIds) : EnchantmentWorldModel.Rebase(train);
            BossActionControllerResult performed = BossActionControllerModel.Apply(decided.State, "Perform");
            if (!performed.Supported) return Fail(performed.UnsupportedReason!);
            var reset = new OuterBossActionProgress(action.Behavior, action.TargetTeam, 0, 0, false, false, action.Empty);
            var progress = new OuterBossProgress(boss.UnitId, boss.AssetKey, boss.Phase, boss.ValidAttackPhases, boss.BaseCanAttack,
                room, boss.MovementSpeed, target.VisitedRooms, performed.State!, new[] { new[] { reset } }, boss.DestroyActions);
            EnemySpawnState old = battle.Spawn;
            var spawn = new EnemySpawnState(train, old.Waves, old.SelectedGroups, old.Phase, old.Looping, old.Rng,
                old.NextUnitId, old.Treasures, old.TreasuresRemaining, old.TreasureEnabled, old.FirstTreasureTurn,
                old.FirstTreasureRoom, old.Turn, old.ExternalInteractions, old.CanonicalDecisionReferences, old.PendingDestroyedUnitIds,
                old.OuterBoss == null ? null : progress, queuedTrueFinalBoss: old.QueuedTrueFinalBoss, trueFinalBoss: old.TrueFinalBoss);
            var streams = battle.RngStreams.Select(stream => new BattleRngStream(stream.Name, stream.Seed,
                stream.Name == "BossActions" ? target.Rng : stream.Name == "Battle" ? train.Context!.BattleRng : stream.State)).ToArray();
            return new OuterBossPhaseResult(new OuterBossPhaseState(new BattleTurnState(spawn, battle.Energy, battle.EnergyPerTurn,
                battle.DrawPerTurn, battle.ForgePoints, battle.DragonsHoard, battle.MoonPhase, streams, battle.OtherPiles,
                battle.ExternalInteractions, battle.PlayRules.WithRooms(rules), battle.BattlePreviewEnabled, battle.UiRngIsolated,
                battle.CanonicalDecisionReferences, battle.CanonicalPhysicalReferences, target.TargetRoom, battle.MoonPhaseTrackingActive), progress));
        }
    }
}
