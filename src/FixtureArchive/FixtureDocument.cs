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
    public string SourceSha256 => BitConverter.ToString(sourceHash).Replace("-", "").ToLowerInvariant();
    public bool HasTextSource => SourceLength != 0 || sourceHash.Any(value => value != 0);
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
            Write7Bit(writer, strings.Count);
            foreach (string text in strings.Keys) writer.Write(text);
            Write7Bit(writer, nodes.Length); Write7Bit(writer, rootId);
            foreach (var node in nodes)
            {
                writer.Write((byte)node.Kind);
                if (node.Kind is FixtureKind.Object or FixtureKind.Array)
                {
                    Write7Bit(writer, node.Children.Length);
                    foreach (int child in node.Children) Write7Bit(writer, child);
                }
                else if (node.Kind == FixtureKind.String) Write7Bit(writer, strings[node.Text!]);
                else if (node.Kind == FixtureKind.Number) WriteNumber(writer, node.Text!, strings);
            }
        }
        byte[] bytes = payload.ToArray();
        using var compressed = new MemoryStream();
        // Optimal is available in Unity's runtime as well as the offline tools.
        // Existing archives remain readable regardless of compression quality.
        using (var brotli = new BrotliStream(compressed, CompressionLevel.Optimal, true)) brotli.Write(bytes);
        using var header = new BinaryWriter(destination, Encoding.UTF8, true);
        header.Write(magic); header.Write(Version); header.Write(SourceLength); header.Write(sourceHash);
        header.Write((long)bytes.Length); header.Write(Digest(bytes)); header.Write(compressed.Length);
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
            int offset = 0, count;
            while (offset < payload.Length && (count = brotli.Read(payload, offset, payload.Length - offset)) != 0) offset += count;
            if (offset != payload.Length) throw new EndOfStreamException();
            if (brotli.ReadByte() != -1) throw new InvalidDataException("Unexpected decompressed bytes.");
        }
        if (!Digest(payload).AsSpan().SequenceEqual(payloadHash)) throw new InvalidDataException("Fixture payload SHA-256 mismatch.");
        using var reader = new BinaryReader(new MemoryStream(payload, false), new UTF8Encoding(false, true));
        int stringCount = Count(reader, payload.Length);
        var strings = new string[stringCount];
        for (int i = 0; i < strings.Length; i++) strings[i] = reader.ReadString();
        int nodeCount = Count(reader, payload.Length), rootId = Read7Bit(reader);
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
                    int child = Read7Bit(reader);
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
        int count = Read7Bit(reader);
        if (count < 0 || count > bound) throw new InvalidDataException("Invalid table length.");
        return count;
    }
    private static string String(BinaryReader reader, string[] strings)
    {
        int id = Read7Bit(reader);
        return (uint)id < (uint)strings.Length ? strings[id] : throw new InvalidDataException("Invalid string reference.");
    }
    private static void WriteNumber(BinaryWriter writer, string text, Dictionary<string, int> strings)
    {
        var invariant = CultureInfo.InvariantCulture;
        if (long.TryParse(text, NumberStyles.Integer, invariant, out long signed) && signed.ToString(invariant) == text)
        { writer.Write((byte)0); Write7Bit64(writer, unchecked((signed << 1) ^ (signed >> 63))); }
        else if (ulong.TryParse(text, NumberStyles.Integer, invariant, out ulong unsigned) && unsigned.ToString(invariant) == text)
        { writer.Write((byte)1); Write7Bit64(writer, unchecked((long)unsigned)); }
        else if (double.TryParse(text, NumberStyles.Float, invariant, out double real) && Finite(real) && real.ToString("R", invariant) == text)
        { writer.Write((byte)2); writer.Write(real); }
        else // Preserve unusual/exponent/decimal lexemes exactly rather than rounding them.
        { writer.Write((byte)3); Write7Bit(writer, strings[text]); }
    }
    private static string ReadNumber(BinaryReader reader, string[] strings)
    {
        var invariant = CultureInfo.InvariantCulture;
        switch (reader.ReadByte())
        {
            case 0:
                ulong encoded = unchecked((ulong)Read7Bit64(reader));
                return unchecked((long)(encoded >> 1) ^ -((long)encoded & 1)).ToString(invariant);
            case 1: return unchecked((ulong)Read7Bit64(reader)).ToString(invariant);
            case 2:
                double value = reader.ReadDouble();
                if (!Finite(value)) throw new InvalidDataException("Nonfinite fixture number.");
                return value.ToString("R", invariant);
            case 3: return String(reader, strings);
            default: throw new InvalidDataException("Unknown numeric encoding.");
        }
    }
    private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    private static byte[] Digest(byte[] bytes) { using var hash = SHA256.Create(); return hash.ComputeHash(bytes); }
    private static void Write7Bit(BinaryWriter writer, int value)
    {
        uint bits = unchecked((uint)value);
        while (bits >= 128) { writer.Write((byte)(bits | 128)); bits >>= 7; }
        writer.Write((byte)bits);
    }
    private static void Write7Bit64(BinaryWriter writer, long value)
    {
        ulong bits = unchecked((ulong)value);
        while (bits >= 128) { writer.Write((byte)(bits | 128)); bits >>= 7; }
        writer.Write((byte)bits);
    }
    private static int Read7Bit(BinaryReader reader)
    {
        uint result = 0;
        for (int shift = 0; shift < 35; shift += 7)
        {
            byte next = reader.ReadByte();
            if (shift == 28 && next > 15) throw new FormatException("Invalid seven-bit integer.");
            result |= (uint)(next & 127) << shift;
            if (next < 128) return unchecked((int)result);
        }
        throw new FormatException("Invalid seven-bit integer.");
    }
    private static long Read7Bit64(BinaryReader reader)
    {
        ulong result = 0;
        for (int shift = 0; shift < 70; shift += 7)
        {
            byte next = reader.ReadByte();
            if (shift == 63 && next > 1) throw new FormatException("Invalid seven-bit integer.");
            result |= (ulong)(next & 127) << shift;
            if (next < 128) return unchecked((long)result);
        }
        throw new FormatException("Invalid seven-bit integer.");
    }
}
