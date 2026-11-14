using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class CelebrateHealthBattleScenario
    {
        internal const string Name = "celebrate-health-battle";
        internal const string OwnerId = "6d77addd-17ad-43d1-a6b6-56bb39d78d82";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        private static int[] cards = Array.Empty<int>();
        private static int step;
        private static object? definition, calibration;
        private static ManualLogSource? logger;
        internal static object Evidence => new { Prepared, CompletedControlPlays = step, ControlCardIds = cards,
            OwnerDefinition = definition, Calibration = calibration };

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            var save = managers.GetSaveManager(); var all = managers.GetAllGameData(); var manager = managers.GetCardManager();
            if (!ConditionalActivationProbe.HasVanguard || save.PreviewMode || managers.GetCombatManager().GetTurnCount() != 0 ||
                save.GetCollectedRelics().Any(owner => owner.GetRelicDataID() == OwnerId))
                throw new InvalidOperationException("Celebrate health requires isolated 2.3.1 deployment.");
            RelicData owner = all.GetAllCollectableRelicData().Single(relic => relic.GetID() == OwnerId);
            RelicEffectData effect = owner.GetEffects().Single();
            var interactions = new List<string>();
            definition = new { Id = owner.GetID(), Key = owner.name, Placement = owner.DisallowInDeploymentPhase,
                Type = effect.GetEffectClassName(), Int = effect.GetParamInt(), NoCountModifiers = effect.GetParamBool(),
                Triggers = effect.GetTriggers().Select(trigger => EnemySpawningProbe.TriggerDefinition(trigger, interactions)).ToArray(),
                Interactions = interactions.ToArray() };
            if (owner.name != "DamagedMaxHealthOnCelebrate" || effect.GetEffectClassName() != RelicModel.AddTrigger ||
                effect.GetParamBool() || effect.GetEffectConditions().Count != 0 || interactions.Count != 0)
                throw new InvalidOperationException("Changed original celebrate-health owner.");
            CardData steward = all.FindCardData("d14a50f3-728d-43e1-87f0-ef1b013f6678");
            CharacterData unit = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(unit, "health", 100); Set(unit, "attackDamage", 60); Set(unit, "size", 1);
            Set(unit, "triggers", new List<CharacterTriggerData>()); Set(unit, "startingStatusEffects", Array.Empty<StatusEffectStackData>());
            Set(unit, "unitAbility", null!);
            CardData template = all.FindCardData(DevotionSpawnBattleScenario.VenerateId);
            CardData Control(int index, string type, int amount)
            {
                CardData card = UnityEngine.Object.Instantiate(template); card.name = "PojuCelebrateControl" + index;
                Set(card, "id", "fc231c60-0000-4000-8000-00000000000" + index); Set(card, "cost", 0);
                Set(card, "targetless", false); Set(card, "targetsRoom", true);
                var action = new CardEffectData(type, null!, Team.Type.Monsters);
                action.Cheat_SetTargetMode(TargetMode.DropTargetCharacter); Set(action, "paramInt", amount);
                if (type == "CardEffectAddStatusEffect")
                    Set(action, "paramStatusEffects", new[] { new StatusEffectStackData { statusId = "silenced", count = 1 } });
                Set(card, "effects", new List<CardEffectData> { action }); Set(card, "traits", new List<CardTraitData>());
                Set(card, "triggers", new List<CardTriggerEffectData>()); Set(card, "effectTriggers", new List<CharacterTriggerData>());
                Set(card, "startingUpgrades", new List<CardUpgradeData>());
                ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(card);
                return card;
            }
            CardData damage = Control(1, "CardEffectDamage", 13), silence = Control(2, "CardEffectAddStatusEffect", 0),
                sacrifice = Control(3, "CardEffectSacrifice", 0);
            foreach (CardState card in manager.GetHand().ToArray()) manager.MoveCardFromHandToTopOfDeck(card);
            manager.GetDrawPile().Clear(); save.AddRelic(owner);
            cards = new[] { steward, steward, damage, silence, steward, steward, damage, steward, sacrifice }
                .Select(data => manager.AddNewCard(data, CardPile.HandPile, false, false, animate: false) ??
                    throw new InvalidOperationException("Unable to add celebrate-health input.")).Select(FullBattleTrace.Active!.CardId).ToArray();
            managers.GetCardStatistics().GetStatValue(new CardStatistics.StatValueData { trackedValue = CardStatistics.TrackedValueType.TimesPlayed,
                cardState = manager.GetHand()[0], entryDuration = CardStatistics.EntryDuration.ThisBattle, cardTypeTarget = CardStatistics.CardTypeTarget.Any });
            FullBattleTrace.Active!.InvalidateCardRules(); Prepared = true; logger = log;
            Set(managers.GetCombatManager(), "combatStateChanged", true);
            log.LogInfo("CELEBRATE-HEALTH-PREPARED unchanged original owner; wounded, silenced, healthy and sacrificed carriers on three floors.");
        }
        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            if (step >= cards.Length) return null;
            if (step == 1 && calibration == null) calibration = SelfHealthTargetCalibration.Run(AllGameManagers.Instance!);
            int room = step < 4 ? 0 : step is 4 or 7 or 8 ? 1 : 2;
            bool summon = step is 0 or 1 or 4 or 5 or 7;
            int carrier = step is 2 or 3 ? 1 : step == 6 ? 5 : 7;
            int target = summon ? 0 : state.Spawn.Train.Rooms[room].Units.Single(unit => unit.SpawnerCardId == cards[carrier]).Id;
            var action = new PlayCardAction(cards[step], room, playerPosition: summon ? (step is 1 or 7 ? 1 : 0) : -1, targetUnitId: target);
            BattleActionResult result = BattleActionModel.PlayCard(state, action);
            if (!result.Supported) { logger?.LogInfo("CELEBRATE-HEALTH-REJECT " + step + ": " + result.Reason); return null; }
            step++; return action;
        }
        internal static bool IsScenarioAction(PlayCardAction action) => cards.Contains(action.CardInstanceId);
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
