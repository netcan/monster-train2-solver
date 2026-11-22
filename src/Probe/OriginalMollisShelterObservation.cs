#if DEVELOPMENT_PROBE
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalMollisShelterObservation
    {
        internal const string Name = "original-mollis-shelter-observation";
        private const string ShelterId = "1754dece-cf0b-455e-b9ca-d238134d7762";
        private const string MollisId = "391c87fa-5583-469d-88be-326b0afbf0b7";
        private const string StewardId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";
        private static CardState[]? cardsToCast;
        private static CardData shelter = null!;
        private static SoulData mollis = null!;
        private static CardLifecycleUpgrade installed = null!;
        private static JToken definition = null!;
        private static readonly List<object> outcomes = new();
        private static bool prepared, deployed, corruptionSet;
        private static BattleTurnState? before;
        private static CardTraitCompositionState? traits;
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Completed { get; private set; }

        internal static void AcquireBeforeBattle(AllGameManagers managers)
        {
            if (cardsToCast != null) return;
            SaveManager save = managers.GetSaveManager();
            if (typeof(CardState).Module.ModuleVersionId != new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d") ||
                save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle || save.GetSelectedSouls().Count != 0 ||
                save.GetMutators().Count != 0 || save.GetCovenants().Count != 0)
                throw new InvalidOperationException("Shelter observation requires the isolated base game 2.3.1.");
            var data = save.GetAllGameData();
            shelter = data.FindCardData(ShelterId);
            mollis = data.FindSoulData(MollisId);
            if (shelter.name != "Shelter" || shelter.GetCardType() != CardType.Spell ||
                shelter.GetEffects().Single().GetEffectStateName() != nameof(CardEffectAddStatusEffect) ||
                !shelter.GetTraits().Select(trait => trait.GetTraitStateName()).SequenceEqual(
                    new[] { nameof(CardTraitScalingAddStatusEffect), nameof(CardTraitExhaustState) }) ||
                mollis.name != "MollisT3" || mollis.GetTierLevel() != 3 ||
                mollis.GetSoulCardUpgrade().GetTraitDataUpgrades().Single().GetTraitStateName() != nameof(CardTraitJuice4))
                throw new InvalidOperationException("Original Shelter or Mollis definition differs.");
            definition = Describe();
            CardState OwnedShelter() => save.AddCardToDeck(shelter,
                applyExistingRelicModifiers: false, showAnimation: false, notifyDeckCount: false);
            CardState withSoul = OwnedShelter();
            installed = OriginalMollisCycleOfLifeScenario.AttachSoul(managers, mollis, withSoul);
            cardsToCast = new[] { OwnedShelter(), withSoul };
        }

        // Called by the existing settled battle gate. Return true to end deployment.
        internal static bool Tick(AllGameManagers managers, ManualLogSource log)
        {
            var gameCards = managers.GetCardManager();
            var combat = managers.GetCombatManager();
            var rooms = managers.GetRoomManager();
            var room = rooms.GetRoom(0);
            if (cardsToCast == null) throw new InvalidOperationException("Missing owned original Shelter cards.");
            if (rooms.GetSelectedRoom() != 0)
            {
                managers.GetSaveManager().StartCoroutine(rooms.GetRoomUI().SetSelectedRoom(0));
                return false;
            }
            if (!prepared)
            {
                foreach (CardState card in cardsToCast)
                {
                    if (!gameCards.GetHand().Contains(card) &&
                        (!gameCards.GetDrawPile().Contains(card) || !gameCards.DrawSpecificCard(card, useEffectDelay: false)))
                        throw new InvalidOperationException("Original Shelter did not reach deployment.");
                    var freeze = new CardTraitData();
                    freeze.Setup(nameof(CardTraitFreeze));
                    card.AddTemporaryTrait(freeze, managers.GetCoreManagers());
                }
                prepared = true;
                return false;
            }
            if (combat.IsPlacementPhase)
            {
                if (deployed) return true;
                CardState steward = gameCards.GetHand().First(card => card.GetCardDataID() == StewardId);
                Play(gameCards, steward, room.GetFirstEmptyMonsterPoint());
                deployed = true;
                return false;
            }
            var allies = new List<CharacterState>();
            room.AddCharactersToList(allies, Team.Type.Monsters);
            if (!deployed || allies.Count(unit => unit.IsAlive && !unit.IsPyreHeart()) != 1 || combat.GetTurnCount() != 1)
                throw new InvalidOperationException("Expected one original allied unit on the first normal turn.");
            if (!corruptionSet)
            {
                var adjustment = room.AdjustCorruption(combat, managers.GetRelicManager(),
                    3 - room.GetCurrentCorruption(), 4, skipTriggersAndNotifications: true);
                while (adjustment.MoveNext()) { }
                corruptionSet = true;
                return false;
            }
            if (room.GetCurrentCorruption() != 3) throw new InvalidOperationException("Shelter needs three Corruption on the selected floor.");
            var trace = FullBattleTrace.Active!;
            CardState next = cardsToCast[outcomes.Count];
            if (before == null)
            {
                traits = CardTraitCompositionProbe.Capture(next, combined: true);
                before = trace.CaptureDecision();
                Play(gameCards, next, drop: null);
                return false;
            }
            BattleTurnState after = trace.CaptureDecision();
            int cardId = trace.CardId(next);
            if (gameCards.GetHand().Contains(next) || before.Energy - after.Energy != 1)
                throw new InvalidOperationException("Original Shelter did not settle as one paid native cast.");
            outcomes.Add(new { CardId = cardId, WithMollis = outcomes.Count == 1, Traits = traits!, Before = before, After = after });
            before = null;
            if (outcomes.Count != cardsToCast.Length) return false;
            JToken definitionAfter = Describe();
            if (!JToken.DeepEquals(definition, definitionAfter))
                throw new InvalidOperationException("Original Shelter or Mollis definition changed.");
            using var document = NativeFixtureCapture.Capture(new
            {
                Schema = 1, Boundary = "OriginalMollisShelterCasts", GameVersion = UnityEngine.Application.version,
                GameModuleMvid = typeof(CardState).Module.ModuleVersionId, Definition = definition, DefinitionAfter = definitionAfter,
                InstalledUpgrade = installed, Corruption = room.GetCurrentCorruption(), Cases = outcomes
            });
            using var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "mollis-shelter.mt2f"));
            document.Write(stream);
            log.LogInfo("ORIGINAL-MOLLIS-SHELTER recorded paid unmodified and Mollis T3 casts at three Corruption.");
            Completed = true;
            return false;
        }

        private static void Play(CardManager manager, CardState card, SpawnPoint? drop)
        {
            int index = manager.GetHand().IndexOf(card);
            if (index < 0) throw new InvalidOperationException("Required original card was absent from hand.");
            if (!manager.CanPlayHandCard(card, 0, drop, null, null, out var error) || !manager.PlayCard(index, drop, ref error))
                throw new InvalidOperationException("Original native card play was rejected: " + error);
        }

        private static JToken Describe()
        {
            var upgrade = new CardUpgradeState();
            upgrade.Setup(mollis.GetSoulCardUpgrade());
            return JToken.FromObject(new
            {
                Card = BattleActionProbe.Definition(shelter),
                RawEffects = shelter.GetEffects().Select(RuleCatalogProbe.Effect).ToArray(),
                RawTraits = shelter.GetTraits().Select(CardSourceTraitProbe.Capture).ToArray(),
                Soul = mollis.name, SoulId = mollis.GetID(), Upgrade = CardUpgradeLifecycleProbe.Definition(upgrade),
                Filters = mollis.GetSoulCardUpgrade().GetFilters().Select(CardUpgradeMaskProbe.Rule).ToArray()
            });
        }
    }
}
#endif
