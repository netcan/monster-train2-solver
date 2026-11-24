using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine.UI;

namespace MonsterTrain2Poju.Probe
{
    internal sealed partial class BattleAdvisorOverlay
    {
        private static readonly FieldInfo elixirDraftItems = AccessTools.Field(typeof(DraftScreenBase), "draftItems");
        private AdvisorMixChoice? pendingMixChoice;
        private string? pendingSummonChoice;
        private ElixirDraftScreen? elixirChoiceScreen;
        private bool HasPendingElixirChoice => pendingMixChoice != null || pendingSummonChoice != null;

        private void ClearElixirChoice()
        {
            pendingMixChoice = null;
            pendingSummonChoice = null;
            elixirChoiceScreen = null;
        }

        private bool ShowPendingElixirChoice(AllGameManagers managers)
        {
            var screens = managers.GetScreenManager();
            if (elixirChoiceScreen == null || screens.GetTopScreen() != ScreenName.Elixir ||
                !ReferenceEquals(elixirChoiceScreen, screens.GetScreen(ScreenName.Elixir))) return false;
            var items = (List<IDraftableUI>)elixirDraftItems.GetValue(elixirChoiceScreen);
            string? guidance = pendingMixChoice?.Prompt(screenActive: true, items.Count);
            if (pendingSummonChoice is string expected)
            {
                int index = AdvisorForecastPresentation.SummonOfferIndex(items.Select(item =>
                    (item as CardUI)?.GetCardState()?.GetCardDataID()).ToArray(), expected);
                if (index >= 0 && items[index] is CardUI chosen)
                    guidance = "选择 " + AdvisorCardPresentation.Plain(chosen.GetCardState().GetTitle()) + " · 第 " + (index + 1) + " 项";
            }
            if (guidance == null) return false;
            if (body.text != guidance)
            {
                body.text = guidance;
                LayoutRebuilder.ForceRebuildLayoutImmediate(body.rectTransform);
                planScroll.StopMovement();
                planScroll.verticalNormalizedPosition = 1;
            }
            showingCardChoice = true;
            SetPending(true);
            return true;
        }

        private void OpenElixirDraft(ElixirDraftScreen screen, bool infusion, bool summon)
        {
            if (!HasPendingElixirChoice) return;
            bool matched = pendingMixChoice != null ? infusion && pendingMixChoice.Open() : summon && elixirChoiceScreen == null;
            if (!matched)
            {
                ClearElixirChoice();
                return;
            }
            elixirChoiceScreen = screen;
        }

        private void CompleteElixirDraft(ElixirDraftScreen screen, IDraftableUI? chosen)
        {
            if (!HasPendingElixirChoice || !ReferenceEquals(screen, elixirChoiceScreen)) return;
            var items = (List<IDraftableUI>)elixirDraftItems.GetValue(screen);
            if (chosen == null || pendingMixChoice?.Choose(items.IndexOf(chosen)) != true) ClearElixirChoice();
            elixirChoiceScreen = null;
            if (showingCardChoice) body.text = "正在核对局面…";
            showingCardChoice = false;
        }

        [HarmonyPatch]
        private static class ElixirDraftSetupPatch
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                yield return AccessTools.Method(typeof(ElixirDraftScreen), nameof(ElixirDraftScreen.SetupForNewElixir));
                yield return AccessTools.Method(typeof(ElixirDraftScreen), nameof(ElixirDraftScreen.SetupForElixirUpgrade));
                yield return AccessTools.Method(typeof(ElixirDraftScreen), nameof(ElixirDraftScreen.SetupForCardChoice));
            }

            private static void Postfix(ElixirDraftScreen __instance, ElixirDraftScreen.DraftType draftType, MethodBase __originalMethod)
            {
                if (AllGameManagers.Instance?.GetSaveManager()?.PreviewMode != false || PreviewRngIsolation.Active) return;
                bool cardChoice = __originalMethod.Name == nameof(ElixirDraftScreen.SetupForCardChoice);
                Active?.OpenElixirDraft(__instance, draftType == ElixirDraftScreen.DraftType.Infusion &&
                    !cardChoice, draftType == ElixirDraftScreen.DraftType.SummonChoice && cardChoice);
            }
        }

        [HarmonyPatch(typeof(ElixirDraftScreen), "ApplyDraft")]
        private static class ElixirDraftApplyPatch
        {
            private static void Prefix(ElixirDraftScreen __instance, IDraftableUI draftedItem) =>
                Active?.CompleteElixirDraft(__instance, draftedItem);
        }

        [HarmonyPatch(typeof(ElixirDraftScreen), "SkipDraft")]
        private static class ElixirDraftSkipPatch
        {
            private static void Prefix(ElixirDraftScreen __instance) => Active?.CompleteElixirDraft(__instance, null);
        }
    }
}
