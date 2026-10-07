using System.Collections.Concurrent;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;

namespace MonsterTrain2Poju.Fixtures;

internal static class FixtureHydrator
{
    private static readonly ConcurrentDictionary<Type, Func<FixtureValue, object?>> plans = new();
    internal static object? Read(FixtureValue value, Type type)
    {
        if (value.ValueKind == FixtureKind.Null)
        {
            if (!type.IsValueType || Nullable.GetUnderlyingType(type) != null) return null;
            throw new InvalidDataException($"Null cannot initialize {type.Name}.");
        }
        return plans.GetOrAdd(type, Build)(value);
    }
    private static Func<FixtureValue, object?> Build(Type type)
    {
        Type? nullable = Nullable.GetUnderlyingType(type);
        if (nullable != null) return value => Read(value, nullable);
        if (type == typeof(string)) return value => value.GetString();
        if (type == typeof(bool)) return value => value.GetBoolean();
        if (type == typeof(int)) return value => value.GetInt32();
        if (type == typeof(uint)) return value => value.GetUInt32();
        if (type == typeof(long)) return value => value.GetInt64();
        if (type == typeof(ulong)) return value => value.GetUInt64();
        if (type == typeof(float)) return value => value.GetSingle();
        if (type == typeof(double)) return value => value.GetDouble();
        if (type == typeof(decimal)) return value => value.GetDecimal();
        if (type.IsEnum) return value => Enum.ToObject(type, Read(value, Enum.GetUnderlyingType(type))!);
        Type? item = type.IsArray ? type.GetElementType() : type.IsGenericType &&
            (type.GetGenericTypeDefinition() == typeof(IReadOnlyList<>) || type.GetGenericTypeDefinition() == typeof(IEnumerable<>) ||
             type.GetGenericTypeDefinition() == typeof(IList<>) || type.GetGenericTypeDefinition() == typeof(ICollection<>) ||
             type.GetGenericTypeDefinition() == typeof(List<>)) ? type.GetGenericArguments()[0] : null;
        if (item != null) return value =>
        {
            Array array = Array.CreateInstance(item, value.GetArrayLength());
            for (int i = 0; i < array.Length; i++) array.SetValue(Read(value[i], item), i);
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>) ? Activator.CreateInstance(type, array) : array;
        };
        var constructors = type.GetConstructors();
        if (constructors.Length != 1)
            throw new NotSupportedException($"Binary fixtures require one public constructor for {type.FullName}.");
        ConstructorInfo constructor = constructors[0];
        var valueParameter = Expression.Parameter(typeof(FixtureValue), "value");
        MethodInfo argumentMethod = typeof(FixtureHydrator).GetMethod(nameof(Argument), BindingFlags.Static | BindingFlags.NonPublic)!;
        var arguments = constructor.GetParameters().Select(parameter =>
        {
            PropertyInfo? property = type.GetProperties().SingleOrDefault(property =>
                string.Equals(property.Name, parameter.Name, StringComparison.OrdinalIgnoreCase) && property.PropertyType == parameter.ParameterType);
            if (property == null) throw new NotSupportedException($"No matching property for {type.Name}.{parameter.Name}.");
            object? fallback = parameter.HasDefaultValue ? parameter.DefaultValue : parameter.ParameterType.IsValueType ? Activator.CreateInstance(parameter.ParameterType) : null;
            return Expression.Convert(Expression.Call(argumentMethod, valueParameter, Expression.Constant(property.Name),
                Expression.Constant(parameter.ParameterType), Expression.Constant(fallback, typeof(object))), parameter.ParameterType);
        });
        return Expression.Lambda<Func<FixtureValue, object?>>(Expression.Convert(Expression.New(constructor, arguments), typeof(object)), valueParameter).Compile();
    }
    private static object? Argument(FixtureValue value, string name, Type type, object? fallback) =>
        value.TryGetProperty(name, out var property) ? Read(property, type) : fallback;
}
