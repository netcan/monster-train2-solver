using MonsterTrain2Poju.Fixtures;
using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class OriginalPrimordiumChecks
{
    private const string CardDataId = "2e1b29f2-fbe8-47b9-8ef8-1bac4c1f1f65";

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("ModifierScenario").GetString() == "original-primordium" &&
            root.GetProperty("OriginalPrimordiumPrepared").GetBoolean() &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean(),
            "Missing completed original Champion Primordium battle.");
        VerifyNativeAction(root);
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);

        RecordedBattleChecks.Native(root);
        TrainCombatChecks.Native(root);
        BattleTurnChecks.Native(root);
        Console.WriteLine("ORIGINAL-PRIMORDIUM PASS: played the unchanged Champion and replayed its native Eatmany deployment.");
    }

    internal static void ActionOnly(FixtureValue root)
    {
        Require(root.GetProperty("ModifierScenario").GetString() == "original-primordium" &&
            root.GetProperty("OriginalPrimordiumPrepared").GetBoolean(), "Missing original Primordium action capture.");
        VerifyNativeAction(root);
    }

    private static void VerifyNativeAction(FixtureValue root)
    {
        FixtureValue[] plays = root.GetProperty("Actions").EnumerateArray().Where(entry =>
        {
            PlayCardAction action = entry.GetProperty("Action").Deserialize<PlayCardAction>()!;
            BattleTurnState before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
            return before.Spawn.Train.Context!.FindCard(action.CardInstanceId)?.DataId == CardDataId;
        }).ToArray();
        if (plays.Length != 1)
            throw new InvalidOperationException("Expected exactly one original Champion Primordium play. " +
                DescribeMissingPlay(root));

        FixtureValue entry = plays[0];
        BattleTurnState before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState after = entry.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction action = entry.GetProperty("Action").Deserialize<PlayCardAction>()!;
        CombatContext beforeContext = before.Spawn.Train.Context!;
        CombatContext afterContext = after.Spawn.Train.Context!;
        CardInstanceState card = beforeContext.FindCard(action.CardInstanceId)!;
        CardPlayRule rule = before.PlayRules!.Cards.Single(candidate => candidate.DataId == CardDataId);
        CombatUnit template = rule.SpawnUnit!;
        CombatUnit born = after.Spawn.Train.Rooms.Single(room => room.RoomIndex == action.RoomIndex).Units
            .Single(unit => unit.SpawnerCardId == action.CardInstanceId);

        Require(card.MaskDescriptor!.SourceTraits is { Count: 0 } && rule.AssetKey == "ChampionPrimordium" &&
            rule.CardType == "Monster" && rule.Cost == 0 && rule.CostType == "Default" && rule.Effect == "SpawnMonster" &&
            rule.Destination == "Standby" && template.AssetKey == "MonsterChampionUmbraPrimordium" && template.BaseAttack == 3 &&
            template.MaxHealth == 3 && template.Size == 2 && template.Status("eatmany")?.Stacks == 3 && template.Triggers.Count == 0 &&
            template.Modifiers?.IsChampion == true && action.CardInstanceId == card.InstanceId && action.TargetUnitId == 0 &&
            action.PlayerPosition >= 0 && born.Team == CombatTeam.Player && born.AssetKey == template.AssetKey &&
            born.Size == template.Size && before.Energy == after.Energy &&
            afterContext.Statistics!.Value(card.InstanceId, "TimesPlayed", "ThisBattle") == 1,
            "Champion Primordium's unchanged identity, Champion spawn, Eatmany payload or zero-cost payment changed.");

        BattleActionResult replay = BattleActionModel.PlayCard(before, action);
        string? replayDifference = replay.Supported
            ? ModelJson.Difference(BattleTurnChecks.Comparable(replay.State!), BattleTurnChecks.Comparable(after)) : replay.Reason;
        string unitContext = replay.Supported && replayDifference?.Contains("LastFeederId") == true
            ? " expected=" + UnitAt(replay.State!, action.RoomIndex, 0) + " actual=" + UnitAt(after, action.RoomIndex, 0) : "";
        Require(replayDifference == null, "Original Champion Primordium does not replay to native state: " + replayDifference + unitContext);
        Require(BattleActionModel.EnumerateActions(before).SupportedCandidates.Any(candidate =>
            candidate.Action.CardInstanceId == action.CardInstanceId && candidate.Action.RoomIndex == action.RoomIndex &&
            candidate.Action.PlayerPosition == action.PlayerPosition),
            "Action search did not enumerate the native Champion Primordium deployment.");

        ModelOnly(root);
        Console.WriteLine("ORIGINAL-PRIMORDIUM ACTION PASS: native play action replayed exactly.");
    }

    internal static void ModelOnly(FixtureValue root)
    {
        var failures = new System.Collections.Generic.List<string>();
        foreach (FixtureValue entry in root.GetProperty("Actions").EnumerateArray())
        {
            PlayCardAction action = entry.GetProperty("Action").Deserialize<PlayCardAction>()!;
            BattleTurnState before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
            if (before.Spawn.Train.Context!.FindCard(action.CardInstanceId)?.DataId != CardDataId) continue;
            BattleActionResult played = BattleActionModel.PlayCard(before, action);
            if (!played.Supported) { failures.Add("native Primordium play rejected: " + played.Reason); continue; }
            RoomCombatState? room = played.State!.Spawn.Train.Rooms.SingleOrDefault(item => item.RoomIndex == action.RoomIndex);
            if (room != null && VerifyFeeding(room, action.CardInstanceId, failures)) return;
        }
        foreach (FixtureValue turn in root.GetProperty("Turns").EnumerateArray())
        {
            BattleTurnState before = turn.GetProperty("Before").Deserialize<BattleTurnState>()!;
            foreach (RoomCombatState room in before.Spawn.Train.Rooms)
                if (room.Units.Any(unit => unit.AssetKey == "MonsterChampionUmbraPrimordium" && unit.Team == CombatTeam.Player) &&
                    VerifyFeeding(room, room.Units.First(unit => unit.AssetKey == "MonsterChampionUmbraPrimordium" &&
                        unit.Team == CombatTeam.Player).SpawnerCardId, failures)) return;
        }
        throw new InvalidOperationException("Could not build a spawner-backed Primordium feeding check from the capture: " +
            (failures.Count == 0 ? "no Primordium play action or hand-state deployment was captured." : string.Join("; ", failures.Distinct())));
    }

    private static bool VerifyFeeding(RoomCombatState capturedRoom, int primordiumCardId, System.Collections.Generic.List<string> failures)
    {
        CombatUnit? primordium = capturedRoom.Units.SingleOrDefault(unit => unit.SpawnerCardId == primordiumCardId &&
            unit.AssetKey == "MonsterChampionUmbraPrimordium");
        CombatUnit? feeder = capturedRoom.Units.FirstOrDefault(unit => unit.Team == CombatTeam.Player && unit.Health > 0 &&
            unit.Status("untouchable") == null && !IsFood(unit));
        if (primordium == null || feeder == null || feeder.SpawnerCardId <= 0 ||
            capturedRoom.Context?.FindCard(feeder.SpawnerCardId) is not CardInstanceState feederCard)
        { failures.Add("room " + capturedRoom.RoomIndex + " lacked a captured Primordium or spawner-backed feeder"); return false; }

        int attack = primordium.Modifiers?.AttackDamage ?? primordium.BaseAttack;
        int health = primordium.MaxHealth;
        int oldAttack = feeder.Modifiers?.AttackDamage ?? feeder.BaseAttack;
        int oldAddedAttack = feeder.Modifiers?.AttackDamageAdded ?? 0;
        int oldMaxHealth = feeder.MaxHealth;
        int oldCopiedUpgradeCount = feederCard.Temporary.Upgrades.Count(upgrade => upgrade.DataId.Length == 0 &&
            upgrade.Stats.Damage == attack && upgrade.Stats.Health == health);
        var combatRoom = new RoomCombatState(capturedRoom.RoomIndex, deployment: false, capturedRoom.Units,
            capturedRoom.ExternalInteractions, capturedRoom.Context, capturedRoom.Preview, capturedRoom.ActiveCompanionBossIds);
        RoomCombatResult fed = RoomCombatModel.ApplyFeeding(combatRoom, CombatTeam.Player, new[] { primordium.Id });
        if (!fed.Supported) { failures.Add("Primordium feeding unsupported: " + fed.UnsupportedReason); return false; }
        CombatUnit changedFeeder = fed.State!.Units.Single(unit => unit.Id == feeder.Id);
        CardInstanceState changedCard = fed.State.Context!.FindCard(feeder.SpawnerCardId)!;
        int copiedUpgradeCount = changedCard.Temporary.Upgrades.Count(upgrade => upgrade.DataId.Length == 0 &&
            upgrade.Stats.Damage == attack && upgrade.Stats.Health == health);
        Require(changedFeeder.Modifiers?.AttackDamage == oldAttack + attack &&
            changedFeeder.Modifiers.AttackDamageAdded == oldAddedAttack + attack &&
            changedFeeder.MaxHealth == oldMaxHealth + health,
            "Primordium OnEaten did not copy its unbuffed attack and maximum health to the feeder.");
        Require(copiedUpgradeCount == oldCopiedUpgradeCount + 1,
            "Primordium did not append the anonymous stat upgrade to the feeder's spawner-card temporary modifiers.");
        Require(CardModifierModel.UnsupportedReason(changedCard) == null,
            "Primordium's dynamic spawner-card upgrade did not preserve card-mask upgrade metadata: " +
            CardModifierModel.UnsupportedReason(changedCard));
        Require(fed.Events.Any(item => item.Kind == "AddStatsFromSelf" && item.Actor == primordium.Id && item.Target == feeder.Id),
            "Primordium's native self-stat-copy trigger did not resolve against its last feeder.");
        Require(fed.State.Units.Single(unit => unit.Id == primordium.Id).LastFeederId == feeder.Id,
            "Primordium did not retain its native last-feeder relationship.");
        Console.WriteLine("ORIGINAL-PRIMORDIUM MODEL PASS: synthetic deployment and feeding copied " + attack + "/" + health +
            " stats to unit " + feeder.Id + " and its spawner card.");
        return true;
    }

    private static bool IsFood(CombatUnit unit) => unit.Status("inedible") == null && unit.Status("silenced") == null &&
        unit.Status("muted") == null && unit.Triggers.Any(trigger => trigger.Kind == "OnEaten");

    private static string UnitAt(BattleTurnState state, int roomIndex, int index)
    {
        CombatUnit unit = state.Spawn.Train.Rooms.Single(room => room.RoomIndex == roomIndex).Units[index];
        return unit.Id + "/" + unit.AssetKey + "/feeder=" + unit.LastFeederId;
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }

    private static string DescribeMissingPlay(FixtureValue root)
    {
        var observations = new System.Collections.Generic.List<string>();
        foreach (FixtureValue turn in root.GetProperty("Turns").EnumerateArray())
        {
            BattleTurnState state = turn.GetProperty("Before").Deserialize<BattleTurnState>()!;
            CombatContext? context = state.Spawn.Train.Context;
            if (context == null || !context.Cards.Hand.Any(card => card.DataId == CardDataId)) continue;
            CardToken card = context.Cards.Hand.First(item => item.DataId == CardDataId);
            CardInstanceState? instance = context.FindCard(card.InstanceId);
            CardPlayRule? rule = state.PlayRules?.Cards.SingleOrDefault(item => item.DataId == CardDataId);
            string ruleDetails = rule == null ? "" : " effects=" + string.Join(",", rule.Effects.Select(effect => effect.Type)) +
                " onCast=" + string.Join(",", rule.OnCastEffects.Select(effect => effect.Type)) +
                " spawnTriggers=" + string.Join(";", (rule.SpawnUnit?.Triggers ?? Array.Empty<CombatTrigger>())
                    .Select(trigger => trigger.Kind + ":" + string.Join(",", trigger.Effects.Select(effect => effect.Type)))) +
                " summon=" + (rule.Summon == null ? "none" : "upgradeTriggers=" +
                    string.Join(";", rule.Summon.Upgrade?.TriggerUpgrades?.Select(trigger => trigger.Kind + ":" +
                        string.Join(",", trigger.Effects.Select(effect => effect.Type))) ?? Array.Empty<string>())) +
                " cardUpgrades=" + string.Join(";", (instance?.Permanent.Upgrades ?? Array.Empty<CardUpgradeModifier>())
                    .Concat(instance?.Temporary.Upgrades ?? Array.Empty<CardUpgradeModifier>())
                    .Select(upgrade => upgrade.AssetKey + ":" + string.Join(",", upgrade.TriggerUpgrades?.Select(trigger =>
                        trigger.Kind + "=" + string.Join("/", trigger.Effects.Select(effect => effect.Type))) ?? Array.Empty<string>())));
            string[] triggerOwners = state.Spawn.Train.Rooms.SelectMany(triggerRoom => triggerRoom.Units.SelectMany(unit => unit.Triggers
                .Where(trigger => trigger.Effects.Any(effect => effect.Type == "CardEffectAddStatsFromSelf"))
                .Select(trigger => triggerRoom.RoomIndex + ":" + unit.Id + "/" + unit.AssetKey + "/" + trigger.Kind + "/" +
                    string.Join(",", trigger.Effects.Select(effect => effect.Type))))).ToArray();
            string roomValidation = string.Join(";", state.Spawn.Train.Rooms.Select(room => room.RoomIndex + ":" +
                (RoomCombatModel.Validate(room) ?? "valid")));
            var attempts = new System.Collections.Generic.List<string>();
            foreach (RoomPlayRule roomRule in state.PlayRules?.Rooms ?? Array.Empty<RoomPlayRule>())
            {
                RoomCombatState? room = state.Spawn.Train.Rooms.SingleOrDefault(item => item.RoomIndex == roomRule.RoomIndex);
                if (room == null || rule == null) continue;
                int players = room.Units.Count(unit => unit.Team == CombatTeam.Player);
                for (int position = 0; position <= players; position++)
                {
                    BattleActionResult attempt = BattleActionModel.PlayCard(state,
                        new PlayCardAction(card.InstanceId, roomRule.RoomIndex, position));
                    if (!attempt.Supported)
                        attempts.Add("room=" + roomRule.RoomIndex + " pos=" + position + " " +
                            attempt.Rejection + ":" + attempt.Reason + (triggerOwners.Length == 0 ? "" :
                                " owners=" + string.Join(";", triggerOwners)));
                }
            }
            observations.Add("turn=" + turn.GetProperty("Index").GetInt32() + " card=" + card.InstanceId +
                " rule=" + (rule == null ? "missing" : rule.Effect + "/" + rule.Cost + "/" + rule.Destination) +
                ruleDetails +
                " roomValidation=[" + roomValidation + "]" +
                (triggerOwners.Length == 0 ? "" : " triggerOwners=[" + string.Join(";", triggerOwners) + "]") +
                " attempts=[" + string.Join(" | ", attempts.Distinct()) + "]");
        }
        return "Captured hand-state deployment diagnostics: " +
            (observations.Count == 0 ? "the card was never present in an EndTurn hand snapshot." : string.Join("; ", observations));
    }
}
