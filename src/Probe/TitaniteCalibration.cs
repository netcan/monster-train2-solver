using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MonsterTrain2Poju.Capture;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class TitaniteCalibration
    {
        internal static void Capture(FullBattleTrace trace)
        {
            if (Environment.GetEnvironmentVariable("MT2_PROBE_TITANITE") != "1") return;
            var managers = AllGameManagers.Instance!;
            var save = managers.GetSaveManager();
            var core = managers.GetCoreManagers();
            var initial = JToken.FromObject(trace.CaptureDecision());
            int frame = UnityEngine.Time.frameCount;
            JObject Rng()
            {
                var result = new JObject();
                foreach (RngId id in Enum.GetValues(typeof(RngId)))
                    result[id.ToString()] = JToken.FromObject(RngCalibration.Words(RandomManager.GetState(id)));
                result["Unity"] = JToken.FromObject(UnityEngine.Random.state);
                return result;
            }
            var rng = Rng();
            var target = trace.KnownUnits.First(unit => unit.IsAlive && !unit.IsDestroyed);
            var definition = save.GetAllGameData().FindCardData("fb0683b1-20ac-4dd2-b580-14b502e182f4")!;
            var state = new StatusEffectDamageReductionState();
            var rows = new List<object>();
            foreach (bool piercing in new[] { false, true })
            {
                var card = new CardState(); card.Setup(definition, save);
                if (piercing)
                {
                    var trait = new CardTraitData(); trait.Setup("CardTraitIgnoreArmor"); card.AddTrait(trait);
                }
                foreach (int damage in new[] { -1, 0, 1, 2, 3, 4, 5, 255, 9999, 16777217, 16777219, int.MaxValue })
                {
                    var input = new StatusEffectState.InputTriggerParams { attacked = target, damage = damage, damageSourceCard = card };
                    var output = new StatusEffectState.OutputTriggerParams { damage = damage };
                    bool triggered = state.TestTrigger(input, output, core);
                    rows.Add(new { Damage = damage, Piercing = piercing, Triggered = triggered, Actual = output.damage });
                }
            }
            var data = managers.GetStatusEffectManager().GetAllStatusEffectsData().GetStatusEffectData()
                .Single(item => item.GetStatusId() == "titanite");
            bool unchanged = frame == UnityEngine.Time.frameCount && JToken.DeepEquals(rng, Rng()) &&
                JToken.DeepEquals(initial, JToken.FromObject(trace.CaptureDecision()));
            if (!unchanged) throw new InvalidOperationException("Titanite calibration changed live decision/RNG/frame.");
            using (var fixture = NativeFixtureCapture.Capture(new { Schema = 1,
                GameModuleMvid = typeof(CardState).Assembly.ManifestModule.ModuleVersionId,
                ContextUnchanged = unchanged, FrameBefore = frame, FrameAfter = UnityEngine.Time.frameCount,
                RngBefore = rng, RngAfter = Rng(), Stage = data.GetPrimaryTriggerStage().ToString(), Rows = rows }))
            using (var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "titanite-calibration.mt2f")))
                fixture.Write(stream);
        }
    }
}
