using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalSelfPurgeEnergyGainScenario
    {
        internal const string Name = "original-self-purge-energy-gain";
        internal const string CardId = "d0e1fe8a-fb2a-44e0-9d9b-a3ddd7475c34";
        private const string StewardId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            AllGameData all = save.GetAllGameData();
            CardManager cards = managers.GetCardManager()!;
            CardData card = all.FindCardData(CardId) ??
                throw new InvalidOperationException("Original Self-Purge Energy Gain CardData lookup failed: " + CardId);
            IReadOnlyList<CardEffectData> effects = card.GetEffects();
            if (card.name != "SelfPurgeEnergyGain" || card.GetCardType() != CardType.Spell || card.GetCost() != 0 ||
                effects.Count != 2 || effects[0].GetEffectStateName() != "CardEffectGainEnergy" ||
                effects[0].GetTargetMode().ToString() != "Room" || effects[0].GetParamInt() != 2 ||
                effects[1].GetEffectStateName() != "CardEffectDraw" || effects[1].GetTargetMode().ToString() != "Room" ||
                effects[1].GetParamInt() != 1 || !card.GetTraits().Select(trait => trait.GetTraitStateName())
                    .SequenceEqual(new[] { "CardTraitExhaustState" }) || card.GetCardTriggers().Count != 0)
                throw new InvalidOperationException("Original Self-Purge Energy Gain definition changed.");

            CardData steward = all.FindCardData(StewardId) ??
                throw new InvalidOperationException("Original Train Steward CardData lookup failed: " + StewardId);
            CharacterData unit = steward.GetSpawnCharacterData() ??
                throw new InvalidOperationException("Train Steward spawn data lookup failed.");
            Set(steward, "cost", 0); Set(unit, "attackDamage", 9999); Set(unit, "health", 9999); Set(unit, "size", 1);
            Set(unit, "startingStatusEffects", Array.Empty<StatusEffectStackData>());
            foreach (CardState existing in cards.GetAllCards(new List<CardState>()).Where(item => item.GetCardDataID() == steward.GetID()))
                existing.Setup(steward, save);
            foreach (CardState existing in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(existing);
            cards.GetDrawPile().Clear();

            CardState anchor = cards.AddNewCard(steward, CardPile.HandPile, false, false, animate: false);
            CardState spell = cards.AddNewCard(card, CardPile.HandPile, false, false, animate: false);
            CardState drawn = cards.AddNewCard(steward, CardPile.DeckPile, false, false, animate: false);
            if (anchor == null || spell == null || drawn == null || !cards.GetHand().Contains(anchor) ||
                !cards.GetHand().Contains(spell) || !cards.GetDrawPile().Contains(drawn))
                throw new InvalidOperationException("Original Self-Purge Energy Gain scenario could not add its controlled cards.");
            CardIds = new[] { anchor, spell, drawn }.Select(FullBattleTrace.Active!.CardId).ToArray();

            save.SetForgePoints(0);
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true;
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-SELF-PURGE-ENERGY-GAIN-PREPARED unchanged zero-cost card, +2 energy, one controlled draw and native Consume trait.");
        }

        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            CombatContext context = source.Spawn.Train.Context!;
            if (context.Statistics!.Value(ids[0], "TimesPlayed", "ThisBattle") == 0)
            {
                var summon = new PlayCardAction(ids[0], 0, playerPosition: 0);
                if (BattleActionModel.PlayCard(source, summon).Supported) return summon;
            }
            if (context.Statistics.Value(ids[1], "TimesPlayed", "ThisBattle") == 0 &&
                context.Cards.Hand.Any(item => item.InstanceId == ids[1]) &&
                context.Cards.Draw.Any(item => item.InstanceId == ids[2]))
            {
                var cast = new PlayCardAction(ids[1], 0);
                if (BattleActionModel.PlayCard(source, cast).Supported) return cast;
            }
            return null;
        }

        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
