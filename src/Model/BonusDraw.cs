using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class BonusDrawCounter
    {
        public string Key { get; }
        public int Value { get; }
        public BonusDrawCounter(string key, int value) { Key = key; Value = value; }
    }
    public sealed class BonusDrawListener
    {
        public string Key { get; }
        public CardUpgradeModifier Upgrade { get; }
        public BonusDrawListener(string key, CardUpgradeModifier upgrade) { Key = key; Upgrade = upgrade; }
    }
    public sealed class BonusDrawState
    {
        public IReadOnlyList<BonusDrawCounter> Counters { get; }
        public IReadOnlyList<BonusDrawListener> Listeners { get; }
        public BonusDrawState(IReadOnlyList<BonusDrawCounter> counters, IReadOnlyList<BonusDrawListener> listeners)
        { Counters = Array.AsReadOnly(counters.Where(counter => counter.Value != 0).OrderBy(counter => counter.Key, StringComparer.Ordinal).ToArray());
            Listeners = Array.AsReadOnly(listeners.ToArray()); }
        public static string CardKey(int id, int effect) => "card:" + id + ":" + effect;
        public static string UnitKey(int id, int trigger, int effect) => "unit:" + id + ":" + trigger + ":" + effect;
    }
    public sealed class BonusUpgradeApplication
    {
        public int CardId { get; }
        public CardUpgradeModifier Upgrade { get; }
        public BonusUpgradeApplication(int cardId, CardUpgradeModifier upgrade) { CardId = cardId; Upgrade = upgrade; }
    }
    public static class BonusDrawModel
    {
        public static string? Validate(CardCycleState cards)
        {
            BonusDrawState? bonus = cards.BonusDraw;
            if (bonus == null) return null;
            if (bonus.Counters.Any(counter => string.IsNullOrEmpty(counter.Key)) ||
                bonus.Counters.Select(counter => counter.Key).Distinct().Count() != bonus.Counters.Count ||
                bonus.Listeners.Any(listener => string.IsNullOrEmpty(listener.Key))) return "Invalid bonus-draw effect identity.";
            foreach (BonusDrawListener listener in bonus.Listeners)
                if (listener.Upgrade.ExternalInteractions.Count > 0) return string.Join("; ", listener.Upgrade.ExternalInteractions);
            return null;
        }
        public static CardCycleState Schedule(CardCycleState cards, string key, int amount, CardUpgradeModifier? upgrade)
        {
            if (cards.BonusDraw == null) throw new ArgumentException("Missing captured bonus-draw state.", nameof(cards));
            var counters = cards.BonusDraw.Counters.ToDictionary(counter => counter.Key, counter => counter.Value);
            var listeners = cards.BonusDraw.Listeners.ToList();
            if (upgrade != null)
            {
                counters.TryGetValue(key, out int previous); counters[key] = unchecked(previous + amount);
                if (amount != 0) listeners.Add(new BonusDrawListener(key, upgrade));
            }
            return new CardCycleState(cards.Hand, cards.Draw, cards.Discard, cards.Rng, unchecked(cards.DrawModifier + amount),
                cards.ExternalInteractions, new BonusDrawState(counters.Select(pair => new BonusDrawCounter(pair.Key, pair.Value)).ToArray(), listeners));
        }
        internal static BonusDrawState? CompleteDraw(BonusDrawState? source, IReadOnlyList<CardToken> drawn,
            int existingHand, int bonusStart, List<BonusUpgradeApplication> applications)
        {
            if (source == null) return null;
            var counters = source.Counters.ToDictionary(counter => counter.Key, counter => counter.Value);
            for (int index = 0; index < drawn.Count; index++)
                if (bonusStart >= 0 && existingHand + index + 1 > bonusStart)
                    foreach (BonusDrawListener listener in source.Listeners)
                        if (counters.TryGetValue(listener.Key, out int remaining) && remaining > 0)
                        { counters[listener.Key] = remaining - 1; applications.Add(new BonusUpgradeApplication(drawn[index].InstanceId, listener.Upgrade)); }
            // Every DrawCards dispatches null and clears its listeners, even a zero/negative draw.
            foreach (BonusDrawListener listener in source.Listeners) counters.Remove(listener.Key);
            return new BonusDrawState(counters.Select(pair => new BonusDrawCounter(pair.Key, pair.Value)).ToArray(), Array.Empty<BonusDrawListener>());
        }
        internal static CombatContext ApplyUpgrades(CombatContext context, CardCycleResult draw, BattlePlayRules? definitions, out string? error)
        {
            error = null;
            foreach (BonusUpgradeApplication application in draw.UpgradeApplications)
            {
                CardInstanceState? card = context.FindCard(application.CardId);
                CardPlayRule? rule = definitions?.Cards.FirstOrDefault(item => item.DataId == card?.DataId);
                if (card == null || rule == null) { error = "Missing bonus-draw card instance or upgrade definition."; return context; }
                var interactions = rule.UpgradeInteractions ?? rule.ExternalInteractions;
                if (interactions.Count > 0) { error = string.Join("; ", interactions); return context; }
                context = context.WithCard(new CardInstanceState(card.InstanceId, card.DataId, card.Permanent,
                    UnitModifierModel.Add(card.Temporary, application.Upgrade), card.LastPlayedCost, card.LastForgedAmount, card.PlayCount,
                    card.ExternalInteractions, card.EffectCounters, card.DamageScalingTraits, card.StatusScalingTraits, card.UnitUpgradeScalingTraits, card.CapacityScalingTraits, card.EquippedUnitId, card.PlayedRoomUnitIds, card.RawPlayedRoomUnitIds));
            }
            return context;
        }
    }
}
