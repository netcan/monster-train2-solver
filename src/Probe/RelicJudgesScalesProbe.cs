using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class RelicJudgesScalesProbe
    {
        internal sealed class TestRecord
        {
            public int Turn { get; set; }
            public bool Placement { get; set; }
            public bool Result { get; set; }
            public int[] EligibleCardIds { get; set; } = Array.Empty<int>();
        }

        internal sealed class ChoiceRecord
        {
            public int CardId { get; set; }
            public string DataId { get; set; } = string.Empty;
        }

        internal sealed class ApplicationRecord
        {
            public int Turn { get; set; }
            public bool Placement { get; set; }
            public CombatContext Before { get; set; } = null!;
            public CombatContext? After { get; set; }
            public List<ChoiceRecord> Choices { get; } = new();
            public bool Cancelled { get; set; }
            public int SuppressedFollowupSelections { get; set; }
            public int[] ModelEligibleCardIds { get; set; } = Array.Empty<int>();
            public int[] ModelDiscardedCardIds { get; set; } = Array.Empty<int>();
            public int[] ModelDrawnCardIds { get; set; } = Array.Empty<int>();
            public bool ModelSupported { get; set; }
            public string? ModelUnsupportedReason { get; set; }
            public string? Difference { get; set; }
            public bool Completed { get; set; }
            public string? Error { get; set; }
        }

        internal static readonly List<TestRecord> Tests = new();
        internal static readonly List<ApplicationRecord> Applications = new();
        private static ApplicationRecord? current;
        private static bool applying;

        private static bool IsOwner(RelicEffectChooseDiscardsDrawReplacementsOnPostOpeningDraw effect) =>
            OriginalJudgesScalesScenario.Acquired && FullBattleTrace.Active != null &&
            effect.SourceRelicState?.GetRelicDataID() == OriginalJudgesScalesScenario.OwnerId &&
            !AllGameManagers.Instance!.GetSaveManager().PreviewMode;

        [HarmonyPatch(typeof(RelicEffectChooseDiscardsDrawReplacementsOnPostOpeningDraw),
            nameof(RelicEffectChooseDiscardsDrawReplacementsOnPostOpeningDraw.TestEffectOnPostOpeningDraw))]
        private static class TestPatch
        {
            private static void Postfix(RelicEffectChooseDiscardsDrawReplacementsOnPostOpeningDraw __instance, bool __result)
            {
                if (!IsOwner(__instance)) return;
                try
                {
                    CardManager cards = AllGameManagers.Instance!.GetCardManager()!;
                    Tests.Add(new TestRecord
                    {
                        Turn = AllGameManagers.Instance.GetCombatManager()!.GetTurnCount(),
                        Placement = AllGameManagers.Instance.GetCombatManager()!.IsPlacementPhase,
                        Result = __result,
                        EligibleCardIds = cards.GetHand().Where(card => !card.HasTrait<CardTraitInfusion>() &&
                            !card.HasTrait<CardTraitPersistent>()).Select(FullBattleTrace.Active!.CardId).ToArray()
                    });
                }
                catch (Exception error) { FullBattleTrace.Active!.CaptureFailure(error); }
            }
        }

        [HarmonyPatch(typeof(RelicEffectChooseDiscardsDrawReplacementsOnPostOpeningDraw),
            nameof(RelicEffectChooseDiscardsDrawReplacementsOnPostOpeningDraw.ApplyEffectOnPostOpeningDraw))]
        private static class ApplyPatch
        {
            private static void Postfix(RelicEffectChooseDiscardsDrawReplacementsOnPostOpeningDraw __instance, ref IEnumerator __result)
            {
                if (IsOwner(__instance)) __result = Observe(__result);
            }
        }

        [HarmonyPatch(typeof(CardManager), nameof(CardManager.SelectCardFromHand))]
        private static class SelectPatch
        {
            private static bool Prefix(CardManager __instance, HandSelectionUI.Params parameters, ref IEnumerator __result)
            {
                if (!applying || current == null) return true;
                __result = ChooseOneThenCancel(__instance, parameters);
                return false;
            }
        }

        [HarmonyPatch(typeof(CardManager), nameof(CardManager.AddSelectCardFromHandOnCompletedCallback))]
        private static class FollowupPatch
        {
            private static bool Prefix(Action callback)
            {
                if (!applying || current == null) return true;
                current.SuppressedFollowupSelections++;
                return false;
            }
        }

        private static IEnumerator Observe(IEnumerator native)
        {
            FullBattleTrace trace = FullBattleTrace.Active!;
            var record = new ApplicationRecord
            {
                Turn = AllGameManagers.Instance!.GetCombatManager()!.GetTurnCount(),
                Placement = AllGameManagers.Instance.GetCombatManager()!.IsPlacementPhase,
                Before = trace.CaptureContext()
            };
            Applications.Add(record);
            current = record;
            applying = true;
            try
            {
                while (MoveNext(native, record, trace, out object? value)) yield return value;
                record.Completed = true;
            }
            finally
            {
                (native as IDisposable)?.Dispose();
                applying = false;
                current = null;
                CaptureAfter(record, trace);
                Predict(record, trace);
            }
        }

        private static IEnumerator ChooseOneThenCancel(CardManager cards, HandSelectionUI.Params parameters)
        {
            ApplicationRecord record = current!;
            FullBattleTrace trace = FullBattleTrace.Active!;
            CardState? selected = cards.GetHand().FirstOrDefault(card => !parameters.filterCallback(card));
            if (selected == null)
            {
                record.Error = "Native Judges Scales requested a selection with no eligible card.";
                yield return parameters.cancelledCallback(cards);
                yield break;
            }
            record.Choices.Add(new ChoiceRecord { CardId = trace.CardId(selected), DataId = selected.GetCardDataID() });
            yield return parameters.cardChosenCallback(selected, cards);
            if (cards.GetHand().Any(card => !parameters.filterCallback(card)))
            {
                record.Cancelled = true;
                yield return parameters.cancelledCallback(cards);
            }
        }

        private static void Predict(ApplicationRecord record, FullBattleTrace trace)
        {
            try
            {
                EnemySpawnState spawn = trace.CaptureCanonicalSpawn();
                BattlePlayRules definitions = trace.CapturePlayRules(spawn);
                RelicJudgesScalesResult result = RelicJudgesScalesModel.Apply(record.Before, definitions,
                    record.Choices.Select(choice => choice.CardId).ToArray(), record.Placement);
                record.ModelEligibleCardIds = result.EligibleCardIds.ToArray();
                record.ModelDiscardedCardIds = result.DiscardedCardIds.ToArray();
                record.ModelDrawnCardIds = result.DrawnCardIds.ToArray();
                record.ModelSupported = result.Supported;
                record.ModelUnsupportedReason = result.UnsupportedReason;
                if (result.Supported && record.After != null)
                    record.Difference = JToken.DeepEquals(
                        JToken.FromObject(BattleTurnProbe.ComparableContext(result.Context)!),
                        JToken.FromObject(BattleTurnProbe.ComparableContext(record.After)!))
                        ? null : "Native Judges Scales post-opening discard/draw differs from the model.";
            }
            catch (Exception error)
            {
                record.ModelUnsupportedReason = error.ToString();
                trace.CaptureFailure(error);
            }
        }

        private static bool MoveNext(IEnumerator native, ApplicationRecord record, FullBattleTrace trace, out object? currentValue)
        {
            try
            {
                bool next = native.MoveNext();
                currentValue = next ? native.Current : null;
                return next;
            }
            catch (Exception error)
            {
                record.Error = error.ToString();
                trace.CaptureFailure(error);
                throw;
            }
        }

        private static void CaptureAfter(ApplicationRecord record, FullBattleTrace trace)
        {
            try { record.After = trace.CaptureContext(); }
            catch (Exception error) { record.Error = error.ToString(); trace.CaptureFailure(error); }
        }
    }
}
