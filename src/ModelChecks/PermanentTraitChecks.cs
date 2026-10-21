using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class PermanentTraitChecks
{
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("ContextUnchanged").GetBoolean() &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("FrameBefore").ContentEquals(root.GetProperty("FrameAfter")) &&
            root.GetProperty("RngBefore").ContentEquals(root.GetProperty("RngAfter")), "Permanent trait capture changed native decision/RNG/frame.");
        int count = 0; var groups = new HashSet<string>();
        foreach (var row in root.GetProperty("Rows").EnumerateArray())
        {
            var before = row.GetProperty("Before").Deserialize<CardInstanceState>()!;
            var upgrade = row.GetProperty("Upgrade").Deserialize<CardUpgradeModifier>()!;
            var actual = row.GetProperty("Actual").Deserialize<CardInstanceState>()!;
            string parent = Json(before), group = row.GetProperty("Group").GetString()!;
            Require(CardExhaustModel.PermanentUpgradeError(before, upgrade) == null, "Permanent trait operation remains rejected.");
            Require(CardExhaustModel.PermanentUpgradeError(CardInstanceState.Empty(before.InstanceId, before.DataId), upgrade) != null,
                "Permanent trait operation accepted missing branch/private state.");
            var predicted = before.WithPermanentUpgrade(upgrade, false);
            string? difference = ModelJson.Difference(Json(predicted), Json(actual));
            Require(difference == null, group + ": " + difference);
            Require(parent == Json(before), "Permanent trait operation changed its parent.");
            groups.Add(group); count++;
        }
        Require(count == 176 && groups.Count == 96, "Incomplete permanent trait matrix.");
        Console.WriteLine("PERMANENT-TRAITS PASS: 176 native operations; five trait families with all four stack modes, " +
            "duplicate applications, clobber protection, retained Consume/Holdover private state, Ephemeral/CopyOnPlay removal, " +
            "temporary/nonremovable controls and immutable full cards.");
    }
    private static string Json<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
