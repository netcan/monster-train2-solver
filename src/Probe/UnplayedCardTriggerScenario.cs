using System;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class UnplayedCardTriggerScenario
    {
        internal const string Name = "unplayed-card-trigger";
        private static readonly (string Id, string AssetKey, int Damage)[] Cards =
        {
            ("186180e9-94e9-47d0-b69c-865b9a6d27a4", "BlightPyreDamageInHand", 1),
            ("2785cc8e-d0e8-46c0-84ab-9c1d602525d4", "ScourgeT2", 3),
            ("3b603b7f-300c-4022-9fc2-275448446e58", "TFB_EntropyScourge", 3),
            ("913694e9-bace-4480-8691-24f699ae583e", "DantesCandle", 3),
            ("fbf74ec4-2781-4b99-b6f7-b7d75236da60", "ScourgeT3", 5)
        };
        internal static int ExpectedDamage => Cards.Sum(card => card.Damage);
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (Prepared) return;
            CombatManager combat = managers.GetCombatManager()!;
            SaveManager save = managers.GetSaveManager();
            CardManager cards = managers.GetCardManager()!;
            if (save.PreviewMode || combat.GetTurnCount() != 0)
                throw new InvalidOperationException("Unplayed-card trigger scenario must start on deployment turn.");
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
            Prepared = true;
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(combat, true);
            log.LogInfo("UNPLAYED-CARD-TRIGGER-PREPARED originals=" + Cards.Length + " totalDamage=" + ExpectedDamage +
                " cards=" + string.Join(",", Cards.Select(card => card.AssetKey + ":" + card.Damage)) + " target=Pyre.");
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

        private static void VerifyDefinition(CardData data, (string Id, string AssetKey, int Damage) expected)
        {
            CardTriggerEffectData[] triggers = data.GetCardTriggers().Where(trigger =>
                trigger.GetTrigger().ToString() == "OnUnplayedNegative").ToArray();
            CardEffectData[] effects = triggers.SelectMany(trigger => trigger.GetCardEffects()).ToArray();
            if (data.GetID() != expected.Id || data.name != expected.AssetKey || triggers.Length != 1 ||
                triggers[0].GetTriggerEffects().Count != 0 || effects.Length != 1 ||
                effects[0].GetEffectStateName() != "CardEffectDamage" ||
                effects[0].GetTargetMode().ToString() != "Pyre" || effects[0].GetParamInt() != expected.Damage ||
                effects[0].GetUseIntRange())
                throw new InvalidOperationException("Original " + expected.AssetKey + " trigger definition changed.");
        }
    }
}
