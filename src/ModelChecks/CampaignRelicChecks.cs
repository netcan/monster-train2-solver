using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class CampaignRelicChecks
{
    private static readonly (string Id, string Key, string Type, int Int, float Float)[] Owners = [
        ("60a7f580-0788-42f2-8896-cb8d22e849a5", "ExtraSpellUpgrades", "RelicEffectAddSpellUpgradeSlot", 0, 0),
        ("3203c2d3-58f3-4f38-888e-886d8faae758", "ExtraUnitUpgrades", "RelicEffectAddUnitUpgradeSlot", 0, 0),
        ("ffcb6931-e45e-4e27-bacf-4c649779c2be", "CheaperMerchants", "RelicEffectModifyMerchantAllCosts", 25, -.25f),
        ("07bdefdb-03b8-4159-b74b-be85f68c4c56", "ApplyUnitUpgradeWhenRich", "RelicEffectModifyTrialGoldRewards", 50, 0),
        ("00a8f443-cdea-437a-98bf-0ebe0ead00b0", "ModifyDragonsHoardCap", "RelicEffectModifyDragonsHoardCap", 3, 0),
        ("c3f91e0e-629f-4d3f-b422-ce1653147293", "ModifyDragonsHoardCap_DH", "RelicEffectModifyDragonsHoardCap", 2, 0)];
    internal static void Run()
    {
        Require(CampaignRelicModel.UpgradeSlots("Spell", 2, 3, 0, ["Spell", "Monster"]) == 3 &&
            CampaignRelicModel.UpgradeSlots("Spell", 2, 3, 20, ["Spell"]) == 3 &&
            CampaignRelicModel.UpgradeSlots("Monster", 2, 3, -5, ["Monster"]) == 0 &&
            CampaignRelicModel.UpgradeSlots("Blight", 2, 3, 20, ["Spell"]) == 0,
            "Campaign upgrade-slot type/max/lower boundaries differ.");
        Require(CampaignRelicModel.MerchantCost([101], 0, [-.25f]) == 75 &&
            CampaignRelicModel.MerchantCost([101], 5, [-.25f], .5f) == 40 &&
            CampaignRelicModel.MerchantCost([101], 5, [-.25f], .5f, discountAllowed: false) == 75 &&
            CampaignRelicModel.MerchantCost([], 0, [-.25f]) == 0 &&
            CampaignRelicModel.MerchantCost([1], 0, [-.25f]) == 0,
            "Campaign merchant ceiling/round-five, exhausted schedule, discount exclusion or zero-price semantics differ.");
        Require(GoldRewardModel.Adjust(105, applyTrialDataModifiers: true, trialGoldPercents: [50]) == 155 &&
            GoldRewardModel.Adjust(105, applyTrialDataModifiers: false, trialGoldPercents: [50]) == 105 &&
            GoldRewardModel.Adjust(-5, applyTrialDataModifiers: true, trialGoldPercents: [50]) == -5 &&
            GoldRewardModel.Adjust(1, applyTrialDataModifiers: true, trialGoldPercents: [50]) == 5,
            "Trial percentage ordering, truncation, optional gate or reward minimum differs.");
        Require(CampaignRelicModel.AcquireHoardCap(99, 3, 100) == 100 &&
            CampaignRelicModel.RemoveHoardCap(100, 100, 3, 100) == (97, 97),
            "Hoard acquisition clamp/removal truncation differs.");
        string[] silverEffects = ["RelicEffectBuffPyreForBossKill", "RelicEffectAddPostBattleRewardOnBossDeath"];
        foreach (var (id, key) in new[] {
            ("50198df9-788c-4618-942a-57361c585a57", "SilverT1"),
            ("354e6614-8b05-4933-bd24-97a41fc63061", "SilverT2"),
            ("44e74028-4548-4f36-911d-cdf4dd821511", "SilverT3") })
        {
            CombatRelicState Owner(string asset, IReadOnlyList<string> effects) =>
                new(id, asset, effects, isCovenant: false, disallowedInPlacementPhase: false);
            Require(RelicModel.Validate(new[] { Owner(key, silverEffects) }) == null &&
                RelicModel.Validate(new[] { Owner("OtherSilverOwner", silverEffects) }) != null &&
                RelicModel.Validate(new[] { Owner(key, [.. silverEffects, "UncalibratedCombatCallback"]) }) != null,
                "Only original Silver souls with their exact post-battle effects are compatible with battle search.");
        }
        foreach (var (id, key, effect) in new[] {
            (RelicModel.ChampionReplacementId, "ChampionReplacement", "RelicEffectReplaceChampion"),
            (RelicModel.ForceTrialId, "TimeForTrials", "RelicEffectModifyTrial") })
        {
            CombatRelicState Owner(IReadOnlyList<string> effects) =>
                new(id, key, effects, isCovenant: false, disallowedInPlacementPhase: false);
            Require(RelicModel.Validate(new[] { Owner([effect]) }) == null &&
                RelicModel.Validate(new[] { Owner([effect, "UncalibratedCombatCallback"]) }) != null,
                "An original setup mutator must retain its exact effect list.");
        }
        foreach (var owner in Owners.Skip(4))
        {
            CombatRelicState[] relics = [new CombatRelicState(owner.Id, owner.Key, [owner.Type],
                isCovenant: false, disallowedInPlacementPhase: false)];
            Require(RelicModel.Validate(relics) == null &&
                RelicModel.Validate(new[] { new CombatRelicState(owner.Id, "UnknownHoardOwner", [owner.Type],
                    isCovenant: false, disallowedInPlacementPhase: false) }) != null &&
                RelicModel.Validate(new[] { new CombatRelicState(owner.Id, owner.Key, [owner.Type, owner.Type],
                    isCovenant: false, disallowedInPlacementPhase: false) }) != null,
                "Battle compatibility must identify each original Hoard acquisition owner.");
            var context = new CombatContext(new CardCycleState([], [], [], default, 0, []), default, 0, 0, 10, relics: relics);
            Require(RelicModel.Validate(context) == "Missing or inconsistent settled native Dragon's Hoard state.",
                "Battle decisions with a Hoard acquisition owner require a settled native amount and cap.");
        }
        Console.WriteLine("CAMPAIGN-RELIC-CHECKS PASS: upgrade slot bounds/types, all-cost merchant rounding/discount exclusions, ordered trial gold and capped hoard acquisition/removal.");
    }
    internal static void GoldMultiplierNative(string path)
    {
        using var document = ModelJson.ReadFixture(path);
        FixtureValue root = document.RootElement;
        Require(root.GetProperty("Schema").GetInt32() == 1 &&
            root.TryGetProperty("Boundary", out FixtureValue boundary) && boundary.GetString() == "OriginalGoldRewardMultiplier" &&
            root.GetProperty("GameVersion").GetString() == "2.3.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d",
            "Gold multiplier evidence schema, boundary or module differs.");

        FixtureValue owner = root.GetProperty("Owner"), reward = root.GetProperty("Reward");
        float multiplier = root.GetProperty("Multiplier").GetSingle();
        string? variant = root.GetProperty("Variant").GetString();
        CombatRelicState roster = new(owner.GetProperty("Id").GetString()!, owner.GetProperty("Key").GetString()!,
            owner.GetProperty("Effects").Deserialize<string[]>()!, isCovenant: false,
            disallowedInPlacementPhase: owner.GetProperty("DisallowedInPlacementPhase").GetBoolean());
        Require(GoldRewardModel.IsOriginalMultiplier(roster) && owner.GetProperty("Conditions").GetInt32() == 0 &&
            multiplier == GoldRewardModel.OriginalMultiplier(roster.DataId, roster.AssetKey) &&
            ((variant == "Double" && multiplier == 2f) || (variant == "Half" && multiplier == 0.5f)) &&
            RelicModel.Validate([roster]) == null, "Original gold multiplier owner or rule differs.");

        int amount = reward.GetProperty("Amount").GetInt32(), cost = Int("Cost");
        int expected = GoldRewardModel.Adjust(amount, multiplier: multiplier);
        Require(!string.IsNullOrEmpty(reward.GetProperty("Id").GetString()) &&
            !string.IsNullOrEmpty(reward.GetProperty("Key").GetString()) &&
            amount >= 10 && !reward.GetProperty("ApplyTrialDataModifiers").GetBoolean() &&
            Int("Baseline") == GoldRewardModel.Adjust(amount) && expected != Int("Baseline") &&
            Int("Adjusted") == expected && Int("GoldGranted") - Int("GoldBefore") == expected &&
            Int("Grants") == 1 && Int("Completions") == 1, "Native stock gold grant differs from the reward calculation.");
        Require(cost == 3 && Int("RawIncome") == amount && Int("RawCost") == -cost &&
            Int("Debit") == -cost && Int("GoldAfter") == Int("GoldGranted") - cost,
            "Native non-reward income or actual cost was scaled.");
        FixtureValue before = root.GetProperty("RngBefore");
        Require(before.GetProperty("Battle").GetArrayLength() == 4 && before.GetProperty("Rewards").GetArrayLength() == 4 &&
            before.ContentEquals(root.GetProperty("RngAfter")), "Native gold grant or debit changed deterministic RNG.");
        Console.WriteLine($"ORIGINAL-GOLD-REWARD-MULTIPLIER CHECK PASS: {variant}, grant={expected}, cost={cost}.");

        int Int(string property) => root.GetProperty(property).GetInt32();
    }

    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path);
        FixtureValue root = document.RootElement;
        Require(root.GetProperty("Schema").GetInt32() == 1 && SupportsCalibrationBuild(root) &&
            root.GetProperty("Before").ContentEquals(root.GetProperty("After")),
            "Campaign calibration build or recorded state restoration differs.");
        var owners = root.GetProperty("Owners").EnumerateArray().ToArray();
        Require(owners.Length == 6, "Expected six unchanged original campaign owners.");
        for (int index = 0; index < Owners.Length; index++)
        {
            var row = owners[index]; var expected = Owners[index]; var effects = row.GetProperty("Effects");
            Require(row.GetProperty("DataId").GetString() == expected.Id && row.GetProperty("AssetKey").GetString() == expected.Key &&
                !row.GetProperty("DisallowedInPlacementPhase").GetBoolean() && effects.GetArrayLength() == 1,
                "Original campaign owner identity changed.");
            var effect = effects[0];
            Require(effect.GetProperty("Type").GetString() == expected.Type && effect.GetProperty("Int").GetInt32() == expected.Int &&
                effect.GetProperty("Float").GetSingle() == expected.Float && effect.GetProperty("Int2").GetInt32() == 0 &&
                effect.GetProperty("Conditions").GetInt32() == 0 && effect.GetProperty("Subtype").GetString() is "SubtypesData_None" or "SubtypeData_None",
                "Original campaign effect payload changed.");
        }
        var slots = root.GetProperty("Slots").EnumerateArray().ToArray();
        foreach (var owner in new[] { Owners[0], Owners[1], Owners[3] })
            Require(RelicModel.Validate(new[] { new CombatRelicState(owner.Id, owner.Key, new[] { owner.Type }) }) == null,
                "A native campaign-only slot/trial-reward owner blocks an otherwise captured battle context.");
        var trialOwner = Owners[3];
        Require(RelicModel.Validate(new[] { new CombatRelicState(trialOwner.Id, trialOwner.Key,
                new[] { trialOwner.Type, "UncalibratedTrialCombatCallback" }) }) != null,
            "Accepting the calibrated trial-reward effect must not hide an unknown combat callback.");
        Require(slots.Length == 80, "Missing slot matrix observations.");
        foreach (var row in slots)
        {
            string phase = row.GetProperty("Phase").GetString()!;
            string[] types = phase switch { "spell" => ["Spell"], "both" => ["Spell", "Monster"], "unit" => ["Monster"], "baseline" or "removed" => [], _ => throw new InvalidDataException("Unknown slot phase.") };
            var effects = row.GetProperty("Effects").EnumerateArray().ToArray();
            Require(effects.Select(effect => effect.GetProperty("CardType").GetString()).SequenceEqual(types) &&
                effects.All(effect => effect.GetProperty("Value").GetInt32() == 1 && effect.GetProperty("Subtype").GetString() ==
                    (effect.GetProperty("CardType").GetString() == "Spell" ? "SubtypeData_None" : "SubtypesData_None")),
                "Native slot interfaces disagree with original unconditional owner payloads.");
            int predicted = CampaignRelicModel.UpgradeSlots(row.GetProperty("CardType").GetString()!, row.GetProperty("Base").GetInt32(),
                row.GetProperty("Maximum").GetInt32(), row.GetProperty("Modifier").GetInt32(), types);
            Require(predicted == row.GetProperty("Actual").GetInt32(), "Native BalanceData.GetUpgradeSlots differs.");
        }
        var merchant = root.GetProperty("Merchant").EnumerateArray().ToArray();
        Require(merchant.Length == 108, "Missing merchant price matrix.");
        foreach (var row in merchant)
        {
            bool owned = row.GetProperty("Phase").GetString() == "owned";
            int predicted = CampaignRelicModel.MerchantCost(row.GetProperty("Costs").Deserialize<int[]>()!, row.GetProperty("Purchases").GetInt32(),
                owned ? [-.25f] : [], row.GetProperty("Discount").GetSingle(), row.GetProperty("DiscountAllowed").GetBoolean());
            Require(predicted == row.GetProperty("Actual").GetInt32(), "Native merchant price differs.");
        }
        var trial = root.GetProperty("Trial").EnumerateArray().ToArray();
        Require(trial.Length == 66, "Missing trial reward display/grant matrix.");
        foreach (var row in trial)
        {
            bool owned = row.GetProperty("Phase").GetString() == "owned";
            int predicted = GoldRewardModel.Adjust(row.GetProperty("Amount").GetInt32(), multiplier: row.GetProperty("Multiplier").GetSingle(),
                applyTrialDataModifiers: row.GetProperty("ApplyTrial").GetBoolean(), trialGoldPercents: owned ? [50] : []);
            Require(predicted == row.GetProperty("Shown").GetInt32() && predicted == row.GetProperty("Granted").GetInt32(),
                "Actual trial reward display or gold grant differs.");
        }
        var hoard = root.GetProperty("Hoard").EnumerateArray().ToArray();
        Require(hoard.Length == 6 && hoard.Select(row => row.GetProperty("OwnerId").GetString()).Distinct().Count() == 2, "Missing both original hoard owner matrices.");
        foreach (var row in hoard)
        {
            int change = Owners.Single(owner => owner.Id == row.GetProperty("OwnerId").GetString()).Int;
            int maximum = row.GetProperty("Maximum").GetInt32();
            int acquired = CampaignRelicModel.AcquireHoardCap(row.GetProperty("BeforeCap").GetInt32(), change, maximum);
            var removed = CampaignRelicModel.RemoveHoardCap(acquired, row.GetProperty("BeforeRemovalAmount").GetInt32(), change, maximum);
            Require(row.GetProperty("Change").GetInt32() == change && row.GetProperty("AcquiredCap").GetInt32() == acquired &&
                row.GetProperty("RemovedCap").GetInt32() == removed.Cap && row.GetProperty("RemovedAmount").GetInt32() == removed.Amount,
                "Native hoard owner acquisition/removal differs.");
        }
        Require(slots.Any(row => row.GetProperty("Phase").GetString() == "spell" && row.GetProperty("CardType").GetString() == "Spell" &&
            row.GetProperty("Modifier").GetInt32() == 0 && row.GetProperty("Actual").GetInt32() > row.GetProperty("Base").GetInt32()) &&
            slots.Any(row => row.GetProperty("Phase").GetString() == "unit" && row.GetProperty("CardType").GetString() == "Monster" &&
            row.GetProperty("Modifier").GetInt32() == 0 && row.GetProperty("Actual").GetInt32() > row.GetProperty("Base").GetInt32()),
            "Original slot owners did not change an eligible native card.");
        Console.WriteLine("CAMPAIGN-RELICS-NATIVE PASS: six unchanged owners; 80 slot queries, 108 merchant prices, 66 actual reward displays/grants, six hoard acquisition/removal transitions; baseline/removed controls and recorded state/RNG restoration.");
    }
    internal static bool SupportsCalibrationBuild(FixtureValue root)
        => (root.GetProperty("GameVersion").GetString(), root.GetProperty("GameModuleMvid").GetString()) switch
        {
            (Game231FixtureCatalog.Version, Game231FixtureCatalog.Module) => true,
            ("2.2.1", var module) => module == NativeBranchIdentity.HistoricalModule.ToString(),
            _ => false
        };

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
