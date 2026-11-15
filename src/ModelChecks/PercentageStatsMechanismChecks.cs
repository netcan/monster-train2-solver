using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using System.Threading.Tasks;
using MonsterTrain2Poju.Model;

internal static class PercentageStatsMechanismChecks
{
    private static void Require(bool value, string message) => TrueFinalBossChecks.Require(value, message);
    internal static void Batch(string path)
    {
        using var document = ModelJson.ReadFixture(path); var root = document.RootElement;
        Require(root.GetProperty("Schema").GetInt32() == 1 && root.GetProperty("Scope").GetString() == "DefinitionDrivenPercentageStats" &&
            root.GetProperty("GameVersion").GetString() == "2.3.1" && root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d",
            "Wrong percentage batch version/scope.");
        foreach (var pair in new[] { ("Before", "After"), ("OriginalDefinitions", "OriginalDefinitionsAfter"),
            ("RngBefore", "RngAfter"), ("FrameBefore", "FrameAfter"), ("QueueBefore", "QueueAfter") })
            Require(root.GetProperty(pair.Item1).ContentEquals(root.GetProperty(pair.Item2)), "Percentage batch changed " + pair.Item1);
        Require(root.GetProperty("QueueBefore").GetInt32() == 0, "Percentage batch was not settled.");
        var originals = root.GetProperty("OriginalDefinitions").EnumerateArray().ToArray();
        var definitions = root.GetProperty("Definitions").EnumerateArray().ToArray();
        Require(originals.Length == 56 && originals.Select(row => row.GetProperty("Id").GetString()).Distinct().Count() == 6 &&
            definitions.Count(row => row.GetProperty("Original").GetBoolean()) == 56 && originals.All(row => row.GetProperty("Conditions").GetInt32() == 0),
            "Incomplete original percentage definition discovery.");
        var rules = definitions.ToDictionary(row => row.GetProperty("Id").GetString()!, row => row.GetProperty("Rule").Deserialize<RelicPercentageStat>()!);
        foreach (var original in originals)
        {
            string id = original.GetProperty("Id").GetString()! + ":" + original.GetProperty("Index").GetInt32();
            Require(original.GetProperty("Rule").ContentEquals(definitions.Single(row => row.GetProperty("Id").GetString() == id).GetProperty("Rule")),
                "Original rule differs from its source definition.");
        }
        Require(rules.Values.Any(rule => rule.RequiredMutatorId != null && rule.RequiredMutatorPresent) &&
            rules.Values.Any(rule => rule.RequiredMutatorId != null && !rule.RequiredMutatorPresent), "Missing active/inactive required-mutator controls.");
        var samples = root.GetProperty("Samples").EnumerateArray().ToArray();
        Require(samples.Length == rules.Count * 7 * 4 * 2, "Percentage matrix cardinality differs.");
        int? Nullable(FixtureValue row, string name) => !row.TryGetProperty(name, out var value) || value.ValueKind == FixtureKind.Null ? null : value.GetInt32();
        void Verify(bool unitIntegration = false)
        {
            foreach (var row in samples)
            {
                string id = row.GetProperty("Definition").GetString()!; var rule = rules[id];
                string team = row.GetProperty("Team").GetString()!;
                bool subtype = rule.SubtypeKey.Length == 0 || row.GetProperty("Subtypes").EnumerateArray().Any(value => value.GetString() == rule.SubtypeKey);
                int attack = row.GetProperty("Attack").GetInt32(), health = row.GetProperty("Health").GetInt32();
                int? query = RelicPercentageStatModel.Query(rule, team, subtype, attack, health);
                Require(Nullable(row, "QueryAttack") == (query.HasValue ? rule.Health ? 0 : query : null) &&
                    Nullable(row, "QueryHealth") == (query.HasValue ? rule.Health ? query : 0 : null), "Native stat query differs: " + id + "/" + team);
                int inputRaw = row.GetProperty("InputRaw").GetInt32(), maximum = row.GetProperty("InputMaximum").GetInt32();
                int hp = row.GetProperty("InputHp").GetInt32(), raw = attack, buff = 0;
                if (RelicPercentageStatModel.BirthApplies(rule, team, subtype))
                {
                    if (rule.Health)
                    {
                        int scaled = RelicPercentageStatModel.Scale(maximum, rule.Multiplier!.Value);
                        buff = unchecked(scaled - maximum); hp = maximum = Math.Min(99999, scaled);
                    }
                    else
                    {
                        int additional = row.GetProperty("Additional").GetInt32(); float late = row.GetProperty("Late").GetSingle();
                        raw = RelicPercentageStatModel.BirthAttack(inputRaw, additional, late, rule.Multiplier!.Value);
                        int before = unchecked((int)Math.Ceiling((float)unchecked(inputRaw + additional) * late));
                        buff = unchecked(raw - before);
                    }
                }
                Require(row.GetProperty("ActualHp").GetInt32() == hp && row.GetProperty("ActualMaximum").GetInt32() == maximum &&
                    row.GetProperty("ActualRaw").GetInt32() == raw && row.GetProperty("LastBuff").GetInt32() == buff,
                    "Native percentage birth differs: " + id + "/" + team + "/" + attack + "/" + health);
                if (unitIntegration && team is "Monsters" or "Heroes" && attack is 7 or 101)
                {
                    var actor = new CombatUnit(1, "generated-control", team == "Monsters" ? CombatTeam.Player : CombatTeam.Enemy,
                        Math.Max(0, attack), row.GetProperty("InputHp").GetInt32(), row.GetProperty("InputMaximum").GetInt32(),
                        true, false, false, Array.Empty<CombatStatus>(), Array.Empty<CombatTrigger>(),
                        subtypes: row.GetProperty("Subtypes").EnumerateArray().Select(value => value.GetString()!).ToArray(),
                        modifiers: new UnitModifiers(attack, 0, 0, 1, 0, true, false, Array.Empty<CardUpgradeModifier>()));
                    var state = new RoomCombatState(0, false, new[] { actor }, Array.Empty<string>());
                    var applied = RelicPercentageStatModel.Apply(state, actor.Id, rule);
                    Require(applied.Supported && applied.State!.Units.Single().Health == hp && applied.State.Units.Single().MaxHealth == maximum &&
                        applied.State.Units.Single().Modifiers!.AttackDamage == raw && actor.Modifiers!.AttackDamage == attack &&
                        actor.Health == row.GetProperty("InputHp").GetInt32(), "Shared immutable unit percentage transition differs: " + id);
                }
            }
        }
        Verify(unitIntegration: true); Parallel.For(0, 16, _ => Verify());
        // Negative controls exercise the public descriptor validation, including
        // orphan/missing fields and an unknown companion on the same owner.
        var ordinary = rules.Values.First(rule => rule.RequiredMutatorId == null && rule.EffectIndex == 0);
        var accepted = new CombatRelicState("new-owner", "new-owner", new[] { RelicPercentageStatModel.Effect },
            isCovenant: false, disallowedInPlacementPhase: false, percentageStats: new[] { ordinary });
        Require(RelicModel.Validate(new[] { accepted }) == null, "Shared percentage rule depends on content identity.");
        var copies = typeof(CombatRelicState).GetMethods(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .Where(method => method.Name.StartsWith("With", StringComparison.Ordinal) && method.ReturnType == typeof(CombatRelicState) &&
                method.GetParameters() is { Length: 1 } parameters && parameters[0].ParameterType.IsGenericType &&
                parameters[0].ParameterType.GetGenericTypeDefinition() == typeof(IReadOnlyList<>)).ToArray();
        Require(copies.Length >= 20, "Relic immutable-copy check did not discover its existing builders.");
        foreach (var method in copies)
        {
            var empty = Array.CreateInstance(method.GetParameters()[0].ParameterType.GetGenericArguments()[0], 0);
            var copy = (CombatRelicState)method.Invoke(accepted, new object[] { empty })!;
            Require(copy.PercentageStats is { Count: 1 } && ReferenceEquals(copy.PercentageStats[0], ordinary),
                "Relic copy dropped percentage birth state: " + method.Name);
        }
        var missingMultiplier = new RelicPercentageStat(0, 100, true, "Heroes", "");
        Require(RelicModel.Validate(new[] { new CombatRelicState("new", "new", new[] { RelicPercentageStatModel.Effect },
            isCovenant: false, disallowedInPlacementPhase: false, percentageStats: new[] { missingMultiplier }) }) != null &&
            RelicModel.Validate(new[] { accepted, new CombatRelicState("late", "late", new[] { "RelicEffectMultiplyAttackLate" },
                isCovenant: false, disallowedInPlacementPhase: false) }) != null,
            "Missing initialized float or unmodeled late provider was accepted.");
        Require(RelicModel.Validate(new[] { new CombatRelicState("new", "new", new[] { RelicPercentageStatModel.Effect }, isCovenant: false) }) != null &&
            RelicModel.Validate(new[] { new CombatRelicState("new", "new", new[] { RelicPercentageStatModel.Effect, "UnknownCompanion" },
                isCovenant: false, disallowedInPlacementPhase: false, percentageStats: new[] { ordinary }) }) != null,
            "Incomplete percentage rule or unknown companion was accepted.");
        Console.WriteLine($"PERCENTAGE-STATS-BATCH PASS: six original owners/56 effects, {rules.Count} configurations, {samples.Length} query/birth pairs, 16 parallel evaluations; state/definitions/RNG/frame/queue unchanged.");
    }
    internal static void Battle(string path)
    {
        using var document = ModelJson.ReadFixture(path); var root = document.RootElement;
        Require(root.GetProperty("OriginalPercentageStatsPrepared").GetBoolean() &&
            root.GetProperty("OriginalPercentageStatsDefinition").ContentEquals(root.GetProperty("OriginalPercentageStatsDefinitionAfter")),
            "Original percentage mutator changed or was not prepared.");
        var definition = root.GetProperty("OriginalPercentageStatsDefinition");
        Require(definition.GetProperty("Id").GetString() == "af00fcb9-d921-4d32-8ab1-2c2ff3c2c39a" &&
            definition.GetProperty("Key").GetString() == "Echoes_DoubleEnemyUnitHealth", "Unexpected original percentage owner.");
        var final = root.GetProperty("Turns").EnumerateArray().Last().GetProperty("ActualOutcome").Deserialize<RoomOutcome>();
        Require(final == (root.GetProperty("NativeWon").GetBoolean() ? RoomOutcome.BattleWon : RoomOutcome.PlayerDefeated), "Native outcome flag differs.");
        var opening = root.GetProperty("Actions")[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var owner = opening.Spawn.Train.Context!.Relics!.Single(relic => relic.DataId == "af00fcb9-d921-4d32-8ab1-2c2ff3c2c39a");
        Require(owner.PercentageStats is { Count: 1 } && owner.PercentageStats[0] is { Health: true, Percent: 100, SourceTeam: "Heroes", Multiplier: 2f },
            "Original double-health effect was not captured as a shared live birth rule.");
        var rawDefinitions = opening.Spawn.Waves.SelectMany(wave => wave.Candidates).SelectMany(group => group.Units).Concat(opening.Spawn.Treasures).ToArray();
        Require(rawDefinitions.All(enemy => enemy.BirthStats == null), "Percentage health was pre-applied to raw wave definitions.");
        var rawHealth = rawDefinitions.GroupBy(enemy => enemy.Unit.AssetKey).ToDictionary(group => group.Key, group => group.First().Unit.MaxHealth);
        var observed = root.GetProperty("Turns").EnumerateArray().Select(turn => turn.GetProperty("Actual").Deserialize<BattleTurnState>()!)
            .SelectMany(state => state.Spawn.Train.Rooms).SelectMany(room => room.Units)
            .Where(unit => unit.Team == CombatTeam.Enemy && rawHealth.ContainsKey(unit.AssetKey)).ToArray();
        Require(observed.Length > 0 && observed.All(unit => unit.MaxHealth == Math.Min(99999, unchecked(rawHealth[unit.AssetKey] * 2))),
            "Missing positive native double-health births or enemy maximum health differs.");
        SolverStageChecks.Native(path, requireVictory: false);
        Console.WriteLine($"ORIGINAL-PERCENTAGE-STATS PASS: unchanged double-health mutator, {observed.Select(unit => unit.Id).Distinct().Count()} positive enemy births, complete native battle, immutable branches/frontiers and serial/parallel root search.");
    }
}
