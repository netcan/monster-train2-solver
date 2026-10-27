using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class EchoWaxenRelicsChecks
{
    private const string EndReason = "Extended relic Waxen Echo retains paid3 and replays its raw effects during pre-combat.";
    internal static void Native(FixtureValue source, FixtureValue? endTurn = null, FixtureValue? replay = null)
    {
        string reason = source.GetProperty("Request").GetProperty("Reason").GetString()!;
        Require(reason.StartsWith("Echo replay needs native calibration: onCast=0, unplayed=0, interactions=, traits=CardTraitScalingUpgradeUnitAttack;CardTraitExhaustState, relics=", StringComparison.Ordinal),
            "Missing the extended-relic Waxen Spike Echo guard.");
        var rows = NativeBranchObservationChecks.Replay(source, reason);
        Require(rows.Length == 41, "Expected40 native inputs and the Waxen Spike Echo copy.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var after = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        var context = after.Spawn.Train.Context!;
        var echo = context.RoomCapacities!.Single(room => room.RoomIndex == 1).Attachments!
            .Single(attachment => attachment.Echo != null).Echo!;
        var copy = context.FindCard(echo.CopyCardId)!;
        Require(before.Spawn.Turn == 8 && before.Energy == 3 && action.CardInstanceId == 35 && action.RoomIndex == 1 &&
            action.TargetUnitId == 0 && after.Energy == 0 && echo is { OriginalCardId: 35, CopyCardId: 44, PaidCost: 3 } &&
            context.NextCardId == 45 && copy.LastPlayedCost == 3 && copy.PlayCount == 0 && copy.Temporary.Upgrades.Count == 1 &&
            CardJuiceModel.KnownTitanCurse(copy.Temporary.Upgrades.Single()) &&
            EchoWaxenSpikeModel.Known(copy, echo.Effects!) && EchoJuiceSpikeModel.KnownRelics(context) &&
            JsonSerializer.Serialize(echo.Effects, ModelJson.Options) == JsonSerializer.Serialize(
                before.PlayRules!.Cards.Single(rule => rule.DataId == EchoWaxenSpikeModel.DataId).Effects, ModelJson.Options) &&
            context.Relics!.Any(relic => relic.AssetKey == "ManyBuffonAngelEquip") &&
            context.Relics.Any(relic => relic.AssetKey == "ExtraUnitUpgrades") &&
            context.Relics.Any(relic => relic.AssetKey == "MoreDeploymentPhaseEmber"),
            "Expected paid3/raw effects/single-curse copy under the natural equipment, slot and deployment relics.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            var result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(after),
                "Extended-relic Waxen Echo differs from full native state/RNG.");
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Extended-relic Waxen Echo mutated parent.");
        foreach (string control in new[] { "Identity", "EquipmentCallback", "Deployment", "EnemyCallback" })
        {
            var changed = JsonSerializer.SerializeToNode(context, ModelJson.Options)!;
            var relics = changed["Relics"]!.AsArray();
            if (control == "Identity") relics.Single(relic => relic!["AssetKey"]!.GetValue<string>() == "ExtraUnitUpgrades")!["DataId"] = "unknown-slots";
            if (control == "EquipmentCallback") relics.Single(relic => relic!["AssetKey"]!.GetValue<string>() == "ManyBuffonAngelEquip")!["TriggerAdditions"]![0]!["Triggers"]![0]!["Kind"] = "OnCardPlayed";
            if (control == "Deployment") relics.Single(relic => relic!["AssetKey"]!.GetValue<string>() == "MoreDeploymentPhaseEmber")!["ResourceEffects"]![0]!["ParamInt"] = 4;
            if (control == "EnemyCallback") relics.Single(relic => relic!["AssetKey"]!.GetValue<string>() == "Enemy_CorruptSeraph_BuffAllies")!["EffectTypes"]![0] = "RelicEffectCopyCardOnCardPlayed";
            Require(!EchoJuiceSpikeModel.KnownRelics(changed.Deserialize<CombatContext>(ModelJson.Options)!), "Unknown Echo relic admitted: " + control);
        }
        if (endTurn.HasValue)
        {
            var extended = NativeBranchObservationChecks.Replay(endTurn.Value, EndReason);
            Require(extended.Length == 42 && extended[^1].GetProperty("IsEndTurn").GetBoolean(), "Missing extended-relic Echo EndTurn.");
            for (int i = 0; i < rows.Length; i++)
            {
                foreach (string field in new[] { "Action", "IsEndTurn" })
                    Require(rows[i].GetProperty(field).ContentEquals(extended[i].GetProperty(field)), "Echo extension changed its input.");
                foreach (string field in new[] { "Before", "Actual" })
                    Require(NativeBranchObservationChecks.Key(rows[i].GetProperty(field).Deserialize<BattleTurnState>()!) ==
                        NativeBranchObservationChecks.Key(extended[i].GetProperty(field).Deserialize<BattleTurnState>()!),
                        "Echo extension changed a complete prefix state/RNG.");
            }
            var final = extended[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            Require(final.Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 1).Attachments!
                .Single(attachment => attachment.Echo != null).Echo!.CopyCardId == 0, "Echo cache did not clear after replay.");
            string immutable = JsonSerializer.Serialize(after, ModelJson.Options);
            Parallel.For(0, 4, _ =>
            {
                var result = BattleTurnModel.EndTurn(after);
                Require(result.Supported && result.Outcome == RoomOutcome.Exchanged &&
                    NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(final),
                    "Extended-relic Echo EndTurn differs from complete native state/RNG.");
            });
            Require(immutable == JsonSerializer.Serialize(after, ModelJson.Options), "Echo EndTurn mutated parent.");
            CheckPreCombat(endTurn.Value);
            if (replay.HasValue)
            {
                NativeBranchObservationChecks.Replay(replay.Value, EndReason);
                NativeBranchObservationChecks.Pair(endTurn.Value, replay.Value, originalPredictionPresent: true, originalPredictionMatched: true);
                CheckPreCombat(replay.Value);
            }
        }
        Require(!replay.HasValue || endTurn.HasValue, "Replay requires its source EndTurn observation.");
        Console.WriteLine("ECHO-WAXEN-RELICS PASS:41 native inputs/full state/RNG, paid3/raw copy/single curse, sealed equipment/slot/deployment relics, four immutable branches/four relic guards; nativeEndTurn=" + endTurn.HasValue + "; independentPair=" + replay.HasValue);
    }
    private static void CheckPreCombat(FixtureValue fixture)
    {
        var phase = fixture.GetProperty("RoomPreCombatRecords").EnumerateArray().Single(row =>
            row.GetProperty("Turn").GetInt32() == 8 && row.GetProperty("RoomIndex").GetInt32() == 1);
        Require(phase.GetProperty("Completed").GetBoolean(), "Incomplete native Echo pre-combat callback.");
        var before = phase.GetProperty("Before").Deserialize<TrainCombatState>()!.Rooms.Single(room => room.RoomIndex == 1);
        var after = phase.GetProperty("After").Deserialize<TrainCombatState>()!.Rooms.Single(room => room.RoomIndex == 1);
        foreach (int id in new[] { 2, 26, 27 })
        {
            var old = before.Units.Single(unit => unit.Id == id);
            var next = after.Units.Single(unit => unit.Id == id);
            Require(next.Attack - old.Attack == 12 && next.Health == old.Health &&
                next.Status("ephemeral")!.Stacks - old.Status("ephemeral")!.Stacks == 4 &&
                next.Modifiers!.Upgrades.Count == old.Modifiers!.Upgrades.Count + 1 &&
                next.Modifiers.Upgrades[^1].DataId == "31243b24-f327-42c8-84ad-b014e6fec675" &&
                next.Modifiers.Upgrades[^1].Stats.Damage == 12, "Native paid3 Echo must add12 attack/four Burnout once per ally.");
        }
        Require(before.Units.Where(unit => unit.Team == CombatTeam.Enemy).All(old =>
            JsonSerializer.Serialize(old, ModelJson.Options) == JsonSerializer.Serialize(after.Units.Single(unit => unit.Id == old.Id), ModelJson.Options)),
            "Friendly Waxen Echo changed a native enemy.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
