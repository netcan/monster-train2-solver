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
            if (combined.Any(trait => trait.RuntimeType == "CardTraitCorruptState"))
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
            allowed = room.Corruption >= amount;
            return null;
        }

        private static bool UsesSourceRoom(CardInstanceState card) =>
            card.MaskDescriptor?.SourceTraits?.Any(trait => trait.Type == Trait && trait.Bool) == true;

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
