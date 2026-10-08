using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    // Uses real paid units and real Add/RemoveStatusEffect calls. No gameplay API is suppressed.
    internal static class EnchantmentCombatCalibration
    {
        internal static bool Started { get; private set; }
        internal static bool Completed { get; private set; }
        internal static string? Error { get; private set; }
        private static bool recording;
        private static CardEffectEnchant? currentEffect;
        private static readonly List<EnchantmentRequest> requests = new List<EnchantmentRequest>();
        private static bool recordPreviewPreparation;
        private static readonly List<int> previewPreparedUnits = new List<int>();
        private static AllGameManagers Managers => AllGameManagers.Instance!;
        private static SaveManager Save => Managers.GetSaveManager();
        private static FullBattleTrace Trace => FullBattleTrace.Active!;
        private static readonly Type StateType = AccessTools.Inner(typeof(CharacterState), "StateInformation");
        private static void Set(object target, string field, object? value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
        private static object Get(object target, string field) => AccessTools.Field(target.GetType(), field).GetValue(target);
        private static HadesRNG Rng(RngId id) => (HadesRNG)AccessTools.Method(typeof(RandomManager), "GetRng").Invoke(null, new object[] { id });
        private static UnityRng SnapshotRng(RngId id)
        { uint[] w = RngCalibration.Words(Rng(id).GetState()); return new UnityRng(w[0], w[1], w[2], w[3]); }
        private static CharacterState[] LiveActors()
        {
            var actors = new List<CharacterState>();
            for (int floor = 0; floor < Managers.GetRoomManager()!.GetNumRooms(); floor++)
                Managers.GetRoomManager()!.GetRoom(floor).AddCharactersToList(actors, Team.Type.Monsters | Team.Type.Heroes);
            return actors.Where(unit => unit.IsAlive && !unit.IsDestroyed).ToArray();
        }
        internal static bool Eligible => LiveActors().Where(unit => !unit.IsPyreHeart() && unit.GetTeamType() == Team.Type.Monsters)
            .GroupBy(unit => unit.GetCurrentRoomIndex()).Any(group => group.Count() >= 2);
        internal static void Start(ManualLogSource log)
        { Started = true; Save.StartCoroutine(Protect(Run(), log)); }
        private static IEnumerator Protect(IEnumerator root, ManualLogSource log)
        {
            var stack = new Stack<IEnumerator>(); stack.Push(root);
            try
            {
                while (stack.Count > 0)
                {
                    IEnumerator step = stack.Peek(); bool next;
                    try { next = step.MoveNext(); }
                    catch (Exception error) { Error = error.ToString(); log.LogError(Error); break; }
                    if (!next) { (step as IDisposable)?.Dispose(); stack.Pop(); continue; }
                    if (step.Current is IEnumerator child) stack.Push(child); else yield return step.Current;
                }
            }
            finally { while (stack.Count > 0) (stack.Pop() as IDisposable)?.Dispose(); recording = false; recordPreviewPreparation = false; currentEffect = null; }
            Completed = Error == null;
        }
        private static EnchantmentCombatState Snapshot()
        {
            CharacterState[] actors = LiveActors();
            return new EnchantmentCombatState(Trace.CaptureTrain(), Array.Empty<EnchantmentRetainedUnit>(),
                actors.Where(unit => unit.IsEnchanter).Select(Trace.UnitId).ToArray(), Managers.GetRoomManager()!.AllowEnchantmentUpdates,
                (bool)Get(Managers.GetRoomManager()!, "_updatingEnchantments"), Save.PreviewMode, SnapshotRng(RngId.BattleTest));
        }
        private static EnchantmentCallback[] Queue()
        {
            var queue = (Queue<CombatManager.TriggerQueueData>)AccessTools.Property(typeof(CombatManager), "TriggerQueue").GetValue(Managers.GetCombatManager());
            return queue.Select(item => new EnchantmentCallback(item.character.GetCurrentRoomIndex(), Trace.UnitId(item.character), item.trigger.ToString(),
                item.fireTriggersData?.paramInt ?? 0, item.fireTriggersData?.paramInt2 ?? 0, item.fireTriggersData?.paramString,
                item.fireTriggersData?.overrideTargetCharacter == null ? (int?)null : Trace.UnitId(item.fireTriggersData.overrideTargetCharacter),
                item.dyingCharacter == null ? (int?)null : Trace.UnitId(item.dyingCharacter), item.canFireTriggers)).ToArray();
        }
        private static IEnumerator Run()
        {
            RoomManager rooms = Managers.GetRoomManager()!; CombatManager combat = Managers.GetCombatManager()!;
            CharacterState[] hosts = LiveActors().Where(unit => !unit.IsPyreHeart() && unit.GetTeamType() == Team.Type.Monsters)
                .GroupBy(unit => unit.GetCurrentRoomIndex()).First(group => group.Count() >= 2).Take(2).ToArray();
            TrainCombatState originalTrain = Trace.CaptureTrain(); CombatContext originalContext = Trace.CaptureContext();
            var originalStates = hosts.Select(unit => Get(unit, "_primaryStateInformation")).ToArray();
            var originalTriggers = hosts.Select(unit => Get(unit, "triggers")).ToArray();
            bool[] originalEnchanter = hosts.Select(unit => unit.IsEnchanter).ToArray();
            var identityStore = (Dictionary<CharacterState, Dictionary<CharacterTriggerState, int>>)Get(Trace, "triggerIdentities");
            var originalIds = hosts.Select(unit => Trace.TriggerStates(unit).ToDictionary(item => item.Key, item => item.Value)).ToArray();
            HadesRNG battle = Rng(RngId.Battle), test = Rng(RngId.BattleTest);
            var originalBattle = new HadesRNG(battle); var originalTest = new HadesRNG(test);
            var originalGlobal = UnityEngine.Random.state; int originalGold = Save.GetGold();
            bool originalAllow = rooms.AllowEnchantmentUpdates;
            var samples = new List<object>(); int mismatches = 0;
            var worldSamples = new List<object>(); int worldMismatches = 0;
            try
            {
                Set(rooms, "_allowEnchantmentUpdates", true);
                for (int i = 0; i < hosts.Length; i++)
                {
                    object cloned = AccessTools.Method(StateType, "DeepCopyForPreview").Invoke(originalStates[i], null);
                    Set(hosts[i], "_primaryStateInformation", cloned);
                    Set(hosts[i], "triggers", new List<CharacterTriggerState>());
                    AccessTools.Property(typeof(CharacterState), "IsEnchanter").SetValue(hosts[i], true);
                    foreach (var callback in new[] { (CharacterTriggerData.Trigger.OnStatusEffectChanged, 3),
                        (CharacterTriggerData.Trigger.OnArmorAdded, 5), (CharacterTriggerData.Trigger.OnNewStatusEffectAdded, 7) })
                    {
                        CharacterTriggerData data = HealingScenario.HealGold(callback.Item2, false, true); Set(data, "trigger", callback.Item1);
                        var trigger = new CharacterTriggerState(); trigger.Setup(data, hosts[i].GetTeamType(), Save); hosts[i].GetTriggers().Add(trigger);
                    }
                }
                Configure("armor", 2, "armor", 3);
                yield return Sample("initial-all"); yield return Sample("duplicate-all");
                Raw(hosts[0], "muted", 1); yield return Sample("muted-source");
                Raw(hosts[0], "muted", 0); yield return Sample("unmuted-source");
                Raw(hosts[1], "silenced", 1); yield return Sample("silenced-second-source");
                Raw(hosts[1], "silenced", 0); yield return Sample("unsilenced-second-source");
                Set(rooms, "_allowEnchantmentUpdates", false); Raw(hosts[0], "muted", 1); yield return Sample("disabled-all");
                Set(rooms, "_allowEnchantmentUpdates", true); yield return Sample("reenabled-all");
                Raw(hosts[0], "muted", 0); yield return Sample("readd-after-enable");
                // Muting a second enchanter requests a nested global pass. The room guard stops
                // reentry under UpdateAll, while a direct effect call permits that global pass.
                Configure("silenced", 1, "armor", 4); yield return Sample("silencing-all");
                Raw(hosts[1], "silenced", 0); Configure("silenced", 1, "armor", 4); yield return Sample("silencing-direct", true);
                Raw(hosts[1], "silenced", 0); Configure("poison", 3, "armor", 0); yield return Sample("poison-and-zero");
                Raw(hosts[0], "muted", 1); yield return Sample("poison-remove"); Raw(hosts[0], "muted", 0);
                Raw(hosts[0], "duality", 1); Configure("armor", 3, "armor", -2); yield return Sample("duality-and-negative");
                Raw(hosts[0], "muted", 1); yield return Sample("duality-remove"); Raw(hosts[0], "muted", 0);
                Configure("armor", int.MaxValue, "armor", 0); yield return Sample("duality-overflow");
                Raw(hosts[0], "duality", 0); Configure("armor", 2, "poison", 1, random: true);
                for (int i = 0; i < 16; i++)
                { Raw(hosts[0], "muted", i % 2); yield return Sample("random-carried-" + i); }
                if (Environment.GetEnvironmentVariable("MT2_PROBE_ENCHANTMENT_WORLD") == "1")
                {
                    Raw(hosts[0], "muted", 0); Raw(hosts[1], "muted", 0);
                    Raw(hosts[0], "silenced", 0); Raw(hosts[1], "silenced", 0);
                    Configure("armor", 2, "armor", 3);
                    yield return StatusSample("mute-add", "muted", 1);
                    yield return StatusSample("mute-zero-add", "muted", 0);
                    yield return StatusSample("mute-repeat", "muted", 1);
                    yield return StatusSample("mute-zero-remove", "muted", 0, true);
                    yield return StatusSample("mute-remove-all", "muted", -1, true);
                    yield return StatusSample("silence-add", "silenced", 1);
                    yield return StatusSample("silence-remove", "silenced", 1, true);
                    yield return StatusSample("armor-zero-no-global", "armor", 0);
                    yield return StatusSample("dormant-no-global", "dormant", 1);
                    yield return StatusSample("spark-zero-dormant", "spark", 0);
                    yield return StatusSample("spark-enable-dormant", "spark", 1);
                    yield return StatusSample("spark-zero-remove", "spark", 0, true);
                    yield return StatusSample("spark-remove-dormant", "spark", 1, true);
                    yield return StatusSample("dormant-remove-no-global", "dormant", -1, true);
                    Set(rooms, "_allowEnchantmentUpdates", false);
                    yield return StatusSample("disabled-mute", "muted", 1);
                    Set(rooms, "_allowEnchantmentUpdates", true);
                    yield return StatusSample("enabled-unmute", "muted", -1, true);
                    Configure("armor", 2, "poison", 1, random: true);
                    for (int i = 0; i < 16; i++)
                        yield return StatusSample("control-random-carried-" + i, "muted", i % 2 == 0 ? 1 : -1, i % 2 != 0);
                }
            }
            finally
            {
                recording = false; currentEffect = null;
                Set(rooms, "_allowEnchantmentUpdates", false);
                for (int i = 0; i < hosts.Length; i++)
                {
                    Set(hosts[i], "_primaryStateInformation", originalStates[i]); Set(hosts[i], "triggers", originalTriggers[i]);
                    AccessTools.Property(typeof(CharacterState), "IsEnchanter").SetValue(hosts[i], originalEnchanter[i]);
                    identityStore[hosts[i]] = originalIds[i];
                }
                Save.SetGold(originalGold); battle.Init(originalBattle); test.Init(originalTest); UnityEngine.Random.state = originalGlobal;
                Set(rooms, "_allowEnchantmentUpdates", originalAllow);
            }
            bool restored = Equal(originalTrain, Trace.CaptureTrain()) && Equal(originalContext, Trace.CaptureContext()) &&
                Equals(UnityEngine.Random.state, originalGlobal) && Queue().Length == 0 && rooms.AllowEnchantmentUpdates == originalAllow;
            string path = Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "enchantment-combat-calibration.mt2f");
            using (var archive = NativeFixtureCapture.Capture(new { Schema = 1, GameVersion = UnityEngine.Application.version,
                GameModuleMvid = typeof(CharacterState).Assembly.ManifestModule.ModuleVersionId, Boundary = "RealStatusAndDrainedQueue",
                StatusMutationsSuppressed = false, ExternalPreviewPreparationsRecorded = true, LiveContextUnchanged = restored, Mismatches = mismatches, Samples = samples }))
            using (var stream = File.Create(path)) archive.Write(stream);
            if (!restored || mismatches != 0) throw new InvalidOperationException("Real enchantment combat calibration differs/restoration failed: " + mismatches + "/" + restored);
            if (worldSamples.Count > 0)
            {
                string worldPath = Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "enchantment-world-calibration.mt2f");
                using (var archive = NativeFixtureCapture.Capture(new { Schema = 1, GameVersion = UnityEngine.Application.version,
                    GameModuleMvid = typeof(CharacterState).Assembly.ManifestModule.ModuleVersionId, Boundary = "AutomaticControlStatusAndQueue",
                    StatusMutationsSuppressed = false, ExternalPreviewPreparationsRecorded = true, LiveContextUnchanged = restored,
                    Mismatches = worldMismatches, Samples = worldSamples }))
                using (var stream = File.Create(worldPath)) archive.Write(stream);
                if (worldMismatches != 0) throw new InvalidOperationException("Automatic world status calibration differs: " + worldMismatches);
            }

            void Configure(string first, int firstCount, string second, int secondCount, bool random = false)
            {
                for (int i = 0; i < hosts.Length; i++)
                {
                    hosts[i].GetTriggers().RemoveAll(trigger => trigger.GetTrigger() == CharacterTriggerData.Trigger.PostCombat);
                    var data = new CardEffectData("CardEffectEnchant", null!, Team.Type.Monsters | Team.Type.Heroes); data.Cheat_SetTargetMode(TargetMode.Room);
                    Set(data, "paramStatusEffects", (random && i == 0 ? new[] { (first, firstCount), ("poison", 3), ("cardless", 0) } :
                        new[] { i == 0 ? (first, firstCount) : (second, secondCount) }).Select(pair => new StatusEffectStackData { statusId = pair.Item1, count = pair.Item2 }).ToArray());
                    CharacterTriggerData triggerData = HealingScenario.HealGold(1, false, true); Set(triggerData, "trigger", CharacterTriggerData.Trigger.PostCombat);
                    Set(triggerData, "effects", new List<CardEffectData> { data });
                    var trigger = new CharacterTriggerState(); trigger.Setup(triggerData, hosts[i].GetTeamType(), Save); hosts[i].GetTriggers().Add(trigger);
                    ((CardEffectEnchant)trigger.GetEffectStates()[0].GetCardEffect()).SetEnchanterCharacter(hosts[i], Managers.GetCoreManagers());
                }
            }
            IEnumerator Sample(string label, bool direct = false)
            {
                if (Queue().Length != 0) throw new InvalidOperationException("Calibration requires an empty starting trigger queue.");
                EnchantmentCombatState before = Snapshot(); int id = Trace.UnitId(hosts[0]); int triggerIndex = hosts[0].GetTriggers().Count - 1;
                EnchantmentCombatResult predicted = direct ? EnchantmentCombatModel.UpdateEffect(before, id, triggerIndex, 0) : EnchantmentCombatModel.UpdateAll(before);
                requests.Clear(); previewPreparedUnits.Clear(); recordPreviewPreparation = true; recording = true;
                try
                {
                    if (direct) ((CardEffectEnchant)hosts[0].GetTriggers().Last().GetEffectStates()[0].GetCardEffect()).OnUpdateEnchantments();
                    else rooms.UpdateEnchantments();
                }
                finally { recording = false; }
                EnchantmentCombatState actual = Snapshot(); EnchantmentCallback[] queued = Queue();
                EnchantmentRequest[] nativeRequests = requests.ToArray();
                string? difference = !predicted.Supported ? predicted.UnsupportedReason : !Equal(predicted.State!, actual) ? "Status/train/effect state differs" :
                    !Equal(predicted.Requests, nativeRequests) ? "Status API requests differ" : !Equal(predicted.Callbacks, queued) ? "Queued callback payloads differ" : null;
                EnchantmentCombatResult drained = EnchantmentCombatModel.Drain(predicted);
                yield return combat.RunTriggerQueue();
                recordPreviewPreparation = false;
                EnchantmentCombatState afterQueue = Snapshot();
                EnchantmentCombatState? modeledAfterQueue = drained.State;
                if (modeledAfterQueue != null)
                    foreach (int preparedId in previewPreparedUnits) modeledAfterQueue = EnchantmentCombatModel.PrepareForPreview(modeledAfterQueue, preparedId);
                if (difference == null && (!drained.Supported || !Equal(modeledAfterQueue!, afterQueue))) difference = drained.UnsupportedReason ?? "Drained child effects/preview preparations differ";
                if (difference != null) mismatches++;
                samples.Add(new { Label = label, Direct = direct, SourceId = id, TriggerIndex = triggerIndex, EffectIndex = 0,
                    Before = before, Actual = actual, Requests = nativeRequests, Callbacks = queued, AfterQueue = afterQueue,
                    PreviewPreparedUnitIds = previewPreparedUnits.ToArray(), Difference = difference, Completed = true });
            }
            IEnumerator StatusSample(string label, string statusId, int count, bool remove = false)
            {
                if (Queue().Length != 0) throw new InvalidOperationException("Automatic calibration requires an empty trigger queue.");
                EnchantmentCombatState before = Snapshot(); int id = Trace.UnitId(hosts[0]);
                CombatStatus definition = BattleActionProbe.Status(statusId, count);
                EnchantmentCombatResult predicted = EnchantmentWorldModel.ChangeStatus(before, id, definition, remove);
                requests.Clear(); previewPreparedUnits.Clear(); recordPreviewPreparation = true; recording = true;
                try
                {
                    if (remove) hosts[0].RemoveStatusEffect(statusId, count, new CharacterState.RemoveStatusEffectParams());
                    else hosts[0].AddStatusEffect(statusId, count, new CharacterState.AddStatusEffectParams());
                }
                finally { recording = false; }
                EnchantmentCombatState actual = Snapshot(); EnchantmentCallback[] queued = Queue();
                EnchantmentRequest[] nativeRequests = requests.ToArray();
                string? difference = !predicted.Supported ? predicted.UnsupportedReason : !Equal(predicted.State!, actual) ? "Automatic status/train/effect state differs" :
                    !Equal(predicted.Callbacks, queued) ? "Automatic callback payloads/order differ" : null;
                EnchantmentCombatResult drained = EnchantmentWorldModel.Drain(predicted);
                yield return combat.RunTriggerQueue();
                recordPreviewPreparation = false;
                EnchantmentCombatState afterQueue = Snapshot(); EnchantmentCombatState? modeled = drained.State;
                if (modeled != null) foreach (int preparedId in previewPreparedUnits) modeled = EnchantmentCombatModel.PrepareForPreview(modeled, preparedId);
                if (difference == null && (!drained.Supported || !Equal(modeled!, afterQueue))) difference = drained.UnsupportedReason ?? "Automatic queue/context/preparation differs";
                if (difference != null) worldMismatches++;
                worldSamples.Add(new { Label = label, UnitId = id, Status = definition, Remove = remove, Before = before, Actual = actual,
                    ObservedAuraRequests = nativeRequests, Callbacks = queued, AfterQueue = afterQueue,
                    PreviewPreparedUnitIds = previewPreparedUnits.ToArray(), Difference = difference, Completed = true });
            }
        }
        private static void Raw(CharacterState unit, string status, int count)
        {
            var map = (Dictionary<string, CharacterState.StatusEffectStack>)AccessTools.Field(StateType, "statusEffects").GetValue(Get(unit, "_primaryStateInformation"));
            if (map.TryGetValue(status, out var value)) value.Count = count;
            else map.Add(status, new CharacterState.StatusEffectStack(Managers.GetStatusEffectManager().Create(status, unit), count));
        }
        private static bool Equal(object left, object right) => JToken.DeepEquals(JToken.FromObject(left), JToken.FromObject(right));

        // Frame-driven UI preparation is an explicit native event, independent of the trigger
        // queue. Record it without suppressing it or inferring it from the expected output.
        [HarmonyPatch(typeof(CharacterState), "PrepareEnchantmentsForPreview")]
        private static class PreviewPreparePatch
        {
            private static void Prefix(CharacterState __instance)
            { if (recordPreviewPreparation) previewPreparedUnits.Add(Trace.UnitId(__instance)); }
        }

        [HarmonyPatch(typeof(CardEffectEnchant), nameof(CardEffectEnchant.OnUpdateEnchantments))]
        private static class UpdatePatch
        {
            private static void Prefix(CardEffectEnchant __instance, out CardEffectEnchant? __state) { __state = currentEffect; if (recording) currentEffect = __instance; }
            private static void Finalizer(CardEffectEnchant? __state) { currentEffect = __state; }
        }
        [HarmonyPatch(typeof(CharacterState), nameof(CharacterState.AddStatusEffect), new[] { typeof(string), typeof(int), typeof(CharacterState.AddStatusEffectParams), typeof(CharacterState), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool) })]
        private static class AddPatch
        {
            private static void Prefix(CharacterState __instance, string statusId, int numStacks, CharacterState.AddStatusEffectParams addStatusEffectParams,
                CharacterState triggeredByCharacter, bool allowModification, bool isFromHiddenTrigger, bool allowDualism)
            {
                if (!recording || currentEffect == null || addStatusEffectParams.fromEffectType != typeof(CardEffectEnchant)) return;
                var p = addStatusEffectParams;
                requests.Add(new EnchantmentRequest(Trace.UnitId(__instance), "Add", statusId, numStacks, p.sourceIsHero, null, "CardEffectEnchant",
                    p.sourceCardState != null, p.sourceRelicState != null, triggeredByCharacter != null, allowModification, isFromHiddenTrigger,
                    allowDualism, p.overrideImmunity, p.spawnEffect, p.fromRoomModifier, null, null, EnchantmentCombatProbe.Snapshot(currentEffect)));
            }
        }
        [HarmonyPatch(typeof(CharacterState), nameof(CharacterState.RemoveStatusEffect), new[] { typeof(string), typeof(int), typeof(CharacterState.RemoveStatusEffectParams), typeof(bool) })]
        private static class RemovePatch
        {
            private static void Prefix(CharacterState __instance, string statusId, int numStacks, CharacterState.RemoveStatusEffectParams removeParams, bool allowModification)
            {
                if (!recording || currentEffect == null || removeParams.fromEffectType != typeof(CardEffectEnchant)) return;
                var p = removeParams;
                requests.Add(new EnchantmentRequest(Trace.UnitId(__instance), "Remove", statusId, numStacks, null, p.showNotification, "CardEffectEnchant",
                    p.sourceCardState != null, p.sourceRelicState != null, false, allowModification, null, null, null, null, null,
                    p.fromCardUpgrade, p.removeAtEndOfTurn, EnchantmentCombatProbe.Snapshot(currentEffect)));
            }
        }
    }
}
