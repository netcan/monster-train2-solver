using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalPyreGroScenario
    {
        internal const string Name = "original-pyre-gro";
        internal const string CardId = "2ba0f9eb-744e-4b3c-af4e-3ad39ae8a74f";
        private const string StewardId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            AllGameData all = save.GetAllGameData();
            CardManager cards = managers.GetCardManager()!;
            CardData pyreGro = all.FindCardData(CardId) ??
                throw new InvalidOperationException("Original PyreGro CardData lookup failed: " + CardId);
            IReadOnlyList<CardEffectData> effects = pyreGro.GetEffects();
            if (pyreGro.name != "PyreGro" || pyreGro.GetCardType() != CardType.Spell || pyreGro.GetCost() != 0 ||
                effects.Count != 2 || effects[0].GetEffectStateName() != "CardEffectDrawAdditionalNextTurn" ||
                effects[0].GetTargetMode().ToString() != "Room" || effects[0].GetParamInt() != -2 ||
                effects[1].GetEffectStateName() != "CardEffectGainEnergyEveryTurn" ||
                effects[1].GetTargetMode().ToString() != "Room" || effects[1].GetParamInt() != 1 ||
                !pyreGro.GetTraits().Select(trait => trait.GetTraitStateName()).SequenceEqual(new[] { "CardTraitExhaustState" }) ||
                pyreGro.GetCardTriggers().Count != 0)
                throw new InvalidOperationException("Original PyreGro definition changed.");

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
            CardState spell = cards.AddNewCard(pyreGro, CardPile.HandPile, false, false, animate: false);
            if (anchor == null || spell == null || !cards.GetHand().Contains(anchor) || !cards.GetHand().Contains(spell))
                throw new InvalidOperationException("Original PyreGro scenario could not add its controlled cards.");
            CardIds = new[] { anchor, spell }.Select(FullBattleTrace.Active!.CardId).ToArray();

            save.SetForgePoints(0);
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true;
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-PYRE-GRO-PREPARED unchanged zero-cost spell, next-turn draw -2, recurring energy +1 and Consume.");
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
                context.Cards.Hand.Any(item => item.InstanceId == ids[1]))
            {
                var cast = new PlayCardAction(ids[1], 0);
                if (BattleActionModel.PlayCard(source, cast).Supported) return cast;
            }
            return null;
        }

        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
