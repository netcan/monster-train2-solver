using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalUnitUpgradeTriggerRelicChecks
{
    private const string CarrierCardId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";

    internal static PlayCardAction? Choose(BattleTurnState source)
    {
        if (source.PlayRules == null || source.Spawn.Train.Context == null)
            return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);

        IReadOnlyList<PlayCardAction> plays = BattleActionModel.EnumerateSupportedPlays(source);
        foreach (CardToken card in source.Spawn.Train.Context.Cards.Hand.Where(card => card.DataId == CarrierCardId))
        {
            PlayCardAction? summon = plays.FirstOrDefault(action => action.CardInstanceId == card.InstanceId &&
                action.RoomIndex == 0 && action.ActivatorUnitId == 0 && action.TargetUnitId == 0);
            if (summon != null) return summon;
        }
        return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
    }

    internal static void Native(FixtureValue fixture)
    {
        if (!fixture.TryGetProperty("UnitUpgradeTriggerEvents", out FixtureValue observations))
            throw new InvalidOperationException("Native trigger-relic archive has no trigger observations.");

        FixtureValue[] events = observations.EnumerateArray().ToArray();
        FixtureValue[] added = events.Where(item => item.GetProperty("Kind").GetString() == "OnTroopAdded").ToArray();
        FixtureValue[] removed = events.Where(item => item.GetProperty("Kind").GetString() == "OnTroopRemoved").ToArray();
        Require(events.Length > 0 && events.All(item => item.GetProperty("Completed").GetBoolean() &&
            item.GetProperty("CanFire").GetBoolean() && item.GetProperty("FromRunningQueue").GetBoolean() &&
            item.GetProperty("Subtypes").GetArrayLength() > 0), "Native Horde trigger callbacks were incomplete or not running from the queue.");
        Require(added.Any(item => Delta(item) == 4 && item.GetProperty("ParamInt2").GetInt32() == 2),
            "Two native Horde additions did not apply both +2 relic upgrades.");
        Require(removed.Any(item => Delta(item) == -2 && item.GetProperty("ParamInt2").GetInt32() > 0),
            "A native Horde removal did not remove one matching +2 relic upgrade.");

        string summary = string.Join("; ", events.Select(item =>
            $"{item.GetProperty("Kind").GetString()} actor={item.GetProperty("ActorId").GetInt32()} " +
            $"attack={item.GetProperty("AttackBefore").GetInt32()}->{(item.GetProperty("AttackAfter").ValueKind == FixtureKind.Null ? "null" : item.GetProperty("AttackAfter").GetInt32().ToString())} " +
            $"paramInt2={item.GetProperty("ParamInt2").GetInt32()} queue={item.GetProperty("FromRunningQueue").GetBoolean()}"));
        Console.WriteLine($"NATIVE-UNIT-UPGRADE-TRIGGER-RELIC PASS: {added.Length} added, {removed.Length} removed; {summary}");
    }

    private static int? Delta(FixtureValue item)
    {
        FixtureValue after = item.GetProperty("AttackAfter");
        return after.ValueKind == FixtureKind.Null ? null : after.GetInt32() - item.GetProperty("AttackBefore").GetInt32();
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
