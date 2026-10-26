using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class EchoWaxenSpikeChecks
{
    private const string EndReason = "Echo Waxen Spike retains its original paid cost and scales again during pre-combat.";
    internal static void Native(FixtureValue source, FixtureValue? endTurn = null, FixtureValue? replay = null)
    {
        string reason = source.GetProperty("Request").GetProperty("Reason").GetString()!;
        Require(reason.StartsWith("Echo replay needs native calibration: onCast=0, unplayed=0, interactions=, traits=CardTraitScalingUpgradeUnitAttack;CardTraitExhaustState, relics=", StringComparison.Ordinal),
            "Missing native Waxen Spike Echo boundary.");
        var rows = NativeBranchObservationChecks.Replay(source, reason);
        Require(rows.Length == 48, "Missing47 native inputs plus the remembered Waxen Spike.");
        var last = rows[^1];
        var before = last.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = last.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = last.GetProperty("Action").Deserialize<PlayCardAction>()!;
        var context = actual.Spawn.Train.Context!;
        var echo = context.RoomCapacities!.Single(room => room.RoomIndex == 2).Attachments!.Single(attachment => attachment.Echo != null).Echo!;
        var copy = context.FindCard(echo.CopyCardId)!;
        var raw = before.PlayRules!.Cards.Single(rule => rule.DataId == EchoWaxenSpikeModel.DataId);
        Require(before.Spawn.Turn == 9 && action.CardInstanceId == 19 && action.RoomIndex == 2 && action.TargetUnitId == 0 &&
            before.Energy == 5 && actual.Energy == 0 && echo is { OriginalCardId: 19, CopyCardId: 33, PaidCost: 5 } &&
            context.NextCardId == 34 && copy.LastPlayedCost == 5 && copy.PlayCount == 0 && copy.Temporary.Upgrades.Count == 2 &&
            EchoWaxenSpikeModel.Known(copy, echo.Effects!) && EchoWaxenSpikeModel.KnownRelics(context) &&
            JsonSerializer.Serialize(echo.Effects, ModelJson.Options) == JsonSerializer.Serialize(raw.Effects, ModelJson.Options),
            "Echo must retain raw unscaled effects, both Titan curses and the five paid ember.");
        string immutable = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            var result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Detached Waxen Spike Echo copy differs.");
        });
        Require(immutable == JsonSerializer.Serialize(before, ModelJson.Options), "Echo copy changed its parent.");
        foreach (string control in new[] { "Id", "Trait", "Scale", "Curse", "Offset" })
        {
            var changed = JsonSerializer.SerializeToNode(copy, ModelJson.Options)!;
            if (control == "Id") changed["DataId"] = "unknown-spike";
            if (control == "Trait") changed["MaskDescriptor"]!["BaseTraits"]![0]!["ParamInt"] = 5;
            if (control == "Scale") changed["UnitUpgradeScalingTraits"]![0]!["AmountPerStat"] = 8;
            if (control == "Curse") changed["Temporary"]!["Upgrades"]![0]!["Stats"]!["Damage"] = 1;
            if (control == "Offset") changed["Permanent"]!["Offsets"]!["Damage"] = 1;
            Require(!EchoWaxenSpikeModel.Known(changed.Deserialize<CardInstanceState>(ModelJson.Options)!, echo.Effects!),
                "Unknown Echo card payload was admitted: " + control);
        }
        foreach (string control in new[] { "RelicId", "RelicAsset", "Resource", "Callback" })
        {
            var changed = JsonSerializer.SerializeToNode(context, ModelJson.Options)!;
            if (control == "RelicId") changed["Relics"]![1]!["DataId"] = "unknown-ember-relic";
            if (control == "RelicAsset") changed["Relics"]![2]!["AssetKey"] = "OtherEmber";
            if (control == "Resource") changed["Relics"]![2]!["ResourceEffects"]![3]!["ParamInt2"] = 7;
            if (control == "Callback") changed["Relics"]![0]!["EffectTypes"]![0] = "RelicEffectAddTrigger";
            Require(!EchoWaxenSpikeModel.KnownRelics(changed.Deserialize<CombatContext>(ModelJson.Options)!),
                "Unknown Echo relic payload was admitted: " + control);
        }
        if (endTurn.HasValue)
        {
            var ends = NativeBranchObservationChecks.Replay(endTurn.Value, EndReason, boundaryOutcome: RoomOutcome.PlayerDefeated);
            Require(ends.Length == 49 && ends[^1].GetProperty("IsEndTurn").GetBoolean(), "Missing actual Echo pre-combat replay.");
            for (int i = 0; i < rows.Length; i++)
                Require(rows[i].GetProperty("Action").ContentEquals(ends[i].GetProperty("Action")) &&
                    rows[i].GetProperty("IsEndTurn").GetBoolean() == ends[i].GetProperty("IsEndTurn").GetBoolean() &&
                    NativeBranchObservationChecks.Key(rows[i].GetProperty("Actual").Deserialize<BattleTurnState>()!) ==
                        NativeBranchObservationChecks.Key(ends[i].GetProperty("Actual").Deserialize<BattleTurnState>()!),
                    "Native EndTurn extension changed the original input or successor: " + i);
            var final = ends[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            Require(final.Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 2).Attachments!
                .Single(attachment => attachment.Echo != null).Echo!.CopyCardId == 0,
                "Replayed Echo cache was not cleared.");
            string parent = JsonSerializer.Serialize(actual, ModelJson.Options);
            Parallel.For(0, 4, _ =>
            {
                var result = BattleTurnModel.EndTurn(actual);
                Require(result.Supported && result.Outcome == RoomOutcome.PlayerDefeated && NativeBranchObservationChecks.Key(result.State!, true) == NativeBranchObservationChecks.Key(final, true),
                    "Detached Waxen Spike Echo EndTurn differs.");
            });
            Require(parent == JsonSerializer.Serialize(actual, ModelJson.Options), "Echo EndTurn changed its parent.");
            if (replay.HasValue)
            {
                var fresh = NativeBranchObservationChecks.Replay(replay.Value, EndReason, boundaryOutcome: RoomOutcome.PlayerDefeated);
                Require(endTurn.Value.GetProperty("Request").ContentEquals(replay.Value.GetProperty("Request")) && fresh.Length == ends.Length,
                    "Independent Echo replay changed its request or input count.");
                for (int i = 0; i < ends.Length; i++)
                {
                    bool terminal = i == ends.Length - 1;
                    foreach (string field in new[] { "Action", "IsEndTurn", "Boundary" })
                        Require(ends[i].GetProperty(field).ContentEquals(fresh[i].GetProperty(field)), "Independent Echo input changed.");
                    foreach (string field in new[] { "Before", "Actual" })
                        Require(NativeBranchObservationChecks.Key(ends[i].GetProperty(field).Deserialize<BattleTurnState>()!, terminal && field == "Actual") ==
                            NativeBranchObservationChecks.Key(fresh[i].GetProperty(field).Deserialize<BattleTurnState>()!, terminal && field == "Actual"),
                            "Independent Echo full state/RNG changed at" + i);
                }
                var phase = replay.Value.GetProperty("RoomPreCombatRecords").EnumerateArray().Single(row =>
                    row.GetProperty("Turn").GetInt32() == 9 && row.GetProperty("RoomIndex").GetInt32() == 2);
                Require(phase.GetProperty("Completed").GetBoolean(), "Incomplete native Echo pre-combat callback.");
                var phaseBefore = phase.GetProperty("Before").Deserialize<TrainCombatState>()!;
                var phaseAfter = phase.GetProperty("After").Deserialize<TrainCombatState>()!;
                foreach (int id in new[] { 2, 3 })
                {
                    var old = phaseBefore.Rooms.Single(room => room.RoomIndex == 2).Units.Single(unit => unit.Id == id);
                    var next = phaseAfter.Rooms.Single(room => room.RoomIndex == 2).Units.Single(unit => unit.Id == id);
                    Require(next.Attack - old.Attack == 20 && next.Status("ephemeral")!.Stacks - old.Status("ephemeral")!.Stacks == 4 &&
                        next.Health == old.Health && next.Modifiers!.Upgrades.Count == old.Modifiers!.Upgrades.Count + 1 &&
                        next.Modifiers.Upgrades[^1].DataId == "31243b24-f327-42c8-84ad-b014e6fec675" && next.Modifiers.Upgrades[^1].Stats.Damage == 20,
                        "Native Echo must positively add20 attack/four burnout once to each friendly unit, using five paid ember.");
                }
                Require(phaseBefore.Rooms.Single(room => room.RoomIndex == 2).Units.Where(unit => unit.Team == CombatTeam.Enemy).All(old =>
                    JsonSerializer.Serialize(old, ModelJson.Options) == JsonSerializer.Serialize(phaseAfter.Rooms.Single(room => room.RoomIndex == 2).Units.Single(unit => unit.Id == old.Id), ModelJson.Options)),
                    "Friendly Waxen Echo changed an enemy during its pre-combat callback.");
            }
        }
        Console.WriteLine("ECHO-WAXEN-SPIKE PASS:48 native inputs/full state/RNG, raw effects, paid-cost copy, Titan curses, four immutable branches and nine payload guards; nativeEndTurn=" + endTurn.HasValue + "; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}
