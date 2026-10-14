using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class DamageByUnitsKilledChecks
{
    internal static void Native(FixtureValue fixture)
    {
        const string spellId = "d9c0c510-ccb5-4a89-9f3b-99799e765540";
        int matched = 0;
        foreach (FixtureValue entry in fixture.GetProperty("Actions").EnumerateArray())
        {
            BattleTurnState before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
            PlayCardAction action = entry.GetProperty("Action").Deserialize<PlayCardAction>()!;
            CardToken? spell = before.Spawn.Train.Context!.Cards.Hand.FirstOrDefault(card =>
                card.InstanceId == action.CardInstanceId && card.DataId == spellId);
            if (spell == null) continue;
            RoomCombatState room = before.Spawn.Train.Rooms.Single(item => item.RoomIndex == action.RoomIndex);
            const string impSubtype = "SubtypesData_Imp_0f9b989f-15b5-4b16-8378-5d8ed8691e7c";
            int[] imps = room.Units.Where(unit => unit.Team == CombatTeam.Player && unit.Subtypes.Contains(impSubtype))
                .Select(unit => unit.Id).ToArray();
            int[] heroes = room.Units.Where(unit => unit.Team == CombatTeam.Enemy).Select(unit => unit.Id).ToArray();
            BattleActionResult result = BattleActionModel.PlayCard(before, action);
            BattleTurnState actual = entry.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            RoomCombatState after = actual.Spawn.Train.Rooms.Single(item => item.RoomIndex == action.RoomIndex);
            Require(result.Supported && imps.Length > 0 && heroes.Length > 0 &&
                imps.All(id => after.Units.All(unit => unit.Id != id)) &&
                heroes.Any(id => room.Units.Single(unit => unit.Id == id).Health >
                    (after.Units.FirstOrDefault(unit => unit.Id == id)?.Health ?? 0)),
                "Original Shardtail action did not sacrifice an Imp and damage an enemy.");
            string? difference = ModelJson.Difference(BattleTurnChecks.Comparable(result.State!),
                BattleTurnChecks.Comparable(actual));
            Require(difference == null, "Shardtail action differs: " + difference);
            matched++;
        }
        Require(matched > 0, "No original Shardtail action was captured.");
        Console.WriteLine("NATIVE-DAMAGE-BY-UNITS-KILLED PASS: " + matched + " original Shardtail actions matched.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
