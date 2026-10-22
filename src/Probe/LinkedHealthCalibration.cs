using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;
using ObfuscatedNumber = ShinyShoe.ObfuscatedNumber;

namespace MonsterTrain2Poju.Probe
{
    internal static class LinkedHealthCalibration
    {
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_TRUE_FINAL_BOSS_HEALTH") == "1";
        internal static void Capture(IReadOnlyList<CharacterState> actors, Func<object> world)
        {
            if (!Enabled) return;
            if (actors.Count != 5 || actors.Any(actor => actor.PreviewMode || actor.TemporaryPreviewsEnabled))
                throw new InvalidOperationException("Linked health requires five original non-preview TFB actors.");
            var primary = AccessTools.Field(typeof(CharacterState), "_primaryStateInformation");
            object[] originals = actors.Select(actor => primary.GetValue(actor)).ToArray();
            Type stateType = originals[0].GetType();
            var hp = AccessTools.Property(stateType, "Hp");
            var maxHp = AccessTools.Property(stateType, "MaxHp");
            var link = AccessTools.Property(stateType, "linkedCharacterState");
            var dirty = AccessTools.Field(stateType, "hpDirtied");
            var rawHp = AccessTools.Field(stateType, "hp");
            var rawMaxHp = AccessTools.Field(stateType, "maxHp");
            var copy = AccessTools.Method(stateType, "DeepCopyForPreview");
            int Id(CharacterState? actor) => actor == null ? 0 : Enumerable.Range(0, actors.Count).Single(index => ReferenceEquals(actors[index], actor)) + 1;
            object Rng() => new { Streams = Enum.GetValues(typeof(RngId)).Cast<RngId>()
                .Select(id => new { Id = id.ToString(), Words = RngCalibration.Words(RandomManager.GetState(id)) }).ToArray(),
                Unity = RngCalibration.Words(UnityEngine.Random.state) };
            object beforeWorld = world(), beforeRng = Rng();
            int frame = UnityEngine.Time.frameCount;
            object[] shadows = Array.Empty<object>();
            var rows = new List<object>();
            LinkedHealthSnapshot Snapshot()
            {
                var state = new LinkedHealthState(actors.Select((actor, index) => new LinkedHealthActor(index + 1,
                    Id((CharacterState?)link.GetValue(shadows[index])), Convert.ToInt32(((ObfuscatedNumber)rawHp.GetValue(shadows[index])).Value),
                    Convert.ToInt32(((ObfuscatedNumber)rawMaxHp.GetValue(shadows[index])).Value), (bool)dirty.GetValue(shadows[index]))).ToArray());
                return new LinkedHealthSnapshot(state, actors.Select((actor, index) => new LinkedHealthValue(index + 1, actor.GetHP(), actor.GetMaxHP())).ToArray());
            }
            void Reset()
            {
                for (int i = 0; i < actors.Count; i++) primary.SetValue(actors[i], originals[i]);
                // Allocate empty detached StateInformation objects. Calling the
                // native preview copier on live state can dirty private lists.
                shadows = actors.Select((actor, index) =>
                {
                    object state = Activator.CreateInstance(stateType, true)!;
                    hp.SetValue(state, actor.GetHP()); maxHp.SetValue(state, actor.GetMaxHP());
                    link.SetValue(state, actor.GetLinkedCharacterState()); dirty.SetValue(state, false);
                    return state;
                }).ToArray();
                for (int i = 0; i < actors.Count; i++) primary.SetValue(actors[i], shadows[i]);
            }
            void Write(string label, int target, string property, int value)
            {
                LinkedHealthSnapshot before = Snapshot();
                (property == "Hp" ? hp : maxHp).SetValue(shadows[target - 1], value);
                rows.Add(new { Label = label, Operation = "Write", TargetId = target, Property = property, Value = value,
                    Before = before, Actual = Snapshot() });
            }
            void Copy()
            {
                LinkedHealthSnapshot before = Snapshot();
                object[] copies = shadows.Select(state => copy.Invoke(state, null)).ToArray();
                shadows = copies;
                for (int i = 0; i < actors.Count; i++) primary.SetValue(actors[i], shadows[i]);
                rows.Add(new { Label = "native-preview-copy", Operation = "CopyForPreview", TargetId = 0,
                    Property = "", Value = 0, Before = before, Actual = Snapshot() });
            }
            try
            {
                foreach (int target in Enumerable.Range(1, 5))
                foreach (string property in new[] { "Hp", "MaxHp" })
                foreach (int value in new[] { 17, 0, -3, 99999, 100000, int.MaxValue, int.MinValue })
                {
                    Reset(); Write("matrix", target, property, value);
                }
                Reset();
                Write("chain-mid-hp", 4, "Hp", 3241);
                Write("chain-top-max", 5, "MaxHp", 4001);
                Write("chain-bot-hp", 3, "Hp", 3189);
                Write("chain-dominion-hp", 1, "Hp", 101);
                Write("chain-savagery-max", 2, "MaxHp", 2000);
                Copy();
                Write("preview-mid-independent", 4, "Hp", 1234);
                Write("preview-top-max-independent", 5, "MaxHp", 17);
                Write("preview-bot-independent", 3, "Hp", 3030);
                Copy();
            }
            finally { for (int i = 0; i < actors.Count; i++) primary.SetValue(actors[i], originals[i]); }
            object afterWorld = world(), afterRng = Rng();
            bool restored = actors.Select((actor, index) => ReferenceEquals(primary.GetValue(actor), originals[index])).All(value => value);
            bool unchanged = restored && frame == UnityEngine.Time.frameCount &&
                JToken.DeepEquals(JToken.FromObject(beforeWorld), JToken.FromObject(afterWorld)) &&
                JToken.DeepEquals(JToken.FromObject(beforeRng), JToken.FromObject(afterRng));
            if (!unchanged) throw new InvalidOperationException("Linked-health calibration changed original state, world, frame or RNG.");
            using (var document = NativeFixtureCapture.Capture(new
            {
                Schema = 1, GameModuleMvid = typeof(CharacterState).Assembly.ManifestModule.ModuleVersionId,
                Boundary = "DetachedNativeStateInformationPropertiesAndPreviewCopy", OriginalStateReferencesRestored = restored,
                ContextUnchanged = unchanged, FrameBefore = frame, FrameAfter = UnityEngine.Time.frameCount,
                WorldBefore = beforeWorld, WorldAfter = afterWorld, RngBefore = beforeRng, RngAfter = afterRng, Rows = rows
            }))
            using (var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "true-final-boss-linked-health-calibration.mt2f"))) document.Write(output);
        }
    }
}
