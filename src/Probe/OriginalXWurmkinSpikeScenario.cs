using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalXWurmkinSpikeScenario
    {
        internal const string Name = "original-x-wurmkin-spike";
        internal const string CardDataId = "83c87c23-39a9-4c92-a6d0-3f640c35534d";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            AllGameData all = save.GetAllGameData();
            CardManager cards = managers.GetCardManager()!;
            CardData spike = all.FindCardData(CardDataId)!;
            if (spike.name != "WurmkinSpike" || spike.GetCardType() != CardType.Spell || spike.GetCost() != 0 ||
                spike.GetCostType() != CardData.CostType.ConsumeRemainingEnergy || spike.GetEffects().Count != 1 ||
                spike.GetEffects()[0].GetEffectStateName() != "CardEffectAddStatusEffect" ||
                spike.GetEffects()[0].GetTargetMode() != TargetMode.Room || spike.GetTraits().Count != 2)
                throw new InvalidOperationException("Original Wurmkin Spike definition changed.");

            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!;
            CharacterData unit = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0);
            Set(unit, "attackDamage", 0);
            Set(unit, "health", 200);
            Set(unit, "size", 1);
            Set(unit, "startingStatusEffects", Array.Empty<StatusEffectStackData>());
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID()))
                card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            cards.GetDrawPile().Clear();
            CardState Add(string id) => cards.AddNewCard(all.FindCardData(id)!, CardPile.HandPile, false, false, animate: false);
            CardState[] created = { Add(steward.GetID()), Add(steward.GetID()), Add(CardDataId), Add(CardDataId) };
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true;
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-X-WURMKIN-SPIKE-PREPARED unchanged Wurmkin Spike at X=4/0 against the same enemy room.");
        }

        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            for (int index = 0; index < ids.Length; index++)
                if (source.Spawn.Train.Context!.Statistics!.Value(ids[index], "TimesPlayed", "ThisBattle") == 0)
                {
                    if (index >= 2 && !source.Spawn.Train.Rooms[0].Units.Any(unit => unit.Team == CombatTeam.Enemy)) return null;
                    return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play =>
                        play.CardInstanceId == ids[index] && play.RoomIndex == (index == 1 ? 1 : 0));
                }
            return null;
        }

        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
