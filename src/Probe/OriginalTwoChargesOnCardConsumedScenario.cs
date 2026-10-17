using System;
using System.Linq;
using System.Collections.Generic;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalTwoChargesOnCardConsumedScenario
    {
        internal const string Name = "original-two-charges-on-card-consumed";
        internal const string RelicId = "217d553b-cddf-4a8c-a4c9-cfdb3c99389c";
        internal const string RelicKey = "2ChargesOnCardConsumed";
        internal const string CarrierTrait = "CardTraitExhaustState";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        private static string? carrierCardDataId;

        internal static CoverageRecord Evidence { get; } = new CoverageRecord();

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            if (Acquired) return;
            SaveManager save = managers.GetSaveManager();
            if (save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("2ChargesOnCardConsumed must be acquired before battle setup.");
            CollectableRelicData relic = save.GetAllGameData().GetAllCollectableRelicData()
                .Single(item => item.GetID() == RelicId);
            CheckOriginal(relic);
            if (save.GetCollectedRelics().Any(item => item.GetRelicDataID() == RelicId))
                throw new InvalidOperationException("2ChargesOnCardConsumed is already present in the native test run.");
            save.AddRelic(relic);
            Acquired = true;
            RelicEffectData effect = relic.GetEffects().Single();
            log.LogInfo("ORIGINAL-TWO-CHARGES-ACQUIRED unchanged relic=" + relic.name + " id=" + relic.GetID() +
                " amount=" + effect.GetParamInt() + " conditions=" + effect.GetEffectConditions().Count +
                " requiredTraits=" + string.Join(",", effect.GetTraits().Select(trait => trait.GetTraitStateName())) + ".");
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (Prepared) return;
            if (!Acquired || managers.GetCombatManager()!.GetTurnCount() != 0)
                throw new InvalidOperationException("2ChargesOnCardConsumed must be prepared on deployment turn.");

            SaveManager save = managers.GetSaveManager();
            AllGameData gameData = save.GetAllGameData();
            CardManager cards = managers.GetCardManager()!;
            CardState[] owned = cards.GetAllCards(new List<CardState>()).ToArray();
            CardData carrier = owned.Select(card => gameData.FindCardData(card.GetCardDataID())!)
                .FirstOrDefault(card => card.GetSpawnCharacterData()?.name.StartsWith("TrainSteward", StringComparison.Ordinal) == true)
                ?? throw new InvalidOperationException("2ChargesOnCardConsumed fixture needs an owned Train Steward card.");
            CardTraitData exhaust = gameData.GetAllCardData().Where(card => card != null).SelectMany(card => card.GetTraits())
                .FirstOrDefault(trait => trait.GetTraitStateName() == CarrierTrait)
                ?? throw new InvalidOperationException("Native CardTraitExhaustState data is unavailable.");
            List<CardTraitData> traits = carrier.GetTraits().ToList();
            if (!traits.Any(trait => trait.GetTraitStateName() == CarrierTrait)) traits.Add(exhaust);
            Set(carrier, "traits", traits);
            Set(carrier, "cost", 0);
            foreach (CardState card in owned.Where(card => card.GetCardDataID() == carrier.GetID()))
                card.Setup(carrier, save, setupStartingUpgrades: false);

            CardState? carrierInHand = cards.GetHand().FirstOrDefault(card => card.GetCardDataID() == carrier.GetID());
            if (carrierInHand == null)
            {
                carrierInHand = cards.AddNewCard(carrier, CardPile.HandPile, false, false, animate: false);
                if (carrierInHand == null || !cards.GetHand().Contains(carrierInHand))
                    throw new InvalidOperationException("Exhaust-trait Train Steward did not enter the native hand.");
            }

            RoomManager rooms = managers.GetRoomManager()!;
            RoomState[] openFloors = Enumerable.Range(0, rooms.GetNumRooms()).Select(rooms.GetRoom)
                .Where(room => !room.GetIsPyreRoom() && room.GetNumLivingCharacters(Team.Type.Monsters) == 0 &&
                    room.GetNumLivingCharacters(Team.Type.Heroes) == 0).ToArray();
            if (openFloors.Length < 2)
                throw new InvalidOperationException("2ChargesOnCardConsumed controls require two empty non-Pyre floors.");
            RoomState controlRoom = openFloors[0];
            RoomState targetRoom = openFloors[openFloors.Length - 1];
            carrierCardDataId = carrier.GetID();
            Evidence.Prepared = true;
            Evidence.CarrierCardDataId = carrierCardDataId;
            Evidence.CarrierCardInstanceId = FullBattleTrace.Active!.CardId(carrierInHand);
            Evidence.ControlRoomIndex = controlRoom.GetRoomIndex();
            Evidence.TargetRoomIndex = targetRoom.GetRoomIndex();
            Evidence.ControlRoomInitialCorruption = controlRoom.GetCurrentCorruption();
            Evidence.TargetRoomInitialCorruption = targetRoom.GetCurrentCorruption();
            Evidence.TargetRoomMaxCorruption = targetRoom.GetMaxCorruption();
            Prepared = true;
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(managers.GetCombatManager(), true);
            log.LogInfo("ORIGINAL-TWO-CHARGES-PREPARED carrier=" + carrier.name + " card=" + carrierCardDataId +
                " instance=" + Evidence.CarrierCardInstanceId + " trait=" + CarrierTrait + " controlRoom=" + Evidence.ControlRoomIndex +
                " targetRoom=" + Evidence.TargetRoomIndex + " initialCorruption=" + Evidence.TargetRoomInitialCorruption +
                "/" + Evidence.TargetRoomMaxCorruption + ".");
        }

        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            if (!Prepared || carrierCardDataId == null || state.PlayRules == null || state.Spawn.Train.Context == null) return null;
            IReadOnlyList<PlayCardAction> plays = BattleActionModel.EnumerateSupportedPlays(state);
            if (!Evidence.ControlCompleted)
            {
                PlayCardAction? control = plays.FirstOrDefault(action => action.RoomIndex == Evidence.ControlRoomIndex &&
                    action.CardInstanceId != Evidence.CarrierCardInstanceId &&
                    state.PlayRules.Cards.FirstOrDefault(rule =>
                        rule.DataId == state.Spawn.Train.Context.FindCard(action.CardInstanceId)?.DataId)?.CardType == "Monster" &&
                    !HasTrait(state.Spawn.Train.Context.FindCard(action.CardInstanceId), CarrierTrait));
                if (control == null)
                    throw new InvalidOperationException("No supported non-Exhaust control card was playable on the separate floor.");
                Evidence.ControlCardInstanceId = control.CardInstanceId;
                Evidence.ControlSelected = true;
                return control;
            }

            if (!Evidence.CarrierCompleted)
            {
                PlayCardAction? carrier = plays.FirstOrDefault(action => action.CardInstanceId == Evidence.CarrierCardInstanceId &&
                    action.RoomIndex == Evidence.TargetRoomIndex);
                if (carrier == null)
                    throw new InvalidOperationException("The exhaust-trait Train Steward was not a supported play on the target floor.");
                Evidence.CarrierSelected = true;
                return carrier;
            }
            return null;
        }

        internal static bool IsScenarioAction(PlayCardAction action) =>
            Evidence.ControlSelected && !Evidence.ControlCompleted && action.CardInstanceId == Evidence.ControlCardInstanceId ||
            Evidence.CarrierSelected && !Evidence.CarrierCompleted && action.CardInstanceId == Evidence.CarrierCardInstanceId;

        internal static void CompleteCardPlay(AllGameManagers managers)
        {
            if (!Prepared || Evidence.CarrierCompleted) return;
            RoomManager rooms = managers.GetRoomManager()!;
            if (Evidence.ControlSelected && !Evidence.ControlCompleted)
            {
                Evidence.ControlAfterCorruption = rooms.GetRoom(Evidence.ControlRoomIndex).GetCurrentCorruption();
                Evidence.ControlCompleted = true;
                if (Evidence.ControlAfterCorruption != Evidence.ControlRoomInitialCorruption)
                    Evidence.Error = "The native non-Exhaust control changed room corruption.";
                return;
            }
            if (Evidence.CarrierSelected)
            {
                RoomState target = rooms.GetRoom(Evidence.TargetRoomIndex);
                Evidence.TargetRoomAfterCorruption = target.GetCurrentCorruption();
                Evidence.CarrierCompleted = true;
                if (Evidence.TargetRoomAfterCorruption != Evidence.TargetRoomInitialCorruption + 1)
                    Evidence.Error = "The native eligible card play did not add exactly one corruption to its selected room.";
            }
        }

        private static bool HasTrait(CardInstanceState? card, string trait) =>
            card?.MaskDescriptor?.BaseTraits.Concat(card.MaskDescriptor.TemporaryTraits)
                .Any(item => item.RuntimeType == trait) == true;

        private static void Set(object target, string field, object value) =>
            AccessTools.Field(target.GetType(), field).SetValue(target, value);

        internal static void CheckOriginal(CollectableRelicData relic)
        {
            RelicEffectData effect = relic.GetEffects().Single();
            if (relic.GetID() != RelicId || relic.name != RelicKey ||
                effect.GetEffectClassName() != RelicModel.ModifyCorruptionOnCardPlayed || effect.GetParamInt() != 1 ||
                effect.GetEffectConditions().Count != 0 ||
                !effect.GetTraits().Select(trait => trait.GetTraitStateName()).SequenceEqual(new[] { CarrierTrait }))
                throw new InvalidOperationException("Original 2ChargesOnCardConsumed definition changed: id=" + relic.GetID() +
                    ", key=" + relic.name + ", effect=" + effect.GetEffectClassName() + ", amount=" + effect.GetParamInt() +
                    ", conditions=" + effect.GetEffectConditions().Count + ", traits=" +
                    string.Join(",", effect.GetTraits().Select(trait => trait.GetTraitStateName())) + ".");
        }

        internal sealed class CoverageRecord
        {
            public bool Prepared { get; set; }
            public string CarrierCardDataId { get; set; } = string.Empty;
            public int CarrierCardInstanceId { get; set; }
            public int ControlCardInstanceId { get; set; }
            public int ControlRoomIndex { get; set; } = -1;
            public int TargetRoomIndex { get; set; } = -1;
            public int ControlRoomInitialCorruption { get; set; } = -1;
            public int ControlAfterCorruption { get; set; } = -1;
            public int TargetRoomInitialCorruption { get; set; } = -1;
            public int TargetRoomAfterCorruption { get; set; } = -1;
            public int TargetRoomMaxCorruption { get; set; } = -1;
            public bool ControlSelected { get; set; }
            public bool ControlCompleted { get; set; }
            public bool CarrierSelected { get; set; }
            public bool CarrierCompleted { get; set; }
            public string? Error { get; set; }
        }
    }
}
