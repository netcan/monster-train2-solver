using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class BattleStatisticsProbe
    {
        internal static BattleStatistics Capture(CardStatistics statistics, Func<CardState, int> cardId,
            IReadOnlyList<CardState>? ownedCards = null)
        {
            T Field<T>(string name) => (T)AccessTools.Field(typeof(CardStatistics), name).GetValue(statistics);
            var values = new List<CardStatisticValue>();
            foreach (var card in Field<Dictionary<CardState, CardStatsEntry>>("deckStats"))
            {
                var durations = (Dictionary<CardStatistics.EntryDuration, Dictionary<CardStatistics.TrackedValueType, int>>)
                    AccessTools.Field(typeof(CardStatsEntry), "entryValues").GetValue(card.Value);
                foreach (var duration in durations)
                foreach (var value in duration.Value)
                    if (value.Value != 0) values.Add(new CardStatisticValue(cardId(card.Key), duration.Key.ToString(), value.Key.ToString(), value.Value));
            }
            StatisticCount[] Floors(string name) => Field<Dictionary<int, int>>(name).Select(pair =>
                new StatisticCount(pair.Key.ToString(System.Globalization.CultureInfo.InvariantCulture), pair.Value)).ToArray();
            StatisticCount[] Subtypes(string name) => Field<Dictionary<SubtypeData, int>>(name).Select(pair =>
                new StatisticCount(pair.Key.Key, pair.Value)).ToArray();
            return new BattleStatistics(values,
                Field<Dictionary<CardState, int>>("playedCardCostStats").Select(pair => new CardPlayedCost(cardId(pair.Key), pair.Value)).ToArray(),
                Field<List<CardState>>("cardsPlayedThisTurn").Select(cardId).ToArray(),
                Floors("numMonstersSpawnedThisTurnPerFloor"), Floors("numMonstersSpawnedThisBattlePerFloor"),
                Subtypes("numMonsterSubtypesSpawnedThisTurn"), Subtypes("numMonsterSubtypesSpawnedThisBattle"),
                Field<int>("numMonstersDeadThisTurn"), Field<int>("numMonstersDeadThisBattle"),
                Field<int>("energyRemainingEndOfTurn"), Field<int>("goldStartOfThisTurn"), Field<int>("lastAttackDamageDealt"),
                Field<Dictionary<CardState, CardStatsEntry>>("deckStats").Keys
                    .Concat(ownedCards ?? AllGameManagers.Instance!.GetCardManager()!.GetAllCards(new List<CardState>())).Select(cardId).ToArray(),
                AllGameManagers.Instance!.GetSaveManager().GetDeckState().Select(cardId).ToArray(),
                Field<Dictionary<CardState, CardStatsEntry>>("deckStats").Keys.Select(cardId).ToArray());
        }
    }
}
