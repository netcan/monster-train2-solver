using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TrueFinalBossPaidSpellChecks
{
    private static void Require(bool value, string reason) => TrueFinalBossChecks.Require(value, reason);
    private static string Json(object value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static BattleTurnState Prepare(BattleTurnState before, string target)
    {
        BattleTurnState prepared = TrueFinalBossLethalChecks.Prepare(before);
        if (target != "Dominion") return prepared;
        TrainCombatState train = prepared.Spawn.Train;
        RoomCombatState room = train.Rooms.Single(item => item.RoomIndex == 3);
        CombatUnit dominion = room.Units.Single(unit => unit.AssetKey == "Boss_Level9_Dominion_V2");
        RoomCombatResult removed = StatusRemovalModel.Remove(room, dominion.Id, "spell shield", 4);
        Require(removed.Supported, "Explicit four-shield removal failed: " + removed.UnsupportedReason);
        CombatContext context = removed.State!.Context!;
        return TrueFinalBossLethalChecks.WithTrain(prepared, CardSpellModel.WithContext(new TrainCombatState(
            context.Enchantments!.Rooms, train.Movement, train.EnemySlotsPerRoom, context), context));
    }
    internal static void Native(FixtureValue root)
    {
        FixtureValue route = root.GetProperty("OriginalTrueFinalBossRoute");
        Require(route.GetProperty("Scenario").GetString() == "Level9TrueFinalBoss_V2" &&
            route.GetProperty("ScenarioAssetsUnchanged").GetBoolean() && route.GetProperty("CharacterAssetsUnchanged").GetBoolean() &&
            route.GetProperty("StartingDeckUnchanged").GetBoolean() && route.GetProperty("PyreHealth").GetInt32() == 80 &&
            root.GetProperty("Policy").GetString() == "units-spells-and-junk" && root.GetProperty("BattleDistance").GetInt32() == 8 &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean() && root.GetProperty("TerminalCaptureBoundary").GetString() == "AfterStopCombatLoop",
            "Missing unchanged original route or settled terminal capture.");
        FixtureValue control = root.GetProperty("TrueFinalBossLethalControl");
        string target = control.GetProperty("PaidSpellTarget").GetString()!;
        Require(new[] { "Dominion", "Savagery", "EntropyBot", "EntropyMid", "EntropyTop" }.Contains(target) &&
            !control.GetProperty("RefreshPreview").GetBoolean() && control.GetProperty("RemovedSpellShield").GetInt32() == (target == "Dominion" ? 4 : 0) &&
            control.GetProperty("Debuffs").Deserialize<int[]>()!.SequenceEqual(new[] { 1554, 2749, 3499 }),
            "Missing explicit three-owner HP control and target-specific shield control.");
        BattleTurnState before = control.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState controlled = control.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(Json(Prepare(before, target)) == Json(controlled), "Native preparation changed more than its recorded controls.");
        string[] owners = { "Boss_Level9_Dominion_V2", "Boss_Level9_Savagery_V2", "Boss_Level9_Entropy_Bot_V2" };
        Require(control.GetProperty("OwnerIds").Deserialize<int[]>()!.SequenceEqual(owners.Select(asset =>
            before.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.AssetKey == asset).Id)),
            "Recorded HP owners differ from the unchanged original three-owner roster.");
        FixtureValue turns = root.GetProperty("Turns");
        FixtureValue actions = root.GetProperty("Actions");
        Require(turns.GetArrayLength() == 1 && actions.GetArrayLength() == 1 && root.GetProperty("NativeWon").GetBoolean(),
            "Expected one deployment EndTurn, one paid spell and victory.");
        FixtureValue entry = actions[0];
        BattleTurnState actionBefore = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = entry.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction action = entry.GetProperty("Action").Deserialize<PlayCardAction>()!;
        string asset = target == "Dominion" ? "Boss_Level9_Dominion_V2" : target == "Savagery" ? "Boss_Level9_Savagery_V2" :
            "Boss_Level9_Entropy_" + target[7..] + "_V2";
        Require(BattleTurnChecks.Comparable(controlled) == BattleTurnChecks.Comparable(actionBefore) &&
            actionBefore.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == action.TargetUnitId).AssetKey == asset &&
            actionBefore.Energy == 3 && actual.Energy == 2 && actual.Spawn.OuterBoss == null && actual.Spawn.Train.Context!.LifeLinks == null &&
            actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Single() is { IsPyre: true, Health: 80 },
            "Paid target, one-energy payment or complete five-body cleanup differs.");
        BattleActionEnumeration frontier = BattleActionModel.EnumerateActions(controlled, action.CardInstanceId, includeAbilities: false);
        Require(frontier.SupportedCandidates.Any(candidate => candidate.Action.TargetUnitId == action.TargetUnitId &&
            candidate.Action.RoomIndex == action.RoomIndex && candidate.Result.Outcome == RoomOutcome.BattleWon),
            "The original paid winning spell is missing from the search frontier.");
        BattleTurnState deployment = turns[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        string parent = Json(deployment);
        string expected = BattleTurnChecks.Comparable(actual, true, true);
        string Replay()
        {
            BattleTurnResult born = BattleTurnModel.EndTurn(deployment);
            Require(born.Supported && BattleTurnChecks.Comparable(born.State!) == BattleTurnChecks.Comparable(before),
                "Independent unchanged native birth differs: " + born.UnsupportedReason);
            BattleActionResult played = BattleActionModel.PlayCard(Prepare(born.State!, target), action);
            Require(played.Supported && played.Outcome == RoomOutcome.BattleWon,
                "Independent paid spell failed: " + played.Reason);
            string modeled = BattleTurnChecks.Comparable(played.State!, true, true);
            Require(modeled == expected, "Paid terminal state/RNG differs: " + ModelJson.Difference(modeled, expected));
            return modeled;
        }
        Replay();
        Parallel.For(0, 16, _ => Require(Replay() == expected, "Concurrent paid chain differs."));
        Require(Json(deployment) == parent, "Paid chain mutated its deployment root.");
        Require(new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }.All(key => root.GetProperty(key).GetInt32() == 0),
            "Native paid spell acceptance requires all four failure counters zero.");
        Console.WriteLine("TRUE-FINAL-BOSS-PAID-SPELL PASS target=" + target + "; original spell, one-energy payment, full terminal state/RNG and 16 independent immutable deployment-to-victory chains.");
    }
}
