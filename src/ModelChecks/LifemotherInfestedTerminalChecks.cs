using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class LifemotherInfestedTerminalChecks
{
    internal static void Native(FixtureValue fixture)
    {
        Require(fixture.GetProperty("GameVersion").GetString() == Game231FixtureCatalog.Version &&
            fixture.GetProperty("GameModuleMvid").GetString() == Game231FixtureCatalog.Module &&
            fixture.GetProperty("OriginalUnrulyBattle").GetProperty("Scenario").GetString() == "SoulSavior_R5_BossBattle_Lifemother_Infested" &&
            fixture.GetProperty("TerminalEffectsSettled").GetBoolean() && !fixture.GetProperty("NativeWon").GetBoolean(),
            "Expected the settled original Lifemother Infested defeat.");

        FixtureValue ascend = fixture.GetProperty("TrainPhases")[7];
        Require(ascend.GetProperty("Kind").GetString() == "Ascend" && ascend.GetProperty("Turn").GetInt32() == 3,
            "Expected the original terminal Ascend phase.");
        var before = ascend.GetProperty("Before").Deserialize<TrainCombatState>()!;
        var actual = ascend.GetProperty("Actual").Deserialize<TrainCombatState>()!;
        Require(HivemindTurnUpgrade.IsInfestedTerminalAscension(before) &&
            before.Rooms[3].Units is [{ IsPyre: true, Health: 78 }] &&
            before.Rooms[2].Units.Select(unit => unit.Id).SequenceEqual(actual.Rooms[3].Units.Select(unit => unit.Id)),
            "Expected a healthy Pyre followed by the same three original arriving actors.");
        string immutable = Serialize(before);
        TrainCombatResult moved = TrainCombatModel.Ascend(before);
        Require(moved.Supported && moved.Outcome == RoomOutcome.PlayerDefeated,
            "Original terminal ascension did not replay: " + moved.UnsupportedReason);
        Match(moved.State!, actual, "Terminal ascension");
        Require(Serialize(before) == immutable, "Terminal ascension changed its parent state.");

        CheckPyreRoom(fixture.GetProperty("Stages")[31], exchange: false);
        CheckPyreRoom(fixture.GetProperty("Stages")[32], exchange: true);
        Console.WriteLine("NATIVE-INFESTED-TERMINAL PASS: terminal ascension, Pyre room and exchange, full state equality.");
    }

    private static void CheckPyreRoom(FixtureValue phase, bool exchange)
    {
        Require(phase.GetProperty("Kind").GetString() == (exchange ? "Exchange" : "Room") &&
            phase.GetProperty("Turn").GetInt32() == 3, "Wrong native Pyre stage kind or turn.");
        var before = phase.GetProperty("Before").Deserialize<RoomCombatState>()!;
        var actual = phase.GetProperty("Actual").Deserialize<RoomCombatState>()!;
        Require(HivemindTurnUpgrade.IsInfestedTerminalPyre(before) && actual.Units.All(unit => !unit.IsPyre) &&
            actual.Units.Select(unit => unit.Id).SequenceEqual(before.Units.Where(unit => !unit.IsPyre).Select(unit => unit.Id)),
            "Expected the same native attackers and removal of the Pyre.");
        var altered = before.WithUnits(before.Units.Where(unit => !unit.IsPyre).ToArray());
        Require(!HivemindTurnUpgrade.IsInfestedTerminalPyre(altered), "Terminal Pyre admission accepted a missing Pyre.");
        string immutable = Serialize(before);
        RoomCombatResult replayed = exchange ? RoomCombatModel.Exchange(before,
            pendingPhysicalDeathIds: phase.GetProperty("PendingPhysicalDeaths").Deserialize<int[]>()) : RoomCombatModel.Resolve(before);
        Require(replayed.Supported && replayed.Outcome == (exchange ? RoomOutcome.Exchanged : RoomOutcome.PlayerDefeated),
            "Original Pyre phase did not replay: " + replayed.UnsupportedReason);
        Match(replayed.State!, actual, exchange ? "Terminal Pyre exchange" : "Terminal Pyre room");
        Require(Serialize(before) == immutable, "Pyre replay changed its parent state.");
    }

    private static void Match<T>(T predicted, T actual, string subject)
    {
        string? difference = ModelJson.Difference(Serialize(predicted), Serialize(actual));
        Require(difference == null, subject + " differs at " + difference);
    }

    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidDataException(message);
    }
}
