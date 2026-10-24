using System;
using System.Buffers;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace MonsterTrain2Poju.Model
{
    // Scoped to one planning call and its immutable snapshots. Every public
    // state property participates, including private-history projections, RNG,
    // card identities and ordered collections. Shared immutable subtrees are
    // hashed once. Hashes are only a search optimization; native replay remains
    // the correctness oracle for a returned recommendation.
    public sealed class BattleStateKeys : IDisposable
    {
        private readonly Dictionary<object, string> objects = new Dictionary<object, string>(new IdentityComparer());
        private readonly Dictionary<string, string> strings = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly Dictionary<object, string> scalars = new Dictionary<object, string>();
        // Layouts and getters depend only on the model type. Share them across
        // captures/workers; hashed values remain local to one immutable branch.
        private static readonly ConcurrentDictionary<Type, Lazy<Field[]>> layouts = new ConcurrentDictionary<Type, Lazy<Field[]>>();
        private readonly SHA256 hash = SHA256.Create();
        private readonly Stack<StringBuilder> builders = new Stack<StringBuilder>();
        private readonly char[] hex = new char[64];
        private char[]? characters;
        private byte[]? encoded;
        private bool disposed;
        private readonly bool ignorePreviewCopyAllocator;
        private readonly bool ignoreRoomSelection;

        public BattleStateKeys() { }
        // UI previews can allocate additional copies between settled captures.
        // Only their next unused number is irrelevant to a retained verified line;
        // allocator presence, existing copy identities and every RNG remain keyed.
        internal BattleStateKeys(bool ignorePreviewCopyAllocator) : this(ignorePreviewCopyAllocator, false) { }

        internal BattleStateKeys(bool ignorePreviewCopyAllocator, bool ignoreRoomSelection)
        { this.ignorePreviewCopyAllocator = ignorePreviewCopyAllocator; this.ignoreRoomSelection = ignoreRoomSelection; }

        public string Key(BattleTurnState state) => SnapshotKey(state);
        public string SnapshotKey(object? snapshot)
        {
            if (disposed) throw new ObjectDisposedException(nameof(BattleStateKeys));
            return Value(snapshot);
        }
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            hash.Dispose(); objects.Clear(); strings.Clear(); scalars.Clear(); builders.Clear();
            if (characters != null) ArrayPool<char>.Shared.Return(characters);
            if (encoded != null) ArrayPool<byte>.Shared.Return(encoded);
            characters = null; encoded = null;
        }

        private string Value(object? value)
        {
            if (value == null) return "null";
            if (value is string text)
            {
                if (strings.TryGetValue(text, out string? cachedText)) return cachedText;
                string result = "s" + text.Length.ToString(CultureInfo.InvariantCulture) + ":" + text;
                strings.Add(text, result); return result;
            }
            Type type = value.GetType();
            if (value is double real) return "double:" + real.ToString("R", CultureInfo.InvariantCulture);
            if (value is float single) return "float:" + single.ToString("R", CultureInfo.InvariantCulture);
            if (type.IsPrimitive || type.IsEnum || value is decimal)
            {
                // Decimal equality ignores scale, but its existing key text does
                // not. Floats above also preserve signed zero and round-trip text.
                if (value is not decimal && scalars.TryGetValue(value, out string? cachedScalar)) return cachedScalar;
                string result = type.FullName + ":" + (value is IFormattable formatted ? formatted.ToString(null, CultureInfo.InvariantCulture) : value.ToString());
                if (value is not decimal) scalars.Add(value, result);
                return result;
            }
            if (!type.IsValueType && objects.TryGetValue(value, out string? cached)) return cached;
            StringBuilder data = builders.Count == 0 ? new StringBuilder() : builders.Pop();
            try
            {
                data.Append(type.FullName).Append('|');
                void Append(string field) => data.Append(field.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(field);
                if (value is IEnumerable sequence)
                {
                    foreach (object? item in sequence) Append(Value(item));
                }
                else
                {
                    Field[] fields = layouts.GetOrAdd(type, item => new Lazy<Field[]>(() => item
                        .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                        .Where(field => field.CanRead && field.GetIndexParameters().Length == 0)
                        .OrderBy(field => field.Name, StringComparer.Ordinal).Select(field => new Field(field)).ToArray())).Value;
                    foreach (Field field in fields)
                    {
                        Append(field.Name);
                        if (ignoreRoomSelection &&
                            (value is BattleTurnState && field.Name == nameof(BattleTurnState.SelectedRoom) ||
                             value is StatisticQueryFrame && field.Name == nameof(StatisticQueryFrame.SelectedRoomIndex)) &&
                            field.Read(value) is int selected && selected >= 0)
                            Append("known-room-selection");
                        else if (ignorePreviewCopyAllocator && value is BattleSpawnPoints points &&
                            field.Name == nameof(BattleSpawnPoints.NextPreviewCopyId) && points.NextPreviewCopyId is int next &&
                            next > 0 && points.Groups.All(group => !group.PreviewCopyId.HasValue || group.PreviewCopyId.Value < next))
                            Append("enabled-preview-copy-allocator");
                        else Append(Value(field.Read(value)));
                    }
                }
                // Children finish hashing before their parent reaches this point,
                // so one scratch pair serves every node without per-node strings
                // or UTF-8 arrays. Buffers belong to this key instance/worker only.
                if (characters == null || characters.Length < data.Length)
                {
                    char[] next = ArrayPool<char>.Shared.Rent(data.Length);
                    if (characters != null) ArrayPool<char>.Shared.Return(characters);
                    characters = next;
                }
                data.CopyTo(0, characters, 0, data.Length);
                int length = Encoding.UTF8.GetByteCount(characters, 0, data.Length);
                if (encoded == null || encoded.Length < length)
                {
                    byte[] next = ArrayPool<byte>.Shared.Rent(length);
                    if (encoded != null) ArrayPool<byte>.Shared.Return(encoded);
                    encoded = next;
                }
                Encoding.UTF8.GetBytes(characters, 0, data.Length, encoded, 0);
                byte[] digest = hash.ComputeHash(encoded, 0, length);
                const string alphabet = "0123456789ABCDEF";
                for (int index = 0; index < digest.Length; index++)
                { hex[index * 2] = alphabet[digest[index] >> 4]; hex[index * 2 + 1] = alphabet[digest[index] & 15]; }
                string result = new string(hex);
                if (!type.IsValueType) objects.Add(value, result);
                return result;
            }
            finally
            {
                // Avoid retaining unusually large builders for the whole search.
                if (data.Capacity <= 16384) { data.Clear(); builders.Push(data); }
            }
        }

        private sealed class Field
        {
            internal readonly string Name;
            internal readonly Func<object, object?> Read;
            internal Field(PropertyInfo property)
            {
                Name = property.Name;
                var target = Expression.Parameter(typeof(object), "target");
                Read = Expression.Lambda<Func<object, object?>>(Expression.Convert(Expression.Property(
                    Expression.Convert(target, property.DeclaringType!), property), typeof(object)), target).Compile();
            }
        }

        private sealed class IdentityComparer : IEqualityComparer<object>
        {
            public new bool Equals(object? left, object? right) => ReferenceEquals(left, right);
            public int GetHashCode(object value) => RuntimeHelpers.GetHashCode(value);
        }
    }
}
