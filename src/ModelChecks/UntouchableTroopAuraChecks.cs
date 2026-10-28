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

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
