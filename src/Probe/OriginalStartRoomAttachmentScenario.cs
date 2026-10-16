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
        internal const string RelicId = "2c97e1b3-d4bb-4133-9572-e582805c106f";
        internal const string RelicKey = "TrainRoomInstallFloor1AtStartOfCombat";
        internal const string UpgradeId = "05e215c6-600c-4ecb-a75e-c806dd596bcf";
        internal const string UpgradeKey = "FuelCar_Upgrade";

        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        internal static CoverageRecord Evidence { get; } = new CoverageRecord();

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            if (Acquired) return;
            SaveManager save = managers.GetSaveManager();
            if (save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("The floor-one attachment relic must be acquired before battle setup.");
            CollectableRelicData relic = save.GetAllGameData().GetAllCollectableRelicData()
                .Single(item => item.GetID() == RelicId);
            CheckOriginal(relic);
            if (save.GetCollectedRelics().Any(item => item.GetRelicDataID() == RelicId))
                throw new InvalidOperationException("The floor-one attachment relic is already present in this native test run.");
            save.AddRelic(relic);
            Acquired = true;
            log.LogInfo("ORIGINAL-START-ROOM-ATTACHMENT-ACQUIRED unchanged relic=" + relic.name + " id=" + relic.GetID() + ".");
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (Prepared) return;
            if (!Acquired || managers.GetCombatManager()!.GetTurnCount() != 0)
                throw new InvalidOperationException("The floor-one attachment relic must be inspected on deployment turn.");
            SaveManager save = managers.GetSaveManager();
            CollectableRelicData relic = save.GetAllGameData().GetAllCollectableRelicData()
                .Single(item => item.GetID() == RelicId);
            CheckOriginal(relic);
            if (managers.GetRelicManager().GetRelicEffect<RelicEffectAddTrainRoomAttachmentOnStartOfCombat>() == null)
                throw new InvalidOperationException("The original start-of-combat attachment effect was not initialized.");

            RoomState room = managers.GetRoomManager()!.GetRoom(0);
            if (room.GetIsPyreRoom() || room.HasEmbeddedTrainRoomAttachment())
                throw new InvalidOperationException("The native floor-one attachment scenario requires a non-Pyre, non-embedded target room.");
            TrainRoomAttachmentState[] matches = room.Attachments.Where(item => item.IsActive &&
                item.UpgradeStates.Any(upgrade => upgrade.Item2.GetID() == UpgradeId)).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException("The start-of-combat relic did not create exactly one active FuelCar attachment on floor one.");
            TrainRoomAttachmentState attachment = matches[0];
            if (attachment.CardState != null || (int)attachment.AttachType != 0 ||
                attachment.TrainSide != (Team.Type.Heroes | Team.Type.Monsters) || attachment.Guid.Length == 0 ||
                !Guid.TryParse(attachment.Guid, out Guid parsedId) || parsedId == Guid.Empty ||
                attachment.UpgradeStates.Count != 1 || attachment.UpgradeStates[0].Item1.GetTriggerUpgrades().Count != 1 ||
                attachment.UpgradeStates[0].Item2.name != UpgradeKey || room.NumRoomAttachmentsAddedFromCard != 0)
                throw new InvalidOperationException("The native FuelCar attachment identity, team, type, card counter or upgrade payload changed.");

            Evidence.Prepared = true;
            Evidence.RelicId = RelicId;
            Evidence.RelicKey = relic.name;
            Evidence.UpgradeId = UpgradeId;
            Evidence.UpgradeKey = attachment.UpgradeStates[0].Item2.name;
            Evidence.AttachmentGuid = attachment.Guid;
            Evidence.RoomIndex = room.GetRoomIndex();
            Evidence.AttachmentType = (int)attachment.AttachType;
            Evidence.AffectsHeroes = attachment.TrainSide.HasFlag(Team.Type.Heroes);
            Evidence.AffectsMonsters = attachment.TrainSide.HasFlag(Team.Type.Monsters);
            Evidence.CapacityAtSetup = room.GetCapacityInfo(Team.Type.Monsters).max;
            Evidence.AttachmentsAddedFromCards = room.NumRoomAttachmentsAddedFromCard;
            Evidence.InitiallyImpactedCharacters = attachment.CharactersImpacted.Count;
            Prepared = true;
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(managers.GetCombatManager(), true);
            log.LogInfo("ORIGINAL-START-ROOM-ATTACHMENT-PREPARED room=0 attachment=" + attachment.Guid +
                " upgrade=" + Evidence.UpgradeKey + " teams=Heroes|Monsters cardCounter=" + Evidence.AttachmentsAddedFromCards +
                " initiallyImpacted=" + Evidence.InitiallyImpactedCharacters + ".");
        }

        private static void CheckOriginal(CollectableRelicData relic)
        {
            RelicEffectData effect = relic.GetEffects().Single();
            CardUpgradeData upgrade = effect.GetParamCardUpgradeData();
            if (relic.GetID() != RelicId || relic.name != RelicKey || relic.DisallowInDeploymentPhase ||
                effect.GetEffectClassName() != "RelicEffectAddTrainRoomAttachmentOnStartOfCombat" ||
                (int)effect.GetParamSourceTeam() != 255 || effect.GetParamInt() != 0 || effect.GetParamInt2() != 0 ||
                effect.GetParamFloat() != 0 || effect.GetParamBool() || effect.GetParamBool2() ||
                effect.GetParamBool3() || effect.GetParamBool4() || effect.GetParamCardFilter() != null ||
                effect.GetEffectConditions().Count != 0 || upgrade == null || upgrade.GetID() != UpgradeId || upgrade.name != UpgradeKey)
                throw new InvalidOperationException("Original floor-one start-of-combat attachment relic definition changed.");
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
            public string? Error { get; set; }
        }
    }
}
