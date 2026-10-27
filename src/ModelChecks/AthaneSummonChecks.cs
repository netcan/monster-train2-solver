using System.Text.Json;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class AthaneSummonChecks
{
    internal const string Reason = "Triggered enemy birth requires its calibrated original Fleshweaver mini definition.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason);
        Require(rows.Length == 34 && rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Incomplete original Athane prefix.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var room = actual.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0);
        var boss = room.Units.Single(unit => unit.Id == 22);
        var rock = room.Units.Single(unit => unit.Id == 24);
        Require(before.Spawn.Turn == 6 && before.Spawn.NextUnitId == 21 && actual.Spawn.Turn == 7 && actual.Spawn.NextUnitId == 25 &&
            boss.AssetKey == "Boss_Level7_Athane" && boss.Health == 1100 && rock.AssetKey == "SpikyRockT3_Chosen_Boss_BasicNonAttack" &&
            rock.Health == 250 && rock.Status("spikes")?.Stacks == 7 && rock.Status("immobile")?.Stacks == 1 &&
            room.Units.Select(unit => unit.Id).SequenceEqual(new[] { 24, 21, 22, 23 }), "Missing exact original Athane birth/front reorder/immobile.");
        string parent = NativeBranchObservationChecks.Key(before);
        Parallel.For(0, 4, _ => {
            var ended = BattleTurnModel.EndTurn(before);
            Require(ended.Supported && NativeBranchObservationChecks.Key(ended.State!) == NativeBranchObservationChecks.Key(actual),
                "Athane birth differs from native state/forecast/RNG.");
        });
        Require(parent == NativeBranchObservationChecks.Key(before), "Athane branch mutated its parent.");
        var rule = boss.Triggers.Single(trigger => trigger.Kind == "OnSpawn").Effects.Single().Summon!;
        Require(TriggeredSummonModel.Validate(room, rule) == null, "Original singleton pool is not supported.");
        foreach (string control in new[] { "Pool", "Count", "Parent", "World", "Asset", "Health", "Spikes" })
        {
            var node = JsonSerializer.SerializeToNode(room, ModelJson.Options)!;
            var changedRule = JsonSerializer.SerializeToNode(rule, ModelJson.Options)!;
            if (control == "Pool") changedRule["Pool"]!.AsArray().Add(rule.Pool[0]);
            if (control == "Count") changedRule["Count"] = 2;
            if (control == "Parent") changedRule["HasParentCard"] = true;
            if (control == "World") node["Context"]!["Enchantments"] = null;
            var unit = node["Context"]!["SummonCatalog"]!["Units"]![0]!["HeroDefinition"]!["Unit"]!;
            if (control == "Asset") unit["AssetKey"] = "uncalibrated-rock";
            if (control == "Health") unit["Health"] = 251;
            if (control == "Spikes") unit["Statuses"]![0]!["Stacks"] = 8;
            Require(TriggeredSummonModel.Validate(node.Deserialize<RoomCombatState>(ModelJson.Options)!,
                changedRule.Deserialize<TriggeredSummonRule>(ModelJson.Options)!) != null, "Unknown Athane signature lost guard: " + control);
        }
        if (replay.HasValue) { NativeBranchObservationChecks.Replay(replay.Value, Reason); NativeBranchObservationChecks.Pair(source, replay.Value); }
        Console.WriteLine("ATHANE-SUMMON PASS:34 original inputs/full state/forecast/RNG; singleton-pool draw, rock24/front reorder/immobile; four immutable branches/seven guards; independentPair=" + replay.HasValue);
    }
    internal static void ExportRequest(string deploymentPath, string priorRequestPath, string output)
    {
        using var deployment = ModelJson.ReadFixture(deploymentPath);
        using var prior = ModelJson.ReadFixture(priorRequestPath);
        var root = deployment.RootElement.GetProperty("Root").Deserialize<BattleTurnState>()!;
        var overlay = deployment.RootElement.GetProperty("PlayerAdvisor").GetProperty("Overlay");
        Require(deployment.RootElement.GetProperty("EvidenceKind").GetString() == "NativeDeploymentObservation" &&
            !overlay.GetProperty("InTestScenario").GetBoolean() && overlay.GetProperty("PurityFailures").GetInt32() == 0 &&
            root.Spawn.Turn == 0 && root.Spawn.Train.Context is { Enchantments.AutomaticLifecycle: true,
                SummonCatalog.Units.Count: 1 }, "Missing fresh pure deployment with original enemy birth world.");
        var prefix = prior.RootElement.GetProperty("Prefix").EnumerateArray().Select(step => new BattleSearchStep(
            step.GetProperty("Action").Deserialize<PlayCardAction>(), step.GetProperty("IsEndTurn").GetBoolean())).ToArray();
        Require(prefix.Length == 33 && prior.RootElement.GetProperty("IsEndTurn").GetBoolean(), "Wrong original Athane prefix.");
        var current = BattlePlannerChecks.Replay(root, prefix, out var outcome);
        Require(outcome == RoomOutcome.Exchanged && current.Spawn.Turn == 6, "Fresh projection cannot replay the unchanged input prefix.");
        using var document = NativeFixtureCapture.Capture(new { Schema = 1, EvidenceKind = "ModelUnsupportedBranch",
            SourceArchive = Path.GetFileName(deploymentPath), SourceSHA256 = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(deploymentPath))).ToLowerInvariant(),
            Root = root, Prefix = prefix, FailedRoot = current, Action = (PlayCardAction?)null, IsEndTurn = true,
            Reason, NativeValidationRequired = true });
        using var stream = new FileStream(output, FileMode.CreateNew, FileAccess.Write);
        document.Write(stream);
        Console.WriteLine("ATHANE-REQUEST prefix33 preserved, fresh native deployment/catalog/world; native validation required.");
    }
    private static void Require(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
}
