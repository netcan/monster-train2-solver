using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class StatusScalingScenario
    {
        internal static readonly List<Sample> Samples = new List<Sample>();
        internal static readonly List<Application> Applications = new List<Application>();
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager(); save.SetForgePoints(3);
            CardState[] owned = managers.GetCardManager()!.GetAllCards(new List<CardState>()).ToArray();
            CardState[] spells = owned.Where(card => card.GetCardType() == CardType.Spell &&
                card.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectFloorRearrange")).ToArray();
            if (spells.Length == 0) throw new InvalidOperationException("Status fixture requires the owned rearrangement spell.");
            CardState steward = owned.First(card => card.GetCardDataID() == "d14a50f3-728d-43e1-87f0-ef1b013f6678");
            var immune = new CardUpgradeState(); immune.Setup(); immune.AddStatusEffectUpgradeStacks("immune", 1);
            steward.ApplyPermanentUpgrade(immune, save, ignoreUpgradeAnimation: true);
            CardData data = save.GetAllGameData().FindCardData(spells[0].GetCardDataID())!;
            data.GetTraits().Clear();
            data.GetTraits().Add(Trait("ForgePoints", 1, true, 0, "armor"));
            data.GetTraits().Add(Trait("TurnCount", 1, true, 0, "armor"));
            data.GetTraits().Add(Trait("TurnCount", 1, false, 1));
            data.GetTraits().Add(Trait("AnyStatusEffectStacksAdded", -1, false, 0, "armor"));
            data.GetTraits().Add(Trait("MoonPhase", 1, false, 2));
            data.GetTraits().Add(Trait("MoonPhase", 1, false, 0, "regen"));
            data.GetEffects().Clear();
            var opener = new CardEffectData("CardEffectDamage", null!, Team.Type.Heroes);
            opener.Cheat_SetTargetMode(TargetMode.Room); data.GetEffects().Add(opener);
            foreach (int count in new[] { 0, 20000, -40000 }) data.GetEffects().Add(Status("armor", count));
            data.GetEffects().Add(Status("regen", 0));
            var damage = new CardEffectData("CardEffectDamage", null!, Team.Type.Heroes);
            damage.Cheat_SetTargetMode(TargetMode.Tower); Set(damage, "paramInt", 40); data.GetEffects().Add(damage);
            Set(data, "targetless", false); Set(data, "targetsRoom", true);
            foreach (CardState card in spells) card.Setup(data, save);
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("STATUS-SCALING-PREPARED ordered zero-only/list/propagatable/horde traits, signed additions, stack caps, source feedback, immunity and native turn/moon; natural boss/waves retained.");
        }
        private static CardTraitData Trait(string type, int amount, bool onlyZero, int filter, params string[] ids)
        {
            CardTraitData data = DamageScalingScenario.Trait(type, "ThisTurn", amount, 1, false);
            data.Setup("CardTraitScalingAddStatusEffect"); data.SetParamInt(amount);
            Set(data, "paramTrackedValue", Enum.Parse(typeof(CardStatistics.TrackedValueType), type));
            Set(data, "paramEntryDuration", CardStatistics.EntryDuration.ThisTurn);
            Set(data, "paramCardType", CardStatistics.CardTypeTarget.Any);
            Set(data, "paramBool", onlyZero); Set(data, "paramInt2", filter);
            Set(data, "paramStatusEffects", ids.Select(id => new StatusEffectStackData { statusId = id, count = 1 }).ToArray());
            return data;
        }
        private static CardEffectData Status(string id, int count)
        {
            var effect = new CardEffectData("CardEffectAddStatusEffect", null!, Team.Type.Heroes | Team.Type.Monsters);
            effect.Cheat_SetTargetMode(TargetMode.Room);
            Set(effect, "paramStatusEffects", new[] { new StatusEffectStackData { statusId = id, count = count } });
            return effect;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
        private static FullBattleTrace? Active()
        {
            FullBattleTrace? trace = FullBattleTrace.Active;
            return Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "status-scaling" && trace != null &&
                !AllGameManagers.Instance!.GetSaveManager().PreviewMode && (trace.PendingActionIndex.HasValue || trace.PendingTurnIndex.HasValue) ? trace : null;
        }
        internal sealed class Sample
        {
            public int ActionIndex { get; set; }
            public int OwnerCardId { get; set; }
            public CombatTeam TargetTeam { get; set; }
            public string StatusId { get; set; } = "";
            public int SourceStacks { get; set; }
            public int ActualBonus { get; set; }
            public ScalingStatusTrait Trait { get; set; } = null!;
            public CombatContext Before { get; set; } = null!;
            public CombatContext? After { get; set; }
            public string? Difference { get; set; }
            public string? CaptureError { get; set; }
        }
        [HarmonyPatch(typeof(CardTraitScalingAddStatusEffect), nameof(CardTraitScalingAddStatusEffect.OnStatusEffectApplied))]
        private static class TraitPatch
        {
            private static void Prefix(CardTraitScalingAddStatusEffect __instance, CharacterState affectedCharacter, string statusId, int sourceStacks, out Sample? __state)
            {
                __state = null; FullBattleTrace? trace = Active(); if (trace == null) return;
                var sample = new Sample { ActionIndex = trace.PendingActionIndex ?? -1, StatusId = statusId, SourceStacks = sourceStacks,
                    TargetTeam = affectedCharacter.GetTeamType() == Team.Type.Heroes ? CombatTeam.Enemy : CombatTeam.Player };
                __state = sample; Samples.Add(sample);
                try
                {
                    CardState owner = __instance.GetCard(); sample.OwnerCardId = trace.CardId(owner);
                    int index = owner.GetTraitStates().OfType<CardTraitScalingAddStatusEffect>().ToList().IndexOf(__instance);
                    sample.Trait = StatusScalingProbe.Capture(owner)![index]; sample.Before = trace.CaptureContext();
                }
                catch (Exception exception) { sample.CaptureError = exception.ToString(); }
            }
            private static void Postfix(int __result, Sample? __state)
            {
                if (__state == null) return; __state.ActualBonus = __result;
                try
                {
                    __state.After = FullBattleTrace.Active!.CaptureContext(); if (__state.CaptureError != null) return;
                    StatusScalingResult predicted = StatusScalingModel.ApplyTrait(__state.Before, __state.Trait, __state.OwnerCardId,
                        __state.TargetTeam, __state.StatusId, __state.SourceStacks);
                    __state.Difference = !predicted.Supported ? predicted.UnsupportedReason : predicted.Stacks != __result ? "Native status bonus differs." :
                        !JToken.DeepEquals(JToken.FromObject(predicted.Context!), JToken.FromObject(__state.After)) ? "Native status trait context differs." : null;
                }
                catch (Exception exception) { __state.CaptureError = exception.ToString(); }
            }
        }
        internal sealed class Application
        {
            public int SourceCardId { get; set; }
            public int TargetId { get; set; }
            public CombatStatus Added { get; set; } = null!;
            public bool OverrideImmunity { get; set; }
            public bool AllowModification { get; set; }
            public RoomCombatState Before { get; set; } = null!;
            public RoomCombatState? After { get; set; }
            public string? Difference { get; set; }
            public string? CaptureError { get; set; }
        }
        [HarmonyPatch(typeof(CharacterState), nameof(CharacterState.AddStatusEffect), new[] { typeof(string), typeof(int), typeof(CharacterState.AddStatusEffectParams),
            typeof(CharacterState), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool) })]
        private static class ApplyPatch
        {
            private static void Prefix(CharacterState __instance, string statusId, int numStacks, CharacterState.AddStatusEffectParams addStatusEffectParams,
                bool allowModification, out Application? __state)
            {
                __state = null; FullBattleTrace? trace = Active(); if (trace == null || addStatusEffectParams?.sourceCardState == null) return;
                var sample = new Application(); __state = sample; Applications.Add(sample);
                try
                {
                    sample.SourceCardId = trace.CardId(addStatusEffectParams.sourceCardState); sample.TargetId = trace.UnitId(__instance);
                    sample.Added = BattleActionProbe.Status(statusId, numStacks); sample.AllowModification = allowModification;
                    sample.OverrideImmunity = addStatusEffectParams.overrideImmunity; sample.Before = trace.Capture(__instance.GetCurrentRoom());
                }
                catch (Exception exception) { sample.CaptureError = exception.ToString(); }
            }
            private static void Postfix(CharacterState __instance, Application? __state)
            {
                if (__state == null) return;
                try
                {
                    __state.After = FullBattleTrace.Active!.Capture(__instance.GetCurrentRoom()); if (__state.CaptureError != null) return;
                    RoomCombatResult predicted = StatusApplicationModel.Apply(__state.Before, __state.TargetId, __state.Added,
                        __state.SourceCardId, __state.OverrideImmunity, __state.AllowModification);
                    __state.Difference = !predicted.Supported ? predicted.UnsupportedReason :
                        !JToken.DeepEquals(JToken.FromObject(predicted.State!), JToken.FromObject(__state.After)) ? "Native status application differs." : null;
                }
                catch (Exception exception) { __state.CaptureError = exception.ToString(); }
            }
        }
    }
}
