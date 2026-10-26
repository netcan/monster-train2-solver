using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RemotePreviewFrameChecks
{
    internal const string Reason = "Native mixed aura preview bookkeeping differs at fourth EndTurn.";
    internal static void Native(FixtureValue source, string counterexamplePath, FixtureValue? replay = null)
    {
        using var counterexampleDocument = ModelJson.ReadFixture(counterexamplePath);
        var counterexample = counterexampleDocument.RootElement;
        RoomCombatChecks.NativeObservedRooms(counterexamplePath);
        TrainCombatChecks.Native(counterexample);
        Require(source.GetProperty("NativeCompleted").GetBoolean() && source.GetProperty("Request").GetProperty("Reason").GetString() == Reason &&
            source.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            counterexample.GetProperty("NativeWon").ValueKind == FixtureKind.Null && counterexample.GetProperty("Mismatches").GetInt32() == 4,
            "Preserve original native component and aborted raw counterexample.");
        var rows = source.GetProperty("Rows").EnumerateArray().ToArray();
        Require(rows.Length == 22 && rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Missing four-turn twenty-two-input boundary.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(before.Spawn.Turn == 3 && actual.Spawn.Turn == 4, "Lost fourth EndTurn.");
        BattleTurnState current = source.GetProperty("Request").GetProperty("Root").Deserialize<BattleTurnState>()!;
        Require(current.Spawn.Turn == 0 && source.GetProperty("Request").GetProperty("Prefix").GetArrayLength() == 21 &&
            NativeBranchObservationChecks.Key(before) == NativeBranchObservationChecks.Key(source.GetProperty("Request").GetProperty("FailedRoot").Deserialize<BattleTurnState>()!),
            "Lost deployment root or failed boundary.");
        for (int index = 0; index < rows.Length; index++)
        {
            var row = rows[index];
            Require(NativeBranchObservationChecks.Key(current) == NativeBranchObservationChecks.Key(row.GetProperty("Before").Deserialize<BattleTurnState>()!),
                "Owned input root differs at " + index);
            var input = index == 21 ? source.GetProperty("Request") : source.GetProperty("Request").GetProperty("Prefix")[index];
            Require(row.GetProperty("IsEndTurn").ContentEquals(input.GetProperty("IsEndTurn")) &&
                row.GetProperty("Action").ContentEquals(input.GetProperty("Action")) && row.GetProperty("Boundary").GetBoolean() == (index == 21),
                "Original input changed at " + index);
            current = BattlePlannerChecks.Replay(current, [new BattleSearchStep(row.GetProperty("Action").Deserialize<PlayCardAction>(),
                row.GetProperty("IsEndTurn").GetBoolean())], out var outcome);
            Require(outcome == RoomOutcome.Exchanged && (index == 21 ? KeyWithoutOmittedActor(current) : NativeBranchObservationChecks.Key(current)) ==
                NativeBranchObservationChecks.Key(row.GetProperty("Actual").Deserialize<BattleTurnState>()!), "Owned combat frame prefix differs.");
        }
        var retired = current.Spawn.Train.Context!.Enchantments!.RetainedUnits.Single(actor => actor.Unit.Id == 22);
        Require(retired.Unit is { AssetKey: "MonsterFunguy", SpawnerCardId: 0, Health: 0, DeathState.IsDestroyed: true } &&
            retired.Unit.Modifiers!.Upgrades.Count == 0 && !actual.Spawn.Train.Context!.Enchantments!.RetainedUnits.Any(actor => actor.Unit.Id == 22),
            "Lost the original player projection's omitted real cardless aura target.");
        var historical = rows[^1].GetProperty("Expected").Deserialize<BattleTurnState>()!;
        Require(historical.Spawn.Train.Context!.Enchantments!.Rooms[1].Units.Single(unit => unit.Id == 13)
            .Triggers[0].Effects[0].Enchantment!.State.PreviewTargets.Single(target => target.UnitId == 21).NextAction == 0 &&
            actual.Spawn.Train.Context!.Enchantments!.Rooms[1].Units.Single(unit => unit.Id == 13)
            .Triggers[0].Effects[0].Enchantment!.State.PreviewTargets.Single(target => target.UnitId == 21).NextAction == 2,
            "Lost historical deferred preview destruction and next-action mismatch.");
        var frame = counterexample.GetProperty("Stages")[30].GetProperty("Before").Deserialize<RoomCombatState>()!;
        Require(ReferenceEquals(frame.Context, PreviewBirthModel.CompleteRemoteCombatBirths(frame.Context!, 1)),
            "Same-floor preview state was destroyed without its next real combat wait.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            var next = BattleTurnModel.EndTurn(before);
            Require(next.Supported && NativeBranchObservationChecks.Key(next.State!) == NativeBranchObservationChecks.Key(current) &&
                parent == JsonSerializer.Serialize(before, ModelJson.Options), "Frame completion changed its parent or state/RNG.");
        });
        if (replay.HasValue)
        {
            NativeBranchObservationChecks.Replay(replay.Value, Reason);
            Require(source.GetProperty("Request").ContentEquals(replay.Value.GetProperty("Request")), "Independent native request changed.");
            var independent = replay.Value.GetProperty("Rows").EnumerateArray().ToArray();
            Require(independent.Length == 22 && NativeBranchObservationChecks.Key(current) ==
                NativeBranchObservationChecks.Key(independent[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!),
                "Repaired independent player projection omitted the cardless aura target or changed full state/RNG.");
            for (int index = 0; index < rows.Length; index++)
                foreach (string field in new[] { "Before", "Actual" })
                {
                    var value = independent[index].GetProperty(field).Deserialize<BattleTurnState>()!;
                    Require((index == 21 && field == "Actual" ? KeyWithoutOmittedActor(value) : NativeBranchObservationChecks.Key(value)) ==
                        NativeBranchObservationChecks.Key(rows[index].GetProperty(field).Deserialize<BattleTurnState>()!),
                        "Independent native prefix changed at " + index + "/" + field);
                }
        }
        Console.WriteLine("REMOTE-PREVIEW-FRAME PASS:22 native inputs, continuous state/RNG, raw room/train stages, four immutable branches; independentPair=" + replay.HasValue);
    }
    private static string KeyWithoutOmittedActor(BattleTurnState source)
    {
        var node = JsonNode.Parse(NativeBranchObservationChecks.Key(source))!;
        var retained = node["Context"]!["Enchantments"]!["RetainedUnits"]!.AsArray();
        retained.Remove(retained.Single(actor => actor!["Unit"]!["Id"]!.GetValue<int>() == 22));
        return node.ToJsonString(ModelJson.Options);
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
