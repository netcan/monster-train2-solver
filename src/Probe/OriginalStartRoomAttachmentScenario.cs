using System;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalStartRoomAttachmentScenario
    {
        internal const string Name = "original-start-room-attachment";
        internal const string Floor2Name = "original-start-room-attachment-floor2";
        internal const string RelicId = "2c97e1b3-d4bb-4133-9572-e582805c106f";
        internal const string RelicKey = "TrainRoomInstallFloor1AtStartOfCombat";
        internal const string Floor2RelicId = "b037ec04-6164-4cd8-8b9d-3bc420c29351";
        internal const string Floor2RelicKey = "TrainRoomInstallFloor2AtStartOfCombat";
        internal const string UpgradeId = "05e215c6-600c-4ecb-a75e-c806dd596bcf";
        internal const string UpgradeKey = "FuelCar_Upgrade";
        internal const string Floor2UpgradeId = "35178e4a-0880-440b-a929-de29de6e88f8";
        internal const string Floor2UpgradeKey = "SpellSanctum_Upgrade";

        private static string Mode => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") ?? string.Empty;
        internal static bool Floor2 => Mode == Floor2Name;
        internal static int TargetRoomIndex => Floor2 ? 1 : 0;
        private static string TargetRelicId => Floor2 ? Floor2RelicId : RelicId;
        private static string TargetRelicKey => Floor2 ? Floor2RelicKey : RelicKey;
        private static string TargetUpgradeId => Floor2 ? Floor2UpgradeId : UpgradeId;
        private static string TargetUpgradeKey => Floor2 ? Floor2UpgradeKey : UpgradeKey;
        internal static bool Enabled => Mode == Name || Floor2;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        internal static CoverageRecord Evidence { get; } = new CoverageRecord();

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            if (Acquired) return;
            SaveManager save = managers.GetSaveManager();
            if (save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("The start-room attachment relic must be acquired before battle setup.");
            CollectableRelicData relic = save.GetAllGameData().GetAllCollectableRelicData()
                .Single(item => item.GetID() == TargetRelicId);
            CheckOriginal(relic);
            if (save.GetCollectedRelics().Any(item => item.GetRelicDataID() == TargetRelicId))
                throw new InvalidOperationException("The start-room attachment relic is already present in this native test run.");
            save.AddRelic(relic);
            Acquired = true;
            log.LogInfo("ORIGINAL-START-ROOM-ATTACHMENT-ACQUIRED unchanged relic=" + relic.name + " id=" + relic.GetID() +
                " room=" + TargetRoomIndex + ".");
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (Prepared) return;
            if (!Acquired || managers.GetCombatManager()!.GetTurnCount() != 0)
                throw new InvalidOperationException("The start-room attachment relic must be inspected on deployment turn.");
            SaveManager save = managers.GetSaveManager();
            CollectableRelicData relic = save.GetAllGameData().GetAllCollectableRelicData()
                .Single(item => item.GetID() == TargetRelicId);
            CheckOriginal(relic);
            if (managers.GetRelicManager().GetRelicEffect<RelicEffectAddTrainRoomAttachmentOnStartOfCombat>() == null)
                throw new InvalidOperationException("The original start-of-combat attachment effect was not initialized.");

            RoomState room = managers.GetRoomManager()!.GetRoom(TargetRoomIndex);
            if (room.GetIsPyreRoom() || room.HasEmbeddedTrainRoomAttachment())
                throw new InvalidOperationException("The native start-room attachment scenario requires a non-Pyre, non-embedded target room.");
            TrainRoomAttachmentState[] matches = room.Attachments.Where(item => item.IsActive &&
                item.UpgradeStates.Any(upgrade => upgrade.Item2.GetID() == TargetUpgradeId)).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException("The start-of-combat relic did not create exactly one active expected attachment on its target floor.");
            TrainRoomAttachmentState attachment = matches[0];
            if (attachment.CardState != null || (int)attachment.AttachType != 0 ||
                attachment.TrainSide != (Team.Type.Heroes | Team.Type.Monsters) || attachment.Guid.Length == 0 ||
                !Guid.TryParse(attachment.Guid, out Guid parsedId) || parsedId == Guid.Empty ||
                attachment.UpgradeStates.Count != 1 || attachment.UpgradeStates[0].Item2.name != TargetUpgradeKey ||
                room.NumRoomAttachmentsAddedFromCard != 0)
                throw new InvalidOperationException("The native room attachment identity, team, type, card counter or upgrade payload changed.");

            CardUpgradeState upgradeState = attachment.UpgradeStates[0].Item1;
            CardUpgradeData upgradeData = attachment.UpgradeStates[0].Item2;
            string triggerKinds = string.Join(",", upgradeData.GetCharacterTriggerUpgrades().Select(item => item.GetTrigger().ToString()));
            var nativeRoomModifiers = upgradeData.GetRoomModifierUpgrades();
            string roomModifiers = string.Join(",", nativeRoomModifiers.Select(item =>
                item.GetRoomStateModifierClassName() + ":" + item.GetParamInt()));
            string statuses = string.Join(",", upgradeState.GetStatusEffectUpgrades().Select(item => item.statusId + ":" + item.count));

            if (Floor2 && (nativeRoomModifiers.Count != 1 ||
                nativeRoomModifiers[0].GetRoomStateModifierClassName() != "RoomStateSpellCostModifier" ||
                nativeRoomModifiers[0].GetParamInt() != -1))
                throw new InvalidOperationException("SpellSanctum must contain exactly one RoomStateSpellCostModifier(-1).");

            Evidence.Prepared = true;
            Evidence.RelicId = TargetRelicId;
            Evidence.RelicKey = relic.name;
            Evidence.UpgradeId = TargetUpgradeId;
            Evidence.UpgradeKey = TargetUpgradeKey;
            Evidence.AttachmentGuid = attachment.Guid;
            Evidence.RoomIndex = room.GetRoomIndex();
            Evidence.AttachmentType = (int)attachment.AttachType;
            Evidence.AffectsHeroes = attachment.TrainSide.HasFlag(Team.Type.Heroes);
            Evidence.AffectsMonsters = attachment.TrainSide.HasFlag(Team.Type.Monsters);
            Evidence.CapacityAtSetup = room.GetCapacityInfo(Team.Type.Monsters).max;
            Evidence.AttachmentsAddedFromCards = room.NumRoomAttachmentsAddedFromCard;
            Evidence.InitiallyImpactedCharacters = attachment.CharactersImpacted.Count;
            if (nativeRoomModifiers.Count == 1)
            {
                Evidence.RoomModifierClass = nativeRoomModifiers[0].GetRoomStateModifierClassName();
                Evidence.RoomModifierParam = nativeRoomModifiers[0].GetParamInt();
            }
            Prepared = true;
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(managers.GetCombatManager(), true);
            log.LogInfo("ORIGINAL-START-ROOM-ATTACHMENT-PREPARED room=" + room.GetRoomIndex() + " attachment=" + attachment.Guid +
                " upgrade=" + Evidence.UpgradeKey + " teams=Heroes|Monsters cardCounter=" + Evidence.AttachmentsAddedFromCards +
                " initiallyImpacted=" + Evidence.InitiallyImpactedCharacters + " stats=" + upgradeState.GetAttackDamage() + "/" +
                upgradeState.GetAdditionalHP() + "/" + upgradeState.GetAdditionalUnhealedHP() + " statuses=" + statuses +
                " triggers=" + triggerKinds + " roomModifiers=" + roomModifiers + " traits=" + upgradeData.GetTraitDataUpgrades().Count +
                " cardTriggers=" + upgradeData.GetCardTriggerUpgrades().Count + " filters=" + upgradeData.GetFilters().Count +
                " spellCost=" + Evidence.SpellCostWitnessCardId + ":" + Evidence.SpellCostControl + "->" + Evidence.SpellCostTarget +
                " xCost=" + Evidence.XCostWitnessCardId + ":" + Evidence.XCostControlModifier + "->" + Evidence.XCostTargetModifier +
                " unitAbility=" + upgradeData.GetUnitAbilityUpgrade()?.name + " roomAbility=" + upgradeData.GetRoomAbilityUpgrade()?.name + ".");
        }

        internal static void CaptureCostWitness(AllGameManagers managers, CardState[] hand, ManualLogSource log)
        {
            if (!Floor2 || !Prepared) return;
            CardManager cards = managers.GetCardManager()!;
            CardStatistics statistics = managers.GetCardStatistics();
            RoomManager rooms = managers.GetRoomManager()!;
            RoomState controlRoom = rooms.GetRoom(TargetRoomIndex == 0 ? 1 : 0);
            RoomState targetRoom = rooms.GetRoom(TargetRoomIndex);
            bool captured = false;

            if (Evidence.SpellCostWitnessCardId == 0)
            {
                CardState? witness = hand.Where(card => card.GetCardType() == CardType.Spell &&
                        !card.IsConsumeRemainingEnergyCostType())
                    .Select(card => new { Card = card, Cost = card.GetCost(statistics, managers.GetMonsterManager(),
                        managers.GetRelicManager(), controlRoom) })
                    .Where(item => item.Cost > 0).Select(item => item.Card).FirstOrDefault();
                if (witness != null)
                {
                    Evidence.SpellCostWitnessCardId = FullBattleTrace.Active!.CardId(witness);
                    Evidence.SpellCostControl = witness.GetCost(statistics, managers.GetMonsterManager(),
                        managers.GetRelicManager(), controlRoom);
                    Evidence.SpellCostTarget = witness.GetCost(statistics, managers.GetMonsterManager(),
                        managers.GetRelicManager(), targetRoom);
                    if (Evidence.SpellCostTarget != Math.Max(0, Evidence.SpellCostControl - 1))
                        throw new InvalidOperationException("SpellSanctum did not reduce the same hand spell's native room cost by one.");
                    captured = true;
                }
            }

            if (Evidence.XCostWitnessCardId == 0)
            {
                CardState? witness = hand.FirstOrDefault(card => card.GetCardType() == CardType.Spell &&
                    card.IsConsumeRemainingEnergyCostType());
                if (witness != null)
                {
                    Evidence.XCostWitnessCardId = FullBattleTrace.Active!.CardId(witness);
                    Evidence.XCostControlModifier = controlRoom.GetRoomStateCardPlayedCostStatModifier(witness, statistics);
                    Evidence.XCostTargetModifier = targetRoom.GetRoomStateCardPlayedCostStatModifier(witness, statistics);
                    if (Evidence.XCostTargetModifier - Evidence.XCostControlModifier != 1)
                        throw new InvalidOperationException("SpellSanctum did not add one to the native X-spell PlayedCost modifier.");
                    captured = true;
                }
            }

            if (captured)
                log.LogInfo("ORIGINAL-START-ROOM-ATTACHMENT-COST-WITNESS spell=" + Evidence.SpellCostWitnessCardId + ":" +
                    Evidence.SpellCostControl + "->" + Evidence.SpellCostTarget + " xSpell=" + Evidence.XCostWitnessCardId + ":" +
                    Evidence.XCostControlModifier + "->" + Evidence.XCostTargetModifier + ".");
        }

        private static void CheckOriginal(CollectableRelicData relic)
        {
            RelicEffectData effect = relic.GetEffects().Single();
            CardUpgradeData upgrade = effect.GetParamCardUpgradeData();
            if (relic.GetID() != TargetRelicId || relic.name != TargetRelicKey || relic.DisallowInDeploymentPhase ||
                effect.GetEffectClassName() != "RelicEffectAddTrainRoomAttachmentOnStartOfCombat" ||
                (int)effect.GetParamSourceTeam() != 255 || effect.GetParamInt() != TargetRoomIndex || effect.GetParamInt2() != 0 ||
                effect.GetParamFloat() != 0 || effect.GetParamBool() || effect.GetParamBool2() ||
                effect.GetParamBool3() || effect.GetParamBool4() || effect.GetParamCardFilter() != null ||
                effect.GetEffectConditions().Count != 0 || upgrade == null || upgrade.GetID() != TargetUpgradeId || upgrade.name != TargetUpgradeKey)
                throw new InvalidOperationException("Original start-of-combat attachment relic definition changed: id=" + relic.GetID() +
                    " key=" + relic.name + " disallow=" + relic.DisallowInDeploymentPhase + " class=" + effect.GetEffectClassName() +
                    " team=" + (int)effect.GetParamSourceTeam() + " room=" + effect.GetParamInt() + " type=" + effect.GetParamInt2() +
                    " upgrade=" + (upgrade == null ? "null" : upgrade.GetID() + "/" + upgrade.name) + ".");
        }

        internal sealed class CoverageRecord
        {
            public bool Prepared { get; set; }
            public string RelicId { get; set; } = string.Empty;
            public string RelicKey { get; set; } = string.Empty;
            public string UpgradeId { get; set; } = string.Empty;
            public string UpgradeKey { get; set; } = string.Empty;
            public string AttachmentGuid { get; set; } = string.Empty;
            public int RoomIndex { get; set; } = -1;
            public int AttachmentType { get; set; } = -1;
            public bool AffectsHeroes { get; set; }
            public bool AffectsMonsters { get; set; }
            public int CapacityAtSetup { get; set; }
            public int AttachmentsAddedFromCards { get; set; }
            public int InitiallyImpactedCharacters { get; set; }
            public string RoomModifierClass { get; set; } = string.Empty;
            public int RoomModifierParam { get; set; }
            public int SpellCostWitnessCardId { get; set; }
            public int SpellCostControl { get; set; }
            public int SpellCostTarget { get; set; }
            public int XCostWitnessCardId { get; set; }
            public int XCostControlModifier { get; set; }
            public int XCostTargetModifier { get; set; }
            public string? Error { get; set; }
        }
    }
}
