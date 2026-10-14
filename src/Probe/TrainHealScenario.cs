using System;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class TrainHealScenario
    {
        private const string CardId = "996a0953-168a-44b9-bb90-5e46cbbb3e09";
        private static ManualLogSource? logger;
        private static int cardInstanceId;
        private static bool chosen;

        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "train-heal";
        internal static bool IsScenarioAction(PlayCardAction action) => Enabled && action.CardInstanceId == cardInstanceId;

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            logger = log;
            chosen = false;
            SaveManager save = managers.GetSaveManager();
            if (save.GetDragonsHoardCap() < 2 || save.GetMaxTowerHP() < 12)
                throw new InvalidOperationException("Train heal fixture requires Dragon's Hoard and a live Pyre.");
            save.SetDragonsHoardAmount(2);
            save.Cheat_SetTowerHP(save.GetMaxTowerHP() - 10);
            CardManager cards = managers.GetCardManager()!;
            if (!cards.GetBelowHandSize()) throw new InvalidOperationException("Train heal fixture requires a free hand slot.");
            CardData definition = save.GetAllGameData().FindCardData(CardId) ??
                throw new InvalidOperationException("Train heal fixture requires native PayOff.");
            CardState added = cards.AddNewCard(definition, CardPile.HandPile, false, false, animate: false);
            if (added == null || !cards.GetHand().Contains(added) ||
                !added.GetEffectStates().Any(effect => effect.GetCardEffect() is CardEffectHealTrain) ||
                !added.GetTraitStates().Any(trait => trait is CardTraitScalingHealTrain))
                throw new InvalidOperationException("Native PayOff did not initialize its train heal and scaling trait.");
            AccessTools.Field(typeof(CardState), "cost").SetValue(added, 0);
            cardInstanceId = FullBattleTrace.Active!.CardId(added);
            log.LogInfo("TRAIN-HEAL-PREPARED PayOff card=" + cardInstanceId + " hp=" + save.GetTowerHP() +
                "/" + save.GetMaxTowerHP() + " hoard=" + save.GetDragonsHoardAmount());
        }

        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            if (chosen) return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
            PlayCardAction? action = BattleActionModel.EnumerateSupportedPlays(source)
                .FirstOrDefault(item => item.CardInstanceId == cardInstanceId);
            if (action == null)
            {
                logger!.LogInfo("TRAIN-HEAL-NO-CANDIDATE card=" + cardInstanceId);
                return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
            }
            BattleActionResult predicted = BattleActionModel.PlayCard(source, action);
            int before = source.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre).Health;
            int after = predicted.State?.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .Single(unit => unit.IsPyre).Health ?? before;
            logger!.LogInfo("TRAIN-HEAL-CANDIDATE supported=" + predicted.Supported + " before=" + before +
                " after=" + after + " reason=" + predicted.Reason);
            if (!predicted.Supported) return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
            chosen = true;
            return action;
        }
    }
}
