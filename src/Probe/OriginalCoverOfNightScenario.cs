using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalCoverOfNightScenario
    {
        internal const string Name = "original-cover-of-night";
        internal const string CardId = "e022d66a-f794-4ff9-95a6-b3cc53f49a27";
        internal const string MoonriseId = "fe6dfafa-9712-484c-9bb4-145c0989e667";
        private const string StewardId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            AllGameData all = save.GetAllGameData();
            CardManager cards = managers.GetCardManager()!;
            CardData cover = all.FindCardData(CardId) ?? throw new InvalidOperationException("Original Cover of Night CardData lookup failed.");
            CardData moonrise = all.FindCardData(MoonriseId) ?? throw new InvalidOperationException("Original Moonrise CardData lookup failed.");
            CardData steward = all.FindCardData(StewardId) ?? throw new InvalidOperationException("Train Steward CardData lookup failed.");
            CardEffectData[] effects = cover.GetEffects().ToArray();
            if (cover.name != "CoverOfNight" || cover.GetCardType() != CardType.Spell || cover.GetCost() != 1 ||
                effects.Length != 3 || effects[0].GetEffectStateName() != "CardEffectSetMoonPhase" || effects[0].GetParamInt() != 1 ||
                effects[1].GetEffectStateName() != "CardEffectAddTempCardUpgradeToUnits" ||
                effects[2].GetEffectStateName() != "CardEffectAddStatusEffect" ||
                effects[2].GetParamStatusEffects().Single().statusId != "stealth" || effects[2].GetParamStatusEffects().Single().count != 1 ||
                moonrise.GetEffects().Count != 2 || moonrise.GetEffects()[0].GetEffectStateName() != "CardEffectSetMoonPhase" ||
                moonrise.GetEffects()[0].GetParamInt() != 2 || moonrise.GetEffects()[1].GetEffectStateName() != "CardEffectGainEnergy")
                throw new InvalidOperationException("Original Cover of Night or Moonrise control definition changed.");

            CharacterData unit = steward.GetSpawnCharacterData() ?? throw new InvalidOperationException("Train Steward has no spawn character.");
            Set(steward, "cost", 0);
            Set(unit, "attackDamage", 60);
            Set(unit, "health", 500);
            Set(unit, "size", 1);
            Set(unit, "startingStatusEffects", Array.Empty<StatusEffectStackData>());
            foreach (CardState existing in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID()))
                existing.Setup(steward, save);
            foreach (CardState existing in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(existing);
            cards.GetDrawPile().Clear();
            cards.GetDiscardPile().Clear();
            cards.GetExhaustedPile().Clear();

            CardState firstFloor = cards.AddNewCard(steward, CardPile.HandPile, false, false, animate: false);
            CardState controlFloor = cards.AddNewCard(steward, CardPile.HandPile, false, false, animate: false);
            CardState phaseControl = cards.AddNewCard(moonrise, CardPile.HandPile, false, false, animate: false);
            CardState spell = cards.AddNewCard(cover, CardPile.HandPile, false, false, animate: false);
            CardState[] created = { firstFloor, controlFloor, phaseControl, spell };
            if (created.Any(card => card == null || !cards.GetHand().Contains(card)))
                throw new InvalidOperationException("Original Cover of Night scenario could not add its controlled cards.");
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            bool tracking = (bool)AccessTools.Field(typeof(PlayerManager), "isMoonPhaseTrackingActive").GetValue(managers.GetPlayerManager());
            if (!tracking) throw new InvalidOperationException("Original Moonrise did not activate moon-phase tracking.");
            Prepared = true;
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-COVER-OF-NIGHT-PREPARED original two-floor units; Moonrise sets Full before Cover of Night sets New, upgrades one floor and grants Stealth.");
        }

        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            CombatContext context = source.Spawn.Train.Context!;
            for (int index = 0; index < ids.Length; index++)
            {
                if (context.Statistics!.Value(ids[index], "TimesPlayed", "ThisBattle") != 0 ||
                    !context.Cards.Hand.Any(card => card.InstanceId == ids[index])) continue;
                int room = index switch { 0 => 0, 1 => 1, _ => 0 };
                var action = new PlayCardAction(ids[index], room, playerPosition: index < 2 ? 0 : -1);
                if (BattleActionModel.PlayCard(source, action).Supported) return action;
            }
            return null;
        }

        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
