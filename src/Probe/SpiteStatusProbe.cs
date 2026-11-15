using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using ObfuscatedNumber = ShinyShoe.ObfuscatedNumber;

namespace MonsterTrain2Poju.Probe
{
    internal static class SpiteStatusProbe
    {
        internal sealed class Sample
        {
            public bool Preview { get; set; }
            public CombatTeam? Attacker { get; set; }
            public CombatTeam? Target { get; set; }
            public int Health { get; set; }
            public int Maximum { get; set; }
            public int Stacks { get; set; }
            public bool TargetAlive { get; set; }
            public int Damage { get; set; }
            public int DamageType { get; set; }
            public int ActualDamage { get; set; }
            public bool NativeTest { get; set; }
        }
        internal static readonly List<Sample> Live = new List<Sample>();
        private static bool calibrating;
        private static Sample Capture(StatusEffectState.InputTriggerParams input, StatusEffectState.OutputTriggerParams output, bool result)
        {
            var a = input.attacker; var t = input.attacked;
            CombatTeam Team(CharacterState actor) => actor.GetTeamType() == global::Team.Type.Monsters ? CombatTeam.Player : CombatTeam.Enemy;
            return new Sample { Preview = AllGameManagers.Instance!.GetSaveManager().PreviewMode,
                Attacker = a == null ? null : Team(a), Target = t == null ? null : Team(t),
                Health = a?.GetHP() ?? 0, Maximum = a?.GetMaxHP() ?? 0, Stacks = a?.GetStatusEffectStacks("spite") ?? 0,
                TargetAlive = t?.IsAlive ?? false, Damage = input.damage, DamageType = (int)input.damageType,
                ActualDamage = output.damage, NativeTest = result };
        }
        internal static object Run(AllGameManagers managers)
        {
            var trace = FullBattleTrace.Active!;
            var player = managers.GetMonsterManager().GetCharactersReadOnly().First(unit => unit.IsAlive && !unit.IsPyreHeart());
            var enemy = managers.GetHeroManager().GetCharactersReadOnly().First(unit => unit.IsAlive);
            var actors = new[] { player, enemy };
            var field = AccessTools.Field(typeof(CharacterState), "_primaryStateInformation");
            var original = actors.Select(field.GetValue).ToArray();
            var before = trace.CaptureTrain(); var rng = HalfHealthLifecycleCalibration.Rng(); var rows = new List<Sample>();
            calibrating = true;
            try
            {
                foreach (int who in new[] { 0, 1 })
                foreach (int hp in new[] { 0, 9, 10, 11, 20 })
                foreach (int stacks in new[] { 0, 1, 2 })
                foreach (int damage in new[] { -1, 0, 3 }) Test(who, 1 - who, hp, 20, stacks, damage, 0);
                foreach (int attacker in new[] { -1, 0, 1 })
                foreach (int target in new[] { -1, 0, 1 }) Test(attacker, target, 10, 20, 1, 3, 0);
                foreach (int maximum in new[] { 19, 21 })
                foreach (int hp in new[] { 9, 10, 11 }) Test(0, 1, hp, maximum, 1, 3, 0);
                foreach (int type in Enumerable.Range(0, 13)) Test(0, 1, 10, 20, 1, 3, type);
                foreach (int stacks in new[] { 0, 1, 2, int.MaxValue }) Test(0, 1, 10, 20, stacks, int.MaxValue, 0);
                Test(0, 1, 10, 20, 1, 3, 0, targetDead: true);
            }
            finally
            {
                for (int i = 0; i < actors.Length; i++) field.SetValue(actors[i], original[i]);
                calibrating = false;
            }
            var after = trace.CaptureTrain(); var rngAfter = HalfHealthLifecycleCalibration.Rng();
            string? difference = HalfHealthLifecycleCalibration.Difference(before, after) ?? HalfHealthLifecycleCalibration.Difference(rng, rngAfter);
            if (difference != null) throw new InvalidOperationException("Spite calibration restoration: " + difference);
            return new { Completed = true, LiveContextUnchanged = true, Before = before, Actual = after, RngBefore = rng, RngAfter = rngAfter, Tests = rows };

            void Test(int attacker, int target, int hp, int maximum, int stacks, int damage, int type, bool targetDead = false)
            {
                for (int i = 0; i < actors.Length; i++)
                {
                    var shadow = AccessTools.Method(original[i].GetType(), "DeepCopyForPreview").Invoke(original[i], null);
                    field.SetValue(actors[i], shadow);
                    AccessTools.Field(shadow.GetType(), "hp").SetValue(shadow, new ObfuscatedNumber(i == attacker ? hp : targetDead && i == target ? 0 : 10));
                    AccessTools.Field(shadow.GetType(), "maxHp").SetValue(shadow, new ObfuscatedNumber(i == attacker ? maximum : 20));
                    var status = managers.GetStatusEffectManager().Create("spite", actors[i])!;
                    AccessTools.Field(shadow.GetType(), "statusEffects").SetValue(shadow, new Dictionary<string, CharacterState.StatusEffectStack> {
                        ["spite"] = new CharacterState.StatusEffectStack(status, i == attacker ? stacks : 0) });
                }
                var instance = managers.GetStatusEffectManager().Create("spite", player)!;
                var input = new StatusEffectState.InputTriggerParams { attacker = attacker < 0 ? null! : actors[attacker],
                    attacked = target < 0 ? null! : actors[target], damage = damage, damageType = (Damage.Type)type };
                var output = new StatusEffectState.OutputTriggerParams { damage = int.MinValue };
                bool result = instance.TestTrigger(input, output, managers.GetCoreManagers());
                var row = Capture(input, output, result); rows.Add(row);
                int expected = SpiteStatusModel.Adjust(row.Attacker, row.Health, row.Maximum, row.Stacks, row.Target, row.TargetAlive, damage);
                if (expected != row.ActualDamage || (expected != damage) != result)
                    throw new InvalidOperationException("Native Spite status query differs.");
            }
        }
        [HarmonyPatch]
        private static class TestPatch
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                var type = AccessTools.TypeByName("StatusEffectSpiteState");
                if (type != null) yield return AccessTools.Method(type, "TestTrigger");
            }
            private static void Postfix(StatusEffectState.InputTriggerParams inputTriggerParams,
                StatusEffectState.OutputTriggerParams outputTriggerParams, bool __result)
            {
                if (!calibrating && OriginalSpiteEquipmentScenario.Prepared)
                    Live.Add(Capture(inputTriggerParams, outputTriggerParams, __result));
            }
        }
    }
}
