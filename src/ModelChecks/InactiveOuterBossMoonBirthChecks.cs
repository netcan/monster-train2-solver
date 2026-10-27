using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class InactiveOuterBossMoonBirthChecks
{
    internal const string Reason = "Native inactive Fragile boss birth moon mask differs on the first turn.";
    internal static void Native(string path, string? replayPath = null)
    {
        using var document = ModelJson.ReadFixture(path);
        var source = document.RootElement;
        Require(source.GetProperty("EvidenceKind").GetString() == "NativeIncompleteBranchObservation" &&
            !source.GetProperty("NativeCompleted").GetBoolean() &&
            source.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4",
            "Expected the preserved incomplete native moon-birth prefix.");
        var rows = source.GetProperty("Rows").EnumerateArray().ToArray();
        var request = source.GetProperty("Request");
        Require(rows.Length == 8 && request.GetProperty("Reason").GetString() == PyregelPowerstoneJuiceChecks.CastReason,
            "Wrong native cast prefix at the inactive boss birth.");
        BattleTurnState current = request.GetProperty("Root").Deserialize<BattleTurnState>()!;
        for (int index = 0; index < rows.Length; index++)
        {
            var row = rows[index];
            var before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            var actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            var step = request.GetProperty("Prefix")[index];
            bool end = row.GetProperty("IsEndTurn").GetBoolean();
            var action = row.GetProperty("Action").Deserialize<PlayCardAction>();
            Require(NativeBranchObservationChecks.Key(current) == NativeBranchObservationChecks.Key(before) &&
                !row.GetProperty("Boundary").GetBoolean() && end == step.GetProperty("IsEndTurn").GetBoolean() &&
                row.GetProperty("Action").ContentEquals(step.GetProperty("Action")), "Original native prefix changed at " + index);
            var result = BattlePlannerChecks.Replay(before, [new BattleSearchStep(action, end)], out RoomOutcome outcome);
            Require(outcome == RoomOutcome.Exchanged && NativeBranchObservationChecks.Key(result) == NativeBranchObservationChecks.Key(actual),
                "Inactive boss native state/RNG differs at " + index + ": " + ModelJson.Difference(NativeBranchObservationChecks.Key(result), NativeBranchObservationChecks.Key(actual)));
            var old = row.GetProperty("Expected").Deserialize<BattleTurnState>()!;
            if (index < 7)
                Require(NativeBranchObservationChecks.Key(old) == NativeBranchObservationChecks.Key(actual), "Earlier historical prediction changed.");
            else
            {
                CombatUnit boss = actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == 11);
                Require(end && before.Spawn.Turn == 0 && before.MoonPhaseTrackingActive == false &&
                    actual.MoonPhaseTrackingActive == false && actual.MoonPhase == "Full" &&
                    boss is { AssetKey: "Boss_Level8_Fragile", Attack: 30, Health: 3000, MoonPhaseMask: 6 } &&
                    boss.Status("fullmoon") == null && boss.Status("newmoon") == null &&
                    before.Spawn.QueuedOuterBoss!.Actor.Unit.MoonPhaseMask == 4 &&
                    old.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == 11).MoonPhaseMask == 4,
                    "Missing native inactive Fragile birth4-to6 without phase statuses.");
                JsonNode projected = JsonSerializer.SerializeToNode(old, ModelJson.Options)!;
                int corrected = 0;
                void Correct(JsonNode? node)
                {
                    if (node is JsonObject obj)
                    {
                        if (obj.ContainsKey("BaseAttack") && obj["Id"]?.GetValue<int>() == 11 && obj["AssetKey"]?.GetValue<string>() == "Boss_Level8_Fragile" &&
                            obj["MoonPhaseMask"]?.GetValue<int>() == 4) { obj["MoonPhaseMask"] = 6; corrected++; }
                        foreach (var child in obj.ToArray()) Correct(child.Value);
                    }
                    else if (node is JsonArray array) foreach (var child in array) Correct(child);
                }
                Correct(projected);
                Require(corrected > 0 && NativeBranchObservationChecks.Key(projected.Deserialize<BattleTurnState>(ModelJson.Options)!) ==
                    NativeBranchObservationChecks.Key(actual), "Historical difference includes more than the captured boss moon mask.");
                string immutable = JsonSerializer.Serialize(before, ModelJson.Options);
                Parallel.For(0, 4, _ => Require(NativeBranchObservationChecks.Key(BattleTurnModel.EndTurn(before).State!) ==
                    NativeBranchObservationChecks.Key(actual), "Inactive boss parallel EndTurn differs."));
                Require(immutable == JsonSerializer.Serialize(before, ModelJson.Options), "Inactive boss EndTurn mutated its parent.");
                var unknownCallback = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
                unknownCallback["Spawn"]!["QueuedOuterBoss"]!["Actor"]!["Unit"]!["Triggers"]![0]!["Kind"] = "OnMoonLit";
                Require(!BattleTurnModel.EndTurn(unknownCallback.Deserialize<BattleTurnState>(ModelJson.Options)!).Supported,
                    "Unknown inactive boss moon callback lost its guard.");
            }
            current = actual;
        }
        if (replayPath != null)
        {
            using var replayDocument = ModelJson.ReadFixture(replayPath);
            var replay = replayDocument.RootElement;
            var repeated = NativeBranchObservationChecks.Replay(replay, Reason);
            Require(repeated.Length == rows.Length && replay.GetProperty("Request").GetProperty("SourceSHA256").GetString() ==
                Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant(),
                "Independent inactive boss request differs from its preserved source.");
            for (int index = 0; index < rows.Length; index++)
                Require(rows[index].GetProperty("Action").ContentEquals(repeated[index].GetProperty("Action")) &&
                    NativeBranchObservationChecks.Key(rows[index].GetProperty("Before").Deserialize<BattleTurnState>()!) ==
                    NativeBranchObservationChecks.Key(repeated[index].GetProperty("Before").Deserialize<BattleTurnState>()!) &&
                    NativeBranchObservationChecks.Key(rows[index].GetProperty("Actual").Deserialize<BattleTurnState>()!) ==
                    NativeBranchObservationChecks.Key(repeated[index].GetProperty("Actual").Deserialize<BattleTurnState>()!),
                    "Independent inactive boss input/state/RNG differs at " + index);
        }
        Console.WriteLine("INACTIVE-OUTER-BOSS-MOON-BIRTH PASS: eight native inputs/full state/RNG, exact historical mask4-to6 and four immutable EndTurns; incomplete source retained; independentPair=" + (replayPath != null));
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
