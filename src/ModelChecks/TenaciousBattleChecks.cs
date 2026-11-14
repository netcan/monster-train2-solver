using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TenaciousBattleChecks
{
    private const string OwnerId = "2d68b9db-bcbc-487e-96de-b6398ad3c36b";
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path);
        FixtureValue root = document.RootElement;
        Require(root.GetProperty("GameVersion").GetString() == "2.3.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            root.GetProperty("ModifierScenario").GetString() == "tenacious-battle" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            root.GetProperty("TerminalCaptureBoundary").GetString() == "AfterStopCombatLoop", "Missing settled native Tenacious victory.");
        foreach (string counter in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(counter).GetInt32() == 0, "Incomplete Tenacious battle: " + counter);
        FixtureValue evidence = root.GetProperty("TenaciousBattle");
        int[] ids = evidence.GetProperty("ControlCardIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(evidence.GetProperty("Prepared").GetBoolean() && evidence.GetProperty("CompletedControlPlays").GetInt32() == 11 &&
            ids.Length == 11 && ids.Distinct().Count() == 11 && actions.Length == 11 && actions.Select(row =>
                row.GetProperty("Action").GetProperty("CardInstanceId").GetInt32()).SequenceEqual(ids), "Missing eleven ordered native plays.");
        BattleTurnState After(int index) => actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatContext initial = actions[0].GetProperty("Before").Deserialize<BattleTurnState>()!.Spawn.Train.Context!;
        CombatRelicState owner = initial.Relics!.Single(item => item.DataId == OwnerId);
        Require(owner.AssetKey == "SpellsNoKillFriendlies" && owner.IsCovenant == false && owner.DisallowedInPlacementPhase == false &&
            owner.EffectTypes.SequenceEqual([RelicModel.AddStatusOnSpawn]) && owner.SpawnStatuses is { Count: 1 } &&
            owner.SpawnStatuses[0] is { TargetPlayer: true, TargetEnemy: false, SubtypeIsNone: true, ApplyToTeam: false } &&
            owner.SpawnStatuses[0].Statuses.Single().Id == "tenacious" && owner.SpawnStatuses[0].Statuses[0].Stacks == 1,
            "Missing unchanged original Tenacious spawn owner.");
        FixtureValue definition = evidence.GetProperty("OwnerDefinition");
        Require(definition.GetProperty("Id").GetString() == OwnerId && definition.GetProperty("Key").GetString() == owner.AssetKey &&
            !definition.GetProperty("Placement").GetBoolean() && definition.GetProperty("Type").GetString() == RelicModel.AddStatusOnSpawn &&
            definition.GetProperty("Team").GetString() == "Monsters" && definition.GetProperty("Statuses").EnumerateArray().Single()
                .GetProperty("statusId").GetString() == "tenacious", "Changed native original-owner definition.");
        int[] health = [100, 1, 1, 1, 9, 9, 1, 1, 1];
        int[] damage = [0, 99, 99, 99, 99, 99, 111, 111, 111];
        int[] friendly = [0, 99, 100, 1099, 1099, 1099, 1112, 1112, 1112];
        int playerId = After(0).Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == ids[0]).Id;
        for (int index = 0; index < 9; index++)
        {
            CombatContext context = After(index).Spawn.Train.Context!;
            CombatUnit unit = After(index).Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == playerId);
            Require(unit.Health == health[index] && unit.MaxHealth == 100 && unit.Status("tenacious")?.Stacks == 1 &&
                (unit.Status("armor")?.Stacks ?? 0) == (index == 5 ? 4 : 0),
                "Native friendly lethal/repeated/heal/armor control differs at " + index);
            Require(context.Statistics!.MonsterDamage!.ThisBattle == damage[index] &&
                context.Statistics.MonsterDamage.FriendlyThisBattle == friendly[index],
                "Actual versus prevented-friendly damage statistics differ at " + index);
            if (index is >= 1 and <= 6)
                Require(context.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(card => card.InstanceId == ids[index]),
                    "Native control spell did not consume.");
        }
        var firstTurn = root.GetProperty("Turns").EnumerateArray().First();
        BattleTurnState afterStart = firstTurn.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(afterStart.Spawn.Train.Context!.Statistics!.MonsterDamage is { ThisBattle: 111, FriendlyThisBattle: 1112 } &&
            afterStart.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == playerId).Health == 1,
            "Deployment incorrectly ran ordinary turn-begin self-damage.");
        int enemyId = evidence.GetProperty("EnemyTargetId").GetInt32();
        Require(After(9).Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == enemyId) is
            { Team: CombatTeam.Enemy, Health: 1 } enemy && enemy.Status("tenacious")?.Stacks == 1 &&
            !After(10).Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit => unit.Id == enemyId && unit.Health > 0),
            "An enemy's Tenacious incorrectly prevented a hostile player spell.");
        bool hostileRetaliation = root.GetProperty("Stages").EnumerateArray().Any(stage => {
            if (stage.GetProperty("Kind").GetString() != "Room" || stage.GetProperty("Turn").GetInt32() != 1) return false;
            RoomCombatState before = stage.GetProperty("Before").Deserialize<RoomCombatState>()!;
            RoomCombatState actual = stage.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            bool boundary = before.Units.Any(unit => unit.Id == playerId && unit.Health == 1 && unit.Status("tenacious")?.Stacks == 1) &&
                before.Units.Any(unit => unit.Team == CombatTeam.Enemy && unit.Health > 0 && unit.Status("spikes")?.Stacks == 2) &&
                !actual.Units.Any(unit => unit.Id == playerId && unit.Health > 0) &&
                before.Context!.Statistics!.MonsterDamage is { ThisBattle: 113, FriendlyThisBattle: 1114 } &&
                actual.Context!.Statistics!.MonsterDamage is { ThisBattle: 114, FriendlyThisBattle: 1115 };
            if (!boundary) return false;
            RoomCombatResult result = RoomCombatModel.Resolve(before);
            Require(result.Supported, result.UnsupportedReason ?? "Self-damage/hostile retaliation room unsupported.");
            CombatEvent[] hits = result.Events.Where(item => item.Target == playerId && item.Kind is "TriggeredDamage" or "Spikes").ToArray();
            Require(hits.Length == 2 && hits[0] is { Kind: "TriggeredDamage", Amount: 0 } && hits[0].Actor == playerId &&
                hits[1] is { Kind: "Spikes", Amount: 2 } && before.Units.Single(unit => unit.Id == hits[1].Actor).Team == CombatTeam.Enemy,
                "Friendly self-damage was not prevented before the hostile lethal retaliation.");
            return true;
        });
        Require(hostileRetaliation, "Missing natural hostile Spikes killing the one-HP Tenacious holder.");
        int playerBirths = 0, enemyBirths = 0;
        foreach (FixtureValue row in root.GetProperty("RelicSpawnStatuses").EnumerateArray())
        {
            Require(row.GetProperty("Completed").GetBoolean() && row.GetProperty("Difference").ValueKind == FixtureKind.Null &&
                row.GetProperty("UnsupportedQueueReason").ValueKind == FixtureKind.Null, "Incomplete native Tenacious birth.");
            TrainCombatState before = row.GetProperty("BeforeTrain").Deserialize<TrainCombatState>()!;
            TrainCombatState actual = row.GetProperty("AfterTrain").Deserialize<TrainCombatState>()!;
            int id = row.GetProperty("UnitId").GetInt32(); string parent = Serialize(before);
            var result = RelicBirthModel.CharacterAdded(before, id, row.GetProperty("FromCardId").GetInt32(),
                row.GetProperty("OnlyCovenants").GetBoolean(), row.GetProperty("DeferCallbacks").GetBoolean(),
                row.GetProperty("QueuedBefore").Deserialize<UnitCloneCallback[]>()!);
            Require(result.Supported, result.UnsupportedReason ?? "Tenacious birth unsupported.");
            Equal(result.State, actual, "complete native birth train");
            Equal(result.Queued, row.GetProperty("QueuedAfter").Deserialize<UnitCloneCallback[]>(), "native birth queue");
            Equal(result.Dispatched, row.GetProperty("Dispatched").Deserialize<UnitCloneCallback[]>(), "native dispatched queue");
            CombatUnit actor = actual.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == id);
            if (actor.Team == CombatTeam.Player)
            {
                playerBirths++;
                Require(actor.Status("tenacious")?.Stacks == 1 && actual.Context!.BattleRng.Equals(before.Context!.BattleRng.Range(0, 1).State),
                    "Native player birth status or one-element selection RNG differs.");
            }
            else { enemyBirths++; Require(actor.Status("tenacious") == null, "Friendly-only owner applied Tenacious to an enemy."); }
            Require(parent == Serialize(before), "A birth branch mutated its parent.");
        }
        Require(playerBirths == 3 && enemyBirths > 0, "Missing native player/enemy birth controls.");
        Calibration(evidence.GetProperty("Calibration"));
        RoomCombatChecks.NativeRoomOnly(path); TrainCombatChecks.Native(root); RecordedBattleChecks.Native(root);
        Console.WriteLine($"TENACIOUS-BATTLE PASS: unchanged original SpellsNoKillFriendlies, friendly lethal/repeated-one-HP/Heal/armor controls, character self-damage, hostile spell and natural Spikes deaths, prevented-friendly statistics, 58 native damage-stage/source controls and four nonstackable admissions, {playerBirths + enemyBirths} full births, complete battle/RNG and 16 immutable branches.");
    }
    private static void Calibration(FixtureValue evidence)
    {
        Require(evidence.GetProperty("Completed").GetBoolean() && evidence.GetProperty("LiveContextUnchanged").GetBoolean() &&
            evidence.GetProperty("RestorationDifference").ValueKind == FixtureKind.Null, "Native Tenacious calibration did not restore the live train.");
        Equal(evidence.GetProperty("RngBefore").ToObjectGraph(), evidence.GetProperty("RngAfter").ToObjectGraph(), "native stage RNG restoration");
        FixtureValue[] samples = evidence.GetProperty("Samples").EnumerateArray().ToArray();
        Require(samples.Length == 58 && samples.Take(48).Select(sample => (sample.GetProperty("TargetTeam").GetInt32(),
            sample.GetProperty("Source").GetString(), sample.GetProperty("Damage").GetInt32())).Distinct().Count() == 48,
            "Missing two-team/source/below-exact-overkill native matrix.");
        foreach (FixtureValue sample in samples) Verify(sample);
        Parallel.For(0, 32, _ => { foreach (FixtureValue sample in samples) Verify(sample); });
        FixtureValue[] admissions = evidence.GetProperty("Admissions").EnumerateArray().ToArray();
        Require(admissions.Length == 4 && admissions.Count(row => row.GetProperty("NativeTest").GetBoolean()) == 2,
            "Missing both-team absent/present native nonstackable admission controls.");
        foreach (FixtureValue row in admissions) Admission(row);
        Parallel.For(0, 32, _ => { foreach (FixtureValue row in admissions) Admission(row); });
        static void Admission(FixtureValue row)
        {
            Require(row.GetProperty("Completed").GetBoolean() && row.GetProperty("Difference").ValueKind == FixtureKind.Null,
                "Incomplete native nonstackable effect test.");
            TrainCombatState before = row.GetProperty("Before").Deserialize<TrainCombatState>()!;
            string parent = Serialize(before);
            var result = CardSpellModel.TestPlay(before, row.GetProperty("Room").GetInt32(),
                [row.GetProperty("Action").Deserialize<CardActionEffect>()!], row.GetProperty("TargetId").GetInt32());
            Require(result.Supported && result.CanPlay == row.GetProperty("NativeTest").GetBoolean() &&
                result.CanPlay == (row.GetProperty("Stacks").GetInt32() == 0), "Native fixed nonstackable status admission differs.");
            Require(parent == Serialize(before), "A nonstackable status branch mutated its parent.");
        }
        static void Verify(FixtureValue sample)
        {
            CombatTeam team = sample.GetProperty("TargetTeam").Deserialize<CombatTeam>();
            CombatTeam? attacker = sample.GetProperty("AttackerTeam").ValueKind == FixtureKind.Null ? null : sample.GetProperty("AttackerTeam").Deserialize<CombatTeam>();
            bool hasCard = sample.GetProperty("HasCard").GetBoolean(), hasRelic = sample.GetProperty("HasRelic").GetBoolean();
            bool friendly = TenaciousModel.IsFriendlySource(team, attacker, hasCard, hasRelic);
            TenaciousDamageResult result = TenaciousModel.Adjust(sample.GetProperty("Health").GetInt32(), sample.GetProperty("Damage").GetInt32(),
                friendly, sample.GetProperty("Stacks").GetInt32() > 0);
            Require(sample.GetProperty("Completed").GetBoolean() && sample.GetProperty("Difference").ValueKind == FixtureKind.Null &&
                friendly == sample.GetProperty("NativeFriendly").GetBoolean() && result.Damage == sample.GetProperty("ActualDamage").GetInt32() &&
                result.Prevented == sample.GetProperty("ActualPrevented").GetInt32() &&
                unchecked(result.Prevented + sample.GetProperty("Blocked").GetInt32()) == sample.GetProperty("ActualBlocked").GetInt32(),
                "Native source precedence, health boundary, stack/negative/zero or additive blocked damage differs.");
        }
    }
    private static string Serialize(object? value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Equal(object? a, object? b, string label)
    {
        string? difference = ModelJson.Difference(Serialize(a), Serialize(b));
        Require(difference == null, label + ": " + difference);
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
