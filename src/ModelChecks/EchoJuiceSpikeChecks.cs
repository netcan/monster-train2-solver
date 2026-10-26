using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class EchoJuiceSpikeChecks
{
    internal const string EndReason = "Triple Juice Echo repeats paid-cost scaling and its Pyre-heal OnCast upgrade.";
    internal static void Native(FixtureValue source, FixtureValue? endTurn = null, FixtureValue? replay = null)
    {
        string reason = source.GetProperty("Request").GetProperty("Reason").GetString()!;
        Require(reason.StartsWith("Echo replay needs native calibration: onCast=1, unplayed=0, interactions=, traits=CardTraitScalingAddStatusEffect;CardTraitExhaustState;CardTraitJuice3, relics=", StringComparison.Ordinal),
            "Missing original triple-Juice Echo guard.");
        var rows = NativeBranchObservationChecks.Replay(source, reason);
        Require(rows.Length == 46, "Expected45 original inputs and the triple-Juice Echo copy.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        var context = actual.Spawn.Train.Context!;
        var echo = context.RoomCapacities!.Single(room => room.RoomIndex == 2).Attachments!.Single(attachment => attachment.Echo != null).Echo!;
        var copy = context.FindCard(echo.CopyCardId)!;
        Require(before.Spawn.Turn == 9 && before.Energy == 5 && actual.Energy == 0 && action.CardInstanceId == 22 &&
            action.RoomIndex == 2 && action.TargetUnitId == 2 && echo is { OriginalCardId: 22, CopyCardId: 45, PaidCost: 5 } &&
            context.NextCardId == 46 && copy.LastPlayedCost == 5 && copy.PlayCount == 0 &&
            EchoJuiceSpikeModel.Known(copy, echo.Effects!) && EchoJuiceSpikeModel.KnownRelics(context) &&
            echo.Effects![0].Statuses[0].Stacks == 0 && copy.Permanent.Upgrades.Count == 2 && copy.Temporary.Upgrades.Count == 1,
            "Native Echo must retain raw valor, paid5, original triple-Juice/Pyre-heal upgrades and the Titan curse.");
        string immutable = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            var played = BattleActionModel.PlayCard(before, action);
            Require(played.Supported && NativeBranchObservationChecks.Key(played.State!) == NativeBranchObservationChecks.Key(actual),
                "Detached triple-Juice Echo copy differs.");
        });
        Require(immutable == JsonSerializer.Serialize(before, ModelJson.Options), "Triple-Juice Echo mutated its parent.");
        foreach (string control in new[] { "Id", "Multiplier", "Heal", "HealId", "Curse", "Scale" })
        {
            var changed = JsonSerializer.SerializeToNode(copy, ModelJson.Options)!;
            if (control == "Id") changed["DataId"] = "unknown-juice";
            if (control == "Multiplier") changed["Permanent"]!["Upgrades"]![1]!["Lifecycle"]!["AddedTraits"]![0]!["ParamInt"] = 4;
            if (control == "Heal") changed["Permanent"]!["Upgrades"]![0]!["Lifecycle"]!["Triggers"]![0]!["Actions"]![0]!["Value"] = 2;
            if (control == "HealId") changed["Permanent"]!["Upgrades"]![0]!["DataId"] = "unknown-pyre-heal";
            if (control == "Curse") changed["Temporary"]!["Upgrades"]![0]!["Stats"]!["Cost"] = 2;
            if (control == "Scale") changed["StatusScalingTraits"]![0]!["StacksPerStat"] = 8;
            Require(!EchoJuiceSpikeModel.Known(changed.Deserialize<CardInstanceState>(ModelJson.Options)!, echo.Effects!),
                "Unknown triple-Juice Echo card admitted: " + control);
        }
        foreach (string control in new[] { "Id", "Callback", "DeploymentEnergy", "EquipmentTrigger" })
        {
            var changed = JsonSerializer.SerializeToNode(context, ModelJson.Options)!;
            var relics = changed["Relics"]!.AsArray();
            if (control == "Id") relics[0]!["DataId"] = "unknown-relic";
            if (control == "Callback") relics.Single(value => value!["AssetKey"]!.GetValue<string>() == "ExtraUnitUpgrades")!["EffectTypes"]![0] = "RelicEffectAddTrigger";
            if (control == "DeploymentEnergy") relics.Single(value => value!["AssetKey"]!.GetValue<string>() == "MoreDeploymentPhaseEmber")!["ResourceEffects"]![0]!["ParamInt"] = 4;
            if (control == "EquipmentTrigger") relics.Single(value => value!["AssetKey"]!.GetValue<string>() == "ManyBuffonAngelEquip")!["TriggerAdditions"]![0]!["Triggers"]![0]!["Kind"] = "OnCardPlayed";
            Require(!EchoJuiceSpikeModel.KnownRelics(changed.Deserialize<CombatContext>(ModelJson.Options)!),
                "Unknown triple-Juice Echo relic admitted: " + control);
        }
        if (endTurn.HasValue)
        {
            var ends = NativeBranchObservationChecks.Replay(endTurn.Value, EndReason,
                historicalUnsupportedReason: "Train combat floor 2: " + EndReason);
            Require(ends.Length == 47 && ends[^1].GetProperty("IsEndTurn").GetBoolean(), "Missing actual Echo pre-combat recast.");
            for (int i = 0; i < rows.Length; i++)
                Require(rows[i].GetProperty("Action").ContentEquals(ends[i].GetProperty("Action")) &&
                    rows[i].GetProperty("IsEndTurn").GetBoolean() == ends[i].GetProperty("IsEndTurn").GetBoolean() &&
                    NativeBranchObservationChecks.Key(rows[i].GetProperty("Actual").Deserialize<BattleTurnState>()!) ==
                    NativeBranchObservationChecks.Key(ends[i].GetProperty("Actual").Deserialize<BattleTurnState>()!),
                    "Native EndTurn changed the original input/successor: " + i);
            var final = ends[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            Require(final.Spawn.Turn == 10 && final.Spawn.Train.Rooms.Single(room => room.RoomIndex == 3).Units.Single(unit => unit.IsPyre).Health == 4 &&
                final.Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 2).Attachments!
                .Single(attachment => attachment.Echo != null).Echo!.CopyCardId == 0, "Echo cache was not cleared.");
            CheckPreCombat(endTurn.Value);
            string parent = JsonSerializer.Serialize(actual, ModelJson.Options);
            Parallel.For(0, 4, _ =>
            {
                var result = BattleTurnModel.EndTurn(actual);
                Require(result.Supported && result.Outcome == RoomOutcome.Exchanged &&
                    NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(final),
                    "Detached Juice Echo EndTurn differs.");
            });
            Require(parent == JsonSerializer.Serialize(actual, ModelJson.Options), "Echo EndTurn mutated its parent.");
            if (replay.HasValue)
            {
                NativeBranchObservationChecks.Replay(replay.Value, EndReason);
                NativeBranchObservationChecks.Pair(endTurn.Value, replay.Value);
                CheckPreCombat(replay.Value);
            }
        }
        Console.WriteLine("ECHO-JUICE-SPIKE PASS:46 native inputs/full state/RNG, raw valor, paid5 and original upgrades, four immutable branches/ten guards; nativeEndTurn=" + endTurn.HasValue + "; independentPair=" + replay.HasValue);
    }
    private static void CheckPreCombat(FixtureValue fixture)
    {
        var phase = fixture.GetProperty("RoomPreCombatRecords").EnumerateArray().Single(row =>
            row.GetProperty("Turn").GetInt32() == 9 && row.GetProperty("RoomIndex").GetInt32() == 2);
        Require(phase.GetProperty("Completed").GetBoolean(), "Incomplete Echo pre-combat record.");
        var before = phase.GetProperty("Before").Deserialize<TrainCombatState>()!;
        var after = phase.GetProperty("After").Deserialize<TrainCombatState>()!;
        var old = before.Rooms.Single(room => room.RoomIndex == 2).Units.Single(unit => unit.Id == 2);
        var next = after.Rooms.Single(room => room.RoomIndex == 2).Units.Single(unit => unit.Id == 2);
        Require(old.Status("valor")!.Stacks == 152 && next.Status("valor")!.Stacks == 212 && next.Health == old.Health &&
            before.Rooms.Single(room => room.RoomIndex == 3).Units.Single(unit => unit.IsPyre).Health == 27 &&
            after.Rooms.Single(room => room.RoomIndex == 3).Units.Single(unit => unit.IsPyre).Health == 32 &&
            after.Context!.Statistics!.CardsPlayedThisTurn!.SequenceEqual(new[] { 22, 45 }),
            "Echo must add60 Valor once, heal five Pyre HP once and record its copied card play.");
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}
