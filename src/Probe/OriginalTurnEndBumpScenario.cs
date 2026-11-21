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
    internal static class OriginalTurnEndBumpScenario
    {
        internal const string Name = "original-turn-end-bump";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static object? Definition { get; private set; }
        internal static object? CurrentDefinition => original == null ? null : Describe(original);
        internal static object? Witness { get; private set; }
        internal static readonly List<Callback> Callbacks = new();

        private static MutatorData? original;
        private static int[] ids = Array.Empty<int>();
        private static Callback? active;

        internal sealed class Callback
        {
            public string Kind { get; set; } = "";
            public bool? Passed { get; set; }
            public bool Completed { get; set; }
            public object Before { get; set; } = null!;
            public object? After { get; set; }
            public List<Bump> Bumps { get; } = new();
        }

        internal sealed class Bump
        {
            public int Room { get; set; }
            public int Amount { get; set; }
            public int[] Targets { get; set; } = Array.Empty<int>();
            public bool Completed { get; set; }
            public object Before { get; set; } = null!;
            public object? After { get; set; }
        }

        private static object Snapshot()
        {
            var managers = AllGameManagers.Instance!;
            var rooms = managers.GetRoomManager();
            var combat = managers.GetCombatManager();
            var words = RngCalibration.Words(RandomManager.GetState(RngId.Battle));
            var actors = new List<object>();
            for (int index = 0; index < rooms.GetNumRooms(); index++)
            {
                var characters = new List<CharacterState>();
                rooms.GetRoom(index).AddCharactersToList(characters, Team.Type.Monsters | Team.Type.Heroes);
                foreach (CharacterState character in characters)
                    actors.Add(new { Id = FullBattleTrace.Active!.UnitId(character),
                        CardId = character.GetSpawnerCard() is CardState card ? FullBattleTrace.Active!.CardId(card) : 0,
                        Team = character.GetTeamType().ToString(), Room = index,
                        Point = character.GetSpawnPoint()?.GetIndexInRoom(), Alive = character.IsAlive,
                        Miniboss = character.IsMiniboss(), OuterBoss = character.IsOuterTrainBoss(), Pyre = character.IsPyreHeart() });
            }
            return new { Turn = combat.GetTurnCount(), Placement = combat.IsPlacementPhase,
                Preview = managers.GetSaveManager().PreviewMode, SelectedRoom = rooms.GetSelectedRoom(),
                BattleRng = new UnityRng(words[0], words[1], words[2], words[3]), Actors = actors.ToArray() };
        }

        private static object Describe(MutatorData owner)
        {
            var effect = owner.GetEffects().Single();
            return new { Id = owner.GetID(), Key = owner.name, Type = effect.GetEffectClassName(),
                Amount = effect.GetParamInt(), Team = effect.GetParamSourceTeam().ToString(),
                Placement = owner.DisallowInDeploymentPhase, Conditions = effect.GetEffectConditions().Count };
        }

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            if (original != null) return;
            var save = managers.GetSaveManager();
            if (save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle ||
                save.GetMutators().Count != 0 || save.GetCovenants().Count != 0)
                throw new InvalidOperationException("Original turn-end movement requires a fresh isolated first battle.");
            string key = Environment.GetEnvironmentVariable("MT2_PROBE_TURN_END_BUMP")!;
            (string id, int amount) = key switch
            {
                "Gravity" => ("adb3bfb7-2c54-4742-b55e-053fe3217737", -1),
                "RaceYaThere" => ("f843a4a2-1e29-4710-a88a-94b30a651d8e", 1),
                _ => throw new InvalidOperationException("Unknown original turn-end movement mutator.")
            };
            var owner = SpawnStatusScalingMechanismBatch.Owners(save.GetAllGameData()).OfType<MutatorData>()
                .Single(value => value.GetID() == id);
            var effect = owner.GetEffects().Single();
            if (owner.name != key || !owner.DisallowInDeploymentPhase ||
                effect.GetEffectClassName() != nameof(RelicEffectBumpOnTurnEnd) || effect.GetParamInt() != amount ||
                effect.GetParamSourceTeam() != Team.Type.Monsters || effect.GetEffectConditions().Count != 0)
                throw new InvalidOperationException("Original turn-end movement mutator definition changed.");
            original = owner;
            Definition = Describe(owner);
            save.AddMutator(owner);
            log.LogInfo("ORIGINAL-TURN-END-BUMP-ACQUIRED " + key);
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            var save = managers.GetSaveManager();
            if (original == null || !managers.GetCombatManager().IsPlacementPhase ||
                save.GetMutators().Single().GetRelicDataID() != original.GetID() || Callbacks.Count != 0 ||
                managers.GetRoomManager().GetNumRooms() != 4)
                throw new InvalidOperationException("Original turn-end movement must first settle in deployment without a callback.");
            var cards = OriginalRandomRoomCapacityScenario.CreateDeploymentControls(managers, 3, 1);
            ids = cards.Select(FullBattleTrace.Active!.CardId).ToArray();
            Witness = new { CardIds = ids, Floors = new[] { 0, 1, 2 }, Sizes = cards.Select(card => card.GetSize()).ToArray() };
            Prepared = true;
            OriginalRandomRoomCapacityScenario.RefreshDeploymentControls(managers);
            log.LogInfo("ORIGINAL-TURN-END-BUMP-PREPARED three durable deployment controls on separate floors.");
        }

        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            int next = Array.FindIndex(ids, id => state.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            if (next < 0) return null;
            if (state.Spawn.Turn != 0) throw new InvalidOperationException("Turn-end movement controls must deploy on turn zero.");
            return BattleActionModel.EnumerateSupportedPlays(state, ids[next]).First(action => action.RoomIndex == next);
        }

        internal static bool IsScenarioAction(PlayCardAction action) => ids.Contains(action.CardInstanceId);

        private static bool Observe(RelicEffectBumpOnTurnEnd effect) => Enabled && original != null &&
            effect.GetRelicDataId() == original.GetID() && !AllGameManagers.Instance!.GetSaveManager().PreviewMode;

        [HarmonyPatch(typeof(RelicEffectBumpOnTurnEnd), nameof(RelicEffectBumpOnTurnEnd.TestEffectEndOfCombat))]
        private static class TestPatch
        {
            private static void Prefix(RelicEffectBumpOnTurnEnd __instance, out Callback? __state)
            {
                __state = null;
                if (!Observe(__instance)) return;
                __state = new Callback { Kind = "Test", Before = Snapshot() };
                Callbacks.Add(__state);
            }
            private static void Postfix(bool __result, Callback? __state)
            {
                if (__state == null) return;
                __state.Passed = __result;
                __state.After = Snapshot();
                __state.Completed = true;
            }
        }

        [HarmonyPatch(typeof(RelicEffectBumpOnTurnEnd), nameof(RelicEffectBumpOnTurnEnd.ApplyEffectEndOfCombat))]
        private static class ApplyPatch
        {
            private static void Postfix(RelicEffectBumpOnTurnEnd __instance, ref IEnumerator __result)
            {
                if (Observe(__instance)) __result = Wrap(__result);
            }
        }

        private static IEnumerator Wrap(IEnumerator native)
        {
            var row = new Callback { Kind = "Apply", Before = Snapshot() };
            Callbacks.Add(row);
            active = row;
            try
            {
                while (native.MoveNext()) yield return native.Current;
                row.After = Snapshot();
                row.Completed = true;
            }
            finally { active = null; (native as IDisposable)?.Dispose(); }
        }

        [HarmonyPatch(typeof(CardEffectBump), nameof(CardEffectBump.Bump))]
        private static class BumpPatch
        {
            private static void Postfix(CardEffectParams cardEffectParams, int bumpAmount, ref IEnumerator __result)
            {
                if (active is Callback owner && cardEffectParams.sourceRelic?.GetRelicDataID() == original?.GetID())
                    __result = WrapBump(__result, owner, cardEffectParams.targets.ToArray(), bumpAmount);
            }
        }

        private static IEnumerator WrapBump(IEnumerator native, Callback owner, CharacterState[] targets, int amount)
        {
            var row = new Bump { Room = AllGameManagers.Instance!.GetRoomManager().GetSelectedRoom(), Amount = amount,
                Targets = targets.Select(FullBattleTrace.Active!.UnitId).ToArray(), Before = Snapshot() };
            owner.Bumps.Add(row);
            try
            {
                while (native.MoveNext()) yield return native.Current;
                row.After = Snapshot();
                row.Completed = true;
            }
            finally { (native as IDisposable)?.Dispose(); }
        }
    }
}
#endif
