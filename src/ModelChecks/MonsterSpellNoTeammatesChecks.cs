using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class MonsterSpellNoTeammatesChecks
{
    private const string Reason = "Only a single fixed, Monster-targeted DropTarget damage effect without OnCast is modeled.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason);
        Require(rows.Length == 28, "Incomplete original no-teammate spell prefix.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        var prior = before.Spawn.Train.Context!;
        var after = actual.Spawn.Train.Context!;
        var card = prior.FindCard(15)!;
        var target = actual.Spawn.Train.Rooms[0].Units.Single(unit => unit.Team == CombatTeam.Player);
        Require(before.Spawn.Turn == 5 && action.CardInstanceId == 15 && action.RoomIndex == 0 && action.TargetUnitId == 7 &&
            card.DataId == "7cb76384-d8e7-4b4e-a3eb-f9c5b844c941" && card.Permanent.Upgrades.Count == 2 &&
            CardExhaustModel.HasConsume(card) && before.Spawn.Train.Rooms[0].Units.Count(unit => unit.Team == CombatTeam.Player) == 1 &&
            target.Id == 7 && target.Status("melee weakness")?.Stacks == 1 && target.Status("armor")?.Stacks == 3 &&
            prior.RoomCapacities!.Single(room => room.RoomIndex == 0).Corruption == 1 &&
            after.RoomCapacities!.Single(room => room.RoomIndex == 0).Corruption == 2 &&
            after.NextCardId == prior.NextCardId && after.FindCard(15)!.PlayCount == card.PlayCount + 1 &&
            after.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Count(token => token.InstanceId == 15) == 1,
            "Missing the native upgraded original spell, single corruption/Consume or empty teammate set.");
        foreach (var context in new[] { prior, after })
        {
            var condition = context.Relics!.Single(relic => relic.DataId == RelicMonsterSpellTargetingModel.RelicId)
                .MonsterSpellTargetingModifiers!.Single().Conditions.Single();
            Require(!condition.Triggered && condition.DurationTriggerCount == 0, "An empty teammate set spent the native once-per-turn trigger.");
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
            Require(result.Supported && result.Outcome == RoomOutcome.Exchanged && NativeBranchObservationChecks.Key(result.State!) == native &&
                JsonSerializer.Serialize(before, ModelJson.Options) == parent, "No-teammate original cast changed native state/RNG or parent.");
        });
        Console.WriteLine("MONSTER-SPELL-NO-TEAMMATES PASS:28 native inputs/full state/RNG, upgraded original Challenge, one corruption/Consume, unspent relic condition and four immutable branches; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
