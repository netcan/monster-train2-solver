using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalMagnitudeRelicBatchChecks
{
    private static readonly (string Key, string Id, string Status)[] Artifacts =
    [
        ("IncreaseRageDamage", "b9bddf11-acaa-40fb-bc40-eee83e478424", "buff"),
        ("StrongerSpikes", "aa94ad34-ee09-464e-89ff-2a2c2867c097", "spikes"),
        ("AwokenRegen", "1fd244de-7d7b-4345-974b-4e5bf287a3df", "regen")
    ];

    // The ordinary fixture runner already checks the complete policy chain.
    // The dedicated strict entry can request the same checks without duplicating
    // them during a full fixture regression.
    internal static IReadOnlyDictionary<string, int>? Native(FixtureValue root, bool required = false,
        bool replayBattle = false)
    {
        bool eligible = root.TryGetProperty("ModifierScenario", out var scenario) &&
            scenario.GetString() == "original-magnitude-relic-batch";
        Require(eligible || !required, "Fixture lacks the original magnitude relic batch.");
        if (!eligible) return null;
        Require(root.GetProperty("Schema").GetInt32() == 114 &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4",
            "Native magnitude relic schema or build changed.");
        Require(root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalCaptureBoundary").GetString() == "AfterStopCombatLoop" &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            root.GetProperty("Policy").GetString() == "units-spells-and-junk",
            "Native magnitude relic battle is incomplete or lacks the independently replayed policy.");
        foreach (string field in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(field).GetInt32() == 0, "Native magnitude relic capture has nonzero " + field + ".");

        var actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        var stages = root.GetProperty("Stages").EnumerateArray().ToArray();
        var turns = root.GetProperty("Turns").EnumerateArray().ToArray();
        Require(actions.Length > 0 && stages.Length > 0 && turns.Length > 0 &&
            (turns[^1].GetProperty("ActualOutcome").GetInt32() == (int)RoomOutcome.BattleWon ||
                actions.Any(row => row.TryGetProperty("ActualOutcome", out var outcome) &&
                    outcome.GetInt32() == (int)RoomOutcome.BattleWon)),
            "Native magnitude relic archive lacks card actions, combat stages or its terminal win.");

        Query[] queries = root.GetProperty("RelicStatusMagnitudeQueries").EnumerateArray().Select(Read).ToArray();
        Require(queries.Length > 0, "Native magnitude getter queries are missing.");
        foreach (Query row in queries) CheckDefinitions(row.Relics);
        string queryParent = Serialize(queries);
        void CheckQueries()
        {
            foreach (Query row in queries)
            {
                CombatContext context = QueryContext(row);
                string parent = Serialize(context);
                StatusScalingResult result = RelicStatusMagnitudeModel.GetModifier(context, row.StatusId, row.NativeTeam);
                Require(result.Supported && result.Stacks == row.Actual,
                    "Native magnitude getter differs: " + Label(row) + " " + result.UnsupportedReason);
                Require(Serialize(context) == parent && Serialize(result.Context!.Relics) == Serialize(context.Relics) &&
                    result.Context.BattleRng.Equals(context.BattleRng),
                    "A magnitude query mutated its parent, recorded conditions or consumed RNG.");
            }
        }
        CheckQueries(); Parallel.For(0, 32, _ => CheckQueries());
        Require(Serialize(queries) == queryParent, "Parallel magnitude getters mutated their native query parents.");
        CheckMatrix(queries);
        foreach (var artifact in Artifacts)
            Require(queries.Any(row => !row.Matrix && !row.Preview && row.NativeTeam == 2 &&
                row.StatusId == artifact.Status && row.Actual == 1),
                "Original magnitude relic lacks a live getter: " + artifact.Key);

        var witnesses = Artifacts.ToDictionary(item => item.Key, _ => 0);
        foreach (FixtureValue row in actions)
        {
            var before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            var actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            var action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
            CheckDefinitions(before.Spawn.Train.Context!.Relics!);
            CheckRawUnits(before.Spawn.Train.Rooms.SelectMany(room => room.Units));
            CheckRawUnits(actual.Spawn.Train.Rooms.SelectMany(room => room.Units));
            string parent = Serialize(before);
            BattleActionResult modeled = BattleActionModel.PlayCard(before, action);
            Require(modeled.Supported, "Magnitude relic native action unsupported: " + modeled.Reason);
            Equal(BattleTurnChecks.Comparable(modeled.State!), BattleTurnChecks.Comparable(actual),
                "complete native magnitude relic action");
            if (row.TryGetProperty("ActualOutcome", out var actionOutcome))
                Require(modeled.Outcome == (RoomOutcome)actionOutcome.GetInt32(), "Native magnitude relic action outcome differs.");

            // Keep the captured status-damage accumulator intact. Only future
            // native additions/removals may create an attack difference.
            var withoutOwner = Remove(before, Artifacts[0].Id);
            Equal(Units(before), Units(withoutOwner), "buff ablation starting units and cached status damage");
            BattleActionResult ablated = BattleActionModel.PlayCard(withoutOwner, action);
            Require(ablated.Supported, "Original buff action ablation unsupported: " + ablated.Reason);
            witnesses[Artifacts[0].Key] += BuffWitnesses(Units(before), Units(actual), Units(ablated.State!));
            Require(Serialize(before) == parent, "Magnitude action or its ablation mutated the native starting state.");
        }

        foreach (FixtureValue row in stages)
        {
            var before = row.GetProperty("Before").Deserialize<RoomCombatState>()!;
            var actual = row.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            CheckDefinitions(before.Context!.Relics!);
            CheckRawUnits(before.Units); CheckRawUnits(actual.Units);
            string parent = Serialize(before);
            bool exchange = row.GetProperty("Kind").GetString() == "Exchange";
            RoomCombatResult Run(RoomCombatState frame) => exchange ? RoomCombatModel.Exchange(frame) : RoomCombatModel.Resolve(frame);
            RoomCombatResult modeled = Run(before);
            Require(modeled.Supported, "Native magnitude relic combat unsupported: " + modeled.UnsupportedReason);
            Equal(modeled.State, actual, "complete native magnitude relic combat stage");
            var ownerIds = before.Units.Concat(actual.Units).Concat(modeled.RetainedUnits)
                .Where(unit => unit.Team == CombatTeam.Player).Select(unit => unit.Id).ToHashSet();
            foreach (var artifact in Artifacts)
            {
                RoomCombatState withoutOwner = Remove(before, artifact.Id);
                Equal(withoutOwner.Units, before.Units, "magnitude ablation starting units and cached status damage");
                RoomCombatResult ablated = Run(withoutOwner);
                Require(ablated.Supported, "Original magnitude combat ablation unsupported: " + artifact.Key + " " + ablated.UnsupportedReason);
                witnesses[artifact.Key] += artifact.Status == "buff"
                    ? BuffWitnesses(before.Units, actual.Units, ablated.State!.Units)
                    : EventWitnesses(modeled.Events, ablated.Events, artifact.Status == "spikes" ? "Spikes" : "Regen", ownerIds);
            }
            Require(Serialize(before) == parent, "Magnitude combat or an owner ablation mutated its native starting state.");
        }
        Require(witnesses.Values.All(count => count > 0),
            "Some original magnitude relics lack actual future effects: " +
            string.Join(", ", witnesses.Select(item => item.Key + "=" + item.Value)));

        if (replayBattle)
        {
            BattleTurnChecks.Native(root);
            BattleActionChecks.Native(root);
        }
        Console.WriteLine("ORIGINAL-MAGNITUDE-RELIC-WITNESSES " +
            string.Join(", ", witnesses.Select(item => item.Key + "=" + item.Value)));
        Console.WriteLine($"NATIVE-ORIGINAL-MAGNITUDE-RELICS PASS: three unchanged originals, {queries.Count(row => row.Matrix)} matrix/{queries.Count(row => !row.Matrix && !row.Preview)} live/{queries.Count(row => !row.Matrix && row.Preview)} preview getter queries, {actions.Length} native card actions, {stages.Length} complete combat stages; future buff attack, Spikes damage and Regen restoration owner ablations, cached starting damage preserved and 32 immutable getter branches.");
        return witnesses;
    }

    private static void CheckDefinitions(IReadOnlyList<CombatRelicState> relics)
    {
        foreach (var item in Artifacts)
        {
            CombatRelicState[] matches = relics.Where(relic => relic.DataId == item.Id).ToArray();
            Require(matches.Length == 1, "Missing or duplicated original magnitude relic: " + item.Key);
            CombatRelicState relic = matches[0];
            Require(relic.AssetKey == item.Key && relic.EffectTypes.SequenceEqual([RelicModel.ModifyStatusMagnitude]) &&
                relic.IsCovenant == false && relic.DisallowedInPlacementPhase == false && relic.StatusMagnitudes?.Count == 1,
                "Original magnitude relic identity or effect type changed: " + item.Key);
            RelicStatusMagnitude rule = relic.StatusMagnitudes![0];
            Require(rule.EffectIndex == 0 && rule.SourceTeam == 2 && rule.StatusId == item.Status &&
                rule.Modifier == 1 && rule.Conditions.Count == 0,
                "Original magnitude relic payload changed: " + item.Key);
        }
    }

    private static void CheckMatrix(Query[] allRows)
    {
        Query[] rows = allRows.Where(row => row.Matrix && !row.Preview).ToArray();
        Require(rows.Select(row => (row.StatusId, row.NativeTeam)).Distinct().Count() >= 20,
            "Native magnitude matrix lacks its 20 distinct status/team gates.");
        foreach (int team in new[] { 0, 1, 2, 255 })
        foreach (string status in new[] { "buff", "spikes", "regen", "mageblade", "armor" })
        {
            Query[] matching = rows.Where(row => row.NativeTeam == team && row.StatusId == status).ToArray();
            int expected = (status is "buff" or "spikes" or "regen") && team != 1 ? 1 : 0;
            Require(matching.Length > 0 && matching.All(row => row.Actual == expected),
                "Native magnitude None/hero/monster/combined or unrelated-status gate differs: " + status + "/" + team);
        }
    }

    private static int BuffWitnesses(IEnumerable<CombatUnit> priorUnits, IEnumerable<CombatUnit> actualUnits,
        IEnumerable<CombatUnit> withoutUnits)
    {
        var actual = actualUnits.ToDictionary(unit => unit.Id);
        var without = withoutUnits.ToDictionary(unit => unit.Id);
        return priorUnits.Count(prior => prior.Team == CombatTeam.Player && prior.Status("buff")?.Stacks > 0 &&
            actual.TryGetValue(prior.Id, out CombatUnit? added) && without.TryGetValue(prior.Id, out CombatUnit? ablated) &&
            added.Status("buff")?.Stacks > prior.Status("buff")!.Stacks &&
            added.Status("buff")?.Stacks == ablated.Status("buff")?.Stacks &&
            added.AttackBeforeStatuses == ablated.AttackBeforeStatuses && added.Attack > ablated.Attack &&
            added.Modifiers?.DamageBuffFromStatusEffects > ablated.Modifiers?.DamageBuffFromStatusEffects);
    }

    private static int EventWitnesses(IReadOnlyList<CombatEvent> actual, IReadOnlyList<CombatEvent> without, string kind,
        ISet<int> ownerIds)
    {
        int witnesses = 0;
        foreach (var group in actual.Where(effect => effect.Kind == kind && ownerIds.Contains(effect.Actor))
            .GroupBy(effect => (effect.Actor, effect.Target)))
        {
            CombatEvent[] corresponding = without.Where(effect => effect.Kind == kind && effect.Actor == group.Key.Actor &&
                effect.Target == group.Key.Target).ToArray();
            CombatEvent[] baseline = group.ToArray();
            for (int index = 0; index < Math.Min(baseline.Length, corresponding.Length); index++)
                if (baseline[index].Amount > 0 && baseline[index].Amount > corresponding[index].Amount) witnesses++;
        }
        return witnesses;
    }

    private static void CheckRawUnits(IEnumerable<CombatUnit> units) => Require(units.All(unit =>
        unit.Modifiers?.DamageBuffFromStatusEffects.HasValue == true), "Magnitude battle lacks raw native status-damage accumulators.");
    private static CombatUnit[] Units(BattleTurnState state) => state.Spawn.Train.Rooms.SelectMany(room => room.Units).ToArray();
    private static RoomCombatState Remove(RoomCombatState source, string id) => new(source.RoomIndex, source.Deployment,
        source.Units, source.ExternalInteractions, source.Context!.WithRelics(source.Context.Relics!.Where(relic => relic.DataId != id).ToArray()), source.Preview);
    private static BattleTurnState Remove(BattleTurnState source, string id)
    {
        EnemySpawnState old = source.Spawn;
        CombatContext context = old.Train.Context!.WithRelics(old.Train.Context.Relics!.Where(relic => relic.DataId != id).ToArray());
        TrainCombatState train = CardSpellModel.WithContext(old.Train, context);
        var spawn = new EnemySpawnState(train, old.Waves, old.SelectedGroups, old.Phase, old.Looping, old.Rng,
            old.NextUnitId, old.Treasures, old.TreasuresRemaining, old.TreasureEnabled, old.FirstTreasureTurn,
            old.FirstTreasureRoom, old.Turn, old.ExternalInteractions, old.CanonicalDecisionReferences, old.PendingDestroyedUnitIds);
        return new(spawn, source.Energy, source.EnergyPerTurn, source.DrawPerTurn, source.ForgePoints, source.DragonsHoard,
            source.MoonPhase, source.RngStreams, source.OtherPiles, source.ExternalInteractions, source.PlayRules,
            source.BattlePreviewEnabled, source.UiRngIsolated, source.CanonicalDecisionReferences,
            source.CanonicalPhysicalReferences, source.SelectedRoom);
    }

    private sealed record Query(string StatusId, int NativeTeam, int Actual, bool Matrix, bool Preview,
        CombatRelicState[] Relics, CombatContext? Context);
    private static Query Read(FixtureValue row) => new(row.GetProperty("StatusId").GetString()!, row.GetProperty("NativeTeam").GetInt32(),
        row.GetProperty("Actual").GetInt32(), row.GetProperty("Matrix").GetBoolean(), row.GetProperty("Preview").GetBoolean(),
        row.GetProperty("Relics").Deserialize<CombatRelicState[]>()!,
        row.TryGetProperty("Context", out var context) && context.ValueKind != FixtureKind.Null ? context.Deserialize<CombatContext>() : null);
    private static CombatContext QueryContext(Query row) => row.Context?.WithRelics(row.Relics) ??
        new(new([], [], [], UnityRng.Seed(13), 0, []), UnityRng.Seed(13), 0, 1, 10, relics: row.Relics);
    private static string Label(Query row) => row.StatusId + "/" + row.NativeTeam + "/matrix=" + row.Matrix + "/preview=" + row.Preview;
    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Equal(string expected, string actual, string label)
    {
        string? difference = ModelJson.Difference(expected, actual);
        Require(difference == null, label + " differs: " + difference);
    }
    private static void Equal<T>(T expected, T actual, string label)
    {
        string? difference = ModelJson.Difference(Serialize(expected), Serialize(actual));
        Require(difference == null, label + " differs: " + difference);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidDataException(message); }
}
