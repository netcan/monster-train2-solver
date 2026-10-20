using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class CardRecursionSelection
    {
        public int EffectIndex { get; }
        public IReadOnlyList<int> CardInstanceIds { get; }

        public CardRecursionSelection(int effectIndex, IReadOnlyList<int> cardInstanceIds)
        {
            EffectIndex = effectIndex;
            CardInstanceIds = Array.AsReadOnly(cardInstanceIds.ToArray());
        }
    }

    public sealed class CardRecursionResult
    {
        public CardCycleState? Cards { get; }
        public IReadOnlyList<CardPileState>? OtherPiles { get; }
        public UnityRng? BattleRng { get; }
        public IReadOnlyList<int> RestoredCardIds { get; }
        public IReadOnlyList<CardInstanceState> ChangedCards { get; }
        public string? UnsupportedReason { get; }
        public bool Supported => Cards != null;
        internal CardRecursionResult(CardCycleState? cards, IReadOnlyList<CardPileState>? otherPiles,
            UnityRng? battleRng, string? error = null, IReadOnlyList<int>? restoredCardIds = null,
            IReadOnlyList<CardInstanceState>? changedCards = null)
        { Cards = cards; OtherPiles = otherPiles == null ? null : Array.AsReadOnly(otherPiles.ToArray());
            BattleRng = battleRng; RestoredCardIds = Array.AsReadOnly((restoredCardIds ?? Array.Empty<int>()).ToArray());
            ChangedCards = Array.AsReadOnly((changedCards ?? Array.Empty<CardInstanceState>()).ToArray()); UnsupportedReason = error; }
    }

    public static class CardRecursionModel
    {
        public static IReadOnlyList<int> EligibleCards(CardCycleState cards, IReadOnlyList<CardPileState>? otherPiles,
            CardActionEffect effect, BattlePlayRules? definitions, int sourceCardId, out string? error,
            IReadOnlyList<CardInstanceState>? cardRegistry = null)
        {
            error = Validate(effect, otherPiles, cards);
            if (error != null) return Array.Empty<int>();
            IEnumerable<CardToken> candidates = effect.Target switch
            {
                "Discard" => cards.Discard,
                "Exhaust" => otherPiles!.Single(pile => pile.Name == "Exhausted").Cards,
                "Eaten" => otherPiles!.Single(pile => pile.Name == "Eaten").Cards,
                _ => Array.Empty<CardToken>()
            };
            var eligible = new List<int>();
            foreach (CardToken card in candidates)
            {
                if (card.InstanceId == sourceCardId) continue;
                CardPlayRule? rule = definitions?.Cards.FirstOrDefault(item => item.DataId == card.DataId);
                if (rule == null)
                {
                    error = "Recursion requires the captured play definition for card " + card.DataId + ".";
                    return Array.Empty<int>();
                }
                if (!string.IsNullOrEmpty(effect.Recursion!.CardType) && effect.Recursion.CardType != "Invalid" &&
                    rule.CardType != effect.Recursion.CardType) continue;
                if (effect.Recursion.Subtype.Length > 0 && rule.SpawnUnit?.Subtypes.Contains(effect.Recursion.Subtype) != true)
                    continue;
                if (effect.Recursion.CardFilter != null)
                {
                    CardInstanceState? instance = cardRegistry?.FirstOrDefault(item => item.InstanceId == card.InstanceId);
                    if (instance?.MaskDescriptor == null)
                    {
                        error = "Recursion card filtering requires branch-owned card mask metadata for card " + card.InstanceId + ".";
                        return Array.Empty<int>();
                    }
                    CardUpgradeMaskCard maskCard;
                    try { maskCard = CardBranchMaskModel.Resolve(instance); }
                    catch (InvalidOperationException exception)
                    {
                        error = "Recursion card filtering cannot resolve card " + card.InstanceId + ": " + exception.Message;
                        return Array.Empty<int>();
                    }
                    if (!CardUpgradeMaskModel.FilterCard(effect.Recursion.CardFilter, maskCard)) continue;
                }
                eligible.Add(card.InstanceId);
            }
            return Array.AsReadOnly(eligible.ToArray());
        }

        public static CardRecursionResult Apply(CardCycleState cards, IReadOnlyList<CardPileState>? otherPiles,
            UnityRng battleRng, CardActionEffect effect, BattlePlayRules? definitions, int sourceCardId, int maxHandSize,
            IReadOnlyList<int>? selectedCardIds = null, IReadOnlyList<CardInstanceState>? cardRegistry = null, int? drawCount = null)
        {
            if (effect.Recursion?.SelectionMode == "RandomToRoomUntilCapacityFull")
                return new CardRecursionResult(null, null, null, "Room recursion requires the complete train birth transition.");
            IReadOnlyList<int> eligible = EligibleCards(cards, otherPiles, effect, definitions, sourceCardId,
                out string? error, cardRegistry);
            if (error != null) return new CardRecursionResult(null, null, null, error);
            if (maxHandSize < cards.Hand.Count) return new CardRecursionResult(null, null, null, "Invalid hand size.");
            int[] selected;
            UnityRng nextBattleRng;
            if (IsChoiceMode(effect.Recursion!.SelectionMode))
            {
                if (eligible.Count == 0 && selectedCardIds == null)
                    selected = Array.Empty<int>();
                else if (selectedCardIds == null || selectedCardIds.Count != 1 ||
                    selectedCardIds.Distinct().Count() != selectedCardIds.Count ||
                    !eligible.Contains(selectedCardIds[0]))
                    return new CardRecursionResult(null, null, null, "Card recursion requires exactly one eligible selected card.");
                else
                    selected = [selectedCardIds[0]];
                nextBattleRng = battleRng;
            }
            else
            {
                if (selectedCardIds != null)
                    return new CardRecursionResult(null, null, null, "Random card recursion cannot take a selected card.");
                ShuffleResult<int> shuffled = battleRng.Shuffle(eligible);
                int count = Math.Min(eligible.Count, Math.Max(0, drawCount ?? effect.Value));
                selected = shuffled.Items.Take(count).ToArray();
                nextBattleRng = shuffled.State;
            }
            var restoredCardIds = new List<int>();
            var hand = cards.Hand.ToList();
            var draw = cards.Draw.ToList();
            var discard = cards.Discard.ToList();
            CardPileState[]? piles = otherPiles?.ToArray();
            string? sourcePileName = effect.Target == "Exhaust" ? "Exhausted" : effect.Target == "Eaten" ? "Eaten" : null;
            CardPileState? sourcePile = sourcePileName == null ? null : piles!.Single(pile => pile.Name == sourcePileName);
            IReadOnlyList<CardToken> sourceCards = sourcePile?.Cards ?? discard;
            Dictionary<int, CardToken> selectedTokens = sourceCards.Where(card => selected.Contains(card.InstanceId))
                .ToDictionary(card => card.InstanceId);
            int capacity = maxHandSize - hand.Count;
            for (int index = selected.Length - 1; index >= 0; index--)
            {
                // The upgraded native path ignores ParamBool2; a full-hand draw leaves restored cards in Draw.
                if (effect.Recursion.DisallowOverflowToDrawPile &&
                    effect.Recursion.SelectionMode == "RandomToHand" && capacity <= 0) break;
                int cardId = selected[index];
                if (!selectedTokens.TryGetValue(cardId, out CardToken? token))
                    return new CardRecursionResult(null, null, null, "A selected recursion card left its source pile.");
                if (sourcePile != null)
                {
                    sourcePile = CardPileModel.Remove(sourcePile, cardId);
                    int standbyIndex = Array.FindIndex(piles!, pile => pile.Name == "Standby");
                    if (standbyIndex >= 0) piles![standbyIndex] = CardPileModel.Remove(piles[standbyIndex], cardId);
                }
                else
                {
                    int discardIndex = discard.FindIndex(card => card.InstanceId == cardId);
                    if (discardIndex < 0)
                        return new CardRecursionResult(null, null, null, "A selected recursion card left its discard pile.");
                    discard.RemoveAt(discardIndex);
                }
                restoredCardIds.Add(cardId);
                if (effect.Recursion.SelectionMode is "RandomToDeck" or "ChooseToDeck")
                    draw.Add(token);
                else if (capacity > 0)
                {
                    hand.Insert(0, token);
                    capacity--;
                }
                else
                    draw.Add(token);
            }
            if (sourcePileName != null)
                piles = piles!.Select(pile => pile.Name == sourcePileName ? sourcePile! : pile).ToArray();
            var changedCards = new List<CardInstanceState>();
            if (effect.Upgrade != null && restoredCardIds.Count > 0)
            {
                if (cardRegistry == null)
                    return new CardRecursionResult(null, null, null, "Card recursion upgrades require the captured card registry.");
                foreach (int cardId in restoredCardIds)
                {
                    CardInstanceState? card = cardRegistry.FirstOrDefault(item => item.InstanceId == cardId);
                    if (card == null)
                        return new CardRecursionResult(null, null, null, "Missing card instance for recursion upgrade " + cardId + ".");
                    if (card.MaskDescriptor != null && effect.Upgrade.MaskMetadata == null)
                        return new CardRecursionResult(null, null, null, "Card recursion upgrade requires branch-owned upgrade mask metadata.");
                    if (card.ExhaustState != null && card.MaskDescriptor == null)
                        return new CardRecursionResult(null, null, null, "Card recursion upgrade cannot refresh traits without branch-owned card mask metadata.");
                    CardModifiers temporary = UnitModifierModel.Add(card.Temporary, effect.Upgrade);
                    if (!ReferenceEquals(temporary, card.Temporary))
                    {
                        // Native ApplyTemporaryUpgrade adds first, then removes the first upgrade matching each asset key.
                        foreach (string asset in effect.Upgrade.Lifecycle?.ReplacedAssets ?? Array.Empty<string>())
                        {
                            List<CardUpgradeModifier> upgrades = temporary.Upgrades.ToList();
                            CardUpgradeModifier? replaced = upgrades.FirstOrDefault(item => item.AssetKey == asset);
                            if (replaced == null) continue;
                            int removeIndex = replaced.DataId.Length == 0 ? upgrades.IndexOf(replaced) :
                                upgrades.FindIndex(item => item.DataId == replaced.DataId);
                            if (removeIndex < 0) continue;
                            upgrades.RemoveAt(removeIndex);
                            temporary = new CardModifiers(temporary.Offsets, upgrades, temporary.PersistentHealth,
                                temporary.ExternalInteractions);
                        }
                    }
                    changedCards.Add(card.WithModifiers(card.Permanent, temporary));
                }
            }
            var updatedCards = new CardCycleState(hand, draw, discard, cards.Rng, cards.DrawModifier,
                cards.ExternalInteractions, cards.BonusDraw, cards.NextDrawUpgrades, cards.NextUpgradeReferenceId, cards.DeckDrawDisabledThisTurn);
            return new CardRecursionResult(updatedCards, piles, nextBattleRng, restoredCardIds: restoredCardIds,
                changedCards: changedCards);
        }

        public static IReadOnlyList<IReadOnlyList<int>> EnumerateChosenCards(IReadOnlyList<int> eligibleCardIds) =>
            Array.AsReadOnly(eligibleCardIds.Select(id => (IReadOnlyList<int>)Array.AsReadOnly(new[] { id })).ToArray());

        public static string? ValidateSelections(IReadOnlyList<CardActionEffect> effects,
            IReadOnlyList<CardRecursionSelection>? selections)
        {
            if (selections == null || selections.Count == 0) return null;
            if (selections.Select(selection => selection.EffectIndex).Distinct().Count() != selections.Count)
                return "Card recursion has duplicate effect selections.";
            foreach (CardRecursionSelection selection in selections)
            {
                if (selection.EffectIndex < 0 || selection.EffectIndex >= effects.Count ||
                    !(effects[selection.EffectIndex].Type == "Recursion" &&
                        IsChoiceMode(effects[selection.EffectIndex].Recursion?.SelectionMode ?? string.Empty) ||
                        effects[selection.EffectIndex].Type is "ChooseDiscard" or "FreezeCard" or "DuplicateCard" or "MergeEquipment") ||
                    (effects[selection.EffectIndex].Type == "MergeEquipment" ? selection.CardInstanceIds.Count != 0 && selection.CardInstanceIds.Count != 2 : selection.CardInstanceIds.Count != 1))
                    return "Card selection does not match a single-card choose-mode effect.";
            }
            return null;
        }

        internal static string? Validate(CardActionEffect effect, IReadOnlyList<CardPileState>? otherPiles,
            CardCycleState? cards = null)
        {
            CardRecursionRule? rule = effect.Recursion;
            if (rule == null) return "Missing native card-recursion definition.";
            if (rule.SelectionMode != "ChooseToHand" && rule.SelectionMode != "ChooseToDeck" &&
                rule.SelectionMode != "RandomToHand" && rule.SelectionMode != "RandomToDeck" &&
                rule.SelectionMode != "RandomToHandWithUpgrades" && rule.SelectionMode != "RandomToRoomUntilCapacityFull")
                return "Unmodeled card-recursion selection mode " + rule.SelectionMode + ".";
            if (rule.HasCardFilter && rule.CardFilter == null) return "Card recursion requires its native card-filter definition.";
            if (!rule.HasCardFilter && rule.CardFilter != null) return "Card recursion has unexpected card-filter metadata.";
            if (rule.SelectionMode == "RandomToHandWithUpgrades" && !rule.HasUpgrade)
                return "Random-to-hand-with-upgrades recursion requires its temporary card-upgrade definition.";
            if (rule.HasUpgrade != (effect.Upgrade != null))
                return "Card recursion requires its temporary card-upgrade definition.";
            if (effect.Upgrade != null && CardModifierModel.UpgradeInteractions(effect.Upgrade).Any())
                return string.Join("; ", CardModifierModel.UpgradeInteractions(effect.Upgrade).Distinct());
            if (effect.Target == "Discard")
            {
                if (cards == null) return "Card recursion requires its captured discard pile.";
                if (rule.SelectionMode != "ChooseToHand")
                    return "Unmodeled Discard card-recursion selection mode " + rule.SelectionMode + ".";
                return null;
            }
            if (effect.Target != "Exhaust" && effect.Target != "Eaten")
                return "Unmodeled card-recursion source pile " + effect.Target + ".";
            if (otherPiles == null || !otherPiles.Any(pile => pile.Name == (effect.Target == "Exhaust" ? "Exhausted" : "Eaten")))
                return "Card recursion requires its captured source pile.";
            return null;
        }

        private static bool IsChoiceMode(string mode) => mode == "ChooseToHand" || mode == "ChooseToDeck";
    }
}
