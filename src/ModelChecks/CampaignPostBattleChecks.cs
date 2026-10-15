using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class CampaignPostBattleChecks
{
    private static readonly (string Id, string Key, int Int, int Int2, CampaignFixedReward Reward)[] Owners = [
        ("5cad6ad0-769b-46d3-9464-514b8e74190a", "VoidPortal", 8, 2,
            new("5d700f1d-f163-4d15-bc79-69be1a23f3d6", "PurgeReward", "PurgeRewardData")),
        ("202a7153-bfe9-4da9-be3e-ddcf8c195354", "MothersRemnant2Relic", 0, 0,
            new("7ad92d5c-baec-4090-a94b-dd6a5ed1ea15", "CardDuplicatorReward", "EnhancerRewardData")) ];
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path); var root = document.RootElement;
        Require(root.GetProperty("Schema").GetInt32() == 1 && root.GetProperty("GameVersion").GetString() == "2.2.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("Before").ContentEquals(root.GetProperty("After")), "Post-battle native identity or recorded state/frame/RNG restoration differs.");
        var definitions = root.GetProperty("Owners").EnumerateArray().ToArray();
        Require(definitions.Length == 2, "Expected two unchanged post-battle owners.");
        for (int index = 0; index < definitions.Length; index++)
        {
            var row = definitions[index]; var expected = Owners[index]; var effects = row.GetProperty("Effects");
            Require(row.GetProperty("Id").GetString() == expected.Id && row.GetProperty("Key").GetString() == expected.Key &&
                !row.GetProperty("DisallowedInPlacementPhase").GetBoolean() && effects.GetArrayLength() == 1 &&
                effects[0].GetProperty("Type").GetString() == "RelicEffectAddPostBattleReward" &&
                effects[0].GetProperty("Int").GetInt32() == expected.Int && effects[0].GetProperty("Int2").GetInt32() == expected.Int2 &&
                effects[0].GetProperty("Conditions").GetInt32() == 0, "Original post-battle owner definition changed.");
            Equal(effects[0].GetProperty("Reward").Deserialize<CampaignFixedReward>(), expected.Reward, "original direct reward asset");
        }
        var rows = root.GetProperty("Rows").EnumerateArray().ToArray();
        Require(rows.Length == 108 && rows.Select(row => row.GetProperty("Scenario").GetString()).Distinct().Count() == 3 &&
            rows.Select(row => (row.GetProperty("Phase").GetString(), row.GetProperty("Scenario").GetString(),
                row.GetProperty("PrefixCount").GetInt32(), row.GetProperty("Repetition").GetInt32())).Distinct().Count() == rows.Length,
            "Incomplete or duplicated actual post-battle consumer matrix.");
        int appended = 0, reversed = 0, duplicates = 0;
        foreach (var row in rows)
        {
            string phase = row.GetProperty("Phase").GetString()!;
            string[] ids = phase switch { "baseline" or "removed" => [], "void" => [Owners[0].Id], "mother" => [Owners[1].Id],
                "both" => [Owners[0].Id, Owners[1].Id], "reversed" => [Owners[1].Id, Owners[0].Id], _ => throw new InvalidOperationException("Unknown native phase.") };
            Require(row.GetProperty("Owned").Deserialize<string[]>()!.SequenceEqual(ids) && row.GetProperty("OriginalReferences").GetBoolean() &&
                row.GetProperty("RngBefore").ContentEquals(row.GetProperty("RngAfter")), "Native owner order, reward references or RNG differs.");
            int count = row.GetProperty("PrefixCount").GetInt32(); Require(count is >= 0 and <= 2, "Invalid native prefix control.");
            var input = Enumerable.Repeat(Owners[0].Reward, count).ToArray();
            var owners = ids.Select(id => new CampaignPostBattleOwner(id, Owners.Single(owner => owner.Id == id).Reward)).ToArray();
            var result = CampaignPostBattleRewardModel.Collect(input, owners);
            Equal(result, row.GetProperty("Actual").Deserialize<CampaignFixedReward[]>(), "actual native reward collection");
            Require(input.Length == count && result.Count == count + ids.Length &&
                result.Take(count).All(reward => ReferenceEquals(reward, Owners[0].Reward)), "Existing rewards were consumed, deduplicated or copied.");
            appended += ids.Length; if (phase == "reversed") reversed++; if (count == 2) duplicates++;
        }
        var baseline = new[] { Owners[0].Reward, Owners[0].Reward };
        var both = Owners.Select(owner => new CampaignPostBattleOwner(owner.Id, owner.Reward)).ToArray();
        string parent = JsonSerializer.Serialize(baseline, ModelJson.Options);
        Parallel.For(0, 16, _ => Require(CampaignPostBattleRewardModel.Collect(baseline, both).Count == 4 &&
            JsonSerializer.Serialize(baseline, ModelJson.Options) == parent, "Parallel post-battle reward branch mutated its input."));
        Require(appended == 108 && reversed == 18 && duplicates == 36, "Missing positive native reward/order/duplicate controls.");
        Console.WriteLine($"CAMPAIGN-POST-BATTLE-NATIVE PASS: two unchanged owners, {rows.Length} actual manager collections, {appended} appended original reward references, {reversed} reversed-order and {duplicates} duplicate-prefix controls; unchanged recorded state/frame/RNG and 16 parallel branches. Reward acceptance/grant is outside this collection claim.");
    }
    private static void Equal<T>(T a, T b, string label) => Require(JsonSerializer.Serialize(a, ModelJson.Options) == JsonSerializer.Serialize(b, ModelJson.Options), "Post-battle " + label + " differs.");
    private static void Require(bool condition, string error) { if (!condition) throw new InvalidOperationException(error); }
}
