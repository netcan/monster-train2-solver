using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TrueFinalBossPaidSpellChecks
{
    private static void Require(bool value, string reason) => TrueFinalBossChecks.Require(value, reason);
    private static string Json(object value) => JsonSerializer.Serialize(value, ModelJson.Options);
    internal static BattleTurnState Prepare(BattleTurnState before, string target, bool targetOwnerOnly = false)
    {
        string owner = target == "Dominion" ? "Boss_Level9_Dominion_V2" : target == "Savagery" ? "Boss_Level9_Savagery_V2" : "Boss_Level9_Entropy_Bot_V2";
        BattleTurnState prepared = TrueFinalBossLethalChecks.Prepare(before, targetOwnerOnly ? owner : null);
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
    internal static void Native(FixtureValue root, bool allied = false, bool triggeredAlly = false)
    {
        Require(!triggeredAlly || allied, "Triggered terminal calibration requires its explicitly added ally.");
        FixtureValue route = root.GetProperty("OriginalTrueFinalBossRoute");
        Require(route.GetProperty("Scenario").GetString() == "Level9TrueFinalBoss_V2" &&
            route.GetProperty("ScenarioAssetsUnchanged").GetBoolean() && route.GetProperty("CharacterAssetsUnchanged").GetBoolean() &&
            route.GetProperty("StartingDeckUnchanged").GetBoolean() == !allied && route.GetProperty("PyreHealth").GetInt32() == 80 &&
            root.GetProperty("Policy").GetString() == "units-spells-and-junk" && root.GetProperty("BattleDistance").GetInt32() == 8 &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean() && root.GetProperty("TerminalCaptureBoundary").GetString() == "AfterStopCombatLoop",
            "Missing unchanged original route or settled terminal capture.");
        bool added = root.TryGetProperty("TrueFinalBossOrdinaryAllyControl", out FixtureValue addition) && addition.ValueKind != FixtureKind.Null;
        Require(added == allied && (!added || addition.GetProperty("Kind").GetString() == (triggeredAlly ? "Avenger" : "Steward") &&
            addition.GetProperty("CardDataId").GetString() == (triggeredAlly ? "69fcbe85-a5b7-4a83-bf6d-21888604d99e" : "38e70e6a-20f3-4e36-8d31-c5110be07f87")),
            "Allied paid acceptance requires only its explicitly declared original ally.");
        FixtureValue control = root.GetProperty("TrueFinalBossLethalControl");
        string target = control.GetProperty("PaidSpellTarget").GetString()!;
        BattleTurnState before = control.GetProperty("Before").Deserialize<BattleTurnState>()!;
        Require(!control.TryGetProperty("TargetOwnerOnly", out FixtureValue selectedOwner) || !selectedOwner.GetBoolean(),
            "Paid victory requires the declared three-owner critical-health control.");
        Require(new[] { "Dominion", "Savagery", "EntropyBot", "EntropyMid", "EntropyTop" }.Contains(target) &&
            !control.GetProperty("RefreshPreview").GetBoolean() && control.GetProperty("RemovedSpellShield").GetInt32() == (target == "Dominion" ? 4 : 0) &&
            control.GetProperty("Debuffs").Deserialize<int[]>()!.SequenceEqual(new[] {
                "Boss_Level9_Dominion_V2", "Boss_Level9_Savagery_V2", "Boss_Level9_Entropy_Bot_V2" }.Select(asset =>
                    before.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.AssetKey == asset).Health - 1)),
            "Missing explicit three-owner HP control and target-specific shield control.");
        BattleTurnState controlled = control.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(Json(Prepare(before, target)) == Json(controlled), "Native preparation changed more than its recorded controls.");
        string[] owners = { "Boss_Level9_Dominion_V2", "Boss_Level9_Savagery_V2", "Boss_Level9_Entropy_Bot_V2" };
        Require(control.GetProperty("OwnerIds").Deserialize<int[]>()!.SequenceEqual(owners.Select(asset =>
            before.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.AssetKey == asset).Id)),
            "Recorded HP owners differ from the unchanged original three-owner roster.");
        FixtureValue turns = root.GetProperty("Turns");
        FixtureValue actions = root.GetProperty("Actions");
        Require(turns.GetArrayLength() == 1 && actions.GetArrayLength() == (allied ? 2 : 1) && root.GetProperty("NativeWon").GetBoolean(),
            "Expected one deployment EndTurn, one paid spell and victory.");
        FixtureValue entry = actions[allied ? 1 : 0];
        BattleTurnState actionBefore = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = entry.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction action = entry.GetProperty("Action").Deserialize<PlayCardAction>()!;
        string asset = target == "Dominion" ? "Boss_Level9_Dominion_V2" : target == "Savagery" ? "Boss_Level9_Savagery_V2" :
            "Boss_Level9_Entropy_" + target[7..] + "_V2";
        Require(BattleTurnChecks.Comparable(controlled) == BattleTurnChecks.Comparable(actionBefore) &&
            actionBefore.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == action.TargetUnitId).AssetKey == asset &&
            actionBefore.Energy == 3 && actual.Energy == 2 && actual.Spawn.OuterBoss == null && actual.Spawn.Train.Context!.LifeLinks == null &&
            actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Count() == (allied ? 2 : 1) &&
            actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre).Health == 80 &&
            (!allied || (triggeredAlly ? actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => !unit.IsPyre) is
                { AssetKey: "MonsterAvenger", BaseAttack: 20, Health: 40, MaxHealth: 40, Size: 2, Triggers.Count: 1 } :
                actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => !unit.IsPyre) is
                { AssetKey: "TrainStewardSmall", BaseAttack: 14, Health: 5, MaxHealth: 5, Size: 2 })),
            "Paid target, one-energy payment or complete five-body cleanup differs.");
        BattleActionEnumeration frontier = BattleActionModel.EnumerateActions(controlled, action.CardInstanceId, includeAbilities: false);
        Require(frontier.SupportedCandidates.Any(candidate => candidate.Action.TargetUnitId == action.TargetUnitId &&
            candidate.Action.RoomIndex == action.RoomIndex && candidate.Result.Outcome == RoomOutcome.BattleWon),
            "The original paid winning spell is missing from the search frontier.");
        BattleTurnState deployment = turns[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        if (route.TryGetProperty("CovenantControl", out FixtureValue covenant) && covenant.ValueKind != FixtureKind.Null)
        {
            TrueFinalBossSpawnDefinition definition = deployment.Spawn.QueuedTrueFinalBoss!;
            TrueFinalBossSpawnChecks.CovenantMetadata(covenant, new TrueFinalBossSpawnState(deployment,
                definition.Definitions, definition.QueuedBosses, 0, new int[4], definition.ExternalInteractions));
        }
        BattleTurnState prepared = allied ? addition.GetProperty("Actual").Deserialize<BattleTurnState>()! : deployment;
        if (allied)
        {
            int allyCardId = addition.GetProperty("CardId").GetInt32();
            Require(prepared.Spawn.Train.Rooms.SelectMany(room => room.Units).Single().IsPyre && prepared.Energy == 4 &&
                actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => !unit.IsPyre).SpawnerCardId == allyCardId &&
                prepared.Spawn.Train.Context!.FindCard(allyCardId)!.Permanent.Upgrades.Count == 0,
                "Missing the explicit original added-card boundary and surviving spawner identity.");
            CombatUnit survivor = controlled.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => !unit.IsPyre && unit.Team == CombatTeam.Player);
            var extra = new CardUpgradeModifier("uncalibrated", "uncalibrated", new CardStatModifier(), [], false, false, false, 0, 0, []);
            CombatContext changed = controlled.Spawn.Train.Context!.WithCard(controlled.Spawn.Train.Context.FindCard(allyCardId)!.WithPermanentUpgrade(extra, false));
            RoomCombatState hitRoom = controlled.Spawn.Train.Rooms.Single(room => room.Units.Any(unit => unit.Id == action.TargetUnitId));
            Require(TrueFinalBossCombatModel.SupportsAlly(survivor, controlled.Spawn.Train.Context) &&
                !TrueFinalBossTerminalCombatModel.SupportsRoot(new RoomCombatState(hitRoom.RoomIndex, hitRoom.Deployment,
                    hitRoom.Units, hitRoom.ExternalInteractions, changed, hitRoom.Preview, hitRoom.ActiveCompanionBossIds), action.TargetUnitId,
                    plainSpellDamage: triggeredAlly),
                "Uncalibrated ally upgrades must retain the terminal guard.");
            if (triggeredAlly)
            {
                Require(!TrueFinalBossTerminalCombatModel.SupportsRoot(hitRoom, action.TargetUnitId) &&
                    TrueFinalBossTerminalCombatModel.SupportsRoot(hitRoom, action.TargetUnitId, plainSpellDamage: true) &&
                    Json(survivor) == Json(actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => !unit.IsPyre)),
                    "OnHit survivor must stay unchanged; only plain spell terminal sources are calibrated.");
                RoomCombatResult plain = RoomCombatModel.ApplyCardDamage(hitRoom, action.TargetUnitId, 1, action.CardInstanceId);
                RoomCombatResult anonymous = RoomCombatModel.ApplyCardDamage(hitRoom, action.TargetUnitId, 1);
                Require(plain.Supported && plain.Outcome == RoomOutcome.BattleWon && !anonymous.Supported,
                    "Plain captured spell source must allow OnHit survivors; anonymous damage must retain its guard.");
                if (target == "Dominion")
                {
                    EnchantmentWorld world = hitRoom.Context!.Enchantments!;
                    var previewWorld = new EnchantmentWorld(world.Rooms.Select(room => new RoomCombatState(room.RoomIndex,
                        room.Deployment, room.Units, room.ExternalInteractions, null, true, room.ActiveCompanionBossIds)).ToArray(),
                        world.Movement, world.EnemySlotsPerRoom, world.RetainedUnits, world.EnchanterIds,
                        world.AllowUpdates, world.Updating, true, world.TestRng, world.AutomaticLifecycle);
                    var links = hitRoom.Context.LifeLinks!;
                    CombatContext previewContext = hitRoom.Context.WithLifeLinks(new LifeLinkContext(links.Actors, true, 0))
                        .WithEnchantments(previewWorld);
                    var preview = new RoomCombatState(hitRoom.RoomIndex, hitRoom.Deployment, hitRoom.Units,
                        hitRoom.ExternalInteractions, previewContext, true, hitRoom.ActiveCompanionBossIds);
                    Require(TrueFinalBossCombatModel.Supports(preview) &&
                        !TrueFinalBossTerminalCombatModel.SupportsRoot(preview, action.TargetUnitId, plainSpellDamage: true),
                        "A valid linked preview with OnHit ally still requires separate terminal preview calibration.");
                }
            }
        }
        string parent = Json(prepared);
        string expected = BattleTurnChecks.Comparable(actual, true, true);
        string Replay()
        {
            BattleTurnState deployed = prepared;
            if (allied)
            {
                PlayCardAction summon = actions[0].GetProperty("Action").Deserialize<PlayCardAction>()!;
                Require(summon.CardInstanceId == addition.GetProperty("CardId").GetInt32() && summon.RoomIndex == 0 && summon.PlayerPosition == 0 &&
                    BattleTurnChecks.Comparable(prepared) == BattleTurnChecks.Comparable(actions[0].GetProperty("Before").Deserialize<BattleTurnState>()!),
                    "Allied paid chain must begin with the declared original ally summon.");
                BattleActionResult summoned = BattleActionModel.PlayCard(prepared, summon);
                Require(summoned.Supported && BattleTurnChecks.Comparable(summoned.State!) ==
                    BattleTurnChecks.Comparable(actions[0].GetProperty("Actual").Deserialize<BattleTurnState>()!) &&
                    BattleTurnChecks.Comparable(summoned.State!) == BattleTurnChecks.Comparable(deployment),
                    "Independent paid allied summon differs: " + summoned.Reason);
                deployed = summoned.State!;
            }
            BattleTurnResult born = BattleTurnModel.EndTurn(deployed);
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
        Require(Json(prepared) == parent, "Paid chain mutated its prepared root.");
        Require(new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }.All(key => root.GetProperty(key).GetInt32() == 0),
            "Native paid spell acceptance requires all four failure counters zero.");
        Console.WriteLine("TRUE-FINAL-BOSS-PAID-SPELL PASS target=" + target + "; survivingAlly=" + allied + "; triggeredAlly=" + triggeredAlly + "; original spell, one-energy payment, full terminal state/RNG and 16 independent immutable deployment-to-victory chains.");
    }
}
