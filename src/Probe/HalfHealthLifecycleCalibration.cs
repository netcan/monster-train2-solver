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
    internal static class HalfHealthLifecycleCalibration
    {
        internal static bool Started { get; private set; }
        internal static bool Completed { get; private set; }
        internal static string? Error { get; private set; }
        private static bool recording;
        private static readonly List<CharacterTriggerDispatch> dispatches = new List<CharacterTriggerDispatch>();
        private static AllGameManagers Managers => AllGameManagers.Instance!;
        private static SaveManager Save => Managers.GetSaveManager();
        private static FullBattleTrace Trace => FullBattleTrace.Active!;
        private static readonly Type StateType = AccessTools.Inner(typeof(CharacterState), "StateInformation");
        private static object Get(object target, string field) => AccessTools.Field(target.GetType(), field).GetValue(target);
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
        private static Queue<CombatManager.TriggerQueueData> Queue =>
            (Queue<CombatManager.TriggerQueueData>)AccessTools.Property(typeof(CombatManager), "TriggerQueue").GetValue(Managers.GetCombatManager());
        private static CharacterState[] Actors()
        {
            var actors = new List<CharacterState>();
            for (int room = 0; room < Managers.GetRoomManager().GetNumRooms(); room++)
                Managers.GetRoomManager().GetRoom(room).AddCharactersToList(actors, Team.Type.Monsters | Team.Type.Heroes);
            return actors.Where(actor => actor.IsAlive && !actor.IsDestroyed).ToArray();
        }
        private static CharacterState? Host() => Actors().FirstOrDefault(actor => actor.GetTeamType() == Team.Type.Heroes &&
            Actors().Any(other => other.GetTeamType() == Team.Type.Monsters && other.GetCurrentRoomIndex() == actor.GetCurrentRoomIndex() &&
                other.GetSourceCharacterData().GetAssetKey().StartsWith("TrainSteward", StringComparison.Ordinal)));
        internal static bool Eligible => ConditionalActivationProbe.HasVanguard && Host() != null && Queue.Count == 0;
        internal static void Start(ManualLogSource log)
        { Started = true; Save.StartCoroutine(Protect(Run(log), log)); }
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
            finally { while (stack.Count > 0) (stack.Pop() as IDisposable)?.Dispose(); recording = false; }
            Completed = Error == null;
        }
        private static object Rng() => new { Unity = RngCalibration.Words(UnityEngine.Random.state),
            Streams = Enum.GetValues(typeof(RngId)).Cast<RngId>().Select(id =>
                new { Id = id.ToString(), Words = RngCalibration.Words(RandomManager.GetState(id)) }).ToArray() };
        private static HadesRNG NativeRng(RngId id) => (HadesRNG)AccessTools.Method(typeof(RandomManager), "GetRng").Invoke(null, new object[] { id });
        private static JToken GameplayRng(object snapshot)
        {
            var value = JObject.FromObject(snapshot);
            value["Streams"] = new JArray(value["Streams"]!.Where(item => (string?)item["Id"] != "NonDeterministic" && (string?)item["Id"] != "Chatter"));
            return value;
        }
        private static IEnumerator Run(ManualLogSource log)
        {
            CharacterState[] actors = Actors(); CharacterState host = Host()!;
            RoomManager rooms = Managers.GetRoomManager();
            TrainCombatState original = Trace.CaptureTrain(); object originalRng = Rng();
            var originalStreams = Enum.GetValues(typeof(RngId)).Cast<RngId>().ToDictionary(id => id, id => new HadesRNG(NativeRng(id)));
            UnityEngine.Random.State originalUnity = UnityEngine.Random.state;
            object[] states = actors.Select(actor => Get(actor, "_primaryStateInformation")).ToArray();
            object[] triggers = actors.Select(actor => Get(actor, "triggers")).ToArray();
            bool[] enchanters = actors.Select(actor => actor.IsEnchanter).ToArray();
            var identities = (Dictionary<CharacterState, Dictionary<CharacterTriggerState, int>>)Get(Trace, "triggerIdentities");
            var originalIds = actors.Select(actor => Trace.TriggerStates(actor).ToDictionary(item => item.Key, item => item.Value)).ToArray();
            bool allow = rooms.AllowEnchantmentUpdates;
            int originalGold = Save.GetGold();
            CardStatistics statistics = Managers.GetCardStatistics();
            int originalLastAttackDamage = (int)Get(statistics, "lastAttackDamageDealt");
            var samples = new List<object>(); int mismatches = 0;
            try
            {
                foreach (CharacterState actor in actors)
                {
                    Set(actor, "_primaryStateInformation", AccessTools.Method(StateType, "DeepCopyForPreview").Invoke(Get(actor, "_primaryStateInformation"), null));
                    Set(actor, "triggers", new List<CharacterTriggerState>());
                    AccessTools.Property(typeof(CharacterState), "IsEnchanter").SetValue(actor, false);
                    identities[actor] = new Dictionary<CharacterTriggerState, int>();
                }
                object clean = AccessTools.Method(StateType, "DeepCopyForPreview").Invoke(Get(host, "_primaryStateInformation"), null);
                foreach (string label in new[] { "damage-enters-half", "damage-remains-half", "damage-zero-enters-half", "damage-armor-blocked",
                    "heal-leaves-half", "heal-remains-half", "heal-zero-enters-half", "heal-clipped", "heal-zero-leaves-half",
                    "damage-onhit-heals-first", "heal-onheal-damages-first", "gain-callback-heals-and-loses", "lost-callback-damages-and-gains" })
                {
                    Set(host, "_primaryStateInformation", AccessTools.Method(StateType, "DeepCopyForPreview").Invoke(clean, null));
                    var statuses = (Dictionary<string, CharacterState.StatusEffectStack>)Get(Get(host, "_primaryStateInformation"), "statusEffects");
                    foreach (var status in statuses.Values) status.Count = 0;
                    Set(host, "triggers", new List<CharacterTriggerState>()); identities[host] = new Dictionary<CharacterTriggerState, int>();
                    // SetHealth is deliberately raw: stale membership controls prove that
                    // the lifecycle API checks even after zero damage/healing.
                    host.SetHealth(11, 20);
                    Add("OnHit", 2, label == "damage-onhit-heals-first" ? "CardEffectHeal" : null);
                    Add("OnHeal", 3, label == "heal-onheal-damages-first" ? "CardEffectDamage" : null);
                    Add("OnDeathwish", 5, label == "gain-callback-heals-and-loses" ? "CardEffectHeal" : null);
                    Add("OnDeathwishLost", 7, label == "lost-callback-damages-and-gains" ? "CardEffectDamage" : null);
                    bool heal = label.StartsWith("heal-", StringComparison.Ordinal) || label.StartsWith("lost-", StringComparison.Ordinal);
                    bool active = label == "heal-leaves-half" || label == "heal-remains-half" || label == "heal-onheal-damages-first" ||
                        label == "lost-callback-damages-and-gains" || label == "heal-zero-leaves-half" || label == "damage-remains-half";
                    if (active) { host.SetHealth(10, 20); AccessTools.Method(typeof(CharacterState), "TryUpdateDeathwish").Invoke(host, null); Queue.Clear(); }
                    int hp = active || label == "damage-zero-enters-half" || label == "heal-zero-enters-half" ? 10 : 11;
                    if (label == "heal-remains-half") hp = 8;
                    if (label == "heal-clipped") hp = 20;
                    if (label == "heal-zero-leaves-half") hp = 11;
                    host.SetHealth(hp, 20);
                    if (label == "damage-armor-blocked")
                    {
                        if (statuses.TryGetValue("armor", out var armor)) armor.Count = 3;
                        else statuses.Add("armor", new CharacterState.StatusEffectStack(Managers.GetStatusEffectManager().Create("armor", host), 3));
                    }
                    int amount = label.Contains("zero") ? 0 : 1;
                    RoomCombatState before = Trace.Capture(host.GetCurrentRoom()); object rngBefore = Rng();
                    RoomCombatResult result = heal ? RoomCombatModel.ApplyCardHeal(before, Trace.UnitId(host), amount, captureAllDispatches: true) :
                        RoomCombatModel.ApplyCardDamage(before, Trace.UnitId(host), amount, captureAllDispatches: true);
                    dispatches.Clear(); recording = true;
                    try
                    {
                        if (heal) yield return host.ApplyHeal(amount);
                        else yield return Managers.GetCombatManager().ApplyDamageToTarget(amount, host,
                            new CombatManager.ApplyDamageToTargetParameters { damageType = Damage.Type.Default, finalEffectInSequence = true });
                    }
                    finally { recording = false; }
                    if (actors.Any(actor => !actor.IsAlive || actor.IsDestroyed) || Queue.Count != 0)
                        throw new InvalidOperationException("Half-health lifecycle calibration left a dead actor or pending queue.");
                    RoomCombatState actual = Trace.Capture(host.GetCurrentRoom()); object rngAfter = Rng();
                    bool gameplayRngUnchanged = JToken.DeepEquals(GameplayRng(rngBefore), GameplayRng(rngAfter));
                    string? difference = result.UnsupportedReason ?? Difference(result.State, actual) ?? Difference(result.Dispatches, dispatches) ??
                        (gameplayRngUnchanged ? null : "Native gameplay RNG or Unity RNG changed.");
                    if (difference != null) { mismatches++; log.LogWarning(label + ": " + difference); }
                    samples.Add(new { Label = label, Operation = heal ? "Heal" : "Damage", Amount = amount, ActorId = Trace.UnitId(host),
                        Before = before, Actual = actual, Dispatches = dispatches.ToArray(), RngBefore = rngBefore, RngAfter = rngAfter, GameplayRngUnchanged = gameplayRngUnchanged,
                        Predicted = difference == null ? null : result.State, Difference = difference, Completed = true });
                }
            }
            finally
            {
                recording = false; Queue.Clear(); Set(rooms, "_allowEnchantmentUpdates", false);
                for (int index = 0; index < actors.Length; index++)
                {
                    Set(actors[index], "_primaryStateInformation", states[index]); Set(actors[index], "triggers", triggers[index]);
                    AccessTools.Property(typeof(CharacterState), "IsEnchanter").SetValue(actors[index], enchanters[index]); identities[actors[index]] = originalIds[index];
                }
                Set(statistics, "lastAttackDamageDealt", originalLastAttackDamage);
                Save.SetGold(originalGold); Set(rooms, "_allowEnchantmentUpdates", allow);
                foreach (var pair in originalStreams) NativeRng(pair.Key).Init(pair.Value);
                UnityEngine.Random.state = originalUnity;
            }
            string? restoration = Difference(original, Trace.CaptureTrain()) ?? Difference(originalRng, Rng());
            using (var document = NativeFixtureCapture.Capture(new { Schema = 1, GameVersion = UnityEngine.Application.version,
                GameModuleMvid = typeof(CharacterState).Assembly.ManifestModule.ModuleVersionId, Boundary = "NativeHalfHealthDamageHealLifecycle",
                LiveContextUnchanged = restoration == null, RestorationDifference = restoration, EffectsDrained = true,
                RngBefore = originalRng, RngAfter = Rng(), CosmeticStreams = new[] { "NonDeterministic", "Chatter" }, Mismatches = mismatches, Samples = samples }))
            using (var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "half-health-lifecycle.mt2f"))) document.Write(output);
            if (restoration != null || mismatches != 0) throw new InvalidOperationException("Half-health lifecycle differs: " + mismatches + "; restoration: " + restoration);

            void Add(string kind, int gold, string? effectType)
            {
                CharacterTriggerData data = HealingScenario.HealGold(gold, effectType != null, false);
                Set(data, "trigger", Enum.Parse(typeof(CharacterTriggerData.Trigger), kind));
                if (effectType != null)
                {
                    var effect = new CardEffectData(effectType, null!, Team.Type.Heroes); effect.Cheat_SetTargetMode(TargetMode.Self); Set(effect, "paramInt", 1);
                    Set(data, "effects", data.GetEffects().Concat(new[] { effect }).ToList());
                }
                var trigger = new CharacterTriggerState(); trigger.Setup(data, host.GetTeamType(), Save); host.GetTriggers().Add(trigger);
            }
        }
        private static string? Difference(object? left, object? right) => Difference(left == null ? JValue.CreateNull() : JToken.FromObject(left),
            right == null ? JValue.CreateNull() : JToken.FromObject(right), "$");
        private static string? Difference(JToken left, JToken right, string path)
        {
            if (JToken.DeepEquals(left, right)) return null;
            if (left is JObject a && right is JObject b)
            {
                foreach (string name in a.Properties().Select(property => property.Name).Union(b.Properties().Select(property => property.Name)))
                { string? value = Difference(a[name] ?? JValue.CreateNull(), b[name] ?? JValue.CreateNull(), path + "." + name); if (value != null) return value; }
            }
            if (left is JArray x && right is JArray y && x.Count == y.Count)
                for (int index = 0; index < x.Count; index++) { string? value = Difference(x[index], y[index], path + "[" + index + "]"); if (value != null) return value; }
            return path + ": model=" + left.ToString(Newtonsoft.Json.Formatting.None) + ", native=" + right.ToString(Newtonsoft.Json.Formatting.None);
        }

        [HarmonyPatch(typeof(CharacterState), nameof(CharacterState.FireTriggers))]
        private static class DispatchPatch
        {
            private static void Prefix(CharacterState __instance, CharacterTriggerData.Trigger trigger, CharacterState dyingCharacter,
                bool fromRunningTriggerQueue, CharacterState.FireTriggersData fireTriggersData, int triggerCount, CharacterTriggerState exclusiveTrigger)
            {
                if (!recording || !fromRunningTriggerQueue) return;
                dispatches.Add(new CharacterTriggerDispatch(Trace.UnitId(__instance), trigger.ToString(), dyingCharacter == null ? 0 : Trace.UnitId(dyingCharacter),
                    triggerCount, fireTriggersData?.paramInt ?? 0, fireTriggersData?.paramInt2 ?? 0, fireTriggersData?.paramString,
                    fireTriggersData?.overrideTargetCharacter == null ? 0 : Trace.UnitId(fireTriggersData.overrideTargetCharacter),
                    exclusiveTriggerStateId: exclusiveTrigger == null ? (int?)null : Trace.TriggerStateId(__instance, exclusiveTrigger)));
            }
        }
    }
}
