using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class CampaignBattleResourceChecks
{
    private static readonly (string Id, string Key, string Type, string Resource, int Amount, bool Required)[] Owners = [
        ("28772b0a-6f5c-44a7-98dd-260a851dd391", "Inkbound_KwillingCatcher", "RelicEffectModifyGoldEndOfBattle", "Gold", 100, false),
        ("26c9bc93-f5a6-439d-8239-14c22cbeb02e", "ModifyDragonsHoardBattleEnd", "RelicEffectModifyDragonsHoardEndOfBattle", "Hoard", 3, true) ];
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path); var root = document.RootElement;
        Require(root.GetProperty("Schema").GetInt32() == 1 && root.GetProperty("GameVersion").GetString() == "2.2.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("Before").ContentEquals(root.GetProperty("After")), "End-battle capture identity/restoration differs.");
        var definitions = root.GetProperty("Owners").EnumerateArray().ToArray(); Require(definitions.Length == 2, "Missing original resource owners.");
        for (int i = 0; i < definitions.Length; i++)
        {
            var definition = definitions[i]; var expected = Owners[i]; var effects = definition.GetProperty("Effects");
            Require(definition.GetProperty("Id").GetString() == expected.Id && definition.GetProperty("Key").GetString() == expected.Key &&
                !definition.GetProperty("DisallowedInPlacementPhase").GetBoolean() && effects.GetArrayLength() == 1 &&
                effects[0].GetProperty("Type").GetString() == expected.Type && effects[0].GetProperty("Int").GetInt32() == expected.Amount &&
                effects[0].GetProperty("Bool").GetBoolean() == expected.Required && effects[0].GetProperty("Conditions").GetInt32() == 0,
                "Original end-battle resource owner changed.");
        }
        var rows = root.GetProperty("Rows").EnumerateArray().ToArray();
        Require(rows.Length == 756 && rows.Select(row => (S(row, "Phase"), S(row, "Scenario"), I(row, "Cap"), I(row, "AmountCase"), I(row, "Repetition"))).Distinct().Count() == 756,
            "Incomplete or duplicated native end-battle matrix.");
        int gold = 0, hoard = 0, blocked = 0, reset = 0;
        foreach (var row in rows)
        {
            string phase = S(row, "Phase"), scenario = S(row, "Scenario");
            int[] indices = phase switch { "baseline" or "removed" => [], "gold" => [0], "both" => [0, 1], "hoard" => [1], "reversed" => [1, 0], _ => throw new InvalidOperationException("Unknown native phase.") };
            Require(scenario is "idle" or "zero" or "negative" or "wrong-trigger" or "null-character" or "damage" or "reset-after-damage", "Unknown damage control.");
            var native = row.GetProperty("Owned").EnumerateArray().ToArray(); Require(native.Length == indices.Length, "Native owner count differs.");
            var states = indices.Select(index => {
                var definition = Owners[index]; var parent = new CampaignBattleResourceOwner(definition.Id, definition.Resource, definition.Amount, definition.Required, true);
                var owner = CampaignBattleResourceModel.StartBattle(parent);
                Require(parent.PyreWasDamaged && !owner.PyreWasDamaged, "Start-battle state mutated its parent or failed to reset.");
                if (scenario != "idle") owner = CampaignBattleResourceModel.Observe(owner, scenario == "wrong-trigger" ? "OnDeath" : "OnHit",
                    scenario != "null-character", scenario == "zero" ? 0 : scenario == "negative" ? -1 : 1);
                if (scenario == "reset-after-damage") owner = CampaignBattleResourceModel.StartBattle(owner);
                return owner;
            }).ToArray();
            for (int i = 0; i < states.Length; i++) Require(S(native[i], "Id") == states[i].Id &&
                native[i].GetProperty("Damaged").GetBoolean() == states[i].PyreWasDamaged, "Native Test callback damage memory differs.");
            int cap = I(row, "Cap"), amountCase = I(row, "AmountCase");
            Require(cap is 0 or 2 or 5 && amountCase is >= 0 and <= 2 && I(row, "Repetition") is 0 or 1 && I(row, "BeforeGold") == 123 &&
                I(row, "BeforeHoard") == (amountCase == 0 ? 0 : amountCase == 1 ? Math.Max(0, cap - 1) : cap) &&
                row.GetProperty("RngBefore").ContentEquals(row.GetProperty("RngAfter")), "Native resource inputs or RNG differ.");
            var input = new CampaignBattleResources(I(row, "BeforeGold"), I(row, "BeforeHoard"), cap);
            string parentJson = JsonSerializer.Serialize(states);
            var result = CampaignBattleResourceModel.EndBattle(input, states, row.GetProperty("Multiplier").GetSingle());
            Require(result.Gold == I(row, "Gold") && result.Hoard == I(row, "Hoard") && result.HoardCap == cap &&
                input.Gold == 123 && JsonSerializer.Serialize(states) == parentJson, "Actual end-battle consumer or parent immutability differs.");
            if (result.Gold > input.Gold) gold++;
            if (result.Hoard > input.Hoard) hoard++;
            if (states.Any(owner => owner.RequireNoPyreDamage && owner.PyreWasDamaged)) blocked++;
            if (scenario == "reset-after-damage" && result.Hoard > input.Hoard) reset++;
        }
        var both = Owners.Select(owner => new CampaignBattleResourceOwner(owner.Id, owner.Resource, owner.Amount, owner.Required)).ToArray();
        var branch = new CampaignBattleResources(123, 0, 5); string branchJson = JsonSerializer.Serialize(both);
        Parallel.For(0, 16, _ => {
            var result = CampaignBattleResourceModel.EndBattle(branch, both);
            Require(result.Gold == 223 && result.Hoard == 3 && branch.Gold == 123 && branch.Hoard == 0 && JsonSerializer.Serialize(both) == branchJson,
                "Parallel resource branches changed parent state.");
        });
        Require(gold == 378 && hoard == 144 && blocked == 54 && reset == 24, "Native resource positives/damage/reset controls missing.");
        Console.WriteLine($"CAMPAIGN-BATTLE-RESOURCE-NATIVE PASS: two unchanged original owners, {rows.Length} actual end-battle manager calls, {gold} gold and {hoard} hoard increases, {blocked} damage gates, {reset} reset recoveries, unchanged recorded state/frame/RNG and 16 parallel branches. Campaign resource consumer only; damage events are controlled native callbacks.");
    }
    private static string S(FixtureValue row, string key) => row.GetProperty(key).GetString()!;
    private static int I(FixtureValue row, string key) => row.GetProperty(key).GetInt32();
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
