using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;
using System.Text.Json;

internal static class PreviewBirthStageChecks
{
    internal static void Native(string sourcePath, string replayPath)
    {
        using var sourceDocument = ModelJson.ReadFixture(sourcePath);
        using var replayDocument = ModelJson.ReadFixture(replayPath);
        FixtureValue source = sourceDocument.RootElement, replay = replayDocument.RootElement;
        Require(source.GetProperty("ModifierScenario").GetString() == "persistent-enchantment-summons" &&
            source.GetProperty("Mismatches").GetInt32() == 4 && source.GetProperty("NativeWon").GetBoolean() &&
            replay.GetProperty("NativeWon").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }.All(key => replay.GetProperty(key).GetInt32() == 0),
            "Expected the preserved four-stage counterexample and a clean independent native win.");
        string[] gaps = new[] { "Stages", "TrainPhases" }.SelectMany(name => source.GetProperty(name).EnumerateArray()
            .Where(row => row.GetProperty("Difference").ValueKind != FixtureKind.Null)
            .Select(row => name + ":" + row.GetProperty("Index").GetInt32())).ToArray();
        Require(gaps.SequenceEqual(new[] { "Stages:36", "Stages:37", "Stages:40", "TrainPhases:8" }),
            "The historical counterexample lost its original four boundaries.");
        foreach (string collection in new[] { "Stages", "TrainPhases", "Actions", "Turns" })
        {
            FixtureValue[] first = source.GetProperty(collection).EnumerateArray().ToArray();
            FixtureValue[] second = replay.GetProperty(collection).EnumerateArray().ToArray();
            Require(first.Length == second.Length && first.Length > 0, "Native record count changed: " + collection);
            for (int index = 0; index < first.Length; index++)
            {
                Require(second[index].GetProperty("Difference").ValueKind == FixtureKind.Null,
                    "Corrected capture still differs: " + collection + index);
                foreach (string field in new[] { "Before", "Actual" })
                    Require(first[index].GetProperty(field).ContentEquals(second[index].GetProperty(field)),
                        "Independent native state changed: " + collection + index + field);
                if (collection is not ("Actions" or "Turns")) continue;
                BattleTurnState before = first[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
                string immutable = JsonSerializer.Serialize(before, ModelJson.Options);
                BattleTurnState predicted;
                RoomOutcome outcome;
                if (collection == "Actions")
                {
                    Require(first[index].GetProperty("Action").ContentEquals(second[index].GetProperty("Action")), "Native input changed.");
                    BattleActionResult result = BattleActionModel.PlayCard(before, first[index].GetProperty("Action").Deserialize<PlayCardAction>()!);
                    Require(result.Supported, result.Reason ?? "Unsupported paid input.");
                    predicted = result.State!; outcome = result.Outcome;
                }
                else
                {
                    BattleTurnResult result = BattleTurnModel.EndTurn(before);
                    Require(result.Supported, result.UnsupportedReason ?? "Unsupported EndTurn.");
                    predicted = result.State!; outcome = result.Outcome;
                }
                bool terminal = outcome is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated;
                Require((int)outcome == first[index].GetProperty("ActualOutcome").GetInt32() &&
                    NativeBranchObservationChecks.Key(predicted, terminal) == NativeBranchObservationChecks.Key(
                        first[index].GetProperty("Actual").Deserialize<BattleTurnState>()!, terminal) &&
                    immutable == JsonSerializer.Serialize(before, ModelJson.Options),
                    "Complete successor/forecast/RNG or parent differs: " + collection + index);
            }
        }
        // Recompute every stage from both immutable native processes; historical
        // predictions remain in the source archive as regression witnesses.
        RoomCombatChecks.NativeRoomOnly(sourcePath);
        RoomCombatChecks.NativeRoomOnly(replayPath);
        TrainCombatChecks.Native(source);
        TrainCombatChecks.Native(replay);
        PersistentEnchantmentChecks.Native(replay);
        RoomCombatState root = source.GetProperty("Stages")[37].GetProperty("Before").Deserialize<RoomCombatState>()!;
        string parent = JsonSerializer.Serialize(root, ModelJson.Options);
        string expected = JsonSerializer.Serialize(RoomCombatModel.Exchange(root).State, ModelJson.Options);
        Parallel.For(0, 4, _ => Require(expected == JsonSerializer.Serialize(RoomCombatModel.Exchange(root).State, ModelJson.Options),
            "Parallel temporary births changed the shared parent."));
        Require(parent == JsonSerializer.Serialize(root, ModelJson.Options), "Temporary birth preview mutated its parent.");
        Console.WriteLine("PREVIEW-BIRTH-STAGE PAIR PASS: all four historical gaps closed; 41 room stages, 9 train phases, " +
            "15 paid actions/5 EndTurns, complete native state/RNG, immutable parallel parents and clean independent win.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
