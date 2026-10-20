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
            AccessTools.Field(typeof(CharacterData), "unitAbility").SetValue(created[0].GetSpawnCharacterData(), ability);
            RoomState source = managers.GetRoomManager()!.GetRoom(0);
            var corruption = source.AdjustCorruption(managers.GetCombatManager()!, managers.GetRelicManager(), 4, 4, skipTriggersAndNotifications: true);
            while (corruption.MoveNext()) { }
            if (AbilityId == "1b18c4ef-9df6-466d-b61e-c338800a14a6")
                for (int index = 0; index < 10; index++)
                    managers.GetCardManager()!.AddNewCard(all.FindCardData("f1dbd7d5-74fc-4507-aba1-dc1a8ec9393b")!, CardPile.DeckPile, false, false, animate: false);
        }
        internal static PlayCardAction? Choose(BattleTurnState state, int anchorCardId)
        {
            CombatContext context = state.Spawn.Train.Context!;
            AbilityCardCacheEntry? cache = context.AbilityCardCache?.FirstOrDefault(entry => entry.DataId == AbilityId);
            if (cache != null && context.Statistics!.CardsPlayedThisTurn.Contains(cache.InstanceId)) return null;
            CombatUnit? actor = state.Spawn.Train.Rooms.SelectMany(room => room.Units).FirstOrDefault(unit => unit.SpawnerCardId == anchorCardId);
            if (actor == null || state.Spawn.Turn == 0) return null;
            ObserveEligibility(FullBattleTrace.Active!.KnownUnits.Single(unit => FullBattleTrace.Active.UnitId(unit) == actor.Id));
            if (actor.Status("cooldown") != null) return null;
            if (context.LastAbilityActivatorUnitId == actor.Id) return null;
            var actions = BattleActionModel.EnumerateSupportedPlays(state).Where(action => action.ActivatorUnitId == actor.Id);
            if (AbilityId == "c59d1814-0e0e-414c-b82f-a71cead2951f") actions = actions.Where(action => action.RoomIndex == 1);
            else actions = actions.Where(action => action.RoomIndex == 0);
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
            if (!Enabled) return;
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
