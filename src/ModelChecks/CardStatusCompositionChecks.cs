using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class CardStatusCompositionChecks
{
    private static UpgradeMaskStatus Status(string id, int count, bool permanent = false) => new(id, count, permanent);
    private static CardStatusCompositionState State(UpgradeMaskStatus[] starting, UpgradeMaskStatus[][]? permanent = null,
        UpgradeMaskStatus[][]? temporary = null, bool purified = false, bool supported = true) =>
        new(starting, permanent ?? [], temporary ?? [], purified, supported);
    internal static void Run()
    {
        var source = State([Status("armor", 1), Status("poison", 1)], [[Status("armor", -1)]], [[Status("armor", 2)]]);
        var result = CardStatusCompositionModel.Resolve(source);
        Require(result.Select(status => status.Id).SequenceEqual(["poison", "armor"]) && result[1].Count == 2,
            "Discarded permanent zero groups survived into the temporary merge or retained the wrong position.");
        var grouped = CardStatusCompositionModel.Resolve(State([Status("armor", 2), Status("armor", 3, true)],
            [[Status("armor", -10), Status("armor", 1, true)]], [[Status("armor", 1)]]));
        Require(grouped.Count == 2 && grouped[0].FromPermanentUpgrade && grouped[0].Count == 4 &&
            !grouped[1].FromPermanentUpgrade && grouped[1].Count == 1, "Status source flags were combined or rewritten by their modifier container.");
        Require(CardStatusCompositionModel.Resolve(State([Status("armor", -5), Status("armor", 2)])).Single().Count == 2,
            "Status counts were clamped after the sequence instead of after each addition.");
        var overflow = CardStatusCompositionModel.Resolve(State([Status("armor", int.MaxValue), Status("armor", 1)], temporary: [[Status("armor", 10000)]]));
        Require(overflow.Single().Count == 10000, "Card status query arithmetic failed to wrap before clamping or acquired a unit status cap.");
        Require(CardStatusCompositionModel.Resolve(State([Status("armor", 9)], [[Status("poison", 3)]], purified: true)).Single().Id == "poison" &&
            CardStatusCompositionModel.Resolve(State([Status("armor", 9)], [[Status("poison", 3)]], supported: false)).Count == 0,
            "Purify incorrectly removed upgrades or a non-spawner query acquired spawn statuses.");
        var mutable = new[] { Status("armor", 2) }; var nested = new[] { new[] { Status("poison", 3) } };
        var isolated = State(mutable, nested); mutable[0] = Status("armor", -9); nested[0][0] = Status("poison", -9);
        string parent = JsonSerializer.Serialize(isolated);
        Parallel.For(0, 32, _ => Require(CardStatusCompositionModel.Resolve(isolated).Select(status => status.Count).SequenceEqual([2, 3]),
            "Card status composition shared mutable constructor inputs."));
        Require(JsonSerializer.Serialize(isolated) == parent, "Card status composition mutated its parent.");
        Console.WriteLine("CARD-STATUS-COMPOSITION-CHECKS PASS: source-flag grouping, per-addition wrap/clamp, two-stage zero removal/order, Purify, uncapped card queries and immutable parents.");
    }
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path); var root = document.RootElement;
        Require(root.GetProperty("Schema").GetInt32() == 1 && root.GetProperty("ContextUnchanged").GetBoolean() &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("FrameBefore").ContentEquals(root.GetProperty("FrameAfter")) && root.GetProperty("RngBefore").ContentEquals(root.GetProperty("RngAfter")),
            "Native card status calibration changed context/RNG/frame or game identity.");
        var rows = root.GetProperty("Rows").EnumerateArray().Select(row => (
            Name: row.GetProperty("Name").GetString()!, Kind: row.GetProperty("Kind").GetString()!,
            Input: row.GetProperty("Input").Deserialize<CardStatusCompositionState>()!, Returned: row.GetProperty("Returned").GetBoolean(),
            Actual: row.GetProperty("Actual").Deserialize<UpgradeMaskStatus[]>()!)).ToArray();
        Require(rows.Length == 1039 && rows.Count(row => row.Kind == "OwnedQuery") == 15 && rows.Count(row => row.Kind == "Controlled") == 1024 &&
            rows.Where(row => row.Kind == "Controlled").Select(row => row.Name).Distinct().Count() == 1024 &&
            rows.Count(row => row.Kind == "Controlled" && row.Input.Purified) == 512 &&
            rows.Any(row => row.Actual.Any(status => status.FromPermanentUpgrade)) && rows.Any(row => !row.Returned), "Native card status query scope differs.");
        string parent = JsonSerializer.Serialize(rows.Select(row => new { row.Input, row.Actual }));
        void Check()
        {
            foreach (var row in rows)
            {
                var predicted = CardStatusCompositionModel.Resolve(row.Input);
                Require(row.Input.SupportsSpawnStatuses == row.Returned && JsonSerializer.Serialize(predicted) == JsonSerializer.Serialize(row.Actual),
                    $"Native card status composition differs: {row.Kind}/{row.Name}, predicted={JsonSerializer.Serialize(predicted)}, actual={JsonSerializer.Serialize(row.Actual)}.");
            }
        }
        Check(); Parallel.For(0, 32, _ => Check());
        Require(JsonSerializer.Serialize(rows.Select(row => new { row.Input, row.Actual })) == parent, "Native card status composition replay mutated its parents.");
        Console.WriteLine("NATIVE-CARD-STATUS-COMPOSITION-CHECKS PASS: 1039 exact ordered status queries, 15 owned cards, 1024 controlled two-stage merges, source flags/Purify/overflow and 32 immutable branches.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
