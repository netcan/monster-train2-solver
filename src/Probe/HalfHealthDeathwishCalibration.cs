using System;
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
    // Mutates copied primary states and inspects the real native trigger queue.
    // This component stops at queue admission; gameplay effects are not drained.
    internal static class HalfHealthDeathwishCalibration
    {
        internal static bool Started { get; private set; }
        internal static bool Completed { get; private set; }
        internal static string? Error { get; private set; }
        private static AllGameManagers Managers => AllGameManagers.Instance!;
        private static FullBattleTrace Trace => FullBattleTrace.Active!;
        private static readonly Type StateType = AccessTools.Inner(typeof(CharacterState), "StateInformation");
        private static object Get(object target, string name) => AccessTools.Field(target.GetType(), name).GetValue(target);
        private static void Set(object target, string name, object value) => AccessTools.Field(target.GetType(), name).SetValue(target, value);
        private static Queue<CombatManager.TriggerQueueData> Queue =>
            (Queue<CombatManager.TriggerQueueData>)AccessTools.Property(typeof(CombatManager), "TriggerQueue").GetValue(Managers.GetCombatManager());
        private static CharacterState[] Actors()
        {
            var actors = new List<CharacterState>();
            for (int room = 0; room < Managers.GetRoomManager().GetNumRooms(); room++)
                Managers.GetRoomManager().GetRoom(room).AddCharactersToList(actors, Team.Type.Monsters | Team.Type.Heroes);
            return actors.Where(actor => actor.IsAlive && !actor.IsDestroyed).ToArray();
        }
        private static CharacterState[] Hosts() => Actors().Where(actor => actor.GetTeamType() == Team.Type.Heroes)
            .GroupBy(actor => actor.GetCurrentRoomIndex()).Where(group => group.Count() >= 2 && Actors().Any(actor =>
                actor.GetTeamType() == Team.Type.Monsters && actor.GetCurrentRoomIndex() == group.Key &&
                actor.GetSourceCharacterData().GetAssetKey().StartsWith("TrainSteward", StringComparison.Ordinal)))
            .Select(group => group.Take(2).ToArray()).FirstOrDefault() ?? Array.Empty<CharacterState>();
        internal static bool Eligible => ConditionalActivationProbe.HasVanguard && Hosts().Length == 2 && Queue.Count == 0;
        internal static void Start(ManualLogSource log)
        {
            Started = true;
            try { Capture(); Completed = true; }
            catch (Exception error) { Error = error.ToString(); log.LogError(Error); }
        }
        private static void Capture()
        {
            if (!Eligible) throw new InvalidOperationException("Half-health activation requires two original live enemies and the paid Steward.");
            CharacterState[] actors = Actors(), hosts = Hosts();
            RoomManager rooms = Managers.GetRoomManager();
            SaveManager save = Managers.GetSaveManager();
            TrainCombatState original = Trace.CaptureTrain();
            int originalFrame = UnityEngine.Time.frameCount;
            object Rng() => new { Unity = RngCalibration.Words(UnityEngine.Random.state),
                Streams = Enum.GetValues(typeof(RngId)).Cast<RngId>().Select(id =>
                    new { Id = id.ToString(), Words = RngCalibration.Words(RandomManager.GetState(id)) }).ToArray() };
            object originalRng = Rng();
            object[] states = actors.Select(actor => Get(actor, "_primaryStateInformation")).ToArray();
            object[] triggers = actors.Select(actor => Get(actor, "triggers")).ToArray();
            bool[] enchanters = actors.Select(actor => actor.IsEnchanter).ToArray();
            var identities = (Dictionary<CharacterState, Dictionary<CharacterTriggerState, int>>)Get(Trace, "triggerIdentities");
            var originalIds = actors.Select(actor => Trace.TriggerStates(actor).ToDictionary(item => item.Key, item => item.Value)).ToArray();
            bool allow = rooms.AllowEnchantmentUpdates, updating = (bool)Get(rooms, "_updatingEnchantments");
            var samples = new List<object>();
            int mismatches = 0;
            try
            {
                Set(rooms, "_allowEnchantmentUpdates", true); Set(rooms, "_updatingEnchantments", false);
                foreach (CharacterState actor in actors)
                {
                    Set(actor, "_primaryStateInformation", AccessTools.Method(StateType, "DeepCopyForPreview").Invoke(Get(actor, "_primaryStateInformation"), null));
                    Set(actor, "triggers", new List<CharacterTriggerState>());
                    AccessTools.Property(typeof(CharacterState), "IsEnchanter").SetValue(actor, false);
                    identities[actor] = new Dictionary<CharacterTriggerState, int>();
                }
                foreach (CharacterState actor in hosts)
                {
                    actor.SetHealth(20, 20);
                    foreach (string kind in new[] { "OnDeathwish", "OnVanguardLost", "OnDeathwishLost", "OnVanguard" })
                    {
                        CharacterTriggerData data = HealingScenario.HealGold(1, false, true);
                        Set(data, "trigger", Enum.Parse(typeof(CharacterTriggerData.Trigger), kind));
                        var trigger = new CharacterTriggerState(); trigger.Setup(data, actor.GetTeamType(), save);
                        actor.GetTriggers().Add(trigger);
                    }
                }
                Step("above-half", 20, 20);
                Step("exact-half", 10, 20);
                Step("repeat-half", 10, 20);
                Step("below-half", 9, 20);
                Step("leave-half", 11, 20);
                Step("odd-above", 11, 21);
                Step("odd-below", 10, 21);
                Step("zero-hp", 0, 20);
                Step("negative-hp", -1, 20);
                Step("zero-maximum", 0, 0);
                Step("negative-maximum", 0, -1);
                Step("maximum-hp-below-half", 49999, 99999);
                Step("maximum-hp-above-half", 50000, 99999);
                Step("return-half", 10, 20);
                Raw("muted", 1); Step("muted-half", 10, 20);
                Raw("muted", 0); Step("unmuted-half", 10, 20);
                Raw("silenced", 1); Step("silenced-half", 10, 20);
                Raw("silenced", 0); Step("unsilenced-half", 10, 20);
                Raw("untouchable", 1); Step("untouchable-half", 10, 20);
                Raw("untouchable", 0); Step("touchable-half", 10, 20);
                hosts[1].SetHealth(9, 20); Step("manager-vanguard-before-deathwish", 10, 20, true);
                hosts[1].SetHealth(11, 20); Step("manager-leave-both", 11, 20, true);
                Set(rooms, "_allowEnchantmentUpdates", false);
                hosts[1].SetHealth(10, 20); Step("manager-disabled", 10, 20, true);
                Step("direct-while-disabled", 10, 20);
                Set(rooms, "_allowEnchantmentUpdates", true); Set(rooms, "_updatingEnchantments", true);
                Step("direct-while-updating", 11, 20);
                Step("manager-while-updating", 11, 20, true);
                Set(rooms, "_updatingEnchantments", false); Step("manager-resume", 10, 20, true);
            }
            finally
            {
                Queue.Clear(); Set(rooms, "_allowEnchantmentUpdates", false);
                for (int index = 0; index < actors.Length; index++)
                {
                    Set(actors[index], "_primaryStateInformation", states[index]); Set(actors[index], "triggers", triggers[index]);
                    AccessTools.Property(typeof(CharacterState), "IsEnchanter").SetValue(actors[index], enchanters[index]);
                    identities[actors[index]] = originalIds[index];
                }
                Set(rooms, "_allowEnchantmentUpdates", allow); Set(rooms, "_updatingEnchantments", updating);
            }
            if (!Equal(original, Trace.CaptureTrain())) throw new InvalidOperationException("Half-health activation calibration changed live train/context.");
            object finalRng = Rng();
            if (!Equal(originalRng, finalRng) || UnityEngine.Time.frameCount != originalFrame)
                throw new InvalidOperationException("Half-health activation calibration changed a random stream or advanced a frame.");
            using (var document = NativeFixtureCapture.Capture(new { Schema = 1, GameVersion = UnityEngine.Application.version,
                GameModuleMvid = typeof(CharacterState).Assembly.ManifestModule.ModuleVersionId,
                Boundary = "NativeHalfHealthActivationAndQueueAdmission", LiveContextUnchanged = true,
                RetainedActorsIncluded = true, RngBefore = originalRng, RngAfter = finalRng,
                RngUnchanged = true, FrameUnchanged = true, EffectsDrained = false, Mismatches = mismatches, Samples = samples }))
            using (var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "half-health-deathwish.mt2f"))) document.Write(output);
            if (mismatches != 0) throw new InvalidOperationException("Half-health native activation differs: " + mismatches);

            void Raw(string status, int count)
            {
                var map = (Dictionary<string, CharacterState.StatusEffectStack>)Get(Get(hosts[0], "_primaryStateInformation"), "statusEffects");
                if (map.TryGetValue(status, out var value)) value.Count = count;
                else map.Add(status, new CharacterState.StatusEffectStack(Managers.GetStatusEffectManager().Create(status, hosts[0]), count));
            }
            void Step(string label, int hp, int max, bool manager = false)
            {
                if (Queue.Count != 0) throw new InvalidOperationException("Half-health sample starts with a nonempty queue.");
                hosts[0].SetHealth(hp, max);
                TrainCombatState before = Snapshot();
                int actorId = Trace.UnitId(hosts[0]), roomId = hosts[0].GetCurrentRoomIndex();
                TrainCombatState? predicted;
                EnchantmentCallback[] callbacks;
                string? difference;
                if (manager)
                {
                    var frame = new EnchantmentCombatState(before, Array.Empty<EnchantmentRetainedUnit>(), Array.Empty<int>(),
                        rooms.AllowEnchantmentUpdates, (bool)Get(rooms, "_updatingEnchantments"), save.PreviewMode, default);
                    EnchantmentCombatResult result = EnchantmentCombatModel.UpdateAll(frame);
                    predicted = result.State?.Train; callbacks = result.Callbacks.ToArray(); difference = result.UnsupportedReason;
                    rooms.UpdateEnchantments();
                }
                else
                {
                    RoomCombatResult result = HalfHealthDeathwishModel.Update(before.Rooms.Single(room => room.RoomIndex == roomId), actorId, before.Context);
                    predicted = result.Supported ? new TrainCombatState(before.Rooms.Select(room => room.RoomIndex == roomId ? result.State! : room).ToArray(),
                        before.Movement, before.EnemySlotsPerRoom, before.Context) : null;
                    callbacks = result.PendingCallbacks.Select(EnchantmentCallback.From).ToArray(); difference = result.UnsupportedReason;
                    AccessTools.Method(typeof(CharacterState), "TryUpdateDeathwish").Invoke(hosts[0], null);
                }
                TrainCombatState actual = Snapshot();
                EnchantmentCallback[] queued = Queue.Select(item => new EnchantmentCallback(item.character.GetCurrentRoomIndex(), Trace.UnitId(item.character),
                    item.trigger.ToString(), item.fireTriggersData?.paramInt ?? 0, item.fireTriggersData?.paramInt2 ?? 0, item.fireTriggersData?.paramString,
                    item.fireTriggersData?.overrideTargetCharacter == null ? (int?)null : Trace.UnitId(item.fireTriggersData.overrideTargetCharacter),
                    item.dyingCharacter == null ? (int?)null : Trace.UnitId(item.dyingCharacter), item.canFireTriggers,
                    item.exclusiveTrigger == null ? (int?)null : Trace.TriggerStateId(item.character, item.exclusiveTrigger))).ToArray();
                difference ??= predicted == null ? "Missing prediction" : !Equal(predicted, actual) ? "Native source/room/context state differs" :
                    !Equal(callbacks, queued) ? "Native queue payload/order differs" : null;
                if (difference != null) mismatches++;
                samples.Add(new { Label = label, Manager = manager, ActorId = actorId, RoomIndex = roomId,
                    AllowUpdates = rooms.AllowEnchantmentUpdates, Updating = (bool)Get(rooms, "_updatingEnchantments"),
                    Before = before, Actual = actual, Callbacks = queued, Difference = difference, Completed = true });
                Queue.Clear();
            }
            TrainCombatState Snapshot()
            {
                TrainCombatState train = Trace.CaptureTrain();
                // Room enumeration excludes zero/negative HP. Direct native
                // TryUpdateDeathwish still acts on these retained objects.
                return new TrainCombatState(train.Rooms.Select(room => new RoomCombatState(room.RoomIndex, room.Deployment,
                    room.Units.Concat(hosts.Where(host => host.GetCurrentRoomIndex() == room.RoomIndex &&
                        room.Units.All(unit => unit.Id != Trace.UnitId(host))).Select(host => Trace.CaptureUnit(host))).ToArray(),
                    room.ExternalInteractions, room.Context, room.Preview, room.ActiveCompanionBossIds)).ToArray(),
                    train.Movement, train.EnemySlotsPerRoom, train.Context);
            }
        }
        private static bool Equal(object left, object right) => JToken.DeepEquals(JToken.FromObject(left), JToken.FromObject(right));
    }
}
