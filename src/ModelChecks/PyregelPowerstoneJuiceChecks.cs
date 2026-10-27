using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class PyregelPowerstoneJuiceChecks
{
    internal const string CastReason = "Original Juice Powerstone spell applies eleven damage before four pyregel stacks.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null, FixtureValue? cast = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, PyregelJuiceChecks.Reason);
        Require(rows.Length == 1 && !rows[0].GetProperty("IsEndTurn").GetBoolean(), "Missing native Powerstone deployment refusal.");
        var before = rows[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[0].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var card = before.Spawn.Train.Context!.FindCard(18)!;
        var rule = before.PlayRules!.Cards.Single(item => item.DataId == card.DataId);
        Require(card.DataId == "fb0683b1-20ac-4dd2-b580-14b502e182f4" &&
            card.Permanent.Upgrades.Select(upgrade => upgrade.DataId).SequenceEqual([
                "0fc9f6e7-df71-4045-9867-67453769adf8", "571cd1d6-6959-48ae-831d-86f07de8329e"]) &&
            rule.Effects is [{ Type: "Damage", Value: 1 }, { Type: "AddStatus", Statuses: [{ Id: "pyregel", Stacks: 2 }] }] &&
            CardJuiceModel.Validate(card) == null, "Wrong native Juice/Powerstone owner or payload.");
        var action = rows[0].GetProperty("Action").Deserialize<PlayCardAction>()!;
        string parent = NativeBranchObservationChecks.Key(before);
        Parallel.For(0, 4, _ => {
            var result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "The known Juice/Powerstone composition blocks unrelated deployment.");
        });
        Require(parent == NativeBranchObservationChecks.Key(before), "Powerstone deployment mutated its parent.");
        foreach (string control in new[] { "Owner", "Upgrade", "Damage", "Heal", "Lifecycle", "Order", "Temporary" })
        {
            var node = JsonSerializer.SerializeToNode(card, ModelJson.Options)!;
            var upgrades = node["Permanent"]!["Upgrades"]!.AsArray();
            if (control == "Owner") node["DataId"] = "uncalibrated-juice-powerstone";
            if (control == "Upgrade") upgrades[1]!["DataId"] = "uncalibrated-powerstone";
            if (control == "Damage") upgrades[1]!["Stats"]!["Damage"] = 11;
            if (control == "Heal") upgrades[1]!["Stats"]!["Heal"] = 11;
            if (control == "Lifecycle") upgrades[1]!["Lifecycle"]!["Values"]!["Stats"]!["Damage"] = 11;
            if (control == "Order") { var first = upgrades[0]!.DeepClone(); upgrades[0] = upgrades[1]!.DeepClone(); upgrades[1] = first; }
            if (control == "Temporary") node["Temporary"]!["Upgrades"]!.AsArray().Add(upgrades[1]!.DeepClone());
            Require(CardJuiceModel.Validate(node.Deserialize<CardInstanceState>(ModelJson.Options)!) != null,
                "Unknown Juice/Powerstone signature lost its guard: " + control);
        }
        if (replay.HasValue)
        {
            NativeBranchObservationChecks.Replay(replay.Value, PyregelJuiceChecks.Reason);
            NativeBranchObservationChecks.Pair(source, replay.Value);
        }
        if (cast.HasValue)
        {
            var casts = NativeBranchObservationChecks.Replay(cast.Value, CastReason);
            Require(NativeBranchObservationChecks.Key(casts[0].GetProperty("Before").Deserialize<BattleTurnState>()!) == parent,
                "Native Powerstone cast starts from another deployment root.");
            var last = casts[^1];
            var root = last.GetProperty("Before").Deserialize<BattleTurnState>()!;
            var native = last.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            var input = last.GetProperty("Action").Deserialize<PlayCardAction>()!;
            var target = root.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == input.TargetUnitId);
            var result = native.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == input.TargetUnitId);
            Require(input.CardInstanceId == 18 && target.Health > 11 && target.Status("armor") == null &&
                target.Status("damage shield") == null && result.Health == target.Health - 11 &&
                (result.Status("pyregel")?.Stacks ?? 0) == (target.Status("pyregel")?.Stacks ?? 0) + 4,
                "Native Powerstone Juice cast must deal eleven damage before adding four pyregel stacks.");
        }
        Console.WriteLine("PYREGEL-POWERSTONE-JUICE PASS: original deployment/full state/forecast/RNG, four immutable branches/seven scope controls; independentPair=" + replay.HasValue + "; nativeCast=" + cast.HasValue);
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
