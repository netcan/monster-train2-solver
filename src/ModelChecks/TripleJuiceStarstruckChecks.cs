using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TripleJuiceStarstruckChecks
{
    internal const string Reason = ConsumedCurseCleanseChecks.Reason;
    internal const string CastReason = "Starstruck triple Juice needs a native Full Moon cast.";
    internal const string CurseReason = "Starstruck triple Juice needs native cumulative Titan Curse costs.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason);
        Require(rows.Length == 1, "Wrong natural Starstruck deployment boundary.");
        var before = rows[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var card = before.Spawn.Train.Context!.FindCard(24)!;
        Require(card.DataId == "753f20a2-c625-47b3-be4b-8600b673841e" &&
            card.MaskDescriptor!.BaseTraits.Select(trait => trait.RuntimeType).SequenceEqual([CardMoonUpgradeModel.Trait, CardJuiceModel.TripleTrait]) &&
            card.Permanent.Upgrades.Count == 1 && CardJuiceModel.Validate(card) == null,
            "Changed natural Starstruck/Moon/triple signature.");
        var active = CardMoonUpgradeModel.Update(card, 2).Card!;
        Require(CardJuiceModel.Validate(active) == null && active.Permanent.Upgrades.Count == 2 &&
            active.Permanent.Upgrades[1].Lifecycle!.Triggers.Single().Actions.Single().Statuses.Single().Stacks == 1,
            "Known Moon payload must remain its unscaled one-stack source definition.");
        foreach (string control in new[] { "Owner", "JuiceParam", "Order", "JuiceUpgrade", "MoonPayload", "MoonSource", "Temporary", "ExtraUpgrade", "SourceTraits", "Declared" }) {
            var node = JsonSerializer.SerializeToNode(active, ModelJson.Options)!;
            if (control == "Owner") node["DataId"] = "unknown-juice-moon";
            if (control == "JuiceParam") node["MaskDescriptor"]!["BaseTraits"]![1]!["ParamInt"] = 4;
            if (control == "Order") node["MaskDescriptor"]!["BaseTraits"]![0]!["RuntimeType"] = "CardTraitJuice3";
            if (control == "JuiceUpgrade") node["Permanent"]!["Upgrades"]![0]!["DataId"] = "unknown-triple-upgrade";
            if (control == "MoonPayload") node["Permanent"]!["Upgrades"]![1]!["Lifecycle"]!["Triggers"]![0]!["Actions"]![0]!["Statuses"]![0]!["Stacks"] = 2;
            if (control == "MoonSource") node["MaskDescriptor"]!["SourceTraits"]![0]!["MoonPhaseUpgrade"]!["DataId"] = "unknown-moon-upgrade";
            if (control == "Temporary") node["Temporary"]!["Upgrades"]!.AsArray().Add(node["Permanent"]!["Upgrades"]![0]!.DeepClone());
            if (control == "ExtraUpgrade") node["Permanent"]!["Upgrades"]!.AsArray().Add(node["Permanent"]!["Upgrades"]![0]!.DeepClone());
            if (control == "SourceTraits") node["MaskDescriptor"]!["SourceTraits"] = null;
            if (control == "Declared") node["MaskDescriptor"]!["BaseTraits"]![1]!["DeclaredName"] = "CardTraitJuice";
            Require(CardJuiceModel.Validate(node.Deserialize<CardInstanceState>(ModelJson.Options)!) != null,
                "Changed Starstruck signature lost its guard: " + control);
        }
        if (replay.HasValue) {
            NativeBranchObservationChecks.Replay(replay.Value, Reason);
            NativeBranchObservationChecks.Pair(source, replay.Value);
        }
        Console.WriteLine("TRIPLE-JUICE-STARSTRUCK PASS: natural deployment/full state/forecast/RNG; exact Moon payload retained; ten unknown-signature guards; independentPair=" + replay.HasValue);
    }

    internal static void Cast(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, CastReason);
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        var card = before.Spawn.Train.Context!.FindCard(24)!;
        var oldTarget = before.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == action.TargetUnitId);
        var target = actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == action.TargetUnitId);
        Require(action.CardInstanceId == 24 && before.Spawn.Train.Context.QueryFrame!.MoonPhase == 2 &&
            CardJuiceModel.Validate(card) == null && card.Permanent.Upgrades.Count == 2 &&
            target.Health > 0 && (target.Status("dazed")?.Stacks ?? 0) - (oldTarget.Status("dazed")?.Stacks ?? 0) == 3,
            "Native full-Moon Starstruck must add three Dazed stacks to a surviving target.");
        string parent = NativeBranchObservationChecks.Key(before);
        Parallel.For(0, 4, _ => {
            var result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Starstruck cast changed native successor/RNG.");
        });
        Require(parent == NativeBranchObservationChecks.Key(before), "Starstruck cast mutated parent.");
        var context = before.Spawn.Train.Context;
        var owner = context.Relics!.Single(relic => relic.AssetKey == "Enemy_Boss_Level3SiphonRage");
        Require(owner.DataId == "85ee2dcc-aa59-4be3-90ad-2a1d6ecba3df" &&
            owner.EffectTypes.SequenceEqual(["RelicEffectAddTrigger", "RelicEffectNull"]),
            "Missing original Arkion mixed trigger/null owner.");
        CardMoonUpgradeModel.RefreshHand(context, out string? refreshError);
        Require(refreshError == null, "Original mixed trigger/null owner blocked Moon hand refresh.");
        foreach (string control in new[] { "Owner", "Asset", "Effect", "Order", "Covenant", "Placement", "Trigger" }) {
            var node = JsonSerializer.SerializeToNode(context, ModelJson.Options)!;
            var relic = node["Relics"]!.AsArray().Single(item => item!["DataId"]!.GetValue<string>() == owner.DataId)!;
            if (control == "Owner") relic["DataId"] = "unknown-arkion-moon";
            if (control == "Asset") relic["AssetKey"] = "unknown-arkion-moon";
            if (control == "Effect") relic["EffectTypes"]![1] = "RelicEffectModifyCardMagicPowerOnCardPlayed";
            if (control == "Order") { relic["EffectTypes"]![0] = "RelicEffectNull"; relic["EffectTypes"]![1] = "RelicEffectAddTrigger"; }
            if (control == "Covenant") relic["IsCovenant"] = null;
            if (control == "Placement") relic["DisallowedInPlacementPhase"] = true;
            if (control == "Trigger") relic["TriggerAdditions"] = null;
            CardMoonUpgradeModel.RefreshHand(node.Deserialize<CombatContext>(ModelJson.Options)!, out string? error);
            Require(error != null, "Changed Arkion/Moon owner lost its guard: " + control);
        }
        if (replay.HasValue) {
            NativeBranchObservationChecks.Replay(replay.Value, CastReason);
            NativeBranchObservationChecks.Pair(source, replay.Value, originalPredictionPresent: true, originalPredictionMatched: true);
        }
        Console.WriteLine($"TRIPLE-JUICE-STARSTRUCK-CAST PASS: {rows.Length} natural inputs/full state/forecast/RNG; full Moon adds three Dazed; four immutable branches; seven Arkion/Moon guards; independentPair={replay.HasValue}");
    }

    internal static void Curse(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, CurseReason);
        Require(rows.Length == 36 && rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Wrong natural cumulative curse boundary.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var oldCard = before.Spawn.Train.Context!.FindCard(24)!;
        var card = actual.Spawn.Train.Context!.FindCard(24)!;
        Require(before.Spawn.Turn == 7 && actual.Spawn.Turn == 8 && oldCard.Temporary.Upgrades.Count == 1 &&
            card.Temporary.Upgrades.Count == 2 && card.Temporary.Upgrades.All(upgrade =>
                CardJuiceModel.KnownTitanCurse(upgrade) && upgrade.Stats.Cost == 1) && CardJuiceModel.Validate(card) == null,
            "Native Starstruck must retain both separate one-cost curse upgrades.");
        string parent = NativeBranchObservationChecks.Key(before);
        Parallel.For(0, 4, _ => {
            var result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Cumulative Starstruck curse changed native successor/RNG.");
        });
        Require(parent == NativeBranchObservationChecks.Key(before), "Cumulative curse changed its parent.");
        foreach (string control in new[] { "Owner", "Asset", "Cost", "Damage", "Trigger" }) {
            var node = JsonSerializer.SerializeToNode(card, ModelJson.Options)!;
            var upgrade = node["Temporary"]!["Upgrades"]![1]!;
            if (control == "Owner") upgrade["DataId"] = "unknown-curse";
            if (control == "Asset") upgrade["AssetKey"] = "unknown-curse";
            if (control == "Cost") upgrade["Stats"]!["Cost"] = 2;
            if (control == "Damage") upgrade["Stats"]!["Damage"] = 1;
            if (control == "Trigger") upgrade["Lifecycle"]!["Triggers"]!.AsArray().Add(
                JsonSerializer.SerializeToNode(CardMoonUpgradeModel.Update(card, 2).Card!.Permanent.Upgrades[1].Lifecycle!.Triggers.Single(), ModelJson.Options));
            Require(CardJuiceModel.Validate(node.Deserialize<CardInstanceState>(ModelJson.Options)!) != null,
                "Changed cumulative Starstruck curse lost guard: " + control);
        }
        if (replay.HasValue) {
            NativeBranchObservationChecks.Replay(replay.Value, CurseReason);
            NativeBranchObservationChecks.Pair(source, replay.Value, originalPredictionPresent: true, originalPredictionMatched: true);
        }
        Console.WriteLine($"TRIPLE-JUICE-STARSTRUCK-CURSE PASS: {rows.Length} natural inputs/full state/forecast/RNG; two separate curse costs; four immutable branches; five unknown-curse guards; independentPair={replay.HasValue}");
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}
