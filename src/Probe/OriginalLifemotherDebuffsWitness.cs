#if DEVELOPMENT_PROBE
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalLifemotherDebuffsWitness
    {
        internal const string Variant = "LifemotherDebuffs";
        internal const string Scenario = "SoulSavior_R5_BossBattle_Lifemother_Debuffs";
        internal const string Boss = "Boss_SoulSavior_Final_Lifemother_Debuffer";
        private const string ScenarioId = "9928005f-f7da-42c7-887a-b26b38ae8bcb";
        private const string BossId = "9dc77d80-0ab2-471e-af73-135080c7d4a6";
        private const string OwnerId = "93efdaa9-7496-46d3-ae86-7a08223b8f94";
        private const string Champion = "19705414-816d-4549-bcf5-866f338a551f";
        private const string Avenger = "69fcbe85-a5b7-4a83-bf6d-21888604d99e";
        private static readonly List<Application> Applications = new();
        internal static bool Enabled => OriginalUnrulyWitness.FullBattle &&
            Environment.GetEnvironmentVariable("MT2_PROBE_UNRULY_VARIANT") == Variant;
        internal static bool Complete => Applications.Any(row => row.Kind == "OnAttacking" && row.PositiveMalaise) &&
            Applications.Any(row => row.Kind == "OnAnyMonsterDeathOnFloor" && row.PositiveMalaise);

        internal static bool Supports(ScenarioData scenario) => scenario.GetID() == ScenarioId &&
            Supports(scenario.GetOuterTrainBoss(false)) && Supports(scenario.GetOuterTrainBoss(true)) &&
            scenario.GetEnemyRelicData() is [var owner] && owner.GetID() == OwnerId &&
            owner.name == "Boss_Railforged_Lifemother_Debuff" &&
            owner.GetEffects().All(effect => effect.GetEffectClassName() == nameof(RelicEffectNull)) &&
            owner.GetEffects().Select(effect => effect.GetParamInt()).SequenceEqual(new[] { 12, 10, 15, 18 });

        private static bool Supports(CharacterData? data) => data != null && data.GetAssetKey() == Boss && data.GetID() == BossId &&
            data.GetAttackDamage() == 50 && data.GetHealth() == 9000 && data.GetSize() == 5 &&
            data.GetStartingStatusEffects() is [{ statusId: "trample", count: 1 }] && data.GetRoomModifiersData().Count == 0 &&
            data.GetBossActionData() is [var group] && group.GetNumRepeats() == 0 && group.GetActions() is [var action] &&
            action.GetActionBehaviorName() == "RandomOccupiedBossActionBehavior" && action.GetParamTeamType() == Team.Type.Monsters &&
            action.GetActionEffectData().Count == 0 && data.GetTriggers() is [var attack, var death, var turn] &&
            Matches(attack, CharacterTriggerData.Trigger.OnAttacking) &&
            Matches(death, CharacterTriggerData.Trigger.OnAnyMonsterDeathOnFloor) &&
            Matches(turn, CharacterTriggerData.Trigger.OnTeamTurnBegin);

        private static bool Matches(CharacterTriggerData trigger, CharacterTriggerData.Trigger kind) =>
            trigger.GetTrigger() == kind && !trigger.GetTriggerOnce() && !trigger.GetHideVisualAndIgnoreSilence() &&
            !trigger.GetOnlyTriggerIfEquipped() && trigger.GetTriggerAtThreshold() == 0 &&
            trigger.GetRequiredStatusEffects().Count == 0 && trigger.GetRequiredStatusEffectsForDyingCharacter().Count == 0 &&
            trigger.GetEffects() is [var effect] && Matches(effect, kind);

        private static bool Matches(CardEffectData effect, CharacterTriggerData.Trigger kind) => !effect.GetUseIntRange() && kind switch
        {
            CharacterTriggerData.Trigger.OnAttacking => effect.GetEffectStateName() == nameof(CardEffectAddStatusEffect) &&
                effect.GetTargetMode() == TargetMode.LastAttackedCharacter && effect.GetTargetTeamType() == Team.Type.Monsters &&
                effect.GetParamInt() == 0 && effect.GetParamStatusEffects() is [{ statusId: "malaise", count: 12 }],
            CharacterTriggerData.Trigger.OnAnyMonsterDeathOnFloor =>
                effect.GetEffectStateName() == nameof(CardEffectTransferAllStatusEffectsFromUnit) &&
                effect.GetTargetMode() == TargetMode.DyingCharacter && effect.GetTargetTeamType() == (Team.Type.Heroes | Team.Type.Monsters) &&
                effect.GetParamInt() == (int)CardEffectTransferAllStatusEffectsFromUnit.TransferTarget.FrontFriendlyUnit &&
                effect.GetAdditionalParamInt() == (int)CardEffectTransferAllStatusEffectsFromUnit.AllowedStatusEffectType.DebuffsOnly &&
                effect.GetAdditionalParamInt1() == (int)CardEffectTransferAllStatusEffectsFromUnit.TransferRoomType.SourceRoomOnly &&
                effect.GetParamFloat() == 0 && !effect.GetParamBool() && !effect.GetParamBool2() &&
                effect.GetParamStatusEffects().Length == 0 && effect.GetTargetCharacterSubtype().IsNone &&
                !effect.GetTargetIgnoreBosses(),
            CharacterTriggerData.Trigger.OnTeamTurnBegin => effect.GetEffectStateName() == nameof(CardEffectRemoveStatusEffect) &&
                effect.GetTargetMode() == TargetMode.Self && effect.GetTargetTeamType() == (Team.Type.Heroes | Team.Type.Monsters) &&
                effect.GetParamInt() == 5 && effect.GetParamStatusEffects() is [{ statusId: "dazed", count: 5 }],
            _ => false
        };

        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            if (state.Spawn.Turn != 0) return null;
            var floor = state.Spawn.Train.Rooms.Single(room => room.RoomIndex == 2);
            string key = floor.Units.Any(unit => unit.AssetKey == "MonsterBanishedChampion1") ? Avenger : Champion;
            if (floor.Units.Any(unit => unit.AssetKey == "MonsterAvenger")) return null;
            CardToken card = state.Spawn.Train.Context!.Cards.Hand.Single(item => item.DataId == key);
            return new PlayCardAction(card.InstanceId, floor.RoomIndex, floor.Units.Count(unit => unit.Team == CombatTeam.Player));
        }

        internal static void Write()
        {
            using var archive = NativeFixtureCapture.Capture(new
            {
                Schema = 1, Boundary = "OriginalLifemotherDebuffs", GameVersion = UnityEngine.Application.version,
                GameModuleMvid = typeof(CardState).Module.ModuleVersionId,
                Scenario, ScenarioId, Boss, BossId, OwnerId, Complete, Applications
            });
            using var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "lifemother-debuffs.mt2f"));
            archive.Write(stream);
        }

        private sealed class Frame(RoomCombatState room, CombatUnit boss, CombatUnit[] targets, CombatUnit[] friendlyRoster,
            uint[] battle, uint[] battleTest)
        {
            public RoomCombatState Room { get; } = room;
            public CombatUnit Boss { get; } = boss;
            public CombatUnit[] Targets { get; } = targets;
            public CombatUnit[] FriendlyRoster { get; } = friendlyRoster;
            public uint[] Battle { get; } = battle;
            public uint[] BattleTest { get; } = battleTest;
        }

        private sealed class Application(int sequence, int turn, string kind, int sourceRoom, int selectedRoom,
            CardActionEffect effect, Frame before, Frame actual, bool positiveMalaise)
        {
            public int Sequence { get; } = sequence;
            public int Turn { get; } = turn;
            public string Kind { get; } = kind;
            public int SourceRoom { get; } = sourceRoom;
            public int SelectedRoom { get; } = selectedRoom;
            public CardActionEffect Effect { get; } = effect;
            public Frame Before { get; } = before;
            public Frame Actual { get; } = actual;
            public bool PositiveMalaise { get; } = positiveMalaise;
        }

        [HarmonyPatch]
        private static class Effects
        {
            private static bool Prepare() => Enabled;

            private static IEnumerable<MethodBase> TargetMethods()
            {
                yield return AccessTools.DeclaredMethod(typeof(CardEffectAddStatusEffect), nameof(CardEffectAddStatusEffect.ApplyEffect));
                yield return AccessTools.DeclaredMethod(typeof(CardEffectTransferAllStatusEffectsFromUnit), nameof(CardEffectTransferAllStatusEffectsFromUnit.ApplyEffect));
                yield return AccessTools.DeclaredMethod(typeof(CardEffectRemoveStatusEffect), nameof(CardEffectRemoveStatusEffect.ApplyEffect));
            }

            private static void Postfix(CardEffectState cardEffectState, CardEffectParams cardEffectParams, ref IEnumerator __result)
            {
                if (FullBattleTrace.Active != null && !AllGameManagers.Instance!.GetSaveManager().PreviewMode &&
                    cardEffectParams.selfTarget?.GetSourceCharacterData().GetAssetKey() == Boss &&
                    cardEffectParams.sourceCharacterTriggerState?.GetTrigger() is { } kind &&
                    Matches(cardEffectState.GetSourceCardEffectData(), kind))
                    __result = Observe(__result, cardEffectState, cardEffectParams, kind);
            }
        }

        private static IEnumerator Observe(IEnumerator native, CardEffectState effect, CardEffectParams parameters,
            CharacterTriggerData.Trigger kind)
        {
            FullBattleTrace trace = FullBattleTrace.Active!;
            AllGameManagers managers = AllGameManagers.Instance!;
            CharacterState boss = parameters.selfTarget!;
            CharacterState[] targets = parameters.targets.ToArray();
            CharacterState source = kind == CharacterTriggerData.Trigger.OnAnyMonsterDeathOnFloor && targets.Length > 0 ? targets[0] : boss;
            RoomState? sourceRoom = source.GetCurrentRoom();
            RoomState room = sourceRoom ?? parameters.GetSelectedRoom(managers.GetRoomManager());
            var friendly = new List<CharacterState>();
            room.AddCharactersToList(friendly, Team.Type.Monsters);
            CharacterState[] roster = friendly.ToArray();
            Frame Capture() => new(trace.Capture(room), trace.CaptureUnit(boss), targets.Select(unit => trace.CaptureUnit(unit)).ToArray(),
                roster.Select(unit => trace.CaptureUnit(unit)).ToArray(),
                RngCalibration.Words(RandomManager.GetState(RngId.Battle)), RngCalibration.Words(RandomManager.GetState(RngId.BattleTest)));
            int sequence = trace.NextPhaseSequence();
            Frame before = Capture();
            try { while (native.MoveNext()) yield return native.Current; }
            finally { (native as IDisposable)?.Dispose(); }
            Frame actual = Capture();
            bool positive = false;
            if (before.Targets is [var prior] && actual.Targets is [var after])
            {
                int delta = (after.Status("malaise")?.Stacks ?? 0) - (prior.Status("malaise")?.Stacks ?? 0);
                positive = kind == CharacterTriggerData.Trigger.OnAttacking && delta == 12;
                if (kind == CharacterTriggerData.Trigger.OnAnyMonsterDeathOnFloor && prior.Team == CombatTeam.Player &&
                    delta < 0 && before.FriendlyRoster.FirstOrDefault(unit => unit.Health > 0 && unit.Status("untouchable") == null) is { } receiver &&
                    receiver.Id != prior.Id && actual.FriendlyRoster.Single(unit => unit.Id == receiver.Id).Status("malaise")?.Stacks ==
                    (receiver.Status("malaise")?.Stacks ?? 0) - delta)
                    positive = true;
            }
            Applications.Add(new Application(sequence, managers.GetCombatManager().GetTurnCount(), kind.ToString(),
                sourceRoom?.GetRoomIndex() ?? -1, parameters.selectedRoom, UnitTriggerActionProbe.Capture(effect)!, before, actual, positive));
        }
    }
}
#endif
