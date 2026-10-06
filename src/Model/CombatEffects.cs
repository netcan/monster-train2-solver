using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    // Shared outputs of unit effects. The room and train phases carry this state forward.
    public sealed class CombatContext
    {
        public CardCycleState Cards { get; }
        public UnityRng BattleRng { get; }
        public int Gold { get; }
        public int NextCardId { get; }
        public int MaxHandSize { get; }
        public IReadOnlyList<CombatStatus> StatusRules { get; }
        public BattleStatistics? Statistics { get; }
        public IReadOnlyList<CardInstanceState>? CardInstances { get; }
        // Identity store for observed cards, including references retained after ClearCards.
        // Membership here does not make a card owned or playable.
        public IReadOnlyList<CardInstanceState>? CardRegistry { get; }
        public bool? AllScenarioBossesDead { get; }
        public CombatContext(CardCycleState cards, UnityRng battleRng, int gold, int nextCardId, int maxHandSize,
            IReadOnlyList<CombatStatus>? statusRules = null, BattleStatistics? statistics = null, IReadOnlyList<CardInstanceState>? cardInstances = null,
            IReadOnlyList<CardInstanceState>? cardRegistry = null, bool? allScenarioBossesDead = null)
        { Cards = cards; BattleRng = battleRng; Gold = gold; NextCardId = nextCardId; MaxHandSize = maxHandSize;
            StatusRules = Array.AsReadOnly((statusRules ?? Array.Empty<CombatStatus>()).ToArray()); Statistics = statistics;
            CardInstances = cardInstances == null ? null : Array.AsReadOnly(cardInstances.OrderBy(card => card.InstanceId).ToArray());
            CardRegistry = cardRegistry == null ? null : Array.AsReadOnly(cardRegistry.Concat(cardInstances ?? Array.Empty<CardInstanceState>())
                .GroupBy(card => card.InstanceId).Select(group => group.Last()).OrderBy(card => card.InstanceId).ToArray());
            AllScenarioBossesDead = allScenarioBossesDead; }
        internal CombatContext WithStatistics(BattleStatistics? statistics) => new CombatContext(Cards, BattleRng,
            Gold, NextCardId, MaxHandSize, StatusRules, statistics, CardInstances, CardRegistry, AllScenarioBossesDead);
        internal CombatContext WithCardInstances(IReadOnlyList<CardInstanceState>? instances) => new CombatContext(Cards, BattleRng,
            Gold, NextCardId, MaxHandSize, StatusRules, Statistics, instances, CardRegistry, AllScenarioBossesDead);
        internal CardInstanceState? FindCard(int id) => CardInstances?.FirstOrDefault(card => card.InstanceId == id)
            ?? CardRegistry?.FirstOrDefault(card => card.InstanceId == id);
        internal CombatContext WithCard(CardInstanceState changed) => new CombatContext(Cards, BattleRng,
            Gold, NextCardId, MaxHandSize, StatusRules, Statistics,
            CardInstances?.Select(card => card.InstanceId == changed.InstanceId ? changed : card).ToArray(),
            CardRegistry?.Select(card => card.InstanceId == changed.InstanceId ? changed : card).ToArray(), AllScenarioBossesDead);
        internal CombatContext WithBossesDead() => new CombatContext(Cards, BattleRng, Gold, NextCardId,
            MaxHandSize, StatusRules, Statistics, CardInstances, CardRegistry, AllScenarioBossesDead.HasValue ? true : (bool?)null);
        internal BattleStatistics? LiveStatistics => CardInstances?.Count == 0 ? Statistics?.RefreshDeckAfterCardTerminal() : Statistics;
        internal CombatContext WithBattleRng(UnityRng rng) => new CombatContext(Cards, rng, Gold, NextCardId,
            MaxHandSize, StatusRules, Statistics, CardInstances, CardRegistry, AllScenarioBossesDead);
        internal CombatContext WithCards(CardCycleState cards) => new CombatContext(cards, BattleRng, Gold, NextCardId,
            MaxHandSize, StatusRules, Statistics, CardInstances, CardRegistry, AllScenarioBossesDead);
    }

    public sealed class CombatEffect
    {
        public string Type { get; }
        public int Value { get; }
        public int Counter { get; }
        public string Destination { get; }
        public int Count { get; }
        public IReadOnlyList<string> CardPool { get; }
        public bool SkipDuplicateInHand { get; }
        public CombatEffect(string type, int value, int counter, string destination, int count,
            IReadOnlyList<string> cardPool, bool skipDuplicateInHand)
        {
            Type = type; Value = value;
            // Every remaining count <= 1 despawns on the next application. Native UI previews can
            // decrement the private counter below zero; canonicalize only those equivalent states.
            Counter = type == "CardEffectDespawnCharacter" ? Math.Max(1, counter) : counter;
            // Only generated-card effects interpret this parameter as a pile destination.
            Destination = type == "CardEffectAddBattleCard" ? destination : ""; Count = count;
            CardPool = Array.AsReadOnly(cardPool.ToArray()); SkipDuplicateInHand = skipDuplicateInHand;
        }
        internal CombatEffect WithCounter(int counter) => new CombatEffect(Type, Value, counter,
            Destination, Count, CardPool, SkipDuplicateInHand);
    }

    public sealed class CombatTrigger
    {
        public string Kind { get; }
        public bool Once { get; }
        public bool HasTriggered { get; }
        public bool IgnoreSilence { get; }
        public int FireCount { get; }
        public IReadOnlyList<CombatEffect> Effects { get; }
        public bool? SkipDuringDeployment { get; }
        public CombatTrigger(string kind, bool once, bool hasTriggered, bool ignoreSilence,
            int fireCount, IReadOnlyList<CombatEffect> effects, bool? skipDuringDeployment = null)
        {
            Kind = kind; Once = once; HasTriggered = hasTriggered; IgnoreSilence = ignoreSilence;
            FireCount = fireCount; Effects = Array.AsReadOnly(effects.ToArray()); SkipDuringDeployment = skipDuringDeployment;
        }
        internal CombatTrigger Fired(IReadOnlyList<CombatEffect> effects) => new CombatTrigger(Kind,
            Once, true, IgnoreSilence, FireCount, effects, SkipDuringDeployment);
        internal CombatTrigger ForPreview() => new CombatTrigger(Kind, Once, false, IgnoreSilence, FireCount, Effects, SkipDuringDeployment);
    }
}
