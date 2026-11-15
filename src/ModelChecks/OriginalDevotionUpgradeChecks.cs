using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class OriginalDevotionUpgradeChecks
{
    private const string OriginalId = "0bf610e9-b1a0-4f02-a75c-71baca900fd1";
    private const string UpgradeId = "54e2f15c-6b08-4995-a7a0-b2060b1b5cfb";
    private static void Require(bool condition, string reason) => TrueFinalBossChecks.Require(condition, reason);
    private static string Json<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);

    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path); var root = document.RootElement;
        Require(root.GetProperty("OriginalDevotionUpgradePrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Incomplete original Devotion upgrade battle.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero native " + key);
        var definition = root.GetProperty("OriginalDevotionUpgradeDefinition");
        Require(definition.GetProperty("Id").GetString() == OriginalId && definition.GetProperty("Key").GetString() == "DarkInitiate" &&
            definition.GetProperty("Cost").GetInt32() == 1 && definition.GetProperty("Attack").GetInt32() == 15 &&
            definition.GetProperty("Health").GetInt32() == 25 && definition.GetProperty("Size").GetInt32() == 2,
            "Original Dark Initiate definition changed.");
        Require(definition.GetProperty("Traits").EnumerateArray().Single().GetProperty("Type").GetString() == "CardTraitTreasure",
            "Original Treasure trait was changed or removed.");
        var nativeTrigger = definition.GetProperty("Triggers").EnumerateArray().Single();
        var nativeEffect = nativeTrigger.GetProperty("Effects").EnumerateArray().Single();
        Require(nativeTrigger.GetProperty("Kind").GetString() == "OnSpawn" && nativeEffect.GetProperty("UpgradeId").GetString() == UpgradeId &&
            nativeEffect.GetProperty("Raw").GetProperty("Type").GetString() == DevotionModel.NativeUnitUpgrade,
            "Original summon trigger or upgrade identity changed.");
        int[] ids = root.GetProperty("OriginalDevotionUpgradeIds").Deserialize<int[]>()!;
        var actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 7 && ids.Distinct().Count() == 7 && actions.Length == 7 && actions.Select(row =>
            row.GetProperty("Action").GetProperty("CardInstanceId").GetInt32()).SequenceEqual(ids), "Missing seven ordered native inputs.");
        BattleTurnState Before(int index) => actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int index) => actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatUnit Actor(BattleTurnState state, int cardId) => state.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == cardId);
        foreach (var (index, level) in new[] { (0, 0), (4, 1), (6, 2) })
        {
            var before = Before(index); var after = After(index);
            var rule = before.PlayRules!.Cards.Single(rule => rule.DataId == OriginalId);
            var effect = rule.SpawnUnit!.Triggers.Single().Effects.Single();
            Require(before.UiRngIsolated && before.Spawn.Train.Context!.FindCard(ids[index])!.DataId == OriginalId &&
                before.Spawn.Train.Context.QueryFrame!.Devotion!.Level == level && rule.Cost == 1 &&
                before.Energy - after.Energy == 1 && rule.ExternalInteractions.Count == 0 &&
                rule.SpawnUnit.BaseAttack == 15 && rule.SpawnUnit.Health == 25 && rule.SpawnUnit.Size == 2 &&
                effect.Type == DevotionModel.NativeUnitUpgrade && effect.UnitUpgrade is { Type: DevotionModel.UnitUpgrade,
                    Target: "Self", Lifetime: "TemporaryUntilEndOfBattle", ScaleUpgrade: false, Range: null } action &&
                action.Upgrade!.DataId == UpgradeId && action.Upgrade.Stats.Damage == 15 && action.Upgrade.Stats.Health == 5,
                "Missing unchanged original summon, Devotion input, cost or repeated-upgrade projection.");
            var actor = Actor(after, ids[index]); var upgrades = actor.Modifiers!.Upgrades;
            Require(actor.Attack == 15 + 15 * level && actor.Health == 25 + 5 * level && actor.MaxHealth == actor.Health &&
                actor.Statuses.Count == 0 && upgrades.Count == level && upgrades.All(upgrade => upgrade.DataId == UpgradeId &&
                    upgrade.Stats.Damage == 15 && upgrade.Stats.Health == 5), "Native birth stats or independent upgrade records differ.");
            var sourceUpgrades = after.Spawn.Train.Context!.FindCard(ids[index])!.Temporary.Upgrades;
            Require(sourceUpgrades.Count == level && sourceUpgrades.All(upgrade => upgrade.DataId == UpgradeId &&
                upgrade.Stats.Damage == 15 && upgrade.Stats.Health == 5), "Battle-lifetime upgrades were not written to the source card separately.");
        }
        foreach (int index in new[] { 3, 5 })
        {
            var before = Before(index); var after = After(index);
            Require(after.Spawn.Train.Context!.QueryFrame!.Devotion!.Level == (index == 3 ? 1 : 2), "Control did not reach the intended native Devotion level.");
            Require(Json(Actor(before, ids[0])) == Json(Actor(after, ids[0])) &&
                (index == 3 || Json(Actor(before, ids[4])) == Json(Actor(after, ids[4]))), "Existing units were retroactively upgraded at level-up.");
        }
        RecordedBattleChecks.Native(root);
        BattleSearchChecks.Native(root);
        Console.WriteLine("ORIGINAL-DEVOTION-UPGRADE PASS: unchanged DarkInitiate, level-zero no-op, one/two separate +15/+5 upgrades and source-card records, no retroactive level-up, full winning chain/16 branches and serial/parallel search.");
    }
}
