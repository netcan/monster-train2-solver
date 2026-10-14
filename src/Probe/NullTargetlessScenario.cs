using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class NullTargetlessScenario
    {
        internal const string Name = "null-targetless";
        private static readonly (string CardId, string CardKey)[] Originals =
        {
            ("62cc5532-5fa0-4e66-9fec-85be573af359", "ScourgeT1"),
            ("21038774-c0a4-413c-ab7e-27e458228e5f", "SelfPurgeBlight")
        };
        private static readonly HashSet<string> played = new HashSet<string>(StringComparer.Ordinal);
        private static readonly HashSet<string> observed = new HashSet<string>(StringComparer.Ordinal);
        private static int scenarioActionId;
        private static ManualLogSource? logger;
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            CardManager cards = managers.GetCardManager()!;
            var all = managers.GetSaveManager().GetAllGameData();
            played.Clear(); observed.Clear(); scenarioActionId = 0;
            logger = log;
            foreach (var original in Originals)
            {
                CardData card = all.FindCardData(original.CardId) ??
                    throw new InvalidOperationException("Missing original " + original.CardKey);
                CardEffectData[] effects = card.GetEffects().ToArray();
                if (card.name != original.CardKey || effects.Length != 1 ||
                    effects[0].GetEffectStateName() != "CardEffectNULL" ||
                    effects[0].GetTargetMode() != TargetMode.Room ||
                    effects[0].GetTargetTeamType() != Team.Type.None ||
                    !effects[0].GetParamBool2() || effects[0].GetParamBool() ||
                    effects[0].GetAdditionalParamInt() != 0 ||
                    !card.GetTraits().Any(trait => trait.GetTraitStateName() == "CardTraitSelfPurge"))
                    throw new InvalidOperationException("Original " + original.CardKey + " null effect changed.");
                if (!cards.GetBelowHandSize()) throw new InvalidOperationException("Null-targetless scene has no free hand slot.");
                CardState added = cards.AddNewCard(card, CardPile.HandPile, false, false, animate: false);
                if (added == null || !cards.GetHand().Contains(added))
                    throw new InvalidOperationException("Original " + original.CardKey + " did not enter the hand.");
            }
            log.LogInfo("NULL-TARGETLESS-PREPARED originals=" + string.Join(",", Originals.Select(item => item.CardKey)));
        }

        internal static PlayCardAction? Choose(BattleTurnState state, ManualLogSource log)
        {
            foreach (var original in Originals.Where(item => !played.Contains(item.CardId)))
            {
                CardToken? card = state.Spawn.Train.Context!.Cards.Hand.FirstOrDefault(item => item.DataId == original.CardId);
                if (card == null) continue;
                if (state.Spawn.Train.Rooms[0].Units.Any(unit => unit.Health > 0))
                    throw new InvalidOperationException("Null-targetless scene requires an empty room.");
                PlayCardAction? action = BattleActionModel.EnumerateSupportedPlays(state).FirstOrDefault(item =>
                    item.CardInstanceId == card.InstanceId && item.RoomIndex == 0);
                if (action == null)
                {
                    BattleActionResult reason = BattleActionModel.PlayCard(state, new PlayCardAction(card.InstanceId, 0));
                    throw new InvalidOperationException("Original " + original.CardKey + " cannot be played: " + reason.Reason);
                }
                played.Add(original.CardId);
                scenarioActionId = action.CardInstanceId;
                log.LogInfo("NULL-TARGETLESS-PLAY original=" + original.CardKey + " room=0 card=" + card.InstanceId);
                return action;
            }
            scenarioActionId = 0;
            return BattleActionModel.ChooseUnitSpellAndJunkPlay(state);
        }

        internal static bool ConsumeScenarioActionMark(PlayCardAction action)
        {
            bool matches = scenarioActionId > 0 && action.CardInstanceId == scenarioActionId;
            scenarioActionId = 0;
            return matches;
        }

        [HarmonyPatch(typeof(CardEffectNULL), nameof(CardEffectNULL.TestEffect))]
        private static class NullTestPatch
        {
            private static void Postfix(CardEffectParams cardEffectParams, bool __result)
            {
                if (!Enabled || AllGameManagers.Instance?.GetSaveManager().PreviewMode != false) return;
                string? cardId = cardEffectParams.playedCard?.GetCardDataID();
                if (cardId == null) return;
                foreach (var original in Originals.Where(item => item.CardId == cardId && observed.Add(cardId)))
                    logger?.LogInfo("NULL-TARGETLESS-TEST original=" + original.CardKey +
                        " targets=" + cardEffectParams.targets.Count + " result=" + __result);
            }
        }
    }
}
