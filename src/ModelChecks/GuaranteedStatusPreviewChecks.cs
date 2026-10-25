using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class GuaranteedStatusPreviewChecks
{
    private const string Reason = "Battle preview floor 2: Randomized status previews require the explicit isolated preview RNG protocol.";
    internal static void Native(FixtureValue fixture)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture, Reason);
        var last = rows[^1];
        var before = last.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var after = last.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var champion = before.Spawn.Train.Rooms.Single(room => room.RoomIndex == 2).Units.Single(unit => unit.AssetKey == "MonsterStygianChampionTethys");
        var effect = champion.Triggers.Single(trigger => trigger.Kind == "OnAttacking").Effects.Single();
        Require(rows.Length == 4 && last.GetProperty("IsEndTurn").GetBoolean() &&
            before.Spawn.Turn == 1 && after.Spawn.Turn == 2 &&
            !before.UiRngIsolated && before.Spawn.Train.Context!.IsolatedBattlePreview != true &&
            champion.AssetKey == "MonsterStygianChampionTethys" &&
            effect.Type == "CardEffectAddStatusEffect" && effect.Action is { Value: 100, Range: null, Target: "LastAttackedCharacter" } &&
            effect.Action.Statuses.Count == 1 && effect.Action.Statuses[0].Id == "poison" &&
            effect.Action.Statuses[0].Stacks == 15 &&
            before.Spawn.Train.Context.BattleRng.Equals(after.Spawn.Train.Context!.BattleRng),
            "Missing the ordinary guaranteed-status preview boundary with unchanged live Battle RNG.");
        Console.WriteLine("GUARANTEED-STATUS-PREVIEW PASS:4 native inputs/full successors/RNG; ordinary Tethys poison100 preview retains live RNG and real state; immutable parents.");
    }
    internal static void Pair(FixtureValue source, FixtureValue replay)
    {
        Native(source); Native(replay);
        NativeBranchObservationChecks.Pair(source, replay);
        Console.WriteLine("GUARANTEED-STATUS-PREVIEW-PAIR PASS:original rejection and independent repaired forecast preserved.");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
