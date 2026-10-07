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
        var hash = new HashCode(); hash.Add(kind); hash.Add(text, StringComparer.Ordinal);
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
    internal string Number() { Require(FixtureKind.Number); return Node.Text!; }
    public T? Deserialize<T>() => (T?)FixtureHydrator.Read(this, typeof(T));
    public bool SharesNodeWith(FixtureValue other) => ReferenceEquals(nodes, other.nodes) && Id == other.Id;
    public bool ContentEquals(FixtureValue other)
    {
        if (ValueKind != other.ValueKind) return false;
        if (ReferenceEquals(nodes, other.nodes) && Id == other.Id) return true;
        if (ValueKind is FixtureKind.Array or FixtureKind.Object)
        {
            if (Node.Children.Length != other.Node.Children.Length) return false;
            for (int i = 0; i < Node.Children.Length; i++)
                if (!new FixtureValue(nodes, Node.Children[i]).ContentEquals(new(other.nodes, other.Node.Children[i]))) return false;
            return true;
        }
        return Node.Text == other.Node.Text;
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
