using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalWickedBlazeScenario
    {
        internal const string Name = "original-wicked-blaze";
        internal const string CardId = "d930a4fe-1419-4c21-9ef2-fda4e25ee90b";
        internal const string SpellControlId = "f954f70d-e83b-4c64-9827-89e5770a56ef";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            AllGameData all = save.GetAllGameData();
            CardManager cards = managers.GetCardManager()!;
            CardData wickedBlaze = all.FindCardData(CardId);
            if (wickedBlaze == null) throw new InvalidOperationException("Original Wicked Blaze CardData lookup failed: " + CardId);
            IReadOnlyList<CardEffectData> effects = wickedBlaze.GetEffects();
            if (effects == null || effects.Count != 1) throw new InvalidOperationException("Original Wicked Blaze must have exactly one native effect.");
            CardEffectData effect = effects[0];
            if (effect == null) throw new InvalidOperationException("Original Wicked Blaze native effect lookup failed.");
            CardUpgradeData upgrade = effect.GetParamCardUpgradeData();
            if (wickedBlaze.name != "WickedBlaze" || wickedBlaze.GetCost() != 2 ||
                effect.GetEffectStateName() != "CardEffectRecursion" || effect.GetTargetMode() != TargetMode.Exhaust ||
                effect.GetTargetCardSelectionMode() != CardEffectData.CardSelectionMode.ChooseToHand ||
                effect.GetTargetCardType() != CardType.Monster || upgrade == null || upgrade.name != "RaisePlusWickedBlaze")
                throw new InvalidOperationException("Original Wicked Blaze definition changed.");

            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId);
            if (steward == null) throw new InvalidOperationException("Train Steward CardData lookup failed.");
            CharacterData unit = steward.GetSpawnCharacterData();
            if (unit == null) throw new InvalidOperationException("Train Steward spawn data lookup failed.");
            Set(steward, "cost", 0); Set(unit, "attackDamage", 9999); Set(unit, "health", 9999); Set(unit, "size", 1);
            Set(unit, "startingStatusEffects", Array.Empty<StatusEffectStackData>());
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID()))
                card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            cards.GetDrawPile().Clear();

            CardState Add(string id) => cards.AddNewCard(all.FindCardData(id)!, CardPile.HandPile, false, false, animate: false);
            CardState anchor = Add(steward.GetID());
            CardState spell = Add(CardId);
            CardState monsterTarget = Add(steward.GetID());
            CardState spellControl = Add(SpellControlId);
            cards.MoveToStandByPile(monsterTarget, wasPlayed: false, wasExhausted: true,
                condition: new RemoveFromStandByCondition(() => CardPile.KeepInStandBy));
            cards.MoveToStandByPile(spellControl, wasPlayed: false, wasExhausted: true,
                condition: new RemoveFromStandByCondition(() => CardPile.KeepInStandBy));
            if (!cards.GetExhaustedPile().Contains(monsterTarget) || !cards.GetExhaustedPile().Contains(spellControl))
                throw new InvalidOperationException("Original Wicked Blaze fixture could not seed its controlled Exhaust targets.");

            CardIds = new[] { anchor, spell, monsterTarget, spellControl }.Select(FullBattleTrace.Active!.CardId).ToArray();
            save.SetForgePoints(0);
            managers.GetPlayerManager().AddEnergy(2);
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true;
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-WICKED-BLAZE-PREPARED unchanged 2-cost card, one Exhausted Monster, Spell filter control and RaisePlusWickedBlaze upgrade.");
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
