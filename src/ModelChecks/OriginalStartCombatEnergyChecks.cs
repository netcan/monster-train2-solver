using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalStartCombatEnergyChecks
{
    private const string DataId = "c5a68f6d-671d-4db3-999f-120b5febf41b";
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("ModifierScenario").GetString() == "original-start-combat-energy-relic" &&
            root.GetProperty("GameVersion").GetString() == "2.2.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }
                .All(name => root.GetProperty(name).GetInt32() == 0), "Start-combat energy battle failed acceptance.");
        var rows = root.GetProperty("RelicResourceOperations").EnumerateArray().ToArray();
        Require(rows.Length == 2 && rows[0].GetProperty("Kind").GetString() == "Acquire" &&
            rows[1].GetProperty("Kind").GetString() == "StartCombatEnergy", "Unexpected acquisition/start-combat callback order.");
        var acquisition = rows[0]; var callback = rows[1];
        var before = callback.GetProperty("Before"); var after = callback.GetProperty("After");
        var relics = before.GetProperty("Relics").Deserialize<CombatRelicState[]>()!;
        var owner = relics.Single(relic => relic.DataId == DataId);
        Require(owner.AssetKey == "OneEmberFirstTurn_DH" && owner.DisallowedInPlacementPhase == true &&
            owner.IsCovenant == false && owner.EffectTypes.SequenceEqual([RelicResourceModel.StartCombatEnergy]),
            "Original start-combat energy owner changed.");
        var rule = owner.ResourceEffects!.Single();
        Require(rule.EffectIndex == 0 && rule.Type == RelicResourceModel.StartCombatEnergy &&
            rule.ParamInt == 2 && rule.ParamInt2 == 0 && rule.Amount == 2 && !rule.CanApplyInPreviewMode &&
            rule.RequiredMutatorId == null && rule.Conditions.Count == 0,
            "Original start-combat energy payload changed.");
        Require(!acquisition.GetProperty("Before").GetProperty("Relics").Deserialize<CombatRelicState[]>()!.Any(relic => relic.DataId == DataId) &&
            acquisition.GetProperty("After").GetProperty("Relics").Deserialize<CombatRelicState[]>()!.Count(relic => relic.DataId == DataId) == 1 &&
            acquisition.GetProperty("Before").GetProperty("Energy").GetInt32() == acquisition.GetProperty("After").GetProperty("Energy").GetInt32(),
            "Acquisition was missing, duplicated or incorrectly added combat energy.");
        Require(callback.GetProperty("OwnerDataId").GetString() == DataId && callback.GetProperty("EffectIndex").GetInt32() == 0 &&
            !callback.GetProperty("Deployment").GetBoolean() && callback.GetProperty("Notifications").GetInt32() == 0 &&
            before.GetProperty("Turn").GetInt32() == 1 && after.GetProperty("Turn").GetInt32() == 1 &&
            !before.GetProperty("Preview").GetBoolean(), "Callback lacks the natural first-combat/no-notify boundary.");
        var raw = before.GetProperty("Resources").Deserialize<BattleResourceState>()!;
        int energy = before.GetProperty("Energy").GetInt32();
        var rng = UnityRng.Seed(7);
        var context = new CombatContext(new([], [], [], rng, 0, []), rng, 0, 1, raw.MaxHandSize,
            statistics: BattleStatistics.Empty(deckCards: []), queryFrame: new(energy: energy, runningCombat: true, turn: 1),
            energyState: new(raw.MaxEnergy, 0, 0, before.GetProperty("Phase").GetString()!, true), relics: relics, resources: raw);
        string frozen = JsonSerializer.Serialize(context);
        var result = RelicResourceModel.ApplyEnergyTiming(context, startOfCombat: true);
        Require(result.Supported && result.Value == after.GetProperty("Energy").GetInt32() && result.Value == energy + 2 &&
            result.Events.Count == 1 && !result.Events[0].Notified && frozen == JsonSerializer.Serialize(context),
            "Native first-combat energy model differs: " + result.UnsupportedReason);
        var ablated = RelicResourceModel.ApplyEnergyTiming(context.WithRelics(relics.Where(relic => relic.DataId != DataId).ToArray()), startOfCombat: true);
        Require(ablated.Supported && result.Value - ablated.Value == 2 &&
            RelicResourceModel.ApplyEnergyTiming(context, placement: true, startOfCombat: true).Value == energy,
            "Owner ablation/deployment control did not isolate the first-combat bonus.");
        Require(JsonSerializer.Serialize(relics) == JsonSerializer.Serialize(after.GetProperty("Relics").Deserialize<CombatRelicState[]>()),
            "No-notify callback changed relic state.");
        var turns = root.GetProperty("Turns").EnumerateArray().ToArray();
        Require(turns.Length >= 3 && turns[0].GetProperty("Before").Deserialize<BattleTurnState>()!.Spawn.Turn == 0,
            "Missing deployment and later-turn replay controls.");
        foreach (var row in turns)
        {
            var source = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            var actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            var replay = BattleTurnModel.EndTurn(source);
            bool terminal = replay.Outcome is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated;
            Require(replay.Supported && replay.Outcome == (RoomOutcome)row.GetProperty("ActualOutcome").GetInt32() &&
                BattleTurnChecks.Comparable(replay.State!, terminal) == BattleTurnChecks.Comparable(actual, terminal),
                "Start-combat turn integration differs: " + replay.UnsupportedReason);
        }
        Console.WriteLine($"ORIGINAL-START-COMBAT-ENERGY-NATIVE PASS: unchanged +2 owner, natural turn-one callback, no notifications, acquisition/deployment/ablation controls and {turns.Length} exact EndTurns.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
