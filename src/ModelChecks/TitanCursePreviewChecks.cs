using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TitanCursePreviewChecks
{
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        const string reason = "Uncalibrated preview unplayed effect.";
        var rows = NativeBranchObservationChecks.Replay(source, reason);
        Require(rows.Length == 6 && rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Incomplete natural two-turn draw boundary.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        const string cardId = "17fef621-2bb7-4847-b41d-52a89795c784";
        const string upgradeId = "f37fb903-28d6-4d49-b50e-9226f4854816";
        Require(before.Spawn.Turn == 1 && actual.Spawn.Turn == 2 &&
            before.Spawn.Train.Context!.Cards.Hand.All(card => card.DataId != cardId) &&
            actual.Spawn.Train.Context!.Cards.Hand.Any(card => card.DataId == cardId) &&
            actual.Spawn.Train.Context.CardRegistry!.All(card => card.Temporary.Upgrades.All(upgrade => upgrade.DataId != upgradeId)),
            "Natural draw must retain TitanCurse without applying its preview-disabled deck upgrade.");
        var context = actual.Spawn.Train.Context;
        var curse = context.FindCard(context.Cards.Hand.Single(card => card.DataId == cardId).InstanceId)!;
        Require(curse.PlayedRoomUnitIds?.Count == 4 && context.FindCard(4)!.PlayedRoomUnitIds?.Count == 0 &&
            context.Enchantments!.RetainedUnits.Where(actor => actor.Unit.Id is 9 or 10)
                .Count(actor => actor.Unit.Status("newmoon")?.Stacks == 1 && actor.Unit.MoonPhaseMask == 5) == 2 &&
            context.Enchantments.RetainedUnits.Where(actor => actor.Unit.Id is 11 or 16)
                .Count(actor => actor.Unit.Statuses.Count == 0 && actor.Unit.MoonPhaseMask == 4) == 2,
            "Preview-disabled card still caches its room; failed champion target tests keep their cache, " +
            "and only earlier unplaced enemy births receive the real moon transition.");
        if (replay.HasValue)
        {
            NativeBranchObservationChecks.Replay(replay.Value, reason);
            NativeBranchObservationChecks.Pair(source, replay.Value);
        }
        Console.WriteLine("TITAN-CURSE-PREVIEW PASS: six native inputs/full state/RNG, newly drawn curse preview leaves deck upgrades unchanged; " +
            "original rejection preserved; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
