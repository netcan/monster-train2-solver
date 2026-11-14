using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class ZealHordeBattleScenario
    {
        internal const string Name = "zeal-horde-battle";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        private static int[] cards = Array.Empty<int>();
        private static int step;
        private static object? ownerDefinition;
        internal static object Evidence => new { Prepared, ControlCardIds = cards, CompletedControlPlays = step, OwnerDefinition = ownerDefinition };
        private static void Set(object owner, string field, object value) => AccessTools.Field(owner.GetType(), field).SetValue(owner, value);
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            var save = managers.GetSaveManager(); var all = managers.GetAllGameData(); var manager = managers.GetCardManager();
            if (!ConditionalActivationProbe.HasVanguard || save.PreviewMode || managers.GetCombatManager().GetTurnCount() != 0 ||
                save.GetCollectedRelics().Any(relic => relic.GetRelicDataID() == ZealStatusCalibration.OwnerId))
                throw new InvalidOperationException("Zeal/Horde battle requires isolated 2.3.1 deployment.");
            RelicData owner = all.GetAllCollectableRelicData().Single(data => data.GetID() == ZealStatusCalibration.OwnerId);
            RelicEffectData effect = owner.GetEffects().Single();
            ownerDefinition = new { Id = owner.GetID(), Key = owner.name, Type = effect.GetEffectClassName(),
                Int = effect.GetParamInt(), Team = effect.GetParamSourceTeam().ToString(), Conditions = effect.GetEffectConditions().Count,
                Statuses = effect.GetParamStatusEffects().Select(status => new { status.statusId, status.count }).ToArray() };
            if (owner.name != "ZealDamageIncrease" || effect.GetEffectClassName() != RelicModel.ModifyStatusMagnitude ||
                effect.GetParamInt() != 1 || effect.GetParamSourceTeam() != Team.Type.Monsters || effect.GetEffectConditions().Count != 0 ||
                effect.GetParamStatusEffects().Single().statusId != "zeal")
                throw new InvalidOperationException("Original Zeal magnitude owner changed.");
            CardData steward = all.FindCardData("d14a50f3-728d-43e1-87f0-ef1b013f6678");
            CharacterData observer = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(observer, "health", 100); Set(observer, "attackDamage", 60); Set(observer, "size", 1);
            var harvest = HealingScenario.HealGold(2, false, false); Set(harvest, "trigger", CharacterTriggerData.Trigger.OnAnyUnitDeathOnFloor);
            Set(observer, "triggers", new List<CharacterTriggerData> { harvest });
            Set(observer, "startingStatusEffects", Array.Empty<StatusEffectStackData>()); Set(observer, "unitAbility", null!);
            var horde = UnityEngine.Object.Instantiate(steward); horde.name = "PojuZealHordeCap";
            Set(horde, "id", "fc231a50-0000-4000-8000-000000000001");
            var troop = UnityEngine.Object.Instantiate(observer); troop.name = "PojuZealHordeCapUnit";
            Set(troop, "id", "fc231a51-0000-4000-8000-000000000001"); Set(troop, "health", 99999);
            Set(troop, "triggers", new List<CharacterTriggerData>());
            Set(troop, "startingStatusEffects", new[] { new StatusEffectStackData { statusId = "horde", count = 4 },
                new StatusEffectStackData { statusId = "zeal", count = 3 } });
            Set(horde.GetEffects().Single(), "paramCharacterData", troop);
            ((List<CharacterData>)AccessTools.Field(typeof(AllGameData), "characterDatas").GetValue(all)).Add(troop);
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(horde);
            CardData Control(int index, string type, int stacks)
            {
                var card = UnityEngine.Object.Instantiate(all.FindCardData(DevotionSpawnBattleScenario.VenerateId)); card.name = "PojuZealHordeInput" + index;
                Set(card, "id", "fc231a52-0000-4000-8000-00000000000" + index); Set(card, "cost", 0);
                var action = new CardEffectData(type, null!, Team.Type.Monsters); action.Cheat_SetTargetMode(TargetMode.DropTargetCharacter);
                Set(action, "paramStatusEffects", new[] { new StatusEffectStackData { statusId = "zeal", count = stacks } });
                Set(card, "effects", new List<CardEffectData> { action }); Set(card, "traits", new List<CardTraitData>());
                Set(card, "triggers", new List<CardTriggerEffectData>()); Set(card, "effectTriggers", new List<CharacterTriggerData>());
                Set(card, "startingUpgrades", new List<CardUpgradeData>()); Set(card, "targetless", false); Set(card, "targetsRoom", true);
                ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(card);
                return card;
            }
            var inputs = new[] { steward, horde, Control(1, "CardEffectRemoveStatusEffect", 1),
                Control(2, "CardEffectAddStatusEffect", 2), Control(3, "CardEffectRemoveStatusEffect", 999) };
            foreach (CardState card in manager.GetHand().ToArray()) manager.MoveCardFromHandToTopOfDeck(card);
            manager.GetDrawPile().Clear(); save.AddRelic(owner);
            cards = inputs.Select(data => FullBattleTrace.Active!.CardId(manager.AddNewCard(data, CardPile.HandPile, false, false, animate: false) ??
                throw new InvalidOperationException("Unable to add Zeal/Horde control."))).ToArray();
            managers.GetCardStatistics().GetStatValue(new CardStatistics.StatValueData { trackedValue = CardStatistics.TrackedValueType.TimesPlayed,
                cardState = manager.GetHand()[0], entryDuration = CardStatistics.EntryDuration.ThisBattle, cardTypeTarget = CardStatistics.CardTypeTarget.Any });
            FullBattleTrace.Active!.InvalidateCardRules(); Prepared = true; Set(managers.GetCombatManager(), "combatStateChanged", true);
            log.LogInfo("ZEAL-HORDE-BATTLE-PREPARED controlled capped four-troop unit; original Zeal relic and bosses retained.");
        }
        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            if (step >= cards.Length) return null;
            int target = step < 2 ? 0 : state.Spawn.Train.Rooms[2].Units.Single(unit => unit.Status("horde") != null).Id;
            var action = new PlayCardAction(cards[step], 2, playerPosition: step < 2 ? 0 : -1, targetUnitId: target);
            BattleActionResult result = BattleActionModel.PlayCard(state, action);
            if (!result.Supported) throw new InvalidOperationException("Zeal/Horde input " + step + ": " + result.Reason);
            step++; return action;
        }
        internal static bool IsScenarioAction(PlayCardAction action) => cards.Contains(action.CardInstanceId);
    }
}
