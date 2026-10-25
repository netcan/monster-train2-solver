using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class FeedingSpellRoutingChecks
{
    private const string Reason = "Missing dead unit spawner card routing.";
    private const string CardId = "236215da-c1dd-40b7-8793-27ce327a7e78";
    private static string Key(BattleTurnState state) => BattleTurnChecks.Comparable(state);

    internal static void Native(FixtureValue fixture)
    {
        Require(fixture.GetProperty("Schema").GetInt32() == 1 &&
            fixture.GetProperty("EvidenceKind").GetString() == "NativeUnsupportedBranchObservation" &&
            fixture.GetProperty("NativeCompleted").GetBoolean() &&
            fixture.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4",
            "Missing original feeding-spell observation.");
        FixtureValue request = fixture.GetProperty("Request");
        Require(request.GetProperty("Reason").GetString() == Reason && !request.GetProperty("IsEndTurn").GetBoolean(),
            "Expected the observed feeding-spell routing rejection.");
        FixtureValue[] prefix = request.GetProperty("Prefix").EnumerateArray().ToArray();
        FixtureValue[] rows = fixture.GetProperty("Rows").EnumerateArray().ToArray();
        Require(prefix.Length == 20 && rows.Length == 21, "Missing the complete twenty-input native prefix and final spell.");
        BattleTurnState current = request.GetProperty("Root").Deserialize<BattleTurnState>()!;
        Require(current.Spawn.Turn == 0, "Observation must begin at native deployment.");
        for (int index = 0; index < rows.Length; index++)
        {
            FixtureValue row = rows[index];
            bool boundary = index == prefix.Length;
            BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            BattleTurnState actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            FixtureValue step = boundary ? request : prefix[index];
            bool end = step.GetProperty("IsEndTurn").GetBoolean();
            Require(Key(current) == Key(before) && row.GetProperty("Boundary").GetBoolean() == boundary &&
                row.GetProperty("IsEndTurn").GetBoolean() == end && row.GetProperty("Action").ContentEquals(step.GetProperty("Action")),
                "Native prefix/input differs at " + index);
            PlayCardAction? action = step.GetProperty("Action").Deserialize<PlayCardAction>();
            string immutable = JsonSerializer.Serialize(before, ModelJson.Options);
            BattleTurnState predicted = BattlePlannerChecks.Replay(before, [new BattleSearchStep(action, end)], out RoomOutcome outcome);
            Require(outcome == RoomOutcome.Exchanged && Key(predicted) == Key(actual),
                "Native feeding-spell successor differs at " + index + ": " + ModelJson.Difference(Key(predicted), Key(actual)));
            FixtureValue historical = row.GetProperty("Expected");
            if (historical.ValueKind == FixtureKind.Null)
                Require(boundary && row.GetProperty("UnsupportedReason").GetString() == Reason,
                    "Only the old rejected boundary may lack its prediction.");
            else Require(Key(historical.Deserialize<BattleTurnState>()!) == Key(actual) &&
                row.GetProperty("UnsupportedReason").ValueKind == FixtureKind.Null &&
                row.GetProperty("Outcome").GetInt32() == (int)outcome, "Historical prediction changed at " + index);
            Require(immutable == JsonSerializer.Serialize(before, ModelJson.Options), "Replay mutated its native parent.");
            if (boundary)
            {
                Require(Key(before) == Key(request.GetProperty("FailedRoot").Deserialize<BattleTurnState>()!), "Wrong boundary root.");
                CombatContext old = before.Spawn.Train.Context!, next = actual.Spawn.Train.Context!;
                Require(old.FindCard(action!.CardInstanceId)!.DataId == CardId && before.Spawn.Turn == 4 &&
                    before.PlayRules!.Cards.Single(card => card.DataId == CardId).Effects.Single().Type == "FeederRules",
                    "The boundary is not the original feeding spell.");
                int[] eaten = [32, 36, 39];
                Require(old.OtherPiles!.Single(pile => pile.Name == "Eaten").Cards.Count == 0 &&
                    next.OtherPiles!.Single(pile => pile.Name == "Eaten").Cards.Select(card => card.InstanceId).SequenceEqual(eaten) &&
                    next.OtherPiles.Single(pile => pile.Name == "Exhausted").Cards.Select(card => card.InstanceId).SequenceEqual([21, 20, 29, 10, 8]) &&
                    next.OtherPiles.Single(pile => pile.Name == "Standby").Cards.Select(card => card.InstanceId).SequenceEqual([31, 2]),
                    "Consumed carriers or attached equipment have incorrect native destinations/order.");
                foreach (int id in eaten)
                {
                    Require(before.Spawn.Train.Rooms.SelectMany(room => room.Units).Count(unit => unit.SpawnerCardId == id) == 1 &&
                        actual.Spawn.Train.Rooms.SelectMany(room => room.Units).All(unit => unit.SpawnerCardId != id) &&
                        next.OtherPiles.Where(pile => pile.Name != "Eaten").All(pile => pile.Cards.All(card => card.InstanceId != id)) &&
                        next.Cards.Hand.Concat(next.Cards.Draw).Concat(next.Cards.Discard).All(card => card.InstanceId != id) &&
                        next.Statistics!.Value(id, "TimesExhausted") == old.Statistics!.Value(id, "TimesExhausted"),
                        "A consumed carrier was duplicated, kept alive or exhausted twice.");
                }
                Require(BattleActionModel.EnumerateActions(before).SupportedActions.Any(candidate =>
                    JsonSerializer.Serialize(candidate) == JsonSerializer.Serialize(action)), "Feeding spell is still absent from legal candidates.");
            }
            current = actual;
        }
        FixtureValue overlay = fixture.GetProperty("PlayerAdvisor").GetProperty("Overlay");
        Require(overlay.GetProperty("Captures").GetInt32() > 0 && overlay.GetProperty("PurityFailures").GetInt32() == 0 &&
            overlay.GetProperty("Failures").GetInt32() == 0 && !overlay.GetProperty("InTestScenario").GetBoolean(),
            "Original player capture was not pure.");
        Console.WriteLine("FEEDING-SPELL-ROUTING PASS:21 native inputs/full state/RNG; three eaten carriers, equipment exhaustion order, no double exhaustion, immutable roots.");
    }

    internal static void Pair(FixtureValue source, FixtureValue replay)
    {
        Native(source); Native(replay);
        Require(source.GetProperty("Request").ContentEquals(replay.GetProperty("Request")), "Fresh process changed the diagnostic request.");
        FixtureValue[] first = source.GetProperty("Rows").EnumerateArray().ToArray();
        FixtureValue[] second = replay.GetProperty("Rows").EnumerateArray().ToArray();
        for (int index = 0; index < first.Length; index++)
        {
            foreach (string field in new[] { "Before", "Actual" })
                Require(Key(first[index].GetProperty(field).Deserialize<BattleTurnState>()!) ==
                    Key(second[index].GetProperty(field).Deserialize<BattleTurnState>()!), "Fresh process changed " + field + " at " + index);
            foreach (string field in new[] { "Action", "IsEndTurn", "Boundary" })
                Require(first[index].GetProperty(field).ContentEquals(second[index].GetProperty(field)), "Fresh input differs at " + index);
        }
        Require(first[^1].GetProperty("Expected").ValueKind == FixtureKind.Null &&
            second[^1].GetProperty("Expected").ValueKind == FixtureKind.Object &&
            second[^1].GetProperty("UnsupportedReason").ValueKind == FixtureKind.Null,
            "The original rejection and fresh supported prediction must both be retained.");
        Console.WriteLine("FEEDING-SPELL-PAIR PASS: original rejection preserved; repaired prediction matches all21 native inputs in a fresh process; component observation only.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
