using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class InfusionTriggerMergeChecks
{
    private const string Reason = "Repeated infusion OnCast damage differs from native.";
    internal static void Pair(FixtureValue source, FixtureValue replay)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason, Historical);
        NativeBranchObservationChecks.Replay(replay, Reason);
        NativeBranchObservationChecks.Pair(source, replay, originalPredictionPresent: true);
        var row = rows[^1];
        var before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var after = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
        var card = before.Spawn.Train.Context!.FindCard(26)!;
        var rule = before.PlayRules!.Cards.Single(rule => rule.DataId == card.DataId);
        Require(rows.Length == 27 && before.Spawn.Turn == 4 && action.CardInstanceId == 26 &&
            action.TargetUnitId == 18 && action.RoomIndex == 0 && rule.AssetKey == "BlastInfusion" &&
            card.Permanent.Upgrades.Select(upgrade => upgrade.AssetKey).SequenceEqual(new[] {
                "Infusion_UnstableSingle", "Infusion_DamageSingle", "Infusion_UnstableSingle" }) &&
            before.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units.Single(unit => unit.Id == 16).Health == 260 &&
            after.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units.Single(unit => unit.Id == 16).Health == 250 &&
            after.Spawn.Train.Rooms.SelectMany(room => room.Units).All(unit => unit.Id is not (17 or 18)),
            "Missing the natural duplicate-upgrade lethal explosion boundary.");
        var merged = CardModifierModel.Resolve(rule, card).OnCastEffects;
        Require(merged.Count == 2 && merged[0].Type == "AddStatus" && merged[0].Statuses.Single().Stacks == 10 &&
            merged[1].Type == "Damage" && merged[1].Value == 4, "Unstable upgrades must merge before damage.");
        Controls(rule, card);
        Console.WriteLine("INFUSION-TRIGGER-MERGE PASS:27 paired native inputs/full state/RNG; boss260->250; original255 forecast retained; merged damage/magic power and ordinary-card controls.");
    }

    private static void Historical(BattleTurnState predicted, BattleTurnState actual)
    {
        var node = JsonSerializer.SerializeToNode(predicted, ModelJson.Options)!;
        var room = node["Spawn"]!["Train"]!["Rooms"]!.AsArray().Single(room => room!["RoomIndex"]!.GetValue<int>() == 0)!;
        var boss = room["Units"]!.AsArray().Single(unit => unit!["Id"]!.GetValue<int>() == 16)!;
        Require(boss["Health"]!.GetValue<int>() == 255, "Historical mismatching boss health was overwritten.");
        boss["Health"] = 250;
        var values = node["Spawn"]!["Train"]!["Context"]!["Statistics"]!["Values"]!.AsArray()
            .Where(value => value!["CardId"]!.GetValue<int>() == 26 &&
                value["Type"]!.GetValue<string>() == "AnyStatusEffectStacksAdded" &&
                value["Duration"]!.GetValue<string>() is "ThisBattle" or "ThisTurn").ToArray();
        Require(values.Length == 2 && values.All(value => value!["Value"]!.GetValue<int>() == 5),
            "Historical status-stack counters were overwritten.");
        foreach (var value in values) value!["Value"] = 10;
        var corrected = node.Deserialize<BattleTurnState>(ModelJson.Options)!;
        Require(NativeBranchObservationChecks.Key(corrected) == NativeBranchObservationChecks.Key(actual),
            "Original forecast differs beyond explosion damage and applied-stack counters: " + ModelJson.Difference(
                NativeBranchObservationChecks.Key(corrected), NativeBranchObservationChecks.Key(actual)));
    }

    private static void Controls(CardPlayRule rule, CardInstanceState card)
    {
        var node = JsonSerializer.SerializeToNode(card, ModelJson.Options)!;
        var upgrades = node["Permanent"]!["Upgrades"]!.AsArray();
        var unstable = upgrades[0]!.DeepClone();
        var damage = upgrades[1]!.DeepClone();
        upgrades.Clear();
        upgrades.Add(damage); upgrades.Add(unstable); upgrades.Add(damage.DeepClone());
        node["Temporary"]!["Upgrades"] = new JsonArray();
        // A separate spell-power upgrade must apply once to the merged base8.
        var power = damage.DeepClone();
        power["DataId"] = "merge-control-magic-power";
        power["Stats"]!["Damage"] = 2;
        power["Lifecycle"] = null;
        upgrades.Add(power);
        var changed = node.Deserialize<CardInstanceState>(ModelJson.Options)!;
        var effects = CardModifierModel.Resolve(rule, changed).OnCastEffects;
        Require(effects.Count == 2 && effects[0].Type == "Damage" && effects[0].Value == 10 &&
            effects[1].Statuses.Single().Stacks == 5, "Merged base damage must receive spell power once.");
        var traits = node["MaskDescriptor"]!["BaseTraits"]!.AsArray();
        foreach (var trait in traits.Where(trait => trait!["RuntimeType"]!.GetValue<string>() == "CardTraitInfusion").ToArray())
            traits.Remove(trait);
        effects = CardModifierModel.Resolve(rule, node.Deserialize<CardInstanceState>(ModelJson.Options)!).OnCastEffects;
        Require(effects.Count == 3 && effects[0].Type == "Damage" && effects[0].Value == 6 &&
            effects[1].Type == "AddStatus" && effects[2].Type == "Damage" && effects[2].Value == 6,
            "Ordinary cards must retain separate upgrade triggers.");
        Require(card.Permanent.Upgrades.Count == 3 && card.Permanent.Upgrades[0].Lifecycle!.Triggers[0].Actions[0].Statuses.Single().Stacks == 5,
            "Controls mutated the original card.");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
