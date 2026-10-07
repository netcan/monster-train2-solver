using MonsterTrain2Poju.Fixtures;
using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class StandbyPileChecks
{
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path);
        FixtureValue entry = document.RootElement.GetProperty("Entry");
        BattleTurnState before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = entry.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction action = entry.GetProperty("Action").Deserialize<PlayCardAction>()!;
        CardPileState standby = before.OtherPiles.Single(pile => pile.Name == "Standby");
        if (standby.FreeSlots == null || standby.FreeSlots.Count == 0 || standby.EntrySlots == null)
            throw new InvalidOperationException("Standby oracle lacks a deleted entry.");
        BattleActionResult result = BattleActionModel.PlayCard(before, action);
        if (!result.Supported) throw new InvalidOperationException(result.Reason);
        string? difference = ModelJson.Difference(BattleTurnChecks.Comparable(result.State!), BattleTurnChecks.Comparable(actual));
        if (difference != null) throw new InvalidOperationException("Native standby insertion differs: " + difference);
        if (actual.OtherPiles.Single(pile => pile.Name == "Standby").EntrySlots![standby.FreeSlots[0]] != action.CardInstanceId)
            throw new InvalidOperationException("Native summon did not reuse the free-list head.");
        string parent = JsonSerializer.Serialize(before);
        string expected = BattleTurnChecks.Comparable(actual);
        Parallel.For(0, 32, _ =>
        {
            var child = BattleActionModel.PlayCard(before, action);
            if (!child.Supported || BattleTurnChecks.Comparable(child.State!) != expected)
                throw new InvalidOperationException("Parallel native standby insertion differs.");
        });
        if (JsonSerializer.Serialize(before) != parent) throw new InvalidOperationException("Native standby parent mutated.");
        Console.WriteLine("NATIVE-STANDBY-CHECKS PASS: captured free-slot reuse, complete summon state and 32 isolated branches.");
    }
}
