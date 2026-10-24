using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class EndlessSpellRoutingChecks
{
    internal static void Pair(FixtureValue source, FixtureValue replay)
    {
        Native(source); Native(replay);
        Require(source.GetProperty("Request").ContentEquals(replay.GetProperty("Request")), "Independent process changed its failure request.");
        FixtureValue[] first = source.GetProperty("Rows").EnumerateArray().ToArray();
        FixtureValue[] second = replay.GetProperty("Rows").EnumerateArray().ToArray();
        Require(first.Length == second.Length, "Independent process changed its input count.");
        for (int i = 0; i < first.Length; i++)
        {
            foreach (string field in new[] { "Before", "Actual" })
                Require(Key(first[i].GetProperty(field).Deserialize<BattleTurnState>()!) ==
                    Key(second[i].GetProperty(field).Deserialize<BattleTurnState>()!), "Independent process changed " + field + " at " + i);
            foreach (string field in new[] { "Action", "IsEndTurn", "Boundary" })
                Require(first[i].GetProperty(field).ContentEquals(second[i].GetProperty(field)), "Independent process changed " + field + " at " + i);
        }
        Require(first[^1].GetProperty("Expected").ValueKind == FixtureKind.Null &&
            second[^1].GetProperty("Expected").ValueKind == FixtureKind.Object &&
            second[^1].GetProperty("UnsupportedReason").ValueKind == FixtureKind.Null,
            "Expected the original rejected observation followed by a supported fresh replay.");
        Console.WriteLine("ENDLESS-SPELL-PAIR PASS: original rejection retained; repaired prediction and all 24 native roots/inputs/successors/RNG match in a fresh process.");
    }

    internal static void Native(FixtureValue fixture)
    {
        Require(fixture.GetProperty("Schema").GetInt32() == 1 &&
            fixture.GetProperty("EvidenceKind").GetString() == "NativeUnsupportedBranchObservation" &&
            fixture.GetProperty("NativeCompleted").GetBoolean() &&
            fixture.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4",
            "Missing original native branch observation.");
        FixtureValue request = fixture.GetProperty("Request");
        Require(request.GetProperty("Reason").GetString() == "Missing dead spawner routing during the trigger queue.",
            "This checker requires the captured dead-spawner rejection.");
        FixtureValue[] prefix = request.GetProperty("Prefix").EnumerateArray().ToArray();
        FixtureValue[] rows = fixture.GetProperty("Rows").EnumerateArray().ToArray();
        Require(rows.Length == prefix.Length + 1 && rows.Length is > 1 and <= 257, "Native prefix is incomplete.");
        BattleTurnState current = request.GetProperty("Root").Deserialize<BattleTurnState>()!;
        Require(current.Spawn.Turn == 0, "Native prefix did not start at deployment.");
        for (int index = 0; index < rows.Length; index++)
        {
            FixtureValue row = rows[index];
            bool boundary = index == prefix.Length;
            BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            BattleTurnState actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            Require(Key(current) == Key(before) && row.GetProperty("Boundary").GetBoolean() == boundary,
                "Native branch chain differs at " + index);
            FixtureValue step = boundary ? request : prefix[index];
            bool end = !boundary && step.GetProperty("IsEndTurn").GetBoolean();
            Require(row.GetProperty("Action").ContentEquals(step.GetProperty("Action")) &&
                row.GetProperty("IsEndTurn").GetBoolean() == end, "Native input changed at " + index);
            PlayCardAction? action = end ? null : step.GetProperty("Action").Deserialize<PlayCardAction>();
            string immutable = JsonSerializer.Serialize(before, ModelJson.Options);
            BattleTurnState predicted = BattlePlannerChecks.Replay(before, [new BattleSearchStep(action, end)], out RoomOutcome outcome);
            Require(outcome == RoomOutcome.Exchanged && Key(predicted) == Key(actual),
                "Native branch successor differs at " + index + ": " + ModelJson.Difference(Key(predicted), Key(actual)));
            FixtureValue oldExpected = row.GetProperty("Expected");
            if (oldExpected.ValueKind == FixtureKind.Null)
                Require(boundary && row.GetProperty("UnsupportedReason").GetString() == request.GetProperty("Reason").GetString(),
                    "Only the original rejected boundary may omit its historical prediction.");
            else Require(Key(oldExpected.Deserialize<BattleTurnState>()!) == Key(actual) &&
                row.GetProperty("UnsupportedReason").ValueKind == FixtureKind.Null &&
                row.GetProperty("Outcome").GetInt32() == (int)outcome, "Historical supported forecast differs at " + index);
            Require(immutable == JsonSerializer.Serialize(before, ModelJson.Options), "Branch replay mutated its root.");
            if (boundary)
            {
                Require(Key(before) == Key(request.GetProperty("FailedRoot").Deserialize<BattleTurnState>()!), "Wrong failed root.");
                CombatUnit victim = before.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == action!.TargetUnitId);
                int cardId = victim.SpawnerCardId;
                Require(victim.Team == CombatTeam.Player && cardId > 0 && victim.Statuses.Any(status => status.Id == "endless" && status.Stacks > 0) &&
                    actual.Spawn.Train.Rooms.SelectMany(room => room.Units).All(unit => unit.Id != victim.Id), "No Endless carrier death.");
                CombatContext old = before.Spawn.Train.Context!, next = actual.Spawn.Train.Context!;
                Require(next.Cards.Draw.Count(card => card.InstanceId == cardId) == 1 &&
                    next.Cards.Draw.Select(card => card.InstanceId).SequenceEqual(old.Cards.Draw.Select(card => card.InstanceId).Append(cardId)) &&
                    next.Cards.Hand.All(card => card.InstanceId != cardId) && next.Cards.Discard.All(card => card.InstanceId != cardId) &&
                    next.OtherPiles!.All(pile => pile.Cards.All(card => card.InstanceId != cardId)) &&
                    next.Statistics!.Value(cardId, "TimesExhausted") == old.Statistics!.Value(cardId, "TimesExhausted"),
                    "Native Endless spell death duplicated/exhausted its card or changed draw order/statistics.");
                Require(JsonSerializer.Serialize(old.FindCard(cardId)!.Temporary, ModelJson.Options) ==
                    JsonSerializer.Serialize(next.FindCard(cardId)!.Temporary, ModelJson.Options), "Endless lost its temporary upgrades.");
                Require(BattleActionModel.EnumerateActions(before).SupportedActions.Any(candidate =>
                    JsonSerializer.Serialize(candidate) == JsonSerializer.Serialize(action)), "Repaired branch is absent from candidate enumeration.");
            }
            current = actual;
        }
        FixtureValue overlay = fixture.GetProperty("PlayerAdvisor").GetProperty("Overlay");
        Require(overlay.GetProperty("Captures").GetInt32() > 0 && overlay.GetProperty("PurityFailures").GetInt32() == 0 &&
            overlay.GetProperty("Failures").GetInt32() == 0 && !overlay.GetProperty("InTestScenario").GetBoolean(),
            "Native player projection is not pure/original.");
        Console.WriteLine($"ENDLESS-SPELL-ROUTING PASS: {rows.Length} native inputs, complete state/RNG, draw order, retained upgrades, no exhaustion and immutable branches.");
    }
    private static string Key(BattleTurnState state) => BattleTurnChecks.Comparable(state);
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
