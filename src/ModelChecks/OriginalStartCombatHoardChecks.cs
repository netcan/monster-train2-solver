using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalStartCombatHoardChecks
{
    private const string DataId = "3b6780ab-a1e8-443d-9ddb-5caedbd87775";

    internal static void Native(FixtureValue root)
    {
        Run();
        Require(root.GetProperty("Schema").GetInt32() == 201 &&
            root.GetProperty("ModifierScenario").GetString() == "original-start-combat-hoard-relic" &&
            root.GetProperty("GameVersion").GetString() == "2.2.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }
                .All(name => root.GetProperty(name).GetInt32() == 0), "Start-combat Hoard battle failed acceptance.");

        var operations = root.GetProperty("RelicResourceOperations").EnumerateArray().ToArray();
        Require(operations.Length == 2 && operations[0].GetProperty("Kind").GetString() == "Acquire" &&
            operations[1].GetProperty("Kind").GetString() == "StartCombatHoard", "Unexpected acquisition/start-combat Hoard callback order.");
        var acquisition = operations[0];
        var acquisitionBefore = acquisition.GetProperty("Before");
        var acquisitionAfter = acquisition.GetProperty("After");
        Require(!acquisitionBefore.GetProperty("Relics").Deserialize<CombatRelicState[]>()!.Any(relic => relic.DataId == DataId) &&
            acquisitionAfter.GetProperty("Relics").Deserialize<CombatRelicState[]>()!.Count(relic => relic.DataId == DataId) == 1 &&
            acquisitionBefore.GetProperty("Hoard").GetInt32() == acquisitionAfter.GetProperty("Hoard").GetInt32() &&
            acquisitionBefore.GetProperty("Gold").GetInt32() == acquisitionAfter.GetProperty("Gold").GetInt32(),
            "Acquisition was missing, duplicated or changed campaign resources.");

        var callback = operations[1];
        var before = callback.GetProperty("Before");
        var after = callback.GetProperty("After");
        var relics = before.GetProperty("Relics").Deserialize<CombatRelicState[]>()!;
        CombatRelicState owner = relics.Single(relic => relic.DataId == DataId);
        Require(owner.AssetKey == "ModifyDragonsHoardBattleStart" && owner.DisallowedInPlacementPhase == false &&
            owner.IsCovenant == false && owner.EffectTypes.SequenceEqual([RelicResourceModel.StartCombatHoard]),
            "Original start-combat Hoard owner changed.");
        RelicResourceEffect rule = owner.ResourceEffects!.Single();
        Require(rule.EffectIndex == 0 && rule.Type == RelicResourceModel.StartCombatHoard && rule.ParamInt == 1 &&
            rule.ParamInt2 == 0 && rule.Amount == 1 && !rule.CanApplyInPreviewMode && rule.Conditions.Count == 0,
            "Original +1 Hoard callback payload changed.");
        int initial = before.GetProperty("Hoard").GetInt32();
        int cap = before.GetProperty("Resources").Deserialize<BattleResourceState>()!.DragonsHoardCap!.Value;
        var source = new CampaignBattleResources(before.GetProperty("Gold").GetInt32(), initial, cap);
        CampaignBattleResources predicted = CampaignBattleResourceModel.StartCombatHoard(source, rule.Amount,
            before.GetProperty("Preview").GetBoolean());
        Require(callback.GetProperty("OwnerDataId").GetString() == DataId && callback.GetProperty("EffectIndex").GetInt32() == 0 &&
            callback.GetProperty("Deployment").GetBoolean() && callback.GetProperty("Notifications").GetInt32() == 0 &&
            !before.GetProperty("Preview").GetBoolean() && initial < cap && after.GetProperty("Hoard").GetInt32() == initial + 1 &&
            after.GetProperty("Hoard").GetInt32() == predicted.Hoard && after.GetProperty("Gold").GetInt32() == source.Gold &&
            callback.GetProperty("CardRefreshes").GetInt32() == 1 && callback.GetProperty("MonsterRefreshes").GetInt32() == 1,
            "Native deployment callback, resource clamp or dependent refreshes differ.");

        var turns = root.GetProperty("Turns").EnumerateArray().ToArray();
        Require(turns.Length >= 3 && turns[0].GetProperty("Before").Deserialize<BattleTurnState>()!.Spawn.Turn == 0,
            "Missing deployment and later-turn replay controls.");
        foreach (var row in turns)
        {
            var sourceTurn = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            var actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            var replay = BattleTurnModel.EndTurn(sourceTurn);
            bool terminal = replay.Outcome is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated;
            Require(replay.Supported && replay.Outcome == (RoomOutcome)row.GetProperty("ActualOutcome").GetInt32() &&
                BattleTurnChecks.Comparable(replay.State!, terminal) == BattleTurnChecks.Comparable(actual, terminal),
                "Start-combat Hoard turn integration differs: " + replay.UnsupportedReason);
        }
        Console.WriteLine($"ORIGINAL-START-COMBAT-HOARD-NATIVE PASS: unchanged +1 owner, deployment callback, {turns.Length} exact EndTurns, cap-aware Hoard update and both native resource refreshes.");
    }

    internal static void Run()
    {
        int cases = 0;
        for (int cap = 0; cap <= 12; cap++)
        for (int hoard = 0; hoard <= cap; hoard++)
        for (int change = -16; change <= 16; change++)
        foreach (bool preview in new[] { false, true })
        {
            var source = new CampaignBattleResources(23, hoard, cap);
            string frozen = JsonSerializer.Serialize(source);
            CampaignBattleResources result = CampaignBattleResourceModel.StartCombatHoard(source, change, preview);
            int expected = preview ? hoard : Math.Clamp(unchecked(hoard + change), 0, cap);
            Require(result.Hoard == expected && result.Gold == source.Gold && result.HoardCap == cap &&
                JsonSerializer.Serialize(source) == frozen, "Hoard clamp, preview skip or parent immutability differs.");
            if (preview || expected == hoard) Require(ReferenceEquals(result, source), "No-op Hoard adjustment changed branch identity.");
            cases++;
        }

        var overflow = new CampaignBattleResources(0, 1, 12);
        Require(CampaignBattleResourceModel.StartCombatHoard(overflow, int.MaxValue).Hoard == 0 &&
            CampaignBattleResourceModel.StartCombatHoard(overflow, int.MinValue).Hoard == 0 &&
            CampaignBattleResourceModel.StartCombatHoard(overflow, 1, preview: true).Hoard == 1,
            "Hoard adjustment overflow or preview boundary differs from native arithmetic.");
        var shared = new CampaignBattleResources(23, 3, 7);
        string sharedJson = JsonSerializer.Serialize(shared);
        Parallel.For(0, 32, _ => Require(CampaignBattleResourceModel.StartCombatHoard(shared, 1).Hoard == 4 &&
            JsonSerializer.Serialize(shared) == sharedJson, "Parallel Hoard branch mutated its parent."));
        Console.WriteLine($"ORIGINAL-START-COMBAT-HOARD-PURE PASS: {cases} clamp/preview branches, overflow and parallel immutability.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
