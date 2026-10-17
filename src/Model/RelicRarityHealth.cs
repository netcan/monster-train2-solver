using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class RelicRarityHealthModel
    {
        internal static bool Applies(CombatUnit unit, int fromCardId, RelicCardHealthModifier effect)
        {
            if (unit.Team != CombatTeam.Player || unit.Health <= 0 || fromCardId < 0) return false;
            return effect.EffectType == RelicModel.ModifyCharacterMaxHealthPerRarityType
                ? fromCardId == 0
                : effect.EffectType == RelicModel.ModifyCharacterMaxHealthPerRarityTypeTrigger &&
                    (fromCardId == 0 || unit.Status("cardless") != null);
        }

        internal static bool TryApply(RoomCombatState source, int unitId, int fromCardId,
            RelicCardHealthModifier effect, out RoomCombatState state, out string? error)
        {
            state = source; error = null;
            CombatContext? context = source.Context;
            CombatUnit? unit = source.Units.FirstOrDefault(item => item.Id == unitId);
            if (context == null || unit == null || !Applies(unit, fromCardId, effect))
            { error = "Triboulet birth callback lost its captured unit or context."; return false; }
            if (context.Statistics?.DeckCards == null)
            { error = "Triboulet requires captured permanent deck membership."; return false; }
            int rareCount = 0;
            foreach (int cardId in context.Statistics.DeckCards)
            {
                CardInstanceState? card = context.FindCard(cardId);
                if (card?.MaskDescriptor == null)
                { error = "Triboulet's rarity count requires metadata for every deck card."; return false; }
                if (card.MaskDescriptor.Definition.Rarity == effect.CardRarityType) rareCount++;
            }
            int amount = Math.Max(unchecked(effect.HealthModifier * rareCount), 0);
            if (amount == 0) return true;

            if (effect.EffectType == RelicModel.ModifyCharacterMaxHealthPerRarityTypeTrigger && fromCardId != 0)
            {
                CardInstanceState? card = context.FindCard(fromCardId);
                if (card == null)
                { error = "Triboulet cardless spawn lost its source-card state."; return false; }
                CardModifiers temporary = card.Temporary;
                CardStatModifier offsets = temporary.Offsets;
                var changed = new CardModifiers(new CardStatModifier(offsets.Damage, unchecked(offsets.Health + amount),
                    offsets.Cost, offsets.Heal, offsets.Size, offsets.XCost, offsets.EquipmentLimit,
                    offsets.UpgradeSlotCount), temporary.Upgrades, temporary.PersistentHealth, temporary.ExternalInteractions);
                context = context.WithCard(card.WithModifiers(card.Permanent, changed));
            }

            CombatUnit changedUnit = unit.WithHealthAndMaxHealth(unchecked(unit.Health + amount),
                unchecked(unit.MaxHealth + amount));
            state = new RoomCombatState(source.RoomIndex, source.Deployment,
                source.Units.Select(item => item.Id == unitId ? changedUnit : item).ToArray(),
                source.ExternalInteractions, context, source.Preview, source.ActiveCompanionBossIds);
            return true;
        }
    }
}
