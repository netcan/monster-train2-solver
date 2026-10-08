using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class UnitCloneGearRule
    {
        public int CardId { get; }
        public CardCreationRule Creation { get; }
        public IReadOnlyList<CardUpgradeModifier> EffectUpgrades { get; }
        public bool Grafted { get; }
        public UnitCloneGearRule(int cardId, CardCreationRule creation, IReadOnlyList<CardUpgradeModifier> effectUpgrades, bool grafted = false)
        { CardId = cardId; Creation = creation; EffectUpgrades = Array.AsReadOnly(effectUpgrades.ToArray()); Grafted = grafted; }
    }

    public sealed class UnitCloneRule
    {
        public CardPlayRule Birth { get; }
        public CardCreationRule? SourceCreation { get; }
        public IReadOnlyList<UnitCloneGearRule> Gear { get; }
        public AbilityChangeRule? Ability { get; }
        public bool GraftedBirth { get; }
        public UnitCloneRule(CardPlayRule birth, CardCreationRule? sourceCreation, IReadOnlyList<UnitCloneGearRule> gear,
            AbilityChangeRule? ability = null, bool graftedBirth = false)
        { Birth = birth; SourceCreation = sourceCreation; Gear = Array.AsReadOnly(gear.ToArray()); Ability = ability; GraftedBirth = graftedBirth; }
    }

    public sealed class UnitCloneCallback
    {
        public int ActorId { get; }
        public string Kind { get; }
        public int ParamInt { get; }
        public int ParamInt2 { get; }
        public string? ParamString { get; }
        public int TriggerCount { get; }
        public int DyingId { get; }
        public int OverrideTargetId { get; }
        public int LastSpawnedOverrideUnitId { get; }
        public UnitCloneCallback(int actorId, string kind, int paramInt, int paramInt2, string? paramString,
            int triggerCount, int dyingId, int overrideTargetId, int lastSpawnedOverrideUnitId)
        { ActorId = actorId; Kind = kind; ParamInt = paramInt; ParamInt2 = paramInt2; ParamString = paramString;
            TriggerCount = triggerCount; DyingId = dyingId; OverrideTargetId = overrideTargetId; LastSpawnedOverrideUnitId = lastSpawnedOverrideUnitId; }
        internal static UnitCloneCallback From(RoomCombatModel.QueuedCharacterTrigger callback) => new UnitCloneCallback(callback.Unit.Id,
            callback.Kind, callback.ParamInt, callback.ParamInt2, callback.ParamString, callback.TriggerCount,
            callback.DyingCharacter?.Id ?? 0, callback.OverrideTarget?.Id ?? 0, callback.LastSpawnedOverrideUnitId);
    }

    public sealed class UnitCloneBoundary
    {
        public string Label { get; }
        public TrainCombatState State { get; }
        public int QueueCount { get; }
        public IReadOnlyList<UnitCloneCallback> Queued { get; }
        public UnitCloneBoundary(string label, TrainCombatState state, int queueCount, IReadOnlyList<UnitCloneCallback>? queued = null)
        { Label = label; State = state; QueueCount = queueCount; Queued = Array.AsReadOnly((queued ?? Array.Empty<UnitCloneCallback>()).ToArray()); }
    }

    public sealed class UnitCloneResult
    {
        public TrainCombatState? State { get; }
        public int UnitId { get; }
        public RoomOutcome Outcome { get; }
        public string? UnsupportedReason { get; }
        public bool Supported => State != null;
        public IReadOnlyList<CombatEvent> Events { get; }
        public IReadOnlyList<UnitCloneBoundary> Boundaries { get; }
        internal IReadOnlyList<RoomCombatModel.QueuedCharacterTrigger> PendingCallbacks { get; }
        internal IReadOnlyList<UnitCloneCallback> Dispatched { get; }
        internal UnitCloneResult(TrainCombatState? state, int unitId, RoomOutcome outcome, string? error,
            IReadOnlyList<CombatEvent> events, IReadOnlyList<UnitCloneBoundary> boundaries,
            IReadOnlyList<RoomCombatModel.QueuedCharacterTrigger> pending, IReadOnlyList<UnitCloneCallback>? dispatched = null)
        { State = state; UnitId = unitId; Outcome = outcome; UnsupportedReason = error;
            Events = Array.AsReadOnly(events.ToArray()); Boundaries = Array.AsReadOnly(boundaries.ToArray());
            PendingCallbacks = Array.AsReadOnly(pending.ToArray()); Dispatched = Array.AsReadOnly((dispatched ?? Array.Empty<UnitCloneCallback>()).ToArray()); }
    }

    public static class CharacterCopyModel
    {
        public static RoomCombatResult CopyStats(RoomCombatState room, int targetId, CombatUnit source,
            IReadOnlyList<StatisticCount> excluded, IReadOnlyList<string> sourceEquipmentStatuses, IReadOnlyList<string> targetEquipmentStatuses)
        {
            CombatUnit? target = room.Units.FirstOrDefault(unit => unit.Id == targetId);
            if (target?.Modifiers == null || source.Modifiers == null || target.StatusRegistry == null || source.StatusRegistry == null)
                return Fail("Character copying requires complete raw modifiers and status dictionaries.");
            var pending = new List<RoomCombatModel.QueuedCharacterTrigger>();
            var events = new List<CombatEvent>(); RoomCombatState state = room;
            foreach (CombatStatus status in source.StatusRegistry.Where(status => status.Stacks > 0))
            {
                if (!status.Hidden.HasValue) return Fail("Character copying requires status visibility metadata.");
                if (status.Hidden.Value || status.Id == "unit_grafted_equipment" || status.Id == "silenced" || sourceEquipmentStatuses.Contains(status.Id)) continue;
                int desired = Math.Max(0, unchecked(status.Stacks - Ignored(status.Id)));
                int amount = unchecked(desired - (Current().RegisteredStatus(status.Id)?.Stacks ?? 0));
                if (amount > 0)
                {
                    RoomCombatResult added = StatusApplicationModel.ApplyRetained(state, targetId, status.WithStacks(amount), 0, allowModification: false);
                    if (!added.Supported) return added;
                    state = added.State!; pending.AddRange(added.PendingCallbacks); events.AddRange(added.Events);
                }
            }
            // Native snapshots the recipient only after all additions. Hidden starting
            // statuses participate in this removal pass, unlike the source addition pass.
            foreach (CombatStatus status in Current().StatusRegistry!.Where(status => status.Stacks > 0).ToArray())
            {
                if (status.Id == "cardless" || targetEquipmentStatuses.Contains(status.Id)) continue;
                int desired = Math.Max(0, unchecked((source.RegisteredStatus(status.Id)?.Stacks ?? 0) - Ignored(status.Id)));
                int remove = unchecked(status.Stacks - desired);
                if (remove <= 0) continue;
                RoomCombatResult removed = StatusRemovalModel.Remove(state, targetId, status.Id, remove);
                if (!removed.Supported) return removed;
                state = removed.State!; pending.AddRange(removed.PendingCallbacks); events.AddRange(removed.Events);
            }
            target = Current(); UnitModifiers old = target.Modifiers!;
            bool special = source.Status("fixedattack") != null || source.Status("armorattack") != null || source.Status("equalizer") != null;
            int attack = source.Status("fixedattack")?.Stacks ?? (source.Status("armorattack") != null
                ? source.Status("armor")?.Stacks ?? 0 : source.Status("equalizer") != null ? source.MaxHealth : source.Modifiers.AttackDamage);
            int buff = unchecked(old.DamageBuff + (special ? 0 : Math.Max(0, source.Modifiers.DamageBuff)));
            var modifiers = new UnitModifiers(attack, old.AttackDamageAdded, buff, old.RawSize, old.EquipmentLimit,
                old.CanBeHealed, old.IsClone, old.Upgrades, old.HealthFromUpgrades, old.SpawnerMatchesDefinition);
            CombatUnit changed = UnitCloneModel.Copy(target, modifiers, Math.Max(0, unchecked(attack + buff)), source.Health, source.MaxHealth);
            state = new RoomCombatState(state.RoomIndex, state.Deployment, state.Units.Select(unit => unit.Id == targetId ? changed : unit).ToArray(),
                state.ExternalInteractions, state.Context, state.Preview);
            return new RoomCombatResult(state, RoomOutcome.Exchanged, 0, events, pendingCallbacks: pending);

            int Ignored(string id) => excluded.FirstOrDefault(item => item.Key == id)?.Value ?? 0;
            CombatUnit Current() => state.Units.Single(unit => unit.Id == targetId);
            RoomCombatResult Fail(string reason) => new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), reason);
        }
    }

    public static class UnitCloneModel
    {
        public static UnitCloneResult Apply(TrainCombatState source, int sourceId, int roomIndex, string spawnMode,
            SpawnPointReference? location, bool cardless, UnitCloneRule? rule)
            => ApplyCore(source, sourceId, roomIndex, spawnMode, location, cardless, rule, null);
        internal static UnitCloneResult ApplyWithPending(TrainCombatState source, int sourceId, int roomIndex,
            SpawnPointReference? location, UnitCloneRule? rule, IReadOnlyList<RoomCombatModel.QueuedCharacterTrigger> prior)
            => ApplyCore(source, sourceId, roomIndex, "SelectedSlot", location, true, rule, prior);
        private static UnitCloneResult ApplyCore(TrainCombatState source, int sourceId, int roomIndex, string spawnMode,
            SpawnPointReference? location, bool cardless, UnitCloneRule? rule, IReadOnlyList<RoomCombatModel.QueuedCharacterTrigger>? prior)
        {
            TrainCombatState state = source; var events = new List<CombatEvent>();
            var boundaries = new List<UnitCloneBoundary>(); var pending = (prior ?? Array.Empty<RoomCombatModel.QueuedCharacterTrigger>()).ToList();
            var dispatched = new List<UnitCloneCallback>();
            int unitId = 0; RoomOutcome outcome = RoomOutcome.Exchanged;
            CombatUnit? original = Get(sourceId);
            if (sourceId == 0) return Result();
            if (original == null) return Fail("Missing retained clone source.");
            if (original.Status("horde") != null)
            {
                HordeMergeResult horde = HordeMergeModel.Clone(FindRoom(sourceId)! , sourceId);
                return Accept(horde.Result) ? Result() : Fail(horde.Result.UnsupportedReason!);
            }
            RoomCombatState? destination = state.Rooms.FirstOrDefault(room => room.RoomIndex == roomIndex);
            if (destination == null) return Result();
            if (!new[] { "FrontSlot", "BackSlot", "SelectedSlot" }.Contains(spawnMode)) return Fail("Unknown clone spawn mode.");
            string? error = TrainCombatModel.Validate(source);
            if (error != null) return Fail(error);
            if (rule == null || source.Context?.SpawnPoints == null || source.Rooms.Any(room => room.Preview))
                return Fail("Ordinary cloning requires captured birth definitions, physical points and a primary state.");
            SpawnPointGroupState? group = state.Context!.SpawnPoints!.Group(roomIndex, CombatTeam.Player);
            if (group == null) return Fail("Missing clone destination points.");
            SpawnPointReference? old = state.Context.SpawnPoints.Units.SingleOrDefault(unit => unit.UnitId == sourceId)?.Current;
            int position = -1;
            if (spawnMode == "SelectedSlot" && location != null && old != null &&
                old.RoomIndex == location.RoomIndex && old.Team == location.Team && old.Index == location.Index)
            {
                if (old.Index + 1 >= group.GroupCount) return Result();
                position = old.Index + 1;
            }
            else if (spawnMode == "SelectedSlot" && location != null) position = location.Index;
            // Excluded status contributions are collected before CopyCardState refreshes
            // descriptors, and before the clone's excluded upgrades are removed.
            var excluded = new Dictionary<string, int>();
            foreach (CardUpgradeModifier upgrade in original.SpawnerCardId == 0 ? Array.Empty<CardUpgradeModifier>() :
                state.Context.FindCard(original.SpawnerCardId)!.Permanent.Upgrades.Concat(state.Context.FindCard(original.SpawnerCardId)!.Temporary.Upgrades))
                if (upgrade.ExcludeFromClones)
                    foreach (CombatStatus status in upgrade.Statuses.Where(status => status.Id.Length > 0 && status.Stacks != 0))
                        excluded[status.Id] = unchecked(excluded.GetValueOrDefault(status.Id) + status.Stacks);
            int spawnerCardId = 0;
            if (original.SpawnerCardId != 0)
            {
                if (rule.SourceCreation == null) return Fail("Missing source card creation rules.");
                CardGenerationResult copied = CardGenerationModel.CloneDetached(state.Context, rule.SourceCreation, original.SpawnerCardId);
                if (!copied.Supported) return Fail(copied.UnsupportedReason!);
                state = CardSpellModel.WithContext(state, copied.Context!); spawnerCardId = copied.AddedCards.Single().InstanceId;
                Mark("source-card-copied");
                CardInstanceState card = state.Context!.FindCard(spawnerCardId)!;
                card = new CardInstanceState(card.InstanceId, card.DataId, Strip(card.Permanent), Strip(card.Temporary),
                    card.LastPlayedCost, card.LastForgedAmount, card.PlayCount, card.ExternalInteractions, card.EffectCounters,
                    card.DamageScalingTraits, card.StatusScalingTraits, card.UnitUpgradeScalingTraits, card.CapacityScalingTraits,
                    card.EquippedUnitId, card.PlayedRoomUnitIds, card.RawPlayedRoomUnitIds);
                state = CardSpellModel.WithContext(state, state.Context.WithCard(card));
            }
            Mark("before-birth");
            int empty = state.Context!.SpawnPoints!.FirstEmpty(roomIndex, CombatTeam.Player);
            if (empty < 0) { Mark("after-birth"); return Result(); }
            if (rule.GraftedBirth) return Fail("Clone birth with grafted equipment requires its native birth transition.");
            if (spawnMode == "FrontSlot") position = empty;
            if (spawnMode == "BackSlot") position = Enumerable.Range(0, group.GroupCount).Last(index => group.Occupants[index] == 0);
            if (position < 0) position = empty;
            destination = state.Rooms.Single(room => room.RoomIndex == roomIndex);
            UnitBirthResult born = UnitBirthModel.SpawnClone(destination, rule.Birth, spawnerCardId, position, cardless,
                state.Context.StatusRules.SingleOrDefault(status => status.Id == "cardless")?.WithStacks(1), spawnMode == "SelectedSlot",
                prior == null ? null : DrainBirthQueue);
            if (!born.Supported) return Fail(born.Result.UnsupportedReason!);
            unitId = born.UnitId; if (!Accept(born.Result)) return Fail(born.Result.UnsupportedReason!);
            Mark("after-birth");
            if (Get(unitId) == null) return Fail("Cloning an actor removed during birth requires retained birth-object transitions.");
            original = Get(sourceId);
            if (original == null) return Fail("Clone birth removed its retained source.");
            var targetEquipmentStatuses = new List<string>();
            foreach (int equipmentId in original.EquipmentCards ?? Array.Empty<int>())
            {
                UnitCloneGearRule? equipment = rule.Gear.SingleOrDefault(gear => gear.CardId == equipmentId);
                if (equipment == null) return Fail("Missing clone equipment definitions.");
                if (equipment.Grafted) continue;
                CardGenerationResult copied = CardGenerationModel.CloneDetached(state.Context!, equipment.Creation, equipmentId);
                if (!copied.Supported) return Fail(copied.UnsupportedReason!);
                state = CardSpellModel.WithContext(state, copied.Context!); int newCardId = copied.AddedCards.Single().InstanceId;
                Mark("gear-card-copied");
                CombatUnit actor = Get(unitId)!;
                state = ReplaceUnit(state, Copy(actor, actor.Modifiers!, actor.BaseAttack, actor.Health, actor.MaxHealth,
                    (actor.EquipmentCards ?? Array.Empty<int>()).Append(newCardId).ToArray()));
                state = CardSpellModel.WithContext(state, state.Context!.WithCard(state.Context.FindCard(newCardId)!.WithEquippedUnit(unitId)));
                CardInstanceState newCard = state.Context!.FindCard(newCardId)!;
                foreach (CardUpgradeModifier upgrade in equipment.EffectUpgrades.Concat(newCard.Permanent.Upgrades).Concat(newCard.Temporary.Upgrades))
                {
                    RoomCombatResult applied = RoomCombatModel.ApplyDirectUnitUpgrade(FindRoom(unitId)!, unitId, upgrade,
                        false, "", null, equipmentSourceCardId: newCardId, captureAllDispatches: prior != null);
                    if (!Accept(applied)) return Fail(applied.UnsupportedReason!);
                }
                targetEquipmentStatuses.AddRange(equipment.EffectUpgrades.SelectMany(upgrade => upgrade.Statuses).Select(status => status.Id));
            }
            original = Get(sourceId)!;
            RoomCombatResult stats = CharacterCopyModel.CopyStats(FindRoom(unitId)!, unitId, original,
                excluded.Select(item => new StatisticCount(item.Key, item.Value)).ToArray(),
                rule.Gear.SelectMany(gear => gear.EffectUpgrades).SelectMany(upgrade => upgrade.Statuses).Select(status => status.Id).Distinct().ToArray(),
                targetEquipmentStatuses);
            if (!Accept(stats)) return Fail(stats.UnsupportedReason!);
            Mark("after-stats");
            original = Get(sourceId)!; CombatUnit target = Get(unitId)!;
            if (original.Ability?.HasAbility == true)
            {
                if (target.Ability?.HasAbility != true || target.Ability.DataId != original.Ability.DataId)
                {
                    if (rule.Ability?.Definition?.DataId != original.Ability.DataId) return Fail("Missing copied ability definition.");
                    RoomCombatResult ability = AbilityLifecycleModel.ApplyWithPending(FindRoom(unitId)!, unitId, rule.Ability, pending, deferCallbacks: true);
                    pending.Clear(); if (!Accept(ability)) return Fail(ability.UnsupportedReason!);
                }
                target = Get(unitId)!;
                state = ReplaceUnit(state, AbilityCooldownModel.Copy(target, target.Ability!.WithCooldown(original.Ability.Cooldown)));
            }
            Mark("after-ability"); return Result();

            CombatUnit? Get(int id) => state.Rooms.SelectMany(room => room.Units).FirstOrDefault(unit => unit.Id == id);
            RoomCombatState? FindRoom(int id) => state.Rooms.FirstOrDefault(room => room.Units.Any(unit => unit.Id == id));
            void Mark(string label) => boundaries.Add(new UnitCloneBoundary(label, state, pending.Count, pending.Select(UnitCloneCallback.From).ToArray()));
            bool Accept(RoomCombatResult result)
            {
                if (!result.Supported) return false;
                state = CardSpellModel.WithContext(new TrainCombatState(state.Rooms.Select(room => room.RoomIndex == result.State!.RoomIndex
                    ? result.State : room).ToArray(), state.Movement, state.EnemySlotsPerRoom, result.State!.Context), result.State.Context!);
                pending.AddRange(result.PendingCallbacks); events.AddRange(result.Events);
                if (prior != null) dispatched.AddRange(result.Dispatches.Select(item => new UnitCloneCallback(item.ActorId, item.Kind,
                    item.ParamInt, item.ParamInt2, item.ParamString, item.TriggerCount, item.DyingId, item.OverrideTargetId, item.LastSpawnedOverrideUnitId)));
                if (result.Outcome != RoomOutcome.Exchanged) outcome = result.Outcome;
                return true;
            }
            RoomCombatResult DrainBirthQueue(RoomCombatState frame, IReadOnlyList<RoomCombatModel.QueuedCharacterTrigger> added)
            {
                state = CardSpellModel.WithContext(new TrainCombatState(state.Rooms.Select(room => room.RoomIndex == frame.RoomIndex ? frame : room).ToArray(),
                    state.Movement, state.EnemySlotsPerRoom, frame.Context), frame.Context!);
                pending.AddRange(added);
                UnitCloneResult drained = Drain(new UnitCloneResult(state, unitId, outcome, null, Array.Empty<CombatEvent>(),
                    Array.Empty<UnitCloneBoundary>(), pending));
                if (!drained.Supported) return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, drained.Events.ToList(), drained.UnsupportedReason);
                state = drained.State!; pending.Clear(); dispatched.AddRange(drained.Dispatched);
                return new RoomCombatResult(state.Rooms.Single(room => room.RoomIndex == frame.RoomIndex), drained.Outcome, 0, drained.Events.ToList());
            }
            UnitCloneResult Result() => new UnitCloneResult(state, unitId, outcome, null, events, boundaries, pending, dispatched);
            UnitCloneResult Fail(string reason) => new UnitCloneResult(null, unitId, RoomOutcome.Unsupported, reason, events, boundaries, pending);
        }

        public static UnitCloneResult Drain(UnitCloneResult input)
        {
            if (!input.Supported) return input;
            TrainCombatState state = input.State!; var queue = input.PendingCallbacks.ToList(); var events = input.Events.ToList();
            var dispatched = input.Dispatched.ToList();
            RoomOutcome outcome = input.Outcome; string? error = null;
            bool okay = RoomCombatModel.DrainCharacterQueue(queue, callback =>
                { dispatched.Add(UnitCloneCallback.From(callback)); return Accept(RoomCombatModel.ApplyQueuedCharacterTrigger(
                    state.Rooms.Single(room => room.RoomIndex == callback.RoomIndex), callback, queue.Add)); },
                callback => Accept(RoomCombatModel.SettleQueuedSpawnerAndCenter(state.Rooms.Single(room => room.RoomIndex == callback.RoomIndex), callback.Unit)),
                () =>
                {
                    TrainCombatResult updated = EnchantmentWorldModel.UpdateAll(state, queue.Add);
                    if (!updated.Supported) { error = updated.UnsupportedReason; return false; }
                    state = updated.State!; return true;
                });
            return new UnitCloneResult(okay ? state : null, input.UnitId, okay ? outcome : RoomOutcome.Unsupported,
                error, events, input.Boundaries, okay ? Array.Empty<RoomCombatModel.QueuedCharacterTrigger>() : queue, dispatched);

            bool Accept(RoomCombatResult result)
            {
                if (!result.Supported) { error = result.UnsupportedReason; return false; }
                state = CardSpellModel.WithContext(new TrainCombatState(state.Rooms.Select(room => room.RoomIndex == result.State!.RoomIndex
                    ? result.State : room).ToArray(), state.Movement, state.EnemySlotsPerRoom, result.State!.Context), result.State.Context!);
                events.AddRange(result.Events);
                if (result.Outcome != RoomOutcome.Exchanged) outcome = result.Outcome;
                return true;
            }
        }

        private static CardModifiers Strip(CardModifiers modifiers)
        {
            var list = modifiers.Upgrades.ToList();
            for (int index = list.Count - 1; index >= 0; index--)
                if (list[index].ExcludeFromClones)
                    list.RemoveAt(list[index].DataId.Length == 0 ? index : list.FindIndex(upgrade => upgrade.DataId == list[index].DataId));
            return new CardModifiers(modifiers.Offsets, list, modifiers.PersistentHealth, modifiers.ExternalInteractions);
        }
        internal static CombatUnit Copy(CombatUnit unit, UnitModifiers modifiers, int attack, int hp, int maxHp, IReadOnlyList<int>? equipment = null) =>
            new CombatUnit(unit.Id, unit.AssetKey, unit.Team, attack, hp, maxHp, unit.CanAttack, unit.IsPyre, unit.EndsBattleOnDeath,
                unit.Statuses, unit.Triggers, unit.SpawnerCardId, unit.Size, unit.StatusImmunities, unit.Subtypes, modifiers, unit.IsBoss,
                unit.LastAttackerId, unit.StatusRegistry, equipment ?? unit.EquipmentCards, unit.NextTriggerId, unit.Ability, unit.StatusDictionary,
                unit.AbilityRules, unit.HordeDefinition, unit.IsSpawning, unit.SacrificeCardId, unit.DeathState, unit.BumpRules);
        internal static TrainCombatState ReplaceUnit(TrainCombatState state, CombatUnit unit) => EnchantmentWorldModel.Rebase(
            new TrainCombatState(state.Rooms.Select(room =>
                new RoomCombatState(room.RoomIndex, room.Deployment, room.Units.Select(actor => actor.Id == unit.Id ? unit : actor).ToArray(),
                    room.ExternalInteractions, state.Context, room.Preview)).ToArray(), state.Movement, state.EnemySlotsPerRoom, state.Context));
    }
}
