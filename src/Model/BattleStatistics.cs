using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class CardStatisticValue
    {
        public int CardId { get; }
        public string Duration { get; }
        public string Type { get; }
        public int Value { get; }
        public CardStatisticValue(int cardId, string duration, string type, int value)
        { CardId = cardId; Duration = duration; Type = type; Value = value; }
    }

    public sealed class StatisticCount
    {
        public string Key { get; }
        public int Value { get; }
        public StatisticCount(string key, int value) { Key = key; Value = value; }
    }

    public sealed class CardPlayedCost
    {
        public int CardId { get; }
        public int Cost { get; }
        public CardPlayedCost(int cardId, int cost) { CardId = cardId; Cost = cost; }
    }

    // Zero entries inserted by native statistic queries have the same meaning as absent entries.
    // Capture and model both canonicalize these without invoking the native mutating getters.
    public sealed class BattleStatistics
    {
        public IReadOnlyList<CardStatisticValue> Values { get; }
        public IReadOnlyList<int> TrackedCards { get; }
        public IReadOnlyList<int>? DeckCards { get; }
        public IReadOnlyList<CardPlayedCost> PlayedCosts { get; }
        public IReadOnlyList<int> CardsPlayedThisTurn { get; }
        public IReadOnlyList<StatisticCount> SpawnedThisTurnPerFloor { get; }
        public IReadOnlyList<StatisticCount> SpawnedThisBattlePerFloor { get; }
        public IReadOnlyList<StatisticCount> SubtypesSpawnedThisTurn { get; }
        public IReadOnlyList<StatisticCount> SubtypesSpawnedThisBattle { get; }
        public int MonstersDeadThisTurn { get; }
        public int MonstersDeadThisBattle { get; }
        public int EnergyRemainingEndOfTurn { get; }
        public int GoldStartOfThisTurn { get; }
        public int LastAttackDamageDealt { get; }

        public BattleStatistics(IReadOnlyList<CardStatisticValue> values, IReadOnlyList<CardPlayedCost> playedCosts,
            IReadOnlyList<int> cardsPlayedThisTurn, IReadOnlyList<StatisticCount> spawnedThisTurnPerFloor,
            IReadOnlyList<StatisticCount> spawnedThisBattlePerFloor, IReadOnlyList<StatisticCount> subtypesSpawnedThisTurn,
            IReadOnlyList<StatisticCount> subtypesSpawnedThisBattle, int monstersDeadThisTurn, int monstersDeadThisBattle,
            int energyRemainingEndOfTurn, int goldStartOfThisTurn, int lastAttackDamageDealt, IReadOnlyList<int>? trackedCards = null,
            IReadOnlyList<int>? deckCards = null)
        {
            Values = Array.AsReadOnly(values.Where(value => value.Value != 0).OrderBy(value => value.CardId)
                .ThenBy(value => value.Duration, StringComparer.Ordinal).ThenBy(value => value.Type, StringComparer.Ordinal).ToArray());
            TrackedCards = Array.AsReadOnly((trackedCards ?? values.Select(value => value.CardId).ToArray()).Distinct().OrderBy(id => id).ToArray());
            DeckCards = deckCards == null ? null : Array.AsReadOnly(deckCards.Distinct().OrderBy(id => id).ToArray());
            PlayedCosts = Array.AsReadOnly(playedCosts.OrderBy(value => value.CardId).ToArray());
            CardsPlayedThisTurn = Array.AsReadOnly(cardsPlayedThisTurn.ToArray());
            SpawnedThisTurnPerFloor = Counts(spawnedThisTurnPerFloor);
            SpawnedThisBattlePerFloor = Counts(spawnedThisBattlePerFloor);
            SubtypesSpawnedThisTurn = Counts(subtypesSpawnedThisTurn);
            SubtypesSpawnedThisBattle = Counts(subtypesSpawnedThisBattle);
            MonstersDeadThisTurn = monstersDeadThisTurn; MonstersDeadThisBattle = monstersDeadThisBattle;
            EnergyRemainingEndOfTurn = energyRemainingEndOfTurn; GoldStartOfThisTurn = goldStartOfThisTurn;
            LastAttackDamageDealt = lastAttackDamageDealt;
        }

        public static BattleStatistics Empty(int gold = 0, IReadOnlyList<int>? deckCards = null) => new BattleStatistics(Array.Empty<CardStatisticValue>(),
            Array.Empty<CardPlayedCost>(), Array.Empty<int>(), Array.Empty<StatisticCount>(), Array.Empty<StatisticCount>(),
            Array.Empty<StatisticCount>(), Array.Empty<StatisticCount>(), 0, 0, 0, gold, 0, deckCards: deckCards);

        public int Value(int cardId, string type, string duration = "ThisTurn") =>
            Values.FirstOrDefault(value => value.CardId == cardId && value.Type == type && value.Duration == duration)?.Value ?? 0;

        public BattleStatistics Increment(int cardId, string type, int amount = 1)
        {
            if (cardId <= 0 || amount == 0) return this;
            var values = Values.ToList();
            int[] tracked = TrackedCards.Concat(new[] { cardId }).Distinct().ToArray();
            foreach (string duration in new[] { "ThisTurn", "ThisBattle" })
            {
                AddValue(cardId, duration, type, amount);
                string? any = type == "HeroesKilled" ? "AnyHeroKilled" : type == "SpawnedMonsterDeaths" ? "AnyMonsterDeath" :
                    type == "TimesDiscarded" ? "AnyDiscarded" : type == "TimesPlayed" ? "AnyCardPlayed" :
                    type == "TimesDrawn" ? "AnyCardDrawn" : type == "TimesExhausted" ? "AnyExhausted" : null;
                if (any != null)
                {
                    // Native UpdateScalingTraits increments every entry, and the source entry once more.
                    // This increments by one per event even when the original amount is larger.
                    foreach (int id in tracked) AddValue(id, duration, any, 1);
                    AddValue(cardId, duration, any, 1);
                }
            }
            return Copy(values: values, tracked: tracked, played: type == "TimesPlayed" ? CardsPlayedThisTurn.Concat(new[] { cardId }).ToArray() : null);
            void AddValue(int id, string duration, string counter, int addition)
            {
                CardStatisticValue? prior = values.FirstOrDefault(value => value.CardId == id && value.Type == counter && value.Duration == duration);
                if (prior != null) values.Remove(prior);
                values.Add(new CardStatisticValue(id, duration, counter, checked((prior?.Value ?? 0) + addition)));
            }
        }

        public BattleStatistics TrackCards(IEnumerable<int> cards) => Copy(tracked: TrackedCards.Concat(cards).Distinct().ToArray());
        public BattleStatistics RecordPlayedCard(int cardId) => Copy(played: CardsPlayedThisTurn.Concat(new[] { cardId }).ToArray());
        public BattleStatistics WithPlayedCost(int cardId, int? cost) => Copy(costs: PlayedCosts.Where(value => value.CardId != cardId)
            .Concat(cost == null ? Array.Empty<CardPlayedCost>() : new[] { new CardPlayedCost(cardId, cost.Value) }).ToArray());
        public BattleStatistics RefreshDeckAfterCardTerminal() => DeckCards == null ? this :
            Copy(values: Values.Where(value => DeckCards.Contains(value.CardId)).ToArray(), tracked: DeckCards);
        public BattleStatistics RefreshOwnedCards(IEnumerable<int> cards)
        {
            int[] owned = cards.Distinct().ToArray();
            return Copy(values: Values.Where(value => owned.Contains(value.CardId)).ToArray(), tracked: owned);
        }

        public BattleStatistics Spawn(int roomIndex, IReadOnlyList<string> subtypes) => Copy(
            turnFloors: Add(SpawnedThisTurnPerFloor, new[] { roomIndex.ToString(System.Globalization.CultureInfo.InvariantCulture) }),
            battleFloors: Add(SpawnedThisBattlePerFloor, new[] { roomIndex.ToString(System.Globalization.CultureInfo.InvariantCulture) }),
            turnSubtypes: Add(SubtypesSpawnedThisTurn, subtypes), battleSubtypes: Add(SubtypesSpawnedThisBattle, subtypes));

        public BattleStatistics Death(bool player, int responsibleCardId) => Copy(
            deadTurn: MonstersDeadThisTurn + (player ? 1 : 0), deadBattle: MonstersDeadThisBattle + (player ? 1 : 0))
            .Increment(responsibleCardId, player ? "SpawnedMonsterDeaths" : "HeroesKilled");

        public BattleStatistics WithEndTurnEnergy(int energy) => Copy(energy: energy);
        public BattleStatistics WithLastAttackDamage(int damage) => Copy(lastDamage: damage);

        public BattleStatistics NextTurn(int gold) => Copy(values: Values.Where(value => value.Duration == "ThisBattle")
            .Concat(Values.Where(value => value.Duration == "ThisTurn").Select(value =>
                new CardStatisticValue(value.CardId, "PreviousTurn", value.Type, value.Value))).ToArray(),
            played: Array.Empty<int>(), turnFloors: Array.Empty<StatisticCount>(), turnSubtypes: Array.Empty<StatisticCount>(),
            deadTurn: 0, energy: 0, gold: gold);

        internal string Signature() => string.Join(",", TrackedCards) + "|" + string.Join(";", Values.Select(value =>
            value.CardId + ":" + value.Duration + ":" + value.Type + ":" + value.Value)) + "|" +
            string.Join(";", PlayedCosts.Select(value => value.CardId + ":" + value.Cost)) + "|" +
            string.Join(",", CardsPlayedThisTurn) + "|" + string.Join("|", new[] {
                SpawnedThisTurnPerFloor, SpawnedThisBattlePerFloor, SubtypesSpawnedThisTurn, SubtypesSpawnedThisBattle
            }.Select(counts => string.Join(";", counts.Select(value => value.Key + ":" + value.Value)))) + "|" +
            MonstersDeadThisTurn + ":" + MonstersDeadThisBattle + ":" + EnergyRemainingEndOfTurn + ":" +
            GoldStartOfThisTurn + ":" + LastAttackDamageDealt + "|" + (DeckCards == null ? "legacy" : string.Join(",", DeckCards));

        private BattleStatistics Copy(IReadOnlyList<CardStatisticValue>? values = null, IReadOnlyList<int>? played = null, IReadOnlyList<int>? tracked = null,
            IReadOnlyList<StatisticCount>? turnFloors = null, IReadOnlyList<StatisticCount>? battleFloors = null,
            IReadOnlyList<StatisticCount>? turnSubtypes = null, IReadOnlyList<StatisticCount>? battleSubtypes = null,
            int? deadTurn = null, int? deadBattle = null, int? energy = null, int? gold = null, int? lastDamage = null,
            IReadOnlyList<CardPlayedCost>? costs = null) =>
            new BattleStatistics(values ?? Values, costs ?? PlayedCosts, played ?? CardsPlayedThisTurn,
                turnFloors ?? SpawnedThisTurnPerFloor, battleFloors ?? SpawnedThisBattlePerFloor,
                turnSubtypes ?? SubtypesSpawnedThisTurn, battleSubtypes ?? SubtypesSpawnedThisBattle,
                deadTurn ?? MonstersDeadThisTurn, deadBattle ?? MonstersDeadThisBattle,
                energy ?? EnergyRemainingEndOfTurn, gold ?? GoldStartOfThisTurn, lastDamage ?? LastAttackDamageDealt, tracked ?? TrackedCards, DeckCards);

        private static IReadOnlyList<StatisticCount> Counts(IReadOnlyList<StatisticCount> values) =>
            Array.AsReadOnly(values.Where(value => value.Value != 0).OrderBy(value => value.Key, StringComparer.Ordinal).ToArray());
        private static IReadOnlyList<StatisticCount> Add(IReadOnlyList<StatisticCount> source, IEnumerable<string> keys)
        {
            var counts = source.ToDictionary(value => value.Key, value => value.Value);
            foreach (string key in keys) counts[key] = counts.TryGetValue(key, out int prior) ? checked(prior + 1) : 1;
            return counts.Select(pair => new StatisticCount(pair.Key, pair.Value)).ToArray();
        }
    }
}
