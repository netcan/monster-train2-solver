using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class CardModifierOverflowCalibration
    {
        internal static void Capture(FullBattleTrace trace)
        {
            if (Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIER_OVERFLOW") != "1") return;
            JObject Rng()
            {
                var result = new JObject();
                foreach (RngId id in Enum.GetValues(typeof(RngId)))
                    if (id != RngId.Chatter && id != RngId.NonDeterministic) result[id.ToString()] = JToken.FromObject(RngCalibration.Words(RandomManager.GetState(id)));
                return result;
            }
            var before = JToken.FromObject(trace.CaptureContext());
            var rngBefore = Rng(); int frame = UnityEngine.Time.frameCount;
            var samples = new List<object>();
            int[][] sequences = { new[] { int.MaxValue, 1 }, new[] { int.MinValue, 1 }, new[] { int.MinValue, 99 },
                new[] { int.MinValue, -1 }, new[] { int.MaxValue, int.MaxValue }, new[] { int.MaxValue, -int.MaxValue },
                new[] { int.MinValue, int.MinValue }, new[] { -99, 99, 2 }, new[] { -98, 98, 2 }, new[] { 0, 99, -99 } };
            string[] fields = { "additionalDamage", "additionalMaxHP", "additionalCost", "additionalHeal", "additionalSize",
                "additionalXCost", "additionalEquipmentLimit", "additionalUpgradeSlotCount" };
            foreach (CardStateModifiers.StatType stat in Enum.GetValues(typeof(CardStateModifiers.StatType)))
            foreach (bool enforceFloor in new[] { false, true })
            foreach (int basis in new[] { -5, 0, 3, 100, int.MaxValue - 1, int.MaxValue, int.MinValue })
            foreach (int[] values in sequences)
            foreach (bool firstOffset in new[] { false, true })
            {
                var native = new[] { new CardStateModifiers(), new CardStateModifiers() };
                var projected = new CardModifiers[2];
                for (int group = 0; group < 2; group++)
                {
                    int offset = group == 0 && firstOffset ? values[0] : 0;
                    AccessTools.Field(typeof(CardStateModifiers), fields[(int)stat]).SetValue(native[group], offset);
                    int[] additions = group == 0 ? firstOffset ? Array.Empty<int>() : new[] { values[0] } : values.Skip(1).ToArray();
                    foreach (int value in additions)
                    {
                        var upgrade = new CardUpgradeState(); upgrade.Setup();
                        string field = new[] { "attackDamage", "additionalHP", "costReduction", "additionalHeal", "additionalSize",
                            "xCostReduction", "additionalEquipmentLimit", "additionalUpgradeSlotCount" }[(int)stat];
                        AccessTools.Field(typeof(CardUpgradeState), field).SetValue(upgrade, stat == CardStateModifiers.StatType.Cost ? unchecked(-value) : value);
                        native[group].GetCardUpgrades().Add(upgrade);
                    }
                    CardStatModifier Stats(int value) => new CardStatModifier(stat == CardStateModifiers.StatType.Damage ? value : 0,
                        stat == CardStateModifiers.StatType.HP ? value : 0, stat == CardStateModifiers.StatType.Cost ? value : 0,
                        stat == CardStateModifiers.StatType.Heal ? value : 0, stat == CardStateModifiers.StatType.Size ? value : 0,
                        stat == CardStateModifiers.StatType.XCost ? value : 0, stat == CardStateModifiers.StatType.EquipmentLimit ? value : 0,
                        stat == CardStateModifiers.StatType.UpgradeSlotCount ? value : 0);
                    projected[group] = new CardModifiers(Stats(offset), additions.Select(value => new CardUpgradeModifier("", "", Stats(value),
                        Array.Empty<CombatStatus>(), false, false, false, 0, 0, Array.Empty<string>())).ToArray(), 0, Array.Empty<string>());
                }
                int? actual = null; string? nativeException = null;
                try { actual = CardStateModifiers.GetUpgradedStatValue(basis, stat, enforceFloor, native); }
                catch (OverflowException exception) { nativeException = exception.GetType().FullName; }
                samples.Add(new { BaseValue = basis, Stat = stat == CardStateModifiers.StatType.HP ? "Health" : stat.ToString(),
                    EnforceFloor = enforceFloor, FirstOffset = firstOffset, Modifiers = projected,
                    Actual = actual, NativeException = nativeException });
            }
            var rngAfter = Rng();
            bool contextUnchanged = JToken.DeepEquals(before, JToken.FromObject(trace.CaptureContext()));
            if (!contextUnchanged || frame != UnityEngine.Time.frameCount || !JToken.DeepEquals(rngBefore, rngAfter))
                throw new InvalidOperationException("Modifier overflow calibration changed native context, RNG or frame.");
            using (var document = NativeFixtureCapture.Capture(new { Schema = 1, GameVersion = UnityEngine.Application.version,
                GameModuleMvid = typeof(CardState).Assembly.ManifestModule.ModuleVersionId, ContextUnchanged = contextUnchanged,
                FrameBefore = frame, FrameAfter = UnityEngine.Time.frameCount, RngBefore = rngBefore, RngAfter = rngAfter, Samples = samples }))
            using (var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "card-modifier-overflow-calibration.mt2f")))
                document.Write(stream);
        }
    }
}
