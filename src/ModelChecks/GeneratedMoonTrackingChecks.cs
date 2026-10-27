using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class GeneratedMoonTrackingChecks
{
    internal static void InactiveRoomAttachment(FixtureValue source)
    {
        Require(source.GetProperty("Schema").GetInt32() == 1 &&
            source.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            source.GetProperty("EvidenceKind").GetString() == "NativeIncompleteBranchObservation" &&
            !source.GetProperty("NativeCompleted").GetBoolean(), "Expected preserved incomplete native prefix.");
        var rows = source.GetProperty("Rows").EnumerateArray().ToArray();
        Require(rows.Length == 17, "Wrong inactive attachment prefix.");
        BattleTurnState? current = null;
        for (int index = 0; index < rows.Length; index++)
        {
            var row = rows[index];
            var before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            var actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            if (current != null) Require(NativeBranchObservationChecks.Key(current) == NativeBranchObservationChecks.Key(before),
                "Incomplete native prefix changed its inputs.");
            var step = new BattleSearchStep(row.GetProperty("Action").Deserialize<PlayCardAction>(), row.GetProperty("IsEndTurn").GetBoolean());
            var result = BattlePlannerChecks.Replay(before, [step], out RoomOutcome outcome);
            Require(outcome == RoomOutcome.Exchanged && NativeBranchObservationChecks.Key(result) == NativeBranchObservationChecks.Key(actual),
                "Inactive captured native successor differs at " + index);
            if (index == 16)
            {
                var old = row.GetProperty("Expected").Deserialize<BattleTurnState>()!;
                Require(before.MoonPhaseTrackingActive == false && before.MoonPhase == "Full" &&
                    before.Spawn.Train.Rooms[2].Units.Single(unit => unit.Id == 11).MoonPhaseMask == 5 &&
                    old.Spawn.Train.Rooms[2].Units.Single(unit => unit.Id == 11).MoonPhaseMask == 5 &&
                    actual.Spawn.Train.Rooms[2].Units.Single(unit => unit.Id == 11).MoonPhaseMask == 6 &&
                    step.Action?.CardInstanceId == 29, "Missing original Fight Club/Funguy5-to6 witness.");
                string immutable = JsonSerializer.Serialize(before, ModelJson.Options);
                Parallel.For(0, 4, _ => Require(NativeBranchObservationChecks.Key(BattleActionModel.PlayCard(before, step.Action!).State!) ==
                    NativeBranchObservationChecks.Key(actual), "Parallel inactive attachment differs."));
                Require(immutable == JsonSerializer.Serialize(before, ModelJson.Options), "Inactive attachment changed its parent.");
            }
            current = actual;
        }
        Console.WriteLine("INACTIVE-MOON-ATTACHMENT PASS:17 original native inputs/full state/RNG and four immutable branches; Funguy11 understanding5-to6 without tracking/status changes; incomplete observation retained, not battle acceptance.");
    }
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, GeneratedMoonTrackingModel.Guard,
            normalizeComparison: CaptureKey, matchesRequestCapture: CapturesEquivalent);
        Require(rows.Length == 18, "Missing original seventeen inputs plus Fetch activation.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        CombatContext context = before.Spawn.Train.Context!;
        CardCreationRule creation = before.PlayRules!.Cards.Single(card => card.AssetKey == "FetchAbility")
            .Effects.Single().Generation!.Pool.Single(card => card.DataId == GeneratedMoonTrackingModel.EquipmentId);
        Require(before.Spawn.Turn == 3 && action.CardInstanceId == 36 && action.ActivatorUnitId == 6 && action.RoomIndex == 2 &&
            context.QueryFrame is { MoonPhase: 2, MoonPhaseTrackingActive: false } && context.NextCardId == 38 &&
            GeneratedMoonTrackingModel.KnownPlayerCreation(context, creation), "Wrong natural Fetch/Dusk Mantle boundary.");
        Require(actual.MoonPhaseTrackingActive == true && actual.MoonPhase == "Full" &&
            actual.Spawn.Train.Context!.Cards.Hand[0].InstanceId == 38 &&
            actual.Spawn.Train.Context.FindCard(38)!.DataId == GeneratedMoonTrackingModel.EquipmentId &&
            actual.Spawn.Train.Rooms.SelectMany(room => room.Units).All(unit => unit.MoonPhaseMask == 2 &&
                unit.Status("newmoon") == null && unit.Status("fullmoon") == null),
            "Native activation must reset understanding without adding phase statuses or advancing the phase.");
        string immutable = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            var result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && CaptureKey(NativeBranchObservationChecks.Key(result.State!)) == CaptureKey(NativeBranchObservationChecks.Key(actual)),
                "Generated moon tracking differs from full native state/RNG.");
        });
        Require(immutable == JsonSerializer.Serialize(before, ModelJson.Options), "Generated moon tracking changed its parent.");
        foreach (string control in new[] { "Id", "Cost", "Effect", "Traits" })
        {
            var changed = JsonSerializer.SerializeToNode(creation, ModelJson.Options)!;
            if (control == "Id") changed["DataId"] = "unknown-moon-card";
            if (control == "Cost") changed["MaskDescriptor"]!["BaseCost"] = 2;
            if (control == "Effect") changed["MaskDescriptor"]!["Definition"]!["MainEffectNames"]![0] = "UnknownEffect";
            if (control == "Traits") changed["ActivatesMoonTracking"] = false;
            Require(!GeneratedMoonTrackingModel.KnownPlayerCreation(context, changed.Deserialize<CardCreationRule>(ModelJson.Options)!),
                "Unknown generated moon signature accepted: " + control);
        }
        var pending = CardSpellModel.WithContext(before.Spawn.Train, actual.Spawn.Train.Context!);
        Require(GeneratedMoonTrackingModel.CompletePlayerReset(before.Spawn.Train, pending).Supported,
            "Known reset boundary rejected.");
        var callbackUnit = pending.Rooms[0].Units[0];
        var unknownCallback = new CombatTrigger("OnMoonLit", false, false, false, 0, []);
        var callbacks = new TrainCombatState(pending.Rooms.Select(room => room.RoomIndex == pending.Rooms[0].RoomIndex
            ? room.WithUnits(room.Units.Select(unit => unit.Id == callbackUnit.Id
                ? unit.WithTriggers(unit.Triggers.Concat([unknownCallback]).ToArray()) : unit).ToArray()) : room).ToArray(),
            pending.Movement, pending.EnemySlotsPerRoom, pending.Context);
        Require(!GeneratedMoonTrackingModel.CompletePlayerReset(before.Spawn.Train, callbacks).Supported,
            "Unknown activation callback accepted.");
        var badContext = JsonSerializer.SerializeToNode(context, ModelJson.Options)!;
        badContext["EnergyState"]!["Phase"] = "HeroTurn";
        Require(!GeneratedMoonTrackingModel.KnownPlayerCreation(badContext.Deserialize<CombatContext>(ModelJson.Options)!, creation),
            "Unobserved player reset timing accepted.");
        if (replay.HasValue)
        {
            NativeBranchObservationChecks.Replay(replay.Value, GeneratedMoonTrackingModel.Guard,
                normalizeComparison: CaptureKey, matchesRequestCapture: CapturesEquivalent);
            NativeBranchObservationChecks.Pair(source, replay.Value, normalizeComparison: CaptureKey, capturesEquivalent: CapturesEquivalent);
        }
        Console.WriteLine("GENERATED-MOON-TRACKING PASS:18 native inputs/full successor/forecast/RNG; Dusk Mantle38 enables tracking and resets all12 actors without phase/status changes; four immutable branches/four creation and two reset guards; independentPair=" + replay.HasValue);
    }
    private static string CaptureKey(string value)
    {
        // In the unchanged282 observation, inactive native masks were omitted.
        // Only unspawned definition defaults are known None(4). Live masks
        // remain unknown in the historical capture, never guessed as None.
        JsonNode root = JsonNode.Parse(value)!;
        void Visit(JsonNode? node)
        {
            if (node is JsonObject obj)
            {
                if (obj.ContainsKey("BaseAttack") && obj["Id"]?.GetValue<int>() == 0 &&
                    obj.ContainsKey("MoonPhaseMask") && obj["MoonPhaseMask"] == null) obj["MoonPhaseMask"] = 4;
                foreach (JsonNode? child in obj.Select(pair => pair.Value)) Visit(child);
            }
            else if (node is JsonArray array) foreach (JsonNode? child in array) Visit(child);
        }
        Visit(root);
        return root.ToJsonString();
    }
    private static bool CapturesEquivalent(BattleTurnState original, BattleTurnState repeated)
    {
        JsonNode expected = JsonNode.Parse(NativeBranchObservationChecks.Key(original))!;
        JsonNode actual = JsonNode.Parse(NativeBranchObservationChecks.Key(repeated))!;
        void BindOmitted(JsonNode? left, JsonNode? right)
        {
            if (left is JsonObject obj && right is JsonObject other)
            {
                if (obj.ContainsKey("MoonPhaseMask") && obj["MoonPhaseMask"] == null && other["MoonPhaseMask"] is JsonValue captured)
                {
                    int mask = captured.GetValue<int>();
                    Require(mask is >= 0 and <= 7, "Invalid newly captured native mask.");
                    obj["MoonPhaseMask"] = mask;
                }
                foreach (var field in obj) BindOmitted(field.Value, other[field.Key]);
            }
            else if (left is JsonArray array && right is JsonArray otherArray && array.Count == otherArray.Count)
                for (int index = 0; index < array.Count; index++) BindOmitted(array[index], otherArray[index]);
        }
        BindOmitted(expected, actual);
        return JsonNode.DeepEquals(expected, actual);
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
