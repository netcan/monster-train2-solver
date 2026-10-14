using System;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class MoonPhaseAdvanceScenario
    {
        private const string FaeMeddlerId = "d10e2dac-fa58-4509-b0ee-112b3bf78800";
        private const string StewardId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";
        private static ManualLogSource? logger;
        private static int stewardCardId;
        private static int moonriseCardId;
        private static int faeCardId;
        private static bool chosen;
        private static bool marked;

        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "moon-phase-advance";

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
            CardData steward = data.FindCardData(StewardId) ??
                throw new InvalidOperationException("Original Train Steward is missing.");
            CardData moonrise = data.FindCardData(MoonPhaseCardScenario.MoonriseId) ??
                throw new InvalidOperationException("Original Moonrise is missing.");
            CardData fae = data.FindCardData(FaeMeddlerId) ??
                throw new InvalidOperationException("Original Fae Meddler is missing.");
            CharacterData character = fae.GetSpawnCharacterData() ??
                throw new InvalidOperationException("Original Fae Meddler has no spawn character.");
            if (character.GetTriggers().Count != 1 ||
                character.GetTriggers()[0].GetTrigger() != CharacterTriggerData.Trigger.OnSpawn ||
                character.GetTriggers()[0].GetEffects().Count != 1 ||
                character.GetTriggers()[0].GetEffects()[0].GetEffectStateName() != "CardEffectAdvanceMoonPhase" ||
                character.GetTriggers()[0].GetEffects()[0].GetParamInt() != 1)
                throw new InvalidOperationException("Original Fae Meddler moon-phase trigger changed.");
            CardManager cards = managers.GetCardManager()!;
            if (cards.GetHand().Count > 7) throw new InvalidOperationException("Moon-phase advance requires three free hand slots.");
            CardState unit = cards.AddNewCard(steward, CardPile.HandPile, false, false, animate: false);
            CardState phase = cards.AddNewCard(moonrise, CardPile.HandPile, false, false, animate: false);
            CardState meddler = cards.AddNewCard(fae, CardPile.HandPile, false, false, animate: false);
            if (unit == null || phase == null || meddler == null ||
                !cards.GetHand().Contains(unit) || !cards.GetHand().Contains(phase) || !cards.GetHand().Contains(meddler))
                throw new InvalidOperationException("Moon-phase advance could not add its original cards.");
            stewardCardId = FullBattleTrace.Active!.CardId(unit);
            moonriseCardId = FullBattleTrace.Active.CardId(phase);
            faeCardId = FullBattleTrace.Active.CardId(meddler);
            PlayerManager player = managers.GetPlayerManager();
            bool tracking = (bool)AccessTools.Field(typeof(PlayerManager), "isMoonPhaseTrackingActive").GetValue(player);
            log.LogInfo("MOON-PHASE-ADVANCE-PREPARED steward=" + stewardCardId + " moonrise=" + moonriseCardId +
                " fae=" + faeCardId + " phase=" + player.CurrentMoonPhase + " tracking=" + tracking);
            if (!tracking) throw new InvalidOperationException("Original Moonrise did not activate moon-phase tracking.");
        }

        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            if (chosen) return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
            PlayCardAction[] plays = BattleActionModel.EnumerateSupportedPlays(source).ToArray();
            if (!source.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit => unit.SpawnerCardId == stewardCardId))
                return plays.FirstOrDefault(action => action.CardInstanceId == stewardCardId && action.RoomIndex == 1)
                    ?? BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
            if (source.MoonPhase != "Full")
                return plays.FirstOrDefault(action => action.CardInstanceId == moonriseCardId && action.RoomIndex == 0)
                    ?? BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
            PlayCardAction? fae = plays.FirstOrDefault(action => action.CardInstanceId == faeCardId && action.RoomIndex == 0);
            if (fae == null)
            {
                logger!.LogInfo("MOON-PHASE-ADVANCE-MISSING reason=" +
                    BattleActionModel.PlayCard(source, new PlayCardAction(faeCardId, 0, 1)).Reason);
                return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
            }
            BattleActionResult predicted = BattleActionModel.PlayCard(source, fae);
            logger!.LogInfo("MOON-PHASE-ADVANCE-CANDIDATE supported=" + predicted.Supported +
                " phase=" + source.MoonPhase + "->" + predicted.State?.MoonPhase +
                " newmoon=" + predicted.State?.Spawn.Train.Rooms[0].Units.FirstOrDefault(unit =>
                    unit.SpawnerCardId == faeCardId)?.Status("newmoon")?.Stacks + " reason=" + predicted.Reason);
            if (!predicted.Supported) return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
            chosen = true;
            return fae;
        }
    }
}
