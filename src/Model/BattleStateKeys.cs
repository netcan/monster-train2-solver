using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
        private readonly Dictionary<Type, PropertyInfo[]> properties = new Dictionary<Type, PropertyInfo[]>();
        private readonly SHA256 hash = SHA256.Create();

        public string Key(BattleTurnState state) => Value(state);
        public string SnapshotKey(object? snapshot) => Value(snapshot);
        public void Dispose() => hash.Dispose();

        private string Value(object? value)
        {
            if (value == null) return "null";
            if (value is string text) return "s" + text.Length.ToString(CultureInfo.InvariantCulture) + ":" + text;
            Type type = value.GetType();
            if (value is double real) return "double:" + real.ToString("R", CultureInfo.InvariantCulture);
            if (value is float single) return "float:" + single.ToString("R", CultureInfo.InvariantCulture);
            if (type.IsPrimitive || type.IsEnum || value is decimal)
                return type.FullName + ":" + (value is IFormattable formatted ? formatted.ToString(null, CultureInfo.InvariantCulture) : value.ToString());
            if (!type.IsValueType && objects.TryGetValue(value, out string? cached)) return cached;
            var data = new StringBuilder(type.FullName).Append('|');
            void Append(string field) => data.Append(field.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(field);
            if (value is IEnumerable sequence)
            {
                foreach (object? item in sequence) Append(Value(item));
            }
            else
            {
                if (!properties.TryGetValue(type, out PropertyInfo[]? fields))
                    properties.Add(type, fields = type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                        .Where(field => field.CanRead && field.GetIndexParameters().Length == 0)
                        .OrderBy(field => field.Name, StringComparer.Ordinal).ToArray());
                foreach (PropertyInfo field in fields) { Append(field.Name); Append(Value(field.GetValue(value))); }
            }
            string result = BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(data.ToString()))).Replace("-", "");
            if (!type.IsValueType) objects.Add(value, result);
            return result;
        }

        private sealed class IdentityComparer : IEqualityComparer<object>
        {
            public new bool Equals(object? left, object? right) => ReferenceEquals(left, right);
            public int GetHashCode(object value) => RuntimeHelpers.GetHashCode(value);
        }
    }
}
