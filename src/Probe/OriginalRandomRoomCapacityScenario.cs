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
    internal static class OriginalRandomRoomCapacityScenario
    {
        internal const string Name = "original-random-room-capacity";
        internal const string OwnerId = "5d9aa853-182a-4cc3-96eb-8189ce7c7d74";
        private const string AssetKey = "SoulSavior_Region2Complete_2";
        private const int Adjustment = -3;
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static object? Definition { get; private set; }
        internal static object? CurrentDefinition => original == null ? null : Describe(original);
        internal static object? Witness { get; private set; }
        internal static readonly List<Callback> Callbacks = new();

        private static MutatorData? original;
        private static int reducedRoom;
        private static int[] rooms = Array.Empty<int>();
        private static CardState[] cards = Array.Empty<CardState>();
        private static int[] ids = Array.Empty<int>();

        internal sealed class Callback
        {
            public string Kind { get; set; } = "";
            public string Owner { get; set; } = OwnerId;
            public bool? Passed { get; set; }
            public bool Completed { get; set; }
            public object Before { get; set; } = null!;
            public object? After { get; set; }
        }

        internal class Snapshot
        {
            public int Turn { get; }
            public bool Placement { get; }
            public bool Preview { get; }
            public int SelectedRoom { get; }
            public UnityRng BattleRng { get; }
            public RoomCapacityState[] Rooms { get; }

            internal Snapshot(AllGameManagers managers)
            {
                var combat = managers.GetCombatManager();
                var rooms = managers.GetRoomManager();
                var words = RngCalibration.Words(RandomManager.GetState(RngId.Battle));
                Turn = combat.GetTurnCount();
                Placement = combat.IsPlacementPhase;
                Preview = managers.GetSaveManager().PreviewMode;
                SelectedRoom = rooms.GetSelectedRoom();
                BattleRng = new UnityRng(words[0], words[1], words[2], words[3]);
                Rooms = RoomCapacityProbe.Capture(rooms);
            }
        }

        private static object Describe(MutatorData owner)
        {
            var effect = owner.GetEffects().Single();
            return new { Id = owner.GetID(), Key = owner.name, Placement = owner.DisallowInDeploymentPhase,
                Type = effect.GetEffectClassName(), Amount = effect.GetParamInt(),
                Conditions = effect.GetEffectConditions().Count };
        }

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            if (original != null) return;
            var save = managers.GetSaveManager();
            if (save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle ||
                save.GetMutators().Count != 0 || save.GetCovenants().Count != 0)
                throw new InvalidOperationException("Random room capacity requires an isolated battle before deployment.");
            var mutator = SpawnStatusScalingMechanismBatch.Owners(save.GetAllGameData()).OfType<MutatorData>()
                .Single(owner => owner.GetID() == OwnerId);
            var effect = mutator.GetEffects().Single();
            if (mutator.name != AssetKey || mutator.DisallowInDeploymentPhase ||
                effect.GetEffectClassName() != nameof(RelicEffectModifyRandomRoomCapacity) ||
                effect.GetParamInt() != Adjustment || effect.GetEffectConditions().Count != 0)
                throw new InvalidOperationException("Original random room capacity mutator payload changed.");
            original = mutator;
            Definition = Describe(mutator);
            save.AddMutator(mutator);
            log.LogInfo("ORIGINAL-RANDOM-ROOM-CAPACITY-ACQUIRED " + AssetKey);
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            var current = new Snapshot(managers);
            var application = Callbacks.SingleOrDefault(row => row.Kind == "Apply" && row.Completed);
            if (original == null || current.Turn != 0 || !current.Placement || current.Preview ||
                application?.Before is not Snapshot before || application.After is not Snapshot after)
                throw new InvalidOperationException("Opening random capacity callback did not settle before deployment input.");

            var reduced = after.Rooms.Single(room => room.PlayerMaximum != before.Rooms[room.RoomIndex].PlayerMaximum);
            if (reduced.IsPyre || reduced.RoomIndex != after.SelectedRoom || before.BattleRng.Equals(after.BattleRng) ||
                before.Rooms[reduced.RoomIndex].PlayerMaximum - reduced.PlayerMaximum != -Adjustment ||
                current.Rooms[reduced.RoomIndex].PlayerMaximum != reduced.PlayerMaximum)
                throw new InvalidOperationException("Original random capacity change did not reach the first deployment input.");
            reducedRoom = reduced.RoomIndex;
            rooms = new[] { reducedRoom }.Concat(after.Rooms.Where(room => !room.IsPyre && room.RoomIndex != reducedRoom)
                .Select(room => room.RoomIndex)).ToArray();
            if (rooms.Length != 3 || reduced.PlayerMaximum is < 1 or > 6)
                throw new InvalidOperationException("Random room capacity needs three deployment floors and a fillable reduced floor.");

            cards = CreateDeploymentControls(managers, rooms.Length, reduced.PlayerMaximum);
            ids = cards.Select(FullBattleTrace.Active!.CardId).ToArray();
            Prepared = true;
            RefreshDeploymentControls(managers);
            log.LogInfo($"ORIGINAL-RANDOM-ROOM-CAPACITY-PREPARED floor={reducedRoom} maximum={reduced.PlayerMaximum}");
        }

        internal static CardState[] CreateDeploymentControls(AllGameManagers managers, int count, int firstSize)
        {
            var save = managers.GetSaveManager();
            var manager = managers.GetCardManager();
            foreach (var card in manager.GetHand().ToArray()) manager.MoveCardFromHandToTopOfDeck(card);
            var steward = save.GetAllGameData().FindCardData(MagneticRelayScenario.StewardId)!;
            var controls = Enumerable.Range(0, count)
                .Select(_ => manager.AddNewCard(steward, CardPile.HandPile, false, false, animate: false)).ToArray();
            for (int i = 0; i < controls.Length; i++)
            {
                var upgrade = new CardUpgradeState();
                upgrade.Setup();
                upgrade.SetAttackDamage(80);
                upgrade.SetAdditionalHP(1000);
                upgrade.SetCostReduction(1);
                if (i == 0) upgrade.SetAdditionalSize(firstSize - controls[i].GetSize());
                controls[i].ApplyPermanentUpgrade(upgrade, save, ignoreUpgradeAnimation: true);
            }
            return controls;
        }

        internal static void RefreshDeploymentControls(AllGameManagers managers)
        {
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(managers.GetCombatManager(), true);
        }

        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            var context = state.Spawn.Train.Context!;
            int next = Array.FindIndex(ids, id => context.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            if (next < 0) return null;
            if (state.Spawn.Turn != 0) throw new InvalidOperationException("Random capacity controls must all deploy on turn zero.");
            var plays = BattleActionModel.EnumerateSupportedPlays(state, ids[next]);
            var play = plays.FirstOrDefault(item => item.RoomIndex == rooms[next]);
            if (play == null) throw new InvalidOperationException("Random capacity control did not fit its intended floor.");
            if (next == 1 && Witness == null)
            {
                var managers = AllGameManagers.Instance!;
                var nativeRooms = managers.GetRoomManager();
                var reduced = nativeRooms.GetRoom(reducedRoom);
                var other = nativeRooms.GetRoom(play.RoomIndex);
                int size = cards[next].GetSize();
                var info = reduced.GetCapacityInfo(Team.Type.Monsters);
                int originalMaximum = ((Snapshot)Callbacks.Single(row => row.Kind == "Apply").Before).Rooms[reducedRoom].PlayerMaximum;
                bool nativeBlocked = reduced.WillBeAtCapacity(Team.Type.Monsters, size);
                bool nativeOtherFits = !other.WillBeAtCapacity(Team.Type.Monsters, size);
                if (info.count != info.max || info.count + size > originalMaximum || !nativeBlocked || !nativeOtherFits ||
                    plays.Any(item => item.RoomIndex == reducedRoom))
                    throw new InvalidOperationException("The reduced floor must reject a control that fits its original capacity and another floor.");
                Witness = new { FirstCardId = ids[0], BlockedCardId = ids[next], CardSize = size,
                    ReducedFloor = reducedRoom, AlternateFloor = play.RoomIndex, OriginalMaximum = originalMaximum,
                    Maximum = info.max, Occupied = info.count, NativeBlocked = nativeBlocked, NativeOtherFits = nativeOtherFits };
            }
            return play;
        }

        internal static bool IsScenarioAction(PlayCardAction action) => ids.Contains(action.CardInstanceId);

        private static bool Observe(RelicEffectBase effect) => Enabled && original != null &&
            effect.GetRelicDataId() == OwnerId && !AllGameManagers.Instance!.GetSaveManager().PreviewMode;

        [HarmonyPatch(typeof(RelicEffectModifyRandomRoomCapacity), nameof(RelicEffectModifyRandomRoomCapacity.TestEffectTurnPhaseTiming))]
        private static class TestPatch
        {
            private static void Prefix(RelicEffectModifyRandomRoomCapacity __instance, out Callback? __state)
            {
                __state = null;
                if (!Observe(__instance)) return;
                __state = new Callback { Kind = "Test", Before = new Snapshot(AllGameManagers.Instance!) };
                Callbacks.Add(__state);
            }
            private static void Postfix(bool __result, Callback? __state)
            {
                if (__state == null) return;
                __state.Passed = __result;
                __state.After = new Snapshot(AllGameManagers.Instance!);
                __state.Completed = true;
            }
        }

        [HarmonyPatch(typeof(RelicEffectModifyRandomRoomCapacity), nameof(RelicEffectModifyRandomRoomCapacity.ApplyEffectTurnPhaseTiming))]
        private static class ApplyPatch
        {
            private static void Postfix(RelicEffectModifyRandomRoomCapacity __instance, ref IEnumerator __result)
            {
                if (Observe(__instance)) __result = Wrap(__result);
            }
        }

        private static IEnumerator Wrap(IEnumerator native)
        {
            var callback = new Callback { Kind = "Apply", Before = new Snapshot(AllGameManagers.Instance!) };
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
