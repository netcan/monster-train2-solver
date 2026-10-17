using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalGarfieldBoxRandomCardScenario
    {
        internal const string Name = "original-garfield-box-random-card-per-turn";
        internal const string OwnerId = "d537c198-899f-4385-9552-f35f58826be4";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            if (Acquired || save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("GarfieldBoxRandomCardPerTurn must be acquired before battle.");
            CollectableRelicData relic = save.GetAllGameData().GetAllCollectableRelicData().Single(item => item.GetID() == OwnerId);
            RelicEffectData effect = relic.GetEffects().Single();
            CardPool pool = effect.GetParamCardPool() ?? throw new InvalidOperationException("GarfieldBox card pool is missing.");
            List<CardData> cards = pool.GetAllChoices(new List<CardData>());
            ClassData mainClass = save.GetMainClass();
            ClassData subClass = save.GetSubClass();
            int eligible = cards.Count(card => card.GetLinkedClass() == mainClass || card.GetLinkedClass() == subClass);
            if (relic.name != "GarfieldBoxRandomCardPerTurn" || !relic.DisallowInDeploymentPhase ||
                effect.GetEffectClassName() != "RelicEffectAddPrecachedBattleCardToHand" || effect.GetEffectConditions().Count != 0 ||
                pool.name != "MegaPool" || pool.GetNumCards() != 348 || cards.Count != 348 || eligible != 58 ||
                save.GetCollectedRelics().Any(item => item.GetRelicDataID() == OwnerId))
                throw new InvalidOperationException("The unchanged GarfieldBox owner, pool or isolated acquisition state changed.");
            save.AddRelic(relic);
            Acquired = true;
            log.LogInfo("ORIGINAL-GARFIELD-BOX-ACQUIRED unchanged owner; MegaPool has 348 ordered cards and 58 player-clan candidates.");
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            CombatManager combat = managers.GetCombatManager()!;
            if (Prepared || !Acquired || save.PreviewMode || save.GetGameSequence() != SaveData.GameSequence.InBattle ||
                combat.GetTurnCount() != 0 || save.GetCollectedRelics().Count(item => item.GetRelicDataID() == OwnerId) != 1)
                throw new InvalidOperationException("GarfieldBoxRandomCardPerTurn requires its isolated deployment state.");
            Prepared = true;
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(combat, true);
            log.LogInfo("ORIGINAL-GARFIELD-BOX-RANDOM-CARD-PREPARED unchanged owner; natural post-draw callbacks, no injected cards.");
        }
    }
}
