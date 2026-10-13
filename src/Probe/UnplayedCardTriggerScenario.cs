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
        internal const string CardId = "2785cc8e-d0e8-46c0-84ab-9c1d602525d4";
        internal const string AssetKey = "ScourgeT2";
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
            CardData data = save.GetAllGameData().FindCardData(CardId)
                ?? throw new InvalidOperationException("Original ScourgeT2 definition is missing.");
            VerifyDefinition(data);
            if (cards.GetHand().Any(card => card.GetCardDataID() == CardId))
                throw new InvalidOperationException("Original ScourgeT2 is already in the initial hand.");
            CardState? added = cards.AddNewCard(data, CardPile.HandPile, false, false, animate: false);
            if (added == null || !cards.GetHand().Contains(added) || !added.TriggersOnUnplayed())
                throw new InvalidOperationException("Original ScourgeT2 did not enter the hand with its unplayed trigger.");
            Prepared = true;
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(combat, true);
            log.LogInfo("UNPLAYED-CARD-TRIGGER-PREPARED original=" + AssetKey + " id=" + CardId +
                " inHand=true damage=3 target=Pyre.");
        }

        internal static PlayCardAction? AvoidPlaying(BattleTurnState state, PlayCardAction? selected)
        {
            if (!Enabled || !Prepared || selected == null || state.Spawn.Train.Context == null ||
                state.Spawn.Train.Context.FindCard(selected.CardInstanceId)?.DataId != CardId)
                return selected;
            return BattleActionModel.EnumerateSupportedPlays(state).FirstOrDefault(action =>
                state.Spawn.Train.Context.FindCard(action.CardInstanceId)?.DataId != CardId);
        }

        private static void VerifyDefinition(CardData data)
        {
            CardTriggerEffectData[] triggers = data.GetCardTriggers().Where(trigger =>
                trigger.GetTrigger().ToString() == "OnUnplayedNegative").ToArray();
            CardEffectData[] effects = triggers.SelectMany(trigger => trigger.GetCardEffects()).ToArray();
            if (data.GetID() != CardId || data.name != AssetKey || triggers.Length != 1 ||
                triggers[0].GetTriggerEffects().Count != 0 || effects.Length != 1 ||
                effects[0].GetEffectStateName() != "CardEffectDamage" ||
                effects[0].GetTargetMode().ToString() != "Pyre" || effects[0].GetParamInt() != 3 ||
                effects[0].GetUseIntRange() || !data.GetTraits().Any(trait =>
                    trait.GetTraitStateName() == "CardTraitSelfPurge"))
                throw new InvalidOperationException("Original ScourgeT2 trigger definition changed.");
        }
    }
}
