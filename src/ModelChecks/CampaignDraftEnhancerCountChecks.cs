using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class CampaignDraftEnhancerCountChecks
{
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path); var root = document.RootElement;
        Require(root.GetProperty("Schema").GetInt32() == 1 && root.GetProperty("GameVersion").GetString() == "2.2.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("Before").ContentEquals(root.GetProperty("After")), "Draft-count identity or recorded state/frame/RNG restoration differs.");
        var owner = root.GetProperty("Owner"); var runtime = owner.GetProperty("Runtime");
        Require(owner.GetProperty("Id").GetString() == "9427e610-0c7b-4430-ab1f-c84049097168" && owner.GetProperty("Key").GetString() == "DrawOnCorruption" &&
            owner.GetProperty("Type").GetString() == "RelicEffectChangeNumCardsToApplyRandomDraftEnhancerTo" &&
            owner.GetProperty("Amount").GetInt32() == 1 && owner.GetProperty("Conditions").GetInt32() == 0 &&
            !owner.GetProperty("DisallowedInPlacement").GetBoolean() && runtime.GetProperty("Type").GetString() == "Additive" &&
            runtime.GetProperty("Condition").GetString() == "NumCardsToApplyRandomDraftEnhancerTo" && runtime.GetProperty("Value").GetSingle() == 1f,
            "Original draft-enhancer-count definition or initialized balance interface changed.");
        string[] phases = ["baseline", "owned", "removed", "reacquired", "removed-again"];
        var consumers = root.GetProperty("Consumers").EnumerateArray().ToArray();
        Require(consumers.Select(row => row.GetProperty("Phase").GetString()).SequenceEqual(phases), "Missing acquisition/removal/reset consumer sequence.");
        int positive = 0;
        foreach (var row in consumers)
        {
            string phase = row.GetProperty("Phase").GetString()!; bool owned = phase is "owned" or "reacquired";
            int[] additions = row.GetProperty("Additions").Deserialize<int[]>()!;
            Require(row.GetProperty("Owned").GetBoolean() == owned && additions.SequenceEqual(owned ? [1] : Array.Empty<int>()) &&
                row.GetProperty("Actual").GetInt32() == CampaignRelicModel.RandomDraftEnhancedCardCount(additions) &&
                row.GetProperty("Actual").GetInt32() == (owned ? 2 : 1) && row.GetProperty("Before").ContentEquals(row.GetProperty("After")),
                "Actual SaveManager count consumer, native registration or query state/RNG differs.");
            if (owned) positive++;
        }
        var matrix = root.GetProperty("RuleMatrix").EnumerateArray().ToArray();
        Require(matrix.Length == 30 && positive == 2, "Incomplete native rule matrix or positive reacquisition evidence.");
        foreach (string phase in phases)
            Require(matrix.Where(row => row.GetProperty("Phase").GetString() == phase).Select(row => row.GetProperty("Start").GetInt32())
                .SequenceEqual([-2, 0, 1, 2, 5, int.MaxValue]), "Missing native lower, ordinary or overflow controls.");
        foreach (var row in matrix)
        {
            int[] additions = row.GetProperty("Additions").Deserialize<int[]>()!; int start = row.GetProperty("Start").GetInt32();
            bool owned = row.GetProperty("Phase").GetString() is "owned" or "reacquired";
            Require(row.GetProperty("Owned").GetBoolean() == owned && additions.SequenceEqual(owned ? [1] : Array.Empty<int>()),
                "Rule matrix did not retain its original-owner registration.");
            int actual = row.GetProperty("Actual").GetInt32();
            Require(actual == CampaignRelicModel.RandomDraftEnhancedCardCount(additions, start), "Native additive/clamp/overflow differs.");
            string parent = JsonSerializer.Serialize(additions);
            Parallel.For(0, 16, _ => Require(CampaignRelicModel.RandomDraftEnhancedCardCount(additions, start) == actual, "Parallel draft-count result differs."));
            Require(JsonSerializer.Serialize(additions) == parent, "Draft-count model mutated its captured contributions.");
        }
        Console.WriteLine("CAMPAIGN-DRAFT-ENHANCER-COUNT-NATIVE PASS: unchanged DrawOnCorruption +1 balance effect, five actual SaveManager consumer calls (1/2/1/2/1), 30 native clamp/overflow queries, reacquisition/reset, state/frame/RNG restoration and 16 immutable branches. Draft UI/enhancer application/grant are outside this interface witness.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
