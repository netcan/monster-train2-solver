using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class CelebrateHealthBattleChecks
{
    private const string OwnerId = "6d77addd-17ad-43d1-a6b6-56bb39d78d82";
    private const string UpgradeId = "f55a8dba-1494-4dfe-92ec-894e106fa6cf";
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path); FixtureValue root = document.RootElement;
        Require(root.GetProperty("GameVersion").GetString() == "2.3.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            root.GetProperty("ModifierScenario").GetString() == "celebrate-health-battle" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }.All(name => root.GetProperty(name).GetInt32() == 0),
            "Celebrate-health native battle failed acceptance.");
        FixtureValue evidence = root.GetProperty("CelebrateHealthBattle");
        Require(evidence.GetProperty("Prepared").GetBoolean() && evidence.GetProperty("CompletedControlPlays").GetInt32() == 9 &&
            root.GetProperty("Actions").GetArrayLength() == 9, "Missing nine controlled inputs.");
        int[] cards = evidence.GetProperty("ControlCardIds").Deserialize<int[]>()!;
        FixtureValue definition = evidence.GetProperty("OwnerDefinition");
        Require(definition.GetProperty("Id").GetString() == OwnerId && definition.GetProperty("Key").GetString() == "DamagedMaxHealthOnCelebrate" &&
            definition.GetProperty("Type").GetString() == RelicModel.AddTrigger && !definition.GetProperty("Placement").GetBoolean() &&
            !definition.GetProperty("NoCountModifiers").GetBoolean() && definition.GetProperty("Interactions").GetArrayLength() == 0,
            "Original celebrate-health definition changed.");
        CombatTrigger trigger = definition.GetProperty("Triggers").Deserialize<CombatTrigger[]>()!.Single();
        CardActionEffect action = trigger.Effects.Single().UnitUpgrade!;
        Require(trigger.Kind == "OnEncounterComplete" && !trigger.Once && trigger.IgnoreSilence && trigger.SuppressTriggerNotification == true && trigger.Conditions == null &&
            trigger.Origin?.OnlyIfEquipped == false && action.Type == "UnitUpgrade" && action.Target == "Self" &&
            !action.AllowEnemy && action.AllowPlayer && action.Lifetime == "Permanent" && action.Filters?.Health == "Damaged" && action.Filters.SelfHealthFilter &&
            action.Upgrade?.DataId == UpgradeId && action.Upgrade.AssetKey == "Plus6HP" && action.Upgrade.Stats.Health == 6 &&
            action.Upgrade.Stats.Damage == 0 && !action.ScaleUpgrade, "Original celebrate-health trigger payload changed.");
        FixtureValue record = root.GetProperty("OriginalEquipmentGrowthEncounterComplete").EnumerateArray().Single();
        Require(record.GetProperty("Completed").GetBoolean() && record.GetProperty("CaptureError").ValueKind == FixtureKind.Null,
            "Missing settled native encounter callback.");
        TrainCombatState before = record.GetProperty("Before").Deserialize<TrainCombatState>()!,
            after = record.GetProperty("After").Deserialize<TrainCombatState>()!;
        int[] order = record.GetProperty("ActiveMonsterOrder").Deserialize<int[]>()!;
        CombatRelicState owner = before.Context!.Relics!.Single(relic => relic.DataId == OwnerId);
        RelicCharacterTriggers addition = owner.TriggerAdditions!.Single();
        Require(owner.AssetKey == "DamagedMaxHealthOnCelebrate" && owner.EffectTypes.SequenceEqual([RelicModel.AddTrigger]) &&
            addition.SourcePlayer && !addition.SourceEnemy && addition.SubtypeIsNone && !addition.SubtypeIsPyre &&
            !addition.NoCountModifiersAllowed && addition.CharacterAssetKeys.Count == 0 && addition.Conditions.Count == 0 &&
            addition.ExternalInteractions.Count == 0 && addition.Triggers.Single().Kind == trigger.Kind,
            "Original owner or installation filter changed.");
        CombatUnit[] units = before.Rooms.SelectMany(room => room.Units).ToArray();
        CombatUnit Carrier(int index) => units.Single(unit => unit.SpawnerCardId == cards[index]);
        Require(order.Length == 5 && units.Where(unit => unit.Team == CombatTeam.Player && !unit.IsPyre).All(unit =>
            unit.Triggers.Count(item => item.Kind == "OnEncounterComplete" && item.Effects.Any(effect => effect.UnitUpgrade?.Upgrade?.DataId == UpgradeId)) == 1) &&
            units.Single(unit => unit.IsPyre).Triggers.All(item => item.Effects.All(effect => effect.UnitUpgrade?.Upgrade?.DataId != UpgradeId)),
            "Friendly installation or Pyre exclusion differs.");
        Require(Carrier(1).Status("silenced")?.Stacks == 1 && Carrier(1).Health == 87 && Carrier(5).Health == 87 &&
            Carrier(4).Health == 100 && units.All(unit => unit.SpawnerCardId != cards[7]),
            "Missing wounded, silenced, healthy or sacrificed controls.");
        int upgraded = 0, healthy = 0;
        foreach (CombatUnit unit in units.Where(unit => unit.Team == CombatTeam.Player && !unit.IsPyre))
        {
            CombatUnit actual = after.Rooms.SelectMany(room => room.Units).Single(next => next.Id == unit.Id);
            int gain = unit.Health < unit.MaxHealth ? 6 : 0;
            Require(actual.MaxHealth == unit.MaxHealth + gain && actual.Health == unit.Health + gain &&
                Health(after.Context!.FindCard(unit.SpawnerCardId)!) - Health(before.Context.FindCard(unit.SpawnerCardId)!) == gain &&
                actual.Modifiers!.Upgrades.Count(item => item.DataId == UpgradeId) == (gain > 0 ? 1 : 0),
                "Permanent card/unit HP or damaged gate differs.");
            if (gain > 0) upgraded++; else healthy++;
        }
        Require(upgraded == 3 && healthy == 1 && Health(after.Context!.FindCard(cards[7])!) == 0,
            "Missing three permanent gains, healthy control or dead-card exclusion.");
        string parent = Json(before);
        TrainCombatResult direct = EncounterCompleteModel.Apply(before, order);
        Require(direct.Supported, direct.UnsupportedReason ?? "Unsupported encounter completion.");
        Equal(direct.State, after, "full native encounter-complete state/RNG");
        Require(Json(before) == parent, "Encounter completion mutated its parent.");
        Require(!EncounterCompleteModel.Apply(before, [.. order, order[0]]).Supported &&
            !EncounterCompleteModel.Apply(before, [.. order, int.MaxValue]).Supported, "Invalid native roster accepted.");
        using var keys = new BattleStateKeys();
        Require(keys.SnapshotKey(before) != keys.SnapshotKey(after), "Pruning merged permanent encounter growth.");
        var oldFilters = new CardTargetFilters("Damaged", [], [], false, "", []);
        Require(keys.SnapshotKey(oldFilters) != keys.SnapshotKey(new CardTargetFilters("Damaged", [], [], false, "", [], true)),
            "Pruning merged old/new Self health rules.");
        FixtureValue calibration = evidence.GetProperty("Calibration");
        Require(calibration.GetProperty("Completed").GetBoolean() && calibration.GetProperty("LiveContextUnchanged").GetBoolean() &&
            calibration.GetProperty("RestorationDifference").ValueKind == FixtureKind.Null, "Native target calibration was not restored.");
        FixtureValue[] samples = calibration.GetProperty("Samples").EnumerateArray().ToArray();
        Require(samples.Length == 48 && samples.Select(row => (row.GetProperty("ActorId").GetInt32(), row.GetProperty("Health").GetInt32(),
            row.GetProperty("Testing").GetBoolean(), row.GetProperty("Action").GetProperty("Filters").GetProperty("Health").GetString())).Distinct().Count() == 48,
            "Missing health/filter/testing matrix for both unit and Pyre.");
        foreach (FixtureValue sample in samples) Target(sample);
        Parallel.For(0, 32, _ => { foreach (FixtureValue sample in samples) Target(sample, nativeEvidence: false); });
        RoomCombatChecks.NativeRoomOnly(path); TrainCombatChecks.Native(root);
        RecordedBattleChecks.Native(root, completed: terminal =>
        {
            // Continue from the computed battle state, including its permanent cards.
            // The raw native callback retains a dying boss reference. Decision
            // snapshots finish frame removals; this Self upgrade never reads it.
            Equal(ComparableTrain(EncounterCompleteModel.EnterPhase(terminal.Spawn.Train)), ComparableTrain(before), "terminal-to-celebrate bridge");
            TrainCombatResult settled = EncounterCompleteModel.Apply(terminal.Spawn.Train, order);
            Require(settled.Supported, settled.UnsupportedReason ?? "Unsupported continuous settlement.");
            Equal(ComparableTrain(settled.State!), ComparableTrain(after), "continuous encounter-complete successor");
        });
        Console.WriteLine("CELEBRATE-HEALTH-BATTLE PASS: unchanged original owner, three permanent +6 HP gains including a silenced carrier, healthy/dead/Pyre controls, 48 native Self health targets with 32 component branches, complete settlement/RNG and 16 full immutable chains through encounter completion.");
    }
    private static void Target(FixtureValue row, bool nativeEvidence = true)
    {
        Require(row.GetProperty("Completed").GetBoolean() && row.GetProperty("Difference").ValueKind == FixtureKind.Null,
            "Incomplete native Self target calibration.");
        CombatUnit actor = row.GetProperty("Actor").Deserialize<CombatUnit>()!;
        CardActionEffect action = row.GetProperty("Action").Deserialize<CardActionEffect>()!;
        int floor = row.GetProperty("RoomIndex").GetInt32(); bool testing = row.GetProperty("Testing").GetBoolean();
        var room = new RoomCombatState(floor, false, [actor], []);
        var train = new TrainCombatState([room], [], 4);
        string parent = Json(train);
        int[] targets = row.GetProperty("Targets").Deserialize<int[]>()!;
        CardTargets local = CardTargetModel.Collect(room, action, [], selfUnitId: actor.Id, isTesting: testing),
            tower = CardTargetModel.Collect(train, floor, action, [], selfUnitId: actor.Id, isTesting: testing);
        Require(action.Filters!.SelfHealthFilter && local.Supported && tower.Supported && local.UnitIds.SequenceEqual(targets) &&
            tower.UnitIds.SequenceEqual(targets), "Independent Self target selection differs.");
        if (nativeEvidence)
        {
            Equal(row.GetProperty("Before").Deserialize<TrainCombatState>(), row.GetProperty("Actual").Deserialize<TrainCombatState>(), "native target pure state");
            Equal(row.GetProperty("RngBefore").ToObjectGraph(), row.GetProperty("RngAfter").ToObjectGraph(), "native target pure RNG");
        }
        Require(Json(train) == parent, "Self targeting mutated its parent.");
    }
    private static int Health(CardInstanceState card) => card.Permanent.Upgrades.Where(upgrade => upgrade.DataId == UpgradeId).Sum(upgrade => upgrade.Stats.Health);
    private static object ComparableTrain(TrainCombatState train)
    {
        train = TrainCombatModel.CompleteFrameRemovals(train);
        return new { Rooms = train.Rooms.Select(room => new { room.RoomIndex, room.Deployment, room.Units, room.ExternalInteractions, room.Preview, room.ActiveCompanionBossIds }),
            train.Movement, train.EnemySlotsPerRoom, Context = BattleTurnChecks.ComparableContext(train.Context) };
    }
    private static string Json(object? value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Equal(object? a, object? b, string label)
    {
        string? error = ModelJson.Difference(Json(a), Json(b)); Require(error == null, label + ": " + error);
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
