using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;
using MonsterTrain2Poju.NativeCases;

internal static class RoomHealthMechanismChecks
{
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private sealed class Sample
    {
        public RoomHealthMechanismCase Input { get; }
        public int Dynamic { get; }
        public int Attack { get; }
        public int MagicPower { get; }
        public Sample(RoomHealthMechanismCase input, int @dynamic, int attack, int magicPower)
        { Input = input; Dynamic = @dynamic; Attack = attack; MagicPower = magicPower; }
    }
    internal static void Native(string path, string? baselinePath = null)
    {
        using var document = ModelJson.ReadFixture(path); FixtureValue root = document.RootElement;
        Require(root.GetProperty("Schema").GetInt32() == 1 && root.GetProperty("Scope").GetString() == "DefinitionDrivenRoomHealthQueries" &&
            root.GetProperty("GameVersion").GetString() == "2.3.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d", "Unexpected generated query scope/version.");
        Require(root.GetProperty("DecisionUnchanged").GetBoolean() && root.GetProperty("DefinitionsUnchanged").GetBoolean() &&
            root.GetProperty("Before").ContentEquals(root.GetProperty("After")) &&
            root.GetProperty("OriginalDefinitions").ContentEquals(root.GetProperty("OriginalDefinitionsAfter")) &&
            root.GetProperty("FrameBefore").GetInt32() == root.GetProperty("FrameAfter").GetInt32() &&
            root.GetProperty("RngBefore").ContentEquals(root.GetProperty("RngAfter")) &&
            root.GetProperty("QueueBefore").GetInt32() == 0 && root.GetProperty("QueueAfter").GetInt32() == 0,
            "Native generator changed decision, source definitions, RNG, frame or queue.");
        var definitions = root.GetProperty("Definitions").Deserialize<RoomHealthMechanismDefinition[]>()!;
        var originals = definitions.Where(item => item.Collection != "Control").ToArray();
        Require(originals.Length > 0 && originals.Select(item => item.NativeType).Distinct().Count() == 2 &&
            definitions.All(item => RoomHealthDamageModel.Known(item.NativeType)), "Missing native mechanism families.");
        var authored = root.GetProperty("OriginalDefinitions").EnumerateArray().ToArray();
        Require(authored.Length == originals.Length, "Missing unchanged authored definitions.");
        foreach (var item in authored)
        {
            var definition = item.GetProperty("Definition").Deserialize<RoomHealthMechanismDefinition>()!;
            var native = item.GetProperty("Native");
            Require(definition.Collection is "Card" or "Upgrade" && definition.SourceId.Length > 0 &&
                native.GetProperty("Type").GetString() == definition.NativeType &&
                native.GetProperty("Int").GetInt32() == definition.Coefficient &&
                native.GetProperty("Int2").GetInt32() == definition.Divisor, "Authored identity/parameters differ from native input.");
        }
        Require(originals.Where(item => item.AssetKey is "Punished1Path1a" or "Punished1Path1b" or "Punished1Path1c")
            .OrderBy(item => item.AssetKey, StringComparer.Ordinal).Select(item => (item.NativeType, item.Coefficient, item.Divisor))
            .SequenceEqual(new[] { (RoomHealthDamageModel.FriendlyOwn, 1, 1), (RoomHealthDamageModel.FriendlyOwn, 2, 1),
                (RoomHealthDamageModel.FriendlyOwn, 3, 1) }), "Original champion upgrade parameters were not discovered.");
        var discovered = authored.Select(item => item.GetProperty("Definition").Deserialize<RoomHealthMechanismDefinition>()!).ToArray();
        Require(JsonSerializer.Serialize(definitions, ModelJson.Options) ==
            JsonSerializer.Serialize(RoomHealthMechanismCases.Definitions(discovered), ModelJson.Options), "Generated definition plan differs from native source identities/parameters.");
        var samples = root.GetProperty("Samples").Deserialize<Sample[]>()!;
        var expectedInputs = RoomHealthMechanismCases.Generate(definitions.Length).ToArray();
        Require(samples.Length == definitions.Length * 720 && samples.Length == expectedInputs.Length, "Generated matrix is incomplete.");
        for (int i = 0; i < samples.Length; i++)
        {
            var a = samples[i].Input; var b = expectedInputs[i];
            Require((a.DefinitionIndex, a.MaximumHealth, a.Health, a.Team, a.Placement, a.Attached, a.RequestStats, a.DamageType) ==
                (b.DefinitionIndex, b.MaximumHealth, b.Health, b.Team, b.Placement, b.Attached, b.RequestStats, b.DamageType),
                "Missing, duplicated or reordered generated native query input.");
        }
        string parent = JsonSerializer.Serialize(definitions, ModelJson.Options);
        void Verify()
        {
            foreach (Sample sample in samples)
            {
                var input = sample.Input; var rule = definitions[input.DefinitionIndex];
                int Query(bool stats) => RoomHealthDamageModel.Query(rule.NativeType, rule.Coefficient, rule.Divisor,
                    input.MaximumHealth, input.Health, input.Team == "Player", input.Placement != 0,
                    input.Placement == 1, input.Attached, stats);
                Require(sample.Dynamic == Query(true) && sample.Attack == Query(input.RequestStats) && sample.MagicPower == 0,
                    $"Native room-health query differs: {rule.NativeType}/{input.DefinitionIndex}, hp={input.Health}/{input.MaximumHealth}, {input.Team}, placement={input.Placement}, attached={input.Attached}, stats={input.RequestStats}.");
            }
        }
        Verify(); Parallel.For(0, 16, _ => Verify());
        Require(JsonSerializer.Serialize(definitions, ModelJson.Options) == parent, "Parallel query evaluation changed its definitions.");
        Require(samples.Any(sample => sample.Dynamic != 0 && !sample.Input.Attached &&
            definitions[sample.Input.DefinitionIndex].NativeType == RoomHealthDamageModel.FriendlyOwn) &&
            samples.Any(sample => sample.Dynamic != 0 && sample.Attack == 0 && !sample.Input.RequestStats),
            "Missing positive non-owner or non-stats controls.");
        if (baselinePath != null)
        {
            using var baseline = ModelJson.ReadFixture(baselinePath); var battle = baseline.RootElement;
            Require(battle.GetProperty("GameModuleMvid").ContentEquals(root.GetProperty("GameModuleMvid")) &&
                battle.GetProperty("NativeWon").GetBoolean() && battle.GetProperty("TerminalEffectsSettled").GetBoolean(),
                "Source native baseline is incomplete or from a different module.");
            foreach (string counter in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
                Require(battle.GetProperty(counter).GetInt32() == 0, "Nonzero source baseline " + counter);
            RecordedBattleChecks.Native(battle);
        }
        Console.WriteLine($"ROOM-HEALTH-MECHANISM-BATCH PASS: {originals.Length} discovered original definitions, {definitions.Length} original/control configurations, {samples.Length} native queries, 16 immutable parallel evaluations; complete state/RNG/frame/queue/source restoration. Query component only; friendly-aura lifecycle and champion battle integration remain guarded.");
    }
}
