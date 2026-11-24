using System;
using System.Collections.Generic;
using System.Linq;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.NativeCasePlans
{
    internal static class UndoDecisionComparison
    {
        // Native Undo reconstructs the first ordinary turn. Each capture assigns
        // local identities to the newly observed cards and characters.
        internal static string? Difference(BattleTurnState opening, BattleTurnState restored)
        {
            try
            {
                var before = JObject.FromObject(opening);
                var after = JObject.FromObject(restored);
                var openingCards = PileCards(opening).ToArray();
                var restoredCards = PileCards(restored).ToArray();
                Require(openingCards.Select(card => (card.Pile, card.Card.DataId)).SequenceEqual(
                    restoredCards.Select(card => (card.Pile, card.Card.DataId))), "ordered card piles differ");
                var cards = Bijection(openingCards.Select(card => card.Card.InstanceId),
                    restoredCards.Select(card => card.Card.InstanceId), "cards");

                var openingUnits = PlacedUnits(opening).ToArray();
                var restoredUnits = PlacedUnits(restored).ToArray();
                Require(openingUnits.Select(unit => unit.Position).SequenceEqual(restoredUnits.Select(unit => unit.Position)),
                    "original physical unit slots differ");
                var units = Bijection(openingUnits.Select(unit => unit.Id), restoredUnits.Select(unit => unit.Id), "units");

                Normalize(before, Bijection(cards.Values, cards.Values, "opening cards"),
                    Bijection(units.Values, units.Values, "opening units"));
                Normalize(after, cards, units);
                return JToken.DeepEquals(before, after) ? null : "undo states differ after identity comparison";
            }
            catch (InvalidOperationException error)
            {
                return error.Message;
            }
        }

        private static IEnumerable<(string Pile, CardToken Card)> PileCards(BattleTurnState state)
        {
            Require(state.Spawn.Turn == 1 && state.Spawn.PendingDestroyedUnitIds?.Count is null or 0 &&
                state.OtherPiles.All(pile => pile.Cards.Count == 0), "expected the first settled ordinary turn");
            var cycle = state.Spawn.Train.Context!.Cards;
            return cycle.Hand.Select(card => ("Hand", card)).Concat(cycle.Draw.Select(card => ("Draw", card)))
                .Concat(cycle.Discard.Select(card => ("Discard", card)));
        }

        private static IEnumerable<((int Room, CombatTeam Team, int Index, string Definition) Position, int Id)> PlacedUnits(
            BattleTurnState state)
        {
            var train = state.Spawn.Train;
            var points = train.Context!.SpawnPoints!;
            Require(train.Rooms.All(room => !room.Preview && room.ActiveCompanionBossIds?.Count is null or 0) &&
                points.Units.Count == train.Rooms.Sum(room => room.Units.Count) &&
                points.Groups.All(group => group.PreviewCopyId == null), "expected only live primary floor occupants");
            foreach (var room in train.Rooms)
                foreach (var unit in room.Units)
                {
                    var owner = points.Units.SingleOrDefault(point => point.UnitId == unit.Id);
                    var slot = owner?.Current;
                    Require(owner is { OuterBoss: false, SpawnedInPreview: false, LastKnown: null } &&
                        slot != null && slot.RoomIndex == room.RoomIndex && slot.Team == unit.Team &&
                        slot.PreviewCopyId == null && !slot.OuterBossAnchor &&
                        points.Group(slot.RoomIndex, slot.Team)?.Occupants.ElementAtOrDefault(slot.Index) == unit.Id,
                        "expected an original unit in its primary physical slot");
                    yield return ((room.RoomIndex, unit.Team, slot!.Index, unit.AssetKey), unit.Id);
                }
        }

        private static Dictionary<int, int> Bijection(IEnumerable<int> opening, IEnumerable<int> restored, string label)
        {
            int[] before = opening.ToArray(), after = restored.ToArray();
            Require(before.Length == after.Length && before.All(id => id > 0) && after.All(id => id > 0) &&
                before.Distinct().Count() == before.Length && after.Distinct().Count() == after.Length,
                "expected unique original " + label);
            return after.Zip(before, (oldId, newId) => (oldId, newId)).ToDictionary(item => item.oldId, item => item.newId);
        }

        private static void Normalize(JObject snapshot, Dictionary<int, int> cards, Dictionary<int, int> units)
        {
            var train = (JObject)snapshot["Spawn"]!["Train"]!;
            foreach (JObject room in train["Rooms"]!)
            {
                foreach (JObject unit in room["Units"]!)
                {
                    unit["Id"] = Mapped(unit["Id"]!, units);
                    Require(Empty(unit["Ability"]) && Empty(unit["EquipmentCards"]) && Empty(unit["DamageHistory"]),
                        "undo fixture contains unit history or attached cards");
                }
                NormalizeContext((JObject)room["Context"]!, cards, units);
            }
            foreach (JObject movement in train["Movement"]!)
                movement["UnitId"] = Mapped(movement["UnitId"]!, units);

            NormalizeContext((JObject)train["Context"]!, cards, units);
            // Unhandled captured relation fields must be empty in this stock
            // boundary. Equal raw IDs could otherwise refer to different objects.
            var remaining = snapshot.Descendants().OfType<JProperty>().FirstOrDefault(property =>
                (property.Name.EndsWith("CardId", StringComparison.Ordinal) ||
                 property.Name.EndsWith("UnitId", StringComparison.Ordinal) ||
                 property.Name.EndsWith("CardIds", StringComparison.Ordinal) ||
                 property.Name.EndsWith("UnitIds", StringComparison.Ordinal)) &&
                property.Name is not ("NextCardId" or "NextUnitId" or "InstanceId" or "CardId" or "UnitId") &&
                (property.Value.Type is JTokenType.Integer or JTokenType.Array) && !Empty(property.Value));
            Require(remaining == null, "unhandled undo identity field " + remaining?.Path);
        }

        private static void NormalizeContext(JObject context, Dictionary<int, int> cards, Dictionary<int, int> units)
        {
            Require(Empty(context["Enchantments"]) && Empty(context["AbilityCardCache"]) &&
                Empty(context["LifeLinks"]) && Empty(context["ConduitRetained"]),
                "undo fixture contains retained combat identities");
            foreach (string pile in new[] { "Hand", "Draw", "Discard" })
                foreach (JObject card in context["Cards"]![pile]!)
                    card["InstanceId"] = Mapped(card["InstanceId"]!, cards);
            foreach (string table in new[] { "CardInstances", "CardRegistry" })
            {
                var entries = (JArray)context[table]!;
                Require(entries.Count == cards.Count && entries.Select(card => (int)card["InstanceId"]!).Distinct().Count() == cards.Count,
                    "undo card ownership is outside the observed piles");
                SortIdentities(context, table, "InstanceId", cards);
            }
            var statistics = (JObject)context["Statistics"]!;
            SortIdentities(statistics, "Values", "CardId", cards, sortStatistics: true);
            SortIdentities(statistics, "PlayedCosts", "CardId", cards);
            foreach (string field in new[] { "TrackedCards", "StoredCards", "DeckCards" })
                if (statistics[field] is JArray membership)
                    statistics[field] = new JArray(membership.Select(id => Mapped(id, cards)).OrderBy(id => id));
            foreach (string field in new[] { "CardsPlayedThisTurn", "HandCardsPlayedThisTurn", "HandCardsPlayedLastTurn" })
                if (statistics[field] is JArray history)
                    statistics[field] = new JArray(history.Select(id => Mapped(id, cards)));

            var points = (JObject)context["SpawnPoints"]!;
            SortIdentities(points, "Units", "UnitId", units);
            foreach (JObject group in points["Groups"]!)
                group["Occupants"] = new JArray(group["Occupants"]!.Select(id => Mapped(id, units, allowEmpty: true)));
            if (points["NextPreviewCopyId"]?.Type == JTokenType.Integer)
                points["NextPreviewCopyId"] = 0;
        }

        private static void SortIdentities(JObject owner, string field, string identity, Dictionary<int, int> map,
            bool sortStatistics = false)
        {
            var rows = ((JArray)owner[field]!).Cast<JObject>();
            foreach (var row in rows)
                row[identity] = Mapped(row[identity]!, map);
            var sorted = rows.OrderBy(row => (int)row[identity]!);
            owner[field] = new JArray(sortStatistics
                ? sorted.ThenBy(row => (string?)row["Duration"], StringComparer.Ordinal)
                    .ThenBy(row => (string?)row["Type"], StringComparer.Ordinal)
                : sorted);
        }

        private static int Mapped(JToken token, Dictionary<int, int> map, bool allowEmpty = false)
        {
            int id = (int)token;
            if (id == 0 && allowEmpty) return 0;
            if (map.TryGetValue(id, out int mapped)) return mapped;
            throw new InvalidOperationException("unmapped undo identity at " + token.Path);
        }

        private static bool Empty(JToken? token) => token == null || token.Type == JTokenType.Null ||
            token is JArray { Count: 0 } || token.Type == JTokenType.Integer && (int)token == 0;

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
