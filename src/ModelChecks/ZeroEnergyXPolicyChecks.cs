using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class ZeroEnergyXPolicyChecks
{
    private const string Reason = "Observed zero-energy upgraded X-cost candidate omitted by tactical seed.";

    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason);
        Require(rows.Length == 9, "Missing original zero-energy X input chain.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        var context = before.Spawn.Train.Context!;
        var card = context.FindCard(35)!;
        Require(before.Energy == 0 && before.Spawn.Turn == 1 && action.CardInstanceId == 35 && action.RoomIndex == 2 &&
            card.DataId == CardCopyOnPlayModel.WurmkinSpikeSpellId && CardCopyOnPlayModel.SupportedIntrinsicSpike(card) &&
            CardModifierModel.UpgradedStat(0, "XCost", true, card.Permanent, card.Temporary) == 1 &&
            !actual.Spawn.Train.Context!.Cards.Hand.Any(token => token.InstanceId == 35) &&
            actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Sum(unit => unit.Status("corrupt_poison")?.Stacks ?? 0) >
                before.Spawn.Train.Rooms.SelectMany(room => room.Units).Sum(unit => unit.Status("corrupt_poison")?.Stacks ?? 0),
            "Missing native zero-energy upgraded copy and its poison effect.");
        var selected = BattleTacticalPolicy.Choose(before, out _, preferredDeploymentRoom: 2);
        Require(selected?.Action.CardInstanceId == 35, "Tactical seed still omits the useful zero-energy X card.");
        // The native witness fixes its original input. Improved room ranking
        // may choose another legal floor; preserve the original native successor
        // and independently verify deterministic tactical choices below.
        var recorded = BattleActionModel.PlayCard(before, action);
        Require(recorded.Supported && NativeBranchObservationChecks.Key(recorded.State!) == NativeBranchObservationChecks.Key(actual),
            "The recorded zero-energy X input changed its native successor.");
        // Removing only the X bonus restores the deliberately skipped zero-X
        // case, keeping the native witness and its immutable parent untouched.
        var changed = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
        var instances = changed["Spawn"]!["Train"]!["Context"]!["CardInstances"]!.AsArray();
        var controlCard = instances.Single(value => value!["InstanceId"]!.GetValue<int>() == 35)!;
        foreach (var modifiers in new[] { "Permanent", "Temporary" })
        {
            controlCard[modifiers]!["Offsets"]!["XCost"] = 0;
            foreach (var upgrade in controlCard[modifiers]!["Upgrades"]!.AsArray()) upgrade!["Stats"]!["XCost"] = 0;
        }
        var control = changed.Deserialize<BattleTurnState>(ModelJson.Options)!;
        Require(BattleTacticalPolicy.Choose(control, out _, 2)?.Action.CardInstanceId != 35,
            "The X-bonus gate admitted the unchanged zero-effect X control.");
        if (replay.HasValue)
        {
            var second = NativeBranchObservationChecks.Replay(replay.Value, Reason);
            Require(source.GetProperty("Request").ContentEquals(replay.Value.GetProperty("Request")) && second.Length == rows.Length,
                "Independent X-cost process changed its inputs.");
            for (int index = 0; index < rows.Length; index++)
                foreach (string field in new[] { "Before", "Actual" })
                    Require(NativeBranchObservationChecks.Key(rows[index].GetProperty(field).Deserialize<BattleTurnState>()!) ==
                        NativeBranchObservationChecks.Key(second[index].GetProperty(field).Deserialize<BattleTurnState>()!),
                        "Independent X-cost process changed full native state/RNG at " + index);
        }
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        string selectedKey = NativeBranchObservationChecks.Key(selected!.Result.State!);
        string selectedAction = JsonSerializer.Serialize(selected.Action);
        Parallel.For(0, 4, _ =>
        {
            var choice = BattleTacticalPolicy.Choose(before, out var omitted, 2);
            Require(choice?.Action.CardInstanceId == 35 && JsonSerializer.Serialize(choice.Action) == selectedAction &&
                NativeBranchObservationChecks.Key(choice.Result.State!) == selectedKey &&
                JsonSerializer.Serialize(before, ModelJson.Options) == parent, "Zero-energy X seed changed full state/RNG or parent.");
        });
        Console.WriteLine("ZERO-ENERGY-X-POLICY PASS:9 native inputs/full state/RNG, useful intrinsic X bonus at zero energy, no-bonus negative control and four immutable tactical choices; independentPair=" + replay.HasValue);
    }

    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
