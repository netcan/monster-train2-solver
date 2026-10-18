using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalGrosMichelChecks
{
    private static CombatRelicState Owner(int chance = 10) => new(RelicEndCombatModel.GrosMichelId, RelicEndCombatModel.GrosMichelKey,
        [RelicModel.ModifyCardAttack, RelicEndCombatModel.GrosMichelDestroy, RelicModel.BalatroBananaShake],
        isCovenant: false, disallowedInPlacementPhase: false,
        resourceEffects: [new(1, RelicEndCombatModel.GrosMichelDestroy, chance, 2, chance, false)],
        cardAttackModifiers: [new(0, "", "", 10, 0, 0, "Monster", false, false, true, false, appliedCardAttacks: [])]);
    internal static void Run()
    {
        UnityRng hit = Enumerable.Range(1, 10000).Select(UnityRng.Seed).First(rng => rng.Range(0, 100).Value < 10);
        UnityRng miss = Enumerable.Range(1, 10000).Select(UnityRng.Seed).First(rng => rng.Range(0, 100).Value >= 10);
        CombatContext Root(int count, UnityRng rng) => new(new([], [], [], rng, 0, []), rng, 0, 1, 10, [],
            cardInstances: [], relics: [Owner()], queryFrame: new(grosMichelCombatRoundCount: count));
        foreach (int count in new[] { 0, 1, 2 })
        {
            CombatContext root = Root(count, hit); string parent = JsonSerializer.Serialize(root, ModelJson.Options);
            void Replay()
            {
                CombatContext result = RelicEndCombatModel.Apply(root, out string? error);
                Require(error == null && result.Relics!.Count == (count < 2 ? 1 : 0) &&
                    result.QueryFrame!.GrosMichelCombatRoundCount == Math.Min(2, count + 1) &&
                    result.BattleRng.Equals(hit.Range(0, 100).State), "Low-roll waiting/hit, saved count or one RNG draw differs.");
            }
            Replay(); Parallel.For(0, 16, _ => Replay());
            Require(JsonSerializer.Serialize(root, ModelJson.Options) == parent, "Gros Michel mutated its parent.");
        }
        CombatContext removed = RelicEndCombatModel.Apply(Root(2, hit), out _);
        CombatContext kept = RelicEndCombatModel.Apply(Root(2, miss), out string? error);
        Require(error == null && kept.Relics!.Count == 1 && kept.QueryFrame!.GrosMichelCombatRoundCount == 2 &&
            kept.BattleRng.Equals(miss.Range(0, 100).State), "Mature miss changed owner/count or skipped RNG.");
        Equal(RelicEndCombatModel.Apply(removed, out _), removed, "removed owner");
        Require(kept.QueryFrame.With(energy: 2).GrosMichelCombatRoundCount == 2 && RelicEndCombatModel.Validate(Owner(9)) != null &&
            RelicEndCombatModel.Validate(Root(0, hit).WithQueryFrame(new())) != null, "Missing counter, copied counter or changed payload validation failed.");
        Console.WriteLine("GROS-MICHEL-PURE PASS: low-roll waits, mature miss/hit, one draw per round, saved count after removal, payload rejection and 16 immutable branches.");
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 240 && root.GetProperty("OriginalGrosMichelPrepared").GetBoolean() &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Incomplete Gros Michel battle.");
        foreach (string field in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending", "OriginalGrosMichelCaptureFailures",
            "OriginalGrosMichelMismatches", "OriginalGrosMichelUnsupported", "OriginalGrosMichelPending" })
            Require(root.GetProperty(field).GetInt32() == 0, "Nonzero " + field);
        int tests = 0, attempts = 0, waits = 0, misses = 0, hits = 0;
        var counters = new List<int>();
        foreach (FixtureValue row in root.GetProperty("RelicGrosMichelRecords").EnumerateArray())
        {
            string kind = row.GetProperty("Kind").GetString()!;
            TrainCombatState before = row.GetProperty("Before").Deserialize<TrainCombatState>()!, after = row.GetProperty("Actual").Deserialize<TrainCombatState>()!;
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            void Replay()
            {
                TrainCombatState expected = before;
                if (kind == "Apply")
                {
                    CombatContext context = RelicEndCombatModel.Apply(before.Context!, out string? error);
                    Require(error == null, error!); expected = CardSpellModel.WithContext(before, context);
                }
                Equal(expected, after, kind);
            }
            Replay(); Parallel.For(0, 16, _ => Replay());
            Require(JsonSerializer.Serialize(before, ModelJson.Options) == parent, "Native Gros Michel branch mutated parent.");
            int[] rolls = row.GetProperty("Rolls").Deserialize<int[]>()!;
            string[] notifications = row.GetProperty("Notifications").Deserialize<string[]>()!;
            if (kind == "Test")
            { tests++; Require(row.GetProperty("Returned").GetBoolean() && rolls.Length == 0 && notifications.Length == 0, "Test must be side-effect free and true."); }
            else if (kind == "Attempt")
            { attempts++; Require(rolls.Length == 0 && notifications.SequenceEqual(["Attempted"]), "Banana Shake attempt differs, including after destruction."); }
            else
            {
                Require(kind == "Apply" && rolls.Length == 1, "Destroy callback must draw exactly once.");
                int count = before.Context!.QueryFrame!.GrosMichelCombatRoundCount!.Value; counters.Add(count);
                Require(before.Context.BattleRng.Range(0, 100).Value == rolls[0], "Native destruction roll differs.");
                if (count < 2)
                { waits++; Require(rolls[0] < 10 && notifications.Length == 0 && after.Context!.QueryFrame!.GrosMichelCombatRoundCount == count + 1, "Waiting low roll must advance the count without firing."); }
                else if (rolls[0] >= 10)
                { misses++; Require(notifications.Length == 0, "Miss unexpectedly notified a trigger."); }
                else
                {
                    hits++; Require(notifications.SequenceEqual(["Triggered"]) && after.Context!.Relics!.All(owner => owner.DataId != RelicEndCombatModel.GrosMichelId) &&
                        after.Context.QueryFrame!.GrosMichelCombatRoundCount == 2, "Hit owner/counter/notification transition differs.");
                    CombatUnit[] live = before.Rooms.SelectMany(room => room.Units).Where(unit => unit.Team == CombatTeam.Player && unit.SpawnerCardId > 0).ToArray();
                    Require(live.Length == 2 && live.All(unit => unit.Attack == 40 && after.Rooms.SelectMany(room => room.Units).Single(other => other.Id == unit.Id).Attack == 40), "Deck undo changed existing units.");
                    foreach (var contribution in before.Context.Relics!.Single(owner => owner.DataId == RelicEndCombatModel.GrosMichelId).CardAttackModifiers!.Single().AppliedCardAttacks!.Where(item => item.InDeckState))
                        Require(after.Context.FindCard(contribution.CardInstanceId)!.Temporary.Offsets.Damage == before.Context.FindCard(contribution.CardInstanceId)!.Temporary.Offsets.Damage - contribution.Amount, "Tracked deck undo differs.");
                }
            }
        }
        Require(tests == 4 && waits == 2 && misses == 1 && hits == 1 && attempts == 4 && counters.SequenceEqual([0, 1, 2, 2]), "Missing natural waiting/waiting/miss/hit lifecycle.");
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(actions.Length == 3 && actions.All(row => !row.GetProperty("ScenarioAction").GetBoolean()), "Expected three model-selected policy plays.");
        var late = actions.Last().GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(late.Spawn.Train.Context!.Relics!.All(owner => owner.DataId != RelicEndCombatModel.GrosMichelId) &&
            late.Spawn.Train.Rooms.Single(room => room.RoomIndex == 2).Units.Any(unit => unit.Team == CombatTeam.Player && unit.SpawnerCardId > 0 && unit.Attack == 30), "Post-removal birth did not lose +10 attack.");
        var modifiers = root.GetProperty("RelicCardModifiers").EnumerateArray().ToArray();
        foreach (FixtureValue row in modifiers)
        {
            CardInstanceState before = row.GetProperty("Before").Deserialize<CardInstanceState>()!;
            CombatContext context = row.GetProperty("ContextBefore").Deserialize<CombatContext>()!;
            CombatContext actual = row.GetProperty("ContextAfter").Deserialize<CombatContext>()!;
            void Replay()
            {
                RelicCardModifierResult result = RelicCardModifierModel.Apply(before, context.Relics, row.GetProperty("ResetTemporary").GetBoolean(), context);
                Require(result.Supported, result.UnsupportedReason!);
                Equal(result.Card, row.GetProperty("Actual").Deserialize<CardInstanceState>(), "native card modifier");
                Equal(result.Context!.Relics, actual.Relics, "native contribution tracking");
                Equal(result.Context.Statistics, actual.Statistics, "native modifier statistic cache");
                Equal(result.Dispatches, row.GetProperty("Dispatches").Deserialize<RelicCardModifierDispatch[]>(), "native modifier dispatch");
            }
            Replay(); Parallel.For(0, 16, _ => Replay());
        }
        var kinds = modifiers.Select(row => row.GetProperty("Before").Deserialize<CardInstanceState>()!.MaskDescriptor!.Definition)
            .Select(definition => definition.IsChampionCard == true ? "Champion" : definition.CardType).GroupBy(kind => kind).ToDictionary(group => group.Key, group => group.Count());
        Require(modifiers.Length == 6 && kinds.GetValueOrDefault("Champion") == 2 && kinds.GetValueOrDefault("Monster") == 2 && kinds.GetValueOrDefault("Spell") == 2, "Missing accumulated/reset Monster, Champion or Spell controls.");
        Console.WriteLine("ORIGINAL-GROS-MICHEL-NATIVE PASS: low-roll waits, mature miss/hit, four draws, persistent count, deck undo without live-unit undo, later birth, six modifier controls and 16 immutable branches.");
    }
    private static void Equal<T>(T expected, T actual, string label)
    { string a = JsonSerializer.Serialize(expected, ModelJson.Options), b = JsonSerializer.Serialize(actual, ModelJson.Options); Require(a == b, label + ": " + ModelJson.Difference(a, b)); }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
