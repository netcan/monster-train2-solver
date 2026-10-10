using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class RelicCardUpgradeChecks
{
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path); var root = document.RootElement;
        Require(root.GetProperty("Schema").GetInt32() == 1 && root.GetProperty("ContextUnchanged").GetBoolean() &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("FrameBefore").ContentEquals(root.GetProperty("FrameAfter")) && root.GetProperty("RngBefore").ContentEquals(root.GetProperty("RngAfter")),
            "Relic card upgrade calibration changed context/RNG/frame or game identity.");
        var originals = root.GetProperty("OriginalRelicAssets").Deserialize<string[]>()!;
        int owned = root.GetProperty("OwnedCardCount").GetInt32();
        Require(originals.Length == 11 && originals.Distinct().Count() == 11 && owned == 15, "Original temporary relic/owned card inventory differs.");
        var rows = root.GetProperty("Rows").EnumerateArray().Select(row => (
            Group: row.GetProperty("Group").GetString()!, Operation: row.GetProperty("Operation").GetString()!,
            Rule: row.GetProperty("Rule").Deserialize<RelicCardUpgradeRule>(), Before: row.GetProperty("Before").Deserialize<CardUpgradeLifecycleState>()!,
            Next: row.GetProperty("NextInstanceId").GetInt32(), Returned: row.GetProperty("Returned").Deserialize<bool?>(),
            Added: row.GetProperty("UpgradeAdded").GetBoolean(), Filters: row.GetProperty("Filters").Deserialize<RelicCardFilterResult[]>()!,
            Actual: row.GetProperty("Actual").Deserialize<CardUpgradeLifecycleState>()!, NextAfter: row.GetProperty("NextAfter").GetInt32(),
            Card: row.GetProperty("Card").Deserialize<CardUpgradeMaskCard>()!, AfterQuery: row.GetProperty("AfterQuery").Deserialize<CardUpgradeLifecycleState>()!)).ToArray();
        Require(rows.Length == 432 && rows.Select(row => row.Group).Distinct().Count() == 183 &&
            rows.Count(row => row.Group.StartsWith("eligible/")) == 44 && rows.Count(row => row.Group.StartsWith("owned/")) == 330 &&
            rows.Count(row => row.Group.StartsWith("ordered/")) == 48 && rows.Count(row => row.Group.StartsWith("controlled/")) == 10,
            "Native eligibility/owned/ordered/controlled coverage differs.");
        foreach (string original in originals)
        {
            var sequence = rows.Where(row => row.Group == "eligible/" + original).ToArray();
            Require(sequence.Length == 4 && sequence[0].Returned == true && sequence[0].Added && sequence[1].Returned == true &&
                sequence[3].Returned == true && sequence[3].Added, "Original relic lacks actual eligible apply/repeat/reset/reapply: " + original);
        }
        Require(rows.Any(row => row.Returned == true && !row.Added) && rows.Any(row => row.Returned == false && row.Filters.Length > 0) &&
            rows.Any(row => row.Filters.Length == 2) && rows.Any(row => row.Filters.Length > 0 && !row.Filters[^1].Accepted),
            "Native effect lacks unique rejection or sequential filter coverage.");
        string parent = JsonSerializer.Serialize(rows);
        void Check()
        {
            foreach (var group in rows.GroupBy(row => row.Group))
            {
                var state = group.First().Before;
                int next = state.Permanent.Concat(state.Temporary).Select(upgrade => upgrade.InstanceId).DefaultIfEmpty().Max() + 1;
                foreach (var row in group)
                {
                    Equal(state, row.Before, row.Group + "/" + row.Operation + " carried input");
                    Require(next == row.Next, "Relic upgrade allocation input is not carried.");
                    RelicCardUpgradeStep step;
                    if (row.Operation == "Reset") step = new(CardUpgradeLifecycleModel.Reset(state).State, false, false, next);
                    else if (row.Operation == "Apply") step = RelicCardUpgradeModel.Apply(state, row.Rule!, next);
                    else throw new InvalidOperationException(row.Operation);
                    Require((row.Operation == "Reset" ? row.Returned == null : step.Returned == row.Returned) &&
                        step.UpgradeAdded == row.Added && step.NextUpgradeInstanceId == row.NextAfter, "Native relic returned/add/allocation state differs: " + row.Group);
                    Equal(step.Filters, row.Filters, row.Group + " ordered filter dispatch");
                    Equal(step.State, row.Actual, row.Group + " raw post-effect state");
                    var view = CardOwnedMaskModel.Resolve(step.State.Card);
                    state = new(view.State, step.State.Permanent, step.State.Temporary, step.State.AuthoredTriggers, step.State.UpgradeTriggers,
                        step.State.StandbyOverride, step.State.StandbyPile);
                    Equal(view.Card, row.Card, row.Group + " queried card view");
                    Equal(state, row.AfterQuery, row.Group + " post-query state");
                    next = step.NextUpgradeInstanceId;
                }
            }
        }
        Check(); Parallel.For(0, 32, _ => Check());
        Require(JsonSerializer.Serialize(rows) == parent, "Relic card upgrade replay mutated parent inputs.");
        Console.WriteLine("NATIVE-RELIC-CARD-UPGRADE-CHECKS PASS: 432 carried original-effect operations, all11 eligible/repeated/reset relics,15 owned cards, forward/reverse ordered chains, source/null/filter gates, exact cache/identity/query states and 32 immutable branches.");
    }
    private static void Equal<T>(T expected, T actual, string label)
    {
        string? difference = ModelJson.Difference(JsonSerializer.Serialize(expected), JsonSerializer.Serialize(actual));
        Require(difference == null, label + ": " + difference);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
