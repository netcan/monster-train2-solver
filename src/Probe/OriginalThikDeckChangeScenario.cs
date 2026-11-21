#if DEVELOPMENT_PROBE
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalThikDeckChangeScenario
    {
        internal const string Name = "original-thik-deck-change";
        private const string GeneratorDataId = RunCardGenerationScenario.FirstId;
        private const string PurgeDataId = OriginalPurgeToExhaustScenario.SelfPurgeBlightId;
        private static readonly System.Reflection.PropertyInfo ActiveSave = AccessTools.Property(typeof(SaveManager), "ActiveSaveData");

        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool FullBattle => Enabled && Environment.GetEnvironmentVariable("MT2_PROBE_THIK_FULL_BATTLE") == "1";
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        internal static object? Definition { get; private set; }
        internal static Snapshot? AcquisitionBefore { get; private set; }
        internal static Snapshot? AcquisitionAfter { get; private set; }
        internal static Snapshot? PreparedState { get; private set; }
        internal static object? Cards { get; private set; }
        internal static readonly List<DeckChange> Callbacks = new();
        internal static readonly List<Snapshot> SettledPlays = new();
        internal const int RoomIndex = 2;

        private static RelicEffectAddPyreAttackAndHealthPerCardInDeck? deckEffect;
        private static CardState? generator;
        private static CardState? purgeCard;
        private static int playsStarted;
        private static int generatorId;
        private static int purgeCardId;
        private static int[] defenderIds = Array.Empty<int>();

        internal sealed class Snapshot
        {
            public int? Turn { get; set; }
            public string? Phase { get; set; }
            public bool RunningCombat { get; set; }
            public bool AllScenarioBossesDead { get; set; }
            public int? CachedDeckCount { get; set; }
            public int RawDeckCount { get; set; }
            public int VisibleDeckCount { get; set; }
            public string[] DeckDataIds { get; set; } = Array.Empty<string>();
            public bool SourceInSavedDeck { get; set; }
            public bool PurgeCardInSavedDeck { get; set; }
            public bool HealthBuffApplied { get; set; }
            public int RawSaveHP { get; set; }
            public int SaveHP { get; set; }
            public int RawSaveMaxHP { get; set; }
            public int SaveMaxHP { get; set; }
            public int SaveModifiedAttack { get; set; }
            public int? PyreHP { get; set; }
            public int? PyreMaxHP { get; set; }
            public int? PyreAttack { get; set; }
        }

        internal sealed class DeckChange
        {
            public Snapshot Before { get; }
            public Snapshot? After { get; set; }
            public int PlayNumber { get; }
            public int? Delta => Before.RawDeckCount - Before.CachedDeckCount;

            internal DeckChange(Snapshot before, int playNumber)
            {
                Before = before;
                PlayNumber = playNumber;
            }
        }

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            if (Acquired || save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle ||
                save.GetSelectedSouls().Count != 0 || save.GetRelicPersistentState().HasHealthBuffPerCardInDeckBeenAppliedThisRun())
                throw new InvalidOperationException("Thik requires pre-battle acquisition in a fresh run.");

            int tier = int.Parse(Environment.GetEnvironmentVariable("MT2_PROBE_THIK_TIER") ?? "3", CultureInfo.InvariantCulture);
            string soulId = tier switch
            {
                1 => "7254e0f3-890a-43d9-b767-76df34b8fa05",
                2 => "7ec532ca-5126-4a49-9895-67f33426f0c9",
                3 => "490d737b-fa0d-4f16-bb90-4ff22af11530",
                _ => throw new InvalidOperationException("Thik requires a tier between 1 and 3.")
            };
            int healthPerCard = tier + 2;
            int attackPerCard = tier == 3 ? 2 : 0;
            string soulKey = "ThikT" + tier;
            AllGameData data = save.GetAllGameData();
            SoulData soul = data.GetAllSoulDatas().Single(item => item.GetID() == soulId);
            var effects = soul.GetEffects();
            var healthEffect = effects.Single(effect => effect.GetEffectClassName() == RelicDeckPyreStatsModel.Effect);
            var copiesEffect = effects.Single(effect => effect.GetEffectClassName() == RelicDeckPyreStatsModel.CardCopies);
            if (soul.name != soulKey || soul.GetTierLevel() != tier || effects.Count != 2 ||
                healthEffect.GetParamInt() != attackPerCard || healthEffect.GetParamInt2() != healthPerCard ||
                healthEffect.GetEffectConditions().Count != 0 || copiesEffect.GetParamInt() != 1)
                throw new InvalidOperationException("The installed " + soulKey + " effects changed.");

            CardData blight = data.FindCardData(PurgeDataId) ?? throw new InvalidOperationException("The original SelfPurgeBlight is missing.");
            if (blight.name != "SelfPurgeBlight" || blight.GetCardType() != CardType.Blight ||
                !blight.GetTraits().Any(trait => trait.GetTraitStateName() == nameof(CardTraitSelfPurge)))
                throw new InvalidOperationException("The original SelfPurgeBlight definition changed.");

            purgeCard = save.AddCardToDeck(blight, showAnimation: false);
            save.AddSelectedSoul(soul);
            SoulState selected = save.GetSelectedSouls().Single(owner => owner.GetRelicDataID() == soul.GetID());
            if (selected.GetSoulType() != SoulData.SoulType.Artifact)
                throw new InvalidOperationException(soulKey + " requires its original Artifact soul attachment path.");
            deckEffect = selected.GetEffects().OfType<RelicEffectAddPyreAttackAndHealthPerCardInDeck>().Single();
            AcquisitionBefore = Capture(managers, save, deckEffect);
            SoulHelper.AttachSoulEffect(selected, playerInitiated: false, managers.GetRelicManager(), save);
            AcquisitionAfter = Capture(managers, save, deckEffect);
            if (AcquisitionAfter.CachedDeckCount != AcquisitionAfter.RawDeckCount || !AcquisitionAfter.HealthBuffApplied ||
                AcquisitionAfter.RawSaveMaxHP - AcquisitionBefore.RawSaveMaxHP != healthPerCard * AcquisitionAfter.RawDeckCount ||
                AcquisitionAfter.SaveModifiedAttack - AcquisitionBefore.SaveModifiedAttack != attackPerCard * AcquisitionAfter.RawDeckCount)
                throw new InvalidOperationException("The installed " + soulKey + " did not apply its native attachment buff.");

            Definition = new { Id = soul.GetID(), Key = soul.name, Tier = soul.GetTierLevel(),
                Effects = effects.Select(effect => new { Type = effect.GetEffectClassName(), Int = effect.GetParamInt(),
                    Int2 = effect.GetParamInt2(), Conditions = effect.GetEffectConditions().Count }).ToArray() };
            Acquired = true;
            log.LogInfo("ORIGINAL-THIK-DECK-CHANGE-ACQUIRED native " + soulKey + " " + soul.GetID() + " and permanent SelfPurgeBlight.");
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            CombatManager combat = managers.GetCombatManager()!;
            CardManager cards = managers.GetCardManager()!;
            if (!Acquired || Prepared || purgeCard == null || deckEffect == null || save.PreviewMode || combat.GetTurnCount() != 0)
                throw new InvalidOperationException("The original Thik deck-change scenario requires deployment with the selected soul.");

            CardData spell = save.GetAllGameData().FindCardData(GeneratorDataId) ??
                throw new InvalidOperationException("The original AnnihilateRoomDamage1 is missing.");
            if (spell.name != "AnnihilateRoomDamage1" || spell.GetCardType() != CardType.Spell ||
                !spell.GetEffects().Any(effect => effect.GetEffectStateName() == nameof(CardEffectAddRunCard)))
                throw new InvalidOperationException("The original AnnihilateRoomDamage1 definition changed.");

            FullBattleTrace trace = FullBattleTrace.Active!;
            if (FullBattle)
                defenderIds = OriginalRandomRoomCapacityScenario.CreateDeploymentControls(managers, 3, 1)
                    .Select(trace.CardId).ToArray();
            int slotsNeeded = cards.GetHand().Contains(purgeCard) ? 1 : 2;
            foreach (CardState card in cards.GetHand().Where(card => card != purgeCard).ToArray())
            {
                if (cards.GetHand().Count + slotsNeeded <= cards.GetMaxHandSize()) break;
                cards.MoveCardFromHandToTopOfDeck(card);
            }
            if (!cards.GetHand().Contains(purgeCard) &&
                (!cards.GetDrawPile().Contains(purgeCard) || !cards.DrawSpecificCard(purgeCard)))
                throw new InvalidOperationException("The original permanent SelfPurgeBlight could not be drawn into the deployment hand.");

            generator = cards.AddNewCard(spell, CardPile.HandPile, fromRelic: false, permanent: false, animate: false);
            if (generator == null || !cards.GetHand().Contains(generator) || !save.GetDeckState().Contains(purgeCard) ||
                save.GetDeckState().Contains(generator))
                throw new InvalidOperationException("The deployment hand requires the temporary generator and the permanent Purge control.");

            generatorId = trace.CardId(generator);
            purgeCardId = trace.CardId(purgeCard);
            Cards = new { GeneratorId = generatorId, GeneratorDataId, PurgeCardId = purgeCardId, PurgeDataId, DefenderIds = defenderIds };
            CardData blight = save.GetAllGameData().FindCardData(PurgeDataId);
            int energyNeeded = spell.GetCost() + blight.GetCost() - managers.GetPlayerManager().GetEnergy();
            if (energyNeeded > 0) managers.GetPlayerManager().AddEnergy(energyNeeded);
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(combat, true);
            PreparedState = Capture(managers, save, deckEffect);
            Prepared = true;
            log.LogInfo("ORIGINAL-THIK-DECK-CHANGE-PREPARED native Annihilate and SelfPurgeBlight, deck=" + PreparedState.RawDeckCount + ".");
        }

        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            BattleStatistics stats = state.Spawn.Train.Context!.Statistics!;
            int[] controls = new[] { generatorId, purgeCardId }.Concat(defenderIds).ToArray();
            int next = Array.FindIndex(controls, id => stats.Value(id, "TimesPlayed", "ThisBattle") == 0);
            if (next < 0) return null;
            int floor = next < 2 ? RoomIndex : next - 2;
            return BattleActionModel.EnumerateSupportedPlays(state, controls[next]).First(action => action.RoomIndex == floor);
        }

        internal static bool IsScenarioAction(PlayCardAction action) =>
            action.CardInstanceId == generatorId || action.CardInstanceId == purgeCardId || defenderIds.Contains(action.CardInstanceId);

        internal static bool PlayNext(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            CardManager cards = managers.GetCardManager()!;
            if (playsStarted > SettledPlays.Count)
                SettledPlays.Add(Capture(managers, save, deckEffect!));
            if (playsStarted == 2)
            {
                DeckChange[] mutations = Callbacks.Where(row => row.Delta != 0).ToArray();
                if (mutations.Length != 2 || mutations[0].PlayNumber != 1 || mutations[0].Delta != 1 ||
                    mutations[1].PlayNumber != 2 || mutations[1].Delta != -1 ||
                    SettledPlays.Any(row => !row.RunningCombat || row.AllScenarioBossesDead || row.SaveHP == 0) ||
                    !SettledPlays[0].PurgeCardInSavedDeck || SettledPlays[1].PurgeCardInSavedDeck)
                    throw new InvalidOperationException("The native Thik control plays did not settle with permanent addition then purge on a live train.");
                return true;
            }

            CardState card = playsStarted == 0 ? generator! : purgeCard!;
            int index = cards.GetHand().ToList().IndexOf(card);
            if (index < 0)
                throw new InvalidOperationException("The native Thik control card left the hand: " + card.GetCardDataID());
            if (!cards.CanPlayHandCard(card, RoomIndex, null, null, null, out var rejection))
                throw new InvalidOperationException("The native Thik control card is unplayable: " + card.GetCardDataID() + " " + rejection);
            playsStarted++;
            if (!cards.PlayCard(index, null, ref rejection))
                throw new InvalidOperationException("The native Thik control play failed: " + card.GetCardDataID() + " " + rejection);
            log.LogInfo("ORIGINAL-THIK-DECK-CHANGE-PLAY " + card.GetCardDataID() + " room=" + RoomIndex);
            return false;
        }

        internal static DeckChange? BeginDeckChanged(RelicEffectAddPyreAttackAndHealthPerCardInDeck effect, SaveManager save)
        {
            if (!Enabled || !Prepared || save.PreviewMode || !ReferenceEquals(deckEffect, effect)) return null;
            return new DeckChange(Capture(AllGameManagers.Instance!, save, effect), playsStarted);
        }

        internal static void EndDeckChanged(RelicEffectAddPyreAttackAndHealthPerCardInDeck effect, SaveManager save, DeckChange? change)
        {
            if (change == null) return;
            change.After = Capture(AllGameManagers.Instance!, save, effect);
            Callbacks.Add(change);
        }

        [HarmonyPatch(typeof(RelicEffectAddPyreAttackAndHealthPerCardInDeck), nameof(RelicEffectAddPyreAttackAndHealthPerCardInDeck.ApplyDeckChanged))]
        private static class DeckChangedPatch
        {
            private static void Prefix(RelicEffectAddPyreAttackAndHealthPerCardInDeck __instance, RelicEffectParams relicEffectParams,
                out DeckChange? __state) => __state = BeginDeckChanged(__instance, relicEffectParams.saveManager);

            private static void Postfix(RelicEffectAddPyreAttackAndHealthPerCardInDeck __instance, RelicEffectParams relicEffectParams,
                DeckChange? __state) => EndDeckChanged(__instance, relicEffectParams.saveManager, __state);
        }

        private static Snapshot Capture(AllGameManagers managers, SaveManager save, RelicEffectAddPyreAttackAndHealthPerCardInDeck effect)
        {
            SaveData raw = (SaveData)ActiveSave.GetValue(save);
            bool inBattle = save.GetGameSequence() == SaveData.GameSequence.InBattle;
            CombatManager? combat = inBattle ? managers.GetCombatManager() : null;
            CharacterState? pyre = inBattle ? managers.GetRoomManager()?.GetPyreRoom()?.GetPyreHeart() : null;
            var deck = save.GetDeckState();
            return new Snapshot
            {
                Turn = combat?.GetTurnCount(),
                Phase = combat?.GetCombatPhase().ToString(),
                RunningCombat = combat?.GetIsRunningCombat() == true,
                AllScenarioBossesDead = combat?.AllScenarioBossesDead == true,
                CachedDeckCount = RelicResourceProbe.ReadLastKnownDeckCount(effect),
                RawDeckCount = deck.Count,
                VisibleDeckCount = save.GetVisibleDeckCount(),
                DeckDataIds = deck.Select(card => card.GetCardDataID()).ToArray(),
                SourceInSavedDeck = generator != null && deck.Contains(generator),
                PurgeCardInSavedDeck = purgeCard != null && deck.Contains(purgeCard),
                HealthBuffApplied = save.GetRelicPersistentState().HasHealthBuffPerCardInDeckBeenAppliedThisRun(),
                RawSaveHP = raw.GetTowerHP(),
                SaveHP = save.GetTowerHP(),
                RawSaveMaxHP = raw.GetMaxTowerHP(),
                SaveMaxHP = save.GetMaxTowerHP(),
                SaveModifiedAttack = save.GetModifiedPyreBaseAttack(),
                PyreHP = pyre?.GetHP(),
                PyreMaxHP = pyre?.GetMaxHP(),
                PyreAttack = pyre?.GetAttackDamage(),
            };
        }
    }
}
#endif
