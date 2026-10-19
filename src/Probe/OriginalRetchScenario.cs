using System;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalRetchScenario
    {
        internal const string Name = "original-retch";
        internal const string CardDataId = "9d801796-2b9f-496a-96fe-99b28bb32251";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static int EatenWitnessId { get; private set; }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            AllGameData all = save.GetAllGameData();
            CardManager cards = managers.GetCardManager()!;
            CardData retch = all.FindCardData(CardDataId)!;
            CardEffectData effect = retch.GetEffects().Single();
            if (retch.name != "Retch" || retch.GetCardType() != CardType.Spell || retch.GetTraits().Count != 0 ||
                effect.GetEffectStateName() != "CardEffectRecursion" || effect.GetTargetMode() != TargetMode.Eaten)
                throw new InvalidOperationException($"Original Retch definition changed: asset={retch.name}, type={retch.GetCardType()}, traits={retch.GetTraits().Count}, effect={effect.GetEffectStateName()}, target={effect.GetTargetMode()}.");

            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!;
            CharacterData unit = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0);
            Set(unit, "attackDamage", 9999);
            Set(unit, "health", 9999);
            Set(unit, "size", 1);
            Set(unit, "startingStatusEffects", Array.Empty<StatusEffectStackData>());
            foreach (CardState state in cards.GetAllCards(new System.Collections.Generic.List<CardState>()).Where(state => state.GetCardDataID() == steward.GetID()))
                state.Setup(steward, save);
            foreach (CardState state in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(state);
            cards.GetDrawPile().Clear();

            CardState protector = cards.AddNewCard(steward, CardPile.HandPile, false, false, animate: false);
            CardState spell = cards.AddNewCard(retch, CardPile.HandPile, false, false, animate: false);
            CardState eaten = cards.AddNewCard(steward, CardPile.DeckPile, false, false, animate: false);
            if (eaten == null || !cards.GetDrawPile().Remove(eaten))
                throw new InvalidOperationException("Original Retch fixture could not create its controlled Monster source card.");
            cards.GetEatenPile().Add(eaten);
            if (!cards.GetEatenPile().Contains(eaten)) throw new InvalidOperationException("Original Retch fixture could not seed Eaten.");
            CardIds = new[] { FullBattleTrace.Active!.CardId(protector), FullBattleTrace.Active.CardId(spell) };
            EatenWitnessId = FullBattleTrace.Active.CardId(eaten);
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true;
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-RETCH-PREPARED unchanged Retch, controlled original Monster seeded in Eaten and one protector.");
        }

        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            if (index < 0) return null;
            if (index == 0)
                return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == 0);
            if (source.Spawn.Turn < 1 || !source.Spawn.Train.Rooms[0].Units.Any(unit => unit.Team == CombatTeam.Enemy)) return null;
            return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == 0);
        }

        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
