using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class DeckSpellUpgradeResult
    {
        public CombatContext? Context { get; }
        public IReadOnlyList<int> AppliedCardIds { get; }
        public string? UnsupportedReason { get; }
        public bool Supported => Context != null;
        internal DeckSpellUpgradeResult(CombatContext? context, IReadOnlyList<int> appliedCardIds, string? error = null)
        { Context = context; AppliedCardIds = Array.AsReadOnly(appliedCardIds.ToArray()); UnsupportedReason = error; }
    }

    public static class DeckSpellUpgradeModel
    {
        private static readonly string[] NativeOtherPileOrder = ["Exhausted", "Eaten", "Purged", "Standby", "DiscardBuffer"];

        public static DeckSpellUpgradeResult Apply(CombatContext source, CardActionEffect effect, BattlePlayRules? definitions)
        {
            if (effect.Type != "UpgradeDeckSpells" || effect.Upgrade == null)
                return Unsupported("Missing deck spell upgrade definition.");
            if (effect.Lifetime != "TemporaryUntilEndOfBattle")
                return Unsupported("Deck spell upgrades require the temporary battle lifetime.");
            if (source.CardInstances == null || source.CardRegistry == null || source.OtherPiles == null)
                return Unsupported("Deck spell upgrades require complete card definitions, instances and pile membership.");
            if (effect.Upgrade.ExternalInteractions.Count > 0)
                return Unsupported(string.Join("; ", effect.Upgrade.ExternalInteractions));

            var allCards = new List<CardToken>();
            allCards.AddRange(source.Cards.Hand);
            allCards.AddRange(source.Cards.Discard);
            allCards.AddRange(source.Cards.Draw);
            int PileOrder(CardPileState pile)
            {
                string name = pile.Name.Replace("Pile", "", StringComparison.Ordinal);
                int index = Array.IndexOf(NativeOtherPileOrder, name);
                return index < 0 ? NativeOtherPileOrder.Length : index;
            }
            foreach (CardPileState pile in source.OtherPiles.OrderBy(PileOrder)) allCards.AddRange(pile.Cards);
            allCards = allCards.GroupBy(card => card.InstanceId).Select(group => group.First()).ToList();

            CombatContext context = source;
            var applied = new List<int>();
            foreach (CardToken token in allCards)
            {
                CardInstanceState? card = context.FindCard(token.InstanceId);
                if (card == null || card.DataId != token.DataId)
                    return Unsupported("Deck spell upgrade is missing a matching card instance.");
                CardPlayRule? rule = definitions?.Cards.FirstOrDefault(item => item.DataId == token.DataId);
                string? cardType = rule?.CardType ?? (definitions == null ? card.MaskDescriptor?.Definition.CardType : null);
                if (cardType == null) return Unsupported("Deck spell upgrade is missing a card definition for " + token.DataId + ".");
                if (cardType != "Spell") continue;
                if (card.ExternalInteractions.Count > 0)
                    return Unsupported(string.Join("; ", card.ExternalInteractions));
                IReadOnlyList<string> upgradeInteractions = rule?.UpgradeInteractions ?? rule?.ExternalInteractions ?? Array.Empty<string>();
                if (upgradeInteractions.Count > 0)
                    return Unsupported(string.Join("; ", upgradeInteractions));

                if (effect.CardFilter != null || effect.UpgradeFilters.Count > 0)
                {
                    if (card.MaskDescriptor == null)
                        return Unsupported("Deck spell upgrade filters require branch-owned card metadata.");
                    CardUpgradeMaskCard view;
                    try { view = CardBranchMaskModel.Resolve(card); }
                    catch (InvalidOperationException error) { return Unsupported(error.Message); }
                    if (effect.CardFilter != null && !CardUpgradeMaskModel.FilterCard(effect.CardFilter, view)) continue;
                    bool accepted = true;
                    foreach (CardUpgradeMaskRule filter in effect.UpgradeFilters)
                    {
                        if (!CardUpgradeMaskModel.FilterCard(filter, view)) { accepted = false; break; }
                    }
                    if (!accepted) continue;
                }

                string? modifierError = CardModifierModel.UnsupportedReason(card);
                if (modifierError != null) return Unsupported(modifierError);
                CardUpgradeModifier upgrade = effect.Upgrade.WithSharedReference(null);
                CardModifiers temporary = UnitModifierModel.Add(card.Temporary, upgrade);
                context = CardMagicPowerModel.Refresh(context, card.WithModifiers(card.Permanent, temporary), out _);
                applied.Add(card.InstanceId);
            }
            return new DeckSpellUpgradeResult(context, applied);
        }

        private static DeckSpellUpgradeResult Unsupported(string reason) =>
            new DeckSpellUpgradeResult(null, Array.Empty<int>(), reason);
    }
}
