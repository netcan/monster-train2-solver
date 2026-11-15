using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class PileSpawnUpgradeMechanismChecks
{
    private static void Require(bool condition, string message) => TrueFinalBossChecks.Require(condition, message);
    internal static void Batch(string path)
    {
        using var document = ModelJson.ReadFixture(path); var root = document.RootElement;
        Require(root.GetProperty("Scope").GetString() == "DefinitionDrivenPileSpawnUpgrade" &&
            root.GetProperty("GameVersion").GetString() == "2.3.1" && root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d", "Wrong native pile upgrade version/scope.");
        foreach (var pair in new[] { ("Before", "After"), ("Original", "OriginalAfter"), ("RngBefore", "RngAfter"), ("FrameBefore", "FrameAfter"), ("QueueBefore", "QueueAfter") })
            Require(root.GetProperty(pair.Item1).ContentEquals(root.GetProperty(pair.Item2)), "Pile upgrade batch changed " + pair.Item1);
        Require(root.GetProperty("QueueBefore").GetInt32() == 0, "Native pile-upgrade queue was not empty.");
        var originals = root.GetProperty("Original").EnumerateArray().ToArray();
        Require(originals.Length == 4 && originals.Select(row => row.GetProperty("Key").GetString()).Order().SequenceEqual(
            new[] { "Echoes_GainStatsPerDeckSize", "LilCapT1", "LilCapT2", "LilCapT3" }) &&
            originals.All(row => row.GetProperty("Conditions").GetInt32() == 0), "Original birth-upgrade denominator changed.");
        var definitions = root.GetProperty("Definitions").EnumerateArray().ToDictionary(row => row.GetProperty("Id").GetString()!, row => row.GetProperty("Rule").Deserialize<RelicPileSpawnUpgradeRule>()!);
        var samples = root.GetProperty("Samples").EnumerateArray().ToArray();
        Require(definitions.Count == 52 && samples.Length == 3744, "Missing native threshold/team/subtype matrix.");
        var context = root.GetProperty("Before").Deserialize<BattleTurnState>()!.Spawn.Train.Context!;
        string parent = JsonSerializer.Serialize(context, ModelJson.Options);
        void Verify()
        {
            foreach (var row in samples)
            {
                var rule = definitions[row.GetProperty("Definition").GetString()!];
                string[] subtypes = row.GetProperty("Subtypes").Deserialize<string[]>()!;
                int actual = RelicPileSpawnUpgradeModel.Applications(row.GetProperty("Count").GetInt32(), rule.Threshold,
                    rule.Team, row.GetProperty("Team").GetString()!, rule.Subtype.Length == 0 || subtypes.Contains(rule.Subtype));
                Require(actual == row.GetProperty("Actual").GetInt32(), "Native per-pile upgrade request count differs.");
            }
            foreach (var row in root.GetProperty("Counts").EnumerateArray())
                Require(RelicPileSpawnUpgradeModel.VisibleCount(context, row.GetProperty("Pile").GetString()!) == row.GetProperty("Actual").GetInt32(),
                    "Native CardPileHelper visible count differs at " + row.GetProperty("Pile").GetString());
        }
        Require(root.GetProperty("Counts").GetArrayLength() == 9, "Missing native pile query branch.");
        Verify(); Parallel.For(0, 16, _ => Verify());
        var arbitrary = new CombatRelicState("arbitrary", "arbitrary", new[] { RelicPileSpawnUpgradeModel.Effect }, isCovenant: false,
            disallowedInPlacementPhase: false, resourceEffects: new[] { new RelicResourceEffect(0, RelicPileSpawnUpgradeModel.Effect, 0, 0, 0, false,
                pileSpawnUpgrade: definitions.Values.First()) });
        Require(RelicModel.Validate(new[] { arbitrary }) == null && RelicModel.Validate(new[] {
            new CombatRelicState("missing", "missing", new[] { RelicPileSpawnUpgradeModel.Effect }) }) != null, "Pile birth upgrade uses an identity allowlist or accepts missing fields.");
        var resource = arbitrary.ResourceEffects![0];
        Require(ReferenceEquals(resource.PileSpawnUpgrade, resource.WithConditions(Array.Empty<RelicConditionState>()).PileSpawnUpgrade) &&
            parent == JsonSerializer.Serialize(context, ModelJson.Options), "Resource copy or parallel parent changed.");
        Console.WriteLine($"PILE-SPAWN-UPGRADE-BATCH PASS originals=4 definitions=52 callbackRequests=3744 nativePileQueries=9 parallel=16 nativeSeconds={root.GetProperty("Seconds").GetDouble():F3}; full state/definitions/RNG/frame/queue unchanged.");
    }
    internal static void Battle(string path)
    {
        using var document = ModelJson.ReadFixture(path); var root = document.RootElement;
        Require(root.GetProperty("PileSpawnUpgradePrepared").GetBoolean() &&
            root.GetProperty("PileSpawnUpgradeDefinition").ContentEquals(root.GetProperty("PileSpawnUpgradeDefinitionAfter")), "Original pile upgrade owner changed.");
        int[] ids = root.GetProperty("PileSpawnUpgradeIds").Deserialize<int[]>()!;
        var actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 3 && actions.Length == 3 && actions.Select(row => row.GetProperty("Action").GetProperty("CardInstanceId").GetInt32()).SequenceEqual(ids), "Missing native repeated birth controls.");
        foreach (var action in actions)
        {
            var before = action.GetProperty("Before").Deserialize<BattleTurnState>()!;
            var after = action.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            var context = before.Spawn.Train.Context!;
            var rule = context.Relics!.Single(owner => owner.AssetKey == "Echoes_GainStatsPerDeckSize").ResourceEffects!.Single().PileSpawnUpgrade!;
            int count = context.Resources!.VisibleDeckCount / rule.Threshold;
            int card = action.GetProperty("Action").GetProperty("CardInstanceId").GetInt32();
            var unit = after.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == card);
            Require(count > 0 && unit.Modifiers!.Upgrades.Count(upgrade => upgrade.DataId == rule.Upgrade.DataId) == count &&
                unit.MaxHealth == 5000 + count * 2 && unit.Modifiers.AttackDamage == 80 + count * 2,
                "Native birth did not preserve individual direct upgrade entries and repeated stats.");
        }
        var parent = actions[0].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var room = parent.Spawn.Train.Rooms.First(value => value.Units.Any(unit => unit.SpawnerCardId == ids[0]));
        var actor = room.Units.Single(unit => unit.SpawnerCardId == ids[0]);
        var original = parent.Spawn.Train.Context!.Relics!.Single(owner => owner.AssetKey == "Echoes_GainStatsPerDeckSize");
        var resource = original.ResourceEffects!.Single();
        string initial = JsonSerializer.Serialize(parent, ModelJson.Options);
        foreach (string control in new[] { "placement", "covenant", "condition", "threshold", "identity" })
        {
            var rule = resource.PileSpawnUpgrade!;
            var conditions = control == "condition" ? new[] { new RelicConditionState(new("AnyCardPlayed"), false, 6, 0, false, triggered: true) } : Array.Empty<RelicConditionState>();
            var effect = new RelicResourceEffect(0, RelicPileSpawnUpgradeModel.Effect, 0, 0, 0, false, conditions: conditions,
                pileSpawnUpgrade: new(rule.Pile, control == "threshold" ? 0 : rule.Threshold, rule.Team, rule.Subtype, rule.Upgrade));
            var owner = new CombatRelicState("arbitrary", "arbitrary", new[] { RelicPileSpawnUpgradeModel.Effect },
                isCovenant: control == "covenant", disallowedInPlacementPhase: control == "placement", resourceEffects: new[] { effect });
            var context = parent.Spawn.Train.Context.WithRelics(new[] { owner });
            var result = RelicSpawnStatusModel.CharacterAdded(room.WithContext(context), actor.Id, ids[0], onlyCovenants: false);
            Require(result.Supported, result.UnsupportedReason ?? "Pile birth gate failed.");
            int expected = control == "identity" ? context.Resources!.VisibleDeckCount / rule.Threshold : 0;
            var changed = result.State!.Units.Single(unit => unit.Id == actor.Id);
            Require(changed.Modifiers!.Upgrades.Count == actor.Modifiers!.Upgrades.Count + expected &&
                changed.MaxHealth == actor.MaxHealth + 2 * expected, "Placement/Covenant/condition/zero-threshold/identity control differs: " + control);
        }
        Require(initial == JsonSerializer.Serialize(parent, ModelJson.Options), "Pile birth gate controls changed their parent.");
        CardCycleChecks.Native(root); SolverStageChecks.Native(path);
        Console.WriteLine("PILE-SPAWN-UPGRADE-BATTLE PASS unchanged original mutator; three native births, individual direct upgrades, full successors/16 branches/frontiers and serial/parallel root search.");
    }
}
