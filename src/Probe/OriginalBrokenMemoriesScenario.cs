using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalBrokenMemoriesScenario
    {
        internal const string Name = "original-broken-memories";
        internal const string CardId = "f954f70d-e83b-4c64-9827-89e5770a56ef";
        internal const string SpellTargetId = "dea99a2f-48c9-4246-ad44-01e84f46171e";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            AllGameData all = save.GetAllGameData();
            CardManager cards = managers.GetCardManager()!;
            CardData brokenMemories = all.FindCardData(CardId)!;
            CardEffectData effect = brokenMemories.GetEffects().Single();
            if (brokenMemories.name != "BrokenMemories" || brokenMemories.GetCost() != 0 ||
                effect.GetEffectStateName() != "CardEffectRecursion" || effect.GetTargetMode() != TargetMode.Exhaust ||
                effect.GetTargetCardSelectionMode() != CardEffectData.CardSelectionMode.ChooseToDeck ||
                effect.GetTargetCardType() != CardType.Spell || effect.GetParamInt() != 0 ||
                !brokenMemories.GetTraits().Any(trait => trait.GetTraitStateName() == "CardTraitExhaustState"))
                throw new InvalidOperationException("Original Broken Memories definition changed.");

            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!;
            CharacterData unit = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(unit, "attackDamage", 9999); Set(unit, "health", 9999); Set(unit, "size", 1);
            Set(unit, "startingStatusEffects", Array.Empty<StatusEffectStackData>());
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID()))
                card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            cards.GetDrawPile().Clear();

            CardState Add(string id) => cards.AddNewCard(all.FindCardData(id)!, CardPile.HandPile, false, false, animate: false);
            CardState anchor = Add(steward.GetID());
            CardState spell = Add(CardId);
            CardState spellTarget = Add(SpellTargetId);
            CardState monsterControl = Add(steward.GetID());
            cards.MoveToStandByPile(spellTarget, wasPlayed: false, wasExhausted: true,
                condition: new RemoveFromStandByCondition(() => CardPile.KeepInStandBy));
            cards.MoveToStandByPile(monsterControl, wasPlayed: false, wasExhausted: true,
                condition: new RemoveFromStandByCondition(() => CardPile.KeepInStandBy));
            if (!cards.GetExhaustedPile().Contains(spellTarget) || !cards.GetExhaustedPile().Contains(monsterControl))
                throw new InvalidOperationException("Original Broken Memories fixture could not seed its controlled Exhaust targets.");

            CardIds = new[] { anchor, spell, spellTarget, monsterControl }.Select(FullBattleTrace.Active!.CardId).ToArray();
            save.SetForgePoints(0);
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true;
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-BROKEN-MEMORIES-PREPARED unchanged zero-cost ChooseToDeck card, one Exhausted Spell and Monster filter control.");
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
                context.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(card => card.InstanceId == ids[2]))
            {
                var cast = new PlayCardAction(ids[1], 0,
                    recursionSelections: [new CardRecursionSelection(0, [ids[2]])]);
                if (BattleActionModel.PlayCard(source, cast).Supported) return cast;
            }
            return BattleActionModel.ChooseUnitSpellAndJunkPlay(source,
                card => card.InstanceId != ids[1] && card.InstanceId != ids[2]);
        }

        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
