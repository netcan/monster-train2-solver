using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class DamagedFloorEnergyBattleScenario
    {
        internal const string Name = "damaged-floor-energy-battle";
        internal const string OwnerId = "970f699b-052e-4eec-ad98-ce32b9c5dc21";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        private static int[] cards = Array.Empty<int>();
        private static int step;
        private static object? definition, calibration;
        private static ManualLogSource? logger;
        internal static object Evidence => new { Prepared, CompletedControlPlays = step, ControlCardIds = cards,
            OwnerDefinition = definition, Calibration = calibration, Queries = DamagedFloorEnergyProbe.Queries,
            Operations = DamagedFloorEnergyProbe.Operations };
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            var save = managers.GetSaveManager(); var all = managers.GetAllGameData(); var manager = managers.GetCardManager();
            if (!ConditionalActivationProbe.HasVanguard || save.PreviewMode || managers.GetCombatManager().GetTurnCount() != 0 ||
                save.GetCollectedRelics().Any(owner => owner.GetRelicDataID() == OwnerId))
                throw new InvalidOperationException("Damaged-floor energy requires isolated 2.3.1 deployment.");
            RelicData owner = all.GetAllCollectableRelicData().Single(relic => relic.GetID() == OwnerId);
            RelicEffectData effect = owner.GetEffects().Single();
            definition = new { Id = owner.GetID(), Key = owner.name, Placement = owner.DisallowInDeploymentPhase,
                Type = effect.GetEffectClassName(), Int = effect.GetParamInt(), Conditions = effect.GetEffectConditions().Count };
            if (owner.name != "EmberForDamagedPerFloor" || effect.GetEffectClassName() != RelicDamagedFloorEnergyModel.Effect ||
                effect.GetParamInt() != 1 || effect.GetEffectConditions().Count != 0)
                throw new InvalidOperationException("Changed original damaged-floor energy owner.");
            CardData steward = all.FindCardData("d14a50f3-728d-43e1-87f0-ef1b013f6678");
            CharacterData unit = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(unit, "health", 100); Set(unit, "attackDamage", 60); Set(unit, "size", 1);
            Set(unit, "triggers", new List<CharacterTriggerData>()); Set(unit, "startingStatusEffects", Array.Empty<StatusEffectStackData>()); Set(unit, "unitAbility", null!);
            CardData template = all.FindCardData("96c9ed37-5e59-4c4f-a914-ae7040d24755");
            CardData Control(int index, string type, int amount, int cost)
            {
                CardData card = UnityEngine.Object.Instantiate(template); card.name = "PojuDamagedFloorControl" + index;
                Set(card, "id", "fc231f10-0000-4000-8000-00000000000" + index); Set(card, "cost", cost);
                Set(card, "targetless", false); Set(card, "targetsRoom", true);
                var action = new CardEffectData(type, null!, Team.Type.Monsters);
                action.Cheat_SetTargetMode(TargetMode.DropTargetCharacter); Set(action, "paramInt", amount);
                Set(card, "effects", new List<CardEffectData> { action }); Set(card, "triggers", new List<CardTriggerEffectData>());
                Set(card, "effectTriggers", new List<CharacterTriggerData>()); Set(card, "startingUpgrades", new List<CardUpgradeData>());
                ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(card);
                return card;
            }
            CardData damage = Control(1, "CardEffectDamage", 1, 0), heal = Control(2, "CardEffectHeal", 100, 1),
                reinjure = Control(3, "CardEffectDamage", 1, 1);
            foreach (CardState card in manager.GetHand().ToArray()) manager.MoveCardFromHandToTopOfDeck(card);
            manager.GetDrawPile().Clear(); save.AddRelic(owner);
            CardState Add(CardData data) => manager.AddNewCard(data, CardPile.HandPile, false, false, animate: false) ??
                throw new InvalidOperationException("Unable to add damaged-floor input.");
            CardState futureDamage = Add(reinjure); manager.MoveCardFromHandToTopOfDeck(futureDamage);
            CardState futureHeal = Add(heal); manager.MoveCardFromHandToTopOfDeck(futureHeal);
            cards = new[] { steward, damage, steward, damage, steward, damage }.Select(Add)
                .Concat(new[] { futureHeal, futureDamage }).Select(FullBattleTrace.Active!.CardId).ToArray();
            managers.GetCardStatistics().GetStatValue(new CardStatistics.StatValueData { trackedValue = CardStatistics.TrackedValueType.TimesPlayed,
                cardState = manager.GetHand()[0], entryDuration = CardStatistics.EntryDuration.ThisBattle, cardTypeTarget = CardStatistics.CardTypeTarget.Any });
            FullBattleTrace.Active!.InvalidateCardRules(); Prepared = true; logger = log;
            Set(managers.GetCombatManager(), "combatStateChanged", true);
            log.LogInfo("DAMAGED-FLOOR-ENERGY-PREPARED unchanged original owner; three floors, damage/heal/reinjure across turns.");
        }
        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            if (step >= cards.Length) return null;
            if (step >= 6 && (state.Spawn.Turn < (step == 6 ? 1 : 2) ||
                !state.Spawn.Train.Context!.Cards.Hand.Any(card => card.InstanceId == cards[step]))) return null;
            if (step == 6 && calibration == null) calibration = DamagedFloorEnergyProbe.Calibrate(AllGameManagers.Instance!);
            int room = step < 6 ? step / 2 : 1;
            bool summon = step < 6 && step % 2 == 0;
            int target = summon ? 0 : state.Spawn.Train.Rooms.Single(floor => floor.RoomIndex == room).Units
                .Single(unit => unit.SpawnerCardId == cards[step < 6 ? step - 1 : 2]).Id;
            var action = new PlayCardAction(cards[step], room, playerPosition: summon ? 0 : -1, targetUnitId: target);
            BattleActionResult result = BattleActionModel.PlayCard(state, action);
            if (!result.Supported) { logger?.LogInfo("DAMAGED-FLOOR-ENERGY-REJECT " + step + ": " + result.Reason); return null; }
            DamagedFloorEnergyProbe.Query(AllGameManagers.Instance!);
            step++; return action;
        }
        internal static bool IsScenarioAction(PlayCardAction action) => cards.Contains(action.CardInstanceId);
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
