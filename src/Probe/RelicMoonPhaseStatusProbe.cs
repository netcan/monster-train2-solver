using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class RelicMoonPhaseStatusProbe
    {
        internal sealed class Operation
        {
            public string OwnerDataId { get; set; } = "";
            public int EffectIndex { get; set; }
            public string Phase { get; set; } = "";
            public string SourceTeam { get; set; } = "";
            public string TargetMode { get; set; } = "";
            public string? RequiredSubtype { get; set; }
            public bool DoNotAnimateCamera { get; set; }
            public int MoonPhaseMask { get; set; }
            public string[] StatusIds { get; set; } = System.Array.Empty<string>();
            public int[] StatusStacks { get; set; } = System.Array.Empty<int>();
            public int[] TargetIds { get; set; } = System.Array.Empty<int>();
            public TrainCombatState Before { get; set; } = null!;
            public TrainCombatState? After { get; set; }
        }

        internal static readonly List<Operation> Records = new List<Operation>();

        private static IEnumerator Wrap(IEnumerator native, RelicEffectAddStatusEffectOnMoonPhase effect)
        {
            Operation? record = null;
            try
            {
                AllGameManagers managers = AllGameManagers.Instance!;
                RelicState owner = Current(managers).Single(relic =>
                    relic.GetEffects().Any(item => ReferenceEquals(item, effect)));
                int effectIndex = owner.GetEffects().FindIndex(item => ReferenceEquals(item, effect));
                SubtypeData subtype = (SubtypeData)AccessTools.Field(effect.GetType(), "characterSubtype").GetValue(effect);
                StatusEffectStackData[] statuses = effect.GetStatusEffects();
                record = new Operation
                {
                    OwnerDataId = owner.GetRelicDataID(),
                    EffectIndex = effectIndex,
                    Phase = managers.GetPlayerManager().CurrentMoonPhase.ToString(),
                    SourceTeam = AccessTools.Field(effect.GetType(), "targetTeam").GetValue(effect).ToString()!,
                    TargetMode = AccessTools.Field(effect.GetType(), "targetMode").GetValue(effect).ToString()!,
                    RequiredSubtype = subtype == null || subtype.IsNone ? null : subtype.Key,
                    DoNotAnimateCamera = (bool)AccessTools.Field(effect.GetType(), "doNotAnimateCamera").GetValue(effect),
                    MoonPhaseMask = (int)AccessTools.Field(effect.GetType(), "moonPhaseMask").GetValue(effect),
                    StatusIds = statuses.Select(status => status.statusId).ToArray(),
                    StatusStacks = statuses.Select(status => status.count).ToArray(),
                    Before = FullBattleTrace.Active!.CaptureTrain()
                };
                Records.Add(record);
            }
            catch (System.Exception error) { FullBattleTrace.Active?.CaptureFailure(error); }
            try
            {
                while (native.MoveNext()) yield return native.Current;
            }
            finally
            {
                if (record != null)
                {
                    try
                    {
                        var targets = (List<CharacterState>)AccessTools.Field(effect.GetType(), "targets").GetValue(effect);
                        record.TargetIds = targets.Select(FullBattleTrace.Active!.UnitId).ToArray();
                        record.After = FullBattleTrace.Active!.CaptureTrain();
                    }
                    catch (System.Exception error) { FullBattleTrace.Active?.CaptureFailure(error); }
                }
            }
        }

        private static RelicState[] Current(AllGameManagers managers)
        {
            var relics = new List<RelicState>();
            AccessTools.Method(typeof(RelicManager), "GetCurrentRelics")!
                .Invoke(managers.GetRelicManager(), new object[] { relics });
            return relics.ToArray();
        }

        [HarmonyPatch(typeof(RelicEffectAddStatusEffectOnMoonPhase),
            nameof(RelicEffectAddStatusEffectOnMoonPhase.ApplyEffectMoonPhase))]
        private static class AddStatusOnMoonPhasePatch
        {
            private static void Postfix(RelicEffectAddStatusEffectOnMoonPhase __instance, ref IEnumerator __result)
            {
                if (OriginalMoonPhaseStatusRelicScenario.Prepared)
                    __result = Wrap(__result, __instance);
            }
        }
    }
}
