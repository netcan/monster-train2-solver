using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class DuplicateRetainOrderChecks
{
    internal const string Reason = "Native card base-trait ordering differs after rolling input18";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason, (historical, actual) => {
            var old = historical.Spawn.Train.Context!.FindCard(31)!;
            var native = actual.Spawn.Train.Context!.FindCard(31)!;
            Require(old.MaskDescriptor!.BaseTraits.Select(trait => trait.RuntimeType).SequenceEqual(["CardTraitRetain", "CardTraitEphemeral"]) &&
                native.MaskDescriptor!.BaseTraits.Select(trait => trait.RuntimeType).SequenceEqual(["CardTraitEphemeral", "CardTraitRetain"]),
                "Original duplication trait-order error changed.");
            var changed = JsonSerializer.SerializeToNode(historical, ModelJson.Options)!;
            var card = changed["Spawn"]!["Train"]!["Context"]!["CardInstances"]!.AsArray().Single(card => card!["InstanceId"]!.GetValue<int>() == 31)!;
            var traits = card["MaskDescriptor"]!["BaseTraits"]!.AsArray();
            var first = traits[0]!.DeepClone(); var second = traits[1]!.DeepClone();
            traits.Clear(); traits.Add(second); traits.Add(first);
            Require(NativeBranchObservationChecks.Key(changed.Deserialize<BattleTurnState>(ModelJson.Options)!) == NativeBranchObservationChecks.Key(actual),
                "Original prediction differs beyond ordered duplicate trait installation.");
        });
        Require(rows.Length == 18 && !rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Missing17 natural prefix inputs and chosen hand duplication.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        var selected = before.Spawn.Train.Context!.FindCard(19)!;
        var copy = actual.Spawn.Train.Context!.FindCard(31)!;
        Require(before.Spawn.Turn == 2 && action.CardInstanceId == 25 && action.RecursionSelections!.Single().CardInstanceIds.SequenceEqual([19]) &&
            selected.DataId == "8de281af-ab9a-4d3d-ac5f-89cd43ff6c84" && selected.MaskDescriptor!.SourceTraits!.Count == 0 &&
            selected.MaskDescriptor.BaseTraits.Single().RuntimeType == "CardTraitRetain" &&
            copy.Permanent.Upgrades.Select(upgrade => upgrade.AssetKey).SequenceEqual(["StardustInvocation_Upgrade", "TraitRetain"]) &&
            copy.MaskDescriptor!.BaseTraits.Select(trait => trait.RuntimeType).SequenceEqual(["CardTraitEphemeral", "CardTraitRetain"]) &&
            copy.Temporary.Upgrades.Single().Stats.Heal == 30,
            "Missing natural retained Witchweave copy, optional Ephemeral-before-Retain and Conduit30.");
        string parent = NativeBranchObservationChecks.Key(before);
        Parallel.For(0, 4, _ => {
            var result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Retained hand duplication differs from full native state/RNG.");
        });
        Require(parent == NativeBranchObservationChecks.Key(before), "Retained duplication mutated parent.");
        var duplicate = before.PlayRules!.Cards.Single(rule => rule.DataId == before.Spawn.Train.Context.FindCard(25)!.DataId)
            .Effects.Single(effect => effect.Type == "DuplicateCard");
        foreach (string control in new[] { "Upgrade", "Asset", "Trait", "Amount", "Definition", "Removed", "Replaced", "Callback" })
        {
            var changed = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
            var card = changed["Spawn"]!["Train"]!["Context"]!["CardInstances"]!.AsArray().Single(card => card!["InstanceId"]!.GetValue<int>() == 19)!;
            var upgrade = card["Permanent"]!["Upgrades"]![0]!;
            var trait = upgrade["Lifecycle"]!["AddedTraits"]![0]!;
            if (control == "Upgrade") upgrade["DataId"] = "unknown-retain";
            if (control == "Asset") upgrade["AssetKey"] = "unknown-retain";
            if (control == "Trait") trait["RuntimeType"] = "unknown-retain";
            if (control == "Amount") trait["ParamInt"] = 1;
            if (control == "Definition") trait["DefinitionParamInt"] = 1;
            if (control == "Removed") upgrade["Lifecycle"]!["RemovedRuntimeTypes"]!.AsArray().Add("CardTraitRetain");
            if (control == "Replaced") upgrade["Lifecycle"]!["ReplacedAssets"]!.AsArray().Add("TraitRetain");
            if (control == "Callback") upgrade["ExternalInteractions"]!.AsArray().Add("unknown-retain-callback");
            Require(!CardDuplicateModel.Apply(changed.Deserialize<BattleTurnState>(ModelJson.Options)!.Spawn.Train.Context!, duplicate, 25, 19).Supported,
                "Unknown installed-Retain copy signature accepted: " + control);
        }
        if (replay.HasValue)
        {
            NativeBranchObservationChecks.Replay(replay.Value, Reason);
            NativeBranchObservationChecks.Pair(source, replay.Value, originalPredictionPresent: true);
        }
        Console.WriteLine("DUPLICATE-RETAIN-ORDER PASS:18 natural inputs, original ordered-trait counterexample, full successor/forecast/RNG, Ephemeral-before-copied-Retain, Conduit30, four immutable branches and eight unknown-signature controls; independentPair=" + replay.HasValue);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
