using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class OriginalSpiteEquipmentChecks
{
    private const string Shackles = "a1375ed2-179f-4ede-9233-b88b604836ac";
    private const string Sineater = "ad143b21-be42-43a5-84bb-780ab697f7c1";
    private const string Ability = "489a5c34-b17d-47ed-a1a5-b07fe1b5b370";
    private static void Require(bool condition, string reason) => TrueFinalBossChecks.Require(condition, reason);
    private static int Status(CombatUnit unit, string id) => unit.Status(id)?.Stacks ?? 0;
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path); var root = document.RootElement;
        Require(root.GetProperty("GameVersion").GetString() == "2.3.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            root.GetProperty("OriginalSpiteEquipmentPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Incomplete original Spite equipment native battle.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero native " + key);
        var definitions = root.GetProperty("OriginalSpiteEquipmentDefinitions").EnumerateArray().ToArray();
        string[] originals = { Shackles, Sineater, Ability }; int[] costs = { 1, 3, 0 };
        Require(definitions.Length == 3, "Missing two original equipment cards and their original skill.");
        var rules = definitions.Select(row => row.GetProperty("Rule").Deserialize<CardPlayRule>()!).ToArray();
        for (int i = 0; i < 3; i++) Require(definitions[i].GetProperty("Id").GetString() == originals[i] &&
            definitions[i].GetProperty("Cost").GetInt32() == costs[i] && rules[i].ExternalInteractions.Count == 0,
            "Original identity/cost/projection changed at " + i);
        var shackles = rules[0].Equipment!.Upgrades.Single(); var sineater = rules[1].Equipment!.Upgrades.Single();
        Require(rules[0].Effects.Single().Type == "AttachEquipment" && shackles.Stats.Health == 10 &&
            shackles.Statuses.Single() is { Id: "spite", Stacks: 1, Stackable: false } &&
            sineater.Stats.Damage == 10 && sineater.AbilityUpgrade!.Definition!.DataId == Ability &&
            sineater.AbilityUpgrade.Definition.Cooldown == 2 && rules[2].Effects.Count == 2 &&
            rules[2].Effects[0] is { Type: "Damage", Target: "Self", Value: 8 } &&
            rules[2].Effects[1] is { Type: "AttackWithUnit", Target: "Self", RequireEquipment: true },
            "Original equipment stats, nonstackable Spite or damage-then-attack ability changed.");
        var status = root.GetProperty("OriginalSpiteDefinition");
        Require(status.GetProperty("Type").GetString() == "StatusEffectSpiteState" &&
            status.GetProperty("Stage").GetString() == "OnPreAttacking" &&
            status.GetProperty("Rule").Deserialize<CombatStatus>() is { Id: "spite", Hidden: true, Stackable: false },
            "Native Spite stage or status metadata changed.");
        int[] ids = root.GetProperty("OriginalSpiteEquipmentIds").Deserialize<int[]>()!;
        var actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        int[] skillIndices = { 7, 10, 13, 17, 21 };
        Require(ids.Length == 22 && ids.Count(id => id == 0) == 5 && ids.Where(id => id > 0).Distinct().Count() == 17 &&
            actions.Length == 22, "Missing 22 native inputs/five skill uses.");
        BattleTurnState Before(int i) => actions[i].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int i) => actions[i].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatUnit Host(BattleTurnState state, int index = 1) => state.Spawn.Train.Rooms.SelectMany(room => room.Units)
            .Single(unit => unit.SpawnerCardId == ids[index]);
        CombatUnit Guard(BattleTurnState state) => state.Spawn.Train.Rooms.SelectMany(room => room.Units)
            .Single(unit => unit.AssetKey == "ShieldGuardT1_BasicNonAttack");
        for (int i = 0; i < 22; i++)
        {
            var action = actions[i].GetProperty("Action").Deserialize<PlayCardAction>()!;
            if (skillIndices.Contains(i)) Require(action.ActivatorUnitId == Host(Before(i), i == 21 ? 2 : 1).Id &&
                Before(i).Spawn.Train.Context!.FindCard(action.CardInstanceId)?.DataId == Ability,
                "Original skill source or cache identity differs at " + i);
            else Require(action.CardInstanceId == ids[i] && action.ActivatorUnitId == 0, "Control play order differs at " + i);
        }
        foreach (var (i, original) in new[] { (4, 0), (5, 1), (14, 0), (19, 1) })
            Require(Before(i).Spawn.Train.Context!.FindCard(ids[i])!.DataId == originals[original] &&
                Before(i).Energy - After(i).Energy == costs[original], "Original equipment cast cost/identity differs.");
        int[] expectedHp = { 34, 33, 50, 38 }, expectedDamage = { 20, 40, 20, 40 };
        foreach (int i in skillIndices.Take(4))
        {
            int ordinal = Array.IndexOf(skillIndices, i); var old = Host(Before(i)); var next = Host(After(i));
            var enemy = Guard(Before(i)); var changed = Guard(After(i));
            Require(old.Health == expectedHp[ordinal] && old.Attack == 20 && Status(old, "spite") == 1 &&
                next.Health == old.Health - 8 - 2 && Status(next, "cooldown") == 2 &&
                enemy.Health + Status(enemy, "armor") - changed.Health - Status(changed, "armor") == expectedDamage[ordinal],
                "Sineater must test current HP after eight self damage, before spikes, at " + i);
        }
        Require(Host(After(14)).MaxHealth == 60 && Status(Host(After(14)), "spite") == 1 &&
            Host(After(14)).EquipmentCards!.Count == 3, "Two Shackles must add both HP upgrades while Spite remains nonstackable.");
        Require(Before(18).Spawn.Turn == 2 && Host(Before(18)).MaxHealth == 60 && Host(Before(18)).Health <= 30 &&
            Status(Host(Before(18)), "cooldown") == 1, "Missing natural Spite combat/cooldown progression.");
        var stripped = Host(After(18));
        Require(stripped.Attack == 10 && stripped.MaxHealth == 40 && Status(stripped, "spite") == 0 &&
            stripped.EquipmentCards!.Count == 0 && stripped.Ability?.HasAbility != true,
            "Removing the complete equipment set must restore base stats and remove Spite and the skill.");
        Require(Host(Before(21), 2).Health == 8 && !After(21).Spawn.Train.Rooms.SelectMany(room => room.Units)
            .Any(unit => unit.SpawnerCardId == ids[2]) && Before(21).Spawn.Train.Rooms[0].Units.Any(unit => unit.Team == CombatTeam.Enemy),
            "The second Sineater host must die from its eight self damage with living enemies present.");
        Require(Before(21).Spawn.Train.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Enemy)
            .Select(unit => (unit.Id, unit.Health, Armor: Status(unit, "armor"))).SequenceEqual(
            After(21).Spawn.Train.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Enemy)
                .Select(unit => (unit.Id, unit.Health, Armor: Status(unit, "armor")))),
            "A lethal self hit incorrectly forced an attack against a living enemy.");
        var calibration = root.GetProperty("SpiteStatusCalibration");
        Require(calibration.GetProperty("Completed").GetBoolean() && calibration.GetProperty("LiveContextUnchanged").GetBoolean() &&
            calibration.GetProperty("Before").ContentEquals(calibration.GetProperty("Actual")) &&
            calibration.GetProperty("RngBefore").ContentEquals(calibration.GetProperty("RngAfter")), "Native direct calibration leaked state or RNG.");
        var direct = calibration.GetProperty("Tests").EnumerateArray().ToArray();
        var live = root.GetProperty("SpiteStatusTests").EnumerateArray().ToArray();
        Require(direct.Length == 123 && live.Length > 0 && live.Any(row => !row.GetProperty("Preview").GetBoolean() && row.GetProperty("NativeTest").GetBoolean()) &&
            live.Any(row => !row.GetProperty("NativeTest").GetBoolean()), "Insufficient positive/control native Spite evidence.");
        void Check(MonsterTrain2Poju.Fixtures.FixtureValue row)
        {
            int damage = row.GetProperty("Damage").GetInt32();
            int actual = SpiteStatusModel.Adjust(row.GetProperty("Attacker").Deserialize<CombatTeam?>(), row.GetProperty("Health").GetInt32(),
                row.GetProperty("Maximum").GetInt32(), row.GetProperty("Stacks").GetInt32(), row.GetProperty("Target").Deserialize<CombatTeam?>(),
                row.GetProperty("TargetAlive").GetBoolean(), damage);
            Require(actual == row.GetProperty("ActualDamage").GetInt32() && (actual != damage) == row.GetProperty("NativeTest").GetBoolean(),
                "Independent Spite query differs from native.");
        }
        foreach (var row in direct.Concat(live)) Check(row);
        Parallel.For(0, 32, _ => { foreach (var row in direct) Check(row); });
        RoomCombatChecks.NativeRoomOnly(path); TrainCombatChecks.Native(root); RecordedBattleChecks.Native(root);
        BattleSearchChecks.Native(root, requireZeroUnsupported: true);
        Console.WriteLine($"ORIGINAL-SPITE-EQUIPMENT PASS: two unchanged originals/five skill activations, current half-health after self damage, 20/40/20/40 attack damage, nonstackable duplicate HP equipment, natural combat/removal/lethal self hit, {direct.Length} direct/{live.Length} live Spite tests, 32 query branches, full chain/16 branches and zero-unsupported serial/parallel search.");
    }
}
