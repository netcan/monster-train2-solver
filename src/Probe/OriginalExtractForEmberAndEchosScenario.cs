using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalExtractForEmberAndEchosScenario
    {
        internal const string Name = "original-extract-for-ember-and-echos";
        internal const string OwnerId = "4d237444-5beb-43df-a545-c68a7ae86ba6";
        internal const string CarrierTrait = "CardTraitCorruptRestricted";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        internal static int CarrierInstanceId { get; private set; }
        internal static string CarrierDataId { get; private set; } = "";
        internal static bool Played { get; private set; }
        internal static readonly List<CallbackRecord> Callbacks = new List<CallbackRecord>();

        internal sealed class CallbackRecord
        {
            public string Kind { get; set; } = "";
            public string CardDataId { get; set; } = "";
            public int CardInstanceId { get; set; }
            public int RoomIndex { get; set; }
            public int Amount { get; set; }
            public int BeforeEnergy { get; set; }
            public int AfterEnergy { get; set; }
            public int BeforeCorruption { get; set; }
            public int AfterCorruption { get; set; }
            public bool Completed { get; set; }
        }

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            if (Acquired || save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("ExtractForEmberAndEchos must be acquired before battle in an isolated save.");
            CollectableRelicData relic = save.GetAllGameData().GetAllCollectableRelicData()
                .Single(item => item.GetID() == OwnerId);
            CheckOriginal(relic);
            if (save.GetCollectedRelics().Any(item => item.GetRelicDataID() == OwnerId))
                throw new InvalidOperationException("ExtractForEmberAndEchos is already acquired.");
            save.AddRelic(relic);
            Acquired = true;
            log.LogInfo("ORIGINAL-EXTRACT-EMBER-AND-ECHOS-ACQUIRED unchanged +1 Ember/+1 Corruption effects for Corrupt Restricted cards.");
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            CombatManager combat = managers.GetCombatManager()!;
            if (Prepared || !Acquired || save.PreviewMode || combat.GetTurnCount() != 0 ||
                !save.GetCollectedRelics().Any(item => item.GetRelicDataID() == OwnerId))
                throw new InvalidOperationException("ExtractForEmberAndEchos requires its isolated deployment state.");

            AllGameData gameData = save.GetAllGameData();
            CardState[] owned = managers.GetCardManager()!.GetAllCards(new List<CardState>()).ToArray();
            CardData carrier = owned.Select(card => gameData.FindCardData(card.GetCardDataID())!)
                .FirstOrDefault(card => card != null && card.GetSpawnCharacterData()?.name.StartsWith("TrainSteward", StringComparison.Ordinal) == true)
                ?? throw new InvalidOperationException("ExtractForEmberAndEchos needs an owned Train Steward carrier.");
            CardTraitData restricted = gameData.GetAllCardData().Where(card => card != null).SelectMany(card => card.GetTraits())
                .FirstOrDefault(trait => trait.GetTraitStateName() == CarrierTrait)
                ?? throw new InvalidOperationException("Native CardTraitCorruptRestricted definition is unavailable.");
            CardTraitData testTrait = restricted.Copy();
            testTrait.SetParamInt(0);
            AccessTools.Field(typeof(CardTraitData), "paramBool")!.SetValue(testTrait, false);
            Set(carrier, "traits", new List<CardTraitData> { testTrait });
            Set(carrier, "cost", 2);
            foreach (CardState card in owned.Where(card => card.GetCardDataID() == carrier.GetID()))
                card.Setup(carrier, save, setupStartingUpgrades: false);

            CardManager cards = managers.GetCardManager()!;
            CardState? inHand = cards.GetHand().FirstOrDefault(card => card.GetCardDataID() == carrier.GetID());
            if (inHand == null)
            {
                inHand = cards.AddNewCard(carrier, CardPile.HandPile, false, false, animate: false);
                if (inHand == null || !cards.GetHand().Contains(inHand))
                    throw new InvalidOperationException("Corrupt Restricted Train Steward did not enter the native hand.");
            }
            CarrierDataId = carrier.GetID();
            CarrierInstanceId = FullBattleTrace.Active!.CardId(inHand);
            Prepared = true;
            AccessTools.Field(typeof(CombatManager), "combatStateChanged")!.SetValue(combat, true);
            log.LogInfo("ORIGINAL-EXTRACT-EMBER-AND-ECHOS-PREPARED carrier=" + carrier.name + " card=" + CarrierInstanceId +
                " trait=" + CarrierTrait + " paramInt=0 paramBool=false cost=2; original relic definition unchanged.");
        }

        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            if (!Prepared || Played) return null;
            PlayCardAction? action = BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(item =>
                item.CardInstanceId == CarrierInstanceId && item.RoomIndex == 0);
            if (action != null) Played = true;
            return action;
        }

        internal static CallbackRecord Begin(string kind, CardPlayedRelicEffectParams parameters, int amount)
        {
            CardState card = parameters.cardState ?? throw new InvalidOperationException("Corrupt Restricted relic callback has no card.");
            if (!card.GetTraitStates().Any(trait => trait is CardTraitCorruptRestricted))
                throw new InvalidOperationException("Corrupt Restricted relic callback fired without its native trait.");
            RoomState room = AllGameManagers.Instance!.GetRoomManager()!.GetRoom(parameters.roomIndex)
                ?? throw new InvalidOperationException("Corrupt Restricted relic callback has no selected room.");
            var record = new CallbackRecord
            {
                Kind = kind,
                CardDataId = card.GetCardDataID(),
                CardInstanceId = FullBattleTrace.Active!.CardId(card),
                RoomIndex = parameters.roomIndex,
                Amount = amount,
                BeforeEnergy = AllGameManagers.Instance.GetPlayerManager().GetEnergy(),
                BeforeCorruption = room.GetCurrentCorruption()
            };
            Callbacks.Add(record);
            return record;
        }

        internal static IEnumerator Wrap(IEnumerator native, CallbackRecord record)
        {
            while (native.MoveNext()) yield return native.Current;
            AllGameManagers managers = AllGameManagers.Instance!;
            RoomState room = managers.GetRoomManager()!.GetRoom(record.RoomIndex)
                ?? throw new InvalidOperationException("Corrupt Restricted relic callback room disappeared.");
            record.AfterEnergy = managers.GetPlayerManager().GetEnergy();
            record.AfterCorruption = room.GetCurrentCorruption();
            record.Completed = true;
        }

        internal static bool IsScenarioAction(PlayCardAction action) =>
            Prepared && action.CardInstanceId == CarrierInstanceId;

        internal static bool IsSupportedCarrierTrait(CardState card, CardTraitState trait) =>
            Enabled && Prepared && card.GetCardDataID() == CarrierDataId &&
            trait is CardTraitCorruptRestricted && trait.GetParamInt() == 0;

        internal static void CheckOriginal(CollectableRelicData relic)
        {
            RelicEffectData[] effects = relic.GetEffects().ToArray();
            if (relic.GetID() != OwnerId || relic.name != "ExtractForEmberAndEchos" || effects.Length != 2 ||
                effects[0].GetEffectClassName() != "RelicEffectEnergyOnCorruptRestrictedPlayed" ||
                effects[1].GetEffectClassName() != "RelicEffectCorruptionOnCorruptRestrictedPlayed" ||
                effects.Any(effect => effect.GetParamInt() != 1 || effect.GetEffectConditions().Count != 0 ||
                    effect.GetTraits().Count != 0))
                throw new InvalidOperationException("ExtractForEmberAndEchos native effect order or +1 parameters changed.");
        }

        private static void Set(object target, string field, object value) =>
            AccessTools.Field(target.GetType(), field)!.SetValue(target, value);
    }

    [HarmonyPatch]
    internal static class OriginalExtractForEmberAndEchosProbe
    {
        [HarmonyPatch(typeof(RelicEffectEnergyOnCorruptRestrictedPlayed), nameof(RelicEffectEnergyOnCorruptRestrictedPlayed.ApplyEffectOnCardPlayed))]
        private static class EnergyPatch
        {
            private static void Postfix(RelicEffectEnergyOnCorruptRestrictedPlayed __instance,
                CardPlayedRelicEffectParams relicEffectParams, ref IEnumerator __result)
            {
                if (!OriginalExtractForEmberAndEchosScenario.Enabled || !OriginalExtractForEmberAndEchosScenario.Prepared) return;
                try
                {
                    int amount = (int)AccessTools.Field(__instance.GetType(), "energyChange")!.GetValue(__instance);
                    var record = OriginalExtractForEmberAndEchosScenario.Begin("Energy", relicEffectParams, amount);
                    __result = OriginalExtractForEmberAndEchosScenario.Wrap(__result, record);
                }
                catch (Exception error) { FullBattleTrace.Active?.CaptureFailure(error); }
            }
        }

        [HarmonyPatch(typeof(RelicEffectCorruptionOnCorruptRestrictedPlayed), nameof(RelicEffectCorruptionOnCorruptRestrictedPlayed.ApplyEffectOnCardPlayed))]
        private static class CorruptionPatch
        {
            private static void Postfix(RelicEffectCorruptionOnCorruptRestrictedPlayed __instance,
                CardPlayedRelicEffectParams relicEffectParams, ref IEnumerator __result)
            {
                if (!OriginalExtractForEmberAndEchosScenario.Enabled || !OriginalExtractForEmberAndEchosScenario.Prepared) return;
                try
                {
                    int amount = (int)AccessTools.Field(__instance.GetType(), "corruptionChange")!.GetValue(__instance);
                    var record = OriginalExtractForEmberAndEchosScenario.Begin("Corruption", relicEffectParams, amount);
                    __result = OriginalExtractForEmberAndEchosScenario.Wrap(__result, record);
                }
                catch (Exception error) { FullBattleTrace.Active?.CaptureFailure(error); }
            }
        }
    }
}
