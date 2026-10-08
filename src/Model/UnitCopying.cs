using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class UnitCopyBirthDefinition
    {
        public CombatUnit Unit { get; }
        public IReadOnlyList<string> ExternalInteractions { get; }
        public bool Grafted { get; }
        public EnemyDefinition? HeroDefinition { get; }
        public UnitCopyBirthDefinition(CombatUnit unit, IReadOnlyList<string> externalInteractions, bool grafted, EnemyDefinition? heroDefinition = null)
        { Unit = unit; ExternalInteractions = Array.AsReadOnly(externalInteractions.ToArray()); Grafted = grafted; HeroDefinition = heroDefinition; }
    }
    public sealed class UnitCopyGearDefinition
    {
        public string DataId { get; }
        public IReadOnlyList<CardUpgradeModifier> Upgrades { get; }
        public bool Grafted { get; }
        public UnitCopyGearDefinition(string dataId, IReadOnlyList<CardUpgradeModifier> upgrades, bool grafted)
        { DataId = dataId; Upgrades = Array.AsReadOnly(upgrades.ToArray()); Grafted = grafted; }
    }
    public sealed class UnitCopyCatalog
    {
        public IReadOnlyList<UnitCopyBirthDefinition> Births { get; }
        public IReadOnlyList<CardCreationRule> Cards { get; }
        public IReadOnlyList<UnitCopyGearDefinition> Gear { get; }
        public IReadOnlyList<AbilityChangeRule> Abilities { get; }
        public UnitCopyCatalog(IReadOnlyList<UnitCopyBirthDefinition> births, IReadOnlyList<CardCreationRule> cards,
            IReadOnlyList<UnitCopyGearDefinition> gear, IReadOnlyList<AbilityChangeRule> abilities)
        { Births = Array.AsReadOnly(births.ToArray()); Cards = Array.AsReadOnly(cards.ToArray());
            Gear = Array.AsReadOnly(gear.ToArray()); Abilities = Array.AsReadOnly(abilities.ToArray()); }

        internal UnitCloneRule? Resolve(CombatContext context, CombatUnit actor)
        {
            UnitCopyBirthDefinition? birth = Births.SingleOrDefault(item => item.Unit.AssetKey == actor.AssetKey);
            if (birth == null) return null;
            CardInstanceState? source = actor.SpawnerCardId == 0 ? null : context.FindCard(actor.SpawnerCardId);
            CardCreationRule? creation = source == null ? null : Cards.SingleOrDefault(item => item.DataId == source.DataId);
            if (actor.SpawnerCardId > 0 && creation == null) return null;
            var gear = new List<UnitCloneGearRule>();
            foreach (int id in actor.EquipmentCards ?? Array.Empty<int>())
            {
                CardInstanceState? card = context.FindCard(id);
                UnitCopyGearDefinition? definition = Gear.SingleOrDefault(item => item.DataId == card?.DataId);
                CardCreationRule? copy = Cards.SingleOrDefault(item => item.DataId == card?.DataId);
                if (definition == null || copy == null) return null;
                gear.Add(new UnitCloneGearRule(id, copy, definition.Upgrades, definition.Grafted));
            }
            CombatUnit template = birth.Unit;
            if (template.Modifiers != null && actor.Modifiers != null)
            {
                UnitModifiers old = template.Modifiers;
                template = UnitCloneModel.Copy(template, new UnitModifiers(old.AttackDamage, old.AttackDamageAdded, old.DamageBuff,
                    old.RawSize, old.EquipmentLimit, old.CanBeHealed, old.IsClone, old.Upgrades, old.HealthFromUpgrades,
                    actor.Modifiers.SpawnerMatchesDefinition), template.BaseAttack, template.Health, template.MaxHealth);
            }
            var rule = new CardPlayRule(source?.DataId ?? "", template.AssetKey, 0, "SpawnMonster", "Standby", template, birth.ExternalInteractions);
            AbilityChangeRule? ability = actor.Ability?.HasAbility == true ? Abilities.SingleOrDefault(item =>
                item.Definition?.DataId == actor.Ability.DataId && item.FromEquipment == actor.Ability.FromEquipment) : null;
            return new UnitCloneRule(rule, creation, gear, ability, birth.Grafted);
        }
    }
    public sealed class UnitCopyResult
    {
        public TrainCombatState? State { get; }
        public int Spawned { get; }
        public RoomOutcome Outcome { get; }
        public string? UnsupportedReason { get; }
        public bool Supported => State != null;
        public IReadOnlyList<CombatEvent> Events { get; }
        internal IReadOnlyList<RoomCombatModel.QueuedCharacterTrigger> PendingCallbacks { get; }
        internal IReadOnlyList<UnitCloneCallback> Dispatched { get; }
        internal UnitCopyResult(TrainCombatState? state, int spawned, RoomOutcome outcome, string? error,
            IReadOnlyList<CombatEvent> events, IReadOnlyList<RoomCombatModel.QueuedCharacterTrigger> pending,
            IReadOnlyList<UnitCloneCallback> dispatched)
        { State = state; Spawned = spawned; Outcome = outcome; UnsupportedReason = error;
            Events = Array.AsReadOnly(events.ToArray()); PendingCallbacks = Array.AsReadOnly(pending.ToArray());
            Dispatched = Array.AsReadOnly(dispatched.ToArray()); }
    }
    public static class UnitCopyModel
    {
        public static UnitCopyResult Apply(TrainCombatState source, int roomIndex, IReadOnlyList<int> targets,
            int count, UnitCopyCatalog? catalog, bool heroBirth = false, bool copyHeroStats = false)
            => ApplyWithPending(source, roomIndex, targets, count, catalog, Array.Empty<RoomCombatModel.QueuedCharacterTrigger>(), heroBirth, copyHeroStats);
        internal static UnitCopyResult ApplyWithPending(TrainCombatState source, int roomIndex, IReadOnlyList<int> targets,
            int count, UnitCopyCatalog? catalog, IReadOnlyList<RoomCombatModel.QueuedCharacterTrigger> prior, bool heroBirth = false, bool copyHeroStats = false)
        {
            TrainCombatState state = source; int spawned = 0; RoomOutcome outcome = RoomOutcome.Exchanged;
            var events = new List<CombatEvent>(); var pending = prior.ToList(); var dispatched = new List<UnitCloneCallback>();
            string? error = TrainCombatModel.Validate(source);
            if (error != null) return Fail(error);
            if (source.Context?.SpawnPoints == null || source.Rooms.Any(room => room.Preview))
                return Fail("Paid copying requires physical references and primary birth transitions.");
            RoomCombatState? room = source.Rooms.FirstOrDefault(item => item.RoomIndex == roomIndex);
            if (room == null) return Fail("Missing selected copy room.");
            CombatUnit[] original = room.Units.Where(unit => unit.Health > 0 && unit.DeathState?.IsDestroyed != true)
                .OrderBy(unit => unit.Team == CombatTeam.Enemy ? 0 : 1).ThenBy(unit => Point(unit.Id)?.Index ?? int.MaxValue).ToArray();
            foreach (int id in targets)
            {
                SpawnPointReference? location = Point(id);
                if (location == null) continue;
                for (int index = 0; index < count; index++)
                {
                    CombatUnit? actor = state.Rooms.SelectMany(item => item.Units).FirstOrDefault(unit => unit.Id == id);
                    if (actor == null) return Fail("Copying a removed source requires retained native target objects.");
                    UnitCloneResult copy = heroBirth ? HeroUnitBirthModel.Apply(state, id, roomIndex, catalog, copyHeroStats, pending) :
                        UnitCloneModel.ApplyWithPending(state, id, roomIndex, location, catalog?.Resolve(state.Context!, actor), pending);
                    if (!copy.Supported) return Fail(copy.UnsupportedReason!);
                    state = copy.State!; pending = copy.PendingCallbacks.ToList(); events.AddRange(copy.Events); dispatched.AddRange(copy.Dispatched);
                    // SpawnHeroInRoom's output parameter is passed by value in native code.
                    // Hero actors are created, but this effect's counted spawn total stays zero.
                    if (!heroBirth && copy.UnitId > 0) spawned++;
                    if (copy.Outcome != RoomOutcome.Exchanged) outcome = copy.Outcome;
                }
            }
            if (spawned > 0)
                pending.AddRange(original.Select(actor => new RoomCombatModel.QueuedCharacterTrigger(roomIndex, actor, "CardMonsterPlayed", triggerCount: spawned)));
            if (spawned > 1)
            {
                room = state.Rooms.Single(item => item.RoomIndex == roomIndex);
                BattleSpawnPointResult centered = BattleSpawnPointModel.Apply(state.Context!.SpawnPoints!, room, "Compact", CombatTeam.Player);
                if (!centered.Supported) return Fail(centered.Error!);
                CombatContext context = state.Context.WithSpawnPoints(centered.State!);
                CombatUnit[] players = BattleSpawnPointModel.Order(centered.State!, roomIndex, room.Units.Where(unit => unit.Team == CombatTeam.Player).ToArray()).ToArray();
                var frame = new RoomCombatState(roomIndex, room.Deployment,
                    room.Units.Where(unit => unit.Team == CombatTeam.Enemy).Concat(players).ToArray(), room.ExternalInteractions, context);
                state = CardSpellModel.WithContext(new TrainCombatState(state.Rooms.Select(item => item.RoomIndex == roomIndex ? frame : item).ToArray(),
                    state.Movement, state.EnemySlotsPerRoom, context), context);
            }
            return new UnitCopyResult(state, spawned, outcome, null, events, pending, dispatched);

            SpawnPointReference? Point(int id) => state.Context!.SpawnPoints!.Units.SingleOrDefault(unit => unit.UnitId == id)?.Current;
            UnitCopyResult Fail(string reason) => new UnitCopyResult(null, spawned, RoomOutcome.Unsupported, reason, events, pending, dispatched);
        }
    }
}
