using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalNullRelicChecks
{
    private const string Id = "815a25e7-2fed-4cb2-a568-79af12cd7116";
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path); var root = document.RootElement;
        Require(root.GetProperty("Schema").GetInt32() == 1, "Null equivalence bundle schema differs.");
        var baseline = root.GetProperty("Baseline"); var owned = root.GetProperty("Owned");
        foreach (var input in new[] { baseline, owned })
        {
            Require(input.GetProperty("GameVersion").GetString() == "2.2.1" && input.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
                input.GetProperty("NativeWon").GetBoolean() && input.GetProperty("TerminalEffectsSettled").GetBoolean() &&
                new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }.All(name => input.GetProperty(name).GetInt32() == 0), "Null equivalence battle failed native acceptance.");
            var evidence = input.GetProperty("OriginalNullRelic"); bool isBaseline = evidence.GetProperty("Baseline").GetBoolean();
            Require(evidence.GetProperty("Id").GetString() == Id && evidence.GetProperty("Key").GetString() == "StarterWurmRelic" &&
                evidence.GetProperty("Type").GetString() == "RelicEffectNull" && evidence.GetProperty("Interfaces").EnumerateArray().Select(value => value.GetString()).SequenceEqual(["IOverridesEditorInspectorDescriptions", "IRelicEffect"]) &&
                evidence.GetProperty("Present").GetInt32() == (isBaseline ? 0 : 1) && evidence.GetProperty("Before").ContentEquals(evidence.GetProperty("Reacquired")),
                "Original null identity, interfaces or acquisition snapshot differs.");
            if (!isBaseline) Require(evidence.GetProperty("Before").ContentEquals(evidence.GetProperty("Owned")) && evidence.GetProperty("Before").ContentEquals(evidence.GetProperty("Removed")),
                "Native null acquisition/removal changed recorded state/RNG.");
            BattleTurnChecks.Native(input); BattleActionChecks.Native(input);
        }
        Require(baseline.GetProperty("OriginalNullRelic").GetProperty("Baseline").GetBoolean() && !owned.GetProperty("OriginalNullRelic").GetProperty("Baseline").GetBoolean(), "Baseline/owned bundle inputs reversed.");
        int actions = 0, turns = 0;
        foreach (string kind in new[] { "Actions", "Turns" })
        {
            var left = baseline.GetProperty(kind).EnumerateArray().ToArray(); var right = owned.GetProperty(kind).EnumerateArray().ToArray(); Require(left.Length == right.Length && left.Length > 0, "Native no-op policy length differs.");
            for (int i = 0; i < left.Length; i++)
            {
                foreach (string field in new[] { "Before", "Actual" })
                {
                    var a = left[i].GetProperty(field).Deserialize<BattleTurnState>()!; var b = right[i].GetProperty(field).Deserialize<BattleTurnState>()!;
                    Require(a.Spawn.Train.Context!.Relics!.All(owner => owner.DataId != Id) && b.Spawn.Train.Context!.Relics!.Count(owner => owner.DataId == Id && owner.AssetKey == "StarterWurmRelic" && owner.EffectTypes.SequenceEqual([RelicModel.Null])) == 1,
                        "Null native ownership lost or introduced into the baseline.");
                    Equal(a, b, "complete native " + kind + " " + i + " " + field);
                }
                if (kind == "Actions") { Require(left[i].GetProperty("Action").ContentEquals(right[i].GetProperty("Action")), "Null ownership changed the native chosen action."); actions++; }
                else turns++;
            }
        }
        Console.WriteLine($"ORIGINAL-NULL-RELIC-NATIVE PASS: unchanged StarterWurmRelic, base/editor interfaces only, actual acquire/remove/reacquire without recorded resource/RNG changes; two winning native seeded policies equivalent across {actions} actions/{turns} EndTurns and complete contexts except owner identity. Both independently replay, including 16 parallel branches. No activation claimed; ownership still counts as a relic.");
    }
    private static void Equal(BattleTurnState baseline, BattleTurnState owned, string label)
    {
        JsonNode Normalize(BattleTurnState state)
        {
            var root = JsonSerializer.SerializeToNode(state, ModelJson.Options)!;
            void Visit(JsonNode? node)
            {
                if (node is JsonObject obj)
                {
                    if (obj["Relics"] is JsonArray relics)
                        for (int i = relics.Count - 1; i >= 0; i--) if (relics[i]?["DataId"]?.GetValue<string>() == Id) relics.RemoveAt(i);
                    foreach (var property in obj) Visit(property.Value);
                }
                else if (node is JsonArray array) foreach (var value in array) Visit(value);
            }
            Visit(root); return root;
        }
        var a = Normalize(baseline); var b = Normalize(owned);
        Require(JsonNode.DeepEquals(a, b), "Null equivalence " + label + " differs: " + ModelJson.Difference(a.ToJsonString(), b.ToJsonString()));
    }
    private static void Require(bool pass, string message) { if (!pass) throw new InvalidOperationException(message); }
}
