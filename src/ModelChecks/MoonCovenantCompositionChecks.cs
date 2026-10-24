using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class MoonCovenantCompositionChecks
{
    internal static void Startup(FixtureValue root, CombatContext context, int level, bool guards = false)
    {
        Require(level is >= 1 and <= 10 && CardMoonUpgradeModel.CalibratedCovenants(context.Relics),
            "Moon spell startup requires the calibrated cumulative Covenant prefix.");
        CombatRelicState[] owners = context.Relics!.Where(owner => owner.IsCovenant == true).ToArray();
        FixtureValue startup = root.GetProperty("StartingCovenantControl"), selection = startup.GetProperty("Selection");
        Require(owners.Length == level && startup.GetProperty("Kind").GetString() == "NativeCovenantRunStartup" &&
            startup.GetProperty("AscensionLevel").GetInt32() == level && startup.GetProperty("StartingLevel").GetInt32() == level &&
            selection.GetProperty("Kind").GetString() == "SelectNativeCovenantAtRunSetup" &&
            selection.GetProperty("RequestedLevel").GetInt32() == level && selection.GetProperty("LevelAfter").GetInt32() == level &&
            !selection.GetProperty("EarnedUnlockClaimed").GetBoolean() && !selection.GetProperty("StartupEffectsInvokedByProbe").GetBoolean() &&
            startup.GetProperty("Owners").ContentEquals(startup.GetProperty("ExpectedOwners")) &&
            startup.GetProperty("Owners").GetArrayLength() == level,
            "Moon spell Covenant witnesses must come from original native run setup, without post-startup activation.");
        foreach (var pair in owners.Select((owner, index) => (owner, raw: startup.GetProperty("Owners")[index])))
            Require(pair.owner.DataId == pair.raw.GetProperty("Id").GetString() && pair.owner.AssetKey == pair.raw.GetProperty("AssetKey").GetString() &&
                pair.owner.EffectTypes.SequenceEqual(pair.raw.GetProperty("EffectTypes").EnumerateArray().Select(effect => effect.GetString()!)),
                "Covenant startup and live moon hand disagree on original identities or complete effect signatures.");
        FixtureValue[] calls = startup.GetProperty("StartupCalls").EnumerateArray().ToArray();
        Require(calls.Length == (level >= 5 ? 4 : 2) && calls[0].GetProperty("Effect").GetString() == "RelicEffectPurgeStarterDeck" &&
            calls[1].GetProperty("Effect").GetString() == "RelicEffectAddCardSetStartOfRun" &&
            calls.Take(2).All(call => call.GetProperty("OwnerId").GetString() == owners[0].DataId) &&
            calls.Skip(2).All(call => call.GetProperty("OwnerId").GetString() == owners[4].DataId &&
                call.GetProperty("Effect").GetString() == "RelicEffectAddCardsStartOfRun") &&
            calls.All(call => call.GetProperty("RngBefore").GetArrayLength() == 16 && call.GetProperty("RngAfter").GetArrayLength() == 16) &&
            calls[0].GetProperty("Before").GetArrayLength() > 1 && calls[0].GetProperty("After").GetArrayLength() == 1 &&
            calls[0].GetProperty("After")[0].GetProperty("IsChampion").GetBoolean() &&
            calls[0].GetProperty("RngBefore").ContentEquals(calls[0].GetProperty("RngAfter")) &&
            calls[0].GetProperty("After").ContentEquals(calls[1].GetProperty("Before")) &&
            calls[1].GetProperty("After").GetArrayLength() == 20 &&
            calls.Zip(calls.Skip(1), (before, after) => before.GetProperty("After").ContentEquals(after.GetProperty("Before"))).All(equal => equal) &&
            calls[^1].GetProperty("After").ContentEquals(startup.GetProperty("StartingDeck")),
            "Original startup must execute its complete purge/add chain and retain its native starting deck.");
        if (!guards) return;
        string parent = JsonSerializer.Serialize(context, ModelJson.Options);
        var mutations = new List<Action<JsonNode>> {
            node => node["Relics"]!.AsArray().First(item => item!["IsCovenant"]?.GetValue<bool>() == true)!["DataId"] = "unknown",
            node => node["Relics"]!.AsArray().First(item => item!["IsCovenant"]?.GetValue<bool>() == true)!["AssetKey"] = "unknown",
            node => node["Relics"]!.AsArray().First(item => item!["IsCovenant"]?.GetValue<bool>() == true)!["EffectTypes"]!.AsArray().Add("RelicEffectNull"),
            node => node["Relics"]!.AsArray().Add(node["Relics"]!.AsArray().First(item => item!["IsCovenant"]?.GetValue<bool>() == true)!.DeepClone()),
            node => node["Relics"]!.AsArray().Add(JsonSerializer.SerializeToNode(new CombatRelicState("extra", "extra",
                new[] { RelicModel.ModifyCardTypeCost }), ModelJson.Options))
        };
        if (level >= 2) mutations.Add(node => {
            JsonArray relics = node["Relics"]!.AsArray();
            JsonNode first = relics[0]!.DeepClone(); relics[0] = relics[1]!.DeepClone(); relics[1] = first;
        });
        if (level == 3) mutations.Add(node => node["Relics"]![2]!["EffectTypes"] = new JsonArray("RelicEffectModifyMerchantPurgeCosts", "RelicEffectModifyMerchantRerollCosts"));
        foreach (Action<JsonNode> mutate in mutations)
        {
            JsonNode changed = JsonNode.Parse(parent)!; mutate(changed);
            CombatContext invalid = changed.Deserialize<CombatContext>(ModelJson.Options)!;
            CardMoonUpgradeModel.RefreshHand(invalid, out string? error);
            Require(error != null, "Unknown, reordered, duplicate or additionally active Covenant moon composition bypassed its guard.");
        }
        Require(JsonSerializer.Serialize(context, ModelJson.Options) == parent, "Covenant guard checks mutated the native parent.");
        Console.WriteLine($"MOON-COVENANT PASS rank={level} nativeStartup=true originalDeck={startup.GetProperty("StartingDeck").GetArrayLength()} guardMutations={mutations.Count} earnedUnlockClaimed=false");
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
