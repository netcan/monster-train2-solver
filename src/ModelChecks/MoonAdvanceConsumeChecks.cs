using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class MoonAdvanceConsumeChecks
{
    internal const string Reason = "Moon advance scaling requires one unchanged base trait and captured source parameters.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason);
        Require(rows.Length == 44 && !rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Wrong natural Lunacy/Consume boundary.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        var context = before.Spawn.Train.Context!;
        var card = context.FindCard(9)!;
        var played = actual.Spawn.Train.Context!.FindCard(9)!;
        var oldTarget = before.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == action.TargetUnitId);
        var target = actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == action.TargetUnitId);
        Require(action.CardInstanceId == 9 && card.DataId == "e9fa09e5-13bd-4826-a8e5-969332cbfbbf" &&
            before.Energy == 3 && actual.Energy == 0 && card.MaskDescriptor!.SourceTraits!.Count == 2 &&
            card.MaskDescriptor.BaseTraits.Select(trait => trait.RuntimeType).SequenceEqual([
                CardMoonAdvanceScalingModel.Trait, "CardTraitScalingAddStatusEffect", "CardTraitExhaustState"]) &&
            card.Permanent.Upgrades.Single().AssetKey == "ReduceCostAddConsume" &&
            card.Permanent.Upgrades[0].Stats.XCost == 2 && card.Temporary.Upgrades.Single().DataId == "conduit:2" &&
            played.LastPlayedCost == 3 && played.PlayCount == 1 &&
            played.ExhaustState!.BaseTraits.Single() is { TraitIndex: 2, IsExhausted: true, ExhaustedCardId: 9 } &&
            !actual.Spawn.Train.Context.Cards.Hand.Concat(actual.Spawn.Train.Context.Cards.Draw)
                .Concat(actual.Spawn.Train.Context.Cards.Discard).Any(token => token.InstanceId == 9) &&
            (target.Status("conduit")?.Stacks ?? 0) - (oldTarget.Status("conduit")?.Stacks ?? 0) == 5 &&
            actual.Spawn.Train.Context.QueryFrame!.MoonPhase != context.QueryFrame!.MoonPhase,
            "Native Lunacy must use paid X plus two for phase parity/Conduit and consume the upgraded card.");
        string parent = NativeBranchObservationChecks.Key(before);
        Parallel.For(0, 4, _ => {
            var result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Lunacy/Consume branch differs from complete native successor/RNG.");
        });
        Require(parent == NativeBranchObservationChecks.Key(before), "Lunacy/Consume branch mutated its parent.");
        foreach (string control in new[] { "MissingSource", "WrongSourceOrder", "ShortSource", "Multiplier", "Query", "Parameter", "Duplicate", "AddedMoon" }) {
            var node = JsonSerializer.SerializeToNode(card, ModelJson.Options)!;
            var mask = node["MaskDescriptor"]!;
            if (control == "MissingSource") mask["SourceTraits"] = null;
            if (control == "WrongSourceOrder") mask["SourceTraits"]![0]!["Type"] = "CardTraitScalingAddStatusEffect";
            if (control == "ShortSource") mask["SourceTraits"]!.AsArray().Clear();
            if (control == "Multiplier") mask["SourceTraits"]![0]!["Float"] = 2;
            if (control == "Query") mask["SourceTraits"]![0]!["TrackedValue"] = "TimesPlayed";
            if (control == "Parameter") mask["BaseTraits"]![0]!["ParamInt"] = 1;
            if (control == "Duplicate") mask["BaseTraits"]!.AsArray().Add(mask["BaseTraits"]![0]!.DeepClone());
            if (control == "AddedMoon") node["Permanent"]!["Upgrades"]![0]!["Lifecycle"]!["AddedTraits"]![0]!["RuntimeType"] = CardMoonAdvanceScalingModel.Trait;
            var changed = context.WithCard(node.Deserialize<CardInstanceState>(ModelJson.Options)!);
            Require(!CardMoonAdvanceScalingModel.Adjust(changed, 9, 0).Supported, "Changed Moon source lost guard: " + control);
        }
        if (replay.HasValue) {
            NativeBranchObservationChecks.Replay(replay.Value, Reason);
            NativeBranchObservationChecks.Pair(source, replay.Value);
        }
        Console.WriteLine("MOON-ADVANCE-CONSUME PASS: 44 natural inputs/full state/forecast/RNG; original source prefix survives appended Consume; X=3+2 advances and adds five Conduit; consumed private state; four immutable branches/eight source guards; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
