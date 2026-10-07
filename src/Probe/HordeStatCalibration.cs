using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal static class HordeStatCalibration
    {
        private static CharacterState? activeTarget;
        private static bool observingRemoval;
        private static int requestedRemoval;

        internal static void Capture(FullBattleTrace trace)
        {
            AllGameManagers managers = AllGameManagers.Instance!;
            var characters = new List<CharacterState>();
            managers.GetRoomManager()!.GetRoom(3).AddCharactersToList(characters, Team.Type.Monsters);
            CharacterState target = characters.Single(unit => unit.IsPyreHeart());
            object state = AccessTools.Property(typeof(CharacterState), "PrimaryStateInformation").GetValue(target);
            Type stateType = state.GetType();
            object originalMap = AccessTools.Field(stateType, "statusEffects").GetValue(state);
            var copiedMap = new Dictionary<string, CharacterState.StatusEffectStack>((Dictionary<string, CharacterState.StatusEffectStack>)originalMap);
            if (target.HasStatusEffect("fixedattack") || target.HasStatusEffect("armorattack") || target.HasStatusEffect("equalizer") ||
                AccessTools.Property(stateType, "linkedCharacterState").GetValue(state) != null)
                throw new InvalidOperationException("Horde numerical calibration requires unlinked raw attack state.");
            CharacterData originalData = target.GetSourceCharacterData();
            CharacterData data = UnityEngine.Object.Instantiate(originalData);
            var nativeHorde = (StatusEffectHordeState)managers.GetStatusEffectManager().Create("horde", target)!;
            var stack = new CharacterState.StatusEffectStack(nativeHorde, 0); copiedMap["horde"] = stack;
            var change = AccessTools.Method(typeof(StatusEffectHordeState), "OnStacksChangedUpdateCharacterStats");
            var threshold = AccessTools.Method(typeof(StatusEffectHordeState), "OnHealthChangedShouldRemoveStatusStacks");
            var casualties = AccessTools.Method(typeof(StatusEffectHordeState), "OnHealthChangedRemoveStatusStacks");
            HordeStats Snapshot() => new HordeStats(target.GetUnbuffedAttackDamage(), target.GetHP(), target.GetMaxHP());
            CombatContext originalContext = trace.CaptureContext();
            CombatUnit originalUnit = trace.CaptureUnit(target);
            HordeStats originalStats = Snapshot();
            bool originalDirty = (bool)AccessTools.Field(stateType, "hpDirtied").GetValue(state);
            var samples = new List<object>(); var casualtySamples = new List<object>();
            int mismatches = 0;
            var definitions = new[] { new HordeBaseStats(0, 1), new HordeBaseStats(3, 7), new HordeBaseStats(17, 25),
                new HordeBaseStats(3, 99999), new HordeBaseStats(int.MaxValue, int.MaxValue) };
            var starting = new[] { new HordeStats(0, 0, 0), new HordeStats(17, 15, 21),
                new HordeStats(-3, 31, 99999), new HordeStats(int.MaxValue, 99999, 99999) };
            try
            {
                activeTarget = target;
                AccessTools.Field(typeof(CharacterState), "characterData").SetValue(target, data);
                AccessTools.Field(stateType, "statusEffects").SetValue(state, copiedMap);
                foreach (HordeBaseStats definition in definitions)
                {
                    SetDefinition(definition);
                    foreach (HordeStats seed in starting)
                    foreach (int delta in new[] { 0, 1, 2, 9999, -1, -2, -9999 })
                    foreach (int total in new[] { 0, 1, 2, 9999 })
                    {
                        target.SetAttackDamage(seed.Attack); target.SetHealth(seed.Health, seed.MaxHealth);
                        HordeStats before = Snapshot();
                        change.Invoke(nativeHorde, new object[] { target, delta, total });
                        HordeStats after = Snapshot();
                        if (!JToken.DeepEquals(JToken.FromObject(HordeStatModel.Change(before, definition, delta, total)), JToken.FromObject(after))) mismatches++;
                        samples.Add(new { Definition = definition, Delta = delta, Total = total, Before = before, After = after });
                    }
                    foreach (int count in new[] { 0, 1, 2, 3, 10, 9999, int.MaxValue })
                    foreach (int hp in new[] { 0, 99999, unchecked((count - 1) * definition.Health - 1),
                        unchecked((count - 1) * definition.Health), unchecked((count - 1) * definition.Health + 1) })
                    {
                        target.SetAttackDamage(17); target.SetHealth(hp, 99999); stack.Count = count;
                        int actualCount = target.GetStatusEffectStacks("horde");
                        HordeStats before = Snapshot();
                        bool shouldRemove = (bool)threshold.Invoke(null, new object[] { target });
                        requestedRemoval = 0; observingRemoval = true;
                        try { casualties.Invoke(null, new object[] { target }); }
                        finally { observingRemoval = false; }
                        var after = new HordeCasualties(shouldRemove, requestedRemoval);
                        if (!JToken.DeepEquals(JToken.FromObject(HordeStatModel.Casualties(before, definition, actualCount)), JToken.FromObject(after))) mismatches++;
                        casualtySamples.Add(new { Definition = definition, RequestedStacks = count, Stacks = actualCount, Before = before, After = after });
                    }
                }
            }
            finally
            {
                observingRemoval = false;
                AccessTools.Field(typeof(CharacterState), "characterData").SetValue(target, originalData);
                AccessTools.Field(stateType, "statusEffects").SetValue(state, originalMap);
                target.SetAttackDamage(originalStats.Attack); target.SetHealth(originalStats.Health, originalStats.MaxHealth);
                AccessTools.Field(stateType, "hpDirtied").SetValue(state, originalDirty);
                activeTarget = null; UnityEngine.Object.Destroy(data);
            }
            bool restored = JToken.DeepEquals(JToken.FromObject(originalContext), JToken.FromObject(trace.CaptureContext())) &&
                JToken.DeepEquals(JToken.FromObject(originalUnit), JToken.FromObject(trace.CaptureUnit(target)));
            string path = Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "horde-stat-calibration.mt2f");
            using (var archive = NativeFixtureCapture.Capture(new { Schema = 1, GameVersion = Application.version,
                GameModuleMvid = typeof(CharacterState).Assembly.ManifestModule.ModuleVersionId, LiveContextUnchanged = restored,
                Mismatches = mismatches, Samples = samples, Casualties = casualtySamples }))
            using (var stream = File.Create(path)) archive.Write(stream);
            if (!restored || mismatches != 0) throw new InvalidOperationException("Horde numeric calibration differs or changed live state: " + mismatches);
            void SetDefinition(HordeBaseStats definition)
            {
                AccessTools.Field(typeof(CharacterData), "attackDamage").SetValue(data, definition.Attack);
                AccessTools.Field(typeof(CharacterData), "health").SetValue(data, definition.Health);
            }
        }

        [HarmonyPatch(typeof(CharacterState), "UpdateCharacterStateUI")]
        private static class UiPatch
        { private static bool Prefix(CharacterState __instance) => activeTarget == null || __instance != activeTarget; }

        [HarmonyPatch(typeof(CharacterState), nameof(CharacterState.RemoveStatusEffect), new[] { typeof(string), typeof(int), typeof(bool) })]
        private static class RemovalPatch
        {
            private static bool Prefix(CharacterState __instance, string statusId, int numStacks)
            {
                if (__instance != activeTarget || !observingRemoval || statusId != "horde") return true;
                requestedRemoval = numStacks; return false;
            }
        }
    }
}
