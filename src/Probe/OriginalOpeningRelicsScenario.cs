#if DEVELOPMENT_PROBE
using System;
using System.Linq;
using BepInEx.Logging;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalOpeningRelicsScenario
    {
        private const string Prefix = "original-opening-relics:";
        private const string RosterPrefix = "original-opening-relic-roster:";
        internal const string PhaseName = "original-opening-phase-relics";
        private static readonly string Modifier = Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") ?? "";
        private static readonly string[] DrawIds = OriginalAfterDrawGenerationScenario.Ids.Take(3).ToArray();
        private static readonly (string Id, string UpgradeId)[] RoomOwners =
        {
            (OriginalStartRoomAttachmentScenario.RelicId, OriginalStartRoomAttachmentScenario.UpgradeId),
            (OriginalStartRoomAttachmentScenario.Floor2RelicId, OriginalStartRoomAttachmentScenario.Floor2UpgradeId),
            (OriginalStartRoomAttachmentScenario.Floor3RelicId, OriginalStartRoomAttachmentScenario.Floor3UpgradeId)
        };
        private static bool Phase => Modifier == PhaseName;
        private static bool Passive => OriginalOpeningPassiveRelicsScenario.Enabled;
        private static bool Roster => Modifier.StartsWith(RosterPrefix, StringComparison.Ordinal);
        private static (string DataId, string AssetKey, bool DisallowedInPlacementPhase, string[] EffectTypes)[]? expectedRoster;
        private static bool rosterValidated;
        internal static bool Enabled => Passive || Phase || Roster || Modifier.StartsWith(Prefix, StringComparison.Ordinal);
        internal static bool Acquired { get; private set; }
        internal static bool CaptureSpawnStatuses => Acquired && !Phase && !Passive && !Roster;
        internal static bool CaptureAfterDraw(string ownerId) => Acquired && Phase && DrawIds.Contains(ownerId);
        internal static object? RosterOwners => rosterValidated ? expectedRoster!.Select(owner => new
        {
            owner.DataId, owner.AssetKey, owner.DisallowedInPlacementPhase, owner.EffectTypes
        }).ToArray() : null;

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            if (Acquired) return;
            var save = managers.GetSaveManager();
            if (typeof(CardState).Module.ModuleVersionId != new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d") ||
                save.PreviewMode || save.InTestScenario || save.GetGameSequence() == SaveData.GameSequence.InBattle ||
                save.GetCurrentDistance() != 0 || save.GetMutators().Count != 0 || save.GetCovenants().Count != 0)
                throw new InvalidOperationException("Original opening relics require a fresh 2.3.1 Ring 1 before combat.");

            string[] ids = Passive ? OriginalOpeningPassiveRelicsScenario.OwnerIds :
                Phase ? RoomOwners.Select(owner => owner.Id).Concat(DrawIds).ToArray() :
                Modifier.Substring((Roster ? RosterPrefix : Prefix).Length).Split(',');
            if (ids.Any(id => !Guid.TryParseExact(id, "D", out Guid parsed) || parsed.ToString() != id) ||
                ids.Distinct(StringComparer.Ordinal).Count() != ids.Length)
                throw new InvalidOperationException("Expected distinct original collectible relic IDs.");

            var data = save.GetAllGameData().GetAllCollectableRelicData();
            var owned = save.GetCollectedRelics().Select(relic => relic.GetRelicDataID()).ToArray();
            var definitions = ids.Select(id =>
            {
                var relic = data.SingleOrDefault(item => item.GetID() == id) ??
                    throw new InvalidOperationException("Original collectible relic does not exist: " + id);
                if (Roster)
                {
                    if (owned.Contains(id) || string.IsNullOrEmpty(relic.name) || !relic.GetEffects().Any())
                        throw new InvalidOperationException("Expected an unowned original collectible relic with effects: " + relic.name);
                    return (Relic: relic, StatusId: string.Empty);
                }
                if (Passive)
                {
                    if (owned.Contains(id) || !OriginalOpeningPassiveRelicsScenario.MatchesOriginal(relic))
                        throw new InvalidOperationException("Unexpected original opening passive relic: " + relic.name);
                    return (Relic: relic, StatusId: string.Empty);
                }
                var effect = relic.GetEffects().Single();
                if (Phase)
                {
                    if (owned.Contains(id) || !MatchesPhaseOwner(relic, effect))
                        throw new InvalidOperationException("Unexpected original opening phase relic: " + relic.name);
                    return (Relic: relic, StatusId: string.Empty);
                }
                var status = effect.GetParamStatusEffects().Single();
                if (owned.Contains(id) || relic.DisallowInDeploymentPhase ||
                    effect.GetEffectClassName() != nameof(RelicEffectAddStatusEffectOnSpawn) ||
                    effect.GetParamSourceTeam() is not (Team.Type.Monsters or Team.Type.Heroes) ||
                    effect.GetParamCharacterSubtype()?.IsNone != true || effect.GetParamExcludeCharacterSubtypes().Length != 0 ||
                    effect.GetParamCharacters().Count != 0 || effect.GetEffectConditions().Count != 0 ||
                    effect.GetParamBool() || effect.GetParamBool2() || effect.GetParamInt2() != 0 ||
                    effect.GetParamString() != string.Empty || string.IsNullOrEmpty(status.statusId) || status.count <= 0)
                    throw new InvalidOperationException("Expected an original unconditional spawn-status relic: " + relic.name);
                return (Relic: relic, StatusId: status.statusId);
            }).ToArray();
            if (!Phase && !Passive && !Roster && definitions.Select(item => item.StatusId).Distinct(StringComparer.Ordinal).Count() != definitions.Length)
                throw new InvalidOperationException("Each opening relic needs a distinct status for its native callback witness.");

            if (Roster) expectedRoster = definitions.Select(item => (item.Relic.GetID(), item.Relic.name,
                item.Relic.DisallowInDeploymentPhase, item.Relic.GetEffects().Select(effect => effect.GetEffectClassName()).ToArray())).ToArray();
            foreach (var item in definitions) save.AddRelic(item.Relic);
            if (!save.GetCollectedRelics().Select(relic => relic.GetRelicDataID()).Where(ids.Contains).SequenceEqual(ids))
                throw new InvalidOperationException("Collected original relics differ from acquisition order.");
            Acquired = true;
            log.LogInfo("ORIGINAL-OPENING-RELICS acquired unchanged original definitions " + string.Join(",", ids));
        }

        internal static void ValidateRosterDeployment(BattleTurnState decision, ManualLogSource log)
        {
            if (!Roster || rosterValidated) return;
            if (!Acquired || expectedRoster == null || decision.Spawn.Turn != 0 ||
                decision.Spawn.Train.Context is not { Relics: { } active } context)
                throw new InvalidOperationException("Original relic roster requires a captured deployment before the first action.");

            var ids = expectedRoster.Select(owner => owner.DataId).ToArray();
            var owners = active.Where(owner => ids.Contains(owner.DataId)).ToArray();
            if (!owners.Select(owner => owner.DataId).SequenceEqual(ids))
                throw new InvalidOperationException("Active original relic roster differs from acquisition order.");
            for (int index = 0; index < owners.Length; index++)
            {
                var owner = owners[index];
                var expected = expectedRoster[index];
                if (owner.AssetKey != expected.AssetKey || owner.IsCovenant != false ||
                    owner.DisallowedInPlacementPhase != expected.DisallowedInPlacementPhase ||
                    !owner.EffectTypes.SequenceEqual(expected.EffectTypes))
                    throw new InvalidOperationException("Active relic roster differs from the original definition: " + expected.AssetKey);
            }

            string? unsupported = RelicModel.Validate(context);
            if (unsupported != null)
                throw new InvalidOperationException("Original relic roster is unsupported at deployment: " + unsupported);
            rosterValidated = true;
            log.LogInfo("ORIGINAL-OPENING-RELIC-ROSTER validated active deployment owners " + string.Join(",", ids));
        }

        private static bool MatchesPhaseOwner(CollectableRelicData relic, RelicEffectData effect)
        {
            if (effect.GetEffectConditions().Count != 0) return false;
            int room = Array.FindIndex(RoomOwners, owner => owner.Id == relic.GetID());
            if (room >= 0)
                return !relic.DisallowInDeploymentPhase &&
                    effect.GetEffectClassName() == nameof(RelicEffectAddTrainRoomAttachmentOnStartOfCombat) &&
                    effect.GetParamInt() == room && effect.GetParamInt2() == 0 &&
                    (int)effect.GetParamSourceTeam() == (room == 2 ? (int)Team.Type.Monsters : 255) &&
                    effect.GetParamCardUpgradeData()?.GetID() == RoomOwners[room].UpgradeId;

            return DrawIds.Contains(relic.GetID()) && relic.DisallowInDeploymentPhase &&
                effect.GetEffectClassName() == nameof(RelicEffectAddBattleCardToHand) &&
                effect.GetParamInt() == 1 && effect.GetParamInt2() == 0;
        }
    }
}
#endif
