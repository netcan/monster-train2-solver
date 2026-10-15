using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalTurnDrawScenario
    {
        internal const string Name = "original-turn-draw-relics";
        internal static readonly string[] Ids = { "2058bafa-a15c-4787-9b9e-4dc7fdf09b74", "44aae53e-ee70-4b2c-8d25-a02f977349ee" };
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        internal static string Phase { get; private set; } = "natural";
        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            var save = managers.GetSaveManager(); var data = save.GetAllGameData();
            if (Acquired || save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle) throw new InvalidOperationException("Draw owners require pre-battle acquisition.");
            var steward = save.GetDeckState().Select(card => data.FindCardData(card.GetCardDataID())).First(card => card?.GetSpawnCharacterData() != null)!;
            foreach (var card in save.GetDeckState())
            {
                var definition = data.FindCardData(card.GetCardDataID()); var unit = definition?.GetSpawnCharacterData();
                if (unit == null) continue;
                Set(unit, "health", 80); Set(unit, "attackDamage", 20); Set(unit, "size", 1); Set(definition!, "cost", 0);
            }
            for (int i = 0; i < 12; i++) save.AddCardToDeck(steward);
            foreach (string id in Ids)
            {
                var owner = data.GetAllCollectableRelicData().Single(item => item.GetID() == id);
                if (owner.GetEffects().Count != 1 || owner.GetEffects()[0].GetEffectConditions().Count != 0) throw new InvalidOperationException("Original draw owner changed.");
                save.AddRelic(owner);
            }
            Acquired = true; log.LogInfo("ORIGINAL-TURN-DRAW-ACQUIRED two unchanged owners before native battle.");
        }
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (Prepared || !Acquired || managers.GetCombatManager().GetTurnCount() != 0) throw new InvalidOperationException("Draw controls require deployment.");
            var save = managers.GetSaveManager(); var raw = (SaveData)AccessTools.Property(typeof(SaveManager), "ActiveSaveData").GetValue(save);
            int hp = raw.GetTowerHP(); var effect = save.GetCollectedRelics().Single(owner => owner.GetRelicDataID() == Ids[1]).GetEffects().OfType<RelicEffectAdditionalDrawOnTowerFullHealth>().Single();
            var parameters = new RelicEffectParams { saveManager = save, relicManager = managers.GetRelicManager(), screenManager = managers.GetScreenManager() };
            try
            {
                foreach (int health in new[] { save.GetMaxTowerHP() - 1, 0, save.GetMaxTowerHP() })
                {
                    raw.SetTowerHP(health); Phase = "controlled-health";
                    effect.TestEffectStartOfBattle(parameters, managers.GetCoreManagers());
                    effect.TestEffectTurnPhaseTiming(parameters, managers.GetCoreManagers());
                }
            }
            finally { raw.SetTowerHP(hp); Phase = "restore"; effect.TestEffectTurnPhaseTiming(parameters, managers.GetCoreManagers()); Phase = "natural"; }
            Prepared = true; Set(managers.GetCombatManager(), "combatStateChanged", true);
            log.LogInfo("ORIGINAL-TURN-DRAW-PREPARED native full/partial/zero health Test controls and natural post-opening draw.");
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
