using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class EndlessUpgradeTargetChecks
{
    internal static void Native(FixtureValue fixture)
    {
        var row = fixture.GetProperty("Rows").EnumerateArray().Last();
        var observed = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
        var oldTarget = observed.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == 50);
        var target = actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == 50);
        var played = BattleActionModel.PlayCard(observed, action);
        Require(observed.Spawn.Turn == 10 && action is { CardInstanceId: 11, RoomIndex: 2, TargetUnitId: 50 } &&
            oldTarget.Status("endless")?.Stacks == 1 && target.Status("endless")?.Stacks == 2 &&
            played.Supported && NativeBranchObservationChecks.Key(played.State!) == NativeBranchObservationChecks.Key(actual),
            "Missing native redundant Endless cast/full state/RNG counterexample.");
        var node = JsonSerializer.SerializeToNode(observed, ModelJson.Options)!;
        var cards = node["Spawn"]!["Train"]!["Context"]!["Cards"]!;
        cards["Hand"] = new JsonArray(cards["Hand"]!.AsArray().Single(item => item!["InstanceId"]!.GetValue<int>() == 11)!.DeepClone());
        var source = node.Deserialize<BattleTurnState>(ModelJson.Options)!;
        var rule = source.PlayRules!.Cards.Single(item => item.DataId == EchoRemnantPactModel.DataId);
        string parent = JsonSerializer.Serialize(source, ModelJson.Options);
        foreach (int preferred in new[] { 0, 1, 2 })
        {
            Check(BattleHeuristicPolicy.Choose(source, out _, preferred));
            Check(BattleTacticalPolicy.Choose(source, out _, preferred));
        }
        void Check(SupportedActionCandidate? choice)
        {
            Require(choice?.Action is { CardInstanceId: 11, RoomIndex: 2, TargetUnitId: 51 } && choice.Result.Supported &&
                choice.Result.State!.Spawn.Train.Rooms[2].Units.Single(unit => unit.Id == 51).Status("endless")?.Stacks == 1,
                "A seed repeated Endless on the25ATK host instead of covering the12ATK Dreg.");
        }
        Require(BattleActionModel.EnumerateActions(source).SupportedActions.Any(candidate =>
            candidate.CardInstanceId == 11 && candidate.TargetUnitId == 50), "The legal redundant cast was pruned from full search.");
        foreach (string control in new[] { "Attack", "OnCast", "Expiry" })
        {
            if (control == "Expiry")
            {
                var unit = JsonSerializer.SerializeToNode(oldTarget, ModelJson.Options)!;
                unit["Statuses"]!.AsArray().Single(item => item!["Id"]!.GetValue<string>() == "endless")!["RemoveAllAtEnd"] = true;
                Require(!BattleHeuristicPolicy.RedundantEndlessUpgrade(source, action, rule, unit.Deserialize<CombatUnit>(ModelJson.Options)!),
                    "Expiring Endless must keep its original ranking.");
                continue;
            }
            var changed = JsonSerializer.SerializeToNode(rule, ModelJson.Options)!;
            if (control == "Attack") changed["Effects"]![0]!["Upgrade"]!["Stats"]!["Damage"] = 1;
            else changed["OnCastEffects"] = new JsonArray(changed["Effects"]![0]!.DeepClone());
            Require(!BattleHeuristicPolicy.RedundantEndlessUpgrade(source, action, changed.Deserialize<CardPlayRule>(ModelJson.Options)!, oldTarget),
                "Mixed or callback-bearing upgrades must retain original ranking: " + control);
        }
        Parallel.For(0, 4, index => { Check(BattleHeuristicPolicy.Choose(source, out _)); Check(BattleTacticalPolicy.Choose(source, out _)); });
        Require(parent == JsonSerializer.Serialize(source, ModelJson.Options), "Endless target ranking mutated its parent/RNG.");
        Console.WriteLine("ENDLESS-UPGRADE-TARGET PASS: native redundant cast/full successor, both seeds/three floor preferences choose uncovered Dreg, legal repeated cast retained, three scope controls and four immutable parallel choices; strategy control, not a campaign victory.");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
