using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class DamageScalingScenario
    {
        internal static readonly List<Sample> Samples = new List<Sample>();
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            CardState[] owned = managers.GetCardManager()!.GetAllCards(new List<CardState>()).ToArray();
            CardState[] spells = owned.Where(card => card.GetCardType() == CardType.Spell &&
                card.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectFloorRearrange")).ToArray();
            CardState[] stewards = owned.Where(card => card.GetCardDataID() == "d14a50f3-728d-43e1-87f0-ef1b013f6678").ToArray();
            if (spells.Length == 0 || stewards.Length == 0) throw new InvalidOperationException("Scaling fixture requires the rearrangement spell and Stewards.");
            CardData spell = save.GetAllGameData().FindCardData(spells[0].GetCardDataID())!;
            spell.GetTraits().Clear();
            spell.GetTraits().Add(Trait("AnyHeroKilled", "ThisBattle", 3, .5f, false));
            spell.GetTraits().Add(Trait("AnyCardPlayed", "ThisTurn", 2, .75f, true));
            spell.GetTraits().Add(Trait("MagicPowerInTargetRoom", "ThisTurn", 10, 1f, true));
            spell.GetEffects().Clear();
            foreach (int amount in new[] { 3, 0 })
            {
                var effect = new CardEffectData("CardEffectDamage", null!, Team.Type.Heroes);
                effect.Cheat_SetTargetMode(TargetMode.Tower); Set(effect, "paramInt", amount); spell.GetEffects().Add(effect);
            }
            Set(spell, "targetless", false); Set(spell, "targetsRoom", true);
            foreach (CardState card in spells)
            {
                card.Setup(spell, save);
                var permanent = new CardUpgradeState(); permanent.Setup(); permanent.SetAttackDamage(-3);
                card.ApplyPermanentUpgrade(permanent, save, ignoreUpgradeAnimation: true);
                var temporary = new CardUpgradeState(); temporary.Setup(); temporary.SetAttackDamage(2);
                card.ApplyTemporaryUpgrade(temporary, save);
            }
            CardData steward = save.GetAllGameData().FindCardData(stewards[0].GetCardDataID())!;
            steward.GetTraits().Add(Trait("AnyHeroKilled", "ThisBattle", 1, .5f, true));
            steward.GetTraits().Add(Trait("LastAttackDamageDealt", "ThisTurn", 1, .25f, true));
            foreach (CardState card in stewards)
            {
                card.Setup(steward, save);
                var upgrade = new CardUpgradeState(); upgrade.Setup(); upgrade.SetAdditionalHP(20); upgrade.SetAttackDamage(2);
                upgrade.AddStatusEffectUpgradeStacks("multistrike", 1); upgrade.AddStatusEffectUpgradeStacks("spikes", 2);
                card.ApplyPermanentUpgrade(upgrade, save, ignoreUpgradeAnimation: true);
            }
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("DAMAGE-SCALING-PREPARED replacement/additive traits in order, fractional floors, signed upgrade floors, repeated tower targets, unit multistrike and spikes; natural boss/waves retained.");
        }
        internal static CardTraitData Trait(string type, string duration, int amount, float multiplier, bool additive)
        {
            var data = new CardTraitData(); data.Setup("CardTraitScalingAddDamage"); data.SetParamInt(amount);
            Set(data, "paramTrackedValue", Enum.Parse(typeof(CardStatistics.TrackedValueType), type));
            Set(data, "paramEntryDuration", Enum.Parse(typeof(CardStatistics.EntryDuration), duration));
            Set(data, "paramCardType", CardStatistics.CardTypeTarget.Any); Set(data, "paramFloat", multiplier);
            Set(data, "paramBool", additive); Set(data, "paramStatusEffects", Array.Empty<StatusEffectStackData>());
            return data;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
        internal sealed class Sample
        {
            public int ActionIndex { get; set; }
            public int TurnIndex { get; set; }
            public int OwnerCardId { get; set; }
            public int DamageSourceCardId { get; set; }
            public string DamageType { get; set; } = "";
            public int IncomingDamage { get; set; }
            public ScalingDamageTrait Trait { get; set; } = null!;
            public CombatContext Before { get; set; } = null!;
            public int ActualDamage { get; set; }
            public CombatContext? After { get; set; }
            public string? Difference { get; set; }
            public string? CaptureError { get; set; }
        }
        [HarmonyPatch(typeof(CardTraitScalingAddDamage), nameof(CardTraitScalingAddDamage.OnApplyingDamage))]
        private static class ApplyingPatch
        {
            private static void Prefix(CardTraitScalingAddDamage __instance, CardTraitState.ApplyingDamageParameters damageParams, out Sample? __state)
            {
                __state = null;
                FullBattleTrace? trace = FullBattleTrace.Active;
                if (Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") != "damage-scaling" || trace == null ||
                    AllGameManagers.Instance!.GetSaveManager().PreviewMode || !trace.PendingActionIndex.HasValue && !trace.PendingTurnIndex.HasValue) return;
                var sample = new Sample { ActionIndex = trace.PendingActionIndex ?? -1, TurnIndex = trace.PendingTurnIndex ?? -1,
                    IncomingDamage = damageParams.damage, DamageType = damageParams.damageType.ToString() };
                __state = sample; Samples.Add(sample);
                try
                {
                    CardState owner = __instance.GetCard();
                    sample.OwnerCardId = trace.CardId(owner);
                    sample.DamageSourceCardId = damageParams.damageSourceCard == null ? 0 : trace.CardId(damageParams.damageSourceCard);
                    int index = owner.GetTraitStates().OfType<CardTraitScalingAddDamage>().ToList().IndexOf(__instance);
                    sample.Trait = DamageScalingProbe.Capture(owner)![index];
                    sample.Before = trace.CaptureContext();
                }
                catch (Exception exception) { sample.CaptureError = exception.ToString(); }
            }
            private static void Postfix(int __result, Sample? __state)
            {
                if (__state == null) return;
                __state.ActualDamage = __result;
                try
                {
                    __state.After = FullBattleTrace.Active!.CaptureContext();
                    if (__state.CaptureError != null) return;
                    DamageScalingResult predicted = DamageScalingModel.ApplyTrait(__state.Before, __state.Trait,
                        __state.OwnerCardId, __state.DamageSourceCardId, __state.IncomingDamage);
                    __state.Difference = !predicted.Supported ? predicted.UnsupportedReason : predicted.Damage != __result
                        ? "Native trait damage differs: " + predicted.Damage + " != " + __result
                        : !JToken.DeepEquals(JToken.FromObject(predicted.Context!), JToken.FromObject(__state.After)) ? "Native trait context differs." : null;
                }
                catch (Exception exception) { __state.CaptureError = exception.ToString(); }
            }
        }
    }
}
