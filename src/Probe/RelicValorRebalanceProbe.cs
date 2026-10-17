using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class RelicValorRebalanceProbe
    {
        internal sealed class Observation
        {
            public int Turn { get; set; }
            public int UnitId { get; set; }
            public int CardInstanceId { get; set; }
            public int ValorStacks { get; set; }
            public int BaseAttackPerStack { get; set; }
            public int RelicAttackPerStack { get; set; }
            public int AttackPerStack { get; set; }
            public int BaseArmorPerStack { get; set; }
            public int RelicArmorPerStack { get; set; }
            public int ArmorPerStack { get; set; }
            public int TargetArmor { get; set; }
            public int ActualArmor { get; set; }
        }

        internal static List<Observation> Observations { get; } = new List<Observation>();
    }

    [HarmonyPatch(typeof(StatusEffectValor), "ChangeCharacterArmor")]
    internal static class RelicValorRebalanceProbe_ChangeCharacterArmor
    {
        [HarmonyPostfix]
        private static void Postfix(StatusEffectValor __instance, CharacterState character, int targetAmount)
        {
            if (!OriginalRebalanceValorScenario.Prepared || FullBattleTrace.Active == null ||
                AllGameManagers.Instance!.GetSaveManager().PreviewMode ||
                !OriginalRebalanceValorScenario.IsScenarioCard(character.GetSpawnerCard())) return;

            var effects = new List<IRebalanceValorRelicEffect>();
            AllGameManagers.Instance.GetRelicManager().GetRelicEffects(effects);
            int relicAttack = effects.Sum(item => item.AttackIncrease());
            int relicArmor = 0;
            foreach (IRebalanceValorRelicEffect effect in effects) relicArmor += effect.ArmorIncrease();
            StatusEffectValorProbeRecord(character, __instance, relicAttack, relicArmor, targetAmount);
        }

        private static void StatusEffectValorProbeRecord(CharacterState character, StatusEffectValor effect,
            int relicAttack, int relicArmor, int targetAmount)
        {
            RelicValorRebalanceProbe.Observations.Add(new RelicValorRebalanceProbe.Observation
            {
                Turn = AllGameManagers.Instance!.GetCombatManager()!.GetTurnCount(),
                UnitId = FullBattleTrace.Active!.UnitId(character),
                CardInstanceId = FullBattleTrace.Active.CardId(character.GetSpawnerCard()!),
                ValorStacks = character.GetStatusEffectStacks("valor"),
                BaseAttackPerStack = effect.GetParamInt(),
                RelicAttackPerStack = relicAttack,
                AttackPerStack = effect.GetMagnitudePerStack(),
                BaseArmorPerStack = effect.GetParamInt(),
                RelicArmorPerStack = relicArmor,
                ArmorPerStack = effect.GetSecondaryMagnitudePerStack(),
                TargetArmor = targetAmount,
                ActualArmor = character.GetStatusEffectStacks("armor")
            });
        }
    }
}
