using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalResourceRelicBatchScenario
    {
        internal const string Name = "original-resource-relic-batch";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static bool CaptureReady { get; private set; }
        internal static readonly (string Key, string Id)[] Artifacts =
        {
            ("SilverIncreasedDraw", "4a51a457-9c50-4623-b8f5-e1bd9aac561a"),
            ("SilverIncreasedEmber", "1a69fbff-9f1d-4c98-92ff-a63e0ac520c5"),
            ("Inkbound_LeakyPen", "19c5ec66-4dde-4161-8fcf-68caf38a293c"),
            ("LessDeployEmberMoreOtherEmber", "7bc61ac1-f295-4bf5-a978-1c538c8f2e51"),
            ("MoreDeploymentPhaseEmber", "83116c76-f5cc-4912-8a56-872731393975"),
            ("BossEmber", "780f977e-4007-45ae-900f-c8929b2bf000"),
            ("BossEmber2", "6eef1e2d-3bd5-4f04-bd88-d0c24d5eb691"),
            ("BossEmberDH", "50e516a6-f27a-4878-9586-c0efce046206")
        };

        // Called on the natural map, before the first deployment callbacks.
        // Acquisition records belong to RelicResourceProbe's native hooks.
        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            if (Prepared) return;
            SaveManager save = managers.GetSaveManager();
            if (save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("Resource relics must be installed before native battle setup.");
            AllGameData data = save.GetAllGameData();
            CollectableRelicData[] relics = Artifacts.Select(item =>
            {
                CollectableRelicData relic = data.GetAllCollectableRelicData().Single(definition => definition.GetID() == item.Id);
                CheckOriginal(relic, item.Key);
                if (save.GetCollectedRelics().Any(owned => owned.GetRelicDataID() == item.Id))
                    throw new InvalidOperationException("Resource batch already owns " + item.Key + ".");
                return relic;
            }).ToArray();

            CardState[] ownedCards = save.GetDeckState().ToArray();
            if (ownedCards.Length == 0) throw new InvalidOperationException("Resource batch requires the natural starting deck.");
            CardData[] definitions = ownedCards.Select(card => data.FindCardData(card.GetCardDataID())!).Distinct().ToArray();
            CardData emptySpell = definitions.First(card => card.GetCardType() == CardType.Spell && !card.IsUnitAbility() && !card.IsRoomAbility());
            foreach (CardData card in definitions)
            {
                // Native GetSpawnCharacterData derives from Monster + SpawnMonster
                // effects. Clearing them makes even the champion a normal spell.
                Set(card, "cardType", CardType.Spell);
                Set(card, "cost", 0); Set(card, "costType", CardData.CostType.Default);
                Set(card, "targetless", true); Set(card, "targetsRoom", false);
                Set(card, "isUnitAbility", false); Set(card, "isRoomAbility", false);
                Set(card, "effects", new List<CardEffectData>());
                Set(card, "traits", new List<CardTraitData>());
                Set(card, "effectTriggers", new List<CharacterTriggerData>());
                Set(card, "triggers", new List<CardTriggerEffectData>());
                Set(card, "startingUpgrades", new List<CardUpgradeData>());
            }
            foreach (CardState card in ownedCards)
                card.Setup(data.FindCardData(card.GetCardDataID())!, save, setupStartingUpgrades: false);
            for (int index = 0; index < 20; index++)
                save.AddCardToDeck(emptySpell, applyExistingRelicModifiers: false, extraCopies: 0,
                    applyExtraCopiesMutator: false, showAnimation: false, setupStartingUpgrades: false, notifyDeckCount: false);
            CheckCards(save.GetDeckState());

            foreach (CollectableRelicData relic in relics) save.AddRelic(relic);
            Prepared = true;
            log.LogInfo("ORIGINAL-RESOURCE-RELIC-BATCH-ACQUIRED eight unchanged originals before deployment; " +
                "natural starting deck authored as free empty spells plus 20 native deck additions; " +
                "ordinary energy net +4, deployment-only net +4, draw net +3; " +
                "native Balance, Pyre, Boss and waves retained without resource setters or hand draws.");
        }

        // Battle setup owns all shuffling and drawing. This boundary only checks
        // the installed batch and enables the normal decision capture.
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (CaptureReady) return;
            if (!Prepared) throw new InvalidOperationException("Resource batch missed pre-battle acquisition.");
            SaveManager save = managers.GetSaveManager();
            foreach (var artifact in Artifacts)
            {
                if (save.GetCollectedRelics().Count(relic => relic.GetRelicDataID() == artifact.Id) != 1)
                    throw new InvalidOperationException("Resource batch original owner missing or duplicated: " + artifact.Key);
                CheckOriginal(save.GetAllGameData().GetAllCollectableRelicData().Single(relic => relic.GetID() == artifact.Id), artifact.Key);
            }
            CheckCards(save.GetDeckState());
            CaptureReady = true;
            log.LogInfo("ORIGINAL-RESOURCE-RELIC-BATCH-PREPARED native deployment completed; " +
                "empty-spell-only policy leaves all enemy/Pyre combat natural and empties the hand before real turn draws.");
        }

        // Duplicated by the independent fixture checker. No recorded action,
        // runtime phase counter or captured future state selects this play.
        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            if (source.PlayRules == null || source.Spawn.Train.Context == null) return null;
            IReadOnlyList<PlayCardAction> plays = BattleActionModel.EnumerateSupportedPlays(source);
            foreach (CardToken card in source.Spawn.Train.Context.Cards.Hand)
            {
                CardPlayRule? rule = source.PlayRules.Cards.FirstOrDefault(item => item.DataId == card.DataId);
                if (rule?.CardType != "Spell" || rule.IsAnyAbility != false || rule.Effect != "Null" ||
                    rule.Effects.Count != 0 || rule.SpawnUnit != null || rule.Summon != null || rule.Ability != null ||
                    rule.Equipment != null || rule.Cost != 0 || rule.CostType != "Default" || rule.ExternalInteractions.Count != 0)
                    continue;
                foreach (RoomPlayRule room in source.PlayRules.Rooms.Where(room => !room.IsPyre).OrderBy(room => room.RoomIndex))
                {
                    PlayCardAction? play = plays.FirstOrDefault(action => action.CardInstanceId == card.InstanceId &&
                        action.RoomIndex == room.RoomIndex && action.ActivatorUnitId == 0 && action.TargetUnitId == 0);
                    if (play != null) return play;
                }
            }
            return null;
        }

        private static void CheckCards(IEnumerable<CardState> cards)
        {
            if (cards.Any(card => card.GetCardType() != CardType.Spell || card.IsAnyAbility() ||
                card.GetSpawnCharacterData() != null || card.GetEffects().Count != 0 || card.GetTraits().Count != 0))
                throw new InvalidOperationException("Resource batch requires empty ordinary spells without player spawners.");
        }

        private static void CheckOriginal(CollectableRelicData relic, string key)
        {
            const string energy = "RelicEffectModifyEnergy", deployment = "RelicEffectModifyEnergyDuringDeployment",
                draw = "RelicEffectChangeStartingHandSize", health = "RelicEffectIncreasePyreHealth", attack = "RelicEffectIncreasePyreAttack";
            bool boss = key.StartsWith("BossEmber", StringComparison.Ordinal);
            Team.Type team = key == "Inkbound_LeakyPen" ? Team.Type.Monsters :
                key == "LessDeployEmberMoreOtherEmber" ? Team.Type.Heroes : Team.Type.None;
            (string Type, int Int, int Int2)[] expected;
            if (boss)
            {
                int upgradeIndex = key == "BossEmber" ? 0 : 1;
                expected = new[] { (energy, 1, 0), (health, 30, upgradeIndex),
                    (attack, 10, upgradeIndex), (deployment, 1, upgradeIndex) };
            }
            else if (key == "SilverIncreasedDraw") expected = new[] { (draw, 1, 0) };
            else if (key == "SilverIncreasedEmber") expected = new[] { (energy, 1, 0), (deployment, 1, 0) };
            else if (key == "Inkbound_LeakyPen") expected = new[] { (energy, -1, 0), (draw, 2, 0) };
            else if (key == "LessDeployEmberMoreOtherEmber") expected = new[] { (deployment, -3, 0), (energy, 1, 0) };
            else if (key == "MoreDeploymentPhaseEmber") expected = new[] { (deployment, 3, 0) };
            else throw new InvalidOperationException("Unexpected resource batch owner: " + key);
            List<RelicEffectData> effects = relic.GetEffects();
            if (relic.name != key || relic.DisallowInDeploymentPhase || effects.Count != expected.Length)
                throw new InvalidOperationException("Original resource relic identity or effect count changed: " + key);
            for (int index = 0; index < expected.Length; index++)
            {
                RelicEffectData effect = effects[index];
                var rule = expected[index];
                if (effect.GetEffectClassName() != rule.Type || effect.GetParamInt() != rule.Int || effect.GetParamInt2() != rule.Int2 ||
                    effect.GetParamSourceTeam() != team || effect.GetParamBool() != boss || effect.GetParamBool2() ||
                    effect.GetEffectConditions().Count != 0 || effect.GetParamMutatorData() != null ||
                    effect.GetParamCardUpgradeData() != null || effect.GetParamCharacters().Count != 0)
                    throw new InvalidOperationException("Original resource relic payload changed: " + key + "/" + index);
            }
        }

        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
