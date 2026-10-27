using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class UnitCopyBirthCatalogChecks
{
    internal const string Reason = "Native copy catalog must include queued bosses and triggered births.";
    private static readonly string[] Added = ["Boss_Level3_Arkion_SiphonRage", "MonsterFunguy"];
    internal static void Native(FixtureValue source, FixtureValue replay)
    {
        var repaired = NativeBranchObservationChecks.Replay(replay, Reason);
        var original = source.GetProperty("Rows").EnumerateArray().ToArray();
        Require(source.GetProperty("NativeCompleted").GetBoolean() && original.Length == 4 && repaired.Length == 4 &&
            source.GetProperty("GameModuleMvid").ContentEquals(replay.GetProperty("GameModuleMvid")) &&
            source.GetProperty("Request").GetProperty("Reason").GetString() == Reason &&
            source.GetProperty("Request").GetProperty("Prefix").ContentEquals(replay.GetProperty("Request").GetProperty("Prefix")) &&
            source.GetProperty("Request").GetProperty("Action").ContentEquals(replay.GetProperty("Request").GetProperty("Action")),
            "Changed native catalog capture boundary or input prefix.");
        var oldRoot = Read(original[0], "Before");
        var newRoot = Read(repaired[0], "Before");
        var definitions = newRoot.PlayRules!.UnitCopyCatalog!.Births.Where(birth => Added.Contains(birth.Unit.AssetKey))
            .ToDictionary(birth => birth.Unit.AssetKey, birth => JsonSerializer.Serialize(birth, ModelJson.Options));
        Require(definitions.Count == 2 && oldRoot.PlayRules!.UnitCopyCatalog!.Births.Count == 12 &&
            newRoot.PlayRules.UnitCopyCatalog.Births.Count == 14 &&
            !oldRoot.PlayRules.UnitCopyCatalog.Births.Any(birth => Added.Contains(birth.Unit.AssetKey)),
            "Missing historical omitted boss/cardless definitions.");
        BattleTurnState current = source.GetProperty("Request").GetProperty("Root").Deserialize<BattleTurnState>()!;
        for (int index = 0; index < original.Length; index++) {
            var before = Read(original[index], "Before");
            var actual = Read(original[index], "Actual");
            Require(NativeBranchObservationChecks.Key(current) == NativeBranchObservationChecks.Key(before), "Broken historical native chain.");
            var action = original[index].GetProperty("Action").Deserialize<PlayCardAction>();
            bool end = original[index].GetProperty("IsEndTurn").GetBoolean();
            var predicted = BattlePlannerChecks.Replay(before, [new BattleSearchStep(action, end)], out var outcome);
            Require(outcome == RoomOutcome.Exchanged &&
                NativeBranchObservationChecks.Key(predicted) == NativeBranchObservationChecks.Key(Read(original[index], "Expected")),
                "Historical prediction changed beyond the catalog capture defect.");
            Require(index == 3 ? NativeBranchObservationChecks.Key(predicted) != NativeBranchObservationChecks.Key(actual) :
                NativeBranchObservationChecks.Key(predicted) == NativeBranchObservationChecks.Key(actual),
                "Preserve the original first-EndTurn catalog mismatch.");
            Compare(predicted, actual);
            foreach (string field in new[] { "Before", "Actual" }) Compare(Read(original[index], field), Read(repaired[index], field));
            foreach (string field in new[] { "Action", "IsEndTurn", "Boundary" })
                Require(original[index].GetProperty(field).ContentEquals(repaired[index].GetProperty(field)), "Independent native inputs changed.");
            current = actual;
        }
        Require(NativeBranchObservationChecks.Key(Read(original[^1], "Before")) == NativeBranchObservationChecks.Key(
            source.GetProperty("Request").GetProperty("FailedRoot").Deserialize<BattleTurnState>()!) &&
            Read(original[^1], "Actual").PlayRules!.UnitCopyCatalog!.Births.Count == 13 &&
            original[^1].GetProperty("IsEndTurn").GetBoolean(), "Wrong historical catalog transition.");
        var overlay = source.GetProperty("PlayerAdvisor").GetProperty("Overlay");
        Require(overlay.GetProperty("PurityFailures").GetInt32() == 0 && overlay.GetProperty("Failures").GetInt32() == 0 &&
            !overlay.GetProperty("InTestScenario").GetBoolean(), "Historical native player capture was not pure.");
        var parent = Read(repaired[^1], "Before");
        string parentKey = NativeBranchObservationChecks.Key(parent);
        Parallel.For(0, 4, _ => {
            var result = BattleTurnModel.EndTurn(parent);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) ==
                NativeBranchObservationChecks.Key(Read(repaired[^1], "Actual")), "Catalog repair changed native successor/RNG.");
        });
        Require(parentKey == NativeBranchObservationChecks.Key(parent), "Catalog branch mutated its parent.");
        Console.WriteLine("UNIT-COPY-BIRTH-CATALOG PASS: independent four-input native capture pair; only two exact newly captured birth definitions; all physics/forecast/RNG unchanged; repaired full keys and four immutable branches");

        void Compare(BattleTurnState left, BattleTurnState right)
        {
            foreach (var state in new[] { left, right })
                foreach (var birth in state.PlayRules!.UnitCopyCatalog!.Births.Where(birth => Added.Contains(birth.Unit.AssetKey)))
                    Require(JsonSerializer.Serialize(birth, ModelJson.Options) == definitions[birth.Unit.AssetKey], "Changed added native birth payload.");
            Require(Project(left) == Project(right), "Capture repair changed another definition or physical state/RNG.");
        }
    }
    private static BattleTurnState Read(FixtureValue row, string field) => row.GetProperty(field).Deserialize<BattleTurnState>()!;
    // Historical capture comparison only. Runtime/search keys retain the entire catalog.
    private static string Project(BattleTurnState state)
    {
        var node = JsonNode.Parse(NativeBranchObservationChecks.Key(state))!;
        var births = node["PlayRules"]!["UnitCopyCatalog"]!["Births"]!.AsArray();
        for (int index = births.Count - 1; index >= 0; index--)
            if (Added.Contains(births[index]!["Unit"]!["AssetKey"]!.GetValue<string>())) births.RemoveAt(index);
        return node.ToJsonString();
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
