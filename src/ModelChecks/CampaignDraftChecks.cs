using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class CampaignDraftChecks
{
    private static readonly (string Id, string Key, string Type, int Int, int Int2)[] Owners = [
        ("656861c9-a96d-4c99-988c-390b75e5d8d4", "AlliedClassRelic", "RelicEffectOverrideCardDraftClassType", 1, 2),
        ("8f46fde9-0c75-49ba-9f58-3063864f81bf", "FutureDraftsAreRares", "RelicEffectOverrideCardDraftRarity", 2, 0),
        ("fe768ab5-3f58-4f96-9afc-48f0f30c7b21", "PyreHeartFather", "RelicEffectModifyTowerMaxHealth", 25, 0)
    ];
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path); var root = document.RootElement;
        Require(root.GetProperty("Schema").GetInt32() == 1 && root.GetProperty("GameVersion").GetString() == "2.2.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("Before").ContentEquals(root.GetProperty("After")), "Campaign draft native identity or state/frame/RNG restoration differs.");
        var owners = root.GetProperty("Owners").EnumerateArray().ToArray();
        Require(owners.Length == 3, "Expected three original campaign owners.");
        for (int index = 0; index < owners.Length; index++)
        {
            var row = owners[index]; var expected = Owners[index]; var effects = row.GetProperty("Effects");
            Require(row.GetProperty("DataId").GetString() == expected.Id && row.GetProperty("AssetKey").GetString() == expected.Key &&
                !row.GetProperty("DisallowedInPlacementPhase").GetBoolean() && effects.GetArrayLength() == 1 &&
                effects[0].GetProperty("Type").GetString() == expected.Type && effects[0].GetProperty("Int").GetInt32() == expected.Int &&
                effects[0].GetProperty("Int2").GetInt32() == expected.Int2 && effects[0].GetProperty("Conditions").GetInt32() == 0,
                "Original campaign draft owner changed.");
            if (index == 1) Require(effects[0].GetProperty("Rarity").GetInt32() == 2, "Original rare override changed.");
        }
        var pool = root.GetProperty("Pool").Deserialize<CampaignDraftCard[]>()!;
        Require(pool.Select(card => card.DataId).Distinct().Count() == pool.Length &&
            new[] { 1, 2 }.All(group => new[] { 0, 1, 2 }.All(rarity => pool.Count(card => card.ClassType == group && card.Rarity == rarity) >= 2)),
            "Missing distinct unlocked class/rarity controls.");
        var rows = root.GetProperty("Drafts").EnumerateArray().ToArray();
        Require(rows.Length == 840 && rows.Select(row => row.GetProperty("Phase").GetString()).Distinct().Count() == 5,
            "Incomplete actual native GrantReward draft matrix.");
        int alliedChanges = 0, rareChanges = 0, explicitControls = 0, ignoreControls = 0;
        foreach (var row in rows)
        {
            string phase = row.GetProperty("Phase").GetString()!;
            bool allied = phase is "allied" or "both", rare = phase is "rare" or "both";
            Require(phase is "baseline" or "allied" or "both" or "rare" or "removed", "Unknown campaign draft phase.");
            int classes = row.GetProperty("Classes").GetInt32(), floor = row.GetProperty("Floor").GetInt32();
            bool ignore = row.GetProperty("IgnoreRarityOverride").GetBoolean(), explicitClass = row.GetProperty("ExplicitClass").GetBoolean();
            uint[] words = row.GetProperty("BeforeRng").Deserialize<uint[]>()!;
            UnityRng rng = new(words[0], words[1], words[2], words[3]);
            var result = CampaignDraftModel.Select(pool, classes, floor, 3, rng, allied, rare, ignore, explicitClass ? 1 : null);
            uint[] end = row.GetProperty("AfterRng").Deserialize<uint[]>()!;
            var actual = row.GetProperty("Actual").Deserialize<string[]>()!;
            Require(result.Cards.SequenceEqual(actual) && result.Rng.Equals(new(end[0], end[1], end[2], end[3])),
                $"Native flattened draft selection/RNG differs: phase={phase},class={classes},floor={floor},ignore={ignore},explicit={explicitClass}.");
            var baseline = CampaignDraftModel.Select(pool, classes, floor, 3, rng, false, false, ignore, explicitClass ? 1 : null);
            if (allied && !explicitClass && !rare && !actual.SequenceEqual(baseline.Cards)) alliedChanges++;
            if (rare && !ignore && !allied && !actual.SequenceEqual(baseline.Cards)) rareChanges++;
            if (allied && explicitClass)
            {
                var without = CampaignDraftModel.Select(pool, classes, floor, 3, rng, false, rare, ignore, 1);
                Require(actual.SequenceEqual(without.Cards) && result.Rng.Equals(without.Rng), "Explicit class override failed to bypass AlliedClassRelic.");
                explicitControls++;
            }
            if (rare && ignore)
            {
                var without = CampaignDraftModel.Select(pool, classes, floor, 3, rng, allied, false, true, explicitClass ? 1 : null);
                Require(actual.SequenceEqual(without.Cards) && result.Rng.Equals(without.Rng), "Ignore rarity gate failed to bypass rare owner.");
                ignoreControls++;
            }
        }
        var health = root.GetProperty("Health").EnumerateArray().ToArray();
        Require(health.Length == 3 && alliedChanges > 0 && rareChanges > 0 && explicitControls > 0 && ignoreControls > 0,
            "Missing campaign positive and bypass controls.");
        foreach (var row in health)
        {
            int raw = row.GetProperty("BeforeRaw").GetInt32(), maximum = row.GetProperty("BeforeMaximum").GetInt32();
            var acquired = CampaignDraftModel.AcquireTowerHealth(raw, maximum, 25);
            Require(row.GetProperty("AcquiredRaw").GetInt32() == acquired.RawHealth && row.GetProperty("AcquiredMaximum").GetInt32() == acquired.Maximum &&
                row.GetProperty("RemovedRaw").GetInt32() == acquired.RawHealth && row.GetProperty("RemovedMaximum").GetInt32() == maximum &&
                row.GetProperty("RemovedHealth").GetInt32() == Math.Min(acquired.RawHealth, maximum),
                "Original Pyre max-health acquisition or no-op removal differs.");
        }
        Parallel.For(0, 16, _ => {
            var result = CampaignDraftModel.Select(pool, 3, 0, 3, UnityRng.Seed(41), true, true);
            Require(result.Cards.SequenceEqual(CampaignDraftModel.Select(pool, 3, 0, 3, UnityRng.Seed(41), true, true).Cards), "Parallel campaign draft branch differs.");
        });
        Console.WriteLine($"CAMPAIGN-DRAFTS-NATIVE PASS: three unchanged original owners, {rows.Length} actual GrantReward selections and exact Rewards RNG; positive allied={alliedChanges}, rare={rareChanges}, explicit-class controls={explicitControls}, ignore-rarity controls={ignoreControls}; three Pyre acquisition/removal transitions and restored recorded state/frame/RNG.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
