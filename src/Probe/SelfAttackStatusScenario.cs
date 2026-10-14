using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class SelfAttackStatusScenario
    {
        internal const string Name = "self-attack-status";
        internal const string EnemyKey = "MonsterWeldling";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            CharacterData enemy = save.GetAllGameData().GetAllCharacterData()
                .Single(unit => unit.GetAssetKey() == EnemyKey);
            CharacterTriggerData trigger = enemy.GetTriggers().Single(item =>
                item.GetTrigger() == CharacterTriggerData.Trigger.PostCombat);
            if (trigger.GetEffects().Count != 1 ||
                trigger.GetEffects()[0].GetEffectStateName() != "CardEffectAddStatusEffectPerSelfAttack" ||
                trigger.GetEffects()[0].GetTargetMode() != TargetMode.FrontInRoom ||
                trigger.GetEffects()[0].GetParamInt() != 2)
                throw new InvalidOperationException("Original Weldling status definition changed.");
            var pattern = (SpawnPatternData)AccessTools.Field(typeof(HeroManager), "spawnPattern")
                .GetValue(managers.GetHeroManager());
            var waves = new List<SpawnGroupPoolData>();
            pattern.GetUnlockedWaves(save, waves);
            if (waves.Count < 2) throw new InvalidOperationException("Weldling scene requires two waves.");
            int groupsChanged = 0;
            foreach (SpawnGroupData group in ((IEnumerable)AccessTools.Field(typeof(SpawnGroupPoolData),
                "possibleGroups").GetValue(waves[0])).Cast<SpawnGroupData>())
            {
                var containers = ((IEnumerable)AccessTools.Field(typeof(SpawnGroupData),
                    "characterDataContainerList").GetValue(group)).Cast<SpawnGroupData.CharacterDataContainer>();
                SpawnGroupData.CharacterDataContainer? first = containers.FirstOrDefault(item =>
                    !item.UseBossCharacter && item.Covenant == null && item.Replacements.Count == 0);
                if (first == null) throw new InvalidOperationException("First wave has no ordinary enemy slot.");
                AccessTools.Field(typeof(SpawnGroupData.CharacterDataContainer), "characterData").SetValue(first, enemy);
                groupsChanged++;
            }
            if (groupsChanged == 0) throw new InvalidOperationException("First wave has no candidate groups.");
            log.LogInfo("SELF-ATTACK-STATUS-PREPARED original=" + EnemyKey + " firstWaveGroups=" + groupsChanged);
        }

        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            if (source.Spawn.Turn == 0)
            {
                if (source.Spawn.Train.Rooms[0].Units.Any(unit => unit.Team == CombatTeam.Player)) return null;
                return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(action =>
                    action.RoomIndex == 0 && source.Spawn.Train.Context!.FindCard(action.CardInstanceId)?.DataId ==
                    "d14a50f3-728d-43e1-87f0-ef1b013f6678");
            }
            return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
        }
    }
}
