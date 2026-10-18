using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalBoneDogScenario
    {
        internal const string Name = "original-bone-dog";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        internal static readonly List<Record> Records = new List<Record>();
        internal static readonly List<DamageRecord> DamageRecords = new List<DamageRecord>();
        internal static readonly List<PreviewRecord> PreviewRecords = new List<PreviewRecord>();
        private static bool observing, consumedControl;
        private static Record? active;
        private static DamageRecord? damageRecord;
        internal sealed class Record
        {
            public bool Preview { get; set; }
            internal PreviewRecord? PreviewObservation;
            public TrainCombatState Before { get; set; } = null!;
            public TrainCombatState? Predicted { get; set; }
            public TrainCombatState? Actual { get; set; }
            public bool Returned { get; set; }
            public float Percentage { get; set; }
            public float PredictedPercentage { get; set; }
            public List<string> Notifications { get; set; } = new List<string>();
            public bool Completed { get; set; }
            public string? Error { get; set; }
            public string? Difference { get; set; }
            public string? UnsupportedReason { get; set; }
        }
        internal sealed class DamageRecord
        {
            public int Damage { get; set; }
            public int BeforeHP { get; set; }
            public int AfterHP { get; set; }
            public int MaxHP { get; set; }
            public int BeforeFlag { get; set; }
            public int AfterFlag { get; set; }
            public List<string> Events { get; set; } = new List<string>();
            public bool Completed { get; set; }
        }
        internal sealed class PreviewRecord
        {
            public int BeforeFlag { get; set; }
            public int AfterFlag { get; set; }
            public RelicConditionState[] BeforeConditions { get; set; } = null!;
            public RelicConditionState[] AfterConditions { get; set; } = null!;
            public bool Returned { get; set; }
            public float Percentage { get; set; }
            public string[] Notifications { get; set; } = Array.Empty<string>();
            public bool Completed { get; set; }
        }
        private static int Flag() => AllGameManagers.Instance!.GetSaveManager().GetRelicPersistentState().HasPyreResurrectionTriggeredThisRun() ? 1 : 0;
        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager(); AllGameData data = save.GetAllGameData();
            if (Acquired || save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle || Flag() != 0)
                throw new InvalidOperationException("Expected fresh pre-battle Bone Dog acquisition.");
            CardData card = save.GetDeckState().Select(item => data.FindCardData(item.GetCardDataID()))
                .First(item => item?.GetSpawnCharacterData()?.name.StartsWith("TrainSteward", StringComparison.Ordinal) == true)!;
            // Author only the isolated battle participants; leave the original owner/effect unchanged.
            foreach (CharacterData enemy in data.GetAllCharacterData()) Set(enemy, "attackDamage", 1);
            CharacterData unit = card.GetSpawnCharacterData();
            Set(unit, "health", 1); Set(unit, "attackDamage", 0); Set(unit, "size", 1);
            Set(unit, "triggers", new List<CharacterTriggerData>()); Set(unit, "startingStatusEffects", Array.Empty<StatusEffectStackData>());
            Set(unit, "unitAbility", null!); Set(card, "cost", 0);
            Set(save.GetActiveSavePyreCharacterData().GetPyreHeartData(), "attack", 100);
            save.Cheat_SetTowerMaxHP(81); save.Cheat_SetTowerHP(1);
            save.GetDeckState().Clear(); save.AddCardToDeck(card, applyExistingRelicModifiers: false, showAnimation: false, notifyDeckCount: false);
            CollectableRelicData owner = data.GetAllCollectableRelicData().Single(item => item.GetID() == RelicPyreResurrectionModel.DataId);
            if (owner.name != RelicPyreResurrectionModel.AssetKey || owner.GetEffects().Count != 1 ||
                owner.GetEffects()[0].GetEffectClassName() != RelicPyreResurrectionModel.Effect || owner.GetEffects()[0].GetParamFloat() != .25f)
                throw new InvalidOperationException("Original Bone Dog definition changed.");
            save.AddRelic(owner);
            string? error = RelicModel.Validate(RelicProbe.Capture(managers));
            if (error != null) throw new InvalidOperationException(error);
            Acquired = true; log.LogInfo("ORIGINAL-BONE-DOG-ACQUIRED unchanged 25% once-per-run resurrection; isolated maxHP81, HP1, natural enemies.");
        }
        internal static void PrepareDeployment(AllGameManagers managers)
        {
            if (Prepared || !Acquired || managers.GetCombatManager()!.GetTurnCount() != 0) throw new InvalidOperationException("Expected Bone Dog deployment.");
            CardManager cards = managers.GetCardManager()!;
            foreach (CardState card in cards.GetAllCards(new List<CardState>())) if (!cards.GetHand().Contains(card)) cards.DrawSpecificCard(card);
            Prepared = true; FullBattleTrace.Active!.InvalidateCardRules(); Set(managers.GetCombatManager(), "combatStateChanged", true);
        }
        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            if (source.Spawn.Turn > 0 && Flag() == 1 && !consumedControl)
            {
                consumedControl = true;
                if (AllGameManagers.Instance!.GetRelicManager().TryApplyPyreHeartResurrection(out float percentage) || percentage != 0)
                    throw new InvalidOperationException("Consumed resurrection unexpectedly reset on a later turn.");
            }
            return RelicPyreResurrectionModel.ChoosePlay(source);
        }
        [HarmonyPatch(typeof(RelicManager), nameof(RelicManager.TryApplyPyreHeartResurrection))]
        private static class ResurrectionPatch
        {
            private static void Prefix(out Record? __state)
            {
                __state = null;
                if (!Prepared || observing) return;
                if (AllGameManagers.Instance!.GetSaveManager().PreviewMode)
                {
                    var preview = new PreviewRecord { BeforeFlag = Flag(), BeforeConditions = RelicCardModifierProbe.Conditions(
                        AllGameManagers.Instance.GetRelicManager().GetRelicEffect<RelicEffectPyreHeartResurrection>()) };
                    PreviewRecords.Add(preview); __state = new Record { Preview = true, PreviewObservation = preview }; active = __state;
                    return; // Preview saves intentionally lack the primary Pyre resource definition.
                }
                observing = true; var row = new Record { Preview = AllGameManagers.Instance!.GetSaveManager().PreviewMode }; __state = row; Records.Add(row);
                try
                {
                    row.Before = FullBattleTrace.Active!.CaptureTrain();
                    CombatContext predicted = RelicPyreResurrectionModel.TryApply(row.Before.Context!, out float percentage, out string? error);
                    row.PredictedPercentage = percentage; row.UnsupportedReason = error;
                    row.Predicted = CardSpellModel.WithContext(row.Before, predicted); active = row;
                    damageRecord?.Events.Add("Try:" + Flag());
                }
                catch (Exception error) { row.Error = error.ToString(); }
                finally { observing = false; }
            }
            private static void Postfix(bool __result, float percentage, Record? __state)
            {
                if (__state == null) return; observing = true;
                try
                {
                    if (__state.PreviewObservation is PreviewRecord preview)
                    {
                        preview.AfterFlag = Flag(); preview.Returned = __result; preview.Percentage = percentage;
                        preview.AfterConditions = RelicCardModifierProbe.Conditions(AllGameManagers.Instance!.GetRelicManager().GetRelicEffect<RelicEffectPyreHeartResurrection>());
                        preview.Notifications = __state.Notifications.ToArray(); preview.Completed = true;
                        return;
                    }
                    __state.Returned = __result; __state.Percentage = percentage;
                    __state.Actual = FullBattleTrace.Active!.CaptureTrain(); __state.Completed = true;
                    if (__result != (__state.PredictedPercentage > 0) || percentage != __state.PredictedPercentage ||
                        !JToken.DeepEquals(JToken.FromObject(__state.Predicted!), JToken.FromObject(__state.Actual)))
                        __state.Difference = "Native resurrection manager state or result differs.";
                    damageRecord?.Events.Add("Return:" + Flag());
                }
                catch (Exception error) { __state.Error = error.ToString(); }
                finally { active = null; observing = false; }
            }
        }
        [HarmonyPatch(typeof(CharacterState), nameof(CharacterState.ApplyDamage))]
        private static class DamagePatch
        {
            private static void Postfix(CharacterState __instance, int damage, ref IEnumerator __result)
            { if (Prepared && __instance.IsPyreHeart() && !AllGameManagers.Instance!.GetSaveManager().PreviewMode) __result = ObserveDamage(__result, __instance, damage); }
        }
        private static IEnumerator ObserveDamage(IEnumerator native, CharacterState target, int damage)
        {
            var row = new DamageRecord { Damage = damage, BeforeHP = target.GetHP(), MaxHP = target.GetMaxHP(), BeforeFlag = Flag() };
            DamageRecords.Add(row); damageRecord = row;
            try { while (native.MoveNext()) yield return native.Current; }
            finally { (native as IDisposable)?.Dispose(); row.AfterHP = target.GetHP(); row.AfterFlag = Flag(); row.Completed = true; damageRecord = null; }
        }
        [HarmonyPatch(typeof(CharacterState), "UpdateHp")]
        private static class HpPatch
        {
            private static void Postfix(CharacterState __instance)
            { if (damageRecord != null && __instance.IsPyreHeart()) damageRecord.Events.Add("HP:" + __instance.GetHP() + ":" + Flag()); }
        }
        [HarmonyPatch(typeof(RelicManager), nameof(RelicManager.NotifyRelicTriggered))]
        private static class TriggerPatch
        {
            private static void Prefix(RelicState triggeredRelic)
            {
                if (active != null && triggeredRelic.GetRelicDataID() == RelicPyreResurrectionModel.DataId)
                { active.Notifications.Add("Triggered:" + Flag()); damageRecord?.Events.Add("Triggered:" + Flag()); }
            }
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
