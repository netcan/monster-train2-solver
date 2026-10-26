using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TripleJuiceCurseChecks
{
    private const string Reason = "Triple Juice with this card or ordered scaling traits requires native calibration.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason);
        if (rows.Length != 30) throw new InvalidOperationException("Missing30-input natural curse boundary.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var after = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var old = before.Spawn.Train.Context!.FindCard(18)!;
        var card = after.Spawn.Train.Context!.FindCard(18)!;
        if (!rows[^1].GetProperty("IsEndTurn").GetBoolean() || before.Spawn.Turn != 5 || after.Spawn.Turn != 6 ||
            old.Temporary.Upgrades.Count != 0 || card.Temporary.Upgrades.Count != 1 ||
            card.Temporary.Upgrades[0].DataId != "f37fb903-28d6-4d49-b50e-9226f4854816" ||
            card.Temporary.Upgrades[0].Stats.Cost != 1 || CardJuiceModel.Validate(card) != null ||
            CardModifierModel.UpgradedStat(0, "Cost", true, card.Permanent, card.Temporary) != 1)
            throw new InvalidOperationException("Native temporary +1 cost on triple Juice differs.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            var result = BattleTurnModel.EndTurn(before);
            if (!result.Supported || NativeBranchObservationChecks.Key(result.State!) != NativeBranchObservationChecks.Key(after))
                throw new InvalidOperationException("Detached curse branch differs.");
        });
        if (parent != JsonSerializer.Serialize(before, ModelJson.Options)) throw new InvalidOperationException("Curse changed parent.");
        foreach (string control in new[] { "DataId", "AssetKey", "Cost", "Damage" })
        {
            var changed = JsonSerializer.SerializeToNode(card, ModelJson.Options)!;
            var upgrade = changed["Temporary"]!["Upgrades"]![0]!;
            if (control == "DataId") upgrade["DataId"] = "unknown-curse";
            if (control == "AssetKey") upgrade["AssetKey"] = "OtherCurse";
            if (control == "Cost") upgrade["Stats"]!["Cost"] = 2;
            if (control == "Damage") upgrade["Stats"]!["Damage"] = 1;
            if (CardJuiceModel.Validate(changed.Deserialize<CardInstanceState>(ModelJson.Options)!) == null)
                throw new InvalidOperationException("Unknown temporary curse signature accepted: " + control);
        }
        if (replay.HasValue)
        {
            NativeBranchObservationChecks.Replay(replay.Value, Reason);
            NativeBranchObservationChecks.Pair(source, replay.Value);
        }
        Console.WriteLine("TRIPLE-JUICE-CURSE PASS:30 native inputs/full state/forecast/RNG, temporary +1 cost and fresh hand; four immutable branches and unknown payload guards; independentPair=" + replay.HasValue);
    }
}
