using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalOpeningPassiveRelicsChecks
{
    private static readonly string[] Owners =
    [
        "a19cac7c-b2e0-42bd-bc1e-d180e17a1c15", "f03c7664-0485-4bcf-8bb9-f76321e090c3", "72706202-526e-4377-9745-7df40a26ba7d",
        "4200307a-616d-4cbb-a54e-527c5dc0f492", "fc1f6104-b3c1-4714-ac0f-9c76a6fb1a77", "79122f7a-6895-443a-830c-2db23027776c"
    ];
    private static readonly (string Key, string DataId, bool Upgraded, (int Owner, int Effect)[] Positive)[] Cases =
    [
        ("SelfPurgeBlight", "21038774-c0a4-413c-ab7e-27e458228e5f", false, [(0, 0)]),
        ("ScourgeT1", "62cc5532-5fa0-4e66-9fec-85be573af359", false, [(0, 1)]),
        ("TrainStewardShield", "d14a50f3-728d-43e1-87f0-ef1b013f6678", false, [(1, 0), (5, 0), (5, 1)]),
        ("TrainStewardShield", "d14a50f3-728d-43e1-87f0-ef1b013f6678", true, [(1, 0), (4, 0), (4, 1), (5, 0), (5, 1)]),
        ("RoyalWhelp", "198fb647-d5fc-4fb4-912c-cb8e64789412", false, [(1, 0), (2, 0), (5, 0), (5, 1)]),
        ("GreedDragon", "0060b286-4716-49e0-9714-3fd7811b9f72", false, [(1, 0), (2, 1), (5, 0), (5, 1)]),
        ("StarterTorch", "f1dbd7d5-74fc-4507-aba1-dc1a8ec9393b", true, [(3, 0)]),
        ("DevilishDetails", "10f64bfb-81cd-4b7c-bf46-6fc1fa832008", true, [])
    ];
    private sealed record Witness(CardInstanceState Before, CardInstanceState Actual, CombatRelicState[] Relics,
        CombatContext ContextBefore, CombatContext ContextAfter, bool ResetTemporary, RelicCardModifierDispatch[] Dispatches, string? Difference);

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 1 && root.GetProperty("Boundary").GetString() == "DeploymentOriginalPassiveRelicComparison" &&
            root.GetProperty("GameVersion").GetString() == "2.3.1" && root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            root.GetProperty("OwnerIds").Deserialize<string[]>()!.SequenceEqual(Owners), "Unexpected original passive relic capture identity.");
        FixtureValue[] cases = root.GetProperty("Cases").EnumerateArray().ToArray();
        Require(cases.Length == Cases.Length, "Missing original passive relic cases.");
        for (int index = 0; index < Cases.Length; index++)
        {
            var (key, dataId, upgraded, positive) = Cases[index];
            FixtureValue entry = cases[index];
            var row = entry.GetProperty("Witness").Deserialize<Witness>()!;
            string? upgradeId = entry.GetProperty("OriginalUpgradeId").GetString();
            Require(entry.GetProperty("AssetKey").GetString() == key && row.Before.DataId == dataId && row.Actual.DataId == dataId &&
                row.ResetTemporary && row.Difference == null &&
                row.Relics.Take(Owners.Length).Select(owner => owner.DataId).SequenceEqual(Owners), "Unexpected native passive relic witness for " + key);
            Require(upgraded ? !string.IsNullOrEmpty(upgradeId) && row.Before.Permanent.Upgrades.Any(upgrade => upgrade.DataId == upgradeId &&
                upgrade.MaskMetadata is { HasIcon: true, HideIcon: false, RegionRun: false }) : upgradeId == null && row.Before.Permanent.Upgrades.Count == 0,
                "Unexpected installed original upgrade for " + key);
            Require(row.Dispatches.Where(effect => effect.Returned).Select(effect => (effect.RelicIndex, effect.EffectIndex)).SequenceEqual(positive),
                "Unexpected positive native passive relic dispatches for " + key);

            var replay = RelicCardModifierModel.Apply(row.Before, row.Relics, row.ResetTemporary, row.ContextBefore);
            Require(replay.Supported, "Original passive relic replay unsupported: " + replay.UnsupportedReason);
            Equal(replay.Card, row.Actual, "card " + key);
            Equal(replay.Dispatches.Select(Dispatch).ToArray(), row.Dispatches.Select(Dispatch).ToArray(), "ordered dispatches " + key);
            Equal(replay.Context!.Relics, row.ContextAfter.Relics, "relic context " + key);
            Equal(replay.Context.Statistics, row.ContextAfter.Statistics, "statistics " + key);
        }
        Console.WriteLine($"ORIGINAL-OPENING-PASSIVE-RELICS PASS: {Owners.Length} ordered owners and {Cases.Length} exact native card modifier replays.");
    }

    private static object Dispatch(RelicCardModifierDispatch effect) => new
    { effect.RelicIndex, effect.EffectIndex, effect.Returned, effect.UpgradeAdded };

    private static void Equal<T>(T predicted, T actual, string name)
    {
        string? difference = ModelJson.Difference(JsonSerializer.Serialize(predicted, ModelJson.Options), JsonSerializer.Serialize(actual, ModelJson.Options));
        Require(difference == null, "Original passive relic " + name + " differs: " + difference);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
