using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class ChainedStatusScenario
    {
        internal const string Name = "chained-status-calibration";
        internal const string OwnerId = "d0294a77-7204-4e9e-9b22-7f38e0d59ad0";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        private static int cardId;
        private static object? definition, calibration;
        internal static object Evidence => new { Prepared, OwnerDefinition = definition, Calibration = calibration };
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            var save = managers.GetSaveManager(); var all = managers.GetAllGameData(); var cards = managers.GetCardManager();
            if (!Enum.GetNames(typeof(Damage.Type)).Contains("Chained") || save.PreviewMode || managers.GetCombatManager().GetTurnCount() != 0)
                throw new InvalidOperationException("Chained calibration requires isolated 2.3.1 deployment.");
            var owner = all.GetAllCollectableRelicData().Single(relic => relic.GetID() == OwnerId);
            var effect = owner.GetEffects().Single();
            definition = new { Id = owner.GetID(), Key = owner.name, Placement = owner.DisallowInDeploymentPhase,
                Type = effect.GetEffectClassName(), Team = effect.GetParamSourceTeam().ToString(),
                Statuses = effect.GetParamStatusEffects().Select(status => new { status.statusId, status.count }).ToArray() };
            if (owner.name != "ChainedDamageIncrease" || effect.GetEffectClassName() != RelicModel.AddStatusOnSpawn ||
                effect.GetParamSourceTeam() != Team.Type.Heroes || effect.GetParamStatusEffects().Single().statusId != "chained" ||
                effect.GetParamStatusEffects()[0].count != 1)
                throw new InvalidOperationException("Original chained owner definition changed.");
            // Do not acquire the relic before the complete damage pipeline is
            // calibrated. Only detached status instances are exercised below.
            var steward = all.FindCardData("d14a50f3-728d-43e1-87f0-ef1b013f6678"); var unit = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(unit, "health", 100); Set(unit, "attackDamage", 60); Set(unit, "size", 1);
            Set(unit, "triggers", new List<CharacterTriggerData>()); Set(unit, "startingStatusEffects", Array.Empty<StatusEffectStackData>()); Set(unit, "unitAbility", null!);
            foreach (var card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            cards.GetDrawPile().Clear();
            cardId = FullBattleTrace.Active!.CardId(cards.AddNewCard(steward, CardPile.HandPile, false, false, animate: false) ??
                throw new InvalidOperationException("Unable to add chained calibration carrier."));
            managers.GetCardStatistics().GetStatValue(new CardStatistics.StatValueData { trackedValue = CardStatistics.TrackedValueType.TimesPlayed,
                cardState = cards.GetHand()[0], entryDuration = CardStatistics.EntryDuration.ThisBattle, cardTypeTarget = CardStatistics.CardTypeTarget.Any });
            FullBattleTrace.Active.InvalidateCardRules(); Prepared = true; Set(managers.GetCombatManager(), "combatStateChanged", true);
            log.LogInfo("CHAINED-STATUS-PREPARED original owner inspected; detached status tests and intercepted raw requests only, battle damage remains guarded.");
        }
        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            if (state.Spawn.Train.Context!.Cards.Hand.Any(card => card.InstanceId == cardId))
                return new PlayCardAction(cardId, 0, playerPosition: 0);
            if (calibration == null && state.Spawn.Turn > 0) calibration = ChainedStatusCalibration.Run(AllGameManagers.Instance!);
            return null;
        }
        internal static bool IsScenarioAction(PlayCardAction action) => action.CardInstanceId == cardId;
        private static void Set(object target, string name, object value) => AccessTools.Field(target.GetType(), name).SetValue(target, value);
    }
}
