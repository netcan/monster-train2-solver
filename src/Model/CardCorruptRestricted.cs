using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public static class CardCorruptRestrictedModel
    {
        public const string Trait = "CardTraitCorruptRestricted";

        // Positive requirements currently use one unchanged base trait. Upgrade-added,
        // replaced traits need their own captured provenance.
        public static string? Requirement(CardInstanceState card, out int amount, bool allowSourceRoom = false)
        {
            amount = 0;
            CardMaskDescriptor? descriptor = card.MaskDescriptor;
            if (descriptor == null) return null;
            if (!descriptor.BaseTraits.Concat(descriptor.TemporaryTraits).Any(trait => trait.RuntimeType == Trait) &&
                !descriptor.PermanentReplacements.Any(rule => rule.NewRuntimeType == Trait) &&
                !card.Permanent.Upgrades.Concat(card.Temporary.Upgrades).Any(upgrade => upgrade.Lifecycle?.AddedTraits.Any(trait => trait.RuntimeType == Trait) == true)) return null;
            var combined = CardExhaustModel.RefreshTraits(card).CombinedTraits ?? Array.Empty<CardTraitValue>();
            CardTraitValue[] active = combined.Where(trait => trait.RuntimeType == Trait).ToArray();
            if (active.Length == 0 || active.All(trait => trait.ParamInt == 0)) return null; // Existing zero-cost relic witnesses.
            if (active.Length != 1 || active[0].ParamInt < 0 || active[0].DeclaredName != Trait ||
                descriptor.TemporaryTraits.Any(trait => trait.RuntimeType == Trait) ||
                descriptor.PermanentReplacements.Any(rule => rule.OldName == Trait || rule.NewRuntimeType == Trait) ||
                card.Permanent.Upgrades.Concat(card.Temporary.Upgrades).Any(upgrade => upgrade.Lifecycle?.AddedTraits.Any(trait => trait.RuntimeType == Trait) == true))
                return "Positive Corrupt Restricted requires one unchanged base trait.";
            // Native base-state lists can also contain an installed Holdover
            // trait. Source parameters describe authored card data, so match
            // the single Corrupt Restricted definition by type, not list size.
            CardSourceTrait[] sources = descriptor.SourceTraits?.Where(source => source.Type == Trait).ToArray() ??
                Array.Empty<CardSourceTrait>();
            if (descriptor.BaseTraits.Count(trait => trait.RuntimeType == Trait) != 1 || sources.Length != 1)
                return "Positive Corrupt Restricted requires captured base source parameters.";
            CardSourceTrait source = sources[0];
            if (source.Type != Trait || source.Int != active[0].ParamInt || source.Int != active[0].DefinitionParamInt ||
                source.Int2 != 0 || source.Int3 != 0 || source.Bool && !allowSourceRoom || source.Str != "" || source.Float != 0 || source.Scaling ||
                source.TrackedValue != "SubtypeInDeck" || source.EntryDuration != "ThisTurn")
                return "Uncalibrated Corrupt Restricted parameters or source-room eligibility.";
            CardTraitValue[] corrupt = combined.Where(trait => trait.RuntimeType == CardCorruptStateModel.Trait).ToArray();
            var corruptUpgrades = card.Permanent.Upgrades.Where(upgrade => upgrade.Lifecycle?.AddedTraits.Any(trait => trait.RuntimeType == CardCorruptStateModel.Trait) == true).ToArray();
            var temporaryCorrupt = card.Temporary.Upgrades.Where(upgrade => upgrade.Lifecycle?.AddedTraits.Any(trait => trait.RuntimeType == CardCorruptStateModel.Trait) == true).ToArray();
            // The observed return upgrade adds Corrupt and Consume definitions.
            // RefreshTraits deduplicates Corrupt against its installed base state.
            bool calibratedReturn = temporaryCorrupt.Length == 0 || temporaryCorrupt.Length == 1 &&
                temporaryCorrupt[0].DataId == "2d744be5-cf16-4126-83fa-d6df9d99229c" &&
                temporaryCorrupt[0].AssetKey == "WurmReturnEnhanceCorrupt" &&
                temporaryCorrupt[0].Lifecycle!.AddedTraits.Select(trait => trait.RuntimeType)
                    .SequenceEqual(new[] { CardCorruptStateModel.Trait, "CardTraitExhaustState" }) &&
                temporaryCorrupt[0].Lifecycle!.AddedTraits.All(trait => trait.DeclaredName == trait.RuntimeType &&
                    trait.ParamInt == 0 && trait.DefinitionParamInt == 0);
            bool merchantCorrupt = (card.DataId == "ae114dc4-d08f-4909-a2ad-d99215661463" || IntrinsicConsumedInfusion(card)) &&
                descriptor.BaseTraits.Count(trait => trait.RuntimeType == CardCorruptStateModel.Trait) == 1 &&
                corruptUpgrades.Length == 1 && corruptUpgrades[0].DataId == "bdb7a38c-18de-4461-beb8-c27fdbdc5f62" &&
                corruptUpgrades[0].AssetKey == "TraitCorruptwIcon" && corruptUpgrades[0].Lifecycle!.AddedTraits.Count == 1 &&
                calibratedReturn;
            bool returnedHoldover = card.DataId == "096def92-500b-4b1f-89f7-43e96aa1e54a" &&
                temporaryCorrupt.Length == 1 && calibratedReturn && corruptUpgrades.Length == 0 &&
                descriptor.BaseTraits.Select(trait => trait.RuntimeType).SequenceEqual(new[] { Trait, CardRetainModel.Trait }) &&
                card.Permanent.Upgrades.Any(upgrade => upgrade.DataId == "f43e7cdb-fa8d-4760-88bf-c6f34f72d918" &&
                    upgrade.AssetKey == "TraitRetain" && upgrade.Lifecycle?.AddedTraits.Count == 1 &&
                    upgrade.Lifecycle.AddedTraits[0].RuntimeType == CardRetainModel.Trait);
            if (corrupt.Length > 0 && !(
                descriptor.Definition.CardType == "Spell" && active[0].ParamInt == 1 &&
                corrupt.Length == 1 && corrupt[0].DeclaredName == CardCorruptStateModel.Trait &&
                corrupt[0].DefinitionParamInt == 0 && corrupt[0].ParamInt == 0 &&
                !descriptor.TemporaryTraits.Any(trait => trait.RuntimeType == CardCorruptStateModel.Trait) &&
                !descriptor.PermanentReplacements.Any(rule => rule.OldName == CardCorruptStateModel.Trait || rule.NewRuntimeType == CardCorruptStateModel.Trait) &&
                (merchantCorrupt || returnedHoldover)))
                return "Corrupt State's additional eligibility and pre-play ordering require native calibration.";
            amount = active[0].ParamInt;
            return null;
        }

        public static string? Test(CombatContext context, CardInstanceState card, int selectedRoomIndex, out bool allowed, int? sourceRoomIndex = null)
        {
            allowed = false;
            string? error = Requirement(card, out int amount, sourceRoomIndex.HasValue);
            if (error != null) return error;
            if (amount == 0) { allowed = true; return null; }
            int? roomIndex = UsesSourceRoom(card) ? sourceRoomIndex : selectedRoomIndex;
            RoomCapacityState? room = context.RoomCapacities?.SingleOrDefault(value => value.RoomIndex == roomIndex);
            if (room?.Corruption == null || room.Corruption < 0) return "Corrupt Restricted requires captured selected-room corruption.";
            // Native eligibility credits one for the presence of Corrupt,
            // independently of the mutable amount added by its callback.
            int bonus = CardExhaustModel.RefreshTraits(card).CombinedTraits?.Any(trait => trait.RuntimeType == CardCorruptStateModel.Trait) == true ? 1 : 0;
            allowed = unchecked(room.Corruption.Value + bonus) >= amount;
            return null;
        }

        private static bool UsesSourceRoom(CardInstanceState card) =>
            card.MaskDescriptor?.SourceTraits?.Any(trait => trait.Type == Trait && trait.Bool) == true;

        internal static bool IntrinsicConsumedInfusion(CardInstanceState card) =>
            card.DataId == "096def92-500b-4b1f-89f7-43e96aa1e54a" && card.MaskDescriptor != null &&
            card.MaskDescriptor.BaseTraits.Select(trait => trait.RuntimeType).SequenceEqual(new[] {
                Trait, CardCorruptStateModel.Trait, "CardTraitExhaustState", "CardTraitIntrinsicState" }) &&
            card.MaskDescriptor.BaseTraits.All(trait => trait.DeclaredName == trait.RuntimeType &&
                trait.ParamInt == (trait.RuntimeType == Trait ? 1 : 0) && trait.DefinitionParamInt == trait.ParamInt && !trait.TemporaryReplacement) &&
            card.Permanent.Upgrades.Count == 3 && card.Temporary.Upgrades.Count == 0 &&
            card.Permanent.Upgrades.Select(upgrade => upgrade.DataId).SequenceEqual(new[] {
                "bdb7a38c-18de-4461-beb8-c27fdbdc5f62", "fb416958-813a-435b-b7ea-e428127ece20", "62ea77cc-4f97-476f-9ccc-a23e28bc788d" }) &&
            card.Permanent.Upgrades.Select(upgrade => upgrade.AssetKey).SequenceEqual(new[] {
                "TraitCorruptwIcon", "ReduceCostAddConsume", "TraitInherentSpellReduceCost" }) &&
            card.Permanent.Upgrades.Select((upgrade, index) =>
                new[] { "Damage", "Health", "Cost", "Heal", "Size", "XCost", "EquipmentLimit", "UpgradeSlotCount" }
                    .All(stat => upgrade.Stats.Value(stat) == (stat == "Cost" ? index == 1 ? -2 : index == 2 ? -1 : 0 :
                        stat == "XCost" ? index == 1 ? 2 : index == 2 ? 1 : 0 : 0)) &&
                upgrade is { Statuses.Count: 0, UnhealedHealth: 0, DamageBuff: 0, AbilityUpgrade: null, ExternalInteractions.Count: 0 } &&
                upgrade.TriggerUpgrades?.Count is not > 0).All(value => value);

        internal static RoomCorruptionResult BeforeEffects(TrainCombatState source, CardInstanceState card, int selectedRoomIndex, int? sourceRoomIndex = null)
        {
            string? error = Requirement(card, out int amount, sourceRoomIndex.HasValue);
            if (error != null) return new RoomCorruptionResult(null, error);
            if (amount == 0) return new RoomCorruptionResult(source.Context);
            int? roomIndex = UsesSourceRoom(card) ? sourceRoomIndex : selectedRoomIndex;
            if (!roomIndex.HasValue) return new RoomCorruptionResult(null, "Missing Corrupt Restricted ability source room.");
            return RoomCorruptionModel.Apply(source, roomIndex.Value,
                new CardActionEffect("AdjustCorruption", "Room", amount, false, false, Array.Empty<CombatStatus>(), corruptionAdd: false));
        }
    }
}
