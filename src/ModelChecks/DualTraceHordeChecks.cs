using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class DualTraceHordeChecks
{
    internal static void Diagnose(FixtureValue fixture)
    {
        var mapping = fixture.GetProperty("IdentityCorrespondence").GetProperty("Cards").EnumerateArray()
            .ToDictionary(card => card.GetProperty("DevelopmentId").GetInt32(), card => card.GetProperty("PlayerId").GetInt32());
        var post = fixture.GetProperty("SettlementPost");
        JsonNode development = JsonNode.Parse(WithoutPreviewIdentity(post.GetProperty("Development").Deserialize<BattleTurnState>()!))!;
        JsonNode player = JsonNode.Parse(WithoutPreviewIdentity(post.GetProperty("Player").Deserialize<BattleTurnState>()!))!;
        void MapCardReferences(JsonNode? node)
        {
            if (node is JsonObject obj)
            {
                foreach (string field in new[] { "SpawnerCardId", "ExhaustedCardId", "EquipmentSourceCardId", "GraftedEquipmentCardId" })
                {
                    if (obj[field] is not JsonValue idValue || !idValue.TryGetValue(out int id) || id == 0) continue;
                    Require(mapping.TryGetValue(id, out int translated), "Unknown native card reference: " + id);
                    obj[field] = translated;
                }
                foreach (var item in obj.ToArray()) MapCardReferences(item.Value);
            }
            else if (node is JsonArray array) foreach (var item in array) MapCardReferences(item);
        }
        MapCardReferences(development);
        foreach (string pile in new[] { "Hand", "Draw", "Discard" })
            foreach (var card in development["Context"]!["Cards"]![pile]!.AsArray())
            {
                int id = card!["InstanceId"]!.GetValue<int>();
                Require(mapping.TryGetValue(id, out int translated), "Unknown native card-cycle identity: " + id);
                card["InstanceId"] = translated;
            }
        JsonNode? stats = development["Context"]!["Statistics"];
        if (stats != null)
        {
            foreach (string field in new[] { "Values", "PlayedCosts" })
            {
                var values = stats[field]!.AsArray();
                foreach (var value in values)
                {
                    int id = value!["CardId"]!.GetValue<int>();
                    Require(mapping.TryGetValue(id, out int translated), "Unknown statistic card identity: " + id);
                    value["CardId"] = translated;
                }
                var ordered = values.OrderBy(value => value!["CardId"]!.GetValue<int>())
                    .ThenBy(value => value!["Duration"]?.GetValue<string>(), StringComparer.Ordinal)
                    .ThenBy(value => value!["Type"]?.GetValue<string>(), StringComparer.Ordinal)
                    .Select(value => value!.DeepClone()).ToArray();
                stats[field] = new JsonArray(ordered);
            }
            foreach (string field in new[] { "TrackedCards", "StoredCards", "DeckCards", "CardsPlayedThisTurn", "HandCardsPlayedThisTurn", "HandCardsPlayedLastTurn" })
            {
                if (stats[field] is not JsonArray values) continue;
                int[] translated = values.Select(value => {
                    int id = value!.GetValue<int>();
                    Require(mapping.TryGetValue(id, out int mapped), "Unknown statistic card membership: " + id);
                    return mapped;
                }).ToArray();
                if (field is "TrackedCards" or "StoredCards" or "DeckCards") Array.Sort(translated);
                stats[field] = new JsonArray(translated.Select(id => (JsonNode?)JsonValue.Create(id)).ToArray());
            }
        }
        foreach (string field in new[] { "CardInstances", "CardRegistry" })
        {
            if (development["Context"]![field] is not JsonArray cards) continue;
            foreach (var card in cards)
            {
                int id = card!["InstanceId"]!.GetValue<int>();
                Require(mapping.TryGetValue(id, out int translated), "Unknown card registry identity: " + id);
                card["InstanceId"] = translated;
            }
            development["Context"]![field] = new JsonArray(cards.OrderBy(card => card!["InstanceId"]!.GetValue<int>())
                .Select(card => card!.DeepClone()).ToArray());
        }
        void MapPiles(JsonNode? node)
        {
            if (node is not JsonArray piles) return;
            foreach (var pile in piles)
            {
                foreach (var card in pile!["Cards"]!.AsArray())
                {
                    int id = card!["InstanceId"]!.GetValue<int>();
                    Require(mapping.TryGetValue(id, out int translated), "Unknown pile card identity: " + id);
                    card["InstanceId"] = translated;
                }
                if (pile["EntrySlots"] is JsonArray slots)
                    pile["EntrySlots"] = new JsonArray(slots.Select(value => {
                        int id = value!.GetValue<int>();
                        if (id == 0) return (JsonNode?)JsonValue.Create(0);
                        Require(mapping.TryGetValue(id, out int translated), "Unknown pile entry identity: " + id);
                        return JsonValue.Create(translated);
                    }).ToArray());
                foreach (string field in new[] { "EquipmentConditions", "UnitConditions", "ExhaustConditions" })
                {
                    if (pile[field] is not JsonArray conditions) continue;
                    foreach (var condition in conditions)
                    {
                        int id = condition!["CardId"]!.GetValue<int>();
                        Require(mapping.TryGetValue(id, out int translated), "Unknown pile condition identity: " + id);
                        condition["CardId"] = translated;
                    }
                }
            }
        }
        MapPiles(development["Context"]!["OtherPiles"]);
        MapPiles(development["OtherPiles"]);
        Require(development["Context"]!["NextUnitId"]!.GetValue<int>() == 28 &&
            player["Context"]!["NextUnitId"]!.GetValue<int>() == 27, "Unexpected context unit allocator.");
        development["Context"]!["NextUnitId"] = 0;
        player["Context"]!["NextUnitId"] = 0;
        string normalized = development.ToJsonString();
        string playerJson = player.ToJsonString();
        Require(normalized == playerJson, "Unexplained collector difference: " + ModelJson.Difference(normalized, playerJson));
        foreach (string field in new[] { "Gold", "Health", "Rng", "Statistic" })
        {
            JsonNode altered = player.DeepClone();
            JsonNode parent = field switch {
                "Gold" => altered["Context"]!,
                "Health" => altered["Rooms"]![0]!["Units"]![0]!,
                "Rng" => altered["RngStreams"]![0]!,
                _ => altered["Context"]!["Statistics"]!["Values"]![0]!
            };
            string name = field switch { "Rng" => "Seed", "Statistic" => "Value", _ => field };
            parent[name] = parent[name]!.GetValue<int>() + 1;
            Require(normalized != altered.ToJsonString(), "Comparison hid changed " + field);
        }
        Console.WriteLine("HORDE-IDENTITY-DIAGNOSTIC MATCH: full comparable state/RNG after native-object card mapping and explicit preview identity adjustment; four value controls preserved.");
    }

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
            Diagnose(fixture);
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
