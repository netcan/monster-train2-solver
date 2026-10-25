using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;
using MonsterTrain2Poju.Capture;
using Newtonsoft.Json.Linq;

internal static class DraftUpgradeCombatChecks
{
    private const string Effect = "RelicEffectAddStartingUpgradeToCardDrafts";
    private const string Reason = "Unmodeled relic effect " + Effect + " on UpgradedDrafts.";

    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        using var normalized = NormalizeHistoricalGuard(source);
        var rows = NativeBranchObservationChecks.Replay(normalized.RootElement, Reason);
        Require(rows.Length == 1 && rows[0].GetProperty("IsEndTurn").GetBoolean(), "Expected a single native deployment EndTurn.");
        var before = rows[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var after = rows[0].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        foreach (var state in new[] { before, after })
        {
            var owner = state.Spawn.Train.Context!.Relics!.Single(relic => relic.AssetKey == "UpgradedDrafts");
            Require(owner.DataId == "9e0e5d4e-6d16-43f1-8cd4-cc4c2b431afd" && owner.EffectTypes.SequenceEqual([Effect]),
                "Original draft-upgrade owner/effect changed.");
        }
        Require(before.Spawn.Turn == 0 && after.Spawn.Turn == 1, "Wrong native draft-owner combat boundary.");
        // A recognized draft hook must not hide another unmodeled effect on
        // the same relic. The effect loop still checks every captured type.
        var node = JsonSerializer.SerializeToNode(before)!;
        var relic = node["Spawn"]!["Train"]!["Context"]!["Relics"]!.AsArray()
            .Single(item => item!["AssetKey"]!.GetValue<string>() == "UpgradedDrafts")!;
        relic["EffectTypes"]!.AsArray().Add("UncalibratedDraftCombatEffect");
        var unknown = BattleTurnModel.EndTurn(node.Deserialize<BattleTurnState>()!);
        Require(!unknown.Supported && unknown.UnsupportedReason == "Unmodeled relic effect UncalibratedDraftCombatEffect on UpgradedDrafts.",
            "Draft-only effect bypassed an unknown companion combat effect.");
        if (replay is FixtureValue second)
        {
            using var fixedDocument = NormalizeHistoricalGuard(second);
            NativeBranchObservationChecks.Replay(fixedDocument.RootElement, Reason);
            NativeBranchObservationChecks.Pair(normalized.RootElement, fixedDocument.RootElement);
        }
        Console.WriteLine("DRAFT-UPGRADE-COMBAT PASS: original acquired owner, native deployment EndTurn/full state/RNG, immutable parents and unknown companion guard; independentPair=" + replay.HasValue);
    }

    private static FixtureDocument NormalizeHistoricalGuard(FixtureValue fixture)
    {
        // Old capture duplicated this exact rejected relic message into room
        // and enchantment diagnostics. Normalize only the retained test input;
        // production keeps its ordinary guards and fresh captures need no edit.
        var node = JObject.Parse(JsonSerializer.Serialize(fixture.ToObjectGraph(), ModelJson.Options));
        foreach (var array in node.SelectTokens("$..ExternalInteractions").OfType<JArray>().ToArray())
            foreach (var value in array.Where(value => value.Type == JTokenType.String && value.Value<string>() == Reason).ToArray())
                value.Remove();
        return NativeFixtureCapture.Capture(node);
    }

    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
