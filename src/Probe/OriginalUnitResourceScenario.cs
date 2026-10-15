using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalUnitResourceScenario
    {
        internal const string Name = "original-unit-resource-relics";
        internal const string GearId = "fc119000-0000-4000-8000-000000000461";
        internal const string SacrificeId = "fc119000-0000-4000-8000-000000000462";
        internal static readonly string[] Ids = { "265d9fb6-934f-4b7d-ba12-d3a06c48b610", "4a5b1613-568c-4bf6-8bf7-15f8bf013345", "56316fc2-b41a-41ae-aba8-e83fb668e8e1" };
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            var save = managers.GetSaveManager(); var data = save.GetAllGameData(); var cards = managers.GetCardManager();
            if (Prepared || save.PreviewMode || managers.GetCombatManager().GetTurnCount() != 0)
                throw new InvalidOperationException("Unit resources require isolated deployment.");
            var owned = cards.GetAllCards(new List<CardState>()).ToArray();
            var stewards = owned.Select(card => data.FindCardData(card.GetCardDataID())!).Where(card =>
                card.GetSpawnCharacterData()?.name.StartsWith("TrainSteward", StringComparison.Ordinal) == true).Distinct().Take(2).ToArray();
            if (stewards.Length != 2) throw new InvalidOperationException("Two distinct unit resource carriers required.");
            for (int index = 0; index < 2; index++)
            {
                var unit = stewards[index].GetSpawnCharacterData()!;
                Set(unit, "health", index == 0 ? 80 : 12); Set(unit, "attackDamage", index == 0 ? 20 : 2);
                Set(unit, "size", 1); Set(unit, "equipmentLimit", 3); Set(stewards[index], "cost", 0);
                Set(unit, "startingStatusEffects", Array.Empty<StatusEffectStackData>());
                Set(unit, "triggers", new List<CharacterTriggerData>()); Set(unit, "unitAbility", null);
                if (index == 1) Set(unit, "subtypeKeys", new List<string> { "SubtypesData_Tomb" });
            }
            var gear = UnityEngine.Object.Instantiate(data.GetAllCardData().First(card => card.GetCardType() == CardType.Equipment &&
                card.GetEffects().Count == 1 && card.GetEffects()[0].GetEffectStateName() == "CardEffectAttachEquipment"));
            Configure(gear, "PojuUnitResourceGear", GearId);
            var upgrade = DynamicUpgradeScenario.Upgrade("PojuUnitResourceGearUpgrade", "fc119000-0000-4000-8000-000000000463", 1, 0, 0, 0, "armor", 0);
            Set(upgrade, "statusEffectUpgrades", new List<StatusEffectStackData>());
            var attach = Effect("CardEffectAttachEquipment"); Set(attach, "paramCardUpgradeData", upgrade); Set(gear, "effects", new List<CardEffectData> { attach });
            var sacrifice = UnityEngine.Object.Instantiate(data.GetAllCardData().First(card => card.GetCardType() == CardType.Spell &&
                card.GetEffects().Count == 1 && card.GetEffects()[0].GetEffectStateName() == "CardEffectDamage"));
            Configure(sacrifice, "PojuUnitResourceSacrifice", SacrificeId); Set(sacrifice, "effects", new List<CardEffectData> { Effect("CardEffectSacrifice") });
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(data)).AddRange(new[] { gear, sacrifice });
            foreach (var card in owned) card.Setup(data.FindCardData(card.GetCardDataID())!, save);
            foreach (var card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            foreach (var card in new[] { stewards[0], stewards[1], gear, gear, sacrifice })
                if (cards.AddNewCard(card, CardPile.HandPile, false, false, animate: false) == null) throw new InvalidOperationException("Unit resource carrier hand full.");
            foreach (string id in Ids)
            {
                var owner = data.GetAllCollectableRelicData().Single(item => item.GetID() == id);
                if (save.GetCollectedRelics().Any(item => item.GetRelicDataID() == id) || owner.DisallowInDeploymentPhase || owner.GetEffects().Count != 1)
                    throw new InvalidOperationException("Original unit resource owner changed.");
                save.AddRelic(owner);
            }
            Prepared = true; Set(managers.GetCombatManager(), "combatStateChanged", true);
            log.LogInfo("ORIGINAL-UNIT-RESOURCES-PREPARED three unchanged owners; Tomb sacrifice, ordinary deaths and equipment callbacks.");
        }
        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            var context = state.Spawn.Train.Context!; var plays = BattleActionModel.EnumerateSupportedPlays(state);
            string? Data(int id) => context.FindCard(id)?.DataId;
            bool sacrificed = context.Statistics!.Values.Any(value => value.Type == "TimesPlayed" && value.Duration == "ThisBattle" && value.Value > 0 && Data(value.CardId) == SacrificeId);
            var room = state.Spawn.Train.Rooms[0];
            foreach (bool strong in new[] { true, false })
            {
                if (room.Units.Any(unit => unit.Team == CombatTeam.Player && unit.MaxHealth == (strong ? 80 : 12)) || !strong && sacrificed) continue;
                var summon = plays.FirstOrDefault(play => play.RoomIndex == 0 && play.PlayerPosition == room.Units.Count(unit => unit.Team == CombatTeam.Player) &&
                    state.PlayRules!.Cards.FirstOrDefault(rule => rule.DataId == Data(play.CardInstanceId))?.SpawnUnit?.MaxHealth == (strong ? 80 : 12));
                if (summon != null) return summon;
            }
            var target = room.Units.FirstOrDefault(unit => unit.Team == CombatTeam.Player && unit.MaxHealth == 80);
            if (target != null)
            {
                var gear = plays.FirstOrDefault(play => Data(play.CardInstanceId) == GearId && play.TargetUnitId == target.Id);
                if (gear != null) return gear;
            }
            var tomb = room.Units.FirstOrDefault(unit => unit.Team == CombatTeam.Player && unit.MaxHealth == 12);
            if (tomb != null && !sacrificed)
            {
                var sacrifice = plays.FirstOrDefault(play => Data(play.CardInstanceId) == SacrificeId && play.TargetUnitId == tomb.Id);
                if (sacrifice != null) return sacrifice;
            }
            return BattleActionModel.ChooseUnitSpellAndJunkPlay(state, card => Data(card.InstanceId) != GearId && Data(card.InstanceId) != SacrificeId);
        }
        private static void Configure(CardData card, string key, string id)
        {
            card.name = key; Set(card, "id", id); Set(card, "cost", 0); Set(card, "traits", new List<CardTraitData>());
            Set(card, "triggers", new List<CardTriggerEffectData>()); Set(card, "effectTriggers", new List<CharacterTriggerData>()); Set(card, "startingUpgrades", new List<CardUpgradeData>());
        }
        private static CardEffectData Effect(string type)
        { var effect = new CardEffectData(type, null!, Team.Type.Monsters); effect.Cheat_SetTargetMode(TargetMode.DropTargetCharacter); return effect; }
        private static void Set(object target, string field, object? value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
