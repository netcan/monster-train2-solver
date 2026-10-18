using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RelicCardAttackContribution
    {
        public int CardInstanceId { get; }
        public int Amount { get; }
        public bool InDeckState { get; }
        public RelicCardAttackContribution(int cardInstanceId, int amount, bool inDeckState = true)
        { CardInstanceId = cardInstanceId; Amount = amount; InDeckState = inDeckState; }
    }

    public sealed class RelicCountRole
    {
        public string Id { get; }
        public bool Collected { get; }
        public bool BossGiven { get; }
        public bool SoulSavior { get; }
        public bool ActiveStarter { get; }
        public RelicCountRole(string id, bool collected, bool bossGiven, bool soulSavior, bool activeStarter)
        { Id = id; Collected = collected; BossGiven = bossGiven; SoulSavior = soulSavior; ActiveStarter = activeStarter; }
    }
    public sealed class RelicCardAttackModifier
    {
        public int EffectIndex { get; }
        public string CharacterName { get; }
        public string CharacterSubtypeKey { get; }
        public int AttackModifier { get; }
        public float AttackMultiplier { get; }
        public int UnmodifiedAttackMultiplier { get; }
        public string CardType { get; }
        public bool MultiplyByUpgradeCount { get; }
        public bool MultiplyByRelicCount { get; }
        public bool ExcludeChampions { get; }
        public bool ExcludeStartRelics { get; }
        public CardUpgradeMaskRule? CardFilter { get; }
        public IReadOnlyList<RelicCountRole>? CountRoles { get; }
        public IReadOnlyList<RelicCardAttackContribution>? AppliedCardAttacks { get; }

        public RelicCardAttackModifier(int effectIndex, string characterName, string characterSubtypeKey,
            int attackModifier, float attackMultiplier, int unmodifiedAttackMultiplier, string cardType,
            bool multiplyByUpgradeCount, bool multiplyByRelicCount, bool excludeChampions, bool excludeStartRelics,
            CardUpgradeMaskRule? cardFilter = null, IReadOnlyList<RelicCountRole>? countRoles = null,
            IReadOnlyList<RelicCardAttackContribution>? appliedCardAttacks = null)
        {
            EffectIndex = effectIndex; CharacterName = characterName; CharacterSubtypeKey = characterSubtypeKey;
            AttackModifier = attackModifier; AttackMultiplier = attackMultiplier;
            UnmodifiedAttackMultiplier = unmodifiedAttackMultiplier; CardType = cardType;
            MultiplyByUpgradeCount = multiplyByUpgradeCount; MultiplyByRelicCount = multiplyByRelicCount;
            ExcludeChampions = excludeChampions; ExcludeStartRelics = excludeStartRelics; CardFilter = cardFilter;
            CountRoles = countRoles == null ? null : Array.AsReadOnly(countRoles.ToArray());
            AppliedCardAttacks = appliedCardAttacks == null ? null : Array.AsReadOnly(appliedCardAttacks.ToArray());
        }
        internal RelicCardAttackModifier WithAppliedCardAttacks(IReadOnlyList<RelicCardAttackContribution> contributions) =>
            new RelicCardAttackModifier(EffectIndex, CharacterName, CharacterSubtypeKey, AttackModifier, AttackMultiplier,
                UnmodifiedAttackMultiplier, CardType, MultiplyByUpgradeCount, MultiplyByRelicCount, ExcludeChampions,
                ExcludeStartRelics, CardFilter, CountRoles, contributions);
    }

    internal static class RelicCardAttackModel
    {
        internal static string? Validate(CombatRelicState relic)
        {
            int[] indices = relic.EffectTypes.Select((type, index) => (type, index))
                .Where(item => item.type == RelicModel.ModifyCardAttack).Select(item => item.index).ToArray();
            if (indices.Length == 0) return relic.CardAttackModifiers?.Count > 0
                ? "Unexpected relic card-attack definitions." : null;
            if (relic.CardAttackModifiers == null || !indices.SequenceEqual(relic.CardAttackModifiers.Select(effect => effect.EffectIndex)))
                return "Missing ordered native relic card-attack definitions.";
            foreach (RelicCardAttackModifier effect in relic.CardAttackModifiers)
            {
                if (effect.AttackModifier == int.MinValue)
                    return "Relic card-attack modifier exceeds supported signed attack range.";
                if (effect.CharacterName.Length > 0)
                    return "Relic card-attack card-name filters are not modeled.";
                if (float.IsNaN(effect.AttackMultiplier) || float.IsInfinity(effect.AttackMultiplier))
                    return "Relic card-attack multiplier is not finite.";
                int activeModes = (effect.AttackModifier != 0 ? 1 : 0) +
                    (effect.AttackMultiplier > 1f ? 1 : 0) + (effect.UnmodifiedAttackMultiplier > 1 ? 1 : 0);
                // Native additive effects ignore the default multiplier of one.
                if (activeModes != 1 || effect.AttackMultiplier < 0f ||
                    effect.AttackMultiplier > 0f && effect.AttackMultiplier <= 1f &&
                    !(effect.AttackModifier != 0 && effect.AttackMultiplier == 1f) ||
                    effect.UnmodifiedAttackMultiplier < 0)
                    return "Relic card-attack modifier must select one supported additive or multiplicative mode.";
                if (effect.MultiplyByRelicCount && (effect.CountRoles == null || effect.CountRoles.Any(role => string.IsNullOrEmpty(role.Id)) ||
                    effect.CountRoles.Select(role => role.Id).Distinct().Count() != effect.CountRoles.Count))
                    return "Relic-count scaling requires captured native collection classifications.";
                if (effect.CharacterSubtypeKey.Length == 0 && effect.CardType.Length == 0)
                    return "Relic card-attack effect has no supported subtype or card-type filter.";
            }
            return null;
        }

        internal static CardInstanceState? Apply(CardInstanceState source, RelicCardAttackModifier effect,
            out bool returned, out string? error, out IReadOnlyList<RelicCardFilterResult> filters, IReadOnlyList<CombatRelicState>? owners = null)
        {
            returned = false; error = null;
            filters = Array.Empty<RelicCardFilterResult>();
            CardMaskDescriptor? descriptor = source.MaskDescriptor;
            if (descriptor == null)
            { error = "Relic card-attack modifiers require branch-owned mask metadata."; return null; }
            if (!TryGetModifiedAttack(source, effect, out int amount, out error)) return null;
            if (effect.MultiplyByUpgradeCount)
            {
                if (!RelicCardUpgradeCount.TryCount(source, out int count, out error)) return null;
                amount = unchecked(amount * count);
            }
            if (effect.MultiplyByRelicCount)
            {
                if (!TryCount(effect, owners, out int count, out error)) return null;
                amount = unchecked(amount * count);
            }
            if (amount == 0) return source;
            if (effect.CardFilter != null)
            {
                bool accepted = CardUpgradeMaskModel.FilterCard(effect.CardFilter, CardBranchMaskModel.Resolve(source));
                filters = new[] { new RelicCardFilterResult(effect.CardFilter.AssetKey, accepted) };
                if (!accepted) return source;
            }
            bool matches = effect.CharacterSubtypeKey.Length > 0
                ? descriptor.Definition.Subtypes.Contains(effect.CharacterSubtypeKey, StringComparer.Ordinal)
                : descriptor.Definition.CardType == effect.CardType;
            if (!matches) return source;
            if (effect.ExcludeChampions)
            {
                if (!descriptor.Definition.IsChampionCard.HasValue)
                { error = "Champion-excluding relic card attack requires captured champion classification."; return null; }
                if (descriptor.Definition.IsChampionCard.Value) return source;
            }
            returned = true;
            CardStatModifier offsets = source.Temporary.Offsets;
            var temporary = new CardModifiers(new CardStatModifier(unchecked(offsets.Damage + amount),
                offsets.Health, offsets.Cost, offsets.Heal, offsets.Size, offsets.XCost, offsets.EquipmentLimit,
                offsets.UpgradeSlotCount), source.Temporary.Upgrades, source.Temporary.PersistentHealth,
                source.Temporary.ExternalInteractions);
            return source.WithModifiers(source.Permanent, temporary);
        }

        internal static RoomCombatResult ApplyCardlessSpawn(RoomCombatState source, int unitId, int fromCardId,
            RelicCardAttackModifier effect)
        {
            CombatUnit? actor = source.Units.FirstOrDefault(unit => unit.Id == unitId);
            if (actor == null) return Fail("Missing cardless relic birth unit.");
            if (actor.Status("cardless") == null) return Match(source);
            if (actor.Modifiers == null) return Fail("Cardless relic attack requires retained unit modifiers.");
            if (actor.Modifiers.IsClone) return Match(source);
            if (effect.ExcludeChampions)
            {
                if (!actor.Modifiers.IsChampion.HasValue) return Fail("Champion-excluding cardless relic attack requires captured champion classification.");
                if (actor.Modifiers.IsChampion.Value) return Match(source);
            }
            if (effect.CharacterSubtypeKey.Length > 0 && !actor.Subtypes.Contains(effect.CharacterSubtypeKey, StringComparer.Ordinal))
                return Match(source);
            if (!actor.Modifiers.DamageBuffFromStatusEffects.HasValue && source.Context?.Relics?.Any(relic => relic.StatusAttackModifiers?.Count > 0) == true)
                return Fail("Cardless relic attack requires captured native status-derived attack damage.");
            int currentAttack = actor.Attack;
            int unmodifiedAttack = unchecked(actor.Modifiers.AttackDamage + actor.Modifiers.DamageBuff +
                (actor.Modifiers.DamageBuffFromStatusEffects ?? 0));
            if (actor.Status("horde") != null && actor.HordeDefinition != null)
                currentAttack = unmodifiedAttack = actor.HordeDefinition.Attack;
            int amount = GetModifiedAttack(effect, currentAttack, unmodifiedAttack);
            if (effect.MultiplyByUpgradeCount)
            {
                int count = 0;
                if (fromCardId != 0 && !RelicCardUpgradeCount.TryCount(source.Context!.FindCard(fromCardId)!, out count, out string? error))
                    return Fail(error!);
                amount = unchecked(amount * count);
            }
            if (effect.MultiplyByRelicCount)
            {
                if (!TryCount(effect, source.Context?.Relics, out int count, out string? error)) return Fail(error!);
                amount = unchecked(amount * count);
            }
            if (amount == 0) return Match(source);
            return UnitAttackModel.Apply(source, unitId, amount < 0 ? -amount : amount, debuff: amount < 0);
        }

        private static bool TryGetModifiedAttack(CardInstanceState source, RelicCardAttackModifier effect,
            out int amount, out string? error)
        {
            error = null;
            amount = effect.AttackModifier;
            if (effect.AttackModifier != 0) return true;
            CardMaskDefinition definition = source.MaskDescriptor!.Definition;
            int baseAttack = definition.UnmodifiedSpawnAttack ?? 0;
            if (definition.HasSpawnData && !definition.UnmodifiedSpawnAttack.HasValue)
            { error = "Relic attack multiplier requires captured spawn attack metadata."; return false; }
            if (effect.AttackMultiplier > 1f)
            {
                if (HasAttackOverride(source))
                { error = "Attack-multiplier relic does not model FixedAttack, Equalizer or ArmorAttack cards."; return false; }
                long current = baseAttack + source.Permanent.Offsets.Damage +
                    source.Temporary.Offsets.Damage + source.Permanent.Upgrades.Sum(item => (long)item.Stats.Damage) +
                    source.Temporary.Upgrades.Sum(item => (long)item.Stats.Damage);
                if (current < int.MinValue || current > int.MaxValue)
                { error = "Current card attack exceeds the supported signed range."; return false; }
                amount = unchecked((int)Math.Floor((effect.AttackMultiplier - 1f) * current));
            }
            else if (effect.UnmodifiedAttackMultiplier > 1)
            {
                amount = unchecked((effect.UnmodifiedAttackMultiplier - 1) * baseAttack);
            }
            return true;
        }

        private static int GetModifiedAttack(RelicCardAttackModifier effect, int currentAttack, int unmodifiedAttack)
        {
            if (effect.AttackModifier != 0) return effect.AttackModifier;
            if (effect.AttackMultiplier > 1f)
                return unchecked((int)Math.Floor((effect.AttackMultiplier - 1f) * currentAttack));
            if (effect.UnmodifiedAttackMultiplier > 1)
                return unchecked((effect.UnmodifiedAttackMultiplier - 1) * unmodifiedAttack);
            return 0;
        }

        private static bool HasAttackOverride(CardInstanceState source)
        {
            CardMaskDefinition definition = source.MaskDescriptor!.Definition;
            bool Special(IEnumerable<UpgradeMaskStatus> statuses) => statuses.Any(status =>
                status.Id is "fixedattack" or "equalizer" or "armorattack");
            return Special(definition.StartingStatuses) || definition.MainEffectStatuses.Any(Special) ||
                source.Permanent.Upgrades.Concat(source.Temporary.Upgrades).Any(upgrade =>
                    upgrade.Statuses.Any(status => status.Id is "fixedattack" or "equalizer" or "armorattack"));
        }

        private static bool TryCount(RelicCardAttackModifier effect, IReadOnlyList<CombatRelicState>? owners, out int count, out string? error)
        {
            count = 0; error = null;
            if (owners == null || effect.CountRoles == null) { error = "Missing native collection roles for branch relic count."; return false; }
            foreach (var owner in owners)
            {
                var role = effect.CountRoles.SingleOrDefault(item => item.Id == owner.DataId);
                if (role == null) { error = "New relic ownership requires its native collection classification."; return false; }
                if (role.Collected && !role.BossGiven && !role.SoulSavior && !(effect.ExcludeStartRelics && role.ActiveStarter)) count++;
            }
            return true;
        }

        private static RoomCombatResult Match(RoomCombatState state) =>
            new RoomCombatResult(state, RoomOutcome.Exchanged, 0, new List<CombatEvent>());
        private static RoomCombatResult Fail(string error) =>
            new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), error);
    }
}
