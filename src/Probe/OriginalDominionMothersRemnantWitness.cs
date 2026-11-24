#if DEVELOPMENT_PROBE
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalDominionMothersRemnantWitness
    {
        private const string OwnerId = "caa8e46f-304b-4e0b-bf09-05b6c71b162a";
        private const string OwnerKey = "Echoes_FriendlyUnitsGetDominionBuffs";
        private const string MotherId = "6b5ccd78-056a-4599-b05a-368bb9e1d6bc";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_DOMINION_MOTHERS_REMNANT") == "1";
        internal static bool Acquired => listener != null;

        private static RelicEffectAddStartingUpgradeToUnitCards? listener;
        private static string[][] poolUpgradeIds = Array.Empty<string[]>();
        private static CardState? source;
        private static CardState? observedCopy;
        private static CardState[] deckBefore = Array.Empty<CardState>();
        private static string[] sourceUpgrades = Array.Empty<string>();
        private static PlayCardAction? action;
        private static int energyBefore;
        private static RngSample? rngBefore;
        private static CallbackSample? callbackBefore, callbackAfter;

        internal static void Acquire(SaveManager save)
        {
            if (!MixScenario.MothersRemnant3Mode || Application.version != "2.3.1" ||
                typeof(SaveManager).Module.ModuleVersionId != new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d") ||
                save.PreviewMode || save.InTestScenario || save.GetGameSequence() == SaveData.GameSequence.InBattle ||
                save.GetCurrentDistance() != 0 || save.GetMutators().Count != 0 ||
                save.GetCovenants().Count != 0 || save.GetSelectedSouls().Count != 0)
                throw new InvalidOperationException("Dominion requires the original isolated 2.3.1 opening battle.");

            MutatorData owner = save.GetAllGameData().FindMutatorData(OwnerId)
                ?? throw new InvalidOperationException("The original Dominion mutator is unavailable.");
            if (owner.name != OwnerKey || owner.DisallowInDeploymentPhase ||
                !save.IsDlcAvailableWhenStartingRun(owner.GetRequiredDLC()) ||
                owner.GetEffects() is not [var unitUpgrade, var draftUpgrade] ||
                unitUpgrade.GetEffectClassName() != nameof(RelicEffectAddStartingUpgradeToUnitCards) ||
                unitUpgrade.GetEffectConditions().Count != 0 ||
                unitUpgrade.GetParamInt2() != (int)UnitEnhancerPoolSelection.SelectRandom ||
                draftUpgrade.GetEffectClassName() != nameof(RelicEffectAddStartingUpgradeToCardDrafts) ||
                unitUpgrade.GetParamEnhancerPool() is not EnhancerPool pool)
                throw new InvalidOperationException("The original Dominion owner or upgrade pool changed.");

            var choices = new List<EnhancerData>();
            pool.GetAllChoices(choices);
            poolUpgradeIds = choices.Select(enhancer => enhancer.GetEffects()
                .Select(effect => effect.GetParamCardUpgradeData()?.GetID() ?? "").ToArray()).ToArray();
            if (!poolUpgradeIds.SelectMany(upgrades => upgrades).Any(id => id.Length > 0))
                throw new InvalidOperationException("The original Dominion pool has no card upgrade.");

            save.AddMutator(owner);
            var selected = save.GetMutators().Single();
            if (selected.GetRelicDataID() != OwnerId || selected.GetEffects() is not
                [RelicEffectAddStartingUpgradeToUnitCards active, RelicEffectAddStartingUpgradeToCardDrafts])
                throw new InvalidOperationException("Native Dominion did not activate its ordered effects.");
            listener = active;
        }

        internal static bool Tick(FullBattleTrace trace, AllGameManagers managers)
        {
            var save = managers.GetSaveManager();
            var combat = managers.GetCombatManager();
            var cards = managers.GetCardManager();
            var rooms = managers.GetRoomManager()
                ?? throw new InvalidOperationException("Native deployment rooms are unavailable.");
            var deck = save.GetDeckState();
            if (!Acquired || !MixScenario.Acquired || save.PreviewMode || !combat.IsPlacementPhase ||
                combat.GetTurnCount() != 0 || save.GetCollectedRelics().Count(owner => owner.GetRelicDataID() == MotherId) != 1)
                throw new InvalidOperationException("Expected original Dominion and MothersRemnant3 in deployment.");

            if (action == null)
            {
                source = cards.GetHand().FirstOrDefault(card => card.GetCardDataID() == MagneticRelayScenario.StewardId &&
                    deck.Contains(card) && card.GetCardType() == CardType.Monster && !card.IsChampionCard() && card.CanBeCopied())
                    ?? throw new InvalidOperationException("Expected an original owned Train Steward in the deployment hand.");
                sourceUpgrades = Upgrades(source);
                if (sourceUpgrades.Intersect(poolUpgradeIds.SelectMany(upgrades => upgrades)).Any())
                    throw new InvalidOperationException("The original played card already has a Dominion pool upgrade.");

                int roomIndex = rooms.GetSelectedRoom();
                RoomState room = rooms.GetRoom(roomIndex);
                SpawnPoint drop = room.GetFirstEmptyMonsterPoint()
                    ?? throw new InvalidOperationException("Missing original deployment space for the Steward.");
                if (!room.IsRoomEnabled() || room.GetIsPyreRoom())
                    throw new InvalidOperationException("The selected deployment room cannot accept a monster.");
                if (!cards.CanPlayHandCard(source, roomIndex, drop, null, null, out var error))
                    throw new InvalidOperationException("Native Steward play failed its legal input test: " + error);
                deckBefore = deck.ToArray();
                energyBefore = managers.GetPlayerManager().GetEnergy();
                rngBefore = new RngSample();
                action = new PlayCardAction(trace.CardId(source), roomIndex, drop.GetIndexInRoom());
                if (!cards.PlayCard(cards.GetHand().IndexOf(source), drop, ref error))
                    throw new InvalidOperationException("Native paid Steward play was rejected: " + error);
                return false;
            }

            var rngAfter = new RngSample();
            int energyAfter = managers.GetPlayerManager().GetEnergy();
            CardState copy = deck.Except(deckBefore).Single();
            if (ReferenceEquals(copy, source) || !ReferenceEquals(copy, observedCopy) ||
                deck.Count != deckBefore.Length + 1 || copy.GetCardDataID() != source!.GetCardDataID() ||
                callbackBefore == null || callbackAfter == null || energyAfter >= energyBefore ||
                !sourceUpgrades.SequenceEqual(Upgrades(source)))
                throw new InvalidOperationException("Native paid play did not produce one new paid-card copy through Dominion.");

            string addedUpgrade = callbackAfter.Upgrades.Except(callbackBefore.Upgrades).Single();
            if (callbackAfter.Upgrades.Length != callbackBefore.Upgrades.Length + 1 ||
                !poolUpgradeIds.SelectMany(upgrades => upgrades).Contains(addedUpgrade))
                throw new InvalidOperationException("Native Dominion did not apply one original pool upgrade to the copy.");

            var actors = new List<CharacterState>();
            rooms.GetRoom(action.RoomIndex).AddCharactersToList(actors, Team.Type.Monsters);
            CharacterState actor = actors.Single(unit => ReferenceEquals(unit.GetSpawnerCard(), source));
            using var archive = NativeFixtureCapture.Capture(new
            {
                Schema = 1, GameVersion = Application.version, GameModuleMvid = typeof(SaveManager).Module.ModuleVersionId,
                Boundary = "OriginalPaidDominionMothersRemnant", OwnerId, OwnerKey, MotherId,
                PoolUpgradeIds = poolUpgradeIds, Action = action, ActorId = trace.UnitId(actor),
                Source = new { Id = trace.CardId(source), DataId = source.GetCardDataID(), Upgrades = sourceUpgrades },
                Copy = new { Id = trace.CardId(copy), DataId = copy.GetCardDataID(), Upgrades = Upgrades(copy) },
                DeckCountBefore = deckBefore.Length, DeckCountAfter = deck.Count,
                EnergyBefore = energyBefore, EnergyAfter = energyAfter,
                RngBefore = rngBefore, RngAfter = rngAfter, CallbackBefore = callbackBefore, CallbackAfter = callbackAfter,
                AddedUpgrade = addedUpgrade
            });
            using var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!,
                "dominion-mothers-remnant.mt2f"));
            archive.Write(stream);
            return true;
        }

        private static string[] Upgrades(CardState card) => card.GetCardStateModifiers().GetCardUpgrades()
            .Select(upgrade => upgrade.GetCardUpgradeDataId()).ToArray();

        private sealed class RngSample
        {
            public uint[] Battle { get; } = RngCalibration.Words(RandomManager.GetState(RngId.Battle));
            public uint[] Rewards { get; } = RngCalibration.Words(RandomManager.GetState(RngId.Rewards));
            public uint[] CardDraw { get; } = RngCalibration.Words(RandomManager.GetState(RngId.CardDraw));
            public uint[] BattleTest { get; } = RngCalibration.Words(RandomManager.GetState(RngId.BattleTest));
        }

        private sealed class CallbackSample
        {
            public string[] Upgrades { get; }
            public RngSample Rng { get; } = new();
            internal CallbackSample(CardState card) => Upgrades = OriginalDominionMothersRemnantWitness.Upgrades(card);
        }

        [HarmonyPatch(typeof(RelicEffectAddStartingUpgradeToUnitCards), nameof(RelicEffectAddStartingUpgradeToUnitCards.OnCardAddedToDeck))]
        private static class ObserveUpgrade
        {
            private static bool Prepare() => Enabled;

            private static void Prefix(RelicEffectAddStartingUpgradeToUnitCards __instance,
                CardAddedToDeckRelicEffectParams relicEffectParams, out bool __state)
            {
                CardState card = relicEffectParams.cardState;
                __state = action != null && ReferenceEquals(__instance, listener) && source != null &&
                    !ReferenceEquals(card, source) && card.GetCardDataID() == source.GetCardDataID() &&
                    !relicEffectParams.saveManager.PreviewMode;
                if (!__state) return;
                if (callbackBefore != null) throw new InvalidOperationException("Duplicate native Dominion copy callback.");
                observedCopy = card;
                callbackBefore = new CallbackSample(card);
            }

            private static void Postfix(CardAddedToDeckRelicEffectParams relicEffectParams, bool __state)
            {
                if (__state) callbackAfter = new CallbackSample(relicEffectParams.cardState);
            }
        }
    }
}
#endif
