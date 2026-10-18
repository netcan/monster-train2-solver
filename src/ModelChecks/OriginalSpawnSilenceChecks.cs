using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalSpawnSilenceChecks
{
    private static CombatRelicState Owner(int innerChance = 100, int outerChance = 100,
        string id = RelicSpawnSilenceModel.DataId, string[]? exclusions = null) => new(id, RelicSpawnSilenceModel.AssetKey,
        [RelicSpawnSilenceModel.Effect], isCovenant: false, disallowedInPlacementPhase: false,
        afterSpawnChanceRules: [new(0, outerChance, 0, "Heroes", true, true, "SubtypesData_None",
            exclusions ?? [RelicSpawnSilenceModel.ExcludedSubtype], 0, true, 1, true, [], "CardEffectAddStatusEffect", "Self", "Heroes",
            "Spell", 0f, innerChance, 0, false, false, false, [RelicSpawnSilenceModel.Muted(1)])]);

    internal static void Run()
    {
        UnityRng rng = UnityRng.Seed(631);
        CombatUnit Unit(CombatTeam team = CombatTeam.Enemy, CombatStatus[]? statuses = null, string[]? subtypes = null,
            string[]? immunities = null) => new(1, "carrier", team, 0, 20, 20, false, false, false, statuses ?? [], [],
                subtypes: subtypes ?? [], statusImmunities: immunities ?? [], statusRegistry: statuses ?? [],
                statusDictionary: new([], []));
        RoomCombatState Room(CombatUnit unit, bool preview = false, bool isolated = false) => new(0, false, [unit], [],
            new(new([], [], [], rng, 0, []), rng, 0, 1, 10, relics: [Owner()], purifyBlockedTriggers: [], isolatedBattlePreview: isolated), preview);
        RoomCombatResult Fire(RoomCombatState room, string trigger = "OnSpawn") => RoomCombatModel.ApplyQueuedCharacterTrigger(room,
            new(0, room.Units.Single(), trigger), _ => { });
        RoomCombatState source = Room(Unit());
        string parent = JsonSerializer.Serialize(source, ModelJson.Options);
        RoomCombatResult applied = Fire(source);
        Require(applied.Supported && applied.State!.Units.Single().Status("muted")?.Stacks == 1 &&
            applied.State.Context!.BattleRng.Equals(rng.Range(0, 100).State), "Spawn silence must add muted and consume one inner chance draw.");
        RoomCombatResult repeated = Fire(applied.State!);
        Require(repeated.Supported && repeated.State!.Units.Single().Status("muted")?.Stacks == 2 &&
            repeated.State.Context!.BattleRng.Equals(rng.Range(0, 100).State.Range(0, 100).State),
            "Existing muted and a prior same-room application must not suppress another spawn callback.");
        foreach (var control in new[] { (Room(Unit()), "AfterSpawnEnchant"), (Room(Unit(CombatTeam.Player)), "OnSpawn"),
            (Room(Unit(statuses: [new("untouchable", 1)])), "OnSpawn"),
            (Room(Unit(subtypes: [RelicSpawnSilenceModel.ExcludedSubtype])), "OnSpawn") })
        {
            RoomCombatResult skipped = Fire(control.Item1, control.Item2);
            Require(skipped.Supported && skipped.State!.Context!.BattleRng.Equals(rng) &&
                skipped.State.Units.Single().Status("muted") == null, "A gated spawn callback changed status or RNG.");
        }
        Require(!RelicSpawnSilenceModel.Eligible(Unit().WithDeathState(new(false, false, false, isDestroyed: true)), "OnSpawn") &&
            RelicSpawnSilenceModel.Eligible(Unit().WithHealth(0), "OnSpawn"), "Native Test excludes destroyed actors but has no HP gate.");
        foreach (CombatUnit actor in new[] { Unit(immunities: ["muted"]), Unit(statuses: [new("purify", 1)]) })
        {
            RoomCombatResult immune = Fire(Room(actor));
            Require(immune.Supported && immune.State!.Context!.BattleRng.Equals(rng.Range(0, 100).State) &&
                immune.State.Units.Single().Status("muted") == null, "Inner chance draw must precede status immunity/purify.");
        }
        Require(!RelicSpawnSilenceModel.ApplyOne(Room(Unit(), true), 1, Owner()).Supported &&
            !RelicSpawnSilenceModel.ApplyOne(Room(Unit().WithHealth(0)), 1, Owner()).Supported,
            "Uncalibrated standalone preview and dead-self application must stay explicit boundaries.");
        RoomCombatResult preview = Fire(Room(Unit(), true, true));
        Require(preview.Supported && preview.State!.Units.Single().Status("muted")?.Stacks == 1 &&
            preview.State.Context!.BattleRng.Equals(rng.Range(0, 100).State), "Isolated preview did not use its private RNG stream.");
        foreach (CombatRelicState changed in new[] { Owner(innerChance: 0), Owner(outerChance: 50), Owner(id: "changed"), Owner(exclusions: []) })
            Require(RelicModel.Validate([changed]) != null, "Changed chance, owner or excluded subtype must be rejected.");
        Parallel.For(0, 16, _ => Require(Fire(source).State!.Context!.BattleRng.Equals(rng.Range(0, 100).State), "Parallel spawn silence diverged."));
        Require(JsonSerializer.Serialize(source, ModelJson.Options) == parent, "Spawn silence mutated its parent.");
        Console.WriteLine("SPAWN-SILENCE-PURE PASS: outer/inner chance distinction, team/trigger/untouchable/collector/destroyed gates, immunity/purify ordering, isolated preview, strict payload and immutable branches.");
    }

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 227 && root.GetProperty("OriginalSpawnSilenceSchema").GetInt32() == 227 &&
            root.GetProperty("ModifierScenario").GetString() == "original-chance-to-silence" && root.GetProperty("OriginalSpawnSilencePrepared").GetBoolean() &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending", "OriginalSpawnSilenceCaptureFailures",
                "OriginalSpawnSilenceMismatches", "OriginalSpawnSilenceUnsupported", "OriginalSpawnSilencePending" }
                .All(name => root.GetProperty(name).GetInt32() == 0), "Native spawn-silence acceptance failed.");
        int[] cards = root.GetProperty("OriginalSpawnSilence").GetProperty("CardIds").EnumerateArray().Select(item => item.GetInt32()).ToArray();
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(cards.Length == 2 && actions.Length == 2 && actions.All(row => row.GetProperty("ScenarioAction").GetBoolean()) &&
            actions.Select(row => row.GetProperty("Action").GetProperty("CardInstanceId").GetInt32()).SequenceEqual(cards), "Missing complete deployment.");
        var positiveActors = new List<int>(); int gated = 0, friendly = 0;
        foreach (FixtureValue row in root.GetProperty("RelicSpawnSilenceTests").EnumerateArray())
        {
            CombatUnit actor = row.GetProperty("Actor").Deserialize<CombatUnit>()!;
            string trigger = row.GetProperty("Trigger").GetString()!;
            bool result = row.GetProperty("Result").GetBoolean();
            Require(result == RelicSpawnSilenceModel.Eligible(actor, trigger) &&
                row.GetProperty("RoomTriggers").EnumerateArray().All(item => !item.GetBoolean()), "Spawn Test or unrestricted room flags differ.");
            Equal(row.GetProperty("Before").Deserialize<TrainCombatState>(), row.GetProperty("Actual").Deserialize<TrainCombatState>(), "Test state/RNG");
            if (result) positiveActors.Add(actor.Id); else { gated++; if (trigger == "OnSpawn" && actor.Team == CombatTeam.Player) friendly++; }
        }
        var appliedActors = new List<int>();
        foreach (FixtureValue row in root.GetProperty("RelicSpawnSilenceApplications").EnumerateArray())
        {
            CombatUnit actor = row.GetProperty("Actor").Deserialize<CombatUnit>()!;
            TrainCombatState before = row.GetProperty("Before").Deserialize<TrainCombatState>()!;
            TrainCombatState actual = row.GetProperty("Actual").Deserialize<TrainCombatState>()!;
            Require(row.GetProperty("Completed").GetBoolean() && RelicSpawnSilenceModel.Validate(before.Context!.Relics!
                .Single(item => item.DataId == RelicSpawnSilenceModel.DataId)) == null, "Incomplete effect or changed original owner.");
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            void Replay()
            {
                TrainCombatResult predicted = RelicSpawnSilenceModel.ApplySelected(before, actor.Id);
                Require(predicted.Supported, "Spawn silence unsupported: " + predicted.UnsupportedReason);
                Equal(predicted.State, actual, "status/callbacks/RNG");
            }
            Replay(); Parallel.For(0, 16, _ => Replay());
            CombatUnit changed = actual.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == actor.Id);
            Require(changed.Status("muted")?.Stacks == 1 && actual.Context!.BattleRng.Equals(before.Context!.BattleRng.Range(0, 100).State),
                "Native application must retain one muted stack and consume exactly one Battle RNG draw.");
            Require(JsonSerializer.Serialize(before, ModelJson.Options) == parent, "Spawn silence mutated its parent.");
            appliedActors.Add(actor.Id);
        }
        Require(appliedActors.Count >= 2 && appliedActors.Distinct().Count() >= 2 && appliedActors.SequenceEqual(positiveActors) &&
            gated > appliedActors.Count && friendly >= 2, "Missing natural repeated spawns, gated callbacks or friendly spawn controls.");
        Console.WriteLine($"ORIGINAL-SPAWN-SILENCE-NATIVE PASS: {appliedActors.Count} natural enemy spawns, {gated} gated Tests, unchanged collector exclusion, muted state, one inner RNG draw and immutable parallel replay.");
    }
    private static void Equal<T>(T predicted, T actual, string label)
    {
        string a = JsonSerializer.Serialize(predicted, ModelJson.Options), b = JsonSerializer.Serialize(actual, ModelJson.Options);
        Require(a == b, "Spawn silence " + label + " differs: " + ModelJson.Difference(a, b));
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
