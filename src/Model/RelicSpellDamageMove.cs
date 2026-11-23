using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class RelicSpellDamageMoveModel
    {
        internal const string Effect = "RelicEffectChanceAddEffectOnDamageCardPlayed";
        internal const string OwnerId = "faf96ddc-f2d1-40e5-b0d7-91f42d4b0e25";
        internal const string OwnerKey = "MoveUpOnSpellDamage";
        internal const int Chance = 100;
        internal const int BumpAmount = 1;
        internal const string SpellArmorDamage = "SpellArmorDamage";

        internal static bool IsOriginal(CombatRelicState owner) =>
            owner.DataId == OwnerId && owner.AssetKey == OwnerKey && owner.IsCovenant == false &&
            owner.DisallowedInPlacementPhase == false && owner.EffectTypes.Count == 1 && owner.EffectTypes[0] == Effect;

        internal static TrainCombatResult Apply(TrainCombatState train, int roomIndex,
            IReadOnlyList<RoomPlayRule> roomRules, CardPlayRule card, IReadOnlyList<int> damagedUnitIds)
        {
            RoomCombatState? room = train.Rooms.FirstOrDefault(candidate => candidate.RoomIndex == roomIndex);
            if (room == null) return Unsupported("Spell-damage movement requires the captured selected floor.");
            if (card.IsAnyAbility == true || card.OnCastEffects.Count != 0 ||
                (card.Effect != "Null" && (card.CardType != "Spell" || card.Effects.Count != 1 ||
                    card.Effects[0].Type is not ("Damage" or "AddStatus") || card.Effects[0].Target != "DropTargetCharacter")))
                return Unsupported("Spell-damage movement is calibrated for direct single-target damage and status casts.");
            CombatUnit[] targets = room.Units.Where(unit => unit.Health > 0 && damagedUnitIds.Contains(unit.Id)).ToArray();
            if (targets.Length == 0) return Match(train);
            if (targets.Length != 1 || targets[0].Team != CombatTeam.Enemy || targets[0].IsBoss == true)
                return Unsupported("Spell-damage movement requires one ordinary surviving enemy target.");
            BumpResult moved = BumpModel.Apply(train, new[] { targets[0].Id }, BumpAmount, roomRules);
            if (!moved.Supported) return Unsupported(moved.UnsupportedReason!);
            if (moved.Outcome != RoomOutcome.Exchanged || moved.RetainedUnits.Any(unit => unit.Health <= 0) ||
                moved.RoomRules.Zip(roomRules, (actual, previous) => actual.Enabled == previous.Enabled).Any(equal => !equal))
                return Unsupported("Terminal or floor-changing spell-damage movement callbacks require native calibration.");
            return Match(moved.State!);
        }

        private static TrainCombatResult Match(TrainCombatState train) =>
            new TrainCombatResult(train, RoomOutcome.Exchanged, new List<RoomCombatResult>());
        private static TrainCombatResult Unsupported(string reason) =>
            new TrainCombatResult(null, RoomOutcome.Unsupported, new List<RoomCombatResult>(), reason);
    }
}
