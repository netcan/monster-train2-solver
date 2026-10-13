using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class CardRecursionResult
    {
        public CardCycleState? Cards { get; }
        public IReadOnlyList<CardPileState>? OtherPiles { get; }
        public UnityRng? BattleRng { get; }
        public IReadOnlyList<int> RestoredCardIds { get; }
        public string? UnsupportedReason { get; }
        public bool Supported => Cards != null;
        internal CardRecursionResult(CardCycleState? cards, IReadOnlyList<CardPileState>? otherPiles,
            UnityRng? battleRng, string? error = null, IReadOnlyList<int>? restoredCardIds = null)
        { Cards = cards; OtherPiles = otherPiles == null ? null : Array.AsReadOnly(otherPiles.ToArray());
            BattleRng = battleRng; RestoredCardIds = Array.AsReadOnly((restoredCardIds ?? Array.Empty<int>()).ToArray()); UnsupportedReason = error; }
    }

    public static class CardRecursionModel
    {
        public static IReadOnlyList<int> EligibleCards(CardCycleState cards, IReadOnlyList<CardPileState>? otherPiles,
            CardActionEffect effect, BattlePlayRules? definitions, int sourceCardId, out string? error)
        {
            error = Validate(effect, otherPiles);
            if (error != null) return Array.Empty<int>();
            IEnumerable<CardToken> candidates = effect.Target switch
            {
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
                eligible.Add(card.InstanceId);
            }
            return Array.AsReadOnly(eligible.ToArray());
        }

        public static CardRecursionResult Apply(CardCycleState cards, IReadOnlyList<CardPileState>? otherPiles,
            UnityRng battleRng, CardActionEffect effect, BattlePlayRules? definitions, int sourceCardId, int maxHandSize)
        {
            IReadOnlyList<int> eligible = EligibleCards(cards, otherPiles, effect, definitions, sourceCardId, out string? error);
            if (error != null) return new CardRecursionResult(null, null, null, error);
            if (maxHandSize < cards.Hand.Count) return new CardRecursionResult(null, null, null, "Invalid hand size.");
            ShuffleResult<int> shuffled = battleRng.Shuffle(eligible);
            int count = Math.Min(eligible.Count, Math.Max(0, effect.Value));
            int[] selected = shuffled.Items.Take(count).ToArray();
            var restoredCardIds = new List<int>();
            var hand = cards.Hand.ToList();
            var draw = cards.Draw.ToList();
            CardPileState[] piles = otherPiles!.ToArray();
            string sourcePileName = effect.Target == "Exhaust" ? "Exhausted" : "Eaten";
            CardPileState sourcePile = piles.Single(pile => pile.Name == sourcePileName);
            Dictionary<int, CardToken> selectedTokens = sourcePile.Cards.Where(card => selected.Contains(card.InstanceId))
                .ToDictionary(card => card.InstanceId);
            int capacity = maxHandSize - hand.Count;
            for (int index = selected.Length - 1; index >= 0; index--)
            {
                if (effect.Recursion!.DisallowOverflowToDrawPile && capacity <= 0) break;
                int cardId = selected[index];
                if (!selectedTokens.TryGetValue(cardId, out CardToken? token))
                    return new CardRecursionResult(null, null, null, "A selected recursion card left its source pile.");
                sourcePile = CardPileModel.Remove(sourcePile, cardId);
                int standbyIndex = Array.FindIndex(piles, pile => pile.Name == "Standby");
                if (standbyIndex >= 0) piles[standbyIndex] = CardPileModel.Remove(piles[standbyIndex], cardId);
                restoredCardIds.Add(cardId);
                if (effect.Recursion.SelectionMode == "RandomToDeck")
                    draw.Add(token);
                else if (capacity > 0)
                {
                    hand.Insert(0, token);
                    capacity--;
                }
                else
                    draw.Add(token);
            }
            piles = piles.Select(pile => pile.Name == sourcePileName ? sourcePile : pile).ToArray();
            var updatedCards = new CardCycleState(hand, draw, cards.Discard, cards.Rng, cards.DrawModifier,
                cards.ExternalInteractions, cards.BonusDraw, cards.NextDrawUpgrades, cards.NextUpgradeReferenceId);
            return new CardRecursionResult(updatedCards, piles, shuffled.State, restoredCardIds: restoredCardIds);
        }

        internal static string? Validate(CardActionEffect effect, IReadOnlyList<CardPileState>? otherPiles)
        {
            CardRecursionRule? rule = effect.Recursion;
            if (rule == null) return "Missing native card-recursion definition.";
            if (rule.SelectionMode != "RandomToHand" && rule.SelectionMode != "RandomToDeck")
                return "Unmodeled card-recursion selection mode " + rule.SelectionMode + ".";
            if (rule.RequiresRoomInHand) return "Card recursion requiring hand space is not modeled.";
            if (rule.HasCardFilter) return "Card recursion requires its native card-filter definition.";
            if (rule.HasUpgrade) return "Card recursion requires its temporary card-upgrade definition.";
            if (effect.Target != "Exhaust" && effect.Target != "Eaten")
                return "Unmodeled card-recursion source pile " + effect.Target + ".";
            if (otherPiles == null || !otherPiles.Any(pile => pile.Name == (effect.Target == "Exhaust" ? "Exhausted" : "Eaten")))
                return "Card recursion requires its captured source pile.";
            return null;
        }
    }
}
