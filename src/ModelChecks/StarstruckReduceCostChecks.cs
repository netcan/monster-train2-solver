using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class StarstruckReduceCostChecks
{
    // The request label is preserved verbatim; its captured phase is New Moon.
    internal const string CastReason = "Reduced-cost triple Starstruck requires a native full-Moon cast.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, TripleJuiceStarstruckChecks.Reason);
        Require(rows.Length == 1, "Wrong natural reduced-cost Starstruck deployment.");
        var before = rows[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[0].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = rows[0].GetProperty("Action").Deserialize<PlayCardAction>()!;
        var card = before.Spawn.Train.Context!.FindCard(18)!;
        Require(action.CardInstanceId == 8 && card.DataId == "753f20a2-c625-47b3-be4b-8600b673841e" &&
            card.Permanent.Upgrades.Select(upgrade => upgrade.AssetKey).SequenceEqual(["TraitJuice3", "ReduceCost"]) &&
            card.Permanent.Upgrades[1].Stats is { Cost: -1, XCost: 1 } && CardJuiceModel.Validate(card) == null,
            "Missing original triple-Juice/ReduceCost signature.");
        var active = CardMoonUpgradeModel.Update(card, 2).Card!;
        Require(active.Permanent.Upgrades.Count == 3 && CardJuiceModel.Validate(active) == null &&
            active.Permanent.Upgrades[2].Lifecycle!.Triggers.Single().Actions.Single().Statuses.Single().Stacks == 1 &&
            CardMoonUpgradeModel.Update(active, 1).Card!.Permanent.Upgrades.Select(upgrade => upgrade.AssetKey)
                .SequenceEqual(["TraitJuice3", "ReduceCost"]), "Moon payload changed or removed the original cost upgrade.");
        string parent = NativeBranchObservationChecks.Key(before);
        Parallel.For(0, 4, _ => {
            var result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Reduced-cost signature changed complete native successor/RNG.");
        });
        Require(parent == NativeBranchObservationChecks.Key(before), "Reduced-cost branch mutated parent.");
        foreach (string control in new[] { "Owner", "Asset", "Cost", "XCost", "Damage", "Health", "Status", "AddedTrait", "Order", "Duplicate" }) {
            var node = JsonSerializer.SerializeToNode(active, ModelJson.Options)!;
            var upgrade = node["Permanent"]!["Upgrades"]![1]!;
            if (control == "Owner") upgrade["DataId"] = "unknown-reduce-cost";
            if (control == "Asset") upgrade["AssetKey"] = "unknown-reduce-cost";
            if (control == "Cost") upgrade["Stats"]!["Cost"] = -2;
            if (control == "XCost") upgrade["Stats"]!["XCost"] = 2;
            if (control == "Damage") upgrade["Stats"]!["Damage"] = 1;
            if (control == "Health") upgrade["Stats"]!["Health"] = 1;
            if (control == "Status") upgrade["Statuses"]!.AsArray().Add(node["Permanent"]!["Upgrades"]![2]!["Lifecycle"]!["Triggers"]![0]!["Actions"]![0]!["Statuses"]![0]!.DeepClone());
            if (control == "AddedTrait") upgrade["Lifecycle"]!["AddedTraits"]!.AsArray().Add(node["MaskDescriptor"]!["BaseTraits"]![1]!.DeepClone());
            if (control == "Order") upgrade["DataId"] = node["Permanent"]!["Upgrades"]![0]!["DataId"]!.DeepClone();
            if (control == "Duplicate") node["Permanent"]!["Upgrades"]!.AsArray().Add(upgrade.DeepClone());
            Require(CardJuiceModel.Validate(node.Deserialize<CardInstanceState>(ModelJson.Options)!) != null,
                "Changed Starstruck cost upgrade lost its guard: " + control);
        }
        if (replay.HasValue) {
            NativeBranchObservationChecks.Replay(replay.Value, TripleJuiceStarstruckChecks.Reason);
            NativeBranchObservationChecks.Pair(source, replay.Value);
        }
        Console.WriteLine("STARSTRUCK-REDUCE-COST PASS: natural deployment/full state/forecast/RNG; original ReduceCost retained through both Moon phases; four immutable branches/ten unknown-upgrade guards; independentPair=" + replay.HasValue);
    }
    internal static void Cast(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, CastReason);
        Require(rows.Length == 23, "Wrong reduced-cost actual cast boundary.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        var card = before.Spawn.Train.Context!.FindCard(18)!;
        var played = actual.Spawn.Train.Context!.FindCard(18)!;
        Require(action.CardInstanceId == 18 && before.Spawn.Turn == 4 &&
            before.Spawn.Train.Context.QueryFrame!.MoonPhase == 1 && before.Energy == 1 && actual.Energy == 0 &&
            card.Permanent.Upgrades.Select(upgrade => upgrade.AssetKey).SequenceEqual(["TraitJuice3", "ReduceCost"]) &&
            card.Temporary.Upgrades.Count == 1 && card.Temporary.Upgrades.All(CardJuiceModel.KnownTitanCurse) &&
            played.PlayCount == 1 && played.LastPlayedCost == 1 &&
            before.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == action.TargetUnitId).Health == 1 &&
            !actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit => unit.Id == action.TargetUnitId),
            "Native New Moon cast must pay one after the curse and kill the one-HP target.");
        string parent = NativeBranchObservationChecks.Key(before);
        Parallel.For(0, 4, _ => {
            var result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Reduced-cost cast changed native successor/RNG.");
        });
        Require(parent == NativeBranchObservationChecks.Key(before), "Reduced-cost cast mutated parent.");
        if (replay.HasValue) {
            NativeBranchObservationChecks.Replay(replay.Value, CastReason);
            NativeBranchObservationChecks.Pair(source, replay.Value, originalPredictionPresent: true, originalPredictionMatched: true);
        }
        Console.WriteLine("STARSTRUCK-REDUCE-COST-CAST PASS: 23 native inputs/full state/forecast/RNG; New Moon spell pays one after the curse, kills its target and updates history; four immutable branches; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
