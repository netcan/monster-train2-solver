using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OverhealScenario
    {
        private const string StewardId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";
        internal const string TidalTurningId = "3c96e9b7-bdfb-4784-88f7-4b01bd58b4b7";
        internal const string FaesBlessingId = "f7839c43-c139-4671-9f53-9347673e3b0b";
        private static ManualLogSource? logger;
        private static int stewardCardId;
        private static int tidalCardId;
        private static int faeCardId;
        private static bool tidalCast;
        private static bool faeCast;
        private static bool tidalMarked;
        private static bool faeMarked;

        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "overheal";
        internal static bool IsScenarioAction(PlayCardAction action)
        {
            if (!Enabled) return false;
            if (action.CardInstanceId == tidalCardId && !tidalMarked) { tidalMarked = true; return true; }
            if (action.CardInstanceId == faeCardId && !faeMarked) { faeMarked = true; return true; }
            return false;
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            logger = log;
            tidalCast = false;
            faeCast = false;
            tidalMarked = false;
            faeMarked = false;
            SaveManager save = managers.GetSaveManager();
            AllGameData data = save.GetAllGameData();
            CardData steward = data.FindCardData(StewardId) ??
                throw new InvalidOperationException("Overheal fixture requires original Train Steward.");
            CardData tidal = data.FindCardData(TidalTurningId) ??
                throw new InvalidOperationException("Overheal fixture requires original Tidal Turning.");
            CardData fae = data.FindCardData(FaesBlessingId) ??
                throw new InvalidOperationException("Overheal fixture requires original Fae's Blessing.");
            if (tidal.GetEffects().Count(effect => effect.GetEffectStateName() == "CardEffectHealAndOverheal") != 1)
                throw new InvalidOperationException("Original Tidal Turning overheal definition changed.");
            if (fae.GetEffects().Count(effect => effect.GetEffectStateName() == "CardEffectHealAndOverheal") != 1)
                throw new InvalidOperationException("Original Fae's Blessing overheal definition changed.");
            CardManager cards = managers.GetCardManager()!;
            if (cards.GetHand().Count > 7) throw new InvalidOperationException("Overheal fixture requires three free hand slots.");
            CardState unit = cards.AddNewCard(steward, CardPile.HandPile, false, false, animate: false);
            CardState spell = cards.AddNewCard(tidal, CardPile.HandPile, false, false, animate: false);
            CardState selected = cards.AddNewCard(fae, CardPile.HandPile, false, false, animate: false);
            if (unit == null || spell == null || selected == null ||
                !cards.GetHand().Contains(unit) || !cards.GetHand().Contains(spell) || !cards.GetHand().Contains(selected))
                throw new InvalidOperationException("Overheal fixture could not add its original cards.");
            AccessTools.Field(typeof(CardState), "cost").SetValue(spell, 0);
            AccessTools.Field(typeof(CardState), "cost").SetValue(selected, 0);
            stewardCardId = FullBattleTrace.Active!.CardId(unit);
            tidalCardId = FullBattleTrace.Active.CardId(spell);
            faeCardId = FullBattleTrace.Active.CardId(selected);
            log.LogInfo("OVERHEAL-PREPARED steward=" + stewardCardId + " tidal=" + tidalCardId + " fae=" + faeCardId);
        }

        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            if (faeCast) return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
            PlayCardAction[] plays = BattleActionModel.EnumerateSupportedPlays(source).ToArray();
            CombatUnit? steward = source.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .FirstOrDefault(unit => unit.SpawnerCardId == stewardCardId);
            if (steward == null) return plays.FirstOrDefault(action => action.CardInstanceId == stewardCardId)
                ?? BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
            int roomIndex = source.Spawn.Train.Rooms.Single(room => room.Units.Any(unit => unit.Id == steward.Id)).RoomIndex;
            PlayCardAction? chosen = tidalCast
                ? plays.FirstOrDefault(action => action.CardInstanceId == faeCardId && action.TargetUnitId == steward.Id)
                : plays.FirstOrDefault(action => action.CardInstanceId == tidalCardId && action.RoomIndex == roomIndex);
            if (chosen == null) return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
            BattleActionResult predicted = BattleActionModel.PlayCard(source, chosen);
            logger!.LogInfo("OVERHEAL-CANDIDATE supported=" + predicted.Supported + " hp=" + steward.Health +
                "/" + steward.MaxHealth + " armor=" + (steward.Status("armor")?.Stacks ?? 0) +
                " card=" + chosen.CardInstanceId + " reason=" + predicted.Reason);
            if (!predicted.Supported) return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
            if (tidalCast) faeCast = true;
            else tidalCast = true;
            return chosen;
        }
    }
}
