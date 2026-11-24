using System;
using System.Linq;
using MonsterTrain2Poju.Model;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal static class RelicSkullCollectorsProbe
    {
        private static readonly Guid Module = new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d");
        private static readonly int[] Amounts = { 30, 60, 90, 5, 15, 30 };
        internal static bool Matches(SaveManager save, RelicState owner) =>
            Application.version == "2.3.1" && typeof(HeroManager).Module.ModuleVersionId == Module &&
            !save.IsRegionRun && save.GetCurrentDistance() == 1 && save.GetCovenants().Count == 0 &&
            save.GetCurrentScenarioData()?.name == "Level2BattleJunker" && save.GetMutators().Count == 1 &&
            owner.GetRelicDataID() == RelicSkullCollectorsModel.OwnerId &&
            owner.GetSourceRelicData() is MutatorData definition && definition.name == RelicSkullCollectorsModel.OwnerKey &&
            !owner.DisallowedInPlacementPhase && owner.GetEffects() is
            [RelicEffectSubstituteTreasureUnitOnSpawn,
                RelicEffectModifyCharacterMaxHealth, RelicEffectModifyCharacterMaxHealth, RelicEffectModifyCharacterMaxHealth,
                RelicEffectModifyCharacterAttackDamage, RelicEffectModifyCharacterAttackDamage, RelicEffectModifyCharacterAttackDamage] &&
            definition.GetEffects() is [var substitution, _, _, _, _, _, _] && substitution.GetEffectConditions().Count == 0 &&
            substitution.GetParamSourceTeam() == Team.Type.None && substitution.GetTargetMode() == TargetMode.Room &&
            substitution.GetParamCharacterSubstitution() != null && definition.GetEffects().Skip(1).Select((effect, index) =>
                effect.GetParamInt() == Amounts[index] && effect.GetEffectConditions().Count == 0 &&
                effect.GetParamSourceTeam() == Team.Type.Heroes && effect.GetTargetMode() == TargetMode.Room &&
                effect.GetParamTrigger() == CharacterTriggerData.Trigger.OnSpawn && effect.GetParamMutatorData() == null &&
                !effect.GetParamBool() && effect.GetParamExcludeCharacterSubtypes().Length == 0 &&
                effect.GetParamCharacterSubtype()?.Key == "SubtypesData_TreasureCollectorT" + (index % 3 + 1)).All(matches => matches);

        internal static EnemyDefinition Project(CharacterData candidate, RelicEffectSubstituteTreasureUnitOnSpawn effect)
        {
            if (candidate.GetID() != RelicSkullCollectorsModel.StockTierOneId || !effect.TryGetSubstitution(candidate, out CharacterData? replacement) ||
                replacement == null || replacement.GetID() != RelicSkullCollectorsModel.ReplacementTierOneId)
                return EnemySpawningProbe.Definition(candidate);
            EnemyDefinition resolved = EnemySpawningProbe.Definition(replacement);
            return new EnemyDefinition(resolved.Unit, resolved.Ascends, resolved.Loops, resolved.ExternalInteractions,
                resolved.CompanionBoss, resolved.BirthStats, treasureOriginalId: candidate.GetID());
        }
    }
}
