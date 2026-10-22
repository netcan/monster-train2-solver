using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    // Test native stages on a detached status dictionary, without executing
    // coroutines, consuming live statuses or advancing the frame/RNG.
    internal static class TitaniteWeaknessCalibration
    {
        internal static void Capture(FullBattleTrace trace)
        {
            if (Environment.GetEnvironmentVariable("MT2_PROBE_TITANITE_WEAKNESS") != "1") return;
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
            var metadata = new[] { "titanite", "damage shield", "titanskin", "armor", "melee weakness", "spell weakness", "piercing" }
                .Select(id =>
                {
                    var state = statuses.Create(id, target)!;
                    var data = statuses.GetStatusEffectDataById(id)!;
                    return new { Id = id, Stage = data.GetPrimaryTriggerStage().ToString(),
                        Order = state.GetTriggerOrder(), ParamInt = state.GetParamInt(), RemoveWhenTriggered = data.GetRemoveWhenTriggered(),
                        RemoveAfterCardPlayed = data.GetRemoveWhenTriggeredAfterCardPlayed() };
                }).ToArray();
            try
            {
                field.SetValue(target, shadow);
                foreach (int titanite in new[] { 0, 1 })
                foreach (int shield in new[] { 0, 1 })
                foreach (string defense in new[] { "none", "armor", "titanskin" })
                foreach (string source in new[] { "unit", "unit-piercing", "card", "card-piercing" })
                foreach (int damage in new[] { 0, 4, 5 })
                {
                    bool unit = source.StartsWith("unit", StringComparison.Ordinal);
                    string weakness = unit ? "melee weakness" : "spell weakness";
                    var map = new Dictionary<string, CharacterState.StatusEffectStack>();
                    void Add(string id, int count)
                    {
                        if (count > 0) map.Add(id, new CharacterState.StatusEffectStack(statuses.Create(id, target)!, count));
                    }
                    // Match the natural Firebrand's shield-before-Titanite order.
                    Add("damage shield", shield); Add("titanite", titanite); Add(weakness, 1);
                    Add("armor", defense == "armor" ? 3 : 0); Add("titanskin", defense == "titanskin" ? 2 : 0);
                    if (source == "unit-piercing") Add("piercing", 1);
                    dictionaryField.SetValue(shadow, map);
                    var input = new StatusEffectState.InputTriggerParams { attacked = target, damage = damage,
                        damageType = unit ? Damage.Type.DirectAttack : Damage.Type.Default,
                        attacker = unit ? target : null,
                        damageSourceCard = source == "card" ? plain : source == "card-piercing" ? piercing : null };
                    var output = new StatusEffectState.OutputTriggerParams { damage = damage };
                    statuses.TestTriggerStatusEffects(StatusEffectData.TriggerStage.OnPreAttackedSpellShield, target, input, output);
                    statuses.TestTriggerStatusEffects(StatusEffectData.TriggerStage.OnPreAttackedDamageShield, target, input, output);
                    int shieldDamage = output.damage, shieldBlocked = output.damageBlocked;
                    output.count = 0;
                    statuses.TestTriggerStatusEffects(StatusEffectData.TriggerStage.OnPreAttacked, target, input, output);
                    int weaknessDamage = output.damage, weaknessCount = output.count;
                    statuses.TestTriggerStatusEffects(StatusEffectData.TriggerStage.OnPreAttackedTitanSkin, target, input, output);
                    int skinDamage = output.damage;
                    output.count = 0;
                    statuses.TestTriggerStatusEffects(StatusEffectData.TriggerStage.OnPreAttackedArmor, target, input, output);
                    rows.Add(new { Titanite = titanite, Shield = shield, Defense = defense, Source = source, Damage = damage,
                        Weakness = weakness, StatusOrder = map.Keys.ToArray(), ShieldDamage = shieldDamage,
                        ShieldBlocked = shieldBlocked, WeaknessDamage = weaknessDamage, WeaknessCount = weaknessCount,
                        SkinDamage = skinDamage, ArmorSpent = output.count, Actual = output.damage });
                }
            }
            finally { field.SetValue(target, original); }
            bool unchanged = frame == UnityEngine.Time.frameCount && JToken.DeepEquals(rng, Rng()) &&
                JToken.DeepEquals(before, JToken.FromObject(trace.CaptureDecision()));
            if (!unchanged) throw new InvalidOperationException("Titanite/weakness calibration changed live decision/RNG/frame.");
            using (var fixture = NativeFixtureCapture.Capture(new { Schema = 1,
                GameModuleMvid = typeof(CardState).Assembly.ManifestModule.ModuleVersionId,
                ContextUnchanged = unchanged, FrameBefore = frame, FrameAfter = UnityEngine.Time.frameCount,
                RngBefore = rng, RngAfter = Rng(), Metadata = metadata, Rows = rows }))
            using (var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "titanite-weakness-calibration.mt2f")))
                fixture.Write(stream);
        }
    }
}
