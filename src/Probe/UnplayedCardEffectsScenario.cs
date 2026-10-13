using System;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class UnplayedCardEffectsScenario
    {
        internal const string Name = "unplayed-card-effects";
        private static readonly (string Id, string AssetKey, string Effect)[] Cards =
        {
            ("17fef621-2bb7-4847-b41d-52a89795c784", "TitanCurse", "CardEffectAddTempCardUpgradeToSpellsInDeck"),
            ("4d1f27af-3122-4a08-87e3-5ef5a14ced94", "Scourge_SoulSavior", "CardEffectAddUpgradedCopy")
        };

        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (Prepared) return;
            CombatManager combat = managers.GetCombatManager()!;
            SaveManager save = managers.GetSaveManager();
            CardManager cards = managers.GetCardManager()!;
            if (save.PreviewMode || combat.GetTurnCount() != 0)
                throw new InvalidOperationException("Unplayed-card effects scenario must start on deployment turn.");
            foreach (var item in Cards)
            {
                CardData data = save.GetAllGameData().FindCardData(item.Id)
                    ?? throw new InvalidOperationException("Original " + item.AssetKey + " definition is missing.");
                VerifyDefinition(data, item);
                if (cards.GetHand().Any(card => card.GetCardDataID() == item.Id))
                    throw new InvalidOperationException("Original " + item.AssetKey + " is already in the initial hand.");
                CardState? added = cards.AddNewCard(data, CardPile.HandPile, false, false, animate: false);
                if (added == null || !cards.GetHand().Contains(added) || !added.TriggersOnUnplayed())
                    throw new InvalidOperationException("Original " + item.AssetKey + " did not enter the hand with its unplayed trigger.");
            }
            CardState soulSavior = cards.GetHand().Single(card => card.GetCardDataID() == Cards[1].Id);
            if (!soulSavior.CanBeCopied())
                throw new InvalidOperationException("Scourge_SoulSavior is not copyable in the native setup.");
            Prepared = true;
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(combat, true);
            log.LogInfo("UNPLAYED-CARD-EFFECTS-PREPARED originals=" + Cards.Length + " cards=" +
                string.Join(",", Cards.Select(card => card.AssetKey)) + " soulSaviorCopyable=true.");
        }

        internal static PlayCardAction? AvoidPlaying(BattleTurnState state, PlayCardAction? selected)
        {
            if (!Enabled || !Prepared || selected == null || state.Spawn.Train.Context == null)
                return selected;
            string? selectedId = state.Spawn.Train.Context.FindCard(selected.CardInstanceId)?.DataId;
            if (!Cards.Any(card => card.Id == selectedId)) return selected;
            return BattleActionModel.EnumerateSupportedPlays(state).FirstOrDefault(action =>
                !Cards.Any(card => card.Id == state.Spawn.Train.Context.FindCard(action.CardInstanceId)?.DataId));
        }

        private static void VerifyDefinition(CardData data, (string Id, string AssetKey, string Effect) expected)
        {
            CardTriggerEffectData[] triggers = data.GetCardTriggers().Where(trigger =>
                trigger.GetTrigger().ToString() == "OnUnplayedNegative").ToArray();
            CardEffectData[] effects = triggers.SelectMany(trigger => trigger.GetCardEffects()).ToArray();
            if (data.GetID() != expected.Id || data.name != expected.AssetKey || triggers.Length != 1 ||
                triggers[0].GetTriggerEffects().Count != 0 || effects.Length != 1 ||
                effects[0].GetEffectStateName() != expected.Effect)
                throw new InvalidOperationException("Original " + expected.AssetKey + " trigger definition changed.");
        }
    }
}
