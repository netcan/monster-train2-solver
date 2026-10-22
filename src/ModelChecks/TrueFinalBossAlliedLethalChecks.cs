using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TrueFinalBossAlliedLethalChecks
{
    private static void Require(bool value, string reason) => TrueFinalBossChecks.Require(value, reason);
    private static string Json(object value) => JsonSerializer.Serialize(value, ModelJson.Options);

    internal static void Native(FixtureValue root)
    {
        FixtureValue route = root.GetProperty("OriginalTrueFinalBossRoute");
        Require(route.GetProperty("Scenario").GetString() == "Level9TrueFinalBoss_V2" &&
            route.GetProperty("ScenarioAssetsUnchanged").GetBoolean() && route.GetProperty("CharacterAssetsUnchanged").GetBoolean() &&
            !route.GetProperty("StartingDeckUnchanged").GetBoolean() && route.GetProperty("PyreHealth").GetInt32() == 80 &&
            root.GetProperty("BattleDistance").GetInt32() == 8 && root.GetProperty("Policy").GetString() == "units-and-junk" &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalCaptureBoundary").GetString() == "AfterStopCombatLoop",
            "Missing original isolated encounter and settled ordinary-combat victory.");
        FixtureValue addition = root.GetProperty("TrueFinalBossOrdinaryAllyControl");
        int cardId = addition.GetProperty("CardId").GetInt32();
        CardPlayRule definition = addition.GetProperty("Definition").Deserialize<CardPlayRule>()!;
        BattleTurnState prepared = addition.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(addition.GetProperty("Kind").GetString() == "Avenger" && addition.GetProperty("UpgradeKind").GetString() == "" &&
            addition.GetProperty("CommandCardId").GetInt32() == 0 &&
            definition.DataId == "69fcbe85-a5b7-4a83-bf6d-21888604d99e" && definition.Effect == "SpawnMonster" &&
            prepared.Spawn.Turn == 0 && prepared.Energy == 4 &&
            prepared.Spawn.Train.Context!.FindCard(cardId)!.DataId == definition.DataId &&
            prepared.Spawn.Train.Rooms.SelectMany(room => room.Units).Single() is { IsPyre: true, Health: 80 },
            "Ordinary terminal combat requires the declared unmodified original Avenger, without a command.");
        FixtureValue control = root.GetProperty("TrueFinalBossLethalControl");
        Require(control.GetProperty("OrdinaryCombat").GetBoolean() && control.GetProperty("PaidSpellTarget").GetString() == "" &&
            !control.GetProperty("TargetOwnerOnly").GetBoolean() && !control.GetProperty("RefreshPreview").GetBoolean() &&
            control.GetProperty("RemovedSpellShield").GetInt32() == 0 &&
            control.GetProperty("Debuffs").Deserialize<int[]>()!.SequenceEqual(new[] { 1554, 2749, 3499 }),
            "Ordinary combat must declare only its three-owner HP controls.");
        BattleTurnState beforeControl = control.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState controlled = control.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(Json(TrueFinalBossLethalChecks.Prepare(beforeControl)) == Json(controlled),
            "Independent owner HP control changed more than the three original owners.");
        FixtureValue actions = root.GetProperty("Actions"), turns = root.GetProperty("Turns");
        Require(actions.GetArrayLength() == 1 && turns.GetArrayLength() == 2, "Expected one paid summon and two normal EndTurns.");
        PlayCardAction summon = actions[0].GetProperty("Action").Deserialize<PlayCardAction>()!;
        BattleTurnState final = turns[1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatUnit ally = controlled.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == cardId);
        CombatUnit[] survivors = final.Spawn.Train.Rooms.SelectMany(room => room.Units).ToArray();
        Require(summon.CardInstanceId == cardId && summon.RoomIndex == 0 && summon.PlayerPosition == 0 &&
            ally is { BaseAttack: 20, Health: 40, MaxHealth: 40, Size: 2 } && ally.Triggers.Count == 1 && ally.Statuses.Count == 0 &&
            TrueFinalBossCombatModel.SupportsAlly(ally, controlled.Spawn.Train.Context) &&
            survivors.Length == 2 && survivors.Single(unit => unit.IsPyre).Health == 77 &&
            Json(survivors.Single(unit => !unit.IsPyre)) == Json(ally) &&
            final.Spawn.OuterBoss == null && final.Spawn.Train.Context!.LifeLinks == null && final.Spawn.Train.Context.KillCamActivated == true,
            "Ordinary combat must preserve Avenger20/40 and remove all five bosses after the ordinary Pyre counterattack.");
        FixtureValue[] attacks = root.GetProperty("UnitTurns").EnumerateArray().ToArray();
        int pyreId = controlled.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre).Id;
        Require(attacks.Length == 3 && attacks[1].GetProperty("AttackedTargetIds").Deserialize<int[]>()!.SequenceEqual(new[] { pyreId }) &&
            attacks[2].GetProperty("AttackedTargetIds").Deserialize<int[]>()!.SequenceEqual(new[] { control.GetProperty("OwnerIds")[0].GetInt32() }),
            "Missing the native Dominion-to-Pyre hit and the physical Pyre counterattack.");
        foreach (FixtureValue row in attacks)
        {
            RoomCombatState before = row.GetProperty("Before").Deserialize<RoomCombatState>()!;
            RoomCombatState actual = row.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            RoomCombatResult modeled = RoomCombatModel.ApplyUnitTurn(before, row.GetProperty("ActorId").GetInt32());
            Require(modeled.Supported && Json(modeled.State!) == Json(actual),
                "Ordinary native unit turn differs: " + (modeled.UnsupportedReason ?? ModelJson.Difference(Json(modeled.State!), Json(actual))));
        }
        RoomCombatState lethalRoom = attacks[2].GetProperty("Before").Deserialize<RoomCombatState>()!;
        int target = control.GetProperty("OwnerIds")[0].GetInt32();
        Require(!TrueFinalBossTerminalCombatModel.SupportsRoot(lethalRoom, target) &&
            TrueFinalBossTerminalCombatModel.SupportsRoot(lethalRoom, target, ordinaryPyreAttack: true) &&
            !RoomCombatModel.ApplyCardDamage(lethalRoom, target, 1).Supported,
            "Physical Pyre calibration must retain default and anonymous-damage terminal guards.");
        JsonNode changed = JsonNode.Parse(Json(lethalRoom))!;
        changed["RoomIndex"] = 2;
        Require(!TrueFinalBossTerminalCombatModel.SupportsRoot(changed.Deserialize<RoomCombatState>(ModelJson.Options)!, target, ordinaryPyreAttack: true),
            "Physical Pyre calibration cannot be reused on another floor.");
        JsonNode altered = JsonNode.Parse(Json(lethalRoom))!;
        JsonNode avenger = altered["Context"]!["Enchantments"]!["Rooms"]!.AsArray().SelectMany(room => room!["Units"]!.AsArray())
            .Single(unit => unit!["AssetKey"]!.GetValue<string>() == "MonsterAvenger")!;
        avenger["Triggers"]![0]!["FireCount"] = 2;
        Require(!TrueFinalBossTerminalCombatModel.SupportsRoot(altered.Deserialize<RoomCombatState>(ModelJson.Options)!, target, ordinaryPyreAttack: true),
            "Altered ally callbacks must not bypass physical Pyre calibration.");
        string parent = Json(prepared), expected = BattleTurnChecks.Comparable(final, true, true);
        void Replay()
        {
            BattleActionResult deployed = BattleActionModel.PlayCard(prepared, summon);
            Require(deployed.Supported && BattleTurnChecks.Comparable(deployed.State!) ==
                BattleTurnChecks.Comparable(turns[0].GetProperty("Before").Deserialize<BattleTurnState>()!), "Paid ordinary deployment differs.");
            BattleTurnResult born = BattleTurnModel.EndTurn(deployed.State!);
            Require(born.Supported && BattleTurnChecks.Comparable(born.State!) == BattleTurnChecks.Comparable(beforeControl),
                "Original TFB birth and draw differ: " + born.UnsupportedReason);
            BattleTurnResult won = BattleTurnModel.EndTurn(TrueFinalBossLethalChecks.Prepare(born.State!));
            Require(won.Supported && won.Outcome == RoomOutcome.BattleWon && BattleTurnChecks.Comparable(won.State!, true, true) == expected,
                "Ordinary combat victory state/RNG differs: " + (won.UnsupportedReason ?? (won.Supported ?
                    ModelJson.Difference(BattleTurnChecks.Comparable(won.State!, true, true), expected) : "unsupported")));
        }
        Replay(); Parallel.For(0, 16, _ => Replay());
        Require(Json(prepared) == parent, "Independent combat chains mutated the prepared root.");
        Require(new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }.All(key => root.GetProperty(key).GetInt32() == 0),
            "Ordinary combat acceptance requires all four native failure counters zero.");
        Console.WriteLine("TRUE-FINAL-BOSS-ALLIED-LETHAL PASS: original OnHit survivor, physical Pyre counterattack/kill preview, five-body cleanup, 16 immutable paid-deployment-to-victory chains and source/floor/callback guards.");
    }
}
