using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    // Observation only: original dissolve callbacks, queues and Unity destruction
    // run unchanged. This API calibration does not supply future events to a policy.
    internal static class CharacterRemovalProbe
    {
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_CHARACTER_REMOVAL") == "1";
        private sealed class IdentityComparer : IEqualityComparer<object>
        {
            public new bool Equals(object? left, object? right) => ReferenceEquals(left, right);
            public int GetHashCode(object item) => RuntimeHelpers.GetHashCode(item);
        }
        private static readonly Dictionary<object, int> identities = new Dictionary<object, int>(new IdentityComparer());
        private static readonly Dictionary<string, int> callbacks = new Dictionary<string, int>();
        private static readonly List<Sample> samples = new List<Sample>();
        private static readonly List<string> errors = new List<string>();
        private sealed class Sample
        {
            public string Operation { get; set; } = "";
            public int ActorId { get; set; }
            public int RequestedStage { get; set; }
            public int Frame { get; set; }
            public int Turn { get; set; }
            public bool DuringPreview { get; set; }
            public CharacterRemovalState[] Before { get; set; } = Array.Empty<CharacterRemovalState>();
            public int[] QueueIds { get; set; } = Array.Empty<int>();
            public CharacterRemovalState[]? After { get; set; }
            public bool Completed { get; set; }
            public string? Difference { get; set; }
        }
        private static bool Armed(string operation) => Enabled && FullBattleTrace.Active?.NativeWon == null && FullBattleTrace.Active != null &&
            AllGameManagers.Instance != null && AllGameManagers.Instance.GetCombatManager() != null &&
            (operation == "OnDestroy" || !AllGameManagers.Instance.GetSaveManager().PreviewMode);
        private static int Id(object? value)
        {
            if (ReferenceEquals(value, null)) return 0;
            if (!identities.TryGetValue(value!, out int id)) identities.Add(value!, id = identities.Count + 1);
            return id;
        }
        private static object? Field(object value, string name) => AccessTools.Field(value.GetType(), name).GetValue(value);
        private static int[] One(object? value) => ReferenceEquals(value, null) ? Array.Empty<int>() : new[] { Id(value) };
        private static RemovalReferences Reference(object value, string name) => new RemovalReferences(name, One(Field(value, name)));
        private static int[] ListIds(object? value) => value is IEnumerable list ? list.Cast<object>().Select(Id).ToArray() : Array.Empty<int>();
        private static IEnumerable<DictionaryEntry> Entries(IDictionary dictionary)
        { foreach (DictionaryEntry entry in dictionary) yield return entry; }
        private static int[] ListenerIds(object signal, string name)
        {
            Delegate? value = Field(signal, name) as Delegate;
            return value?.GetInvocationList().Select(callback =>
            {
                string key = callback.Method.Module.ModuleVersionId + ":" + callback.Method.MetadataToken + ":" + Id(callback.Target);
                if (!callbacks.TryGetValue(key, out int id)) callbacks.Add(key, id = callbacks.Count + 1);
                return id;
            }).ToArray() ?? Array.Empty<int>();
        }
        private static RemovalInformation? Information(CharacterState actor, string name)
        {
            object? value = Field(actor, name);
            if (value == null) return null;
            var references = new List<RemovalReferences>();
            foreach (string field in new[] { "spawnPoint", "lastKnownSpawnPoint", "lastAttackerCharacter", "lastFeederCharacter" })
                references.Add(Reference(value, field));
            references.Add(new RemovalReferences("linkedCharacterState", One(AccessTools.Property(value.GetType(), "linkedCharacterState").GetValue(value))));
            foreach (string field in new[] { "equipment", "appliedCardUpgrades" })
                references.Add(new RemovalReferences(field, ListIds(Field(value, field))));
            foreach (string field in new[] { "deathSignal", "characterRemovedSignal", "hpChangedSignal", "statusEffectChangedSignal" })
            {
                object signal = Field(value, field)!;
                foreach (string listener in new[] { "Listener", "OnceListener" })
                    references.Add(new RemovalReferences(field + "." + listener, ListenerIds(signal, listener)));
            }
            var numbers = new List<RemovalNumber>();
            foreach (FieldInfo field in value.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                object? scalar = field.GetValue(value);
                if (scalar is bool flag) numbers.Add(new RemovalNumber(field.Name, flag ? 1 : 0));
                else if (scalar is int number) numbers.Add(new RemovalNumber(field.Name, number));
                else if (scalar is Enum enumeration) numbers.Add(new RemovalNumber(field.Name, Convert.ToInt32(enumeration)));
                else if (scalar?.GetType().Name == "ObfuscatedNumber")
                    numbers.Add(new RemovalNumber(field.Name, Convert.ToInt32(AccessTools.Property(scalar.GetType(), "Value").GetValue(scalar))));
            }
            var maximums = Entries((IDictionary)Field(value, "maxHpFromUpgrades")!)
                .Select(item => new RemovalNumber((string)item.Key, (int)item.Value)).ToArray();
            var statuses = Entries((IDictionary)Field(value, "statusEffects")!)
                .Select(item => new RemovalNumber((string)item.Key, (int)AccessTools.Property(item.Value.GetType(), "Count").GetValue(item.Value))).ToArray();
            return new RemovalInformation(name, references, maximums, numbers, statuses);
        }
        private static CharacterRemovalState Capture(CharacterState actor)
        {
            var information = new[] { "_primaryStateInformation", "_previewStateInformation", "_temporaryStateInformation" }
                .Select(name => Information(actor, name)).Where(info => info != null).Cast<RemovalInformation>().ToArray();
            return new CharacterRemovalState(Id(actor), (int)(CharacterState.DestroyedState)Field(actor, "destroyedState")!,
                Id(actor.GetWeakReference().Ref), new[] { "combatManager", "allGameManagers", "characterManager", "roomManager", "anchor" }
                    .Select(name => Reference(actor, name)).ToArray(), information);
        }
        private static Sample? Begin(string operation, CharacterState? actor = null, int requested = 0, object? manager = null)
        {
            if (!Armed(operation) || !ReferenceEquals(actor, null) && (actor!.SpawnedInPreviewMode || Field(actor, "_primaryStateInformation") == null)) return null;
            try
            {
                CharacterState[] actors = ReferenceEquals(actor, null) ? FullBattleTrace.Active!.KnownUnits.ToArray() : new[] { actor! };
                var sample = new Sample { Operation = operation, ActorId = Id(actor), RequestedStage = requested,
                    Frame = Time.frameCount, Turn = AllGameManagers.Instance!.GetCombatManager()!.GetTurnCount(),
                    DuringPreview = AllGameManagers.Instance.GetSaveManager().PreviewMode,
                    Before = actors.Select(Capture).ToArray(),
                    QueueIds = manager == null ? Array.Empty<int>() : ListIds(Field(manager, manager is HeroManager ? "removeHeroes" : "removeMonsters")) };
                samples.Add(sample); return sample;
            }
            catch (Exception error) { errors.Add(operation + ": " + error); return null; }
        }
        private static void Complete(Sample? sample, CharacterState? actor = null)
        {
            if (sample == null) return;
            try
            {
                sample.After = ReferenceEquals(actor, null) ? FullBattleTrace.Active!.KnownUnits.Select(Capture).ToArray() : new[] { Capture(actor!) };
                var predicted = sample.Operation == "ProcessQueue" ? CharacterRemovalModel.ProcessQueue(sample.Before, sample.QueueIds).ToArray() :
                    new[] { sample.Operation == "OnDestroy" ? CharacterRemovalModel.CompleteDestruction(sample.Before[0]) :
                        CharacterRemovalModel.SetStage(sample.Before[0], sample.RequestedStage) };
                sample.Difference = JToken.DeepEquals(JToken.FromObject(predicted), JToken.FromObject(sample.After)) ? null : "Removal API state differs";
                sample.Completed = true;
            }
            catch (Exception error) { errors.Add(sample.Operation + ": " + error); }
        }
        internal static void Write()
        {
            if (!Enabled) return;
            string path = Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "character-removal-calibration.mt2f");
            using (var archive = NativeFixtureCapture.Capture(new { Schema = 1, GameVersion = Application.version,
                GameModuleMvid = typeof(CharacterState).Assembly.ManifestModule.ModuleVersionId,
                Boundary = "NativeCharacterRemovalApis", GameplaySuppressed = false,
                Errors = errors, Samples = samples }))
            using (var stream = File.Create(path)) archive.Write(stream);
        }
        [HarmonyPatch(typeof(CharacterState), nameof(CharacterState.SetDestroyedState))]
        private static class StagePatch
        {
            private static void Prefix(CharacterState __instance, CharacterState.DestroyedState destroyedState, out Sample? __state)
                => __state = Begin("SetStage", __instance, (int)destroyedState);
            private static void Postfix(CharacterState __instance, Sample? __state) => Complete(__state, __instance);
        }
        [HarmonyPatch(typeof(CharacterState), "OnDestroy")]
        private static class DestroyPatch
        {
            private static void Prefix(CharacterState __instance, out Sample? __state) => __state = Begin("OnDestroy", __instance);
            private static void Postfix(CharacterState __instance, Sample? __state) => Complete(__state, __instance);
        }
        [HarmonyPatch]
        private static class ManagerPatch
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                yield return AccessTools.Method(typeof(HeroManager), nameof(HeroManager.ProcessRemovals));
                yield return AccessTools.Method(typeof(MonsterManager), nameof(MonsterManager.ProcessRemovals));
            }
            private static void Prefix(object __instance, out Sample? __state) => __state = Begin("ProcessQueue", manager: __instance);
            private static void Postfix(Sample? __state) => Complete(__state);
        }
    }
}
