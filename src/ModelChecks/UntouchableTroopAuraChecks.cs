using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class UntouchableTroopAuraChecks
{
    private const string Reason = "Untouchable troop-aura target removal requires native calibration.";
    internal static void Run(FixtureValue fixture)
    {
        FixtureValue[] actions = fixture.GetProperty("Actions").EnumerateArray().ToArray();
        Require(actions.Length == 48, "Expected the observed original action prefix.");
        FixtureValue observed = actions[47];
        BattleTurnState before = observed.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = observed.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction action = observed.GetProperty("Action").Deserialize<PlayCardAction>()!;
        CombatUnit oldTarget = before.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == action.TargetUnitId);
        CombatUnit newTarget = actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == action.TargetUnitId);
        CardToken card = before.Spawn.Train.Context!.Cards.Hand.Single(item => item.InstanceId == action.CardInstanceId);
        Require(before.PlayRules!.Cards.Single(item => item.DataId == card.DataId).AssetKey == "StasisCrystal" &&
            oldTarget.AssetKey == "MonsterFunguy" && oldTarget.Status("horde")?.Stacks == 4 &&
            oldTarget.Health == 19 && oldTarget.MaxHealth == 28 && newTarget.Health == 16 && newTarget.MaxHealth == 16 &&
            newTarget.Status("untouchable") != null, "Native troop-aura counterexample changed.");
        string original = JsonSerializer.Serialize(before);
        BattleActionResult rejected = BattleActionModel.PlayCard(before, action);
        Require(!rejected.Supported && rejected.Reason == Reason &&
            JsonSerializer.Serialize(before) == original, "Uncalibrated targetability change was not rejected without mutation.");
        FixtureValue previous = actions[46];
        BattleTurnState priorRoot = previous.GetProperty("Before").Deserialize<BattleTurnState>()!;
        PlayCardAction priorAction = previous.GetProperty("Action").Deserialize<PlayCardAction>()!;
        BattleActionResult prior = BattleActionModel.PlayCard(priorRoot, priorAction);
        using var keys = new BattleStateKeys();
        Require(prior.Supported && keys.Key(prior.State!) == keys.Key(previous.GetProperty("Actual").Deserialize<BattleTurnState>()!),
            "The preceding independently observed action was incorrectly blocked or changed.");
        Console.WriteLine("UNTOUCHABLE-TROOP-AURA PASS: native counterexample rejected; prior action and immutable root retained.");
    }

    internal static void Independent(FixtureValue original, FixtureValue observation)
    {
        Run(original);
        Require(observation.GetProperty("EvidenceKind").GetString() == "NativeUnsupportedBranchObservation" &&
            observation.GetProperty("NativeCompleted").GetBoolean() &&
            observation.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4", "Missing native branch observation.");
        FixtureValue request = observation.GetProperty("Request");
        FixtureValue[] prefix = request.GetProperty("Prefix").EnumerateArray().ToArray();
        FixtureValue[] rows = observation.GetProperty("Rows").EnumerateArray().ToArray();
        Require(request.GetProperty("Reason").GetString() == Reason && prefix.Length == 55 && rows.Length == 56,
            "Wrong branch or incomplete native prefix.");
        Require(original.GetProperty("RollingDecisions")[55].GetProperty("Action").ContentEquals(request.GetProperty("Action")),
            "Independent native action differs from the original player action.");
        BattleTurnState current = request.GetProperty("Root").Deserialize<BattleTurnState>()!;
        for (int index = 0; index < rows.Length; index++)
        {
            FixtureValue row = rows[index];
            bool boundary = index == prefix.Length;
            FixtureValue step = boundary ? request : prefix[index];
            BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            BattleTurnState actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            Require(NativeBranchObservationChecks.Key(current) == NativeBranchObservationChecks.Key(before) &&
                row.GetProperty("Boundary").GetBoolean() == boundary &&
                row.GetProperty("IsEndTurn").ContentEquals(step.GetProperty("IsEndTurn")) &&
                row.GetProperty("Action").ContentEquals(step.GetProperty("Action")), "Native chain differs at " + index);
            string immutable = JsonSerializer.Serialize(before);
            PlayCardAction? action = step.GetProperty("Action").Deserialize<PlayCardAction>();
            if (boundary)
            {
                BattleActionResult rejected = BattleActionModel.PlayCard(before, action!);
                Require(!rejected.Supported && rejected.Reason == Reason &&
                    row.GetProperty("UnsupportedReason").GetString() == Reason &&
                    row.GetProperty("Expected").ValueKind == FixtureKind.Null, "Native boundary was not protected.");
                BattleTurnState originalActual = original.GetProperty("RollingDecisions")[55].GetProperty("Actual").Deserialize<BattleTurnState>()!;
                string originalKey = NativeBranchObservationChecks.Key(originalActual);
                string actualKey = NativeBranchObservationChecks.Key(actual);
                Require(originalKey == actualKey,
                    "Independent native boundary differs from the original observed state or RNG: " + ModelJson.Difference(originalKey, actualKey));
            }
            else
            {
                BattleTurnState predicted = BattlePlannerChecks.Replay(before,
                    [new BattleSearchStep(action, step.GetProperty("IsEndTurn").GetBoolean())], out RoomOutcome outcome);
                Require(outcome == RoomOutcome.Exchanged && NativeBranchObservationChecks.Key(predicted) == NativeBranchObservationChecks.Key(actual),
                    "Native successor differs at " + index);
            }
            Require(immutable == JsonSerializer.Serialize(before), "Native replay mutated its parent at " + index);
            current = actual;
        }
        FixtureValue overlay = observation.GetProperty("PlayerAdvisor").GetProperty("Overlay");
        Require(overlay.GetProperty("Captures").GetInt32() > 0 && overlay.GetProperty("PurityFailures").GetInt32() == 0 &&
            overlay.GetProperty("Failures").GetInt32() == 0 && !overlay.GetProperty("InTestScenario").GetBoolean(),
            "Player capture failed or changed native state.");
        Console.WriteLine("UNTOUCHABLE-TROOP-AURA-NATIVE PASS: 55 full successors/RNG, independent boundary, guard and immutable roots.");
    }

    internal static void TracePair(FixtureValue original, FixtureValue first, FixtureValue second)
    {
        Independent(original, first);
        Independent(original, second);
        FixtureValue[] a = first.GetProperty("TroopTargetability").EnumerateArray().ToArray();
        FixtureValue[] b = second.GetProperty("TroopTargetability").EnumerateArray().ToArray();
        Require(a.Length == 7 && b.Length == a.Length, "Unexpected troop targetability operation count.");
        for (int index = 0; index < a.Length; index++)
        {
            Require(a[index].GetProperty("Completed").GetBoolean() && b[index].GetProperty("Completed").GetBoolean(),
                "Incomplete native troop operation at " + index);
            foreach (string side in new[] { "Before", "After" })
            foreach (string field in new[] { "Room", "Preview", "Cached", "Units" })
                Require(a[index].GetProperty(side).GetProperty(field).ContentEquals(b[index].GetProperty(side).GetProperty(field)),
                    "Independent troop operation differs at " + index + ": " + side + "." + field);
            Require(a[index].GetProperty("Before").GetProperty("Preview").GetBoolean() == (index < 6) &&
                a[index].GetProperty("After").GetProperty("Preview").GetBoolean() == (index < 6),
                "Unexpected troop operation preview phase at " + index);
        }
        FixtureValue previewBefore = a[0].GetProperty("Before"), previewAfter = a[0].GetProperty("After");
        FixtureValue realBefore = a[6].GetProperty("Before"), realAfter = a[6].GetProperty("After");
        Require(previewBefore.GetProperty("Cached").ContentEquals(previewAfter.GetProperty("Cached")) &&
            !realBefore.GetProperty("Cached").ContentEquals(realAfter.GetProperty("Cached")), "Native cache transition changed.");
        FixtureValue pu = previewBefore.GetProperty("Units")[0], pv = previewAfter.GetProperty("Units")[0];
        FixtureValue ru = realBefore.GetProperty("Units")[0], rv = realAfter.GetProperty("Units")[0];
        Require(pu.GetProperty("Id").GetInt32() == 46 && pu.GetProperty("Horde").GetInt32() == 7 &&
            pu.GetProperty("Health").GetInt32() == 40 && pu.GetProperty("MaxHealth").GetInt32() == 49 &&
            pv.GetProperty("Health").GetInt32() == 28 && pv.GetProperty("MaxHealth").GetInt32() == 28 &&
            ru.GetProperty("Id").GetInt32() == 46 && ru.GetProperty("Horde").GetInt32() == 4 &&
            ru.GetProperty("Health").GetInt32() == 19 && ru.GetProperty("MaxHealth").GetInt32() == 28 &&
            rv.GetProperty("Health").GetInt32() == 16 && rv.GetProperty("MaxHealth").GetInt32() == 16 &&
            ru.GetProperty("Untouchable").GetInt32() == 1 && rv.GetProperty("Untouchable").GetInt32() == 1,
            "Native preview or actual target health differs.");
        Console.WriteLine("UNTOUCHABLE-TROOP-TRACE PASS: two isolated traces, six preview operations and one real cache removal.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
