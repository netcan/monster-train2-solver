using System.Text;
using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;
using MonsterTrain2Poju.Capture;

internal static class FixtureArchiveChecks
{
    internal static void Run()
    {
        const string sample = """
            {"null":null,"empty":[],"emptyObject":{},"unicode":"列车🚂\u0000",
             "duplicate":1,"duplicate":2,"numbers":[-2147483648,4294967295,-9223372036854775808,18446744073709551615,-0,1e3,0.10000000000000000000000001],
             "Action":{"CardInstanceId":3,"RoomIndex":2},"Rng":{"S0":4294967295,"S1":0,"S2":2147483648,"S3":7},
             "Status":{"Id":"armor","Stacks":4},"shared":[{"x":[1,2,3]}, {"x":[1,2,3]}]}
            """;
        using var imported = LegacyFixtureImport.Read(new MemoryStream(Encoding.UTF8.GetBytes(sample)));
        byte[] encoded = Encode(imported);
        Require(encoded.AsSpan().SequenceEqual(Encode(imported)), "Archive encoding is not deterministic.");
        using var decoded = FixtureDocument.Read(new MemoryStream(encoded));
        var value = decoded.RootElement;
        Require(imported.SourceSha256 == decoded.SourceSha256 && imported.SourceLength == decoded.SourceLength &&
            imported.RootElement.ContentEquals(value), "Binary graph lost values or source provenance.");
        Require(value.GetProperty("duplicate").GetInt32() == 2 && value.EnumerateObject().Count(p => p.Name == "duplicate") == 2 &&
            value.GetProperty("unicode").GetString() == "列车🚂\0", "Object ordering/duplicate keys or Unicode changed.");
        Require(value.GetProperty("numbers")[0].GetInt32() == int.MinValue && value.GetProperty("numbers")[1].GetUInt32() == uint.MaxValue &&
            value.GetProperty("numbers")[2].GetInt64() == long.MinValue && value.GetProperty("numbers")[3].GetUInt64() == ulong.MaxValue &&
            value.GetProperty("numbers")[5].GetDouble() == 1000 && value.GetProperty("shared")[0].SharesNodeWith(value.GetProperty("shared")[1]),
            "Integer widths, exponent value or structural deduplication changed.");
        using var json = JsonDocument.Parse(sample); // Independent legacy hydration oracle, only for migration checks.
        foreach (var name in new[] { "Action", "Rng", "Status" })
        {
            string legacy = name switch
            {
                "Action" => JsonSerializer.Serialize(json.RootElement.GetProperty(name).Deserialize<PlayCardAction>()),
                "Rng" => JsonSerializer.Serialize(json.RootElement.GetProperty(name).Deserialize<UnityRng>(ModelJson.Options)),
                _ => JsonSerializer.Serialize(json.RootElement.GetProperty(name).Deserialize<CombatStatus>())
            };
            string binary = name switch
            {
                "Action" => JsonSerializer.Serialize(value.GetProperty(name).Deserialize<PlayCardAction>()),
                "Rng" => JsonSerializer.Serialize(value.GetProperty(name).Deserialize<UnityRng>()),
                _ => JsonSerializer.Serialize(value.GetProperty(name).Deserialize<CombatStatus>())
            };
            Require(legacy == binary, "Binary constructor/default hydration differs for " + name);
        }
        Parallel.For(0, 32, _ => Require(value.GetProperty("Rng").Deserialize<UnityRng>().S0 == uint.MaxValue,
            "Shared immutable graph hydration changed across branches."));
        var builder = new FixtureBuilder();
        int atom = builder.Add(FixtureKind.Number, "7"); int[] children = [atom];
        int root = builder.Add(FixtureKind.Array, children: children);
        children[0] = 999;
        using var owned = builder.Build(root, 0, new byte[32]);
        Require(owned.RootElement[0].GetInt32() == 7, "Builder retained a caller-owned reference collection.");
        byte[] version = (byte[])encoded.Clone(); version[8] = 99; Reject(version, "Unknown format version was accepted.");
        byte[] digest = (byte[])encoded.Clone(); digest[60] ^= 1; Reject(digest, "Corrupt payload digest was accepted.");
        Reject(encoded[..^4], "Truncated archive was accepted.");
        Reject([.. encoded, 0], "Trailing archive bytes were accepted.");
        var invalid = new FixtureBuilder(); int invalidRoot = invalid.Add(FixtureKind.Array, children: [0]);
        using var cyclic = invalid.Build(invalidRoot, 0, new byte[32]);
        Reject(Encode(cyclic), "Cyclic/forward graph reference was accepted.");
        CheckNativeCapture();
        Console.WriteLine("FIXTURE-ARCHIVE-CHECKS PASS: typed values, integer boundaries, exact numeric lexemes, duplicate/order/null/Unicode preservation, shared-node deduplication, deterministic archives, direct constructor defaults, 32 parallel hydrations and corruption rejection.");
    }
    private static void CheckNativeCapture()
    {
        var shared = new CaptureSample();
        var snapshot = new
        {
            Shared = new[] { shared, shared },
            Equivalent = new CaptureSample(),
            Integers = new object[] { int.MinValue, uint.MaxValue, long.MinValue, ulong.MaxValue },
            Reals = new object[] { 1f, -0.0d, 0.125f, 0.10000000000000000000000001m, double.NaN },
            Enum = FixtureKind.Object,
            Guid = new Guid("d14a50f3-728d-43e1-87f0-ef1b013f6678"),
            Bytes = new byte[] { 0, 127, 255 },
            Date = new DateTimeOffset(2026, 10, 7, 8, 9, 10, TimeSpan.FromHours(8)),
            Dictionary = new Dictionary<string, object?> { ["列车"] = null, ["empty"] = Array.Empty<int>() }
        };
        using var direct = NativeFixtureCapture.Capture(snapshot);
        Require(shared.Reads == 1, "Native capture revisited a shared object instance.");
        string json = Newtonsoft.Json.JsonConvert.SerializeObject(snapshot);
        using var legacy = LegacyFixtureImport.Read(new MemoryStream(Encoding.UTF8.GetBytes(json)));
        foreach (var property in legacy.RootElement.EnumerateObject())
            Require(direct.RootElement.GetProperty(property.Name).ContentEquals(property.Value), "Native object contracts differ from the independent JSON capture: " + property.Name);
        Require(direct.RootElement.ContentEquals(legacy.RootElement), "Native object property order differs from the independent JSON capture.");
        using var decoded = FixtureDocument.Read(new MemoryStream(Encode(direct)));
        Require(!decoded.HasTextSource && decoded.SourceLength == 0 && decoded.SourceSha256 == new string('0', 64) &&
            decoded.RootElement.ContentEquals(legacy.RootElement), "Direct archive values or binary-only provenance changed.");
        Require(decoded.RootElement.GetProperty("Shared")[0].SharesNodeWith(decoded.RootElement.GetProperty("Equivalent")),
            "Equivalent native objects were not structurally deduplicated.");
        var inspection = (IDictionary<string, object?>)decoded.RootElement.ToObjectGraph()!;
        var array = (object?[])inspection["Shared"]!;
        Require(ReferenceEquals(array[0], array[1]) && ReferenceEquals(array[0], inspection["Equivalent"]) &&
            (ulong)((object?[])inspection["Integers"]!)[3]! == ulong.MaxValue, "Launcher inspection expanded shared nodes or lost unsigned integers.");
        var cycle = new Dictionary<string, object?>(); cycle["self"] = cycle;
        try { using var invalid = NativeFixtureCapture.Capture(cycle); throw new InvalidOperationException("Native capture accepted a cycle."); }
        catch (InvalidOperationException error) when (error.Message.StartsWith("Cyclic native capture graph", StringComparison.Ordinal)) { }
        Console.WriteLine("NATIVE-FIXTURE-CAPTURE PASS: native contracts match JSON values and exact number spellings; shared references visited once; binary-only provenance, Unicode/nulls, ignored properties, structural deduplication, direct inspection and cycles verified.");
    }
    private sealed class CaptureSample
    {
        [Newtonsoft.Json.JsonIgnore] public int Reads { get; private set; }
        public string Text { get { Reads++; return "列车🚂\0"; } }
        [Newtonsoft.Json.JsonIgnore] public string Ignored => "not captured";
        public int[] Values { get; } = [1, 2, 3];
    }
    private static byte[] Encode(FixtureDocument document)
    {
        using var output = new MemoryStream(); document.Write(output); return output.ToArray();
    }
    private static void Reject(byte[] bytes, string message)
    {
        try { using var invalid = FixtureDocument.Read(new MemoryStream(bytes)); }
        catch (InvalidDataException) { return; }
        throw new InvalidOperationException(message);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
