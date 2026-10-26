using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TripleJuiceCurseCastChecks
{
    private const string Reason = "Native triple Juice cast after TitanCurse cost upgrade.";
    internal static void Native(FixtureValue source, FixtureValue replay)
    {
        var first = NativeBranchObservationChecks.Replay(source, Reason);
        var second = NativeBranchObservationChecks.Replay(replay, Reason);
        if (first.Length != 31 || second.Length != 31 || !source.GetProperty("Request").ContentEquals(replay.GetProperty("Request")))
            throw new InvalidOperationException("Post-curse cast changed its30-input natural prefix.");
        for (int i = 0; i < first.Length; i++)
        {
            foreach (string field in new[] { "Before", "Expected", "Actual" })
                if (NativeBranchObservationChecks.Key(first[i].GetProperty(field).Deserialize<BattleTurnState>()!) !=
                    NativeBranchObservationChecks.Key(second[i].GetProperty(field).Deserialize<BattleTurnState>()!))
                    throw new InvalidOperationException("Independent post-curse cast differs at " + i + ": " + field);
            foreach (string field in new[] { "Action", "IsEndTurn", "Boundary" })
                if (!first[i].GetProperty(field).ContentEquals(second[i].GetProperty(field)))
                    throw new InvalidOperationException("Independent post-curse input differs.");
        }
        var before = first[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var after = first[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = first[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        var card = before.Spawn.Train.Context!.FindCard(18)!;
        CombatUnit Target(BattleTurnState state) => state.Spawn.Train.Rooms.SelectMany(room => room.Units)
            .Single(unit => unit.Id == action.TargetUnitId);
        if (before.Spawn.Turn != 6 || before.Energy != 3 || after.Energy != 0 || action.CardInstanceId != 18 ||
            card.Temporary.Upgrades.Count != 1 || card.Temporary.Upgrades[0].Stats.Cost != 1 ||
            Target(after).Status("valor")!.Stacks != (Target(before).Status("valor")?.Stacks ?? 0) + 36 ||
            after.Spawn.Train.Context!.FindCard(18)!.ExhaustState?.BaseTraits.Any(trait => trait.IsExhausted) != true)
            throw new InvalidOperationException("Post-curse paid energy3/scaled12/triple36/exhaust boundary differs.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            var played = BattleActionModel.PlayCard(before, action);
            if (!played.Supported || NativeBranchObservationChecks.Key(played.State!) != NativeBranchObservationChecks.Key(after))
                throw new InvalidOperationException("Detached post-curse cast differs.");
        });
        if (parent != JsonSerializer.Serialize(before, ModelJson.Options)) throw new InvalidOperationException("Post-curse cast changed parent.");
        Console.WriteLine("TRIPLE-JUICE-CURSE-CAST PASS:31 independent native inputs/full state/forecast/RNG; cost+1, paid3/scaled12/triple36, exhaust and four immutable branches.");
    }
}
