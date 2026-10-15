using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
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

        public RelicCardAttackModifier(int effectIndex, string characterName, string characterSubtypeKey,
            int attackModifier, float attackMultiplier, int unmodifiedAttackMultiplier, string cardType,
            bool multiplyByUpgradeCount, bool multiplyByRelicCount, bool excludeChampions, bool excludeStartRelics,
            CardUpgradeMaskRule? cardFilter = null)
        {
            EffectIndex = effectIndex; CharacterName = characterName; CharacterSubtypeKey = characterSubtypeKey;
            AttackModifier = attackModifier; AttackMultiplier = attackMultiplier;
            UnmodifiedAttackMultiplier = unmodifiedAttackMultiplier; CardType = cardType;
            MultiplyByUpgradeCount = multiplyByUpgradeCount; MultiplyByRelicCount = multiplyByRelicCount;
            ExcludeChampions = excludeChampions; ExcludeStartRelics = excludeStartRelics; CardFilter = cardFilter;
        }
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
                if (effect.AttackModifier == 0 || effect.AttackMultiplier != 0 || effect.UnmodifiedAttackMultiplier != 0 ||
                    effect.MultiplyByRelicCount || effect.ExcludeChampions ||
                    effect.ExcludeStartRelics || effect.CardFilter != null)
                    return "Relic card-attack multipliers, scaling and advanced filters are not modeled.";
                if (effect.CharacterSubtypeKey.Length == 0 && effect.CardType.Length == 0)
                    return "Relic card-attack effect has no supported subtype or card-type filter.";
            }
            return null;
        }

        internal static CardInstanceState? Apply(CardInstanceState source, RelicCardAttackModifier effect,
            out bool returned, out string? error)
        {
            returned = false; error = null;
            CardMaskDescriptor? descriptor = source.MaskDescriptor;
            if (descriptor == null)
            { error = "Relic card-attack modifiers require branch-owned mask metadata."; return null; }
            int amount = effect.AttackModifier;
            if (effect.MultiplyByUpgradeCount)
            {
                if (!RelicCardUpgradeCount.TryCount(source, out int count, out error)) return null;
                amount = unchecked(amount * count);
            }
            if (amount == 0) return source;
            bool matches = effect.CharacterSubtypeKey.Length > 0
                ? descriptor.Definition.Subtypes.Contains(effect.CharacterSubtypeKey, StringComparer.Ordinal)
                : descriptor.Definition.CardType == effect.CardType;
            if (!matches) return source;
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
            if (effect.CharacterSubtypeKey.Length > 0 && !actor.Subtypes.Contains(effect.CharacterSubtypeKey, StringComparer.Ordinal))
                return Match(source);
            int amount = effect.AttackModifier;
            if (effect.MultiplyByUpgradeCount)
            {
                int count = 0;
                if (fromCardId != 0 && !RelicCardUpgradeCount.TryCount(source.Context!.FindCard(fromCardId)!, out count, out string? error))
                    return Fail(error!);
                amount = unchecked(amount * count);
            }
            return UnitAttackModel.Apply(source, unitId, amount < 0 ? -amount : amount, debuff: amount < 0);
        }

        private static RoomCombatResult Match(RoomCombatState state) =>
            new RoomCombatResult(state, RoomOutcome.Exchanged, 0, new List<CombatEvent>());
        private static RoomCombatResult Fail(string error) =>
            new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), error);
    }
}
