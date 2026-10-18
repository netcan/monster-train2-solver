using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public static class CardCorruptRestrictedModel
    {
        public const string Trait = "CardTraitCorruptRestricted";

        // Positive requirements currently use one unchanged base trait. Upgrade-added,
        // replaced and source-room ability traits need their own captured provenance.
        public static string? Requirement(CardInstanceState card, out int amount)
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
            int index = descriptor.BaseTraits.ToList().FindIndex(trait => trait.RuntimeType == Trait);
            if (index < 0 || descriptor.SourceTraits == null || descriptor.SourceTraits.Count != descriptor.BaseTraits.Count)
                return "Positive Corrupt Restricted requires captured base source parameters.";
            CardSourceTrait source = descriptor.SourceTraits[index];
            if (source.Type != Trait || source.Int != active[0].ParamInt || source.Int != active[0].DefinitionParamInt ||
                source.Int2 != 0 || source.Int3 != 0 || source.Bool || source.Str != "" || source.Float != 0 || source.Scaling ||
                source.TrackedValue != "SubtypeInDeck" || source.EntryDuration != "ThisTurn")
                return "Uncalibrated Corrupt Restricted parameters or source-room eligibility.";
            if (combined.Any(trait => trait.RuntimeType == "CardTraitCorruptState"))
                return "Corrupt State's additional eligibility and pre-play ordering require native calibration.";
            amount = active[0].ParamInt;
            return null;
        }

        public static string? Test(CombatContext context, CardInstanceState card, int selectedRoomIndex, out bool allowed)
        {
            allowed = false;
            string? error = Requirement(card, out int amount);
            if (error != null) return error;
            if (amount == 0) { allowed = true; return null; }
            RoomCapacityState? room = context.RoomCapacities?.SingleOrDefault(value => value.RoomIndex == selectedRoomIndex);
            if (room?.Corruption == null || room.Corruption < 0) return "Corrupt Restricted requires captured selected-room corruption.";
            allowed = room.Corruption >= amount;
            return null;
        }

        internal static RoomCorruptionResult BeforeEffects(TrainCombatState source, CardInstanceState card, int selectedRoomIndex)
        {
            string? error = Requirement(card, out int amount);
            if (error != null) return new RoomCorruptionResult(null, error);
            if (amount == 0) return new RoomCorruptionResult(source.Context);
            return RoomCorruptionModel.Apply(source, selectedRoomIndex,
                new CardActionEffect("AdjustCorruption", "Room", amount, false, false, Array.Empty<CombatStatus>(), corruptionAdd: false));
        }
    }
}
