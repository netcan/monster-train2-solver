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
    internal static class OriginalShowtimeScenario
    {
        internal const string Name = "original-showtime";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static object? Definition { get; private set; }
        internal static object? CurrentDefinition => original == null ? null : Describe(original);
        internal static object? Witness { get; private set; }
        internal static readonly List<Record> OpeningCallbacks = new();
        internal static readonly List<Record> PostCombat = new();

        private static SoulData? original;
        private static CardUpgradeData? roomUpgrade;
        private static CardUpgradeData? unitUpgrade;
        private static int tier;
        private static int floor;
        private static int[] deploymentFloors = Array.Empty<int>();
        private static int[] ids = Array.Empty<int>();

        internal sealed class Record
        {
            public string Kind { get; set; } = "";
            public bool? Passed { get; set; }
            public bool Completed { get; set; }
            public object Before { get; set; } = null!;
            public object? After { get; set; }
        }

        private sealed class Opening
        {
            public int Turn { get; }
            public int SelectedRoom { get; }
            public UnityRng BattleRng { get; }
            public Floor[] Floors { get; }

            internal Opening(AllGameManagers managers)
            {
                var rooms = managers.GetRoomManager();
                var words = RngCalibration.Words(RandomManager.GetState(RngId.Battle));
                Turn = managers.GetCombatManager().GetTurnCount();
                SelectedRoom = rooms.GetSelectedRoom();
                BattleRng = new UnityRng(words[0], words[1], words[2], words[3]);
                Floors = Enumerable.Range(0, rooms.GetNumRooms()).Select(index => new Floor(rooms.GetRoom(index))).ToArray();
            }
        }

        private sealed class Floor
        {
            public int Index { get; }
            public bool Pyre { get; }
            public int Capacity { get; }
            public int Occupied { get; }
            public object[] Attachments { get; }

            internal Floor(RoomState room)
            {
                var info = room.GetCapacityInfo(Team.Type.Monsters);
                Index = room.GetRoomIndex();
                Pyre = room.GetIsPyreRoom();
                Capacity = info.max;
                Occupied = info.count;
                Attachments = room.Attachments.Select(attachment => new { Type = (int)attachment.AttachType,
                    Side = attachment.TrainSide.ToString(), Active = attachment.IsActive, HasCard = attachment.CardState != null,
                    Upgrades = attachment.UpgradeStates.Select(upgrade => upgrade.Item2.GetID()).ToArray() }).ToArray();
            }
        }

        private static object Describe(SoulData soul)
        {
            var effect = soul.GetEffects().Single();
            var upgrade = effect.GetParamCardUpgradeData();
            var postCombat = upgrade.GetRoomModifierUpgrades().Single(value =>
                value.GetRoomStateModifierClassName() == nameof(RoomStateAddEffectPostCombatModifier));
            var nested = postCombat.GetParamCardEffectData().Single();
            var buff = nested.GetParamCardUpgradeData();
            return new { Id = soul.GetID(), Key = soul.name, Tier = soul.GetTierLevel(), Dlc = soul.GetRequiredDLC().ToString(),
                Type = effect.GetEffectClassName(), Placement = soul.DisallowInDeploymentPhase,
                Count = effect.GetParamInt(), ParamInt2 = effect.GetParamInt2(), Conditions = effect.GetEffectConditions().Count,
                RoomUpgradeId = upgrade.GetID(), RoomUpgradeKey = upgrade.name,
                Modifiers = upgrade.GetRoomModifierUpgrades().Select(value => new { Type = value.GetRoomStateModifierClassName(),
                    Amount = value.GetParamInt(), DisallowedInDeployment = value.DisallowedInDeploymentPhase }).ToArray(),
                PostEffect = new { Type = nested.GetEffectStateName(), Target = nested.GetTargetMode().ToString(),
                    Team = nested.GetTargetTeamType().ToString(), UnitUpgradeId = buff.GetID(), UnitUpgradeKey = buff.name,
                    Damage = buff.GetBonusDamage(), Health = buff.GetBonusHP() } };
        }

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            if (original != null) return;
            var save = managers.GetSaveManager();
            if (save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle ||
                save.GetSelectedSouls().Count != 0 || save.GetMutators().Count != 0 || save.GetCovenants().Count != 0)
                throw new InvalidOperationException("The original Showtime soul needs a clean pre-deployment save.");
            tier = int.Parse(Environment.GetEnvironmentVariable("MT2_PROBE_SHOWTIME_TIER")!);
            string id = tier switch
            {
                1 => "20d57085-8651-49c0-8c66-5530c314ff21",
                2 => "103f4378-9ab0-45dd-9de8-5f4c561a9799",
                3 => "9638b7dd-461e-4810-a029-b7d24cf1e7af",
                _ => throw new InvalidOperationException("Expected Showtime tier 1, 2 or 3.")
            };
            var soul = save.GetAllGameData().FindSoulData(id)!;
            var effect = soul.GetEffects().Single();
            if (soul.name != "ShowtimeT" + tier || soul.GetTierLevel() != tier || soul.DisallowInDeploymentPhase ||
                effect.GetEffectClassName() != nameof(RelicEffectShowtime) || effect.GetParamInt() != 1 ||
                effect.GetEffectConditions().Count != 0)
                throw new InvalidOperationException("The original Showtime soul definition changed.");
            var upgrade = effect.GetParamCardUpgradeData();
            var modifiers = upgrade.GetRoomModifierUpgrades();
            if (modifiers.Count != 2 || modifiers.Single(value => value.GetRoomStateModifierClassName() == nameof(RoomStateCapacityModifier))
                .GetParamInt() != (tier == 3 ? 5 : 3))
                throw new InvalidOperationException("The original Showtime room capacity definition changed.");
            var nested = modifiers.Single(value => value.GetRoomStateModifierClassName() == nameof(RoomStateAddEffectPostCombatModifier))
                .GetParamCardEffectData().Single();
            if (nested.GetEffectStateName() != nameof(CardEffectAddTempCardUpgradeToUnits) ||
                nested.GetTargetMode() != TargetMode.FrontInRoom || nested.GetTargetTeamType() != Team.Type.Monsters)
                throw new InvalidOperationException("The original Showtime post-combat target definition changed.");
            original = soul;
            roomUpgrade = upgrade;
            unitUpgrade = nested.GetParamCardUpgradeData();
            Definition = Describe(soul);
            save.AddSelectedSoul(soul);
            log.LogInfo("ORIGINAL-SHOWTIME-ACQUIRED " + soul.name + " with its installed room and unit upgrades.");
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            var apply = OpeningCallbacks.SingleOrDefault(row => row.Kind == "Apply" && row.Completed);
            var decision = new Opening(managers);
            if (decision.Turn != 0 || apply?.Before is not Opening before || apply.After is not Opening after ||
                before.BattleRng.Equals(after.BattleRng))
                throw new InvalidOperationException("The original Showtime room did not settle before deployment input.");
            var changed = after.Floors.Single(room => room.Capacity != before.Floors[room.Index].Capacity);
            var old = before.Floors[changed.Index];
            if (changed.Pyre || changed.Index != after.SelectedRoom || changed.Capacity - old.Capacity != (tier == 3 ? 5 : 3) ||
                decision.Floors[changed.Index].Capacity != changed.Capacity || changed.Capacity < 8 || old.Capacity >= 8 ||
                !managers.GetRoomManager().GetRoom(changed.Index).Attachments.Any(IsShowtimeAttachment))
                throw new InvalidOperationException("The selected Showtime room lacks its native hidden attachment or extra capacity.");
            floor = changed.Index;
            deploymentFloors = new[] { floor, floor }.Concat(after.Floors.Where(room => !room.Pyre && room.Index != floor)
                .Select(room => room.Index)).ToArray();
            var controls = OriginalRandomRoomCapacityScenario.CreateDeploymentControls(managers, deploymentFloors.Length, 4, secondSize: 2);
            ids = controls.Select(FullBattleTrace.Active!.CardId).ToArray();
            Witness = new { Room = floor, OriginalCapacity = old.Capacity, ExpandedCapacity = changed.Capacity,
                FrontCardId = ids[0], RearCardId = ids[1], FrontSize = controls[0].GetSize(), RearSize = controls[1].GetSize(), Decision = decision };
            Prepared = true;
            OriginalRandomRoomCapacityScenario.RefreshDeploymentControls(managers);
            log.LogInfo($"ORIGINAL-SHOWTIME-PREPARED tier={tier} floor={floor} capacity={old.Capacity}->{changed.Capacity}");
        }

        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            int next = Array.FindIndex(ids, id => state.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            if (next < 0) return null;
            if (state.Spawn.Turn != 0) throw new InvalidOperationException("Showtime controls must deploy on turn zero.");
            int position = next == 1 ? 1 : 0;
            return BattleActionModel.EnumerateSupportedPlays(state, ids[next]).First(action =>
                action.RoomIndex == deploymentFloors[next] && action.PlayerPosition == position);
        }

        internal static bool IsScenarioAction(PlayCardAction action) => ids.Contains(action.CardInstanceId);

        private static bool Observe(RelicEffectShowtime effect) => Enabled && original != null &&
            effect.GetRelicDataId() == original.GetID() && !AllGameManagers.Instance!.GetSaveManager().PreviewMode;

        private static bool IsShowtimeAttachment(TrainRoomAttachmentState attachment) => roomUpgrade != null &&
            attachment.IsActive && attachment.AttachType == TrainRoomAttachmentType.Hidden && attachment.CardState == null &&
            attachment.UpgradeStates.Count == 1 && attachment.UpgradeStates[0].Item2.GetID() == roomUpgrade.GetID();

        private static object Combat(RoomState room)
        {
            var units = new List<CharacterState>();
            room.AddCharactersToList(units, Team.Type.Monsters);
            return new { Turn = AllGameManagers.Instance!.GetCombatManager().GetTurnCount(), Room = room.GetRoomIndex(),
                Actors = units.Where(unit => !unit.IsPyreHeart()).Select(unit => new { Id = FullBattleTrace.Active!.UnitId(unit),
                    CardId = unit.GetSpawnerCard() is CardState card ? FullBattleTrace.Active!.CardId(card) : 0,
                    Size = unit.GetSize(), Attack = unit.GetAttackDamage(), Health = unit.GetHP(), MaxHealth = unit.GetMaxHP(),
                    UpgradeCount = unit.GetAppliedCardUpgrades().Count(upgrade => upgrade.GetCardUpgradeDataId() == unitUpgrade!.GetID()) }).ToArray() };
        }

        [HarmonyPatch(typeof(RelicEffectShowtime), nameof(RelicEffectShowtime.TestEffectTurnPhaseTiming))]
        private static class TestPatch
        {
            private static void Prefix(RelicEffectShowtime __instance, out Record? __state)
            {
                __state = null;
                if (!Observe(__instance)) return;
                __state = new Record { Kind = "Test", Before = new Opening(AllGameManagers.Instance!) };
                OpeningCallbacks.Add(__state);
            }
            private static void Postfix(bool __result, Record? __state)
            {
                if (__state == null) return;
                __state.Passed = __result;
                __state.After = new Opening(AllGameManagers.Instance!);
                __state.Completed = true;
            }
        }

        [HarmonyPatch(typeof(RelicEffectShowtime), nameof(RelicEffectShowtime.ApplyEffectTurnPhaseTiming))]
        private static class ApplyPatch
        {
            private static void Postfix(RelicEffectShowtime __instance, ref IEnumerator __result)
            {
                if (Observe(__instance)) __result = Wrap(__result, OpeningCallbacks, "Apply", () => new Opening(AllGameManagers.Instance!));
            }
        }

        [HarmonyPatch(typeof(RoomStateAddEffectPostCombatModifier), nameof(RoomStateAddEffectPostCombatModifier.PostCombat))]
        private static class PostCombatPatch
        {
            private static void Postfix(RoomStateAddEffectPostCombatModifier __instance, RoomState room, ref IEnumerator __result)
            {
                if (!Enabled || !Prepared || AllGameManagers.Instance!.GetSaveManager().PreviewMode ||
                    !room.Attachments.Any(attachment => IsShowtimeAttachment(attachment) &&
                        attachment.RoomStateModifiers.Any(value => ReferenceEquals(value, __instance)))) return;
                __result = Wrap(__result, PostCombat, "PostCombat", () => Combat(room));
            }
        }

        private static IEnumerator Wrap(IEnumerator native, List<Record> records, string kind, Func<object> capture)
        {
            var row = new Record { Kind = kind, Before = capture() };
            records.Add(row);
            try
            {
                while (native.MoveNext()) yield return native.Current;
                row.After = capture();
                row.Completed = true;
            }
            finally { (native as IDisposable)?.Dispose(); }
        }
    }
}
#endif
