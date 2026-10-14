using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class MixScenario
    {
        internal const string Name = "card-effect-mix";
        private const string MixologyId = "9582724d-2d5e-4349-a642-9720bbb833e9";
        private const string BlastInfusionId = "3cf25a9b-daa1-42cd-8174-eb0f59cd590f";
        private static ManualLogSource? logger;
        private static int sourceId;
        private static bool played;
        private static int playedOpeningElixirId;
        private static int drafts;
        private static int firstOpeningChoice;
        private static int secondOpeningChoice;
        private static bool explosionWitness;
        private static bool seededPoison;
        private static IReadOnlyList<ElixirMixSelection> pendingSelections = Array.Empty<ElixirMixSelection>();
        private static int pendingSelectionIndex;

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            logger = log;
            played = false;
            playedOpeningElixirId = 0;
            seededPoison = false;
            drafts = 0;
            string opening = Environment.GetEnvironmentVariable("MT2_PROBE_MIX_CHOICES") ?? "00";
            if (opening.Length != 2 || opening.Any(choice => choice != '0' && choice != '1'))
                throw new InvalidOperationException("Mix opening choices must contain two binary indices.");
            firstOpeningChoice = opening[0] - '0';
            secondOpeningChoice = opening[1] - '0';
            explosionWitness = Environment.GetEnvironmentVariable("MT2_PROBE_MIX_UNSTABLE_EXPLOSION") == "1";
            if (explosionWitness && opening != "01")
                throw new InvalidOperationException("Unstable explosion fixture requires Mix choices 01.");
            pendingSelections = Array.Empty<ElixirMixSelection>();
            pendingSelectionIndex = 0;
            CardManager cards = managers.GetCardManager()!;
            CardData definition = managers.GetSaveManager().GetAllGameData().FindCardData(MixologyId) ??
                throw new InvalidOperationException("Mix fixture requires original Mixology.");
            if (definition.GetEffects().Count(effect => effect.GetEffectStateName() == typeof(CardEffectMix).Name) != 2 ||
                !cards.GetBelowHandSize())
                throw new InvalidOperationException("Mix fixture requires two original Mix effects and a free hand slot.");
            CardState added = cards.AddNewCard(definition, CardPile.HandPile, false, false, animate: false);
            if (added == null || !cards.GetHand().Contains(added))
                throw new InvalidOperationException("Mix fixture could not place Mixology in hand.");
            sourceId = FullBattleTrace.Active!.CardId(added);
            log.LogInfo("MIX-PREPARED original Mixology card=" + sourceId);
        }

        internal static bool IsScenarioPrelude(PlayCardAction action) =>
            action.CardInstanceId == sourceId || action.CardInstanceId == playedOpeningElixirId;

        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            if (!played)
            {
                CardToken? source = state.Spawn.Train.Context!.Cards.Hand.FirstOrDefault(card => card.InstanceId == sourceId);
                if (source != null && state.Energy >= 2)
                {
                    played = true;
                    return new PlayCardAction(sourceId, 0, mixSelections: new[] {
                        new ElixirMixSelection(0, 0, firstOpeningChoice),
                        new ElixirMixSelection(1, 0, secondOpeningChoice) });
                }
            }
            if (playedOpeningElixirId == 0 && firstOpeningChoice == 0 && secondOpeningChoice == 1)
            {
                CardToken? elixir = state.Spawn.Train.Context!.Cards.Hand.FirstOrDefault(card => card.DataId == BlastInfusionId);
                if (elixir != null)
                {
                    var target = state.Spawn.Train.Rooms
                        .SelectMany(room => room.Units.Select(unit => (room.RoomIndex, unit)))
                        .FirstOrDefault(item => item.unit.Team == CombatTeam.Player && !item.unit.IsPyre && item.unit.Health > 5);
                    if (target.unit != null)
                    {
                        playedOpeningElixirId = elixir.InstanceId;
                        return new PlayCardAction(elixir.InstanceId, target.RoomIndex, targetUnitId: target.unit.Id);
                    }
                }
            }
            if (explosionWitness && !seededPoison && playedOpeningElixirId != 0 && state.Spawn.Turn == 1)
            {
                RoomCombatState room = state.Spawn.Train.Rooms.Single(item => item.RoomIndex == 0);
                CombatUnit target = room.Units.Single(unit => unit.Team == CombatTeam.Player &&
                    unit.Status("unstable")?.Stacks == 5);
                if (!room.Units.Any(unit => unit.Team == CombatTeam.Enemy))
                    throw new InvalidOperationException("Unstable explosion fixture requires an enemy in the target room.");
                var nativeUnits = new List<CharacterState>();
                AllGameManagers.Instance!.GetRoomManager()!.GetRoom(0).AddCharactersToList(nativeUnits,
                    Team.Type.Heroes | Team.Type.Monsters);
                CharacterState nativeTarget = nativeUnits.Single(unit => FullBattleTrace.Active!.UnitId(unit) == target.Id);
                int prior = nativeTarget.GetStatusEffectStacks("poison");
                nativeTarget.AddStatusEffect("poison", 12, triggeredByCharacter: null,
                    allowModification: false, isFromHiddenTrigger: true);
                if (nativeTarget.GetStatusEffectStacks("poison") != prior + 12)
                    throw new InvalidOperationException("Original Frostbite seeding did not add twelve stacks.");
                seededPoison = true;
                logger!.LogInfo("MIX-UNSTABLE-EXPLOSION-SEEDED unit=" + target.Id +
                    " room=0 poison=" + prior + "->" + nativeTarget.GetStatusEffectStacks("poison") +
                    " enemies=" + room.Units.Count(unit => unit.Team == CombatTeam.Enemy));
            }
            if (playedOpeningElixirId != 0 && state.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .Any(unit => unit.Team == CombatTeam.Player && unit.Status("unstable")?.Stacks > 0))
                return null;
            return BattleActionModel.ChooseUnitSpellAndJunkPlay(state);
        }

        internal static void Expect(PlayCardAction action)
        {
            pendingSelections = action.MixSelections;
            pendingSelectionIndex = 0;
        }

        private static IEnumerator SelectDraft(ElixirDraftScreen screen, string kind)
        {
            yield return null;
            FieldInfo itemsField = AccessTools.Field(typeof(ElixirDraftScreen), "draftItems") ??
                throw new MissingFieldException(typeof(ElixirDraftScreen).FullName, "draftItems");
            IList items = (IList)itemsField.GetValue(screen);
            if (pendingSelectionIndex >= pendingSelections.Count)
                throw new InvalidOperationException("Native Mix opened a draft without a modeled choice.");
            int choiceIndex = pendingSelections[pendingSelectionIndex++].ChoiceIndex;
            if (choiceIndex < 0 || choiceIndex >= items.Count || items[choiceIndex] is not CardUI selected)
                throw new InvalidOperationException("Original Mix draft lacks its modeled card choice.");
            string choices = string.Join(",", items.Cast<CardUI>().Select(item =>
                item.GetCardState().GetCardDataID() + ":" + (item.GetCardUpgrade()?.GetID() ?? "none")));
            drafts++;
            logger!.LogInfo("MIX-DRAFT index=" + drafts + " kind=" + kind + " choice=" + choiceIndex + " choices=" + choices +
                " selected=" + selected.GetCardState().GetCardDataID() +
                " upgrade=" + (selected.GetCardUpgrade()?.GetID() ?? "none"));
            MethodInfo apply = AccessTools.Method(typeof(ElixirDraftScreen), "ApplyDraft") ??
                throw new MissingMethodException(typeof(ElixirDraftScreen).FullName, "ApplyDraft");
            CardManager cards = AllGameManagers.Instance!.GetCardManager()!;
            var before = new HashSet<CardState>(cards.GetHand());
            apply.Invoke(screen, new object[] { selected });
            CardState generated = cards.GetHand().Single(card => !before.Contains(card));
            logger.LogInfo("MIX-RESULT index=" + drafts + " card=" + FullBattleTrace.Active!.CardId(generated) +
                " data=" + generated.GetCardDataID() + " upgrades=" +
                string.Join(",", generated.GetCardStateModifiers().GetCardUpgrades().Select(upgrade => upgrade.GetAssetName())) +
                " hand=" + cards.GetHand().Count);
        }

        [HarmonyPatch(typeof(ElixirDraftScreen), nameof(ElixirDraftScreen.SetupForNewElixir))]
        private static class NewDraftPatch
        {
            [HarmonyPostfix]
            private static void Postfix(ElixirDraftScreen __instance)
            {
                if (logger != null && sourceId > 0) __instance.StartCoroutine(SelectDraft(__instance, "new"));
            }
        }

        [HarmonyPatch(typeof(ElixirDraftScreen), nameof(ElixirDraftScreen.SetupForElixirUpgrade))]
        private static class UpgradeDraftPatch
        {
            [HarmonyPostfix]
            private static void Postfix(ElixirDraftScreen __instance)
            {
                if (logger != null && sourceId > 0) __instance.StartCoroutine(SelectDraft(__instance, "upgrade"));
            }
        }
    }
}
