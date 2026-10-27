using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TripleJuicePurifyingCleanseChecks
{
    internal const string Reason = "Triple Juice with this card or ordered scaling traits requires native calibration.";
    internal const string CastReason = "Native triple-Juice Purifying Cleanse healing and Burnout application.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason);
        Require(rows.Length == 1 && rows[0].GetProperty("IsEndTurn").GetBoolean(), "Missing original deployment/first-EndTurn rejection.");
        var before = rows[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var after = rows[0].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(before.Spawn.Turn == 0 && after.Spawn.Turn == 1, "Wrong native triple-Juice boundary.");
        var card = before.Spawn.Train.Context!.FindCard(8)!;
        Require(card.DataId == "d4366b7c-588c-49aa-b2c6-2aeb30b0e01b" && CardJuiceModel.Validate(card) == null &&
            CardJuiceModel.Apply(card, 3) == 9, "Missing natural Purifying Cleanse triple upgrade.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            var result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(after),
                "Parallel triple-Juice deployment branch differs.");
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Triple-Juice check changed its parent.");
        foreach (string control in new[] { "Owner", "Multiplier", "DefinitionMultiplier", "Upgrade", "Damage", "Scaling", "MissingMask", "Temporary" })
        {
            var changed = JsonSerializer.SerializeToNode(card, ModelJson.Options)!;
            if (control == "Owner") changed["DataId"] = "unknown-cleanse";
            if (control == "Multiplier") changed["MaskDescriptor"]!["BaseTraits"]![0]!["ParamInt"] = 4;
            if (control == "DefinitionMultiplier") changed["MaskDescriptor"]!["BaseTraits"]![0]!["DefinitionParamInt"] = 4;
            if (control == "Upgrade") changed["Permanent"]!["Upgrades"]![0]!["AssetKey"] = "OtherJuice";
            if (control == "Damage") changed["Permanent"]!["Upgrades"]![0]!["Stats"]!["Damage"] = 1;
            if (control == "Scaling") changed["StatusScalingTraits"] = JsonSerializer.SerializeToNode(new[] {
                new ScalingStatusTrait(new CardStatisticQuery("PlayedCost", "ThisBattle"), 1, false, 0, ["ephemeral"]) }, ModelJson.Options);
            if (control == "MissingMask") changed["MaskDescriptor"] = null;
            if (control == "Temporary") changed["Temporary"]!["Upgrades"]!.AsArray().Add(changed["Permanent"]!["Upgrades"]![0]!.DeepClone());
            Require(CardJuiceModel.Validate(changed.Deserialize<CardInstanceState>(ModelJson.Options)!) != null,
                "Unknown Purifying Cleanse triple signature accepted: " + control);
        }
        if (replay.HasValue)
        {
            NativeBranchObservationChecks.Replay(replay.Value, Reason);
            NativeBranchObservationChecks.Pair(source, replay.Value);
        }
        Console.WriteLine("TRIPLE-JUICE-PURIFYING PASS: original first-EndTurn guard, complete native successor/forecast/RNG, four immutable branches and eight signature controls; independentPair=" + replay.HasValue);
    }

    internal static void Cast(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, CastReason, historicalUnsupportedReason: Reason);
        Require(rows.Length == 18, "Missing17 unchanged natural inputs before the positive healing cast.");
        var last = rows[^1];
        var before = last.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var after = last.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = last.GetProperty("Action").Deserialize<PlayCardAction>()!;
        CombatUnit Target(BattleTurnState state) => state.Spawn.Train.Rooms.Single(room => room.RoomIndex == action.RoomIndex)
            .Units.Single(unit => unit.Id == action.TargetUnitId);
        var a = Target(before); var b = Target(after);
        var context = before.Spawn.Train.Context!;
        var selected = CardSpellModel.WithContext(before.Spawn.Train,
            context.WithQueryFrame(context.QueryFrame!.With(selectedRoomIndex: action.RoomIndex)));
        var during = selected.Context!.FindCard(8)!;
        var conduit = during.Temporary.Upgrades.Single();
        var played = after.Spawn.Train.Context!.FindCard(8)!;
        Console.WriteLine($"PURIFYING-NATIVE turn={before.Spawn.Turn} card={action.CardInstanceId} room={action.RoomIndex} target={action.TargetUnitId} HP={a.Health}->{b.Health}/{a.MaxHealth} Burnout={a.Status("ephemeral")?.Stacks}->{b.Status("ephemeral")?.Stacks} Conduit={conduit.Stats.Heal}");
        Require(before.Spawn.Turn == 3 && action is { CardInstanceId: 8, RoomIndex: 2, TargetUnitId: 3 } &&
            a.Health == 33 && a.MaxHealth == 65 && b.Health == 56 &&
            (b.Status("ephemeral")?.Stacks ?? 0) - (a.Status("ephemeral")?.Stacks ?? 0) == 9 &&
            conduit.Stats is { Damage: 8, Heal: 8 } && CardJuiceModel.Validate(during) == null &&
            played.PlayCount == 1 && played.Temporary.Upgrades.Count == 0 &&
            after.Spawn.Train.Context.Cards.Discard.Any(card => card.InstanceId == 8),
            "Native heal15+Conduit8, triple Burnout3->9 or ordinary discard differs.");
        foreach (var sample in source.GetProperty("TripleJuiceCalibration").EnumerateArray())
            Require(CardJuiceModel.Apply(during, sample.GetProperty("Input").GetInt32()) ==
                unchecked(sample.GetProperty("Input").GetInt32() + sample.GetProperty("Bonus").GetInt32()),
                "Native signed/wrapped triple callback differs.");
        Require(source.GetProperty("TripleJuiceCalibration").GetArrayLength() == 6, "Missing native triple callback limits.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            var result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(after),
                "Parallel positive triple-Juice cast differs.");
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Triple-Juice cast mutated its parent.");
        foreach (string control in new[] { "Identity", "Heal", "Cost", "Lifecycle", "Trait", "Duplicate", "Callback" })
        {
            var changed = JsonSerializer.SerializeToNode(during, ModelJson.Options)!;
            var upgrade = changed["Temporary"]!["Upgrades"]![0]!;
            if (control == "Identity") upgrade["DataId"] = "unknown-conduit";
            if (control == "Heal") upgrade["Stats"]!["Heal"] = 6;
            if (control == "Cost") upgrade["Stats"]!["Cost"] = 1;
            if (control == "Lifecycle") upgrade["Lifecycle"] = null;
            if (control == "Trait") upgrade["Lifecycle"]!["AddedTraits"]!.AsArray().Add(changed["Permanent"]!["Upgrades"]![0]!["Lifecycle"]!["AddedTraits"]![0]!.DeepClone());
            if (control == "Duplicate") changed["Temporary"]!["Upgrades"]!.AsArray().Add(upgrade.DeepClone());
            if (control == "Callback") upgrade["ExternalInteractions"]!.AsArray().Add("unknown-callback");
            Require(CardJuiceModel.Validate(changed.Deserialize<CardInstanceState>(ModelJson.Options)!) != null,
                "Unknown Purifying Cleanse Conduit combination accepted: " + control);
        }
        if (replay.HasValue)
        {
            NativeBranchObservationChecks.Replay(replay.Value, CastReason);
            NativeBranchObservationChecks.Pair(source, replay.Value);
        }
        Console.WriteLine("TRIPLE-JUICE-PURIFYING-CAST PASS:18 native inputs/full state/forecast/RNG, positive heal15+Conduit8 and Burnout+9; six native callback limits, four immutable branches/seven Conduit guards; independentPair=" + replay.HasValue);
    }

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
