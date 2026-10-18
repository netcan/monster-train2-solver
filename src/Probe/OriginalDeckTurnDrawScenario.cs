using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalDeckTurnDrawScenario
    {
        internal const string Name = "original-deck-turn-draw";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        internal static string Phase { get; private set; } = "natural";
        internal static object Evidence => new { OwnerId = RelicDeckTurnDrawModel.DataId,
            AssetKey = RelicDeckTurnDrawModel.AssetKey, VisibleDeckCount = 50, Threshold = 25,
            Restored = Prepared, ControlCounts = new[] { 0, 24, 25, 49, 50, 24, 50 } };

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            AllGameData data = save.GetAllGameData();
            if (Acquired || save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle ||
                save.GetCollectedRelics().Any(owner => owner.GetRelicDataID() == RelicDeckTurnDrawModel.DataId))
                throw new InvalidOperationException("Deck-scaled draw requires one pre-battle acquisition.");
            CollectableRelicData relic = data.GetAllCollectableRelicData()
                .Single(item => item.GetID() == RelicDeckTurnDrawModel.DataId);
            CheckOriginal(relic);
            CardData steward = save.GetDeckState().Select(card => data.FindCardData(card.GetCardDataID()))
                .First(card => card?.GetSpawnCharacterData()?.name.StartsWith("TrainSteward", StringComparison.Ordinal) == true)!;
            foreach (CardData card in save.GetDeckState().Select(item => data.FindCardData(item.GetCardDataID()))
                .Where(item => item?.GetSpawnCharacterData() != null).Cast<CardData>().Distinct())
            {
                CharacterData unit = card.GetSpawnCharacterData()!;
                Set(unit, "health", 80); Set(unit, "attackDamage", 20); Set(unit, "size", 1); Set(card, "cost", 0);
            }
            int initialCount = save.GetVisibleDeckCount();
            if (initialCount >= 50) throw new InvalidOperationException("Deck-scaled draw needs an isolated deck below 50 cards.");
            for (int count = initialCount; count < 50; count++)
                save.AddCardToDeck(steward, applyExistingRelicModifiers: false, showAnimation: false, notifyDeckCount: false);
            if (save.GetVisibleDeckCount() != 50) throw new InvalidOperationException("Visible deck did not reach 50 cards.");
            save.AddRelic(relic);
            Acquired = true;
            log.LogInfo("ORIGINAL-DECK-TURN-DRAW-ACQUIRED unchanged owner; visible save deck=50 threshold=25.");
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            if (Prepared || !Acquired || save.PreviewMode || managers.GetCombatManager()!.GetTurnCount() != 0)
                throw new InvalidOperationException("Deck-scaled draw controls require deployment.");
            CheckOriginal(save.GetAllGameData().GetAllCollectableRelicData()
                .Single(item => item.GetID() == RelicDeckTurnDrawModel.DataId));
            RelicEffectCardDrawPerPileStartOfTurn effect = save.GetCollectedRelics()
                .Single(owner => owner.GetRelicDataID() == RelicDeckTurnDrawModel.DataId)
                .GetEffects().OfType<RelicEffectCardDrawPerPileStartOfTurn>().Single();
            List<CardState> deck = save.GetDeckState();
            CardState[] original = deck.ToArray();
            if (original.Length != 50 || save.GetVisibleDeckCount() != 50)
                throw new InvalidOperationException("Threshold controls require an unhidden 50-card save deck.");
            var parameters = new RelicEffectParams { saveManager = save, relicManager = managers.GetRelicManager(),
                screenManager = managers.GetScreenManager() };
            try
            {
                Phase = "threshold-control";
                foreach (int count in new[] { 0, 24, 25, 49, 50, 24, 50 })
                {
                    deck.Clear(); deck.AddRange(original.Take(count));
                    effect.TestEffectTurnPhaseTiming(parameters, managers.GetCoreManagers());
                }
            }
            finally
            {
                deck.Clear(); deck.AddRange(original); Phase = "natural";
            }
            if (!deck.SequenceEqual(original) || save.GetVisibleDeckCount() != 50)
                throw new InvalidOperationException("Threshold controls failed to restore the exact save deck.");
            Prepared = true;
            FullBattleTrace.Active!.InvalidateCardRules();
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-DECK-TURN-DRAW-PREPARED native Test counts 0/24/25/49/50/24/50; exact deck restored for natural +2 after-draw callbacks.");
        }

        private static void CheckOriginal(CollectableRelicData relic)
        {
            RelicEffectData effect = relic.GetEffects().Single();
            if (relic.GetID() != RelicDeckTurnDrawModel.DataId || relic.name != RelicDeckTurnDrawModel.AssetKey ||
                !relic.DisallowInDeploymentPhase || effect.GetEffectClassName() != RelicDeckTurnDrawModel.Effect ||
                effect.GetParamMinInt() != 0 || effect.GetParamInt() != 25 || effect.GetParamMaxInt() != 25 ||
                effect.GetParamInt2() != 0 || effect.GetEffectConditions().Count != 0)
                throw new InvalidOperationException("Original DrawCardPerDeckSize identity or payload changed.");
        }

        private static void Set(object target, string field, object value) =>
            AccessTools.Field(target.GetType(), field)!.SetValue(target, value);
    }
}
