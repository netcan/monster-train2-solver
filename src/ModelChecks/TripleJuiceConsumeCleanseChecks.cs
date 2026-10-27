using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TripleJuiceConsumeCleanseChecks
{
    internal const string CastReason = "Native triple-Juice Purifying Cleanse with ChaosScepter healing, Burnout and Consume.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, TripleJuicePurifyingCleanseChecks.Reason);
        Require(rows.Length == 1 && rows[0].GetProperty("IsEndTurn").GetBoolean(), "Missing natural first-EndTurn upgrade rejection.");
        var before = rows[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[0].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var card = before.Spawn.Train.Context!.FindCard(18)!;
        var rule = before.PlayRules!.Cards.Single(rule => rule.DataId == card.DataId);
        Require(before.Spawn.Turn == 0 && actual.Spawn.Turn == 1 &&
            card.DataId == "d4366b7c-588c-49aa-b2c6-2aeb30b0e01b" &&
            card.Permanent.Upgrades.Select(upgrade => upgrade.AssetKey).SequenceEqual(["TraitJuice3", "ChaosScepter"]) &&
            card.MaskDescriptor!.BaseTraits.Select(trait => trait.RuntimeType).SequenceEqual(["CardTraitJuice3", "CardTraitExhaustState"]) &&
            CardJuiceModel.Validate(card) == null && CardJuiceModel.Apply(card, 3) == 9 &&
            CardModifierModel.Resolve(rule, card).Effects[0].Value == 35 && CardExhaustModel.HasConsume(card),
            "Missing native triple-Juice/+20 magic power/Consume Cleanse signature.");
        using var keys = new BattleStateKeys();
        string parent = keys.Key(before);
        Parallel.For(0, 4, _ => {
            var result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Parallel upgraded triple-Juice EndTurn differs.");
        });
        Require(parent == keys.Key(before), "Upgraded triple-Juice branch changed its native root.");
        foreach (string control in new[] { "Owner", "Upgrade", "Heal", "Damage", "Cost", "CloneHeal", "OriginalHeal", "Trait", "TraitParameter", "Order", "Callback", "SourceTrait" })
        {
            var changed = JsonSerializer.SerializeToNode(card, ModelJson.Options)!;
            var upgrade = changed["Permanent"]!["Upgrades"]![1]!;
            if (control == "Owner") changed["DataId"] = "unknown-upgraded-cleanse";
            if (control == "Upgrade") upgrade["DataId"] = "unknown-scepter";
            if (control == "Heal") upgrade["Stats"]!["Heal"] = 21;
            if (control == "Damage") upgrade["Stats"]!["Damage"] = 21;
            if (control == "Cost") upgrade["Stats"]!["Cost"] = 1;
            if (control == "CloneHeal") upgrade["CloneHealBase"] = 21;
            if (control == "OriginalHeal") upgrade["Lifecycle"]!["OriginalHeal"] = 21;
            if (control == "Trait") changed["MaskDescriptor"]!["BaseTraits"]![1]!["RuntimeType"] = "UnknownConsume";
            if (control == "TraitParameter") upgrade["Lifecycle"]!["AddedTraits"]![0]!["ParamInt"] = 1;
            if (control == "Order") {
                var upgrades = changed["Permanent"]!["Upgrades"]!.AsArray();
                var first = upgrades[0]!.DeepClone(); var second = upgrades[1]!.DeepClone();
                upgrades.Clear(); upgrades.Add(second); upgrades.Add(first);
            }
            if (control == "Callback") upgrade["ExternalInteractions"]!.AsArray().Add("unknown-callback");
            if (control == "SourceTrait") changed["MaskDescriptor"]!["SourceTraits"]!.AsArray().Add(
                JsonSerializer.SerializeToNode(new { Type = "UnknownSource", ParamInt = 0 }, ModelJson.Options));
            Require(CardJuiceModel.Validate(changed.Deserialize<CardInstanceState>(ModelJson.Options)!) != null,
                "Unknown Consume/triple-Juice signature accepted: " + control);
        }
        if (replay.HasValue)
        {
            NativeBranchObservationChecks.Replay(replay.Value, TripleJuicePurifyingCleanseChecks.Reason);
            NativeBranchObservationChecks.Pair(source, replay.Value);
        }
        Console.WriteLine("TRIPLE-JUICE-CONSUME-CLEANSE PASS: original natural first-EndTurn guard, complete successor/forecast/RNG, heal35/Burnout9/Consume signature, four immutable branches and12 unknown-signature controls; independentPair=" + replay.HasValue);
    }

    internal static void Cast(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, CastReason);
        var last = rows[^1];
        var before = last.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = last.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = last.GetProperty("Action").Deserialize<PlayCardAction>()!;
        CombatUnit Target(BattleTurnState state) => state.Spawn.Train.Rooms.Single(room => room.RoomIndex == action.RoomIndex)
            .Units.Single(unit => unit.Id == action.TargetUnitId);
        var a = Target(before); var b = Target(actual);
        var card = before.Spawn.Train.Context!.FindCard(18)!;
        var played = actual.Spawn.Train.Context!.FindCard(18)!;
        var context = before.Spawn.Train.Context;
        var selected = CardSpellModel.WithContext(before.Spawn.Train,
            context.WithQueryFrame(context.QueryFrame!.With(selectedRoomIndex: action.RoomIndex)));
        var during = selected.Context!.FindCard(18)!;
        var resolved = CardModifierModel.Resolve(before.PlayRules!.Cards.Single(rule => rule.DataId == card.DataId), during);
        int heal = resolved.Effects[0].Value;
        Console.WriteLine($"CONSUME-CLEANSE-NATIVE inputs={rows.Length} turn={before.Spawn.Turn} target={a.Id} heal={heal} HP={a.Health}->{b.Health}/{a.MaxHealth} Burnout={a.Status("ephemeral")?.Stacks}->{b.Status("ephemeral")?.Stacks}");
        Require(before.Spawn.Turn > 0 && action.CardInstanceId == 18 &&
            card.DataId == "d4366b7c-588c-49aa-b2c6-2aeb30b0e01b" && CardJuiceModel.Validate(during) == null &&
            a.Health < a.MaxHealth && heal >= 35 && b.Health == Math.Min(a.MaxHealth, a.Health + heal) &&
            (b.Status("ephemeral")?.Stacks ?? 0) == (a.Status("ephemeral")?.Stacks ?? 0) + 9 &&
            played.PlayCount == 1 && played.Temporary.Upgrades.Count == 0 &&
            actual.Spawn.Train.Context.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(token => token.InstanceId == 18) &&
            actual.Spawn.Train.Context.Cards.Discard.All(token => token.InstanceId != 18),
            "Missing positive +20power/triple Burnout/Consume native cast or Conduit cleanup.");
        using var keys = new BattleStateKeys();
        string parent = keys.Key(before);
        Parallel.For(0, 4, _ => {
            var result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Parallel Consume/triple-Juice cast differs.");
        });
        Require(parent == keys.Key(before), "Consume/triple-Juice cast changed its native parent.");
        if (replay.HasValue)
        {
            NativeBranchObservationChecks.Replay(replay.Value, CastReason);
            NativeBranchObservationChecks.Pair(source, replay.Value, originalPredictionPresent: true, originalPredictionMatched: true);
        }
        Console.WriteLine("TRIPLE-JUICE-CONSUME-CLEANSE-CAST PASS: complete native prefix/successors/forecasts/RNG, positive healing/Burnout+9, Exhausted destination and four immutable casts; independentPair=" + replay.HasValue);
    }

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
