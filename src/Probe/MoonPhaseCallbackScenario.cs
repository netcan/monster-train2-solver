using System;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class MoonPhaseCallbackScenario
    {
        private const string AcolyteId = "2b7a187f-da0b-4c34-b8c5-b18755911b43";
        private const string FaeMeddlerId = "d10e2dac-fa58-4509-b0ee-112b3bf78800";
        private static ManualLogSource? logger;
        private static int acolyteCardId;
        private static int moonriseCardId;
        private static int faeCardId;
        private static bool chosen;
        private static bool marked;

        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "moon-phase-callbacks";

        internal static bool IsScenarioAction(PlayCardAction action)
        {
            if (!Enabled || marked || action.CardInstanceId != faeCardId) return false;
            marked = true;
            return true;
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            logger = log;
            chosen = false;
            marked = false;
            AllGameData data = managers.GetSaveManager().GetAllGameData();
            CardData acolyte = data.FindCardData(AcolyteId) ??
                throw new InvalidOperationException("Original Selene Acolyte is missing.");
            CardData moonrise = data.FindCardData(MoonPhaseCardScenario.MoonriseId) ??
                throw new InvalidOperationException("Original Moonrise is missing.");
            CardData fae = data.FindCardData(FaeMeddlerId) ??
                throw new InvalidOperationException("Original Fae Meddler is missing.");
            CharacterData character = acolyte.GetSpawnCharacterData() ??
                throw new InvalidOperationException("Original Selene Acolyte has no spawn character.");
            if (character.GetTriggers().Count != 2 ||
                !character.GetTriggers().Any(trigger => trigger.GetTrigger() == CharacterTriggerData.Trigger.OnMoonLit &&
                    trigger.GetEffects().Count == 1 &&
                    trigger.GetEffects()[0].GetEffectStateName() == "CardEffectAddTempCardUpgradeToUnits") ||
                !character.GetTriggers().Any(trigger => trigger.GetTrigger() == CharacterTriggerData.Trigger.OnMoonlitLost &&
                    trigger.GetEffects().Count == 1 &&
                    trigger.GetEffects()[0].GetEffectStateName() == "CardEffectRemoveTempUpgradeFromUnit"))
                throw new InvalidOperationException("Original Selene Acolyte moon-phase callbacks changed.");
            CardManager cards = managers.GetCardManager()!;
            if (cards.GetHand().Count > 7) throw new InvalidOperationException("Moon-phase callbacks require three free hand slots.");
            CardState unit = cards.AddNewCard(acolyte, CardPile.HandPile, false, false, animate: false);
            CardState phase = cards.AddNewCard(moonrise, CardPile.HandPile, false, false, animate: false);
            CardState meddler = cards.AddNewCard(fae, CardPile.HandPile, false, false, animate: false);
            if (unit == null || phase == null || meddler == null ||
                !cards.GetHand().Contains(unit) || !cards.GetHand().Contains(phase) || !cards.GetHand().Contains(meddler))
                throw new InvalidOperationException("Moon-phase callbacks could not add the original cards.");
            acolyteCardId = FullBattleTrace.Active!.CardId(unit);
            moonriseCardId = FullBattleTrace.Active.CardId(phase);
            faeCardId = FullBattleTrace.Active.CardId(meddler);
            PlayerManager player = managers.GetPlayerManager();
            bool tracking = (bool)AccessTools.Field(typeof(PlayerManager), "isMoonPhaseTrackingActive").GetValue(player);
            log.LogInfo("MOON-PHASE-CALLBACKS-PREPARED acolyte=" + acolyteCardId +
                " moonrise=" + moonriseCardId + " fae=" + faeCardId +
                " phase=" + player.CurrentMoonPhase + " tracking=" + tracking);
            if (!tracking) throw new InvalidOperationException("Original Moonrise did not activate moon-phase tracking.");
        }

        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            if (chosen) return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
            PlayCardAction[] plays = BattleActionModel.EnumerateSupportedPlays(source).ToArray();
            CombatUnit? acolyte = source.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .FirstOrDefault(unit => unit.SpawnerCardId == acolyteCardId);
            if (acolyte == null)
                return plays.FirstOrDefault(action => action.CardInstanceId == acolyteCardId && action.RoomIndex == 1)
                    ?? BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
            if (source.MoonPhase != "Full")
            {
                PlayCardAction? moonrise = plays.FirstOrDefault(action => action.CardInstanceId == moonriseCardId && action.RoomIndex == 0);
                if (moonrise == null)
                    logger!.LogInfo("MOON-PHASE-CALLBACKS-FULL-MISSING reason=" +
                        BattleActionModel.PlayCard(source, new PlayCardAction(moonriseCardId, 0)).Reason);
                return moonrise ?? BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
            }
            PlayCardAction? fae = plays.FirstOrDefault(action => action.CardInstanceId == faeCardId && action.RoomIndex == 0);
            if (fae == null)
            {
                logger!.LogInfo("MOON-PHASE-CALLBACKS-NEW-MISSING reason=" +
                    BattleActionModel.PlayCard(source, new PlayCardAction(faeCardId, 0, 0)).Reason);
                return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
            }
            BattleActionResult predicted = BattleActionModel.PlayCard(source, fae);
            CombatUnit? changed = predicted.State?.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .FirstOrDefault(unit => unit.Id == acolyte.Id);
            logger!.LogInfo("MOON-PHASE-CALLBACKS-CANDIDATE supported=" + predicted.Supported +
                " phase=" + source.MoonPhase + "->" + predicted.State?.MoonPhase +
                " acolyte=" + acolyte.AttackBeforeStatuses + "->" + changed?.AttackBeforeStatuses +
                " reason=" + predicted.Reason);
            if (!predicted.Supported) return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
            chosen = true;
            return fae;
        }
    }
}
