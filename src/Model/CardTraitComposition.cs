using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class CardTraitValue
    {
        public string RuntimeType { get; }
        public string? DeclaredName { get; }
        public int DefinitionParamInt { get; }
        public int ParamInt { get; }
        public bool Removable { get; }
        public bool? DefinitionRemovable { get; }
        public float? MagicPowerScale { get; }
        public int StackMode { get; }
        public bool TemporaryReplacement { get; }
        public IReadOnlyList<string> ParameterUpgradeCastEffects { get; }
        // Historical capture flag for a declared name. Native CardTraitData.None
        // is non-null and remains in GetTraits with a null declared name.
        public bool HasData => DeclaredName != null;
        public CardTraitValue(string runtimeType, string? declaredName, int definitionParamInt, int paramInt,
            bool removable, int stackMode, bool temporaryReplacement = false, IReadOnlyList<string>? parameterUpgradeCastEffects = null,
            bool? definitionRemovable = null, float? magicPowerScale = null)
        {
            RuntimeType = runtimeType; DeclaredName = declaredName; DefinitionParamInt = definitionParamInt; ParamInt = paramInt;
            Removable = removable; StackMode = stackMode; TemporaryReplacement = temporaryReplacement;
            DefinitionRemovable = definitionRemovable ?? (declaredName == null ? true : removable);
            MagicPowerScale = runtimeType == "CardTraitScalingMagicPowerOnMoonPhase" ? magicPowerScale ?? 1f : magicPowerScale;
            ParameterUpgradeCastEffects = Array.AsReadOnly((parameterUpgradeCastEffects ?? Array.Empty<string>()).ToArray());
        }
        internal CardTraitValue Retyped(CardTraitReplacement replacement, bool temporary) =>
            new CardTraitValue(replacement.NewRuntimeType, replacement.NewName, DefinitionParamInt, DefinitionParamInt,
                DefinitionRemovable ?? Removable, StackMode, temporary, ParameterUpgradeCastEffects, DefinitionRemovable);
        internal static CardTraitValue Dummy() => new CardTraitValue("CardTraitDummy", null, 0, 0, false, 0);
    }

    public sealed class CardTraitReplacement
    {
        public string OldName { get; }
        public string NewName { get; }
        public string NewRuntimeType { get; }
        public CardTraitReplacement(string oldName, string newName, string newRuntimeType)
        { OldName = oldName; NewName = newName; NewRuntimeType = newRuntimeType; }
    }

    public sealed class CardTraitCompositionState
    {
        public IReadOnlyList<CardTraitValue> BaseTraits { get; }
        public IReadOnlyList<CardTraitValue> TemporaryTraits { get; }
        public IReadOnlyList<IReadOnlyList<CardTraitValue>> TemporaryUpgradeTraits { get; }
        public IReadOnlyList<string> RemovedRuntimeTypes { get; }
        public IReadOnlyList<CardTraitReplacement> PermanentReplacements { get; }
        public IReadOnlyList<CardTraitValue>? CombinedTraits { get; }
        public CardTraitCompositionState(IReadOnlyList<CardTraitValue> baseTraits, IReadOnlyList<CardTraitValue> temporaryTraits,
            IReadOnlyList<IReadOnlyList<CardTraitValue>> temporaryUpgradeTraits, IReadOnlyList<string> removedRuntimeTypes,
            IReadOnlyList<CardTraitReplacement> permanentReplacements, IReadOnlyList<CardTraitValue>? combinedTraits = null)
        {
            BaseTraits = Array.AsReadOnly(baseTraits.ToArray()); TemporaryTraits = Array.AsReadOnly(temporaryTraits.ToArray());
            TemporaryUpgradeTraits = CardUpgradeMaskRule.CopyLists(temporaryUpgradeTraits);
            RemovedRuntimeTypes = Array.AsReadOnly(removedRuntimeTypes.ToArray());
            PermanentReplacements = Array.AsReadOnly(permanentReplacements.ToArray());
            CombinedTraits = combinedTraits == null ? null : Array.AsReadOnly(combinedTraits.ToArray());
        }
    }

    // Refresh consumes persistent trait instances and upgrade definitions. It does
    // not execute trait callbacks or install permanent upgrades.
    public static class CardTraitCompositionModel
    {
        public static CardTraitCompositionState Refresh(CardTraitCompositionState source)
        {
            var bases = source.BaseTraits.ToList();
            int previousCombinedCount = source.CombinedTraits?.Count ?? 0;
            var removed = new HashSet<string>(source.RemovedRuntimeTypes, StringComparer.Ordinal);
            foreach (var replacement in source.PermanentReplacements)
            {
                int index = bases.FindIndex(trait => trait.RuntimeType == replacement.OldName);
                if (index < 0) continue;
                CardTraitValue added = removed.Contains(replacement.NewRuntimeType) ? CardTraitValue.Dummy() : bases[index].Retyped(replacement, false);
                bases.RemoveAt(index);
                // AddTrait also checks the previous combined list's length when
                // inserting. A newly seeded card can therefore append instead.
                if (index <= previousCombinedCount) bases.Insert(index, added);
                else bases.Add(added);
                previousCombinedCount++;
            }
            var combined = bases.Concat(source.TemporaryTraits).ToList();
            var present = new HashSet<string>(combined.Select(trait => trait.RuntimeType), StringComparer.Ordinal);
            foreach (var upgrade in source.TemporaryUpgradeTraits)
            foreach (var trait in upgrade)
            {
                if (present.Contains(trait.RuntimeType) || removed.Contains(trait.RuntimeType)) continue;
                var replacement = source.PermanentReplacements.FirstOrDefault(rule => rule.OldName == trait.DeclaredName);
                CardTraitValue added = trait;
                if (replacement != null)
                {
                    if (present.Contains(replacement.NewRuntimeType)) continue;
                    added = trait.Retyped(replacement, true);
                }
                // Upgrade definitions instantiate a fresh state with the declared
                // parameter, even when the persistent input has a changed parameter.
                else added = new CardTraitValue(trait.RuntimeType, trait.DeclaredName, trait.DefinitionParamInt,
                    trait.DefinitionParamInt, trait.Removable, trait.StackMode, false, trait.ParameterUpgradeCastEffects);
                combined.Add(added); present.Add(added.RuntimeType);
            }
            if (present.Contains("CardTraitExhaustState") && present.Contains("CardTraitSelfPurge"))
            {
                for (int index = 0; index < combined.Count; index++)
                    if (combined[index].RuntimeType == "CardTraitExhaustState") combined[index] = CardTraitValue.Dummy();
                present.Remove("CardTraitExhaustState");
            }
            if (present.Contains("CardTraitExhaustState"))
            {
                bool retained = false;
                for (int index = combined.Count - 1; index >= 0; index--)
                    if (combined[index].RuntimeType == "CardTraitExhaustState")
                    {
                        if (retained) combined[index] = CardTraitValue.Dummy();
                        retained = true;
                    }
            }
            return new CardTraitCompositionState(bases, source.TemporaryTraits, source.TemporaryUpgradeTraits,
                source.RemovedRuntimeTypes, source.PermanentReplacements, combined);
        }
    }
}
