using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalRapidRaiseScenario
    {
        internal const string Name = "original-rapid-raise";
        internal const string CardId = "5b88c130-70b3-42cc-9903-4d245b4f946f";
        internal const string RaiseId = "8e99fcbd-5415-4bc9-9133-7131b0fb33a6";
        private const string StewardId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            AllGameData all = save.GetAllGameData();
            CardManager cards = managers.GetCardManager()!;
            CardData spell = all.FindCardData(CardId) ??
                throw new InvalidOperationException("Original Rapid Raise CardData lookup failed: " + CardId);
            IReadOnlyList<CardEffectData> effects = spell.GetEffects();
            CardTraitData[] traits = spell.GetTraits().ToArray();
            CardUpgradeData? upgrade = effects.Count == 1 ? effects[0].GetParamCardUpgradeData() : null;
            var upgradeState = new CardUpgradeState();
            if (upgrade != null) upgradeState.Setup(upgrade);
            CardUpgradeModifier? projectedUpgrade = upgrade == null ? null : CardModifierProbe.Upgrade(upgradeState);
            if (spell.name != "RapidRaise" || spell.GetCardType() != CardType.Spell || spell.GetCost() != 0 ||
                effects.Count != 1 || effects[0].GetEffectStateName() != "CardEffectRecursion" ||
                effects[0].GetTargetMode() != TargetMode.Exhaust || effects[0].GetTargetCardSelectionMode() != CardEffectData.CardSelectionMode.RandomToHand ||
                effects[0].GetTargetCardType() != CardType.Monster || effects[0].GetParamInt() != 2 ||
                upgrade?.GetID() != RaiseId || upgrade.name != "Raise" || projectedUpgrade?.Stats.Damage != 5 ||
                projectedUpgrade.Stats.Health != 5 || projectedUpgrade.Stats.Cost != -99 ||
                !projectedUpgrade.Statuses.Any(status => status.Id == "ephemeral" && status.Stacks == 1) ||
                traits.Length != 2 || traits[0].GetTraitStateName() != "CardTraitShowCardTargets" ||
                traits[1].GetTraitStateName() != "CardTraitExhaustState" ||
                traits.Any(trait => trait.GetParamInt() != 0 || trait.GetParamInt2() != 0 || trait.GetParamInt3() != 0 ||
                    trait.GetParamBool() || trait.GetParamStr() != "" || trait.GetParamFloat() != 0 || trait.GetUseScalingParams() ||
                    trait.GetParamTrackedValue().ToString() != "SubtypeInDeck" || trait.GetParamEntryDuration().ToString() != "ThisTurn") ||
                spell.GetCardTriggers().Count != 0)
                throw new InvalidOperationException("Original Rapid Raise definition changed.");

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
            cards.GetDiscardPile().Clear();
            cards.GetExhaustedPile().Clear();

            CardState anchor = cards.AddNewCard(steward, CardPile.HandPile, false, false, animate: false);
            CardState firstTarget = cards.AddNewCard(steward, CardPile.HandPile, false, false, animate: false);
            CardState secondTarget = cards.AddNewCard(steward, CardPile.HandPile, false, false, animate: false);
            CardState rapidRaise = cards.AddNewCard(spell, CardPile.HandPile, false, false, animate: false);
            if (anchor == null || firstTarget == null || secondTarget == null || rapidRaise == null ||
                !cards.GetHand().Contains(anchor) || !cards.GetHand().Contains(firstTarget) ||
                !cards.GetHand().Contains(secondTarget) || !cards.GetHand().Contains(rapidRaise))
                throw new InvalidOperationException("Original Rapid Raise scenario could not add its controlled cards.");
            foreach (CardState target in new[] { firstTarget, secondTarget })
                cards.MoveToStandByPile(target, wasPlayed: false, wasExhausted: true,
                    condition: new RemoveFromStandByCondition(() => CardPile.KeepInStandBy));
            if (!cards.GetExhaustedPile().Contains(firstTarget) || !cards.GetExhaustedPile().Contains(secondTarget) ||
                cards.GetExhaustedPile().Count != 2)
                throw new InvalidOperationException("Rapid Raise requires exactly two controlled Monsters in Exhaust.");
            CardIds = new[] { anchor, rapidRaise, firstTarget, secondTarget }.Select(FullBattleTrace.Active!.CardId).ToArray();

            save.SetForgePoints(0);
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true;
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-RAPID-RAISE-PREPARED unchanged zero-cost RandomToHand recursion of two Exhausted Monsters with original Raise upgrade.");
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
                context.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(card => card.InstanceId == ids[2]) &&
                context.OtherPiles.Single(pile => pile.Name == "Exhausted").Cards.Any(card => card.InstanceId == ids[3]))
            {
                var cast = new PlayCardAction(ids[1], 0);
                if (BattleActionModel.PlayCard(source, cast).Supported) return cast;
            }
            return null;
        }

        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
