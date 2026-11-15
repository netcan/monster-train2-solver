#if DEVELOPMENT_PROBE
using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalLateAttackScenario
    {
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "original-late-attack";
        internal static bool Prepared { get; private set; }
        internal static object? Definition { get; private set; }
        private static MutatorData? original;
        internal static object? CurrentDefinition => original == null ? null : Describe(original);
        private static object Describe(MutatorData data) => new { Id = data.GetID(), Key = data.name,
            Effects = data.GetEffects().Select(effect => new { Type = effect.GetEffectClassName(), Multiplier = effect.GetParamFloat(),
                IgnoreChampion = effect.GetParamBool(), Team = effect.GetParamSourceTeam().ToString(),
                Conditions = effect.GetEffectConditions().Count }).ToArray() };
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            OriginalPercentageStatsScenario.Prepare(managers, log);
            var save = managers.GetSaveManager(); var all = save.GetAllGameData();
            original = all.FindMutatorData("8af5b701-3d86-4b69-8117-758e1f4cb16d");
            if (original == null || original.name != "Echoes_FriendlyUnitsHalfAttack" || original.GetEffects().Count != 1 ||
                original.GetEffects()[0].GetEffectClassName() != "RelicEffectMultiplyAttackLate" ||
                original.GetEffects()[0].GetParamFloat() != .5f || !original.GetEffects()[0].GetParamBool() ||
                original.GetEffects()[0].GetParamSourceTeam() != Team.Type.Monsters)
                throw new InvalidOperationException("Original half-attack mutator changed.");
            Definition = Describe(original); save.AddMutator(original);
            var steward = all.FindCardData(MagneticRelayScenario.StewardId)!;
            AccessTools.Field(typeof(CharacterData), "attackDamage").SetValue(steward.GetSpawnCharacterData(), 81);
            foreach (var card in managers.GetCardManager().GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            Prepared = true; log.LogInfo("ORIGINAL-LATE-ATTACK-PREPARED unchanged half-attack plus double-enemy-health; authored odd-attack Steward control.");
        }
    }
}
#endif
