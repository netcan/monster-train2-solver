#if DEVELOPMENT_PROBE
using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalPercentageStatsScenario
    {
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "original-percentage-stats";
        internal static bool Prepared { get; private set; }
        internal static object? Definition { get; private set; }
        private static MutatorData? original;
        internal static object? CurrentDefinition => original == null ? null : Describe(original);
        private static object Describe(MutatorData data) => new { Id = data.GetID(), Key = data.name,
            Effects = data.GetEffects().Select(effect => new { Type = effect.GetEffectClassName(), Percent = effect.GetParamInt(),
                Health = effect.GetParamBool(), Team = effect.GetParamSourceTeam().ToString(),
                Subtype = effect.GetParamCharacterSubtype()?.Key, RequiredMutator = effect.GetParamMutatorData()?.GetID(),
                Conditions = effect.GetEffectConditions().Count }).ToArray() };
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            var save = managers.GetSaveManager(); var all = save.GetAllGameData();
            if (save.PreviewMode || managers.GetCombatManager().GetTurnCount() != 0 || save.GetMutators().Count != 0 || save.GetCovenants().Count != 0)
                throw new InvalidOperationException("Original percentage scenario requires isolated deployment without other mutators/covenants.");
            original = all.FindMutatorData("af00fcb9-d921-4d32-8ab1-2c2ff3c2c39a");
            if (original == null || original.name != "Echoes_DoubleEnemyUnitHealth" || original.GetEffects().Count != 1 ||
                original.GetEffects()[0].GetEffectClassName() != RelicPercentageStatModel.Effect ||
                original.GetEffects()[0].GetParamInt() != 100 || !original.GetEffects()[0].GetParamBool() ||
                original.GetEffects()[0].GetParamSourceTeam() != Team.Type.Heroes)
                throw new InvalidOperationException("Original double-health mutator changed.");
            Definition = Describe(original); save.AddMutator(original);
            // An explicitly authored durable defender lets future native waves
            // reach the terminal boss without changing any mutator definition.
            var steward = all.FindCardData(MagneticRelayScenario.StewardId)!;
            AccessTools.Field(typeof(CardData), "cost").SetValue(steward, 0);
            AccessTools.Field(typeof(CharacterData), "attackDamage").SetValue(steward.GetSpawnCharacterData(), 80);
            AccessTools.Field(typeof(CharacterData), "health").SetValue(steward.GetSpawnCharacterData(), 5000);
            foreach (var card in managers.GetCardManager().GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            Prepared = true; log.LogInfo("ORIGINAL-PERCENTAGE-STATS-PREPARED unchanged double-enemy-health mutator; authored durable Steward control.");
        }
    }
}
#endif
