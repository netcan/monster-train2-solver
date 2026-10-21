using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    // Creation has finished, but the native boss has no room or initial action.
    // This boundary deliberately does not model CharacterState.Setup itself.
    public sealed class OuterBossCreationState
    {
        public BattleTurnState Battle { get; }
        public CombatUnit CreatedUnit { get; }
        public OuterBossProgress CreatedBoss { get; }
        public EnemyMovement CreatedMovement { get; }
        public IReadOnlyList<string> ExternalInteractions { get; }
        public OuterBossCreationState(BattleTurnState battle, CombatUnit createdUnit, OuterBossProgress createdBoss,
            IReadOnlyList<string> externalInteractions, EnemyMovement createdMovement)
        {
            Battle = battle; CreatedUnit = createdUnit; CreatedBoss = createdBoss;
            CreatedMovement = createdMovement;
            ExternalInteractions = Array.AsReadOnly(externalInteractions.ToArray());
        }
    }
    public static class OuterBossBirthModel
    {
        public static OuterBossPhaseResult Apply(OuterBossCreationState source)
        {
            OuterBossPhaseResult Fail(string reason) => new OuterBossPhaseResult(null, reason);
            BattleTurnState battle = source.Battle; OuterBossProgress boss = source.CreatedBoss; CombatUnit unit = source.CreatedUnit;
            TrainCombatState train = battle.Spawn.Train;
            if (source.ExternalInteractions.Count > 0) return Fail(string.Join("; ", source.ExternalInteractions));
            if (!OuterBossDefinitions.Supports(boss.AssetKey) || boss.Phase != "Casting" || boss.Room != -1 || boss.UnitId != unit.Id ||
                unit.BumpRules?.OuterBoss != true || unit.Health <= 0 || unit.DeathState?.IsDestroyed == true ||
                unit.BumpRules.PairedBossUnitId != 0 || source.CreatedMovement?.UnitId != unit.Id || source.CreatedMovement.Speed != 1 ||
                source.CreatedMovement.CompanionBoss || boss.Controller.Groups.Count != 1 || boss.DestroyActions.Count != 1 ||
                boss.VisitedRooms.Count != 0 || boss.Controller.InitialAction != null ||
                boss.Controller.CurrentGroup != null || boss.Actions.Count != 1 || boss.Actions[0].Count != 1 || !boss.Actions[0][0].Empty ||
                battle.Spawn.OuterBoss != null || battle.PlayRules == null || train.Context?.SpawnPoints == null ||
                train.Rooms.SelectMany(room => room.Units).Any(actor => actor.Id == unit.Id))
                return Fail("Outer-boss birth requires the calibrated created actor before initial setup and room placement.");
            BossActionControllerResult initial = BossActionControllerModel.Apply(boss.Controller, "DecideInitial");
            if (!initial.Supported || initial.State!.InitialAction?.Group != 0 || initial.State.InitialAction.Action != 0)
                return Fail(initial.UnsupportedReason ?? "Unexpected initial outer-boss action.");
            OuterBossActionProgress action = boss.Actions[0][0];
            RoomPlayRule[] rooms = battle.PlayRules.Rooms.Where(room => !room.IsPyre).ToArray();
            bool[] Occupied(CombatTeam team) => rooms.Select(rule => train.Rooms[rule.RoomIndex].Units
                .Any(actor => actor.Team == team && actor.BumpRules?.OuterBoss != true)).ToArray();
            BattleRngStream? stream = battle.RngStreams.FirstOrDefault(rng => rng.Name == "BossActions");
            if (stream == null) return Fail("Missing initial BossActions random stream.");
            BossActionTargetResult setup = BossActionTargetModel.Setup(new BossActionTargetState(action.Behavior,
                rooms.Select(room => room.Enabled).ToArray(), Occupied(CombatTeam.Player), Occupied(action.TargetTeam),
                new int[rooms.Length], true, -1, boss.MovementSpeed, action.TargetRoom, action.PostActionRoom,
                action.SetupAsInitialAction, stream.State));
            if (!setup.Supported) return Fail(setup.UnsupportedReason!);
            int target = setup.TargetRoom;
            BattleSpawnPoints points = train.Context.SpawnPoints;
            UnitSpawnPointState? owner = points.Units.SingleOrDefault(actor => actor.UnitId == unit.Id);
            SpawnPointGroupState? group = points.Group(target, CombatTeam.Enemy);
            IReadOnlyList<int>? anchors = unit.BumpRules.CastingAnchorSlots;
            if (owner?.OuterBoss != true || owner.Current != null || owner.LastKnown != null || group == null ||
                anchors == null || target >= anchors.Count || anchors[target] < 0 || anchors[target] >= group.Occupants.Count ||
                !group.Outside[anchors[target]] || group.Occupants[anchors[target]] != 0)
                return Fail("Missing unplaced native boss identity or free initial casting anchor.");
            SpawnPointGroupState[] groups = points.Groups.Select(prior =>
            {
                if (prior != group) return prior;
                int[] occupants = prior.Occupants.ToArray(); occupants[anchors[target]] = unit.Id;
                return new SpawnPointGroupState(prior.RoomIndex, prior.Team, prior.InnerCount, prior.GroupCount,
                    occupants, prior.Outside, prior.PreviewCopyId);
            }).ToArray();
            points = points.WithState(groups, points.Units.Select(prior => prior.UnitId != unit.Id ? prior :
                new UnitSpawnPointState(unit.Id, new SpawnPointReference(target, CombatTeam.Enemy, 0, outerBossAnchor: true),
                    null, true, prior.SpawnedInPreview)).ToArray());
            CombatContext context = train.Context.WithSpawnPoints(points);
            var movement = train.Movement.Append(source.CreatedMovement).ToArray();
            train = EnchantmentWorldModel.Rebase(new TrainCombatState(train.Rooms.Select(room => new RoomCombatState(room.RoomIndex,
                room.Deployment, BattleSpawnPointModel.Order(points, room.RoomIndex, room.RoomIndex == target ? room.Units.Append(unit).ToArray() : room.Units),
                room.ExternalInteractions, context, room.Preview, room.ActiveCompanionBossIds)).ToArray(), movement, train.EnemySlotsPerRoom, context));
            foreach (string phase in new[] { "OnSpawn", "OnUnscaledSpawn", "OnSpawnNotFromCard" })
            {
                CombatUnit actor = train.Rooms[target].Units.Single(value => value.Id == unit.Id);
                TrainCombatResult fired = TrainCombatModel.ApplyCharacterQueue(train,
                    new List<RoomCombatModel.QueuedCharacterTrigger> { new RoomCombatModel.QueuedCharacterTrigger(target, actor, phase) });
                if (!fired.Supported) return Fail(fired.UnsupportedReason!);
                if (fired.Outcome != RoomOutcome.Exchanged) return Fail("Terminal initial outer-boss callbacks require separate calibration.");
                train = fired.State!;
            }
            var configured = new OuterBossActionProgress(action.Behavior, action.TargetTeam, target, setup.PostActionRoom, true, false, true);
            var progress = new OuterBossProgress(unit.Id, boss.AssetKey, boss.Phase, boss.ValidAttackPhases, boss.BaseCanAttack,
                target, boss.MovementSpeed, setup.VisitedRooms, initial.State!, new[] { new[] { configured } }, boss.DestroyActions);
            EnemySpawnState old = battle.Spawn;
            var spawn = new EnemySpawnState(train, old.Waves, old.SelectedGroups, old.Phase, old.Looping, old.Rng,
                old.NextUnitId, old.Treasures, old.TreasuresRemaining, old.TreasureEnabled, old.FirstTreasureTurn,
                old.FirstTreasureRoom, old.Turn, old.ExternalInteractions, old.CanonicalDecisionReferences, old.PendingDestroyedUnitIds, progress);
            var streams = battle.RngStreams.Select(rng => new BattleRngStream(rng.Name, rng.Seed,
                rng.Name == "BossActions" ? setup.Rng : rng.Name == "Battle" ? train.Context!.BattleRng : rng.State)).ToArray();
            return new OuterBossPhaseResult(new OuterBossPhaseState(new BattleTurnState(spawn, battle.Energy, battle.EnergyPerTurn,
                battle.DrawPerTurn, battle.ForgePoints, battle.DragonsHoard, battle.MoonPhase, streams,
                train.Context!.OtherPiles ?? battle.OtherPiles, battle.ExternalInteractions, battle.PlayRules,
                battle.BattlePreviewEnabled, battle.UiRngIsolated, battle.CanonicalDecisionReferences, battle.CanonicalPhysicalReferences,
                battle.SelectedRoom, battle.MoonPhaseTrackingActive), progress));
        }
    }
}
