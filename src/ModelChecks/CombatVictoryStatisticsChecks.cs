using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class CombatVictoryStatisticsChecks
{
    internal static void Native(FixtureValue fixture, FixtureValue? replay = null, FixtureValue? retention = null)
    {
        var rows = fixture.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        Require(fixture.GetProperty("NativeWon").GetBoolean() && fixture.GetProperty("CaptureFailures").GetInt32() == 0 &&
            fixture.GetProperty("Mismatches").GetInt32() == 3 && fixture.GetProperty("Unsupported").GetInt32() == 0 &&
            fixture.GetProperty("Pending").GetInt32() == 0 && rows.Length == 37 && rows[^1].GetProperty("IsEndTurn").GetBoolean() &&
            rows[^1].GetProperty("Outcome").GetInt32() == (int)RoomOutcome.BattleWon,
            "Missing37-input native combat victory counterexample.");
        var before = rows[^1].GetProperty("Root").Deserialize<BattleTurnState>()!;
        var historical = rows[^1].GetProperty("Expected").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var oldStats = historical.Spawn.Train.Context!.Statistics!;
        var newStats = actual.Spawn.Train.Context!.Statistics!;
        var terminalRoom = fixture.GetProperty("Stages").EnumerateArray().Single(stage =>
            stage.GetProperty("Kind").GetString() == "Room" && stage.GetProperty("Predicted").GetProperty("Outcome").GetInt32() == (int)RoomOutcome.BattleWon);
        var boss = terminalRoom.GetProperty("Before").Deserialize<RoomCombatState>()!.Units.Single(unit => unit.IsBoss == true);
        Require(boss.Status("unstable")?.Stacks == 5 && terminalRoom.GetProperty("Predicted").GetProperty("Events").EnumerateArray()
            .Any(item => item.GetProperty("Kind").GetString() == "Death" && item.GetProperty("Actor").GetInt32() == 0 &&
                item.GetProperty("Target").GetInt32() == boss.Id),
            "Missing the unstable boss's null-source sacrifice death.");
        Require(before.Spawn.Turn == 4 && historical.Spawn.Train.Context.KillCamActivated == true &&
            actual.Spawn.Train.Context.KillCamActivated == true && oldStats.TrackedCards.Count == 24 &&
            oldStats.Values.Count == 424 && newStats.TrackedCards.Count == 15 && newStats.Values.Count == 293 &&
            oldStats.TrackedCards.Except(newStats.TrackedCards).SequenceEqual(Enumerable.Range(17, 9)) &&
            newStats.TrackedCards.SequenceEqual(newStats.DeckCards!) &&
            JsonSerializer.Serialize(oldStats.RefreshDeckAfterCardTerminal(), ModelJson.Options) ==
                JsonSerializer.Serialize(newStats, ModelJson.Options),
            "The old native mismatch must be exactly nine generated infusion statistic entries.");
        using var keys = new BattleStateKeys();
        var root = rows[0].GetProperty("Root").Deserialize<BattleTurnState>()!;
        string immutable = keys.Key(root);
        var current = root;
        for (int i = 0; i < rows.Length; i++)
        {
            var row = rows[i];
            var captured = row.GetProperty("Root").Deserialize<BattleTurnState>()!;
            var expected = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            bool terminal = i == rows.Length - 1;
            Require(NativeBranchObservationChecks.Key(current) == NativeBranchObservationChecks.Key(captured),
                "Native victory input root differs at" + i);
            var step = new BattleSearchStep(row.GetProperty("Action").Deserialize<PlayCardAction>(),
                row.GetProperty("IsEndTurn").GetBoolean());
            current = BattlePlannerChecks.Replay(current, [step], out var outcome);
            Require((int)outcome == row.GetProperty("Outcome").GetInt32() &&
                NativeBranchObservationChecks.Key(current, terminal) == NativeBranchObservationChecks.Key(expected, terminal),
                "Native victory complete successor/RNG differs at" + i);
            if (!terminal)
                Require(NativeBranchObservationChecks.Key(row.GetProperty("Expected").Deserialize<BattleTurnState>()!) ==
                    NativeBranchObservationChecks.Key(expected), "The original nonterminal forecast differs at" + i);
        }
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            var result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && result.Outcome == RoomOutcome.BattleWon &&
                NativeBranchObservationChecks.Key(result.State!, true) == NativeBranchObservationChecks.Key(actual, true),
                "Detached victory statistics differ.");
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options) && keys.Key(root) == immutable,
            "Victory statistic refresh changed its parent.");
        if (replay.HasValue)
        {
            var fresh = NativeBranchObservationChecks.Replay(replay.Value,
                "Native combat victory refreshes statistics after clearing battle piles.", boundaryOutcome: RoomOutcome.BattleWon);
            Require(fresh.Length == rows.Length && fresh[^1].GetProperty("Expected").ValueKind == FixtureKind.Object,
                "Missing independent37-input repaired victory forecast.");
            for (int i = 0; i < rows.Length; i++)
            {
                bool terminal = i == rows.Length - 1;
                Require(rows[i].GetProperty("Action").ContentEquals(fresh[i].GetProperty("Action")) &&
                    rows[i].GetProperty("IsEndTurn").GetBoolean() == fresh[i].GetProperty("IsEndTurn").GetBoolean() &&
                    NativeBranchObservationChecks.Key(rows[i].GetProperty("Root").Deserialize<BattleTurnState>()!) ==
                        NativeBranchObservationChecks.Key(fresh[i].GetProperty("Before").Deserialize<BattleTurnState>()!) &&
                    NativeBranchObservationChecks.Key(rows[i].GetProperty("Actual").Deserialize<BattleTurnState>()!, terminal) ==
                        NativeBranchObservationChecks.Key(fresh[i].GetProperty("Actual").Deserialize<BattleTurnState>()!, terminal),
                    "Independent native victory input/full state/RNG differs at" + i);
            }
        }
        // Whole-turn equality alone can hide stale shared context in lower-level
        // room/train results. Verify every native train phase independently too.
        TrainCombatChecks.Native(fixture);
        if (retention.HasValue) Retention(retention.Value);
        Console.WriteLine("COMBAT-VICTORY-STATISTICS PASS:37 native inputs/full state/forecast/RNG and native train phases; preserved424/24 vs293/15 counterexample; exactly nine generated infusion entries removed, permanent stats retained; four immutable branches; independentPair=" + replay.HasValue);
    }

    private static bool HasUiRefresh(FixtureValue row) =>
        row.TryGetProperty("TerminalUiStatisticsRefresh", out var boundary) && boundary.ValueKind == FixtureKind.Object;

    internal static void TerminalUiNative(FixtureValue fixture, FixtureValue? retention = null)
    {
        Require(fixture.GetProperty("GameVersion").GetString() == "2.3.1" &&
            fixture.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            fixture.GetProperty("NativeWon").GetBoolean() && fixture.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            fixture.GetProperty("TerminalCaptureBoundary").GetString() == "AfterStopCombatLoop" &&
            new[] { "CaptureFailures", "Unsupported", "Pending" }.All(field => fixture.GetProperty(field).GetInt32() == 0),
            "Incomplete current terminal card-text statistics witness.");
        FixtureValue[] stages = fixture.GetProperty("Stages").EnumerateArray().Where(HasUiRefresh).ToArray();
        FixtureValue train = fixture.GetProperty("TrainPhases").EnumerateArray().Single(HasUiRefresh);
        FixtureValue turn = fixture.GetProperty("Turns").EnumerateArray().Single(HasUiRefresh);
        Require(stages.Length == 2 && stages.Select(row => row.GetProperty("Kind").GetString()).Order().SequenceEqual(new[] { "Exchange", "Room" }) &&
            train.GetProperty("Kind").GetString() == "Combat" && (RoomOutcome)turn.GetProperty("ActualOutcome").GetInt32() == RoomOutcome.BattleWon &&
            fixture.GetProperty("Mismatches").GetInt32() == 4 && stages.Append(train).Append(turn).All(row =>
                row.GetProperty("Difference").ValueKind == FixtureKind.String &&
                row.GetProperty("TerminalUiStatisticsRefresh").ContentEquals(turn.GetProperty("TerminalUiStatisticsRefresh"))),
            "The raw room, exchange, train and turn rows must share exactly one observed UI refresh and retain their diagnostic differences.");

        foreach (FixtureValue row in stages)
        {
            RoomCombatState before = row.GetProperty("Before").Deserialize<RoomCombatState>()!;
            RoomCombatState actual = row.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            int[]? pending = row.GetProperty("PendingPhysicalDeaths").Deserialize<int[]>();
            var external = row.GetProperty("ExternalTargets").EnumerateArray().Select(target =>
                (target.GetProperty("RoomIndex").GetInt32(), target.GetProperty("Unit").Deserialize<CombatUnit>()!)).ToArray();
            CombatUnit? pyre = row.GetProperty("ExternalPyre").Deserialize<CombatUnit>();
            bool exchange = row.GetProperty("Kind").GetString() == "Exchange";
            RoomCombatResult modeled = exchange
                ? RoomCombatModel.Exchange(before, externalTargets: external, pendingPhysicalDeathIds: pending)
                : RoomCombatModel.Resolve(before, externalPyre: pyre, externalPyreRoomIndex: row.GetProperty("ExternalPyreRoomIndex").GetInt32(),
                    externalTargets: external, relentlessDecisions: RecordedBattleChecks.Decisions(row));
            Require(modeled.Supported && modeled.Outcome == (exchange ? RoomOutcome.Exchanged : RoomOutcome.BattleWon) &&
                Equal(modeled.State!.Units, actual.Units),
                "Native terminal room units or outcome differ.");
            if (pyre != null)
                Require(Equal(modeled.RetainedUnits.Single(unit => unit.Id == pyre.Id), row.GetProperty("ActualExternalPyre").Deserialize<CombatUnit>()),
                    "Native terminal external Pyre differs.");
            FixtureValue actualTargets = row.GetProperty("ActualExternalTargets");
            foreach (var target in actualTargets.ValueKind == FixtureKind.Array ? actualTargets.EnumerateArray().ToArray() : Array.Empty<FixtureValue>())
            {
                CombatUnit actualTarget = target.GetProperty("Unit").Deserialize<CombatUnit>()!;
                Require(Equal(modeled.RetainedUnits.Single(unit => unit.Id == actualTarget.Id), actualTarget), "Native terminal external target differs.");
            }
            CombatContext adjusted = ReplayUiRefresh(row, row.GetProperty("Turn").GetInt32(), before.Context!, modeled.State!.Context!, actual.Context!);
            Require(Equal(BattleTurnChecks.ComparableContext(adjusted), BattleTurnChecks.ComparableContext(actual.Context)),
                "Native terminal room context differs after the observed UI refresh.");
        }

        TrainCombatState trainBefore = train.GetProperty("Before").Deserialize<TrainCombatState>()!;
        TrainCombatState trainActual = train.GetProperty("Actual").Deserialize<TrainCombatState>()!;
        var roomRules = train.TryGetProperty("RoomRules", out FixtureValue rules) ? rules.Deserialize<RoomPlayRule[]>() : null;
        TrainCombatResult trainModeled = TrainCombatModel.ResolveCombat(trainBefore, RecordedBattleChecks.Decisions(train), roomRules);
        Require(trainModeled.Supported && trainModeled.Outcome == RoomOutcome.BattleWon &&
            Equal(trainModeled.State!.Rooms.Select(room => new { room.RoomIndex, room.Units }),
                trainActual.Rooms.Select(room => new { room.RoomIndex, room.Units })), "Native terminal train units or outcome differ.");
        CombatContext trainAdjusted = ReplayUiRefresh(train, train.GetProperty("Turn").GetInt32(), trainBefore.Context!, trainModeled.State!.Context!, trainActual.Context!);
        Require(Equal(BattleTurnChecks.ComparableContext(trainAdjusted), BattleTurnChecks.ComparableContext(trainActual.Context)),
            "Native terminal train context differs after the observed UI refresh.");

        BattleTurnState turnBefore = turn.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState turnActual = turn.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        BattleTurnResult turnModeled = BattleTurnModel.EndTurn(turnBefore, RecordedBattleChecks.Decisions(turn));
        Require(turnModeled.Supported && turnModeled.Outcome == RoomOutcome.BattleWon, "Native terminal turn outcome differs.");
        string modelBeforeAdjustment = BattleTurnChecks.Comparable(turnModeled.State!, terminal: true);
        CombatContext turnAdjusted = ReplayUiRefresh(turn, turnBefore.Spawn.Turn, turnBefore.Spawn.Train.Context!,
            turnModeled.State!.Spawn.Train.Context!, turnActual.Spawn.Train.Context!);
        JsonNode modeledTurn = JsonNode.Parse(modelBeforeAdjustment)!;
        modeledTurn["Context"]!["Statistics"] = JsonSerializer.SerializeToNode(turnAdjusted.Statistics);
        Require(modeledTurn.ToJsonString() == BattleTurnChecks.Comparable(turnActual, terminal: true) &&
            BattleTurnChecks.Comparable(turnModeled.State, terminal: true) == modelBeforeAdjustment,
            "Native terminal turn differs after the observed UI refresh or its raw prediction changed.");

        if (retention.HasValue) TerminalUiRetention(retention.Value);
        Console.WriteLine("TERMINAL-UI-STATISTICS PASS: one native after-clear damage-text event, four independently replayed raw mismatch rows, exact permanent-deck refresh, retained control=" + retention.HasValue);
    }

    private static CombatContext ReplayUiRefresh(FixtureValue row, int turn, CombatContext before, CombatContext modeled, CombatContext actual)
    {
        FixtureValue boundary = row.GetProperty("TerminalUiStatisticsRefresh");
        FixtureValue entry = boundary.GetProperty("Event");
        int start = boundary.GetProperty("StartSequence").GetInt32(), end = boundary.GetProperty("EndSequence").GetInt32();
        int[] prior = entry.GetProperty("BeforeStoredCards").Deserialize<int[]>()!;
        int[] post = entry.GetProperty("AfterStoredCards").Deserialize<int[]>()!;
        int[] deck = entry.GetProperty("DeckCards").Deserialize<int[]>()!;
        string[] caller = entry.GetProperty("Caller").Deserialize<string[]>()!;
        int query = Array.IndexOf(caller, "CardStatistics.GetStatValue");
        int text = Array.IndexOf(caller, "CardTraitScalingAddDamage.GetCurrentEffectText");
        int hand = Array.IndexOf(caller, "HandUI.UpdateCardUIAtEndOfFrame");
        int late = Array.IndexOf(caller, "CardUI.LateUpdate");
        int cardId = entry.GetProperty("CardId").GetInt32();
        string cardDataId = entry.GetProperty("CardDataId").GetString()!;
        Require(start >= 0 && end == start + 1 && entry.GetProperty("Sequence").GetInt32() == end &&
            entry.GetProperty("Turn").GetInt32() == turn && entry.GetProperty("Frame").GetInt32() > 0 &&
            entry.GetProperty("Phase").GetString() == actual.EnergyState?.Phase && actual.EnergyState?.Phase == "Combat" &&
            query >= 0 && text > query && hand > text && late > hand &&
            before.Cards.Hand.Any(card => card.InstanceId == cardId && card.DataId == cardDataId) &&
            before.FindCard(cardId)?.MaskDescriptor?.BaseTraits.Any(trait => trait.RuntimeType == "CardTraitScalingAddDamage") == true &&
            actual.FindCard(cardId)?.DataId == cardDataId && before.KillCamActivated == false &&
            modeled.KillCamActivated == true && actual.KillCamActivated == true && modeled.CardInstances?.Count == 0 && actual.CardInstances?.Count == 0,
            "Terminal UI query does not belong to this exact combat boundary.");
        BattleStatistics stats = modeled.Statistics!;
        Require(deck.Length > 0 && prior.Except(deck).Any() && post.SequenceEqual(deck) &&
            before.Statistics!.StoredCards!.SequenceEqual(prior) && stats.StoredCards!.SequenceEqual(prior) &&
            stats.DeckCards!.Order().SequenceEqual(deck) && actual.Statistics!.DeckCards!.Order().SequenceEqual(deck),
            "The witnessed terminal statistics do not match both snapshots and the permanent deck.");
        BattleStatistics refreshed = stats.RefreshDeckAfterCardTerminal();
        Require(Equal(refreshed, actual.Statistics), "Observed UI refresh differs from the exact native statistic delta.");
        return modeled.WithStatistics(refreshed);
    }

    private static void TerminalUiRetention(FixtureValue fixture)
    {
        FixtureValue row = fixture.GetProperty("Turns").EnumerateArray().Last();
        BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        BattleTurnResult modeled = BattleTurnModel.EndTurn(before, RecordedBattleChecks.Decisions(row));
        CombatContext native = actual.Spawn.Train.Context!;
        BattleStatistics stats = native.Statistics!;
        Require(!HasUiRefresh(row) && native.KillCamActivated == true && native.CardInstances?.Count == 0 &&
            stats.StoredCards!.Except(stats.DeckCards!).Any() && modeled.Supported && modeled.Outcome == RoomOutcome.BattleWon &&
            BattleTurnChecks.Comparable(modeled.State!, terminal: true) == BattleTurnChecks.Comparable(actual, terminal: true),
            "The ordinary native victory control must retain off-deck statistics without a witnessed UI refresh.");
    }

    private static bool Equal<T>(T a, T b) =>
        JsonSerializer.Serialize(a, ModelJson.Options) == JsonSerializer.Serialize(b, ModelJson.Options);

    private static void Retention(FixtureValue fixture)
    {
        var rows = fixture.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        Require(fixture.GetProperty("NativeWon").GetBoolean() && fixture.GetProperty("CaptureFailures").GetInt32() == 0 &&
            fixture.GetProperty("Mismatches").GetInt32() == 3 && fixture.GetProperty("Unsupported").GetInt32() == 0 &&
            fixture.GetProperty("Pending").GetInt32() == 0 && rows.Length == 32,
            "Missing the native ordinary victory cache-retention counterexample.");
        var stale = rows[^1].GetProperty("Expected").Deserialize<BattleTurnState>()!.Spawn.Train.Context!.Statistics!;
        var native = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!.Spawn.Train.Context!.Statistics!;
        Require(stale.Values.Count == 330 && native.Values.Count == 400 && stale.TrackedCards.Count == 18 && native.TrackedCards.Count == 23 &&
            JsonSerializer.Serialize(native.RefreshDeckAfterCardTerminal(), ModelJson.Options) == JsonSerializer.Serialize(stale, ModelJson.Options),
            "Ordinary victory must retain the five generated-card cache entries; a blanket refresh is incorrect.");
        var current = rows[0].GetProperty("Root").Deserialize<BattleTurnState>()!;
        string immutable = JsonSerializer.Serialize(current, ModelJson.Options);
        var root = current;
        foreach (var row in rows)
        {
            Require(NativeBranchObservationChecks.Key(current) == NativeBranchObservationChecks.Key(row.GetProperty("Root").Deserialize<BattleTurnState>()!),
                "Cache-retention native root differs.");
            current = BattlePlannerChecks.Replay(current, [new(row.GetProperty("Action").Deserialize<PlayCardAction>(), row.GetProperty("IsEndTurn").GetBoolean())], out var outcome);
            Require((int)outcome == row.GetProperty("Outcome").GetInt32() && NativeBranchObservationChecks.Key(current, outcome == RoomOutcome.BattleWon) ==
                NativeBranchObservationChecks.Key(row.GetProperty("Actual").Deserialize<BattleTurnState>()!, outcome == RoomOutcome.BattleWon),
                "Cache-retention full native successor/RNG differs.");
        }
        Require(immutable == JsonSerializer.Serialize(root, ModelJson.Options), "Cache-retention replay changed its root.");
        TrainCombatChecks.Native(fixture);
        Console.WriteLine("COMBAT-VICTORY-CACHE-RETENTION PASS:32 native inputs/full state/RNG; retained400 values/23 cards, preserving five generated infusions; ordinary victory must not force a refresh.");
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}
