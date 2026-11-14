using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class ChainedBattleChecks
{
    private const string OwnerId = "d0294a77-7204-4e9e-9b22-7f38e0d59ad0";
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path); FixtureValue root = document.RootElement;
        Require(root.GetProperty("GameVersion").GetString() == "2.3.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            root.GetProperty("ModifierScenario").GetString() == "chained-battle" && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }.All(name => root.GetProperty(name).GetInt32() == 0),
            "Incomplete native chained original-owner battle.");
        FixtureValue evidence = root.GetProperty("ChainedBattle");
        int[] ids = evidence.GetProperty("ControlCardIds").EnumerateArray().Select(value => value.GetInt32()).ToArray();
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(evidence.GetProperty("Prepared").GetBoolean() && evidence.GetProperty("CompletedControlPlays").GetInt32() == 13 &&
            ids.Length == 13 && ids.Distinct().Count() == 13 && actions.Length == 13 && actions.Select(row =>
                row.GetProperty("Action").GetProperty("CardInstanceId").GetInt32()).SequenceEqual(ids), "Missing thirteen ordered control inputs.");
        BattleTurnState After(int index) => actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatUnit Carrier(int index, int card) => After(index).Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == ids[card]);
        CombatRelicState owner = actions[0].GetProperty("Before").Deserialize<BattleTurnState>()!.Spawn.Train.Context!.Relics!.Single(relic => relic.DataId == OwnerId);
        Require(owner.AssetKey == "ChainedDamageIncrease" && owner.IsCovenant == false && owner.DisallowedInPlacementPhase == false &&
            owner.EffectTypes.SequenceEqual([RelicModel.AddStatusOnSpawn]) && owner.SpawnStatuses is { Count: 1 } &&
            owner.SpawnStatuses[0] is { TargetPlayer: false, TargetEnemy: true, SubtypeIsNone: true, ApplyToTeam: false } &&
            owner.SpawnStatuses[0].Statuses.Single().Id == "chained" && owner.SpawnStatuses[0].Statuses[0].Stacks == 1,
            "Original enemy-spawn chained owner changed.");
        FixtureValue definition = evidence.GetProperty("OwnerDefinition");
        Require(definition.GetProperty("Id").GetString() == OwnerId && definition.GetProperty("Key").GetString() == owner.AssetKey &&
            !definition.GetProperty("Placement").GetBoolean() && definition.GetProperty("Type").GetString() == RelicModel.AddStatusOnSpawn &&
            definition.GetProperty("Team").GetString() == "Heroes" && definition.GetProperty("Statuses").EnumerateArray().Single()
                .GetProperty("statusId").GetString() == "chained", "Native original-owner definition changed.");
        Require(Carrier(6, 0).Health == 40 && Carrier(6, 0).Status("armor")?.Stacks == 3 &&
            Carrier(8, 0).Health == 40 && Carrier(8, 0).Status("armor")?.Stacks == 3 && Carrier(8, 0).Status("damage shield") == null &&
            Carrier(9, 0).Health == 1 && Carrier(9, 0).Status("tenacious")?.Stacks == 1 && Carrier(10, 0).Health == 40 &&
            Carrier(11, 1).Health == 1 && Carrier(11, 1).Status("undying") == null &&
            !After(12).Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit => unit.SpawnerCardId == ids[2]),
            "Armor, shield, Tenacious, heal, Undying or final-removal control differs.");
        CombatUnit[] ShieldEnemies(int index) => After(index).Spawn.Train.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Enemy).ToArray();
        Require(ShieldEnemies(7).Length > 0 && ShieldEnemies(7).Select(unit => (unit.Id, unit.Health, unit.Status("armor")?.Stacks))
            .SequenceEqual(ShieldEnemies(8).Select(unit => (unit.Id, unit.Health, unit.Status("armor")?.Stacks))),
            "A shield-absorbed hit propagated chained damage.");
        FixtureValue[] propagations = evidence.GetProperty("Propagations").EnumerateArray().ToArray(),
            requests = evidence.GetProperty("Requests").EnumerateArray().ToArray();
        Require(propagations.Length == 63 && requests.Length == 42 && propagations.Concat(requests).All(row => row.GetProperty("Completed").GetBoolean()),
            "Missing complete native real/preview observations.");
        foreach (FixtureValue row in propagations)
            Require(row.GetProperty("Damage").GetInt32() > 0 && row.GetProperty("DamageType").GetInt32() != ChainedStatusModel.DamageType &&
                Unit(row, "Before").Team == CombatTeam.Player, "Recursive or wrong-team native propagation.");
        Require(propagations.Any(row => !Preview(row) && Unit(row, "Before").Health == 0 && Unit(row, "Before").Status("undying")?.Stacks == 1 &&
            Unit(row, "After").Health == 1 && Unit(row, "After").Status("undying") == null), "Missing early Undying revival inside native propagation.");
        foreach (bool preview in new[] { false, true })
        {
            FixtureValue[] rows = requests.Where(row => Preview(row) == preview).ToArray();
            int armor = 0, health = 0;
            foreach (FixtureValue row in rows)
            {
                CombatUnit before = Unit(row, "Before"), after = Unit(row, "After"), source = Unit(row, "Source");
                int damage = Math.Max(0, row.GetProperty("Damage").GetInt32()), blocked = Math.Min(damage, before.Status("armor")?.Stacks ?? 0);
                Require(row.GetProperty("DamageType").GetInt32() == ChainedStatusModel.DamageType &&
                    row.GetProperty("NullCard").GetBoolean() && row.GetProperty("NullRelic").GetBoolean() &&
                    before.Team == CombatTeam.Enemy && source.Team == CombatTeam.Player && before.Status("chained")?.Stacks == 1 &&
                    after.Health == Math.Max(0, before.Health - (damage - blocked)) &&
                    (after.Status("armor")?.Stacks ?? 0) == (before.Status("armor")?.Stacks ?? 0) - blocked,
                    "Native secondary damage, attribution or armor/health result differs.");
                if (blocked > 0) armor++;
                if (after.Health < before.Health) health++;
            }
            Require(armor > 0 && health > 0 && rows.Any(row => Unit(row, "Source").Health == 0),
                "Missing armor/health damage or dead friendly source in real/preview observations.");
        }
        FixtureValue[] timeline = evidence.GetProperty("Timeline").EnumerateArray().ToArray();
        int earlyRevives = 0;
        foreach (bool preview in new[] { false, true })
        {
            var stack = new Stack<(string Kind, int Unit)>();
            foreach (FixtureValue row in timeline.Where(row => Preview(row) == preview))
            {
                string kind = row.GetProperty("Kind").GetString()!; int id = row.GetProperty("UnitId").GetInt32();
                if (row.GetProperty("Phase").GetString() == "Enter")
                {
                    if (kind == "Chained") Require(stack.Any(entry => entry.Kind == "Propagation"), "Secondary damage escaped its native propagation scope.");
                    if (!preview && kind == "OnReanimated" && stack.Any(entry => entry.Kind == "Propagation")) earlyRevives++;
                    stack.Push((kind, id));
                }
                else Require(stack.TryPop(out var active) && active == (kind, id), "Native trigger/coroutine nesting differs.");
            }
            Require(stack.Count == 0 && timeline.Any(row => Preview(row) == preview && row.GetProperty("Kind").GetString() == "OnPunish" &&
                row.GetProperty("DamageType").GetInt32() == ChainedStatusModel.DamageType), "Missing complete secondary Punish dispatch.");
        }
        Require(earlyRevives > 0, "Revival callbacks did not run inside Chained propagation.");
        BattleTurnState queuedControl = actions[6].GetProperty("Before").Deserialize<BattleTurnState>()!;
        RoomCombatState queuedRoom = queuedControl.Spawn.Train.Rooms[0];
        int queuedTarget = queuedRoom.Units.Single(unit => unit.SpawnerCardId == ids[0]).Id;
        var deferred = new List<RoomCombatModel.QueuedCharacterTrigger>();
        RoomCombatResult guarded = RoomCombatModel.ApplyNativeCardDamageAfterTraits(queuedRoom, queuedTarget, 5, ids[6],
            enqueueCharacterTrigger: deferred.Add);
        Require(!guarded.Supported && guarded.UnsupportedReason == "Chained requires synchronous Bloodshed settlement across the external trigger queue." &&
            deferred.Any(callback => callback.Kind == "OnBloodshed"), "External deferred Bloodshed silently propagated out of order.");
        RoomCombatChecks.NativeRoomOnly(path); TrainCombatChecks.Native(root); RecordedBattleChecks.Native(root);
        Console.WriteLine("CHAINED-BATTLE PASS: unchanged original enemy-spawn owner, 13 inputs, armor/shield/Tenacious/heal/Undying/final death, 63 propagations and 42 real/preview damage requests with native nesting, complete successor/RNG replay and 16 immutable full battle branches. External deferred Bloodshed queues remain guarded.");
    }
    private static CombatUnit Unit(FixtureValue row, string key) => row.GetProperty(key).Deserialize<CombatUnit>()!;
    private static bool Preview(FixtureValue row) => row.GetProperty("Preview").GetBoolean();
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
