using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.NativeCases
{
    internal sealed class RoomHealthMechanismDefinition
    {
        public string NativeType { get; }
        public int Coefficient { get; }
        public int Divisor { get; }
        public string Collection { get; }
        public string SourceId { get; }
        public string AssetKey { get; }
        public int ModifierIndex { get; }
        public RoomHealthMechanismDefinition(string nativeType, int coefficient, int divisor,
            string collection, string sourceId, string assetKey, int modifierIndex)
        { NativeType = nativeType; Coefficient = coefficient; Divisor = divisor; Collection = collection;
            SourceId = sourceId; AssetKey = assetKey; ModifierIndex = modifierIndex; }
    }

    internal sealed class RoomHealthMechanismCase
    {
        public int DefinitionIndex { get; }
        public int MaximumHealth { get; }
        public int Health { get; }
        public string Team { get; }
        public int Placement { get; }
        public bool Attached { get; }
        public bool RequestStats { get; }
        public int DamageType { get; }
        public RoomHealthMechanismCase(int definitionIndex, int maximumHealth, int health, string team,
            int placement, bool attached, bool requestStats, int damageType)
        { DefinitionIndex = definitionIndex; MaximumHealth = maximumHealth; Health = health; Team = team;
            Placement = placement; Attached = attached; RequestStats = requestStats; DamageType = damageType; }
    }

    // A reusable definition-driven matrix. The native runner supplies the original
    // definitions; no individual card/champion IDs or expected results live here.
    internal static class RoomHealthMechanismCases
    {
        internal static RoomHealthMechanismDefinition[] Definitions(IEnumerable<RoomHealthMechanismDefinition> originals)
        {
            var values = originals.OrderBy(value => value.NativeType, StringComparer.Ordinal)
                .ThenBy(value => value.Collection, StringComparer.Ordinal).ThenBy(value => value.SourceId, StringComparer.Ordinal)
                .ThenBy(value => value.ModifierIndex).ToList();
            foreach (string type in values.Select(value => value.NativeType).Distinct().ToArray())
            foreach (int coefficient in new[] { -2, 0, 1, int.MaxValue })
            foreach (int divisor in new[] { -1, 0, 1, 2, 3, int.MaxValue })
                values.Add(new RoomHealthMechanismDefinition(type, coefficient, divisor, "Control",
                    type + ":" + coefficient + ":" + divisor, "GeneratedParameterControl", 0));
            return values.ToArray();
        }

        internal static IEnumerable<RoomHealthMechanismCase> Generate(int definitionCount)
        {
            var health = new[] { (0, 0), (17, 17), (17, 16), (17, 9), (17, 0), (17, -3), (17, 23),
                (99999, 0), (int.MinValue, 99999), (99999, int.MinValue) };
            for (int index = 0; index < definitionCount; index++)
            foreach (var hp in health)
            foreach (string team in new[] { "Player", "Enemy", "Combined" })
            foreach (int placement in new[] { 0, 1, 2 }) // absent point, point+room, orphan point
            foreach (bool attached in new[] { false, true })
            foreach (bool stats in new[] { false, true })
            foreach (int damageType in new[] { 0, int.MaxValue }) // both native classes ignore this input
                yield return new RoomHealthMechanismCase(index, hp.Item1, hp.Item2, team, placement, attached, stats, damageType);
        }
    }
}
