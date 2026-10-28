using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class GeneratedMoonRetiredEnemiesChecks
{
    internal static void Native(FixtureValue fixture, FixtureValue? replay = null, bool tenRetired = false, bool mixedRetired = false)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture, GeneratedMoonTrackingModel.Guard);
        Require(rows.Length == (mixedRetired ? 35 : tenRetired ? 39 : 43) && !rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Missing native chain.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        var context = before.Spawn.Train.Context!;
        var creation = before.PlayRules!.Cards.SelectMany(card => card.Effects).Where(effect => effect.Generation != null)
            .SelectMany(effect => effect.Generation!.Pool).First(card => card.DataId == GeneratedMoonTrackingModel.AlternateEquipmentId);
        int phase = mixedRetired ? 2 : 1;
        Require(action.CardInstanceId == (tenRetired || mixedRetired ? 40 : 42) && context.QueryFrame?.MoonPhase == phase && context.QueryFrame.MoonPhaseTrackingActive == false &&
            GeneratedMoonTrackingModel.KnownPlayerCreation(context, creation) && actual.Spawn.Train.Context!.FindCard(mixedRetired ? 41 : tenRetired ? 42 : 43)?.DataId == creation.DataId &&
            actual.Spawn.Train.Context.QueryFrame?.MoonPhaseTrackingActive == true &&
            actual.Spawn.Train.Rooms.SelectMany(room => room.Units).All(unit => unit.MoonPhaseMask == phase),
            "Missing original equipment activation and live-unit reset.");
        var oldRetired = context.Enchantments!.RetainedUnits;
        var newRetired = actual.Spawn.Train.Context.Enchantments!.RetainedUnits;
        Require(oldRetired.Count == (tenRetired || mixedRetired ? 10 : 8) && JsonSerializer.Serialize(oldRetired, ModelJson.Options) == JsonSerializer.Serialize(newRetired, ModelJson.Options),
            "Native reset changed retained destroyed actors.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ => {
            var result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Generated moon reset differs: " + result.Reason);
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "PlayCard mutated its parent.");
        foreach (string control in new[] { "Id", "Definition", "Cost", "Effect" })
        {
            JsonNode node = JsonSerializer.SerializeToNode(creation, ModelJson.Options)!;
            if (control == "Id") node["DataId"] = "unknown";
            if (control == "Definition") node["MaskDescriptor"]!["Definition"]!["DataId"] = "unknown";
            if (control == "Cost") node["MaskDescriptor"]!["BaseCost"] = 2;
            if (control == "Effect") node["MaskDescriptor"]!["Definition"]!["MainEffectNames"]![0] = "unknown";
            Require(!GeneratedMoonTrackingModel.KnownPlayerCreation(context, node.Deserialize<CardCreationRule>(ModelJson.Options)!),
                "Unknown equipment escaped its creation guard: " + control);
        }
        var pending = CardSpellModel.WithContext(before.Spawn.Train, actual.Spawn.Train.Context);
        Require(GeneratedMoonTrackingModel.CompletePlayerReset(before.Spawn.Train, pending).Supported, "Known reset rejected.");
        foreach (string control in new[] { "Identity", "Room", "Preview", "Health", "Destroyed", "Mask", "Count", "Extra" })
        {
            JsonNode node = JsonSerializer.SerializeToNode(pending, ModelJson.Options)!;
            var actor = node["Context"]!["Enchantments"]!["RetainedUnits"]![0]!;
            if (control == "Identity") actor["Unit"]!["AssetKey"] = "unknown";
            if (control == "Room") actor["RoomIndex"] = 0;
            if (control == "Preview") actor["Preview"] = true;
            if (control == "Health") actor["Unit"]!["Health"] = 1;
            if (control == "Destroyed") actor["Unit"]!["DeathState"]!["IsDestroyed"] = false;
            if (control == "Mask") actor["Unit"]!["MoonPhaseMask"] = 7;
            if (control == "Count") node["Context"]!["Enchantments"]!["RetainedUnits"]!.AsArray().RemoveAt(0);
            if (control == "Extra") node["Context"]!["Enchantments"]!["RetainedUnits"]!.AsArray().Add(actor.DeepClone());
            Require(!GeneratedMoonTrackingModel.CompletePlayerReset(before.Spawn.Train, node.Deserialize<TrainCombatState>(ModelJson.Options)!).Supported,
                "Unknown retained actor escaped its reset guard: " + control);
        }
        if (mixedRetired)
        {
            foreach (string control in new[] { "MoonPhase", "Turn" })
            {
                JsonNode node = JsonSerializer.SerializeToNode(pending, ModelJson.Options)!;
                node["Context"]!["QueryFrame"]![control] = 1;
                Require(!GeneratedMoonTrackingModel.CompletePlayerReset(before.Spawn.Train, node.Deserialize<TrainCombatState>(ModelJson.Options)!).Supported,
                    "Unknown mixed-retired reset boundary accepted: " + control);
            }
        }
        if (replay.HasValue) { Native(replay.Value, tenRetired: tenRetired, mixedRetired: mixedRetired); NativeBranchObservationChecks.Pair(fixture, replay.Value); }
        Console.WriteLine($"GENERATED-MOON-RETIRED-ENEMIES PASS: {rows.Length} native inputs/full state/RNG, live reset and {oldRetired.Count} unchanged retained actors, four immutable branches/{(mixedRetired ? 14 : 12)} guards; independentPair={replay.HasValue}");
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}
