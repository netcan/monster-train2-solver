using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class AttackTriggerProbe
    {
        internal static readonly List<Record> Records = new List<Record>();
        internal sealed class Record
        {
            public int Sequence { get; set; }
            public string Stage { get; set; } = "";
            public string Kind { get; set; } = "";
            public int ActorId { get; set; }
            public int OverrideTargetId { get; set; }
            public int TargetHealth { get; set; }
            public int Health { get; set; }
            public bool DeadBoss { get; set; }
            public bool Silenced { get; set; }
            public bool CanFire { get; set; }
            public bool QueueRunning { get; set; }
            public int ParamInt { get; set; }
            public int GoldBefore { get; set; }
            public int? GoldAfter { get; set; }
            public bool[]? AfterTriggered { get; set; }
            public HitKillProbe.TriggerRecord[] Triggers { get; set; } = Array.Empty<HitKillProbe.TriggerRecord>();
            public bool Completed { get; set; }
        }
        private static bool Enabled(CharacterTriggerData.Trigger kind) =>
            Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "attack-triggers" && AttackTriggerScenario.Prepared &&
            FullBattleTrace.Active != null && !AllGameManagers.Instance!.GetSaveManager().PreviewMode &&
            (kind == CharacterTriggerData.Trigger.OnAttackingBeforeDamage || kind == CharacterTriggerData.Trigger.OnAttacking);
        private static IEnumerator Wrap(IEnumerator native, CharacterState actor, CharacterTriggerData.Trigger kind,
            CharacterState.FireTriggersData data, bool canFire, string stage)
        {
            FullBattleTrace trace = FullBattleTrace.Active!; SaveManager save = AllGameManagers.Instance!.GetSaveManager();
            CharacterTriggerState[] triggers = actor.GetTriggers().Where(trigger => trigger.GetTrigger() == kind).ToArray();
            CharacterState? target = data?.overrideTargetCharacter;
            int targetHealth = -1;
            if (target != null) using (new CharacterState.SetAllowDestroyedAccessHelper(target, onlyIfDestroyed: true)) targetHealth = target.GetHP();
            var record = new Record { Sequence = trace.NextPhaseSequence(), Stage = stage, Kind = kind.ToString(), ActorId = trace.UnitId(actor),
                OverrideTargetId = target == null ? 0 : trace.UnitId(target), TargetHealth = targetHealth, Health = actor.GetHP(),
                DeadBoss = actor.IsDead && (actor.IsMiniboss() || actor.IsOuterTrainBoss()), Silenced = actor.HasStatusEffect("silenced"),
                CanFire = canFire, QueueRunning = AllGameManagers.Instance!.GetCombatManager()!.IsRunningTriggerQueue,
                ParamInt = data?.paramInt ?? 0, GoldBefore = save.GetGold(),
                Triggers = triggers.Select(trigger => new HitKillProbe.TriggerRecord { Once = trigger.GetTriggerData().GetTriggerOnce(),
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
            private static void Postfix(CharacterState character, CharacterTriggerData.Trigger trigger,
                CharacterState.FireTriggersData fireTriggersData, bool canFireTriggers, ref IEnumerator __result)
            { if (Enabled(trigger)) __result = Wrap(__result, character, trigger, fireTriggersData, canFireTriggers, "Queue"); }
        }
        [HarmonyPatch(typeof(CharacterState), nameof(CharacterState.FireTriggers))]
        private static class FirePatch
        {
            private static void Postfix(CharacterState __instance, CharacterTriggerData.Trigger trigger,
                CharacterState.FireTriggersData fireTriggersData, bool canFireTriggers, bool fromRunningTriggerQueue, ref IEnumerator __result)
            { if (fromRunningTriggerQueue && Enabled(trigger)) __result = Wrap(__result, __instance, trigger, fireTriggersData, canFireTriggers, "Fire"); }
        }
    }
}
