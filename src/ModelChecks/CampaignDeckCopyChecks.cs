using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class CampaignDeckCopyChecks
{
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path); var root = document.RootElement; var owner = root.GetProperty("Owner");
        Require(root.GetProperty("Schema").GetInt32() == 1 && root.GetProperty("GameVersion").GetString() == "2.2.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" && root.GetProperty("Before").ContentEquals(root.GetProperty("After")), "Copy capture identity/restoration differs.");
        Require(owner.GetProperty("Id").GetString() == "8ee399d2-4ee4-409d-af95-319bcd6accc4" && owner.GetProperty("Key").GetString() == "MothersRemnant1Relic" &&
            owner.GetProperty("Type").GetString() == "RelicEffectCopyRandomCardFromDeckEndOfBattle" && owner.GetProperty("Conditions").GetInt32() == 0, "Original permanent-copy owner changed.");
        var mask = owner.GetProperty("Mask").Deserialize<CardUpgradeMaskRule>()!;
        Require(mask.AssetKey == "ExcludeChampions" && mask.CardType == "Invalid" && mask.Subtypes.Excluded.SequenceEqual(["SubtypesData_Champion_83f21cbe-9d9b-4566-a2c3-ca559ab8ff34"]), "Original champion exclusion mask changed.");
        int rows = 0, positive = 0, empty = 0, modified = 0, multi = 0;
        foreach (var row in root.GetProperty("Rows").EnumerateArray())
        {
            var before = row.GetProperty("Before").Deserialize<CampaignCopyCard[]>()!; var actual = row.GetProperty("Actual").Deserialize<CampaignCopyCard[]>()!;
            var rng = row.GetProperty("RewardsBefore").Deserialize<UnityRng>(); bool owned = row.GetProperty("Owned").GetBoolean(); int next = row.GetProperty("NextIdentity").GetInt32();
            int[] candidates = before.Where(card => CardUpgradeMaskModel.FilterCard(mask, card.Mask)).Select(card => card.Identity).ToArray();
            Require(candidates.SequenceEqual(row.GetProperty("Candidates").EnumerateArray().Select(value => value.GetInt32())), "Native candidate order/filter differs.");
            var result = CampaignDeckCopyModel.EndBattle(before, rng, owned, mask, next);
            Equal(result.Deck, actual, "permanent deck mutation"); Equal(result.Rng, row.GetProperty("RewardsAfter").Deserialize<UnityRng>(), "Rewards RNG");
            Require(row.GetProperty("FreshReferences").GetBoolean() && row.GetProperty("EmptyTemporary").GetBoolean() && row.GetProperty("AnimationReferences").GetBoolean(), "Copy reused a source object, copied temporary modifiers or missed its presentation boundary.");
            Equal(WithoutRewards(row.GetProperty("RngBefore")), WithoutRewards(row.GetProperty("RngAfter")), "unrelated RNG");
            if (owned && candidates.Length > 0)
            {
                Require(actual.Length == before.Length + 1 && !before.Any(card => card.Identity == actual.Last().Identity), "Missing fresh permanent copy."); positive++;
                var source = before.Single(card => card.Identity == result.SourceIdentity);
                if (source.Purified && source.EventCached)
                {
                    Require(actual.Last().Purified && actual.Last().EventCached && !actual.Last().EventCachedDuplicate &&
                        actual.Last().Permanent.Upgrades.Count == 1 && actual.Last().Permanent.Offsets.Damage == 3, "Modified/purified/cached copy differs from native flag behavior."); modified++;
                }
                if (candidates.Length > 1) multi++;
            }
            else { Require(result.Rng.Equals(rng) && actual.Length == before.Length, "Empty/removed copy consumed state."); empty++; }
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            Parallel.For(0, 16, _ => { var branch = CampaignDeckCopyModel.EndBattle(before, rng, owned, mask, next); Equal(branch.Deck, actual, "parallel copy"); Equal(branch.Rng, result.Rng, "parallel RNG"); });
            Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Permanent copy mutated its parent."); rows++;
        }
        Require(rows == 72 && positive == 16 && empty == 56 && modified >= 4 && multi == 8, "Missing empty/champion-only/modified/mixed/removed native copy matrix.");
        Console.WriteLine($"CAMPAIGN-DECK-COPY-NATIVE PASS: unchanged mother relic, {rows} actual end-battle manager calls, {positive} fresh copies, {modified} modified/purified/cached witnesses, empty/champion/mixed/removed controls, exact permanent modifiers and Rewards RNG, 16 parallel branches. Presentation/pending-animation bookkeeping is suppressed after deck mutation.");
    }
    private static object[] WithoutRewards(FixtureValue rows) => rows.EnumerateArray().Where(row => row.GetProperty("Id").GetString() != "Rewards").Select(row => row.ToObjectGraph()).ToArray()!;
    private static void Equal<T>(T predicted, T actual, string label)
    {
        string a = JsonSerializer.Serialize(predicted, ModelJson.Options), b = JsonSerializer.Serialize(actual, ModelJson.Options);
        Require(a == b, "Permanent copy " + label + " differs: " + ModelJson.Difference(a, b));
    }
    private static void Require(bool pass, string error) { if (!pass) throw new InvalidOperationException(error); }
}
