using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class HalfHealthBattleChecks
{
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path);
        FixtureValue root = document.RootElement;
        Require(root.GetProperty("GameVersion").GetString() == "2.3.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            root.GetProperty("ModifierScenario").GetString() == "half-health-battle" &&
            root.GetProperty("NativeWon").ValueKind is FixtureKind.True or FixtureKind.False &&
            root.GetProperty("TerminalCaptureBoundary").GetString() == "AfterStopCombatLoop" &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing versioned native battle terminal.");
        foreach (string counter in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(counter).GetInt32() == 0, "Incomplete native battle: " + counter);
        FixtureValue evidence = root.GetProperty("HalfHealthBattle");
        int[] cards = evidence.GetProperty("ControlCardIds").EnumerateArray().Select(item => item.GetInt32()).ToArray();
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        FixtureValue[] turns = root.GetProperty("Turns").EnumerateArray().ToArray();
        FixtureValue[] transitions = evidence.GetProperty("Transitions").EnumerateArray().ToArray();
        Require(evidence.GetProperty("Prepared").GetBoolean() && evidence.GetProperty("CompletedControlPlays").GetInt32() == 5 &&
            cards.Length == 5 && cards.Distinct().Count() == 5 && actions.Length >= 5 && turns.Length > 1 &&
            actions.Take(5).Select(row => row.GetProperty("Action").GetProperty("CardInstanceId").GetInt32()).SequenceEqual(cards),
            "The two paid deployments and three friendly damage/heal controls were not played.");
        foreach (FixtureValue transition in transitions)
        {
            Require(transition.GetProperty("Completed").GetBoolean() && transition.GetProperty("After").ValueKind == FixtureKind.Object,
                "Incomplete half-health callback observation.");
            FixtureValue dispatch = transition.GetProperty("Dispatch");
            CombatUnit before = transition.GetProperty("Before").Deserialize<CombatUnit>()!;
            CombatUnit after = transition.GetProperty("After").Deserialize<CombatUnit>()!;
            Require(dispatch.GetProperty("Kind").GetString() is "OnDeathwish" or "OnDeathwishLost" &&
                dispatch.GetProperty("ActorId").GetInt32() == before.Id && before.Id == after.Id &&
                dispatch.GetProperty("ExclusiveTriggerStateId").ValueKind != FixtureKind.Null &&
                before.Triggers.Any(trigger => HalfHealthDeathwishModel.IsTrigger(trigger) &&
                    trigger.StateId == dispatch.GetProperty("ExclusiveTriggerStateId").GetInt32()), "Half-health callback lost its exclusive source identity.");
            if (transition.GetProperty("Preview").GetBoolean())
                Require(transition.GetProperty("GoldBefore").GetInt32() == transition.GetProperty("GoldAfter").GetInt32(),
                    "Preview callback changed gold.");
        }
        foreach (bool preview in new[] { false, true })
        {
            FixtureValue[] rows = transitions.Where(row => row.GetProperty("Preview").GetBoolean() == preview).ToArray();
            Require(rows.Any(row => Kind(row) == "OnDeathwish" && Hp(row, "After") == Hp(row, "Before") - 2) &&
                rows.Any(row => Kind(row) == "OnDeathwishLost" && Hp(row, "After") > Hp(row, "Before")),
                "Missing nested damage/healing in " + (preview ? "preview" : "real battle") + ".");
            if (!preview)
                foreach (string kind in new[] { "OnDeathwish", "OnDeathwishLost" })
                    Require(rows.Any(row => Kind(row) == kind && row.GetProperty("GoldAfter").GetInt32() > row.GetProperty("GoldBefore").GetInt32()),
                        "Real half-health reward callback did not execute: " + kind);
        }
        BattleTurnState[] states = actions.Concat(turns).SelectMany(row => new[] { "Before", "Actual" }
            .Select(field => row.GetProperty(field).Deserialize<BattleTurnState>()!)).ToArray();
        CombatUnit[] actors = states.SelectMany(state => state.Spawn.Train.Rooms).SelectMany(room => room.Units)
            .Concat(root.GetProperty("Stages").EnumerateArray().SelectMany(stage => new[] { "Before", "Actual" }
                .Select(field => stage.GetProperty(field).Deserialize<RoomCombatState>()!).SelectMany(room => room.Units)))
            .Where(unit => cards.Take(2).Contains(unit.SpawnerCardId)).ToArray();
        Require(actors.Any(unit => unit.Status("undying")?.Stacks > 0) &&
            actors.Any(unit => unit.Health > 0 && (unit.Status("undying")?.Stacks ?? 0) == 0), "Native Undying revival was not observed.");
        BattleTurnState terminal = turns[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(cards.Take(2).Any(id => actors.Any(unit => unit.SpawnerCardId == id) &&
            !terminal.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit => unit.SpawnerCardId == id)),
            "Native battle did not remove a half-health actor.");
        RoomCombatChecks.NativeRoomOnly(path);
        TrainCombatChecks.Native(root);
        RecordedBattleChecks.Native(root);
        Console.WriteLine($"HALF-HEALTH-BATTLE PASS: five control plays; {transitions.Length} complete callbacks ({transitions.Count(row => row.GetProperty("Preview").GetBoolean())} preview); nested wounds/healing, Undying/removal, native room/train phases and complete multi-turn replay with 16 immutable branches.");
    }
    private static string? Kind(FixtureValue row) => row.GetProperty("Dispatch").GetProperty("Kind").GetString();
    private static int Hp(FixtureValue row, string field) => row.GetProperty(field).GetProperty("Health").GetInt32();
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
