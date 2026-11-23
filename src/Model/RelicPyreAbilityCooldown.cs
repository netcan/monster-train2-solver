using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class RelicPyreAbilityCooldownModel
    {
        internal const string Effect = "RelicEffectModifyUnitAbilityCooldown";
        internal const string OwnerId = "806077f8-3495-4f36-a27f-a0454229fc57";
        internal const string OwnerKey = "Echoes_PyreCanUseAbilityMultipleTimes";
        internal const int Cooldown = 2;

        internal static bool IsOriginal(CombatRelicState owner) => owner.DataId == OwnerId && owner.AssetKey == OwnerKey &&
            owner.EffectTypes.Count == 1 && owner.IsCovenant == false && owner.DisallowedInPlacementPhase == false;

        internal static RoomCombatState Apply(RoomCombatState room, int unitId)
        {
            CombatUnit unit = room.Units.Single(candidate => candidate.Id == unitId);
            if (!unit.IsPyre || unit.Ability is not { Cooldown: > 0 } ability || ability.Cooldown == Cooldown) return room;
            CombatUnit changed = AbilityCooldownModel.Copy(unit, ability.WithCooldown(Cooldown));
            return room.WithUnits(room.Units.Select(candidate => candidate.Id == unitId ? changed : candidate).ToArray());
        }
    }
}
