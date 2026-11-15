using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class HealthPercentCalibration
    {
        internal static readonly int[] Maximums = { 1, 3, 17, 99999 };
        internal static readonly int[] Percents = { -100, 0, 1, 33, 50, 100, 101, 200, int.MinValue, int.MaxValue };
        private static object Get(object owner, string field) => AccessTools.Field(owner.GetType(), field).GetValue(owner);
        private static void Set(object owner, string field, object value) => AccessTools.Field(owner.GetType(), field).SetValue(owner, value);
        internal static void Capture(AllGameManagers managers)
        {
            var trace = FullBattleTrace.Active!; var save = managers.GetSaveManager(); var combat = managers.GetCombatManager();
            var queue = (Queue<CombatManager.TriggerQueueData>)AccessTools.Property(typeof(CombatManager), "TriggerQueue").GetValue(combat);
            if (queue.Count != 0 || combat.IsRunningTriggerQueue || save.PreviewMode) throw new InvalidOperationException("Percentage calibration needs a settled primary state.");
            CharacterState host = Enumerable.Range(0, managers.GetRoomManager().GetNumRooms()).SelectMany(room =>
            {
                var actors = new List<CharacterState>(); managers.GetRoomManager().GetRoom(room).AddCharactersToList(actors, Team.Type.Heroes);
                return actors;
            }).First(actor => actor.IsAlive && !actor.IsPyreHeart() && !actor.IsMiniboss() && !actor.IsOuterTrainBoss());
            HealingScenario.RegisterHealingStatus("heal immunity", "StatusEffectHealImmunityState", 0);
            var original = trace.CaptureTrain(); object originalRng = HalfHealthLifecycleCalibration.Rng();
            object primary = Get(host, "_primaryStateInformation"), triggers = Get(host, "triggers");
            var identities = (Dictionary<CharacterState, Dictionary<CharacterTriggerState, int>>)Get(trace, "triggerIdentities");
            var originalIds = trace.TriggerStates(host).ToDictionary(pair => pair.Key, pair => pair.Value);
            Type stateType = primary.GetType(); var rows = new List<object>();
            try
            {
                foreach (int maximum in Maximums)
                foreach (int percent in Percents)
                foreach (int statusCase in Enumerable.Range(0, 4))
                foreach (bool wounded in new[] { false, true })
                {
                    object copy = AccessTools.Method(stateType, "DeepCopyForPreview").Invoke(primary, null);
                    Set(host, "_primaryStateInformation", copy); Set(host, "triggers", new List<CharacterTriggerState>());
                    identities[host] = new Dictionary<CharacterTriggerState, int>();
                    object hpSignal = Get(copy, "hpChangedSignal"); Set(copy, "hpChangedSignal", Activator.CreateInstance(hpSignal.GetType())!);
                    int notifications = 0; host.AddHpChangedListener((_, __) => notifications++);
                    foreach (string kind in new[] { "OnHit", "OnHeal", "OnDeathwish", "OnDeathwishLost", "OnDeath", "OnReanimated" })
                    {
                        var triggerData = HealingScenario.HealGold(11, false, false);
                        Set(triggerData, "trigger", Enum.Parse(typeof(CharacterTriggerData.Trigger), kind));
                        var trigger = new CharacterTriggerState(); trigger.Setup(triggerData, host.GetTeamType(), save); host.GetTriggers().Add(trigger);
                    }
                    var statuses = (Dictionary<string, CharacterState.StatusEffectStack>)Get(copy, "statusEffects");
                    foreach (var status in new[] { (Id: "horde", Count: (statusCase & 1) != 0 ? 2 : 0),
                        (Id: "undying", Count: (statusCase & 2) != 0 ? 1 : 0), (Id: "armor", Count: 7), (Id: "heal immunity", Count: 1) })
                    {
                        if (statuses.TryGetValue(status.Id, out var existing)) existing.Count = status.Count;
                        else statuses.Add(status.Id, new CharacterState.StatusEffectStack(managers.GetStatusEffectManager().Create(status.Id, host), status.Count));
                    }
                    host.SetHealth(wounded ? maximum / 2 : maximum, maximum);
                    AccessTools.Method(typeof(CharacterState), "TryUpdateDeathwish").Invoke(host, null); queue.Clear(); notifications = 0;
                    var data = new CardEffectData(UnitHealthPercentModel.NativeEffect, null!, host.GetTeamType());
                    data.Cheat_SetTargetMode(TargetMode.DropTargetCharacter); Set(data, "paramInt", percent);
                    // This effect reads ParamInt directly, even when a range is authored.
                    Set(data, "useIntRange", true); Set(data, "paramMinInt", 1); Set(data, "paramMaxInt", 91); Set(data, "paramMultiplier", .75f);
                    var effect = new CardEffectState(); effect.Setup(data, host.GetTeamType(), save);
                    CombatUnit before = trace.CaptureUnit(host); RoomCombatState roomBefore = trace.Capture(host.GetCurrentRoom());
                    object rngBefore = HalfHealthLifecycleCalibration.Rng();
                    var iterator = effect.GetCardEffect().ApplyEffect(effect, new CardEffectParams { targets = new List<CharacterState> { host } },
                        managers.GetCoreManagers(), managers.GetSystemManagers());
                    if (iterator.MoveNext()) throw new InvalidOperationException("Raw percentage effect unexpectedly yielded a frame.");
                    (iterator as IDisposable)?.Dispose();
                    CombatUnit actual = trace.CaptureUnit(host); RoomCombatState roomAfter = trace.Capture(host.GetCurrentRoom());
                    object rngAfter = HalfHealthLifecycleCalibration.Rng();
                    rows.Add(new { Maximum = maximum, Percent = percent, StatusCase = statusCase, Wounded = wounded,
                        Before = before, Actual = actual, RoomBefore = roomBefore, RoomAfter = roomAfter,
                        RngBefore = rngBefore, RngAfter = rngAfter, HpNotifications = notifications, QueueEmpty = queue.Count == 0,
                        RangeIgnored = true, NativeReturned = true });
                }
            }
            finally
            {
                Set(host, "_primaryStateInformation", primary); Set(host, "triggers", triggers); identities[host] = originalIds; queue.Clear();
            }
            bool restored = JToken.DeepEquals(JToken.FromObject(original), JToken.FromObject(trace.CaptureTrain()));
            bool rngRestored = JToken.DeepEquals(HalfHealthLifecycleCalibration.GameplayRng(originalRng),
                HalfHealthLifecycleCalibration.GameplayRng(HalfHealthLifecycleCalibration.Rng()));
            if (!restored || !rngRestored) throw new InvalidOperationException("Percentage component changed its primary state or RNG.");
            using (var document = NativeFixtureCapture.Capture(new { Schema = 1, GameVersion = UnityEngine.Application.version,
                GameModuleMvid = typeof(CharacterState).Assembly.ManifestModule.ModuleVersionId, Boundary = "NativeSetHealthPercent",
                NativeCompleted = true, PrimaryRestored = restored, GameplayRngRestored = rngRestored, Samples = rows }))
            using (var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "health-percent.mt2f"))) document.Write(output);
        }
    }
}
