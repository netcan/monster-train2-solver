using System;
using System.Collections;
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
    // Calibrates the native lifecycle up to its status API boundary. Those calls are recorded
    // and suppressed in this isolated calibration; it does not claim whole-battle aura support.
    internal static class EnchantmentCalibration
    {
        private sealed class Sample
        {
            public string Label { get; set; } = "";
            public EnchantmentState Before { get; set; } = null!;
            public EnchantmentInput Input { get; set; } = null!;
            public EnchantmentTransition? After { get; set; }
            public UnityRng TestBefore { get; set; }
            public UnityRng TestAfter { get; set; }
            public bool Completed { get; set; }
            public string? Difference { get; set; }
            public List<EnchantmentRequest> Requests { get; set; } = new List<EnchantmentRequest>();
        }
        private static Sample? active;
        private static CardEffectEnchant? effect;
        private static CardEffectState? effectState;
        private static CharacterState[] actors = Array.Empty<CharacterState>();
        private static readonly Type StateType = AccessTools.Inner(typeof(CharacterState), "StateInformation");
        private static readonly Type EntryType = AccessTools.Inner(typeof(CardEffectEnchant), "EnchantedState");
        private static AllGameManagers Managers => AllGameManagers.Instance!;
        private static SaveManager Save => Managers.GetSaveManager();
        private static HadesRNG Rng(RngId id) => (HadesRNG)AccessTools.Method(typeof(RandomManager), "GetRng").Invoke(null, new object[] { id });
        private static UnityRng SnapshotRng(RngId id)
        { uint[] words = RngCalibration.Words(Rng(id).GetState()); return new UnityRng(words[0], words[1], words[2], words[3]); }
        private static void Set(object target, string name, object? value) => AccessTools.Field(target.GetType(), name).SetValue(target, value);
        private static int Id(CharacterState unit) => Array.IndexOf(actors, unit) + 1;
        private static object State(CharacterState actor) => AccessTools.Property(typeof(CharacterState), "PrimaryStateInformation").GetValue(actor);
        private static IDictionary Map(string name) => (IDictionary)AccessTools.Field(typeof(CardEffectEnchant), name).GetValue(effect);
        private static EnchantmentTarget[] SnapshotMap(string name)
        {
            var result = new List<EnchantmentTarget>();
            foreach (DictionaryEntry entry in Map(name)) result.Add(new EnchantmentTarget(Id((CharacterState)entry.Key),
                (bool)AccessTools.Field(EntryType, "isEnchanted").GetValue(entry.Value),
                Convert.ToInt32(AccessTools.Field(EntryType, "nextStateAction").GetValue(entry.Value))));
            return result.ToArray();
        }
        private static EnchantmentStatus Status(StatusEffectStackData item) => new EnchantmentStatus(item.statusId, item.count,
            Managers.GetStatusEffectManager().GetStatusEffectDataById(item.statusId)?.GetDisplayCategory().ToString());
        private static EnchantmentState Snapshot()
        {
            var status = (StatusEffectStackData?)AccessTools.Field(typeof(CardEffectEnchant), "statusEffect").GetValue(effect);
            return new EnchantmentState(SnapshotMap("_primaryEnchantedTargets"), SnapshotMap("_previewEnchantedTargets"),
                (bool)AccessTools.Field(typeof(CardEffectEnchant), "_previewEnchantedTargetsRequireSync").GetValue(effect), status == null ? null : Status(status));
        }
        private static EnchantmentActor SnapshotActor(CharacterState actor)
        {
            using (new CharacterState.SetAllowDestroyedAccessHelper(actor, onlyIfDestroyed: true))
                return new EnchantmentActor(Id(actor), actor.GetCurrentRoomIndex(), actor.GetTeamType() == Team.Type.Heroes,
                    actor.IsAlive, actor.IsDestroyed, actor.IsDormant(), actor.GetStatusEffectStacks("muted") > 0,
                    actor.GetStatusEffectStacks("silenced") > 0, actor.GetStatusEffectStacks("duality") > 0,
                    actor.GetStatusEffectStacks("undying") > 0, actor.PreviewMode);
        }
        private static EnchantmentInput Input(string operation, IReadOnlyList<int>? collected = null)
        {
            var bound = (CharacterState?)AccessTools.Field(typeof(CardEffectEnchant), "enchanterCharacter").GetValue(effect);
            bool can = bound != null && AccessTools.Field(typeof(CardEffectEnchant), "cachedState").GetValue(effect) != null &&
                AccessTools.Field(typeof(CardEffectEnchant), "cachedCoreGameManagers").GetValue(effect) != null;
            return new EnchantmentInput(operation, bound == null ? 0 : Id(bound), can, Save.PreviewMode,
                active?.Input.BattleRng ?? SnapshotRng(RngId.Battle), active?.Input.TestRng ?? SnapshotRng(RngId.BattleTest),
                effectState!.GetParamStatusEffectStackData().Select(Status).ToArray(),
                collected ?? Array.Empty<int>(), actors.Select(SnapshotActor).ToArray());
        }
        private static void Seed(string name, params EnchantmentTarget[] targets)
        {
            IDictionary map = Map(name); map.Clear();
            foreach (EnchantmentTarget target in targets)
            {
                object entry = Activator.CreateInstance(EntryType, true);
                Set(entry, "isEnchanted", target.IsEnchanted);
                var action = AccessTools.Field(EntryType, "nextStateAction"); action.SetValue(entry, Enum.ToObject(action.FieldType, target.NextAction));
                map.Add(actors[target.UnitId - 1], entry);
            }
        }
        private static void RawStatus(CharacterState unit, string status, int count)
        {
            var map = (Dictionary<string, CharacterState.StatusEffectStack>)AccessTools.Field(StateType, "statusEffects").GetValue(State(unit));
            // Only the native read gates are needed. No native status mutation/callback runs here.
            map[status] = new CharacterState.StatusEffectStack(null!, count);
        }
        private static void ResetActor(CharacterState actor, string kind, bool preview)
        {
            Set(actor, "destroyedState", CharacterState.DestroyedState.None); actor.PreviewMode = preview;
            foreach (string name in new[] { "_primaryStateInformation", "_previewStateInformation" })
            {
                object state = Activator.CreateInstance(StateType, true);
                AccessTools.Property(StateType, "MaxHp").SetValue(state, 30); AccessTools.Property(StateType, "Hp").SetValue(state, kind.StartsWith("dead", StringComparison.Ordinal) ? 0 : 17);
                Set(state, "despawned", kind == "despawned");
                var pyreActors = new List<CharacterState>(); Managers.GetRoomManager()!.GetRoom(3).AddCharactersToList(pyreActors, Team.Type.Monsters);
                Set(state, "spawnPoint", kind == "unplaced" ? null : kind == "other-room" ? Managers.GetRoomManager()!.GetFirstEmptyMonsterPoint(1) :
                    pyreActors.Single(unit => unit.IsPyreHeart()).GetSpawnPoint());
                Set(actor, name, state);
            }
            if (kind == "destroyed") Set(actor, "destroyedState", CharacterState.DestroyedState.InRemoveList);
            if (kind == "preview-mismatch") actor.PreviewMode = !preview;
            if (kind == "hero") Set(actor, "teamType", Team.Type.Heroes);
            else Set(actor, "teamType", Team.Type.Monsters);
            foreach (string status in new[] { "muted", "silenced", "duality", "undying" })
                if (kind == status || kind == "dead-undying" && status == "undying") RawStatus(actor, status, 1);
            if (kind == "dormant" || kind == "dormant-spark") RawStatus(actor, "dormant", 1);
            if (kind == "dormant-spark") RawStatus(actor, "spark", 1);
        }
        private static void Reset(bool preview = false, string source = "alive", string target = "alive")
        {
            Save.PreviewMode = preview;
            for (int i = 0; i < actors.Length; i++) ResetActor(actors[i], i == 0 ? source : i == 1 ? target : "alive", preview);
            Set(Managers.GetMonsterManager()!, "lastSpawnedCharacterThisTurn", null);
        }
        private static void NewEffect(params StatusEffectStackData[] pool)
        {
            var data = new CardEffectData("CardEffectEnchant", null!, Team.Type.Monsters | Team.Type.Heroes);
            data.Cheat_SetTargetMode(TargetMode.LastSpawnedCharacter);
            Set(data, "paramStatusEffects", pool);
            // Enchant's selector receives no actor/targets, so these configured fields are ignored.
            Set(data, "useStatusEffectStackMultiplier", true); Set(data, "statusEffectStackMultiplier", "duality");
            Set(data, "useHealthMissingStackMultiplier", true); Set(data, "useMagicPowerMultiplier", true);
            Set(data, "paramInt", 1); Set(data, "useIntRange", true); Set(data, "paramMinInt", -100); Set(data, "paramMaxInt", 100);
            Set(data, "paramBool", true);
            Set(data, "paramBool2", true);
            effectState = new CardEffectState(); effectState.Setup(data, Team.Type.Heroes, Save);
            effect = (CardEffectEnchant)effectState.GetCardEffect();
            using (new CharacterState.SetAllowDestroyedAccessHelper(actors[0], onlyIfDestroyed: true))
                effect.SetEnchanterCharacter(actors[0], Managers.GetCoreManagers());
        }
        private static StatusEffectStackData Stack(string id, int count) => new StatusEffectStackData { statusId = id, count = count };

        internal static void Capture(FullBattleTrace trace)
        {
            CombatContext originalContext = trace.CaptureContext(); TrainCombatState originalTrain = trace.CaptureTrain();
            HadesRNG battle = Rng(RngId.Battle), test = Rng(RngId.BattleTest);
            var savedBattle = new HadesRNG(battle); var savedTest = new HadesRNG(test);
            UnityEngine.Random.State savedGlobal = UnityEngine.Random.state;
            object originalLast = AccessTools.Field(typeof(MonsterManager), "lastSpawnedCharacterThisTurn").GetValue(Managers.GetMonsterManager());
            object originalPreview = AccessTools.Field(typeof(SaveManager), "previewSaveData").GetValue(Save);
            var objects = new List<GameObject>(); var samples = new List<Sample>(); int mismatches = 0;
            try
            {
                actors = Enumerable.Range(1, 4).Select(id =>
                {
                    var obj = new GameObject("Poju enchant calibration " + id); obj.SetActive(false); objects.Add(obj);
                    CharacterState actor = obj.AddComponent<CharacterState>(); Set(actor, "combatManager", Managers.GetCombatManager());
                    Set(actor, "allGameManagers", Managers); return actor;
                }).ToArray();
                Reset(); NewEffect(Stack("armor", 2));
                Run("initial");
                foreach (int id in new[] { 2, 3, 4, 2, 1 })
                { Last(id); Run("collect-" + id); }
                Last(0); Run("empty-collection-retains-targets");
                foreach (string kind in new[] { "silenced", "alive", "muted", "alive", "dormant", "dormant-spark", "other-room", "alive", "dead", "alive", "despawned", "alive", "destroyed", "alive" })
                { ResetActor(actors[0], kind, false); Run("source-" + kind); }
                foreach (string kind in new[] { "dead", "dead-undying", "destroyed", "other-room", "alive", "preview-mismatch", "alive" })
                { ResetActor(actors[1], kind, false); Run("target-" + kind); }
                Run("prepare-primary", "PrepareForPreview"); Reset(true);
                Run("preview-sync"); ResetActor(actors[1], "other-room", true); Run("preview-remove");
                Reset(false); Run("primary-unaffected"); Run("prepare-again", "PrepareForPreview"); Reset(true); Run("preview-resync");
                Seed("_previewEnchantedTargets", new EnchantmentTarget(4, false, 2), new EnchantmentTarget(2, false, 2));
                Seed("_primaryEnchantedTargets", new EnchantmentTarget(2, true), new EnchantmentTarget(3, true));
                Run("prepare-stale-preview", "PrepareForPreview"); Run("sync-retains-preview-only-keys");
                Run("prepare-before-setup", "PrepareForPreview"); Run("setup-preserves-sync-and-cache", "Setup");

                string[] sources = { "alive", "muted", "silenced", "dormant", "dormant-spark", "duality", "dead", "despawned", "destroyed", "other-room", "unplaced", "preview-mismatch" };
                string[] targets = { "alive", "hero", "dead", "dead-undying", "destroyed", "other-room", "unplaced", "preview-mismatch" };
                foreach (bool preview in new[] { false, true })
                foreach (bool applied in new[] { false, true })
                foreach (int pending in new[] { 0, 1, 2 })
                foreach (string source in sources)
                foreach (string target in targets)
                {
                    Reset(preview, source, target); NewEffect(Stack("armor", 2));
                    Seed(preview ? "_previewEnchantedTargets" : "_primaryEnchantedTargets", new EnchantmentTarget(2, applied, pending));
                    Run($"gates:{preview}:{applied}:{pending}:{source}:{target}");
                }
                foreach (bool preview in new[] { false, true })
                foreach (string id in new[] { "armor", "poison", "horde", "cardless" })
                foreach (int count in new[] { int.MinValue, -2, 0, 1, 3, int.MaxValue })
                foreach (bool duality in new[] { false, true })
                foreach (bool applied in new[] { false, true })
                {
                    Reset(preview, duality ? "duality" : "alive", applied ? "other-room" : "hero"); NewEffect(Stack(id, count));
                    Seed(preview ? "_previewEnchantedTargets" : "_primaryEnchantedTargets", new EnchantmentTarget(2, applied));
                    Run($"counts:{preview}:{id}:{count}:{duality}:{applied}");
                }
                Reset(); NewEffect(Stack("armor", 2)); Last(2); Run("duality-changes-add");
                RawStatus(actors[0], "duality", 1); ResetActor(actors[1], "other-room", false); Last(0); Run("duality-changes-remove");
                RawStatus(actors[0], "duality", 0); ResetActor(actors[1], "alive", false); Run("duality-changes-readd");
                Reset(); NewEffect(Stack("armor", 2), Stack("poison", 3), Stack("cardless", 0)); Last(2);
                for (int i = 0; i < 64; i++)
                {
                    bool preview = (i & 8) != 0; Save.PreviewMode = preview;
                    foreach (CharacterState actor in actors) actor.PreviewMode = preview;
                    RawStatus(actors[0], "muted", i % 3 == 0 ? 1 : 0); RawStatus(actors[0], "duality", (i & 1));
                    if (i % 8 == 0) Run("random-prepare-" + i, "PrepareForPreview");
                    Run("random-update-" + i);
                }
                Reset(); NewEffect(Stack("armor", 2));
                foreach (string field in new[] { "cachedCoreGameManagers", "cachedState", "enchanterCharacter" })
                {
                    object original = AccessTools.Field(typeof(CardEffectEnchant), field).GetValue(effect);
                    Set(effect!, field, null); Run("unbound-" + field); Set(effect!, field, original);
                }
            }
            finally
            {
                active = null; Save.PreviewMode = false; Set(Save, "previewSaveData", originalPreview);
                Set(Managers.GetMonsterManager()!, "lastSpawnedCharacterThisTurn", originalLast);
                battle.Init(savedBattle); test.Init(savedTest); UnityEngine.Random.state = savedGlobal;
                foreach (GameObject obj in objects) UnityEngine.Object.Destroy(obj);
                actors = Array.Empty<CharacterState>(); effect = null; effectState = null;
            }
            bool restored = Equal(originalContext, trace.CaptureContext()) && Equal(originalTrain, trace.CaptureTrain()) &&
                SnapshotRng(RngId.Battle).Equals(ToModel(savedBattle)) && SnapshotRng(RngId.BattleTest).Equals(ToModel(savedTest)) &&
                Equals(UnityEngine.Random.state, savedGlobal) && Save.PreviewMode == false &&
                ReferenceEquals(AccessTools.Field(typeof(SaveManager), "previewSaveData").GetValue(Save), originalPreview) &&
                ReferenceEquals(AccessTools.Field(typeof(MonsterManager), "lastSpawnedCharacterThisTurn").GetValue(Managers.GetMonsterManager()), originalLast);
            string path = Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "enchantment-lifecycle-calibration.mt2f");
            using (var archive = NativeFixtureCapture.Capture(new { Schema = 1, GameVersion = Application.version,
                GameModuleMvid = typeof(CharacterState).Assembly.ManifestModule.ModuleVersionId, Boundary = "StatusApiRequests",
                StatusMutationsSuppressed = true, IgnoredScalingAndChanceFieldsConfigured = true, LiveContextUnchanged = restored,
                Mismatches = mismatches, Samples = samples }))
            using (var stream = File.Create(path)) archive.Write(stream);
            if (!restored || mismatches != 0) throw new InvalidOperationException("Native enchant lifecycle calibration differs or changed live state: " + mismatches);

            void Last(int id) => Set(Managers.GetMonsterManager()!, "lastSpawnedCharacterThisTurn", id == 0 ? null : actors[id - 1]);
            void Run(string label, string operation = "Update")
            {
                var sample = new Sample { Label = label, Before = Snapshot(), Input = Input(operation), TestBefore = SnapshotRng(RngId.BattleTest) };
                active = sample; samples.Add(sample);
                try
                {
                    if (operation == "Setup") effect!.Setup(effectState!);
                    else if (operation == "PrepareForPreview") effect!.PrepareForPreview();
                    else effect!.OnUpdateEnchantments();
                    sample.After = new EnchantmentTransition(Snapshot(), SnapshotRng(RngId.Battle), SnapshotRng(RngId.BattleTest), sample.Requests);
                    sample.TestAfter = SnapshotRng(RngId.BattleTest); sample.Completed = true;
                    EnchantmentTransition predicted = EnchantmentLifecycleModel.Apply(sample.Before, sample.Input);
                    sample.Difference = Equal(predicted, sample.After) ? null : "Native enchant lifecycle or API request differs";
                    if (sample.Difference != null) mismatches++;
                }
                finally { active = null; }
            }
        }
        private static UnityRng ToModel(HadesRNG rng)
        { uint[] words = RngCalibration.Words(rng.GetState()); return new UnityRng(words[0], words[1], words[2], words[3]); }
        private static bool Equal(object left, object right) => JToken.DeepEquals(JToken.FromObject(left), JToken.FromObject(right));

        [HarmonyPatch(typeof(TargetHelper), nameof(TargetHelper.CollectTargets), new[] { typeof(CardEffectState), typeof(CardEffectParams), typeof(ICoreGameManagers), typeof(bool) })]
        private static class CollectPatch
        {
            private static void Postfix(CardEffectState __0, CardEffectParams __1)
            {
                if (active == null || !ReferenceEquals(__0, effectState)) return;
                int[] collected = __1.targets.Select(Id).ToArray();
                if (collected.Any(id => id == 0)) throw new InvalidOperationException("Calibration collected a live actor outside its isolated set.");
                active.Input = Input(active.Input.Operation, collected);
            }
        }
        [HarmonyPatch(typeof(CharacterState), nameof(CharacterState.DoMovementAttacking))]
        private static class MovementPatch
        { private static bool Prefix(CharacterState __instance) => active == null || !actors.Contains(__instance); }
        [HarmonyPatch(typeof(CharacterState), nameof(CharacterState.AddStatusEffect), new[] { typeof(string), typeof(int), typeof(CharacterState.AddStatusEffectParams), typeof(CharacterState), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool) })]
        private static class AddPatch
        {
            private static bool Prefix(CharacterState __instance, string statusId, int numStacks, CharacterState.AddStatusEffectParams addStatusEffectParams,
                CharacterState triggeredByCharacter, bool allowModification, bool isFromHiddenTrigger, bool allowDualism)
            {
                if (active == null) return true;
                var p = addStatusEffectParams;
                active.Requests.Add(new EnchantmentRequest(Id(__instance), "Add", statusId, numStacks, p.sourceIsHero, null,
                    p.fromEffectType?.Name ?? "", p.sourceCardState != null, p.sourceRelicState != null, triggeredByCharacter != null,
                    allowModification, isFromHiddenTrigger, allowDualism, p.overrideImmunity, p.spawnEffect, p.fromRoomModifier, null, null, Snapshot()));
                return false;
            }
        }
        [HarmonyPatch(typeof(CharacterState), nameof(CharacterState.RemoveStatusEffect), new[] { typeof(string), typeof(int), typeof(CharacterState.RemoveStatusEffectParams), typeof(bool) })]
        private static class RemovePatch
        {
            private static bool Prefix(CharacterState __instance, string statusId, int numStacks, CharacterState.RemoveStatusEffectParams removeParams, bool allowModification)
            {
                if (active == null) return true;
                var p = removeParams;
                active.Requests.Add(new EnchantmentRequest(Id(__instance), "Remove", statusId, numStacks, null, p.showNotification,
                    p.fromEffectType?.Name ?? "", p.sourceCardState != null, p.sourceRelicState != null, false, allowModification,
                    null, null, null, null, null, p.fromCardUpgrade, p.removeAtEndOfTurn, Snapshot())); return false;
            }
        }
    }
}
