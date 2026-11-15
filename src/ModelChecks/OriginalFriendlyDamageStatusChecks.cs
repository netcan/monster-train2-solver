using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class OriginalFriendlyDamageStatusChecks
{
    private const string OriginalId = "67678536-8a3b-4eb5-8c3f-50137c36f038";
    private static void Require(bool condition, string reason) => TrueFinalBossChecks.Require(condition, reason);
    private static string Json<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);

    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path); var root = document.RootElement;
        Require(root.GetProperty("OriginalFriendlyDamageStatusPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Incomplete original damage-taken status battle.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero native " + key);
        var definition = root.GetProperty("OriginalFriendlyDamageStatusDefinition");
        Require(definition.GetProperty("Id").GetString() == OriginalId && definition.GetProperty("Key").GetString() == "ScourgedZealot" &&
            definition.GetProperty("Cost").GetInt32() == 2 && definition.GetProperty("Attack").GetInt32() == 10 &&
            definition.GetProperty("Health").GetInt32() == 60 && definition.GetProperty("Size").GetInt32() == 3 &&
            definition.GetProperty("Traits").GetArrayLength() == 0, "Original Scourged Zealot definition changed.");
        var nativeTrigger = definition.GetProperty("Triggers").EnumerateArray().Single();
        var raw = nativeTrigger.GetProperty("Effects").EnumerateArray().Single().GetProperty("Raw");
        var nativeStatus = raw.GetProperty("Statuses").EnumerateArray().Single();
        Require(nativeTrigger.GetProperty("Kind").GetString() == "OnHit" && raw.GetProperty("Type").GetString() == FriendlyDamageStatusModel.NativeEffect &&
            raw.GetProperty("Int").GetInt32() == 0 && raw.GetProperty("Target").GetString() == "Self" && raw.GetProperty("Bool2").GetBoolean() &&
            raw.GetProperty("HasUpgrade").GetBoolean() && nativeStatus.GetProperty("statusId").GetString() == "zeal" &&
            nativeStatus.GetProperty("count").GetInt32() == 1, "Original zero-default divisor, IgnoreDualism, ignored upgrade field or status payload changed.");
        int[] ids = root.GetProperty("OriginalFriendlyDamageStatusIds").Deserialize<int[]>()!;
        var actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 14 && ids.Distinct().Count() == 14 && actions.Length == 14 && actions.Select(row =>
            row.GetProperty("Action").GetProperty("CardInstanceId").GetInt32()).SequenceEqual(ids), "Missing fourteen ordered native inputs.");
        BattleTurnState Before(int index) => actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int index) => actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatUnit Actor(BattleTurnState state, int cardId) => state.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == cardId);
        Require(Before(0).Spawn.Train.Context!.MaxHandSize == 20, "Missing captured twenty-slot control hand.");
        foreach (int index in new[] { 2, 3 })
        {
            var before = Before(index); var after = After(index);
            var rule = before.PlayRules!.Cards.Single(rule => rule.DataId == OriginalId);
            var effect = rule.SpawnUnit!.Triggers.Single().Effects.Single();
            Require(before.UiRngIsolated && before.Spawn.Train.Context!.FindCard(ids[index])!.DataId == OriginalId &&
                before.Energy - after.Energy == 2 && rule.Cost == 2 && rule.ExternalInteractions.Count == 0 &&
                rule.SpawnUnit.BaseAttack == 10 && rule.SpawnUnit.Health == 60 && rule.SpawnUnit.Size == 3 &&
                rule.SpawnUnit.DamageHistory != null && effect.Type == FriendlyDamageStatusModel.NativeEffect && effect.UnitUpgrade == null &&
                effect.Action is { Type: "AddStatus", Value: 0, Target: "Self", Range: null } action &&
                action.Statuses.Single() is { Id: "zeal", Stacks: 1 } && effect.StatusScaling?.IgnoreDualism == true,
                "Missing unchanged spawn, cost, birth cache or native status projection.");
            CombatUnit actor = Actor(after, ids[index]);
            Require(actor.Attack == 10 && actor.Health == 60 && actor.MaxHealth == 60 && actor.Statuses.Count == 0 &&
                Json(actor.DamageHistory) == Json(new UnitDamageHistory()), "Original birth was upgraded, damaged or inherited a stale cache.");
        }
        void Expect(int index, int hp, int maximum, int attack, int zeal, UnitDamageHistory history, int armor = 0, int shield = 0)
        {
            var state = After(index); var actor = Actor(state, ids[2]);
            Require(actor.Health == hp && actor.MaxHealth == maximum && actor.Attack == attack &&
                (actor.Status("zeal")?.Stacks ?? 0) == zeal && (actor.Status("armor")?.Stacks ?? 0) == armor &&
                (actor.Status("damage shield")?.Stacks ?? 0) == shield && Json(actor.DamageHistory) == Json(history),
                "Native HP/status/cache differs at action " + index);
            Require(actor.Modifiers!.Upgrades.Count == 0 && state.Spawn.Train.Context!.FindCard(ids[2])!.Temporary.Upgrades.Count == 0,
                "Unused native upgrade editor field was applied to the unit/source card.");
        }
        Expect(4, 53, 67, 17, 7, new UnitDamageHistory(7, 7));
        Expect(5, 53, 67, 17, 7, new UnitDamageHistory(7, 7), armor: 20);
        Expect(6, 53, 78, 28, 18, new UnitDamageHistory(0, 11, 11), armor: 9);
        Expect(7, 48, 92, 42, 32, new UnitDamageHistory(5, 14, 9));
        Expect(8, 48, 92, 42, 32, new UnitDamageHistory(5, 14));
        Expect(9, 48, 92, 42, 32, new UnitDamageHistory(5, 14), shield: 1);
        Expect(10, 48, 92, 42, 32, new UnitDamageHistory());
        Expect(13, 1, 1092, 1042, 1032, new UnitDamageHistory(47, 1000, 0, 953));
        var other = Actor(After(11), ids[3]);
        Require(other.Health == 56 && other.MaxHealth == 64 && other.Attack == 14 && other.Status("zeal")?.Stacks == 4 &&
            Json(other.DamageHistory) == Json(new UnitDamageHistory(4, 4)) && Json(Actor(After(10), ids[2])) == Json(Actor(After(11), ids[2])),
            "Different victims shared damage history or the wrong status amount.");
        foreach (int index in new[] { 4, 6, 7, 8, 10, 11, 13 })
            Require(Json(Before(index).Spawn.Train.Context!.BattleRng) == Json(After(index).Spawn.Train.Context!.BattleRng),
                "A fixed native status/divisor unexpectedly consumed Battle RNG.");
        var turns = root.GetProperty("Turns").EnumerateArray().ToArray();
        Require(turns.Length == 5, "Missing full native turns.");
        var prePreview = Actor(turns[1].GetProperty("Before").Deserialize<BattleTurnState>()!, ids[2]);
        var postPreview = Actor(turns[1].GetProperty("Actual").Deserialize<BattleTurnState>()!, ids[2]);
        Require(prePreview.Health == 1 && postPreview.Health == 1 && prePreview.DamageHistory?.DamageTakenBeforeArmor == 1000 &&
            postPreview.DamageHistory?.DamageTakenBeforeArmor == 1 && postPreview.Status("zeal")?.Stacks == 1031,
            "UI preview did not restore primary HP/statuses while retaining native non-primary damage caches.");
        using (var keys = new BattleStateKeys())
        {
            string original = keys.SnapshotKey(prePreview);
            Require(original != keys.SnapshotKey(prePreview.WithDamageHistory(new UnitDamageHistory(47, 999, 0, 953))) &&
                original == keys.SnapshotKey(prePreview), "Search pruning ignored or mutated damage history.");
        }
        foreach (var row in actions)
            Require(!BattleActionModel.EnumerateActions(row.GetProperty("Before").Deserialize<BattleTurnState>()!).HasUnsupportedAttempts,
                "Recorded damage-taken status battle contains an unsupported search candidate.");
        RecordedBattleChecks.Native(root);
        BattleSearchChecks.FrontierDiagnostics(root);
        BattleSearchChecks.Native(root);
        Console.WriteLine("ORIGINAL-FRIENDLY-DAMAGE-STATUS PASS: unchanged Scourged Zealots, pre-armor damage, full/partial armor, zero hit persistence, shield reset, Tenacious prevention, independent caches, native UI preview writes, keyed history, full chain/16 branches and serial/parallel search.");
    }
}
