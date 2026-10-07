using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using MonsterTrain2Poju.Fixtures;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace MonsterTrain2Poju.Capture
{
    // Uses the same serialization contracts as the diagnostic JSON export, but
    // visits shared object instances once and builds typed binary nodes directly.
    // The captured object graph must stay stable for the duration of this call.
    public static class NativeFixtureCapture
    {
        public static FixtureDocument Capture(object snapshot)
        {
            var capture = new CaptureState();
            int root = capture.Add(snapshot, 0);
            // Native binary captures have no textual source. Payload integrity
            // is still recorded and checked by the archive writer and reader.
            return capture.Builder.Build(root, 0, new byte[32]);
        }

        private sealed class CaptureState
        {
            internal readonly FixtureBuilder Builder = new FixtureBuilder();
            private readonly JsonSerializer serializer = JsonSerializer.CreateDefault();
            private readonly Dictionary<object, int> instances = new Dictionary<object, int>(ReferenceIdentity.Instance);
            private readonly Dictionary<string, int> strings = new Dictionary<string, int>(StringComparer.Ordinal);
            private readonly Dictionary<long, int> integers = new Dictionary<long, int>();
            private readonly Dictionary<ulong, int> largeUnsigned = new Dictionary<ulong, int>();
            private readonly Dictionary<JsonObjectContract, PropertyPlan[]> objectPlans = new Dictionary<JsonObjectContract, PropertyPlan[]>();
            private readonly int nullNode, trueNode, falseNode;

            internal CaptureState()
            {
                if (serializer.NullValueHandling != NullValueHandling.Include || serializer.DefaultValueHandling != DefaultValueHandling.Include ||
                    serializer.TypeNameHandling != TypeNameHandling.None || serializer.PreserveReferencesHandling != PreserveReferencesHandling.None ||
                    serializer.ReferenceLoopHandling != ReferenceLoopHandling.Error || serializer.Converters.Count != 0 ||
                    serializer.ContractResolver.GetType() != typeof(DefaultContractResolver))
                    throw new NotSupportedException("Native capture requires default structural serialization settings.");
                nullNode = Builder.Add(FixtureKind.Null);
                trueNode = Builder.Add(FixtureKind.True);
                falseNode = Builder.Add(FixtureKind.False);
            }

            internal int Add(object? value, int depth)
            {
                if (depth > 128) throw new InvalidOperationException("Native fixture nesting exceeds the archive limit.");
                if (value == null) return nullNode;
                if (value is string text) return String(text);
                if (value is bool flag) return flag ? trueNode : falseNode;
                // Reflection providers already box these scalars. Avoid contract
                // resolution, formatting and allocating another node key for the
                // millions of repeated numeric fields in complete captures.
                if (value is int signed) return Integer(signed);
                if (value is uint unsigned) return Integer(unsigned);
                if (value is long signed64) return Integer(signed64);
                if (value is ulong unsigned64) return Unsigned(unsigned64);
                if (value is short signed16) return Integer(signed16);
                if (value is ushort unsigned16) return Integer(unsigned16);
                if (value is byte unsigned8) return Integer(unsigned8);
                if (value is sbyte signed8) return Integer(signed8);
                if (instances.TryGetValue(value, out int found))
                {
                    if (found < 0) throw new InvalidOperationException("Cyclic native capture graph: " + value.GetType().FullName);
                    return found;
                }
                Type type = value.GetType();
                JsonContract contract = serializer.ContractResolver.ResolveContract(type);
                if (contract.Converter != null || serializer.Converters.Count != 0)
                    throw new NotSupportedException("Native binary capture requires an unconverted contract: " + type.FullName);
                if (contract is JsonPrimitiveContract || contract is JsonStringContract) return Scalar(value);
                if (contract.IsReference.HasValue || contract.OnSerializingCallbacks.Count != 0 || contract.OnSerializedCallbacks.Count != 0 ||
                    contract is JsonContainerContract container && (container.ItemConverter != null || container.ItemIsReference.HasValue ||
                        container.ItemReferenceLoopHandling.HasValue || container.ItemTypeNameHandling.HasValue))
                    throw new NotSupportedException("Native capture requires a stable, unconverted container: " + type.FullName);

                bool cache = !type.IsValueType;
                if (cache) instances.Add(value, -1);
                int node;
                if (value is JToken token) node = Token(token, depth);
                else if (contract is JsonArrayContract && value is IEnumerable sequence)
                {
                    var children = new List<int>();
                    foreach (object? child in sequence) children.Add(Add(child, depth + 1));
                    node = Builder.Add(FixtureKind.Array, children: children.ToArray());
                }
                else if (contract is JsonDictionaryContract dictionary)
                {
                    var children = new List<int>();
                    foreach (object entry in (IEnumerable)value)
                    {
                        object? key, child;
                        if (entry is DictionaryEntry item) { key = item.Key; child = item.Value; }
                        else
                        {
                            Type entryType = entry.GetType();
                            key = entryType.GetProperty("Key")!.GetValue(entry);
                            child = entryType.GetProperty("Value")!.GetValue(entry);
                        }
                        if (!(key is string name)) throw new NotSupportedException("Native capture dictionary keys must be strings.");
                        name = dictionary.DictionaryKeyResolver?.Invoke(name) ?? name;
                        children.Add(String(name));
                        children.Add(Add(child, depth + 1));
                    }
                    node = Builder.Add(FixtureKind.Object, children: children.ToArray());
                }
                else if (contract is JsonObjectContract properties)
                {
                    var children = new List<int>();
                    foreach (PropertyPlan property in Plan(properties))
                    {
                        if (property.ShouldSerialize?.Invoke(value) == false || property.IsSpecified?.Invoke(value) == false) continue;
                        children.Add(property.Key);
                        children.Add(Add(property.Provider.GetValue(value), depth + 1));
                    }
                    node = Builder.Add(FixtureKind.Object, children: children.ToArray());
                }
                else throw new NotSupportedException("Unsupported native capture contract: " + type.FullName);
                if (cache) instances[value] = node;
                return node;
            }

            private PropertyPlan[] Plan(JsonObjectContract contract)
            {
                if (objectPlans.TryGetValue(contract, out var found)) return found;
                if (contract.ExtensionDataGetter != null)
                    throw new NotSupportedException("Native capture does not support extension-data contracts.");
                var properties = new List<PropertyPlan>();
                foreach (JsonProperty property in contract.Properties)
                {
                    if (property.Ignored || !property.Readable) continue;
                    if (property.Converter != null || property.NullValueHandling.HasValue || property.DefaultValueHandling.HasValue ||
                        property.ReferenceLoopHandling.HasValue || property.TypeNameHandling.HasValue || property.IsReference.HasValue ||
                        property.ItemConverter != null || property.ItemIsReference.HasValue || property.ItemTypeNameHandling.HasValue ||
                        property.ItemReferenceLoopHandling.HasValue)
                        throw new NotSupportedException("Native capture property settings require an explicit adapter: " + property.PropertyName);
                    properties.Add(new PropertyPlan(String(property.PropertyName!), property.ValueProvider!, property.ShouldSerialize, property.GetIsSpecified));
                }
                found = properties.ToArray(); objectPlans.Add(contract, found); return found;
            }
            private sealed class PropertyPlan
            {
                internal readonly int Key;
                internal readonly IValueProvider Provider;
                internal readonly Predicate<object>? ShouldSerialize, IsSpecified;
                internal PropertyPlan(int key, IValueProvider provider, Predicate<object>? shouldSerialize, Predicate<object>? isSpecified)
                { Key = key; Provider = provider; ShouldSerialize = shouldSerialize; IsSpecified = isSpecified; }
            }

            private int String(string text)
            {
                if (strings.TryGetValue(text, out int found)) return found;
                int node = Builder.Add(FixtureKind.String, text);
                strings.Add(text, node); return node;
            }
            private int Integer(long value)
            {
                if (integers.TryGetValue(value, out int found)) return found;
                int node = Builder.Add(FixtureKind.Number, value.ToString(CultureInfo.InvariantCulture));
                integers.Add(value, node); return node;
            }
            private int Unsigned(ulong value)
            {
                if (value <= long.MaxValue) return Integer((long)value);
                if (largeUnsigned.TryGetValue(value, out int found)) return found;
                int node = Builder.Add(FixtureKind.Number, value.ToString(CultureInfo.InvariantCulture));
                largeUnsigned.Add(value, node); return node;
            }

            private int Scalar(object value)
            {
                Type type = value.GetType();
                if (type.IsEnum) return Add(Convert.ChangeType(value, Enum.GetUnderlyingType(type), CultureInfo.InvariantCulture), 0);
                switch (Type.GetTypeCode(value.GetType()))
                {
                    case TypeCode.SByte: case TypeCode.Byte: case TypeCode.Int16: case TypeCode.UInt16:
                    case TypeCode.Int32: case TypeCode.UInt32: case TypeCode.Int64: case TypeCode.UInt64:
                        return Builder.Add(FixtureKind.Number, Convert.ToString(value, CultureInfo.InvariantCulture));
                    case TypeCode.Char: return String(value.ToString()!);
                }
                // Preserve Json.NET's exact numeric spelling (including 1.0,
                // decimals and integer widths), without serializing containers.
                using var textWriter = new System.IO.StringWriter(CultureInfo.InvariantCulture);
                using (var json = new JsonTextWriter(textWriter)) serializer.Serialize(json, value);
                string serialized = textWriter.ToString();
                JToken token = JToken.Parse(serialized);
                if (token.Type == JTokenType.Integer || token.Type == JTokenType.Float)
                {
                    return Builder.Add(FixtureKind.Number, serialized);
                }
                // Dates, byte arrays, GUIDs and nonfinite floats serialize as
                // strings. Parse only that scalar to preserve its actual value.
                if (token.Type == JTokenType.Date)
                    return String(JsonConvert.DeserializeObject<string>(serialized)!);
                return Token(token, 0);
            }

            private int Token(JToken token, int depth)
            {
                if (depth > 128) throw new InvalidOperationException("Native fixture nesting exceeds the archive limit.");
                if (token.Type == JTokenType.Object)
                {
                    var children = new List<int>();
                    foreach (JProperty property in ((JObject)token).Properties())
                    {
                        children.Add(String(property.Name));
                        children.Add(Token(property.Value, depth + 1));
                    }
                    return Builder.Add(FixtureKind.Object, children: children.ToArray());
                }
                if (token.Type == JTokenType.Array)
                {
                    var children = new List<int>();
                    foreach (JToken child in (JArray)token) children.Add(Token(child, depth + 1));
                    return Builder.Add(FixtureKind.Array, children: children.ToArray());
                }
                if (token.Type == JTokenType.String) return String(token.Value<string>()!);
                if (token.Type == JTokenType.Integer || token.Type == JTokenType.Float)
                    return Builder.Add(FixtureKind.Number, token.ToString(Formatting.None));
                if (token.Type == JTokenType.Boolean) return token.Value<bool>() ? trueNode : falseNode;
                if (token.Type == JTokenType.Null) return nullNode;
                // Date parsing is a Json.NET default; convert back to the
                // serialized string rather than creating an extra binary kind.
                if (token.Type == JTokenType.Date || token.Type == JTokenType.Bytes || token.Type == JTokenType.Guid || token.Type == JTokenType.TimeSpan || token.Type == JTokenType.Uri)
                    return String(JsonConvert.DeserializeObject<string>(token.ToString(Formatting.None))!);
                throw new NotSupportedException("Unsupported native capture token: " + token.Type);
            }
        }

        private sealed class ReferenceIdentity : IEqualityComparer<object>
        {
            internal static readonly ReferenceIdentity Instance = new ReferenceIdentity();
            public new bool Equals(object? left, object? right) => ReferenceEquals(left, right);
            public int GetHashCode(object value) => RuntimeHelpers.GetHashCode(value);
        }
    }
}
