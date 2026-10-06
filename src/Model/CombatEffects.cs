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
        public IReadOnlyList<CardUpgradeModifier>? NextAddedTemporaryUpgrades { get; }
        // Shared with room/effect resolution; null identifies legacy captures without this state.
        public IReadOnlyList<CardPileState>? OtherPiles { get; }
        public StatisticQueryFrame? QueryFrame { get; }
        public CombatContext(CardCycleState cards, UnityRng battleRng, int gold, int nextCardId, int maxHandSize,
            IReadOnlyList<CombatStatus>? statusRules = null, BattleStatistics? statistics = null, IReadOnlyList<CardInstanceState>? cardInstances = null,
            IReadOnlyList<CardInstanceState>? cardRegistry = null, bool? allScenarioBossesDead = null,
            IReadOnlyList<CardUpgradeModifier>? nextAddedTemporaryUpgrades = null, IReadOnlyList<CardPileState>? otherPiles = null,
            StatisticQueryFrame? queryFrame = null)
        { Cards = cards; BattleRng = battleRng; Gold = gold; NextCardId = nextCardId; MaxHandSize = maxHandSize;
            StatusRules = Array.AsReadOnly((statusRules ?? Array.Empty<CombatStatus>()).ToArray()); Statistics = statistics;
            CardInstances = cardInstances == null ? null : Array.AsReadOnly(cardInstances.OrderBy(card => card.InstanceId).ToArray());
            CardRegistry = cardRegistry == null ? null : Array.AsReadOnly(cardRegistry.Concat(cardInstances ?? Array.Empty<CardInstanceState>())
                .GroupBy(card => card.InstanceId).Select(group => group.Last()).OrderBy(card => card.InstanceId).ToArray());
            AllScenarioBossesDead = allScenarioBossesDead;
            NextAddedTemporaryUpgrades = nextAddedTemporaryUpgrades == null ? null : Array.AsReadOnly(nextAddedTemporaryUpgrades.ToArray());
            OtherPiles = otherPiles == null ? null : Array.AsReadOnly(otherPiles.ToArray()); QueryFrame = queryFrame; }
        internal CombatContext WithQueryFrame(StatisticQueryFrame? frame) => new CombatContext(Cards, BattleRng,
            Gold, NextCardId, MaxHandSize, StatusRules, Statistics, CardInstances, CardRegistry, AllScenarioBossesDead,
            NextAddedTemporaryUpgrades, OtherPiles, frame);
        internal CombatContext WithStatistics(BattleStatistics? statistics) => new CombatContext(Cards, BattleRng,
            Gold, NextCardId, MaxHandSize, StatusRules, statistics, CardInstances, CardRegistry, AllScenarioBossesDead, NextAddedTemporaryUpgrades, OtherPiles, QueryFrame);
        internal CombatContext WithCardInstances(IReadOnlyList<CardInstanceState>? instances) => new CombatContext(Cards, BattleRng,
            Gold, NextCardId, MaxHandSize, StatusRules, Statistics, instances, CardRegistry, AllScenarioBossesDead, NextAddedTemporaryUpgrades, OtherPiles, QueryFrame);
        internal CardInstanceState? FindCard(int id) => CardInstances?.FirstOrDefault(card => card.InstanceId == id)
            ?? CardRegistry?.FirstOrDefault(card => card.InstanceId == id);
        internal CombatContext WithCard(CardInstanceState changed) => new CombatContext(Cards, BattleRng,
            Gold, NextCardId, MaxHandSize, StatusRules, Statistics,
            CardInstances?.Select(card => card.InstanceId == changed.InstanceId ? changed : card).ToArray(),
            CardRegistry?.Select(card => card.InstanceId == changed.InstanceId ? changed : card).ToArray(), AllScenarioBossesDead, NextAddedTemporaryUpgrades, OtherPiles, QueryFrame);
        internal CombatContext WithBossesDead() => new CombatContext(Cards, BattleRng, Gold, NextCardId,
            MaxHandSize, StatusRules, Statistics, CardInstances, CardRegistry, AllScenarioBossesDead.HasValue ? true : (bool?)null, NextAddedTemporaryUpgrades, OtherPiles, QueryFrame);
        internal BattleStatistics? LiveStatistics => CardInstances?.Count == 0 ? Statistics?.RefreshDeckAfterCardTerminal() : Statistics;
        internal CombatContext WithBattleRng(UnityRng rng) => new CombatContext(Cards, rng, Gold, NextCardId,
            MaxHandSize, StatusRules, Statistics, CardInstances, CardRegistry, AllScenarioBossesDead, NextAddedTemporaryUpgrades, OtherPiles, QueryFrame);
        internal CombatContext WithCards(CardCycleState cards) => new CombatContext(cards, BattleRng, Gold, NextCardId,
            MaxHandSize, StatusRules, Statistics, CardInstances, CardRegistry, AllScenarioBossesDead, NextAddedTemporaryUpgrades, OtherPiles, QueryFrame);
        internal CombatContext WithOtherPiles(IReadOnlyList<CardPileState> piles) => OtherPiles == null ? this : new CombatContext(Cards, BattleRng,
            Gold, NextCardId, MaxHandSize, StatusRules, Statistics, CardInstances, CardRegistry, AllScenarioBossesDead, NextAddedTemporaryUpgrades, piles, QueryFrame);
        internal CombatContext AfterCardEffects() => NextAddedTemporaryUpgrades == null || NextAddedTemporaryUpgrades.Count == 0 ? this : new CombatContext(Cards, BattleRng, Gold,
            NextCardId, MaxHandSize, StatusRules, Statistics, CardInstances, CardRegistry, AllScenarioBossesDead, Array.Empty<CardUpgradeModifier>(), OtherPiles, QueryFrame);
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
        public CardGenerationRule? Generation { get; }
        public CombatEffect(string type, int value, int counter, string destination, int count,
            IReadOnlyList<string> cardPool, bool skipDuplicateInHand, CardGenerationRule? generation = null)
        {
            Type = type; Value = value;
            // Every remaining count <= 1 despawns on the next application. Native UI previews can
            // decrement the private counter below zero; canonicalize only those equivalent states.
            Counter = type == "CardEffectDespawnCharacter" ? Math.Max(1, counter) : counter;
            // Only generated-card effects interpret this parameter as a pile destination.
            Destination = type == "CardEffectAddBattleCard" ? destination : ""; Count = count;
            CardPool = Array.AsReadOnly(cardPool.ToArray()); SkipDuplicateInHand = skipDuplicateInHand;
            Generation = generation;
        }
        internal CombatEffect WithCounter(int counter) => new CombatEffect(Type, Value, counter,
            Destination, Count, CardPool, SkipDuplicateInHand, Generation);
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
