using MonsterTrain2Poju.Model;

internal static class CampaignPurgeGoldChecks
{
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path); var root = document.RootElement; var owner = root.GetProperty("Owner");
        Require(root.GetProperty("Schema").GetInt32() == 1 && root.GetProperty("GameVersion").GetString() == "2.2.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("Before").ContentEquals(root.GetProperty("After")), "Purge consumer identity/restoration differs.");
        const string id = "d8d1e069-938e-4141-b6ac-e20f4578163a";
        Require(owner.GetProperty("Id").GetString() == id && owner.GetProperty("Key").GetString() == "BalatroEventCanio" &&
            owner.GetProperty("Type").GetString() == "RelicEffectAddGoldOnPurge" && owner.GetProperty("Amount").GetInt32() == 75 &&
            owner.GetProperty("CardType").GetString() == "Invalid" && !owner.GetProperty("DisallowedInPlacement").GetBoolean() &&
            owner.GetProperty("Conditions").GetInt32() == 0, "Original purge-gold definition changed.");
        var rule = new CampaignPurgeGoldOwner(id, 75, "Invalid", false, false);
        int count = 0, positive = 0, combatControls = 0, absent = 0; var types = new HashSet<string>();
        foreach (var row in root.GetProperty("Rows").EnumerateArray())
        {
            string type = row.GetProperty("CardType").GetString()!; types.Add(type);
            bool owned = row.GetProperty("Owned").GetBoolean(), combat = row.GetProperty("InCombat").GetBoolean();
            int gold = row.GetProperty("BeforeGold").GetInt32();
            var result = CampaignPurgeGoldModel.Apply(gold, owned ? [rule] : [], type, row.GetProperty("Multiplier").GetSingle(), combat, row.GetProperty("Placement").GetBoolean());
            Require(!row.GetProperty("CanApplyInPreview").GetBoolean() && row.GetProperty("Completed").GetInt32() == 1 &&
                !string.IsNullOrEmpty(row.GetProperty("CardId").GetString()) &&
                result.Gold == row.GetProperty("Gold").GetInt32() && result.TriggeredOwners.SequenceEqual(row.GetProperty("Notifications").EnumerateArray().Select(value => value.GetString()!)) &&
                row.GetProperty("RngBefore").ContentEquals(row.GetProperty("RngAfter")), "Purge gold, notifications, completion or RNG differs.");
            if (owned && !combat) { Require(result.Gold == gold + 75 && result.TriggeredOwners.Count == 1, "Missing positive purge reward."); positive++; }
            else if (owned) combatControls++; else absent++;
            Require(CampaignPurgeGoldModel.Apply(gold, [rule], type, preview: true).Gold == gold, "Pure preview control awarded purge gold.");
            Parallel.For(0, 16, _ => Require(CampaignPurgeGoldModel.Apply(gold, owned ? [rule] : [], type, inCombat: combat).Gold == result.Gold, "Parallel purge result differs."));
            count++;
        }
        Require(count == 144 && positive == 24 && combatControls == 24 && absent == 96 &&
            types.SetEquals(["Spell", "Monster", "Blight", "Junk", "Equipment", "TrainRoomAttachment"]), "Purge matrix lacks original card types or ownership controls.");
        Console.WriteLine($"CAMPAIGN-PURGE-GOLD-NATIVE PASS: unchanged Canio, {count} actual manager calls, {positive} rewards, six card types, combat/removed controls, notification and completion order, unchanged RNG and 16 parallel branches. Callback evidence excludes deck removal/payment UI.");
    }
    private static void Require(bool pass, string message) { if (!pass) throw new InvalidOperationException(message); }
}
