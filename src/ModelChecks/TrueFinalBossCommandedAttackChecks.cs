using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TrueFinalBossCommandedAttackChecks
{
    private static void Require(bool value, string reason) => TrueFinalBossChecks.Require(value, reason);
    private static string Json(object value) => JsonSerializer.Serialize(value, ModelJson.Options);
    internal static void Native(FixtureValue root)
    {
        FixtureValue route = root.GetProperty("OriginalTrueFinalBossRoute");
        Require(route.GetProperty("Scenario").GetString() == "Level9TrueFinalBoss_V2" &&
            route.GetProperty("ScenarioAssetsUnchanged").GetBoolean() && route.GetProperty("CharacterAssetsUnchanged").GetBoolean() &&
            !route.GetProperty("StartingDeckUnchanged").GetBoolean() && route.GetProperty("PyreHealth").GetInt32() == 80 &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean() && root.GetProperty("NativeWon").GetBoolean(),
            "Missing original encounter and explicit deck/HP controls for the settled commanded victory.");
        FixtureValue addition = root.GetProperty("TrueFinalBossOrdinaryAllyControl");
        int allyCardId = addition.GetProperty("CardId").GetInt32(), commandId = addition.GetProperty("CommandCardId").GetInt32();
        CardPlayRule command = addition.GetProperty("CommandDefinition").Deserialize<CardPlayRule>()!;
        Require(addition.GetProperty("Kind").GetString() == "Steward" && addition.GetProperty("UpgradeKind").GetString() == "" &&
            addition.GetProperty("CardDataId").GetString() == "38e70e6a-20f3-4e36-8d31-c5110be07f87" &&
            command.DataId == "1737fa0b-7f37-41e8-9183-ed9e6a391556" && command.AssetKey == "Eagleheart" && command.Cost == 2 &&
            command.Effects.Select(effect => effect.Type).SequenceEqual(new[] { "FloorRearrange", "AddStatus", "AttackWithUnit" }),
            "Missing original small Steward and complete Eagleheart command definition.");
        BattleTurnState prepared = addition.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        BattleTurnState beforeAddition = addition.GetProperty("Before").Deserialize<BattleTurnState>()!;
        Require(prepared.Spawn.Train.Context!.Cards.Draw[addition.GetProperty("CommandDrawIndex").GetInt32()].InstanceId == commandId &&
            prepared.Spawn.Train.Context.Cards.Rng.Equals(beforeAddition.Spawn.Train.Context!.Cards.Rng) && prepared.Energy == 4 &&
            prepared.Spawn.Train.Rooms.SelectMany(room => room.Units).Single().IsPyre,
            "Command must be explicitly prepared in the draw pile before paid deployment.");
        FixtureValue control = root.GetProperty("TrueFinalBossLethalControl");
        Require(control.GetProperty("PaidSpellTarget").GetString() == "Savagery" && !control.GetProperty("TargetOwnerOnly").GetBoolean() &&
            !control.GetProperty("RefreshPreview").GetBoolean() && control.GetProperty("RemovedSpellShield").GetInt32() == 0 &&
            control.GetProperty("Debuffs").Deserialize<int[]>()!.SequenceEqual(new[] { 1554, 2749, 3499 }),
            "Commanded attack requires only the declared three-owner HP control.");
        BattleTurnState bornBeforeControl = control.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState controlled = control.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(Json(TrueFinalBossPaidSpellChecks.Prepare(bornBeforeControl, "Savagery")) == Json(controlled),
            "Commanded attack preparation changed more than the declared HP controls.");
        FixtureValue actions = root.GetProperty("Actions"), turns = root.GetProperty("Turns");
        Require(actions.GetArrayLength() == 2 && turns.GetArrayLength() == 1, "Expected one paid summon, one EndTurn and one paid command.");
        PlayCardAction summon = actions[0].GetProperty("Action").Deserialize<PlayCardAction>()!;
        PlayCardAction cast = actions[1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        CombatUnit actor = controlled.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == allyCardId);
        CombatUnit victim = controlled.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.AssetKey == "Boss_Level9_Entropy_Bot_V2");
        BattleTurnState actual = actions[1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(summon.CardInstanceId == allyCardId && cast.CardInstanceId == commandId && cast.TargetUnitId == actor.Id && cast.RoomIndex == 0 &&
            controlled.Energy == 3 && actual.Energy == 1 && actual.Spawn.OuterBoss == null && actual.Spawn.Train.Context!.LifeLinks == null &&
            actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Count() == 2 &&
            actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre).Health == 80 &&
            actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => !unit.IsPyre) is { Health: 5, MaxHealth: 5, BaseAttack: 14 } survivor &&
            survivor.Status("valor") is { Stacks: 5, ParamInt: 1 },
            "Paid command must preserve its original actor with five Valor stacks and win through the linked group.");
        FixtureValue turn = root.GetProperty("UnitTurns").EnumerateArray().Single(row => row.GetProperty("ActorId").GetInt32() == actor.Id &&
            !row.GetProperty("Before").GetProperty("Deployment").GetBoolean());
        RoomCombatState attackBefore = turn.GetProperty("Before").Deserialize<RoomCombatState>()!;
        RoomCombatState attackActual = turn.GetProperty("Actual").Deserialize<RoomCombatState>()!;
        RoomCombatResult attack = RoomCombatModel.ApplyUnitTurn(attackBefore, actor.Id);
        Require(turn.GetProperty("AttackedTargetIds").Deserialize<int[]>()!.SequenceEqual(new[] { victim.Id }) &&
            attack.Supported && attack.Outcome == RoomOutcome.BattleWon && Json(attack.State!) == Json(attackActual),
            "Native commanded ordinary unit turn must identify its actor/victim and match full state/RNG: " + attack.UnsupportedReason);
        JsonNode uncalibrated = JsonNode.Parse(Json(attackBefore.Units.Single(unit => unit.Id == actor.Id)))!;
        uncalibrated["Statuses"]![0]!["Stacks"] = 6;
        Require(!TrueFinalBossCombatModel.SupportsAlly(uncalibrated.Deserialize<CombatUnit>(ModelJson.Options)!, attackBefore.Context),
            "Other Valor magnitudes require native calibration.");
        Require(BattleActionModel.EnumerateActions(controlled, commandId, includeAbilities: false).SupportedCandidates.Any(candidate =>
            candidate.Action.TargetUnitId == actor.Id && candidate.Result.Outcome == RoomOutcome.BattleWon),
            "The paid command victory must be available to the search frontier.");
        string parent = Json(prepared), expected = BattleTurnChecks.Comparable(actual, true, true);
        void Replay()
        {
            Require(BattleTurnChecks.Comparable(prepared) == BattleTurnChecks.Comparable(actions[0].GetProperty("Before").Deserialize<BattleTurnState>()!),
                "Independent command chain started at the wrong prepared root.");
            BattleActionResult summoned = BattleActionModel.PlayCard(prepared, summon);
            Require(summoned.Supported && BattleTurnChecks.Comparable(summoned.State!) ==
                BattleTurnChecks.Comparable(turns[0].GetProperty("Before").Deserialize<BattleTurnState>()!), "Independent summon differs.");
            BattleTurnResult born = BattleTurnModel.EndTurn(summoned.State!);
            Require(born.Supported && BattleTurnChecks.Comparable(born.State!) == BattleTurnChecks.Comparable(bornBeforeControl),
                "Independent birth/draw must deliver the prepared original command card.");
            BattleActionResult played = BattleActionModel.PlayCard(TrueFinalBossPaidSpellChecks.Prepare(born.State!, "Savagery"), cast);
            Require(played.Supported && played.Outcome == RoomOutcome.BattleWon && BattleTurnChecks.Comparable(played.State!, true, true) == expected,
                "Complete independent Eagleheart victory differs: " + played.Reason);
        }
        Replay(); Parallel.For(0, 16, _ => Replay());
        Require(Json(prepared) == parent, "Commanded attack chains mutated the prepared deck root.");
        Require(new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }.All(key => root.GetProperty(key).GetInt32() == 0),
            "Commanded attack acceptance requires all four native failure counters zero.");
        Console.WriteLine("TRUE-FINAL-BOSS-COMMANDED-ATTACK PASS: original paid Eagleheart, front/Valor/attack chain, native ordinary actor/victim, complete unit turn and 16 immutable summon-to-victory chains.");
    }
}
