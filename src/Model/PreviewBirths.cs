using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class PreviewBirthModel
    {
        internal static EnchantmentRetainedUnit CancelPlayer(EnchantmentRetainedUnit actor)
        {
            if (!actor.Preview || actor.Unit.Team != CombatTeam.Player || actor.Unit.DeathState?.IsDestroyed == true) return actor;
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
            int firstUnitId, int firstCardId, out int[] forgotten)
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
                // Infectory's room-death fallback creates an unowned character,
                // not a spawner card. Like an unobserved egg hatch, its preview
                // character is gone before native decision identity capture.
                // The calibrated player snapshot has no triggered-summon
                // observation catalog; controlled catalog captures observe
                // these temporary actors and retain their assigned identities.
                // Retain every other cardless birth until separately calibrated.
                if (context.SummonCatalog != null) return preview;
                CombatUnit[] births = world.Rooms.SelectMany(room => room.Units)
                    .Concat(world.RetainedUnits.Select(actor => actor.Unit)).Where(unit => unit.Id >= firstUnitId).ToArray();
                bool roomFallback = context.RoomCapacities?.SelectMany(room => room.Attachments ??
                    Array.Empty<RoomCapacityAttachmentState>()).Any(attachment =>
                        attachment.DataId == "e1a39d88-4a77-4399-98b2-0b81f44fe6a8" &&
                        attachment.DeathEffect != null && RoomDeathEffectModel.Validate(attachment.DeathEffect) == null) == true;
                if (!roomFallback || births.Length != nextId - firstUnitId || births.Any(unit =>
                    unit.Team != CombatTeam.Player || unit.AssetKey != "MonsterFunguy" || unit.SpawnerCardId != 0)) return preview;
            }
            else if (context.SummonCatalog == null || nextId - firstUnitId != newCards.Length) return preview;
            if (world.Rooms.SelectMany(room => room.Units).Concat(world.RetainedUnits.Select(actor => actor.Unit))
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

        internal static CombatUnit Restore(CombatUnit preview, IReadOnlyCollection<int> removedIds, bool afterFrame = false,
            IReadOnlyCollection<int>? resetPreviewActionsForUnitIds = null)
        {
            CombatUnit primary = preview.DeathState?.PreviewPrimary ?? throw new ArgumentException("A preview-born actor needs its initial primary state.");
            CombatUnit restored = PreviewEffectsModel.Restore(primary, preview, removedIds,
                resetPreviewActionsForUnitIds: resetPreviewActionsForUnitIds);
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

        internal static EnchantmentRetainedUnit[] Retain(EnchantmentWorld observed, int firstPreviewId, IReadOnlyCollection<int> removedIds,
            IReadOnlyCollection<int>? resetPreviewActionsForUnitIds = null)
            => observed.Rooms.SelectMany(room => room.Units).Concat(observed.RetainedUnits.Select(actor => actor.Unit))
                .Where(unit => unit.Id >= firstPreviewId).Select(unit => new EnchantmentRetainedUnit(
                    Restore(unit, removedIds, resetPreviewActionsForUnitIds: resetPreviewActionsForUnitIds), -1, false)).ToArray();

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
