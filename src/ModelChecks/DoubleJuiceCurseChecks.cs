using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class DoubleJuiceCurseChecks
{
    private const string Reason = "Juice with this card or ordered scaling traits requires native calibration.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason);
        if (rows.Length != 19 || !rows[^1].GetProperty("IsEndTurn").GetBoolean())
            throw new InvalidOperationException("Missing natural18-input prefix plus TitanCurse EndTurn.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var after = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        if (before.Spawn.Turn != 4 || after.Spawn.Turn != 5) throw new InvalidOperationException("Wrong curse turn.");
        foreach (int id in new[] { 12, 25 })
        {
            var old = before.Spawn.Train.Context!.FindCard(id)!;
            var card = after.Spawn.Train.Context!.FindCard(id)!;
            if (old.Temporary.Upgrades.Count != 0 || card.Temporary.Upgrades.Count != 1 ||
                card.Temporary.Upgrades[0].DataId != "f37fb903-28d6-4d49-b50e-9226f4854816" ||
                card.Temporary.Upgrades[0].Stats.Cost != 1 || CardJuiceModel.Validate(card) != null ||
                CardModifierModel.UpgradedStat(0, "Cost", true, card.Permanent, card.Temporary) != 1)
                throw new InvalidOperationException("Native cost+1 on concurrent double/triple Spike differs.");
        }
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            var ended = BattleTurnModel.EndTurn(before);
            if (!ended.Supported || NativeBranchObservationChecks.Key(ended.State!) != NativeBranchObservationChecks.Key(after))
                throw new InvalidOperationException("Detached double/triple curse branch differs.");
        });
        if (parent != JsonSerializer.Serialize(before, ModelJson.Options)) throw new InvalidOperationException("Curse changed parent.");
        var upgraded = after.Spawn.Train.Context!.FindCard(12)!;
        foreach (string control in new[] { "DataId", "AssetKey", "Cost", "Damage" })
        {
            var changed = JsonSerializer.SerializeToNode(upgraded, ModelJson.Options)!;
            var upgrade = changed["Temporary"]!["Upgrades"]![0]!;
            if (control == "DataId") upgrade["DataId"] = "unknown-curse";
            if (control == "AssetKey") upgrade["AssetKey"] = "OtherCurse";
            if (control == "Cost") upgrade["Stats"]!["Cost"] = 2;
            if (control == "Damage") upgrade["Stats"]!["Damage"] = 1;
            if (CardJuiceModel.Validate(changed.Deserialize<CardInstanceState>(ModelJson.Options)!) == null)
                throw new InvalidOperationException("Unknown double curse signature accepted: " + control);
        }
        if (replay.HasValue)
        {
            NativeBranchObservationChecks.Replay(replay.Value, Reason);
            NativeBranchObservationChecks.Pair(source, replay.Value);
        }
        Console.WriteLine("DOUBLE-JUICE-CURSE PASS:19 native inputs/full state/forecast/RNG, concurrent double/triple Spike temporary cost+1; four immutable branches and four guards; independentPair=" + replay.HasValue);
    }
}
