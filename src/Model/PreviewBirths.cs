using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class PreviewBirthModel
    {
        private static bool IsOriginalGreenRecruit(CombatUnit unit, CombatContext context) =>
            unit is { AssetKey: "MonsterGenerousHost", Team: CombatTeam.Player } &&
            context.FindCard(unit.SpawnerCardId)?.DataId == "c052c567-1e3c-4f36-a6cd-89b0648b9475" &&
            unit.Triggers is [{ Kind: "OnDeath", Effects: [{ Type: "CardEffectSpawnMonsterOrAddStatusEffect", Action:
                { Type: "SpawnOrStatus", Target: "Room", Value: 4, AllowPlayer: true, AllowEnemy: false, Range: null,
                    Statuses: [{ Id: "horde", Stacks: 4 }], Filters.Subtype: "SubtypesData_Horde",
                    SpawnFallbackUnit: { AssetKey: "MonsterFunguy", BaseAttack: 3, Health: 4 } } }] }];

        private static bool IsOriginalChampionEnemyDeath(CombatUnit unit, CombatContext context) =>
            unit is { AssetKey: "MonsterChampionUnderlegion1", Team: CombatTeam.Player } &&
            context.FindCard(unit.SpawnerCardId)?.DataId == "0422f6ca-896c-4676-a01f-71d8d4b232fb" &&
            unit.Triggers is [{ Kind: "OnAnyHeroDeathOnFloor", Effects: [{ Type: "CardEffectSpawnMonsterOrAddStatusEffect", Action:
                { Type: "SpawnOrStatus", Target: "Room", Value: 2, AllowPlayer: true, AllowEnemy: false, Range: null,
                    Statuses: [{ Id: "horde", Stacks: 2 }], Filters.Subtype: "SubtypesData_Horde",
                    SpawnFallbackUnit: { AssetKey: "MonsterFunguy", BaseAttack: 3, Health: 4, Modifiers.Upgrades.Count: 0 } } }] }];

        private static bool IsOriginalChampionAuraPreview(EnchantmentRetainedUnit actor, EnchantmentWorld world, CombatContext context) =>
            world.EnchanterIds.Count == 0 && actor.Unit is { Health: 12, MaxHealth: 12, Triggers.Count: 0,
                HordeDefinition: { Attack: 3, Health: 4 }, Modifiers.Upgrades.Count: 4 } &&
            actor.Unit.Status("horde")?.Stacks == 2 && actor.Unit.Status("trample") == null &&
            world.Rooms.Where(room => room.RoomIndex == actor.RoomIndex).SelectMany(room => room.Units).Any(owner =>
                owner.Triggers is [_, { Kind: "OnHit", Effects: [{ Type: "CardEffectAddStatusEffect", Action:
                    { Type: "AddStatus", Target: "LastAttackerCharacter", Range: null,
                        AllowEnemy: true, AllowPlayer: true, Statuses: [{ Id: "pyregel", Stacks: 3 }] } }] }] &&
                IsOriginalChampionEnemyDeath(owner.WithTriggers(new[] { owner.Triggers[0] }), context) &&
                owner.Health > 0 && owner.Modifiers?.TroopAura is
                    { Subtype: "SubtypesData_None", Statuses.Count: 0, Upgrade: { DataId: "9cb768b3-efb9-480c-819b-33882cf0908d",
                        AssetKey: "Underlegion1RegenOnHarvest1_Upgrade", Stats.Health: 1 } } aura &&
                !aura.Targets.Contains(actor.Unit.Id) && actor.Unit.Modifiers.Upgrades.All(upgrade =>
                    upgrade.DataId == TroopAuraModel.Key(owner.Id) && upgrade.AssetKey == aura.Upgrade.AssetKey &&
                    upgrade.Stats.Health == 1 && upgrade.Stats.Damage == 0 && upgrade.Stats.Cost == 0 &&
                    upgrade.Stats.Heal == 0 && upgrade.Stats.Size == 0 && upgrade.Stats.XCost == 0 &&
                    upgrade.Stats.EquipmentLimit == 0 && upgrade.Stats.UpgradeSlotCount == 0 && upgrade.Statuses.Count == 0 &&
                    upgrade.ExternalInteractions.Count == 0 && upgrade.TriggerUpgrades?.Count is not > 0 && upgrade.AbilityUpgrade == null));

        internal static int[] UnobservedRoomPlayers(RoomCombatState source, CombatContext preview,
            IEnumerable<CombatUnit> observed)
        {
            CombatContext context = source.Context!;
            // Room-only captures have no actor world to retain temporary objects.
            // The calibrated kill preview creates no card and no durable observer.
            if (context.Enchantments != null || preview.Enchantments != null || context.SummonCatalog != null ||
                context.NextUnitId is not int first || preview.NextUnitId is not int next || next <= first ||
                context.NextCardId != preview.NextCardId ||
                preview.CardRegistry?.Any(card => card.InstanceId >= context.NextCardId) == true ||
                !source.Units.Any(unit => IsOriginalGreenRecruit(unit, context))) return Array.Empty<int>();
            CombatUnit[] births = observed.Where(unit => unit.Id >= first).ToArray();
            if (births.Length != next - first || births.Select(unit => unit.Id).Distinct().Count() != births.Length || births.Any(unit => unit.Id >= next ||
                unit.Team != CombatTeam.Player || unit.AssetKey != "MonsterFunguy" || unit.SpawnerCardId != 0 ||
                unit.Triggers.SelectMany(trigger => trigger.Effects).Any(effect => effect.Enchantment != null)))
                return Array.Empty<int>();
            return births.Select(unit => unit.Id).ToArray();
        }

        internal static CombatContext CompleteRemoteCombatBirths(CombatContext context, int roomIndex)
        {
            if (context.Enchantments is not { AutomaticLifecycle: true, Preview: false } world ||
                context.SpawnPoints is not BattleSpawnPoints points) return context;
            // A prior floor's temporary boss-kill preview can leave an unowned
            // Fun Guy in its copied group. The next real combat yields a frame
            // before damage, completing that pending player object destruction.
            bool removedTroopOwner = world.RetainedUnits.Any(actor => actor.Unit is
                { AssetKey: "MonsterSquadLeader", Health: <= 0, DeathState.IsDestroyed: true, Modifiers.TroopAura: not null } &&
                actor.Unit.Modifiers.Upgrades is [{ AssetKey: "InfectoryEffect_Upgrade" }]);
            var pending = world.RetainedUnits.Where(actor => actor.Preview && actor.RoomIndex != roomIndex &&
                actor.Unit is { Team: CombatTeam.Player, AssetKey: "MonsterFunguy", SpawnerCardId: 0,
                    Health: > 0, DeathState.IsDestroyed: false } &&
                (actor.Unit.Status("horde")?.Stacks == 1 &&
                (actor.Unit.Status("trample")?.Stacks == 2 && actor.Unit.Modifiers?.Upgrades.Count == 3 &&
                    actor.Unit.Modifiers.Upgrades.Count(upgrade => upgrade.AssetKey == "SquadLeader_Upgrade") == 2 &&
                    actor.Unit.Modifiers.Upgrades.Count(upgrade => upgrade.AssetKey == "InfectoryEffect_Upgrade") == 1 ||
                    removedTroopOwner && actor.Unit.Status("trample") == null &&
                        actor.Unit.Modifiers?.Upgrades is [{ AssetKey: "InfectoryEffect_Upgrade" }]) ||
                // The original Green Recruit death creates four unupgraded
                // troop stacks. Its preview object survives that floor's raw
                // capture, then retires during the next lower floor's real
                // combat yield, just like the calibrated one-stack births.
                actor.RoomIndex > roomIndex && actor.Unit.Status("horde")?.Stacks == 4 &&
                    actor.Unit.Status("trample") == null && actor.Unit.Triggers.Count == 0 &&
                    actor.Unit.Modifiers?.Upgrades.Count == 0 &&
                    actor.Unit.HordeDefinition is { Attack: 3, Health: 4 } &&
                    world.RetainedUnits.Any(owner => owner.Unit.Health <= 0 && IsOriginalGreenRecruit(owner.Unit, context) &&
                        (owner.RoomIndex == actor.RoomIndex || points.Units.Any(point => point.UnitId == owner.Unit.Id &&
                            point.LastKnown?.RoomIndex == actor.RoomIndex))) ||
                actor.RoomIndex > roomIndex && IsOriginalChampionAuraPreview(actor, world, context)) &&
                points.Units.Any(point => point.UnitId == actor.Unit.Id && point.SpawnedInPreview &&
                    point.Current?.PreviewCopyId != null)).ToArray();
            if (pending.Length == 0) return context;
            var ids = pending.Select(actor => actor.Unit.Id).ToHashSet();
            bool troopOwnerRemoval = removedTroopOwner && pending.Any(actor => actor.Unit.Modifiers!.Upgrades.Count == 1);
            var retained = world.RetainedUnits.Select(actor => ids.Contains(actor.Unit.Id) ? CancelPlayer(actor) :
                // Native waits settle the observed Sporelauncher death, or the
                // Steward/Fun Guy deaths preceding the retired troop owner.
                // That owner's own raw collection survives until the next frame.
                actor.Unit.Team == CombatTeam.Player &&
                    actor.Unit.DeathState?.IsDestroyed == true && actor.Unit.Health <= 0 &&
                    (actor.Unit.AssetKey == "MonsterSporelauncher" && actor.Unit.SpawnerCardId > 0 &&
                        actor.Unit.Modifiers?.Upgrades is [{ AssetKey: "InfectoryEffect_Upgrade" }] ||
                    troopOwnerRemoval && actor.Unit.AssetKey is "TrainStewardSmall" or "MonsterFunguy" &&
                        actor.Unit.Modifiers?.Upgrades.Any(upgrade => upgrade.AssetKey == "InfectoryEffect_Upgrade") == true &&
                        actor.Unit.Modifiers.Upgrades.All(upgrade => upgrade.AssetKey is "InfectoryEffect_Upgrade" or "SquadLeader_Upgrade"))
                    ? new EnchantmentRetainedUnit(actor.Unit.AfterDestruction().WithoutRemovedAttacker(new HashSet<int>()),
                        actor.RoomIndex, actor.Preview) : actor).ToArray();
            return context.WithSpawnPoints(points.ClearDestroyedPreviewBirths(ids)).WithEnchantments(new EnchantmentWorld(
                world.Rooms, world.Movement, world.EnemySlotsPerRoom, retained, world.EnchanterIds, world.AllowUpdates,
                world.Updating, world.Preview, world.TestRng, world.AutomaticLifecycle, world.TemporaryPreviewUnitIds));
        }

        internal static EnchantmentRetainedUnit CancelPlayer(EnchantmentRetainedUnit actor)
        {
            if (!actor.Preview || actor.Unit.Team != CombatTeam.Player || actor.Unit.DeathState is
                { IsDestroyed: true, HasStatisticsListener: false } && actor.Unit.Modifiers?.Upgrades.Count == 0) return actor;
            // MonsterManager removes preview-born players when the full UI
            // preview is reset. HideUnit/ProcessRemovals do not fire death
            // triggers or restore numeric state; OnDestroy clears collections
            // and statistics listeners on the retained managed object.
            CombatUnit unit = actor.Unit.AfterDestruction();
            UnitDeathState death = unit.DeathState ?? new UnitDeathState(false, false, false);
            unit = unit.WithDeathState(new UnitDeathState(death.HasFinishedDying, death.IsBeingRemoved, false,
                null, death.IsSacrifice, death.StatisticsListenerOnce.HasValue ? false : (bool?)null,
                death.IsDespawned, true));
            return new EnchantmentRetainedUnit(unit, -1, actor.Preview);
        }

        internal static CombatContext ForgetUnobservedPlayers(TrainCombatState source, CombatContext preview,
            int firstUnitId, int firstCardId, IReadOnlyCollection<CombatUnit> observedBirths, out int[] forgotten)
        {
            forgotten = Array.Empty<int>();
            CombatContext context = source.Context!;
            EnchantmentWorld? world = preview.Enchantments;
            // Temporary player births without durable aura references are gone
            // before native decision capture assigns their synthetic identities.
            // Preserve observed births and all enemy births. Check the allocator
            // first so ordinary preview refreshes do not scan units or cards.
            if (context.Enchantments?.AutomaticLifecycle != true || preview.NextUnitId is not int nextId ||
                nextId <= firstUnitId || world == null || world.EnchanterIds.Count != 0 || preview.CardRegistry == null)
                return preview;
            CardInstanceState[] newCards = preview.CardRegistry?.Where(card => card.InstanceId >= firstCardId).ToArray()
                ?? Array.Empty<CardInstanceState>();
            if (newCards.Length == 0)
            {
                // Infectory and the original Green Recruit death fallback create an unowned character,
                // not a spawner card. Like an unobserved egg hatch, its preview
                // character is gone before native decision identity capture.
                // The calibrated player snapshot has no triggered-summon
                // observation catalog; controlled catalog captures observe
                // these temporary actors and retain their assigned identities.
                // Retain every other cardless birth until separately calibrated.
                if (context.SummonCatalog != null) return preview;
                CombatUnit[] births = world.Rooms.SelectMany(room => room.Units)
                    .Concat(world.RetainedUnits.Select(actor => actor.Unit)).Concat(observedBirths)
                    .Where(unit => unit.Id >= firstUnitId).GroupBy(unit => unit.Id).Select(group => group.First()).ToArray();
                bool roomFallback = context.RoomCapacities?.SelectMany(room => room.Attachments ??
                    Array.Empty<RoomCapacityAttachmentState>()).Any(attachment =>
                        attachment.DataId == "e1a39d88-4a77-4399-98b2-0b81f44fe6a8" &&
                        attachment.DeathEffect != null && RoomDeathEffectModel.Validate(attachment.DeathEffect) == null) == true;
                bool recruitFallback = source.Rooms.SelectMany(room => room.Units).Any(unit => IsOriginalGreenRecruit(unit, context));
                // The original two-stack champion enemy-death fallback has the
                // same unowned Fun Guy lifetime as Green Recruit. Its temporary
                // preview character is destroyed before decision identity capture.
                bool championFallback = source.Rooms.SelectMany(room => room.Units).Any(unit => IsOriginalChampionEnemyDeath(unit, context));
                if (!(roomFallback || recruitFallback || championFallback) || births.Length != nextId - firstUnitId || births.Any(unit =>
                    unit.Id >= nextId || unit.Team != CombatTeam.Player || unit.AssetKey != "MonsterFunguy" || unit.SpawnerCardId != 0)) return preview;
            }
            else if (context.SummonCatalog == null || nextId - firstUnitId != newCards.Length) return preview;
            if (world.Rooms.SelectMany(room => room.Units).Concat(world.RetainedUnits.Select(actor => actor.Unit)).Concat(observedBirths)
                .Any(unit => unit.Id >= firstUnitId && unit.Team != CombatTeam.Player ||
                    unit.Triggers.SelectMany(trigger => trigger.Effects).Any(effect => effect.Enchantment != null))) return preview;
            var spawnerIds = (context.SummonCatalog?.Units ?? Array.Empty<SummonUnitDefinition>()).Where(unit => unit.Unit.Team == CombatTeam.Player && unit.Fallback != null)
                .Select(unit => unit.Fallback!.DataId).ToHashSet();
            foreach (CombatUnit actor in source.Rooms.SelectMany(room => room.Units).Where(unit =>
                unit.Team == CombatTeam.Player && unit.Triggers.SelectMany(trigger => trigger.Effects).Any(effect => effect.Summon != null)))
                if (context.FindCard(actor.SpawnerCardId) is CardInstanceState spawner) spawnerIds.Add(spawner.DataId);
            if (newCards.Any(card => !spawnerIds.Contains(card.DataId))) return preview;
            forgotten = Enumerable.Range(firstUnitId, nextId - firstUnitId).ToArray();
            preview = preview.WithNextUnitId(firstUnitId)
                .WithCardRegistry(preview.CardRegistry!.Where(card => card.InstanceId < firstCardId).ToArray())
                .WithNextCardId(firstCardId).WithEnchantments(new EnchantmentWorld(world.Rooms.Select(room =>
                    new RoomCombatState(room.RoomIndex, room.Deployment, room.Units.Where(unit => unit.Id < firstUnitId).ToArray(),
                        room.ExternalInteractions, null, room.Preview, room.ActiveCompanionBossIds)).ToArray(), world.Movement,
                    world.EnemySlotsPerRoom, world.RetainedUnits.Where(actor => actor.Unit.Id < firstUnitId).ToArray(),
                    world.EnchanterIds, world.AllowUpdates, world.Updating, world.Preview, world.TestRng,
                    world.AutomaticLifecycle, world.TemporaryPreviewUnitIds));
            if (preview.SpawnPoints is BattleSpawnPoints points)
                preview = preview.WithSpawnPoints(points.WithState(points.Groups, points.Units.Where(unit => unit.UnitId < firstUnitId).ToArray()));
            return preview;
        }

        internal static CombatUnit InitialPrimary(CardPlayRule definition, CardInstanceState? card, int unitId,
            IReadOnlyList<string> disabledAbilities, CombatStatus abilityMarker)
        {
            var cards = new CardCycleState(Array.Empty<CardToken>(), Array.Empty<CardToken>(), Array.Empty<CardToken>(),
                new UnityRng(1, 2, 3, 4), 0, Array.Empty<string>());
            var context = new CombatContext(cards, new UnityRng(1, 2, 3, 4), 0, 1, 10,
                new[] { abilityMarker }, permanentlyDisabledAbilities: disabledAbilities);
            CombatUnit raw = definition.SpawnUnit ?? throw new ArgumentException("A preview birth needs its character definition.");
            CombatUnit initialized = AbilityLifecycleModel.InitialAtSpawn(raw, card, context, out string? error);
            if (error != null) throw new ArgumentException(error);
            CombatUnit resolved = card == null ? initialized : CardModifierModel.Resolve(definition.WithSpawn(initialized), card).SpawnUnit!;
            return InitialPrimary(raw, resolved, unitId, card?.InstanceId ?? 0);
        }

        internal static CombatUnit InitialPrimary(CombatUnit raw, CombatUnit resolved, int unitId, int sourceCardId)
        {
            UnitModifiers? metadata = raw.Modifiers;
            var modifiers = metadata == null ? null : new UnitModifiers(metadata.AttackDamage, 0, 0,
                metadata.RawSize, metadata.EquipmentLimit, metadata.CanBeHealed, false,
                Array.Empty<CardUpgradeModifier>(), Array.Empty<StatisticCount>(), metadata.SpawnerMatchesDefinition,
                metadata.DamageBuffFromStatusEffects.HasValue ? 0 : (int?)null,
                metadata.MagebladeDamagePerMagicPower.HasValue ? 0 : (int?)null, metadata.StatusAttackModifiers,
                metadata.IsChampion, metadata.SelfAttackRules, metadata.RoomResources, metadata.RoomStatuses, metadata.RoomTriggerCounts, metadata.SpawnCopySubtype, metadata.Graft, metadata.SynergizeStatuses == null ? null : Array.Empty<StatisticCount>(), metadata.Sentry == null ? null : new SentryState(Array.Empty<int>()), metadata.TroopAura?.WithTargets(Array.Empty<int>()));
            var death = raw.DeathState == null ? new UnitDeathState(false, false, false) : new UnitDeathState(false, false, false,
                isSacrifice: raw.DeathState.IsSacrifice.HasValue ? false : (bool?)null,
                statisticsListenerOnce: raw.DeathState.StatisticsListenerOnce.HasValue ? false : (bool?)null,
                isDespawned: raw.DeathState.IsDespawned.HasValue ? false : (bool?)null,
                isDestroyed: raw.DeathState.IsDestroyed.HasValue ? false : (bool?)null);
            return new CombatUnit(unitId, raw.AssetKey, raw.Team, raw.BaseAttack, raw.MaxHealth, raw.MaxHealth,
                raw.CanAttack, false, false, Array.Empty<CombatStatus>(), resolved.Triggers, sourceCardId,
                Math.Max(1, Math.Min(6, metadata?.RawSize ?? raw.Size)), Array.Empty<string>(), raw.Subtypes,
                modifiers, raw.IsBoss, raw.LastAttackerId.HasValue ? 0 : (int?)null,
                raw.StatusRegistry == null ? null : Array.Empty<CombatStatus>(),
                raw.EquipmentCards == null ? null : Array.Empty<int>(), resolved.NextTriggerId, resolved.Ability,
                raw.StatusDictionary == null ? null : new StatusDictionaryState(Array.Empty<string?>(), Array.Empty<int>()),
                raw.AbilityRules, raw.HordeDefinition, false, raw.SacrificeCardId.HasValue ? 0 : (int?)null, death,
                bumpRules: raw.BumpRules, lastFeederId: raw.LastFeederId, moonPhaseMask: raw.MoonPhaseMask);
        }

        internal static CombatUnit Restore(CombatUnit preview, IReadOnlyCollection<int> removedIds, bool afterFrame = false)
        {
            CombatUnit primary = preview.DeathState?.PreviewPrimary ?? throw new ArgumentException("A preview-born actor needs its initial primary state.");
            CombatUnit restored = PreviewEffectsModel.Restore(primary, preview, removedIds);
            var triggers = restored.Triggers.Select(trigger =>
            {
                CombatTrigger observed = preview.Triggers.Single(item => item.StateId == trigger.StateId);
                return trigger.WithEffects(trigger.Effects.Select((effect, index) =>
                {
                    EnchantmentRule? shared = observed.Effects[index].Enchantment;
                    return effect.Enchantment == null || shared == null ? effect : effect.WithEnchantment(new EnchantmentRule(
                        effect.Enchantment.Targeting, effect.Enchantment.StatusPool, effect.Enchantment.State,
                        !afterFrame && shared.Bound, shared.HasParentCard));
                }).ToArray());
            }).ToArray();
            return new CombatUnit(restored.Id, restored.AssetKey, restored.Team, restored.BaseAttack, restored.Health,
                restored.MaxHealth, restored.CanAttack, restored.IsPyre, restored.EndsBattleOnDeath, restored.Statuses,
                triggers, restored.SpawnerCardId, restored.Size, preview.StatusImmunities, restored.Subtypes,
                restored.Modifiers, restored.IsBoss, restored.LastAttackerId, restored.StatusRegistry, restored.EquipmentCards,
                restored.NextTriggerId, restored.Ability, restored.StatusDictionary, restored.AbilityRules,
                restored.HordeDefinition, restored.IsSpawning, restored.SacrificeCardId,
                restored.DeathState!.WithLifecycle(restored.DeathState.IsDespawned,
                    restored.Team == CombatTeam.Player ? true : restored.DeathState.IsDestroyed),
                bumpRules: restored.BumpRules, lastFeederId: restored.LastFeederId, moonPhaseMask: restored.MoonPhaseMask);
        }

        internal static EnchantmentRetainedUnit[] Retain(EnchantmentWorld observed, int firstPreviewId, IReadOnlyCollection<int> removedIds)
            => observed.Rooms.SelectMany(room => room.Units).Concat(observed.RetainedUnits.Select(actor => actor.Unit))
                .Where(unit => unit.Id >= firstPreviewId).Select(unit => new EnchantmentRetainedUnit(
                    Restore(unit, removedIds), -1, false)).ToArray();

        internal static CardInstanceState[] RestoreSourceCards(CombatContext preview, int firstCardId, ISet<int> restoredIds)
            => (preview.CardRegistry ?? Array.Empty<CardInstanceState>()).Where(card => card.InstanceId >= firstCardId).Select(card =>
            {
                IReadOnlyList<int>? cached = card.RawPlayedRoomUnitIds ?? card.PlayedRoomUnitIds;
                if (card.PlayedRoomUnitIds == null || cached == null) return card;
                int[] restored = cached.Where(restoredIds.Contains).ToArray();
                return card.WithRoomCacheState(restored, card.RawPlayedRoomUnitIds == null ? null : restored);
            }).ToArray();
    }
}
