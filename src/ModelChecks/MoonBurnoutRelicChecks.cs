using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class MoonBurnoutRelicChecks
{
    internal const string Reason = "Moon-phase status application with relic modifiers is not modeled.";
    internal const string HandRefreshReason = "Uncalibrated moon hand-refresh relic: RageOnBurnoutStackLoss.";

    internal static void NativeHandRefresh(FixtureValue fixture)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture, HandRefreshReason,
            expectedGameModule: new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d"));
        var boundary = rows[^1];
        var before = boundary.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = boundary.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var context = actual.Spawn.Train.Context!;
        var original = before.Spawn.Train.Context!;
        var drawn = context.Cards.Hand.Where(token => original.Cards.Hand.All(old => old.InstanceId != token.InstanceId))
            .Select(token => context.FindCard(token.InstanceId)!).Single(CardMoonUpgradeModel.Present);
        var owner = context.Relics!.Single(relic => relic.AssetKey == "RageOnBurnoutStackLoss");
        Require(rows.Length == 13 && boundary.GetProperty("IsEndTurn").GetBoolean() && before.Spawn.Turn == 2 &&
            actual.Spawn.Turn == 3 && original.QueryFrame?.MoonPhase == 1 && context.QueryFrame?.MoonPhase == 2 &&
            RelicStatusStackModel.IsCalibratedBurnoutLossRelic(owner) && drawn.MaskDescriptor!.BaseTraits.Count(trait =>
                trait.MoonPhaseUpgradeState is { Active: true, Cached: true }) == 1 &&
            original.FindCard(drawn.InstanceId)!.MaskDescriptor!.BaseTraits.All(trait =>
                trait.MoonPhaseUpgradeState is { Active: false, Cached: false }),
            "Expected the same Burnout-loss owner and a newly drawn spell with a native full-Moon upgrade.");

        foreach (string control in new[] { "DataId", "EffectTypes", "TriggerStatusId", "AdditionalStacks", "DisallowedInPlacementPhase" })
        {
            var node = JsonSerializer.SerializeToNode(owner, ModelJson.Options)!;
            switch (control)
            {
                case "DataId": node[control] = "unknown-owner"; break;
                case "EffectTypes": node[control]!.AsArray().Add(RelicModel.AddStatusOnApplied); break;
                case "TriggerStatusId": node["StatusStackAdditions"]![0]![control] = "newmoon"; break;
                case "AdditionalStacks": node["StatusStackAdditions"]![0]![control] = 4; break;
                case "DisallowedInPlacementPhase": node[control] = true; break;
            }
            var changed = node.Deserialize<CombatRelicState>(ModelJson.Options)!;
            var rejected = context.WithRelics(context.Relics!.Select(relic => ReferenceEquals(relic, owner) ? changed : relic).ToArray());
            CardMoonUpgradeModel.RefreshHand(rejected, out string? error, [drawn.InstanceId]);
            Require(error == HandRefreshReason, "Changed Burnout-loss owner passed Moon hand refresh: " + control);
        }
        Console.WriteLine("MOON-BURNOUT-HAND-REFRESH PASS: 13 original inputs, full successor/RNG, native drawn-card upgrade and five owner guards.");
    }

    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason, revalidateHistoricalCapture: Recapture);
        var row = rows[^1];
        var before = Recapture(row.GetProperty("Before").Deserialize<BattleTurnState>()!);
        var actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var context = before.Spawn.Train.Context!;
        var owner = context.Relics!.Single(relic => relic.AssetKey == "RageOnBurnoutStackLoss");
        Require(rows.Length == 2 && row.GetProperty("IsEndTurn").GetBoolean() &&
            before.Spawn.Turn == 0 && actual.Spawn.Turn == 1 &&
            context.QueryFrame!.MoonPhase == 1 && actual.Spawn.Train.Context!.QueryFrame!.MoonPhase == 2 &&
            owner.DataId == "85667d60-8053-4204-8c02-ba101dab7945" &&
            owner.StatusStackAdditions!.Single().TriggerStatusId == "ephemeral" && RelicModel.Validate(context) == null,
            "Missing natural new-to-full Moon with unchanged Burnout-loss Rage relic.");
        string parent = NativeBranchObservationChecks.Key(before);
        Parallel.For(0, 4, _ => {
            var result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Moon/Burnout relic branch differs from complete native successor/RNG.");
        });
        Require(parent == NativeBranchObservationChecks.Key(before), "Moon/Burnout relic branch changed parent.");
        foreach (string control in new[] { "Owner", "Asset", "Effect", "NewMoon", "FullMoon", "Status", "Amount", "Team", "Condition", "Definition", "Magnitude" })
        {
            var changed = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
            var relic = changed["Spawn"]!["Train"]!["Context"]!["Relics"]!.AsArray()
                .Single(relic => relic!["DataId"]!.GetValue<string>() == owner.DataId)!;
            var rule = relic["StatusStackAdditions"]![0]!;
            if (control == "Owner") relic["DataId"] = "unknown-burnout-relic";
            if (control == "Asset") relic["AssetKey"] = "unknown-burnout-relic";
            if (control == "Effect") relic["EffectTypes"]![0] = "RelicEffectAddStatusEffectOnStatusApplied";
            if (control == "NewMoon") rule["TriggerStatusId"] = "newmoon";
            if (control == "FullMoon") rule["TriggerStatusId"] = "fullmoon";
            if (control == "Status") rule["StatusId"] = "newmoon";
            if (control == "Amount") rule["AdditionalStacks"] = 4;
            if (control == "Team") rule["SourceTeam"] = "Heroes";
            if (control == "Condition") rule["AddCondition"] = 1;
            if (control == "Definition") rule["AddedStatusDefinition"] = null;
            if (control == "Magnitude") relic["StatusMagnitudes"] = JsonSerializer.SerializeToNode(new[] { new RelicStatusMagnitude(0, 1, "buff", 1) }, ModelJson.Options);
            var rejected = BattleTurnModel.EndTurn(changed.Deserialize<BattleTurnState>(ModelJson.Options)!);
            Require(!rejected.Supported && rejected.State == null, "Unknown moon/relic signature accepted: " + control);
        }
        if (replay.HasValue)
        {
            NativeBranchObservationChecks.Replay(replay.Value, Reason);
            NativeBranchObservationChecks.Pair(source, replay.Value);
        }
        Console.WriteLine("MOON-BURNOUT-RELIC PASS: two natural inputs, new-to-full Moon, complete successor/forecast/RNG, four immutable branches and11 unknown-signature controls; independentPair=" + replay.HasValue);
    }

    // Old capture stored the validator's refusal in ExternalInteractions. Preserve
    // that archive and require fresh validation before recapturing only this field
    // for its offline replay. Production still rejects any captured interaction.
    private static BattleTurnState Recapture(BattleTurnState source)
    {
        if (!source.ExternalInteractions.Contains(Reason)) return source;
        Require(!BattleTurnModel.EndTurn(source).Supported, "Original captured guard must remain effective.");
        Require(MoonPhaseTransitionModel.Validate(source.Spawn.Train,
            new CardActionEffect("AdvanceMoonPhase", "Room", 1, false, false, Array.Empty<CombatStatus>()),
            0, playerActionOnly: false) == null, "Historical capture no longer matches calibrated Moon signature.");
        var node = JsonSerializer.SerializeToNode(source, ModelJson.Options)!;
        node["ExternalInteractions"] = JsonSerializer.SerializeToNode(source.ExternalInteractions.Where(value => value != Reason).ToArray());
        var result = node.Deserialize<BattleTurnState>(ModelJson.Options)!;
        Require(NativeBranchObservationChecks.Key(result) == NativeBranchObservationChecks.Key(source), "Recapture changed native state/RNG.");
        return result;
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
