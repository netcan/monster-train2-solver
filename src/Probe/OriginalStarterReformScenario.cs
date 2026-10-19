using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalStarterReformScenario
    {
        internal const string Name = "original-starter-reform";
        internal const string CardId = "fa394e19-4279-4e43-b0b3-75da045fc1eb";
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
                throw new InvalidOperationException("Original Starter Reform CardData lookup failed: " + CardId);
            IReadOnlyList<CardEffectData> effects = spell.GetEffects();
            CardTraitData[] traits = spell.GetTraits().ToArray();
            CardUpgradeData? upgrade = effects.Count == 1 ? effects[0].GetParamCardUpgradeData() : null;
            var upgradeState = new CardUpgradeState();
            if (upgrade != null) upgradeState.Setup(upgrade);
            CardUpgradeModifier? projectedUpgrade = upgrade == null ? null : CardModifierProbe.Upgrade(upgradeState);
            if (spell.name != "StarterReform" || spell.GetCardType() != CardType.Spell || spell.GetCost() != 1 ||
                effects.Count != 1 || effects[0].GetEffectStateName() != "CardEffectRecursion" ||
                effects[0].GetTargetMode() != TargetMode.Exhaust || effects[0].GetTargetCardSelectionMode() != CardEffectData.CardSelectionMode.RandomToHand ||
                effects[0].GetTargetCardType() != CardType.Monster || effects[0].GetParamInt() != 1 ||
                upgrade?.GetID() != RaiseId || upgrade.name != "Raise" || projectedUpgrade?.Stats.Damage != 5 ||
                projectedUpgrade.Stats.Health != 5 || projectedUpgrade.Stats.Cost != -99 ||
                !projectedUpgrade.Statuses.Any(status => status.Id == "ephemeral" && status.Stacks == 1) ||
                traits.Length != 1 || traits[0].GetTraitStateName() != "CardTraitShowCardTargets" ||
                traits[0].GetParamInt() != 0 || traits[0].GetParamInt2() != 0 || traits[0].GetParamInt3() != 0 ||
                traits[0].GetParamBool() || traits[0].GetParamStr() != "" || traits[0].GetParamFloat() != 0 ||
                traits[0].GetUseScalingParams() || traits[0].GetParamTrackedValue().ToString() != "SubtypeInDeck" ||
                traits[0].GetParamEntryDuration().ToString() != "ThisTurn" || spell.GetCardTriggers().Count != 0)
                throw new InvalidOperationException("Original Starter Reform definition changed.");

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
            CardState target = cards.AddNewCard(steward, CardPile.HandPile, false, false, animate: false);
            CardState reform = cards.AddNewCard(spell, CardPile.HandPile, false, false, animate: false);
            if (anchor == null || target == null || reform == null || !cards.GetHand().Contains(anchor) ||
                !cards.GetHand().Contains(target) || !cards.GetHand().Contains(reform))
                throw new InvalidOperationException("Original Starter Reform scenario could not add its controlled cards.");
            cards.MoveToStandByPile(target, wasPlayed: false, wasExhausted: true,
                condition: new RemoveFromStandByCondition(() => CardPile.KeepInStandBy));
            if (!cards.GetExhaustedPile().Contains(target) || cards.GetExhaustedPile().Count != 1)
                throw new InvalidOperationException("Starter Reform requires exactly one controlled Monster in Exhaust.");
            CardIds = new[] { anchor, reform, target }.Select(FullBattleTrace.Active!.CardId).ToArray();

            save.SetForgePoints(0);
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true;
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-STARTER-REFORM-PREPARED unchanged one-cost RandomToHand Monster recursion, one controlled Exhaust target and original Raise upgrade.");
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
                var cast = new PlayCardAction(ids[1], 0);
                if (BattleActionModel.PlayCard(source, cast).Supported) return cast;
            }
            return null;
        }

        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
