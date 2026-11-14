using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    // Actual API coroutines on the original five bodies, from their unmodified
    // birth. The isolated worker exits after this dependent, nonterminal chain.
    internal static class TrueFinalBossMaximumHealthCalibration
    {
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_TRUE_FINAL_BOSS_MAXIMUM_HEALTH") == "1";
        private sealed class Input
        {
            internal string Label = "";
            internal int Target, Amount, Floor = 1;
            internal bool Debuff, DecreaseHp = true, Heal = true;
        }
        internal static IEnumerator Capture(IReadOnlyList<CharacterState> actors)
        {
            AllGameManagers managers = AllGameManagers.Instance!;
            FullBattleTrace trace = FullBattleTrace.Active!;
            CombatManager combat = managers.GetCombatManager();
            if (actors.Count != 5 || actors.Any(actor => actor.PreviewMode || actor.TemporaryPreviewsEnabled || !actor.HasStatusEffect("lifelink")))
                throw new InvalidOperationException("Maximum-health controls require five original live linked bodies.");
            var queue = (Queue<CombatManager.TriggerQueueData>)AccessTools.Property(typeof(CombatManager), "TriggerQueue").GetValue(combat);
            object Rng() => new { Streams = Enum.GetValues(typeof(RngId)).Cast<RngId>().Select(id =>
                new { Id = id.ToString(), Words = RngCalibration.Words(RandomManager.GetState(id)) }).ToArray(), Unity = RngCalibration.Words(UnityEngine.Random.state) };
            var inputs = new List<Input>();
            void Add(string label, int target, int amount, bool debuff = true, int floor = 1, bool decrease = true, bool heal = true) =>
                inputs.Add(new Input { Label = label, Target = target, Amount = amount, Debuff = debuff, Floor = floor, DecreaseHp = decrease, Heal = heal });
            for (int target = 0; target < 5; target++)
            {
                Add("grow-unhealed", target, 4, debuff: false, heal: false);
                Add("zero", target, 0); Add("negative", target, -3);
                Add("subtract", target, 3); Add("clip-only", target, 2, floor: 0, decrease: false);
                Add("protected-lethal", target, int.MaxValue, floor: 0);
                Add("protected-one", target, 5, floor: 0);
                Add("grow-one-unhealed", target, 9, debuff: false, heal: false);
                Add("hp-only-protection", target, 2, floor: 0);
                Add("grow-and-heal", target, 5, debuff: false);
                Add("clip-wounded", target, 4, decrease: false);
                Add("raising-floor", target, 2, floor: 20);
                Add("recover-group", target, 16, debuff: false);
            }
            Add("reduce-dominion", 0, 19); Add("reduce-savagery", 1, 19);
            // Bring both independent groups to exactly one, then show that the
            // other two Entropy bodies do not count as independent protectors.
            Add("one-dominion", 0, int.MaxValue, floor: 0);
            Add("one-savagery", 1, int.MaxValue, floor: 0);
            Add("unprotected-subtract", 3, 1);
            Add("unprotected-one", 4, 18);
            Add("unprotected-clip-floor", 2, int.MaxValue, decrease: false);
            Add("unprotected-grow-one", 4, 9, debuff: false, heal: false);
            Add("unprotected-clip-only", 3, 9, decrease: false);
            Add("restore-protector", 0, 2, debuff: false);
            Add("protected-again", 4, int.MaxValue, floor: 0);
            Add("upper-floor", 4, 1, floor: 99999, decrease: false);
            Add("zero-buff", 4, 0, debuff: false); Add("negative-buff", 4, -3, debuff: false);
            var rows = new List<object>(); var events = new List<HealthChangedEvent>(); bool recording = false;
            foreach (CharacterState actor in actors)
                actor.AddHpChangedListener((changed, hp) => { if (recording) events.Add(new HealthChangedEvent(trace.UnitId(changed), hp)); });
            foreach (Input input in inputs)
            {
                CharacterState target = actors[input.Target]; RoomState room = target.GetCurrentRoom();
                if (queue.Count != 0) throw new InvalidOperationException("Maximum-health control entered a pending native queue.");
                RoomCombatState before = trace.Capture(room); object rngBefore = Rng();
                bool protection = StatusEffectLifeLinkState.ProtectedByLifeLink(target, managers.GetHeroManager(), managers.GetMonsterManager(), combat);
                events.Clear(); recording = true;
                try
                {
                    if (input.Debuff) yield return target.DebuffMaxHP(input.Amount, input.Floor, input.DecreaseHp);
                    else yield return target.BuffMaxHP(input.Amount, triggerOnHeal: false, heal: input.Heal);
                }
                finally { recording = false; }
                if (actors.Any(actor => !actor.IsAlive || actor.IsDestroyed) || queue.Count != 0)
                    throw new InvalidOperationException("Maximum-health control unexpectedly became terminal or queued a callback: " + input.Label);
                rows.Add(new { Index = rows.Count, input.Label, TargetId = trace.UnitId(target), input.Amount, input.Debuff, input.Floor,
                    input.DecreaseHp, input.Heal, Protected = protection, Before = before, Actual = trace.Capture(room), Events = events.ToArray(),
                    RngBefore = rngBefore, RngAfter = Rng(), AllBossesAlive = true, NativeQueueEmpty = true });
            }
            using (var document = NativeFixtureCapture.Capture(new { Schema = 1,
                GameModuleMvid = typeof(CharacterState).Assembly.ManifestModule.ModuleVersionId,
                Boundary = "OriginalLiveLifeLinkMaximumHealthCoroutines", OriginalActorsAndStartingStats = true,
                ControllerExcluded = true, NativeCompleted = true, Rows = rows }))
            using (var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!,
                "true-final-boss-maximum-health.mt2f"))) document.Write(output);
        }
    }
}
