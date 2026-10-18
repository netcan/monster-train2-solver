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
    internal static class OriginalSpawnSilenceScenario
    {
        internal const string Name = "original-chance-to-silence";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static readonly List<TestRecord> Tests = new List<TestRecord>();
        internal static readonly List<ApplicationRecord> Applications = new List<ApplicationRecord>();
        private static int step;
        private static bool actionMark;
        internal static object Evidence => new { OwnerId = RelicSpawnSilenceModel.DataId, AssetKey = RelicSpawnSilenceModel.AssetKey,
            Chance = 100, InnerChance = 100, Status = "muted", Amount = 1, CardIds };
        internal sealed class TestRecord
        {
            public CombatUnit Actor { get; set; } = null!;
            public string Trigger { get; set; } = "";
            public TrainCombatState Before { get; set; } = null!;
            public TrainCombatState? Actual { get; set; }
            public bool Result { get; set; }
            public bool[] RoomTriggers { get; set; } = Array.Empty<bool>();
            public string? Difference { get; set; }
            public string? Error { get; set; }
        }
        internal sealed class ApplicationRecord
        {
            public CombatUnit Actor { get; set; } = null!;
            public TrainCombatState Before { get; set; } = null!;
            public TrainCombatState? Predicted { get; set; }
            public TrainCombatState? Actual { get; set; }
            public bool Completed { get; set; }
            public string? Difference { get; set; }
            public string? UnsupportedReason { get; set; }
            public string? Error { get; set; }
        }
        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            if (Acquired || save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("Spawn silence requires one pre-battle acquisition.");
            AllGameData data = save.GetAllGameData();
            CollectableRelicData relic = data.GetAllCollectableRelicData().Single(item => item.GetID() == RelicSpawnSilenceModel.DataId);
            RelicEffectData effect = relic.GetEffects().Single();
            CardEffectData nested = effect.GetParamCardEffectData().Single();
            StatusEffectStackData status = nested.GetParamStatusEffects().Single();
            if (relic.name != RelicSpawnSilenceModel.AssetKey || relic.DisallowInDeploymentPhase ||
                effect.GetEffectClassName() != RelicSpawnSilenceModel.Effect || effect.GetParamInt() != 100 || effect.GetParamInt2() != 0 ||
                effect.GetParamSourceTeam() != Team.Type.Heroes || effect.GetParamCharacterSubtype()?.IsNone != true ||
                !effect.GetParamExcludeCharacterSubtypes().Select(item => item.Key).SequenceEqual(new[] { RelicSpawnSilenceModel.ExcludedSubtype }) ||
                effect.GetEffectConditions().Count != 0 ||
                nested.GetEffectStateName() != "CardEffectAddStatusEffect" || nested.GetTargetMode() != TargetMode.Self ||
                nested.GetTargetTeamType() != Team.Type.Heroes || nested.GetParamInt() != 100 || status.statusId != "muted" || status.count != 1)
                throw new InvalidOperationException("Original ChanceToSilence definition changed: " + JToken.FromObject(new {
                    relic.name, Id = relic.GetID(), relic.DisallowInDeploymentPhase, Effect = effect.GetEffectClassName(),
                    Chance = effect.GetParamInt(), Restriction = effect.GetParamInt2(), Team = effect.GetParamSourceTeam().ToString(),
                    Subtype = effect.GetParamCharacterSubtype()?.Key, IsNone = effect.GetParamCharacterSubtype()?.IsNone,
                    Exclusions = effect.GetParamExcludeCharacterSubtypes().Select(item => item.Key).ToArray(), Conditions = effect.GetEffectConditions().Count,
                    Nested = nested.GetEffectStateName(), Target = nested.GetTargetMode().ToString(),
                    NestedTeam = nested.GetTargetTeamType().ToString(), InnerChance = nested.GetParamInt(), status.statusId, status.count }));
            CardData steward = save.GetDeckState().Select(card => data.FindCardData(card.GetCardDataID()))
                .First(card => card?.GetSpawnCharacterData()?.name.StartsWith("TrainSteward", StringComparison.Ordinal) == true)!;
            CharacterData unit = steward.GetSpawnCharacterData()!;
            Set(unit, "health", 80); Set(unit, "attackDamage", 30); Set(unit, "size", 1);
            Set(unit, "triggers", new List<CharacterTriggerData>()); Set(unit, "startingStatusEffects", Array.Empty<StatusEffectStackData>());
            Set(steward, "cost", 0); save.GetDeckState().Clear();
            for (int i = 0; i < 2; i++) save.AddCardToDeck(steward, applyExistingRelicModifiers: false, showAnimation: false, notifyDeckCount: false);
            save.AddRelic(relic); Acquired = true;
            log.LogInfo("ORIGINAL-SPAWN-SILENCE-ACQUIRED unchanged 100% OnSpawn owner and inner 100% muted effect; natural enemy witnesses.");
        }
        internal static void PrepareDeployment(AllGameManagers managers)
        {
            if (Prepared || managers.GetCombatManager()!.GetTurnCount() != 0) throw new InvalidOperationException("Expected one deployment setup.");
            CardIds = managers.GetCardManager()!.GetHand().Where(card => card.GetCardType() == CardType.Monster)
                .Take(2).Select(FullBattleTrace.Active!.CardId).ToArray();
            if (CardIds.Length != 2) throw new InvalidOperationException("Missing natural-combat carriers.");
            Prepared = true;
        }
        internal static PlayCardAction? Choose(BattleTurnState source, PlayCardAction? fallback)
        {
            if (step >= CardIds.Length) return fallback;
            PlayCardAction? play = BattleActionModel.EnumerateSupportedPlays(source)
                .FirstOrDefault(action => action.CardInstanceId == CardIds[step] && action.RoomIndex == 0);
            if (play != null) { step++; actionMark = true; }
            return play ?? fallback;
        }
        internal static bool IsScenarioAction(PlayCardAction action)
        { if (!actionMark || !CardIds.Contains(action.CardInstanceId)) return false; actionMark = false; return true; }
        private static bool Active => Acquired && FullBattleTrace.Active != null && !AllGameManagers.Instance!.GetSaveManager().PreviewMode;
        [HarmonyPatch(typeof(RelicEffectChanceAddEffectOnSpawn), nameof(RelicEffectChanceAddEffectOnSpawn.TestCharacterTriggerEffect))]
        private static class TestPatch
        {
            private static void Prefix(CharacterTriggerRelicEffectParams relicEffectParams, out TestRecord? __state)
            {
                __state = null; if (!Active) return;
                var row = new TestRecord(); __state = row; Tests.Add(row);
                try { row.Actor = FullBattleTrace.Active!.CaptureUnit(relicEffectParams.characterState);
                    row.Trigger = relicEffectParams.trigger.ToString(); row.Before = FullBattleTrace.Active.CaptureTrain(); }
                catch (Exception error) { row.Error = error.ToString(); FullBattleTrace.Active!.CaptureFailure(error); }
            }
            private static void Postfix(RelicEffectChanceAddEffectOnSpawn __instance, bool __result, TestRecord? __state)
            {
                if (__state == null) return;
                try { __state.Result = __result; __state.Actual = FullBattleTrace.Active!.CaptureTrain();
                    __state.RoomTriggers = (bool[])((bool[])AccessTools.Field(__instance.GetType(), "_triggeredInRoom").GetValue(__instance)).Clone();
                    if (__result != RelicSpawnSilenceModel.Eligible(__state.Actor, __state.Trigger) || __state.RoomTriggers.Any(value => value) ||
                        !JToken.DeepEquals(JToken.FromObject(__state.Before), JToken.FromObject(__state.Actual)))
                        __state.Difference = "Native spawn-silence Test/state/RNG differs."; }
                catch (Exception error) { __state.Error = error.ToString(); FullBattleTrace.Active!.CaptureFailure(error); }
            }
        }
        [HarmonyPatch(typeof(RelicEffectChanceAddEffectOnSpawn), nameof(RelicEffectChanceAddEffectOnSpawn.ApplyCharacterTriggerEffect))]
        private static class ApplyPatch
        {
            private static void Postfix(CharacterTriggerRelicEffectParams relicEffectParams, ref IEnumerator __result)
            { if (Active) __result = Observe(__result, relicEffectParams.characterState); }
        }
        private static IEnumerator Observe(IEnumerator native, CharacterState actor)
        {
            var row = new ApplicationRecord(); Applications.Add(row);
            try { row.Actor = FullBattleTrace.Active!.CaptureUnit(actor); row.Before = FullBattleTrace.Active.CaptureTrain();
                TrainCombatResult predicted = RelicSpawnSilenceModel.ApplySelected(row.Before, row.Actor.Id);
                row.Predicted = predicted.State; row.UnsupportedReason = predicted.UnsupportedReason; }
            catch (Exception error) { row.Error = error.ToString(); FullBattleTrace.Active!.CaptureFailure(error); }
            try { while (native.MoveNext()) yield return native.Current; row.Completed = true; }
            finally {
                (native as IDisposable)?.Dispose();
                try { row.Actual = FullBattleTrace.Active!.CaptureTrain();
                    if (row.Predicted != null && !JToken.DeepEquals(JToken.FromObject(row.Predicted), JToken.FromObject(row.Actual)))
                        row.Difference = "Native spawn-silence status/callback/RNG differs."; }
                catch (Exception error) { row.Error = error.ToString(); FullBattleTrace.Active!.CaptureFailure(error); }
            }
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field)!.SetValue(target, value);
    }
}
