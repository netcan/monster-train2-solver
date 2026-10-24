using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class LiveArkionPyreChecks
{
    internal static void Run(FixtureValue live, FixtureValue native)
    {
        bool completeNative = native.TryGetProperty("Turns", out FixtureValue turns);
        if (completeNative)
            foreach (string counter in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
                Require(native.GetProperty(counter).GetInt32() == 0, "Native failure: " + counter);
        BattleTurnState before = live.GetProperty("Root").Deserialize<BattleTurnState>()!;
        Require(before.Spawn.Turn == 0 && before.Spawn.QueuedOuterBoss == null &&
            before.Spawn.ExternalInteractions.SequenceEqual(new[] { "Outer or queued final boss spawning" }) &&
            live.GetProperty("Diagnostic").GetProperty("Error").GetString()!.Contains("Outer or queued final boss spawning"),
            "Missing the original live deployment rejection.");
        Require(!BattleTurnModel.EndTurn(before).Supported, "An absent boss definition must remain rejected.");
        BattleTurnState accepted = (completeNative ? turns[0].GetProperty("Before") : native.GetProperty("Root"))
            .Deserialize<BattleTurnState>()!;
        OuterBossSpawnDefinition definition = accepted.Spawn.QueuedOuterBoss!;
        Require(definition?.Actor.Unit.AssetKey == "Boss_Level3_Arkion_DamagePyre" &&
            definition.Actor.ExternalInteractions.Count == 0 && definition.Actor.BirthStats?.CovenantIds.Count == 10 &&
            TrueFinalBossCovenantModel.MatchesOrigins(before.Spawn.Train.Context, definition.Actor.BirthStats),
            "Missing a native accepted birth with the same live Covenant origins.");
        string original = JsonSerializer.Serialize(before, ModelJson.Options);
        // The old player omitted the boss and trial trigger definitions. Restore
        // only those definitions from the independent run of its exact save
        // backup; never inject a native successor, hand, board or RNG.
        JsonNode projected = JsonNode.Parse(original)!;
        projected["Spawn"]!["QueuedOuterBoss"] = JsonSerializer.SerializeToNode(definition, ModelJson.Options);
        projected["Spawn"]!["ExternalInteractions"] = new JsonArray();
        CombatRelicState trial = accepted.Spawn.Train.Context!.Relics!.Single(ArkionAttackTrial.IsOwner);
        Require(ArkionAttackTrial.Validate(trial) == null, "Missing the original native trial trigger definition.");
        void ProjectTrial(JsonNode node)
        {
            if (node is JsonObject obj && obj["DataId"]?.ToString() == ArkionAttackTrial.DataId && obj.ContainsKey("UnitUpgradeTriggers"))
                obj["UnitUpgradeTriggers"] = JsonSerializer.SerializeToNode(trial.UnitUpgradeTriggers, ModelJson.Options);
            if (node is JsonObject properties)
                foreach (var property in properties.ToArray()) { if (property.Value != null) ProjectTrial(property.Value); }
            else if (node is JsonArray array)
                foreach (JsonNode? child in array) { if (child != null) ProjectTrial(child); }
        }
        ProjectTrial(projected);
        BattleTurnState root = projected.Deserialize<BattleTurnState>(ModelJson.Options)!;
        BattleTurnResult end = BattleTurnModel.EndTurn(root);
        Require(end.Supported, "Live deployment still fails: " + end.UnsupportedReason);
        BattlePlanResult plan = ParallelBattlePlanner.Plan(root, new BattlePlannerOptions(maxNodes: 256,
            maxTurns: 3, beamWidth: 8, timeLimit: TimeSpan.FromSeconds(10), allowShorterHorizon: true,
            searchToBattleEnd: true, stopAtBestPyreHealth: true), 2);
        Require(!plan.Cancelled && plan.Steps.Count > 0 && (plan.CompletedHorizon || plan.UsedShorterHorizon),
            "Live position still has no verified continuation: " + string.Join("; ", plan.UnsupportedAttempts.Select(a => a.Reason).Distinct()));
        BattleAdviceSession.Verify(root, plan);
        Require(original == JsonSerializer.Serialize(before, ModelJson.Options), "Live input was mutated.");
        JsonNode spoofed = projected.DeepClone();
        spoofed["Spawn"]!["QueuedOuterBoss"]!["Actor"]!["BirthStats"]!["CovenantIds"]![0] = "another-origin";
        Require(!BattleTurnModel.EndTurn(spoofed.Deserialize<BattleTurnState>(ModelJson.Options)!).Supported,
            "Changed birth origins bypassed validation.");
        JsonNode changedTrial = JsonSerializer.SerializeToNode(trial, ModelJson.Options)!;
        changedTrial["UnitUpgradeTriggers"]![0]!["Upgrade"]!["Stats"]!["Damage"] = 6;
        Require(ArkionAttackTrial.Validate(changedTrial.Deserialize<CombatRelicState>(ModelJson.Options)!) != null,
            "Changed trial attack upgrade bypassed validation.");
        CombatRelicState[] owners = root.Spawn.Train.Context!.Relics!.Where(owner => owner.IsCovenant == true).ToArray();
        CombatRelicState merchant = owners.Single(owner => owner.AssetKey == "Covenant03_MerchantCostsIncrease");
        var changed = new CombatRelicState(merchant.DataId, merchant.AssetKey,
            merchant.EffectTypes.Append("RelicEffectAddTrigger").ToArray(), isCovenant: true);
        Require(!TrueFinalBossCovenantModel.CalibratedOuterBossOwners(owners.Select(o => o == merchant ? changed : o).ToArray(), definition.Actor.Unit.AssetKey) &&
            !TrueFinalBossCovenantModel.CalibratedOuterBossOwners(owners.Append(merchant).ToArray(), definition.Actor.Unit.AssetKey) &&
            !TrueFinalBossCovenantModel.CalibratedOuterBossOwners(owners, "Boss_Level3_Arkion_DamagePyre_TitanTrial"),
            "Changed owner signatures, duplicate owners or an unknown boss bypassed validation.");
        Console.WriteLine($"LIVE-ARKION-PYRE PASS: rank=10 steps={plan.Steps.Count} turns={plan.TurnsCompleted} " +
            $"outcome={plan.Outcome} complete={plan.CompletedHorizon} searchMs={plan.Elapsed.TotalMilliseconds:F1}; independent full-state/RNG replay and negative birth guards.");
    }

    private static void Require(bool condition, string reason)
    { if (!condition) throw new InvalidOperationException(reason); }
}
