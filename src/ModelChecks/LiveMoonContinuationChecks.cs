using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;
using System.Text.Json;
using System.Text.Json.Nodes;

internal static class LiveMoonContinuationChecks
{
    internal static void Native(FixtureValue fixture)
    {
        Require(fixture.GetProperty("OriginalMoonCardUpgradesLiveRelics").GetBoolean(), "Missing original live relic composition.");
        BattleTurnState initial = fixture.GetProperty("Actions")[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        Require(initial.Spawn.Train.Context!.Relics!.Count(relic => relic.IsCovenant == true) == 10 &&
            new[] { "Enemy_LowerDraw", "FutureDraftsAreRares", "FreezeReduceCost", "PyreHeartSavagery_ArtifactData" }
                .All(asset => initial.Spawn.Train.Context.Relics.Count(relic => relic.AssetKey == asset) == 1),
            "Missing native Covenant ten and the four original live relic definitions.");
        EnemySpawningChecks.Native(fixture);
        BattleTurnChecks.Native(fixture);
        OriginalMoonCardUpgradesChecks.Native(fixture);
        Console.WriteLine("NATIVE-LIVE-MOON PASS cumulativeRank=10 originalRelics=4 fullNativeHandSpawnTurnStateAndRng=true");
    }

    internal static void Run(FixtureValue fixture)
    {
        Require(fixture.GetProperty("EvidenceKind").GetString() == "LiveUnavailableAfterPartialForecast" &&
            fixture.GetProperty("EarlierPlan").GetProperty("UsedShorterHorizon").GetBoolean() &&
            fixture.GetProperty("EarlierPlan").GetProperty("TurnsCompleted").GetInt32() == 2 &&
            fixture.GetProperty("EarlierPlan").GetProperty("State").GetProperty("Spawn").GetProperty("Turn").GetInt32() == 4,
            "Missing the actual exhausted two-turn forecast and unavailable continuation.");
        foreach (string name in new[] { "Root", "EarlierRoot" })
        {
            BattleTurnState root = fixture.GetProperty(name).Deserialize<BattleTurnState>()!;
            using var keys = new BattleStateKeys();
            string original = keys.Key(root);
            var end = BattleTurnModel.EndTurn(root);
            Require(end.Supported, "The real turn must advance through the moon draw: " + end.UnsupportedReason);
            var result = ParallelBattlePlanner.Plan(root, new BattlePlannerOptions(maxNodes: 256, beamWidth: 8,
                timeLimit: TimeSpan.FromSeconds(3), allowShorterHorizon: true, searchToBattleEnd: true), 2);
            Require(result.CompletedHorizon && !result.UsedShorterHorizon && result.SearchedToBattleEnd &&
                result.Outcome is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated or RoomOutcome.Stalemate,
                "The default parallel search must reach a terminal outcome in the actual saved position.");
            BattleAdviceSession.Verify(root, result);
            Require(original == keys.Key(root), "Continuation search or full-state/RNG replay mutated its native parent.");
            if (name == "Root") Guards(end.State!.Spawn.Train.Context!);
            Console.WriteLine($"LIVE-MOON PASS {name} rootTurn={root.Spawn.Turn} outcome={result.Outcome} complete=true turns={result.TurnsCompleted} steps={result.Steps.Count} nodes={result.ExpandedNodes} elapsedMs={result.Elapsed.TotalMilliseconds:F0} fullReplay=true");
        }
    }

    private static void Guards(CombatContext context)
    {
        Require(context.Cards.Hand.Any(token => CardMoonUpgradeModel.Present(context.FindCard(token.InstanceId)!)),
            "Missing drawn moon spell at the failing native boundary.");
        string parent = JsonSerializer.Serialize(context, ModelJson.Options);
        var mutations = new List<Action<JsonNode>> {
            node => Owners(node)[0]!["DataId"] = "unknown",
            node => Owners(node)[3]!["EffectTypes"]!.AsArray().Add("RelicEffectNull"),
            node => Owners(node)[4]!["EffectTypes"] = new JsonArray("RelicEffectAddCardsStartOfRun"),
            node => Owners(node)[9]!["EffectTypes"]!.AsArray().Add("RelicEffectNull"),
            node => { JsonArray relics = node["Relics"]!.AsArray(); relics.Add(Owners(node)[0]!.DeepClone()); },
            node => { var owners = Owners(node); string id = owners[7]!["DataId"]!.GetValue<string>(); owners[7]!["DataId"] = owners[8]!["DataId"]!.GetValue<string>(); owners[8]!["DataId"] = id; },
            node => Relic(node, "FreezeReduceCost")["CardModifiers"]![0]!["FreezeHandUpgrade"]!["Upgrade"]!["Stats"]!["Cost"] = -98,
            node => Relic(node, "PyreHeartSavagery_ArtifactData")["CardModifiers"]![0]!["Rule"]!["Filters"]![0]!["CardType"] = "Spell",
            node => Relic(node, "Enemy_LowerDraw")["EffectTypes"]!.AsArray().Add("RelicEffectAddTempUpgrade"),
            node => Relic(node, "FutureDraftsAreRares")["EffectTypes"]!.AsArray().Add("RelicEffectNull")
        };
        foreach (var mutate in mutations)
        {
            JsonNode changed = JsonNode.Parse(parent)!; mutate(changed);
            CardMoonUpgradeModel.RefreshHand(changed.Deserialize<CombatContext>(ModelJson.Options)!, out string? error);
            Require(error != null, "Changed native composition bypassed the moon callback guard.");
        }
        Require(parent == JsonSerializer.Serialize(context, ModelJson.Options), "Moon guards mutated their native parent.");
        JsonNode renamed = JsonNode.Parse(parent)!;
        Relic(renamed, "FutureDraftsAreRares")["DataId"] = "another-draft-only-owner";
        CardMoonUpgradeModel.RefreshHand(renamed.Deserialize<CombatContext>(ModelJson.Options)!, out string? passiveError);
        Require(passiveError == null, "A sealed reward-only effect was restricted by relic identity instead of its native callback signature.");
        Console.WriteLine($"LIVE-MOON-GUARDS PASS mutations={mutations.Count} cumulativeRank=10 originalRelics=4");
        static JsonNode[] Owners(JsonNode node) => node["Relics"]!.AsArray().Where(relic => relic!["IsCovenant"]!.GetValue<bool>()).Select(relic => relic!).ToArray();
        static JsonNode Relic(JsonNode node, string asset) => node["Relics"]!.AsArray().Single(relic => relic!["AssetKey"]!.GetValue<string>() == asset)!;
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}
