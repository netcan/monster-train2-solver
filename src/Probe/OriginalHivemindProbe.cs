#if DEVELOPMENT_PROBE
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalHivemindProbe
    {
        private const string Status = "hivemind";
        private const string Upgrade = "RoomStateHivemindModifierUpgrade";
        internal static readonly List<Callback> Records = new();

        internal sealed class Callback
        {
            public string Kind { get; set; } = "";
            public int Sequence { get; set; }
            public int Turn { get; set; }
            public int UnitId { get; set; }
            public bool Preview { get; set; }
            public string PrimaryStage { get; set; } = "";
            public string[] AdditionalStages { get; set; } = Array.Empty<string>();
            public object? Before { get; set; }
            public object? After { get; set; }
            public bool Completed { get; set; }
        }

        private static void Observe(string kind, CharacterState? actor, ref IEnumerator native)
        {
            if (FullBattleTrace.Active == null || actor == null || actor.GetTeamType() != Team.Type.Heroes ||
                AllGameManagers.Instance is not AllGameManagers managers || managers.GetCombatManager().GetTurnCount() != 1)
                return;
            native = Capture(native, kind, actor, managers);
        }

        private static IEnumerator Capture(IEnumerator native, string kind, CharacterState actor, AllGameManagers managers)
        {
            var trace = FullBattleTrace.Active!;
            Callback? row = null;
            try
            {
                var rule = managers.GetStatusEffectManager().GetStatusEffectDataById(Status);
                row = new Callback
                {
                    Kind = kind, Sequence = trace.NextPhaseSequence(), Turn = managers.GetCombatManager().GetTurnCount(),
                    UnitId = trace.UnitId(actor), Preview = managers.GetSaveManager().PreviewMode,
                    PrimaryStage = rule.GetPrimaryTriggerStage().ToString(),
                    AdditionalStages = rule.GetAdditionalTriggerStages().Select(stage => stage.ToString()).ToArray(),
                    Before = Snapshot(managers, trace)
                };
                Records.Add(row);
            }
            catch (Exception error) { trace.CaptureFailure(error); }
            try
            {
                while (native.MoveNext()) yield return native.Current;
                if (row != null) row.Completed = true;
            }
            finally
            {
                (native as IDisposable)?.Dispose();
                if (row != null)
                {
                    try { row.After = Snapshot(managers, trace); }
                    catch (Exception error) { trace.CaptureFailure(error); }
                }
            }
        }

        private static object Snapshot(AllGameManagers managers, FullBattleTrace trace)
        {
            var actors = new List<CharacterState>();
            managers.GetHeroManager()!.AddCharactersInTowerToList(actors);
            return new
            {
                Actors = actors.Select(actor => new
                {
                    UnitId = trace.UnitId(actor), Key = actor.GetSourceCharacterData().GetAssetKey(),
                    Room = actor.GetCurrentRoom()?.GetRoomIndex(), actor.IsDeadAndUnrevivable,
                    Attack = actor.GetAttackDamage(), Health = actor.GetHP(), Stacks = actor.GetStatusEffectStacks(Status),
                    Upgrades = actor.GetAppliedCardUpgrades().Where(upgrade => upgrade.GetCardUpgradeDataId() == Upgrade)
                        .Select(upgrade => upgrade.GetAttackDamage()).ToArray()
                }).ToArray(),
                Battle = RngCalibration.Words(RandomManager.GetState(RngId.Battle)),
                BattleTest = RngCalibration.Words(RandomManager.GetState(RngId.BattleTest)),
                CardDraw = RngCalibration.Words(RandomManager.GetState(RngId.CardDraw))
            };
        }

        [HarmonyPatch(typeof(StatusEffectHivemindState), "OnTriggered")]
        private static class TurnStartPatch
        {
            private static bool Prepare() => OriginalUnrulyWitness.IndulgentTitanskin;

            private static void Postfix(StatusEffectState.InputTriggerParams inputTriggerParams, ref IEnumerator __result) =>
                Observe("TurnStart", inputTriggerParams.associatedCharacter, ref __result);
        }

        [HarmonyPatch(typeof(RoomStateHivemindModifier), nameof(RoomStateHivemindModifier.OnRoomModifierOwnerKilled))]
        private static class OwnerKilledPatch
        {
            private static bool Prepare() => OriginalUnrulyWitness.IndulgentTitanskin;

            private static void Postfix(CharacterState ownerCharacterState, ref IEnumerator __result) =>
                Observe("OwnerKilled", ownerCharacterState, ref __result);
        }

        [HarmonyPatch(typeof(RoomStateHivemindModifier), nameof(RoomStateHivemindModifier.ApplyCharacterTriggerModification))]
        private static class OwnerStatusChangedPatch
        {
            private static bool Prepare() => OriginalUnrulyWitness.IndulgentTitanskin;

            private static void Postfix(RoomStateHivemindModifier __instance, CharacterTriggerData.Trigger trigger,
                CharacterState affectedCharacter, CharacterState.FireTriggersData? fireTriggersData, ref IEnumerator __result)
            {
                if (trigger != CharacterTriggerData.Trigger.OnStatusEffectChanged || fireTriggersData?.paramString != Status ||
                    !affectedCharacter.GetRoomStateModifiers().Any(modifier => ReferenceEquals(modifier, __instance)))
                    return;
                Observe("OwnerStatusChanged", affectedCharacter, ref __result);
            }
        }
    }
}
#endif
