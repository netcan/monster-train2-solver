using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalXBanishedSpikeScenario
    {
        internal const string Name = "original-x-banished-spike";
        internal const string CardDataId = "ab3c2bfa-0ad4-4be0-a050-293f4866bd65";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            AllGameData all = save.GetAllGameData();
            CardManager cards = managers.GetCardManager()!;
            CardData banished = all.FindCardData(CardDataId)!;
            if (banished.name != "SpikeOfTheBanished" || banished.GetCardType() != CardType.Spell || banished.GetCost() != 0 ||
                banished.GetCostType() != CardData.CostType.ConsumeRemainingEnergy || banished.GetEffects().Count != 1 ||
                banished.GetEffects()[0].GetEffectStateName() != "CardEffectAddStatusEffect" ||
                banished.GetEffects()[0].GetTargetMode() != TargetMode.DropTargetCharacter || banished.GetTraits().Count != 2)
                throw new InvalidOperationException("Original Spike of the Banished definition changed.");

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
            log.LogInfo("ORIGINAL-X-BANISHED-SPIKE-PREPARED unchanged Spike of the Banished at X=4/0 with selected ally and neighboring-floor controls.");
        }

        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            for (int index = 0; index < ids.Length; index++)
                if (source.Spawn.Train.Context!.Statistics!.Value(ids[index], "TimesPlayed", "ThisBattle") == 0)
                {
                    int target = index >= 2
                        ? source.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == ids[0]).Id
                        : 0;
                    return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play =>
                        play.CardInstanceId == ids[index] && play.RoomIndex == (index == 1 ? 1 : 0) &&
                        (target == 0 || play.TargetUnitId == target));
                }
            return null;
        }

        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
