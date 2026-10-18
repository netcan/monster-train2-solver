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
    internal static class OriginalDecayDeathScenario
    {
        internal const string Name = "original-decay-death";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        internal static readonly List<TestRecord> Tests = new List<TestRecord>();
        internal static readonly List<ApplicationRecord> Applications = new List<ApplicationRecord>();
        private static bool observing, controls;
        private static ApplicationRecord? active;
        internal sealed class TestRecord
        {
            public CombatUnit? Actor { get; set; }
            public string Trigger { get; set; } = "";
            public int DamageType { get; set; }
            public bool Expected { get; set; }
            public bool Returned { get; set; }
            public TrainCombatState Before { get; set; } = null!;
            public TrainCombatState? Actual { get; set; }
            public string? Error { get; set; }
            public string? Difference { get; set; }
        }
        internal sealed class ApplicationRecord
        {
            public CombatUnit Actor { get; set; } = null!;
            public CombatUnit? ActorAfter { get; set; }
            public int RoomIndex { get; set; }
            public TrainCombatState Before { get; set; } = null!;
            public TrainCombatState? Predicted { get; set; }
            public TrainCombatState? Actual { get; set; }
            public List<string> Notifications { get; set; } = new List<string>();
            public bool Completed { get; set; }
            public string? Error { get; set; }
            public string? Difference { get; set; }
            public string? UnsupportedReason { get; set; }
        }
        private static bool Active => Prepared && !observing && !AllGameManagers.Instance!.GetSaveManager().PreviewMode;
        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager(); AllGameData data = save.GetAllGameData();
            if (Acquired || save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("Expected fresh pre-battle Decay-death acquisition.");
            CardData template = save.GetDeckState().Select(item => data.FindCardData(item.GetCardDataID()))
                .First(item => item.GetSpawnCharacterData()?.name.StartsWith("TrainSteward", StringComparison.Ordinal) == true);
            foreach (CharacterData definition in data.GetAllCharacterData()) Set(definition, "attackDamage", 0);
            CharacterData mage = data.GetAllCharacterData().Single(item => item.GetAssetKey() == "MageT1_Junker");
            Set(mage, "health", 3); Set(mage, "triggers", new List<CharacterTriggerData>());
            Set(mage, "startingStatusEffects", new[] { new StatusEffectStackData { statusId = "decay", count = 5 } });
            save.GetDeckState().Clear();
            for (int index = 0; index < RelicDecayDeathModel.CardIds.Length; index++)
            {
                string id = RelicDecayDeathModel.CardIds[index];
                CardData card = UnityEngine.Object.Instantiate(template);
                CharacterData unit = UnityEngine.Object.Instantiate(template.GetSpawnCharacterData());
                card.name = unit.name = "PojuDecayDeath" + index; Set(card, "id", id); Set(unit, "id", id);
                Set(unit, "health", index == 0 || index == 4 ? 3 : 100); Set(unit, "attackDamage", index == 5 ? 100 : 0); Set(unit, "size", 1);
                Set(unit, "triggers", new List<CharacterTriggerData>()); Set(unit, "unitAbility", null!);
                Set(unit, "startingStatusEffects", index == 0 ? new[] {
                    new StatusEffectStackData { statusId = "decay", count = 5 }, new StatusEffectStackData { statusId = "regen", count = 2 },
                    new StatusEffectStackData { statusId = "spell weakness", count = 4 } } : index == 3 ? new[] {
                    new StatusEffectStackData { statusId = "untouchable", count = 1 } } : index == 4 ? new[] {
                    new StatusEffectStackData { statusId = "decay", count = 5 } } : Array.Empty<StatusEffectStackData>());
                var spawn = new CardEffectData("CardEffectSpawnMonster", null!, Team.Type.Monsters);
                Set(spawn, "targetMode", TargetMode.Room); Set(spawn, "paramCharacterData", unit);
                Set(card, "effects", new List<CardEffectData> { spawn }); Set(card, "cost", 0);
                Set(card, "traits", new List<CardTraitData>()); Set(card, "startingUpgrades", new List<CardUpgradeData>());
                ((List<CharacterData>)AccessTools.Field(typeof(AllGameData), "characterDatas").GetValue(data)).Add(unit);
                ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(data)).Add(card);
                save.AddCardToDeck(card, applyExistingRelicModifiers: false, showAnimation: false, notifyDeckCount: false);
            }
            save.AddRelic(data.GetAllCollectableRelicData().Single(item => item.GetID() == RelicDecayDeathModel.DataId));
            string? error = RelicModel.Validate(RelicProbe.Capture(managers));
            if (error != null) throw new InvalidOperationException(error);
            Acquired = true; log.LogInfo("ORIGINAL-DECAY-DEATH-ACQUIRED unchanged all-team lethal Decay OnHit, friendly even distribution, source status retained.");
        }
        internal static void PrepareDeployment(AllGameManagers managers)
        {
            if (Prepared || !Acquired || managers.GetCombatManager()!.GetTurnCount() != 0) throw new InvalidOperationException("Expected Decay-death deployment.");
            CardManager cards = managers.GetCardManager()!;
            foreach (CardState card in cards.GetAllCards(new List<CardState>())) if (!cards.GetHand().Contains(card)) cards.DrawSpecificCard(card);
            Prepared = true; FullBattleTrace.Active!.InvalidateCardRules(); Set(managers.GetCombatManager(), "combatStateChanged", true);
        }
        [HarmonyPatch(typeof(RelicEffectAddEffectOnUnitDeath), nameof(RelicEffectAddEffectOnUnitDeath.TestCharacterTriggerEffect))]
        private static class TestPatch
        {
            private static void Prefix(CharacterTriggerRelicEffectParams relicEffectParams, out TestRecord? __state)
            {
                __state = null; if (!Active) return;
                observing = true; var row = new TestRecord(); __state = row; Tests.Add(row);
                try
                {
                    row.Actor = relicEffectParams.characterState == null ? null : FullBattleTrace.Active!.CaptureUnit(relicEffectParams.characterState);
                    row.Trigger = relicEffectParams.trigger.ToString(); row.DamageType = relicEffectParams.paramInt2;
                    row.Before = FullBattleTrace.Active!.CaptureTrain(); row.Expected = RelicDecayDeathModel.Test(row.Actor, row.Trigger, row.DamageType);
                }
                catch (Exception error) { row.Error = error.ToString(); }
                finally { observing = false; }
            }
            private static void Postfix(bool __result, TestRecord? __state)
            {
                if (__state == null) return; observing = true;
                try
                {
                    __state.Returned = __result; __state.Actual = FullBattleTrace.Active!.CaptureTrain();
                    if (__result != __state.Expected || !JToken.DeepEquals(JToken.FromObject(__state.Before), JToken.FromObject(__state.Actual)))
                        __state.Difference = "Native Decay-death Test result or complete state differs.";
                }
                catch (Exception error) { __state.Error = error.ToString(); }
                finally { observing = false; }
            }
        }
        [HarmonyPatch(typeof(RelicEffectAddEffectOnUnitDeath), nameof(RelicEffectAddEffectOnUnitDeath.ApplyCharacterTriggerEffect))]
        private static class ApplyPatch
        {
            private static void Postfix(RelicEffectAddEffectOnUnitDeath __instance, CharacterTriggerRelicEffectParams relicEffectParams, ref IEnumerator __result)
            { if (Active) __result = Observe(__result, __instance, relicEffectParams); }
        }
        private static IEnumerator Observe(IEnumerator native, RelicEffectAddEffectOnUnitDeath effect, CharacterTriggerRelicEffectParams input)
        {
            var row = new ApplicationRecord(); Applications.Add(row); observing = true;
            try
            {
                row.Actor = FullBattleTrace.Active!.CaptureUnit(input.characterState); row.RoomIndex = input.characterState.GetCurrentRoomIndex();
                row.Before = FullBattleTrace.Active.CaptureTrain();
                RoomCombatState room = row.Before.Rooms.Single(item => item.RoomIndex == row.RoomIndex);
                RoomCombatResult predicted = RelicDecayDeathModel.Apply(room, row.Actor); row.UnsupportedReason = predicted.UnsupportedReason;
                if (predicted.Supported)
                    row.Predicted = CardSpellModel.WithContext(new TrainCombatState(row.Before.Rooms.Select(item => item.RoomIndex == row.RoomIndex ? predicted.State! : item).ToArray(),
                        row.Before.Movement, row.Before.EnemySlotsPerRoom, predicted.State!.Context), predicted.State.Context!);
                active = row;
            }
            catch (Exception error) { row.Error = error.ToString(); }
            finally { observing = false; }
            try { while (native.MoveNext()) yield return native.Current; row.Completed = true; }
            finally
            {
                (native as IDisposable)?.Dispose(); observing = true;
                try
                {
                    row.ActorAfter = FullBattleTrace.Active!.CaptureUnit(input.characterState); row.Actual = FullBattleTrace.Active.CaptureTrain();
                    if (row.Predicted != null && !JToken.DeepEquals(JToken.FromObject(row.Predicted), JToken.FromObject(row.Actual)))
                        row.Difference = "Native Decay-death transfer full state differs.";
                }
                catch (Exception error) { row.Error = error.ToString(); }
                finally { observing = false; active = null; }
            }
            if (!controls)
            {
                controls = true;
                foreach (var control in new[] {
                    new CharacterTriggerRelicEffectParams(new RelicEffectParams()) { characterState = input.characterState, trigger = CharacterTriggerData.Trigger.OnDeath, paramInt2 = 8 },
                    new CharacterTriggerRelicEffectParams(new RelicEffectParams()) { characterState = input.characterState, trigger = CharacterTriggerData.Trigger.OnHit, paramInt2 = 0 },
                    new CharacterTriggerRelicEffectParams(new RelicEffectParams()) { characterState = null!, trigger = CharacterTriggerData.Trigger.OnHit, paramInt2 = 8 } })
                    if (effect.TestCharacterTriggerEffect(control, AllGameManagers.Instance!.GetCoreManagers())) throw new InvalidOperationException("Decay-death negative control passed.");
            }
        }
        [HarmonyPatch(typeof(RelicManager), nameof(RelicManager.NotifyRelicTriggered))]
        private static class TriggerPatch
        { private static void Prefix(RelicState triggeredRelic) { if (active != null && triggeredRelic.GetRelicDataID() == RelicDecayDeathModel.DataId) active.Notifications.Add("Triggered"); } }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
