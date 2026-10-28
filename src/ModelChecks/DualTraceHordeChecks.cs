using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class DualTraceHordeChecks
{
    internal static void Correspondence(FixtureValue original, FixtureValue first, FixtureValue second)
    {
        Native(original, first, second);
        int[]? previous = null;
        foreach (FixtureValue fixture in new[] { first, second })
        {
            var evidence = fixture.GetProperty("IdentityCorrespondence");
            var spawner = evidence.GetProperty("Spawner");
            Require(spawner.GetProperty("DevelopmentUnitId").GetInt32() == 20 && spawner.GetProperty("PlayerUnitFound").GetBoolean() &&
                spawner.GetProperty("PlayerUnitId").GetInt32() == 20 && spawner.GetProperty("DevelopmentCardFound").GetBoolean() &&
                spawner.GetProperty("DevelopmentCardId").GetInt32() == 5 && spawner.GetProperty("PlayerCardFound").GetBoolean() &&
                spawner.GetProperty("PlayerCardId").GetInt32() == 6, "Native spawner identity correspondence differs.");
            var cards = evidence.GetProperty("Cards").EnumerateArray().ToArray();
            Require(cards.Length == 39 && cards.All(card => card.GetProperty("DevelopmentFound").GetBoolean() && card.GetProperty("PlayerFound").GetBoolean()),
                "A development card is absent from the player identity map.");
            var ordered = cards.OrderBy(card => card.GetProperty("DevelopmentId").GetInt32()).ToArray();
            int[] source = ordered.Select(card => card.GetProperty("DevelopmentId").GetInt32()).ToArray();
            int[] target = ordered.Select(card => card.GetProperty("PlayerId").GetInt32()).ToArray();
            Require(source.SequenceEqual(Enumerable.Range(1, 39)) && target.Distinct().Count() == 39 && target.All(id => id > 0) && target[4] == 6,
                "Native card-object correspondence is not one-to-one.");
            if (previous != null) Require(previous.SequenceEqual(target), "Independent native card-object correspondence differs.");
            previous = target;
        }
        Console.WriteLine("HORDE-IDENTITY-MAP PASS: two independent native runs map all 39 development card objects one-to-one, including the same 5->6 spawner; boundary remains guarded.");
    }

    internal static void Native(FixtureValue original, FixtureValue timeline, FixtureValue? replay = null)
    {
        TripleHordePhaseBoundaryChecks.CheckTimeline(original, timeline);
        var frames = timeline.GetProperty("SettlementTimeline").EnumerateArray().ToArray();
        var post = timeline.GetProperty("SettlementPost");
        Require(post.GetProperty("Frame").GetInt32() == frames[0].GetProperty("ReadyFrame").GetInt32() + 2, "Wrong post-settlement frame.");
        var playerFinal = post.GetProperty("Player").Deserialize<BattleTurnState>()!;
        var developmentFinal = post.GetProperty("Development").Deserialize<BattleTurnState>()!;
        var boundary = timeline.GetProperty("Rows").EnumerateArray().Last();
        var rowFinal = boundary.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var guarded = BattleTurnModel.EndTurn(boundary.GetProperty("Before").Deserialize<BattleTurnState>()!);
        Require(!guarded.Supported && guarded.UnsupportedReason ==
            "Train combat floor 2: Combined troop upgrades and enchantment source ordering require native calibration.", "Unverified boundary escaped its guard.");
        Require(NativeBranchObservationChecks.Key(playerFinal) == NativeBranchObservationChecks.Key(rowFinal), "Wrong final player capture.");
        for (int i = 0; i < frames.Length; i++)
        {
            var development = frames[i].GetProperty("First").Deserialize<BattleTurnState>()!;
            var player = frames[i].GetProperty("PlayerFirst").Deserialize<BattleTurnState>()!;
            var playerSecond = frames[i].GetProperty("PlayerSecond").Deserialize<BattleTurnState>()!;
            Require(frames[i].GetProperty("PlayerCaptureStable").GetBoolean() &&
                NativeBranchObservationChecks.Key(player) == NativeBranchObservationChecks.Key(playerSecond), "Player capture changed within frame " + i);
            Require(NativeBranchObservationChecks.Key(development) == NativeBranchObservationChecks.Key(developmentFinal) &&
                NativeBranchObservationChecks.Key(player) == NativeBranchObservationChecks.Key(playerFinal), "Collector changed between settlement frames " + i);
            string left = WithoutPreviewIdentity(development), right = WithoutPreviewIdentity(player);
            Require(ModelJson.Difference(left, right) == "$.Rooms[1].Units[2].SpawnerCardId: 5 != 6",
                "Missing additional collector identity mismatch: " + ModelJson.Difference(left, right));
        }
        Require(developmentFinal.Spawn.NextUnitId == 28 && playerFinal.Spawn.NextUnitId == 27 &&
            developmentFinal.Spawn.Train.Context!.SpawnPoints!.Units.Any(point => point.UnitId == 27 && point.SpawnedInPreview && point.Current == null && point.LastKnown == null) &&
            !playerFinal.Spawn.Train.Context!.SpawnPoints!.Units.Any(point => point.UnitId == 27), "Wrong independent identity maps.");
        if (replay.HasValue)
        {
            Native(original, replay.Value);
            var otherFrames = replay.Value.GetProperty("SettlementTimeline").EnumerateArray().ToArray();
            for (int i = 0; i < frames.Length; i++)
                foreach (string field in new[] { "First", "Second", "PlayerFirst", "PlayerSecond" })
                    Require(NativeBranchObservationChecks.Key(frames[i].GetProperty(field).Deserialize<BattleTurnState>()!) ==
                        NativeBranchObservationChecks.Key(otherFrames[i].GetProperty(field).Deserialize<BattleTurnState>()!),
                        "Independent frame differs: " + i + "/" + field);
            foreach (string field in new[] { "Development", "Player" })
                Require(NativeBranchObservationChecks.Key(post.GetProperty(field).Deserialize<BattleTurnState>()!) ==
                    NativeBranchObservationChecks.Key(replay.Value.GetProperty("SettlementPost").GetProperty(field).Deserialize<BattleTurnState>()!),
                    "Independent post-settlement capture differs: " + field);
        }
        Console.WriteLine("DUAL-TRACE-HORDE PASS: stable independent collectors retain differing preview and spawner-card identities; unverified boundary remains guarded; independent=" + replay.HasValue);
    }

    private static string WithoutPreviewIdentity(BattleTurnState state)
    {
        JsonNode value = JsonNode.Parse(NativeBranchObservationChecks.Key(state))!;
        value["NextUnitId"] = 0;
        var points = value["Context"]!["SpawnPoints"]!["Units"]!.AsArray();
        var extra = points.SingleOrDefault(point => point!["UnitId"]!.GetValue<int>() == 27);
        if (extra != null)
        {
            Require(extra["SpawnedInPreview"]!.GetValue<bool>() && extra["Current"] == null && extra["LastKnown"] == null,
                "Unexpected preview point payload.");
            points.Remove(extra);
        }
        return value.ToJsonString();
    }

    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}
