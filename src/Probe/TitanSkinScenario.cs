using System;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class TitanSkinScenario
    {
        internal const string Name = "titan-skin-combat";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }

        internal static void ConfigureBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            if (!Enabled || Prepared) return;
            CharacterData boss = managers.GetSaveManager().GetAllGameData()
                .FindCharacterData("c2f466ae-fbc5-4209-aeff-73ce04d625eb")!;
            if (boss == null || boss.GetAssetKey() != "Boss_Level1_Basic")
                throw new InvalidOperationException("Titan Skin capture could not find Boss_Level1_Basic.");

            StatusEffectStackData[] original = boss.GetStartingStatusEffects() ?? Array.Empty<StatusEffectStackData>();
            if (original.Any(status => status.statusId == "titanskin"))
                throw new InvalidOperationException("Boss_Level1_Basic already has Titan Skin in its native definition.");
            AccessTools.Field(typeof(CharacterData), "startingStatusEffects").SetValue(boss,
                original.Concat(new[] { new StatusEffectStackData { statusId = "titanskin", count = 2 } }).ToArray());
            if (boss.GetStartingStatusEffects().Count(status => status.statusId == "titanskin" && status.count == 2) != 1)
                throw new InvalidOperationException("Could not install exactly two Titan Skin stacks for the isolated capture.");

            Prepared = true;
            log.LogInfo("TITAN-SKIN-COMBAT-PREPARED Boss_Level1_Basic; starting titanskin stacks=2; original character data is process-local.");
        }
    }
}
