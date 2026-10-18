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
    internal static class OriginalKillAttackScenario
    {
        internal const string Name = "original-kill-attack";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static readonly List<TestRecord> Tests = new List<TestRecord>();
        internal static readonly List<ApplicationRecord> Applications = new List<ApplicationRecord>();
        private static int step;
        private static bool actionMark;
        internal static object Evidence => new { OwnerId = RelicKillAttackModel.DataId, AssetKey = RelicKillAttackModel.AssetKey,
            UpgradeId = RelicKillAttackModel.UpgradeId, Amount = 3, CardIds };

        internal sealed class TestRecord
        {
            public CombatUnit Actor { get; set; } = null!;
            public string Trigger { get; set; } = "";
            public TrainCombatState Before { get; set; } = null!;
            public TrainCombatState? Actual { get; set; }
            public bool Result { get; set; }
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
                throw new InvalidOperationException("Kill attack requires one pre-battle acquisition.");
            AllGameData data = save.GetAllGameData();
            CollectableRelicData relic = data.GetAllCollectableRelicData().Single(owner => owner.GetID() == RelicKillAttackModel.DataId);
            RelicEffectData effect = relic.GetEffects().Single();
            if (relic.name != RelicKillAttackModel.AssetKey || relic.DisallowInDeploymentPhase ||
                effect.GetEffectClassName() != RelicModel.ModifyCharacterAttackDamage ||
                effect.GetParamTrigger() != CharacterTriggerData.Trigger.OnKill || effect.GetParamSourceTeam() != Team.Type.Monsters ||
                effect.GetParamInt() != 3 || !effect.GetParamBool() || effect.GetParamCardType() != CardType.Invalid ||
                effect.GetParamCardUpgradeData()?.GetID() != RelicKillAttackModel.UpgradeId || effect.GetEffectConditions().Count != 0)
                throw new InvalidOperationException("Original AddAttackOnKill definition changed.");
            CardData steward = save.GetDeckState().Select(card => data.FindCardData(card.GetCardDataID()))
                .First(card => card?.GetSpawnCharacterData()?.name.StartsWith("TrainSteward", StringComparison.Ordinal) == true)!;
            CharacterData unit = steward.GetSpawnCharacterData()!;
            Set(unit, "health", 80); Set(unit, "attackDamage", 30); Set(unit, "size", 1);
            Set(unit, "triggers", new List<CharacterTriggerData>()); Set(unit, "startingStatusEffects", Array.Empty<StatusEffectStackData>());
            Set(steward, "cost", 0); save.GetDeckState().Clear();
            for (int i = 0; i < 2; i++) save.AddCardToDeck(steward, applyExistingRelicModifiers: false, showAnimation: false, notifyDeckCount: false);
            save.AddRelic(relic); Acquired = true;
            log.LogInfo("ORIGINAL-KILL-ATTACK-ACQUIRED unchanged OnKill +3 unit/source-card attack upgrade; natural combat witnesses.");
        }
        internal static void PrepareDeployment(AllGameManagers managers)
        {
            if (Prepared || managers.GetCombatManager()!.GetTurnCount() != 0) throw new InvalidOperationException("Expected one deployment setup.");
            CardIds = managers.GetCardManager()!.GetHand().Where(card => card.GetCardType() == CardType.Monster)
                .Take(2).Select(FullBattleTrace.Active!.CardId).ToArray();
            if (CardIds.Length != 2) throw new InvalidOperationException("Missing two natural-combat carriers.");
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
        private static bool Active(RelicEffectModifyCharacterAttackDamage effect) => Acquired && FullBattleTrace.Active != null &&
            !AllGameManagers.Instance!.GetSaveManager().PreviewMode &&
            ((RelicData)AccessTools.Field(typeof(RelicEffectBase), "_srcRelicData").GetValue(effect)).GetID() == RelicKillAttackModel.DataId;

        [HarmonyPatch(typeof(RelicEffectModifyCharacterAttackDamage), nameof(RelicEffectModifyCharacterAttackDamage.TestCharacterTriggerEffect))]
        private static class TestPatch
        {
            private static void Prefix(RelicEffectModifyCharacterAttackDamage __instance, CharacterTriggerRelicEffectParams relicEffectParams, out TestRecord? __state)
            {
                __state = null; if (!Active(__instance)) return;
                var row = new TestRecord(); __state = row; Tests.Add(row);
                try { row.Actor = FullBattleTrace.Active!.CaptureUnit(relicEffectParams.characterState);
                    row.Trigger = relicEffectParams.trigger.ToString(); row.Before = FullBattleTrace.Active.CaptureTrain(); }
                catch (Exception error) { row.Error = error.ToString(); FullBattleTrace.Active!.CaptureFailure(error); }
            }
            private static void Postfix(bool __result, TestRecord? __state)
            {
                if (__state == null) return;
                try { __state.Result = __result; __state.Actual = FullBattleTrace.Active!.CaptureTrain();
                    if (__result != RelicKillAttackModel.Eligible(__state.Actor, __state.Trigger) ||
                        !JToken.DeepEquals(JToken.FromObject(__state.Before), JToken.FromObject(__state.Actual)))
                        __state.Difference = "Native kill-attack eligibility/state differs."; }
                catch (Exception error) { __state.Error = error.ToString(); FullBattleTrace.Active!.CaptureFailure(error); }
            }
        }
        [HarmonyPatch(typeof(RelicEffectModifyCharacterAttackDamage), nameof(RelicEffectModifyCharacterAttackDamage.ApplyCharacterTriggerEffect))]
        private static class ApplyPatch
        {
            private static void Postfix(RelicEffectModifyCharacterAttackDamage __instance, CharacterTriggerRelicEffectParams relicEffectParams, ref IEnumerator __result)
            { if (Active(__instance)) __result = Observe(__result, relicEffectParams.characterState); }
        }
        private static IEnumerator Observe(IEnumerator native, CharacterState actor)
        {
            var row = new ApplicationRecord(); Applications.Add(row);
            try {
                row.Actor = FullBattleTrace.Active!.CaptureUnit(actor); row.Before = FullBattleTrace.Active.CaptureTrain();
                RoomCombatState room = row.Before.Rooms.Single(item => item.Units.Any(unit => unit.Id == row.Actor.Id));
                var children = new List<RoomCombatModel.QueuedCharacterTrigger>();
                RoomCombatResult predicted = RoomCombatModel.ApplyQueuedCharacterTrigger(room,
                    new RoomCombatModel.QueuedCharacterTrigger(room.RoomIndex, row.Actor, "OnKill", admission: RoomCombatModel.CharacterTriggerAdmission.Accepted), children.Add);
                if (children.Count != 0) throw new InvalidOperationException("Isolated attack upgrade unexpectedly queued child triggers.");
                row.UnsupportedReason = predicted.UnsupportedReason;
                if (predicted.Supported) row.Predicted = CardSpellModel.WithContext(new TrainCombatState(row.Before.Rooms
                    .Select(item => item.RoomIndex == room.RoomIndex ? predicted.State! : item).ToArray(), row.Before.Movement,
                    row.Before.EnemySlotsPerRoom, predicted.State!.Context), predicted.State.Context!);
            } catch (Exception error) { row.Error = error.ToString(); FullBattleTrace.Active!.CaptureFailure(error); }
            try { while (native.MoveNext()) yield return native.Current; row.Completed = true; }
            finally {
                (native as IDisposable)?.Dispose();
                try { row.Actual = FullBattleTrace.Active!.CaptureTrain();
                    if (row.Predicted != null && !JToken.DeepEquals(JToken.FromObject(row.Predicted), JToken.FromObject(row.Actual)))
                        row.Difference = "Native kill-attack unit/source-card upgrade differs."; }
                catch (Exception error) { row.Error = error.ToString(); FullBattleTrace.Active!.CaptureFailure(error); }
            }
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field)!.SetValue(target, value);
    }
}
