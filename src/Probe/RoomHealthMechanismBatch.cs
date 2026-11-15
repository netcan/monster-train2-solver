#if DEVELOPMENT_PROBE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;
using MonsterTrain2Poju.NativeCases;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal static class RoomHealthMechanismBatch
    {
        internal static void Capture(FullBattleTrace trace, AllGameManagers managers)
        {
            if (Environment.GetEnvironmentVariable("MT2_PROBE_ROOM_HEALTH_MECHANISM_BATCH") != "1") return;
            if (typeof(CardState).Assembly.ManifestModule.ModuleVersionId.ToString() != "bf93f060-beda-46b1-b808-95ebf1e9a99d")
                throw new InvalidOperationException("Room-health mechanism batch requires the pinned native 2.3.1 module.");
            var save = managers.GetSaveManager(); var all = save.GetAllGameData(); var combat = managers.GetCombatManager();
            var queue = (System.Collections.ICollection)AccessTools.Property(typeof(CombatManager), "TriggerQueue").GetValue(combat);
            if (save.PreviewMode || combat.IsRunningTriggerQueue || queue.Count != 0)
                throw new InvalidOperationException("Mechanism batch requires an empty settled primary queue.");
            var native = new List<(RoomHealthMechanismDefinition Definition, RoomModifierData Data)>();
            void Add(string collection, string id, string key, IReadOnlyList<RoomModifierData> modifiers)
            {
                for (int i = 0; i < modifiers.Count; i++)
                {
                    var data = modifiers[i]; string type = data.GetRoomStateModifierClassName();
                    if (RoomHealthDamageModel.Known(type)) native.Add((new RoomHealthMechanismDefinition(type,
                        data.GetParamInt(), data.GetParamInt2(), collection, id, key, i), data));
                }
            }
            foreach (var card in all.GetAllCardData())
                if (card.GetSpawnCharacterData() is { } character)
                    Add("Card", card.GetID(), card.name, character.GetRoomModifiersData());
            foreach (var upgrade in all.GetAllCardUpgradeData())
                Add("Upgrade", upgrade.GetID(), upgrade.name, upgrade.GetRoomModifierUpgrades());
            if (native.Select(item => item.Definition.NativeType).Distinct().Count() != 2)
                throw new InvalidOperationException("Original definitions do not exercise both room-health classes.");
            var definitions = RoomHealthMechanismCases.Definitions(native.Select(item => item.Definition));
            RoomModifierData Original(RoomHealthMechanismDefinition definition) => native.Single(item =>
                item.Definition.Collection == definition.Collection && item.Definition.SourceId == definition.SourceId &&
                item.Definition.ModifierIndex == definition.ModifierIndex).Data;
            JToken Originals() => JToken.FromObject(native.Select(item => new {
                item.Definition, Native = RuleCatalogProbe.RoomModifier(item.Data) }));
            var originalsBefore = Originals();
            var decisionBefore = JToken.FromObject(trace.CaptureDecision());
            var rngBefore = RngStates(); int frame = Time.frameCount;
            var timer = Stopwatch.StartNew(); var records = new List<object>();
            var obj = new GameObject("Poju generated room-health queries"); obj.SetActive(false);
            try
            {
                var actor = obj.AddComponent<CharacterState>();
                Set(actor, "allGameManagers", managers); Set(actor, "combatManager", combat);
                var destroyedField = AccessTools.Field(typeof(CharacterState), "destroyedState");
                destroyedField.SetValue(actor, Enum.Parse(destroyedField.FieldType, "None"));
                var stateType = AccessTools.Inner(typeof(CharacterState), "StateInformation");
                object state = Activator.CreateInstance(stateType, true);
                Set(actor, "_primaryStateInformation", state); Set(actor, "_previewStateInformation", state);
                var roomPoint = new SpawnPoint(); Set(roomPoint, "roomState", managers.GetRoomManager().GetRoom(0));
                var orphanPoint = new SpawnPoint();
                var effects = definitions.Select(definition =>
                {
                    RoomModifierData data;
                    if (definition.Collection != "Control") data = Original(definition);
                    else
                    {
                        data = new RoomModifierData(); data.SetRoomStateModifierClassName(definition.NativeType);
                        Set(data, "paramInt", definition.Coefficient); Set(data, "paramInt2", definition.Divisor);
                    }
                    var effect = (RoomStateModifierBase)Activator.CreateInstance(AccessTools.TypeByName(definition.NativeType));
                    effect.Initialize(data, save); return effect;
                }).ToArray();
                foreach (var input in RoomHealthMechanismCases.Generate(definitions.Length))
                {
                    var effect = effects[input.DefinitionIndex]; var damage = (IRoomStateDamageModifier)effect;
                    AccessTools.Property(stateType, "MaxHp").SetValue(state, input.MaximumHealth);
                    AccessTools.Property(stateType, "Hp").SetValue(state, input.Health);
                    Set(actor, "teamType", input.Team == "Player" ? Team.Type.Monsters : input.Team == "Enemy" ? Team.Type.Heroes : Team.Type.Monsters | Team.Type.Heroes);
                    Set(state, "spawnPoint", input.Placement == 0 ? null : input.Placement == 1 ? roomPoint : orphanPoint);
                    actor.GetRoomStateModifiers().Clear(); if (input.Attached) actor.GetRoomStateModifiers().Add(effect);
                    if (actor.GetMaxHP() != input.MaximumHealth || actor.GetHP() != input.Health ||
                        (actor.GetSpawnPoint() != null) != (input.Placement != 0) || (actor.GetCurrentRoom() != null) != (input.Placement == 1))
                        throw new InvalidOperationException($"Generated native input differs: definition={input.DefinitionIndex}, requested hp={input.Health}/{input.MaximumHealth}, actual={actor.GetHP()}/{actor.GetMaxHP()}, placement={input.Placement}, point={actor.GetSpawnPoint() != null}, room={actor.GetCurrentRoom() != null}.");
                    records.Add(new { Input = input, Dynamic = effect.GetDynamicInt(actor),
                        Attack = damage.GetModifiedAttackDamage((Damage.Type)input.DamageType, actor, input.RequestStats, managers.GetCoreManagers()),
                        MagicPower = damage.GetModifiedMagicPowerDamage(managers.GetCoreManagers()) });
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(obj); }
            timer.Stop(); var rngAfter = RngStates();
            var decisionAfter = JToken.FromObject(trace.CaptureDecision()); var originalsAfter = Originals();
            bool unchanged = JToken.DeepEquals(decisionBefore, decisionAfter);
            bool definitionsUnchanged = JToken.DeepEquals(originalsBefore, originalsAfter);
            if (!unchanged || !definitionsUnchanged || frame != Time.frameCount || !JToken.DeepEquals(rngBefore, rngAfter) || queue.Count != 0 || combat.IsRunningTriggerQueue)
                throw new InvalidOperationException("Generated query batch changed source definitions, native decision, RNG, frame or queue.");
            using (var document = NativeFixtureCapture.Capture(new {
                Schema = 1, Scope = "DefinitionDrivenRoomHealthQueries", GameVersion = Application.version,
                GameModuleMvid = typeof(CardState).Assembly.ManifestModule.ModuleVersionId,
                Definitions = definitions, OriginalDefinitions = originalsBefore, OriginalDefinitionsAfter = originalsAfter,
                DefinitionsUnchanged = definitionsUnchanged, Before = decisionBefore, After = decisionAfter,
                DecisionUnchanged = unchanged, FrameBefore = frame, FrameAfter = Time.frameCount,
                RngBefore = rngBefore, RngAfter = rngAfter, QueueBefore = 0, QueueAfter = queue.Count,
                Seconds = timer.Elapsed.TotalSeconds, Samples = records }))
            using (var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "room-health-mechanism-batch.mt2f")))
                document.Write(output);
        }
        private static void Set(object target, string name, object? value) => AccessTools.Field(target.GetType(), name).SetValue(target, value);
        private static JObject RngStates() => JObject.FromObject(Enum.GetValues(typeof(RngId)).Cast<RngId>()
            .Where(id => id != RngId.NonDeterministic && id != RngId.Chatter)
            .ToDictionary(id => id.ToString(), id => RngCalibration.Words(RandomManager.GetState(id))));
    }
}
#endif
