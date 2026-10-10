using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class CardBranchMaskChecks
{
    internal static void Native(FixtureValue root, bool required = false)
    {
        if (!root.TryGetProperty("BranchCardMasksEnabled", out var enabled) || !enabled.GetBoolean())
        {
            Require(!required, "Fixture does not contain enabled branch card mask acceptance.");
            return;
        }
        Require(root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("CaptureFailures").GetInt32() == 0 && root.GetProperty("Pending").GetInt32() == 0, "Branch mask capture is incomplete or from another game build.");
        var rows = root.GetProperty("BranchCardMasks").EnumerateArray().Select(row => (
            Context: row.GetProperty("Context").Deserialize<CombatContext>()!,
            Views: row.GetProperty("Views").EnumerateArray().Select(view => (
                CardId: view.GetProperty("CardId").GetInt32(),
                IgnoreTemporaryCost: view.GetProperty("IgnoreTemporaryCost").GetBoolean(),
                Actual: view.GetProperty("Actual").Deserialize<CardUpgradeMaskCard>()!)).ToArray())).ToArray();
        Require(rows.Length > 0 && rows.Sum(row => row.Views.Length) > 0 && rows.All(row => row.Context.CardRegistry != null && row.Views.Length == row.Context.CardRegistry.Count &&
            row.Views.Select(view => view.CardId).Distinct().Count() == row.Views.Length), "Branch mask oracle omits a registered card or duplicates an identity.");
        string parent = JsonSerializer.Serialize(rows);
        void Check()
        {
            foreach (var row in rows)
            foreach (var view in row.Views)
            {
                var card = row.Context.FindCard(view.CardId) ?? throw new InvalidOperationException("Branch mask oracle card is missing.");
                var predicted = CardBranchMaskModel.Resolve(card, view.IgnoreTemporaryCost);
                string modeled = JsonSerializer.Serialize(predicted), native = JsonSerializer.Serialize(view.Actual);
                Require(modeled == native, "Native branch mask view differs for card " + view.CardId + ": " + ModelJson.Difference(modeled, native));
            }
        }
        Check(); Parallel.For(0, 32, _ => Check());
        Require(JsonSerializer.Serialize(rows) == parent, "Branch card mask queries mutate a parent context.");
        int modified = rows.Sum(row => row.Context.CardRegistry!.Count(card => card.Permanent.Upgrades.Count + card.Temporary.Upgrades.Count > 0));
        Console.WriteLine($"NATIVE-BRANCH-CARD-MASK-CHECKS PASS: {rows.Length} complete decision contexts, {rows.Sum(row => row.Views.Length)} exact independently queried native card views, {modified} upgraded-card observations and 32 immutable query branches.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
