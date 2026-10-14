using System;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class MoonPhaseCardScenario
    {
        internal const string MoonriseId = "fe6dfafa-9712-484c-9bb4-145c0989e667";
        private const string StewardId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";
        private static ManualLogSource? logger;
        private static int stewardCardId;
        private static int moonriseCardId;
        private static bool chosen;
        private static bool marked;

        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "moon-phase-card";

        internal static bool IsScenarioAction(PlayCardAction action)
        {
            if (!Enabled || marked || action.CardInstanceId != moonriseCardId) return false;
            marked = true;
            return true;
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            logger = log;
            chosen = false;
            marked = false;
            AllGameData data = managers.GetSaveManager().GetAllGameData();
            CardData moonrise = data.FindCardData(MoonriseId) ??
                throw new InvalidOperationException("Original Moonrise is missing.");
            CardData steward = data.FindCardData(StewardId) ??
                throw new InvalidOperationException("Original Train Steward is missing.");
            if (moonrise.GetEffects().Count != 2 ||
                moonrise.GetEffects()[0].GetEffectStateName() != "CardEffectSetMoonPhase" ||
                moonrise.GetEffects()[0].GetParamInt() != 2 ||
                moonrise.GetEffects()[1].GetEffectStateName() != "CardEffectGainEnergy" ||
                moonrise.GetEffects()[1].GetParamInt() != 1)
                throw new InvalidOperationException("Original Moonrise effects changed.");
            CardManager cards = managers.GetCardManager()!;
            if (cards.GetHand().Count > 8) throw new InvalidOperationException("Moonrise requires two free hand slots.");
            CardState unit = cards.AddNewCard(steward, CardPile.HandPile, false, false, animate: false);
            CardState spell = cards.AddNewCard(moonrise, CardPile.HandPile, false, false, animate: false);
            if (unit == null || spell == null || !cards.GetHand().Contains(unit) || !cards.GetHand().Contains(spell))
                throw new InvalidOperationException("Moonrise could not add its original cards.");
            stewardCardId = FullBattleTrace.Active!.CardId(unit);
            moonriseCardId = FullBattleTrace.Active.CardId(spell);
            PlayerManager player = managers.GetPlayerManager();
            bool tracking = (bool)AccessTools.Field(typeof(PlayerManager), "isMoonPhaseTrackingActive").GetValue(player);
            log.LogInfo("MOON-PHASE-CARD-PREPARED steward=" + stewardCardId + " moonrise=" + moonriseCardId +
                " phase=" + player.CurrentMoonPhase + " tracking=" + tracking);
            if (!tracking) throw new InvalidOperationException("Adding original Moonrise did not activate moon-phase tracking.");
        }

        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            if (chosen) return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
            PlayCardAction[] plays = BattleActionModel.EnumerateSupportedPlays(source).ToArray();
            CombatUnit? steward = source.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .FirstOrDefault(unit => unit.SpawnerCardId == stewardCardId);
            if (steward == null) return plays.FirstOrDefault(action => action.CardInstanceId == stewardCardId && action.RoomIndex == 0)
                ?? BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
            PlayCardAction? action = plays.FirstOrDefault(item => item.CardInstanceId == moonriseCardId && item.RoomIndex == 0);
            if (action == null) return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
            BattleActionResult predicted = BattleActionModel.PlayCard(source, action);
            logger!.LogInfo("MOON-PHASE-CARD-CANDIDATE supported=" + predicted.Supported +
                " phase=" + source.MoonPhase + "->" + predicted.State?.MoonPhase +
                " energy=" + source.Energy + "->" + predicted.State?.Energy +
                " fullmoon=" + predicted.State?.Spawn.Train.Rooms[0].Units.FirstOrDefault(unit => unit.Id == steward.Id)
                    ?.Status("fullmoon")?.Stacks + " reason=" + predicted.Reason);
            if (!predicted.Supported) return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
            chosen = true;
            return action;
        }
    }
}
