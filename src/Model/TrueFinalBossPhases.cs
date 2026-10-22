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
        // Hellforged casting does prepare the native action cursor, but its
        // authored controller has no ordinary actions. It never enters the
        // Level8 queue-empty transition or executes a room-destroy action here.
        public static OuterBossPhaseResult Apply(OuterBossPhaseState source, bool preCombat)
        {
            OuterBossPhaseResult Fail(string reason) => new OuterBossPhaseResult(null, reason);
            BattleTurnState battle = source.Battle; OuterBossProgress boss = source.Boss;
            LifeLinkContext? world = battle.Spawn.Train.Context?.LifeLinks;
            if (source.TrueFinalBoss is not { HellforgedBoss: true, WaitedOnRelentless: false, QueuedBossCount: 0 } ||
                boss.AssetKey != "Boss_Level9_Entropy_Top_V2" || boss.Phase != "Casting" || boss.Room != 2 ||
                boss.Controller.Groups.Count != 0 || boss.Controller.InitialAction != null || boss.Controller.CurrentGroup != null ||
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
