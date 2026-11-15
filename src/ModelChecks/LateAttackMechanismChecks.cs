using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class LateAttackMechanismChecks
{
    private static void Require(bool value, string message) => TrueFinalBossChecks.Require(value, message);
    internal static void Batch(string path)
    {
        using var document = ModelJson.ReadFixture(path); var root = document.RootElement;
        Require(root.GetProperty("Schema").GetInt32() == 1 && root.GetProperty("Scope").GetString() == "DefinitionDrivenLateAttack" &&
            root.GetProperty("GameVersion").GetString() == "2.3.1" && root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d",
            "Wrong late-attack batch version/scope.");
        foreach (var pair in new[] { ("Before", "After"), ("Original", "OriginalAfter"), ("RngBefore", "RngAfter"),
            ("FrameBefore", "FrameAfter"), ("QueueBefore", "QueueAfter") })
            Require(root.GetProperty(pair.Item1).ContentEquals(root.GetProperty(pair.Item2)), "Late-attack batch changed " + pair.Item1);
        Require(root.GetProperty("QueueBefore").GetInt32() == 0 && root.GetProperty("EffectsRestored").GetBoolean(), "Native batch was not restored/settled.");
        var original = root.GetProperty("Original");
        Require(original.GetProperty("Id").GetString() == "8af5b701-3d86-4b69-8117-758e1f4cb16d" &&
            original.GetProperty("Multiplier").GetSingle() == .5f && original.GetProperty("IgnoreChampion").GetBoolean() &&
            original.GetProperty("Team").GetString() == "Monsters" && original.GetProperty("Conditions").GetInt32() == 0, "Original late-attack definition differs.");
        var queries = root.GetProperty("Queries").EnumerateArray().ToArray();
        var products = root.GetProperty("Products").EnumerateArray().ToArray();
        var attacks = root.GetProperty("Attacks").EnumerateArray().ToArray();
        Require(queries.Length == 640 && products.Length == 80 && attacks.Length == 640, "Late-attack calibration matrix is incomplete.");
        var source = root.GetProperty("Before").Deserialize<BattleTurnState>()!;
        CombatUnit Actor(FixtureValue row) => new(123, "generated-control", row.GetProperty("Team").GetString() == "Monsters" ? CombatTeam.Player : CombatTeam.Enemy,
            Math.Max(0, unchecked(row.GetProperty("Raw").GetInt32() + row.GetProperty("Buff").GetInt32())), 100, 100, true,
            row.GetProperty("Pyre").GetBoolean(), false, Array.Empty<CombatStatus>(), Array.Empty<CombatTrigger>(),
            modifiers: new UnitModifiers(row.GetProperty("Raw").GetInt32(), 0, row.GetProperty("Buff").GetInt32(), 1, 0, true, false, Array.Empty<CardUpgradeModifier>(),
                damageBuffFromStatusEffects: row.GetProperty("Status").GetInt32(), isChampion: row.GetProperty("Champion").GetBoolean(),
                statusAttackModifiers: row.GetProperty("Multipliers").EnumerateArray().Select((value, index) =>
                    new RelicStatusAttackModifier(index, "", "Monsters", 0, new RelicLateAttackRule(value.GetSingle(), row.GetProperty("IgnoreChampion").GetBoolean(), true))).ToArray()));
        void Verify()
        {
            foreach (var row in queries)
                Require(RelicLateAttackModel.Query(row.GetProperty("Team").GetString()!, new RelicLateAttackRule(row.GetProperty("Multiplier").GetSingle(),
                    row.GetProperty("IgnoreChampion").GetBoolean(), false), row.GetProperty("Target").GetString()!, row.GetProperty("Champion").GetBoolean()) == row.GetProperty("Actual").GetSingle(),
                    "Native late-attack team/champion query differs.");
            foreach (var row in products)
            {
                var rules = row.GetProperty("Multipliers").EnumerateArray().Select((value, index) => new RelicStatusAttackModifier(index, "", "Monsters", 0,
                    new RelicLateAttackRule(value.GetSingle(), row.GetProperty("IgnoreChampion").GetBoolean(), false))).ToArray();
                float actual = 1f;
                foreach (var rule in rules) actual *= RelicLateAttackModel.Query(rule.Team, rule.LateAttack!, row.GetProperty("Target").GetString()!, row.GetProperty("Champion").GetBoolean());
                Require(BitConverter.SingleToInt32Bits(actual) == BitConverter.SingleToInt32Bits(row.GetProperty("Actual").GetSingle()), "Native ordered late multiplier product differs.");
            }
            foreach (var row in attacks)
            {
                var actor = Actor(row);
                Require(RelicResourceModel.AttackDamage(actor, source.Spawn.Train.Context) == row.GetProperty("Actual").GetInt32(),
                    "Native late attack differs: " + row.GetProperty("Raw").GetInt32() + "/" + row.GetProperty("Team").GetString() + "/pyre=" + actor.IsPyre);
                int without = Math.Max(0, unchecked(actor.Modifiers!.AttackDamage + actor.Modifiers.DamageBuff));
                if (!actor.IsPyre) without = RelicLateAttackModel.Scale(without, RelicLateAttackModel.Multiplier(actor));
                Require(without == row.GetProperty("WithoutStatuses").GetInt32(), "Native attack without statuses differs.");
                if (!actor.IsPyre) Require(actor.Attack == row.GetProperty("Actual").GetInt32(), "Unit attack missed the late multiplier.");
            }
        }
        Verify(); Parallel.For(0, 16, _ => Verify());
        var accepted = new CombatRelicState("new-owner", "new-owner", new[] { RelicLateAttackModel.Effect }, isCovenant: false,
            disallowedInPlacementPhase: false, statusAttackModifiers: new[] { new RelicStatusAttackModifier(0, "", "Monsters", 0, new RelicLateAttackRule(.5f, true, true)) });
        Require(RelicModel.Validate(new[] { accepted }) == null &&
            RelicModel.Validate(new[] { new CombatRelicState("missing", "missing", new[] { RelicLateAttackModel.Effect }) }) != null &&
            RelicModel.Validate(new[] { new CombatRelicState("conditional", "conditional", new[] { RelicLateAttackModel.Effect },
                statusAttackModifiers: new[] { new RelicStatusAttackModifier(0, "", "Monsters", 0, new RelicLateAttackRule(.5f, true, true, true)) }) }) != null,
            "Late-attack structural validation depends on identity or accepts missing/conditional rules.");
        var sampleActor = Actor(attacks[0]);
        var missingChampion = new CombatUnit(1, "missing-metadata", CombatTeam.Player, 7, 100, 100, true, false, false,
            Array.Empty<CombatStatus>(), Array.Empty<CombatTrigger>(), modifiers: new UnitModifiers(7, 0, 0, 1, 0, true, false,
                Array.Empty<CardUpgradeModifier>(), statusAttackModifiers: sampleActor.Modifiers!.StatusAttackModifiers));
        Require(RelicLateAttackModel.Validate(missingChampion) != null && RelicLateAttackModel.Validate(sampleActor) == null,
            "Late attack accepted missing champion metadata.");
        Console.WriteLine($"LATE-ATTACK-BATCH PASS queries={queries.Length} products={products.Length} attacks={attacks.Length} parallel=16 nativeSeconds={root.GetProperty("Seconds").GetDouble():F3} state/definitions/RNG/frame/queue/effects restored.");
    }
    internal static void Battle(string path)
    {
        using var document = ModelJson.ReadFixture(path); var root = document.RootElement;
        Require(root.GetProperty("OriginalLateAttackPrepared").GetBoolean() &&
            root.GetProperty("OriginalLateAttackDefinition").ContentEquals(root.GetProperty("OriginalLateAttackDefinitionAfter")), "Original half-attack definition changed.");
        var states = root.GetProperty("Actions").EnumerateArray().Select(row => row.GetProperty("Actual").Deserialize<BattleTurnState>()!)
            .Concat(root.GetProperty("Turns").EnumerateArray().Select(row => row.GetProperty("Actual").Deserialize<BattleTurnState>()!)).ToArray();
        var actors = states.SelectMany(state => state.Spawn.Train.Rooms).SelectMany(room => room.Units).ToArray();
        Require(actors.Any(unit => unit.AssetKey == "TrainStewardBig" && unit.Modifiers?.AttackDamage == 81 && unit.BaseAttack == 81 && unit.Attack == 41),
            "Missing positive odd-attack native unit.");
        Require(actors.Where(unit => unit.IsPyre).All(unit => unit.Modifiers?.StatusAttackModifiers?.Any(rule => rule.LateAttack?.SkipPyre == true) == true),
            "Pyre exemption was not captured.");
        PercentageStatsMechanismChecks.Battle(path);
        Console.WriteLine("ORIGINAL-LATE-ATTACK PASS unchanged half-attack + double-enemy-health; native successors, immutable branches, complete frontiers and serial/parallel root search.");
    }
}
