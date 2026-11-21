using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalChangedTempUpgradeRelicsChecks
{
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 1 &&
            root.GetProperty("Boundary").GetString() == "DeploymentOriginalCardModifierComparison" &&
            root.GetProperty("GameVersion").GetString() == "2.3.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d",
            "Expected a current original temporary-relic modifier component.");

        const string steward = "38e70e6a-20f3-4e36-8d31-c5110be07f87";
        const string banner = "a7e84160-5e7b-4fd7-9227-7bf8f766454c";
        const string cruelLesson = "0a63cd82-9aee-4991-98af-4cc4233ed77e";
        const string sharedSentence = "de1ef23c-c6bc-4d84-9df5-dac93a4b6a4f";
        var owners = new[]
        {
            (Key: "NonBannerUnitsHaveDeployable", Id: "fb6332bb-3e1c-44ba-951b-39fbc8d43601",
                Upgrade: "5ee5c04a-0fd3-4f71-8cc8-15b92d4e1fd7", ExcludesCopy: true,
                Trait: "CardTraitDrawInDeploymentPhase", Cost: 0, XCost: 0,
                Cards: new[] { (steward, true), (banner, false) }),
            (Key: "ReduceStarterCost", Id: "d892106c-dbbc-40ac-bf0b-902882cdc69d",
                Upgrade: "cd3d2922-f28b-4950-a042-41e1d9e926ff", ExcludesCopy: false,
                Trait: "", Cost: -1, XCost: 1,
                Cards: new[] { (cruelLesson, true), (sharedSentence, true), ("5a88d0e1-1c31-40f0-9e8d-8068dad832e9", false) }),
            (Key: "MoltenManifold", Id: "f141d334-b503-48ff-ac21-5f2ad27105dd",
                Upgrade: "da974fcd-8ba4-4fcb-bb62-48eb07c79841", ExcludesCopy: true,
                Trait: "", Cost: 0, XCost: 2,
                Cards: new[] { ("a54a1fa9-8cf3-4be2-a9c6-d0e50bb82c7d", true), ("f1dbd7d5-74fc-4507-aba1-dc1a8ec9393b", false) }),
            (Key: "MoreRares_Relic", Id: "374995fd-7619-4244-9d1f-269429258584",
                Upgrade: "c62d9641-8948-4c00-b67d-60f0e728c1c1", ExcludesCopy: true,
                Trait: "", Cost: 0, XCost: 0,
                Cards: new[] { (steward, true), ("536e6de0-7ccf-412a-9a4e-ec55ab3988e5", false) })
        };
        FixtureValue[] rows = root.GetProperty("Cases").EnumerateArray().ToArray();
        Require(rows.Length == owners.Sum(owner => owner.Cards.Length), "Incomplete temporary-relic control set.");

        int index = 0, applied = 0, excludedCopies = 0;
        foreach (var expected in owners)
        foreach (var (dataId, accepted) in expected.Cards)
        {
            FixtureValue row = rows[index++];
            CombatRelicState owner = row.GetProperty("Owner").Deserialize<CombatRelicState>()!;
            RelicCardModifier effect = owner.CardModifiers!.Single();
            CardUpgradeModifier upgrade = effect.Upgrade!;
            RelicCardUpgradeRule rule = effect.Rule!;
            CardInstanceState before = row.GetProperty("Before").Deserialize<CardInstanceState>()!;
            CardInstanceState actual = row.GetProperty("Actual").Deserialize<CardInstanceState>()!;
            string name = expected.Key + "/" + row.GetProperty("AssetKey").GetString();
            Require(owner.AssetKey == expected.Key && owner.DataId == expected.Id &&
                owner.EffectTypes.SequenceEqual([RelicModel.AddTempUpgrade]) && effect.EffectIndex == 0 &&
                effect.Conditions.Count == 0 && rule.SourceMonsters && !rule.SourceHeroes &&
                upgrade.DataId == expected.Upgrade && upgrade.Stats.Cost == expected.Cost && upgrade.Stats.XCost == expected.XCost &&
                upgrade.ExcludeFromClones == expected.ExcludesCopy &&
                row.GetProperty("DataId").GetString() == dataId && before.DataId == dataId && actual.DataId == dataId,
                "Changed original modifier or input definition differs for " + name);

            if (expected.Key == "NonBannerUnitsHaveDeployable")
                Require(rule.Filters.Single().DisallowedPools.Single().Count == 117 &&
                    rule.Filters.Single().DisallowedPools.Single().Contains(banner), "Current Banner exclusion pool differs.");
            if (expected.Key == "ReduceStarterCost")
            {
                IReadOnlyList<string> pool = rule.Filters.Single().AllowedPools.Single();
                Require(pool.Count == 26 && pool.Contains(cruelLesson) && pool.Contains(sharedSentence),
                    "Current starter inclusion pool differs.");
            }

            bool[] definitionFilters = row.GetProperty("DefinitionFilters").Deserialize<bool[]>()!;
            bool[] ownedFilters = row.GetProperty("OwnedFilters").Deserialize<bool[]>()!;
            Require(definitionFilters.Length == rule.Filters.Count && ownedFilters.Length == rule.Filters.Count &&
                definitionFilters.All(value => value) == accepted && ownedFilters.All(value => value) == accepted &&
                row.GetProperty("Returned").GetBoolean() == accepted, "Native filter or effect result differs for " + name);

            string parent = Serialize(before);
            var predicted = RelicCardModifierModel.Apply(before, [owner], resetTemporary: false);
            Require(predicted.Supported && predicted.Card != null, "Model rejected native modifier for " + name + ". " + predicted.UnsupportedReason);
            RelicCardModifierDispatch dispatch = predicted.Dispatches.Single();
            int failed = Array.IndexOf(ownedFilters, false);
            bool[] evaluatedFilters = ownedFilters.Take(failed >= 0 ? failed + 1 : ownedFilters.Length).ToArray();
            Require(dispatch.Returned == accepted && dispatch.UpgradeAdded == accepted &&
                dispatch.Filters.Select(filter => filter.Accepted).SequenceEqual(evaluatedFilters) &&
                Serialize(predicted.Card!) == Serialize(actual) && Serialize(before) == parent,
                "Modeled modifier result or immutable input differs for " + name);

            CardUpgradeModifier? installed = actual.Temporary.Upgrades.SingleOrDefault(item => item.DataId == expected.Upgrade);
            Require((installed != null) == accepted, "Unexpected native upgrade on " + name);
            if (accepted && expected.Trait.Length > 0)
                Require(CardBranchMaskModel.Resolve(actual).Traits.Contains(expected.Trait), "Missing granted trait on " + name);
            if (accepted && expected.Key == "MoreRares_Relic")
                Require(installed!.Statuses.Count == 1 && installed.Statuses[0].Id == "endless" &&
                    installed.Statuses[0].Stacks == 1, "Missing granted Endless status on " + name);
            string[] ignored = row.GetProperty("ExcludedCopyUpgradeIds").Deserialize<string[]>()!;
            bool copyExcluded = accepted && expected.ExcludesCopy;
            Require(ignored.Contains(expected.Upgrade) == copyExcluded, "Native copy-filter exclusion differs for " + name);
            if (accepted) applied++;
            if (copyExcluded) excludedCopies++;
        }
        Console.WriteLine($"ORIGINAL-CHANGED-TEMP-UPGRADE-RELICS PASS cases={rows.Length} installed={applied} excludedFromCopies={excludedCopies}");
    }

    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidDataException(message);
    }
}
