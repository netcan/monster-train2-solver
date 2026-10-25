using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RemovedAbilitySelfChecks
{
    private const string Reason = "Self targeting requires the retained ability actor after removal.";
    internal static void Native(FixtureValue fixture)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture, Reason);
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        CombatUnit actor = before.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == action.ActivatorUnitId);
        Require(rows.Length == 12 && actor.AssetKey == "MonsterBanishedChampion1" && actor.Health == 20 &&
            actor.Ability?.DataId == "1cbb4a0b-9ad5-4d05-91e5-d94da55d21e5" &&
            actual.Spawn.Train.Rooms.SelectMany(room => room.Units).All(unit => unit.Id != actor.Id),
            "Missing the native lethal Penitent shift ability.");
        BattleActionEnumeration choices = BattleActionModel.EnumerateActions(before);
        Require(choices.SupportedActions.Any(candidate => candidate.CardInstanceId == action.CardInstanceId &&
            candidate.ActivatorUnitId == action.ActivatorUnitId && candidate.RoomIndex == action.RoomIndex),
            "The repaired native ability is absent from search enumeration.");
        Require(!CardTargetModel.Collect(actual.Spawn.Train, action.RoomIndex,
            new CardActionEffect("AddStatus", "Self", 0, false, true, Array.Empty<CombatStatus>()),
            Array.Empty<int>(), selfUnitId: actor.Id).Supported,
            "A missing actor without its retained death record was silently accepted.");
        Console.WriteLine("REMOVED-ABILITY-SELF PASS:12 native inputs/full state/RNG, lethal champion shift, later Self status effects and immutable parents; branch enumerated and missing-actor guard retained.");
    }
    internal static void Pair(FixtureValue source, FixtureValue replay)
    {
        Native(source); Native(replay); NativeBranchObservationChecks.Pair(source, replay);
        Console.WriteLine("REMOVED-ABILITY-SELF-PAIR PASS:original rejection retained; repaired prediction matches a fresh native process.");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
