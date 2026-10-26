using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class SpentMonsterSpellRelicChecks
{
    private const string Reason = "Only a single fixed, Monster-targeted DropTarget damage effect without OnCast is modeled.";

    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason);
        if (replay.HasValue)
        {
            NativeBranchObservationChecks.Replay(replay.Value, Reason);
            NativeBranchObservationChecks.Pair(source, replay.Value);
        }
        Require(rows.Length == 19, "Wrong spent-relic native prefix.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        var context = before.Spawn.Train.Context!;
        var card = context.FindCard(15)!;
        var relic = context.Relics!.Single(item => item.DataId == RelicMonsterSpellTargetingModel.RelicId);
        var condition = relic.MonsterSpellTargetingModifiers!.Single().Conditions.Single();
        Require(action.CardInstanceId == 15 && action.TargetUnitId == 7 && before.Spawn.Turn == 3 &&
            card.DataId == "a6dfc0d2-9bfe-499a-b521-0a2c4cefc1a2" && CardExhaustModel.HasConsume(card) &&
            condition.Triggered && !condition.AllowMultiple && condition.DurationTriggerCount == 1,
            "Missing original LiquidCourage and exhausted once-per-turn relic condition.");
        var prior = before.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == 7);
        var target = actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == 7);
        Require((target.Status("valor")?.Stacks ?? 0) - (prior.Status("valor")?.Stacks ?? 0) == 4 &&
            (target.Status("damage shield")?.Stacks ?? 0) - (prior.Status("damage shield")?.Stacks ?? 0) == 1 &&
            actual.Spawn.Train.Context!.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(token => token.InstanceId == 15),
            "Original spell did not grant its native statuses and consume.");
        var active = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
        var owner = active["Spawn"]!["Train"]!["Context"]!["Relics"]!.AsArray()
            .Single(item => item!["DataId"]!.GetValue<string>() == relic.DataId)!;
        var changed = owner["MonsterSpellTargetingModifiers"]![0]!["Conditions"]![0]!;
        changed["Triggered"] = false;
        changed["DurationTriggerCount"] = 0;
        var uncalibratedCopy = BattleActionModel.PlayCard(active.Deserialize<BattleTurnState>(ModelJson.Options)!, action);
        Require(!uncalibratedCopy.Supported && uncalibratedCopy.Reason == Reason,
            "An active, uncalibrated LiquidCourage recast was accepted.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        string native = NativeBranchObservationChecks.Key(actual);
        Parallel.For(0, 4, _ =>
        {
            var result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && result.Outcome == RoomOutcome.Exchanged &&
                NativeBranchObservationChecks.Key(result.State!) == native && JsonSerializer.Serialize(before, ModelJson.Options) == parent,
                "Spent-relic spell changed its native successor/RNG or immutable parent.");
        });
        Console.WriteLine("SPENT-MONSTER-SPELL-RELIC PASS:19 native inputs/full state/RNG, original spell statuses/Consume, active-copy rejection and four immutable branches; independentPair=" + replay.HasValue);
    }

    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
