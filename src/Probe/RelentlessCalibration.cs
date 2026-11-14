using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class RelentlessCalibration
    {
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_RELENTLESS") == "1";
        internal static bool Started { get; private set; }
        internal static bool Completed { get; private set; }
        internal static string? Error { get; private set; }
        internal static bool Recording { get; private set; }
        private static AllGameManagers Managers => AllGameManagers.Instance!;
        private static FullBattleTrace Trace => FullBattleTrace.Active!;
        private static readonly Type StateType = AccessTools.Inner(typeof(CharacterState), "StateInformation");
        private static readonly List<string> phases = new List<string>();
        private static readonly List<bool> actualChoices = new List<bool>();
        private static readonly Queue<bool> choices = new Queue<bool>();
        internal static bool Eligible => DevotionCalibration.Eligible;
        private static object Get(object owner, string field) => AccessTools.Field(owner.GetType(), field).GetValue(owner);
        private static void Set(object owner, string field, object value) => AccessTools.Field(owner.GetType(), field).SetValue(owner, value);
        private static object Copy(object state) => AccessTools.Method(StateType, "DeepCopyForPreview").Invoke(state, null);
        internal static void Start(ManualLogSource log)
        { Started = true; Managers.GetSaveManager().StartCoroutine(Protect(Run(log), log)); }
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
                    if (!next) { (stack.Pop() as IDisposable)?.Dispose(); continue; }
                    if (step.Current is IEnumerator child) stack.Push(child); else yield return step.Current;
                }
            }
            finally { while (stack.Count > 0) (stack.Pop() as IDisposable)?.Dispose(); Recording = false; }
            Completed = Error == null;
        }
        private static IEnumerator Run(ManualLogSource log)
        {
            var save = Managers.GetSaveManager(); var combat = Managers.GetCombatManager(); var rooms = Managers.GetRoomManager();
            var actors = new List<CharacterState>();
            for (int floor = 0; floor < rooms.GetNumRooms(); floor++) rooms.GetRoom(floor).AddCharactersToList(actors, Team.Type.Heroes | Team.Type.Monsters);
            CharacterState player = actors.First(actor => actor.GetTeamType() == Team.Type.Monsters && !actor.IsPyreHeart() &&
                actors.Any(enemy => enemy.GetTeamType() == Team.Type.Heroes && enemy.GetCurrentRoomIndex() == actor.GetCurrentRoomIndex()));
            RoomState room = player.GetCurrentRoom();
            CharacterState[] local = actors.Where(actor => actor.GetCurrentRoomIndex() == room.GetRoomIndex()).ToArray();
            if (local.Length < 2 || local.Any(actor => actor.IsMiniboss() || actor.IsOuterTrainBoss()))
                throw new InvalidOperationException("Relentless component needs ordinary living room actors: " +
                    string.Join(",", local.Select(actor => actor.GetSourceCharacterData().name)));
            object[] primary = actors.Select(actor => Get(actor, "_primaryStateInformation")).ToArray();
            object[] originalTriggers = actors.Select(actor => Get(actor, "triggers")).ToArray();
            object[] immunities = actors.Select(actor => Get(actor, "statusEffectImmunities")).ToArray();
            var beforeTrain = Trace.CaptureTrain(); object rngOriginal = HalfHealthLifecycleCalibration.Rng();
            var streams = Enum.GetValues(typeof(RngId)).Cast<RngId>().ToDictionary(id => id,
                id => new HadesRNG((HadesRNG)AccessTools.Method(typeof(RandomManager), "GetRng").Invoke(null, new object[] { id })));
            var unity = UnityEngine.Random.state;
            var statistics = Managers.GetCardStatistics();
            var statFields = typeof(CardStatistics).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            object?[] statValues = statFields.Select(field => field.GetValue(statistics)).ToArray();
            object balance = combat.GetBalanceData(); int originalInterval = combat.GetBalanceData().GetRelentlessIterationsUntilNotification();
            int suppression = (int)Get(combat, "combatPreviewSuppressionCount"); bool changed = (bool)Get(combat, "combatStateChanged");
            object data = AccessTools.Property(typeof(SaveManager), "ActiveSaveData").GetValue(save);
            object runStats = Get(data, "runStats"); int gold = save.GetGold();
            object previewData = Get(save, "previewSaveData"); int previewSession = save.PreviewSessionId;
            var samples = new List<object>(); int mismatches = 0;
            var cases = new[] {
                ("preview-native-interval", true, originalInterval, 0, false),
                ("abandon-native-interval", false, originalInterval, 0, false),
                ("preview-three", true, 3, 0, false),
                ("abandon-three", false, 3, 0, false),
                ("continue-then-abandon", false, 3, 1, false),
                ("continue-twice-then-abandon", false, 3, 2, false),
                ("preview-zero-damage-cycle", true, 3, 0, true),
                ("abandon-zero-damage-cycle", false, 3, 0, true)
            };
            try
            {
                Set(combat, "combatPreviewSuppressionCount", suppression + 1);
                foreach (var input in cases)
                {
                    Set(balance, "relentlessIterationsUntilNotification", input.Item3);
                    Set(data, "runStats", Get(((SaveData)data).CreatePreviewCopy(), "runStats")); save.SetGold(gold);
                    for (int i = 0; i < actors.Count; i++)
                    {
                        Set(actors[i], "_primaryStateInformation", Copy(primary[i]));
                        Set(actors[i], "triggers", new List<CharacterTriggerState>());
                        Set(actors[i], "statusEffectImmunities", new List<string>((IEnumerable<string>)immunities[i]));
                    }
                    for (int i = 0; i < statFields.Length; i++)
                    {
                        object? value = statValues[i];
                        if (value is Dictionary<CardState, CardStatsEntry> deck) value = HalfHealthLifecycleCalibration.ShadowDeck(deck);
                        else if (value is IDictionary map)
                        { var copy = (IDictionary)Activator.CreateInstance(value.GetType()); foreach (DictionaryEntry pair in map) copy.Add(pair.Key, pair.Value); value = copy; }
                        else if (value is IList list) value = HalfHealthLifecycleCalibration.CopyList(list);
                        statFields[i].SetValue(statistics, value);
                    }
                    foreach (CharacterState actor in local)
                    {
                        object state = Get(actor, "_primaryStateInformation");
                        foreach (var status in ((Dictionary<string, CharacterState.StatusEffectStack>)Get(state, "statusEffects")).Values) status.Count = 0;
                        actor.SetHealth(4000, 5000);
                        AccessTools.Property(StateType, "AttackDamage").SetValue(state, actor.GetTeamType() == Team.Type.Monsters || input.Item5 ? 0 : 1);
                        AccessTools.Property(StateType, "DamageBuff").SetValue(state, 0);
                        AccessTools.Property(StateType, "DamageBuffFromStatusEffects").SetValue(state, 0);
                    }
                    local.First(actor => actor.GetTeamType() == Team.Type.Heroes).AddStatusEffect("relentless", 1);
                    player.AddStatusEffect("valor", 3);
                    if (input.Item2) Temporary(true);
                    phases.Clear(); choices.Clear(); actualChoices.Clear();
                    var decisions = new List<RelentlessDecision>();
                    if (!input.Item2)
                        for (int i = 0; i <= input.Item4; i++)
                        { bool resume = i < input.Item4; choices.Enqueue(resume); decisions.Add(new RelentlessDecision(room.GetRoomIndex(), input.Item3 * (i + 1), resume)); }
                    BattleTurnState? decisionBefore = input.Item2 ? null : Trace.CaptureDecision();
                    RoomCombatState before = Trace.Capture(room, observe: false); object rngBefore = HalfHealthLifecycleCalibration.Rng();
                    RoomCombatResult predicted = RoomCombatModel.Resolve(before, relentlessDecisions: decisions);
                    Recording = true;
                    yield return combat.DoRoomCombat(Managers.GetMonsterManager(), Managers.GetHeroManager(), room);
                    Recording = false;
                    RoomCombatState actual = Trace.Capture(room, observe: false); object rngAfter = HalfHealthLifecycleCalibration.Rng();
                    string? difference = predicted.Supported ? HalfHealthLifecycleCalibration.Difference(Comparable(predicted.State!), Comparable(actual)) : predicted.UnsupportedReason;
                    difference ??= choices.Count != 0 || !actualChoices.SequenceEqual(decisions.Select(choice => choice.Continue)) ? "Native dialog choices differ." : null;
                    int rounds = phases.Count(phase => phase == "Exchange");
                    difference ??= rounds != input.Item3 * (input.Item4 + 1) || predicted.Rounds != rounds ? "Native exchange count differs." : null;
                    difference ??= phases.Count(phase => phase == "PostCombat") != (input.Item2 ? 0 : 1) || predicted.PreviewTruncated != input.Item2 ? "Native completion boundary differs." : null;
                    if (difference != null) mismatches++;
                    samples.Add(new { Label = input.Item1, Preview = input.Item2, Interval = input.Item3, ZeroDamage = input.Item5,
                        Decisions = decisions, DecisionBefore = decisionBefore, Before = before, Actual = actual, Rounds = rounds,
                        Phases = phases.ToArray(), ActualChoices = actualChoices.ToArray(),
                        RngBefore = rngBefore, RngAfter = rngAfter, Difference = difference, Completed = true });
                    log.LogInfo("RELENTLESS " + input.Item1 + " " + (difference ?? "MATCH"));
                    if (input.Item2) Temporary(false);
                }
            }
            finally
            {
                Recording = false;
                if (save.PreviewMode) Temporary(false);
                Set(balance, "relentlessIterationsUntilNotification", originalInterval);
                for (int i = 0; i < actors.Count; i++)
                { Set(actors[i], "_primaryStateInformation", primary[i]); Set(actors[i], "triggers", originalTriggers[i]); Set(actors[i], "statusEffectImmunities", immunities[i]); }
                for (int i = 0; i < statFields.Length; i++) statFields[i].SetValue(statistics, statValues[i]);
                Set(data, "runStats", runStats); save.SetGold(gold); Set(save, "previewSaveData", previewData);
                AccessTools.Property(typeof(SaveManager), "PreviewSessionId").SetValue(save, previewSession);
                Set(combat, "combatPreviewSuppressionCount", suppression); Set(combat, "combatStateChanged", changed);
                foreach (var pair in streams) ((HadesRNG)AccessTools.Method(typeof(RandomManager), "GetRng").Invoke(null, new object[] { pair.Key })).Init(pair.Value);
                UnityEngine.Random.state = unity;
            }
            string? restoration = HalfHealthLifecycleCalibration.Difference(Comparable(beforeTrain), Comparable(Trace.CaptureTrain())) ??
                HalfHealthLifecycleCalibration.Difference(rngOriginal, HalfHealthLifecycleCalibration.Rng());
            using (var document = NativeFixtureCapture.Capture(new { Schema = 1, GameVersion = UnityEngine.Application.version,
                GameModuleMvid = typeof(CharacterState).Assembly.ManifestModule.ModuleVersionId, Boundary = "NativeRelentlessNotification",
                NativeInterval = originalInterval, Samples = samples, Mismatches = mismatches, RestorationDifference = restoration,
                LiveContextUnchanged = restoration == null, RngBefore = rngOriginal, RngAfter = HalfHealthLifecycleCalibration.Rng() }))
            using (var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "relentless.mt2f"))) document.Write(output);
            if (mismatches != 0 || restoration != null) throw new InvalidOperationException("Relentless differs: " + mismatches + "; restoration: " + restoration);

            void Temporary(bool enabled) => AccessTools.Method(typeof(CombatManager), "SetTemporaryStateEnabled")
                .Invoke(combat, new object[] { enabled, actors, false });
        }
        internal static object Comparable(RoomCombatState room) => new { room.RoomIndex, room.Deployment, room.Units, room.ExternalInteractions,
            room.Preview, room.ActiveCompanionBossIds, Context = BattleTurnProbe.ComparableContext(room.Context) };
        private static object Comparable(TrainCombatState train) => new { train.Rooms, train.Movement, train.EnemySlotsPerRoom,
            Context = BattleTurnProbe.ComparableContext(train.Context) };
        [HarmonyPatch(typeof(ScreenManager), "WaitForDialog")]
        private static class DialogPatch
        {
            private static bool Prepare() => Enabled;
            private static bool Prefix(Dialog.Data data, ref IEnumerator __result)
            {
                if (!Recording) return true;
                if (choices.Count == 0 || data.content != "Combat_Relentless_Description".Localize())
                    throw new InvalidOperationException("Unexpected controlled Relentless dialog.");
                bool resume = choices.Dequeue(); actualChoices.Add(resume);
                if (resume) data.callback2.Invoke(); else data.callback1.Invoke();
                __result = Empty(); return false;
            }
            private static IEnumerator Empty() { yield break; }
        }
        [HarmonyPatch(typeof(CombatManager), "DoUnitCombat")]
        private static class ExchangePatch
        { private static bool Prepare() => Enabled; private static void Prefix() { if (Recording) phases.Add("Exchange"); } }
        [HarmonyPatch(typeof(CombatManager), "DoUnitPostCombat")]
        private static class PostCombatPatch
        { private static bool Prepare() => Enabled; private static void Prefix() { if (Recording) phases.Add("PostCombat"); } }
    }
}
