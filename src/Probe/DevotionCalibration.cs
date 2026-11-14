using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class DevotionCalibration
    {
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_DEVOTION") == "1";
        internal static bool Started { get; private set; }
        internal static bool Completed { get; private set; }
        internal static string? Error { get; private set; }
        private static bool recording;
        private static readonly List<string> notifications = new List<string>();
        private static AllGameManagers Managers => AllGameManagers.Instance!;
        private static FullBattleTrace Trace => FullBattleTrace.Active!;
        private static object Get(object owner, string field) => AccessTools.Field(owner.GetType(), field).GetValue(owner);
        private static void Set(object owner, string field, object value) => AccessTools.Field(owner.GetType(), field).SetValue(owner, value);
        private static Queue<CombatManager.TriggerQueueData> Queue =>
            (Queue<CombatManager.TriggerQueueData>)AccessTools.Property(typeof(CombatManager), "TriggerQueue").GetValue(Managers.GetCombatManager());
        internal static bool Eligible => ZealStatusCalibration.Eligible && !Managers.GetSaveManager().PreviewMode &&
            NativeEffectSettlement.Ready(Managers.GetCombatManager()) && !Managers.GetReplayManager().IsCardPlaying() &&
            !Managers.GetCombatManager().IsRunningTriggerQueue && !(Managers.GetHandUI()?.AreAnyCardsAnimating() ?? false) &&
            !(bool)Get(Managers.GetCombatManager(), "combatStateChanged");
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
            finally { while (stack.Count > 0) (stack.Pop() as IDisposable)?.Dispose(); recording = false; }
            Completed = Error == null;
        }
        internal sealed class Input
        {
            public string Label { get; set; } = "";
            public string Operation { get; set; } = "Award";
            public int Level { get; set; }
            public int Points { get; set; }
            public int Amount { get; set; }
            public int PerLevel { get; set; }
            public bool Preview { get; set; }
            public bool Range { get; set; }
        }
        private static Input[] Cases() => new[] {
            new Input { Label = "award-below-first", Amount = 2 },
            new Input { Label = "award-exact-first", Amount = 3 },
            new Input { Label = "award-first-remainder", Amount = 4 },
            new Input { Label = "award-exact-second", Amount = 9 },
            new Input { Label = "award-multiple-levels", Amount = 67 },
            new Input { Label = "award-existing-level", Level = 2, Points = 8, Amount = 1 },
            new Input { Label = "award-existing-remainder", Level = 2, Points = 8, Amount = 23 },
            new Input { Label = "award-zero", Points = 2 },
            new Input { Label = "award-negative", Points = 2, Amount = -1 },
            new Input { Label = "award-signed-addition", Level = 1, Points = int.MaxValue, Amount = 1 },
            new Input { Label = "award-preview", Level = 1, Points = 5, Amount = 8, Preview = true },
            new Input { Label = "original-venerate", Operation = "Original", Amount = 6 },
            new Input { Label = "original-venerate-later", Operation = "Original", Amount = 6, Level = 1, Points = 5 },
            new Input { Label = "effect-per-level", Operation = "Effect", Level = 2, Points = 4, Amount = 2, PerLevel = 1 },
            new Input { Label = "effect-crosses-level", Operation = "Effect", Level = 1, Points = 5, Amount = 1, PerLevel = 1 },
            new Input { Label = "effect-multiple-levels", Operation = "Effect", Amount = 67 },
            new Input { Label = "effect-negative-base-positive-total", Operation = "Effect", Level = 2, Amount = -1, PerLevel = 2 },
            new Input { Label = "effect-rejected-zero-total", Operation = "Effect", Level = 2, Points = 4, Amount = -2, PerLevel = 1 },
            new Input { Label = "effect-rejected-negative-total", Operation = "Effect", Level = 2, Points = 4, Amount = 1, PerLevel = -1 },
            new Input { Label = "effect-preview", Operation = "Effect", Level = 1, Points = 5, Amount = 4, Preview = true },
            new Input { Label = "effect-range", Operation = "Effect", Points = 2, Range = true }
        };
        private static IEnumerator Run(ManualLogSource log)
        {
            SaveManager save = Managers.GetSaveManager(); AllGameData all = Managers.GetAllGameData();
            CardData originalCard = all.FindCardData("96c9ed37-5e59-4c4f-a914-ae7040d24755")!;
            CardEffectData originalEffect = originalCard.GetEffects().Single();
            var definition = new { Id = originalCard.GetID(), Key = originalCard.name, Cost = originalCard.GetCost(),
                Type = originalEffect.GetEffectStateName(), Int = originalEffect.GetParamInt(), PerLevel = originalEffect.GetAdditionalParamInt(),
                Traits = originalCard.GetTraits().Select(trait => trait.GetTraitStateName()).ToArray() };
            if (definition.Key != "Venerate" || definition.Cost != 3 || definition.Type != "CardEffectGainDevotionPoints" ||
                definition.Int != 6 || definition.PerLevel != 0 || !definition.Traits.SequenceEqual(new[] { "CardTraitExhaustState" }))
                throw new InvalidOperationException("Changed original Venerate definition.");
            var original = Trace.CaptureTrain(); object rngOriginal = HalfHealthLifecycleCalibration.Rng();
            DevotionState originalDevotion = original.Context!.QueryFrame!.Devotion!;
            object data = AccessTools.Property(typeof(SaveManager), "ActiveSaveData").GetValue(save);
            object stats = Get(data, "runStats");
            object shadowStats = Get(((SaveData)data).CreatePreviewCopy(), "runStats");
            object previewSave = Get(save, "previewSaveData"); int previewSession = save.PreviewSessionId;
            var streams = Enum.GetValues(typeof(RngId)).Cast<RngId>().ToDictionary(id => id,
                id => new HadesRNG((HadesRNG)AccessTools.Method(typeof(RandomManager), "GetRng").Invoke(null, new object[] { id })));
            UnityEngine.Random.State unity = UnityEngine.Random.state;
            var samples = new List<object>(); int mismatches = 0;
            try
            {
                Set(data, "runStats", shadowStats);
                foreach (Input input in Cases())
                {
                    SetDevotion(input.Level, input.Points);
                    if (input.Preview) save.PreviewMode = true;
                    var before = Trace.CaptureTrain(); object rngBefore = HalfHealthLifecycleCalibration.Rng();
                    CardActionEffect? action = null; CardEffectState? effect = null;
                    bool nativeTest = true, nativeLeveled = false;
                    if (input.Operation != "Award")
                    {
                        CardEffectData effectData = originalEffect;
                        if (input.Operation != "Original")
                        {
                            effectData = new CardEffectData("CardEffectGainDevotionPoints", null!, Team.Type.None);
                            effectData.Cheat_SetTargetMode(TargetMode.Room);
                            Set(effectData, "paramInt", input.Amount); Set(effectData, "additionalParamInt", input.PerLevel);
                            if (input.Range) { Set(effectData, "useIntRange", true); Set(effectData, "paramMinInt", 1); Set(effectData, "paramMaxInt", 7); }
                        }
                        effect = new CardEffectState(); effect.Setup(effectData, Team.Type.Monsters, save);
                        var interactions = new List<string>(); action = BattleActionProbe.DescribeTriggerEffect(effectData, interactions);
                        if (interactions.Count != 0) throw new InvalidOperationException(string.Join(";", interactions));
                        nativeTest = effect.GetCardEffect().TestEffect(effect, new CardEffectParams { targets = new List<CharacterState>() }, Managers.GetCoreManagers());
                    }
                    TrainCombatState? predicted; bool predictedLevel;
                    if (action == null)
                    {
                        DevotionResult result = DevotionModel.Award(before.Context!, input.Amount, input.Preview);
                        predicted = result.Supported ? CardSpellModel.WithContext(before, result.Context!) : null; predictedLevel = result.LeveledUp;
                        if (!result.Supported) log.LogWarning(input.Label + ": " + result.UnsupportedReason);
                    }
                    else
                    {
                        TrainSpellResult result = CardSpellModel.Apply(before, before.Context!.QueryFrame!.SelectedRoomIndex!.Value, new[] { action }, 0);
                        predicted = result.State; predictedLevel = predicted?.Context?.QueryFrame?.Devotion?.Level > input.Level;
                        if (!result.Supported) log.LogWarning(input.Label + ": " + result.UnsupportedReason);
                    }
                    notifications.Clear(); recording = true;
                    try
                    {
                        if (action == null)
                        {
                            nativeLeveled = (bool)AccessTools.Method(typeof(CharacterState).Assembly.GetType("MonolithHelper"), "AwardDevotionPoints")
                                .Invoke(null, new object[] { save, all, input.Amount });
                            if (nativeLeveled) { yield return Managers.GetCardManager().OnDevotionLevelChanged(); yield return Managers.GetMonsterManager().OnDevotionLevelChanged(); }
                        }
                        else if (nativeTest && !input.Preview)
                            yield return effect!.GetCardEffect().ApplyEffect(effect, new CardEffectParams { targets = new List<CharacterState>(),
                                selectedRoom = before.Context!.QueryFrame!.SelectedRoomIndex!.Value }, Managers.GetCoreManagers(), Managers.GetSystemManagers());
                    }
                    finally { recording = false; }
                    int level = DevotionProbe.Read(save, "GetDevotionLevel");
                    var statData = new CardStatistics.StatValueData { trackedValue = (CardStatistics.TrackedValueType)Enum.Parse(typeof(CardStatistics.TrackedValueType), "MonolithLevel"),
                        entryDuration = CardStatistics.EntryDuration.ThisTurn, cardTypeTarget = CardStatistics.CardTypeTarget.Any };
                    int nativeQuery = Managers.GetCardStatistics().GetStatValue(statData);
                    StatisticQueryResult? query = predicted?.Context == null ? null : StatisticQueryModel.Evaluate(predicted.Context, new CardStatisticQuery("MonolithLevel"));
                    if (query?.Supported == true) predicted = CardSpellModel.WithContext(predicted!, query.Context!);
                    int[] multipliers = { -2, 0, 1, 3 };
                    int[] passive = multipliers.Select(multiplier => (int)AccessTools.Method(typeof(CharacterState).Assembly.GetType("MonolithHelper"), "GetDevotionPassiveStacks")
                        .Invoke(null, new object[] { all, level, multiplier })).ToArray();
                    var actual = Trace.CaptureTrain(); object rngAfter = HalfHealthLifecycleCalibration.Rng();
                    string? difference = predicted == null ? "Missing model successor." : HalfHealthLifecycleCalibration.Difference(predicted, actual);
                    difference ??= query?.Supported != true || query.Value != nativeQuery ? "Monolith level statistic query differs." : null;
                    difference ??= !passive.SequenceEqual(multipliers.Select(multiplier => DevotionModel.PassiveStacks(actual.Context!.QueryFrame!.Devotion!, multiplier))) ? "Passive stack query differs." : null;
                    difference ??= predictedLevel != (level > input.Level) ? "Level return differs." : null;
                    difference ??= !notifications.SequenceEqual(level > input.Level ? new[] { "Cards", "Monsters" } : Array.Empty<string>()) ? "Level notification order differs." : null;
                    if (Queue.Count != 0) throw new InvalidOperationException("Devotion component left a pending trigger.");
                    if (difference != null) mismatches++;
                    samples.Add(new { Label = input.Label, Input = input, Before = before, Action = action, NativeTest = nativeTest,
                        CanApplyInPreview = effect?.GetCardEffect().CanApplyInPreviewMode, NativeLeveled = nativeLeveled, Actual = actual,
                        NativeLevelQuery = nativeQuery, Multipliers = multipliers, NativePassiveStacks = passive, Notifications = notifications.ToArray(),
                        RngBefore = rngBefore, RngAfter = rngAfter, PendingAfterDrain = Queue.Count, Difference = difference, Completed = true });
                    log.LogInfo("DEVOTION " + input.Label + " " + (difference ?? "MATCH"));
                    save.PreviewMode = false;
                }
            }
            finally
            {
                recording = false; save.PreviewMode = false; SetDevotion(originalDevotion.Level, originalDevotion.Points);
                Set(data, "runStats", stats); Set(save, "previewSaveData", previewSave);
                AccessTools.Property(typeof(SaveManager), "PreviewSessionId").SetValue(save, previewSession);
                foreach (var pair in streams) ((HadesRNG)AccessTools.Method(typeof(RandomManager), "GetRng").Invoke(null, new object[] { pair.Key })).Init(pair.Value);
                UnityEngine.Random.state = unity;
            }
            string? restoration = HalfHealthLifecycleCalibration.Difference(original, Trace.CaptureTrain()) ??
                HalfHealthLifecycleCalibration.Difference(rngOriginal, HalfHealthLifecycleCalibration.Rng());
            using (var document = NativeFixtureCapture.Capture(new { Schema = 1, GameVersion = UnityEngine.Application.version,
                GameModuleMvid = typeof(CharacterState).Assembly.ManifestModule.ModuleVersionId, Boundary = "NativeDevotionResourceAndCardEffects",
                OriginalVenerate = definition, LiveContextUnchanged = restoration == null, RestorationDifference = restoration, EffectsDrained = true,
                Mismatches = mismatches, RngBefore = rngOriginal, RngAfter = HalfHealthLifecycleCalibration.Rng(), Samples = samples }))
            using (var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "devotion.mt2f"))) document.Write(output);
            if (restoration != null || mismatches != 0) throw new InvalidOperationException("Devotion differs: " + mismatches + "; restoration: " + restoration);

            void SetDevotion(int level, int points)
            {
                AccessTools.Method(typeof(SaveManager), "SetDevotionLevel").Invoke(save, new object[] { level });
                AccessTools.Method(typeof(SaveManager), "SetDevotionPoints").Invoke(save, new object[] { points, DevotionModel.Threshold(DevotionProbe.Capture(Managers)!) });
            }
        }
        [HarmonyPatch(typeof(CardManager), "OnDevotionLevelChanged")]
        private static class CardPatch
        {
            private static bool Prepare() => ConditionalActivationProbe.HasVanguard;
            private static void Prefix() { if (recording) notifications.Add("Cards"); }
        }
        [HarmonyPatch(typeof(MonsterManager), "OnDevotionLevelChanged")]
        private static class MonsterPatch
        {
            private static bool Prepare() => ConditionalActivationProbe.HasVanguard;
            private static void Prefix() { if (recording) notifications.Add("Monsters"); }
        }
    }
}
