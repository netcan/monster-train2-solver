using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalXStygianSpikeScenario
    {
        internal const string Name = "original-x-stygian-spike";
        internal const string CardDataId = "bc5cea85-5b63-49fb-bd2b-fe057c015cdd";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            AllGameData all = save.GetAllGameData();
            CardManager cards = managers.GetCardManager()!;
            CardData spike = all.FindCardData(CardDataId)!;
            if (spike.name != "SpikeOfTheStygian" || spike.GetCardType() != CardType.Spell || spike.GetCost() != 0 ||
                spike.GetCostType() != CardData.CostType.ConsumeRemainingEnergy || spike.GetEffects().Count != 2 ||
                spike.GetEffects()[0].GetEffectStateName() != "CardEffectAddStatusEffect" ||
                spike.GetEffects()[0].GetTargetMode() != TargetMode.DropTargetCharacter ||
                spike.GetEffects()[1].GetEffectStateName() != "CardEffectAddStatusEffect" ||
                spike.GetEffects()[1].GetTargetMode() != TargetMode.LastTargetedCharacters || spike.GetTraits().Count != 3)
                throw new InvalidOperationException("Original Spike of the Stygian definition changed.");

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
            log.LogInfo("ORIGINAL-X-STYGIAN-SPIKE-PREPARED unchanged Spike of the Stygian debuff/poison at X=4/0 on the same selected ally.");
        }

        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            for (int index = 0; index < ids.Length; index++)
                if (source.Spawn.Train.Context!.Statistics!.Value(ids[index], "TimesPlayed", "ThisBattle") == 0)
                    return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play =>
                        play.CardInstanceId == ids[index] && play.RoomIndex == (index == 1 ? 1 : 0) &&
                        (index < 2 || play.TargetUnitId > 0));
            return null;
        }

        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
