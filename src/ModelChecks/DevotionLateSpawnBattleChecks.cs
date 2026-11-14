using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class DevotionLateSpawnBattleChecks
{
    private const string OwnerId = "5cb959f3-6a24-4445-b1f5-5c3e5a822b4d";
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path);
        FixtureValue root = document.RootElement;
        Require(root.GetProperty("GameVersion").GetString() == "2.3.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            root.GetProperty("ModifierScenario").GetString() == "devotion-late-spawn-battle" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            root.GetProperty("TerminalCaptureBoundary").GetString() == "AfterStopCombatLoop", "Missing settled native late-spawn victory.");
        foreach (string counter in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(counter).GetInt32() == 0, "Incomplete late-spawn battle: " + counter);
        FixtureValue evidence = root.GetProperty("DevotionLateSpawnBattle");
        int[] ids = evidence.GetProperty("ControlCardIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(evidence.GetProperty("Prepared").GetBoolean() && evidence.GetProperty("CompletedControlPlays").GetInt32() == 8 &&
            ids.Length == 8 && ids.Distinct().Count() == 8 && actions.Length == 8 && actions.Select(row =>
                row.GetProperty("Action").GetProperty("CardInstanceId").GetInt32()).SequenceEqual(ids), "Missing eight ordered native plays.");
        BattleTurnState Before(int index) => actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int index) => actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatUnit Born(int step, int after) => After(after).Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == ids[step]);
        CombatContext initial = Before(0).Spawn.Train.Context!;
        CombatRelicState owner = initial.Relics!.Single(relic => relic.DataId == OwnerId);
        Require(owner.AssetKey == "FriendlyUnitPunish" && owner.IsCovenant == false && owner.DisallowedInPlacementPhase == false &&
            owner.EffectTypes.SequenceEqual([RelicUnitSpawnEffectModel.Effect]) && owner.ResourceEffects is { Count: 1 } &&
            RelicUnitSpawnEffectModel.Validate(owner, owner.ResourceEffects.Single()) == null &&
            owner.ResourceEffects.Single().UnitSpawnEffect is { LateSpawn: true, Action: { Type: DevotionStatusModel.Effect, DevotionLevelsPerGrant: 0 } } &&
            initial.Relics.Any(relic => relic.DataId == RelicUnitSpawnEffectModel.DataId), "Missing unchanged late-spawn and old Whelp owners.");
        FixtureValue definition = evidence.GetProperty("OwnerDefinition");
        Require(definition.GetProperty("Id").GetString() == OwnerId && definition.GetProperty("Key").GetString() == owner.AssetKey &&
            definition.GetProperty("Type").GetString() == RelicUnitSpawnEffectModel.Effect && definition.GetProperty("LateSpawn").GetBoolean() &&
            !definition.GetProperty("Placement").GetBoolean() && definition.GetProperty("Nested").EnumerateArray().Single()
                .GetProperty("Type").GetString() == "CardEffectAddStatusEffectPerDevotionLevel", "Changed original owner definition.");
        Require(Before(0).Spawn.Train.Context!.QueryFrame!.Devotion!.Level == 0 &&
            Before(2).Spawn.Train.Context!.QueryFrame!.Devotion!.Level == 0 && After(2).Spawn.Train.Context!.QueryFrame!.Devotion!.Level == 1 &&
            After(4).Spawn.Train.Context!.QueryFrame!.Devotion!.Level == 2, "Missing OnSpawn level-up before late-spawn status.");
        Require((Born(0, 0).Status("zeal")?.Stacks ?? 0) == 0 && (Born(0, 1).Status("zeal")?.Stacks ?? 0) == 0 &&
            Born(2, 2).Status("zeal")?.Stacks == 1 && (Born(0, 3).Status("zeal")?.Stacks ?? 0) == 0 &&
            Born(0, 5).Status("zeal")?.Stacks == 2 && Born(6, 6).Status("zeal")?.Stacks == 2 &&
            Born(2, 6).Status("zeal")?.Stacks == 1, "Zero/one/two birth, divisor rounding or no-retroactive control differs.");
        Require(Born(0, 2).Status("cooldown")?.Stacks == 6 && Born(2, 2).Status("cooldown")?.Stacks == 6 &&
            Born(6, 6).Status("cooldown")?.Stacks == 8, "Old early-spawn Whelp owner did not independently affect the tower.");
        CombatUnit[] prior = Before(7).Spawn.Train.Rooms[2].Units.Where(unit => unit.Team == CombatTeam.Player).ToArray();
        CombatUnit[] copies = After(7).Spawn.Train.Rooms[2].Units.Where(unit => unit.Team == CombatTeam.Player && prior.All(old => old.Id != unit.Id)).ToArray();
        Require(prior.Length == 3 && copies.Length == 1 && copies[0].Status("cardless")?.Stacks == 1 &&
            copies[0].Status("zeal")?.Stacks == 2 && copies[0].MaxHealth == 102 && copies[0].Health == 100,
            "Copied birth missed late-spawn status or native character-copy replacement order.");
        Require(Before(4).Energy - After(4).Energy == 3 && After(4).Spawn.Train.Context!.OtherPiles!.Single(pile => pile.Name == "Exhausted")
            .Cards.Any(card => card.InstanceId == ids[4] && card.DataId == DevotionSpawnBattleScenarioId), "Original Venerate cost/Consume differs.");
        FixtureValue gates = evidence.GetProperty("Gates");
        Require(gates.GetProperty("Completed").GetBoolean() && gates.GetProperty("LiveContextUnchanged").GetBoolean() &&
            gates.GetProperty("RestorationDifference").ValueKind == FixtureKind.Null, "Native gate calibration was not restored.");
        FixtureValue[] samples = gates.GetProperty("Samples").EnumerateArray().ToArray();
        Require(samples.Length == 20 && samples.Select(row => (row.GetProperty("Level").GetInt32(), row.GetProperty("Divisor").GetInt32())).Distinct().Count() == 20,
            "Missing signed divisor/level matrix.");
        foreach (FixtureValue sample in samples) Gate(sample);
        Parallel.For(0, 32, _ => { foreach (FixtureValue sample in samples) Gate(sample); });
        FixtureValue[] records = evidence.GetProperty("Records").EnumerateArray().ToArray();
        foreach (FixtureValue row in records) Verify(row);
        FixtureValue[] applied = records.Where(row => row.GetProperty("Kind").GetString() == "Apply").ToArray();
        Require(applied.Count(row => row.GetProperty("OwnerId").GetString() == OwnerId) == 4 &&
            applied.Count(row => row.GetProperty("OwnerId").GetString() == RelicUnitSpawnEffectModel.DataId) == 1 &&
            applied.Count(row => row.GetProperty("Notifications").GetInt32() == 1) == 4 &&
            applied.Count(row => row.GetProperty("Notifications").GetInt32() == 0) == 1, "Missing five raw bodies and the zero-level notification control.");
        Require(records.Count(row => row.GetProperty("Kind").GetString() == "Manager" && row.GetProperty("Notifications").GetInt32() == 2) == 4 &&
            records.Count(row => row.GetProperty("Kind").GetString() == "Manager" && row.GetProperty("Notifications").GetInt32() == 1) == 1,
            "Manager notification differs from nested effect notification.");
        Require(records.Any(row => row.GetProperty("Control").GetBoolean() && row.GetProperty("ActorId").GetInt32() == 0 &&
            !row.GetProperty("Returned").GetBoolean()) && records.Any(row => row.GetProperty("Control").GetBoolean() && row.GetProperty("Returned").GetBoolean()),
            "Missing native null/wrong-phase and accepted controls.");
        Parallel.For(0, 32, _ => { foreach (FixtureValue row in applied) Verify(row); });
        using var keys = new BattleStateKeys();
        CardActionEffect divisorAction = samples.First(row => row.GetProperty("Divisor").GetInt32() == 2).GetProperty("Action").Deserialize<CardActionEffect>()!;
        CardActionEffect zeroAction = samples.First(row => row.GetProperty("Divisor").GetInt32() == 0).GetProperty("Action").Deserialize<CardActionEffect>()!;
        Require(keys.SnapshotKey(divisorAction) != keys.SnapshotKey(zeroAction), "Pruning merged different status divisors.");
        var missingDivisor = new CardActionEffect(divisorAction.Type, divisorAction.Target, divisorAction.Value,
            divisorAction.AllowEnemy, divisorAction.AllowPlayer, divisorAction.Statuses, tests: divisorAction.Tests, filters: divisorAction.Filters);
        Require(!CardSpellModel.TestPlay(initialTrain(), 2, [missingDivisor], 0).Supported, "Missing native divisor was inferred.");
        RelicResourceEffect changed = JsonSerializer.Deserialize<RelicResourceEffect>(Serialize(owner.ResourceEffects.Single())
            .Replace("\"LateSpawn\":true", "\"LateSpawn\":false"), ModelJson.Options)!;
        Require(changed.UnitSpawnEffect?.LateSpawn == false && RelicUnitSpawnEffectModel.Validate(owner, changed) != null &&
            keys.SnapshotKey(owner.ResourceEffects.Single()) != keys.SnapshotKey(changed), "Missing late-spawn metadata was accepted or merged by pruning.");
        TrainCombatState missing = CardSpellModel.WithContext(initialTrain(), initial.WithQueryFrame(new StatisticQueryFrame()));
        Require(!CardSpellModel.TestPlay(missing, 2, [divisorAction], 0).Supported, "Missing Devotion was inferred.");
        RoomCombatChecks.NativeRoomOnly(path); TrainCombatChecks.Native(root); RecordedBattleChecks.Native(root);
        Console.WriteLine($"DEVOTION-LATE-SPAWN-BATTLE PASS: unchanged FriendlyUnitPunish and old Whelp owner, eight plays including a copied birth, zero/one/two levels, OnSpawn upgrade before late status, divisor-two application, 20 signed-divisor native tests, {records.Length} full callbacks, 32 immutable component branches and 16 full battle chains.");
        TrainCombatState initialTrain() => Before(0).Spawn.Train;
    }
    private const string DevotionSpawnBattleScenarioId = "96c9ed37-5e59-4c4f-a914-ae7040d24755";
    private static void Gate(FixtureValue row)
    {
        TrainCombatState before = row.GetProperty("Before").Deserialize<TrainCombatState>()!;
        CardActionEffect action = row.GetProperty("Action").Deserialize<CardActionEffect>()!;
        string parent = Serialize(before);
        int level = row.GetProperty("Level").GetInt32(), divisor = row.GetProperty("Divisor").GetInt32();
        SpellCastCheck result = CardSpellModel.TestPlay(before, 2, [action], 0);
        Require(result.Supported && result.CanPlay == (level / Math.Max(1, divisor) > 0) &&
            result.CanPlay == row.GetProperty("NativeTest").GetBoolean() && action.DevotionLevelsPerGrant == divisor &&
            row.GetProperty("Completed").GetBoolean() && row.GetProperty("Difference").ValueKind == FixtureKind.Null, "Native status divisor admission differs.");
        Equal(before, row.GetProperty("Actual").Deserialize<TrainCombatState>(), "native gate successor");
        Equal(row.GetProperty("RngBefore").ToObjectGraph(), row.GetProperty("RngAfter").ToObjectGraph(), "native gate RNG");
        Require(parent == Serialize(before), "Gate test mutated its parent.");
    }
    private static void Verify(FixtureValue row)
    {
        Require(row.GetProperty("Completed").GetBoolean() && new[] { "Error", "Difference", "UnsupportedReason" }.All(name =>
            row.GetProperty(name).ValueKind == FixtureKind.Null), "Incomplete native late-spawn callback.");
        string kind = row.GetProperty("Kind").GetString()!, ownerId = row.GetProperty("OwnerId").GetString()!, trigger = row.GetProperty("Trigger").GetString()!;
        TrainCombatState before = row.GetProperty("Before").Deserialize<TrainCombatState>()!;
        TrainCombatState actual = row.GetProperty("Actual").Deserialize<TrainCombatState>()!;
        string parent = Serialize(before); int actorId = row.GetProperty("ActorId").GetInt32();
        if (kind == "Test")
        {
            CombatUnit? actor = before.Rooms.SelectMany(room => room.Units).FirstOrDefault(unit => unit.Id == actorId);
            Require(RelicUnitSpawnEffectModel.Test(actor, trigger, ownerId) == row.GetProperty("Returned").GetBoolean(), "Native trigger test differs.");
            Equal(before, actual, "pure native trigger Test");
        }
        else
        {
            TrainCombatState state = before;
            string[] owners = kind == "Manager" ? before.Context!.Relics!.Where(owner => owner.EffectTypes.Contains(RelicUnitSpawnEffectModel.Effect))
                .Select(owner => owner.DataId).ToArray() : [ownerId];
            foreach (string id in owners)
            {
                TrainCombatResult result = RelicUnitSpawnEffectModel.Apply(state, actorId, trigger, kind == "Manager", id);
                Require(result.Supported, result.UnsupportedReason ?? "Unsupported callback."); state = result.State!;
            }
            Equal(state, actual, "independent full callback/RNG");
        }
        Require(parent == Serialize(before), "Callback branch mutated its parent.");
    }
    private static string Serialize(object? value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Equal(object? a, object? b, string label)
    {
        string? error = ModelJson.Difference(Serialize(a), Serialize(b));
        Require(error == null, label + ": " + error);
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
