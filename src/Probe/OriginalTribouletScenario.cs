using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalTribouletScenario
    {
        internal const string Name = "original-balatro-event-triboulet";
        internal const string OwnerId = "dfe0d28c-ff6a-4a8b-8290-aefb32d0c1d6";
        internal const string OwnerKey = "BalatroEventTriboulet";
        private const string WitnessCardId = "fc119000-0000-4000-8000-000000000514";
        private const string WitnessCharacterId = "fc119000-0000-4000-8000-000000000515";
        private const string WitnessCharacterKey = "PojuTribouletCardlessWitness";

        private static int witnessCardInstanceId;
        private static bool played;
        private static bool scenarioActionMarkAvailable;
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        internal static int RareDeckCount { get; private set; }
        internal static string RareWitnessDataId { get; private set; } = "";
        internal static float MonsterHealthBeforeOwner { get; private set; }
        internal static float MonsterHealthAfterOwner { get; private set; }
        internal static int WitnessCardInstanceId => witnessCardInstanceId;
        internal static object Evidence => new
        {
            OwnerId, AssetKey = OwnerKey, RareDeckCount, RareWitnessDataId, MonsterHealthBeforeOwner,
            MonsterHealthAfterOwner, ExpectedCardHealthBonus = RareDeckCount * 3,
            WitnessCardId, WitnessCharacterId, WitnessCharacterKey, Cardless = true,
            ExpectedCardlessSpawnMaxHealthBonus = RareDeckCount * 4
        };

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            if (Acquired) return;
            string step = "";
            void Mark(string value) { step = value; log.LogInfo("ORIGINAL-TRIBOULET-ACQUIRE-STEP " + step); }
            Mark("read-save");
            SaveManager save = managers.GetSaveManager();
            if (save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle ||
                save.GetCollectedRelics().Any(item => item.GetRelicDataID() == OwnerId))
                throw new InvalidOperationException("Triboulet must be acquired once before the isolated battle.");

            Mark("find-rare-card");
            AllGameData data = save.GetAllGameData();
            CardData rare = data.GetAllCardData().Where(card => card != null && card.GetCardType() == CardType.Spell &&
                    card.GetRarity() == CollectableRarity.Rare && !card.IsUnitAbility() && !card.IsRoomAbility() &&
                    card.GetEffects().Count == 1 && card.GetEffects().All(BattleActionProbe.SupportsCardActionEffect) &&
                    !card.GetTraits().Any(trait => trait.GetTraitStateName() == "CardTraitScalingAddEnergy"))
                .OrderBy(card => card.GetID(), StringComparer.Ordinal).FirstOrDefault()
                ?? throw new InvalidOperationException("Triboulet requires an original Rare Spell to seed deck rarity.");
            Mark("add-rare-card");
            CardState addedRare = save.AddCardToDeck(rare, applyExistingRelicModifiers: false, extraCopies: 0,
                showAnimation: false, setupStartingUpgrades: false, notifyDeckCount: false);
            if (addedRare == null || addedRare.GetRarity() != CollectableRarity.Rare ||
                !save.GetDeckState().Contains(addedRare))
                throw new InvalidOperationException("Triboulet failed to seed one original Rare Spell in the deck.");
            RareWitnessDataId = rare.GetID();
            RareDeckCount = save.GetDeckState().Count(card => card.GetRarity() == CollectableRarity.Rare);

            Mark("enumerate-deck-cards");
            CardState[] ownedCards = save.GetDeckState().Where(card => card != null).ToArray();
            Mark("select-monster-witness");
            CardState monster = ownedCards.FirstOrDefault(card => card.GetCardType() == CardType.Monster && !card.IsChampionCard() &&
                    card.GetSpawnCharacterData() != null)
                ?? throw new InvalidOperationException("Triboulet card-health witness requires an owned Monster card.");
            Mark("read-monster-health");
            MonsterHealthBeforeOwner = monster.GetTemporaryCardStateModifiers().GetAdditionalHP().Sum();

            Mark("validate-owner");
            CollectableRelicData relic = data.GetAllCollectableRelicData().Single(item => item.GetID() == OwnerId);
            ValidateDefinition(relic);
            Mark("acquire-owner");
            save.AddRelic(relic);
            Mark("verify-card-health");
            MonsterHealthAfterOwner = monster.GetTemporaryCardStateModifiers().GetAdditionalHP().Sum();
            if (Math.Abs(MonsterHealthAfterOwner - MonsterHealthBeforeOwner - RareDeckCount * 3) > 0.01f)
                throw new InvalidOperationException("Triboulet did not apply +3 Monster card health per Rare deck card.");
            Acquired = true;
            log.LogInfo("ORIGINAL-TRIBOULET-ACQUIRED unchanged owner=" + relic.name + " rareDeck=" + RareDeckCount +
                " rareWitness=" + RareWitnessDataId + " monsterHP=" + MonsterHealthBeforeOwner + "->" + MonsterHealthAfterOwner + ".");
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            CombatManager combat = managers.GetCombatManager()!;
            if (Prepared || !Acquired || save.PreviewMode || combat.GetTurnCount() != 0 ||
                save.GetCollectedRelics().Count(item => item.GetRelicDataID() == OwnerId) != 1)
                throw new InvalidOperationException("Triboulet requires one unchanged owner at deployment.");
            ValidateDefinition(save.GetAllGameData().GetAllCollectableRelicData().Single(item => item.GetID() == OwnerId));

            AllGameData gameData = save.GetAllGameData();
            CharacterData template = managers.GetCardManager()!.GetAllCards(new List<CardState>())
                .Select(card => card.GetSpawnCharacterData()).FirstOrDefault(character => character != null)
                ?? throw new InvalidOperationException("Triboulet cardless witness requires an original character template.");
            if (gameData.FindCharacterData(WitnessCharacterId) != null || gameData.FindCardData(WitnessCardId) != null)
                throw new InvalidOperationException("Triboulet witness asset IDs are already registered.");

            CharacterData character = UnityEngine.Object.Instantiate(template);
            character.name = WitnessCharacterKey;
            Set(character, "id", WitnessCharacterId);
            Set(character, "health", 11);
            Set(character, "attackDamage", 0);
            Set(character, "size", 1);
            Set(character, "startingStatusEffects", Array.Empty<StatusEffectStackData>());
            Set(character, "triggers", new List<CharacterTriggerData>());
            Set(character, "unitAbility", null!);

            CardData spellTemplate = gameData.GetAllCardData().Where(card => card != null && card.GetCardType() == CardType.Spell &&
                    !card.IsUnitAbility() && !card.IsRoomAbility())
                .OrderBy(card => card.GetID(), StringComparer.Ordinal).First();
            CardData witness = UnityEngine.Object.Instantiate(spellTemplate);
            witness.name = "PojuTribouletCardlessSpawnWitness";
            Set(witness, "id", WitnessCardId);
            Set(witness, "cardType", CardType.Spell);
            Set(witness, "cost", 0);
            Set(witness, "costType", CardData.CostType.Default);
            Set(witness, "isUnitAbility", false);
            Set(witness, "isRoomAbility", false);
            Set(witness, "targetless", false);
            Set(witness, "targetsRoom", true);
            Set(witness, "traits", new List<CardTraitData>());
            Set(witness, "effectTriggers", new List<CharacterTriggerData>());
            Set(witness, "triggers", new List<CardTriggerEffectData>());
            Set(witness, "startingUpgrades", new List<CardUpgradeData>());
            var spawn = new CardEffectData("CardEffectSpawnMonster", null!, Team.Type.Monsters);
            spawn.Cheat_SetTargetMode(TargetMode.Room);
            Set(spawn, "paramCharacterData", character);
            Set(spawn, "paramBool", true);
            Set(spawn, "paramInt", 1);
            Set(witness, "effects", new List<CardEffectData> { spawn });
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(gameData)!).Add(witness);

            CardState card = managers.GetCardManager()!.AddNewCard(witness, CardPile.HandPile, false, false, animate: false);
            if (card == null || !managers.GetCardManager()!.GetHand().Contains(card))
                throw new InvalidOperationException("Triboulet cardless-spawn witness did not enter the native hand.");
            witnessCardInstanceId = FullBattleTrace.Active!.CardId(card);
            played = false;
            scenarioActionMarkAvailable = false;
            Prepared = true;
            AccessTools.Field(typeof(CombatManager), "combatStateChanged")!.SetValue(combat, true);
            log.LogInfo("ORIGINAL-TRIBOULET-PREPARED unchanged owner rareDeck=" + RareDeckCount +
                " cardless Monster spawn card=" + witnessCardInstanceId + " character=" + WitnessCharacterKey + ".");
        }

        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            if (!Prepared || played) return null;
            PlayCardAction? action = BattleActionModel.EnumerateSupportedPlays(state)
                .FirstOrDefault(item => item.CardInstanceId == witnessCardInstanceId && item.RoomIndex == 0);
            if (action != null) { played = true; scenarioActionMarkAvailable = true; }
            return action;
        }

        internal static PlayCardAction? AvoidRepeatedWitness(BattleTurnState state, PlayCardAction? action) =>
            !played || action?.CardInstanceId != witnessCardInstanceId
                ? action
                : BattleActionModel.ChooseUnitSpellAndJunkPlay(state, card => card.InstanceId != witnessCardInstanceId);

        internal static bool IsScenarioAction(PlayCardAction action)
        {
            if (!Prepared || !scenarioActionMarkAvailable || action.CardInstanceId != witnessCardInstanceId) return false;
            scenarioActionMarkAvailable = false;
            return true;
        }

        private static void ValidateDefinition(CollectableRelicData relic)
        {
            RelicEffectData[] effects = relic.GetEffects().ToArray();
            string[] names = {
                RelicModel.ModifyCardHealthPerRarityType,
                RelicModel.ModifyCharacterMaxHealthPerRarityType,
                RelicModel.ModifyCharacterMaxHealthPerRarityTypeTrigger
            };
            bool valid = relic.name == OwnerKey && effects.Length == 3 &&
                effects.Select(effect => effect.GetEffectClassName()).SequenceEqual(names, StringComparer.Ordinal) &&
                effects[0].GetParamCardType() == CardType.Monster && effects[0].GetParamInt() == 3 &&
                effects[0].GetParamCardRarityType() == CollectableRarity.Rare &&
                effects[1].GetParamSourceTeam() == Team.Type.Monsters && effects[1].GetParamInt() == 2 &&
                effects[1].GetParamCardRarityType() == CollectableRarity.Rare &&
                effects[2].GetParamSourceTeam() == Team.Type.Monsters && effects[2].GetParamInt() == 2 &&
                effects[2].GetParamCardRarityType() == CollectableRarity.Rare &&
                effects[2].GetParamTrigger() == CharacterTriggerData.Trigger.OnSpawnNotFromCard &&
                effects.All(effect => effect.GetEffectConditions().Count == 0);
            if (!valid) throw new InvalidOperationException("Original Triboulet relic identity, effect order, or payload changed.");
        }

        private static void Set(object target, string field, object value) =>
            AccessTools.Field(target.GetType(), field)?.SetValue(target, value);
    }
}
