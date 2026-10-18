using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class ArmamentDriveChecks
{
    private const string DataId = "954f3138-3e3a-4369-9d4b-46adb7b1585c";
    private const string EquipmentId = "b776421c-915c-4c3d-87dc-0fe1a9e8584c";
    private static string UnitId(int index) => "f15d0002-0000-4000-8000-00000000000" + index;
    internal static PlayCardAction? Choose(BattleTurnState source, int[] cards)
    {
        CombatContext context = source.Spawn.Train.Context!;
        var plays = BattleActionModel.EnumerateSupportedPlays(source);
        bool Played(int index) => context.Statistics!.Value(cards[index], "TimesPlayed", "ThisBattle") > 0;
        PlayCardAction? Cast(int index, int room) => plays.FirstOrDefault(play => play.CardInstanceId == cards[index] && play.RoomIndex == room);
        if (!Played(0)) return Cast(0, 2);
        foreach (int index in new[] { 1, 2 })
        {
            var summon = plays.FirstOrDefault(play => play.RoomIndex == 0 && context.FindCard(play.CardInstanceId)!.DataId == UnitId(index));
            if (summon != null) return summon;
        }
        if (!Played(1)) return Cast(1, 0);
        CombatUnit[] units = source.Spawn.Train.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Player &&
            context.FindCard(unit.SpawnerCardId)?.DataId is string id && (id == UnitId(1) || id == UnitId(2))).ToArray();
        int equipped = units.Count(unit => unit.EquipmentCards!.Count > 0);
        if (equipped == 1 && !Played(2)) return Cast(2, 0);
        if (equipped < 2)
            return plays.FirstOrDefault(play => context.FindCard(play.CardInstanceId)!.DataId == EquipmentId &&
                units.Any(unit => unit.Id == play.TargetUnitId && unit.EquipmentCards!.Count == 0));
        if (!Played(3)) return Cast(3, 0);
        return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("ArmamentDrivePrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed Armament Drive witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        var counts = new List<int>(); var targetCounts = new List<int>(); int upgraded = 0;
        foreach (FixtureValue row in root.GetProperty("CardGenerations").EnumerateArray())
        {
            var before = row.GetProperty("Before").Deserialize<CombatContext>()!;
            var after = row.GetProperty("Actual").Deserialize<CombatContext>()!;
            var rule = row.GetProperty("Rule").Deserialize<CardGenerationRule>()!;
            var targets = row.GetProperty("Targets").Deserialize<CombatUnit[]>()!;
            int source = row.GetProperty("SourceCardId").GetInt32();
            Require(before.FindCard(source)!.DataId == DataId && rule.OnlyUnequippedTargets == true &&
                rule.Count == 1 && rule.Destination == "HandPile" && !rule.SkipDuplicateInHand && rule.Upgrade != null,
                "Original Armament Drive parameters changed.");
            Require(rule.Upgrade!.Stats.Damage == 15 && rule.Upgrade.Stats.Health == 15,
                "Original generated equipment upgrade is not +15 attack/+15 HP.");
            int eligible = targets.Count(unit => unit.EquipmentCards!.Count == 0);
            counts.Add(eligible); targetCounts.Add(targets.Length);
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            void Replay()
            {
                var result = CardGenerationModel.ApplyForTargets(before, rule, targets, source);
                Require(result.Supported, result.UnsupportedReason ?? "Per-target generation unsupported.");
                Equal(result.Context, after, "per-target native full context/RNG");
            }
            Replay(); Parallel.For(0, 16, _ => Replay());
            Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Per-target generation mutated its parent.");
            Require(after.NextCardId - before.NextCardId == eligible, "Equipped or absent units contributed to generation.");
            if (eligible == 0) Equal(before, after, "No eligible targets must preserve identities, RNG and pending upgrades");
            for (int id = before.NextCardId; id < after.NextCardId; id++)
            {
                var generated = after.FindCard(id)!;
                Require(generated.DataId == EquipmentId && generated.Temporary.Upgrades.Any(upgrade => upgrade.DataId == rule.Upgrade!.DataId),
                    "Generated equipment lost its original definition or optional temporary upgrade.");
                upgraded++;
            }
            Require(!CardGenerationModel.Apply(before, rule, source).Supported, "Per-target generation accepted missing target metadata.");
            if (targets.Length > 0)
                Require(!CardGenerationModel.ApplyForTargets(before, rule, [new CombatUnit(999, "unknown-equipment", CombatTeam.Player,
                    1, 1, 1, true, false, false, [], [])], source).Supported, "Unknown equipment membership silently counted as unequipped.");
        }
        Require(counts.SequenceEqual([0, 2, 1, 0]) && targetCounts.SequenceEqual([0, 2, 2, 2]) && upgraded == 3,
            "Missing empty-room, two-unequipped, one-equipped or all-equipped generation controls.");
        int originals = root.GetProperty("Actions").EnumerateArray().Count(row =>
        {
            var before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            int id = row.GetProperty("Action").GetProperty("CardInstanceId").GetInt32();
            return before.Spawn.Train.Context!.FindCard(id)!.DataId == DataId;
        });
        Require(originals == 4, "Missing four successful unchanged original casts.");
        int equipmentPlays = root.GetProperty("Actions").EnumerateArray().Count(row =>
        {
            var before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            int id = row.GetProperty("Action").GetProperty("CardInstanceId").GetInt32();
            return before.Spawn.Train.Context!.FindCard(id)!.DataId == EquipmentId;
        });
        Require(equipmentPlays >= 2, "Generated equipment did not reach two live hosts between casts.");
        RecordedBattleChecks.Native(root);
        Console.WriteLine("ARMAMENT-DRIVE PASS: four original casts, zero/two/one/zero eligible targets, three upgraded original equipment cards, live equipment membership, full state/RNG and 16 immutable branches.");
    }
    private static void Equal<T>(T predicted, T actual, string label)
    {
        string? difference = ModelJson.Difference(JsonSerializer.Serialize(predicted, ModelJson.Options), JsonSerializer.Serialize(actual, ModelJson.Options));
        Require(difference == null, label + ": " + difference);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
