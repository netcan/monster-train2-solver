using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RoomHealthCardHoverChecks
{
    private static void Require(bool value, string reason) => TrueFinalBossChecks.Require(value, reason);
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path); var root = document.RootElement;
        Require(root.GetProperty("Schema").GetInt32() == 1 && root.GetProperty("Boundary").GetString() == "NativeOriginalRoomHealthCardHover" &&
            root.GetProperty("GameVersion").GetString() == "2.3.1" && root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            root.GetProperty("NativeCompleted").GetBoolean() && root.GetProperty("Definitions").ContentEquals(root.GetProperty("DefinitionsAfter")) &&
            root.GetProperty("OriginalSuppression").GetInt32() == root.GetProperty("FinalSuppression").GetInt32(),
            "Wrong native version/boundary or un-restored original definitions/suppression.");
        var upgrades = root.GetProperty("Definitions").GetProperty("Upgrades").EnumerateArray()
            .Select(item => item.GetProperty("Rule").Deserialize<CardUpgradeModifier>()!).ToArray();
        Require(root.GetProperty("Definitions").GetProperty("Champion").GetProperty("DataId").GetString() == "c58f8d77-8578-4d97-bcf1-beb4cdbe2f27" &&
            upgrades.Select(upgrade => upgrade.DataId).SequenceEqual(new[] { "da5a3d33-19ee-404c-90c7-1810b08137f8", "fe4cf3be-2c0f-44ad-bdd2-f0418c14e4e0", "93f6e1ca-fc6e-4e23-ab59-207a397c42a0" }) &&
            upgrades.Length == 3 && upgrades.SelectMany(upgrade => upgrade.SelfAttackRules!).Select(rule => rule.Coefficient)
            .SequenceEqual(new[] { 1, 2, 3 }), "Missing unchanged original aura ranks.");
        var rows = root.GetProperty("Samples").EnumerateArray().ToArray();
        Require(rows.Length == 49 && rows.Select(row => row.GetProperty("Label").GetString()).Distinct().Count() == 49, "Incomplete generated hover matrix.");
        var cases = new[] { ("CardEffectDamage", 3), ("CardEffectDamage", 9999), ("CardEffectHeal", 3), ("CardEffectHeal", 9999),
            ("CardEffectDebuffMaxHealth", 3), ("CardEffectDebuffMaxHealth", 9999), ("CardEffectBuffMaxHealth", 3) };
        var first = rows[0].GetProperty("Before").Deserialize<TrainCombatState>()!;
        var targets = first.Rooms.Single(room => room.RoomIndex == 1).Units.Where(unit => unit.Team == CombatTeam.Player).Select(unit => unit.Id).ToArray();
        Require(targets.Length == 5, "Missing original three-source/two-recipient roster.");
        var expected = targets.SelectMany(id => cases.Select(input => (input.Item1, input.Item2, 1, id)))
            .Concat(new[] { 1, 2 }.SelectMany(floor => cases.Select(input => (input.Item1, input.Item2, floor, 0)))).ToHashSet();
        var observed = rows.Select(row => (row.GetProperty("NativeType").GetString()!, row.GetProperty("Amount").GetInt32(),
            row.GetProperty("RoomIndex").GetInt32(), row.GetProperty("DropTargetId").GetInt32())).ToArray();
        Require(observed.Distinct().Count() == rows.Length && expected.SetEquals(observed), "Missing or unexpected native input branch.");
        int queries = 0, positiveRecipients = 0, lethalSources = 0, lethalRooms = 0, damageDeltas = 0, fullHealRooms = 0;
        foreach (var row in rows)
        {
            string label = row.GetProperty("Label").GetString()!;
            Require(row.GetProperty("NativeReturned").GetBoolean() && row.GetProperty("QueueEmpty").GetBoolean() &&
                row.GetProperty("TemporaryDisabled").GetBoolean() && row.GetProperty("EnableCount").GetInt32() == 1 &&
                row.GetProperty("DisableCount").GetInt32() == 1, "Incomplete native temporary-copy lifecycle: " + label);
            Require(!row.GetProperty("Dispatches").EnumerateArray().Any(dispatch => dispatch.GetProperty("Kind").GetString() is
                "OnOwnAbilityActivated" or "OnUnitAbilityAvailable" or "OnUnitAbilityUnavailable"), "An assumed-inactive ability callback fired: " + label);
            CardHoverFixtureChecks.Match(row);
            foreach (var stage in new[] { ("Before", "BeforeAttacks"), ("CopiedBefore", "CopiedAttacks"), ("CopiedActual", "ActualAttacks") })
            {
                var state = row.GetProperty(stage.Item1).Deserialize<TrainCombatState>()!;
                var native = row.GetProperty(stage.Item2).EnumerateArray().ToArray();
                Require(native.Length == state.Rooms.SelectMany(room => room.Units).Count(unit => unit.Health > 0), "Missing living native attack witnesses: " + label);
                foreach (var query in native)
                {
                    int id = query.GetProperty("UnitId").GetInt32(), actual = query.GetProperty("Attack").GetInt32();
                    var room = state.Rooms.Single(value => value.Units.Any(unit => unit.Id == id));
                    var unit = room.Units.Single(unit => unit.Id == id);
                    Require(RelicResourceModel.AttackDamage(unit, state.Context, room) == actual, "Native aura attack differs at " + stage.Item1 + ": " + label + "/" + id);
                    queries++; if (!RoomHealthAttackModel.HasSource(unit) && actual > unit.Attack) positiveRecipients++;
                }
            }
            var before = row.GetProperty("Before").Deserialize<TrainCombatState>()!;
            var after = row.GetProperty("CopiedActual").Deserialize<TrainCombatState>()!;
            int floor = row.GetProperty("RoomIndex").GetInt32(), targetId = row.GetProperty("TargetId").GetInt32();
            string type = row.GetProperty("NativeType").GetString()!; int amount = row.GetProperty("Amount").GetInt32();
            bool targeted = row.GetProperty("DropTargetId").GetInt32() != 0;
            var target = before.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == targetId);
            int Sources(TrainCombatState state) => state.Rooms.Single(room => room.RoomIndex == 1).Units.Count(unit => unit.Health > 0 && RoomHealthAttackModel.HasSource(unit));
            int Attack(string key, int id) => row.GetProperty(key).EnumerateArray().Single(query => query.GetProperty("UnitId").GetInt32() == id).GetProperty("Attack").GetInt32();
            Require(Sources(before) == 3, "Previous hover failed to restore all original sources.");
            if (floor == 1 && targeted && type == "CardEffectDamage" && amount == 3)
            {
                Require(Attack("ActualAttacks", targetId) == Attack("BeforeAttacks", targetId) + 18,
                    "Missing native +18 attack witness from three extra missing HP and coefficient sum six."); damageDeltas++;
            }
            if (floor == 1 && targeted && RoomHealthAttackModel.HasSource(target) && amount == 9999 && type is "CardEffectDamage" or "CardEffectDebuffMaxHealth")
            { Require(Sources(after) == 2, "A preview-dead source still affects its room."); lethalSources++; }
            if (floor == 1 && !targeted && amount == 9999 && type is "CardEffectDamage" or "CardEffectDebuffMaxHealth")
            { Require(Sources(after) == 0, "Lethal room preview retained an aura source."); lethalRooms++; }
            if (floor == 1 && !targeted && type == "CardEffectHeal" && amount == 9999)
            {
                Require(after.Rooms.Single(room => room.RoomIndex == 1).Units.All(unit => unit.Health == unit.MaxHealth &&
                    Attack("ActualAttacks", unit.Id) == unit.Attack), "Full room healing retained missing-health attack."); fullHealRooms++;
            }
        }
        Require(positiveRecipients > 0 && damageDeltas == 5 && lethalSources == 6 && lethalRooms == 2 && fullHealRooms == 1,
            "Missing original-source, additive, healing or lethal native branches.");
        Console.WriteLine($"ROOM-HEALTH-CARD-HOVER PASS: 49 native previews, {queries} exact native attack queries ({positiveRecipients} positive recipient queries), five +18 damage deltas, six source deaths, two lethal rooms, full healing, all copied/restored planes, original definitions/RNG/queue/suppression and 16 immutable evaluations per sample.");
    }
}
