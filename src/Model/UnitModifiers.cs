using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class UnitModifiers
    {
        public int AttackDamage { get; }
        public int AttackDamageAdded { get; }
        public int DamageBuff { get; }
        public int? DamageBuffFromStatusEffects { get; }
        public int? MagebladeDamagePerMagicPower { get; }
        public IReadOnlyList<RelicStatusAttackModifier>? StatusAttackModifiers { get; }
        public int RawSize { get; }
        public int EquipmentLimit { get; }
        public bool CanBeHealed { get; }
        public bool IsClone { get; }
        public bool? IsChampion { get; }
        public IReadOnlyList<UnitSelfAttackRule>? SelfAttackRules { get; }
        public IReadOnlyList<UnitRoomResourceRule>? RoomResources { get; }
        public IReadOnlyList<UnitRoomStatusRule>? RoomStatuses { get; }
        public IReadOnlyList<TriggerCountModifier>? RoomTriggerCounts { get; }
        public bool SpawnerMatchesDefinition { get; }
        public IReadOnlyList<CardUpgradeModifier> Upgrades { get; }
        public IReadOnlyList<StatisticCount> HealthFromUpgrades { get; }
        public UnitModifiers(int attackDamage, int attackDamageAdded, int damageBuff, int rawSize, int equipmentLimit,
            bool canBeHealed, bool isClone, IReadOnlyList<CardUpgradeModifier> upgrades, IReadOnlyList<StatisticCount>? healthFromUpgrades = null,
            bool spawnerMatchesDefinition = true, int? damageBuffFromStatusEffects = null,
            int? magebladeDamagePerMagicPower = null, IReadOnlyList<RelicStatusAttackModifier>? statusAttackModifiers = null,
            bool? isChampion = null, IReadOnlyList<UnitSelfAttackRule>? selfAttackRules = null,
            IReadOnlyList<UnitRoomResourceRule>? roomResources = null, IReadOnlyList<UnitRoomStatusRule>? roomStatuses = null,
            IReadOnlyList<TriggerCountModifier>? roomTriggerCounts = null)
        {
            AttackDamage = attackDamage; AttackDamageAdded = attackDamageAdded; DamageBuff = damageBuff; IsChampion = isChampion; RawSize = rawSize;
            EquipmentLimit = equipmentLimit; CanBeHealed = canBeHealed; IsClone = isClone; Upgrades = Array.AsReadOnly(upgrades.ToArray());
            HealthFromUpgrades = Array.AsReadOnly((healthFromUpgrades ?? Array.Empty<StatisticCount>()).ToArray());
            SpawnerMatchesDefinition = spawnerMatchesDefinition;
            DamageBuffFromStatusEffects = damageBuffFromStatusEffects;
            MagebladeDamagePerMagicPower = magebladeDamagePerMagicPower;
            StatusAttackModifiers = statusAttackModifiers == null ? null : Array.AsReadOnly(statusAttackModifiers.ToArray());
            SelfAttackRules = selfAttackRules == null ? null : Array.AsReadOnly(selfAttackRules.ToArray());
            RoomResources = roomResources == null ? null : Array.AsReadOnly(roomResources.ToArray());
            RoomStatuses = roomStatuses == null ? null : Array.AsReadOnly(roomStatuses.ToArray());
            RoomTriggerCounts = roomTriggerCounts == null ? null : Array.AsReadOnly(roomTriggerCounts.ToArray());
        }
        internal UnitModifiers WithStatusDamage(int amount) => new UnitModifiers(AttackDamage, AttackDamageAdded, DamageBuff,
            RawSize, EquipmentLimit, CanBeHealed, IsClone, Upgrades, HealthFromUpgrades, SpawnerMatchesDefinition, amount,
            MagebladeDamagePerMagicPower, StatusAttackModifiers, IsChampion, SelfAttackRules, RoomResources, RoomStatuses, RoomTriggerCounts);
        internal UnitModifiers WithMagebladeDamage(int amount) => new UnitModifiers(AttackDamage, AttackDamageAdded, DamageBuff,
            RawSize, EquipmentLimit, CanBeHealed, IsClone, Upgrades, HealthFromUpgrades, SpawnerMatchesDefinition,
            DamageBuffFromStatusEffects, amount, StatusAttackModifiers, IsChampion, SelfAttackRules, RoomResources, RoomStatuses, RoomTriggerCounts);
        internal UnitModifiers ResetStartingStatusCaches() => new UnitModifiers(AttackDamage, AttackDamageAdded, DamageBuff,
            RawSize, EquipmentLimit, CanBeHealed, IsClone, Upgrades, HealthFromUpgrades, SpawnerMatchesDefinition,
            DamageBuffFromStatusEffects.HasValue ? 0 : (int?)null,
            MagebladeDamagePerMagicPower.HasValue ? 0 : (int?)null, StatusAttackModifiers, IsChampion, SelfAttackRules, RoomResources, RoomStatuses, RoomTriggerCounts);
    }

    public static class UnitModifierModel
    {
        public static RoomCombatResult Apply(RoomCombatState source, int targetId, CardUpgradeModifier upgrade, string lifetime,
            bool remove = false, int? roomCapacity = null, int sourceCardId = 0, string? triggerKind = null, bool scaleUpgrade = false,
            Func<CombatContext, bool, RelicPyreAttackRefreshResult>? refreshPyre = null,
            bool accumulateScaledUpgradeRecord = true)
            => RoomCombatModel.ApplyUnitUpgrade(source, targetId, upgrade, lifetime, remove, roomCapacity, sourceCardId, triggerKind,
                scaleUpgrade, refreshPyre: refreshPyre, accumulateScaledUpgradeRecord: accumulateScaledUpgradeRecord);

        // CharacterState's direct API, used by equipment, changes only the live unit.
        // Anonymous removal needs an explicit applied-list index to represent object identity;
        // a freshly constructed anonymous descriptor reverses stats but leaves the list entry.
        public static RoomCombatResult ApplyDirect(RoomCombatState source, int targetId, CardUpgradeModifier upgrade,
            bool remove = false, string upgradeId = "", int? anonymousRemovalIndex = null,
            Func<CombatContext, bool, RelicPyreAttackRefreshResult>? refreshPyre = null)
            => RoomCombatModel.ApplyDirectUnitUpgrade(source, targetId, upgrade, remove, upgradeId, anonymousRemovalIndex,
                refreshPyre: refreshPyre);

        internal static RoomCombatResult ApplyDirectDeferred(RoomCombatState source, int targetId, CardUpgradeModifier upgrade,
            Func<CombatContext, bool, RelicPyreAttackRefreshResult>? refreshPyre = null)
            => RoomCombatModel.ApplyDirectUnitUpgrade(source, targetId, upgrade, false, upgrade.DataId, null, deferCallbacks: true,
                refreshPyre: refreshPyre);

        // A running room engine settles deaths on its existing unit references and trigger flags.
        // Starting another engine here would reset preview triggers and detach combat attackers.
        internal static RoomCombatResult ApplyWithSettlement(RoomCombatState source, int targetId, CardUpgradeModifier upgrade,
            string lifetime, bool remove, int? roomCapacity, int sourceCardId, string? triggerKind,
            Func<RoomCombatState, CombatUnit, bool, RoomCombatResult> settle, bool allowDyingTarget = false,
            Action<RoomCombatModel.QueuedCharacterTrigger>? enqueueCallback = null, bool directApi = false,
            string upgradeId = "", int? anonymousRemovalIndex = null, int equipmentSourceCardId = 0,
            bool deferAbilityCallbacks = false, bool scaleUpgrade = false, bool accumulateScaledUpgradeRecord = true)
        {
            string? error = RoomCombatModel.Validate(source, allowDyingTarget ? targetId : (int?)null);
            if (error != null) return Unsupported(error);
            CombatUnit? target = source.Units.FirstOrDefault(unit => unit.Id == targetId);
            if (target?.Modifiers == null || source.Context?.CardInstances == null)
                return Unsupported("Unit upgrades require unit and card instance modifier state.");
            CardInstanceState? spawnerCard = source.Context.FindCard(target.SpawnerCardId);
            string? exhaustError = !directApi && lifetime == "Permanent" && spawnerCard != null ? CardExhaustModel.PermanentUpgradeError(spawnerCard, upgrade) : null;
            if (exhaustError != null) return Unsupported(exhaustError);
            roomCapacity = RoomCapacityModel.Maximum(source.Context, source.RoomIndex, target.Team) ?? roomCapacity;
            if (upgrade.ExternalInteractions.Count > 0) return Unsupported(string.Join("; ", upgrade.ExternalInteractions));
            if (scaleUpgrade && (remove || directApi || upgrade.Unique || upgrade.DataId.Length == 0))
                return Unsupported("Scaled unit upgrades require a non-unique defined addition through the native effect.");
            if (!directApi) upgradeId = upgrade.DataId;
            if (equipmentSourceCardId < 0 || equipmentSourceCardId > 0 && source.Context.FindCard(equipmentSourceCardId) == null)
                return Unsupported("Missing equipment trigger source.");
            if (!remove && !new[] { "TemporaryUntilEndOfBattle", "TemporaryUntilUnitDeath", "Permanent" }.Contains(lifetime))
                return Unsupported("Unmodeled unit upgrade lifetime.");
            if (remove && upgrade.DataId.Length == 0 && !directApi) return Unsupported("Removing an upgrade requires a definition ID.");
            if (anonymousRemovalIndex.HasValue && (!directApi || !remove || upgrade.DataId.Length != 0 ||
                anonymousRemovalIndex.Value < 0 || anonymousRemovalIndex.Value >= target.Modifiers.Upgrades.Count ||
                target.Modifiers.Upgrades[anonymousRemovalIndex.Value].DataId.Length != 0))
                return Unsupported("Invalid anonymous applied-upgrade identity.");
            if (!remove && !directApi && target.Modifiers.IsClone && upgrade.ExcludeFromClones) return Match(source);
            if (!remove && !directApi)
            {
                UnitUpgradeScalingResult scaled = UnitUpgradeScalingModel.Apply(source.Context, sourceCardId, upgrade, triggerKind);
                if (!scaled.Supported) return Unsupported(scaled.UnsupportedReason!);
                upgrade = scaled.Upgrade!;
                source = new RoomCombatState(source.RoomIndex, source.Deployment, source.Units,
                    source.ExternalInteractions, scaled.Context, source.Preview, source.ActiveCompanionBossIds);
            }
            foreach (CombatStatus status in upgrade.Statuses)
            {
                if (status.Stacks <= 0) continue;
                // OnStacksAdded creates the Mageblade cache after admission.
                CombatUnit statusTemplate = StatusAttackDamageModel.PrepareBirthTemplate(target, new[] { status });
                error = RoomCombatModel.Validate(new RoomCombatState(source.RoomIndex, source.Deployment,
                    new[] { CardSpellModel.Copy(statusTemplate, target.Health, target.Statuses.Where(item => item.Id != status.Id).Concat(new[] { status }).ToArray()) },
                    Array.Empty<string>(), source.Context, source.Preview, source.ActiveCompanionBossIds),
                    allowDyingTarget ? targetId : (int?)null);
                if (error != null) return Unsupported(error);
            }
            if (!remove && upgrade.RestrictSizeToRoomCapacity && upgrade.Stats.Size > 0)
            {
                if (roomCapacity == null) return Unsupported("Restricted size upgrades require a room capacity definition.");
                if (source.Units.Where(unit => unit.Team == target.Team).Sum(unit => (long)unit.Size) > roomCapacity.Value - (long)upgrade.Stats.Size)
                    return Match(source);
            }
            RoomCombatState state = source;
            bool accumulatedExistingUpgrade = false;
            int count = !remove ? 1 : directApi ? (upgrade.DataId.Length == 0 ||
                target.Modifiers.Upgrades.Any(item => item.DataId == upgrade.DataId) ? 1 : 0) :
                target.Modifiers.Upgrades.Count(item => item.DataId == upgrade.DataId);
            for (int index = 0; index < count; index++)
            {
                target = state.Units.FirstOrDefault(unit => unit.Id == targetId);
                if (target == null) return Match(state);
                UnitModifiers modifiers = target.Modifiers!;
                int? nextTriggerId = target.NextTriggerId;
                if (!remove && upgrade.Unique && upgrade.DataId.Length > 0 && modifiers.Upgrades.Any(item => item.DataId == upgrade.DataId)) break;
                int existingUpgradeIndex = scaleUpgrade
                    ? modifiers.Upgrades.ToList().FindIndex(item => item.DataId == upgrade.DataId) : -1;
                accumulatedExistingUpgrade |= existingUpgradeIndex >= 0;
                var upgrades = modifiers.Upgrades.ToList();
                if (remove)
                {
                    if (upgrade.DataId.Length > 0) upgrades.RemoveAt(upgrades.FindIndex(item => item.DataId == upgrade.DataId));
                    else if (anonymousRemovalIndex.HasValue) upgrades.RemoveAt(anonymousRemovalIndex.Value);
                }
                else if (existingUpgradeIndex >= 0)
                {
                    if (accumulateScaledUpgradeRecord) upgrades[existingUpgradeIndex] = upgrades[existingUpgradeIndex].Accumulate(upgrade);
                }
                else upgrades.Add(upgrade);
                int sign = remove ? -1 : 1;
                int damage = Math.Max(0, checked(modifiers.AttackDamage + sign * upgrade.Stats.Damage));
                int added = Math.Max(0, checked(modifiers.AttackDamageAdded + sign * upgrade.Stats.Damage));
                int buff = checked(modifiers.DamageBuff + sign * upgrade.DamageBuff);
                int size = checked(modifiers.RawSize + sign * upgrade.Stats.Size);
                int equipment = checked(modifiers.EquipmentLimit + sign * upgrade.Stats.EquipmentLimit);
                if (!remove) equipment = Math.Min(4, equipment);
                IReadOnlyList<CombatTrigger> triggers = UnitHealerModel.ApplyDamageUpgrade(target.Triggers, unchecked(sign * upgrade.Stats.Damage), source.Preview);
                int health = target.Health, maxHealth = target.MaxHealth;
                var healthFromUpgrades = modifiers.HealthFromUpgrades.ToList();
                var statuses = target.Statuses.ToDictionary(status => status.Id);
                var nextModifiers = new UnitModifiers(damage, added, buff, size, equipment, modifiers.CanBeHealed, modifiers.IsClone, upgrades,
                    healthFromUpgrades, modifiers.SpawnerMatchesDefinition, modifiers.DamageBuffFromStatusEffects,
                    modifiers.MagebladeDamagePerMagicPower, modifiers.StatusAttackModifiers, modifiers.IsChampion, modifiers.SelfAttackRules, modifiers.RoomResources, modifiers.RoomStatuses, modifiers.RoomTriggerCounts);
                var statusCallbacks = new List<RoomCombatModel.QueuedCharacterTrigger>();
                RoomCombatResult? healthFailure = null;
                ChangeHealth(sign * upgrade.Stats.Health, !(remove && upgrade.Stats.Health > 0), !remove || upgrade.Stats.Health < 0);
                if (healthFailure != null) return healthFailure;
                // A negative HP step exits the native application if the target is dead.
                // Positive steps can finish on an already-dying target, including statuses.
                bool partial = sign * upgrade.Stats.Health < 0 && health <= 0;
                if (!partial)
                {
                    ChangeHealth(sign * upgrade.UnhealedHealth, !remove, false);
                    if (healthFailure != null) return healthFailure;
                    partial = sign * upgrade.UnhealedHealth < 0 && health <= 0;
                }

                if (!partial && remove)
                {
                    StatisticCount? associated = healthFromUpgrades.FirstOrDefault(item => item.Key == upgradeId);
                    if (associated != null)
                    {
                        ChangeHealth(-associated.Value, false, false);
                        if (healthFailure != null) return healthFailure;
                        healthFromUpgrades.Remove(associated);
                        partial = health <= 0;
                    }
                }
                if (!partial && !source.Preview && upgrade.TriggerUpgrades != null)
                {
                    var changedTriggers = triggers.ToList();
                    foreach (CombatTrigger definition in upgrade.TriggerUpgrades)
                    {
                        if (remove)
                        {
                            if (upgradeId.Length > 0 && changedTriggers.Any(trigger => trigger.Kind == definition.Kind && trigger.Origin == null))
                                return Unsupported("Removing an attributed trigger requires its upgrade origin.");
                            changedTriggers.RemoveAll(trigger => trigger.Kind == definition.Kind &&
                                (upgradeId.Length == 0 || trigger.Origin?.UpgradeId == upgradeId));
                        }
                        else
                        {
                            changedTriggers.Add(definition.WithOrigin(upgradeId, equipmentSourceCardId, nextTriggerId));
                            if (nextTriggerId.HasValue) nextTriggerId = checked(nextTriggerId.Value + 1);
                        }
                    }
                    triggers = changedTriggers;
                }
                nextModifiers = new UnitModifiers(damage, added, buff, size, equipment, modifiers.CanBeHealed, modifiers.IsClone, upgrades,
                    healthFromUpgrades, modifiers.SpawnerMatchesDefinition, nextModifiers.DamageBuffFromStatusEffects,
                    modifiers.MagebladeDamagePerMagicPower, modifiers.StatusAttackModifiers, modifiers.IsChampion, modifiers.SelfAttackRules, modifiers.RoomResources, modifiers.RoomStatuses, modifiers.RoomTriggerCounts);
                CombatUnit Snapshot() => new CombatUnit(target.Id, target.AssetKey, target.Team, Math.Max(0, checked(damage + buff)), health, maxHealth,
                    target.CanAttack, target.IsPyre, target.EndsBattleOnDeath, statuses.Values.ToArray(), triggers, target.SpawnerCardId,
                    Math.Max(1, Math.Min(6, size)), target.StatusImmunities, target.Subtypes, nextModifiers, target.IsBoss, target.LastAttackerId, target.StatusRegistry, target.EquipmentCards, nextTriggerId, target.Ability, target.StatusDictionary, target.AbilityRules, target.HordeDefinition, target.IsSpawning, target.SacrificeCardId, target.DeathState, bumpRules: target.BumpRules, lastFeederId: target.LastFeederId, moonPhaseMask: target.MoonPhaseMask);
                if (!partial && upgrade.TriggerUpgrades?.Count > 0)
                {
                    error = RoomCombatModel.Validate(new RoomCombatState(state.RoomIndex, state.Deployment,
                        state.Units.Select(unit => unit.Id == targetId ? Snapshot() : unit).ToArray(), state.ExternalInteractions,
                        state.Context, state.Preview, state.ActiveCompanionBossIds),
                        allowDyingTarget ? targetId : (int?)null);
                    if (error != null) return Unsupported(error);
                }

                if (!partial && upgrade.AbilityUpgrade?.Definition != null &&
                    (remove ? target.Ability?.DataId == upgrade.AbilityUpgrade.Definition.DataId :
                        !upgrade.DoNotReplaceExistingAbility || target.Ability?.HasAbility != true))
                {
                    var rule = new AbilityChangeRule(upgrade.AbilityUpgrade.Definition, equipmentSourceCardId > 0, false,
                        upgrade.AbilityUpgrade.CommonTriggers);
                    var staged = new RoomCombatState(state.RoomIndex, state.Deployment,
                        state.Units.Select(unit => unit.Id == targetId ? Snapshot() : unit).ToArray(),
                        state.ExternalInteractions, state.Context, state.Preview, state.ActiveCompanionBossIds);
                    var ability = AbilityLifecycleModel.Apply(staged, targetId, rule, remove, deferCallbacks: deferAbilityCallbacks);
                    if (!ability.Supported) return ability;
                    state = ability.State!; statusCallbacks.AddRange(ability.PendingCallbacks);
                    target = state.Units.Single(unit => unit.Id == targetId);
                    nextModifiers = target.Modifiers!;
                    damage = nextModifiers.AttackDamage; added = nextModifiers.AttackDamageAdded; buff = nextModifiers.DamageBuff;
                    size = nextModifiers.RawSize; equipment = nextModifiers.EquipmentLimit;
                    health = target.Health; maxHealth = target.MaxHealth; triggers = target.Triggers; nextTriggerId = target.NextTriggerId;
                    statuses = target.Statuses.ToDictionary(status => status.Id);
                }
                if (!partial)
                    for (int statusIndex = 0; statusIndex < upgrade.Statuses.Count; statusIndex++)
                    {
                        CombatStatus status = upgrade.Statuses[statusIndex];
                        bool permanentStatus = upgrade.MaskMetadata?.Statuses.Count == upgrade.Statuses.Count &&
                            upgrade.MaskMetadata.Statuses[statusIndex].Id == status.Id && upgrade.MaskMetadata.Statuses[statusIndex].FromPermanentUpgrade;
                        // Upgrade stacks go through AddStatusEffectStacks, not the direct
                        // addition API: zero is ignored and negative stacks are removals.
                        if (!remove && status.Stacks == 0) continue;
                        statuses.TryGetValue(status.Id, out CombatStatus? existing);
                        bool removingStatus = remove || status.Stacks < 0;
                        if (removingStatus && existing == null && target.RegisteredStatus(status.Id) == null) continue;
                        if (!removingStatus && (target.StatusImmunities.Contains(status.Id) || target.Status("immune") != null)) continue;
                        if (!removingStatus && (permanentStatus || status.Id is "conduit" or "mageblade" || nextModifiers.DamageBuffFromStatusEffects.HasValue ||
                            state.Context?.Relics?.Any(relic => relic.StatusStackAdditions?.Count > 0) == true))
                        {
                            RoomCombatResult appliedStatus = StatusApplicationModel.ApplyRetained(Stage(), targetId, status, 0,
                                fromPermanentUpgrade: permanentStatus);
                            if (!appliedStatus.Supported) return appliedStatus;
                            state = appliedStatus.State!; statusCallbacks.AddRange(appliedStatus.PendingCallbacks);
                            Refresh(state.Units.Single(unit => unit.Id == targetId));
                            continue;
                        }
                        CombatUnit beforeStatus = Snapshot();
                        int maximum = (existing ?? target.RegisteredStatus(status.Id) ?? status).Stackable == false ? 1 : 9999;
                        int oldStacks = existing?.Stacks ?? 0;
                        int stacks = remove ? (status.Stacks == -1 ? 0 : Math.Min(maximum, Math.Max(0, checked(oldStacks - status.Stacks)))) :
                            Math.Min(maximum, Math.Max(0, checked(oldStacks + status.Stacks)));
                        statuses[status.Id] = (existing ?? target.RegisteredStatus(status.Id) ?? status).WithStacks(Math.Max(0, stacks));
                        if (removingStatus)
                        {
                            StatusAttackDamageResult attack = StatusAttackDamageModel.Change(state.Context, Snapshot(),
                                existing ?? target.RegisteredStatus(status.Id) ?? status, oldStacks - stacks, adding: false);
                            if (!attack.Supported) return Unsupported(attack.UnsupportedReason!);
                            state = new RoomCombatState(state.RoomIndex, state.Deployment, state.Units,
                                state.ExternalInteractions, attack.Context, state.Preview, state.ActiveCompanionBossIds);
                            Refresh(attack.Unit!);
                            if (status.Id == "conduit")
                            {
                                if (!ConduitModel.Captured(state.Context)) return Unsupported("Conduit upgrade removal requires captured native room/card tracker state.");
                                state = new RoomCombatState(state.RoomIndex, state.Deployment, state.Units, state.ExternalInteractions,
                                    ConduitModel.Change(state.Context!, state.RoomIndex, Snapshot(), source.Preview, refreshSelection: false,
                                        order: state.Units.Select(unit => unit.Id).ToArray()), state.Preview, state.ActiveCompanionBossIds);
                            }
                        }
                        if (status.Id == "horde" && (removingStatus ? stacks < oldStacks : stacks > oldStacks))
                        {
                            RoomCombatResult horde = HordeStatusModel.Change(Stage(), beforeStatus, Snapshot(), stacks - oldStacks);
                            if (!horde.Supported) return horde;
                            state = horde.State!; statusCallbacks.AddRange(horde.PendingCallbacks);
                            Refresh(state.Units.Single(unit => unit.Id == targetId));
                        }
                        if (removingStatus) StatusCallbackModel.Removed(state.RoomIndex, beforeStatus, Snapshot(), status.Id, statusCallbacks);
                        else
                        {
                            string? callbackError = StatusCallbackModel.Added(state.RoomIndex, beforeStatus, Snapshot(), status.Id,
                                statusCallbacks, status, state.Context);
                            if (callbackError != null) return Unsupported(callbackError);
                        }
                    }
                var changed = Snapshot();
                foreach (var callback in statusCallbacks) enqueueCallback?.Invoke(callback);
                // Raw Horde stack removal can set HP to zero without a death signal.
                // Only the negative-HP early-exit path invokes native Sacrifice.
                RoomCombatResult applied = settle(state, changed, partial);
                if (!applied.Supported) return applied;
                state = applied.State!;
                // Failed additions retain their partial unit changes but never write back to
                // the spawner. Removal still processes all copies and clears the source card.
                if (partial && !remove) return Match(state);

                RoomCombatState Stage() => new RoomCombatState(state.RoomIndex, state.Deployment,
                    state.Units.Select(unit => unit.Id == targetId ? Snapshot() : unit).ToArray(),
                    state.ExternalInteractions, state.Context, state.Preview, state.ActiveCompanionBossIds);
                void Refresh(CombatUnit actor)
                {
                    target = actor; nextModifiers = actor.Modifiers!;
                    damage = nextModifiers.AttackDamage; added = nextModifiers.AttackDamageAdded; buff = nextModifiers.DamageBuff;
                    size = nextModifiers.RawSize; equipment = nextModifiers.EquipmentLimit;
                    health = actor.Health; maxHealth = actor.MaxHealth; triggers = actor.Triggers; nextTriggerId = actor.NextTriggerId;
                    statuses = actor.Statuses.ToDictionary(status => status.Id);
                }
                void ChangeHealth(int delta, bool decreaseHealth, bool heal)
                {
                    if (delta < 0)
                    {
                        bool wasAlive = health > 0;
                        maxHealth = Math.Max(0, checked(maxHealth + delta));
                        health = decreaseHealth ? Math.Max(0, checked(health + delta)) : Math.Min(health, maxHealth);
                        // Newly lethal loss sacrifices the actor. Already-dying actors
                        // still settle troop casualties before the upgrade's early exit.
                        if ((!wasAlive || health > 0) && statuses.TryGetValue("horde", out CombatStatus? troops) && troops.Stacks > 1)
                        {
                            RoomCombatResult casualties = HordeStatusModel.SettleHealth(Stage(), targetId);
                            if (!casualties.Supported) { healthFailure = casualties; return; }
                            state = casualties.State!; statusCallbacks.AddRange(casualties.PendingCallbacks);
                            Refresh(state.Units.Single(unit => unit.Id == targetId));
                        }
                    }
                    else if (delta > 0)
                    {
                        maxHealth = Math.Min(99999, checked(maxHealth + delta));
                        if (heal) health = HealingModel.HealedHealth(health, maxHealth, delta, modifiers.CanBeHealed,
                            target.Statuses, fromMaxHealthChange: true);
                    }
                }
            }
            // The native source-card update also follows a unique unit upgrade no-op.
            target = state.Units.FirstOrDefault(unit => unit.Id == targetId);
            if (!directApi && !source.Preview && target?.SpawnerCardId > 0 &&
                (remove || target.Modifiers!.SpawnerMatchesDefinition && lifetime != "TemporaryUntilUnitDeath"))
            {
                CardInstanceState? card = state.Context!.FindCard(target.SpawnerCardId);
                if (card == null) return Unsupported("Missing upgraded unit's spawner card.");
                CardModifiers permanent = card.Permanent, temporary = card.Temporary;
                if (remove) temporary = new CardModifiers(temporary.Offsets,
                    temporary.Upgrades.Where(item => item.DataId != upgrade.DataId).ToArray(), temporary.PersistentHealth, temporary.ExternalInteractions);
                else if (lifetime == "Permanent") permanent = accumulatedExistingUpgrade
                    ? AccumulateExisting(permanent, upgrade) : Add(permanent, upgrade);
                else temporary = accumulatedExistingUpgrade ? AccumulateExisting(temporary, upgrade) : Add(temporary, upgrade);
                var changed = card.WithModifiers(permanent, temporary);
                CombatContext context = state.Context.WithCard(changed);
                state = new RoomCombatState(state.RoomIndex, state.Deployment, state.Units,
                    state.ExternalInteractions, context, state.Preview, state.ActiveCompanionBossIds);
            }
            return Match(state);
        }
        internal static CardModifiers Add(CardModifiers modifiers, CardUpgradeModifier upgrade) =>
            upgrade.Unique && upgrade.DataId.Length > 0 && modifiers.Upgrades.Any(item => item.DataId == upgrade.DataId) ? modifiers :
            new CardModifiers(modifiers.Offsets, modifiers.Upgrades.Concat(new[] { upgrade }).ToArray(), modifiers.PersistentHealth, modifiers.ExternalInteractions, modifiers.BoxedCards, modifiers.MergedEquipment);
        private static CardModifiers AccumulateExisting(CardModifiers modifiers, CardUpgradeModifier upgrade)
        {
            int index = modifiers.Upgrades.ToList().FindIndex(item => item.DataId == upgrade.DataId);
            if (index < 0) return modifiers;
            var upgrades = modifiers.Upgrades.ToArray();
            upgrades[index] = upgrades[index].Accumulate(upgrade);
            return new CardModifiers(modifiers.Offsets, upgrades, modifiers.PersistentHealth, modifiers.ExternalInteractions, modifiers.BoxedCards, modifiers.MergedEquipment);
        }
        private static RoomCombatResult Match(RoomCombatState state) => new RoomCombatResult(state, RoomOutcome.Exchanged, 0, new List<CombatEvent>());
        private static RoomCombatResult Unsupported(string reason) => new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), reason);
    }
}
