using System.Text.Json;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class NativeBranchObservationChecks
{
    internal static string Key(BattleTurnState state, bool terminal = false) => BattleTurnChecks.Comparable(state, terminal);

    internal static void CheckModuleIdentity(string historicalPath, string currentPath)
    {
        using var historical = ModelJson.ReadFixture(historicalPath);
        using var current = ModelJson.ReadFixture(currentPath);
        Guid historicalModule = NativeBranchIdentity.ObservationModule(historical.RootElement);
        Guid currentModule = NativeBranchIdentity.SourceModule(current.RootElement);
        FixtureValue historicalRequest = historical.RootElement.GetProperty("Request");
        Require(historicalModule == NativeBranchIdentity.HistoricalModule &&
            !historicalRequest.TryGetProperty("SourceGameModuleMvid", out _) &&
            currentModule == new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d"), "Expected original archive modules.");

        FixtureValue first = current.RootElement.GetProperty("RollingDecisions")[0];
        FixtureValue root = first.GetProperty("Root"), action = first.GetProperty("Action");
        bool isEndTurn = first.GetProperty("IsEndTurn").GetBoolean();
        const string reason = "Offline replay of the first recorded native advisor decision.";
        using var currentRequest = NativeFixtureCapture.Capture(new { Schema = 1,
            EvidenceKind = "ModelUnsupportedBranch", SourceGameModuleMvid = currentModule,
            Root = root, Prefix = Array.Empty<object>(), FailedRoot = root,
            Action = action, IsEndTurn = isEndTurn, Reason = reason });
        NativeBranchIdentity.RequireModule(currentRequest.RootElement, currentModule);
        Reject(() => NativeBranchIdentity.RequireModule(historicalRequest, currentModule), "the native module");
        Reject(() => NativeBranchIdentity.RequireModule(currentRequest.RootElement, historicalModule), "the native module");

        // Replay the actual successor and advisor projection from the existing
        // current-module trace through the branch checker in memory.
        using var currentObservation = NativeFixtureCapture.Capture(new { Schema = 1,
            EvidenceKind = "NativeUnsupportedBranchObservation", NativeCompleted = true,
            GameModuleMvid = currentModule, Request = currentRequest.RootElement,
            Rows = new[] { new { Before = root, Action = action, IsEndTurn = isEndTurn, Boundary = true,
                Expected = first.GetProperty("Expected"), Actual = first.GetProperty("Actual"), Outcome = first.GetProperty("Outcome"),
                UnsupportedReason = (string?)null } }, PlayerAdvisor = current.RootElement.GetProperty("PlayerAdvisor") });
        Require(currentObservation.RootElement.GetProperty("Request").ContentEquals(currentRequest.RootElement) &&
            NativeBranchIdentity.ObservationModule(currentObservation.RootElement) == currentModule, "Current observation lost its request pin.");
        Require(Replay(currentObservation.RootElement, reason, expectedGameModule: currentModule).Length == 1,
            "Current native successor did not replay.");
        Reject(() => Replay(currentObservation.RootElement, reason, expectedGameModule: historicalModule), "verifier version");
        Reject(() => Pair(historical.RootElement, currentObservation.RootElement), "changed its game module");

        using var unpinnedCurrent = NativeFixtureCapture.Capture(new { Schema = 1,
            EvidenceKind = "NativeUnsupportedBranchObservation", NativeCompleted = true,
            GameModuleMvid = currentModule, Request = historicalRequest });
        Require(unpinnedCurrent.RootElement.GetProperty("Request").ContentEquals(historicalRequest), "Historical request changed during capture.");
        Reject(() => NativeBranchIdentity.ObservationModule(unpinnedCurrent.RootElement), "the native module");
        Console.WriteLine("NATIVE-BRANCH-MODULE PASS historical=2.2.1 current=2.3.1 replayInputs=1 matchedRequest=exact wrongModule=rejected mixedPair=rejected");
    }

    private static void Reject(Action attempt, string message)
    {
        try { attempt(); }
        catch (Exception error) when ((error is InvalidDataException or InvalidOperationException) &&
            error.Message.Contains(message, StringComparison.OrdinalIgnoreCase)) { return; }
        throw new InvalidOperationException("Expected native branch module failure: " + message);
    }

    internal static FixtureValue[] Replay(FixtureValue fixture, string reason,
        Action<BattleTurnState, BattleTurnState>? historicalBoundaryMismatch = null,
        RoomOutcome boundaryOutcome = RoomOutcome.Exchanged, string? historicalUnsupportedReason = null,
        RoomOutcome? historicalBoundaryOutcome = null,
        Func<BattleTurnState, BattleTurnState>? revalidateHistoricalCapture = null,
        Func<string, string>? normalizeComparison = null,
        Func<BattleTurnState, BattleTurnState, bool>? matchesRequestCapture = null,
        Guid? expectedGameModule = null)
    {
        string CaptureKey(BattleTurnState state, bool terminal = false) => normalizeComparison?.Invoke(Key(state, terminal)) ?? Key(state, terminal);
        Require(!historicalBoundaryOutcome.HasValue || historicalBoundaryMismatch != null,
            "A changed historical boundary outcome requires an explicit mismatch witness.");
        Guid module = NativeBranchIdentity.ObservationModule(fixture);
        Require(module == (expectedGameModule ?? NativeBranchIdentity.HistoricalModule),
            "Native branch game module differs from the verifier version.");
        FixtureValue request = fixture.GetProperty("Request");
        Require(request.GetProperty("Reason").GetString() == reason, "Wrong native rejection boundary.");
        FixtureValue[] prefix = request.GetProperty("Prefix").EnumerateArray().ToArray();
        FixtureValue[] rows = fixture.GetProperty("Rows").EnumerateArray().ToArray();
        Require(rows.Length == prefix.Length + 1 && rows.Length is >= 1 and <= 257, "Incomplete native prefix.");
        BattleTurnState current = request.GetProperty("Root").Deserialize<BattleTurnState>()!;
        Require(current.Spawn.Turn == 0, "Observation must begin at deployment.");
        for (int index = 0; index < rows.Length; index++)
        {
            FixtureValue row = rows[index];
            bool boundary = index == prefix.Length;
            FixtureValue step = boundary ? request : prefix[index];
            bool end = step.GetProperty("IsEndTurn").GetBoolean();
            BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            BattleTurnState actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            Require((matchesRequestCapture?.Invoke(current, before) ?? CaptureKey(current) == CaptureKey(before)) && row.GetProperty("Boundary").GetBoolean() == boundary &&
                row.GetProperty("IsEndTurn").GetBoolean() == end && row.GetProperty("Action").ContentEquals(step.GetProperty("Action")),
                "Native chain/input differs at " + index);
            if (revalidateHistoricalCapture != null) before = revalidateHistoricalCapture(before);
            string immutable = JsonSerializer.Serialize(before, ModelJson.Options);
            PlayCardAction? action = step.GetProperty("Action").Deserialize<PlayCardAction>();
            BattleTurnState predicted;
            RoomOutcome outcome;
            try { predicted = BattlePlannerChecks.Replay(before, [new BattleSearchStep(action, end)], out outcome); }
            catch (InvalidOperationException error) { throw new InvalidOperationException("Native replay failed at " + index + ": " + error.Message, error); }
            bool terminal = boundary && boundaryOutcome is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated;
            string predictedKey = CaptureKey(predicted, terminal), actualKey = CaptureKey(actual, terminal);
            if (outcome != (boundary ? boundaryOutcome : RoomOutcome.Exchanged) || predictedKey != actualKey)
                throw new InvalidOperationException("Native successor differs at " + index + ": " + ModelJson.Difference(predictedKey, actualKey) +
                    "; predicted room actors=" + string.Join("/", predicted.Spawn.Train.Rooms.Select(room => string.Join(",", room.Units.Select(unit => unit.Id)))) +
                    "; native room actors=" + string.Join("/", actual.Spawn.Train.Rooms.Select(room => string.Join(",", room.Units.Select(unit => unit.Id)))));
            FixtureValue expected = row.GetProperty("Expected");
            if (expected.ValueKind == FixtureKind.Null)
                Require(boundary && row.GetProperty("UnsupportedReason").GetString() == (historicalUnsupportedReason ?? reason),
                    "Only the original rejected boundary may omit its prediction.");
            else
            {
                Require(row.GetProperty("UnsupportedReason").ValueKind == FixtureKind.Null &&
                    row.GetProperty("Outcome").GetInt32() == (int)(boundary ? historicalBoundaryOutcome ?? outcome : outcome),
                    "Historical outcome differs at " + index);
                BattleTurnState historical = expected.Deserialize<BattleTurnState>()!;
                string historicalKey = CaptureKey(historical, terminal);
                if (boundary && historicalBoundaryMismatch != null && historicalKey != actualKey)
                    historicalBoundaryMismatch(historical, actual);
                else Require(historicalKey == actualKey, "Historical prediction differs at " + index);
            }
            Require(immutable == JsonSerializer.Serialize(before, ModelJson.Options), "Replay mutated its parent.");
            if (boundary)
            {
                var failedRoot = request.GetProperty("FailedRoot").Deserialize<BattleTurnState>()!;
                Require(matchesRequestCapture?.Invoke(failedRoot, before) ?? CaptureKey(before) == CaptureKey(failedRoot), "Wrong failed root.");
            }
            current = actual;
        }
        FixtureValue overlay = fixture.GetProperty("PlayerAdvisor").GetProperty("Overlay");
        Require(overlay.GetProperty("Captures").GetInt32() > 0 && overlay.GetProperty("PurityFailures").GetInt32() == 0 &&
            overlay.GetProperty("Failures").GetInt32() == 0 && !overlay.GetProperty("InTestScenario").GetBoolean(),
            "Native player projection was not pure/original.");
        return rows;
    }

    internal static void Pair(FixtureValue source, FixtureValue replay, bool originalPredictionPresent = false,
        bool originalPredictionMatched = false, Func<string, string>? normalizeComparison = null,
        Func<BattleTurnState, BattleTurnState, bool>? capturesEquivalent = null)
    {
        string CaptureKey(BattleTurnState state, bool terminal = false) => normalizeComparison?.Invoke(Key(state, terminal)) ?? Key(state, terminal);
        Require(!originalPredictionMatched || originalPredictionPresent, "A matching original prediction must be present.");
        Require(NativeBranchIdentity.ObservationModule(source) == NativeBranchIdentity.ObservationModule(replay),
            "Independent process changed its game module.");
        Require(source.GetProperty("Request").ContentEquals(replay.GetProperty("Request")), "Independent process changed its request.");
        FixtureValue[] first = source.GetProperty("Rows").EnumerateArray().ToArray();
        FixtureValue[] second = replay.GetProperty("Rows").EnumerateArray().ToArray();
        Require(first.Length == second.Length, "Independent process changed its input count.");
        for (int index = 0; index < first.Length; index++)
        {
            foreach (string field in new[] { "Before", "Actual" })
            {
                var original = first[index].GetProperty(field).Deserialize<BattleTurnState>()!;
                var repeated = second[index].GetProperty(field).Deserialize<BattleTurnState>()!;
                Require(capturesEquivalent?.Invoke(original, repeated) ?? CaptureKey(original) == CaptureKey(repeated),
                    "Independent process changed " + field + " at " + index);
            }
            foreach (string field in new[] { "Action", "IsEndTurn", "Boundary" })
                Require(first[index].GetProperty(field).ContentEquals(second[index].GetProperty(field)), "Independent input changed at " + index);
        }
        Require(first[^1].GetProperty("Expected").ValueKind == (originalPredictionPresent ? FixtureKind.Object : FixtureKind.Null) &&
            second[^1].GetProperty("Expected").ValueKind == FixtureKind.Object &&
            second[^1].GetProperty("UnsupportedReason").ValueKind == FixtureKind.Null,
            "Preserve the original rejection and the independently repaired forecast.");
        bool terminal = (RoomOutcome)second[^1].GetProperty("Outcome").GetInt32() is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated;
        if (originalPredictionPresent)
            Require((CaptureKey(first[^1].GetProperty("Expected").Deserialize<BattleTurnState>()!, terminal) ==
                CaptureKey(first[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!, terminal)) == originalPredictionMatched &&
                CaptureKey(second[^1].GetProperty("Expected").Deserialize<BattleTurnState>()!, terminal) ==
                CaptureKey(second[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!, terminal),
                "Preserve the declared original forecast relationship and the independently matching forecast.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
