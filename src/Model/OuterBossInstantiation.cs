using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class OuterBossSpawnDefinition
    {
        public EnemyDefinition Actor { get; }
        public OuterBossProgress Boss { get; }
        public OuterBossSpawnDefinition(EnemyDefinition actor, OuterBossProgress boss) { Actor = actor; Boss = boss; }
    }
    public sealed class OuterBossInstantiationState
    {
        public BattleTurnState Battle { get; }
        public OuterBossSpawnDefinition Definition { get; }
        public OuterBossInstantiationState(BattleTurnState battle, OuterBossSpawnDefinition definition)
        { Battle = battle; Definition = definition; }
    }
    public sealed class OuterBossInstantiationResult
    {
        public OuterBossCreationState? State { get; }
        public string? UnsupportedReason { get; }
        public bool Supported => State != null;
        internal OuterBossInstantiationResult(OuterBossCreationState? state, string? reason = null)
        { State = state; UnsupportedReason = reason; }
    }
    public static class OuterBossInstantiationModel
    {
        public static OuterBossInstantiationResult Apply(OuterBossInstantiationState source)
        {
            OuterBossInstantiationResult Fail(string reason) => new OuterBossInstantiationResult(null, reason);
            BattleTurnState battle = source.Battle; EnemySpawnState old = battle.Spawn;
            TrainCombatState train = old.Train; OuterBossSpawnDefinition definition = source.Definition;
            EnemyDefinition actor = definition.Actor; OuterBossProgress boss = definition.Boss;
            bool duo = OuterBossDefinitions.IsAngelDuo(actor.Unit.AssetKey);
            int moon = train.Context?.QueryFrame?.MoonPhase ?? 0;
            bool calibratedMoon = battle.MoonPhaseTrackingActive == true &&
                (actor.Unit.AssetKey is "Boss_Level3_Fleshweaver_Corruption" or "Boss_Level6_AngelDuo_Entropic_Variant1_ResolveHealth") && moon == 2 &&
                !actor.Unit.Triggers.Any(trigger => trigger.Kind is "OnMoonLit" or "OnMoonlitLost" or
                    "OnMoonShade" or "OnMoonshadeLost" or "OnMoonPhaseShift");
            if (actor.ExternalInteractions.Count > 0) return Fail(string.Join("; ", actor.ExternalInteractions));
            if (!TrueFinalBossCovenantModel.CalibratedOuterBossOwners(train.Context, actor.Unit.AssetKey))
                return Fail("Outer-boss Covenant owners require separate calibration.");
            string? statError = EnemySpawningModel.ValidateBirthStats(actor, train.Context);
            if (statError != null) return Fail(statError);
            if (train.Context?.Relics?.Any(owner => owner.IsCovenant == true) == true && actor.BirthStats == null)
                return Fail("Outer-boss Covenant creation requires its calibrated native birth stats.");
            if (!OuterBossDefinitions.Supports(actor.Unit.AssetKey) || actor.Unit.Id != 0 || boss.UnitId != 0 ||
                boss.AssetKey != actor.Unit.AssetKey || boss.Room != -1 || boss.Phase != "Casting" ||
                old.OuterBoss != null || train.Context?.SpawnPoints == null || actor.Unit.BumpRules?.OuterBoss != true ||
                (duo ? actor.Unit.Statuses.Count != 1 || actor.Unit.Status("titanskin")?.Stacks != 10 :
                    OuterBossDefinitions.IsArkionPyre(actor.Unit.AssetKey)
                        ? actor.Unit.Statuses.Count != 1 || actor.Unit.Status("multistrike")?.Stacks != 1
                        : OuterBossDefinitions.IsFleshweaver(actor.Unit.AssetKey)
                            ? actor.Unit.Statuses.Count != 1 || actor.Unit.Status("mass")?.Stacks !=
                                (actor.Unit.AssetKey == "Boss_Level3_Fleshweaver_MoreMass" ? 8 : 5) || actor.Unit.Status("mass")?.ParamInt != 2
                        : actor.Unit.Statuses.Count != 0) ||
                actor.Unit.Ability != null || actor.Unit.Modifiers?.Upgrades.Count != 0 ||
                actor.CompanionBoss != duo || actor.Unit.BumpRules.CompanionBoss != duo ||
                battle.MoonPhaseTrackingActive == true && !calibratedMoon)
                return Fail("Outer-boss instantiation requires the calibrated original definition without creation modifiers or moon tracking.");
            string? invalid = UnitIdentityModel.Validate(train.Context, train.Rooms.SelectMany(room => room.Units), old.NextUnitId);
            if (invalid != null) return Fail(invalid);
            UnitIdentityAllocation allocation = UnitIdentityModel.Allocate(train.Context, old.NextUnitId);
            if (!allocation.Supported) return Fail(allocation.UnsupportedReason!);
            CombatContext context = allocation.Context!;
            BattleSpawnPoints points = context.SpawnPoints!;
            context = context.WithSpawnPoints(points.WithState(points.Groups, points.Units.Append(
                new UnitSpawnPointState(allocation.UnitId, null, null, outerBoss: true)).ToArray()));
            train = CardSpellModel.WithContext(train, context);
            var spawn = new EnemySpawnState(train, old.Waves, old.SelectedGroups, old.Phase, old.Looping, old.Rng,
                allocation.NextUnitId, old.Treasures, old.TreasuresRemaining, old.TreasureEnabled, old.FirstTreasureTurn,
                old.FirstTreasureRoom, old.Turn, old.ExternalInteractions, old.CanonicalDecisionReferences, old.PendingDestroyedUnitIds,
                queuedOuterBoss: old.QueuedOuterBoss, queuedTrueFinalBoss: old.QueuedTrueFinalBoss, trueFinalBoss: old.TrueFinalBoss);
            var next = new BattleTurnState(spawn, battle.Energy, battle.EnergyPerTurn, battle.DrawPerTurn,
                battle.ForgePoints, battle.DragonsHoard, battle.MoonPhase, battle.RngStreams, battle.OtherPiles,
                battle.ExternalInteractions, battle.PlayRules, battle.BattlePreviewEnabled, battle.UiRngIsolated,
                battle.CanonicalDecisionReferences, battle.CanonicalPhysicalReferences, battle.SelectedRoom, battle.MoonPhaseTrackingActive);
            CombatUnit created = actor.Create(allocation.UnitId, applyBirthStats: false);
            if (calibratedMoon)
                created = MoonPhaseStatusModel.WithPhase(created, moon).WithMoonPhaseMask(moon);
            if (duo)
            {
                CombatUnit[] partners = train.Rooms.SelectMany(room => room.Units)
                    .Where(unit => unit.BumpRules?.CompanionBoss == true && unit.Health > 0).ToArray();
                if (partners.Length != 1 || partners[0].AssetKey != OuterBossDefinitions.AngelDuoCompanion(actor.Unit.AssetKey))
                    return Fail("Angel duo creation requires its original live Savage companion.");
                UnitBumpRules prior = created.BumpRules!;
                created = created.WithBumpRules(new UnitBumpRules(prior.Loops, prior.CompanionBoss, prior.OuterBoss,
                    prior.OuterBossRelentlessPhase, partners[0].Id, prior.CastingAnchorSlots, prior.RelentlessAnchorSlots))
                    .WithEndsBattleOnDeath(false);
            }
            return new OuterBossInstantiationResult(new OuterBossCreationState(next, created,
                boss.WithIdentity(allocation.UnitId), actor.ExternalInteractions,
                new EnemyMovement(allocation.UnitId, 1, actor.Ascends, actor.Loops, actor.CompanionBoss), actor.BirthStats));
        }
    }
}
