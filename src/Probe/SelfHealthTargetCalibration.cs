using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using ObfuscatedNumber = ShinyShoe.ObfuscatedNumber;

namespace MonsterTrain2Poju.Probe
{
    internal static class SelfHealthTargetCalibration
    {
        internal static object Run(AllGameManagers managers)
        {
            var trace = FullBattleTrace.Active!;
            TrainCombatState original = trace.CaptureTrain(); object originalRng = HalfHealthLifecycleCalibration.Rng();
            var actors = managers.GetMonsterManager().GetCharactersReadOnly().Where(actor => actor.GetHP() > 0).ToArray();
            var primary = AccessTools.Field(typeof(CharacterState), "_primaryStateInformation");
            var rows = new List<object>();
            foreach (CharacterState actor in actors)
            {
                CombatUnit template = original.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == trace.UnitId(actor));
                object state = primary.GetValue(actor); var hp = AccessTools.Field(state.GetType(), "hp"); object saved = hp.GetValue(state);
                try
                {
                    foreach (int value in new[] { 0, actor.GetMaxHP() - 1, actor.GetMaxHP(), actor.GetMaxHP() + 1 })
                    foreach (CardEffectData.HealthFilter health in Enum.GetValues(typeof(CardEffectData.HealthFilter)))
                    foreach (bool testing in new[] { false, true })
                    {
                        hp.SetValue(state, new ObfuscatedNumber(value));
                        // Mismatched team/status and explicit dead/Pyre exclusions
                        // distinguish Self from ordinary room target collection.
                        var filters = new CardTargetFilters(health.ToString(), new[] { "absent" }, new[] { "immune" }, true,
                            "absent", new[] { "" }, TargetFilterProbe.HasSelfHealthFilter);
                        var action = new CardActionEffect("Heal", "Self", 1, true, false, Array.Empty<CombatStatus>(), filters: filters);
                        var request = new TargetHelper.CollectTargetsData { targetMode = TargetMode.Self,
                            targetModeHealthFilter = health, targetTeamType = Team.Type.Heroes, selfTarget = actor,
                            targetModeStatusEffectsFilter = new List<string> { "absent" }, targetModeStatusEffectsExcludedFilter = new List<string> { "immune" },
                            targetSubtype = actor.GetSourceCharacterData().GetSubtypes().FirstOrDefault(), targetIgnoreBosses = true,
                            roomIndex = actor.GetCurrentRoomIndex(), ignoreDead = true, ignorePyre = true, isTesting = testing };
                        TrainCombatState before = trace.CaptureTrain(); object rngBefore = HalfHealthLifecycleCalibration.Rng();
                        var targets = new List<CharacterState>(); TargetHelper.CollectTargets(request, managers.GetCoreManagers(), ref targets);
                        int[] ids = targets.Select(trace.UnitId).ToArray();
                        CombatUnit retained = template.WithHealthAndMaxHealth(value, template.MaxHealth);
                        var modelRoom = new RoomCombatState(actor.GetCurrentRoomIndex(), false, new[] { retained }, Array.Empty<string>());
                        CardTargets model = CardTargetModel.Collect(modelRoom, action, Array.Empty<int>(),
                            selfUnitId: trace.UnitId(actor), isTesting: testing);
                        TrainCombatState after = trace.CaptureTrain(); object rngAfter = HalfHealthLifecycleCalibration.Rng();
                        string? difference = !model.Supported ? model.UnsupportedReason : !model.UnitIds.SequenceEqual(ids) ? "Native Self targets differ." :
                            HalfHealthLifecycleCalibration.Difference(before, after) ?? HalfHealthLifecycleCalibration.Difference(rngBefore, rngAfter);
                        rows.Add(new { ActorId = trace.UnitId(actor), Health = value, Testing = testing, Action = action,
                            Actor = retained, RoomIndex = actor.GetCurrentRoomIndex(), Before = before, Actual = after, Targets = ids,
                            RngBefore = rngBefore, RngAfter = rngAfter, Difference = difference, Completed = true });
                        if (difference != null) throw new InvalidOperationException("Self health target calibration: " + difference);
                    }
                }
                finally { hp.SetValue(state, saved); }
            }
            string? restoration = HalfHealthLifecycleCalibration.Difference(original, trace.CaptureTrain()) ??
                HalfHealthLifecycleCalibration.Difference(originalRng, HalfHealthLifecycleCalibration.Rng());
            if (restoration != null) throw new InvalidOperationException("Self target calibration restoration: " + restoration);
            return new { Completed = true, LiveContextUnchanged = true, RestorationDifference = restoration, Samples = rows };
        }
    }
}
