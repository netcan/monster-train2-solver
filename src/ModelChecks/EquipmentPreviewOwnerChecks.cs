using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class EquipmentPreviewOwnerChecks
{
    private const string Reason = "Native equipment preview clears the card-side owner while unit membership persists.";

    internal static void Native(FixtureValue failure, FixtureValue source, FixtureValue? replay = null)
    {
        Require(failure.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            failure.GetProperty("NativeWon").ValueKind == FixtureKind.Null &&
            failure.GetProperty("CaptureFailures").GetInt32() == 0 && failure.GetProperty("Mismatches").GetInt32() == 1 &&
            failure.GetProperty("Unsupported").GetInt32() == 0 && failure.GetProperty("Pending").GetInt32() == 0,
            "Preserve the original incomplete native observation and its one mismatch.");
        FixtureValue failed = failure.GetProperty("Actions").EnumerateArray().Last();
        var action = failed.GetProperty("Action").Deserialize<PlayCardAction>()!;
        Require(action.CardInstanceId == 16 && action.TargetUnitId == 2 &&
            failed.GetProperty("Index").GetInt32() == 5, "Wrong original equipment action.");
        var before = failed.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var expected = failed.GetProperty("Predicted").GetProperty("State").Deserialize<BattleTurnState>()!;
        var actual = failed.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(before.Spawn.Train.Context!.FindCard(16)!.DataId == "e4ba411f-09b2-4a87-91f6-5f00cd742ba6" &&
            expected.Spawn.Train.Context!.FindCard(16)!.EquippedUnitId == 2 &&
            actual.Spawn.Train.Context!.FindCard(16)!.EquippedUnitId == 0 &&
            actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == 2).EquipmentCards!.Contains(16),
            "Original raw card back-reference and real equipment membership no longer disagree.");
        var normalized = JsonNode.Parse(NativeBranchObservationChecks.Key(actual))!;
        foreach (string collection in new[] { "CardInstances", "CardRegistry" })
            normalized["Context"]![collection]!.AsArray().Single(card => card!["InstanceId"]!.GetValue<int>() == 16)!["EquippedUnitId"] = 2;
        Require(JsonNode.DeepEquals(normalized, JsonNode.Parse(NativeBranchObservationChecks.Key(expected))),
            "Original mismatch extends beyond the card-side equipment owner: " + ModelJson.Difference(normalized.ToJsonString(), NativeBranchObservationChecks.Key(expected)));
        Check(source);
        if (replay is FixtureValue second)
        {
            Check(second);
            Require(source.GetProperty("Request").ContentEquals(second.GetProperty("Request")), "Independent equipment request changed.");
            var first = source.GetProperty("Rows").EnumerateArray().ToArray();
            var other = second.GetProperty("Rows").EnumerateArray().ToArray();
            for (int index = 0; index < first.Length; index++)
                foreach (string field in new[] { "Before", "Actual" })
                    Require(NativeBranchObservationChecks.Key(first[index].GetProperty(field).Deserialize<BattleTurnState>()!) ==
                        NativeBranchObservationChecks.Key(other[index].GetProperty(field).Deserialize<BattleTurnState>()!),
                        "Independent equipment state changed at " + index);
        }
        Console.WriteLine("EQUIPMENT-PREVIEW-OWNER PASS: original one-field capture failure retained; seven natural inputs/full state/RNG; native PreviewCardPlay(false) clears raw owner while primary membership and captured decision remain unchanged; independentPair=" + replay.HasValue);

        void Check(FixtureValue fixture)
        {
            var rows = NativeBranchObservationChecks.Replay(fixture, Reason);
            Require(rows.Length == 7, "Wrong repaired equipment prefix length.");
            var fixedBefore = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
            var fixedAfter = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            // The raw action probe and player projection allocate duplicate
            // spawner-card references independently. Compare this card/owner
            // across them; Replay above checks every full player successor.
            foreach (var pair in new[] { (before, fixedBefore), (expected, fixedAfter) })
            {
                string oldCard = JsonSerializer.Serialize(pair.Item1.Spawn.Train.Context!.FindCard(16), ModelJson.Options);
                string newCard = JsonSerializer.Serialize(pair.Item2.Spawn.Train.Context!.FindCard(16), ModelJson.Options);
                Require(oldCard == newCard, "Fresh native equipment card differs: " + ModelJson.Difference(oldCard, newCard));
                CombatUnit oldOwner = pair.Item1.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == 2);
                CombatUnit newOwner = pair.Item2.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == 2);
                Require(pair.Item1.Spawn.Train.Context.FindCard(oldOwner.SpawnerCardId)!.DataId ==
                    pair.Item2.Spawn.Train.Context.FindCard(newOwner.SpawnerCardId)!.DataId, "Equipment owner changed its native spawner definition.");
                var oldUnit = JsonSerializer.SerializeToNode(oldOwner, ModelJson.Options)!;
                oldUnit["SpawnerCardId"] = newOwner.SpawnerCardId;
                Require(JsonNode.DeepEquals(oldUnit, JsonSerializer.SerializeToNode(newOwner, ModelJson.Options)),
                    "Fresh native equipment owner differs after mapping its spawner reference.");
            }
            FixtureValue clear = fixture.GetProperty("EquipmentPreviewClear");
            var prior = clear.GetProperty("Before").Deserialize<BattleTurnState>()!;
            var after = clear.GetProperty("After").Deserialize<BattleTurnState>()!;
            Require(clear.GetProperty("RawOwnerAfter").GetInt32() == 0 && clear.GetProperty("EquipmentOwnerId").GetInt32() == 2 &&
                clear.GetProperty("StillAttached").GetBoolean() && after.Spawn.Train.Context!.FindCard(16)!.EquippedUnitId == 2 &&
                NativeBranchObservationChecks.Key(prior) == NativeBranchObservationChecks.Key(after) &&
                NativeBranchObservationChecks.Key(after) == NativeBranchObservationChecks.Key(fixedAfter),
                "Native preview cleanup must preserve the complete captured decision and RNG despite its cleared back-reference.");
        }
    }

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
