using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class TypedDrawProbe
    {
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "typed-draw";
        internal static readonly List<Record> Records = new List<Record>();
        internal static readonly List<DrawRecord> Draws = new List<DrawRecord>();
        internal static readonly List<ClearRecord> Clears = new List<ClearRecord>();
        private static Record? current;
        private static readonly Dictionary<CardUpgradeState, int> sharedReferences = new Dictionary<CardUpgradeState, int>();
        private static int nextUpgradeReferenceId = 1;
        private static bool? branchReferences;
        private static bool BranchReferences
        {
            get
            {
                if (!CardBranchMaskProbe.Enabled) return false;
                if (branchReferences.HasValue) return branchReferences.Value;
                string? replay = Environment.GetEnvironmentVariable("MT2_PROBE_ROLLING_INPUT");
                if (string.IsNullOrEmpty(replay)) return (branchReferences = true).Value;
                // Frozen replays retain their source capture schema. A legacy
                // missing queue stays unknown; it is never treated as empty.
                using var document = MonsterTrain2Poju.Fixtures.FixtureDocument.Read(replay!);
                var cards = document.RootElement.GetProperty("RollingDecisions").EnumerateArray().First()
                    .GetProperty("Root").GetProperty("Spawn").GetProperty("Train").GetProperty("Context").GetProperty("Cards");
                return (branchReferences = cards.TryGetProperty("NextUpgradeReferenceId", out var reference) && reference.ValueKind ==
                    MonsterTrain2Poju.Fixtures.FixtureKind.Number).Value;
            }
        }
        private static bool TrackReferences => BranchReferences || Enabled || SoldierOfFortuneScenario.Enabled || OriginalDrawUpgradeScalingScenario.Enabled || OriginalDiscardDrawScenario.Enabled || OriginalCorruptedAngelsScenario.CostOrder != null || OriginalSupportSpellsScenario.IsolatedIds != null;
        internal static int? NextUpgradeReferenceId => TrackReferences ? nextUpgradeReferenceId : (int?)null;
        internal static int? SharedReferenceId(CardUpgradeState upgrade) => TrackReferences && sharedReferences.TryGetValue(upgrade, out int id) ? id : (int?)null;
        private static bool Recording => (BranchReferences || Enabled && TypedDrawScenario.Prepared || SoldierOfFortuneScenario.Prepared || OriginalDrawUpgradeScalingScenario.Prepared || OriginalDiscardDrawScenario.Prepared) && FullBattleTrace.Active != null &&
            !AllGameManagers.Instance!.GetSaveManager().PreviewMode;

        internal static TypedDrawRule DrawRule(CardEffectData effect) => new TypedDrawRule(effect.GetTargetCardType().ToString(),
            effect.GetParamCardFilter() == null ? null : CardUpgradeMaskProbe.Rule(effect.GetParamCardFilter()),
            effect.GetParamCardFilterSecondary() == null ? null : CardUpgradeMaskProbe.Rule(effect.GetParamCardFilterSecondary()));

        internal static NextDrawUpgradeRule UpgradeRule(CardEffectData effect)
        {
            var upgrade = new CardUpgradeState(); upgrade.Setup(effect.GetParamCardUpgradeData());
            return UpgradeRule(upgrade);
        }

        private static NextDrawUpgradeRule UpgradeRule(CardUpgradeState upgrade) => new NextDrawUpgradeRule(
            CardModifierProbe.Upgrade(upgrade, rejectFilters: false, filtersAlreadyApplied: true),
            upgrade.GetFilters().Select(CardUpgradeMaskProbe.Rule).ToArray());

        internal static IReadOnlyList<NextDrawUpgradeRule>? CaptureQueue(CardManager cards)
        {
            var queue = (IEnumerable<CardUpgradeState>)AccessTools.Field(typeof(CardManager), "nextDrawnTempCardUpgrades").GetValue(cards);
            NextDrawUpgradeRule[] values = queue.Select(UpgradeRule).ToArray();
            return values.Length == 0 && !TrackReferences ? null : values;
        }

        internal sealed class Record
        {
            public string Kind { get; set; } = "";
            public string CardAsset { get; set; } = "";
            public int PlayedCardId { get; set; }
            public int EffectIndex { get; set; }
            public int Amount { get; set; }
            public int RequiredGold { get; set; }
            public bool Sampled { get; set; }
            public bool Completed { get; set; }
            public TypedDrawRule? Rule { get; set; }
            public NextDrawUpgradeRule? Upgrade { get; set; }
            public CardEffectRange? Range { get; set; }
            public BattlePlayRules Definitions { get; set; } = null!;
            public CombatContext Before { get; set; } = null!;
            public CombatContext? After { get; set; }
        }

        internal sealed class DrawRecord
        {
            public int EffectRecordIndex { get; set; }
            public int CardId { get; set; }
            public int SourceCardId { get; set; }
            public int HandCount { get; set; }
            public int MaxHandSize { get; set; }
            public int PendingUpgrades { get; set; }
            public int MagicPowerMultiplier { get; set; }
            public bool FromDiscard { get; set; }
            public bool Success { get; set; }
            public CombatContext? Before { get; set; }
            public CombatContext? After { get; set; }
            public BattlePlayRules? Definitions { get; set; }
        }

        internal sealed class ClearRecord
        {
            public string Boundary { get; set; } = "OnCardEffectsWereApplied";
            public IReadOnlyList<NextDrawUpgradeRule> Before { get; set; } = null!;
            public IReadOnlyList<NextDrawUpgradeRule>? After { get; set; }
        }

        [HarmonyPatch(typeof(CardManager), nameof(CardManager.AddTempCardUpgradeToNextDrawnCard))]
        private static class QueueIdentityPatch
        {
            private static void Postfix(CardManager __instance, CardUpgradeState upgradeState)
            {
                if (!TrackReferences || AllGameManagers.Instance!.GetSaveManager().PreviewMode || sharedReferences.ContainsKey(upgradeState)) return;
                var queue = (IEnumerable<CardUpgradeState>)AccessTools.Field(typeof(CardManager), "nextDrawnTempCardUpgrades").GetValue(__instance);
                // Allocation follows successful manager operations, never the
                // order in which a snapshot happens to visit cards or upgrades.
                if (queue.Any(upgrade => ReferenceEquals(upgrade, upgradeState)))
                    sharedReferences.Add(upgradeState, nextUpgradeReferenceId++);
            }
        }

        [HarmonyPatch]
        private static class EffectPatch
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                yield return AccessTools.Method(typeof(CardEffectDrawType), nameof(CardEffectDrawType.ApplyEffect));
                yield return AccessTools.Method(typeof(CardEffectAddTempCardUpgradeToNextDrawnCard), nameof(CardEffectAddTempCardUpgradeToNextDrawnCard.ApplyEffect));
            }
            private static void Postfix(CardEffectState cardEffectState, CardEffectParams cardEffectParams, ref IEnumerator __result)
            { if (Recording) __result = Observe(__result, cardEffectState, cardEffectParams); }
        }

        private static IEnumerator Observe(IEnumerator native, CardEffectState effect, CardEffectParams parameters)
        {
            FullBattleTrace trace = FullBattleTrace.Active!;
            CardEffectData data = effect.GetSourceCardEffectData();
            bool drawing = effect.GetCardEffect() is CardEffectDrawType;
            Record? parent = current;
            var record = new Record
            {
                Kind = drawing ? "DrawType" : "NextDrawUpgrade",
                CardAsset = effect.GetParentCardState()?.GetAssetName() ?? "",
                PlayedCardId = parameters.playedCard == null ? 0 : trace.CardId(parameters.playedCard),
                EffectIndex = effect.GetParentCardState()?.GetEffectStates().IndexOf(effect) ?? -1,
                Rule = drawing ? DrawRule(data) : null,
                Upgrade = drawing ? null : UpgradeRule(data),
                RequiredGold = drawing ? 0 : effect.GetAdditionalParamInt(),
                Range = drawing && effect.GetUseIntRange() ? new CardEffectRange(effect.GetParamMinInt(), effect.GetParamMaxInt(), effect.GetParamMultiplier()) : null,
                Before = trace.CaptureContext()
            };
            record.Definitions = CaptureDefinitions(trace);
            Records.Add(record); current = record;
            try { while (native.MoveNext()) yield return native.Current; record.Completed = true; }
            finally { (native as IDisposable)?.Dispose(); current = parent; record.After = trace.CaptureContext(); }
        }

        [HarmonyPatch(typeof(CardEffectState), nameof(CardEffectState.GetIntInRange))]
        private static class AmountPatch
        {
            private static void Postfix(CardEffectState __instance, int __result)
            {
                if (current != null && !current.Sampled && __instance.GetCardEffect() is CardEffectDrawType)
                { current.Amount = __result; current.Sampled = true; }
            }
        }

        [HarmonyPatch(typeof(CardManager), nameof(CardManager.DrawSpecificCard))]
        private static class DrawPatch
        {
            private static void Prefix(CardManager __instance, CardState drawCard, CardState playedSourceCard, out DrawRecord? __state)
            {
                __state = null;
                if (!Recording) return;
                FullBattleTrace trace = FullBattleTrace.Active!;
                __state = new DrawRecord
                {
                    EffectRecordIndex = current == null ? -1 : Records.IndexOf(current), CardId = trace.CardId(drawCard),
                    SourceCardId = playedSourceCard == null ? 0 : trace.CardId(playedSourceCard),
                    HandCount = __instance.GetHand().Count, MaxHandSize = __instance.GetMaxHandSize(),
                    PendingUpgrades = CaptureQueue(__instance)?.Count ?? 0, MagicPowerMultiplier = drawCard.GetMagicPowerMultiplierFromTraits(),
                    FromDiscard = __instance.GetDiscardPile().Contains(drawCard)
                };
                if (__state.PendingUpgrades > 0)
                {
                    __state.Before = trace.CaptureContext();
                    __state.Definitions = current?.Definitions ?? CaptureDefinitions(trace);
                }
                Draws.Add(__state);
            }
            private static void Postfix(bool __result, DrawRecord? __state)
            {
                if (__state == null) return;
                __state.Success = __result;
                if (__state.Before != null) __state.After = FullBattleTrace.Active!.CaptureContext();
            }
        }

        [HarmonyPatch(typeof(CardManager), nameof(CardManager.OnCardEffectsWereApplied))]
        private static class ClearPatch
        {
            private static void Prefix(CardManager __instance, out ClearRecord? __state)
            {
                __state = null;
                if (!Recording) return;
                IReadOnlyList<NextDrawUpgradeRule>? queue = CaptureQueue(__instance);
                if (queue == null || queue.Count == 0) return;
                __state = new ClearRecord { Before = queue }; Clears.Add(__state);
            }
            private static void Postfix(CardManager __instance, ClearRecord? __state)
            { if (__state != null) __state.After = CaptureQueue(__instance); }
        }

        [HarmonyPatch(typeof(CardManager), "OnCardPlayed")]
        private static class PlayedClearPatch
        {
            private static void Postfix(CardManager __instance, ref IEnumerator __result)
            { if (Recording) __result = ObservePlayedClear(__instance, __result); }
        }

        private static IEnumerator ObservePlayedClear(CardManager cards, IEnumerator native)
        {
            IReadOnlyList<NextDrawUpgradeRule>? before = CaptureQueue(cards);
            try
            {
                while (native.MoveNext()) yield return native.Current;
                // OnCardPlayed temporarily hides the queue during callbacks,
                // restores it for discard, then clears it before completion.
                // Only record completion, never that temporary hiding step.
                if (before != null && before.Count > 0)
                    Clears.Add(new ClearRecord { Boundary = "OnCardPlayed", Before = before, After = CaptureQueue(cards) });
            }
            finally { (native as IDisposable)?.Dispose(); }
        }

        private static BattlePlayRules CaptureDefinitions(FullBattleTrace trace) => new BattlePlayRules(Array.Empty<RoomPlayRule>(),
            trace.KnownCards.Select(card => card.GetCardDataID()).Distinct().Select(id =>
                BattleActionProbe.Definition(AllGameManagers.Instance!.GetSaveManager().GetAllGameData().FindCardData(id)!)).ToArray());
    }
}
