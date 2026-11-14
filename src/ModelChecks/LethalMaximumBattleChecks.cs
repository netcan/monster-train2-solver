using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class LethalMaximumBattleChecks
{
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path); FixtureValue root = document.RootElement;
        FixtureValue evidence = root.GetProperty("LethalMaximumBattle");
        string mode = evidence.GetProperty("Mode").GetString()!;
        bool horde = mode.Contains("horde"), undying = mode.Contains("undying"), zero = mode.EndsWith("-zero", StringComparison.Ordinal);
        Require(new[] { "plain", "undying", "horde", "horde-undying", "plain-zero", "undying-zero", "horde-zero", "horde-undying-zero" }.Contains(mode) &&
            root.GetProperty("GameVersion").GetString() == "2.3.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            root.GetProperty("ModifierScenario").GetString() == "lethal-maximum-battle-" + mode &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            root.GetProperty("TerminalCaptureBoundary").GetString() == "AfterStopCombatLoop" &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }.All(key => root.GetProperty(key).GetInt32() == 0),
            "Incomplete current-version lethal maximum-health battle: " + mode);
        int[] cards = evidence.GetProperty("ControlCardIds").EnumerateArray().Select(item => item.GetInt32()).ToArray();
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(evidence.GetProperty("Prepared").GetBoolean() && evidence.GetProperty("CompletedControlPlays").GetInt32() == 5 &&
            actions.Length == 5 && cards.Length == 5 && cards.Distinct().Count() == 5 && root.GetProperty("Turns").GetArrayLength() == 7 &&
            actions.Select(action => action.GetProperty("Action").GetProperty("CardInstanceId").GetInt32()).SequenceEqual(cards),
            "Missing actual lethal-health controls: " + mode);
        BattleTurnState Before(int index) => actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int index) => actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatUnit Victim(BattleTurnState state) => state.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == cards[1]);
        CombatUnit original = Victim(Before(3)); int victimId = original.Id;
        Require(original.Health == (horde ? 19 : 4) && original.MaxHealth == (horde ? 40 : 10) &&
            (original.Status("horde")?.Stacks ?? 0) == (horde ? 2 : 0) && (original.Status("undying")?.Stacks ?? 0) == (undying ? 1 : 0) &&
            original.Triggers.Where(HalfHealthDeathwishModel.IsTrigger).Count() == 2 &&
            original.Triggers.Where(HalfHealthDeathwishModel.IsTrigger).All(trigger => trigger.DeathwishActive == true),
            "Missing active half-health/Horde/Undying predecessor: " + mode);
        CombatUnit? survivor = After(3).Spawn.Train.Rooms.SelectMany(room => room.Units).SingleOrDefault(unit => unit.Id == victimId);
        if (undying)
            Require(survivor != null && survivor.Health == 1 && survivor.MaxHealth == (zero ? 1 : horde ? 20 : 5) &&
                survivor.Status("undying") == null && (survivor.Status("horde")?.Stacks ?? 0) == (horde ? 2 : 0) &&
                survivor.SacrificeCardId == 0 && survivor.DeathState?.IsSacrifice == false &&
                survivor.Triggers.Where(HalfHealthDeathwishModel.IsTrigger).All(trigger => trigger.DeathwishActive == !zero),
                "Lethal maximum-health revival must precede Horde settlement and clamp max HP to one: " + mode);
        else
            Require(survivor == null && After(3).Spawn.Train.Context!.OtherPiles!.Single(pile => pile.Name == "Exhausted")
                .Cards.Any(card => card.InstanceId == cards[1]), "Physical death did not return the victim spawner to Exhausted: " + mode);
        FixtureValue[] callbacks = evidence.GetProperty("Callbacks").EnumerateArray().Where(row => row.GetProperty("Step").GetInt32() == 4).ToArray();
        FixtureValue[] self = callbacks.Where(row => row.GetProperty("ActorId").GetInt32() == victimId).ToArray();
        Require(self.Count(row => Kind(row) == "OnDeathwish") == 0 &&
            self.Count(row => Kind(row) == "OnDeathwishLost") == (zero && undying ? 2 : 0) &&
            self.Count(row => Kind(row) == "OnReanimated") == (undying ? 1 : 0) &&
            self.Count(row => Kind(row) == "OnDeath") == 1, "Unexpected extra half-health update or missing death/revival callback: " + mode);
        FixtureValue death = self.Single(row => Kind(row) == "OnDeath");
        Require(death.GetProperty("ParamInt").GetInt32() == (undying ? 1 : 0) &&
            death.GetProperty("Before").GetProperty("Health").GetInt32() == (undying ? 1 : 0),
            "Wrong native physical/revival death payload: " + mode);
        int observerId = Before(3).Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == cards[0]).Id;
        FixtureValue harvest = callbacks.Single(row => Kind(row) == "OnAnyUnitDeathOnFloor" && row.GetProperty("ActorId").GetInt32() == observerId);
        Require(harvest.GetProperty("Count").GetInt32() == (!undying && horde ? 2 : 1) &&
            harvest.GetProperty("DyingId").GetInt32() == victimId &&
            After(3).Spawn.Train.Context!.Gold - Before(3).Spawn.Train.Context!.Gold ==
                (undying ? 30 + (zero ? 5 : 0) : horde ? 25 : 20),
            "Horde Harvest must multiply physical deaths only; revived actors harvest once: " + mode);
        foreach (string duration in new[] { "ThisTurn", "ThisBattle" })
            Require(After(3).Spawn.Train.Context!.Statistics!.Value(cards[1], "SpawnedMonsterDeaths", duration) -
                    Before(3).Spawn.Train.Context!.Statistics!.Value(cards[1], "SpawnedMonsterDeaths", duration) == 1,
                "Maximum-health sacrifice/revival must record one death on the victim spawner: " + mode);
        Require(Serialize(Before(3).Spawn.Train.Context!.Statistics!.MonsterDamage) ==
            Serialize(After(3).Spawn.Train.Context!.Statistics!.MonsterDamage), "Maximum-health sacrifice invented ordinary damage statistics.");
        Require(After(4).Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == cards[0]).MaxHealth == 102,
            "Following card could not execute after lethal maximum-health changes.");
        RoomCombatChecks.NativeRoomOnly(path); TrainCombatChecks.Native(root); BattleTurnChecks.Native(root); RecordedBattleChecks.Native(root);
        BattleTurnState source = Before(3); string parent = Serialize(source);
        var frontier = BattleActionModel.EnumerateActions(source);
        PlayCardAction input = actions[3].GetProperty("Action").Deserialize<PlayCardAction>()!;
        var candidate = frontier.SupportedCandidates.Single(action => action.Action.CardInstanceId == input.CardInstanceId &&
            action.Action.RoomIndex == input.RoomIndex && action.Action.TargetUnitId == input.TargetUnitId);
        RecordedBattleChecks.Compare(candidate.Result.State!, After(3), false, "searchable native lethal maximum-health input");
        Require(frontier.UnsupportedAttempts.Count == 0, "Unsupported lethal maximum-health search candidate: " + mode);
        var serial = BattleSearchModel.Minimize(source, BattlePlanner.Evaluate, new(128, 1, 1));
        var parallel = BattleSearchModel.Minimize(source, BattlePlanner.Evaluate, new(128, 1, 4));
        foreach (var search in new[] { serial, parallel })
        {
            Require(search.BestState != null && !search.HasUnsupportedAttempts && search.Steps.Count > 0, "Lethal health search failed: " + mode);
            var replay = BattlePlannerChecks.Replay(source, search.Steps, out var outcome);
            Require(Serialize(replay) == Serialize(search.BestState) && outcome == search.Outcome, "Lethal health search failed independent full replay.");
        }
        Require(Serialize(serial.BestState) == Serialize(parallel.BestState) && Serialize(source) == parent,
            "Lethal health serial/parallel search differs or mutated its parent.");
        Console.WriteLine("LETHAL-MAXIMUM-BATTLE PASS: " + mode + "; five actual plays, native sacrifice/revival and half-health callbacks, full seven-turn state/RNG chain, 16 immutable branches and 128-state serial/parallel search with executable replay.");
    }
    private static string? Kind(FixtureValue row) => row.GetProperty("Kind").GetString();
    private static string Serialize(object? value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
