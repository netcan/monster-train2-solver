using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class SpellShieldScenario
    {
        internal const string Name = "original-spell-shield";
        private const string EnemyKey = "HordeT3_PureSouls";
        private const string SpellId = "3d1a2e1b-7751-4b2c-a549-77d03db715c9";
        private static int scenarioActionId;
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            CharacterData enemy = save.GetAllGameData().GetAllCharacterData()
                .Single(unit => unit.GetAssetKey() == EnemyKey);
            StatusEffectStackData[] statuses = enemy.GetStartingStatusEffects();
            if (statuses.Length != 1 || statuses[0].statusId != "spell shield" || statuses[0].count != 2)
                throw new InvalidOperationException("Original Pure Souls Spell Shield definition changed.");
            StatusEffectData shield = StatusEffectManager.Instance.GetStatusEffectDataById("spell shield")!;
            if (shield.GetStatusEffectStateName() != "StatusEffectSpellShieldState" || !shield.GetRemoveWhenTriggered())
                throw new InvalidOperationException("Original Spell Shield trigger rule changed.");
            CardData spell = save.GetAllGameData().FindCardData(SpellId) ??
                throw new InvalidOperationException("Original Flicker is missing.");
            if (spell.name != "Flicker" || spell.GetCardType() != CardType.Spell ||
                spell.GetEffects().Count != 1 || spell.GetEffects()[0].GetEffectStateName() != "CardEffectDamage" ||
                spell.GetEffects()[0].GetTargetMode() != TargetMode.Room || spell.GetEffects()[0].GetParamInt() != 1)
                throw new InvalidOperationException("Original Flicker damage definition changed.");
            var pattern = (SpawnPatternData)AccessTools.Field(typeof(HeroManager), "spawnPattern")
                .GetValue(managers.GetHeroManager());
            var waves = new List<SpawnGroupPoolData>();
            pattern.GetUnlockedWaves(save, waves);
            if (waves.Count == 0) throw new InvalidOperationException("Spell Shield scene requires an enemy wave.");
            int groupsChanged = 0;
            foreach (SpawnGroupData group in ((IEnumerable)AccessTools.Field(typeof(SpawnGroupPoolData),
                "possibleGroups").GetValue(waves[0])).Cast<SpawnGroupData>())
            {
                var containers = ((IEnumerable)AccessTools.Field(typeof(SpawnGroupData),
                    "characterDataContainerList").GetValue(group)).Cast<SpawnGroupData.CharacterDataContainer>();
                SpawnGroupData.CharacterDataContainer[] ordinary = containers.Where(item =>
                    !item.UseBossCharacter && item.Covenant == null && item.Replacements.Count == 0).ToArray();
                if (ordinary.Length == 0) throw new InvalidOperationException("First wave has no ordinary enemy slot.");
                foreach (SpawnGroupData.CharacterDataContainer slot in ordinary)
                    AccessTools.Field(typeof(SpawnGroupData.CharacterDataContainer), "characterData").SetValue(slot, enemy);
                groupsChanged++;
            }
            CardManager cards = managers.GetCardManager()!;
            for (int index = 0; index < 2; index++)
            {
                if (!cards.GetBelowHandSize()) throw new InvalidOperationException("Spell Shield scene needs two hand slots.");
                CardState added = cards.AddNewCard(spell, CardPile.HandPile, false, false, animate: false);
                if (added == null || !cards.GetHand().Contains(added))
                    throw new InvalidOperationException("Original Flicker did not enter the hand.");
                cards.MoveCardFromHandToTopOfDeck(added);
            }
            scenarioActionId = 0;
            log.LogInfo("SPELL-SHIELD-PREPARED original=" + EnemyKey + " stacks=2 spell=Flicker firstWaveGroups=" + groupsChanged);
        }

        internal static PlayCardAction? Choose(BattleTurnState state, ManualLogSource log)
        {
            if (state.Spawn.Turn == 0) return null;
            CombatUnit[] targets = state.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .Where(unit => unit.AssetKey == EnemyKey && unit.Health > 0 &&
                    unit.Status("spell shield")?.Stacks > 0).ToArray();
            if (targets.Length > 0)
            {
                PlayCardAction[] plays = BattleActionModel.EnumerateSupportedPlays(state).ToArray();
                PlayCardAction? action = plays.FirstOrDefault(item =>
                    targets.Any(target => state.Spawn.Train.Rooms[item.RoomIndex].Units.Contains(target)) &&
                    state.Spawn.Train.Context!.FindCard(item.CardInstanceId)?.DataId == SpellId);
                if (action != null)
                {
                    CombatUnit target = targets.First(unit => state.Spawn.Train.Rooms[action.RoomIndex].Units.Contains(unit));
                    scenarioActionId = action.CardInstanceId;
                    log.LogInfo("SPELL-SHIELD-PLAY target=" + target.Id + " stacks=" + target.Status("spell shield")!.Stacks +
                        " spell=" + action.CardInstanceId);
                    return action;
                }
                return null;
            }
            scenarioActionId = 0;
            return BattleActionModel.ChooseUnitSpellAndJunkPlay(state);
        }

        internal static bool ConsumeScenarioActionMark(PlayCardAction action)
        {
            bool matches = scenarioActionId > 0 && action.CardInstanceId == scenarioActionId;
            scenarioActionId = 0;
            return matches;
        }
    }
}
