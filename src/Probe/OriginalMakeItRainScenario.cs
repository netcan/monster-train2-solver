using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalMakeItRainScenario
    {
        internal const string Name = "original-make-it-rain";
        internal const string CardDataId = "8a6ebb24-d5c1-430f-9332-1d3f43f47121";
        private const string StewardId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            AllGameData all = save.GetAllGameData();
            CardManager cards = managers.GetCardManager()!;
            CardData spell = all.FindCardData(CardDataId) ?? throw new InvalidOperationException("Original Make It Rain CardData lookup failed.");
            CardEffectData[] effects = spell.GetEffects().ToArray();
            CardTraitData[] traits = spell.GetTraits().ToArray();
            string[] effectTypes = effects.Select(effect => effect.GetEffectStateName()).ToArray();
            if (spell.name != "MakeItRain" || spell.GetCardType() != CardType.Spell ||
                !effectTypes.SequenceEqual(new[] { "CardEffectNULL", "CardEffectDamage", "CardEffectRewardGold", "CardEffectDamage",
                    "CardEffectRewardGold", "CardEffectDamage", "CardEffectRewardGold", "CardEffectDamage" }) || traits.Length != 0)
                throw new InvalidOperationException($"Original Make It Rain definition changed: name={spell.name}, type={spell.GetCardType()}, cost={spell.GetCost()}, effects=[{string.Join(";", effects.Select(effect => $"{effect.GetEffectStateName()}:{effect.GetTargetMode()}:{effect.GetParamInt()}:{effect.GetParamBool()}:{effect.GetAdditionalParamInt()}:{effect.GetParamBool2()}"))}], traits={traits.Length}.");
            log.LogInfo($"ORIGINAL-MAKE-IT-RAIN-DEFINITION cost={spell.GetCost()} costType={spell.GetCostType()} effects=[{string.Join(";", effects.Select(effect => $"{effect.GetEffectStateName()}:{effect.GetTargetMode()}:{effect.GetParamInt()}:{effect.GetParamBool()}:{effect.GetAdditionalParamInt()}:{effect.GetParamBool2()}"))}].");

            CardData steward = all.FindCardData(StewardId) ?? throw new InvalidOperationException("Train Steward CardData lookup failed.");
            CharacterData unit = steward.GetSpawnCharacterData() ?? throw new InvalidOperationException("Train Steward has no spawn character.");
            Set(steward, "cost", 0);
            Set(unit, "attackDamage", 9999);
            Set(unit, "health", 9999);
            Set(unit, "size", 1);
            Set(unit, "startingStatusEffects", Array.Empty<StatusEffectStackData>());
            foreach (CardState state in cards.GetAllCards(new List<CardState>()).Where(state => state.GetCardDataID() == steward.GetID()))
                state.Setup(steward, save);
            foreach (CardState state in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(state);
            cards.GetDrawPile().Clear();
            cards.GetDiscardPile().Clear();
            cards.GetExhaustedPile().Clear();

            CardState Add(string id) => cards.AddNewCard(all.FindCardData(id)!, CardPile.HandPile, false, false, animate: false);
            CardState[] created = { Add(StewardId), Add(StewardId), Add(CardDataId) };
            if (created.Any(card => card == null || !cards.GetHand().Contains(card)))
                throw new InvalidOperationException("Original Make It Rain scenario could not add its controlled cards.");
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true;
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-MAKE-IT-RAIN-PREPARED unchanged damage/NULL/reward spell and controlled original Stewards on separate floors.");
        }

        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            CombatContext context = source.Spawn.Train.Context!;
            for (int index = 0; index < ids.Length; index++)
            {
                if (context.Statistics!.Value(ids[index], "TimesPlayed", "ThisBattle") != 0 ||
                    !context.Cards.Hand.Any(card => card.InstanceId == ids[index])) continue;
                int room = index == 1 ? 1 : 0;
                if (index == 2 && (source.Spawn.Turn < 1 || !source.Spawn.Train.Rooms[0].Units.Any(unit => unit.Team == CombatTeam.Enemy))) return null;
                return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == room &&
                    (index < 2 ? play.PlayerPosition == 0 : true));
            }
            return null;
        }

        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
