using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    // Synchronous status tests on a detached status dictionary. No live status
    // mutation, callback, coroutine, frame advance or RNG restoration is used.
    internal static class TitaniteCombinationCalibration
    {
        internal static void Capture(FullBattleTrace trace)
        {
            if (Environment.GetEnvironmentVariable("MT2_PROBE_TITANITE_COMBINATIONS") != "1") return;
            var managers = AllGameManagers.Instance!;
            var statuses = managers.GetStatusEffectManager();
            var save = managers.GetSaveManager();
            var target = trace.KnownUnits.First(unit => unit.IsAlive && !unit.IsDestroyed);
            var field = AccessTools.Field(typeof(CharacterState), "_primaryStateInformation");
            object original = field.GetValue(target);
            var stateType = original.GetType();
            var dictionaryField = AccessTools.Field(stateType, "statusEffects");
            object shadow = AccessTools.Method(stateType, "DeepCopyForPreview").Invoke(original, null);
            JObject Rng()
            {
                var value = new JObject();
                foreach (RngId id in Enum.GetValues(typeof(RngId)))
                    value[id.ToString()] = JToken.FromObject(RngCalibration.Words(RandomManager.GetState(id)));
                value["Unity"] = JToken.FromObject(UnityEngine.Random.state);
                return value;
            }
            var before = JToken.FromObject(trace.CaptureDecision());
            var rng = Rng(); int frame = UnityEngine.Time.frameCount;
            var rows = new List<object>();
            var definition = save.GetAllGameData().FindCardData("fb0683b1-20ac-4dd2-b580-14b502e182f4")!;
            var plain = new CardState(); plain.Setup(definition, save);
            var piercing = new CardState(); piercing.Setup(definition, save);
            var trait = new CardTraitData(); trait.Setup("CardTraitIgnoreArmor"); piercing.AddTrait(trait);
            var metadata = new[] { "titanite", "damage shield", "titanskin", "armor", "piercing" }.Select(id =>
            {
                var state = statuses.Create(id, target)!;
                return new { Id = id, Stage = statuses.GetStatusEffectDataById(id)!.GetPrimaryTriggerStage().ToString(),
                    Order = state.GetTriggerOrder(), ParamInt = state.GetParamInt() };
            }).ToArray();
            // Native status dispatch sorts all active entries before filtering
            // by stage. Establish the stable-sort boundary in the game runtime.
            var sortControls = new[] { 2, 5, 8, 16, 17, 32 }.Select(count =>
            {
                var entries = Enumerable.Range(0, count).Select(index => new
                    { Index = index, State = statuses.Create(index % 2 == 0 ? "titanite" : "damage shield", target)! }).ToList();
                entries.Sort((left, right) => left.State.GetTriggerOrder().CompareTo(right.State.GetTriggerOrder()));
                return new { Count = count, Order = entries.Select(entry => entry.Index).ToArray() };
            }).ToArray();
            try
            {
                field.SetValue(target, shadow);
                foreach (bool reverse in new[] { false, true })
                foreach (int titanite in new[] { 1, 3 })
                foreach (int shield in new[] { 0, 1 })
                foreach (int skin in new[] { 0, 2, 5 })
                foreach (int armor in new[] { 0, 7 })
                foreach (string source in new[] { "card", "card-piercing", "unit", "unit-piercing" })
                foreach (int damage in new[] { 0, 1, 2, 3, 9, 10, 21, 255 })
                {
                    var map = new Dictionary<string, CharacterState.StatusEffectStack>();
                    void Add(string id, int count)
                    {
                        if (count > 0) map.Add(id, new CharacterState.StatusEffectStack(statuses.Create(id, target)!, count));
                    }
                    if (reverse) { Add("damage shield", shield); Add("titanite", titanite); }
                    else { Add("titanite", titanite); Add("damage shield", shield); }
                    Add("titanskin", skin); Add("armor", armor);
                    if (source == "unit-piercing") Add("piercing", 1);
                    dictionaryField.SetValue(shadow, map);
                    var input = new StatusEffectState.InputTriggerParams { attacked = target, damage = damage,
                        attacker = source.StartsWith("unit", StringComparison.Ordinal) ? target : null,
                        damageSourceCard = source == "card" ? plain : source == "card-piercing" ? piercing : null };
                    var output = new StatusEffectState.OutputTriggerParams { damage = damage };
                    statuses.TestTriggerStatusEffects(StatusEffectData.TriggerStage.OnPreAttackedDamageShield, target, input, output);
                    int shieldDamage = output.damage, blocked = output.damageBlocked;
                    statuses.TestTriggerStatusEffects(StatusEffectData.TriggerStage.OnPreAttackedTitanSkin, target, input, output);
                    int skinDamage = output.damage, soaked = output.titanSkinDamageSoak;
                    statuses.TestTriggerStatusEffects(StatusEffectData.TriggerStage.OnPreAttackedArmor, target, input, output);
                    rows.Add(new { Reverse = reverse, Titanite = titanite, Shield = shield, Skin = skin, Armor = armor,
                        Source = source, Damage = damage, StatusOrder = map.Keys.ToArray(), ShieldDamage = shieldDamage,
                        ShieldBlocked = blocked, SkinDamage = skinDamage, SkinSoaked = soaked,
                        Actual = output.damage, Blocked = output.damageBlocked });
                }
            }
            finally { field.SetValue(target, original); }
            bool unchanged = frame == UnityEngine.Time.frameCount && JToken.DeepEquals(rng, Rng()) &&
                JToken.DeepEquals(before, JToken.FromObject(trace.CaptureDecision()));
            if (!unchanged) throw new InvalidOperationException("Titanite combination calibration changed live decision/RNG/frame.");
            using (var fixture = NativeFixtureCapture.Capture(new { Schema = 1,
                GameModuleMvid = typeof(CardState).Assembly.ManifestModule.ModuleVersionId,
                ContextUnchanged = unchanged, FrameBefore = frame, FrameAfter = UnityEngine.Time.frameCount,
                RngBefore = rng, RngAfter = Rng(), Metadata = metadata, SortControls = sortControls, Rows = rows }))
            using (var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "titanite-combinations-calibration.mt2f")))
                fixture.Write(stream);
        }
    }
}
