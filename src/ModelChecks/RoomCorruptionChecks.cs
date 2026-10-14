using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RoomCorruptionChecks
{
    private const string UnearthedId = "17fb5276-2311-44c9-b662-b73581d9af56";
    private const string EchoesId = "78996030-e8d1-4731-897e-ee4b809fa26c";
    private const string GlugsiderId = "e83182b1-ae94-4f7e-b7fe-aea5b825ecbe";
    private const string CarverId = "8cd475e4-6e39-4306-b544-026bf30f4eac";
    private const string KeeperId = "1acb9146-44b9-476c-a30e-5778750710a1";
    private const string CorruptionRelicId = "f302d4a1-13ba-4907-a26d-f0d8eead6f2e";
    private const string StewardId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";

    internal static void Run()
    {
        TrainCombatState Train(int current, int maximum, int permanent = 0, CombatRelicState? relic = null)
        {
            UnityRng rng = UnityRng.Seed(227);
            var context = new CombatContext(new CardCycleState([], [], [], rng, 0, []), rng, 0, 10, 10,
                roomCapacities: [new RoomCapacityState(0, 5, 5, false, current, [], maximum, permanent),
                    new RoomCapacityState(1, 5, 5, true, 0, [], 1, 0)],
                relics: relic == null ? [] : [relic]);
            return new TrainCombatState([new RoomCombatState(0, false, [], [], context),
                new RoomCombatState(1, false, [], [], context)], [], 1, context);
        }
        CardActionEffect Effect(string kind, int value, bool? add = null) =>
            new(kind, "Room", value, false, false, [], corruptionAdd: add);
        RoomCapacityState Room(CombatContext context) => context.RoomCapacities!.Single(room => room.RoomIndex == 0);
        TrainCombatState source = Train(2, 4, 1);
        string parent = JsonSerializer.Serialize(source);
        TrainSpellResult paired = CardSpellModel.Apply(source, 0,
            [Effect("AdjustMaxCorruption", 1), Effect("AdjustCorruption", 1, true)], 0);
        Require(paired.Supported && Room(paired.State!.Context!).Corruption == 3 &&
            Room(paired.State.Context!).MaxCorruption == 5 && Room(paired.State.Context!).PermanentCorruption == 1,
            "Maximum and current corruption did not settle in native effect order: " + paired.UnsupportedReason);
        RoomCorruptionResult filled = RoomCorruptionModel.Apply(source, 0, Effect("AdjustCorruption", -1, true));
        Require(filled.Supported && Room(filled.Context!).Corruption == 4,
            "Negative add amount did not fill to the current maximum.");
        RoomCorruptionResult overMaximum = RoomCorruptionModel.Apply(Train(6, 4), 0, Effect("AdjustCorruption", -1, true));
        Require(overMaximum.Supported && Room(overMaximum.Context!).Corruption == 6,
            "Fill-to-max incorrectly reduced corruption already above maximum.");
        RoomCorruptionResult removed = RoomCorruptionModel.Apply(source, 0, Effect("AdjustCorruption", -1, false));
        Require(removed.Supported && Room(removed.Context!).Corruption == 1 &&
            RoomCorruptionModel.Test(source.Rooms[0], Effect("AdjustCorruption", -1, false)) &&
            !RoomCorruptionModel.Test(source.Rooms[0], Effect("AdjustCorruption", 3, false)),
            "Remove-all ignored the permanent floor or native cast test.");
        RoomCorruptionResult beyondMax = RoomCorruptionModel.Apply(source, 0, Effect("AdjustCorruption", 8, true));
        Require(beyondMax.Supported && Room(beyondMax.Context!).Corruption == 10 && Room(beyondMax.Context!).MaxCorruption == 4 &&
            beyondMax.Additions.Count == 1 && beyondMax.Additions[0].RoomIndex == 0 && beyondMax.Additions[0].Count == 8,
            "Fixed corruption addition was incorrectly capped by the maximum.");
        RoomCorruptionResult permanent = RoomCorruptionModel.Apply(source, 0, Effect("AddPermanentCorruption", 1));
        Require(permanent.Supported && Room(permanent.Context!).PermanentCorruption == 2 &&
            Room(permanent.Context!).Corruption == 3 && Room(permanent.Context!).MaxCorruption == 4,
            "Permanent corruption did not update its floor before adding one current corruption.");
        RoomCorruptionResult cappedPermanent = RoomCorruptionModel.Apply(Train(2, 2, 2), 0,
            Effect("AddPermanentCorruption", 3));
        Require(cappedPermanent.Supported && Room(cappedPermanent.Context!).PermanentCorruption == 2 &&
            Room(cappedPermanent.Context!).Corruption == 3,
            "Permanent corruption exceeded the native room maximum.");
        var callback = new CombatRelicState("callback", "callback", ["RelicEffectDrawOnCorruptionAdded"]);
        RoomCorruptionResult rejected = RoomCorruptionModel.Apply(Train(0, 4, relic: callback), 0,
            Effect("AdjustCorruption", 1, true));
        Require(!rejected.Supported && rejected.UnsupportedReason!.Contains("relic callbacks", StringComparison.Ordinal) &&
            JsonSerializer.Serialize(source) == parent,
            "Unmodeled corruption callback was accepted or a parent branch changed.");
        RoomCorruptionResult rejectedPermanent = RoomCorruptionModel.Apply(Train(0, 4, relic: callback), 0,
            Effect("AddPermanentCorruption", 1));
        Require(!rejectedPermanent.Supported && rejectedPermanent.UnsupportedReason!.Contains("relic callbacks", StringComparison.Ordinal),
            "Permanent corruption skipped the native corruption-added callback guard.");
        var missingCorruptionRule = new CombatRelicState("missing", "missing",
            ["RelicEffectAddStatusEffectOnCorruptionAdded"]);
        Require(RelicModel.Validate([missingCorruptionRule])?.Contains("Missing ordered native", StringComparison.Ordinal) == true,
            "Uncaptured corruption relic parameters were accepted.");
        var statusRule = new RelicCorruptionStatus(0, "RandomInRoom", false, true, true, false, false,
            [new CombatStatus("corrupt_poison", 1)]);
        var excluded = new CombatUnit(1, "excluded", CombatTeam.Enemy, 0, 1, 1, false, false, false,
            [new CombatStatus("pyrebane", 1), new CombatStatus("lifelink", 1)]);
        var eligible = new CombatUnit(2, "eligible", CombatTeam.Enemy, 0, 2, 2, false, false, false, []);
        RoomCombatState targetRoom = Train(0, 4).Rooms[0].WithUnits([excluded, eligible]);
        string targetParent = JsonSerializer.Serialize(targetRoom);
        CardTargets selected = RelicCorruptionStatusModel.Collect(targetRoom, statusRule);
        Require(selected.Supported && selected.UnitIds.SequenceEqual([eligible.Id]) &&
            selected.BattleRng.HasValue && JsonSerializer.Serialize(targetRoom) == targetParent,
            "Corruption relic RandomInRoom selected the native pyrebane/lifelink exclusion or mutated its parent.");
        string expected = JsonSerializer.Serialize(paired.State);
        Parallel.For(0, 16, _ => Require(JsonSerializer.Serialize(CardSpellModel.Apply(source, 0,
            [Effect("AdjustMaxCorruption", 1), Effect("AdjustCorruption", 1, true)], 0).State) == expected,
            "Parallel corruption branches diverged."));
        Console.WriteLine("ROOM-CORRUPTION-CHECKS PASS: max/current order, fill, remove, permanent floor, callback guard and branches.");
    }

    internal static void Native(FixtureValue fixture)
    {
        Require(fixture.GetProperty("Schema").GetInt32() == 103 &&
            fixture.GetProperty("ModifierScenario").GetString() == "corruption" &&
            fixture.GetProperty("NativeWon").GetBoolean() &&
            fixture.GetProperty("CaptureFailures").GetInt32() == 0 &&
            fixture.GetProperty("Mismatches").GetInt32() == 0 &&
            fixture.GetProperty("Unsupported").GetInt32() == 0 &&
            fixture.GetProperty("Pending").GetInt32() == 0,
            "Native corruption battle did not finish with a complete match.");
        FixtureValue[] actions = fixture.GetProperty("Actions").EnumerateArray()
            .Where(record => record.GetProperty("ScenarioAction").GetBoolean()).ToArray();
        Require(actions.Length == 2, "Expected original increase and removal actions.");
        for (int index = 0; index < actions.Length; index++)
        {
            BattleTurnState before = actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
            BattleTurnState actual = actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            PlayCardAction action = actions[index].GetProperty("Action").Deserialize<PlayCardAction>()!;
            string expectedId = index == 0 ? UnearthedId : EchoesId;
            CardPlayRule rule = before.PlayRules!.Cards.Single(card => card.DataId == expectedId);
            RoomCapacityState oldRoom = before.Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 0);
            RoomCapacityState newRoom = actual.Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 0);
            CardActionEffect corruption = rule.Effects[1];
            Require(before.Spawn.Train.Context.FindCard(action.CardInstanceId)?.DataId == expectedId &&
                rule.Effects.Count == 2 && action.RoomIndex == 0 && corruption.Type == "AdjustCorruption" &&
                corruption.Value == (index == 0 ? 1 : -1) && corruption.CorruptionAdd == (index == 0) &&
                oldRoom.Corruption == index && oldRoom.MaxCorruption == 4 + index &&
                newRoom.Corruption == 1 - index && newRoom.MaxCorruption == 5 &&
                oldRoom.PermanentCorruption == 0 && newRoom.PermanentCorruption == 0 &&
                (index != 0 || rule.Effects[0].Type == "AdjustMaxCorruption" && rule.Effects[0].Value == 1) &&
                (index != 1 || rule.Effects[0].Type == "UnitUpgrade" && action.TargetUnitId > 0),
                "Marked action does not match the original ordered corruption effects.");
            if (index == 1)
            {
                CombatUnit oldTarget = before.Spawn.Train.Rooms.SelectMany(room => room.Units)
                    .Single(unit => unit.Id == action.TargetUnitId);
                CombatUnit newTarget = actual.Spawn.Train.Rooms.SelectMany(room => room.Units)
                    .Single(unit => unit.Id == action.TargetUnitId);
                CardUpgradeModifier applied = newTarget.Modifiers!.Upgrades.Single();
                Require(oldTarget.AssetKey.StartsWith("TrainSteward", StringComparison.Ordinal) &&
                    newTarget.BaseAttack == oldTarget.BaseAttack + 8 && applied.Stats.Damage == 8 &&
                    applied.Lifecycle?.Values.Stats.Damage == 8,
                    "Echoes did not scale its stored upgrade from pre-removal corruption.");
            }
            BattleActionResult predicted = BattleActionModel.PlayCard(before, action);
            string? difference = predicted.State == null ? predicted.Reason :
                ModelJson.Difference(BattleTurnChecks.Comparable(predicted.State), BattleTurnChecks.Comparable(actual));
            Require(predicted.Supported && difference == null,
                "Original corruption action differs from the native replay: " + difference);
        }
        Console.WriteLine("NATIVE-ROOM-CORRUPTION-CHECKS PASS: ordered increase and corruption-scaled removal in a complete battle.");
    }

    internal static void NativeUnitTrigger(FixtureValue fixture)
    {
        Require(fixture.GetProperty("ModifierScenario").GetString() == "corruption-unit-trigger" &&
            fixture.GetProperty("NativeWon").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }
                .All(name => fixture.GetProperty(name).GetInt32() == 0),
            "Original Glugsider battle did not finish with a complete native match.");
        FixtureValue[] actions = fixture.GetProperty("Actions").EnumerateArray().ToArray();
        Require(actions.Length > 0 && fixture.GetProperty("Stages").GetArrayLength() > 0,
            "Glugsider battle has no recorded decisions or stages.");
        FixtureValue? witness = actions.Cast<FixtureValue?>().SingleOrDefault(record =>
            record.Value.GetProperty("Before").Deserialize<BattleTurnState>()!.Spawn.Train.Context!
                .FindCard(record.Value.GetProperty("Action").Deserialize<PlayCardAction>()!.CardInstanceId)?.DataId == UnearthedId);
        Require(witness.HasValue, "No original Unearthed Remains corruption callback action was recorded.");
        BattleTurnState before = witness.Value.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = witness.Value.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction action = witness.Value.GetProperty("Action").Deserialize<PlayCardAction>()!;
        RoomCombatState oldRoom = before.Spawn.Train.Rooms.Single(room => room.RoomIndex == action.RoomIndex);
        RoomCombatState newRoom = actual.Spawn.Train.Rooms.Single(room => room.RoomIndex == action.RoomIndex);
        CombatUnit oldUnit = oldRoom.Units.Single(unit => unit.AssetKey == "MonsterGlugsider");
        CombatUnit newUnit = newRoom.Units.Single(unit => unit.Id == oldUnit.Id);
        CombatTrigger trigger = oldUnit.Triggers.Single(item => item.Kind == "CorruptionAdded");
        CardPlayRule spell = before.PlayRules!.Cards.Single(rule => rule.DataId == UnearthedId);
        RoomCapacityState oldCapacity = before.Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == action.RoomIndex);
        RoomCapacityState newCapacity = actual.Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == action.RoomIndex);
        AbilityCardResult ability = AbilityCardModel.Get(before.Spawn.Train.Context, oldUnit.Ability!.CardCreation!);
        Require(before.Spawn.Train.Context.FindCard(oldUnit.SpawnerCardId)?.DataId == GlugsiderId &&
            action.RoomIndex == 0 && spell.Effects.Count == 2 &&
            spell.Effects[0].Type == "AdjustMaxCorruption" && spell.Effects[0].Value == 1 &&
            spell.Effects[1].Type == "AdjustCorruption" && spell.Effects[1].Value == 1 &&
            trigger.Effects.Count == 1 && trigger.Effects[0].Type == "CardEffectHeal" && trigger.Effects[0].Value == 10 &&
            oldUnit.Health > 0 && oldUnit.Health < oldUnit.MaxHealth &&
            newUnit.Health == Math.Min(oldUnit.MaxHealth, oldUnit.Health + 10) &&
            oldCapacity.Corruption == 0 && newCapacity.Corruption == 1 &&
            newCapacity.MaxCorruption == oldCapacity.MaxCorruption + 1 &&
            ability.Supported && ability.Card!.MaskDescriptor?.BaseTraits.Any(trait =>
                trait.RuntimeType == "CardTraitCorruptRestricted" && trait.ParamInt == 4) == true,
            "Recorded action lacks the original wounded Glugsider healing witness.");
        PlayCardAction activation = new(ability.Card!.InstanceId, action.RoomIndex,
            activatorUnitId: oldUnit.Id);
        BattleActionResult rejected = UnitAbilityModel.Activate(before, activation);
        Require(!rejected.Supported &&
            !BattleActionModel.EnumerateActions(before).SupportedCandidates.Any(candidate =>
                candidate.Action.ActivatorUnitId == oldUnit.Id),
            "Corrupt Restricted ability became an executable search branch.");
        BattleActionResult replay = BattleActionModel.PlayCard(before, action);
        Require(replay.Supported && ModelJson.Difference(BattleTurnChecks.Comparable(replay.State!),
            BattleTurnChecks.Comparable(actual)) == null,
            "Original corruption-trigger heal differs from the native action.");
        BattleTurnChecks.Native(fixture);
        BattleSearchChecks.Native(fixture);
        Console.WriteLine("NATIVE-CORRUPTION-UNIT-TRIGGER PASS: original Glugsider healed on corruption addition; ability guarded.");
    }

    internal static void NativeCarver(FixtureValue fixture)
    {
        Require(fixture.GetProperty("Schema").GetInt32() == 103 &&
            fixture.GetProperty("ModifierScenario").GetString() == "corruption-carver-trigger" &&
            fixture.GetProperty("NativeWon").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }
                .All(name => fixture.GetProperty(name).GetInt32() == 0),
            "Original Shardsoul Carver battle did not finish with a complete native match.");
        FixtureValue[] marked = fixture.GetProperty("Actions").EnumerateArray()
            .Where(record => record.GetProperty("ScenarioAction").GetBoolean()).ToArray();
        Require(marked.Length == 2, "Expected original corruption addition and removal actions.");
        BattleTurnState before = marked[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = marked[0].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction action = marked[0].GetProperty("Action").Deserialize<PlayCardAction>()!;
        CombatUnit oldUnit = before.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units
            .Single(unit => unit.AssetKey == "MonsterShardsoulCarver");
        CombatUnit newUnit = actual.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units
            .Single(unit => unit.Id == oldUnit.Id);
        CombatTrigger callback = oldUnit.Triggers.Single(trigger => trigger.Kind == "CorruptionAdded");
        RoomCapacityState oldCapacity = before.Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 0);
        RoomCapacityState newCapacity = actual.Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 0);
        CardUpgradeModifier[] added = newUnit.Modifiers!.Upgrades
            .Where(upgrade => oldUnit.Modifiers!.Upgrades.All(existing => existing.DataId != upgrade.DataId)).ToArray();
        Require(action.RoomIndex == 0 &&
            before.Spawn.Train.Context.FindCard(action.CardInstanceId)?.DataId == UnearthedId &&
            before.Spawn.Train.Context.FindCard(oldUnit.SpawnerCardId)?.DataId == CarverId &&
            callback.Effects.Count == 2 &&
            callback.Effects[0].Type == "CardEffectAddTempCardUpgradeToUnits" &&
            callback.Effects[1].Type == "CardEffectAddStatusEffect" &&
            oldUnit.BaseAttack == 20 && newUnit.BaseAttack == 23 &&
            newUnit.Status("armor")?.Stacks == (oldUnit.Status("armor")?.Stacks ?? 0) + 3 &&
            added.Length == 1 && added[0].Stats.Damage == 3 && added[0].Lifecycle != null &&
            oldCapacity.Corruption == 0 && newCapacity.Corruption == 1 &&
            oldCapacity.MaxCorruption == 4 && newCapacity.MaxCorruption == 5,
            "Original Shardsoul Carver lacks a positive upgrade and armor callback witness: " +
            $"attack={oldUnit.BaseAttack}->{newUnit.BaseAttack}, armor={oldUnit.Status("armor")?.Stacks}->{newUnit.Status("armor")?.Stacks}, " +
            $"upgrades={oldUnit.Modifiers?.Upgrades.Count}->{newUnit.Modifiers?.Upgrades.Count}, added={added.Length}.");
        BattleActionResult replay = BattleActionModel.PlayCard(before, action);
        Require(replay.Supported && ModelJson.Difference(BattleTurnChecks.Comparable(replay.State!),
            BattleTurnChecks.Comparable(actual)) == null,
            "Original Shardsoul Carver corruption callback differs from the native action.");
        BattleTurnState removeBefore = marked[1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState removeAfter = marked[1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction removeAction = marked[1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        CombatUnit beforeRemoval = removeBefore.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units
            .Single(unit => unit.Id == oldUnit.Id);
        CombatUnit afterRemoval = removeAfter.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units
            .Single(unit => unit.Id == oldUnit.Id);
        Require(removeBefore.Spawn.Train.Context!.FindCard(removeAction.CardInstanceId)?.DataId == EchoesId &&
            removeBefore.Spawn.Train.Context.RoomCapacities!.Single(room => room.RoomIndex == 0).Corruption == 1 &&
            removeAfter.Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 0).Corruption == 0 &&
            afterRemoval.Status("armor")?.Stacks == beforeRemoval.Status("armor")?.Stacks,
            "Removing corruption unexpectedly fired the Carver status callback.");
        BattleTurnChecks.Native(fixture);
        BattleSearchChecks.Native(fixture);
        Console.WriteLine("NATIVE-CORRUPTION-CARVER-TRIGGER PASS: original temporary +3 attack and +3 armor callback.");
    }

    internal static void NativeKeeper(FixtureValue fixture)
    {
        Require(fixture.GetProperty("Schema").GetInt32() == 103 &&
            fixture.GetProperty("ModifierScenario").GetString() == "corruption-keeper-trigger" &&
            fixture.GetProperty("NativeWon").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }
                .All(name => fixture.GetProperty(name).GetInt32() == 0),
            "Original Keeper of Echoes battle did not finish with a complete native match.");
        FixtureValue[] marked = fixture.GetProperty("Actions").EnumerateArray()
            .Where(record => record.GetProperty("ScenarioAction").GetBoolean()).ToArray();
        Require(marked.Length == 2, "Expected original corruption addition and removal actions.");
        BattleTurnState before = marked[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = marked[0].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction action = marked[0].GetProperty("Action").Deserialize<PlayCardAction>()!;
        CombatUnit[] oldUnits = before.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units
            .Where(unit => unit.AssetKey == "MonsterKeeperOfEchoes" ||
                unit.AssetKey.StartsWith("TrainSteward", StringComparison.Ordinal)).ToArray();
        CombatUnit[] newUnits = actual.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units
            .Where(unit => oldUnits.Any(old => old.Id == unit.Id)).ToArray();
        CombatUnit keeper = oldUnits.Single(unit => unit.AssetKey == "MonsterKeeperOfEchoes");
        CombatUnit teammate = oldUnits.Single(unit => unit.AssetKey.StartsWith("TrainSteward", StringComparison.Ordinal));
        CombatTrigger callback = keeper.Triggers.Single(trigger => trigger.Kind == "CorruptionAdded");
        CardActionEffect? upgradeEffect = callback.Effects.Single().UnitUpgrade;
        string? upgradeId = upgradeEffect?.Upgrade?.DataId;
        RoomCapacityState oldCapacity = before.Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 0);
        RoomCapacityState newCapacity = actual.Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 0);
        Require(action.RoomIndex == 0 && oldUnits.Length == 2 && newUnits.Length == 2 &&
            before.Spawn.Train.Context.FindCard(action.CardInstanceId)?.DataId == UnearthedId &&
            before.Spawn.Train.Context.FindCard(keeper.SpawnerCardId)?.DataId == KeeperId &&
            before.Spawn.Train.Context.FindCard(teammate.SpawnerCardId)?.DataId == StewardId &&
            callback.Effects[0].Type == "CardEffectAddTempCardUpgradeToUnits" &&
            upgradeEffect?.Target == "Room" && upgradeId is { Length: > 0 } &&
            oldUnits.All(unit => unit.Modifiers!.Upgrades.All(upgrade => upgrade.DataId != upgradeId)) &&
            newUnits.All(unit => unit.Modifiers!.Upgrades.Any(upgrade =>
                upgrade.DataId == upgradeId && upgrade.Lifecycle != null)) &&
            oldCapacity.Corruption == 0 && newCapacity.Corruption == 1 &&
            oldCapacity.MaxCorruption == 4 && newCapacity.MaxCorruption == 5,
            "Original Keeper callback did not give its room-wide temporary upgrade to both units: " +
            $"upgrade={upgradeId}, units={oldUnits.Length}->{newUnits.Length}, " +
            $"owned={string.Join(',', newUnits.Select(unit => unit.AssetKey + ':' +
                string.Join('/', unit.Modifiers?.Upgrades.Select(upgrade => upgrade.DataId) ?? Array.Empty<string>())))}.");
        BattleTurnState removeBefore = marked[1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState removeAfter = marked[1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction removeAction = marked[1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        Require(removeBefore.Spawn.Train.Context!.FindCard(removeAction.CardInstanceId)?.DataId == EchoesId &&
            removeBefore.Spawn.Train.Context.RoomCapacities!.Single(room => room.RoomIndex == 0).Corruption == 1 &&
            removeAfter.Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 0).Corruption == 0 &&
            newUnits.All(unit =>
            {
                CombatUnit old = removeBefore.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units.Single(actor => actor.Id == unit.Id);
                CombatUnit next = removeAfter.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units.Single(actor => actor.Id == unit.Id);
                return old.Modifiers!.Upgrades.Count(upgrade => upgrade.DataId == upgradeId) ==
                    next.Modifiers!.Upgrades.Count(upgrade => upgrade.DataId == upgradeId);
            }),
            "Removing corruption unexpectedly repeated the Keeper room-wide callback.");
        BattleActionResult replay = BattleActionModel.PlayCard(before, action);
        Require(replay.Supported && ModelJson.Difference(BattleTurnChecks.Comparable(replay.State!),
            BattleTurnChecks.Comparable(actual)) == null,
            "Original Keeper room-wide corruption callback differs from the native action.");
        BattleTurnChecks.Native(fixture);
        BattleSearchChecks.Native(fixture);
        Console.WriteLine("NATIVE-CORRUPTION-KEEPER-TRIGGER PASS: two original units received the same temporary room upgrade.");
    }

    internal static void NativeRelicTrigger(FixtureValue fixture)
    {
        Require(fixture.GetProperty("Schema").GetInt32() == 103 &&
            fixture.GetProperty("ModifierScenario").GetString() == "corruption-relic-trigger" &&
            fixture.GetProperty("NativeWon").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }
                .All(name => fixture.GetProperty(name).GetInt32() == 0),
            "Original DamageOnCorruptPlay battle did not finish with a complete native match.");
        FixtureValue[] marked = fixture.GetProperty("Actions").EnumerateArray()
            .Where(record => record.GetProperty("ScenarioAction").GetBoolean()).ToArray();
        Require(marked.Length == 2, "Expected original corruption addition and removal actions.");
        BattleTurnState before = marked[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = marked[0].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction action = marked[0].GetProperty("Action").Deserialize<PlayCardAction>()!;
        CombatRelicState relic = before.Spawn.Train.Context!.Relics!.Single(item => item.DataId == CorruptionRelicId);
        RelicCorruptionStatus[] effects = relic.CorruptionStatuses!.ToArray();
        RoomCombatState oldRoom = before.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0);
        RoomCombatState newRoom = actual.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0);
        CombatUnit[] oldEnemies = oldRoom.Units.Where(unit => unit.Team == CombatTeam.Enemy).ToArray();
        CombatUnit[] newEnemies = newRoom.Units.Where(unit => unit.Team == CombatTeam.Enemy).ToArray();
        int OldStacks(CombatUnit unit) => unit.Status("corrupt_poison")?.Stacks ?? 0;
        Require(action.RoomIndex == 0 &&
            before.Spawn.Train.Context.FindCard(action.CardInstanceId)?.DataId == UnearthedId &&
            relic.AssetKey == "DamageOnCorruptPlay" && effects.Length == 2 &&
            effects.All(effect => effect.TargetMode == "RandomInRoom" && effect.TargetHeroes &&
                !effect.TargetMonsters && effect.OnlyDuringMonsterTurn && !effect.HasConditions &&
                effect.Statuses.Count == 1 && effect.Statuses[0].Id == "corrupt_poison" &&
                effect.Statuses[0].Stacks == 1) &&
            oldEnemies.Length >= 2 && newEnemies.Length == oldEnemies.Length &&
            newEnemies.Sum(OldStacks) == oldEnemies.Sum(OldStacks) + 2 &&
            before.Spawn.Train.Context.RoomCapacities!.Single(room => room.RoomIndex == 0).Corruption == 0 &&
            actual.Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 0).Corruption == 1,
            "Original relic did not add exactly two corrupt_poison stacks to enemies before room corruption changed.");
        BattleTurnState removeBefore = marked[1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState removeAfter = marked[1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction removeAction = marked[1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        Require(removeBefore.Spawn.Train.Context!.FindCard(removeAction.CardInstanceId)?.DataId == EchoesId &&
            removeBefore.Spawn.Train.Context.RoomCapacities!.Single(room => room.RoomIndex == 0).Corruption == 1 &&
            removeAfter.Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 0).Corruption == 0 &&
            removeAfter.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units
                .Where(unit => unit.Team == CombatTeam.Enemy).Sum(OldStacks) ==
            removeBefore.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units
                .Where(unit => unit.Team == CombatTeam.Enemy).Sum(OldStacks),
            "Removing corruption unexpectedly repeated the relic callback.");
        BattleActionResult replay = BattleActionModel.PlayCard(before, action);
        Require(replay.Supported && ModelJson.Difference(BattleTurnChecks.Comparable(replay.State!),
            BattleTurnChecks.Comparable(actual)) == null,
            "Original random corruption relic callback differs from native action: " + replay.Reason);
        BattleTurnChecks.Native(fixture);
        BattleSearchChecks.Native(fixture);
        Console.WriteLine("NATIVE-CORRUPTION-RELIC-TRIGGER PASS: two original random enemy statuses and complete battle replay.");
    }

    internal static void NativePermanent(FixtureValue fixture)
    {
        Require(fixture.GetProperty("Schema").GetInt32() == 103 &&
            fixture.GetProperty("ModifierScenario").GetString() == "permanent-corruption" &&
            fixture.GetProperty("NativeWon").GetBoolean() &&
            fixture.GetProperty("CaptureFailures").GetInt32() == 0 &&
            fixture.GetProperty("Mismatches").GetInt32() == 0 &&
            fixture.GetProperty("Unsupported").GetInt32() == 0 &&
            fixture.GetProperty("Pending").GetInt32() == 0,
            "Native Eternal Kinstone battle did not finish with a complete match.");
        FixtureValue[] actions = fixture.GetProperty("Actions").EnumerateArray()
            .Where(record => record.GetProperty("ScenarioAction").GetBoolean()).ToArray();
        Require(actions.Length == 1, "Expected one original Eternal Kinstone action.");
        BattleTurnState before = actions[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = actions[0].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction action = actions[0].GetProperty("Action").Deserialize<PlayCardAction>()!;
        CardPlayRule rule = before.PlayRules!.Cards.Single(card => card.DataId == "c354dd2c-0087-4eae-82d9-d4ab17b15f19");
        RoomCapacityState oldRoom = before.Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 0);
        RoomCapacityState newRoom = actual.Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 0);
        BattleActionResult predicted = BattleActionModel.PlayCard(before, action);
        string? difference = predicted.State == null ? predicted.Reason :
            ModelJson.Difference(BattleTurnChecks.Comparable(predicted.State), BattleTurnChecks.Comparable(actual));
        Require(before.Spawn.Train.Context.FindCard(action.CardInstanceId)?.DataId == rule.DataId &&
            rule.Effects.Count == 1 && rule.Effects[0].Type == "AddPermanentCorruption" &&
            rule.Effects[0].Value == 1 && action.RoomIndex == 0 &&
            newRoom.MaxCorruption == oldRoom.MaxCorruption &&
            newRoom.PermanentCorruption == Math.Min(oldRoom.PermanentCorruption!.Value + 1, oldRoom.MaxCorruption!.Value) &&
            newRoom.Corruption == Math.Max(oldRoom.Corruption!.Value + 1, newRoom.PermanentCorruption!.Value) &&
            predicted.Supported && difference == null,
            "Original Eternal Kinstone differs from the native replay: " + difference);
        Console.WriteLine("NATIVE-PERMANENT-CORRUPTION-CHECKS PASS: original room floor and current corruption in a complete battle.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
