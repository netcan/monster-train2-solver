using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal sealed partial class BattleAdvisorOverlay
    {
        private static readonly FieldInfo elixirDraftItems = AccessTools.Field(typeof(DraftScreenBase), "draftItems");
        private AdvisorMixChoice? pendingMixChoice;
        private ElixirDraftScreen? mixChoiceScreen;

        private void ClearMixChoice()
        {
            pendingMixChoice = null;
            mixChoiceScreen = null;
        }

        private bool ShowPendingMixChoice(AllGameManagers managers)
        {
            var screens = managers.GetScreenManager();
            if (mixChoiceScreen == null || screens.GetTopScreen() != ScreenName.Elixir ||
                !ReferenceEquals(mixChoiceScreen, screens.GetScreen(ScreenName.Elixir))) return false;
            var items = (List<IDraftableUI>)elixirDraftItems.GetValue(mixChoiceScreen);
            string? guidance = pendingMixChoice?.Prompt(screenActive: true, items.Count);
            if (guidance == null) return false;
            if (body.text != guidance) body.text = guidance;
            showingCardChoice = true;
            SetPending(true);
            return true;
        }

        private void OpenElixirDraft(ElixirDraftScreen screen, bool infusion)
        {
            if (pendingMixChoice == null) return;
            if (!infusion || !pendingMixChoice.Open())
            {
                ClearMixChoice();
                return;
            }
            mixChoiceScreen = screen;
        }

        private void CompleteElixirDraft(ElixirDraftScreen screen, IDraftableUI? chosen)
        {
            if (pendingMixChoice == null || !ReferenceEquals(screen, mixChoiceScreen)) return;
            var items = (List<IDraftableUI>)elixirDraftItems.GetValue(screen);
            if (chosen == null || !pendingMixChoice.Choose(items.IndexOf(chosen))) ClearMixChoice();
            mixChoiceScreen = null;
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
                Active?.OpenElixirDraft(__instance, draftType == ElixirDraftScreen.DraftType.Infusion &&
                    __originalMethod.Name != nameof(ElixirDraftScreen.SetupForCardChoice));
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
