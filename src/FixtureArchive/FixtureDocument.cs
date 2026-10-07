using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace MonsterTrain2Poju.Fixtures;

public sealed class FixtureDocument : IDisposable
{
    private static readonly byte[] magic = "MT2FDAG\0"u8.ToArray();
    private const int Version = 1;
    private readonly ValueNode[] nodes;
    private readonly int rootId;
    private readonly byte[] sourceHash;
    public long SourceLength { get; }
    public string SourceSha256 => Convert.ToHexString(sourceHash).ToLowerInvariant();
    public int UniqueNodeCount => nodes.Length;
    public FixtureValue RootElement => new(nodes, rootId);
    internal FixtureDocument(ValueNode[] nodes, int rootId, long sourceLength, byte[] sourceHash)
    {
        if (sourceLength < 0 || sourceHash.Length != 32 || (uint)rootId >= (uint)nodes.Length) throw new InvalidDataException("Invalid fixture metadata.");
        this.nodes = nodes; this.rootId = rootId; SourceLength = sourceLength; this.sourceHash = (byte[])sourceHash.Clone();
    }
    public void Dispose() { } // Values remain immutable and do not own a file handle.
    public void Write(Stream destination)
    {
        using var payload = new MemoryStream();
        using (var writer = new BinaryWriter(payload, Encoding.UTF8, true))
        {
            var strings = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var node in nodes)
                if (node.Text != null && !strings.ContainsKey(node.Text)) strings.Add(node.Text, strings.Count);
            writer.Write7BitEncodedInt(strings.Count);
            foreach (string text in strings.Keys) writer.Write(text);
            writer.Write7BitEncodedInt(nodes.Length); writer.Write7BitEncodedInt(rootId);
            foreach (var node in nodes)
            {
                writer.Write((byte)node.Kind);
                if (node.Kind is FixtureKind.Object or FixtureKind.Array)
                {
                    writer.Write7BitEncodedInt(node.Children.Length);
                    foreach (int child in node.Children) writer.Write7BitEncodedInt(child);
                }
                else if (node.Kind == FixtureKind.String) writer.Write7BitEncodedInt(strings[node.Text!]);
                else if (node.Kind == FixtureKind.Number) WriteNumber(writer, node.Text!, strings);
            }
        }
        byte[] bytes = payload.ToArray();
        using var compressed = new MemoryStream();
        using (var brotli = new BrotliStream(compressed, CompressionLevel.SmallestSize, true)) brotli.Write(bytes);
        using var header = new BinaryWriter(destination, Encoding.UTF8, true);
        header.Write(magic); header.Write(Version); header.Write(SourceLength); header.Write(sourceHash);
        header.Write((long)bytes.Length); header.Write(SHA256.HashData(bytes)); header.Write(compressed.Length);
        compressed.Position = 0; compressed.CopyTo(destination);
    }
    public static FixtureDocument Read(string path)
    {
        using var file = File.OpenRead(path); return Read(file);
    }
    public static FixtureDocument Read(Stream input)
    {
        try { return ReadCore(input); }
        catch (Exception error) when (error is EndOfStreamException or FormatException or OverflowException or ArgumentException or DecoderFallbackException)
        { throw new InvalidDataException("Malformed binary fixture.", error); }
    }
    private static FixtureDocument ReadCore(Stream input)
    {
        using var header = new BinaryReader(input, Encoding.UTF8, true);
        if (!header.ReadBytes(magic.Length).AsSpan().SequenceEqual(magic)) throw new InvalidDataException("Not an MT2 binary fixture.");
        if (header.ReadInt32() != Version) throw new InvalidDataException("Unsupported fixture archive version.");
        long sourceLength = header.ReadInt64(); byte[] sourceHash = Exact(header, 32);
        long payloadLength = header.ReadInt64(); byte[] payloadHash = Exact(header, 32); long compressedLength = header.ReadInt64();
        if (sourceLength < 0 || payloadLength <= 0 || payloadLength > 1_073_741_824 || compressedLength <= 0 || compressedLength > 1_073_741_824)
            throw new InvalidDataException("Invalid archive lengths.");
        byte[] compressed = Exact(header, checked((int)compressedLength));
        if (input.ReadByte() != -1) throw new InvalidDataException("Trailing archive bytes.");
        byte[] payload = new byte[checked((int)payloadLength)];
        using (var brotli = new BrotliStream(new MemoryStream(compressed, false), CompressionMode.Decompress))
        {
            brotli.ReadExactly(payload);
            if (brotli.ReadByte() != -1) throw new InvalidDataException("Unexpected decompressed bytes.");
        }
        if (!CryptographicOperations.FixedTimeEquals(SHA256.HashData(payload), payloadHash)) throw new InvalidDataException("Fixture payload SHA-256 mismatch.");
        using var reader = new BinaryReader(new MemoryStream(payload, false), new UTF8Encoding(false, true));
        int stringCount = Count(reader, payload.Length);
        var strings = new string[stringCount];
        for (int i = 0; i < strings.Length; i++) strings[i] = reader.ReadString();
        int nodeCount = Count(reader, payload.Length), rootId = reader.Read7BitEncodedInt();
        var nodes = new ValueNode[nodeCount];
        var depths = new int[nodeCount];
        for (int i = 0; i < nodes.Length; i++)
        {
            var kind = (FixtureKind)reader.ReadByte();
            if (kind is < FixtureKind.Object or > FixtureKind.Null) throw new InvalidDataException("Unknown fixture node kind.");
            string? text = null; int[] children = [];
            if (kind is FixtureKind.Object or FixtureKind.Array)
            {
                int count = Count(reader, payload.Length);
                if (kind == FixtureKind.Object && count % 2 != 0) throw new InvalidDataException("Incomplete object property.");
                children = new int[count];
                for (int j = 0; j < count; j++)
                {
                    int child = reader.Read7BitEncodedInt();
                    if ((uint)child >= (uint)i) throw new InvalidDataException("Forward or invalid node reference.");
                    if (kind == FixtureKind.Object && j % 2 == 0 && nodes[child].Kind != FixtureKind.String)
                        throw new InvalidDataException("Object key is not a string.");
                    children[j] = child; depths[i] = Math.Max(depths[i], depths[child] + 1);
                }
                if (depths[i] > 128) throw new InvalidDataException("Fixture nesting exceeds version-one limit.");
            }
            else if (kind == FixtureKind.String) text = String(reader, strings);
            else if (kind == FixtureKind.Number) text = ReadNumber(reader, strings);
            nodes[i] = new(kind, text, children);
        }
        if (reader.BaseStream.Position != payload.Length) throw new InvalidDataException("Trailing fixture payload.");
        return new(nodes, rootId, sourceLength, sourceHash);
    }
    private static byte[] Exact(BinaryReader reader, int length)
    {
        byte[] bytes = reader.ReadBytes(length);
        if (bytes.Length != length) throw new EndOfStreamException();
        return bytes;
    }
    private static int Count(BinaryReader reader, int bound)
    {
        int count = reader.Read7BitEncodedInt();
        if (count < 0 || count > bound) throw new InvalidDataException("Invalid table length.");
        return count;
    }
    private static string String(BinaryReader reader, string[] strings)
    {
        int id = reader.Read7BitEncodedInt();
        return (uint)id < (uint)strings.Length ? strings[id] : throw new InvalidDataException("Invalid string reference.");
    }
    private static void WriteNumber(BinaryWriter writer, string text, Dictionary<string, int> strings)
    {
        var invariant = CultureInfo.InvariantCulture;
        if (long.TryParse(text, NumberStyles.Integer, invariant, out long signed) && signed.ToString(invariant) == text)
        { writer.Write((byte)0); writer.Write7BitEncodedInt64(unchecked((signed << 1) ^ (signed >> 63))); }
        else if (ulong.TryParse(text, NumberStyles.Integer, invariant, out ulong unsigned) && unsigned.ToString(invariant) == text)
        { writer.Write((byte)1); writer.Write7BitEncodedInt64(unchecked((long)unsigned)); }
        else if (double.TryParse(text, NumberStyles.Float, invariant, out double real) && double.IsFinite(real) && real.ToString("R", invariant) == text)
        { writer.Write((byte)2); writer.Write(real); }
        else // Preserve unusual/exponent/decimal lexemes exactly rather than rounding them.
        { writer.Write((byte)3); writer.Write7BitEncodedInt(strings[text]); }
    }
    private static string ReadNumber(BinaryReader reader, string[] strings)
    {
        var invariant = CultureInfo.InvariantCulture;
        switch (reader.ReadByte())
        {
            case 0:
                ulong encoded = unchecked((ulong)reader.Read7BitEncodedInt64());
                return unchecked((long)(encoded >> 1) ^ -((long)encoded & 1)).ToString(invariant);
            case 1: return unchecked((ulong)reader.Read7BitEncodedInt64()).ToString(invariant);
            case 2:
                double value = reader.ReadDouble();
                if (!double.IsFinite(value)) throw new InvalidDataException("Nonfinite fixture number.");
                return value.ToString("R", invariant);
            case 3: return String(reader, strings);
            default: throw new InvalidDataException("Unknown numeric encoding.");
        }
    }
}
