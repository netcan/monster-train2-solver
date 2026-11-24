#if DEVELOPMENT_PROBE
using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using UnityEngine;
using UnityEngine.UI;

namespace MonsterTrain2Poju.Probe
{
    internal static partial class PlayerAdvisorProbe
    {
        internal static bool SummonPromptWitness => Environment.GetEnvironmentVariable("MT2_PROBE_ADVISOR_SUMMON_PROMPT") == "1";
        private static readonly Guid SummonModule = new("bf93f060-beda-46b1-b808-95ebf1e9a99d");
        private static readonly string[] GrimFlightRanks =
        {
            "823ab526-9cfe-4383-9e17-a033d49a7fa9",
            "6b9e1108-af4e-468f-a912-48853b0f0413",
            "3de67996-0562-41ce-b892-1cedabf0bffc"
        };
        private static int summonRank;
        private static string? summonAbility;
        private static bool summonInstalled, summonEnabledInRun;
        private static SummonChoiceSelection? expectedSummon;
        internal static object? SummonPrompt { get; private set; }
        internal static bool SummonPromptCleared => Field<string?>("pendingSummonChoice") == null && !Field<bool>("showingCardChoice");
        internal static object SummonEntitlement => new
        {
            Installed = summonInstalled, EnabledInRun = summonEnabledInRun, ChampionId = OriginalSummonChoiceScenario.Champion,
            Rank = summonRank, RankId = GrimFlightRanks[summonRank - 1],
            AbilityId = summonAbility, CheckpointSha256 = Environment.GetEnvironmentVariable("MT2_PROBE_ADVISOR_SUMMON_CHECKPOINT_SHA256")
        };

        internal static void CheckSummonPromptCheckpoint(SaveManager save)
        {
            Require(Enabled && Application.version == "2.3.1" && typeof(CardState).Module.ModuleVersionId == SummonModule &&
                !save.PreviewMode && !save.InTestScenario && !SaveManager.WasDevCheatUsed &&
                save.GetRunType() == RunType.Class && save.GetGameSequence() == SaveData.GameSequence.BattleIntro,
                "the licensed GrimFlight witness requires the pinned original Class BattleIntro");
            CheckSummonEntitlement(save);
            Require(int.TryParse(Environment.GetEnvironmentVariable("MT2_PROBE_GRIM_FLIGHT_RANK"),
                NumberStyles.None, CultureInfo.InvariantCulture, out int rank) &&
                rank >= 1 && rank <= GrimFlightRanks.Length, "the licensed checkpoint has no expected GrimFlight rank");
            summonRank = rank;
            (_, summonAbility) = ReadSummonChampion(save);
        }

        private static (CardState Card, string AbilityId) ReadSummonChampion(SaveManager save)
        {
            CardState card = save.GetDeckState().Single(candidate => candidate.GetCardDataID() == OriginalSummonChoiceScenario.Champion);
            var all = save.GetAllGameData();
            var definition = all.FindCardData(OriginalSummonChoiceScenario.Champion);
            string rankId = GrimFlightRanks[summonRank - 1];
            Require(definition != null && definition.GetRequiredDLC() == ShinyShoe.DLC.Punished &&
                card.GetCardStateModifiers().GetCardUpgrades().Where(upgrade => GrimFlightRanks.Contains(upgrade.GetCardUpgradeDataId()))
                    .Select(upgrade => upgrade.GetCardUpgradeDataId()).SequenceEqual(new[] { rankId }),
                "the resumed deck does not own the expected original GrimFlight rank");
            var rankDefinition = PunishedChampionDefinitionProbe.Tree(all, definition!)!.GetUpgradeTrees()
                .SelectMany(path => path.GetCardUpgrades()).Single(upgrade => upgrade.GetID() == rankId);
            var ability = rankDefinition.GetUnitAbilityUpgrade();
            Require(ability != null && ability.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectChooseMonsterToSpawnInBack"),
                "the original GrimFlight rank has no native summon skill");
            return (card, ability!.GetID());
        }

        private static void CheckSummonEntitlement(SaveManager save)
        {
            summonInstalled = save.IsDlcInstalled(ShinyShoe.DLC.Punished);
            Require(summonInstalled, "the original client reports Punished DLC is not installed");
            summonEnabledInRun = save.IsDlcEnabledForCurrentRun(ShinyShoe.DLC.Punished);
            Require(summonEnabledInRun, "the original client reports Punished DLC is not enabled in this run");
        }

        private static void ArmSummonPrompt(PlayCardAction action, int nativeActorId)
        {
            Require(expectedSummon == null && summonRank > 0 && summonAbility != null,
                "the previous summon choice is unsettled or the licensed checkpoint was not verified");
            if (action.SummonSelections.Count == 0) return;
            SaveManager save = AllGameManagers.Instance.GetSaveManager();
            CheckSummonEntitlement(save);
            var (champion, abilityId) = ReadSummonChampion(save);
            var trace = FullBattleTrace.Active!;
            CharacterState? actor = trace.KnownUnits.SingleOrDefault(unit => trace.UnitId(unit) == nativeActorId);
            Require(nativeActorId > 0 && abilityId == summonAbility && actor != null && ReferenceEquals(actor.GetSpawnerCard(), champion) &&
                actor.GetUnitAbility()?.GetID() == abilityId &&
                actor.GetUnitAbilityCardState()?.GetCardDataID() == abilityId &&
                action.SummonSelections.Count == 1 && action.ActivatorRoomIndex == null && action.MixSelections.Count == 0 &&
                action.TargetUnitId == 0 && action.TargetSpawnPointIndex >= 0,
                "the published action does not identify the official GrimFlight skill, destination and one summon");
            expectedSummon = action.SummonSelections.Single();
        }

        [HarmonyPatch(typeof(ElixirDraftScreen), nameof(ElixirDraftScreen.SetupForCardChoice))]
        private static class SummonPromptDraftPatch
        {
            private static bool Prepare() => SummonPromptWitness;
            private static void Postfix(ElixirDraftScreen __instance, ElixirDraftScreen.DraftType draftType)
            {
                if (draftType == ElixirDraftScreen.DraftType.SummonChoice &&
                    AllGameManagers.Instance?.GetSaveManager()?.PreviewMode == false && expectedSummon != null)
                    __instance.StartCoroutine(ObserveSummonPrompt(__instance, draftType));
            }
        }

        private static IEnumerator ObserveSummonPrompt(ElixirDraftScreen screen, ElixirDraftScreen.DraftType draftType)
        {
            yield return null;
            var screens = AllGameManagers.Instance.GetScreenManager();
            var choice = expectedSummon ?? throw new InvalidOperationException("Original summon draft has no published choice.");
            var input = RollingAdviceScenario.Records.LastOrDefault()?.Action;
            Require(input is { ActivatorUnitId: > 0 } && input.SummonSelections.Count == 1 &&
                input.SummonSelections[0].EffectIndex == choice.EffectIndex && input.SummonSelections[0].CardDataId == choice.CardDataId,
                "the active native summon draft does not match the published input");
            var items = (IList)AccessTools.Field(typeof(DraftScreenBase), "draftItems").GetValue(screen);
            var offers = items.Cast<object>().Select(item => (item as CardUI)?.GetCardState()?.GetCardDataID()).ToArray();
            int index = AdvisorForecastPresentation.SummonOfferIndex(offers, choice.CardDataId);
            Require(offers.Length == 3 && index >= 0, "the published summon is not unique among the three native offers");
            var selected = (CardUI)items[index];
            string prompt = "选择 " + AdvisorCardPresentation.Plain(selected.GetCardState().GetTitle()) + " · 第 " + (index + 1) + " 项";
            for (int frame = 0; frame < 12 && !VisibleElixirPrompt(prompt); frame++) yield return null;
            Require(VisibleElixirPrompt(prompt) && ReferenceEquals(screen, screens.GetScreen(ScreenName.Elixir)),
                "the production summon prompt is absent from the active original viewport");
            int promptFrame = Time.frameCount;
            CheckSummonEntitlement(AllGameManagers.Instance.GetSaveManager());
            expectedSummon = null;
            AccessTools.Method(typeof(ElixirDraftScreen), "ApplyDraft").Invoke(screen, new object[] { selected });
            bool cleared = SummonPromptCleared && Field<Text>("body").text == "正在核对局面…";
            Require(cleared, "the completed native summon left a visible production choice prompt");
            SummonPrompt = new { DraftType = draftType.ToString(), Offers = offers, choice.EffectIndex, choice.CardDataId,
                ChoiceIndex = index, Text = prompt, PromptFrame = promptFrame, ConfirmationFrame = Time.frameCount,
                HiddenAfterConfirmation = cleared };
        }
    }
}
#endif
