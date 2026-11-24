using System;
using System.Linq;
using MonsterTrain2Poju.Model;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal static class RelicSkullCollectorsProbe
    {
        private static readonly Guid Module = new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d");
        internal static bool Matches(SaveManager save, RelicState owner) =>
            Application.version == "2.3.1" && typeof(HeroManager).Module.ModuleVersionId == Module &&
            save.GetRunType() == RunType.Class && save.GetCovenants().Count == 0 && save.GetMutators().Count == 1 &&
            RelicSkullCollectorsModel.Tiers.Any(tier => tier.Distance == save.GetCurrentDistance() &&
                tier.Scenario == save.GetCurrentScenarioData()?.name) &&
            owner.GetRelicDataID() == RelicSkullCollectorsModel.OwnerId &&
            owner.GetSourceRelicData() is MutatorData definition && definition.name == RelicSkullCollectorsModel.OwnerKey &&
            !owner.DisallowedInPlacementPhase && owner.GetEffects() is
            [RelicEffectSubstituteTreasureUnitOnSpawn,
                RelicEffectModifyCharacterMaxHealth, RelicEffectModifyCharacterMaxHealth, RelicEffectModifyCharacterMaxHealth,
                RelicEffectModifyCharacterAttackDamage, RelicEffectModifyCharacterAttackDamage, RelicEffectModifyCharacterAttackDamage] &&
            definition.GetEffects() is [var substitution, _, _, _, _, _, _] && substitution.GetEffectConditions().Count == 0 &&
            substitution.GetParamSourceTeam() == Team.Type.None && substitution.GetTargetMode() == TargetMode.Room &&
            substitution.GetParamCharacterSubstitution() != null &&
            definition.GetEffects().Skip(1).Select(MatchesStatEffect).All(matches => matches);

        private static bool MatchesStatEffect(RelicEffectData effect, int index)
        {
            int tierCount = RelicSkullCollectorsModel.Tiers.Count;
            var tier = RelicSkullCollectorsModel.Tiers[index % tierCount];
            return effect.GetParamInt() == (index < tierCount ? tier.Health : tier.Attack) &&
                effect.GetEffectConditions().Count == 0 && effect.GetParamSourceTeam() == Team.Type.Heroes &&
                effect.GetTargetMode() == TargetMode.Room && effect.GetParamTrigger() == CharacterTriggerData.Trigger.OnSpawn &&
                effect.GetParamMutatorData() == null && !effect.GetParamBool() && effect.GetParamExcludeCharacterSubtypes().Length == 0 &&
                effect.GetParamCharacterSubtype()?.Key == tier.Subtype;
        }

        internal static EnemyDefinition Project(CharacterData candidate, RelicEffectSubstituteTreasureUnitOnSpawn effect, int distance)
        {
            string originalId = candidate.GetID();
            var tier = RelicSkullCollectorsModel.Tiers.FirstOrDefault(item => item.Distance == distance && item.OriginalId == originalId);
            if (tier.OriginalId == null || !effect.TryGetSubstitution(candidate, out CharacterData? replacement) ||
                replacement == null || replacement.GetID() != tier.ReplacementId)
                return EnemySpawningProbe.Definition(candidate);
            EnemyDefinition resolved = EnemySpawningProbe.Definition(replacement);
            return new EnemyDefinition(resolved.Unit, resolved.Ascends, resolved.Loops, resolved.ExternalInteractions,
                resolved.CompanionBoss, resolved.BirthStats, treasureOriginalId: originalId);
        }
    }
}
