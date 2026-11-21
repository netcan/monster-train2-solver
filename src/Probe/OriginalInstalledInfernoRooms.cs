using System;
using System.Linq;
using BepInEx.Logging;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static partial class OriginalStartRoomAttachmentScenario
    {
        internal const string InfernoName = "original-installed-inferno-rooms";
        private const string InfernoMutatorId = "a5b3592e-e0ee-457d-8cd4-9fa102dfdc32";
        private const string InfernoMutatorKey = "Echoes_InfernoRoomInstalled";
        private const string InfernoUpgradeId = "96547940-8feb-4f73-b147-c763a64161c2";
        private const string InfernoUpgradeKey = "InfernoRoom";

        internal static bool InstalledInferno => Mode == InfernoName;
        private static MutatorData? inferno;
        private static object? infernoBefore;
        private static object[]? infernoRooms;

        internal static object? InstalledInfernoEvidence => inferno == null ? null : new
        { Before = infernoBefore, After = DescribeInferno(inferno), Rooms = infernoRooms };

        internal static bool SupportsInfernoMutator(RelicState owner, SaveManager save) =>
            owner.GetRelicDataID() == InfernoMutatorId && owner.GetEffects().Count == 3 &&
            owner.GetEffects().All(effect => effect is RelicEffectAddTrainRoomAttachmentOnStartOfCombat) &&
            save.GetAllGameData().FindMutatorData(InfernoMutatorId) is MutatorData definition && IsOriginalInferno(definition);

        private static bool IsOriginalInferno(MutatorData owner) =>
            owner.GetID() == InfernoMutatorId && owner.name == InfernoMutatorKey && !owner.DisallowInDeploymentPhase &&
            owner.GetEffects().Count == 3 && owner.GetEffects().Select((effect, index) =>
                effect.GetEffectClassName() == nameof(RelicEffectAddTrainRoomAttachmentOnStartOfCombat) &&
                effect.GetParamSourceTeam() == Team.Type.None && effect.GetParamInt() == index && effect.GetParamInt2() == 0 &&
                effect.GetParamFloat() == 0 && !effect.GetParamBool() && !effect.GetParamBool2() && !effect.GetParamBool3() &&
                !effect.GetParamBool4() && effect.GetParamCardFilter() == null && effect.GetEffectConditions().Count == 0 &&
                effect.GetParamCardUpgradeData() is CardUpgradeData upgrade && upgrade.GetID() == InfernoUpgradeId && upgrade.name == InfernoUpgradeKey &&
                RoomPreCombatProbe.Rule(upgrade, new System.Collections.Generic.List<string>()) is RoomPreCombatRule rule &&
                RoomPreCombatModel.Validate(rule) == null && rule.Effects.Count == 1 && rule.Effects[0].Type == "Damage").All(matches => matches);

        private static object DescribeInferno(MutatorData owner) => new
        {
            Id = owner.GetID(), Key = owner.name, PlacementExcluded = owner.DisallowInDeploymentPhase,
            Effects = owner.GetEffects().Select(effect => new
            {
                Type = effect.GetEffectClassName(), RoomIndex = effect.GetParamInt(), AttachType = effect.GetParamInt2(),
                SourceTeam = effect.GetParamSourceTeam().ToString(), Conditions = effect.GetEffectConditions().Count,
                UpgradeId = effect.GetParamCardUpgradeData().GetID(), UpgradeKey = effect.GetParamCardUpgradeData().name,
                Modifiers = effect.GetParamCardUpgradeData().GetRoomModifierUpgrades().Select(modifier => new
                {
                    Type = modifier.GetRoomStateModifierClassName(), modifier.DisallowedInDeploymentPhase,
                    Effects = modifier.GetParamCardEffectData().Select(RuleCatalogProbe.Effect).ToArray()
                }).ToArray()
            }).ToArray()
        };

        private static void AcquireInferno(SaveManager save, ManualLogSource log)
        {
            if (save.GetMutators().Count != 0 || save.GetCovenants().Count != 0 ||
                save.GetAllGameData().FindMutatorData(InfernoMutatorId) is not MutatorData owner || !IsOriginalInferno(owner))
                throw new InvalidOperationException("Expected the unchanged Echoes Inferno mutator in an isolated run.");
            inferno = owner;
            infernoBefore = DescribeInferno(owner);
            save.AddMutator(owner);
            log.LogInfo("ORIGINAL-INSTALLED-INFERNO-ACQUIRED " + owner.name + " " + owner.GetID());
        }

        private static void PrepareInferno(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            RoomManager rooms = managers.GetRoomManager();
            if (inferno == null || save.GetMutators().Count != 1 || !SupportsInfernoMutator(save.GetMutators().Single(), save))
                throw new InvalidOperationException("The original mutator's three effects did not initialize.");
            RoomCapacityState[] projected = RoomCapacityProbe.Capture(rooms);
            infernoRooms = Enumerable.Range(0, 3).Select(index =>
            {
                RoomState room = rooms.GetRoom(index);
                TrainRoomAttachmentState attachment = room.Attachments.Single(item => item.IsActive);
                RoomCapacityAttachmentState data = projected.Single(item => item.RoomIndex == index).Attachments!.Single();
                string upgradeId = attachment.UpgradeStates.Single().Item2.GetID();
                if (room.GetIsPyreRoom() || room.HasEmbeddedTrainRoomAttachment() || attachment.CardState != null ||
                    (int)attachment.AttachType != 0 || attachment.TrainSide != (Team.Type.Heroes | Team.Type.Monsters) ||
                    !Guid.TryParse(attachment.Guid, out Guid id) || id == Guid.Empty || upgradeId != InfernoUpgradeId ||
                    room.NumRoomAttachmentsAddedFromCard != 0 || data.DataId != upgradeId || data.AttachmentId != attachment.Guid)
                    throw new InvalidOperationException("Original Inferno startup attachment changed on floor " + index + ".");
                return new
                {
                    RoomIndex = index, attachment.Guid, AttachType = (int)attachment.AttachType,
                    Team = attachment.TrainSide.ToString(), UpgradeId = upgradeId,
                    AttachmentsAddedFromCards = room.NumRoomAttachmentsAddedFromCard, Projection = data
                };
            }).ToArray();
            log.LogInfo("ORIGINAL-INSTALLED-INFERNO-PREPARED all three original cardless Inferno attachments.");
        }
    }
}
