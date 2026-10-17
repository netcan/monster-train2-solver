using System.Linq;
using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class CampaignDraftChecks
{
    private static readonly (string Id, string Key, string Type, int? Int, int? Int2)[] Owners = [
        ("656861c9-a96d-4c99-988c-390b75e5d8d4", "AlliedClassRelic", "RelicEffectOverrideCardDraftClassType", 1, 2),
        ("8f46fde9-0c75-49ba-9f58-3063864f81bf", "FutureDraftsAreRares", "RelicEffectOverrideCardDraftRarity", 2, 0),
        ("fe768ab5-3f58-4f96-9afc-48f0f30c7b21", "PyreHeartFather", "RelicEffectModifyTowerMaxHealth", 25, 0),
        ("9e0e5d4e-6d16-43f1-8cd4-cc4c2b431afd", "UpgradedDrafts", "RelicEffectAddStartingUpgradeToCardDrafts", null, null)
    ];
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path); var root = document.RootElement;
        Require(root.GetProperty("Schema").GetInt32() == 1 && root.GetProperty("GameVersion").GetString() == "2.2.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("Before").ContentEquals(root.GetProperty("After")), "Campaign draft native identity or state/frame/RNG restoration differs.");
        var owners = root.GetProperty("Owners").EnumerateArray().ToArray();
        Require(owners.Length == Owners.Length, "Expected four original campaign owners.");
        for (int index = 0; index < owners.Length; index++)
        {
            var row = owners[index]; var expected = Owners[index]; var effects = row.GetProperty("Effects");
            Require(row.GetProperty("DataId").GetString() == expected.Id && row.GetProperty("AssetKey").GetString() == expected.Key &&
                !row.GetProperty("DisallowedInPlacementPhase").GetBoolean() && effects.GetArrayLength() == 1 &&
                effects[0].GetProperty("Type").GetString() == expected.Type &&
                (!expected.Int.HasValue || effects[0].GetProperty("Int").GetInt32() == expected.Int.Value) &&
                (!expected.Int2.HasValue || effects[0].GetProperty("Int2").GetInt32() == expected.Int2.Value) && effects[0].GetProperty("Conditions").GetInt32() == 0,
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
        StartingUpgrades(root.GetProperty("StartingUpgrades"));
        Parallel.For(0, 16, _ => {
            var result = CampaignDraftModel.Select(pool, 3, 0, 3, UnityRng.Seed(41), true, true);
            Require(result.Cards.SequenceEqual(CampaignDraftModel.Select(pool, 3, 0, 3, UnityRng.Seed(41), true, true).Cards), "Parallel campaign draft branch differs.");
        });
        Console.WriteLine($"CAMPAIGN-DRAFTS-NATIVE PASS: four unchanged original owners, {rows.Length} actual GrantReward selections and exact Rewards RNG; positive allied={alliedChanges}, rare={rareChanges}, explicit-class controls={explicitControls}, ignore-rarity controls={ignoreControls}; three Pyre transitions; UpgradedDrafts starting-upgrade selection/filter/RNG/card-state checks; restored recorded state/frame/RNG.");
    }

    private static void StartingUpgrades(FixtureValue sample)
    {
        string type = sample.GetProperty("CardType").GetString()!;
        var cardMask = sample.GetProperty("CardMask").Deserialize<CardUpgradeMaskCard>()!;
        var pool = sample.GetProperty("Pool").Deserialize<CampaignStartingUpgradeEnhancer[]>()!;
        var rows = sample.GetProperty("Rows").EnumerateArray().ToArray();
        bool singleRandom = sample.GetProperty("ApplyToSingleRandomCard").GetBoolean();
        bool canPreview = sample.GetProperty("CanApplyInPreview").GetBoolean();
        Require(cardMask.CardType == type && pool.Length > 0,
            "UpgradedDrafts target or ordered enhancer pool is incomplete.");

        int filters = 0, rejectedFilters = 0;
        foreach (var enhancerRow in sample.GetProperty("Pool").EnumerateArray())
        foreach (var effectRow in enhancerRow.GetProperty("Effects").EnumerateArray())
        foreach (var filterRow in effectRow.GetProperty("Filters").EnumerateArray())
        {
            var rule = filterRow.GetProperty("Rule").Deserialize<CardUpgradeMaskRule>()!;
            bool actual = filterRow.GetProperty("NativeAccepted").GetBoolean();
            bool predicted = CardUpgradeMaskModel.FilterCard(rule, cardMask);
            Require(predicted == actual, $"UpgradedDrafts native filter differs for {enhancerRow.GetProperty("AssetKey").GetString()}.");
            filters++; if (!actual) rejectedFilters++;
        }

        var selected = new HashSet<int>();
        int activeSamples = 0, noOwnerControls = 0, randomGateControls = 0;
        foreach (FixtureValue row in rows)
        {
            var before = row.GetProperty("Before").Deserialize<CardInstanceState>()!;
            var after = row.GetProperty("After").Deserialize<CardInstanceState>()!;
            uint[] words = row.GetProperty("BeforeRng").Deserialize<uint[]>()!;
            var rng = new UnityRng(words[0], words[1], words[2], words[3]);
            bool ownerActive = row.GetProperty("OwnerActive").GetBoolean();
            bool randomSelection = row.GetProperty("CardIsRandomSelection").GetBoolean();
            var result = CampaignDraftStartingUpgradeModel.Apply(pool, before, cardMask, rng, ownerActive,
                previewMode: false, canApplyInPreview: canPreview, applyToSingleRandomCard: singleRandom,
                cardIsRandomSelection: randomSelection);
            Require(result.Supported, "Native UpgradedDrafts sample selected an upgrade outside the model boundary: " + result.UnsupportedReason);
            string[] nativeEligible = ownerActive ? pool.Where(enhancer => enhancer.DlcAllowed && enhancer.Effects.All(effect =>
                effect.CardType == type && effect.Filters.All(filter => filter.NativeAccepted))).Select(enhancer => enhancer.DataId).ToArray() : Array.Empty<string>();
            Require(result.EligibleEnhancerIds.SequenceEqual(nativeEligible),
                "UpgradedDrafts model candidate order/eligibility differs from native DLC, type and filter decisions.");
            string? difference = ModelJson.Difference(JsonSerializer.Serialize(result.State, ModelJson.Options),
                JsonSerializer.Serialize(after, ModelJson.Options));
            uint[] actualRng = row.GetProperty("AfterRng").Deserialize<uint[]>()!;
            Require(difference == null && result.Rng.Equals(new UnityRng(actualRng[0], actualRng[1], actualRng[2], actualRng[3])),
                "UpgradedDrafts resulting card state or Rewards RNG differs: " + difference);
            string[] additions = row.GetProperty("AddedUpgradeIds").Deserialize<string[]>()!;
            Require(additions.SequenceEqual(result.Applied ? new[] { result.UpgradeId! } : Array.Empty<string>()),
                "UpgradedDrafts selected first-effect upgrade differs from the native card state.");
            if (ownerActive && randomSelection && result.Applied)
            { activeSamples++; selected.Add(result.PoolIndex!.Value); }
            if (!ownerActive)
            {
                Require(!result.Applied && rng.Equals(result.Rng) && additions.Length == 0,
                    "Missing-owner call changed the draft card or Rewards RNG."); noOwnerControls++;
            }
            if (ownerActive && !randomSelection && singleRandom)
            {
                Require(!result.Applied && rng.Equals(result.Rng) && additions.Length == 0,
                    "Single-random-card gate changed a non-random draft candidate."); randomGateControls++;
            }
        }

        Require(filters > 0 && rejectedFilters > 0 && activeSamples > 0 && noOwnerControls == 1 &&
            (singleRandom ? randomGateControls == 1 : rows.Any(row => row.GetProperty("OwnerActive").GetBoolean() && !row.GetProperty("CardIsRandomSelection").GetBoolean())),
            "UpgradedDrafts is missing positive, filtering, owner-absence or random-card gate evidence.");

        CardInstanceState parent = rows.First(row => row.GetProperty("OwnerActive").GetBoolean())
            .GetProperty("Before").Deserialize<CardInstanceState>()!;
        string parentJson = JsonSerializer.Serialize(parent, ModelJson.Options);
        var blocked = CampaignDraftStartingUpgradeModel.Apply(pool, parent, cardMask, UnityRng.Seed(123), ownerActive: false);
        var preview = CampaignDraftStartingUpgradeModel.Apply(pool, parent, cardMask, UnityRng.Seed(123), previewMode: true,
            canApplyInPreview: false, applyToSingleRandomCard: singleRandom);
        var randomGate = CampaignDraftStartingUpgradeModel.Apply(pool, parent, cardMask, UnityRng.Seed(123),
            applyToSingleRandomCard: true, cardIsRandomSelection: false);
        var emptyPool = pool.Select(item => new CampaignStartingUpgradeEnhancer(item.DataId, item.AssetKey, false, item.Effects)).ToArray();
        var noChoices = CampaignDraftStartingUpgradeModel.Apply(emptyPool, parent, cardMask, UnityRng.Seed(123));
        Require(blocked.Supported && !blocked.Applied && blocked.Rng.Equals(UnityRng.Seed(123)) &&
            preview.Supported && !preview.Applied && preview.Rng.Equals(UnityRng.Seed(123)) &&
            randomGate.Supported && !randomGate.Applied && randomGate.Rng.Equals(UnityRng.Seed(123)) &&
            noChoices.Supported && !noChoices.Applied && noChoices.Rng.Equals(UnityRng.Seed(123)) && noChoices.EligibleEnhancerIds.Count == 0 &&
            JsonSerializer.Serialize(parent, ModelJson.Options) == parentJson,
            "UpgradedDrafts owner/preview/random/no-match gates consumed RNG or mutated their immutable parent.");
        Parallel.For(0, 16, _ =>
        {
            var result = CampaignDraftStartingUpgradeModel.Apply(pool, parent, cardMask, UnityRng.Seed(41),
                applyToSingleRandomCard: singleRandom);
            Require(JsonSerializer.Serialize(result.State, ModelJson.Options) ==
                JsonSerializer.Serialize(CampaignDraftStartingUpgradeModel.Apply(pool, parent, cardMask, UnityRng.Seed(41),
                    applyToSingleRandomCard: singleRandom).State, ModelJson.Options),
                "Parallel UpgradedDrafts branch differs.");
        });
        Console.WriteLine($"UPGRADED-DRAFTS-NATIVE PASS: card={type}/{sample.GetProperty("CardDataId").GetString()}, pool={pool.Length}, filters={filters} (rejected={rejectedFilters}), applied={activeSamples}, distinct selected enhancers={selected.Count}, owner-absence={noOwnerControls}, random gate={randomGateControls}; preview/RNG/parent controls passed.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
