using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalHephsBlueprintsScenario
    {
        internal const string Name = "original-hephs-blueprints";
        internal const string DataId = "5152b7d0-4f8a-4b7b-8871-06c25bb1a5af";
        internal const string AssetKey = "HephsBlueprints";
        internal const int ForgePoints = 3;
        private const string Effect = "RelicEffectModifyForgeUpgrade";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool EquipmentPoolControls => Enabled && Environment.GetEnvironmentVariable("MT2_PROBE_HEPHS_POOL_CONTROLS") == "1";
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        internal static bool EquipmentPoolControlsCompleted { get; private set; }
        internal static string? EquipmentPoolControlsError { get; private set; }
        internal static DefinitionEvidence? Definition { get; private set; }
        private static int scenarioCardInstanceId;
        private static int scenarioEquipmentInstanceId;
        private static readonly HashSet<int> ScenarioActionIds = new HashSet<int>();

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            if (Acquired || save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("HephsBlueprints must be acquired before battle in an isolated save.");
            CollectableRelicData relic = save.GetAllGameData().GetAllCollectableRelicData()
                .Single(item => item.GetID() == DataId);
            Definition = CheckOriginal(relic);
            if (save.GetCollectedRelics().Any(item => item.GetRelicDataID() == DataId))
                throw new InvalidOperationException("HephsBlueprints is already acquired.");
            save.AddRelic(relic);
            Acquired = true;
            log.LogInfo("ORIGINAL-HEPHS-BLUEPRINTS-ACQUIRED unchanged owner; native ForgeUpgrade modifier.");
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            CombatManager combat = managers.GetCombatManager()!;
            if (Prepared || !Acquired || save.PreviewMode || combat.GetTurnCount() != 0 ||
                !save.GetCollectedRelics().Any(item => item.GetRelicDataID() == DataId))
                throw new InvalidOperationException("HephsBlueprints requires its isolated deployment state.");

            CollectableRelicData relic = save.GetAllGameData().GetAllCollectableRelicData()
                .Single(item => item.GetID() == DataId);
            Definition = CheckOriginal(relic);
            CardManager cards = managers.GetCardManager()!;
            AllGameData data = save.GetAllGameData();
            CardState[] owned = cards.GetAllCards(new List<CardState>()).ToArray();
            CardData carrier = owned.Select(card => data.FindCardData(card.GetCardDataID()))
                .FirstOrDefault(card => card?.GetSpawnCharacterData() != null)
                ?? throw new InvalidOperationException("A native monster carrier is required for HephsBlueprints.");
            CharacterData character = carrier.GetSpawnCharacterData()!;
            Set(character, "health", 80);
            Set(character, "attackDamage", 20);
            Set(character, "size", 1);
            Set(character, "equipmentLimit", 3);
            Set(character, "startingStatusEffects", Array.Empty<StatusEffectStackData>());
            Set(character, "triggers", new List<CharacterTriggerData>());
            Set(character, "unitAbility", null);
            Set(carrier, "cost", 0);
            Set(carrier, "traits", new List<CardTraitData>());
            Set(carrier, "triggers", new List<CardTriggerEffectData>());
            Set(carrier, "effectTriggers", new List<CharacterTriggerData>());
            Set(carrier, "startingUpgrades", new List<CardUpgradeData>());

            CardUpgradeData relicUpgrade = DefinitionUpgrade(relic);
            CardData equipment = data.GetAllCardData().FirstOrDefault(card => card != null &&
                card.GetCardType() == CardType.Equipment && card.GetEffects().Count == 1 &&
                card.GetEffects()[0].GetEffectStateName() == "CardEffectAttachEquipment" &&
                card.GetEffects()[0].GetParamCardUpgradeData() != null &&
                card.GetEffects()[0].GetParamCardUpgradeData()!.GetFilters().Count == 0 &&
                card.GetEffects()[0].GetParamCardUpgradeData()!.GetUnitAbilityUpgrade() == null &&
                card.GetTraits().Count == 0 && card.GetCardTriggers().Count == 0 && card.GetEffectTriggers().Count == 0 &&
                relicUpgrade.GetFilters().All(filter => filter.FilterCard(card, managers.GetRelicManager())))
                ?? throw new InvalidOperationException("A native HephsBlueprints-filtered equipment with a simple friendly host upgrade is required.");
            Set(equipment, "cost", 0);
            Set(equipment, "triggers", new List<CardTriggerEffectData>());
            Set(equipment, "effectTriggers", new List<CharacterTriggerData>());
            Set(equipment, "startingUpgrades", new List<CardUpgradeData>());
            CardUpgradeData equipmentUpgrade = DynamicUpgradeScenario.Upgrade("PojuHephsEquipmentUpgrade",
                "fc119000-0000-4000-8000-000000000408", 0, 0, 0, 0, "armor", 0);
            var attach = new CardEffectData("CardEffectAttachEquipment", null!, Team.Type.Monsters);
            attach.Cheat_SetTargetMode(TargetMode.DropTargetCharacter);
            Set(attach, "paramCardUpgradeData", equipmentUpgrade);
            Set(equipment, "effects", new List<CardEffectData> { attach });

            foreach (CardState card in owned) card.Setup(data.FindCardData(card.GetCardDataID())!, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            CardState testCard = cards.AddNewCard(carrier, CardPile.HandPile, false, false, animate: false)
                ?? throw new InvalidOperationException("HephsBlueprints test carrier creation failed.");
            CardState testEquipment = cards.AddNewCard(equipment, CardPile.HandPile, false, false, animate: false)
                ?? throw new InvalidOperationException("HephsBlueprints test equipment creation failed.");
            scenarioCardInstanceId = FullBattleTrace.Active!.CardId(testCard);
            scenarioEquipmentInstanceId = FullBattleTrace.Active.CardId(testEquipment);
            ScenarioActionIds.Clear();

            save.SetForgePoints(ForgePoints);
            save.SetForgeToggleActive(true);
            AccessTools.Field(typeof(PlayerManager), "isForgeTrackingActive")
                .SetValue(managers.GetPlayerManager(), true);
            if (!CardForgeHelper.IsCardOfInterest(testCard, managers.GetCoreManagers()) ||
                !CardForgeHelper.IsCardOfInterest(testEquipment, managers.GetCoreManagers()))
                throw new InvalidOperationException("The HephsBlueprints native control or equipment is not forge eligible.");
            bool rejectsMonster = relicUpgrade.GetFilters().Count > 0 &&
                relicUpgrade.GetFilters().Any(filter => !filter.FilterCard(testCard, managers.GetRelicManager()));
            bool acceptsEquipment = relicUpgrade.GetFilters().Count > 0 &&
                relicUpgrade.GetFilters().All(filter => filter.FilterCard(testEquipment, managers.GetRelicManager()));
            if (!rejectsMonster || !acceptsEquipment)
            {
                CardUpgradeMaskRule filter = relicUpgrade.GetFilters().Select(CardUpgradeMaskProbe.Rule).Single();
                CardUpgradeMaskData filterData = relicUpgrade.GetFilters().Single();
                throw new InvalidOperationException("HephsBlueprints native filter mismatch: monster=" + testCard.GetCardType() +
                    "/" + testCard.GetCardTargetMode() + ", equipment=" + testEquipment.GetCardType() + "/" +
                    testEquipment.GetRarity() + "/cost=" + testEquipment.GetCostWithoutTraits() + "/clan=" + testEquipment.GetLinkedClassID() + "/" +
                    testEquipment.GetCardTargetMode() + "/" + string.Join(",", testEquipment.GetEffects().Select(item => item.GetEffectStateName())) +
                    ", filter=" + filter.AssetKey + "/" + filter.CardType + "/target=" + filter.TargetMode +
                    "/minCost=" + filter.MinCost + "/maxCost=" + filter.MaxCost +
                    "/requiredEffects=" + string.Join(",", filter.Effects.Required) +
                    "/excludedEffects=" + string.Join(",", filter.Effects.Excluded) +
                    "/requiredTraits=" + string.Join(",", filter.Traits.Required) +
                    "/excludedTraits=" + string.Join(",", filter.Traits.Excluded) +
                    "/all=" + JsonConvert.SerializeObject(CardUpgradeMaskProbe.Definition(filterData)) +
                    "/rejectMonster=" + rejectsMonster + "/acceptEquipment=" + acceptsEquipment);
            }
            Prepared = true;
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(combat, true);
            log.LogInfo("ORIGINAL-HEPHS-BLUEPRINTS-PREPARED points=3 equipment-filter control monster then eligible equipment.");
        }

        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            if (!Prepared || ScenarioActionIds.Count >= 2) return null;
            IReadOnlyList<PlayCardAction> plays = BattleActionModel.EnumerateSupportedPlays(state);
            PlayCardAction? action;
            if (ScenarioActionIds.Count == 0)
            {
                action = plays.FirstOrDefault(play => play.CardInstanceId == scenarioCardInstanceId &&
                    play.RoomIndex == 0 && play.TargetUnitId == 0);
                if (action == null) throw new InvalidOperationException("HephsBlueprints filter-control monster has no supported room-zero play.");
            }
            else
            {
                CombatUnit host = state.Spawn.Train.Rooms.SelectMany(room => room.Units)
                    .Single(unit => unit.Team == CombatTeam.Player && unit.SpawnerCardId == scenarioCardInstanceId);
                action = plays.FirstOrDefault(play => play.CardInstanceId == scenarioEquipmentInstanceId &&
                    play.RoomIndex == 0 && play.TargetUnitId == host.Id);
                if (action == null)
                {
                    CardToken? token = state.Spawn.Train.Context!.Cards.Hand.FirstOrDefault(card => card.InstanceId == scenarioEquipmentInstanceId);
                    CardPlayRule? rule = token == null ? null : state.PlayRules!.Cards.FirstOrDefault(item => item.DataId == token.DataId);
                    BattleActionResult direct = token == null ? null! : BattleActionModel.PlayCard(state,
                        new PlayCardAction(scenarioEquipmentInstanceId, 0, targetUnitId: host.Id));
                    string hand = string.Join(";", state.Spawn.Train.Context.Cards.Hand.Select(card =>
                    {
                        CardPlayRule? item = state.PlayRules!.Cards.FirstOrDefault(candidate => candidate.DataId == card.DataId);
                        return card.InstanceId + ":" + card.DataId + ":" + (item?.Effect ?? "missing") + ":" +
                            (item?.ForgeEligible?.ToString() ?? "null") + ":" + string.Join(",", item?.ExternalInteractions ?? Array.Empty<string>());
                    }));
                    throw new InvalidOperationException("HephsBlueprints equipment action unavailable: id=" + scenarioEquipmentInstanceId +
                        ", token=" + (token?.DataId ?? "missing") + ", effect=" + (rule?.Effect ?? "missing") +
                        ", forge=" + (rule?.ForgeEligible?.ToString() ?? "null") + ", host=" + host.Id +
                        ", direct=" + (token == null ? "not-in-hand" : direct.Rejection + "/" + direct.Reason) + ", plays=" + plays.Count + ", hand=" + hand);
                }
                SaveManager save = AllGameManagers.Instance!.GetSaveManager();
                if (save.GetForgePoints() != 0 || !save.IsForgeToggleActive())
                    throw new InvalidOperationException("HephsBlueprints equipment phase must restore three forge points after the control action.");
                save.SetForgePoints(ForgePoints);
            }
            ScenarioActionIds.Add(action.CardInstanceId);
            return action;
        }

        internal static IEnumerator RunEquipmentPoolControls(AllGameManagers managers)
        {
            var stack = new Stack<IEnumerator>();
            stack.Push(CaptureEquipmentPoolControls(managers));
            try
            {
                while (stack.Count > 0)
                {
                    IEnumerator current = stack.Peek();
                    bool advanced;
                    try { advanced = current.MoveNext(); }
                    catch (Exception error) { EquipmentPoolControlsError = error.ToString(); yield break; }
                    if (!advanced)
                    {
                        (stack.Pop() as IDisposable)?.Dispose();
                        continue;
                    }
                    if (current.Current is IEnumerator nested) stack.Push(nested);
                    else yield return current.Current;
                }
                EquipmentPoolControlsCompleted = true;
            }
            finally
            {
                while (stack.Count > 0) (stack.Pop() as IDisposable)?.Dispose();
            }
        }

        private static IEnumerator CaptureEquipmentPoolControls(AllGameManagers managers)
        {
            if (!EquipmentPoolControls || !Acquired || Prepared || managers.GetCombatManager()!.GetTurnCount() != 0)
                throw new InvalidOperationException("Expected the original HephsBlueprints owner before deployment actions.");

            SaveManager save = managers.GetSaveManager();
            AllGameData data = save.GetAllGameData();
            CardManager cards = managers.GetCardManager()!;
            RelicManager relics = managers.GetRelicManager();
            FullBattleTrace trace = FullBattleTrace.Active!;
            CollectableRelicData owner = data.GetAllCollectableRelicData().Single(item => item.GetID() == DataId);
            CardUpgradeMaskData mask = DefinitionUpgrade(owner).GetFilters().Single();
            var originals = new[]
            {
                ("SpitefulShackles", "a1375ed2-179f-4ede-9233-b88b604836ac", true, true),
                ("ZealotsHalo", "cbfec4a4-00fe-406e-b52a-e35e6456ba7d", true, true),
                ("Sineater", "ad143b21-be42-43a5-84bb-780ab697f7c1", true, true),
                ("Soulsmelter", "bee94cde-06f4-4bee-b616-8d1116fbb803", true, true),
                ("TrainStewardSpear", "38e70e6a-20f3-4e36-8d31-c5110be07f87", false, true),
                ("PenitentsDeathsuit", "2d27a710-347c-482f-981d-d311a4f241ce", false, false)
            };
            var results = new List<object>();

            save.SetForgeToggleActive(true);
            Set(managers.GetPlayerManager(), "isForgeTrackingActive", true);
            foreach (var (assetKey, cardId, expectedFilter, spendForge) in originals)
            {
                CardData definition = data.FindCardData(cardId);
                if (definition == null || definition.GetAssetKey() != assetKey)
                    throw new InvalidOperationException("Missing original forge-control card " + assetKey);
                CardState card = cards.AddNewCard(definition, CardPile.HandPile, false, false, animate: false)
                    ?? throw new InvalidOperationException("Could not add original forge-control card " + assetKey);
                int instanceId = trace.CardId(card);
                bool definitionMatches = mask.FilterCard(definition, relics);
                bool ownedMatches = mask.FilterCard(card, relics);
                if (definitionMatches != expectedFilter || ownedMatches != expectedFilter)
                    throw new InvalidOperationException("Original HephsBlueprints pool membership differs for " + assetKey);

                trace.InvalidateCardRules();
                save.SetForgePoints(ForgePoints);
                BattleTurnState decision = trace.CaptureDecision();
                CombatContext context = decision.Spawn.Train.Context!;
                CardInstanceState before = context.FindCard(instanceId)!;
                CardPlayRule rule = decision.PlayRules!.Cards.Single(item => item.DataId == cardId);
                if (before.MaskDescriptor == null || decision.PlayRules.ForgeUpgrade == null)
                    throw new InvalidOperationException("Original forge control requires native branch mask capture.");
                if (spendForge)
                {
                    if (rule.ForgeEligible != true) throw new InvalidOperationException("Original card is not eligible for default forge: " + assetKey);
                    yield return CardForgeHelper.ApplyForgePointsUpgradeToCard(card, null,
                        CardUpgradeLifetime.TemporaryUntilEndOfBattle, managers.GetCoreManagers());
                }
                CardInstanceState actual = CardModifierProbe.Capture(new[] { card }, trace.CardId).Single();
                results.Add(new { AssetKey = assetKey, DataId = cardId, DefinitionMatches = definitionMatches, OwnedMatches = ownedMatches,
                    SpentForge = spendForge, Before = before, Actual = actual, Rule = rule, Context = context,
                    Forge = decision.PlayRules.ForgeUpgrade, ForgePointsAfter = save.GetForgePoints() });
            }

            using var document = NativeFixtureCapture.Capture(new
            {
                Schema = 1,
                Boundary = "NativeOriginalHephsEquipmentPool",
                GameVersion = UnityEngine.Application.version,
                GameModuleMvid = typeof(CardState).Assembly.ManifestModule.ModuleVersionId,
                Definition,
                Cases = results
            });
            using var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!,
                "hephs-blueprints-equipment-pool.mt2f"));
            document.Write(stream);
        }

        internal static bool IsScenarioAction(PlayCardAction action) => Enabled && ScenarioActionIds.Contains(action.CardInstanceId);

        private static DefinitionEvidence CheckOriginal(CollectableRelicData relic)
        {
            RelicEffectData effect = relic.GetEffects().Single();
            CardUpgradeData upgrade = effect.GetParamCardUpgradeData();
            if (relic.name != AssetKey || relic.GetID() != DataId || effect.GetEffectClassName() != Effect ||
                effect.GetEffectConditions().Count != 0 || upgrade == null)
                throw new InvalidOperationException("The unchanged HephsBlueprints owner or forge payload changed.");
            var state = new CardUpgradeState();
            state.Setup(upgrade);
            return new DefinitionEvidence
            {
                OwnerId = relic.GetID(), AssetKey = relic.name, Effect = effect.GetEffectClassName(),
                UpgradeId = upgrade.GetID(), AttackDamage = state.GetAttackDamage(), AttackDamageBuff = state.GetAttackDamageBuff(),
                AdditionalHP = state.GetAdditionalHP(), AdditionalUnhealedHP = state.GetAdditionalUnhealedHP(),
                CostReduction = state.GetCostReduction(), AdditionalHeal = state.GetAdditionalHeal(),
                AdditionalSize = state.GetAdditionalSize(), EquipmentLimit = state.GetAdditionalEquipmentLimit(),
                UpgradeSlots = state.GetAdditionalUpgradeSlotCount(), StatusUpgradeCount = state.GetStatusEffectUpgrades().Count,
                TraitUpgradeCount = state.GetTraitDataUpgrades().Count, RemovedTraitCount = state.GetRemoveTraitUpgrades().Count,
                TriggerUpgradeCount = state.GetTriggerUpgrades().Count, CardTriggerUpgradeCount = state.GetCardTriggerUpgrades().Count,
                UnitAbilityUpgrade = state.GetUnitAbilityUpgrade() != null, RoomAbilityUpgrade = state.GetRoomAbilityUpgrade() != null,
                FilterCount = state.GetFilters().Count
            };
        }

        private static CardUpgradeData DefinitionUpgrade(CollectableRelicData relic) =>
            relic.GetEffects().Single().GetParamCardUpgradeData() ??
            throw new InvalidOperationException("HephsBlueprints native forge payload is missing.");

        private static void Set(object target, string field, object? value) =>
            AccessTools.Field(target.GetType(), field).SetValue(target, value);

        internal sealed class DefinitionEvidence
        {
            public string OwnerId { get; set; } = string.Empty;
            public string AssetKey { get; set; } = string.Empty;
            public string Effect { get; set; } = string.Empty;
            public string UpgradeId { get; set; } = string.Empty;
            public int AttackDamage { get; set; }
            public int AttackDamageBuff { get; set; }
            public int AdditionalHP { get; set; }
            public int AdditionalUnhealedHP { get; set; }
            public int CostReduction { get; set; }
            public int AdditionalHeal { get; set; }
            public int AdditionalSize { get; set; }
            public int EquipmentLimit { get; set; }
            public int UpgradeSlots { get; set; }
            public int StatusUpgradeCount { get; set; }
            public int TraitUpgradeCount { get; set; }
            public int RemovedTraitCount { get; set; }
            public int TriggerUpgradeCount { get; set; }
            public int CardTriggerUpgradeCount { get; set; }
            public bool UnitAbilityUpgrade { get; set; }
            public bool RoomAbilityUpgrade { get; set; }
            public int FilterCount { get; set; }
        }
    }
}
