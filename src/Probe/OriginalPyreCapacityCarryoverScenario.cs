#if DEVELOPMENT_PROBE
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalPyreCapacityCarryoverScenario
    {
        internal const string Name = "original-pyre-capacity-carryover";
        private const string Effect = nameof(RelicEffectModifyRandomRoomCapacityAfterBattle);
        private const string BogwurmId = "7a60e645-e4f0-403c-9b4a-e794675f3a60";
        private const string LifeId = "96cca2fd-d096-4340-9054-d498826a0781";
        private const int ExpandedFloor = 0;
        private const int ControlFloor = 1;
        private static readonly int[] bonuses = { 2, 0, 1 };
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static object? Definition { get; private set; }
        internal static object? CurrentDefinition => original == null || artifact == null ? null : Describe(original, artifact);
        internal static object? Witness { get; private set; }
        internal static readonly List<OriginalRandomRoomCapacityScenario.Callback> Callbacks = new();
        private static string? mode;
        private static CharacterData? original;
        private static PyreArtifactData? artifact;
        private static bool ControlledAttachment => mode == "Life";
        private static bool seeded;
        private static CardState[] cards = Array.Empty<CardState>();
        private static int[] ids = Array.Empty<int>();

        private sealed class Snapshot : OriginalRandomRoomCapacityScenario.Snapshot
        {
            public int[] Bonuses { get; }
            public bool Applied { get; }

            internal Snapshot(AllGameManagers managers) : base(managers)
            {
                var persistent = managers.GetSaveManager().GetRelicPersistentState();
                Bonuses = ReadBonuses(persistent);
                Applied = HasApplied(persistent);
            }
        }

        private static int[] ReadBonuses(RelicPersistentState state) => Enumerable.Range(0, 3).Select(state.GetAdditionalCapacity).ToArray();
        private static bool HasApplied(RelicPersistentState state) =>
            (bool)AccessTools.Field(typeof(RelicPersistentState), "hasAppliedCapacityToThisBattle").GetValue(state);

        private static object Describe(CharacterData character, PyreArtifactData owner)
        {
            return new { Id = owner.GetID(), Key = owner.name, PyreId = character.GetID(), PyreKey = character.GetAssetKey(),
                Mode = mode, ControlledAttachment, Placement = owner.DisallowInDeploymentPhase, Effects = owner.GetEffects().Select(effect => new {
                    Type = effect.GetEffectClassName(), Amount = effect.GetParamInt(), Conditions = effect.GetEffectConditions().Count
                }).ToArray() };
        }

        internal static void SelectBeforeRun(RunSetupScreen setup, SaveManager save, ManualLogSource log)
        {
            mode = Environment.GetEnvironmentVariable("MT2_PROBE_PYRE_CAPACITY_MODE");
            (string id, string key) = mode switch
            {
                "Bogwurm" => (BogwurmId, "PyreHeartBogwurm_Roomsize_ArtifactData"),
                "Life" => (LifeId, "PyreHeartLife_ArtifactData"),
                _ => throw new InvalidOperationException("Unknown original Pyre capacity carryover selection.")
            };
            var all = save.GetAllGameData();
            var character = all.GetAllCharacterData()
                .Single(value => value.GetPyreHeartData()?.GetPyreArtifact()?.GetID() == BogwurmId);
            var owner = ControlledAttachment ? all.FindPyreArtifactData(id)! : character.GetPyreHeartData().GetPyreArtifact();
            if (owner == null) throw new InvalidOperationException("The original catalog Pyre capacity asset is missing.");
            var effects = owner.GetEffects();
            if (owner.name != key || owner.DisallowInDeploymentPhase || effects.Any(effect => effect.GetEffectConditions().Count != 0) ||
                !effects.Select(effect => (effect.GetEffectClassName(), effect.GetParamInt())).SequenceEqual(mode == "Life"
                    ? new[] { (RelicResourceModel.FloorCapacity, -1), (Effect, 1) } : new[] { (Effect, 1) }))
                throw new InvalidOperationException("Original Pyre capacity carryover definition changed.");
            original = character;
            artifact = owner;
            Definition = Describe(character, owner);
            var info = (PyreHeartInfoUI)AccessTools.Field(typeof(RunSetupScreen), "pyreHeartInfo").GetValue(setup);
            info.Set(character, setEditable: true);
            log.LogInfo("ORIGINAL-PYRE-CAPACITY-SELECTED " + character.GetAssetKey() +
                " catalogArtifact=" + key + " controlledAttachment=" + ControlledAttachment);
        }

        internal static void SeedBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            if (seeded) return;
            var save = managers.GetSaveManager();
            var activeArtifact = save.GetActiveSavePyreArtifactState();
            if (original == null || artifact == null || save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle ||
                save.GetActiveSavePyreCharacterData().GetID() != original.GetID() || activeArtifact.GetRelicDataID() != BogwurmId)
                throw new InvalidOperationException("The original Pyre selection did not reach the isolated active save.");
            if (ControlledAttachment)
            {
                if (activeArtifact.GetEffects().OfType<IBalanceEditingRelicEffect>().Any())
                    throw new InvalidOperationException("Replacing this Pyre state would leave registered balance effects.");
                var rawSave = (SaveData)AccessTools.Property(typeof(SaveManager), "ActiveSaveData").GetValue(save);
                var rules = (MutableRules)AccessTools.Field(typeof(SaveManager), "mutableRules").GetValue(save);
                rawSave.SetPyreArtifactState(new PyreArtifactState(artifact, rules));
                log.LogInfo("ORIGINAL-PYRE-CAPACITY-CONTROLLED-ATTACHMENT " + artifact.name + " on " + original.GetAssetKey());
            }
            if (save.GetActiveSavePyreArtifactState().GetRelicDataID() != artifact.GetID())
                throw new InvalidOperationException("The requested native Pyre artifact did not reach the isolated active save.");
            var persistent = save.GetRelicPersistentState();
            if (ReadBonuses(persistent).Any(value => value != 0) || HasApplied(persistent))
                throw new InvalidOperationException("The isolated Pyre capacity state must begin empty.");
            for (int floor = 0; floor < bonuses.Length; floor++)
                if (bonuses[floor] != 0) persistent.AddAdditionalCapacityAfterBattle(floor, bonuses[floor]);
            if (!ReadBonuses(persistent).SequenceEqual(bonuses) || HasApplied(persistent))
                throw new InvalidOperationException("The isolated Pyre capacity state could not be seeded.");
            seeded = true;
            log.LogInfo("ORIGINAL-PYRE-CAPACITY-SEEDED 2,0,1 in the isolated run.");
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            var deployment = new Snapshot(managers);
            var application = Callbacks.SingleOrDefault(row => row.Kind == "Apply" && row.Completed);
            if (!seeded || deployment.Turn != 0 || !deployment.Placement || deployment.Preview ||
                application?.Before is not Snapshot before || application.After is not Snapshot after ||
                before.Applied || !after.Applied || !deployment.Applied || !before.Bonuses.SequenceEqual(bonuses) ||
                !after.Bonuses.SequenceEqual(bonuses) || !deployment.Bonuses.SequenceEqual(bonuses) ||
                !before.BattleRng.Equals(after.BattleRng) || after.SelectedRoom != ExpandedFloor)
                throw new InvalidOperationException("Original Pyre capacity carryover did not settle before deployment input.");
            foreach (var room in before.Rooms)
            {
                int expected = room.PlayerMaximum + (room.IsPyre ? 0 : bonuses[room.RoomIndex]);
                if (after.Rooms[room.RoomIndex].PlayerMaximum != expected || deployment.Rooms[room.RoomIndex].PlayerMaximum != expected)
                    throw new InvalidOperationException("The seeded capacity did not reach the first deployment input.");
            }
            int witnessSize = Math.Max(before.Rooms[ExpandedFloor].PlayerMaximum, before.Rooms[ControlFloor].PlayerMaximum) + 1;
            if (witnessSize > 6 || after.Rooms[ExpandedFloor].PlayerMaximum < witnessSize)
                throw new InvalidOperationException("The isolated deployment does not expose a size supported only by the expanded floor.");
            cards = OriginalRandomRoomCapacityScenario.CreateDeploymentControls(managers, 3, witnessSize);
            ids = cards.Select(FullBattleTrace.Active!.CardId).ToArray();
            Prepared = true;
            OriginalRandomRoomCapacityScenario.RefreshDeploymentControls(managers);
            log.LogInfo($"ORIGINAL-PYRE-CAPACITY-PREPARED mode={mode} floor={ExpandedFloor} witnessSize={witnessSize}");
        }

        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            int next = Array.FindIndex(ids, id => state.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            if (next < 0) return null;
            if (state.Spawn.Turn != 0) throw new InvalidOperationException("Pyre capacity controls must deploy on turn zero.");
            var plays = BattleActionModel.EnumerateSupportedPlays(state, ids[next]);
            var play = plays.FirstOrDefault(item => item.RoomIndex == next);
            if (play == null) throw new InvalidOperationException("Pyre capacity control did not fit its intended floor.");
            if (next == 0 && Witness == null)
            {
                var managers = AllGameManagers.Instance!;
                var rooms = managers.GetRoomManager();
                int size = cards[next].GetSize();
                bool nativeFits = !rooms.GetRoom(ExpandedFloor).WillBeAtCapacity(Team.Type.Monsters, size);
                bool nativeControlBlocked = rooms.GetRoom(ControlFloor).WillBeAtCapacity(Team.Type.Monsters, size);
                var before = (Snapshot)Callbacks.Single(row => row.Kind == "Apply").Before;
                if (!nativeFits || !nativeControlBlocked || plays.Any(item => item.RoomIndex == ControlFloor) ||
                    size <= before.Rooms[ExpandedFloor].PlayerMaximum)
                    throw new InvalidOperationException("The opening unit must require capacity beyond the expanded floor's original maximum.");
                Witness = new { CardId = ids[next], CardSize = size, ExpandedFloor, ControlFloor, ControlledAttachment, NativeFits = nativeFits,
                    NativeControlBlocked = nativeControlBlocked, OriginalMaximum = before.Rooms[ExpandedFloor].PlayerMaximum,
                    Decision = new Snapshot(managers) };
            }
            return play;
        }

        internal static bool IsScenarioAction(PlayCardAction action) => ids.Contains(action.CardInstanceId);

        private static bool Observe(RelicEffectBase effect) => Enabled && seeded && artifact != null &&
            effect.GetRelicDataId() == artifact.GetID() &&
            !AllGameManagers.Instance!.GetSaveManager().PreviewMode;

        [HarmonyPatch(typeof(RelicEffectModifyRandomRoomCapacityAfterBattle), nameof(RelicEffectModifyRandomRoomCapacityAfterBattle.TestEffectTurnPhaseTiming))]
        private static class TestPatch
        {
            private static void Prefix(RelicEffectModifyRandomRoomCapacityAfterBattle __instance,
                out OriginalRandomRoomCapacityScenario.Callback? __state)
            {
                __state = null;
                if (!Observe(__instance)) return;
                __state = new OriginalRandomRoomCapacityScenario.Callback { Kind = "Test", Owner = __instance.GetRelicDataId(),
                    Before = new Snapshot(AllGameManagers.Instance!) };
                Callbacks.Add(__state);
            }
            private static void Postfix(bool __result, OriginalRandomRoomCapacityScenario.Callback? __state)
            {
                if (__state == null) return;
                __state.Passed = __result;
                __state.After = new Snapshot(AllGameManagers.Instance!);
                __state.Completed = true;
            }
        }

        [HarmonyPatch(typeof(RelicEffectModifyRandomRoomCapacityAfterBattle), nameof(RelicEffectModifyRandomRoomCapacityAfterBattle.ApplyEffectTurnPhaseTiming))]
        private static class ApplyPatch
        {
            private static void Postfix(RelicEffectModifyRandomRoomCapacityAfterBattle __instance, ref IEnumerator __result)
            {
                if (Observe(__instance)) __result = Wrap(__result, __instance.GetRelicDataId());
            }
        }

        private static IEnumerator Wrap(IEnumerator native, string owner)
        {
            var callback = new OriginalRandomRoomCapacityScenario.Callback { Kind = "Apply", Owner = owner,
                Before = new Snapshot(AllGameManagers.Instance!) };
            Callbacks.Add(callback);
            try
            {
                while (native.MoveNext()) yield return native.Current;
                callback.After = new Snapshot(AllGameManagers.Instance!);
                callback.Completed = true;
            }
            finally { (native as IDisposable)?.Dispose(); }
        }
    }
}
#endif
