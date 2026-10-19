using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalWaxenSpikeScenario
    {
        internal const string Name = "original-waxen-spike";
        internal const string CardId = "8d291f9a-30a1-4a6d-8d9e-6dc2df4432d1";
        private const string StewardId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            AllGameData all = save.GetAllGameData();
            CardManager cards = managers.GetCardManager()!;
            CardData waxen = all.FindCardData(CardId) ?? throw new InvalidOperationException("Original Waxen Spike CardData lookup failed.");
            CardTraitData[] traits = waxen.GetTraits().ToArray();
            CardTraitData scaling = traits.SingleOrDefault(trait => trait.GetTraitStateName() == "CardTraitScalingUpgradeUnitAttack")!;
            CardEffectData[] effects = waxen.GetEffects().ToArray();
            StatusEffectStackData[] ephemeral = effects.ElementAtOrDefault(1)?.GetParamStatusEffects() ?? Array.Empty<StatusEffectStackData>();
            if (waxen.name != "WaxenSpike" || waxen.GetCardType() != CardType.Spell || waxen.GetCost() != 0 ||
                waxen.GetCostType() != CardData.CostType.ConsumeRemainingEnergy || effects.Length != 2 || scaling == null ||
                scaling.GetParamInt() != 4 || scaling.GetParamFloat() != 1 || !scaling.GetUseScalingParams() ||
                scaling.GetParamTrackedValue().ToString() != "PlayedCost" || scaling.GetParamEntryDuration().ToString() != "ThisBattle" ||
                effects[0].GetEffectStateName() != "CardEffectAddTempCardUpgradeToUnits" || effects[0].GetTargetMode() != TargetMode.Room ||
                effects[1].GetEffectStateName() != "CardEffectAddStatusEffect" || effects[1].GetTargetMode() != TargetMode.Room ||
                ephemeral.Length != 1 || ephemeral[0].statusId != "ephemeral" || ephemeral[0].count != 4)
                throw new InvalidOperationException("Original Waxen Spike X-cost effects or source scaling changed.");

            CardData steward = all.FindCardData(StewardId) ?? throw new InvalidOperationException("Train Steward CardData lookup failed.");
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

            CardState target = cards.AddNewCard(steward, CardPile.HandPile, false, false, animate: false);
            CardState control = cards.AddNewCard(steward, CardPile.HandPile, false, false, animate: false);
            CardState spell = cards.AddNewCard(waxen, CardPile.HandPile, false, false, animate: false);
            CardState[] created = { target, control, spell };
            if (created.Any(card => card == null || !cards.GetHand().Contains(card)))
                throw new InvalidOperationException("Original Waxen Spike scenario could not add its controlled cards.");
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true;
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-WAXEN-SPIKE-PREPARED two-floor original Train Stewards; unchanged Waxen Spike consumes all remaining Ember for its unit upgrade and Ephemeral effect.");
        }

        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            CombatContext context = source.Spawn.Train.Context!;
            for (int index = 0; index < ids.Length; index++)
            {
                if (context.Statistics!.Value(ids[index], "TimesPlayed", "ThisBattle") != 0 ||
                    !context.Cards.Hand.Any(card => card.InstanceId == ids[index])) continue;
                int room = index == 1 ? 1 : 0;
                var action = new PlayCardAction(ids[index], room, playerPosition: index < 2 ? 0 : -1);
                if (BattleActionModel.PlayCard(source, action).Supported) return action;
            }
            return null;
        }

        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
