using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class NullTriggerScenario
    {
        internal const string Name = "null-triggers";
        private static readonly (string CardId, string CardKey, string UnitKey, string Trigger, string Target)[] Originals =
        {
            ("5d2b15ed-e9b4-464a-b029-f337f2ed6cd2", "LunaCovenChamp1", "MonsterChampionLunaCoven1", "OnDeath", "Self"),
            ("63eba1e1-3118-44a3-8906-c8bfbe559667", "LunaCovenChamp2", "MonsterChampionLunaCoven2", "OnSpawn", "Room")
        };
        private static readonly HashSet<string> played = new HashSet<string>(StringComparer.Ordinal);
        private static int scenarioActionId;
        private static ManualLogSource? logger;
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            CardManager cards = managers.GetCardManager()!;
            var all = managers.GetSaveManager().GetAllGameData();
            played.Clear(); scenarioActionId = 0;
            logger = log;
            foreach (var original in Originals)
            {
                CardData card = all.FindCardData(original.CardId) ??
                    throw new InvalidOperationException("Missing original " + original.CardKey);
                CharacterData unit = all.GetAllCharacterData().Single(item => item.GetAssetKey() == original.UnitKey);
                CharacterTriggerData trigger = unit.GetTriggers().Single(item => item.GetTrigger().ToString() == original.Trigger);
                CardEffectData[] effects = trigger.GetEffects().ToArray();
                if (card.name != original.CardKey || card.GetSpawnCharacterData() != unit || effects.Length != 1 ||
                    effects[0].GetEffectStateName() != "CardEffectNULL" ||
                    effects[0].GetTargetMode().ToString() != original.Target || effects[0].GetParamBool() ||
                    effects[0].GetParamBool2() || effects[0].GetAdditionalParamInt() != 0)
                    throw new InvalidOperationException("Original " + original.UnitKey + " null trigger changed.");
                if (!cards.GetBelowHandSize()) throw new InvalidOperationException("Null-trigger scene has no free hand slot.");
                CardState added = cards.AddNewCard(card, CardPile.HandPile, false, false, animate: false);
                if (added == null || !cards.GetHand().Contains(added))
                    throw new InvalidOperationException("Original " + original.CardKey + " did not enter the hand.");
            }
            log.LogInfo("NULL-TRIGGERS-PREPARED originals=" + string.Join(",", Originals.Select(item => item.UnitKey)));
        }

        internal static PlayCardAction? Choose(BattleTurnState state, ManualLogSource log)
        {
            foreach (var original in Originals.Where(item => !played.Contains(item.CardId)))
            {
                CardToken? card = state.Spawn.Train.Context!.Cards.Hand.FirstOrDefault(item => item.DataId == original.CardId);
                if (card == null) continue;
                PlayCardAction? action = BattleActionModel.EnumerateSupportedPlays(state).FirstOrDefault(item =>
                    item.CardInstanceId == card.InstanceId && item.RoomIndex < 3);
                if (action == null)
                {
                    var candidate = new PlayCardAction(card.InstanceId, 0, 0);
                    BattleActionResult reason = BattleActionModel.PlayCard(state, candidate);
                    throw new InvalidOperationException("Original " + original.CardKey + " cannot be played: " + reason.Reason);
                }
                played.Add(original.CardId);
                scenarioActionId = action.CardInstanceId;
                log.LogInfo("NULL-TRIGGERS-PLAY original=" + original.CardKey + " card=" + card.InstanceId);
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
                string? actor = cardEffectParams.selfTarget?.GetSourceCharacterData()?.GetAssetKey();
                if (!Originals.Any(item => item.UnitKey == actor)) return;
                logger?.LogInfo("NULL-TRIGGER-TEST actor=" + actor + " targets=" + cardEffectParams.targets.Count +
                    " result=" + __result);
            }
        }
    }
}
