using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalCombatTriggerBatchScenario
    {
        internal const string Name = "original-combat-trigger-relics";
        internal const string Shroom = "SubtypesData_Shroom";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static readonly string[] Ids = { "3accd42e-3f82-4bf1-9026-29a624d3dde5" };
        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            bool IsShroom(int cardId) => state.PlayRules!.Cards.FirstOrDefault(rule =>
                rule.DataId == state.Spawn.Train.Context!.FindCard(cardId)?.DataId)?.SpawnUnit?.Subtypes.Contains(Shroom) == true;
            if (state.Spawn.Turn > 0)
            {
                var positive = BattleActionModel.EnumerateSupportedPlays(state).FirstOrDefault(play => IsShroom(play.CardInstanceId) &&
                    state.Spawn.Train.Rooms.Single(room => room.RoomIndex == play.RoomIndex).Units.Any(unit => unit.Team == CombatTeam.Enemy && unit.Health > 0));
                if (positive != null) return positive;
            }
            return BattleActionModel.ChooseUnitSpellAndJunkPlay(state, card => !IsShroom(card.InstanceId));
        }
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (Prepared) return;
            var save = managers.GetSaveManager(); var data = save.GetAllGameData(); var cards = managers.GetCardManager()!;
            if (managers.GetCombatManager()!.GetTurnCount() != 0 || save.PreviewMode)
                throw new InvalidOperationException("Combat trigger batch requires deployment.");
            var owned = cards.GetAllCards(new List<CardState>()).ToArray();
            var stewards = owned.Select(card => data.FindCardData(card.GetCardDataID())!).Where(card =>
                card.GetSpawnCharacterData()?.name.StartsWith("TrainSteward", StringComparison.Ordinal) == true).Distinct().Take(2).ToArray();
            if (stewards.Length != 2) throw new InvalidOperationException("Two Steward carrier definitions required.");
            for (int index = 0; index < 2; index++)
            {
                var unit = stewards[index].GetSpawnCharacterData()!;
                Set(unit, "health", 60); Set(unit, "attackDamage", 2); Set(unit, "size", 1); Set(unit, "equipmentLimit", 2);
                Set(unit, "subtypeKeys", index == 0 ? new List<string> { Shroom } : new List<string>());
                Set(unit, "startingStatusEffects", Array.Empty<StatusEffectStackData>());
                Set(unit, "triggers", new List<CharacterTriggerData>()); Set(unit, "unitAbility", null);
                Set(stewards[index], "cost", 0);
            }
            foreach (var card in owned) card.Setup(data.FindCardData(card.GetCardDataID())!, save);
            foreach (var card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            foreach (var definition in stewards)
                if (cards.AddNewCard(definition, CardPile.HandPile, false, false, animate: false) == null)
                    throw new InvalidOperationException("Combat trigger carrier not created.");
            for (int index = 0; index < Ids.Length; index++)
            {
                var relic = data.GetAllCollectableRelicData().Single(item => item.GetID() == Ids[index]);
                var effect = relic.GetEffects().Single(); var trigger = effect.GetTriggers().Single(); var action = trigger.GetEffects().Single();
                string[] keys = { "ShroomsGainSapDecay" };
                string[] kinds = { "OnSpawn" };
                string[] statuses = { "decay" };
                if (relic.name != keys[index] || relic.DisallowInDeploymentPhase || effect.GetEffectClassName() != "RelicEffectAddTrigger" ||
                    effect.GetParamSourceTeam() != Team.Type.Monsters || !effect.GetParamBool() || effect.GetParamBool2() ||
                    effect.GetEffectConditions().Count != 0 || effect.GetParamCharacters().Count != 0 ||
                    (index == 0 ? effect.GetParamCharacterSubtype().Key != Shroom : !effect.GetParamCharacterSubtype().IsNone) ||
                    trigger.GetTrigger().ToString() != kinds[index] || trigger.GetTriggerOnce() || !trigger.GetHideVisualAndIgnoreSilence() ||
                    trigger.GetOnlyTriggerIfEquipped() || action.GetEffectStateName() != "CardEffectAddStatusEffect" ||
                    action.GetTargetMode() != (index == 0 ? TargetMode.Room : TargetMode.Self) ||
                    action.GetParamStatusEffects().Single().statusId != statuses[index] ||
                    action.GetParamStatusEffects().Single().count != (index == 2 ? 4 : 1))
                    throw new InvalidOperationException("Original combat trigger payload changed: " + relic.name);
                if (save.GetCollectedRelics().Any(item => item.GetRelicDataID() == Ids[index])) throw new InvalidOperationException("Owner already present.");
                save.AddRelic(relic);
            }
            Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-COMBAT-TRIGGERS-PREPARED unchanged ShroomsGainSapDecay; Shroom/non-Shroom Stewards, original waves and boss.");
        }
        private static void Set(object target, string field, object? value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
