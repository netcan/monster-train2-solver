using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalDecayRelicBatchChecks
{
    private static readonly (string Key, string Id)[] Artifacts =
    [
        ("RagePreservedOnFriendlies", "b7822614-96ec-4ace-9029-6efc8adef374"),
        ("IncreaseFrostbiteStacks", "9cb2b9b2-0308-4f71-92e8-e96a30b87416")
    ];
    private static readonly string[] ExcludedSources =
    ["CardEffectMultiplyStatusEffect", "CardEffectTransferAllStatusEffects", "CardEffectTransferAllStatusEffectsFromUnit"];

    internal static IReadOnlyDictionary<string, int>? Native(FixtureValue root, bool required = false,
        bool replayBattle = false)
    {
        bool eligible = root.TryGetProperty("ModifierScenario", out var scenario) &&
            scenario.GetString() == "original-decay-relic-batch";
        Require(eligible || !required, "Fixture lacks the original decay relic batch.");
        if (!eligible) return null;
        Require(root.GetProperty("Schema").GetInt32() == 116 && root.GetProperty("GameVersion").GetString() == "2.2.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4",
            "Native decay relic schema or game build changed.");
        Require(root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalCaptureBoundary").GetString() == "AfterStopCombatLoop" &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean() && root.GetProperty("Policy").GetString() == "units-spells-and-junk",
            "Native decay relic battle is incomplete or lacks its independently replayed policy.");
        foreach (string field in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(field).GetInt32() == 0, "Native decay relic capture has nonzero " + field + ".");
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        FixtureValue[] stages = root.GetProperty("Stages").EnumerateArray().ToArray();
        FixtureValue[] turns = root.GetProperty("Turns").EnumerateArray().ToArray();
        Require(actions.Length > 0 && stages.Length > 0 && turns.Length >= 2 &&
            (turns[^1].GetProperty("ActualOutcome").GetInt32() == (int)RoomOutcome.BattleWon ||
                actions.Any(row => row.TryGetProperty("ActualOutcome", out var outcome) && outcome.GetInt32() == (int)RoomOutcome.BattleWon)),
            "Native decay relic archive lacks actions, complete stages, two EndTurns or its terminal win.");

        DecayQuery[] queries = root.GetProperty("RelicStatusDecayQueries").EnumerateArray().Select(ReadDecay).ToArray();
        Require(queries.Length > 0, "Native status-decay queries are missing.");
        foreach (DecayQuery query in queries) CheckDefinitions(query.Relics);
        string queryParent = Serialize(queries);
        void CheckQueries()
        {
            foreach (DecayQuery row in queries)
            {
                CombatContext context = QueryContext(row.Relics);
                string parent = Serialize(context);
                RelicStatusDecayResult result = RelicStatusDecayModel.GetShouldRemove(context, row.RemoveAtContext,
                    row.StatusId, row.NativeTeam, row.Preview);
                (bool remove, string? owner, int? index) = Expected(row);
                Require(result.Supported && result.ShouldRemove == row.Actual && row.Actual == remove &&
                    result.SuppressingRelicId == row.SuppressingRelicId && row.SuppressingRelicId == owner &&
                    result.SuppressingEffectIndex == index,
                    "Native status-decay result or first suppressor differs: " + Label(row) + " " + result.UnsupportedReason);
                Require(Serialize(context) == parent && Serialize(result.Context) == parent &&
                    result.Context!.BattleRng.Equals(context.BattleRng) && result.Context.Cards.Rng.Equals(context.Cards.Rng),
                    "A decay getter mutated its context, conditions or RNG.");
            }
        }
        CheckQueries(); Parallel.For(0, 32, _ => CheckQueries());
        Require(Serialize(queries) == queryParent, "Parallel decay getters mutated their native query parents.");
        CheckDecayCoverage(queries);

        StackQuery[] stackQueries = root.GetProperty("RelicStatusStackQueries").EnumerateArray().Select(ReadStack).ToArray();
        Require(stackQueries.Length > 0, "Original Frostbite relic stack queries are missing.");
        foreach (StackQuery row in stackQueries) CheckDefinitions(row.Relics);
        string stackParent = Serialize(stackQueries);
        void CheckStacks()
        {
            foreach (StackQuery row in stackQueries)
                Require(GetStacks(row, row.Relics) == row.Actual, "Original Frostbite native stack query differs: " + StackLabel(row));
        }
        CheckStacks(); Parallel.For(0, 32, _ => CheckStacks());
        Require(Serialize(stackQueries) == stackParent, "Parallel Frostbite queries mutated their native relic parents.");
        CheckStackCoverage(stackQueries);

        int ragePreserved = 0, enemyRageDecayed = 0;
        var poisonPreserved = new Dictionary<CombatTeam, int> { [CombatTeam.Player] = 0, [CombatTeam.Enemy] = 0 };
        var poisonApplied = new Dictionary<CombatTeam, int> { [CombatTeam.Player] = 0, [CombatTeam.Enemy] = 0 };
        foreach (FixtureValue row in actions)
        {
            BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            BattleTurnState actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            PlayCardAction action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
            CheckDefinitions(before.Spawn.Train.Context!.Relics!);
            CheckRawUnits(Units(before)); CheckRawUnits(Units(actual));
            string parent = Serialize(before);
            BattleActionResult modeled = BattleActionModel.PlayCard(before, action);
            Require(modeled.Supported, "Original decay relic action unsupported: " + modeled.Reason);
            Equal(BattleTurnChecks.Comparable(modeled.State!), BattleTurnChecks.Comparable(actual), "complete native decay relic action");
            if (row.TryGetProperty("ActualOutcome", out var outcome))
                Require(modeled.Outcome == (RoomOutcome)outcome.GetInt32(), "Native decay relic action outcome differs.");
            BattleTurnState withoutOwner = Remove(before, Artifacts[1].Id);
            Equal(Units(before), Units(withoutOwner), "Frostbite ablation starting units and accumulated status damage");
            BattleActionResult ablated = BattleActionModel.PlayCard(withoutOwner, action);
            Require(ablated.Supported, "Original Frostbite card ablation unsupported: " + ablated.Reason);
            if (IsStatusSpell(before, action.CardInstanceId))
            {
                CombatUnit prior = Units(before).Single(unit => unit.Id == action.TargetUnitId);
                CombatUnit added = Units(actual).Single(unit => unit.Id == prior.Id);
                CombatUnit removed = Units(ablated.State!).Single(unit => unit.Id == prior.Id);
                Require(Count(prior, "poison") == 0 && Count(added, "poison") == 3 && Count(removed, "poison") == 1 &&
                    Count(added, "buff") == Count(prior, "buff") + 2 && Count(added, "armor") == Count(prior, "armor") + 30 &&
                    Count(added, "buff") == Count(removed, "buff") && Count(added, "armor") == Count(removed, "armor"),
                    "Original Frostbite did not turn the three-effect card's poison1 into poison3 while applying buff2 and armor30 unchanged.");
                poisonApplied[prior.Team]++;
            }
            Require(Serialize(before) == parent, "Decay relic action or its owner ablation mutated the native starting state.");
        }

        foreach (FixtureValue row in stages)
        {
            RoomCombatState before = row.GetProperty("Before").Deserialize<RoomCombatState>()!;
            RoomCombatState actual = row.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            CheckDefinitions(before.Context!.Relics!);
            CheckRawUnits(before.Units); CheckRawUnits(actual.Units);
            string parent = Serialize(before);
            string kind = row.GetProperty("Kind").GetString()!;
            Require(kind is "Exchange" or "Room", "Unexpected native decay relic combat stage: " + kind);
            RoomCombatResult Run(RoomCombatState frame) => kind == "Exchange" ? RoomCombatModel.Exchange(frame) : RoomCombatModel.Resolve(frame);
            RoomCombatResult modeled = Run(before);
            Require(modeled.Supported, "Original decay relic combat unsupported: " + modeled.UnsupportedReason);
            Equal(modeled.State, actual, "complete native decay relic combat stage");
            var without = new Dictionary<string, RoomCombatState>();
            foreach (var artifact in Artifacts)
            {
                RoomCombatState ablation = Remove(before, artifact.Id);
                Equal(before.Units, ablation.Units, "decay ablation starting units and accumulated status damage");
                RoomCombatResult result = Run(ablation);
                Require(result.Supported, "Original decay combat ablation unsupported: " + artifact.Key + " " + result.UnsupportedReason);
                without[artifact.Id] = result.State!;
            }
            // Only completed native stages with a surviving positive stack are
            // preservation evidence. Getter hits and absent/dead units do not count.
            foreach (CombatUnit prior in before.Units)
            {
                CombatUnit? added = actual.Units.FirstOrDefault(unit => unit.Id == prior.Id);
                if (added == null || added.Health <= 0) continue;
                CombatUnit? noRage = without[Artifacts[0].Id].Units.FirstOrDefault(unit => unit.Id == prior.Id);
                CombatUnit? noFrostbite = without[Artifacts[1].Id].Units.FirstOrDefault(unit => unit.Id == prior.Id);
                if (prior.Team == CombatTeam.Player && Count(prior, "buff") > 0 && Count(added, "buff") == Count(prior, "buff") &&
                    noRage != null && noRage.Health > 0 && Count(added, "buff") - Count(noRage, "buff") >= 1)
                    ragePreserved++;
                if (prior.Team == CombatTeam.Enemy && Count(prior, "buff") > 0 &&
                    Count(added, "buff") == Math.Max(0, Count(prior, "buff") - 1) && noRage != null && noRage.Health > 0 &&
                    Count(noRage, "buff") == Count(added, "buff"))
                    enemyRageDecayed++;
                if (Count(prior, "poison") > 0 && Count(added, "poison") == Count(prior, "poison") &&
                    noFrostbite != null && noFrostbite.Health > 0 && Count(added, "poison") - Count(noFrostbite, "poison") >= 1)
                    poisonPreserved[prior.Team]++;
            }
            Require(Serialize(before) == parent, "Decay combat or its owner ablations mutated the native starting state.");
        }
        Require(ragePreserved > 0 && enemyRageDecayed > 0 && poisonPreserved.Values.All(value => value > 0) &&
            poisonApplied.Values.All(value => value > 0),
            "Missing actual decay relic effects: Rage preserved=" + ragePreserved + ", enemy Rage decayed=" + enemyRageDecayed +
            ", poison preserved=" + string.Join(",", poisonPreserved.Select(item => item.Key + "=" + item.Value)) +
            ", card poison3/1=" + string.Join(",", poisonApplied.Select(item => item.Key + "=" + item.Value)));
        var witnesses = new Dictionary<string, int>
        {
            [Artifacts[0].Key] = ragePreserved,
            [Artifacts[1].Key] = poisonPreserved.Values.Sum() + poisonApplied.Values.Sum()
        };
        if (replayBattle)
        {
            BattleTurnChecks.Native(root);
            BattleActionChecks.Native(root);
        }
        Console.WriteLine("ORIGINAL-DECAY-RELIC-WITNESSES " + string.Join(", ", witnesses.Select(item => item.Key + "=" + item.Value)));
        Console.WriteLine($"NATIVE-ORIGINAL-DECAY-RELICS PASS: two unchanged originals, {queries.Count(row => row.Matrix)} matrix/{queries.Count(row => !row.Matrix && !row.Preview)} live/{queries.Count(row => !row.Matrix && row.Preview)} preview decay queries, {stackQueries.Length} stack queries, {actions.Length} native actions, {stages.Length} complete stages; player Rage preservation, enemy Rage decay, both-team poison preservation and real card poison3/1 owner ablations, retained starting accumulators and 32 immutable getter branches.");
        return witnesses;
    }

    private static void CheckDefinitions(IReadOnlyList<CombatRelicState> relics)
    {
        foreach (var item in Artifacts)
        {
            CombatRelicState[] matches = relics.Where(relic => relic.DataId == item.Id).ToArray();
            Require(matches.Length == 1, "Missing or duplicated original decay relic: " + item.Key);
            CombatRelicState relic = matches[0];
            bool rage = item.Id == Artifacts[0].Id;
            string[] expectedTypes = rage ? new[] { RelicModel.PreventStatusDecay } :
                new[] { RelicModel.AddStatusOnApplied, RelicModel.PreventStatusDecay };
            Require(relic.AssetKey == item.Key && relic.IsCovenant == false && relic.DisallowedInPlacementPhase == false &&
                relic.EffectTypes.SequenceEqual(expectedTypes) && relic.StatusDecays?.Count == 1,
                "Original decay relic identity or effect types changed: " + item.Key);
            RelicStatusDecay decay = relic.StatusDecays![0];
            Require(decay.EffectIndex == (rage ? 0 : 1) && decay.SourceTeam == (rage ? 2 : 0) &&
                decay.StatusIds.SequenceEqual([rage ? "buff" : "poison"]) && decay.Conditions.Count == 0,
                "Original decay relic payload changed: " + item.Key);
            if (rage) Require(relic.StatusStackAdditions?.Count is null or 0, "Original Rage gained an unrelated stack effect.");
            else
            {
                Require(relic.StatusStackAdditions?.Count == 1, "Original Frostbite stack effect is missing or duplicated.");
                RelicStatusStacks stacks = relic.StatusStackAdditions![0];
                Require(stacks.EffectIndex == 0 && stacks.SourceTeam == "None" && stacks.StatusId == "poison" &&
                    stacks.AdditionalStacks == 2 && stacks.AddCondition == 1 && stacks.Conditions.Count == 0,
                    "Original Frostbite None-team poison+2 positive gate changed.");
            }
        }
        Require(relics.Where(relic => relic.StatusDecays?.Count > 0).All(relic => Artifacts.Any(item => item.Id == relic.DataId)),
            "An additional decay suppressor confounds the two original owners.");
    }

    private static (bool Remove, string? Owner, int? Index) Expected(DecayQuery row)
    {
        if (!row.RemoveAtContext) return (false, null, null);
        if (row.StatusId == "poison") return (false, Artifacts[1].Id, 1);
        if (row.StatusId == "buff" && row.NativeTeam == 2) return (false, Artifacts[0].Id, 0);
        return (true, null, null);
    }
    private static void CheckDecayCoverage(DecayQuery[] rows)
    {
        foreach (int team in new[] { 1, 2 })
        foreach (string id in new[] { "buff", "poison", "armor", "regen" })
        foreach (bool remove in new[] { false, true })
            Require(rows.Any(row => row.Matrix && !row.Preview && row.NativeTeam == team && row.StatusId == id && row.RemoveAtContext == remove),
                "Native decay matrix lacks a team/status/context gate: " + team + "/" + id + "/" + remove);
        foreach (int team in new[] { 1, 2 })
            Require(rows.Any(row => !row.Matrix && !row.Preview && row.UnitId > 0 && row.NativeTeam == team &&
                row.StatusId == "poison" && row.RemoveAtContext && !row.Actual && row.SuppressingRelicId == Artifacts[1].Id),
                "Original Frostbite lacks a real poison decay query for native team " + team + ".");
        Require(rows.Any(row => !row.Matrix && !row.Preview && row.UnitId > 0 && row.NativeTeam == 2 &&
                row.StatusId == "buff" && row.RemoveAtContext && !row.Actual && row.SuppressingRelicId == Artifacts[0].Id) &&
            rows.Any(row => !row.Matrix && !row.Preview && row.UnitId > 0 && row.NativeTeam == 1 &&
                row.StatusId == "buff" && row.RemoveAtContext && row.Actual && row.SuppressingRelicId == null),
            "Original Rage lacks its real friendly preservation and enemy removal gates.");
        Require(rows.Any(row => !row.Matrix && row.Preview && row.UnitId > 0 && row.RemoveAtContext &&
            !row.Actual && row.SuppressingRelicId != null), "Original decay relics lack a real preview suppression query.");
    }

    private static void CheckStackCoverage(StackQuery[] rows)
    {
        foreach (CombatTeam? team in new CombatTeam?[] { CombatTeam.Player, CombatTeam.Enemy, null })
        foreach (int incoming in new[] { -1, 0, 1, int.MaxValue })
        {
            StackQuery[] matching = rows.Where(row => row.Matrix && row.TargetTeam == team && row.StatusId == "poison" &&
                row.Incoming == incoming && !row.Permanent && !row.Deferred && row.IncludeAdders && row.EffectType == null).ToArray();
            int expected = incoming > 0 ? unchecked(incoming + 2) : incoming;
            Require(matching.Length > 0 && matching.All(row => row.Actual == expected),
                "Original Frostbite positive/overflow stack gate is missing: " + team + "/" + incoming);
        }
        foreach (CombatTeam team in new[] { CombatTeam.Player, CombatTeam.Enemy })
            Require(rows.Any(row => !row.Matrix && row.TargetTeam == team && row.StatusId == "poison" && row.Incoming == 1 &&
                row.Actual == 3 && !row.Permanent && !row.Deferred && row.IncludeAdders &&
                !ExcludedSources.Contains(row.EffectType ?? "") &&
                GetStacks(row, row.Relics.Where(relic => relic.DataId != Artifacts[1].Id).ToArray()) == 1),
                "Original Frostbite lacks a real two-stack addition query for " + team + ".");
    }

    private sealed record DecayQuery(bool RemoveAtContext, string StatusId, int NativeTeam, int UnitId, bool Actual,
        string? SuppressingRelicId, bool Matrix, bool Preview, CombatRelicState[] Relics);
    private static DecayQuery ReadDecay(FixtureValue row) => new(row.GetProperty("RemoveAtContext").GetBoolean(),
        row.GetProperty("StatusId").GetString()!, row.GetProperty("NativeTeam").GetInt32(), row.GetProperty("UnitId").GetInt32(),
        row.GetProperty("Actual").GetBoolean(), row.GetProperty("SuppressingRelicId").GetString(), row.GetProperty("Matrix").GetBoolean(),
        row.GetProperty("Preview").GetBoolean(), row.GetProperty("Relics").Deserialize<CombatRelicState[]>()!);
    private sealed record StackQuery(string StatusId, int Incoming, CombatTeam? TargetTeam, bool HasStatus,
        bool Permanent, bool Deferred, bool IncludeAdders, string? EffectType, CombatRelicState[] Relics, int Actual, bool Matrix);
    private static StackQuery ReadStack(FixtureValue row) => new(row.GetProperty("StatusId").GetString()!, row.GetProperty("Incoming").GetInt32(),
        row.GetProperty("TargetTeam").Deserialize<CombatTeam?>(), row.GetProperty("HasStatus").GetBoolean(),
        row.GetProperty("Permanent").GetBoolean(), row.GetProperty("Deferred").GetBoolean(), row.GetProperty("IncludeAdders").GetBoolean(),
        row.GetProperty("EffectType").GetString(), row.GetProperty("Relics").Deserialize<CombatRelicState[]>()!,
        row.GetProperty("Actual").GetInt32(), row.GetProperty("Matrix").GetBoolean());
    private static int GetStacks(StackQuery row, IReadOnlyList<CombatRelicState> relics) => RelicStatusStackModel.GetStacks(
        row.IncludeAdders ? relics : null, row.StatusId, row.Incoming, row.TargetTeam, row.HasStatus,
        row.Permanent, row.Deferred, row.EffectType);
    private static CombatContext QueryContext(CombatRelicState[] relics) =>
        new(new([], [], [], UnityRng.Seed(13), 0, []), UnityRng.Seed(13), 0, 1, 10, relics: relics);
    private static string Label(DecayQuery row) => row.StatusId + "/" + row.NativeTeam + "/remove=" + row.RemoveAtContext +
        "/matrix=" + row.Matrix + "/preview=" + row.Preview;
    private static string StackLabel(StackQuery row) => row.StatusId + "/" + row.Incoming + "/" + row.TargetTeam +
        "/permanent=" + row.Permanent + "/deferred=" + row.Deferred + "/source=" + row.EffectType;
    private static int Count(CombatUnit unit, string id) => unit.Status(id)?.Stacks ?? 0;
    private static void CheckRawUnits(IEnumerable<CombatUnit> units) => Require(units.All(unit =>
        unit.Modifiers?.DamageBuffFromStatusEffects.HasValue == true), "Decay battle lacks raw native status-damage accumulators.");
    private static CombatUnit[] Units(BattleTurnState source) => source.Spawn.Train.Rooms.SelectMany(room => room.Units).ToArray();
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

    // Keep this chooser identical to the native recording policy. It reads only
    // captured definitions and live state for whole-battle and mid-battle replay.
    internal static PlayCardAction? Choose(BattleTurnState source)
    {
        if (source.PlayRules == null || source.Spawn.Train.Context == null) return null;
        IReadOnlyList<PlayCardAction> plays = BattleActionModel.EnumerateSupportedPlays(source);
        CombatContext context = source.Spawn.Train.Context;
        foreach (CardToken card in context.Cards.Hand)
        {
            CombatUnit? template = source.PlayRules.Cards.FirstOrDefault(rule => rule.DataId == card.DataId)?.SpawnUnit;
            if (template?.AssetKey.StartsWith("TrainSteward", StringComparison.Ordinal) != true) continue;
            PlayCardAction? summon = plays.FirstOrDefault(action => action.CardInstanceId == card.InstanceId && action.RoomIndex == 0);
            if (summon != null) return summon;
        }
        CombatUnit[] untreated = source.Spawn.Train.Rooms.SelectMany(room => room.Units)
            .Where(unit => unit.Health > 0 && !unit.IsPyre && !unit.Statuses.Any(status => status.Id == "poison" && status.Stacks > 0)).ToArray();
        IEnumerable<CombatUnit> targets = untreated.Where(unit => unit.Team == CombatTeam.Enemy && unit.Health > 3)
            .OrderByDescending(unit => unit.MaxHealth).ThenBy(unit => unit.Id)
            .Concat(untreated.Where(unit => unit.Team == CombatTeam.Player && unit.AssetKey.StartsWith("TrainSteward", StringComparison.Ordinal))
                .OrderBy(unit => unit.Id));
        foreach (CombatUnit target in targets)
        {
            PlayCardAction? apply = plays.FirstOrDefault(action => action.TargetUnitId == target.Id && IsStatusSpell(source, action.CardInstanceId));
            if (apply != null) return apply;
        }
        return plays.FirstOrDefault(action => !IsStatusSpell(source, action.CardInstanceId));
    }
    private static bool IsStatusSpell(BattleTurnState source, int instanceId)
    {
        CardInstanceState? card = source.Spawn.Train.Context!.FindCard(instanceId);
        CardPlayRule? rule = source.PlayRules!.Cards.FirstOrDefault(item => item.DataId == card?.DataId);
        if (rule?.Effects.Count != 3) return false;
        for (int index = 0; index < 3; index++)
        {
            CardActionEffect effect = rule.Effects[index];
            string id = index == 0 ? "buff" : index == 1 ? "poison" : "armor";
            int stacks = index == 0 ? 2 : index == 1 ? 1 : 30;
            if (effect.Type != "AddStatus" || effect.Target != "DropTargetCharacter" || !effect.AllowEnemy || !effect.AllowPlayer ||
                effect.Value != 0 || effect.Statuses.Count != 1 || effect.Statuses[0].Id != id || effect.Statuses[0].Stacks != stacks)
                return false;
        }
        return true;
    }
    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Equal(string expected, string actual, string label)
    { string? difference = ModelJson.Difference(expected, actual); Require(difference == null, label + ": " + difference); }
    private static void Equal<T>(T expected, T actual, string label)
    { string? difference = ModelJson.Difference(Serialize(expected), Serialize(actual)); Require(difference == null, label + ": " + difference); }
    private static void Require(bool value, string message) { if (!value) throw new InvalidDataException(message); }
}
