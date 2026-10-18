using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class EclipseChamberChecks
{
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        CombatContext context = source.Spawn.Train.Context!;
        for (int index = 0; index < ids.Length; index++)
            if (context.Statistics!.Value(ids[index], "TimesPlayed", "ThisBattle") == 0)
                return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == (index == 1 ? 1 : 0));
        return null;
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("EclipseChamberPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original Eclipse witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int characters = 0, support = 0;
        foreach (FixtureValue row in root.GetProperty("EclipseChamberRecords").EnumerateArray())
        {
            Require(row.GetProperty("Completed").GetBoolean(), "Incomplete native moon refresh.");
            var before = row.GetProperty("Before").Deserialize<TrainCombatState>()!;
            var after = row.GetProperty("After").Deserialize<TrainCombatState>()!;
            bool refresh = row.GetProperty("Characters").GetBoolean();
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            void Replay()
            {
                var result = RoomMoonPhaseModel.Refresh(before, refresh);
                Require(result.Supported, result.UnsupportedReason ?? "Refresh unsupported.");
                Equal(result.State, after, "native complete resource/character refresh");
            }
            Replay(); Parallel.For(0, 16, _ => Replay());
            Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Refresh mutated its parent.");
            Require(before.Context!.QueryFrame!.MoonPhase == after.Context!.QueryFrame!.MoonPhase && before.Context.BattleRng.Equals(after.Context.BattleRng),
                "Resource refresh must not advance global phase or RNG.");
            if (refresh) characters++; else support++;
        }
        Require(characters == 2 && support > 0, "Missing original full refresh and queued support-card controls.");
        int[] ids = root.GetProperty("EclipseChamberCardIds").Deserialize<int[]>()!;
        var actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 8 && actions.Length == 8, "Missing deployment, original Moonrise, two Eclipses, replacement or later birth.");
        BattleTurnState After(int index) => actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        foreach (int index in new[] { 4, 5, 7 })
        {
            BattleTurnState state = After(index);
            Require(state.MoonPhase == "Full" && state.Spawn.Train.Rooms[0].Units.All(unit => unit.MoonPhaseMask == 7 &&
                unit.Status("fullmoon")?.Stacks == 1 && unit.Status("newmoon") == null),
                "Room understanding must include both phases while icons show only the global Full phase.");
            Require(state.Spawn.Train.Rooms[1].Units.Single().MoonPhaseMask == 6, "Eclipse leaked into the other floor.");
        }
        Require(After(6).Spawn.Train.Rooms[0].Units.All(unit => unit.MoonPhaseMask == 6), "Replacing Eclipse failed to remove its extra New bit.");
        int pulses = 0, controls = 0, deployment = 0, terminal = 0;
        foreach (FixtureValue row in root.GetProperty("TrainPhases").EnumerateArray())
        {
            if (row.GetProperty("Kind").GetString() != "Combat") continue;
            var before = row.GetProperty("Before").Deserialize<TrainCombatState>()!;
            var after = row.GetProperty("Actual").Deserialize<TrainCombatState>()!;
            foreach (CombatUnit unit in before.Rooms.SelectMany(room => room.Units).Where(unit => ids.Contains(unit.SpawnerCardId)))
            {
                CombatUnit? updated = after.Rooms.SelectMany(room => room.Units).FirstOrDefault(item => item.Id == unit.Id);
                if (updated == null) continue;
                bool turnZero = before.Context!.QueryFrame!.Turn == 0;
                bool won = after.Context!.AllScenarioBossesDead == true;
                bool positive = !turnZero && !won && (unit.SpawnerCardId == ids[0] || unit.SpawnerCardId == ids[5]);
                Require(updated.BaseAttack - unit.BaseAttack == (positive ? 15 : 0), "Native Silent/floor/deployment post-combat boundary differs.");
                if (turnZero) deployment++; else if (won) terminal++; else if (positive) pulses++; else controls++;
            }
        }
        Require(pulses >= 4 && controls >= 4 && deployment >= 3 && terminal >= 3, "Missing positive Silent growth, ordinary/outside-floor, deployment or terminal controls.");
        RecordedBattleChecks.Native(root);
        Console.WriteLine($"ECLIPSE-CHAMBER PASS: two original room plays, replacement, later birth, {characters} character/{support} support refreshes, {pulses} Silent growth and {controls} floor/subtype controls, exact immutable action chains.");
    }
    private static void Equal<T>(T predicted, T actual, string label)
    {
        string? difference = ModelJson.Difference(JsonSerializer.Serialize(predicted, ModelJson.Options), JsonSerializer.Serialize(actual, ModelJson.Options));
        Require(difference == null, label + ": " + difference);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
