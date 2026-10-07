using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

namespace MonsterTrain2Poju.Fixtures;

// Import only: the binary reader and model hydrator do not use this JSON path.
public static class LegacyFixtureImport
{
    public static FixtureDocument Read(string path)
    {
        using var file = File.OpenRead(path);
        using Stream input = path.EndsWith(".gz", StringComparison.OrdinalIgnoreCase) ? new GZipStream(file, CompressionMode.Decompress) : file;
        return Read(input);
    }
    public static FixtureDocument Read(Stream input)
    {
        using var hash = new HashingStream(input);
        using var document = JsonDocument.Parse(hash);
        var builder = new FixtureBuilder();
        int root = Add(document.RootElement);
        return builder.Build(root, hash.Count, hash.Digest());
        int Add(JsonElement value)
        {
            var kind = (FixtureKind)value.ValueKind;
            if (kind == FixtureKind.Array) return builder.Add(kind, children: value.EnumerateArray().Select(Add).ToArray());
            if (kind == FixtureKind.Object)
            {
                var children = new List<int>();
                foreach (var property in value.EnumerateObject())
                { children.Add(builder.Add(FixtureKind.String, property.Name)); children.Add(Add(property.Value)); }
                return builder.Add(kind, children: children.ToArray());
            }
            return builder.Add(kind, kind == FixtureKind.String ? value.GetString() : kind == FixtureKind.Number ? value.GetRawText() : null);
        }
    }
    public static void Verify(string source, FixtureDocument archive)
    {
        using var file = File.OpenRead(source);
        using Stream input = source.EndsWith(".gz", StringComparison.OrdinalIgnoreCase) ? new GZipStream(file, CompressionMode.Decompress) : file;
        using var hash = new HashingStream(input);
        using var document = JsonDocument.Parse(hash);
        if (hash.Count != archive.SourceLength || Convert.ToHexString(hash.Digest()).ToLowerInvariant() != archive.SourceSha256)
            throw new InvalidDataException("Archive source provenance differs.");
        Compare(document.RootElement, archive.RootElement, "$");
    }
    private static void Compare(JsonElement source, FixtureValue archive, string path)
    {
        if ((FixtureKind)source.ValueKind != archive.ValueKind) throw new InvalidDataException(path + ": value kind differs.");
        if (source.ValueKind == JsonValueKind.Object)
        {
            var original = source.EnumerateObject().ToArray(); var binary = archive.EnumerateObject().ToArray();
            if (original.Length != binary.Length) throw new InvalidDataException(path + ": property count differs.");
            for (int i = 0; i < original.Length; i++)
            {
                if (original[i].Name != binary[i].Name) throw new InvalidDataException(path + ": property name/order differs.");
                Compare(original[i].Value, binary[i].Value, path + "." + original[i].Name);
            }
        }
        else if (source.ValueKind == JsonValueKind.Array)
        {
            if (source.GetArrayLength() != archive.GetArrayLength()) throw new InvalidDataException(path + ": array length differs.");
            for (int i = 0; i < source.GetArrayLength(); i++) Compare(source[i], archive[i], path + "[" + i + "]");
        }
        else if (source.ValueKind == JsonValueKind.String && source.GetString() != archive.GetString() ||
                 source.ValueKind == JsonValueKind.Number && source.GetRawText() != archive.Number())
            throw new InvalidDataException(path + ": scalar value differs.");
    }
    private sealed class HashingStream(Stream input) : Stream
    {
        private readonly IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        public long Count { get; private set; }
        public byte[] Digest() => hash.GetHashAndReset();
        public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));
        public override int Read(Span<byte> buffer)
        {
            int read = input.Read(buffer); hash.AppendData(buffer[..read]); Count += read; return read;
        }
        protected override void Dispose(bool disposing) { if (disposing) hash.Dispose(); base.Dispose(disposing); }
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => Count; set => throw new NotSupportedException(); }
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
