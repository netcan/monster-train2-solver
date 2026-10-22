using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TrueFinalBossCommandedAttackChecks
{
    private static void Require(bool value, string reason) => TrueFinalBossChecks.Require(value, reason);
    private static string Json(object value) => JsonSerializer.Serialize(value, ModelJson.Options);
    internal static void Native(FixtureValue root, bool pyreCommand = false, bool triggeredAlly = false)
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
        Require(addition.GetProperty("Kind").GetString() == (triggeredAlly ? "Avenger" : "Steward") && addition.GetProperty("UpgradeKind").GetString() == "" &&
            addition.GetProperty("CardDataId").GetString() == (triggeredAlly ? "69fcbe85-a5b7-4a83-bf6d-21888604d99e" : "38e70e6a-20f3-4e36-8d31-c5110be07f87") &&
            command.DataId == (pyreCommand ? "c30c479c-e7aa-4628-9091-dd5d2fb8e5a6" : "1737fa0b-7f37-41e8-9183-ed9e6a391556") &&
            command.AssetKey == (pyreCommand ? "Pyreshot" : "Eagleheart") && command.Cost == (pyreCommand ? 3 : 2) &&
            command.Effects.Select(effect => effect.Type).SequenceEqual(pyreCommand ? new[] { "AttackWithUnit" } : new[] { "FloorRearrange", "AddStatus", "AttackWithUnit" }),
            "Missing the declared original ally and complete original command definition.");
        if (pyreCommand)
            Require(command.Effects.Single() is { Target: "Pyre", UseSelectedRoom: true, SuppressPyreRoomFocus: true, RequireEquipment: false },
                "Pyreshot must retain its original cross-floor Pyre attack flags.");
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
        CombatUnit ally = controlled.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == allyCardId);
        CombatUnit actor = pyreCommand ? controlled.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre) : ally;
        CombatUnit victim = controlled.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.AssetKey == "Boss_Level9_Entropy_Bot_V2");
        BattleTurnState actual = actions[1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(summon.CardInstanceId == allyCardId && cast.CardInstanceId == commandId && cast.TargetUnitId == (pyreCommand ? 0 : actor.Id) && cast.RoomIndex == 0 &&
            controlled.Energy == 3 && actual.Energy == (pyreCommand ? 0 : 1) && actual.Spawn.OuterBoss == null && actual.Spawn.Train.Context!.LifeLinks == null &&
            actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Count() == 2 &&
            actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre).Health == 80 &&
            actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => !unit.IsPyre) is CombatUnit survivor &&
            survivor.Health == (triggeredAlly ? 40 : 5) && survivor.MaxHealth == survivor.Health &&
            survivor.BaseAttack == (triggeredAlly ? 20 : 14) && survivor.Triggers.Count == (triggeredAlly ? 1 : 0) &&
            (pyreCommand ? survivor.Statuses.Count == 0 : survivor.Status("valor") is { Stacks: 5, ParamInt: 1 }),
            "Paid command must preserve its original surviving ally and win through the linked group.");
        FixtureValue turn = root.GetProperty("UnitTurns").EnumerateArray().Single(row => row.GetProperty("ActorId").GetInt32() == actor.Id &&
            !row.GetProperty("Before").GetProperty("Deployment").GetBoolean());
        RoomCombatState attackBefore = turn.GetProperty("Before").Deserialize<RoomCombatState>()!;
        RoomCombatState attackActual = turn.GetProperty("Actual").Deserialize<RoomCombatState>()!;
        RoomCombatResult attack = RoomCombatModel.ApplyUnitTurn(attackBefore, actor.Id, externalActor: pyreCommand ? actor : null);
        Require(turn.GetProperty("AttackedTargetIds").Deserialize<int[]>()!.SequenceEqual(new[] { victim.Id }) &&
            attack.Supported && attack.Outcome == RoomOutcome.BattleWon && Json(attack.State!) == Json(attackActual),
            "Native commanded unit turn must identify its actor/victim and match full state/RNG: " +
                (attack.UnsupportedReason ?? ModelJson.Difference(Json(attack.State!), Json(attackActual))));
        if (!pyreCommand)
        {
            JsonNode uncalibrated = JsonNode.Parse(Json(attackBefore.Units.Single(unit => unit.Id == actor.Id)))!;
            uncalibrated["Statuses"]![0]!["Stacks"] = 6;
            Require(!TrueFinalBossCombatModel.SupportsAlly(uncalibrated.Deserialize<CombatUnit>(ModelJson.Options)!, attackBefore.Context),
                "Other Valor magnitudes require native calibration.");
            if (triggeredAlly)
            {
                LifeLinkContext links = attackBefore.Context!.LifeLinks!;
                CombatContext healthy = attackBefore.Context.WithLifeLinks(new LifeLinkContext(links.Actors.Select(link =>
                    new LifeLinkActor(link.Id, link.LinkedId, 2, 2, link.Initialized, link.HasLifeLink, link.TrueFinalBoss, link.OuterBoss)).ToArray(), false, 0));
                CombatUnit commanded = attackBefore.Units.Single(unit => unit.Id == actor.Id);
                Require(TrueFinalBossCombatModel.SupportsAlly(commanded, attackBefore.Context) &&
                    !TrueFinalBossCombatModel.SupportsAlly(commanded, healthy),
                    "Triggered Valor signature must remain restricted to the calibrated one-HP terminal state.");
            }
        }
        else
            Require(Json(turn.GetProperty("ExternalActor").Deserialize<CombatUnit>()!) == Json(actor) && attackBefore.RoomIndex == 0 &&
                attackBefore.Units.All(unit => !unit.IsPyre) && actual.Spawn.Train.Rooms.Single(room => room.Units.Any(unit => unit.IsPyre)).RoomIndex == 3,
                "Cross-floor command must retain the original external Pyre on its physical floor.");
        if (triggeredAlly)
        {
            RoomCombatResult ordinary = RoomCombatModel.Resolve(attackBefore);
            Require(!ordinary.Supported && ordinary.UnsupportedReason?.Contains("terminal updates remain guarded", StringComparison.Ordinal) == true,
                "Ordinary combat must not inherit the calibrated commanded terminal attack: " + ordinary.UnsupportedReason);
        }
        BattleActionResult paid = BattleActionModel.PlayCard(controlled, cast);
        Require(paid.Supported && paid.Outcome == RoomOutcome.BattleWon &&
            BattleTurnChecks.Comparable(paid.State!, true, true) == BattleTurnChecks.Comparable(actual, true, true),
            "Paid command result differs: " + (paid.Reason ?? (paid.Supported ? ModelJson.Difference(
                BattleTurnChecks.Comparable(paid.State!, true, true), BattleTurnChecks.Comparable(actual, true, true)) : "unsupported")));
        Require(BattleActionModel.EnumerateActions(controlled, commandId, includeAbilities: false).SupportedCandidates.Any(candidate =>
            (pyreCommand || candidate.Action.TargetUnitId == cast.TargetUnitId) && candidate.Action.RoomIndex == 0 && candidate.Result.Outcome == RoomOutcome.BattleWon),
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
                "Complete independent command victory differs: " + played.Reason);
        }
        Replay(); Parallel.For(0, 16, _ => Replay());
        Require(Json(prepared) == parent, "Commanded attack chains mutated the prepared deck root.");
        Require(new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }.All(key => root.GetProperty(key).GetInt32() == 0),
            "Commanded attack acceptance requires all four native failure counters zero.");
        Console.WriteLine("TRUE-FINAL-BOSS-COMMANDED-ATTACK PASS card=" + command.AssetKey + "; triggeredAlly=" + triggeredAlly + "; original paid command, native actor/victim, complete unit turn and 16 immutable summon-to-victory chains.");
    }
}
