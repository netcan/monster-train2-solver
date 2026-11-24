using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalFrontReanimationChecks
{
    private const string Effect = "RelicEffectAddStatusEffectOnFirstSpawn";

    internal static void Native(FixtureValue witness, bool observeOnly, bool wholeTurns)
    {
        var (ownerId, ownerKey, stacks) = witness.GetProperty("Mode").GetString() switch
        {
            "Tier2" => ("d5bc1611-0bd2-47c5-9d78-3280efbc8fbe", "Enemy_FrontReanimationT2", 1),
            "Tier3" => ("a8202bfb-55f0-4630-a592-d60177796bf0", "Enemy_FrontReanimationT3", 2),
            _ => throw new InvalidOperationException("Unknown original front reanimation tier.")
        };
        FixtureValue owner = witness.GetProperty("Owner");
        Require(witness.GetProperty("Schema").GetInt32() == 1 &&
            witness.GetProperty("Boundary").GetString() == "OriginalFirstSpawnReanimation" &&
            witness.GetProperty("GameVersion").GetString() == Game231FixtureCatalog.Version &&
            witness.GetProperty("GameModuleMvid").GetString() == Game231FixtureCatalog.Module &&
            witness.GetProperty("Activation").GetString() == "HeroManager.AddHeroBlessing" &&
            owner.GetProperty("Id").GetString() == ownerId && owner.GetProperty("AssetKey").GetString() == ownerKey &&
            owner.GetProperty("Type").GetString() == Effect && owner.GetProperty("Stacks").GetInt32() == stacks &&
            owner.GetProperty("Team").GetString() == "Heroes" && owner.GetProperty("Target").GetString() == "FrontInRoom" &&
            owner.GetProperty("Trigger").GetString() == "OnSpawn" && !owner.GetProperty("CanApplyInPreviewMode").GetBoolean() &&
            !owner.GetProperty("DisallowInDeploymentPhase").GetBoolean(), "Expected the pinned original front reanimation owner.");

        FixtureValue input = witness.GetProperty("NativeRunInput");
        Require(input.ValueKind == FixtureKind.Null || input.GetProperty("Seed").GetInt32() == 424242 && input.GetProperty("Distance").GetInt32() == 1 &&
            input.GetProperty("AscensionLevel").GetInt32() == 0 && input.GetProperty("Scenario").GetString() == "Level2BattleJunker",
            "Expected the original rank-zero Ring 2 donor.");

        FixtureValue[] rows = witness.GetProperty("Rows").EnumerateArray().ToArray();
        Require(rows.Select(row => row.GetProperty("Kind").GetString()).SequenceEqual(new[]
            { "Birth", "Birth", "AfterDraw", "Birth", "Birth", "Birth", "Birth", "AfterDraw" }) &&
            rows.All(row => !row.GetProperty("Preview").GetBoolean()) &&
            rows.Select(row => row.GetProperty("Frame").GetInt32()).SequenceEqual(
                rows.Select(row => row.GetProperty("Frame").GetInt32()).Order()) &&
            rows.Take(7).All(row => row.GetProperty("Turn").GetInt32() == 1) && rows[7].GetProperty("Turn").GetInt32() == 2,
            "Expected two original birth waves separated by a committed AfterDraw despite identical native turn counters.");
        int firstActor = CheckBirth(rows[0], "HeavyT1_Basic", 0, stacks);
        int secondActor = CheckBirth(rows[3], "HeavyT1_Basic", 0, stacks);
        int[] actors = [firstActor, CheckBirth(rows[1], "MageT1_Junker", 0, 0), secondActor,
            CheckBirth(rows[4], "AttackerT1_Spearman", 0, 0), CheckBirth(rows[5], "MageT1_Junker", 0, 0),
            CheckBirth(rows[6], "TreasureBagLvl1Gold", 2, 0)];
        Require(actors.All(actor => actor > 0) && actors.Distinct().Count() == actors.Length,
            "The birth and skip controls must name distinct live actors.");
        foreach (FixtureValue reset in new[] { rows[2], rows[7] })
            Require(reset.GetProperty("BeforeLatch").GetBoolean() && !reset.GetProperty("AfterLatch").GetBoolean() &&
                Rng(reset, "RngBefore").Equals(Rng(reset, "RngAfter")), "Native AfterDraw did not reset the latch without touching Battle RNG.");

        BattleTurnState[] decisions = witness.GetProperty("Decisions").Deserialize<BattleTurnState[]>()!;
        Require(decisions.Select(state => state.Spawn.Turn).SequenceEqual(new[] { 0, 1, 2 }) &&
            decisions.All(state => Rule(state.Spawn.Train.Context!, ownerId).FirstSpawnLatched == (observeOnly ? (bool?)null : false)),
            "The observed deployment and post-draw decisions have an unexpected projected latch.");
        FixtureValue[] spawns = witness.GetProperty("Phases").GetProperty("Spawns").EnumerateArray().ToArray();
        Require(spawns.Select(row => row.GetProperty("Index").GetInt32()).SequenceEqual(new[] { 0, 1, 2 }) &&
            spawns.Select(row => row.GetProperty("IncludeTreasure").GetBoolean()).SequenceEqual(new[] { false, true, false }),
            "Expected the two live waves and the nested second wave without treasure.");

        string guard = $"Unmodeled relic effect {Effect} on {ownerKey}.";
        string[] exclusions = stacks == 1 ? ["SubtypesData_TreasureCollector", "SubtypesData_Boss"] :
            ["SubtypesData_TreasureCollector", "SubtypesData_Boss", "SubtypesData_ChosenSpikes"];
        TrainCombatState? firstWave = null;
        foreach (FixtureValue row in spawns)
        {
            int index = row.GetProperty("Index").GetInt32();
            int grantedActor = index == 0 ? firstActor : secondActor;
            EnemySpawnState before = row.GetProperty("Before").Deserialize<EnemySpawnState>()!;
            EnemySpawnState actual = row.GetProperty("Actual").Deserialize<EnemySpawnState>()!;
            Require(before.Turn == 1 && actual.Turn == 1 && before.Phase == (index == 0 ? 0 : 1) && actual.Phase == before.Phase + 1,
                "Each native wave must advance its phase under the same native combat turn.");
            CheckRule(before, ownerId, stacks, exclusions, guard, observeOnly ? null : false, observeOnly);
            CheckRule(actual, ownerId, stacks, exclusions, guard, observeOnly ? null : true, observeOnly);
            int[] beforeIds = before.Train.Rooms.SelectMany(room => room.Units).Select(unit => unit.Id).ToArray();
            CombatUnit[] births = actual.Train.Rooms.SelectMany(room => room.Units).Where(unit => !beforeIds.Contains(unit.Id)).ToArray();
            Require(births.Count(unit => unit.Status("undying")?.Stacks > 0) == 1 &&
                births.Single(unit => unit.Id == grantedActor).Status("undying")?.Stacks == stacks,
                "Exactly one newly born hero must gain the native number of reanimation stacks in each observed wave.");
            if (observeOnly) continue;
            string parent = JsonSerializer.Serialize(before);
            EnemySpawnResult result = EnemySpawningModel.Spawn(before, row.GetProperty("IncludeTreasure").GetBoolean(),
                row.GetProperty("Rooms").Deserialize<RoomPlayRule[]>());
            Require(result.Supported, result.UnsupportedReason ?? "The projected original wave could not be replayed.");
            string expected = EnemySpawningChecks.Comparable(actual);
            string predicted = EnemySpawningChecks.Comparable(result.State!);
            if (predicted != expected)
                throw new InvalidOperationException($"Original front reanimation Spawn[{index}] differs: {ModelJson.Difference(predicted, expected)}");
            Require(JsonSerializer.Serialize(before) == parent, "Spawn replay mutated its recorded parent.");
            if (index == 0) firstWave = result.State!.Train;
        }
        if (firstWave != null) CheckAfterDraw(firstWave, ownerId);
        if (wholeTurns) CheckDecisionChain(decisions);
        Console.WriteLine($"NATIVE-FRONT-REANIMATION PASS. {stacks} stack(s) on each first hero, later hero skips, two AfterDraw resets, " +
            $"and exact Battle RNG{(observeOnly ? " in the historical observation" : " and three complete spawned successors")}" +
            $"{(wholeTurns ? " plus two consecutive complete EndTurns" : "")}.");
    }

    private static int CheckBirth(FixtureValue row, string key, int floor, int stacks)
    {
        bool grant = stacks > 0;
        UnityRng before = Rng(row, "RngBefore");
        Require(row.GetProperty("AssetKey").GetString() == key && row.GetProperty("Room").GetInt32() == floor &&
            row.GetProperty("Team").GetString() == "Heroes" && !row.GetProperty("FromCard").GetBoolean() &&
            row.GetProperty("BeforeLatch").GetBoolean() == !grant && row.GetProperty("AfterLatch").GetBoolean() &&
            row.GetProperty("BeforeStacks").GetInt32() == 0 && row.GetProperty("AfterStacks").GetInt32() == stacks &&
            (grant ? before.Next() : before).Equals(Rng(row, "RngAfter")), "Original hero birth differs for " + key + ".");
        return row.GetProperty("Actor").GetInt32();
    }

    private static void CheckRule(EnemySpawnState state, string ownerId, int stacks, string[] exclusions, string guard,
        bool? latch, bool observeOnly)
    {
        RelicSpawnStatus rule = Rule(state.Train.Context!, ownerId);
        Require(rule.EffectIndex == 0 && rule.TargetEnemy && !rule.TargetPlayer && rule.SubtypeIsNone && !rule.SubtypeIsPyre &&
            rule.RequiredSubtype == "SubtypesData_None" && rule.ExcludedSubtypes.SequenceEqual(exclusions) &&
            rule.RestrictedRoom == null && rule.HpPercentAsStacks == 0 && rule.SourceKind == "" && !rule.RequireGraft &&
            !rule.ApplyToTeam && rule.CharacterAssetKeys.Count == 0 && rule.Conditions.Count == 0 && rule.EnergyGain == 0 &&
            rule.DrawCount == 0 && rule.Statuses is [{ Id: "undying" } status] && status.Stacks == stacks &&
            rule.FirstSpawnLatched == latch && state.ExternalInteractions.SequenceEqual(observeOnly ? [guard] : Array.Empty<string>()),
            "The projected original first-birth rule or historical guard differs.");
    }

    private static RelicSpawnStatus Rule(CombatContext context, string ownerId) => context.Relics!
        .Single(relic => relic.DataId == ownerId && relic.EffectTypes.SequenceEqual(new[] { Effect })).SpawnStatuses!.Single();

    private static void CheckAfterDraw(TrainCombatState source, string ownerId)
    {
        string original = JsonSerializer.Serialize(source);
        TrainCombatResult result = RelicDeckTurnDrawModel.ApplyAfterDraw(source, definitions: null);
        Require(result.Supported && result.State?.Context is CombatContext settled &&
            Rule(source.Context!, ownerId).FirstSpawnLatched == true && Rule(settled, ownerId).FirstSpawnLatched == false &&
            settled.BattleRng.Equals(source.Context!.BattleRng) && JsonSerializer.Serialize(source) == original,
            "Model AfterDraw must reset the originally consumed latch without drawing RNG or mutating the parent.");
    }

    private static void CheckDecisionChain(BattleTurnState[] decisions)
    {
        BattleTurnState current = decisions[0];
        foreach (BattleTurnState actual in decisions.Skip(1))
        {
            string parent = JsonSerializer.Serialize(current);
            BattleTurnResult result = BattleTurnModel.EndTurn(current);
            string label = $"Original front reanimation EndTurn {current.Spawn.Turn} to {actual.Spawn.Turn}";
            Require(result.Supported && result.Outcome == RoomOutcome.Exchanged,
                $"{label} could not settle: {result.UnsupportedReason ?? result.Outcome.ToString()}");
            RecordedBattleChecks.Compare(result.State!, actual, terminal: false, label);
            Require(JsonSerializer.Serialize(current) == parent, $"{label} mutated its parent.");
            current = result.State!;
        }
    }

    private static UnityRng Rng(FixtureValue row, string property)
    {
        uint[] words = row.GetProperty(property).EnumerateArray().Select(word => word.GetUInt32()).ToArray();
        Require(words.Length == 4, "Expected four recorded Battle RNG words.");
        return new UnityRng(words[0], words[1], words[2], words[3]);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
