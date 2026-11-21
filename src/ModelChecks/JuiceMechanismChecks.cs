using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class JuiceMechanismChecks
{
    private static void Require(bool condition, string reason) => TrueFinalBossChecks.Require(condition, reason);
    private static readonly (string Id, string Key, string Type, int Param, int Multiplier)[] Originals = {
        (CardJuiceModel.Upgrade, "TraitJuice", CardJuiceModel.Trait, 2, 2),
        (CardJuiceModel.TripleUpgrade, "TraitJuice3", CardJuiceModel.TripleTrait, 3, 3),
        ("a2fbf5f4-ac4f-41d2-bf04-69b251d7dbe8", "MollisT3_Upgrade", CardJuiceModel.QuadrupleTrait, 0, 4),
        ("cc3e844d-3875-443a-89d8-c752c7289ab2", "MollisT1_Upgrade", CardJuiceModel.TripleTrait, 0, 3),
        ("d6e8adc0-6db7-46ce-9984-e53f113fa5e9", "MollisT2_Upgrade", CardJuiceModel.TripleTrait, 0, 3)
    };
    internal static void Batch(string path)
    {
        using var document = ModelJson.ReadFixture(path); var root = document.RootElement;
        Require(root.GetProperty("Schema").GetInt32() == 1 &&
            root.GetProperty("Scope").GetString() == "DefinitionDrivenJuiceCallbacks" &&
            root.GetProperty("GameVersion").GetString() == "2.3.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d", "Unexpected Juice batch scope/version.");
        Require(root.GetProperty("Before").ContentEquals(root.GetProperty("After")) &&
            root.GetProperty("Definitions").ContentEquals(root.GetProperty("DefinitionsAfter")) &&
            root.GetProperty("RngBefore").ContentEquals(root.GetProperty("RngAfter")) &&
            root.GetProperty("FrameBefore").GetInt32() == root.GetProperty("FrameAfter").GetInt32() &&
            root.GetProperty("QueueBefore").GetInt32() == 0 && root.GetProperty("QueueAfter").GetInt32() == 0,
            "Native Juice query mutated state, definitions, RNG, frame or queue.");
        var definitions = root.GetProperty("Definitions").EnumerateArray().ToArray();
        Require(definitions.Length == Originals.Length, "Missing original Juice upgrade definitions.");
        for (int i = 0; i < definitions.Length; i++)
        {
            var definition = definitions[i]; var expected = Originals[i];
            var upgrade = definition.GetProperty("Upgrade").Deserialize<CardUpgradeModifier>()!;
            Require(definition.GetProperty("Id").GetString() == expected.Id && definition.GetProperty("Key").GetString() == expected.Key &&
                definition.GetProperty("TraitIndex").GetInt32() == 0 &&
                upgrade.Lifecycle!.AddedTraits[0].RuntimeType == expected.Type &&
                upgrade.Lifecycle.AddedTraits[0].DefinitionParamInt == expected.Param &&
                CardJuiceModel.SimpleUpgrade(upgrade) && !CardModifierModel.UpgradeInteractions(upgrade).Any(),
                "Original Juice source/structure was not recognized: " + expected.Key);
            foreach (string control in new[] { "UnknownTrait", "DeclaredType", "UnknownInteraction", "SelfUpgradeCast" })
            {
                var node = JsonSerializer.SerializeToNode(upgrade, ModelJson.Options)!;
                if (control == "UnknownTrait") node["Lifecycle"]!["AddedTraits"]![0]!["RuntimeType"] = "CardTraitFutureJuice";
                if (control == "DeclaredType") node["Lifecycle"]!["AddedTraits"]![0]!["DeclaredName"] = "CardTraitFutureJuice";
                if (control == "UnknownInteraction") node["ExternalInteractions"]!.AsArray().Add("Unrepresented companion effect");
                if (control == "SelfUpgradeCast") node["Lifecycle"]!["AddedTraits"]![0]!["ParameterUpgradeCastEffects"]!.AsArray().Add("CardEffectAddTempCardUpgrade");
                Require(!CardJuiceModel.SimpleUpgrade(node.Deserialize<CardUpgradeModifier>(ModelJson.Options)!),
                    "Unknown companion escaped the shared Juice contract: " + control);
            }
        }
        int[] counts = { int.MinValue, int.MinValue / 2 - 1, -3, -1, 0, 1, 4, int.MaxValue / 4,
            int.MaxValue / 3, int.MaxValue / 2, int.MaxValue / 2 + 1, int.MaxValue };
        string[] statuses = { "valor", "scorch", "unknown-status-control" };
        var samples = root.GetProperty("Samples").EnumerateArray().ToArray();
        Require(samples.Length == Originals.Length * counts.Length * statuses.Length, "Juice matrix is incomplete.");
        void Verify()
        {
            int index = 0;
            foreach (var expected in Originals)
            foreach (int count in counts)
            foreach (string status in statuses)
            {
                var sample = samples[index++];
                Require(sample.GetProperty("Id").GetString() == expected.Id && sample.GetProperty("TraitIndex").GetInt32() == 0 &&
                    sample.GetProperty("Count").GetInt32() == count && sample.GetProperty("Status").GetString() == status &&
                    sample.GetProperty("Multiplier").GetInt32() == CardJuiceModel.Multiplier(expected.Type) &&
                    sample.GetProperty("Bonus").GetInt32() == CardJuiceModel.StatusBonus(count, expected.Multiplier) &&
                    sample.GetProperty("UpgradeStacks").GetInt32() == CardJuiceModel.UpgradeStacks(count, expected.Multiplier),
                    $"Native Juice callback differs: {expected.Key}/{count}/{status}.");
            }
        }
        Verify(); Parallel.For(0, 16, _ => Verify());
        Require(samples.Any(sample => sample.GetProperty("Count").GetInt32() < 0 && sample.GetProperty("UpgradeStacks").GetInt32() > 0),
            "Missing overflow-before-clamp control.");
        Console.WriteLine($"JUICE-MECHANISM-BATCH PASS: {definitions.Length} original upgrades, {samples.Length} native callback pairs, 16 parallel evaluations; state/definitions/RNG/frame/queue unchanged. Self-upgrade callback dispatch is outside this component scope.");
    }
    internal static void Battle(string path)
    {
        using var document = ModelJson.ReadFixture(path); var root = document.RootElement;
        Require(root.GetProperty("OriginalJuiceUpgradesPrepared").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            root.GetProperty("OriginalJuiceUpgradesDefinitions").ContentEquals(root.GetProperty("OriginalJuiceUpgradesDefinitionsAfter")),
            "Incomplete Juice battle or changed original spell/upgrade definitions.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero native " + key);
        var terminal = root.GetProperty("Turns").EnumerateArray().Last().GetProperty("ActualOutcome").Deserialize<RoomOutcome>();
        Require(terminal == (root.GetProperty("NativeWon").GetBoolean() ? RoomOutcome.BattleWon : RoomOutcome.PlayerDefeated),
            "Native terminal outcome differs from its win/loss flag.");
        var ids = root.GetProperty("OriginalJuiceUpgradesIds").Deserialize<int[]>()!;
        var rows = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 12 && rows.Length == 12 && rows.Select(row => row.GetProperty("Action").GetProperty("CardInstanceId").GetInt32()).SequenceEqual(ids),
            "Missing original soul-upgrade spell plays.");
        for (int i = 3; i < rows.Length; i++)
        {
            var before = rows[i].GetProperty("Before").Deserialize<BattleTurnState>()!;
            var after = rows[i].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            var play = rows[i].GetProperty("Action").Deserialize<PlayCardAction>()!;
            var card = before.Spawn.Train.Context!.FindCard(ids[i])!;
            var target = before.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == play.TargetUnitId);
            var changed = after.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == play.TargetUnitId);
            int rank = (i - 3) / 3, multiplier = rank == 2 ? 4 : 3;
            Require(CardJuiceModel.Validate(card) == null && CardJuiceModel.SimpleUpgrade(card.Permanent.Upgrades.Single()),
                "Original spell failed shared structure-based validation.");
            int Delta(string status) => (changed.Status(status)?.Stacks ?? 0) - (target.Status(status)?.Stacks ?? 0);
            if ((i - 3) % 3 == 0)
                Require(Delta("lifesteal") == 4 * multiplier && Delta("scorch") == 2 * multiplier, "Native friendly source status multiplier differs.");
            else Require(Delta("buff") == 12 * multiplier, "Native Ritual status multiplier differs.");
            Require(Delta(target.Team == CombatTeam.Player ? "armor" : "poison") == (rank == 0 ? 0 : 3 * multiplier),
                "Native OnCast team filter or source Juice multiplier differs.");
            // Content IDs carry provenance; support is derived from the captured
            // mechanism shape, including for another otherwise identical owner.
            var json = JsonSerializer.SerializeToNode(card, ModelJson.Options)!;
            json["DataId"] = "structurally-equivalent-status-owner";
            json["MaskDescriptor"]!["Definition"]!["DataId"] = "structurally-equivalent-status-owner";
            var equivalent = json.Deserialize<CardInstanceState>(ModelJson.Options)!;
            Require(CardJuiceModel.Validate(equivalent) == null && CardJuiceModel.Apply(equivalent, 4) == 4 * multiplier,
                "Shared mechanism still depends on a card ID whitelist.");
            json["MaskDescriptor"]!["Definition"]!["MainEffectNames"]![0] = "CardEffectAddTempCardUpgrade";
            Require(CardJuiceModel.Validate(json.Deserialize<CardInstanceState>(ModelJson.Options)!) != null,
                "Self-upgrade callback escaped the component boundary.");
        }
        SolverStageChecks.Native(path, requireVictory: false);
        Console.WriteLine("ORIGINAL-JUICE-UPGRADES PASS: three original soul upgrades, nine unchanged status-spell plays, friendly/enemy OnCast armor/poison, ID-independent structural reuse; complete native chain/candidates and serial/parallel root search. Actual native won=" + root.GetProperty("NativeWon").GetBoolean());
    }

    internal static void MollisCycleOfLife(string path)
    {
        using var document = ModelJson.ReadFixture(path);
        FixtureValue battle = document.RootElement;
        FixtureValue definition = battle.GetProperty("OriginalMollisCycleOfLifeDefinition");
        Require(battle.GetProperty("OriginalMollisCycleOfLifePrepared").GetBoolean() &&
            definition.ContentEquals(battle.GetProperty("OriginalMollisCycleOfLifeDefinitionAfter")) &&
            definition.GetProperty("SoulKey").GetString() == "MollisT3" &&
            definition.GetProperty("CardKey").GetString() == "CycleOfLife",
            "Missing unmodified original Mollis and Cycle of Life.");

        int cardId = battle.GetProperty("OriginalMollisCycleOfLifeCardId").GetInt32();
        var installed = battle.GetProperty("OriginalMollisCycleOfLifeInstalledUpgrade").Deserialize<CardLifecycleUpgrade>()!;
        Require(installed.AvoidClobbering && installed.TraitsModified.SequenceEqual(new[] { CardJuiceModel.QuadrupleTrait }),
            "Missing the original soul attachment's recorded trait installation.");
        FixtureValue action = battle.GetProperty("Actions").EnumerateArray().Single(row =>
            row.GetProperty("ScenarioAction").GetBoolean() && row.GetProperty("Action").GetProperty("CardInstanceId").GetInt32() == cardId);
        var input = action.GetProperty("Action").Deserialize<PlayCardAction>()!;
        var before = action.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var after = action.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var card = before.Spawn.Train.Context!.FindCard(cardId)!;
        var upgrade = card.Permanent.Upgrades.Single(item => item.DataId == installed.Values.DataId);
        Require(card.DataId == definition.GetProperty("CardId").GetString() && CardJuiceModel.Validate(card) == null &&
            CardJuiceModel.SimpleUpgrade(upgrade), "Original cast did not use the verified shared Juice path.");
        var target = before.Spawn.Train.Rooms.Single(room => room.RoomIndex == input.RoomIndex)
            .Units.Single(unit => unit.Id == input.TargetUnitId);
        var changed = after.Spawn.Train.Rooms.Single(room => room.RoomIndex == input.RoomIndex)
            .Units.Single(unit => unit.Id == input.TargetUnitId);
        int Delta(string id) => (changed.Status(id)?.Stacks ?? 0) - (target.Status(id)?.Stacks ?? 0);
        Require(target.Team == CombatTeam.Player && changed.Health - target.Health == 10 &&
            changed.MaxHealth - target.MaxHealth == 10 && Delta("spikes") == 32 && Delta("armor") == 12,
            "Native unit upgrade, direct status or OnCast status differs.");

        var rejected = JsonSerializer.SerializeToNode(card, ModelJson.Options)!;
        rejected["MaskDescriptor"]!["Definition"]!["MainEffectNames"]![1] = "CardEffectAddTempCardUpgrade";
        Require(CardJuiceModel.Validate(rejected.Deserialize<CardInstanceState>(ModelJson.Options)!) != null,
            "An unverified card-upgrade callback escaped Juice validation.");
        SolverStageChecks.Native(path, requireVictory: false);
        Console.WriteLine("ORIGINAL-MOLLIS-CYCLE-OF-LIFE PASS: native soul attachment, +10 HP, +32 Spikes, +12 Armor, complete replay and search.");
    }
}
