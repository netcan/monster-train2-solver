using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TrueFinalBossProtectedSpellChecks
{
    private static void Require(bool value, string reason) => TrueFinalBossChecks.Require(value, reason);
    private static string Json(object value) => JsonSerializer.Serialize(value, ModelJson.Options);
    internal static void Native(FixtureValue root)
    {
        FixtureValue route = root.GetProperty("OriginalTrueFinalBossRoute");
        Require(route.GetProperty("Scenario").GetString() == "Level9TrueFinalBoss_V2" &&
            route.GetProperty("ScenarioAssetsUnchanged").GetBoolean() && route.GetProperty("CharacterAssetsUnchanged").GetBoolean() &&
            route.GetProperty("StartingDeckUnchanged").GetBoolean() && route.GetProperty("PyreHealth").GetInt32() == 80 &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean() && !root.GetProperty("NativeWon").GetBoolean(),
            "Missing unchanged original route and settled protected-spell defeat.");
        FixtureValue control = root.GetProperty("TrueFinalBossLethalControl");
        string target = control.GetProperty("PaidSpellTarget").GetString()!;
        Require(new[] { "Dominion", "Savagery", "EntropyBot", "EntropyMid", "EntropyTop" }.Contains(target) &&
            control.GetProperty("TargetOwnerOnly").GetBoolean() && !control.GetProperty("RefreshPreview").GetBoolean() &&
            control.GetProperty("RemovedSpellShield").GetInt32() == (target == "Dominion" ? 4 : 0),
            "Missing explicit selected-owner HP and target-specific shield controls.");
        int owner = target == "Dominion" ? 0 : target == "Savagery" ? 1 : 2;
        int[] debuffs = { 1554, 2749, 3499 };
        Require(control.GetProperty("Debuffs").Deserialize<int[]>()!.SequenceEqual(debuffs.Select((value, index) => index == owner ? value : 0)),
            "Protected-spell control must leave unrelated owners at original HP.");
        FixtureValue actions = root.GetProperty("Actions"), turns = root.GetProperty("Turns");
        Require(actions.GetArrayLength() == 1 && turns.GetArrayLength() == 6, "Expected one paid protected spell and six EndTurns.");
        BattleTurnState before = control.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState controlled = control.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        string[] owners = { "Boss_Level9_Dominion_V2", "Boss_Level9_Savagery_V2", "Boss_Level9_Entropy_Bot_V2" };
        Require(control.GetProperty("OwnerIds").Deserialize<int[]>()!.SequenceEqual(owners.Select(asset =>
            before.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.AssetKey == asset).Id)),
            "Recorded protected-spell owners differ from the original roster.");
        Require(Json(TrueFinalBossPaidSpellChecks.Prepare(before, target, true)) == Json(controlled),
            "Selected-owner control changed more than its declared HP/shield inputs.");
        FixtureValue entry = actions[0];
        PlayCardAction action = entry.GetProperty("Action").Deserialize<PlayCardAction>()!;
        BattleTurnState actual = entry.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        string asset = target == "Dominion" ? "Boss_Level9_Dominion_V2" : target == "Savagery" ? "Boss_Level9_Savagery_V2" :
            "Boss_Level9_Entropy_" + target[7..] + "_V2";
        CombatUnit hit = controlled.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == action.TargetUnitId);
        LifeLinkContext links = controlled.Spawn.Train.Context!.LifeLinks!;
        Require(hit.AssetKey == asset && hit.Health == 1 && LifeLinkModel.Protected(links, hit.Id) &&
            LifeLinkModel.Test(links, hit.Id, 1) is { Triggered: true, Damage: 0, Prevented: 1 } &&
            controlled.Energy == 3 && actual.Energy == 2 && actual.Spawn.Train.Context!.AllScenarioBossesDead == false &&
            Json(links) == Json(actual.Spawn.Train.Context.LifeLinks!) &&
            actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == hit.Id).Status("pyregel")?.Stacks == 2 &&
            BattleTurnChecks.Comparable(controlled) == BattleTurnChecks.Comparable(entry.GetProperty("Before").Deserialize<BattleTurnState>()!),
            "Protected paid hit must spend energy, prevent death/damage and still apply the original Pyregel effect.");
        RoomCombatState hitRoom = controlled.Spawn.Train.Rooms.Single(room => room.Units.Any(unit => unit.Id == hit.Id));
        Require(!TrueFinalBossTerminalCombatModel.SupportsRoot(hitRoom, hit.Id), "Healthy independent owners must retain the terminal guard.");
        var frontier = BattleActionModel.EnumerateActions(controlled, action.CardInstanceId, includeAbilities: false)
            .SupportedCandidates.Where(candidate => candidate.Action.TargetUnitId == hit.Id && candidate.Action.RoomIndex == action.RoomIndex).ToArray();
        Require(frontier.Length > 0 && frontier.All(candidate => candidate.Result.Outcome != RoomOutcome.BattleWon),
            "The search frontier must retain the protected paid spell without declaring a false victory.");
        BattleTurnState deployment = turns[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        string parent = Json(deployment);
        void Replay()
        {
            BattleTurnResult born = BattleTurnModel.EndTurn(deployment);
            Require(born.Supported && BattleTurnChecks.Comparable(born.State!) == BattleTurnChecks.Comparable(before),
                "Independent original five-body birth differs.");
            BattleActionResult played = BattleActionModel.PlayCard(TrueFinalBossPaidSpellChecks.Prepare(born.State!, target, true), action);
            Require(played.Supported && played.Outcome != RoomOutcome.BattleWon &&
                BattleTurnChecks.Comparable(played.State!) == BattleTurnChecks.Comparable(actual),
                "Independent protected paid successor differs: " + played.Reason);
            BattleTurnChecks.RunIndependentChain(played.State!, turns, true, startIndex: 1);
        }
        Replay();
        Parallel.For(0, 16, _ => Replay());
        Require(Json(deployment) == parent, "Protected spell chains mutated their deployment root.");
        Require(new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }.All(key => root.GetProperty(key).GetInt32() == 0),
            "Protected spell acceptance requires all four native failure counters zero.");
        Console.WriteLine("TRUE-FINAL-BOSS-PROTECTED-SPELL PASS target=" + target +
            "; original paid spell, zero damage/one prevented, Pyregel effect, healthy independent owners and 16 immutable full battle chains.");
    }
}
