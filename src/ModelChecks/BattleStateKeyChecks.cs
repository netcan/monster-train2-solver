using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class BattleStateKeyChecks
{
    internal static void NativePair(FixtureValue source, FixtureValue replay)
    {
        FloorBrowseAdviceChecks.NativePair(source, replay);
        OrdinaryAdviceChecks.NativeReuse(source, replay);
        if (!source.GetProperty("NativeWon").GetBoolean() || !replay.GetProperty("NativeWon").GetBoolean())
            throw new InvalidOperationException("Optimized state-key acceptance requires complete native victories.");
        Console.WriteLine("PLAYER-STATE-KEY PASS: optimized Unity/Mono player, complete native forecasts/state/RNG and independent replay; ordinary continuation and floor browsing preserved.");
    }

    internal static void Run(FixtureValue fixture)
    {
        // Compare deployment, ordinary combat and boss turns against the same
        // schema encoded independently. New captured properties can change
        // old snapshots' keys even when the encoding itself is unchanged.
        int[] indexes = [0, 7, 14, 24];
        BattleTurnState[] states = indexes.Select(index => fixture.GetProperty("RollingDecisions")[index]
            .GetProperty("Root").Deserialize<BattleTurnState>()!).ToArray();
        string[][] expected = states.Select(state => Enumerable.Range(0, 3).Select(mode => ReferenceKey(state, mode)).ToArray()).ToArray();
        Parallel.For(0, states.Length * 3 * 2, new ParallelOptions { MaxDegreeOfParallelism = 2 }, sample =>
        {
            int index = sample / 3 % states.Length, mode = sample % 3;
            using var keys = new BattleStateKeys(mode > 0, mode == 2);
            string actual = keys.Key(states[index]);
            if (actual != expected[index][mode])
                throw new InvalidOperationException($"State key differs from reference encoding: sample={index}, mode={mode}, actual={actual}.");
        });
        CheckRandomTargetMetadata(states[0]);
        using (var keys = new BattleStateKeys())
        {
            if (keys.SnapshotKey(new EncodingWitness()) != "DAEA4C806CCE2A479A5155D396B8BE8CCD1EF7B83C0509C4E34FE09C2A16E8C6")
                throw new InvalidOperationException("UTF-8, surrogate fallback or large-buffer key encoding changed.");
            if (keys.SnapshotKey(1.0m) == keys.SnapshotKey(1.00m) ||
                keys.SnapshotKey(0d) == keys.SnapshotKey(-0d) || keys.SnapshotKey(0f) == keys.SnapshotKey(-0f))
                throw new InvalidOperationException("Scalar reuse lost decimal scale or floating-point signed zero.");
        }
        long allocated = GC.GetAllocatedBytesForCurrentThread();
        int collections = GC.CollectionCount(0);
        var watch = Stopwatch.StartNew();
        for (int index = 0; index < 20; index++)
        {
            using var keys = new BattleStateKeys();
            if (keys.Key(states[0]) != expected[0][0]) throw new InvalidOperationException("A repeated capture changed its key.");
        }
        Console.WriteLine($"STATE-KEY PASS: 12 independent reference keys, absent/false/true card targeting metadata, frozen Unicode digest, concurrent instances, all three modes; " +
            $"20 warm fresh-root keys={watch.Elapsed.TotalMilliseconds:F1}ms " +
            $"allocatedBytes={GC.GetAllocatedBytesForCurrentThread() - allocated} gc0={GC.CollectionCount(0) - collections}.");
    }

    private static void CheckRandomTargetMetadata(BattleTurnState source)
    {
        EnemySpawnState spawn = source.Spawn;
        CombatContext context = spawn.Train.Context!;
        CardInstanceState card = context.CardInstances!.First(item => item.MaskDescriptor?.Definition.CardType == "Spell");
        CardMaskDescriptor mask = card.MaskDescriptor!;
        if (mask.RandomTargetsFromRelic.HasValue)
            throw new InvalidOperationException("Expected a legacy card without random-target metadata.");
        BattleTurnState[] states = [WithMask(mask), WithMask(mask.WithRandomTargets(false)), WithMask(mask.WithRandomTargets(true))];
        for (int mode = 0; mode < 3; mode++)
        {
            using var keys = new BattleStateKeys(mode > 0, mode == 2);
            string[] hashes = states.Select(keys.Key).ToArray();
            if (hashes[0] != keys.Key(source) || hashes.Distinct().Count() != states.Length ||
                !hashes.SequenceEqual(states.Select(state => ReferenceKey(state, mode))))
                throw new InvalidOperationException($"Optional random-target metadata changed key compatibility or identity in mode {mode}.");
        }

        BattleTurnState WithMask(CardMaskDescriptor descriptor)
        {
            TrainCombatState train = CardSpellModel.WithContext(spawn.Train, context.WithCard(card.WithMaskDescriptor(descriptor)));
            EnemySpawnState changed = new(train, spawn.Waves, spawn.SelectedGroups, spawn.Phase, spawn.Looping,
                spawn.Rng, spawn.NextUnitId, spawn.Treasures, spawn.TreasuresRemaining, spawn.TreasureEnabled,
                spawn.FirstTreasureTurn, spawn.FirstTreasureRoom, spawn.Turn, spawn.ExternalInteractions,
                spawn.CanonicalDecisionReferences, spawn.PendingDestroyedUnitIds, spawn.OuterBoss, spawn.QueuedOuterBoss,
                spawn.QueuedTrueFinalBoss, spawn.TrueFinalBoss);
            return new(changed, source.Energy, source.EnergyPerTurn, source.DrawPerTurn, source.ForgePoints, source.DragonsHoard,
                source.MoonPhase, source.RngStreams, source.OtherPiles, source.ExternalInteractions, source.PlayRules,
                source.BattlePreviewEnabled, source.UiRngIsolated, source.CanonicalDecisionReferences,
                source.CanonicalPhysicalReferences, source.SelectedRoom, source.MoonPhaseTrackingActive);
        }
    }

    private static string ReferenceKey(object snapshot, int mode)
    {
        var encodedObjects = new Dictionary<object, string>(ReferenceEqualityComparer.Instance);
        return Encode(snapshot);

        string Encode(object? value)
        {
            if (value == null) return "null";
            if (value is string text) return "s" + Frame(text);
            if (value is double number) return "double:" + number.ToString("R", CultureInfo.InvariantCulture);
            if (value is float single) return "float:" + single.ToString("R", CultureInfo.InvariantCulture);
            Type type = value.GetType();
            if (type.IsPrimitive || type.IsEnum || value is decimal)
                return type.FullName + ":" + (value is IFormattable formattable
                    ? formattable.ToString(null, CultureInfo.InvariantCulture) : value.ToString());
            if (encodedObjects.TryGetValue(value, out string? existing)) return existing;

            IEnumerable<string> tokens = value is IEnumerable sequence
                ? sequence.Cast<object?>().Select(Encode)
                : PropertyTokens(value, type);
            string content = type.FullName + "|" + string.Concat(tokens.Select(Frame));
            string key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content)));
            encodedObjects.Add(value, key);
            return key;
        }

        IEnumerable<string> PropertyTokens(object value, Type type)
        {
            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(item => item.CanRead && item.GetIndexParameters().Length == 0)
                .OrderBy(item => item.Name, StringComparer.Ordinal))
            {
                object? child = property.GetValue(value);
                if (child == null &&
                    (value is RelicResourceEffect && property.Name == nameof(RelicResourceEffect.LastKnownDeckCount) ||
                     value is CardMaskDescriptor && property.Name == nameof(CardMaskDescriptor.RandomTargetsFromRelic)))
                    continue;
                yield return property.Name;
                if (mode == 2 && child is int selected && selected >= 0 &&
                    (value is BattleTurnState && property.Name == nameof(BattleTurnState.SelectedRoom) ||
                     value is StatisticQueryFrame && property.Name == nameof(StatisticQueryFrame.SelectedRoomIndex)))
                    yield return "known-room-selection";
                else if (mode != 0 && value is BattleSpawnPoints points && property.Name == nameof(BattleSpawnPoints.NextPreviewCopyId) &&
                    child is int next && next > 0 && points.Groups.All(group => group.PreviewCopyId == null || group.PreviewCopyId.Value < next))
                    yield return "enabled-preview-copy-allocator";
                else yield return Encode(child);
            }
        }

        static string Frame(string token) => token.Length.ToString(CultureInfo.InvariantCulture) + ":" + token;
    }

    private sealed class EncodingWitness
    {
        public string Text => "\u7834\u5c40 \ud83c\udf19 \u0000 \ud800";
        public string Large { get; } = new string('\u754c', 24000);
        public string[] Sequence { get; } = ["", "a:b|c", "\udfff", "\u00e9"];
    }
}
