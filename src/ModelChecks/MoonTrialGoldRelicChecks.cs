using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class MoonTrialGoldRelicChecks
{
    internal const string Reason = "Uncalibrated moon hand-refresh relic: ApplyUnitUpgradeWhenRich.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason);
        Require(rows.Length == 5 && rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Wrong natural Moon/reward boundary.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var owner = before.Spawn.Train.Context!.Relics!.Single(relic => relic.AssetKey == "ApplyUnitUpgradeWhenRich");
        Require(before.Spawn.Turn == 1 && actual.Spawn.Turn == 2 &&
            owner.DataId == "07bdefdb-03b8-4159-b74b-be85f68c4c56" && owner.IsCovenant == false &&
            owner.DisallowedInPlacementPhase == false && owner.EffectTypes.SequenceEqual(["RelicEffectModifyTrialGoldRewards"]) &&
            actual.Spawn.Train.Context!.Cards.Hand.Any(token => CardMoonUpgradeModel.Present(actual.Spawn.Train.Context.FindCard(token.InstanceId)!)),
            "Missing original reward-only owner or naturally drawn Moon-upgrade card.");
        string parent = NativeBranchObservationChecks.Key(before);
        Parallel.For(0, 4, _ => {
            var result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Moon/reward branch differs from complete native successor/RNG.");
        });
        Require(parent == NativeBranchObservationChecks.Key(before), "Moon/reward branch changed its parent.");
        foreach (string control in new[] { "Owner", "Asset", "Effect", "ExtraEffect", "Covenant", "Placement" }) {
            var node = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
            var relic = node["Spawn"]!["Train"]!["Context"]!["Relics"]!.AsArray()
                .Single(item => item!["DataId"]!.GetValue<string>() == owner.DataId)!;
            if (control == "Owner") relic["DataId"] = "unknown-trial-gold";
            if (control == "Asset") relic["AssetKey"] = "unknown-trial-gold";
            if (control == "Effect") relic["EffectTypes"]![0] = "RelicEffectAddStatusEffectOnStatusApplied";
            if (control == "ExtraEffect") relic["EffectTypes"]!.AsArray().Add("RelicEffectModifyCardMagicPowerOnCardPlayed");
            if (control == "Covenant") relic["IsCovenant"] = null;
            if (control == "Placement") relic["DisallowedInPlacementPhase"] = true;
            var result = BattleTurnModel.EndTurn(node.Deserialize<BattleTurnState>(ModelJson.Options)!);
            Require(!result.Supported && result.State == null, "Changed Moon/reward owner lost guard: " + control);
        }
        if (replay.HasValue) {
            NativeBranchObservationChecks.Replay(replay.Value, Reason);
            NativeBranchObservationChecks.Pair(source, replay.Value);
        }
        Console.WriteLine("MOON-TRIAL-GOLD-RELIC PASS: five natural inputs/full state/forecast/RNG; reward-only owner and Moon hand refresh; four immutable branches; six unknown-signature guards; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
