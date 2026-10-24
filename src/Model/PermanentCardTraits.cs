using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class PermanentCardTraits
    {
        internal static CardMaskDescriptor Install(CardInstanceState source, CardMaskDescriptor descriptor,
            CardUpgradeModifier upgrade, out CardUpgradeModifier prepared)
        {
            var lifecycle = upgrade.Lifecycle; prepared = upgrade;
            if (lifecycle == null) return descriptor;
            bool Relevant(string type) => type is CardRetainModel.Trait or CardCorruptStateModel.Trait or
                "CardTraitExhaustState" or "CardTraitDistinct" or "CardTraitEphemeral" or "CardTraitPermafrost";
            bool Removal(string type) => type is "CardTraitEphemeral" or "CardTraitCopyOnPlay";
            if (!lifecycle.AddedTraits.Any(trait => Relevant(trait.RuntimeType)) && !lifecycle.RemovedRuntimeTypes.Any(Removal)) return descriptor;
            var bases = CardExhaustModel.RefreshTraits(source.WithMaskDescriptor(descriptor)).BaseTraits.ToList();
            var temporary = descriptor.TemporaryTraits.ToList(); var modified = new List<string>();
            CardMaskDescriptor Current() => descriptor.WithHoldover(bases, temporary, descriptor.HoldoverUpgradePending);
            CardTraitValue[] Active() => CardExhaustModel.RefreshTraits(source.WithMaskDescriptor(Current())).CombinedTraits!.ToArray();
            bool Add(CardTraitValue data)
            {
                bool blocked = source.Permanent.Upgrades.Concat(source.Temporary.Upgrades)
                    .Any(old => old.Lifecycle?.RemovedRuntimeTypes.Contains(data.RuntimeType) == true);
                bases.Add(blocked ? CardTraitValue.Dummy() : data);
                // CardState.SetupTraitUpgrades installs the permanent base trait.
                // Permafrost.OnTraitAdded immediately creates a separate Freeze.
                if (!blocked && data.RuntimeType == "CardTraitPermafrost" &&
                    !Active().Any(trait => trait.RuntimeType == "CardTraitFreeze"))
                    temporary.Add(new CardTraitValue("CardTraitFreeze", "CardTraitFreeze", 0, 0, true, 0));
                return !blocked;
            }
            bool Remove(string name)
            {
                bool found = Active().Any(trait => trait.DeclaredName == name && trait.Removable);
                int index = bases.FindIndex(trait => trait.DeclaredName == name && trait.Removable);
                if (index >= 0) bases[index] = CardTraitValue.Dummy();
                return found;
            }
            foreach (var data in lifecycle.AddedTraits.Where(trait => Relevant(trait.RuntimeType)))
            {
                var active = Active(); var existing = active.FirstOrDefault(trait => trait.RuntimeType == data.RuntimeType);
                if (existing == null || data.StackMode == 3)
                { if (Add(data) && lifecycle.AvoidClobbering) modified.Add(data.RuntimeType); }
                else if (!lifecycle.AvoidClobbering && data.StackMode == 1)
                {
                    for (int index = 0; index < bases.Count; index++)
                        if (bases[index].DeclaredName == data.DeclaredName) bases[index] = bases[index].WithParamInt(unchecked(bases[index].ParamInt + 1));
                    for (int index = 0; index < temporary.Count; index++)
                        if (temporary[index].DeclaredName == data.DeclaredName) temporary[index] = temporary[index].WithParamInt(unchecked(temporary[index].ParamInt + 1));
                }
                else if (!lifecycle.AvoidClobbering && data.StackMode == 2 && data.ParamInt > existing.ParamInt)
                { Remove(data.DeclaredName!); Add(data); }
            }
            foreach (string name in lifecycle.RemovedRuntimeTypes.Where(Removal))
                if (Active().Any(trait => trait.RuntimeType == name) && Remove(name) && lifecycle.AvoidClobbering) modified.Add(name);
            prepared = upgrade.WithLifecycle(lifecycle.WithModified(modified));
            return Current();
        }
    }
}
