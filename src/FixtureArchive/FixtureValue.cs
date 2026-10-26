using System.Globalization;

namespace MonsterTrain2Poju.Fixtures;

public enum FixtureKind : byte { Undefined, Object, Array, String, Number, True, False, Null }

internal sealed class ValueNode(FixtureKind kind, string? text, int[] children) : IEquatable<ValueNode>
{
    internal readonly FixtureKind Kind = kind;
    internal readonly string? Text = text;
    internal readonly int[] Children = children;
    private readonly int hash = Hash(kind, text, children);
    public bool Equals(ValueNode? other) => other != null && Kind == other.Kind && Text == other.Text && Children.AsSpan().SequenceEqual(other.Children);
    public override bool Equals(object? other) => other is ValueNode node && Equals(node);
    public override int GetHashCode() => hash;
    private static int Hash(FixtureKind kind, string? text, int[] children)
    {
        var hash = new HashCode(); hash.Add(kind);
        // Unity's Mono HashCode.Add(value, comparer) invokes the comparer even
        // for null; .NET's implementation guards it. Keep null container/boolean
        // node text valid on both runtimes.
        hash.Add(text == null ? 0 : StringComparer.Ordinal.GetHashCode(text));
        foreach (int child in children) hash.Add(child);
        return hash.ToHashCode();
    }
}

public readonly struct FixtureProperty(string name, FixtureValue value)
{
    public string Name { get; } = name;
    public FixtureValue Value { get; } = value;
}

// A view of immutable binary nodes. Reading or hydrating a model never parses JSON.
public readonly struct FixtureValue
{
    private readonly ValueNode[] nodes;
    internal readonly int Id;
    internal FixtureValue(ValueNode[] nodes, int id) { this.nodes = nodes; Id = id; }
    private ValueNode Node => nodes[Id];
    public FixtureKind ValueKind => nodes == null ? FixtureKind.Undefined : Node.Kind;
    public FixtureValue this[int index]
    {
        get { Require(FixtureKind.Array); return new(nodes, Node.Children[index]); }
    }
    public int GetArrayLength() { Require(FixtureKind.Array); return Node.Children.Length; }
    public IEnumerable<FixtureValue> EnumerateArray()
    {
        Require(FixtureKind.Array);
        foreach (int id in Node.Children) yield return new(nodes, id);
    }
    public IEnumerable<FixtureProperty> EnumerateObject()
    {
        Require(FixtureKind.Object);
        int[] children = Node.Children;
        for (int i = 0; i < children.Length; i += 2)
            yield return new(nodes[children[i]].Text!, new(nodes, children[i + 1]));
    }
    public bool TryGetProperty(string name, out FixtureValue value)
    {
        Require(FixtureKind.Object);
        int[] children = Node.Children;
        // Preserve the last-property-wins behavior for duplicate captured keys.
        for (int i = children.Length - 2; i >= 0; i -= 2)
            if (nodes[children[i]].Text == name) { value = new(nodes, children[i + 1]); return true; }
        value = default; return false;
    }
    public FixtureValue GetProperty(string name) => TryGetProperty(name, out var value) ? value : throw new KeyNotFoundException(name);
    public string? GetString()
    {
        if (ValueKind == FixtureKind.Null) return null;
        Require(FixtureKind.String); return Node.Text;
    }
    public bool GetBoolean() => ValueKind switch { FixtureKind.True => true, FixtureKind.False => false, _ => throw new InvalidOperationException("Expected boolean.") };
    public int GetInt32() => int.Parse(Number(), NumberStyles.Integer, CultureInfo.InvariantCulture);
    public long GetInt64() => long.Parse(Number(), NumberStyles.Integer, CultureInfo.InvariantCulture);
    public uint GetUInt32() => uint.Parse(Number(), NumberStyles.Integer, CultureInfo.InvariantCulture);
    public ulong GetUInt64() => ulong.Parse(Number(), NumberStyles.Integer, CultureInfo.InvariantCulture);
    public float GetSingle() => float.Parse(Number(), CultureInfo.InvariantCulture);
    public double GetDouble() => double.Parse(Number(), CultureInfo.InvariantCulture);
    public decimal GetDecimal() => decimal.Parse(Number(), NumberStyles.Float, CultureInfo.InvariantCulture);
    public string GetNumberText() => Number();
    internal string Number() { Require(FixtureKind.Number); return Node.Text!; }
#if NET8_0_OR_GREATER
    public T? Deserialize<T>() => (T?)FixtureHydrator.Read(this, typeof(T));

    // Launcher inspection shares repeated captured values instead of expanding
    // the graph into hundreds of megabytes of independently allocated objects.
    public object? ToObjectGraph()
    {
        var values = new Dictionary<int, object?>();
        object? Read(FixtureValue value)
        {
            if (values.TryGetValue(value.Id, out var found)) return found;
            object? result;
            if (value.ValueKind == FixtureKind.Object)
            {
                var properties = new Dictionary<string, object?>(StringComparer.Ordinal);
                foreach (var property in value.EnumerateObject()) properties[property.Name] = Read(property.Value);
                result = properties;
            }
            else if (value.ValueKind == FixtureKind.Array) result = value.EnumerateArray().Select(Read).ToArray();
            else if (value.ValueKind == FixtureKind.String) result = value.GetString();
            else if (value.ValueKind == FixtureKind.Number)
            {
                string number = value.Number();
                if (long.TryParse(number, NumberStyles.Integer, CultureInfo.InvariantCulture, out long signed)) result = signed;
                else if (ulong.TryParse(number, NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong unsigned)) result = unsigned;
                else result = double.Parse(number, NumberStyles.Float, CultureInfo.InvariantCulture);
            }
            else if (value.ValueKind is FixtureKind.True or FixtureKind.False) result = value.GetBoolean();
            else if (value.ValueKind == FixtureKind.Null) result = null;
            else throw new InvalidDataException("Undefined fixture value.");
            values.Add(value.Id, result);
            return result;
        }
        return Read(this);
    }
#endif
    public bool SharesNodeWith(FixtureValue other) => ReferenceEquals(nodes, other.nodes) && Id == other.Id;
    public bool ContentEquals(FixtureValue other)
    {
        if (ValueKind != other.ValueKind) return false;
        if (ReferenceEquals(nodes, other.nodes) && Id == other.Id) return true;
        if (ValueKind is not (FixtureKind.Array or FixtureKind.Object)) return Node.Text == other.Node.Text;
        var leftNodes = nodes; var rightNodes = other.nodes;
        bool sameGraph = ReferenceEquals(leftNodes, rightNodes);
        var equalPairs = new HashSet<(int Left, int Right)>();
        return Equal(Id, other.Id);

        bool Equal(int leftId, int rightId)
        {
            if (sameGraph && leftId == rightId) return true;
            ValueNode left = leftNodes[leftId], right = rightNodes[rightId];
            if (left.Kind != right.Kind) return false;
            if (left.Kind is not (FixtureKind.Array or FixtureKind.Object)) return left.Text == right.Text;
            if (left.Children.Length != right.Children.Length) return false;
            var pair = (leftId, rightId);
            if (equalPairs.Contains(pair)) return true;
            for (int i = 0; i < left.Children.Length; i++)
                if (!Equal(left.Children[i], right.Children[i])) return false;
            // A shared subtree is compared once per graph pair, rather than
            // once per expanded JSON path. Remember only fully checked pairs;
            // distinct documents, node pairs and calls never share this cache.
            equalPairs.Add(pair);
            return true;
        }
    }
    private void Require(FixtureKind kind)
    {
        if (ValueKind != kind) throw new InvalidOperationException($"Expected {kind}, received {ValueKind}.");
    }
}

public sealed class FixtureBuilder
{
    private readonly List<ValueNode> nodes = [];
    private readonly Dictionary<ValueNode, int> interned = [];
    private readonly Dictionary<string, string> strings = new(StringComparer.Ordinal);
    public int Import(FixtureValue root)
    {
        // This invocation imports one source graph. Its node IDs already form
        // complete keys; default value-type hashing of FixtureValue can put
        // every node's common array reference in the same hash bucket.
        var imported = new Dictionary<int, int>();
        int Copy(FixtureValue value)
        {
            if (imported.TryGetValue(value.Id, out int existing)) return existing;
            int id = value.ValueKind switch
            {
                FixtureKind.Object => Add(FixtureKind.Object, children: value.EnumerateObject().SelectMany(property => new[] { Add(FixtureKind.String, property.Name), Copy(property.Value) }).ToArray()),
                FixtureKind.Array => Add(FixtureKind.Array, children: value.EnumerateArray().Select(Copy).ToArray()),
                FixtureKind.String => Add(FixtureKind.String, value.GetString()),
                FixtureKind.Number => Add(FixtureKind.Number, value.Number()),
                _ => Add(value.ValueKind)
            };
            imported.Add(value.Id, id); return id;
        }
        return Copy(root);
    }
    public int Add(FixtureKind kind, string? text = null, int[]? children = null)
    {
        children ??= [];
        if (text != null)
        {
            if (strings.TryGetValue(text, out var existing)) text = existing;
            else strings.Add(text, text);
        }
        var node = new ValueNode(kind, text, children);
        if (interned.TryGetValue(node, out int id)) return id;
        // Own the collection before exposing a built immutable graph.
        node = new ValueNode(kind, text, (int[])children.Clone());
        id = nodes.Count; nodes.Add(node); interned.Add(node, id); return id;
    }
    public FixtureDocument Build(int rootId, long sourceLength, byte[] sourceHash) => new(nodes.ToArray(), rootId, sourceLength, sourceHash);
}
