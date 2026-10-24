using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;
using System.Text.Json;
using System.Text.Json.Nodes;

internal static class OpeningForsakenTrialChecks
{
    internal static void Native(FixtureValue fixture)
    {
        Require(fixture.GetProperty("OriginalOpeningForsakenTrial").GetBoolean() &&
            fixture.GetProperty("NativeWon").GetBoolean() && fixture.GetProperty("TerminalEffectsSettled").GetBoolean(),
            "Missing settled original enemy trial witness.");
        foreach (string counter in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending", "OriginalOpeningMorselsCaptureFailures",
            "OriginalOpeningMorselsMismatches", "OriginalOpeningMorselsUnsupported", "OriginalOpeningMorselsPending" })
            Require(fixture.GetProperty(counter).GetInt32() == 0, "Nonzero " + counter);
        var applications = fixture.GetProperty("RelicOpeningMorselApplications").EnumerateArray().ToArray();
        Require(applications.Length == 2, "Missing both native enemy-spawn effects.");
        for (int index = 0; index < applications.Length; index++)
        {
            FixtureValue row = applications[index];
            Require(row.GetProperty("EffectIndex").GetInt32() == index && row.GetProperty("Completed").GetBoolean() &&
                row.GetProperty("Notifications").GetInt32() == 0, "Wrong native effect order/completion/notification.");
            TrainCombatState before = row.GetProperty("Before").Deserialize<TrainCombatState>()!;
            TrainCombatState actual = row.GetProperty("Actual").Deserialize<TrainCombatState>()!;
            CombatRelicState owner = before.Context!.Relics!.Single(relic => relic.DataId == RelicOpeningSpawnModel.HordeDataId);
            Require(RelicModel.Validate(before.Context.Relics) == null && before.Context.Relics.Count(relic => relic.IsCovenant == true) == 10 &&
                before.Context.Relics.Any(relic => relic.AssetKey == "CheaperMerchants"), "Missing original trial/merchant/Covenant composition.");
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            void Replay()
            {
                TrainCombatResult result = RelicOpeningSpawnModel.Apply(before, owner.DataId, index);
                Require(result.Supported, result.UnsupportedReason ?? "Enemy opening spawn unsupported.");
                Equal(result.State, actual, "native enemy opening full state and RNG");
            }
            Replay(); Parallel.For(0, 16, _ => Replay());
            Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Opening birth mutated native parent.");
            CombatUnit[] born = actual.Rooms.SelectMany(room => room.Units).Where(unit =>
                !before.Rooms.SelectMany(room => room.Units).Any(previous => previous.Id == unit.Id)).ToArray();
            Require(born.Length == 4 + index && born.All(unit => unit.Team == CombatTeam.Enemy &&
                unit.AssetKey == "HordeT1_Forsaken" && unit.SpawnerCardId == 0 && unit.Status("cardless") == null) &&
                actual.Rooms.Single(room => room.RoomIndex == 2 - index).Units.Count(unit => born.Any(added => added.Id == unit.Id)) == born.Length,
                "Wrong enemy team, original Forsaken count/floor or cardless player lifecycle.");
            Equal(before.Context.CardRegistry, actual.Context!.CardRegistry, "Opening hero birth must not allocate fallback cards");
            if (index == 0) Guards(before, owner);
        }
        var tests = fixture.GetProperty("RelicOpeningMorselTests").EnumerateArray().ToArray();
        Require(tests.Length == 4 && tests.Count(row => row.GetProperty("Returned").GetBoolean()) == 2,
            "Missing two original positive and two isolated native invalid-room controls.");
        foreach (FixtureValue row in tests)
        {
            Require(RelicOpeningSpawnModel.Test(row.GetProperty("RoomIndex").GetInt32(), row.GetProperty("RoomCount").GetInt32()) ==
                row.GetProperty("Returned").GetBoolean(), "Native opening Test differs.");
            Equal(row.GetProperty("Before").Deserialize<TrainCombatState>(), row.GetProperty("Actual").Deserialize<TrainCombatState>(), "Read-only native Test");
        }
        EnemySpawningChecks.Native(fixture);
        BattleTurnChecks.Native(fixture);
        Console.WriteLine("OPENING-FORSAKEN-NATIVE PASS: original trial, ordered 4 top/5 middle enemies, Covenant10, merchant, full state/RNG, 16 immutable branches, native terminal win.");
    }

    private static void Guards(TrainCombatState before, CombatRelicState owner)
    {
        var mutations = new List<Action<JsonNode>> {
            node => node["DataId"] = "unknown-opening-owner",
            node => node["EffectTypes"]!.AsArray().Add(RelicOpeningSpawnModel.Effect),
            node => Effect(node)["ParamInt"] = 0,
            node => Effect(node)["OpeningSpawn"]!["Team"] = "Monsters",
            node => Effect(node)["OpeningSpawn"]!["Units"]!.AsArray().RemoveAt(0),
            node => Unit(node)["HeroDefinition"] = null,
            node => Unit(node)["CharacterId"] = "unknown-character",
            node => Unit(node)["ExternalInteractions"]!.AsArray().Add("unknown birth callback"),
            node => Unit(node)["Unit"]!["Team"] = (int)CombatTeam.Player,
            node => Unit(node)["HeroDefinition"]!["ExternalInteractions"]!.AsArray().Add("unknown hero callback")
        };
        foreach (var mutate in mutations)
        {
            JsonNode node = JsonSerializer.SerializeToNode(owner, ModelJson.Options)!; mutate(node);
            Require(RelicModel.Validate([node.Deserialize<CombatRelicState>(ModelJson.Options)!]) != null,
                "Changed trial definition bypassed native payload validation.");
        }
        JsonNode changed = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
        JsonNode trial = changed["Context"]!["Relics"]!.AsArray().Single(node => node!["DataId"]!.GetValue<string>() == owner.DataId)!;
        Unit(trial)["HeroDefinition"]!["BirthStats"]!["CovenantIds"]!.AsArray().RemoveAt(0);
        Require(!RelicOpeningSpawnModel.Apply(changed.Deserialize<TrainCombatState>(ModelJson.Options)!, owner.DataId, 0).Supported,
            "Changed Covenant birth-stat origins were accepted.");
        Console.WriteLine($"OPENING-FORSAKEN-GUARDS PASS: {mutations.Count + 1} changed owner/payload/team/lifecycle/Covenant controls rejected.");
        static JsonNode Effect(JsonNode node) => node["ResourceEffects"]![0]!;
        static JsonNode Unit(JsonNode node) => Effect(node)["OpeningSpawn"]!["Units"]![0]!;
    }

    // Upgrade only the captured projection. Preserve the exact old failing root
    // and read-only native definitions so this transformation is reproducible.
    internal static void ProjectLive(string input, string output)
    {
        JsonNode source = JsonNode.Parse(File.ReadAllText(input))!;
        Require(source["EvidenceKind"]!.GetValue<string>() == "LiveForsakenTrialOriginalAndNativeInputs" &&
            source["ReadOnlyNativePurityGuard"]!.GetValue<bool>(), "Missing native read-only inputs.");
        JsonNode root = source["OriginalRoot"]!.DeepClone();
        var definitions = source["NativeOpeningDefinitions"]!.AsArray();
        int effects = 0, diagnostics = 0;
        foreach (JsonObject node in Objects(root).Where(node => node["AssetKey"]?.GetValue<string>() == "Enemy_SpawnHordeT1").ToArray())
        {
            Require(node["DataId"]!.GetValue<string>() == RelicOpeningSpawnModel.HordeDataId, "Unexpected trial identity.");
            foreach (JsonNode? effect in node["ResourceEffects"]!.AsArray())
            {
                JsonNode native = definitions.Single(item => item!["OwnerId"]!.GetValue<string>() == RelicOpeningSpawnModel.HordeDataId &&
                    item["EffectIndex"]!.GetValue<int>() == effect!["EffectIndex"]!.GetValue<int>())!;
                effect!["OpeningSpawn"] = native["OpeningSpawn"]!.DeepClone(); effects++;
            }
        }
        foreach (JsonObject node in Objects(root).Where(node => node["Relics"] is JsonArray))
            Require(RelicModel.Validate(node["Relics"]!.Deserialize<CombatRelicState[]>(ModelJson.Options)) == null,
                "Corrected native projection still has an unmodeled relic.");
        const string legacy = "Missing or changed original start-of-combat Morsel spawn definition.";
        foreach (JsonObject node in Objects(root).Where(node => node["ExternalInteractions"] is JsonArray))
        {
            var interactions = node["ExternalInteractions"]!.AsArray();
            for (int index = interactions.Count - 1; index >= 0; index--)
                if (interactions[index]!.GetValue<string>() == legacy) { interactions.RemoveAt(index); diagnostics++; }
        }
        Require(effects >= 2 && diagnostics > 0, "The actual old failing signature was not projected.");
        var archive = new JsonObject { ["EvidenceKind"] = "LiveForsakenTrialProjectionRepair", ["NativeCapture"] = source,
            ["Root"] = root, ["ProjectedOpeningEffects"] = effects, ["RevalidatedLegacyDiagnostics"] = diagnostics };
        File.WriteAllText(output, archive.ToJsonString());
        Console.WriteLine($"LIVE-FORSAKEN-PROJECTION PASS effects={effects} revalidatedDiagnostics={diagnostics} originalNativeInputPreserved=true");
    }

    internal static void Live(FixtureValue fixture)
    {
        Require(fixture.GetProperty("EvidenceKind").GetString() == "LiveForsakenTrialProjectionRepair", "Missing reproducible live projection.");
        BattleTurnState root = fixture.GetProperty("Root").Deserialize<BattleTurnState>()!;
        Require(root.Spawn.Turn == 0 && root.Spawn.Train.Context!.Relics!.Count(relic => relic.IsCovenant == true) == 10 &&
            root.Spawn.Train.Context.Relics.Any(relic => relic.AssetKey == "PyreHeartSavagery_ArtifactData"), "Changed actual native opening composition.");
        using var keys = new BattleStateKeys();
        string parent = keys.Key(root);
        BattleTurnResult end = BattleTurnModel.EndTurn(root);
        Require(end.Supported, "Actual first EndTurn unsupported: " + end.UnsupportedReason);
        var plan = ParallelBattlePlanner.Plan(root, new BattlePlannerOptions(256, beamWidth: 8, timeLimit: TimeSpan.FromSeconds(3),
            allowShorterHorizon: true, searchToBattleEnd: true), 2);
        Require(plan.CompletedHorizon && plan.SearchedToBattleEnd && !plan.UsedShorterHorizon, "Actual position still lacks a complete continuation: " +
            JsonSerializer.Serialize(plan.UnsupportedAttempts, ModelJson.Options));
        BattleAdviceSession.Verify(root, plan);
        Require(keys.Key(root) == parent, "Live-root search/replay mutated captured input.");
        Console.WriteLine($"LIVE-FORSAKEN PASS turn=0 complete=true outcome={plan.Outcome} steps={plan.Steps.Count} turns={plan.TurnsCompleted} nodes={plan.ExpandedNodes} elapsedMs={plan.Elapsed.TotalMilliseconds:F0} fullReplay=true");
    }

    private static IEnumerable<JsonObject> Objects(JsonNode node)
    {
        if (node is JsonObject obj) yield return obj;
        IEnumerable<JsonNode?> children = node is JsonObject map ? map.Select(pair => pair.Value).ToArray() :
            node is JsonArray array ? array.ToArray() : [];
        foreach (JsonNode? child in children)
            if (child != null) foreach (JsonObject nested in Objects(child)) yield return nested;
    }
    private static void Equal<T>(T predicted, T actual, string label)
    {
        string? difference = ModelJson.Difference(JsonSerializer.Serialize(predicted, ModelJson.Options), JsonSerializer.Serialize(actual, ModelJson.Options));
        Require(difference == null, label + ": " + difference);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
