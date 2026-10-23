using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    // Native dictionary enumeration and runtime sorting, on detached state only.
    internal static class StatusTriggerOrderCalibration
    {
        internal static void Capture(FullBattleTrace trace)
        {
            if (Environment.GetEnvironmentVariable("MT2_PROBE_STATUS_TRIGGER_ORDER") != "1") return;
            var managers = AllGameManagers.Instance!;
            var manager = managers.GetStatusEffectManager();
            var target = trace.KnownUnits.First(unit => unit.IsAlive && !unit.IsDestroyed);
            var field = AccessTools.Field(typeof(CharacterState), "_primaryStateInformation");
            object original = field.GetValue(target);
            object shadow = AccessTools.Method(original.GetType(), "DeepCopyForPreview").Invoke(original, null);
            var dictionary = AccessTools.Field(original.GetType(), "statusEffects");
            string[] ids = {
                "titanite", "damage shield", "armor", "titanskin", "dazed", "stealth", "ambush", "multistrike", "burst",
                "spikes", "lifesteal", "fragile", "piercing", "immune", "immobile", "relentless", "sweep", "sniper",
                "rooted", "haste", "untouchable", "endless", "buff", "debuff", "regen", "poison", "decay", "corruption",
                "corrupt_poison", "melee weakness", "spell weakness", "silenced", "muted", "valor", "pyregel",
                "heal multiplier", "heal immunity", "cooldown", "unit_ability", "unit_ability_available", "horde",
                "cardless", "undying", "ephemeral", "purify", "mageblade", "duality", "conduit", "newmoon", "fullmoon",
                "stygian_blessing", "scorch", "trample", "spell shield", "unstable", "hatch", "avarice", "inedible",
                "eatmany", "steelguard", "fixedattack", "armorattack", "equalizer", "inert", "spark", "captured_soul",
                "restricted", "unit_grafted_equipment"
            };
            string[] absent = ids.Where(id => manager.GetStatusEffectDataById(id, false) == null).ToArray();
            ids = ids.Except(absent).ToArray();
            JObject Rng()
            {
                var value = new JObject();
                foreach (RngId id in Enum.GetValues(typeof(RngId)))
                    value[id.ToString()] = JToken.FromObject(RngCalibration.Words(RandomManager.GetState(id)));
                value["Unity"] = JToken.FromObject(UnityEngine.Random.state);
                return value;
            }
            JToken before = JToken.FromObject(trace.CaptureDecision());
            JObject rng = Rng(); int frame = UnityEngine.Time.frameCount;
            var metadata = ids.Select(id => {
                var state = manager.Create(id, target) ?? throw new InvalidOperationException("Missing native status " + id);
                return new { Id = id, RuntimeType = state.GetType().Name, Order = state.GetTriggerOrder(),
                    ParamInt = state.GetParamInt() };
            }).ToArray();
            var rows = new List<object>();
            var damageRows = new List<object>();
            uint sequence = 0x18c5a24du;
            try
            {
                field.SetValue(target, shadow);
                for (int count = 2; count <= ids.Length; count++)
                for (int sample = 0; sample < 32; sample++)
                {
                    string[] order = ids.Take(count).ToArray();
                    if (sample == 1) Array.Reverse(order);
                    else if (sample > 1)
                        for (int index = order.Length - 1; index > 0; index--)
                        {
                            sequence = unchecked(sequence * 1664525u + 1013904223u);
                            int other = (int)(sequence % (uint)(index + 1));
                            string item = order[index]; order[index] = order[other]; order[other] = item;
                        }
                    var map = new Dictionary<string, CharacterState.StatusEffectStack>();
                    foreach (string id in order)
                        map.Add(id, new CharacterState.StatusEffectStack(manager.Create(id, target)!, 1));
                    // Retained zero-stack entries do not participate in native sorting.
                    if (count < ids.Length)
                        map.Add(ids[count], new CharacterState.StatusEffectStack(manager.Create(ids[count], target)!, 0));
                    dictionary.SetValue(shadow, map);
                    List<CharacterState.StatusEffectStack> sorted = new List<CharacterState.StatusEffectStack>();
                    target.GetStatusEffects(ref sorted);
                    sorted.Sort((left, right) => left.State.GetTriggerOrder().CompareTo(right.State.GetTriggerOrder()));
                    var input = new StatusEffectState.InputTriggerParams { attacked = target, attacker = target, damage = 21 };
                    var output = new StatusEffectState.OutputTriggerParams { damage = 21 };
                    manager.TestTriggerStatusEffects(StatusEffectData.TriggerStage.OnPreAttackedDamageShield, target, input, output);
                    rows.Add(new { Count = count, Sample = sample, DictionaryOrder = map.Keys.ToArray(),
                        ZeroStackId = count < ids.Length ? ids[count] : null,
                        SortedOrder = sorted.Select(entry => entry.State.GetStatusId()).ToArray(),
                        ShieldDamage = output.damage, ShieldBlocked = output.damageBlocked });
                }
                string[] neutral = { "titanite", "damage shield", "armor", "titanskin", "dazed", "stealth", "ambush",
                    "multistrike", "burst", "spikes", "lifesteal", "piercing", "immobile", "sweep", "sniper", "rooted",
                    "haste", "endless", "regen", "poison", "decay", "corruption", "corrupt_poison", "silenced" };
                foreach (int count in new[] { 16, 17, 24 })
                for (int sample = 0; sample < 64; sample++)
                {
                    string[] order = neutral.Take(count).ToArray();
                    for (int index = order.Length - 1; index > 0; index--)
                    {
                        sequence = unchecked(sequence * 1664525u + 1013904223u);
                        int other = (int)(sequence % (uint)(index + 1));
                        string item = order[index]; order[index] = order[other]; order[other] = item;
                    }
                    var map = new Dictionary<string, CharacterState.StatusEffectStack>();
                    foreach (string id in order)
                        map.Add(id, new CharacterState.StatusEffectStack(manager.Create(id, target)!, 1));
                    dictionary.SetValue(shadow, map);
                    var input = new StatusEffectState.InputTriggerParams { attacked = target, attacker = target, damage = 21 };
                    var output = new StatusEffectState.OutputTriggerParams { damage = 21 };
                    manager.TestTriggerStatusEffects(StatusEffectData.TriggerStage.OnPreAttackedDamageShield, target, input, output);
                    damageRows.Add(new { Count = count, Sample = sample, StatusOrder = order,
                        ShieldDamage = output.damage, ShieldBlocked = output.damageBlocked });
                }
            }
            finally { field.SetValue(target, original); }
            bool unchanged = frame == UnityEngine.Time.frameCount && JToken.DeepEquals(rng, Rng()) &&
                JToken.DeepEquals(before, JToken.FromObject(trace.CaptureDecision()));
            if (!unchanged) throw new InvalidOperationException("Status sort calibration changed live decision/RNG/frame.");
            using (var fixture = NativeFixtureCapture.Capture(new { Schema = 1,
                GameModuleMvid = typeof(CardState).Assembly.ManifestModule.ModuleVersionId,
                ContextUnchanged = unchanged, FrameBefore = frame, FrameAfter = UnityEngine.Time.frameCount,
                RngBefore = rng, RngAfter = Rng(), Metadata = metadata, AbsentModelIds = absent,
                Rows = rows, DamageRows = damageRows }))
            using (var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "status-trigger-order-calibration.mt2f")))
                fixture.Write(stream);
        }
    }
}
