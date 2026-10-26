using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class BossEmberMoonChecks
{
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        const string reason = "Uncalibrated moon hand-refresh relic: BossEmber.";
        var rows = NativeBranchObservationChecks.Replay(source, reason);
        Require(rows.Length == 3 && !rows[0].GetProperty("IsEndTurn").GetBoolean() &&
            rows.Skip(1).All(row => row.GetProperty("IsEndTurn").GetBoolean()),
            "Missing natural champion deployment and two native turns.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var context = actual.Spawn.Train.Context!;
        var owner = context.Relics!.Single(relic => relic.DataId == "780f977e-4007-45ae-900f-c8929b2bf000");
        Require(before.Spawn.Turn == 1 && actual.Spawn.Turn == 2 && actual.Energy == 4 &&
            owner.AssetKey == "BossEmber" && owner.IsCovenant == false &&
            owner.EffectTypes.SequenceEqual([RelicResourceModel.Energy, RelicResourceModel.PyreHealth,
                RelicResourceModel.PyreAttack, RelicResourceModel.DeploymentEnergy]) &&
            owner.ResourceEffects!.Select(effect => effect.Amount).SequenceEqual([1, 20, 30, 1]) &&
            owner.ResourceEffects.All(effect => effect.ParamInt2 == 0 && !effect.CanApplyInPreviewMode && effect.Conditions.Count == 0) &&
            context.Cards.Hand.Any(token => CardMoonUpgradeModel.Present(context.FindCard(token.InstanceId)!)),
            "Lost the indexed native boss energy/Pyre reward or naturally drawn moon-upgrade card.");
        if (replay.HasValue)
        {
            NativeBranchObservationChecks.Replay(replay.Value, reason);
            NativeBranchObservationChecks.Pair(source, replay.Value);
        }
        Console.WriteLine("BOSS-EMBER-MOON PASS:3 native inputs/full state/RNG, natural moon hand refresh with original boss reward, " +
            "4 turn energy and indexed20HP/30attack payload retained; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
