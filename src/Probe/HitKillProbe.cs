using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class HitKillProbe
    {
        internal static readonly List<Record> Records = new List<Record>();
        internal sealed class TriggerRecord
        {
            public bool Once { get; set; }
            public bool HasTriggered { get; set; }
            public bool IgnoreSilence { get; set; }
            public int Threshold { get; set; }
            public int FireCount { get; set; }
            public int Gold { get; set; }
        }
        internal sealed class Record
        {
            public int Sequence { get; set; }
            public string Stage { get; set; } = "";
            public string Kind { get; set; } = "";
            public int ActorId { get; set; }
            public int DyingId { get; set; }
            public int Health { get; set; }
            public bool FinishedDying { get; set; }
            public bool DeadBoss { get; set; }
            public bool Silenced { get; set; }
            public bool CanFire { get; set; }
            public bool QueueRunning { get; set; }
            public int ParamInt { get; set; }
            public int GoldBefore { get; set; }
            public int? GoldAfter { get; set; }
            public bool[]? AfterTriggered { get; set; }
            public TriggerRecord[] Triggers { get; set; } = Array.Empty<TriggerRecord>();
            public bool Completed { get; set; }
        }
        private static bool Enabled(CharacterTriggerData.Trigger trigger) =>
            Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "hit-kill" && HitKillScenario.Prepared &&
            FullBattleTrace.Active != null && !AllGameManagers.Instance!.GetSaveManager().PreviewMode &&
            (trigger == CharacterTriggerData.Trigger.OnHit || trigger == CharacterTriggerData.Trigger.OnKill);
        private static IEnumerator Wrap(IEnumerator native, CharacterState actor, CharacterTriggerData.Trigger kind,
            CharacterState dying, CharacterState.FireTriggersData data, bool canFire, string stage)
        {
            FullBattleTrace trace = FullBattleTrace.Active!;
            SaveManager save = AllGameManagers.Instance!.GetSaveManager();
            CharacterTriggerState[] triggers = actor.GetTriggers().Where(trigger => trigger.GetTrigger() == kind).ToArray();
            var record = new Record { Sequence = trace.NextPhaseSequence(), Stage = stage, Kind = kind.ToString(), ActorId = trace.UnitId(actor),
                DyingId = dying == null ? 0 : trace.UnitId(dying), Health = actor.GetHP(), FinishedDying = actor.HasFinishedDying,
                DeadBoss = actor.IsDead && (actor.IsMiniboss() || actor.IsOuterTrainBoss()), Silenced = actor.HasStatusEffect("silenced"),
                CanFire = canFire, QueueRunning = AllGameManagers.Instance!.GetCombatManager()!.IsRunningTriggerQueue,
                ParamInt = data?.paramInt ?? 0, GoldBefore = save.GetGold(),
                Triggers = triggers.Select(trigger => new TriggerRecord { Once = trigger.GetTriggerData().GetTriggerOnce(),
                    HasTriggered = trigger.GetHasTriggeredOnce(false), IgnoreSilence = trigger.GetHideVisualAndIgnoreSilence(),
                    Threshold = trigger.GetTriggerData().GetTriggerAtThreshold(), FireCount = actor.GetTriggerFireCount(kind, trigger),
                    Gold = trigger.GetEffectStates().Where(effect => effect.GetCardEffect() is CardEffectRewardGold).Sum(effect => effect.GetParamInt()) }).ToArray() };
            Records.Add(record);
            try { while (native.MoveNext()) yield return native.Current; record.Completed = true; }
            finally
            {
                (native as IDisposable)?.Dispose(); record.GoldAfter = save.GetGold();
                record.AfterTriggered = triggers.Select(trigger => trigger.GetHasTriggeredOnce(false)).ToArray();
            }
        }
        [HarmonyPatch(typeof(CombatManager), nameof(CombatManager.QueueAndRunTrigger))]
        private static class QueuePatch
        {
            private static void Postfix(CharacterState character, CharacterTriggerData.Trigger trigger, CharacterState dyingCharacter,
                CharacterState.FireTriggersData fireTriggersData, bool canFireTriggers, ref IEnumerator __result)
            { if (Enabled(trigger)) __result = Wrap(__result, character, trigger, dyingCharacter, fireTriggersData, canFireTriggers, "Queue"); }
        }
        [HarmonyPatch(typeof(CharacterState), nameof(CharacterState.FireTriggers))]
        private static class FirePatch
        {
            private static void Postfix(CharacterState __instance, CharacterTriggerData.Trigger trigger, CharacterState dyingCharacter,
                CharacterState.FireTriggersData fireTriggersData, bool canFireTriggers, bool fromRunningTriggerQueue, ref IEnumerator __result)
            { if (fromRunningTriggerQueue && Enabled(trigger)) __result = Wrap(__result, __instance, trigger, dyingCharacter, fireTriggersData, canFireTriggers, "Fire"); }
        }
    }
}
