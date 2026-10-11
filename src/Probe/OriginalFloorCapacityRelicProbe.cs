using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalFloorCapacityRelicProbe
    {
        internal sealed class OriginalEffect
        {
            public int EffectIndex { get; set; }
            public string Type { get; set; } = "";
            public int ParamInt { get; set; }
            public int ParamInt2 { get; set; }
            public int SourceTeam { get; set; }
            public bool ParamBool { get; set; }
            public bool ParamBool2 { get; set; }
            public string ParamString { get; set; } = "";
            public int ConditionCount { get; set; }
            public int CharacterFilterCount { get; set; }
            public string? RequiredMutatorId { get; set; }
            public string? CardUpgradeId { get; set; }
            public int CharacterCount { get; set; }
            public string? CharacterSubtypeKey { get; set; }
            public int ExcludedSubtypeCount { get; set; }
            public int StatusEffectCount { get; set; }
            public int CardEffectCount { get; set; }
        }
        internal sealed class OriginalDefinition
        {
            public string OwnerDataId { get; set; } = "";
            public string AssetKey { get; set; } = "";
            public bool DisallowedInPlacementPhase { get; set; }
            public OriginalEffect[] Effects { get; set; } = null!;
        }
        internal sealed class DeploymentCard
        {
            public string CardDataId { get; set; } = "";
            public int Size { get; set; }
            public bool ActualDeployable { get; set; }
            public string[] DeploymentUpgradeIds { get; set; } = null!;
            public string[] DeploymentTraitNames { get; set; } = null!;
            public bool[] DeploymentTraitFlags { get; set; } = null!;
        }
        internal sealed class Query
        {
            public BattleResourceState Resources { get; set; } = null!;
            public CombatRelicState[] Relics { get; set; } = null!;
            public bool Preview { get; set; }
            public int Actual { get; set; }
            public bool Matrix { get; set; }
            // Scratch rules unregister only this owner's capacity mutation.
            // Its other effects and native owned relic remain installed.
            public string? OwnerRemovalDataId { get; set; }
            public RoomCapacityState[] RoomCapacitiesBefore { get; set; } = null!;
            public RoomCapacityState[] RoomCapacitiesAfter { get; set; } = null!;
            public int Notifications { get; set; }
        }
        internal sealed class Initialization
        {
            public int RoomIndex { get; set; }
            public bool IsPyre { get; set; }
            public int PlayerSlots { get; set; }
            public int EnemySlots { get; set; }
            public RelicResourceProbe.ResourceSnapshot Before { get; set; } = null!;
            public RoomCapacityState? Actual { get; set; }
            internal int EnemyMaximum;
        }
        internal sealed class Restoration
        {
            public bool MutableRulesObjectRestored { get; set; }
            public bool MutationReferencesAndOrderRestored { get; set; }
            public bool PreviewBefore { get; set; }
            public bool PreviewAfter { get; set; }
            public RoomCapacityState[] RoomCapacitiesBefore { get; set; } = null!;
            public RoomCapacityState[] RoomCapacitiesAfter { get; set; } = null!;
            public BattleRngStream[] RngBefore { get; set; } = null!;
            public BattleRngStream[] RngAfter { get; set; } = null!;
            public uint[] UnityRngBefore { get; set; } = null!;
            public uint[] UnityRngAfter { get; set; } = null!;
        }
        internal static readonly List<OriginalDefinition> OriginalDefinitions = new List<OriginalDefinition>();
        internal static readonly List<DeploymentCard> DeploymentCards = new List<DeploymentCard>();
        internal static readonly List<Query> Queries = new List<Query>();
        internal static readonly List<Initialization> Initializations = new List<Initialization>();
        internal static readonly List<Restoration> MatrixRestorations = new List<Restoration>();
        private static readonly Dictionary<RoomState, Initialization> pendingRooms = new Dictionary<RoomState, Initialization>();
        private static readonly Dictionary<RoomState, Initialization> initializedRooms = new Dictionary<RoomState, Initialization>();
        private static readonly HashSet<string> seen = new HashSet<string>();
        private static bool armed, capturing, matrix, matrixCompleted, matrixAttempted;
        private static string? omittedOwner;
        private static Query? current;

        internal static void Begin(AllGameManagers managers, IEnumerable<CollectableRelicData> originals)
        {
            foreach (CollectableRelicData relic in originals)
                OriginalDefinitions.Add(new OriginalDefinition { OwnerDataId = relic.GetID(), AssetKey = relic.name,
                    DisallowedInPlacementPhase = relic.DisallowInDeploymentPhase,
                    Effects = relic.GetEffects().Select((effect, index) => new OriginalEffect {
                        EffectIndex = index, Type = effect.GetEffectClassName(), ParamInt = effect.GetParamInt(), ParamInt2 = effect.GetParamInt2(),
                        SourceTeam = (int)effect.GetParamSourceTeam(), ParamBool = effect.GetParamBool(), ParamBool2 = effect.GetParamBool2(),
                        ParamString = effect.GetParamString(), ConditionCount = effect.GetEffectConditions().Count,
                        CharacterFilterCount = (effect.GetParamCardFilter() == null ? 0 : 1) + (effect.GetParamCardFilterSecondary() == null ? 0 : 1),
                        RequiredMutatorId = effect.GetParamMutatorData()?.GetID(), CardUpgradeId = effect.GetParamCardUpgradeData()?.GetID(),
                        CharacterCount = effect.GetParamCharacters().Count,
                        CharacterSubtypeKey = effect.GetParamCharacterSubtype().IsNone ? null : effect.GetParamCharacterSubtype().Key,
                        ExcludedSubtypeCount = effect.GetParamExcludeCharacterSubtypes().Length,
                        StatusEffectCount = effect.GetParamStatusEffects().Length, CardEffectCount = effect.GetParamCardEffectData()?.Count ?? 0 }).ToArray() });
            armed = true;
        }

        internal static void CaptureDeploymentCards(AllGameManagers managers)
        {
            DeploymentCards.Clear();
            foreach (CardState card in managers.GetSaveManager().GetDeckState().Where(card => card.GetSpawnCharacterData() != null))
            {
                CardUpgradeState[] upgrades = card.GetCardStateModifiers().GetCardUpgrades().ToArray();
                CardTraitData[] traits = upgrades.SelectMany(upgrade => upgrade.GetTraitDataUpgrades()).ToArray();
                DeploymentCards.Add(new DeploymentCard { CardDataId = card.GetCardDataID(), Size = card.GetSize(),
                    ActualDeployable = managers.GetCardManager()!.IsDeployableCard(card),
                    DeploymentUpgradeIds = upgrades.Select(upgrade => upgrade.GetCardUpgradeDataId()).ToArray(),
                    DeploymentTraitNames = traits.Select(trait => trait.GetTraitStateName()).ToArray(),
                    DeploymentTraitFlags = traits.Select(trait => trait.GetDrawInDeploymentPhase()).ToArray() });
            }
        }

        internal static void Matrix(BattleResourceState baseline)
        {
            if (!armed || matrixCompleted || !OriginalFloorCapacityRelicScenario.CaptureReady || FullBattleTrace.Active == null) return;
            SaveManager save = AllGameManagers.Instance!.GetSaveManager();
            var field = AccessTools.Field(typeof(SaveManager), "mutableRules");
            var original = (MutableRules)field.GetValue(save);
            var mutations = ((List<IBalanceEditingRelicEffect>)AccessTools.Field(typeof(MutableRules), "mutators").GetValue(original)).ToArray();
            object? previewSave = AccessTools.Field(typeof(SaveManager), "previewSaveData").GetValue(save);
            var proof = new Restoration { PreviewBefore = save.PreviewMode, RoomCapacitiesBefore = Rooms(),
                RngBefore = Rng(), UnityRngBefore = RngCalibration.Words(UnityEngine.Random.state) };
            matrix = true;
            try
            {
                foreach (bool preview in new[] { false, true })
                {
                    save.PreviewMode = preview;
                    RecordMatrixQuery(save, baseline);
                }
                save.PreviewMode = false;
                foreach (var owner in OriginalFloorCapacityRelicScenario.Artifacts)
                {
                    RelicState relic = save.GetCollectedRelics().Single(item => item.GetRelicDataID() == owner.Id);
                    IRelicEffect excluded = relic.GetEffects().Single(effect => effect is RelicEffectChangeFloorCapacity);
                    var scratch = new MutableRules();
                    foreach (IBalanceEditingRelicEffect effect in mutations)
                        if (!ReferenceEquals(effect, excluded)) scratch.AddMutation(effect);
                    omittedOwner = owner.Id;
                    field.SetValue(save, scratch);
                    RecordMatrixQuery(save, baseline);
                    field.SetValue(save, original);
                }
            }
            finally
            {
                field.SetValue(save, original); omittedOwner = null;
                save.PreviewMode = proof.PreviewBefore;
                AccessTools.Field(typeof(SaveManager), "previewSaveData").SetValue(save, previewSave);
                matrix = false;
                proof.MutableRulesObjectRestored = ReferenceEquals(field.GetValue(save), original);
                var restored = (List<IBalanceEditingRelicEffect>)AccessTools.Field(typeof(MutableRules), "mutators").GetValue(original);
                proof.MutationReferencesAndOrderRestored = restored.Count == mutations.Length &&
                    restored.Where((effect, index) => !ReferenceEquals(effect, mutations[index])).Count() == 0;
                proof.PreviewAfter = save.PreviewMode; proof.RoomCapacitiesAfter = Rooms();
                proof.RngAfter = Rng(); proof.UnityRngAfter = RngCalibration.Words(UnityEngine.Random.state);
                MatrixRestorations.Add(proof);
            }
            matrixCompleted = true;
        }

        private static void RecordMatrixQuery(SaveManager save, BattleResourceState baseline)
        {
            bool priorCapturing = capturing;
            capturing = true;
            Query query;
            try
            {
                var managers = AllGameManagers.Instance!;
                query = new Query { Resources = baseline, Relics = RelicProbe.Capture(managers), Preview = save.PreviewMode,
                    Matrix = true, OwnerRemovalDataId = omittedOwner, RoomCapacitiesBefore = Rooms() };
                query.Actual = save.GetCharacterCapacityPerFloor();
                query.RoomCapacitiesAfter = Rooms();
            }
            finally { capturing = priorCapturing; }
            CombatManager? combat = AllGameManagers.Instance!.GetCombatManager();
            string key = query.Matrix + "/" + query.OwnerRemovalDataId + "/" + query.Preview + "/" + query.Actual + "/" +
                (combat?.GetTurnCount() ?? 0) + "/" + string.Join(",", query.RoomCapacitiesAfter.Select(room => room.RoomIndex));
            if (seen.Add(key)) Queries.Add(query);
        }

        private static RoomCapacityState[] Rooms()
        {
            RoomManager? manager = AllGameManagers.Instance!.GetRoomManager();
            if (manager == null) return Array.Empty<RoomCapacityState>();
            var rooms = new List<RoomCapacityState>();
            for (int index = 0; index < manager.GetNumRooms(); index++)
            {
                RoomState? room = manager.GetRoom(index);
                if (room == null || AccessTools.Field(typeof(RoomState), "heroSpawnPointGroup").GetValue(room) == null ||
                    AccessTools.Field(typeof(RoomState), "monsterSpawnPointGroup").GetValue(room) == null) continue;
                rooms.Add(new RoomCapacityState(index, room.GetCapacityInfo(Team.Type.Monsters).max,
                    room.GetCapacityInfo(Team.Type.Heroes).max, room.GetIsPyreRoom()));
            }
            return rooms.ToArray();
        }
        private static void TryMatrix(Query? source)
        {
            if (!armed || source == null || source.Preview || capturing || matrix || matrixCompleted || matrixAttempted ||
                !OriginalFloorCapacityRelicScenario.CaptureReady || FullBattleTrace.Active == null || Rooms().Length == 0) return;
            matrixAttempted = true;
            try { Matrix(source.Resources); }
            catch (Exception error) { FullBattleTrace.Active.CaptureFailure(error); }
        }
        private static BattleRngStream[] Rng() => Enum.GetValues(typeof(RngId)).Cast<RngId>()
            .Where(id => id != RngId.NonDeterministic && id != RngId.Chatter).Select(id =>
            {
                var native = (HadesRNG)AccessTools.Method(typeof(RandomManager), "GetRng").Invoke(null, new object[] { id });
                uint[] words = RngCalibration.Words(native.GetState());
                return new BattleRngStream(id.ToString(), native.GetSeed(), new UnityRng(words[0], words[1], words[2], words[3]));
            }).ToArray();

        [HarmonyPatch(typeof(SaveManager), nameof(SaveManager.GetCharacterCapacityPerFloor))]
        private static class GetterPatch
        {
            private static void Prefix(out Query? __state)
            {
                __state = null;
                if (!armed || capturing || FullBattleTrace.Active == null) return;
                capturing = true;
                try
                {
                    RelicResourceProbe.ResourceSnapshot snapshot = RelicResourceProbe.Capture();
                    __state = new Query { Resources = snapshot.Resources, Relics = snapshot.Relics, Preview = snapshot.Preview,
                        Matrix = matrix, OwnerRemovalDataId = omittedOwner, RoomCapacitiesBefore = Rooms() };
                    current = __state;
                }
                catch (Exception error) { FullBattleTrace.Active.CaptureFailure(error); }
                finally { capturing = false; }
            }
            private static void Postfix(int __result, Query? __state)
            {
                if (__state == null) return;
                try
                {
                    __state.Actual = __result; __state.RoomCapacitiesAfter = Rooms();
                    CombatManager? combat = AllGameManagers.Instance!.GetCombatManager();
                    string key = __state.Matrix + "/" + __state.OwnerRemovalDataId + "/" + __state.Preview + "/" + __result + "/" +
                        (combat?.GetTurnCount() ?? 0) + "/" + string.Join(",", __state.RoomCapacitiesAfter.Select(room => room.RoomIndex));
                    if (seen.Add(key)) Queries.Add(__state);
                }
                catch (Exception error) { FullBattleTrace.Active!.CaptureFailure(error); }
                finally { current = null; }
                TryMatrix(__state);
            }
        }

        [HarmonyPatch(typeof(RoomState), "SetupSpawnPoints")]
        private static class InitializationPatch
        {
            private static void Prefix(RoomState __instance, Team.Type team)
            {
                if (!armed || !OriginalFloorCapacityRelicScenario.Prepared || team != Team.Type.Heroes || FullBattleTrace.Active == null) return;
                capturing = true;
                try
                {
                    var record = new Initialization { RoomIndex = __instance.GetRoomIndex(), IsPyre = __instance is PyreRoomState,
                        Before = RelicResourceProbe.Capture() };
                    pendingRooms[__instance] = record; Initializations.Add(record);
                }
                catch (Exception error) { FullBattleTrace.Active.CaptureFailure(error); }
                finally { capturing = false; }
            }
            private static void Postfix(RoomState __instance, Team.Type team, SpawnPointGroup __result)
            {
                if (!pendingRooms.TryGetValue(__instance, out Initialization record)) return;
                if (team == Team.Type.Heroes) { record.EnemySlots = __result.NumSpawnPoints; record.EnemyMaximum = __result.GetMaxCapacity(); }
                else if (team == Team.Type.Monsters)
                {
                    record.PlayerSlots = __result.NumSpawnPoints;
                    record.Actual = new RoomCapacityState(record.RoomIndex, __result.GetMaxCapacity(), record.EnemyMaximum, record.IsPyre);
                    initializedRooms[__instance] = record;
                    pendingRooms.Remove(__instance);
                }
            }
        }
        // RoomManager assigns this index after RoomState.Setup has created both
        // spawn groups; the initial native value is zero for every new room.
        [HarmonyPatch(typeof(RoomState), nameof(RoomState.SetRoomIndex))]
        private static class RoomIndexPatch
        {
            private static void Postfix(RoomState __instance, int setRoomIndex)
            {
                if (!initializedRooms.TryGetValue(__instance, out Initialization record) || record.Actual == null) return;
                record.RoomIndex = setRoomIndex;
                record.Actual = new RoomCapacityState(setRoomIndex, record.Actual.PlayerMaximum, record.Actual.EnemyMaximum, record.IsPyre);
            }
        }
        [HarmonyPatch(typeof(RelicManager), nameof(RelicManager.NotifyRelicTriggered))]
        private static class NotificationPatch
        {
            private static void Prefix(IRelicEffect triggeredEffect)
            { if (!capturing && current != null && triggeredEffect is RelicEffectChangeFloorCapacity) current.Notifications++; }
        }
    }
}
