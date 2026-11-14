using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TrueFinalBossMaximumBattleChecks
{
    private static void Require(bool condition, string reason) => TrueFinalBossChecks.Require(condition, reason);
    private static string Json(object? value) => JsonSerializer.Serialize(value, ModelJson.Options);
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path); FixtureValue root = document.RootElement;
        FixtureValue control = root.GetProperty("TrueFinalBossMaximumBattle"), route = root.GetProperty("OriginalTrueFinalBossRoute");
        Require(root.GetProperty("GameVersion").GetString() == "2.3.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            root.GetProperty("TerminalCaptureBoundary").GetString() == "AfterStopCombatLoop" &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }.All(key => root.GetProperty(key).GetInt32() == 0) &&
            control.GetProperty("Prepared").GetBoolean() && control.GetProperty("CompletedControlPlays").GetInt32() == 10 &&
            control.GetProperty("AuthoredCardsAndDrawOrder").GetBoolean() && control.GetProperty("OriginalBossDefinitionsAndBirthStats").GetBoolean() &&
            route.GetProperty("Scenario").GetString() == "Level9TrueFinalBoss_V2" && route.GetProperty("ScenarioAssetsUnchanged").GetBoolean() &&
            route.GetProperty("CharacterAssetsUnchanged").GetBoolean(), "Incomplete original-boss maximum-health paid battle.");
        int[] cards = control.GetProperty("ControlCardIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(cards.Length == 10 && cards.Distinct().Count() == 10 && actions.Length == 10 && root.GetProperty("Turns").GetArrayLength() == 4 &&
            actions.Select(row => row.GetProperty("Action").GetProperty("CardInstanceId").GetInt32()).SequenceEqual(cards),
            "Missing ten actual paid inputs and four natural EndTurns.");
        BattleTurnState Before(int index) => actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int index) => actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        BattleTurnState prepared = control.GetProperty("Actual").Deserialize<BattleTurnState>()!, original = control.GetProperty("Before").Deserialize<BattleTurnState>()!;
        Require(original.Spawn.Turn == 1 && prepared.Spawn.Turn == 1 &&
            Json(original.Spawn.Train.Rooms.Select(room => room.Units)) == Json(prepared.Spawn.Train.Rooms.Select(room => room.Units)) &&
            // Native pile enumeration runs from the bottom to the top.
            prepared.Spawn.Train.Context!.Cards.Hand.Select(card => card.InstanceId).SequenceEqual(cards.Take(3).Reverse()) &&
            prepared.Spawn.Train.Context.Cards.Draw.Select(card => card.InstanceId).SequenceEqual(cards.Skip(3).Reverse()) &&
            actions.Select(row => row.GetProperty("Before").GetProperty("Spawn").GetProperty("Turn").GetInt32()).SequenceEqual(
                new[] { 1, 1, 1, 2, 2, 2, 2, 3, 3, 3 }), "Card/deck preparation changed boss actors or the native draw/turn schedule differs.");
        string[] types = ["DebuffHealth", "BuffHealth", "DebuffHealth", "DebuffHealth", "DebuffHealth", "DebuffHealth", "DebuffHealth",
            "DebuffHealth", "DebuffHealth", "DebuffHealth"];
        int[] amounts = [99999, 9, 2, 99999, 99999, 99999, 99999, 99999, 99999, 7];
        foreach (int index in Enumerable.Range(0, cards.Length))
        {
            CardInstanceState card = prepared.Spawn.Train.Context!.FindCard(cards[index])!;
            CardPlayRule rule = prepared.PlayRules!.Cards.Single(rule => rule.DataId == card.DataId);
            Require(card.DataId == "fc231b61-0000-4000-8000-" + (index + 1).ToString("D12") && rule.Effects.Count == 1 &&
                rule.Effects[0].Type == types[index] && rule.Effects[0].Value == amounts[index] && rule.Cost == (index is 0 or 3 or 4 ? 1 : 0),
                "The declared authored input effect/cost differs: " + index);
        }
        CombatUnit Boss(BattleTurnState state, string name) => state.Spawn.Train.Rooms.SelectMany(room => room.Units)
            .Single(unit => unit.AssetKey == "Boss_Level9_" + name + "_V2");
        Require(new[] { "Dominion", "Savagery", "Entropy_Bot", "Entropy_Mid", "Entropy_Top" }.Select(name => Boss(original, name).Health)
            .SequenceEqual(new[] { 1555, 2750, 3500, 3500, 3500 }), "Paid maximum-health controls changed original born boss HP.");
        BattleTurnState deployment = root.GetProperty("Turns").EnumerateArray().First().GetProperty("Before").Deserialize<BattleTurnState>()!;
        Require(deployment.Spawn.QueuedTrueFinalBoss?.ClearBirthImmobile == true && original.Spawn.OuterBoss?.MovementSpeed == 1 &&
            new[] { "Entropy_Bot", "Entropy_Mid", "Entropy_Top" }.All(name => Boss(original, name).Status("immobile") == null &&
                Boss(original, name).StatusRegistry!.Single(status => status.Id == "immobile").Stacks == 0),
            "Native 2.3.1 birth failed to clear immobile while preserving its zero-stack registry.");
        void Expect(int index, string name, int hp, int max)
        {
            CombatUnit unit = Boss(After(index), name);
            Require(unit.Health == hp && unit.MaxHealth == max, $"Native maximum-health witness differs: {index}/{name}");
        }
        foreach (string name in new[] { "Entropy_Bot", "Entropy_Mid", "Entropy_Top" })
        { Expect(0, name, 1, 1); Expect(1, name, 10, 10); Expect(2, name, 8, 8); Expect(9, name, 1, 1); }
        Expect(3, "Savagery", 1, 1); Expect(4, "Dominion", 1, 1);
        foreach (int index in Enumerable.Range(4, 5))
        {
            Expect(index, "Dominion", 1, 1);
            Require(Boss(After(index), "Dominion").Status("spell shield")?.Stacks == 4,
                "Maximum-health debuffs incorrectly consumed the native damage-only spell shield.");
        }
        foreach (int index in new[] { 0, 3, 4 }) Require(After(index).Energy == Before(index).Energy - 1, "Actual maximum-health play did not spend its ember.");
        Require(!LifeLinkModel.Protected(Before(9).Spawn.Train.Context!.LifeLinks!, Boss(Before(9), "Entropy_Mid").Id) &&
            After(9).Spawn.Train.Context!.LifeLinks!.Actors.All(actor => actor.Health == 1),
            "Independent protection did not release before the final original linked-boss cleanup.");
        BattleTurnState final = root.GetProperty("Turns").EnumerateArray().Last().GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(final.Spawn.Train.Context!.LifeLinks == null && final.Spawn.Train.Context.AllScenarioBossesDead == true &&
            final.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit => unit.Team == CombatTeam.Enemy && unit.IsBoss != true),
            "Native Pyre attack failed to finish the five linked bosses while ordinary waves remain.");
        FixtureValue[] harvest = control.GetProperty("TerminalHarvest").EnumerateArray().ToArray();
        Require(harvest.Length == 2 && harvest.Select(row => row.GetProperty("ActorId").GetInt32()).Distinct().Count() == 2 &&
            harvest.All(row => row.GetProperty("Kind").GetString() == "OnAnyUnitDeathOnFloor" && row.GetProperty("Count").GetInt32() == 1 &&
                row.GetProperty("DyingId").GetInt32() == Boss(Before(9), "Entropy_Bot").Id && row.GetProperty("DyingHp").GetInt32() == 1),
            "Native terminal removal lost a bottom-floor Harvest observer or changed its source/count.");
        foreach (FixtureValue row in harvest)
        {
            CombatUnit observer = row.GetProperty("Before").Deserialize<CombatUnit>()!;
            CombatUnit survivor = final.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == observer.Id);
            Require(observer.AssetKey == "HeavyT2_Zealot" && observer.BaseAttack == 3 && survivor.BaseAttack == 4,
                "A native terminal Harvest observer failed to gain exactly one attack.");
        }
        RoomCombatChecks.NativeRoomOnly(path); TrainCombatChecks.Native(root); BattleTurnChecks.Native(root);
        // The authored hand/deck is the explicit starting control at turn one;
        // All subsequent draws, three combat rounds and terminal Pyre attacks are native.
        RecordedBattleChecks.Native(root, firstTurn: 1, completed: state => RecordedBattleChecks.Compare(state, final, true, "maximum-health native Pyre terminal chain"));
        foreach (int index in new[] { 0, 1, 2, 3, 8, 9 })
        {
            PlayCardAction input = actions[index].GetProperty("Action").Deserialize<PlayCardAction>()!;
            var frontier = BattleActionModel.EnumerateActions(Before(index), input.CardInstanceId, includeAbilities: false);
            var candidate = frontier.SupportedCandidates.Single(item => item.Action.RoomIndex == input.RoomIndex && item.Action.TargetUnitId == input.TargetUnitId);
            RecordedBattleChecks.Compare(candidate.Result.State!, After(index), false, "native maximum-health action in search frontier");
        }
        BattleTurnState source = Before(0); string parent = Json(source);
        var serial = BattleSearchModel.Minimize(source, BattlePlanner.Evaluate, new(128, 1, 1));
        var parallel = BattleSearchModel.Minimize(source, BattlePlanner.Evaluate, new(128, 1, 4));
        foreach (var search in new[] { serial, parallel })
        {
            Require(search.BestState != null && !search.HasUnsupportedAttempts && search.Steps.Count > 0, "Maximum-health bounded search failed.");
            BattleTurnState replayed = BattlePlannerChecks.Replay(source, search.Steps, out var outcome);
            Require(Json(replayed) == Json(search.BestState) && outcome == search.Outcome, "Maximum-health search failed independent full plan replay.");
        }
        Require(Json(serial.BestState) == Json(parallel.BestState) && Json(source) == parent,
            "Maximum-health serial/parallel search diverged or mutated the parent.");
        Console.WriteLine("TRUE-FINAL-BOSS-MAXIMUM-BATTLE PASS: ten actual plays, four EndTurns, original five-body victory, shared HP/max across three combat rounds, ember costs, maximum debuffs preserve spell shields, protection release, native Pyre terminal cleanup with ordinary waves alive, full state/RNG replay, 16 immutable branches and 128-state serial/parallel search with executable replay. Authored cards do not increase original card coverage.");
    }
}
