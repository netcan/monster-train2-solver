using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class DevotionSpawnBattleScenario
    {
        internal const string Name = "devotion-spawn-battle";
        internal const string OwnerId = "ecb009cc-30c5-47ba-93dd-91b8eab5c78f";
        internal const string MagnitudeId = "ee429978-fa41-4c66-93c6-3d3c7d5a4733";
        internal const string VenerateId = "96c9ed37-5e59-4c4f-a914-ae7040d24755";
        internal const string SermonId = "6dc0af49-3ede-4091-afeb-8676eca15ca2";
        internal const string AgonizeId = "b64904e9-ab9c-44d8-ac40-f53a9c9edd03";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        private static int[] cards = Array.Empty<int>();
        private static int step;
        private static ManualLogSource? logger;
        private static string? rejection;
        private static object[] owners = Array.Empty<object>(), originalCards = Array.Empty<object>();
        private static object? gates;
        internal static object Evidence => new { Prepared, ControlCardIds = cards, CompletedControlPlays = step,
            OwnerDefinitions = owners, OriginalCards = originalCards, Gates = gates };
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            var save = managers.GetSaveManager(); var all = save.GetAllGameData(); var manager = managers.GetCardManager();
            string[] ownerIds = { OwnerId, MagnitudeId };
            if (!ConditionalActivationProbe.HasVanguard || save.PreviewMode || managers.GetCombatManager().GetTurnCount() != 0 ||
                save.GetCollectedRelics().Any(owner => ownerIds.Contains(owner.GetRelicDataID())) ||
                DevotionProbe.Read(save, "GetDevotionLevel") != 0 || DevotionProbe.Read(save, "GetDevotionPoints") != 0)
                throw new InvalidOperationException("Devotion spawn battle requires isolated 2.3.1 deployment at zero Devotion.");
            RelicData[] relics = ownerIds.Select(id => all.GetAllCollectableRelicData().Single(owner => owner.GetID() == id)).ToArray();
            owners = relics.Select(owner => {
                RelicEffectData effect = owner.GetEffects().Single();
                return (object)new { Id = owner.GetID(), Key = owner.name, Placement = owner.DisallowInDeploymentPhase,
                    Type = effect.GetEffectClassName(), Int = effect.GetParamInt(), Int2 = effect.GetParamInt2(),
                    Team = effect.GetParamSourceTeam().ToString(), Conditions = effect.GetEffectConditions().Count,
                    Statuses = effect.GetParamStatusEffects().Select(status => new { status.statusId, status.count }).ToArray() };
            }).ToArray();
            if (relics[0].name != "MonolithDevotion" || relics[0].GetEffects()[0].GetEffectClassName() != RelicModel.AddStatusPerDevotionOnSpawn ||
                relics[0].GetEffects()[0].GetParamStatusEffects().Single().count != 4 ||
                relics[1].name != "ZealDamageIncrease" || relics[1].GetEffects()[0].GetEffectClassName() != "RelicEffectModifyStatusMagnitude" ||
                relics[1].GetEffects()[0].GetParamInt() != 1)
                throw new InvalidOperationException("Original Devotion/Zeal owners changed.");
            CardData venerate = all.FindCardData(VenerateId), sermon = all.FindCardData(SermonId), agonize = all.FindCardData(AgonizeId);
            originalCards = new[] { venerate, sermon, agonize }.Select(card => (object)new {
                Id = card.GetID(), Key = card.name, Cost = card.GetCost(),
                Effects = card.GetEffects().Select(effect => new { Type = effect.GetEffectStateName(), Int = effect.GetParamInt(),
                    DevotionMinimum = DevotionProbe.Requirement(effect), DevotionMaximum = DevotionProbe.Requirement(effect, true),
                    PerLevel = effect.GetAdditionalParamInt(), Statuses = effect.GetParamStatusEffects().Select(status => new { status.statusId, status.count }).ToArray() }).ToArray(),
                Traits = card.GetTraits().Select(trait => new { Type = trait.GetTraitStateName(), Int = trait.GetParamInt(),
                    Tracked = trait.GetParamTrackedValue().ToString() }).ToArray() }).ToArray();
            CardData steward = all.FindCardData("d14a50f3-728d-43e1-87f0-ef1b013f6678");
            CharacterData unit = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(unit, "health", 100); Set(unit, "attackDamage", 60); Set(unit, "size", 1);
            Set(unit, "triggers", new List<CharacterTriggerData>()); Set(unit, "startingStatusEffects", Array.Empty<StatusEffectStackData>()); Set(unit, "unitAbility", null!);
            gates = DevotionGateCalibration.Run(managers, sermon.GetEffects().Single());
            Set(all.GetAllCharacterData().Single(character => character.name == "ShieldGuardT1_BasicNonAttack"), "health", 100);
            CardData Control(int index, string type, int stacks, Team.Type team)
            {
                CardData card = UnityEngine.Object.Instantiate(sermon); card.name = "PojuDevotionSpawnControl" + index;
                Set(card, "id", "fc231d30-0000-4000-8000-00000000000" + index); Set(card, "cost", 0);
                var effect = new CardEffectData(type, null!, team); effect.Cheat_SetTargetMode(TargetMode.DropTargetCharacter);
                Set(effect, "paramStatusEffects", new[] { new StatusEffectStackData { statusId = "zeal", count = stacks } });
                Set(card, "effects", new List<CardEffectData> { effect }); Set(card, "traits", new List<CardTraitData>());
                Set(card, "triggers", new List<CardTriggerEffectData>()); Set(card, "effectTriggers", new List<CharacterTriggerData>());
                Set(card, "startingUpgrades", new List<CardUpgradeData>()); Set(card, "targetless", false); Set(card, "targetsRoom", true);
                ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(card);
                return card;
            }
            CardData removal = Control(1, "CardEffectRemoveStatusEffect", 999, Team.Type.Monsters);
            CardData addition = Control(2, "CardEffectAddStatusEffect", 3, Team.Type.Monsters);
            CardData enemy = Control(3, "CardEffectAddStatusEffect", 2, Team.Type.Heroes);
            foreach (CardState card in manager.GetHand().ToArray()) manager.MoveCardFromHandToTopOfDeck(card);
            manager.GetDrawPile().Clear();
            foreach (RelicData relic in relics) save.AddRelic(relic);
            CardState Add(CardData data)
            {
                CardState added = manager.AddNewCard(data, CardPile.HandPile, false, false, animate: false);
                return added ?? throw new InvalidOperationException("Unable to add native Devotion spawn input " + data.name);
            }
            CardState agonizeState = Add(agonize); manager.MoveCardFromHandToTopOfDeck(agonizeState);
            CardState enemyState = Add(enemy); manager.MoveCardFromHandToTopOfDeck(enemyState);
            CardData[] sequence = { steward, removal, venerate, steward, sermon, venerate, steward, sermon, removal, addition };
            cards = sequence.Select(Add).Concat(new[] { enemyState, agonizeState }).Select(FullBattleTrace.Active!.CardId).ToArray();
            managers.GetPlayerManager().AddEnergy(10);
            FullBattleTrace.Active!.InvalidateCardRules(); logger = log; Prepared = true;
            Set(managers.GetCombatManager(), "combatStateChanged", true);
            log.LogInfo("DEVOTION-SPAWN-PREPARED unchanged Monolith/Zeal relics and Venerate/Sermon/Agonize; levels 0/1/2, no retroactive spawn buffs and ordinary next-turn enemy plays.");
        }
        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            if (step >= cards.Length) return null;
            int room = 2, target = 0;
            if (step >= 10)
            {
                if (state.Spawn.Turn == 0 || !state.Spawn.Train.Context!.Cards.Hand.Any(card => card.InstanceId == cards[step])) return null;
                RoomCombatState? enemyRoom = state.Spawn.Train.Rooms.FirstOrDefault(floor =>
                    floor.Units.FirstOrDefault(unit => unit.Team == CombatTeam.Enemy && unit.Health > 0) is CombatUnit front &&
                    (step == 10 || front.Health >= 60));
                if (enemyRoom == null) return null;
                room = enemyRoom.RoomIndex;
                if (step == 10) target = enemyRoom.Units.First(unit => unit.Team == CombatTeam.Enemy && unit.Health > 0).Id;
            }
            else if (step is 1 or 8 or 9) target = state.Spawn.Train.Rooms[2].Units.Single(unit => unit.SpawnerCardId == cards[step == 1 ? 0 : 3]).Id;
            var action = new PlayCardAction(cards[step], room, playerPosition: step is 0 or 3 or 6 ? 0 : -1, targetUnitId: target);
            BattleActionResult result = BattleActionModel.PlayCard(state, action);
            if (!result.Supported)
            {
                if (rejection != result.Reason) { rejection = result.Reason; logger?.LogInfo("DEVOTION-SPAWN-REJECT " + step + ": " + rejection); }
                return null;
            }
            step++; return action;
        }
        internal static bool IsScenarioAction(PlayCardAction action) => cards.Contains(action.CardInstanceId);
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
