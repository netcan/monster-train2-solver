using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class CardLifecycleTrigger
    {
        public string Type { get; }
        public string? DescriptionKey { get; }
        public IReadOnlyList<string> Effects { get; }
        public string Id { get; }
        public CardLifecycleTrigger(string type, string? descriptionKey, IReadOnlyList<string> effects, string id = "")
        { Type = type; DescriptionKey = descriptionKey; Effects = Array.AsReadOnly(effects.ToArray()); Id = id; }
        internal CardLifecycleTrigger WithId(string? id) => new CardLifecycleTrigger(Type, DescriptionKey, Effects, id ?? "");
    }
    public sealed class CardLifecycleUpgrade
    {
        public CardMaskUpgrade Values { get; }
        public string AssetKey { get; }
        public bool Unique { get; }
        public bool RemoveOnDiscard { get; }
        public IReadOnlyList<CardTraitValue> AddedTraits { get; }
        public IReadOnlyList<string> RemovedRuntimeTypes { get; }
        public bool AvoidClobbering { get; }
        public IReadOnlyList<string> TraitsModified { get; }
        public IReadOnlyList<string> ReplacedAssets { get; }
        public IReadOnlyList<CardLifecycleTrigger> Triggers { get; }
        public int? OriginalDamage { get; }
        public int? OriginalHeal { get; }
        public int InstanceId { get; }
        public CardLifecycleUpgrade(CardMaskUpgrade values, string assetKey, bool unique, bool removeOnDiscard,
            IReadOnlyList<CardTraitValue> addedTraits, IReadOnlyList<string> removedRuntimeTypes, bool avoidClobbering,
            IReadOnlyList<string> traitsModified, IReadOnlyList<string> replacedAssets, IReadOnlyList<CardLifecycleTrigger> triggers,
            int? originalDamage = null, int? originalHeal = null, int instanceId = 0)
        {
            Values = values; AssetKey = assetKey; Unique = unique; RemoveOnDiscard = removeOnDiscard;
            AddedTraits = Array.AsReadOnly(addedTraits.ToArray()); RemovedRuntimeTypes = Array.AsReadOnly(removedRuntimeTypes.ToArray());
            AvoidClobbering = avoidClobbering; TraitsModified = Array.AsReadOnly(traitsModified.ToArray());
            ReplacedAssets = Array.AsReadOnly(replacedAssets.ToArray()); Triggers = Array.AsReadOnly(triggers.ToArray());
            OriginalDamage = originalDamage; OriginalHeal = originalHeal;
            InstanceId = instanceId;
        }
        internal CardLifecycleUpgrade Rescale(int multiplier)
        {
            var stats = Values.Stats;
            var scaled = new CardStatModifier(OriginalDamage.HasValue ? unchecked(OriginalDamage.Value * multiplier) : stats.Damage,
                stats.Health, stats.Cost, OriginalHeal.HasValue ? unchecked(OriginalHeal.Value * multiplier) : stats.Heal,
                stats.Size, stats.XCost, stats.EquipmentLimit, stats.UpgradeSlotCount);
            return new CardLifecycleUpgrade(new CardMaskUpgrade(Values.DataId, scaled, Values.Statuses, Values.HasIcon, Values.HideIcon, Values.RegionRun, Values.UnitAbility),
                AssetKey, Unique, RemoveOnDiscard, AddedTraits, RemovedRuntimeTypes, AvoidClobbering, TraitsModified, ReplacedAssets, Triggers, OriginalDamage, OriginalHeal, InstanceId);
        }
        internal CardLifecycleUpgrade ClearModified() => new CardLifecycleUpgrade(Values, AssetKey, Unique, RemoveOnDiscard, AddedTraits, RemovedRuntimeTypes,
            AvoidClobbering, Array.Empty<string>(), ReplacedAssets, Triggers, OriginalDamage, OriginalHeal, InstanceId);
        internal CardLifecycleUpgrade WithInstanceId(int instanceId) => new CardLifecycleUpgrade(Values, AssetKey, Unique, RemoveOnDiscard, AddedTraits, RemovedRuntimeTypes,
            AvoidClobbering, TraitsModified, ReplacedAssets, Triggers, OriginalDamage, OriginalHeal, instanceId);
    }
    public sealed class CardUpgradeLifecycleState
    {
        public CardOwnedMaskState Card { get; }
        public IReadOnlyList<CardLifecycleUpgrade> Permanent { get; }
        public IReadOnlyList<CardLifecycleUpgrade> Temporary { get; }
        public IReadOnlyList<CardLifecycleTrigger> AuthoredTriggers { get; }
        public IReadOnlyList<CardLifecycleTrigger> UpgradeTriggers { get; }
        public bool StandbyOverride { get; }
        public string? StandbyPile { get; }
        public CardUpgradeLifecycleState(CardOwnedMaskState card, IReadOnlyList<CardLifecycleUpgrade> permanent, IReadOnlyList<CardLifecycleUpgrade> temporary,
            IReadOnlyList<CardLifecycleTrigger> authoredTriggers, IReadOnlyList<CardLifecycleTrigger> upgradeTriggers, bool standbyOverride, string? standbyPile = null)
        { Card = card; Permanent = Array.AsReadOnly(permanent.ToArray()); Temporary = Array.AsReadOnly(temporary.ToArray());
            AuthoredTriggers = Array.AsReadOnly(authoredTriggers.ToArray()); UpgradeTriggers = Array.AsReadOnly(upgradeTriggers.ToArray()); StandbyOverride = standbyOverride; StandbyPile = standbyPile; }
    }
    public sealed class CardUpgradeLifecycleStep
    {
        public CardUpgradeLifecycleState State { get; }
        public bool? Returned { get; }
        public CardUpgradeLifecycleStep(CardUpgradeLifecycleState state, bool? returned) { State = state; Returned = returned; }
    }
    // CardState's temporary upgrade operations and their ordered query state.
    // These operations install trigger definitions; executing those effects is
    // the battle adapter's responsibility.
    public static class CardUpgradeLifecycleModel
    {
        public static CardUpgradeLifecycleStep ApplyTemporary(CardUpgradeLifecycleState source, CardLifecycleUpgrade upgrade, string? triggerId = null)
        {
            if (upgrade.Unique && upgrade.Values.DataId.Length != 0 && source.Temporary.Any(item => item.Values.DataId == upgrade.Values.DataId))
                return new CardUpgradeLifecycleStep(source, false);
            var temporary = source.Temporary.ToList(); temporary.Add(upgrade);
            var triggers = source.UpgradeTriggers.Concat(upgrade.Triggers.Select(trigger => trigger.WithId(triggerId))).ToList();
            int count = unchecked(source.Card.Traits.TemporaryCount + 1);
            foreach (string asset in upgrade.ReplacedAssets)
            {
                int index = temporary.FindIndex(item => item.AssetKey == asset);
                if (index < 0) continue;
                var selected = temporary[index]; RemoveTriggers(triggers, selected.Triggers, null);
                RemoveObject(temporary, selected); count = unchecked(count + 1);
            }
            var state = Rebuild(source, temporary: temporary, upgradeTriggers: triggers, temporaryCount: count);
            return new CardUpgradeLifecycleStep(Ensure(state), true); // UpdateCardBodyText reads traits.
        }
        public static CardUpgradeLifecycleStep RemoveById(CardUpgradeLifecycleState source, string dataId, string? triggerId = null)
        {
            int index = source.Temporary.ToList().FindIndex(upgrade => upgrade.Values.DataId == dataId);
            if (index < 0) return new CardUpgradeLifecycleStep(source, false);
            var selected = source.Temporary[index]; var state = source;
            foreach (var trait in selected.AddedTraits)
            {
                if (selected.AvoidClobbering && !selected.TraitsModified.Contains(trait.DeclaredName!)) continue;
                state = Ensure(state);
                if (state.Card.Traits.Composition.CombinedTraits!.Any(item => item.RuntimeType == trait.RuntimeType))
                    state = RemoveTrait(state, trait.DeclaredName!);
            }
            // ClearTraitsModified affects all references to the selected native instance.
            state = Rebuild(state, permanent: state.Permanent.Select(item => SameInstance(item, selected) ? item.ClearModified() : item).ToArray(),
                temporary: state.Temporary.Select(item => SameInstance(item, selected) ? item.ClearModified() : item).ToArray());
            state = Ensure(state); // RemoveTraitUpgrades always refreshes before modifier removal.
            var temporary = state.Temporary.ToList(); var triggers = state.UpgradeTriggers.ToList();
            RemoveTriggers(triggers, selected.Triggers, triggerId); RemoveObject(temporary, temporary[index]);
            state = Rebuild(state, temporary: temporary, upgradeTriggers: triggers, temporaryCount: unchecked(state.Card.Traits.TemporaryCount + 1));
            return new CardUpgradeLifecycleStep(state, true);
        }
        public static CardUpgradeLifecycleStep RemoveByIndex(CardUpgradeLifecycleState source, int index)
        {
            if (index < 0 || index >= source.Temporary.Count) return new CardUpgradeLifecycleStep(source, false);
            var temporary = source.Temporary.ToList(); var triggers = source.UpgradeTriggers.ToList();
            RemoveTriggers(triggers, temporary[index].Triggers, null); temporary.RemoveAt(index);
            // Native indexed removal does not increment CombinedTraitsDirtyCount.
            return new CardUpgradeLifecycleStep(Rebuild(source, temporary: temporary, upgradeTriggers: triggers), true);
        }
        public static CardUpgradeLifecycleStep Discard(CardUpgradeLifecycleState source)
        {
            var temporary = source.Temporary.ToList(); int count = source.Card.Traits.TemporaryCount;
            for (int index = temporary.Count - 1; index >= 0; index--)
                if (temporary[index].RemoveOnDiscard) { RemoveObject(temporary, temporary[index]); count = unchecked(count + 1); }
            // OnCardDiscarded removes modifiers directly, retaining installed triggers.
            return new CardUpgradeLifecycleStep(Rebuild(source, temporary: temporary, temporaryCount: count), null);
        }
        public static CardUpgradeLifecycleStep ResetTraits(CardUpgradeLifecycleState source)
        {
            var composition = source.Card.Traits.Composition;
            var traits = new CardTraitCompositionState(composition.BaseTraits, Array.Empty<CardTraitValue>(), composition.TemporaryUpgradeTraits,
                composition.RemovedRuntimeTypes, composition.PermanentReplacements, composition.CombinedTraits);
            return new CardUpgradeLifecycleStep(Ensure(Rebuild(source, composition: traits, temporaryCount: unchecked(source.Card.Traits.TemporaryCount + 1))), null);
        }
        public static CardUpgradeLifecycleStep Reset(CardUpgradeLifecycleState source)
        {
            var triggers = source.UpgradeTriggers.ToList();
            for (int index = source.Temporary.Count - 1; index >= 0; index--) RemoveTriggers(triggers, source.Temporary[index].Triggers, null);
            var old = source.Card.Traits.Composition;
            var composition = new CardTraitCompositionState(old.BaseTraits, Array.Empty<CardTraitValue>(), Array.Empty<IReadOnlyList<CardTraitValue>>(),
                old.RemovedRuntimeTypes, old.PermanentReplacements, old.CombinedTraits);
            var state = Ensure(Rebuild(source, temporary: Array.Empty<CardLifecycleUpgrade>(), upgradeTriggers: triggers, composition: composition,
                temporaryCount: 1, temporaryOffsets: new CardStatModifier(), standbyOverride: false, standbyPile: "None"));
            int multiplier = 1;
            foreach (var trait in state.Card.Traits.Composition.CombinedTraits!)
                multiplier = unchecked(multiplier * MagicMultiplier(trait));
            state = Rebuild(state, permanent: state.Permanent.Select(upgrade => upgrade.Rescale(multiplier)).ToArray());
            return new CardUpgradeLifecycleStep(state, null);
        }
        public static CardUpgradeLifecycleState Ensure(CardUpgradeLifecycleState source)
        {
            var traits = CardTraitRefreshModel.Ensure(source.Card.Traits);
            return new CardUpgradeLifecycleState(source.Card.WithTraits(traits), source.Permanent, source.Temporary,
                source.AuthoredTriggers, source.UpgradeTriggers, source.StandbyOverride, source.StandbyPile);
        }
        private static CardUpgradeLifecycleState RemoveTrait(CardUpgradeLifecycleState source, string name)
        {
            var old = source.Card.Traits.Composition; var bases = old.BaseTraits.ToList(); var combined = old.CombinedTraits!.ToList();
            int index = bases.FindIndex(trait => (trait.DeclaredName ?? "") == name && trait.Removable); if (index >= 0) bases.RemoveAt(index);
            index = combined.FindIndex(trait => (trait.DeclaredName ?? "") == name && trait.Removable); if (index >= 0) combined.RemoveAt(index);
            var composition = new CardTraitCompositionState(bases, old.TemporaryTraits, old.TemporaryUpgradeTraits, old.RemovedRuntimeTypes, old.PermanentReplacements, combined);
            return Rebuild(source, composition: composition, dirty: true);
        }
        private static void RemoveObject(List<CardLifecycleUpgrade> upgrades, CardLifecycleUpgrade selected)
        {
            int index = selected.Values.DataId.Length == 0 ? upgrades.IndexOf(selected) : upgrades.FindIndex(item => item.Values.DataId == selected.Values.DataId);
            if (index >= 0) upgrades.RemoveAt(index);
        }
        private static bool SameInstance(CardLifecycleUpgrade first, CardLifecycleUpgrade second) =>
            first.InstanceId > 0 && first.InstanceId == second.InstanceId || ReferenceEquals(first, second);
        private static int MagicMultiplier(CardTraitValue trait)
        {
            if (trait.RuntimeType == "CardTraitStrongerMagicPower") return 5;
            if (trait.RuntimeType != "CardTraitScalingMagicPowerOnMoonPhase") return 1;
            double rounded = Math.Round(trait.MagicPowerScale ?? 1f, MidpointRounding.ToEven);
            return double.IsNaN(rounded) || rounded < int.MinValue || rounded > int.MaxValue ? int.MinValue : (int)rounded;
        }
        private static void RemoveTriggers(List<CardLifecycleTrigger> installed, IReadOnlyList<CardLifecycleTrigger> definitions, string? id)
        {
            foreach (var definition in definitions)
                installed.RemoveAll(trigger => trigger.Type == definition.Type && trigger.DescriptionKey == definition.DescriptionKey && (id == null || trigger.Id == id));
        }
        private static CardUpgradeLifecycleState Rebuild(CardUpgradeLifecycleState source, IReadOnlyList<CardLifecycleUpgrade>? permanent = null,
            IReadOnlyList<CardLifecycleUpgrade>? temporary = null, IReadOnlyList<CardLifecycleTrigger>? upgradeTriggers = null,
            CardTraitCompositionState? composition = null, int? temporaryCount = null, bool? dirty = null,
            CardStatModifier? temporaryOffsets = null, bool? standbyOverride = null, string? standbyPile = null)
        {
            permanent ??= source.Permanent; temporary ??= source.Temporary; upgradeTriggers ??= source.UpgradeTriggers; composition ??= source.Card.Traits.Composition;
            var current = source.Card.Traits;
            var traits = new CardTraitCompositionState(composition.BaseTraits, composition.TemporaryTraits,
                temporary.Select(upgrade => upgrade.AddedTraits).ToArray(), permanent.Concat(temporary).SelectMany(upgrade => upgrade.RemovedRuntimeTypes).ToArray(),
                composition.PermanentReplacements, composition.CombinedTraits);
            var cache = new CardTraitRefreshState(traits, dirty ?? current.Dirty, current.PermanentCount, temporaryCount ?? current.TemporaryCount,
                current.CachedPermanentCount, current.CachedTemporaryCount);
            var card = new CardOwnedMaskState(source.Card.Definition, source.Card.BaseCost,
                new CardMaskModifiers(source.Card.Permanent.Offsets, permanent.Select(upgrade => upgrade.Values).ToArray()),
                new CardMaskModifiers(temporaryOffsets ?? source.Card.Temporary.Offsets, temporary.Select(upgrade => upgrade.Values).ToArray()), cache,
                source.AuthoredTriggers.Concat(upgradeTriggers).Where(trigger => trigger.Type == "OnCast").SelectMany(trigger => trigger.Effects).ToArray(),
                source.Card.Purified, source.Card.PermanentGraft);
            return new CardUpgradeLifecycleState(card, permanent, temporary, source.AuthoredTriggers, upgradeTriggers, standbyOverride ?? source.StandbyOverride, standbyPile ?? source.StandbyPile);
        }
    }
}
