using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class RelicEndCombatModel
    {
        internal const string CavendishId = "24c4ad26-f2b3-4a95-bddc-caab94bd4767";
        internal const string CavendishKey = "BalatroEventCavendish";
        private const int CavendishDestroyChance = 1;

        internal static string? Validate(CombatRelicState relic)
        {
            bool hasDestroy = relic.EffectTypes.Contains(RelicModel.ChanceToDestroySelf);
            bool hasAttempt = relic.EffectTypes.Contains(RelicModel.BalatroBananaShake);
            if (!hasDestroy && !hasAttempt) return null;

            string[] expected = { RelicModel.ModifyCardAttack, RelicModel.ChanceToDestroySelf, RelicModel.BalatroBananaShake };
            if (relic.DataId != CavendishId || relic.AssetKey != CavendishKey || !relic.EffectTypes.SequenceEqual(expected))
                return "End-of-combat relic effects are only modeled for the unchanged BalatroEventCavendish owner.";
            if (relic.CardAttackModifiers?.Count != 1 || relic.CardAttackModifiers[0].EffectIndex != 0 ||
                relic.CardAttackModifiers[0].AttackModifier != 0 || relic.CardAttackModifiers[0].AttackMultiplier != 0f ||
                relic.CardAttackModifiers[0].UnmodifiedAttackMultiplier != 3 || relic.CardAttackModifiers[0].CardType != "Monster" ||
                relic.CardAttackModifiers[0].CharacterName.Length != 0 || relic.CardAttackModifiers[0].CharacterSubtypeKey.Length != 0 ||
                relic.CardAttackModifiers[0].MultiplyByUpgradeCount || relic.CardAttackModifiers[0].MultiplyByRelicCount ||
                !relic.CardAttackModifiers[0].ExcludeChampions || relic.CardAttackModifiers[0].CardFilter != null ||
                relic.CardAttackModifiers[0].AppliedCardAttacks == null)
                return "BalatroEventCavendish end-of-combat undo requires its captured native 3x-base Monster contribution list.";

            RelicCardAttackContribution[] contributions = relic.CardAttackModifiers[0].AppliedCardAttacks!.ToArray();
            if (contributions.Any(item => item.CardInstanceId <= 0 || item.Amount <= 0))
                return "BalatroEventCavendish has invalid native tracked card contributions.";
            return null;
        }

        internal static string? Validate(CombatContext? context)
        {
            CombatRelicState[] owners = (context?.Relics ?? Array.Empty<CombatRelicState>())
                .Where(relic => relic.EffectTypes.Contains(RelicModel.ChanceToDestroySelf) ||
                    relic.EffectTypes.Contains(RelicModel.BalatroBananaShake)).ToArray();
            if (owners.Length == 0) return null;
            if (owners.Length != 1) return "Duplicate end-of-combat relic owners are not modeled.";
            CombatRelicState owner = owners[0];
            string? error = Validate(owner);
            if (error != null) return error;
            if (context!.CardInstances == null && context.CardRegistry == null)
                return "BalatroEventCavendish end-of-combat undo requires captured owned deck cards.";
            HashSet<int> cardIds = (context.CardInstances ?? Array.Empty<CardInstanceState>())
                .Concat(context.CardRegistry ?? Array.Empty<CardInstanceState>())
                .Select(card => card.InstanceId).ToHashSet();
            if (owner.CardAttackModifiers![0].AppliedCardAttacks!.Any(item => item.InDeckState && !cardIds.Contains(item.CardInstanceId)))
                return "BalatroEventCavendish native tracked deck card is missing from the battle card state.";
            return null;
        }

        internal static CombatContext Apply(CombatContext context, out string? error,
            IReadOnlyCollection<int>? removedFromDeck = null)
        {
            error = Validate(context);
            if (error != null || context.IsolatedBattlePreview == true || context.Relics == null) return context;
            int ownerIndex = -1;
            for (int index = 0; index < context.Relics!.Count; index++)
            {
                if (context.Relics[index].DataId == CavendishId)
                { ownerIndex = index; break; }
            }
            if (ownerIndex < 0) return context;

            RngDraw draw = context.BattleRng.Range(0, 100);
            context = context.WithBattleRng(draw.State);
            if (draw.Value >= CavendishDestroyChance) return context;

            CombatRelicState owner = context.Relics![ownerIndex];
            HashSet<int> purgedCardIds = (context.OtherPiles ?? Array.Empty<CardPileState>())
                .Where(pile => pile.Name == "Purged").SelectMany(pile => pile.Cards)
                .Select(card => card.InstanceId).ToHashSet();
            if (removedFromDeck != null) purgedCardIds.UnionWith(removedFromDeck);
            var contributions = owner.CardAttackModifiers![0].AppliedCardAttacks!
                .Where(item => item.InDeckState && !purgedCardIds.Contains(item.CardInstanceId))
                .GroupBy(item => item.CardInstanceId)
                .ToDictionary(group => group.Key, group => group.Aggregate(0,
                    (total, item) => unchecked(total + item.Amount)));
            CardInstanceState[] Undo(IEnumerable<CardInstanceState>? source) => source?.Select(card =>
            {
                if (!contributions.TryGetValue(card.InstanceId, out int amount)) return card;
                CardStatModifier offsets = card.Temporary.Offsets;
                return card.WithModifiers(card.Permanent, new CardModifiers(new CardStatModifier(
                    unchecked(offsets.Damage - amount), offsets.Health, offsets.Cost, offsets.Heal, offsets.Size,
                    offsets.XCost, offsets.EquipmentLimit, offsets.UpgradeSlotCount), card.Temporary.Upgrades,
                    card.Temporary.PersistentHealth, card.Temporary.ExternalInteractions));
            }).ToArray()!;
            context = context.WithCardInstances(Undo(context.CardInstances));
            context = context.WithCardRegistry(Undo(context.CardRegistry));
            return context.WithRelics(context.Relics.Where((_, index) => index != ownerIndex).ToArray());
        }
    }
}
