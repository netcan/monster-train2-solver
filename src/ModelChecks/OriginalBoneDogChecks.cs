using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalBoneDogChecks
{
    private static CombatRelicState Owner(float percentage = .25f) => new(RelicPyreResurrectionModel.DataId,
        RelicPyreResurrectionModel.AssetKey, [RelicPyreResurrectionModel.Effect], isCovenant: false,
        disallowedInPlacementPhase: false, resourceEffects: [new(0, RelicPyreResurrectionModel.Effect, 25, 0, 0, false,
            conditions: [new(new("PyreHeartResurrection"), false, 0, 1, false)], paramFloat: percentage)]);
    internal static void Run()
    {
        UnityRng rng = UnityRng.Seed(17);
        CombatContext root = new(new([], [], [], rng, 0, []), rng, 0, 1, 10, [], cardInstances: [],
            relics: [Owner()], queryFrame: new(pyreResurrectionCount: 0), otherPiles: [],
            statistics: new([], [], [], [], [], [], [], 0, 0, 0, 0, 0, deckCards: [], storedCards: []));
        string parent = JsonSerializer.Serialize(root, ModelJson.Options);
        void Replay()
        {
            CombatContext result = RelicPyreResurrectionModel.TryApply(root, out float percentage, out string? error);
            Require(error == null && percentage == .25f && result.QueryFrame!.PyreResurrectionCount == 1, error ?? "First resurrection did not consume the run flag.");
            RelicConditionState condition = result.Relics!.Single().ResourceEffects!.Single().Conditions.Single();
            Require(condition.Triggered && condition.ValueAtLastTrigger == 1 && condition.DurationTriggerCount == 1,
                "Notification must record the already-consumed flag.");
            Equal(RelicPyreResurrectionModel.TryApply(result, out percentage, out error), result, "consumed control");
            Require(percentage == 0 && error == null, "Consumed resurrection returned a percentage.");
            CombatContext next = RelicConditionModel.EndDuration(result, "ThisTurn");
            Equal(RelicPyreResurrectionModel.TryApply(next, out percentage, out error), next, "next-turn control");
            Require(percentage == 0 && error == null && next.QueryFrame!.PyreResurrectionCount == 1, "Turn reset restored a run-scoped resurrection.");
        }
        Replay(); Parallel.For(0, 16, _ => Replay());
        Require(JsonSerializer.Serialize(root, ModelJson.Options) == parent, "Resurrection mutated its parent.");
        CombatContext ablated = root.WithRelics([]);
        Equal(RelicPyreResurrectionModel.TryApply(ablated, out float absent, out string? absentError), ablated, "owner ablation");
        Require(absent == 0 && absentError == null, "Absent owner resurrected.");
        RelicPyreResurrectionModel.TryApply(root.WithQueryFrame(new()), out _, out string? missing);
        RelicPyreResurrectionModel.TryApply(root.WithRelics([Owner(.5f)]), out _, out string? changed);
        Require(missing != null && changed != null, "Missing consumed flag or changed definition was accepted.");
        Console.WriteLine("BONE-DOG-PURE PASS: first/consumed/next-turn/absent owner, post-mark notification, payload rejection and 16 immutable branches.");
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 241 && root.GetProperty("OriginalBoneDogPrepared").GetBoolean() &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Incomplete Bone Dog battle.");
        foreach (string field in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending", "OriginalBoneDogCaptureFailures",
            "OriginalBoneDogMismatches", "OriginalBoneDogUnsupported", "OriginalBoneDogPending" })
            Require(root.GetProperty(field).GetInt32() == 0, "Nonzero " + field);
        int successes = 0, consumed = 0; bool laterTurn = false;
        foreach (FixtureValue row in root.GetProperty("RelicBoneDogRecords").EnumerateArray())
        {
            TrainCombatState before = row.GetProperty("Before").Deserialize<TrainCombatState>()!, actual = row.GetProperty("Actual").Deserialize<TrainCombatState>()!;
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            void Replay()
            {
                CombatContext result = RelicPyreResurrectionModel.TryApply(before.Context!, out float percentage, out string? error);
                Require(error == null && percentage == row.GetProperty("Percentage").GetSingle() &&
                    (percentage > 0) == row.GetProperty("Returned").GetBoolean(), error ?? "Native resurrection result differs.");
                Equal(CardSpellModel.WithContext(before, result), actual, "native resurrection manager");
            }
            Replay(); Parallel.For(0, 16, _ => Replay());
            Require(JsonSerializer.Serialize(before, ModelJson.Options) == parent, "Native resurrection mutated parent state.");
            string[] notifications = row.GetProperty("Notifications").Deserialize<string[]>()!;
            if (row.GetProperty("Returned").GetBoolean())
            {
                successes++;
                Require(notifications.SequenceEqual(["Triggered:1"]), "Native notification preceded persistent marking.");
            }
            else
            { consumed++; Require(notifications.Length == 0 && before.Context!.QueryFrame!.PyreResurrectionCount == 1, "Consumed control differs.");
                laterTurn |= before.Context.QueryFrame.Turn > 0; }
        }
        Require(successes == 1 && consumed >= 1 && laterTurn, "Missing once-per-run and later-turn manager controls.");
        var previews = root.GetProperty("PyreResurrectionPreviews").EnumerateArray().ToArray();
        Require(previews.Length >= 1 && previews.All(row => row.GetProperty("Completed").GetBoolean()), "Missing completed native preview observation.");
        foreach (FixtureValue row in previews)
        {
            RelicConditionState before = row.GetProperty("BeforeConditions").Deserialize<RelicConditionState[]>()!.Single();
            RelicConditionState after = row.GetProperty("AfterConditions").Deserialize<RelicConditionState[]>()!.Single();
            Require(row.GetProperty("BeforeFlag").GetInt32() == 0 && row.GetProperty("AfterFlag").GetInt32() == 1 &&
                row.GetProperty("Returned").GetBoolean() && row.GetProperty("Percentage").GetSingle() == .25f &&
                after.DurationTriggerCount == before.DurationTriggerCount + 1 && after.ValueAtLastTrigger == 1 && after.Triggered &&
                row.GetProperty("Notifications").Deserialize<string[]>()!.SequenceEqual(["Triggered:1"]), "Preview flag or shared condition bookkeeping differs.");
        }
        var damage = root.GetProperty("PyreDamageRecords").EnumerateArray().ToArray();
        var lethal = damage.Where(row => row.GetProperty("BeforeFlag").GetInt32() == 0 && row.GetProperty("AfterFlag").GetInt32() == 1).ToArray();
        Require(lethal.Length == 1 && lethal[0].GetProperty("BeforeHP").GetInt32() == 1 && lethal[0].GetProperty("MaxHP").GetInt32() == 81 &&
            lethal[0].GetProperty("AfterHP").GetInt32() == 21 && lethal[0].GetProperty("Damage").GetInt32() >= 1 &&
            lethal[0].GetProperty("Events").Deserialize<string[]>()!.SequenceEqual(["Try:0", "Triggered:1", "Return:1", "HP:1:1", "HP:21:1"]),
            "Actual lethal damage did not retain HP through callbacks then ceil(81*.25) to 21.");
        Require(damage.All(row => row.GetProperty("Completed").GetBoolean()), "Incomplete Pyre damage observation.");
        bool ordinaryDeath = root.GetProperty("Turns").EnumerateArray().Any(row =>
        {
            BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!, after = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            return before.Spawn.Train.Context!.QueryFrame!.PyreResurrectionCount == 0 && after.Spawn.Train.Context!.QueryFrame!.PyreResurrectionCount == 0 &&
                before.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit => unit.Team == CombatTeam.Player && !unit.IsPyre && unit.SpawnerCardId > 0 &&
                    !after.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(other => other.Id == unit.Id));
        });
        Require(ordinaryDeath, "Ordinary friendly death did not leave resurrection available.");
        var actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(actions.Length == 1 && !actions[0].GetProperty("ScenarioAction").GetBoolean(), "Expected one independently selected policy play.");
        Console.WriteLine($"ORIGINAL-BONE-DOG-NATIVE PASS: actual lethal Pyre hit restores 1/81 to21, mark/notification/HP ordering, ordinary death, later-turn consumed control, {damage.Length} Pyre hits and 16 immutable branches.");
    }
    private static void Equal<T>(T expected, T actual, string label)
    { string a = JsonSerializer.Serialize(expected, ModelJson.Options), b = JsonSerializer.Serialize(actual, ModelJson.Options); Require(a == b, label + ": " + ModelJson.Difference(a, b)); }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
