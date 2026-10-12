using System;
using System.Linq;
using System.Collections.Generic;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalCardPlayMagicPowerScenario
    {
        internal const string Name = "original-card-play-magic-power";
        internal const string RelicId = "a4c87eb7-1c4e-4f63-af08-79f910e5a2e8";
        internal const string RelicKey = "MagicPowerWhenCardConsumed";
        internal const string CarrierTrait = "CardTraitExhaustState";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        private static string? carrierCardId;

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            if (Acquired) return;
            SaveManager save = managers.GetSaveManager();
            if (save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("Card-play magic-power relic must be acquired before battle setup.");
            CollectableRelicData relic = save.GetAllGameData().GetAllCollectableRelicData().Single(item => item.GetID() == RelicId);
            CheckOriginal(relic);
            if (save.GetCollectedRelics().Any(item => item.GetRelicDataID() == RelicId))
                throw new InvalidOperationException("Card-play magic-power relic is already present in the test run.");
            save.AddRelic(relic);
            Acquired = true;
            RelicEffectData effect = relic.GetEffects().Single();
            log.LogInfo("ORIGINAL-CARD-PLAY-MAGIC-POWER-ACQUIRED unchanged relic=" + relic.name + " id=" + relic.GetID() +
                " power=" + effect.GetParamInt() + " conditions=" + effect.GetEffectConditions().Count + " requiredTraits=" +
                string.Join(",", effect.GetTraits().Select(trait => trait.GetTraitStateName())) + ".");
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (Prepared) return;
            if (!Acquired) throw new InvalidOperationException("Original card-play magic-power relic was not acquired before battle.");
            if (managers.GetCombatManager()!.GetTurnCount() != 0)
                throw new InvalidOperationException("Card-play magic-power carrier must be prepared on deployment turn.");

            SaveManager save = managers.GetSaveManager();
            AllGameData gameData = save.GetAllGameData();
            CardManager cards = managers.GetCardManager()!;
            CardState[] owned = cards.GetAllCards(new List<CardState>()).ToArray();
            CardData steward = owned.Select(card => gameData.FindCardData(card.GetCardDataID())!)
                .FirstOrDefault(card => card.GetSpawnCharacterData()?.name.StartsWith("TrainSteward", StringComparison.Ordinal) == true)
                ?? throw new InvalidOperationException("Card-play magic-power fixture needs an owned Train Steward card.");
            CardTraitData carrierTrait = gameData.GetAllCardData().Where(card => card != null).SelectMany(card => card.GetTraits())
                .FirstOrDefault(trait => trait.GetTraitStateName() == CarrierTrait)
                ?? throw new InvalidOperationException("Native CardTraitExhaustState data is unavailable.");
            List<CardTraitData> traits = steward.GetTraits().ToList();
            if (!traits.Any(trait => trait.GetTraitStateName() == CarrierTrait)) traits.Add(carrierTrait);
            Set(steward, "traits", traits);
            Set(steward, "cost", 0);
            foreach (CardState card in owned.Where(card => card.GetCardDataID() == steward.GetID()))
                card.Setup(steward, save, setupStartingUpgrades: false);

            CardState? inHand = cards.GetHand().FirstOrDefault(card => card.GetCardDataID() == steward.GetID());
            if (inHand == null)
            {
                inHand = cards.AddNewCard(steward, CardPile.HandPile, false, false, animate: false);
                if (inHand == null || !cards.GetHand().Contains(inHand))
                    throw new InvalidOperationException("Exhaust-trait Train Steward did not enter the native hand.");
            }
            carrierCardId = steward.GetID();
            Prepared = true;
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(managers.GetCombatManager(), true);
            log.LogInfo("ORIGINAL-CARD-PLAY-MAGIC-POWER-PREPARED carrier=" + steward.name + " card=" + carrierCardId +
                " requiredTrait=" + CarrierTrait + "; original relic definition unchanged.");
        }

        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            if (!Prepared || carrierCardId == null) return null;
            return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(action =>
                source.Spawn.Train.Context!.FindCard(action.CardInstanceId)?.DataId == carrierCardId);
        }

        private static void Set(object target, string field, object value) =>
            AccessTools.Field(target.GetType(), field).SetValue(target, value);

        internal static void CheckOriginal(CollectableRelicData relic)
        {
            RelicEffectData effect = relic.GetEffects().Single();
            if (relic.GetID() != RelicId ||
                effect.GetEffectClassName() != "RelicEffectModifyCardMagicPowerOnCardPlayed" || effect.GetParamInt() != 1 ||
                effect.GetEffectConditions().Count != 0 ||
                !effect.GetTraits().Select(trait => trait.GetTraitStateName()).SequenceEqual(new[] { CarrierTrait }))
                throw new InvalidOperationException("Original MagicPowerWhenCardConsumed definition changed: id=" + relic.GetID() +
                    ", key=" + relic.name + ", effect=" + effect.GetEffectClassName() + ", power=" + effect.GetParamInt() +
                    ", conditions=" + effect.GetEffectConditions().Count + ", traits=" +
                    string.Join(",", effect.GetTraits().Select(trait => trait.GetTraitStateName())) + ".");
        }
    }
}
