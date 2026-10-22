using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class TrueFinalBossPhaseFlags
    {
        public bool HellforgedBoss { get; }
        public bool WaitedOnRelentless { get; }
        public int QueuedBossCount { get; }
        public TrueFinalBossPhaseFlags(bool hellforgedBoss, bool waitedOnRelentless, int queuedBossCount)
        { HellforgedBoss = hellforgedBoss; WaitedOnRelentless = waitedOnRelentless; QueuedBossCount = queuedBossCount; }
    }
    public static class TrueFinalBossPhaseModel
    {
        // Isolated native control, kept separate from the natural phase rules.
        public static OuterBossPhaseResult EnterRelentlessWithExhaustedQueue(OuterBossPhaseState source)
        {
            OuterBossPhaseResult valid = Apply(source, false);
            if (!valid.Supported || source.Boss.Phase != "Casting" || source.Battle.Spawn.Turn != 1 ||
                source.Battle.Spawn.Train.Rooms.SelectMany(room => room.Units).Count() != 6)
                return new OuterBossPhaseResult(null, valid.UnsupportedReason ?? "Relentless control requires the original five-body first-turn boundary.");
            TrainCombatState train = source.Battle.Spawn.Train;
            RoomCombatState room = train.Rooms[2];
            CombatStatus? relentless = train.Context!.StatusRules.SingleOrDefault(status => status.Id == "relentless");
            if (relentless == null) return new OuterBossPhaseResult(null, "Missing native relentless status definition.");
            RoomCombatResult added = StatusApplicationModel.Apply(room, source.Boss.UnitId, relentless.WithStacks(1));
            if (!added.Supported) return new OuterBossPhaseResult(null, added.UnsupportedReason);
            CombatUnit top = CompanionBossModel.RemoveTriggers(added.State!.Units.Single(unit => unit.Id == source.Boss.UnitId))
                .WithOuterBossPhase(source.Boss.BaseCanAttack && (source.Boss.ValidAttackPhases & 2) != 0, true);
            train = CardSpellModel.WithContext(new TrainCombatState(train.Rooms.Select(floor => new RoomCombatState(
                floor.RoomIndex, floor.Deployment, floor.Units.Select(unit => unit.Id == top.Id ? top : unit).ToArray(),
                floor.ExternalInteractions, added.State.Context, floor.Preview, floor.ActiveCompanionBossIds)).ToArray(),
                train.Movement, train.EnemySlotsPerRoom, added.State.Context), added.State.Context!);
            train = EnchantmentWorldModel.Rebase(train);
            TrainCombatResult settled = TrainCombatModel.ApplyCharacterQueue(train, added.PendingCallbacks.ToList());
            if (!settled.Supported || settled.Outcome != RoomOutcome.Exchanged)
                return new OuterBossPhaseResult(null, settled.UnsupportedReason ?? "Terminal phase-control callbacks remain guarded.");
            train = settled.State!;
            OuterBossProgress boss = source.Boss;
            var progress = new OuterBossProgress(boss.UnitId, boss.AssetKey, "Relentless", boss.ValidAttackPhases,
                boss.BaseCanAttack, boss.Room, boss.MovementSpeed, boss.VisitedRooms, boss.Controller, boss.Actions, boss.DestroyActions);
            return new OuterBossPhaseResult(TrueFinalBossRelentlessPhaseModel.Rebuild(source, train, progress,
                source.TrueFinalBoss!, source.Battle.PlayRules!, source.Battle.SelectedRoom, spawnPhase: source.Battle.Spawn.Waves.Count, looping: false));
        }
        // Hellforged casting does prepare the native action cursor, but its
        // authored controller has no ordinary actions. It never enters the
        // Level8 queue-empty transition or executes a room-destroy action here.
        public static OuterBossPhaseResult Apply(OuterBossPhaseState source, bool preCombat)
        {
            OuterBossPhaseResult Fail(string reason) => new OuterBossPhaseResult(null, reason);
            BattleTurnState battle = source.Battle; OuterBossProgress boss = source.Boss;
            LifeLinkContext? world = battle.Spawn.Train.Context?.LifeLinks;
            if (source.TrueFinalBoss is not { HellforgedBoss: true, QueuedBossCount: 0 } ||
                boss.AssetKey != "Boss_Level9_Entropy_Top_V2" || boss.Phase is not ("Casting" or "Relentless") || boss.Room != 2 ||
                boss.Phase == "Casting" && source.TrueFinalBoss.WaitedOnRelentless ||
                boss.Controller.Groups.Count != 0 || boss.Controller.InitialAction != null ||
                boss.Phase == "Casting" && boss.Controller.CurrentGroup != null ||
                boss.Actions.Count != 0 || battle.ExternalInteractions.Count > 0 || battle.Spawn.ExternalInteractions.Count > 0 ||
                battle.Spawn.Train.Rooms.Count != 4 || battle.Spawn.Train.Rooms.Where((room, index) => room.RoomIndex != index || room.Preview).Any() ||
                world == null || LifeLinkModel.Validate(world) != null || world.CalculatingPreview || world.SharedPreviewHpChange != 0 ||
                world.Actors.Count != 5 || world.Actors.Any(actor => !actor.Initialized || !actor.TrueFinalBoss || !actor.HasLifeLink || actor.Health <= 0) ||
                battle.Spawn.Train.Movement.Any(movement => movement.CompanionBoss))
                return Fail("True-final-boss phases require the calibrated born Hellforged casting roster and empty ordinary action controller.");
            CombatUnit[] units = battle.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .Where(unit => unit.Team == CombatTeam.Enemy && world.Actors.Any(actor => actor.Id == unit.Id)).ToArray();
            string[] assets = { "Boss_Level9_Dominion_V2", "Boss_Level9_Savagery_V2", "Boss_Level9_Entropy_Bot_V2",
                "Boss_Level9_Entropy_Mid_V2", "Boss_Level9_Entropy_Top_V2" };
            int[] floors = { 3, 0, 0, 1, 2 };
            if (units.Length != 5 || assets.Any(asset => units.Count(unit => unit.AssetKey == asset) != 1))
                return Fail("True-final-boss casting requires all five original physical bodies.");
            int bottom = units.Single(unit => unit.AssetKey == assets[2]).Id;
            for (int index = 0; index < 5; index++)
            {
                CombatUnit unit = units.Single(actor => actor.AssetKey == assets[index]);
                LifeLinkActor? linked = world.Actors.SingleOrDefault(actor => actor.Id == unit.Id);
                if (linked == null || linked.Health != unit.Health || linked.PreviewHealth != unit.Health || linked.OuterBoss != (index >= 2) ||
                    linked.LinkedId != (index >= 3 ? bottom : 0) ||
                    index != 1 && !battle.Spawn.Train.Rooms[floors[index]].Units.Any(actor => actor.Id == unit.Id) ||
                    index == 1 && battle.Spawn.Train.Rooms[3].Units.Any(actor => actor.Id == unit.Id))
                    return Fail("True-final-boss phase has inconsistent physical rooms, roles or linked health.");
            }
            if (units.Single(unit => unit.AssetKey == assets[4]).Id != boss.UnitId)
                return Fail("True-final-boss global controller must belong to the top Entropy body.");
            if (boss.Phase == "Relentless")
                return TrueFinalBossRelentlessPhaseModel.Apply(source, preCombat);
            if (preCombat)
            {
                BossActionControllerResult next = BossActionControllerModel.Apply(boss.Controller, "DecideNext");
                if (!next.Supported || next.State!.NextAction != null || next.State.CurrentGroup != null)
                    return Fail(next.UnsupportedReason ?? "Unexpected Hellforged casting action.");
            }
            return new OuterBossPhaseResult(source);
        }
    }
}
