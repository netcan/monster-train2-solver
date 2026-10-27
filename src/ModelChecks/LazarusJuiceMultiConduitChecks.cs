using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class LazarusJuiceMultiConduitChecks
{
    private const string Reason = "Juice with this card or ordered scaling traits requires native calibration.";
    internal static void Native(FixtureValue source)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason);
        Require(rows.Length == 20, "Expected19 native prefix inputs and the Aurora summon.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        var card = actual.Spawn.Train.Context!.FindCard(13)!;
        Require(before.Spawn.Turn == 2 && action.CardInstanceId == 35 && action.RoomIndex == 2 &&
            before.Spawn.Train.Context!.FindCard(13)!.Temporary.Upgrades.Count == 0 &&
            card.DataId == LazarusJuiceSpikeModel.DataId && card.Temporary.Upgrades.Count == 2 &&
            card.Temporary.Upgrades.Select(upgrade => upgrade.DataId).SequenceEqual(new[] { "conduit:4", "conduit:3" }) &&
            card.Temporary.Upgrades.All(upgrade => upgrade.Stats.Damage == 5 && upgrade.Stats.Heal == 5) &&
            LazarusJuiceSpikeModel.Known(card) && CardJuiceModel.Validate(card) == null,
            "Missing the natural twin-source Conduit5+5/Lazarus Juice hand refresh.");
        foreach (var control in new[] { "Duplicate", "Third", "Cost", "Health", "Heal", "Unknown" })
        {
            var changed = JsonSerializer.SerializeToNode(card, ModelJson.Options)!;
            var upgrades = changed["Temporary"]!["Upgrades"]!.AsArray();
            if (control == "Duplicate") upgrades[1] = upgrades[0]!.DeepClone();
            if (control == "Third") { var third = upgrades[0]!.DeepClone(); third["DataId"] = "conduit:999"; upgrades.Add(third); }
            if (control == "Cost") upgrades[0]!["Stats"]!["Cost"] = 1;
            if (control == "Health") upgrades[0]!["Stats"]!["Health"] = 1;
            if (control == "Heal") upgrades[0]!["Stats"]!["Heal"] = 6;
            if (control == "Unknown") upgrades[0]!["DataId"] = "unknown-conduit";
            Require(!LazarusJuiceSpikeModel.Known(changed.Deserialize<CardInstanceState>(ModelJson.Options)!),
                "Uncalibrated twin-source Conduit admitted: " + control);
        }
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            var result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Twin-source hand refresh differs from complete native state/RNG.");
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Twin-source refresh mutated its parent.");
        Console.WriteLine("LAZARUS-JUICE-MULTI-CONDUIT PASS:20 native inputs/state/RNG, two distinct5+5 sources, six guards and four immutable branches; cast pair pending.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
