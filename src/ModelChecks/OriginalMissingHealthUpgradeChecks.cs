using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class OriginalMissingHealthUpgradeChecks
{
    private const string OriginalId = "80d43965-4baf-4b30-b4d6-e6bdef08f115";
    private const string UpgradeId = "9c84ad24-7aff-4cab-a00f-118e2f0515a8";
    private static void Require(bool condition, string reason) => TrueFinalBossChecks.Require(condition, reason);
    private static string Json<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);

    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path); var root = document.RootElement;
        Require(root.GetProperty("OriginalMissingHealthUpgradePrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Incomplete original missing-health upgrade battle.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero native " + key);
        var definition = root.GetProperty("OriginalMissingHealthUpgradeDefinition");
        Require(definition.GetProperty("Id").GetString() == OriginalId && definition.GetProperty("Key").GetString() == "Mournguard" &&
            definition.GetProperty("Cost").GetInt32() == 2 && definition.GetProperty("Attack").GetInt32() == 20 &&
            definition.GetProperty("Health").GetInt32() == 30 && definition.GetProperty("Size").GetInt32() == 2 &&
            definition.GetProperty("Traits").GetArrayLength() == 0, "Original Mournguard definition changed.");
        var nativeTrigger = definition.GetProperty("Triggers").EnumerateArray().Single();
        var nativeEffect = nativeTrigger.GetProperty("Effects").EnumerateArray().Single();
        var raw = nativeEffect.GetProperty("Raw");
        Require(nativeTrigger.GetProperty("Kind").GetString() == "PostCombat" && raw.GetProperty("Type").GetString() == MissingHealthUnitUpgradeModel.NativeEffect &&
            raw.GetProperty("Int").GetInt32() == 1 && raw.GetProperty("Target").GetString() == "Self" &&
            nativeEffect.GetProperty("Upgrade").GetProperty("DataId").GetString() == UpgradeId,
            "Original missing-health divisor, trigger or upgrade identity changed.");
        int[] ids = root.GetProperty("OriginalMissingHealthUpgradeIds").Deserialize<int[]>()!;
        var actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 8 && ids.Distinct().Count() == 8 && actions.Length == 8 && actions.Select(row =>
            row.GetProperty("Action").GetProperty("CardInstanceId").GetInt32()).SequenceEqual(ids), "Missing eight ordered native inputs.");
        BattleTurnState Before(int index) => actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int index) => actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatUnit Actor(BattleTurnState state, int cardId) => state.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == cardId);
        foreach (int index in new[] { 2, 3 })
        {
            var before = Before(index); var after = After(index);
            var rule = before.PlayRules!.Cards.Single(rule => rule.DataId == OriginalId);
            var effect = rule.SpawnUnit!.Triggers.Single().Effects.Single();
            Require(before.UiRngIsolated && before.Spawn.Train.Context!.FindCard(ids[index])!.DataId == OriginalId &&
                before.Energy - after.Energy == 2 && rule.Cost == 2 && rule.ExternalInteractions.Count == 0 &&
                rule.SpawnUnit.BaseAttack == 20 && rule.SpawnUnit.Health == 30 && rule.SpawnUnit.Size == 2 &&
                effect.Type == MissingHealthUnitUpgradeModel.NativeEffect && effect.UnitUpgrade is {
                    Type: MissingHealthUnitUpgradeModel.Effect, Value: 1, Target: "Self", ScaleUpgrade: true,
                    Lifetime: "TemporaryUntilEndOfBattle", Range: null } action && action.Upgrade!.DataId == UpgradeId &&
                action.Upgrade.Stats.Damage == 1 && action.Upgrade.Stats.Health == 0,
                "Original spawn, cost or accumulated missing-health projection differs.");
            var actor = Actor(after, ids[index]);
            Require(actor.Attack == 30 && actor.Health == 30 && actor.MaxHealth == 40 && actor.Status("zeal")?.Stacks == 10 &&
                actor.Modifiers!.Upgrades.Count == 0, "Original Zeal birth was altered or upgraded before PostCombat.");
        }
        Require(Actor(After(4), ids[2]).Health == 40 && Actor(After(4), ids[3]).Health == 30 &&
            Actor(After(5), ids[2]).Health == 32 && Actor(After(6), ids[2]).Health == 38 && Actor(After(7), ids[2]).Health == 32,
            "Missing native full-heal/damage/reset controls or independent actor state.");
        var turns = root.GetProperty("Turns").EnumerateArray().ToArray();
        Require(turns.Length == 5, "Missing complete native turns.");
        int[] firstGrowth = { 0, 7, 7, 11 }, secondGrowth = { 9, 17, 24, 30 };
        foreach (var (row, index) in turns.Skip(1).Select((row, index) => (row, index)))
        {
            var state = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            foreach (var (cardId, growth) in new[] { (ids[2], firstGrowth[index]), (ids[3], secondGrowth[index]) })
            {
                CombatUnit actor = Actor(state, cardId);
                var source = state.Spawn.Train.Context!.FindCard(cardId)!;
                Require(source.DataId == OriginalId && source.Permanent.Upgrades.Count == 0 &&
                    actor.Modifiers!.Upgrades.Count == (growth == 0 ? 0 : 1) &&
                    source.Temporary.Upgrades.Count == actor.Modifiers.Upgrades.Count,
                    "Repeated growth added multiple upgrade records, lost ownership or became permanent.");
                if (growth > 0)
                {
                    var upgrade = actor.Modifiers.Upgrades.Single();
                    Require(upgrade.DataId == UpgradeId && upgrade.Stats.Damage == growth && upgrade.Stats.Health == 0 &&
                        upgrade.Statuses.Count == 0 && Json(upgrade) == Json(source.Temporary.Upgrades.Single()),
                        "Accumulated native unit/source-card upgrade values or metadata differ.");
                }
                Require(actor.Attack == 20 + (actor.Status("zeal")?.Stacks ?? 0) + growth,
                    "Native accumulated attack differs after Zeal decay.");
            }
        }
        foreach (var row in actions)
            Require(!BattleActionModel.EnumerateActions(row.GetProperty("Before").Deserialize<BattleTurnState>()!).HasUnsupportedAttempts,
                "Recorded missing-health battle contains an unsupported search candidate.");
        RecordedBattleChecks.Native(root);
        BattleSearchChecks.FrontierDiagnostics(root);
        BattleSearchChecks.Native(root);
        Console.WriteLine("ORIGINAL-MISSING-HEALTH-UPGRADE PASS: unchanged Mournguards, Zeal missing HP, heal/damage/reset controls, independent repeated growth in one exact unit/source record, full winning chain/16 branches and serial/parallel search.");
    }
}
