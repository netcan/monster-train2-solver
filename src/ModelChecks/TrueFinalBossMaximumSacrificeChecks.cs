using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TrueFinalBossMaximumSacrificeChecks
{
    private static void Require(bool condition, string reason) => TrueFinalBossChecks.Require(condition, reason);
    private static string Json(object? value) => JsonSerializer.Serialize(value, ModelJson.Options);
    internal static void Matrix(string directory)
    {
        var searched = new HashSet<string>(StringComparer.Ordinal);
        foreach (string mode in new[] { "zero", "retained" })
        foreach (string target in new[] { "dominion", "savagery", "entropybot", "entropymid", "entropytop" })
            Native(Path.Combine(directory, target + "-" + mode + ".mt2f"), searched);
        TrueFinalBossMaximumCommandChecks.Native(Path.Combine(directory, "terminal-command.mt2f"), searched);
        Console.WriteLine($"LIFE-LINK-SACRIFICE-MATRIX PASS: eleven full native chains; {searched.Count} distinct gameplay decision roots each searched serially and in parallel, identical decision roots reuse successful search validation.");
    }
    internal static void Search(BattleTurnState source, HashSet<string>? searched = null)
    {
        // The production key includes future waves, rules, private history and
        // every RNG. Ignore only the established UI selection/preview allocator.
        using var keys = new BattleStateKeys(ignorePreviewCopyAllocator: true, ignoreRoomSelection: true);
        string key = keys.Key(source);
        if (searched?.Contains(key) == true)
        {
            Console.WriteLine("TERMINAL-SEARCH REUSED: identical complete gameplay decision including RNG; independent native chain/frontier still verified.");
            return;
        }
        string parent = Json(source);
        var serial = BattleSearchModel.Minimize(source, BattlePlanner.Evaluate, new(128, 1, 1));
        var parallel = BattleSearchModel.Minimize(source, BattlePlanner.Evaluate, new(128, 1, 4));
        foreach (var search in new[] { serial, parallel })
        {
            Require(search.BestState != null && search.Outcome == RoomOutcome.BattleWon && !search.HasUnsupportedAttempts && search.Steps.Count > 0,
                "The bounded terminal search failed: " + Json(new { search.Outcome, Steps = search.Steps.Count,
                    Unsupported = search.UnsupportedAttempts.Select(attempt => attempt.Reason).Distinct().ToArray() }));
            BattleTurnState replayed = BattlePlannerChecks.Replay(source, search.Steps, out var outcome);
            Require(Json(replayed) == Json(search.BestState) && outcome == search.Outcome, "Terminal plan replay differs.");
        }
        Require(Json(serial.BestState) == Json(parallel.BestState) && Json(source) == parent, "Terminal search mutated its parent or diverged.");
        searched?.Add(key);
    }
    internal static void Native(string path, HashSet<string>? searched = null)
    {
        using var document = ModelJson.ReadFixture(path);
        FixtureValue root = document.RootElement, control = root.GetProperty("TrueFinalBossMaximumBattle");
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        int[] cards = control.GetProperty("ControlCardIds").Deserialize<int[]>()!;
        string targetName = control.GetProperty("SacrificeTarget").GetString()!;
        bool retain = control.GetProperty("RetainMaximum").GetBoolean();
        Require(new[] { "Dominion", "Savagery", "EntropyBot", "EntropyMid", "EntropyTop" }.Contains(targetName) &&
            control.GetProperty("Prepared").GetBoolean() && control.GetProperty("CompletedControlPlays").GetInt32() == 11 &&
            cards.Length == 11 && cards.Distinct().Count() == 11 && actions.Length == 11 && root.GetProperty("Turns").GetArrayLength() == 3 &&
            actions.Select(row => row.GetProperty("Action").GetProperty("CardInstanceId").GetInt32()).SequenceEqual(cards),
            "Missing original-boss sacrifice inputs: " + path);
        BattleTurnState Before(int index) => actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int index) => actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        BattleTurnState prepared = control.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        BattleTurnState original = control.GetProperty("Before").Deserialize<BattleTurnState>()!;
        Require(Json(original.Spawn.Train.Rooms.Select(room => room.Units)) == Json(prepared.Spawn.Train.Rooms.Select(room => room.Units)) &&
            prepared.Spawn.Train.Context!.Cards.Hand.Select(card => card.InstanceId).SequenceEqual(cards.Take(4).Reverse()) &&
            prepared.Spawn.Train.Context.Cards.Draw.Select(card => card.InstanceId).SequenceEqual(cards.Skip(4).Reverse()) &&
            actions.Select(row => row.GetProperty("Before").GetProperty("Spawn").GetProperty("Turn").GetInt32()).SequenceEqual(
                new[] { 1, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3 }), "The explicit hand/deck control changed actors or natural draws.");
        int[] amounts = [99999, 9, 2, 99999, 99999, 99999, 99999, 99999, 99999, 7, 1];
        for (int index = 0; index < cards.Length; index++)
        {
            CardInstanceState card = prepared.Spawn.Train.Context.FindCard(cards[index])!;
            CardPlayRule rule = prepared.PlayRules!.Cards.Single(rule => rule.DataId == card.DataId);
            Require(card.DataId == "fc231b61-0000-4000-8000-" + (index + 1).ToString("D12") && rule.Effects.Count == 1 &&
                rule.Effects[0].Type == (index == 1 ? "BuffHealth" : retain && index is >= 3 and < 10 ? "Damage" : "DebuffHealth") &&
                rule.Effects[0].Value == amounts[index] && rule.Cost == (index is 0 or 3 or 4 or 10 ? 1 : 0), "Unexpected authored sacrifice input.");
        }
        BattleTurnState terminalParent = Before(10), final = After(10);
        int targetId = actions[10].GetProperty("Action").GetProperty("TargetUnitId").GetInt32();
        CombatUnit target = terminalParent.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == targetId);
        Require(target.AssetKey!.Replace("Boss_Level9_", "").Replace("_V2", "").Replace("_", "") == targetName &&
            terminalParent.Spawn.Train.Context!.LifeLinks!.Actors.All(actor => actor.Health == 1) &&
            !LifeLinkModel.Protected(terminalParent.Spawn.Train.Context.LifeLinks, targetId) &&
            target.MaxHealth == (retain ? targetName == "Dominion" ? 1555 : targetName == "Savagery" ? 2750 : 8 : 1) &&
            final.Energy == terminalParent.Energy - 1 && actions[10].GetProperty("ActualOutcome").Deserialize<RoomOutcome>() == RoomOutcome.BattleWon &&
            final.Spawn.Train.Context!.LifeLinks == null && final.Spawn.Train.Context.AllScenarioBossesDead == true,
            "Maximum-health sacrifice did not finish the original unprotected group or spend its ember.");
        CombatUnit dominion = terminalParent.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.AssetKey == "Boss_Level9_Dominion_V2");
        Require((dominion.Status("spell shield")?.Stacks ?? 0) == (retain ? 0 : 4), "Damage and maximum-health effects consumed the wrong spell shields.");
        // First verify the independent implementation, including diagnostic captures
        // whose recorded model still rejected the newly observed terminal operation.
        RoomCombatChecks.NativeRoomOnly(path); TrainCombatChecks.Native(root); BattleTurnChecks.Native(root);
        RecordedBattleChecks.Native(root, firstTurn: 1, completed: state => RecordedBattleChecks.Compare(state, final, true, "maximum-health sacrifice terminal chain"));
        PlayCardAction input = actions[10].GetProperty("Action").Deserialize<PlayCardAction>()!;
        var frontier = BattleActionModel.EnumerateActions(terminalParent, input.CardInstanceId, includeAbilities: false);
        foreach (LifeLinkActor actor in terminalParent.Spawn.Train.Context.LifeLinks.Actors)
            Require(frontier.SupportedCandidates.Any(candidate => candidate.Action.TargetUnitId == actor.Id && candidate.Result.Outcome == RoomOutcome.BattleWon),
                "A linked-body terminal action is missing from the search frontier.");
        FixtureValue operation = control.GetProperty("TerminalSacrifices").EnumerateArray().Single();
        CombatUnit nativeRoot = operation.GetProperty("Root").Deserialize<CombatUnit>()!;
        Require(nativeRoot.Id == targetId && nativeRoot.Health == 0 && nativeRoot.MaxHealth == target.MaxHealth - 1 &&
            operation.GetProperty("NativeReturned").GetBoolean() && operation.GetProperty("SourceCardId").GetInt32() == 0 &&
            operation.GetProperty("IsSacrifice").GetBoolean() && operation.GetProperty("Destroyed").GetBoolean() && operation.GetProperty("AllBossesDead").GetBoolean(),
            "Native DebuffMaxHP failed to await Sacrifice(null) with the expected HP/max and lifecycle.");
        bool shared = targetName.StartsWith("Entropy");
        CombatUnit[] dying = operation.GetProperty("Actors").Deserialize<CombatUnit[]>()!;
        Require(dying.Length == 5 && dying.All(unit => unit.Health == (unit.Id == targetId || shared && unit.AssetKey!.StartsWith("Boss_Level9_Entropy_") ? 0 : 1)),
            "Native shared health did not reach zero before the root sacrifice.");
        FixtureValue[] removals = control.GetProperty("TerminalRemovals").EnumerateArray().ToArray();
        Require(removals.Length == 5, "Native group removal omitted an original body.");
        var actors = removals[0].GetProperty("ActiveBefore").Deserialize<TrueFinalBossRemovalActor[]>()!;
        var plan = TrueFinalBossRemovalModel.Plan(actors, targetId, true, true, true);
        Require(plan.Supported && plan.Steps.Select(step => step.Id).SequenceEqual(removals.Select(row => row.GetProperty("Id").GetInt32())) &&
            plan.Steps.Select(step => step.Room).SequenceEqual(removals.Select(row => row.GetProperty("Room").GetInt32())) &&
            plan.Steps.Select(step => step.LastDeath).SequenceEqual(removals.Select(row => row.GetProperty("LastDeath").GetBoolean())) &&
            removals.All(row => row.GetProperty("Death").GetBoolean() && row.GetProperty("Vanish").GetBoolean()), "Native group removal order/flags differ.");
        FixtureValue[] harvest = control.GetProperty("TerminalHarvest").EnumerateArray().ToArray();
        Require(harvest.Length == 2 && harvest.All(row => row.GetProperty("Kind").GetString() == "OnAnyUnitDeathOnFloor" &&
            row.GetProperty("Count").GetInt32() == 1 && row.GetProperty("DyingHp").GetInt32() == (shared ? 0 : 1)), "Native terminal Harvest source/count differs.");
        foreach (FixtureValue row in harvest)
        {
            CombatUnit observer = row.GetProperty("Before").Deserialize<CombatUnit>()!;
            Require(observer.AssetKey == "HeavyT2_Zealot" && observer.BaseAttack == 3 && final.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .Single(unit => unit.Id == observer.Id).BaseAttack == 4, "Terminal Harvest lost an original observer's attack.");
        }
        FixtureValue route = root.GetProperty("OriginalTrueFinalBossRoute");
        Require(root.GetProperty("GameVersion").GetString() == "2.3.1" && root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            root.GetProperty("TerminalCaptureBoundary").GetString() == "AfterStopCombatLoop" &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }.All(key => root.GetProperty(key).GetInt32() == 0) &&
            control.GetProperty("AuthoredCardsAndDrawOrder").GetBoolean() && control.GetProperty("OriginalBossDefinitionsAndBirthStats").GetBoolean() &&
            route.GetProperty("Scenario").GetString() == "Level9TrueFinalBoss_V2" && route.GetProperty("ScenarioAssetsUnchanged").GetBoolean() && route.GetProperty("CharacterAssetsUnchanged").GetBoolean(),
            "Native acceptance is incomplete; diagnostic captures cannot become curated evidence.");
        Search(terminalParent, searched);
        Console.WriteLine($"TRUE-FINAL-BOSS-MAXIMUM-SACRIFICE PASS: {targetName}/{(retain ? "retained" : "zero")}; eleven actual plays, three EndTurns, native Sacrifice(null), shared HP/max, five removals, two Harvest observers, full state/RNG chain, 16 immutable branches, five terminal targets and bounded serial/parallel search with executable replay. Authored controls add no original coverage.");
    }
}
