using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class HandRemovalResult
    {
        public CombatContext? Context { get; }
        public IReadOnlyList<CardPileState>? OtherPiles { get; }
        public IReadOnlyList<CardToken> RemovedCards { get; }
        public int ConsumedCount { get; }
        public string? UnsupportedReason { get; }
        public bool Supported => Context != null;
        internal HandRemovalResult(CombatContext? context, IReadOnlyList<CardPileState>? piles, IReadOnlyList<CardToken> removed,
            int consumed, string? error = null)
        { Context = context; OtherPiles = piles == null ? null : Array.AsReadOnly(piles.ToArray());
            RemovedCards = Array.AsReadOnly(removed.ToArray()); ConsumedCount = consumed; UnsupportedReason = error; }
    }

    public static class HandRemovalModel
    {
        public static HandRemovalResult Apply(CombatContext source, int mode, int sourceCardId, int effectIndex,
            BattlePlayRules? definitions, IReadOnlyList<CardPileState>? otherPiles = null,
            IReadOnlyList<int>? pendingExhaustedCards = null, bool exhaustionRunsTriggerQueue = false)
        {
            if (source.Cards.ExternalInteractions.Count > 0) return Unsupported(string.Join("; ", source.Cards.ExternalInteractions));
            CardToken[] targets = mode == 0 || mode == 1
                ? source.Cards.Hand.Where(card => card.InstanceId != sourceCardId).ToArray() : Array.Empty<CardToken>();
            CardPileState[]? piles = otherPiles?.ToArray();
            int[] pending = (pendingExhaustedCards ?? Array.Empty<int>()).ToArray();
            string? membershipError = CardPileModel.ValidateMembership(source.Cards, piles ?? Array.Empty<CardPileState>(), source.NextCardId);
            if (membershipError != null) return Unsupported(membershipError);
            if (piles != null)
            {
                if (piles.Select(pile => pile.Name).Distinct().Count() != piles.Length) return Unsupported("Duplicate card piles.");
                foreach (CardPileState pile in piles)
                {
                    string? error = CardPileModel.Validate(pile);
                    if (error != null) return Unsupported(error);
                }
            }
            if (mode == 1 && targets.Length > 0 && (piles == null || !piles.Any(pile => pile.Name == "Standby") ||
                !piles.Any(pile => pile.Name == "Exhausted"))) return Unsupported("Hand consumption requires standby and exhausted piles.");
            if (mode == 0 && targets.Length > 0 && (piles == null || !piles.Any(pile => pile.Name == "DiscardBuffer")))
                return Unsupported("Spell hand discard requires the discard buffer alias state.");
            if (targets.Length > 0 && pending.Length > 0 && (pending.Distinct().Count() != pending.Length || piles == null ||
                !piles.Any(pile => pile.Name == "Exhausted") || pending.Any(id => !piles.Any(pile => pile.Name == "Standby" &&
                    pile.Cards.Any(card => card.InstanceId == id))))) return Unsupported("Invalid pending standby exhaustion.");
            CardToken[] returning = targets.Length == 0 ? Array.Empty<CardToken>() :
                (piles?.Where(pile => pile.Name == "Standby").SelectMany(pile => pile.Cards).Where(card => pending.Contains(card.InstanceId)).ToArray()
                    ?? Array.Empty<CardToken>());
            foreach (CardToken token in targets.Concat(returning))
            {
                CardPlayRule? rule = definitions?.Cards.FirstOrDefault(card => card.DataId == token.DataId);
                IReadOnlyList<string>? interactions = mode == 0 && targets.Contains(token) ? rule?.HandDiscardInteractions : rule?.HandConsumeInteractions;
                if (interactions == null) return Unsupported("Missing hand removal callback definitions for " + token.DataId);
                if (interactions.Count > 0) return Unsupported(string.Join("; ", interactions));
                if (source.CardInstances != null)
                {
                    CardInstanceState? instance = source.CardInstances.FirstOrDefault(card => card.InstanceId == token.InstanceId);
                    if (instance == null || instance.DataId != token.DataId) return Unsupported("Missing or mismatched removed card instance.");
                    string? error = CardModifierModel.UnsupportedReason(instance);
                    if (error != null) return Unsupported(error);
                }
            }
            CombatContext context = source;
            var processing = new HashSet<int>();
            foreach (CardToken token in targets)
            {
                CardCycleState cards = context.Cards;
                context = context.WithCards(new CardCycleState(cards.Hand.Where(card => card.InstanceId != token.InstanceId).ToArray(),
                    cards.Draw, mode == 0 ? cards.Discard.Concat(new[] { token }).ToArray() : cards.Discard,
                    cards.Rng, cards.DrawModifier, cards.ExternalInteractions));
                if (mode == 0)
                {
                    CardPileState buffer = piles!.Single(pile => pile.Name == "DiscardBuffer");
                    CardPileState moved = CardPileModel.Add(CardPileModel.Remove(buffer, token.InstanceId), token);
                    piles = piles.Select(pile => pile == buffer ? moved : pile).ToArray();
                    context = context.WithStatistics(context.Statistics?.Increment(token.InstanceId, "TimesDiscarded"));
                    CardInstanceState? instance = context.FindCard(token.InstanceId);
                    if (instance != null) context = context.WithCard(instance.OnDiscard(false));
                    CheckStandby(0);
                }
                else
                {
                    CardPileState standby = piles!.Single(pile => pile.Name == "Standby");
                    CardPileState exhausted = piles.Single(pile => pile.Name == "Exhausted");
                    piles = piles.Select(pile => pile == standby ? CardPileModel.Add(standby, token) :
                        pile == exhausted ? CardPileModel.Add(exhausted, token) : pile).ToArray();
                    context = context.WithStatistics(context.Statistics?.Increment(token.InstanceId, "TimesExhausted"));
                    CheckStandby(token.InstanceId);
                    RunTriggerQueue();
                }
            }
            int consumed = mode == 1 ? targets.Length : 0;
            CardInstanceState? resolving = context.FindCard(sourceCardId);
            if (resolving != null) context = context.WithCard(resolving.WithCounter(effectIndex, "CardEffectDiscardHand", consumed));
            return new HandRemovalResult(context, piles, targets, consumed);

            void CheckStandby(int consumedId)
            {
                if (piles == null) return;
                // Native snapshots physical dictionary order, but callbacks can remove a later entry recursively.
                CardPileState? standby = piles.FirstOrDefault(pile => pile.Name == "Standby");
                if (standby == null) return;
                CardToken[] eligible = standby.Cards
                    .Where(card => card.InstanceId == consumedId || pending.Contains(card.InstanceId)).ToArray();
                foreach (CardToken card in eligible) ReturnCard(card, exhaustionRunsTriggerQueue);
            }
            void ReturnCard(CardToken card, bool runQueue)
            {
                CardPileState standby = piles!.Single(pile => pile.Name == "Standby");
                if (!standby.Cards.Any(item => item.InstanceId == card.InstanceId) || !processing.Add(card.InstanceId)) return;
                CardPileState exhausted = piles.Single(pile => pile.Name == "Exhausted");
                piles = piles.Select(pile => pile == exhausted ? CardPileModel.Add(CardPileModel.Remove(exhausted, card.InstanceId), card) : pile).ToArray();
                context = context.WithStatistics(context.Statistics?.Increment(card.InstanceId, "TimesExhausted"));
                // CardExhausted character callbacks drain deaths before this standby entry is removed.
                if (runQueue) RunTriggerQueue();
                standby = piles.Single(pile => pile.Name == "Standby");
                piles = piles.Select(pile => pile == standby ? CardPileModel.Remove(standby, card.InstanceId) : pile).ToArray();
                processing.Remove(card.InstanceId);
            }
            void RunTriggerQueue()
            {
                foreach (int id in pending)
                {
                    CardToken? card = piles!.Single(pile => pile.Name == "Standby").Cards.FirstOrDefault(item => item.InstanceId == id);
                    if (card != null) ReturnCard(card, false);
                }
            }
        }
        private static HandRemovalResult Unsupported(string error) => new HandRemovalResult(null, null, Array.Empty<CardToken>(), 0, error);
    }
}
