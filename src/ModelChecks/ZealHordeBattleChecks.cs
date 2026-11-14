using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class ZealHordeBattleChecks
{
    internal static void Native(string path, bool maximumHealth = false)
    {
        using var document = ModelJson.ReadFixture(path); FixtureValue root = document.RootElement;
        Require(root.GetProperty("GameVersion").GetString() == "2.3.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            root.GetProperty("ModifierScenario").GetString() == (maximumHealth ? "horde-maximum-battle" : "zeal-horde-battle") &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }.All(key => root.GetProperty(key).GetInt32() == 0),
            "Incomplete native Zeal/Horde battle.");
        FixtureValue evidence = root.GetProperty(maximumHealth ? "HordeMaximumBattle" : "ZealHordeBattle");
        if (maximumHealth) Require(evidence.GetProperty("MaximumHealth").GetBoolean(), "Missing direct maximum-health control mode.");
        FixtureValue owner = evidence.GetProperty("OwnerDefinition");
        Require(evidence.GetProperty("Prepared").GetBoolean() && evidence.GetProperty("CompletedControlPlays").GetInt32() == 5 &&
            owner.GetProperty("Id").GetString() == "ee429978-fa41-4c66-93c6-3d3c7d5a4733" &&
            owner.GetProperty("Key").GetString() == "ZealDamageIncrease" && owner.GetProperty("Type").GetString() == RelicModel.ModifyStatusMagnitude &&
            owner.GetProperty("Int").GetInt32() == 1 && owner.GetProperty("Team").GetString() == "Monsters" &&
            owner.GetProperty("Conditions").GetInt32() == 0 && owner.GetProperty("Statuses")[0].GetProperty("statusId").GetString() == "zeal",
            "Missing unchanged original Zeal owner or actual control inputs.");
        var actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(actions.Length == 5 && root.GetProperty("Turns").GetArrayLength() == 7, "Expected five actual inputs and seven complete turns.");
        BattleTurnState Before(int index) => actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int index) => actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatUnit Troop(BattleTurnState state) => state.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Status("horde") != null);
        CombatUnit original = Troop(Before(2)), partial = Troop(After(2)), added = Troop(After(3)), removed = Troop(After(4));
        Require(original.HordeDefinition is { Attack: 60, Health: 99999 } && original.Status("horde")!.Stacks == 4 &&
            original.Modifiers!.AttackDamage == 240 && original.Health == 99999 && original.MaxHealth == 99999 &&
            partial.Status("horde")!.Stacks == 1 && partial.Health == 99998 && partial.MaxHealth == 99998 &&
            partial.Modifiers!.AttackDamage == 60 &&
            // Each authored two-gold trigger rounds to the native minimum five.
            After(2).Spawn.Train.Context!.Gold - Before(2).Spawn.Train.Context!.Gold == 15,
            "Missing capped-health three-casualty and Harvest witness.");
        if (maximumHealth)
        {
            Require(new[] { original, partial, added, removed }.All(unit => unit.Status("zeal") == null &&
                    unit.Modifiers!.DamageBuffFromStatusEffects == 0 && unit.Triggers.Any(trigger => trigger.Kind == "OnDeathwish") &&
                    unit.Triggers.Any(trigger => trigger.Kind == "OnDeathwishLost")) &&
                added.Status("horde")!.Stacks == 1 && added.Health == 99999 && added.MaxHealth == 99999 &&
                removed.Status("horde")!.Stacks == 1 && removed.Health == 99996 && removed.MaxHealth == 99996 &&
                After(3).Spawn.Train.Context!.FindCard(original.SpawnerCardId)!.Temporary.Offsets.Health -
                    Before(3).Spawn.Train.Context!.FindCard(original.SpawnerCardId)!.Temporary.Offsets.Health == 2,
                "Missing direct maximum-health debuff, capped healing, lifecycle triggers or temporary spawner offset.");
        }
        else Require(original.Status("zeal")!.Stacks == 3 && partial.Status("zeal")!.Stacks == 2 &&
            partial.Modifiers!.DamageBuffFromStatusEffects == 4 &&
            added.Status("horde")!.Stacks == 1 && added.Status("zeal")!.Stacks == 4 && added.Health == 99998 && added.MaxHealth == 99999 &&
            removed.Status("horde")!.Stacks == 1 && removed.Status("zeal") == null && removed.Health == 99995 && removed.MaxHealth == 99995 &&
            removed.Modifiers!.DamageBuffFromStatusEffects == 0, "Missing unhealed addition and final removal control.");
        foreach (string duration in new[] { "ThisTurn", "ThisBattle" })
            Require(After(2).Spawn.Train.Context!.Statistics!.Values.Single(value => value.CardId == original.SpawnerCardId &&
                value.Duration == duration && value.Type == "SpawnedMonsterDeaths").Value == 3,
                "Missing three simulated troop deaths on the original spawner.");
        RoomCombatChecks.NativeRoomOnly(path); TrainCombatChecks.Native(root); BattleTurnChecks.Native(root); RecordedBattleChecks.Native(root);
        BattleTurnState source = Before(2); string parent = Serialize(source);
        var frontier = BattleActionModel.EnumerateActions(source);
        PlayCardAction input = actions[2].GetProperty("Action").Deserialize<PlayCardAction>()!;
        var candidate = frontier.SupportedCandidates.Single(action => action.Action.CardInstanceId == input.CardInstanceId &&
            action.Action.RoomIndex == input.RoomIndex && action.Action.TargetUnitId == input.TargetUnitId);
        RecordedBattleChecks.Compare(candidate.Result.State!, After(2), false, "searchable casualty action versus native");
        Require(frontier.UnsupportedAttempts.Count == 0, "Unmodeled Zeal/Horde candidate.");
        var serial = BattleSearchModel.Minimize(source, BattlePlanner.Evaluate, new(128, 1, 1));
        var parallel = BattleSearchModel.Minimize(source, BattlePlanner.Evaluate, new(128, 1, 4));
        foreach (var search in new[] { serial, parallel })
        {
            Require(search.BestState != null && !search.HasUnsupportedAttempts && search.Steps.Count > 0, "Zeal/Horde search failed.");
            var replay = BattlePlannerChecks.Replay(source, search.Steps, out var outcome);
            Require(Serialize(replay) == Serialize(search.BestState) && outcome == search.Outcome, "Search failed independent full replay.");
        }
        Require(Serialize(serial.BestState) == Serialize(parallel.BestState) && Serialize(source) == parent,
            "Serial/parallel Zeal/Horde search differs or mutated its source.");
        Console.WriteLine((maximumHealth ? "HORDE-MAXIMUM-BATTLE" : "ZEAL-HORDE-BATTLE") +
            " PASS: five actual plays, capped four-to-one troops, three simulated deaths/Harvest, " +
            (maximumHealth ? "direct health loss/healing with Deathwish triggers and spawner offset, " : "unchanged original Zeal relic, ") +
            "full seven-turn successors/RNG and 16 immutable branches, 128-state serial/parallel action search and executable replay.");
    }
    private static string Serialize(object? value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
