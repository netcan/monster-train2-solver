using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalAfterDrawGenerationScenario
    {
        internal const string Name = "original-after-draw-generation-relics";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static readonly string[] Ids = { "91211832-747e-4995-b4cb-7ca23c000f10", "00063533-9277-485c-b100-13637a228f9a",
            "81958958-6481-4847-afe1-cb651a041f6c", "5e5d863b-514c-4026-a9b0-ffe56af2a3eb" };
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            var save = managers.GetSaveManager(); var data = save.GetAllGameData(); var cards = managers.GetCardManager();
            if (Prepared || save.PreviewMode || managers.GetCombatManager().GetTurnCount() != 0)
                throw new InvalidOperationException("After-draw generation requires isolated deployment.");
            var owned = cards.GetAllCards(new List<CardState>()).ToArray();
            var stewards = owned.Select(card => data.FindCardData(card.GetCardDataID())!).Where(card =>
                card.GetSpawnCharacterData()?.name.StartsWith("TrainSteward", StringComparison.Ordinal) == true).Distinct().ToArray();
            foreach (var card in stewards)
            {
                var unit = card.GetSpawnCharacterData()!;
                Set(unit, "health", 40); Set(unit, "attackDamage", 12); Set(unit, "size", 1); Set(card, "cost", 0);
            }
            foreach (var card in owned) card.Setup(data.FindCardData(card.GetCardDataID())!, save);
            foreach (string id in Ids)
            {
                var owner = data.GetAllCollectableRelicData().Single(item => item.GetID() == id);
                if (save.GetCollectedRelics().Any(item => item.GetRelicDataID() == id) || !owner.DisallowInDeploymentPhase ||
                    owner.GetEffects().Count != 1 || owner.GetEffects()[0].GetEffectClassName() != "RelicEffectAddBattleCardToHand" ||
                    owner.GetEffects()[0].GetEffectConditions().Count != 0)
                    throw new InvalidOperationException("Original after-draw owner changed.");
                save.AddRelic(owner);
            }
            Prepared = true; Set(managers.GetCombatManager(), "combatStateChanged", true);
            log.LogInfo("ORIGINAL-AFTER-DRAW-GENERATION-PREPARED four unchanged owners; original filtered pools, ephemeral upgrade and natural post-draw callbacks.");
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
