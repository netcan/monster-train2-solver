using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class PreviewBirthModel
    {
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
                Array.Empty<CardUpgradeModifier>(), Array.Empty<StatisticCount>(), metadata.SpawnerMatchesDefinition);
            var death = raw.DeathState == null ? new UnitDeathState(false, false, false) : new UnitDeathState(false, false, false,
                isSacrifice: raw.DeathState.IsSacrifice.HasValue ? false : (bool?)null,
                statisticsListenerOnce: raw.DeathState.StatisticsListenerOnce.HasValue ? false : (bool?)null,
                isDespawned: raw.DeathState.IsDespawned.HasValue ? false : (bool?)null,
                isDestroyed: raw.DeathState.IsDestroyed.HasValue ? false : (bool?)null);
            return new CombatUnit(unitId, raw.AssetKey, CombatTeam.Player, raw.BaseAttack, raw.MaxHealth, raw.MaxHealth,
                raw.CanAttack, false, false, Array.Empty<CombatStatus>(), resolved.Triggers, sourceCardId,
                Math.Max(1, Math.Min(6, metadata?.RawSize ?? raw.Size)), Array.Empty<string>(), raw.Subtypes,
                modifiers, raw.IsBoss, raw.LastAttackerId.HasValue ? 0 : (int?)null,
                raw.StatusRegistry == null ? null : Array.Empty<CombatStatus>(),
                raw.EquipmentCards == null ? null : Array.Empty<int>(), resolved.NextTriggerId, resolved.Ability,
                raw.StatusDictionary == null ? null : new StatusDictionaryState(Array.Empty<string?>(), Array.Empty<int>()),
                raw.AbilityRules, raw.HordeDefinition, false, raw.SacrificeCardId.HasValue ? 0 : (int?)null, death,
                bumpRules: raw.BumpRules);
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
                restored.DeathState!.WithLifecycle(restored.DeathState.IsDespawned, true), bumpRules: restored.BumpRules);
        }

        internal static EnchantmentRetainedUnit[] Retain(EnchantmentWorld observed, int firstPreviewId, IReadOnlyCollection<int> removedIds)
            => observed.Rooms.SelectMany(room => room.Units).Concat(observed.RetainedUnits.Select(actor => actor.Unit))
                .Where(unit => unit.Id >= firstPreviewId).Select(unit => new EnchantmentRetainedUnit(Restore(unit, removedIds), -1, false)).ToArray();

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
