using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;
using MonsterTrain2Poju.Capture;
using Newtonsoft.Json.Linq;

internal static class PostBattleRewardCombatChecks
{
    private const string Effect = "RelicEffectAddPostBattleReward";
    private const string Reason = "Unmodeled relic effect " + Effect + " on VoidPortal.";

    internal static void Pair(FixtureValue source, FixtureValue replay)
    {
        // Preserve the rejected original. Only its exact obsolete diagnostic
        // may be removed from the comparison, never a native state/RNG field.
        using var normalized = Normalize(source);
        using var fixedDocument = Normalize(replay);
        var originalRows = normalized.RootElement.GetProperty("Rows");
        var rows = NativeBranchObservationChecks.Replay(fixedDocument.RootElement, Reason);
        Require(originalRows.GetArrayLength() == 1 && rows.Length == 1 &&
            originalRows[0].GetProperty("Expected").ValueKind == FixtureKind.Null &&
            originalRows[0].GetProperty("UnsupportedReason").GetString() == Reason &&
            rows[0].GetProperty("Expected").ValueKind == FixtureKind.Object &&
            rows[0].GetProperty("IsEndTurn").GetBoolean(),
            "Expected the retained refusal and repaired native deployment EndTurn.");
        NativeBranchObservationChecks.Replay(normalized.RootElement, Reason);
        NativeBranchObservationChecks.Pair(normalized.RootElement, fixedDocument.RootElement);
        var before = rows[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var after = rows[0].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(before.Spawn.Turn == 0 && after.Spawn.Turn == 1,
            "Wrong post-battle owner combat boundary.");
        foreach (var state in new[] { before, after })
        {
            var owner = state.Spawn.Train.Context!.Relics!.Single(relic => relic.AssetKey == "VoidPortal");
            Require(owner.DataId == "5cad6ad0-769b-46d3-9464-514b8e74190a" &&
                owner.EffectTypes.SequenceEqual([Effect]) && RelicModel.Validate([owner]) == null,
                "Original acquired VoidPortal/effect changed.");
            const string unknown = "RelicEffectUncalibratedCombatCallback";
            foreach (string[] effects in new[] { new[] { Effect, unknown }, new[] { unknown, Effect } })
                Require(RelicModel.Validate([new CombatRelicState(owner.DataId, owner.AssetKey, effects)]) ==
                    "Unmodeled relic effect " + unknown + " on VoidPortal.",
                    "The reward-only effect bypassed an unknown companion callback.");
            Require(RelicModel.Validate([new CombatRelicState(owner.DataId, owner.AssetKey,
                ["RelicEffectAddPostBattleRewardOnBossDeath"])]) != null,
                "A different, uncalibrated reward effect was accepted.");
        }
        Console.WriteLine("POST-BATTLE-REWARD-COMBAT PASS: retained refusal and independent repaired deployment EndTurn; full state/RNG, immutable parents and unknown-companion/order/other-type guards. No combat reward grant or default victory claim.");
    }

    private static FixtureDocument Normalize(FixtureValue fixture)
    {
        var node = JObject.Parse(JsonSerializer.Serialize(fixture.ToObjectGraph(), ModelJson.Options));
        foreach (var array in node.SelectTokens("$..ExternalInteractions").OfType<JArray>().ToArray())
            foreach (var value in array.Where(value => value.Type == JTokenType.String && value.Value<string>() == Reason).ToArray())
                value.Remove();
        foreach (string name in new[] { "HistoricalRequestSHA256", "FreshDeploymentSHA256", "RemovedHistoricalDiagnosticCount" })
            ((JObject)node["Request"]!).Property(name)?.Remove();
        return NativeFixtureCapture.Capture(node);
    }

    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
