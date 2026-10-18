using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalStatusCardsScenario
    {
        internal const string Name = "original-status-cards";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager(); AllGameData all = save.GetAllGameData(); CardManager cards = managers.GetCardManager()!;
            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!; CharacterData unit = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(unit, "attackDamage", 60); Set(unit, "health", 200); Set(unit, "size", 1);
            Set(unit, "startingStatusEffects", new[] {
                new StatusEffectStackData { statusId = "buff", count = 3 },
                new StatusEffectStackData { statusId = "regen", count = 2 },
                new StatusEffectStackData { statusId = "armor", count = 4 } });
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            CardState Add(string id) => cards.AddNewCard(all.FindCardData(id)!, CardPile.HandPile, false, false, animate: false);
            CardIds = new[] { Add(steward.GetID()), Add(steward.GetID()), Add(steward.GetID()),
                Add("704dac6e-cb4f-48a0-a6c9-5a24ae67791c"), Add("5c39e06b-8da6-4d2d-b59d-299d13481d26"),
                Add("dc7b0d83-2a0c-460b-ae92-42ee144005a5") }.Select(FullBattleTrace.Active!.CardId).ToArray();
            Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-STATUS-CARDS-PREPARED unchanged Proliferate, Glow Up and Spike of the Legion; propagated rage/regen, armor/floor/target controls and X scaling.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            CombatContext context = source.Spawn.Train.Context!;
            for (int index = 0; index < ids.Length; index++)
            {
                if (context.Statistics!.Value(ids[index], "TimesPlayed", "ThisBattle") != 0) continue;
                int target = index >= 4 ? source.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == ids[0]).Id : 0;
                return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] &&
                    play.RoomIndex == (index == 2 ? 1 : 0) && (target == 0 || play.TargetUnitId == target));
            }
            return null;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
