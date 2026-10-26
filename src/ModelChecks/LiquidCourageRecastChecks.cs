using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class LiquidCourageRecastChecks
{
    private const string Reason = "Only a single fixed, Monster-targeted DropTarget damage effect without OnCast is modeled.";

    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason);
        Require(rows.Length == 19, "Incomplete original LiquidCourage recast chain.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        var context = before.Spawn.Train.Context!;
        var after = actual.Spawn.Train.Context!;
        var card = context.FindCard(15)!;
        var condition = context.Relics!.Single(relic => relic.DataId == RelicMonsterSpellTargetingModel.RelicId)
            .MonsterSpellTargetingModifiers!.Single().Conditions.Single();
        var recorded = after.Relics!.Single(relic => relic.DataId == RelicMonsterSpellTargetingModel.RelicId)
            .MonsterSpellTargetingModifiers!.Single().Conditions.Single();
        Require(action.CardInstanceId == 15 && action.RoomIndex == 0 && action.TargetUnitId == 7 && before.Spawn.Turn == 3 &&
            card.DataId == "a6dfc0d2-9bfe-499a-b521-0a2c4cefc1a2" && CardExhaustModel.HasConsume(card) &&
            !condition.Triggered && condition.DurationTriggerCount == 0 && recorded.Triggered && recorded.DurationTriggerCount == 1 &&
            after.NextCardId == context.NextCardId && after.OtherPiles!.Single(pile => pile.Name == "Exhausted")
                .Cards.Count(token => token.InstanceId == 15) == 1 && after.FindCard(15)!.PlayCount == card.PlayCount + 1,
            "Missing native active once-per-turn recast, unchanged private-copy identity or single Consume.");
        foreach (int id in new[] { 7, 6 })
        {
            var prior = before.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == id);
            var target = actual.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == id);
            Require((target.Status("valor")?.Stacks ?? 0) - (prior.Status("valor")?.Stacks ?? 0) == 4 &&
                (target.Status("damage shield")?.Stacks ?? 0) - (prior.Status("damage shield")?.Stacks ?? 0) == 1,
                "Original target or teammate lost native statuses.");
        }
        if (replay.HasValue)
        {
            NativeBranchObservationChecks.Replay(replay.Value, Reason);
            NativeBranchObservationChecks.Pair(source, replay.Value);
        }
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        string native = NativeBranchObservationChecks.Key(actual);
        Parallel.For(0, 4, _ =>
        {
            var result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == native &&
                JsonSerializer.Serialize(before, ModelJson.Options) == parent, "LiquidCourage recast changed full state/RNG or parent.");
        });
        Console.WriteLine("LIQUID-COURAGE-RECAST PASS:19 native inputs/full state/RNG, original target and teammate shield/Valor, one condition trigger/Consume, unchanged private-copy identity and four immutable branches; independentPair=" + replay.HasValue);
    }

    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
