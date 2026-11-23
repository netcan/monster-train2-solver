using System;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class RelicPercentageStatProbe
    {
        internal static string TeamName(Team.Type team) => team == Team.Type.None ? "None" :
            team == Team.Type.Monsters ? "Monsters" : team == Team.Type.Heroes ? "Heroes" :
            team == (Team.Type.Monsters | Team.Type.Heroes) ? "Both" : throw new InvalidOperationException("Unknown percentage team mask.");
        internal static RelicPercentageStat Capture(RelicEffectModifyCharacterAttackOrHealthPercentage effect, int index, SaveManager save)
        {
            object Field(string name) => AccessTools.Field(effect.GetType(), name).GetValue(effect);
            var data = (RelicEffectData)AccessTools.Field(typeof(RelicEffectBase), "_srcRelicEffectData").GetValue(effect);
            var subtype = (SubtypeData?)Field("characterSubtype");
            var required = (MutatorData?)Field("requiredMutator");
            return new RelicPercentageStat(index, data.GetParamInt(), data.GetParamBool(), TeamName((Team.Type)Field("sourceTeam")),
                subtype == null || subtype.IsNone ? "" : subtype.Key, subtype?.IsPyre == true,
                required == null ? null : required.GetID(), required == null || save.GetHasMutator(required), data.GetEffectConditions().Count != 0,
                (float)Field("percentModification"));
        }
        // Only a fully represented combat-only roster bypasses the old broad
        // mutator guard. Companion effects and mixed Covenant projections retain
        // their ordinary validation and calibration requirements.
        internal static bool SupportedMutators(AllGameManagers managers)
        {
            var save = managers.GetSaveManager();
            return save.GetCovenants().Count == 0 && save.GetMutators().Count > 0 &&
                save.GetMutators().All(owner => OriginalStartRoomAttachmentScenario.SupportsInfernoMutator(owner, save) ||
                    owner.GetRelicDataID() == RelicUnitSpawnCountModel.DataId &&
                    owner.GetEffects() is [RelicEffectExtraSpawnsFirstUnitPerTurn] ||
                    owner.GetRelicDataID() == RelicModel.PreventMonsterDamageOwnerId &&
                    owner.GetEffects() is [RelicEffectPreventMonstersDamagingHeroes] ||
                    owner.GetEffects().Count > 0 && owner.GetEffects().All(effect =>
                    (effect is RelicEffectModifyCharacterAttackOrHealthPercentage || effect is RelicEffectMultiplyAttackLate ||
                        effect is RelicEffectApplyExtraPyreDamage || effect is RelicEffectAddUnitUpgradePerPile ||
                        effect is RelicEffectScaleStatusApplicationsOnSpawn || effect is RelicEffectIncreasePyreAttack ||
                        effect is RelicEffectIncreasePyreHealth ||
                        owner.GetRelicDataID() == RelicOpeningCapacityModel.MutatorId && owner.GetEffects().Count == 1 &&
                        effect is RelicEffectModifyRandomRoomCapacity ||
                        owner.GetRelicDataID() == RelicGoldMagicPowerModel.MutatorId && owner.GetEffects().Count == 1 &&
                        effect is RelicEffectModifyMagicPowerPerGold ||
                        RelicTurnEndBumpModel.IsOwner(owner.GetRelicDataID()) && owner.GetEffects().Count == 1 &&
                        effect is RelicEffectBumpOnTurnEnd ||
                        owner.GetRelicDataID() == RelicHoldoverGrantModel.OwnerId && owner.GetEffects().Count == 1 &&
                        effect is RelicEffectAddTraitToHand ||
                        owner.GetRelicDataID() is RelicResourceProbe.AfterDrawMorselsId or RelicResourceProbe.AfterDrawCultLeaderId &&
                        owner.GetEffects().Count == 1 && effect is RelicEffectSpawnMonsterOnTurnStart) &&
                    ((RelicEffectData)AccessTools.Field(typeof(RelicEffectBase), "_srcRelicEffectData").GetValue(effect)).GetEffectConditions().Count == 0));
        }
    }
}
