using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class PyreDamageTakenMechanismChecks
{
    private static void Require(bool passed, string message) => TrueFinalBossChecks.Require(passed, message);
    private static CombatRelicState Owner(int[] extras, bool disallowed = false, RelicConditionState[]? conditions = null) =>
        new("arbitrary-owner", "arbitrary-owner", extras.Select(_ => RelicResourceModel.ExtraPyreDamage).ToArray(),
            isCovenant: false, disallowedInPlacementPhase: disallowed,
            resourceEffects: extras.Select((extra, index) => new RelicResourceEffect(index, RelicResourceModel.ExtraPyreDamage,
                extra, 0, extra, true, conditions: conditions)).ToArray());
    internal static void Batch(string path)
    {
        using var document = ModelJson.ReadFixture(path); var root = document.RootElement;
        Require(root.GetProperty("Scope").GetString() == "DefinitionDrivenPyreDamageTaken" &&
            root.GetProperty("GameVersion").GetString() == "2.3.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d", "Wrong native Pyre damage batch version/scope.");
        foreach (var pair in new[] { ("Before", "After"), ("Original", "OriginalAfter"), ("RngBefore", "RngAfter"),
            ("FrameBefore", "FrameAfter"), ("QueueBefore", "QueueAfter") })
            Require(root.GetProperty(pair.Item1).ContentEquals(root.GetProperty(pair.Item2)), "Pyre damage batch changed " + pair.Item1);
        Require(root.GetProperty("QueueBefore").GetInt32() == 0 && root.GetProperty("EffectsRestored").GetBoolean(), "Batch did not restore native manager.");
        var definitions = root.GetProperty("Original").EnumerateArray().ToArray();
        Require(definitions.Length == 4 && definitions.Select(row => row.GetProperty("Key").GetString()).Order().SequenceEqual(
            new[] { "DaemosT1", "DaemosT2", "DaemosT3", "Echoes_PyreTakesMoreDamage" }) &&
            definitions.All(row => row.GetProperty("Conditions").GetInt32() == 0), "Original extra Pyre damage sources changed.");
        var samples = root.GetProperty("Samples").EnumerateArray().ToArray();
        var products = root.GetProperty("Products").EnumerateArray().ToArray();
        Require(samples.Length == 40 && products.Length == 650, "Incomplete native Pyre damage matrix.");
        var context = root.GetProperty("Before").Deserialize<BattleTurnState>()!.Spawn.Train.Context!;
        string parent = JsonSerializer.Serialize(context, ModelJson.Options);
        void Verify()
        {
            foreach (var row in samples)
            {
                var definition = definitions.Single(source => source.GetProperty("Id").GetString() == row.GetProperty("OriginalId").GetString());
                Require(definition.GetProperty("Extra").GetInt32() == row.GetProperty("Extra").GetInt32() &&
                    RelicResourceModel.ExtraPyreDamageStep(row.GetProperty("Damage").GetInt32(), row.GetProperty("Extra").GetInt32()) == row.GetProperty("Actual").GetInt32(),
                    "Original extra Pyre damage callback differs.");
            }
            foreach (var row in products)
            {
                int[] extras = row.GetProperty("Extras").Deserialize<int[]>()!;
                var source = context.WithRelics(extras.Length == 0 ? Array.Empty<CombatRelicState>() : new[] { Owner(extras) });
                var result = RelicResourceModel.ModifyPyreDamageTaken(source, row.GetProperty("Damage").GetInt32());
                Require(result.Supported && result.Value == row.GetProperty("Actual").GetInt32(), "Native ordered Pyre damage manager differs.");
            }
        }
        Verify(); Parallel.For(0, 16, _ => Verify());
        var once = new RelicConditionState(new("AnyCardPlayed"), true, 6, 1, false);
        var gated = context.WithRelics(new[] { Owner(new[] { -10 }, true, new[] { once }) });
        var skipped = RelicResourceModel.ModifyPyreDamageTaken(gated, 3, placement: true);
        var zero = RelicResourceModel.ModifyPyreDamageTaken(gated, 0);
        var preview = RelicResourceModel.ModifyPyreDamageTaken(gated, 3, preview: true);
        var fired = RelicResourceModel.ModifyPyreDamageTaken(gated, 3);
        Require(skipped.Supported && skipped.Value == 3 && skipped.Events.Count == 0 && zero.Value == 0 && !zero.Events.Single().Notified &&
            preview.Value == -7 && preview.Events.Single().Notified && preview.Context!.Relics![0].ResourceEffects![0].Conditions[0].Triggered &&
            fired.Value == -7 && fired.Events.Single().Notified && fired.Context!.Relics![0].ResourceEffects![0].Conditions[0].Triggered &&
            RelicResourceModel.ModifyPyreDamageTaken(fired.Context, 3).Value == 3,
            "Placement, preview, zero-damage or notification/condition lifetime differs.");
        var missing = new CombatRelicState("missing", "missing", new[] { RelicResourceModel.ExtraPyreDamage });
        Require(RelicModel.Validate(new[] { missing }) != null && RelicModel.Validate(new[] { Owner(new[] { 3 }) }) == null,
            "Extra Pyre damage accepts missing metadata or depends on owner identity.");
        Require(parent == JsonSerializer.Serialize(context, ModelJson.Options), "Parallel Pyre damage queries changed their parent.");
        Console.WriteLine($"PYRE-DAMAGE-TAKEN-BATCH PASS originalDefinitions=4 callbacks=40 orderedManagerCases=650 parallel=16 nativeSeconds={root.GetProperty("Seconds").GetDouble():F3}; state/definitions/RNG/frame/queue restored.");
    }
    internal static void Battle(string path)
    {
        using var document = ModelJson.ReadFixture(path); var root = document.RootElement;
        Require(root.GetProperty("PyreDamageTakenPrepared").GetBoolean() &&
            root.GetProperty("PyreDamageTakenDefinition").ContentEquals(root.GetProperty("PyreDamageTakenDefinitionAfter")), "Original Pyre damage owner changed.");
        int[] ids = root.GetProperty("PyreDamageTakenIds").Deserialize<int[]>()!;
        var actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 7 && actions.Length == 7 && actions.Select(row => row.GetProperty("Action").GetProperty("CardInstanceId").GetInt32()).SequenceEqual(ids),
            "Missing ordered native defense controls.");
        CombatUnit Pyre(BattleTurnState state) => state.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre);
        int initial = Pyre(actions[0].GetProperty("Before").Deserialize<BattleTurnState>()!).Health;
        int[] lost = { 0, 4, 4, 4, 10, 10, 10 };
        for (int i = 0; i < actions.Length; i++)
        {
            var actual = actions[i].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            Require(Pyre(actual).Health == initial - lost[i], "Native zero/positive/armor/shield ordering differs at control " + i);
            var owner = actual.Spawn.Train.Context!.Relics!.Single(relic => relic.AssetKey == "Echoes_PyreTakesMoreDamage");
            Require(owner.ResourceEffects!.Single(rule => rule.Type == RelicResourceModel.ExtraPyreDamage).Amount == 3,
                "Original extra Pyre damage runtime amount was not captured.");
        }
        Require(root.GetProperty("Turns").GetArrayLength() >= 3 && root.GetProperty("Turns").EnumerateArray().Any(row =>
            Pyre(row.GetProperty("Actual").Deserialize<BattleTurnState>()!).Health < initial - 10), "Missing natural enemy Pyre attacks.");
        CardCycleChecks.Native(root);
        SolverStageChecks.Native(path, requireVictory: false);
        Console.WriteLine("PYRE-DAMAGE-TAKEN-BATTLE PASS unchanged original mutator; zero/positive/full and partial armor/full shield spell controls, natural attacks, complete successors/16 branches/frontiers and serial/parallel root search.");
    }
}
