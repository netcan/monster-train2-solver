using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalUnitAbilityScenario
    {
        internal static string? AbilityId => Environment.GetEnvironmentVariable("MT2_PROBE_ORIGINAL_UNIT_ABILITY");
        internal static bool Enabled => !string.IsNullOrEmpty(AbilityId);
        internal static void Prepare(AllGameManagers managers, CardState[] created)
        {
            if (!Enabled) return;
            SaveManager save = managers.GetSaveManager(); AllGameData all = save.GetAllGameData();
            CardData ability = all.FindCardData(AbilityId!) ?? throw new InvalidOperationException("Missing original unit skill.");
            if (!ability.IsUnitAbility()) throw new InvalidOperationException("Requested definition is not a unit ability.");
            foreach (CardEffectData effect in ability.GetEffects())
                UnityEngine.Debug.Log("ORIGINAL-UNIT-ABILITY-EFFECT type=" + effect.GetEffectStateName() + " status=" + effect.GetUseStatusEffectStackMultiplier() +
                    " health=" + effect.GetUseHealthMissingStackMultiplier() + " magic=" + effect.GetUseMagicPowerMultiplier() + " subtype=" + effect.GetParamSubtype().Key);
            // Only the existing control anchor changes; original skill assets stay untouched.
            if (AbilityId == "4dbfc527-50b2-4068-aa36-9770d4444a57")
            {
                CharacterState pyre = managers.GetRoomManager()!.GetPyreRoom().GetPyreHeart();
                void Drain(System.Collections.IEnumerator iterator) { while (iterator.MoveNext()) if (iterator.Current is System.Collections.IEnumerator child) Drain(child); }
                Drain(pyre.SetUnitAbility(ability, false));
                pyre.GetUnitAbilityCardState(); // Initialize the native Pyre UI cache before the first decision snapshot.
            }
            else AccessTools.Field(typeof(CharacterData), "unitAbility").SetValue(created[0].GetSpawnCharacterData(), ability);
            if (AbilityId == "f7126e4a-ef22-4621-b1f7-31a53e2e5870")
            {
                CharacterTriggerData onFeed = HealingScenario.HealGold(5, false, false);
                AccessTools.Field(typeof(CharacterTriggerData), "trigger").SetValue(onFeed, CharacterTriggerData.Trigger.OnFeed);
                AccessTools.Field(typeof(CharacterData), "triggers").SetValue(created[0].GetSpawnCharacterData(), new List<CharacterTriggerData> { onFeed });
            }
            RoomState source = managers.GetRoomManager()!.GetRoom(0);
            var corruption = source.AdjustCorruption(managers.GetCombatManager()!, managers.GetRelicManager(), 4, 4, skipTriggersAndNotifications: true);
            while (corruption.MoveNext()) { }
            if (AbilityId is "1b18c4ef-9df6-466d-b61e-c338800a14a6" or "4dbfc527-50b2-4068-aa36-9770d4444a57")
                for (int index = 0; index < 10; index++)
                    managers.GetCardManager()!.AddNewCard(all.FindCardData("f1dbd7d5-74fc-4507-aba1-dc1a8ec9393b")!, CardPile.DeckPile, false, false, animate: false);
            if (AbilityId == "4dbfc527-50b2-4068-aa36-9770d4444a57")
            {
                CardState control = managers.GetCardManager()!.AddNewCard(all.FindCardData("f1dbd7d5-74fc-4507-aba1-dc1a8ec9393b")!, CardPile.HandPile, false, false, animate: false);
                var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze");
                control.AddTemporaryTrait(freeze, managers.GetCoreManagers());
            }
        }
        internal static PlayCardAction? Choose(BattleTurnState state, int anchorCardId)
        {
            CombatContext context = state.Spawn.Train.Context!;
            AbilityCardCacheEntry? cache = context.AbilityCardCache?.FirstOrDefault(entry => entry.DataId == AbilityId);
            if (cache != null && context.Statistics!.CardsPlayedThisTurn.Contains(cache.InstanceId)) return null;
            CombatUnit? actor = state.Spawn.Train.Rooms.SelectMany(room => room.Units).FirstOrDefault(unit => unit.SpawnerCardId == anchorCardId);
            if (AbilityId == "4dbfc527-50b2-4068-aa36-9770d4444a57") actor = state.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre);
            if (actor?.Ability?.HasAbility != true || state.Spawn.Turn == 0) return null;
            ObserveEligibility(FullBattleTrace.Active!.KnownUnits.Single(unit => FullBattleTrace.Active.UnitId(unit) == actor.Id));
            if (actor.Status("cooldown") != null) return null;
            if (context.LastAbilityActivatorUnitId == actor.Id) return null;
            var actions = BattleActionModel.EnumerateSupportedPlays(state).Where(action => action.ActivatorUnitId == actor.Id);
            if (AbilityId == "c59d1814-0e0e-414c-b82f-a71cead2951f") actions = actions.Where(action => action.RoomIndex == 1);
            else actions = actions.Where(action => action.RoomIndex == (actor.IsPyre ? 3 : 0));
            if (AbilityId == "f7126e4a-ef22-4621-b1f7-31a53e2e5870")
            {
                actions = actions.Where(action => action.TargetUnitId != actor.Id && state.Spawn.Train.Rooms[0].Units.Any(unit => unit.Id == action.TargetUnitId && unit.Team == CombatTeam.Player));
                int[] food = state.Spawn.Train.Rooms[0].Units.Where(unit => unit.Triggers.Any(trigger => trigger.Kind == "OnEaten")).Select(unit => unit.Id).ToArray();
                if (food.Length > 0) actions = actions.Where(action => food.Contains(action.TargetUnitId));
            }
            return actions.FirstOrDefault();
        }
        internal sealed class TraitQuery
        {
            public string DataId { get; set; } = "";
            public int SourceRoom { get; set; }
            public int SelectedRoom { get; set; }
            public int SourceCorruption { get; set; }
            public int SelectedCorruption { get; set; }
            public bool Allowed { get; set; }
        }
        internal static readonly List<TraitQuery> Queries = new List<TraitQuery>();
        private static readonly HashSet<string> keys = new HashSet<string>();
        internal static void ObserveEligibility(CharacterState actor)
        {
            if (!Enabled || !actor.HasUnitAbility()) return;
            AllGameManagers managers = AllGameManagers.Instance!;
            RoomManager roomManager = managers.GetRoomManager()!;
            foreach (CardTraitCorruptRestricted trait in actor.GetUnitAbilityCardState().GetTraitStates().OfType<CardTraitCorruptRestricted>())
            {
                int sourceCharacterRoomIndex = actor.GetCurrentRoomIndex();
                bool allowed = trait.GetIsPlayableFromHand(managers.GetCardManager()!, roomManager, sourceCharacterRoomIndex, out _);
                int selected = roomManager.GetSelectedRoom();
                int sourceCorruption = roomManager.GetRoom(sourceCharacterRoomIndex)?.GetCurrentCorruption() ?? -1;
                int selectedCorruption = roomManager.GetRoom(selected)?.GetCurrentCorruption() ?? -1;
                string key = sourceCharacterRoomIndex + ":" + selected + ":" + sourceCorruption + ":" + selectedCorruption + ":" + allowed;
                if (!keys.Add(key)) continue;
                Queries.Add(new TraitQuery { DataId = AbilityId!, SourceRoom = sourceCharacterRoomIndex, SelectedRoom = selected,
                    SourceCorruption = sourceCorruption, SelectedCorruption = selectedCorruption, Allowed = allowed });
            }
        }
    }
}
