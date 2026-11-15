using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class OriginalPunishedUnitsChecks
{
    private static readonly (string Id, int Cost, int Attack, int Health, int Size)[] Originals = {
        ("ce6394f3-73b6-4fca-a9c4-b9999cb3f973", 2, 10, 45, 3),
        ("5b16e14e-69e0-4500-985c-c3d62ef2d900", 1, 0, 10, 1),
        ("f16b30a4-88f1-4da6-8f4d-87fcb9f86659", 2, 5, 15, 1),
        ("15100a7a-6288-4b07-a726-2d6be7ec6e33", 0, 9, 9, 1),
        ("a824b80b-4fac-4b20-9d2a-1790804e1232", 1, 8, 8, 1),
        ("cb3c2f95-c556-47f7-86c3-86c773e9510e", 1, 30, 20, 2) };
    private static void Require(bool condition, string reason) => TrueFinalBossChecks.Require(condition, reason);

    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path); var root = document.RootElement;
        Require(root.GetProperty("OriginalPunishedUnitsPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Incomplete original Punished-unit battle.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero native " + key);
        var definitions = root.GetProperty("OriginalPunishedUnitsDefinitions").EnumerateArray().ToArray();
        Require(definitions.Length == 6, "Missing six original definitions.");
        for (int i = 0; i < Originals.Length; i++)
        {
            var expected = Originals[i]; var raw = definitions[i]; var rule = raw.GetProperty("Rule").Deserialize<CardPlayRule>()!;
            Require(raw.GetProperty("Id").GetString() == expected.Id && raw.GetProperty("Cost").GetInt32() == expected.Cost &&
                raw.GetProperty("Traits").GetArrayLength() == 0 && rule.ExternalInteractions.Count == 0 &&
                rule.SpawnUnit is CombatUnit unit && unit.BaseAttack == expected.Attack && unit.Health == expected.Health &&
                unit.MaxHealth == expected.Health && unit.Size == expected.Size,
                "Original cost, traits, stats or definition changed at " + i);
        }
        var retaliation = definitions[1].GetProperty("Rule").Deserialize<CardPlayRule>()!.SpawnUnit!.Triggers.Single().Effects.Single();
        Require(retaliation.Type == OwnerDamageReactionModel.NativeEffect && retaliation.Value == 4 &&
            retaliation.Action is { Type: "Damage", Target: "FrontInRoom", Value: 4, AllowEnemy: true, AllowPlayer: false, Range: null },
            "Original pre-armor Punish retaliation signature changed.");
        int[] ids = root.GetProperty("OriginalPunishedUnitsIds").Deserialize<int[]>()!;
        var actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 25 && ids.Distinct().Count() == 25 && actions.Length == 25 &&
            actions.Select(row => row.GetProperty("Action").Deserialize<PlayCardAction>()!.CardInstanceId).SequenceEqual(ids),
            "Missing ordered original summons and controls.");
        BattleTurnState Before(int index) => actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int index) => actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatUnit Unit(BattleTurnState state, int card) => state.Spawn.Train.Rooms.SelectMany(room => room.Units)
            .Single(unit => unit.SpawnerCardId == ids[card]);
        CombatUnit Enemy(BattleTurnState state) => state.Spawn.Train.Rooms[0].Units.Single(unit => unit.AssetKey == "ShieldGuardT1_BasicNonAttack");
        for (int i = 3; i <= 8; i++)
        {
            Require(Before(i).Spawn.Train.Context!.FindCard(ids[i])!.DataId == Originals[i - 3].Id &&
                Before(i).Energy - After(i).Energy == Originals[i - 3].Cost,
                "Summon changed original identity or cost at " + i);
        }
        Require(Unit(After(9), 3).MaxHealth == Unit(Before(9), 3).MaxHealth + 6 &&
            After(9).Spawn.Train.Context!.FindCard(ids[3])!.Permanent.Upgrades.Any(upgrade => upgrade.Stats.Health == 1) &&
            Unit(After(10), 3).Health == Unit(Before(10), 3).Health,
            "Permanent +1 HP, simultaneous Bloodshed +5 zeal, source-card writeback or heal immunity differs.");
        Require(Unit(After(12), 8).Attack == 45 && Unit(After(12), 8).Health == 20 &&
            Unit(After(13), 8).Attack == 45 && Unit(After(14), 8).Attack == 60 && Unit(After(14), 8).Health == 17,
            "Fully armored Punish, zero exclusion or repeated temporary +15 attack differs.");
        foreach (var item in new[] { (15, 3), (17, 5) })
        {
            var before = Enemy(Before(item.Item1)); var after = Enemy(After(item.Item1));
            Require(before.Health + (before.Status("armor")?.Stacks ?? 0) - after.Health - (after.Status("armor")?.Stacks ?? 0) == item.Item2 * 4 &&
                Unit(After(item.Item1), 4).DamageHistory?.DamageTakenBeforeArmor == 1,
                "Punish must use the exact pre-armor damage at " + item.Item1);
        }
        foreach (int index in new[] { 19, 20 })
            Require(Unit(After(index), 4).DamageHistory?.DamageTakenBeforeArmor == 1 &&
                Enemy(Before(index)).Health == Enemy(After(index)).Health &&
                (Enemy(Before(index)).Status("armor")?.Stacks ?? 0) == (Enemy(After(index)).Status("armor")?.Stacks ?? 0),
                "Shield/zero control caused retaliation or lost the later UI-preview cache write at " + index);
        var ownerDamage = root.GetProperty("TriggeredDamage").EnumerateArray().Where(row =>
            row.TryGetProperty("OwnerDamageBeforeArmor", out var amount) && amount.ValueKind != MonsterTrain2Poju.Fixtures.FixtureKind.Null).ToArray();
        var ownerApplications = ownerDamage.Where(row => row.GetProperty("Stage").GetString() == "Application").ToArray();
        Require(ownerApplications.Any(row => row.GetProperty("OwnerDamageBeforeArmor").GetInt32() == 3) &&
            ownerApplications.Any(row => row.GetProperty("OwnerDamageBeforeArmor").GetInt32() == 5) &&
            ownerDamage.Any(row => row.GetProperty("Stage").GetString() == "Test" &&
                row.GetProperty("OwnerDamageBeforeArmor").GetInt32() == 0 && !row.GetProperty("TestPassed").GetBoolean()),
            "Missing direct, armor-absorbed or shield-cleared native owner-damage boundary.");
        foreach (var row in ownerApplications)
        {
            int damage = row.GetProperty("OwnerDamageBeforeArmor").GetInt32();
            var requests = row.GetProperty("Requests").EnumerateArray()
                .Where(request => request.GetProperty("AttackerPreserved").GetBoolean()).ToArray();
            Require(row.GetProperty("Completed").GetBoolean() && row.GetProperty("Sampled").GetBoolean() &&
                row.GetProperty("Amount").GetInt32() == 4 && row.GetProperty("Targets").GetArrayLength() == 1 &&
                requests.Length == 1 && requests[0].GetProperty("Amount").GetInt32() == unchecked(damage * 4) &&
                requests[0].GetProperty("TargetId").GetInt32() == row.GetProperty("Targets")[0].GetInt32() &&
                requests[0].GetProperty("DefaultDamage").GetBoolean() &&
                requests[0].GetProperty("SourceCardId").GetInt32() == row.GetProperty("ActorCardId").GetInt32(),
                "Native owner-damage multiplier, front target, damage request or source attribution differs.");
        }
        int lasher = Unit(After(6), 6).Id, frontLasher = Unit(After(23), 23).Id;
        int[] front = new[] { 3, 4, 5 }.Select(card => Unit(After(24), card).Id).ToArray();
        var attacks = root.GetProperty("TriggeredDamage").EnumerateArray().Where(row =>
            row.GetProperty("Stage").GetString() == "Application" && row.GetProperty("ActorId").GetInt32() == lasher).ToArray();
        Require(attacks.Any(row => row.GetProperty("Targets").Deserialize<int[]>()!.SequenceEqual(front) &&
            row.GetProperty("Requests").EnumerateArray().Where(request => request.GetProperty("AttackerPreserved").GetBoolean())
                .Select(request => request.GetProperty("TargetId").GetInt32()).SequenceEqual(front)),
            "Lasher must damage all three units in front, retaining nested reactions separately.");
        Require(root.GetProperty("TriggeredDamage").EnumerateArray().Any(row =>
            row.GetProperty("ActorId").GetInt32() == frontLasher && row.GetProperty("Targets").GetArrayLength() == 0),
            "Front-position Lasher needs an independent empty-target witness.");
        int apostate = Unit(After(5), 5).Id, cantor = Unit(After(7), 7).Id, bloodshed = 0, lifesteal = 0;
        foreach (var row in root.GetProperty("TriggeredStatuses").EnumerateArray())
        {
            var before = row.GetProperty("Before").Deserialize<RoomCombatState>()!;
            if (before.Preview) continue;
            bool zeal = row.GetProperty("ActorId").GetInt32() == apostate && row.GetProperty("Kind").GetString() == "OnBloodshed";
            bool steal = row.GetProperty("ActorId").GetInt32() == cantor && row.GetProperty("Kind").GetString() == "PostCombat";
            if (!zeal && !steal) continue;
            var prior = row.GetProperty("BeforeUnits").Deserialize<CombatUnit[]>()!;
            var actual = row.GetProperty("ActualUnits").Deserialize<CombatUnit[]>()!;
            foreach (int targetId in row.GetProperty("Targets").Deserialize<int[]>()!)
            {
                var old = prior.Single(unit => unit.Id == targetId); var next = actual.Single(unit => unit.Id == targetId);
                string status = zeal ? "zeal" : "lifesteal"; int stacks = zeal ? 5 : 1;
                Require(next.Status(status)?.Stacks == (old.Status(status)?.Stacks ?? 0) + stacks &&
                    (!steal || old.Health < old.MaxHealth), "Bloodshed victim or damaged-only PostCombat status differs.");
                if (zeal) bloodshed++; else lifesteal++;
            }
        }
        Require(bloodshed > 0 && lifesteal > 0, "Both original status triggers need positive native witnesses.");
        using (var keys = new BattleStateKeys())
        {
            var actor = Unit(After(17), 4);
            Require(keys.SnapshotKey(actor) != keys.SnapshotKey(actor.WithDamageHistory(new UnitDamageHistory(0, 4, 5))),
                "Search deduplication ignored owner damage history.");
        }
        RoomCombatChecks.NativeRoomOnly(path); TrainCombatChecks.Native(root); RecordedBattleChecks.Native(root);
        // Native already validates every recorded frontier before searching; avoid enumerating all 25 twice.
        BattleSearchChecks.Native(root, requireZeroUnsupported: true);
        Console.WriteLine($"ORIGINAL-PUNISHED-UNITS PASS: six unchanged originals, permanent health/heal immunity, pre-armor Punish/armor/shield/zero controls, all-front and empty-front attacks, {bloodshed} Bloodshed victim buffs/{lifesteal} damaged-only PostCombat lifesteal grants; full chain/16 branches and zero-unsupported serial/parallel search.");
    }
}
