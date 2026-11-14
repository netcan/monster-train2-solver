using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    // Exercise the real temporary-copy toggle, including partial copies of the
    // shared Entropy group. Combat mode is an explicit controlled input; this
    // component does not claim a card-hover or full UI battle acceptance.
    internal static class TrueFinalBossMaximumPreviewCalibration
    {
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_TRUE_FINAL_BOSS_MAXIMUM_PREVIEW") == "1";
        internal static bool Completed { get; private set; }
        internal static string? Error { get; private set; }
        private static IReadOnlyList<CharacterState>? armed;
        private static bool started;
        internal static void Arm(IReadOnlyList<CharacterState> actors) => armed = actors.ToArray();
        internal static void Tick()
        {
            if (started || armed == null) return;
            var managers = AllGameManagers.Instance!; var combat = managers.GetCombatManager();
            // The first player-control signal precedes RunMonsterTurn's reset
            // of the native suppression count. Wait for its first completed
            // battle preview before acquiring our own suppression token.
            if (managers.GetSaveManager().PreviewMode || combat.CombatPreviewState != CharacterState.CombatPreviewState.On ||
                (bool)AccessTools.Field(typeof(CombatManager), "combatStateChanged").GetValue(combat) ||
                combat.IsRunningTriggerQueue || !combat.HaveCardsBeenDrawnForCurrentTurn() ||
                managers.GetReplayManager().IsCardPlaying() || managers.GetHandUI().AreAnyCardsAnimating() || !CardAnimationSettlement.Ready()) return;
            started = true; managers.GetSaveManager().StartCoroutine(Protect(Capture(armed)));
        }
        private static IEnumerator Protect(IEnumerator root)
        {
            var native = CompleteNative(root);
            try
            {
                while (true)
                {
                    bool moved;
                    try { moved = native.MoveNext(); }
                    catch (Exception error) { Error = error.ToString(); break; }
                    if (!moved) { Completed = true; break; }
                    yield return native.Current;
                }
            }
            finally { (native as IDisposable)?.Dispose(); }
        }
        // Hover work can allocate unused preview-copy identities. Keep every
        // existing identity and all gameplay fields; normalize only the next
        // unused preview allocator, as production decision keys already do.
        private static JToken Primary(object train)
        {
            var token = JObject.FromObject(train);
            foreach (var property in token.Descendants().OfType<JProperty>().Where(property => property.Name == "NextPreviewCopyId"))
                if (property.Value.Type != JTokenType.Null) property.Value = 0;
            return token;
        }
        private sealed class Input
        {
            internal string Label = "";
            internal int Target, Amount, Floor = 1;
            internal bool Debuff = true, DecreaseHp = true, Heal = true;
        }
        private static IEnumerator CompleteNative(IEnumerator root)
        {
            var stack = new Stack<IEnumerator>(); stack.Push(root); int steps = 0;
            try
            {
                while (stack.Count != 0)
                {
                    if (++steps > 10000) throw new InvalidOperationException("Temporary native API exceeded its bounded coroutine steps.");
                    var current = stack.Peek();
                    if (!current.MoveNext()) { (stack.Pop() as IDisposable)?.Dispose(); continue; }
                    if (current.Current is IEnumerator child) stack.Push(child);
                    else yield return current.Current;
                }
            }
            finally { while (stack.Count != 0) (stack.Pop() as IDisposable)?.Dispose(); }
        }
        internal static IEnumerator Capture(IReadOnlyList<CharacterState> actors)
        {
            var managers = AllGameManagers.Instance!;
            var trace = FullBattleTrace.Active!;
            var combat = managers.GetCombatManager();
            var save = managers.GetSaveManager();
            if (actors.Count != 5 || save.PreviewMode || actors.Any(actor => actor.PreviewMode || actor.TemporaryPreviewsEnabled))
                throw new InvalidOperationException("Temporary maximum-health controls require five original primary bodies.");
            var modeProperty = AccessTools.Property(typeof(CombatManager), "CombatPreviewState");
            var originalMode = combat.CombatPreviewState;
            int originalCounter = combat.TrueFinalBossBattlePreviewHpChange;
            bool originalSuppression = CombatPreviewControl.Save(combat);
            int originalSuppressionCount = (int)AccessTools.Field(typeof(CombatManager), "combatPreviewSuppressionCount").GetValue(combat);
            CombatPreviewControl.Set(combat, true);
            var queue = (Queue<CombatManager.TriggerQueueData>)AccessTools.Property(typeof(CombatManager), "TriggerQueue").GetValue(combat);
            var sessions = new List<object>();
            var events = new List<HealthChangedEvent>(); bool recording = false;
            foreach (var actor in actors) actor.AddHpChangedListener((changed, hp) =>
            { if (recording) events.Add(new HealthChangedEvent(trace.UnitId(changed), hp)); });
            object originalTrain = trace.CaptureCanonicalTrain(), originalRng = HalfHealthLifecycleCalibration.Rng();
            List<CharacterState>? copied = null;
            RoomCombatState Snapshot(CharacterState target)
            {
                var state = trace.Capture(target.GetCurrentRoom());
                var links = new LifeLinkContext(actors.Select(actor => new LifeLinkActor(trace.UnitId(actor),
                    actor.GetLinkedCharacterState() == null ? 0 : trace.UnitId(actor.GetLinkedCharacterState()),
                    actor.GetHP(), actor.PreviewHP(), actor.Initialized, actor.HasStatusEffect("lifelink"), true,
                    actor.IsOuterTrainBoss(), AccessTools.Field(typeof(CharacterState), "_previewStateInformation").GetValue(actor) != null)).ToArray(),
                    combat.CombatPreviewState == CharacterState.CombatPreviewState.Calculating,
                    combat.TrueFinalBossBattlePreviewHpChange, copied!.Select(trace.UnitId).ToArray());
                var world = state.Context!.Enchantments!;
                var context = state.Context.WithLifeLinks(links).WithEnchantments(new EnchantmentWorld(world.Rooms,
                    world.Movement, world.EnemySlotsPerRoom, world.RetainedUnits, world.EnchanterIds,
                    world.AllowUpdates, world.Updating, world.Preview, world.TestRng, world.AutomaticLifecycle,
                    copied.Select(trace.UnitId).ToArray()));
                return new RoomCombatState(state.RoomIndex, state.Deployment, state.Units, state.ExternalInteractions,
                    context, state.Preview, state.ActiveCompanionBossIds);
            }
            void Temporary(bool enabled) => AccessTools.Method(typeof(CombatManager), "SetTemporaryStateEnabled")
                .Invoke(combat, new object[] { enabled, copied!, true });
            try
            {
                foreach (var mode in new[] { CharacterState.CombatPreviewState.Off, CharacterState.CombatPreviewState.On,
                    CharacterState.CombatPreviewState.Calculating })
                foreach (bool all in new[] { false, true })
                for (int targetIndex = 0; targetIndex < 5; targetIndex++)
                {
                    var target = actors[targetIndex];
                    UnityEngine.Debug.Log("LIFE-LINK-MAXIMUM-PREVIEW session=" + sessions.Count + " mode=" + mode + " all=" + all + " target=" + targetIndex);
                    var beforeTrain = trace.CaptureCanonicalTrain(); var rngBefore = HalfHealthLifecycleCalibration.Rng();
                    copied = all ? actors.ToList() : new List<CharacterState> { target };
                    modeProperty.SetValue(combat, mode); combat.TrueFinalBossBattlePreviewHpChange = 0;
                    Temporary(true);
                    var rows = new List<object>();
                    var inputs = new List<Input>();
                    void Add(string label, int amount, bool debuff = true, bool heal = true, bool decrease = true, int floor = 1, int? index = null) =>
                        inputs.Add(new Input { Label = label, Target = index ?? targetIndex, Amount = amount,
                            Debuff = debuff, Heal = heal, DecreaseHp = decrease, Floor = floor });
                    Add("grow-unhealed", 4, false, false); Add("zero", 0); Add("negative", -3);
                    Add("subtract", 3); Add("clip-only", 2, decrease: false, floor: 0);
                    Add("protected-lethal", int.MaxValue, floor: 0); Add("grow-unhealed-one", 9, false, false);
                    Add("hp-only-protection", 2, floor: 0); Add("grow-and-heal", 5, false);
                    Add("clip-wounded", 4, decrease: false); Add("raising-floor", 2, decrease: false, floor: 20);
                    Add("upper-cap", 99999, false, false);
                    if (all)
                    {
                        for (int index = 0; index < 5; index++) Add("reduce-all-to-one", int.MaxValue, decrease: false, index: index);
                        Add("unprotected-grow", 9, false, false); Add("unprotected-clip", 9, decrease: false);
                    }
                    foreach (var input in inputs)
                    {
                        var selected = actors[input.Target]; var before = Snapshot(selected);
                        var operationRng = HalfHealthLifecycleCalibration.Rng();
                        bool protection = StatusEffectLifeLinkState.ProtectedByLifeLink(selected, managers.GetHeroManager(), managers.GetMonsterManager(), combat);
                        bool prediction = StatusEffectLifeLinkState.ProtectedByLifeLink(selected, managers.GetHeroManager(), managers.GetMonsterManager(), combat, true);
                        events.Clear(); recording = true;
                        try
                        {
                            var operation = CompleteNative(input.Debuff ? selected.DebuffMaxHP(input.Amount, input.Floor, input.DecreaseHp) :
                                selected.BuffMaxHP(input.Amount, triggerOnHeal: false, heal: input.Heal));
                            try { while (operation.MoveNext()) yield return operation.Current; }
                            finally { (operation as IDisposable)?.Dispose(); }
                        }
                        finally { recording = false; }
                        if (actors.Any(actor => !actor.IsAlive || actor.IsDestroyed) || queue.Count != 0)
                            throw new InvalidOperationException("Temporary maximum-health control became terminal or queued work: " + input.Label);
                        rows.Add(new { Index = rows.Count, input.Label, TargetId = trace.UnitId(selected), input.Amount, input.Debuff,
                            input.Floor, input.DecreaseHp, input.Heal, Protected = protection, ProtectedForPrediction = prediction,
                            Before = before, Actual = Snapshot(selected),
                            Events = events.ToArray(), RngBefore = operationRng, RngAfter = HalfHealthLifecycleCalibration.Rng() });
                    }
                    int counterAtCancel = combat.TrueFinalBossBattlePreviewHpChange;
                    Temporary(false);
                    var afterTrain = trace.CaptureCanonicalTrain(); var rngAfter = HalfHealthLifecycleCalibration.Rng();
                    string? rawDifference = HalfHealthLifecycleCalibration.Difference(beforeTrain, afterTrain);
                    string? restoration = HalfHealthLifecycleCalibration.Difference(Primary(beforeTrain), Primary(afterTrain)) ??
                        HalfHealthLifecycleCalibration.Difference(HalfHealthLifecycleCalibration.GameplayRng(rngBefore), HalfHealthLifecycleCalibration.GameplayRng(rngAfter));
                    sessions.Add(new { Mode = mode.ToString(), AllBodiesCopied = all, TargetId = trace.UnitId(target),
                        Before = beforeTrain, After = afterTrain, RngBefore = rngBefore, RngAfter = rngAfter, Rows = rows,
                        CounterAtCancel = counterAtCancel, CounterAfterCancel = combat.TrueFinalBossBattlePreviewHpChange,
                        RawPrimaryDifference = rawDifference, RestorationDifference = restoration, NativeCancelRestoredPrimary = restoration == null });
                    if (restoration != null) throw new InvalidOperationException("Native temporary cancellation did not restore primary state: " + restoration);
                    copied = null;
                }
            }
            finally
            {
                recording = false;
                if (copied != null && save.PreviewMode) Temporary(false);
                modeProperty.SetValue(combat, originalMode); combat.TrueFinalBossBattlePreviewHpChange = originalCounter;
                CombatPreviewControl.Set(combat, originalSuppression);
            }
            string? finalDifference = HalfHealthLifecycleCalibration.Difference(Primary(originalTrain), Primary(trace.CaptureCanonicalTrain())) ??
                HalfHealthLifecycleCalibration.Difference(HalfHealthLifecycleCalibration.GameplayRng(originalRng),
                    HalfHealthLifecycleCalibration.GameplayRng(HalfHealthLifecycleCalibration.Rng()));
            int finalSuppressionCount = (int)AccessTools.Field(typeof(CombatManager), "combatPreviewSuppressionCount").GetValue(combat);
            bool controlsRestored = combat.CombatPreviewState == originalMode && combat.TrueFinalBossBattlePreviewHpChange == originalCounter &&
                finalSuppressionCount == originalSuppressionCount && !save.PreviewMode &&
                actors.All(actor => !actor.PreviewMode && !actor.TemporaryPreviewsEnabled);
            using (var document = NativeFixtureCapture.Capture(new { Schema = 1, GameVersion = UnityEngine.Application.version,
                GameModuleMvid = typeof(CharacterState).Assembly.ManifestModule.ModuleVersionId, Boundary = "NativeLifeLinkMaximumTemporaryCopies",
                OriginalActorsAndStartingStats = true, NativeCompleted = true, PresentationRngMayAdvance = true,
                Sessions = sessions, RestorationDifference = finalDifference, ControlsRestored = controlsRestored,
                OriginalMode = originalMode.ToString(), FinalMode = combat.CombatPreviewState.ToString(),
                OriginalCounter = originalCounter, FinalCounter = combat.TrueFinalBossBattlePreviewHpChange,
                OriginalSuppressionCount = originalSuppressionCount, FinalSuppressionCount = finalSuppressionCount }))
            using (var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "true-final-boss-maximum-preview.mt2f")))
                document.Write(output);
            if (finalDifference != null || !controlsRestored) throw new InvalidOperationException("Maximum preview isolation differs: " + finalDifference + "; controls restored=" + controlsRestored);
            yield break;
        }
    }
}
