#if DEVELOPMENT_PROBE
using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    // Disposable-profile run configuration. Startup is performed by the native
    // RunSetup/StartRun path; the hooks below only observe its deck effects.
    internal static class StartingCovenantScenario
    {
        private static int Level => int.Parse(Environment.GetEnvironmentVariable("MT2_PROBE_STARTING_COVENANT_LEVEL") ?? "0");
        private static object? selection;
        private static readonly List<object> calls = new List<object>();
        private static readonly List<object> queries = new List<object>();
        private static readonly HashSet<string> observed = new HashSet<string>();
        internal static object? Evidence { get; private set; }

        internal static void Select(RunSetupScreen setup, SaveManager save)
        {
            if (Level == 0) return;
            if (selection != null) throw new InvalidOperationException("Starting Covenant selection was repeated.");
            int before = save.GetAscensionLevel();
            int unlocked = save.GetMetagameSave().GetMaxAscensionLevel();
            AccessTools.Method(typeof(RunSetupScreen), "HandleAscensionLevelChanged").Invoke(setup, new object[] { Level });
            if (save.GetAscensionLevel() != Level) throw new InvalidOperationException("Native Covenant run setup did not retain its selected level.");
            selection = new { Kind = "SelectNativeCovenantAtRunSetup", RequestedLevel = Level,
                LevelBefore = before, LevelAfter = save.GetAscensionLevel(), MaxUnlockedLevelBefore = unlocked,
                EarnedUnlockClaimed = false, StartupEffectsInvokedByProbe = false };
        }

        internal static void Observe(SaveManager save)
        {
            if (Level == 0 || Evidence != null) return;
            if (selection == null || save.GetCurrentDistance() != 0 || save.GetAscensionLevel() != Level ||
                save.GetStartingConditions().AscensionLevel != Level)
                throw new InvalidOperationException("Native Covenant startup lacks its original first-battle starting conditions.");
            Evidence = new { Kind = "NativeCovenantRunStartup", Selection = selection,
                AscensionLevel = save.GetAscensionLevel(), StartingLevel = save.GetStartingConditions().AscensionLevel,
                Owners = save.GetCovenants().Select(owner => Definition((CovenantData)owner.GetSourceRelicData())).ToArray(),
                ExpectedOwners = save.GetAllGameData().GetAllCovenantsForLevel(Level).Select(Definition).ToArray(),
                Catalog = save.GetAllGameData().GetAllCovenantDatas().Select(Definition).ToArray(),
                StartupCalls = calls, StartingDeck = Deck(save), StatQueries = queries };
        }

        private static object Definition(CovenantData data) => new { AssetKey = data.name, Id = data.GetID(),
            Level = data.AscensionLevel, EffectTypes = data.GetEffects().Select(effect => effect.GetEffectClassName()).ToArray() };
        private static object Deck(SaveManager save) => save.GetDeckState().Select(card => Level >= 5 ? (object)new {
            AssetKey = card.GetAssetName(), DataId = card.GetCardDataID(), IsChampion = card.IsChampionCard(),
            Upgrades = card.GetCardStateModifiers().GetCardUpgrades().Select(upgrade => new {
                Id = upgrade.GetCardUpgradeDataId(), Attack = upgrade.GetAttackDamage(),
                Health = upgrade.GetAdditionalHP(), CostReduction = upgrade.GetCostReduction() }).ToArray()
        } : new { AssetKey = card.GetAssetName(), DataId = card.GetCardDataID(), IsChampion = card.IsChampionCard() }).ToArray();
        private static object Rng() => Enum.GetValues(typeof(RngId)).Cast<RngId>().Select(id =>
            new { Id = id.ToString(), Words = RngCalibration.Words(RandomManager.GetState(id)) }).ToArray();
        internal static void ObserveStats(CharacterData data, object evidence)
        { if (Evidence != null && observed.Add(data.name)) queries.Add(evidence); }

        private sealed class Call
        {
            public string Effect { get; set; } = "";
            public string OwnerId { get; set; } = "";
            public object? Before { get; set; }
            public object? After { get; set; }
            public object? RngBefore { get; set; }
            public object? RngAfter { get; set; }
        }
        private static Call? Before(RelicEffectBase effect, SaveManager save)
        {
            if (Level == 0 || selection == null || effect.SourceRelicData is not CovenantData) return null;
            var call = new Call { Effect = effect.GetType().Name, OwnerId = effect.GetRelicDataId(), Before = Deck(save), RngBefore = Rng() };
            calls.Add(call); return call;
        }
        private static void After(Call? call, SaveManager save)
        { if (call != null) { call.After = Deck(save); call.RngAfter = Rng(); } }

        [HarmonyPatch(typeof(RelicEffectPurgeStarterDeck), "PurgeStarterDeck")]
        private static class PurgePatch
        {
            private static void Prefix(RelicEffectPurgeStarterDeck __instance, RelicEffectParams relicEffectParams, out Call? __state)
            { __state = Before(__instance, relicEffectParams.saveManager); }
            private static void Postfix(RelicEffectParams relicEffectParams, Call? __state) { After(__state, relicEffectParams.saveManager); }
        }
        [HarmonyPatch(typeof(RelicEffectAddCardSetStartOfRun), "ApplyEffect")]
        private static class AddPatch
        {
            private static void Prefix(RelicEffectAddCardSetStartOfRun __instance, RelicEffectParams relicEffectParams, out Call? __state)
            { __state = Before(__instance, relicEffectParams.saveManager); }
            private static void Postfix(RelicEffectParams relicEffectParams, Call? __state) { After(__state, relicEffectParams.saveManager); }
        }
        [HarmonyPatch(typeof(RelicEffectAddCardsStartOfRun), "ApplyEffect")]
        private static class AddCardsPatch
        {
            private static void Prefix(RelicEffectAddCardsStartOfRun __instance, RelicEffectParams relicEffectParams, out Call? __state)
            { __state = Before(__instance, relicEffectParams.saveManager); }
            private static void Postfix(RelicEffectParams relicEffectParams, Call? __state) { After(__state, relicEffectParams.saveManager); }
        }
    }
}
#endif
