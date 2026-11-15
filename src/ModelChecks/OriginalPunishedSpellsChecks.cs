using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class OriginalPunishedSpellsChecks
{
    private static readonly string[] Originals = {
        "0a63cd82-9aee-4991-98af-4cc4233ed77e", "0c6d7cb8-510a-4658-8cb0-4ec6752d97db",
        "4e1ada4e-1460-4ebf-a8b3-5c0706242839", "64e5b645-a0a8-4143-8448-95c4ad4559a7",
        "dd7d8a6e-459d-4ccc-ab96-efb34b79f149", "ea72d9ed-e3f6-4a4f-989f-5288a30a59ea",
        "7713877b-c121-4e79-8838-3c2b58e8856e" };
    private static void Require(bool condition, string reason) => TrueFinalBossChecks.Require(condition, reason);
    private static int Status(CombatUnit unit, string id) => unit.Status(id)?.Stacks ?? 0;
    private static IEnumerable<CombatUnit> Units(BattleTurnState state) => state.Spawn.Train.Rooms.SelectMany(room => room.Units);
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path); var root = document.RootElement;
        Require(root.GetProperty("GameVersion").GetString() == "2.3.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d",
            "Unexpected native game version or assembly.");
        Require(root.GetProperty("OriginalPunishedSpellsPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Incomplete original Punished spell battle.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero native " + key);
        var definitions = root.GetProperty("OriginalPunishedSpellsDefinitions").EnumerateArray().ToArray();
        int[] costs = { 1, 2, 1, 1, 2, 1, 2 };
        Require(definitions.Length == 7, "Missing seven original spell definitions.");
        var rules = definitions.Select(row => row.GetProperty("Rule").Deserialize<CardPlayRule>()!).ToArray();
        for (int i = 0; i < rules.Length; i++)
            Require(definitions[i].GetProperty("Id").GetString() == Originals[i] &&
                definitions[i].GetProperty("Cost").GetInt32() == costs[i] && rules[i].ExternalInteractions.Count == 0,
                "Original spell identity, cost or projection changed at " + i);
        Require(rules[0].Effects.Count == 2 && rules[0].Effects[0] is { Type: "UnitUpgrade", Target: "DropTargetCharacter",
            Lifetime: "TemporaryUntilEndOfBattle", Upgrade.Stats.Damage: 3 } &&
            rules[0].Effects[1] is { Type: "Damage", Target: "LastTargetedCharacters", Value: 2 }, "Cruel Lesson definition changed.");
        var trait = definitions[1].GetProperty("Traits").EnumerateArray().Single().Deserialize<CardSourceTrait>()!;
        Require(trait is { Type: "CardTraitScalingAddDamage", TrackedValue: "DamageDealtToMonsters", EntryDuration: "ThisBattle",
            Int: 1, Float: 1, Bool: false } && rules[1].Effects.Single() is { Type: "Damage", Value: 0 },
            "Echo of Pain must replace its damage with the live battle-wide monster damage counter.");
        Require(rules[2].Effects.Count == 2 && rules[2].Effects[0] is { Type: "Damage", Value: 5 } &&
            rules[2].Effects[1] is { Type: "Heal", Target: "LastTargetedCharacters", Value: 25, Filters.Health: "Damaged" },
            "Flagellate damage/heal sequence changed.");
        Require(rules[3].Effects.Count == 2 && rules[3].Effects.All(effect => effect is {
            Type: "Damage", Target: "Room", Value: 3, AllowEnemy: true, AllowPlayer: true }), "Collective Punishment wave definitions changed.");
        Require(rules[4].Effects.Count == 2 && rules[4].Effects[0] is { Type: "Damage", Value: 10, AllowPlayer: true, AllowEnemy: false } &&
            rules[4].Effects[1] is { Type: "AttackWithUnit", Target: "LastTargetedCharacters", RequireEquipment: false },
            "Spur the Faithful must damage the selected friendly actor before forcing its attack.");
        Require(rules[5].Effects.Count == 2 && rules[5].Effects[0].Statuses.Single() is { Id: "tenacious", Stacks: 1 } &&
            rules[5].Effects[1].Statuses.Single() is { Id: "zeal", Stacks: 15 } &&
            definitions[5].GetProperty("Traits").EnumerateArray().Single().Deserialize<CardSourceTrait>()!.Type == "CardTraitExhaustState",
            "Tome of Atonement status order or consume trait changed.");
        Require(rules[6].Effects.Single() is { Type: "AddStatus", Filters.Health: "Damaged" } &&
            rules[6].Effects[0].Statuses.Single() is { Id: "lifesteal", Stacks: 4 }, "Unholy Communion health filter/status changed.");
        int[] ids = root.GetProperty("OriginalPunishedSpellsIds").Deserialize<int[]>()!;
        var actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 29 && ids.Distinct().Count() == 29 && actions.Length == 29 &&
            actions.Select(row => row.GetProperty("Action").Deserialize<PlayCardAction>()!.CardInstanceId).SequenceEqual(ids),
            "Missing 29 ordered independent native inputs.");
        BattleTurnState Before(int i) => actions[i].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int i) => actions[i].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatUnit Unit(BattleTurnState state, int card) => Units(state).Single(unit => unit.SpawnerCardId == ids[card]);
        CombatUnit Enemy(BattleTurnState state, int floor = 0) => state.Spawn.Train.Rooms[floor].Units.First(unit => unit.Team == CombatTeam.Enemy);
        int DamageCounter(BattleTurnState state) => state.Spawn.Train.Context!.Statistics!.MonsterDamage!.ThisBattle;
        int[] originalIndices = { -1, -1, -1, -1, -1, 1, 0, 2, 0, 3, 1, 6, 4, 1, 5, -1, 2, -1, 3, 1, 4, 5, 2, 0, 2, -1, 2, -1, 1 };
        for (int i = 0; i < ids.Length; i++)
            if (originalIndices[i] >= 0)
                Require(Before(i).Spawn.Train.Context!.FindCard(ids[i])!.DataId == Originals[originalIndices[i]] &&
                    Before(i).Energy - After(i).Energy == costs[originalIndices[i]], "Original cast identity or cost changed at " + i);
        foreach (var (i, card) in new[] { (6, 2), (23, 4) })
            Require(Unit(After(i), card).Attack == Unit(Before(i), card).Attack + 3 &&
                Unit(Before(i), card).Health - Unit(After(i), card).Health == 2,
                "Cruel Lesson must add three attack before dealing two damage at " + i);
        Require(Enemy(After(8)).Attack == Enemy(Before(8)).Attack + 3 &&
            Status(Enemy(Before(8)), "armor") - Status(Enemy(After(8)), "armor") == 2,
            "Cruel Lesson enemy/armor path differs.");
        foreach (var (i, card) in new[] { (7, 2), (16, 2), (24, 4) })
        {
            var old = Unit(Before(i), card); var next = Unit(After(i), card);
            Require(next.Health == Math.Min(old.MaxHealth, old.Health - 5 + 25), "Flagellate friendly heal or cap differs at " + i);
        }
        Require(Enemy(After(22)).Health == Math.Min(Enemy(Before(22)).MaxHealth, Enemy(Before(22)).Health - 5 + 25),
            "Flagellate enemy healing differs.");
        Require(Unit(Before(26), 25).Health == 4 && !Units(After(26)).Any(unit => unit.SpawnerCardId == ids[25]),
            "Flagellate must not revive its four-health lethal target.");
        Require(Unit(After(9), 2).Health == Unit(Before(9), 2).Health - 6 &&
            !Units(After(9)).Any(unit => unit.SpawnerCardId == ids[3]) && Unit(After(9), 4).Health == Unit(Before(9), 4).Health,
            "Two room waves must kill the four-health target and leave the remote floor untouched.");
        Require(Unit(After(18), 2).Health == Unit(Before(18), 2).Health &&
            Status(Unit(Before(18), 2), "armor") == 8 && Status(Unit(After(18), 2), "armor") == 2 &&
            DamageCounter(After(18)) - DamageCounter(Before(18)) == 6,
            "Armor-absorbed hits must still increment the native global damage statistic.");
        foreach (var (i, expected) in new[] { (5, 0), (10, 17), (13, 29), (19, 40), (28, 61) })
        {
            var old = Enemy(Before(i)); var next = Enemy(After(i));
            Require(DamageCounter(Before(i)) == expected && old.Health + Status(old, "armor") - next.Health - Status(next, "armor") == expected,
                "Echo must read the current global counter including armor and capped lethal HP at " + i);
        }
        Require(Status(Unit(After(11), 2), "lifesteal") == 4 && Status(Unit(After(12), 2), "lifesteal") == 3,
            "Unholy Communion or forced attack lifesteal consumption differs.");
        var oldEnemy = Enemy(Before(12)); var nextEnemy = Enemy(After(12));
        Require(oldEnemy.Health + Status(oldEnemy, "armor") - nextEnemy.Health - Status(nextEnemy, "armor") == Unit(Before(12), 2).Attack &&
            DamageCounter(After(12)) - DamageCounter(Before(12)) == 12,
            "Spur must force the selected actor's attack, retaining self damage and the native spikes reaction.");
        Require(!Before(20).Spawn.Train.Rooms[1].Units.Any(unit => unit.Team == CombatTeam.Enemy) &&
            Unit(Before(20), 4).Health - Unit(After(20), 4).Health == 10 &&
            DamageCounter(After(20)) - DamageCounter(Before(20)) == 10,
            "Spur without an opposing actor must retain only its ten self damage, with no phantom attack or spikes.");
        foreach (var (i, card) in new[] { (14, 2), (21, 4) })
        {
            var old = Unit(Before(i), card); var next = Unit(After(i), card);
            Require(Status(next, "tenacious") == 1 && Status(next, "zeal") == 15 && next.MaxHealth == old.MaxHealth + 15 &&
                next.Health == old.Health && After(i).OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(token => token.InstanceId == ids[i]),
                "Tome must add Tenacious then zeal without healing and consume its source card.");
        }
        Require(DamageCounter(Before(27)) == DamageCounter(After(27)), "Zero damage changed the global counter.");
        int notifications = 0, callbacks = 0; var echoIds = new[] { 5, 10, 13, 19, 28 }.Select(i => ids[i]).OrderBy(id => id).ToArray();
        foreach (var row in root.GetProperty("MonsterDamageTraitNotifications").EnumerateArray())
        {
            Require(row.GetProperty("Completed").GetBoolean(), "Incomplete native monster damage notification.");
            var before = row.GetProperty("Before").Deserialize<BattleStatistics>()!;
            var actual = row.GetProperty("Actual").Deserialize<BattleStatistics>()!;
            int amount = row.GetProperty("Amount").GetInt32(), friendly = row.GetProperty("FriendlyAmount").GetInt32();
            Require(actual.MonsterDamage!.ThisBattle == unchecked(before.MonsterDamage!.ThisBattle + Math.Max(0, amount)) &&
                actual.MonsterDamage.ThisTurn == unchecked(before.MonsterDamage.ThisTurn + Math.Max(0, amount)) &&
                actual.MonsterDamage.FriendlyThisBattle == unchecked(before.MonsterDamage.FriendlyThisBattle + Math.Max(0, friendly)) &&
                actual.MonsterDamage.FriendlyThisTurn == unchecked(before.MonsterDamage.FriendlyThisTurn + Math.Max(0, friendly)),
                "Direct native counter notification changed more than its requested positive deltas.");
            var updates = row.GetProperty("Callbacks").EnumerateArray().ToArray();
            Require(updates.Select(update => update.GetProperty("CardId").GetInt32()).OrderBy(id => id)
                .SequenceEqual(amount > 0 ? echoIds : Array.Empty<int>()), "Native damage callback missed an owned Echo or double-counted its friendly channel.");
            foreach (var update in updates)
            {
                Require(update.GetProperty("TrackedValue").GetString() == "DamageDealtToMonsters" &&
                    update.GetProperty("Before").ContentEquals(update.GetProperty("Actual")),
                    "Native scaling-damage UI refresh mutated gameplay card state.");
                callbacks++;
            }
            notifications++;
        }
        Require(notifications > 10 && callbacks > 50, "Insufficient native counter callback evidence across hand and discard.");
        var samples = root.GetProperty("DamageScaling").EnumerateArray().ToArray();
        Require(samples.Length == 5 && samples.Select(sample => sample.GetProperty("ActionIndex").GetInt32())
            .SequenceEqual(new[] { 5, 10, 13, 19, 28 }) && samples.All(sample =>
            sample.GetProperty("OwnerCardId").GetInt32() == ids[sample.GetProperty("ActionIndex").GetInt32()] &&
            sample.GetProperty("Before").ContentEquals(sample.GetProperty("After")) &&
            sample.GetProperty("ActualDamage").GetInt32() ==
                sample.GetProperty("Before").Deserialize<CombatContext>()!.Statistics!.MonsterDamage!.ThisBattle),
            "Direct native Echo damage traits did not read their current battle counter.");
        RoomCombatChecks.NativeRoomOnly(path); TrainCombatChecks.Native(root); RecordedBattleChecks.Native(root);
        BattleSearchChecks.Native(root, requireZeroUnsupported: true);
        Console.WriteLine($"ORIGINAL-PUNISHED-SPELLS PASS: seven unchanged spells/20 original casts, zero/17/29/40/61 live Echo damage, lethal/heal/armor/last-target/upgrades/forced attacks, {notifications} native statistic notifications/{callbacks} immutable scaling callbacks, full chain/16 branches and zero-unsupported serial/parallel search.");
    }
}
