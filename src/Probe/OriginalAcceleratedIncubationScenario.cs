using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalAcceleratedIncubationScenario
    {
        internal const string Name = "original-accelerated-incubation";
        internal const string CardId = "71df734f-cb47-4f8b-a298-d15052e6b396";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            AllGameData all = save.GetAllGameData();
            CardManager cards = managers.GetCardManager()!;
            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!;
            CharacterData unit = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(unit, "attackDamage", 60); Set(unit, "health", 500); Set(unit, "size", 1);
            Set(unit, "startingStatusEffects", new[] { new StatusEffectStackData { statusId = "hatch", count = 3 } });
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID()))
                card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            cards.GetDrawPile().Clear();

            CardData acceleration = all.FindCardData(CardId)!;
            if (acceleration.GetAssetKey() != "AcceleratedIncubation" || acceleration.GetCardType() != CardType.Spell ||
                acceleration.GetTraits().Select(trait => trait.GetTraitStateName()).SequenceEqual(new[] { "CardTraitExhaustState" }) == false)
                throw new InvalidOperationException("Original Accelerated Incubation definition changed.");

            CardState Add(string id) => cards.AddNewCard(all.FindCardData(id)!, CardPile.HandPile, false, false, animate: false);
            CardState[] created = { Add(steward.GetID()), Add(steward.GetID()), Add(CardId) };
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true;
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-ACCELERATED-INCUBATION-PREPARED unchanged 0-cost hatch-removal Spell and two boosted friendly controls with Hatch 3.");
        }

        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            if (CardIds.Length != 3 || source.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0)
                .Units.Count(unit => unit.Team == CombatTeam.Player) < 2) return null;
            int id = source.Spawn.Train.Context!.Cards.Hand.FirstOrDefault(card => card.DataId == CardId)?.InstanceId ?? 0;
            if (id == 0) return null;
            return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(action => action.CardInstanceId == id && action.RoomIndex == 0);
        }

        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
