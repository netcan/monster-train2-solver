using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class TrueFinalBossSpawnDefinition
    {
        public IReadOnlyList<EnemyDefinition> Definitions { get; }
        public IReadOnlyList<int> QueuedBosses { get; }
        public OuterBossProgress Controller { get; }
        public IReadOnlyList<string> ExternalInteractions { get; }
        public TrueFinalBossSpawnDefinition(IReadOnlyList<EnemyDefinition> definitions, IReadOnlyList<int> queuedBosses,
            OuterBossProgress controller, IReadOnlyList<string> externalInteractions)
        {
            Definitions = Array.AsReadOnly(definitions.ToArray()); QueuedBosses = Array.AsReadOnly(queuedBosses.ToArray());
            Controller = controller; ExternalInteractions = Array.AsReadOnly(externalInteractions.ToArray());
        }
    }
    // The entire native DoTrueFinalBossSpawning boundary, including creation,
    // room placement, OnSpawn and only then Entropy's shared-health links.
    public sealed class TrueFinalBossSpawnState
    {
        public BattleTurnState Battle { get; }
        public IReadOnlyList<EnemyDefinition> Definitions { get; }
        public IReadOnlyList<int> QueuedBosses { get; }
        public int OuterBossId { get; }
        public IReadOnlyList<int> RoomBossIds { get; }
        public IReadOnlyList<string> ExternalInteractions { get; }
        public TrueFinalBossSpawnState(BattleTurnState battle, IReadOnlyList<EnemyDefinition> definitions,
            IReadOnlyList<int> queuedBosses, int outerBossId, IReadOnlyList<int> roomBossIds,
            IReadOnlyList<string> externalInteractions)
        {
            Battle = battle; Definitions = Array.AsReadOnly(definitions.ToArray());
            QueuedBosses = Array.AsReadOnly(queuedBosses.ToArray()); OuterBossId = outerBossId;
            RoomBossIds = Array.AsReadOnly(roomBossIds.ToArray());
            ExternalInteractions = Array.AsReadOnly(externalInteractions.ToArray());
        }
    }
    public sealed class TrueFinalBossSpawnResult
    {
        public TrueFinalBossSpawnState? State { get; }
        public string? UnsupportedReason { get; }
        public bool Supported => State != null;
        internal TrueFinalBossSpawnResult(TrueFinalBossSpawnState? state, string? reason = null)
        { State = state; UnsupportedReason = reason; }
    }
    public static class TrueFinalBossSpawningModel
    {
        // Integrate the calibrated creation boundary with the global controller
        // and persistent HeroManager flags carried through subsequent turns.
        public static BattleTurnResult ApplyQueued(BattleTurnState source)
        {
            BattleTurnResult Fail(string reason) => new BattleTurnResult(null, RoomOutcome.Unsupported, reason);
            TrueFinalBossSpawnDefinition? queued = source.Spawn.QueuedTrueFinalBoss;
            if (queued == null || source.Spawn.Turn != 1 || source.Spawn.Phase != 1 ||
                source.Spawn.TrueFinalBoss != null || source.Spawn.OuterBoss != null ||
                queued.Controller.AssetKey != "Boss_Level9_Entropy_Top_V2" || queued.Controller.UnitId != 0 ||
                queued.Controller.Room != -1 || queued.Controller.Phase != "Casting" || queued.Controller.Controller.Groups.Count != 0 ||
                queued.Controller.Actions.Count != 0)
                return Fail("Missing original queued true-final-boss definition or empty global controller.");
            TrueFinalBossSpawnResult born = Apply(new TrueFinalBossSpawnState(source, queued.Definitions, queued.QueuedBosses,
                0, new[] { 0, 0, 0, 0 }, queued.ExternalInteractions));
            if (!born.Supported) return Fail(born.UnsupportedReason!);
            BattleTurnState battle = born.State!.Battle; EnemySpawnState old = battle.Spawn;
            TrainCombatState train = old.Train;
            train = EnchantmentWorldModel.Rebase(new TrainCombatState(train.Rooms.Select(room => new RoomCombatState(
                room.RoomIndex, room.Deployment, room.Units.Select(unit => unit.BumpRules?.OuterBoss == true
                    ? unit.WithBumpRules(queued.Definitions.Single(definition => definition.Unit.AssetKey == unit.AssetKey).Unit.BumpRules!) : unit).ToArray(),
                room.ExternalInteractions, train.Context, room.Preview, room.ActiveCompanionBossIds)).ToArray(),
                train.Movement, train.EnemySlotsPerRoom, train.Context));
            BossActionControllerResult initial = BossActionControllerModel.Apply(queued.Controller.Controller, "DecideInitial");
            if (!initial.Supported) return Fail(initial.UnsupportedReason!);
            OuterBossProgress authored = queued.Controller;
            var controller = new OuterBossProgress(born.State.OuterBossId, authored.AssetKey, authored.Phase,
                authored.ValidAttackPhases, authored.BaseCanAttack, 2, 0, new int[3], initial.State!, authored.Actions, authored.DestroyActions);
            var spawn = new EnemySpawnState(train, old.Waves, old.SelectedGroups, old.Phase, old.Looping, old.Rng,
                old.NextUnitId, old.Treasures, old.TreasuresRemaining, old.TreasureEnabled, old.FirstTreasureTurn,
                old.FirstTreasureRoom, old.Turn, old.ExternalInteractions, old.CanonicalDecisionReferences, old.PendingDestroyedUnitIds,
                controller, trueFinalBoss: new TrueFinalBossPhaseFlags(true, false, 0));
            return new BattleTurnResult(new BattleTurnState(spawn, battle.Energy, battle.EnergyPerTurn, battle.DrawPerTurn,
                battle.ForgePoints, battle.DragonsHoard, battle.MoonPhase, battle.RngStreams, battle.OtherPiles, battle.ExternalInteractions,
                battle.PlayRules, battle.BattlePreviewEnabled, battle.UiRngIsolated, battle.CanonicalDecisionReferences,
                battle.CanonicalPhysicalReferences, battle.SelectedRoom, battle.MoonPhaseTrackingActive), RoomOutcome.Exchanged);
        }
        private static readonly string[] Assets = { "Boss_Level9_Dominion_V2", "Boss_Level9_Savagery_V2",
            "Boss_Level9_Entropy_Bot_V2", "Boss_Level9_Entropy_Mid_V2", "Boss_Level9_Entropy_Top_V2" };
        private static readonly int[] Health = { 1555, 2750, 3500, 3500, 3500 };
        private static readonly int[] Attack = { 3, 10, 15, 15, 15 };
        private static readonly int[] Floors = { 3, 0, 0, 1, 2 };
        private static readonly string[][] Statuses = { new[] { "immobile", "lifelink", "pyrebound", "spell shield" },
            new[] { "lifelink", "pyrebane", "trample" }, new[] { "immobile", "lifelink" },
            new[] { "immobile", "lifelink" }, new[] { "immobile", "lifelink" } };
        public static TrueFinalBossSpawnResult Apply(TrueFinalBossSpawnState source)
        {
            TrueFinalBossSpawnResult Fail(string reason) => new TrueFinalBossSpawnResult(null, reason);
            BattleTurnState battle = source.Battle; EnemySpawnState old = battle.Spawn;
            TrainCombatState train = old.Train; CombatContext? captured = train.Context;
            var covenants = captured?.Relics?.Where(relic => relic.IsCovenant == true).ToArray() ?? Array.Empty<CombatRelicState>();
            bool covenantBirth = covenants.Length == 1 && new[] { "Covenant00_FTUE", "Covenant01_StrongerEnemiesMoreCards",
                "Covenant04_StrongerBosses", "Covenant08_StrongerEnemies" }.Contains(covenants[0].AssetKey) &&
                !string.IsNullOrEmpty(covenants[0].DataId) && source.Definitions.All(definition => definition.BirthStats is { } stats &&
                    stats.CovenantIds.SequenceEqual(new[] { covenants[0].DataId }) &&
                    TrueFinalBossCovenantModel.Adjustment(captured, definition.Unit.AssetKey, out int attack, out int health) &&
                    stats.Attack == attack && stats.Health == health &&
                    (long)definition.Unit.BaseAttack + stats.Attack <= int.MaxValue &&
                    (long)definition.Unit.MaxHealth + stats.Health <= int.MaxValue && definition.Unit.Modifiers != null);
            if (!covenantBirth && source.Definitions.Any(definition => definition.BirthStats != null))
                return Fail("Birth stat adjustments require their calibrated original Covenant context.");
            if (source.ExternalInteractions.Count > 0) return Fail(string.Join("; ", source.ExternalInteractions));
            if (source.Definitions.Count != 5 || !source.QueuedBosses.OrderBy(value => value).SequenceEqual(new[] { 0, 1, 2 }) ||
                source.OuterBossId != 0 || source.RoomBossIds.Count != 4 || source.RoomBossIds.Any(value => value != 0) ||
                train.Rooms.Count != 4 || train.Rooms.Where((room, index) => room.RoomIndex != index).Any() ||
                train.Movement.Count != 0 || train.Rooms.SelectMany(room => room.Units).Count(unit => unit.IsPyre) != 1 ||
                train.Rooms.Any(room => room.Preview || room.Units.Any(unit => !unit.IsPyre && !TrueFinalBossCombatModel.SupportsAlly(unit, train.Context)) ||
                    room.ExternalInteractions.Any(value => value != "Queued final bosses")) ||
                battle.PlayRules == null || battle.PlayRules.Rooms.Any(room => !room.Enabled) ||
                battle.MoonPhaseTrackingActive == true || battle.ExternalInteractions.Count > 0 || old.ExternalInteractions.Count > 0 ||
                captured?.SpawnPoints == null || captured.LifeLinks != null || captured.Enchantments?.AutomaticLifecycle != true ||
                captured.Enchantments.RetainedUnits.Count != 0 || captured.Enchantments.EnchanterIds.Count != 0 ||
                captured.Relics?.Any(relic => relic.IsCovenant == true ? !covenantBirth : relic.EffectTypes.Count > 0) == true ||
                old.OuterBoss != null || old.QueuedOuterBoss != null)
                return Fail("True-final-boss birth requires the calibrated fresh five-actor route, enabled floors and relic context.");
            for (int index = 0; index < 5; index++)
            {
                EnemyDefinition definition = source.Definitions[index]; CombatUnit unit = definition.Unit;
                string[] phases = index == 1 ? new[] { "PostCombat", "OnTrainRoomLoop" } :
                    index == 2 ? new[] { "PostCombat", "OnSpawn" } : new[] { "PostCombat" };
                if (definition.ExternalInteractions.Count > 0 || definition.CompanionBoss || unit.Id != 0 ||
                    unit.AssetKey != Assets[index] || unit.Health != Health[index] || unit.MaxHealth != Health[index] ||
                    unit.BaseAttack != Attack[index] || !unit.CanAttack || !unit.EndsBattleOnDeath || unit.IsBoss != true ||
                    unit.Ability != null || unit.Modifiers?.Upgrades.Count > 0 || unit.BumpRules == null ||
                    unit.BumpRules.OuterBoss != (index >= 2) || unit.BumpRules.PairedBossUnitId != 0 ||
                    index >= 2 && (unit.BumpRules.CastingAnchorSlots == null || unit.BumpRules.CastingAnchorSlots.Count != 4) ||
                    !unit.Statuses.Select(status => status.Id).SequenceEqual(Statuses[index]) ||
                    unit.Statuses.Any(status => status.Stacks != (status.Id == "spell shield" ? 4 : 1)) ||
                    !unit.Triggers.Select(trigger => trigger.Kind).SequenceEqual(phases) || unit.Triggers.Any(trigger => trigger.HasTriggered))
                    return Fail("Uncalibrated true-final-boss definition or creation callbacks: " + unit.AssetKey);
            }
            CombatTrigger spawnTrigger = source.Definitions[2].Unit.Triggers[1];
            if (spawnTrigger.Once || !spawnTrigger.IgnoreSilence || spawnTrigger.FireCount != 1 ||
                spawnTrigger.Conditions != null || spawnTrigger.Effects.Count != 1 ||
                spawnTrigger.Effects[0].Type != "CardEffectAddBattleCard" || spawnTrigger.Effects[0].Generation is not CardGenerationRule generation ||
                generation.Count != 1 || generation.Destination != "DeckPileRandom" || generation.Pool.Count != 1 ||
                generation.Pool[0].DataId != "3b603b7f-300c-4022-9fc2-275448446e58")
                return Fail("Entropy birth requires its original single scourge-card generation trigger.");
            string? invalid = UnitIdentityModel.Validate(captured, train.Rooms.SelectMany(room => room.Units), old.NextUnitId);
            invalid ??= RelicModel.Validate(captured);
            if (invalid != null) return Fail(invalid);
            CombatContext context = captured;
            var created = new CombatUnit[5];
            for (int index = 0; index < 5; index++)
            {
                UnitIdentityAllocation allocation = UnitIdentityModel.Allocate(context);
                if (!allocation.Supported) return Fail(allocation.UnsupportedReason!);
                context = allocation.Context!;
                CombatUnit unit = source.Definitions[index].Create(allocation.UnitId);
                // The definition carries authored placement anchors; the live
                // canonical unit projection retains only its movement roles.
                if (index >= 2) unit = unit.WithBumpRules(new UnitBumpRules(false, false, true));
                created[index] = unit;
            }
            BattleSpawnPoints points = context.SpawnPoints!;
            var groups = points.Groups.ToArray();
            var owners = points.Units.ToList();
            for (int index = 0; index < 5; index++)
            {
                int floor = Floors[index]; bool outer = index >= 2;
                int slot = outer ? source.Definitions[index].Unit.BumpRules!.CastingAnchorSlots?[floor] ?? -1 : 0;
                int position = Array.FindIndex(groups, group => group.RoomIndex == floor && group.Team == CombatTeam.Enemy && group.PreviewCopyId == null);
                if (position < 0 || slot < 0 || slot >= groups[position].Occupants.Count ||
                    groups[position].Occupants[slot] != 0 || groups[position].Outside[slot] != outer)
                    return Fail("Missing free native true-final-boss spawn point.");
                SpawnPointGroupState prior = groups[position]; int[] occupants = prior.Occupants.ToArray();
                occupants[slot] = created[index].Id;
                groups[position] = new SpawnPointGroupState(prior.RoomIndex, prior.Team, prior.InnerCount, prior.GroupCount,
                    occupants, prior.Outside, prior.PreviewCopyId);
                owners.Add(new UnitSpawnPointState(created[index].Id,
                    new SpawnPointReference(floor, CombatTeam.Enemy, outer ? 0 : slot, outerBossAnchor: outer), null, outer));
            }
            context = context.WithSpawnPoints(points.WithState(groups, owners));
            // OnSpawn runs after the complete Entropy group has entered its
            // rooms and before its middle/top aliases link to the bottom body.
            CardGenerationResult generated = CardGenerationModel.Apply(context, generation);
            if (!generated.Supported) return Fail(generated.UnsupportedReason!);
            context = generated.Context!;
            created[2] = created[2].WithTriggers(created[2].Triggers.Select(trigger => trigger.Kind == "OnSpawn"
                ? trigger.Fired(trigger.Effects) : trigger).ToArray());
            context = context.WithLifeLinks(new LifeLinkContext(created.Select((unit, index) => new LifeLinkActor(unit.Id,
                index >= 3 ? created[2].Id : 0, unit.Health, unit.Health, true, true, true, index >= 2)).ToArray(), false, 0));
            RoomCombatState[] rooms = train.Rooms.Select(room => new RoomCombatState(room.RoomIndex, room.Deployment,
                created.Where((unit, index) => Floors[index] == room.RoomIndex).Concat(room.Units).ToArray(),
                room.ExternalInteractions.Where(value => value != "Queued final bosses").ToArray(), context, room.Preview,
                room.ActiveCompanionBossIds)).ToArray();
            var movement = rooms.SelectMany(room => room.Units).Where(unit => unit.Team == CombatTeam.Enemy).Select(unit =>
            {
                EnemyDefinition definition = source.Definitions[Array.FindIndex(created, actor => actor.Id == unit.Id)];
                return new EnemyMovement(unit.Id, 1, definition.Ascends, definition.Loops, definition.CompanionBoss);
            }).ToArray();
            train = EnchantmentWorldModel.Rebase(new TrainCombatState(rooms, movement, train.EnemySlotsPerRoom, context));
            context = train.Context!;
            // CaptureDecision explicitly admits the physical outer bodies for
            // this component. Its nested ordinary room world keeps the live
            // top-body attack guard used by generic turn simulation.
            EnchantmentWorld world = context.Enchantments!;
            context = context.WithEnchantments(new EnchantmentWorld(world.Rooms.Select(room => room.RoomIndex != 2 ? room :
                new RoomCombatState(room.RoomIndex, room.Deployment, room.Units,
                    room.ExternalInteractions.Append("Outer train boss").ToArray(), null, room.Preview, room.ActiveCompanionBossIds)).ToArray(),
                world.Movement, world.EnemySlotsPerRoom, world.RetainedUnits, world.EnchanterIds, world.AllowUpdates,
                world.Updating, world.Preview, world.TestRng, world.AutomaticLifecycle));
            train = CardSpellModel.WithContext(train, context);
            var spawn = new EnemySpawnState(train, old.Waves, old.SelectedGroups, old.Phase, old.Looping, old.Rng,
                context.NextUnitId!.Value, old.Treasures, old.TreasuresRemaining, old.TreasureEnabled, old.FirstTreasureTurn,
                old.FirstTreasureRoom, old.Turn, old.ExternalInteractions, old.CanonicalDecisionReferences, old.PendingDestroyedUnitIds);
            var streams = battle.RngStreams.Select(stream => new BattleRngStream(stream.Name, stream.Seed,
                stream.Name == "Battle" ? context.BattleRng : stream.State)).ToArray();
            var next = new BattleTurnState(spawn, battle.Energy, battle.EnergyPerTurn, battle.DrawPerTurn, battle.ForgePoints,
                battle.DragonsHoard, battle.MoonPhase, streams, context.OtherPiles ?? battle.OtherPiles, battle.ExternalInteractions,
                battle.PlayRules, battle.BattlePreviewEnabled, battle.UiRngIsolated, battle.CanonicalDecisionReferences,
                battle.CanonicalPhysicalReferences, battle.SelectedRoom, battle.MoonPhaseTrackingActive);
            return new TrueFinalBossSpawnResult(new TrueFinalBossSpawnState(next, source.Definitions, Array.Empty<int>(),
                created[4].Id, new[] { created[2].Id, created[3].Id, created[4].Id, 0 }, source.ExternalInteractions));
        }
    }
}
