using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class LinkedHealthChecks
{
    internal static void Native(FixtureValue root)
    {
        void Require(bool condition, string reason) => TrueFinalBossChecks.Require(condition, reason);
        string Json(object value) => JsonSerializer.Serialize(value, ModelJson.Options);
        Require(root.GetProperty("Schema").GetInt32() == 1 &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("Boundary").GetString() == "DetachedNativeStateInformationPropertiesAndPreviewCopy" &&
            root.GetProperty("OriginalStateReferencesRestored").GetBoolean() && root.GetProperty("ContextUnchanged").GetBoolean() &&
            root.GetProperty("WorldBefore").ContentEquals(root.GetProperty("WorldAfter")) &&
            root.GetProperty("RngBefore").ContentEquals(root.GetProperty("RngAfter")) &&
            root.GetProperty("RngBefore").GetProperty("Streams").GetArrayLength() == 16 &&
            root.GetProperty("RngBefore").GetProperty("Unity").GetArrayLength() == 4 &&
            root.GetProperty("FrameBefore").ContentEquals(root.GetProperty("FrameAfter")),
            "Linked-health native controls changed live state, frame or RNG, or omitted original reference restoration.");
        FixtureValue[] rows = root.GetProperty("Rows").EnumerateArray().ToArray();
        Require(rows.Length == 80, "Incomplete native linked-health matrix and dependent preview chain.");
        var matrix = new HashSet<(int Target, string Property, int Value)>();
        string? baseline = null, previous = null;
        int copies = 0, linkedWrites = 0, negativeWrites = 0, independentPreviewWrites = 0;
        foreach (FixtureValue row in rows)
        {
            LinkedHealthSnapshot before = row.GetProperty("Before").Deserialize<LinkedHealthSnapshot>()!;
            LinkedHealthSnapshot actual = row.GetProperty("Actual").Deserialize<LinkedHealthSnapshot>()!;
            string immutable = Json(before);
            string operation = row.GetProperty("Operation").GetString()!;
            string label = row.GetProperty("Label").GetString()!;
            int target = row.GetProperty("TargetId").GetInt32(), value = row.GetProperty("Value").GetInt32();
            string property = row.GetProperty("Property").GetString()!;
            Require(before.State.Actors.Select(actor => actor.Id).SequenceEqual(new[] { 1, 2, 3, 4, 5 }), "Native actor identity/order differs.");
            LinkedHealthResult result = operation == "CopyForPreview" ? LinkedHealthModel.CopyForPreview(before.State) :
                LinkedHealthModel.Write(before.State, target, property, value);
            Require(result.Supported, "Native linked health was rejected: " + result.UnsupportedReason);
            string? diff = ModelJson.Difference(Json(result.Snapshot!), Json(actual));
            Require(diff == null && Json(before) == immutable, "Native linked-health state/readouts differ or source mutated: " + label + ": " + diff);
            if (label == "matrix")
            {
                baseline ??= immutable;
                Require(baseline == immutable && matrix.Add((target, property, value)), "Native matrix baseline differs or row duplicated.");
                if (target >= 4) linkedWrites++;
                if (value < 0) negativeWrites++;
            }
            else
            {
                if (previous != null) Require(previous == immutable, "Native linked-health/preview sequence lost its previous successor.");
                previous = Json(actual);
            }
            if (operation == "CopyForPreview")
            {
                copies++;
                Require(actual.State.Actors.All(actor => actor.LinkedId == 0), "Native preview copy retained a link.");
                if (copies == 1)
                    Require(before.State.Actors.Count(actor => actor.LinkedId == 3) == 2 &&
                        actual.State.Actors.Skip(2).All(actor => actor.RawHealth == 3189 && actor.RawMaxHealth == 4001),
                        "Missing live-shared to independently stored preview-health witness.");
            }
            if (label.StartsWith("preview-", StringComparison.Ordinal)) independentPreviewWrites++;
        }
        var expected = (from target in Enumerable.Range(1, 5) from property in new[] { "Hp", "MaxHp" }
            from value in new[] { 17, 0, -3, 99999, 100000, int.MaxValue, int.MinValue } select (target, property, value)).ToHashSet();
        Require(matrix.SetEquals(expected) && copies == 2 && linkedWrites == 28 && negativeWrites == 20 && independentPreviewWrites == 3,
            "Native linked-health boundary lacks upper/lower limits, link forwarding or preview independence.");
        LinkedHealthState seed = rows[0].GetProperty("Before").Deserialize<LinkedHealthSnapshot>()!.State;
        string parent = Json(seed);
        var parallelRoots = rows.Select(row => row.GetProperty("Before").Deserialize<LinkedHealthSnapshot>()!).ToArray();
        var immutableRoots = parallelRoots.Select(root => Json(root)).ToArray();
        Parallel.For(0, 128, index =>
        {
            FixtureValue row = rows[index % rows.Length];
            LinkedHealthSnapshot before = parallelRoots[index % rows.Length];
            LinkedHealthResult result = row.GetProperty("Operation").GetString() == "CopyForPreview" ? LinkedHealthModel.CopyForPreview(before.State) :
                LinkedHealthModel.Write(before.State, row.GetProperty("TargetId").GetInt32(), row.GetProperty("Property").GetString()!, row.GetProperty("Value").GetInt32());
            Require(result.Supported && ModelJson.Difference(Json(result.Snapshot!), Json(row.GetProperty("Actual").Deserialize<LinkedHealthSnapshot>()!)) == null,
                "Concurrent linked-health branch differs from original native successor.");
        });
        Require(parallelRoots.Select(root => Json(root)).SequenceEqual(immutableRoots) &&
            Json(seed) == parent && !LinkedHealthModel.Write(seed, 999, "Hp", 1).Supported &&
            !LinkedHealthModel.Write(seed, 3, "Other", 1).Supported &&
            !LinkedHealthModel.CopyForPreview(new LinkedHealthState([new(1, 2, 10, 10, false)])).Supported &&
            !LinkedHealthModel.CopyForPreview(new LinkedHealthState([new(1, 1, 10, 10, false)])).Supported &&
            !LinkedHealthModel.CopyForPreview(new LinkedHealthState([new(1, 2, 10, 10, false), new(2, 1, 10, 10, false)])).Supported &&
            !LinkedHealthModel.CopyForPreview(new LinkedHealthState([new(1, 2, 10, 10, false), new(2, 3, 10, 10, false), new(3, 0, 10, 10, false)])).Supported &&
            !LinkedHealthModel.CopyForPreview(new LinkedHealthState([new(1, 0, 10, 10, false), new(1, 0, 10, 10, false)])).Supported &&
            TrueFinalBossChecks.Assets.All(asset => !OuterBossDefinitions.Supports(asset)),
            "Missing immutable source, malformed-link rejection or full-battle guard.");
        Console.WriteLine("LINKED-HEALTH PASS: 70 native property controls + 10 dependent writes/copies; shared owner, raw/effective HP, dirty flags, upper-only cap, preview detachment, 128 concurrent branches and unchanged live state/frame/RNG. Full battle remains guarded.");
    }
}
