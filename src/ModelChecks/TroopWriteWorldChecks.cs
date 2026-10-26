using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TroopWriteWorldChecks
{
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        const string reason = "Battle preview floor 1: CardEffectEnchant requires a captured automatic lifecycle world and effect definition.";
        var rows = NativeBranchObservationChecks.Replay(source, reason);
        Require(rows.Length == 24 && !rows[^1].GetProperty("IsEndTurn").GetBoolean() &&
            rows[^1].GetProperty("Action").Deserialize<PlayCardAction>() is { CardInstanceId: 13, RoomIndex: 1 },
            "Missing twenty-four-input native Steward deployment prefix.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var room = actual.Spawn.Train.Rooms[1];
        Require(before.Spawn.Turn == 4 && actual.Spawn.Turn == 4 &&
            room.Units.Single(unit => unit.Id == 27) is { AssetKey: "TrainStewardSmall", SpawnerCardId: 13, Attack: 14, Health: 5 } &&
            room.Units.Single(unit => unit.Id == 6).Modifiers!.TroopAura!.Targets.Count == 0 &&
            room.Units.Single(unit => unit.Id == 13).Status("conduit")?.Stacks == 6,
            "Lost real Steward, stable primary troop cache or primary Conduit stacks.");
        var birth = actual.Spawn.Train.Context!.Enchantments!.RetainedUnits.Single(actor => actor.Unit.Id == 28);
        Require(birth.Unit is { AssetKey: "MonsterFunguy", Attack: 3, Health: 4, DeathState.IsDestroyed: true } &&
            birth.Unit.Statuses.Count == 0 && birth.Unit.Modifiers!.Upgrades.Count == 0,
            "Troop preview did not restore its new temporary actor.");
        BattleTurnState current = source.GetProperty("Request").GetProperty("Root").Deserialize<BattleTurnState>()!;
        foreach (var row in rows)
        {
            current = BattlePlannerChecks.Replay(current, [new BattleSearchStep(row.GetProperty("Action").Deserialize<PlayCardAction>(),
                row.GetProperty("IsEndTurn").GetBoolean())], out var outcome);
            Require(outcome == RoomOutcome.Exchanged && NativeBranchObservationChecks.Key(current) ==
                NativeBranchObservationChecks.Key(row.GetProperty("Actual").Deserialize<BattleTurnState>()!), "Owned troop write world prefix differs.");
        }
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            var result = BattleActionModel.PlayCard(before, rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual) &&
                parent == JsonSerializer.Serialize(before, ModelJson.Options), "Troop write world changed its parent, state or RNG.");
        });
        if (replay.HasValue)
        {
            Native(replay.Value);
            NativeBranchObservationChecks.Pair(source, replay.Value);
        }
        Console.WriteLine("TROOP-WRITE-WORLD PASS:24 native inputs/continuous state/RNG, actual Steward and Conduit, restored troop preview, " +
            "four immutable branches; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
