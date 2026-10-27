using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class EchoWaxenRelicsChecks
{
    internal static void Native(FixtureValue source)
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
        Console.WriteLine("ECHO-WAXEN-RELICS PASS:41 native inputs/full state/RNG, paid3/raw copy/single curse, sealed equipment/slot/deployment relics, four immutable branches/four relic guards; pre-combat replay still separate.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
