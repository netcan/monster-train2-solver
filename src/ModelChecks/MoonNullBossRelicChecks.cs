using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class MoonNullBossRelicChecks
{
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        const string reason = "Uncalibrated moon hand-refresh relic: Enemy_Boss_Level3_ImmortalMass_Corruption.";
        var rows = NativeBranchObservationChecks.Replay(source, reason);
        Require(rows.Length == 11 && rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Missing natural third-turn draw boundary.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var context = actual.Spawn.Train.Context!;
        var owner = context.Relics!.Single(relic => relic.DataId == "741546e7-71a9-4817-aab9-066af7c32fcf");
        Require(before.Spawn.Turn == 2 && actual.Spawn.Turn == 3 && actual.MoonPhase == "Full" &&
            owner.AssetKey == "Enemy_Boss_Level3_ImmortalMass_Corruption" && owner.IsCovenant == false &&
            owner.EffectTypes.SequenceEqual(["RelicEffectNull", "RelicEffectNull"]) &&
            context.Cards.Hand.Any(token => CardMoonUpgradeModel.Present(context.FindCard(token.InstanceId)!)),
            "Lost the original null boss owner and naturally drawn moon-upgrade card.");
        if (replay.HasValue)
        {
            NativeBranchObservationChecks.Replay(replay.Value, reason);
            NativeBranchObservationChecks.Pair(source, replay.Value);
        }
        Console.WriteLine("MOON-NULL-BOSS-RELIC PASS: eleven native inputs/full state/RNG, natural full-moon hand refresh with original null boss artifact; " +
            "independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
