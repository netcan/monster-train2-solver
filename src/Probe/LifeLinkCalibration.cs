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
    internal static class LifeLinkCalibration
    {
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_TRUE_FINAL_BOSS_LIFE_LINK") == "1";
        internal static void Capture(IReadOnlyList<CharacterState> actors, Func<object> world)
        {
            if (!Enabled) return;
            if (actors.Count != 5 || actors.Any(actor => actor.PreviewMode || actor.TemporaryPreviewsEnabled || !actor.HasStatusEffect("lifelink")))
                throw new InvalidOperationException("Life-link calibration requires five original non-preview TFB actors with their original statuses.");
            AllGameManagers managers = AllGameManagers.Instance;
            CombatManager combat = managers.GetCombatManager();
            var primary = AccessTools.Field(typeof(CharacterState), "_primaryStateInformation");
            var preview = AccessTools.Field(typeof(CharacterState), "_previewStateInformation");
            object[] originals = actors.Select(actor => primary.GetValue(actor)).ToArray();
            object?[] originalPreviews = actors.Select(actor => preview.GetValue(actor)).ToArray();
            Type stateType = originals[0].GetType();
            var hp = AccessTools.Property(stateType, "Hp");
            var maxHp = AccessTools.Property(stateType, "MaxHp");
            var link = AccessTools.Property(stateType, "linkedCharacterState");
            var statuses = AccessTools.Field(stateType, "statusEffects");
            var previewState = AccessTools.Property(typeof(CombatManager), "CombatPreviewState");
            object originalMode = previewState.GetValue(combat);
            int originalChange = combat.TrueFinalBossBattlePreviewHpChange;
            int Id(CharacterState? actor) => actor == null ? 0 : Enumerable.Range(0, actors.Count).Single(index => ReferenceEquals(actors[index], actor)) + 1;
            object Rng() => new { Streams = Enum.GetValues(typeof(RngId)).Cast<RngId>()
                .Select(id => new { Id = id.ToString(), Words = RngCalibration.Words(RandomManager.GetState(id)) }).ToArray(),
                Unity = RngCalibration.Words(UnityEngine.Random.state) };
            object beforeWorld = world(), beforeRng = Rng();
            int frame = UnityEngine.Time.frameCount;
            StatusEffectLifeLinkState status = (StatusEffectLifeLinkState)managers.GetStatusEffectManager().Create("lifelink", actors[0]);
            var rows = new List<object>();
            void Record(int dominionHp, int savageryHp, int entropyHp, int mask, int mode, string label)
            {
                int[] health = { dominionHp, savageryHp, entropyHp, entropyHp, entropyHp };
                object[] shadows = originals.Select((original, index) =>
                {
                    object state = Activator.CreateInstance(stateType, true)!;
                    hp.SetValue(state, health[index]); maxHp.SetValue(state, 99999);
                    link.SetValue(state, actors[index].GetLinkedCharacterState());
                    var dictionary = new Dictionary<string, CharacterState.StatusEffectStack>((Dictionary<string, CharacterState.StatusEffectStack>)statuses.GetValue(original));
                    if ((mask & (1 << index)) == 0) dictionary.Remove("lifelink");
                    statuses.SetValue(state, dictionary);
                    object previewCopy = Activator.CreateInstance(stateType, true)!;
                    hp.SetValue(previewCopy, mode == 0 ? health[index] : 1);
                    preview.SetValue(actors[index], previewCopy);
                    return state;
                }).ToArray();
                for (int i = 0; i < actors.Count; i++) primary.SetValue(actors[i], shadows[i]);
                previewState.SetValue(combat, mode >= 2 ? CharacterState.CombatPreviewState.Calculating : CharacterState.CombatPreviewState.Off);
                combat.TrueFinalBossBattlePreviewHpChange = mode == 2 ? 1 : mode == 3 ? 3500 : 0;
                var context = new LifeLinkContext(actors.Select((actor, index) => new LifeLinkActor(index + 1,
                    Id(actor.GetLinkedCharacterState()), actor.GetHP(), actor.PreviewHP(), actor.Initialized, actor.HasStatusEffect("lifelink"),
                    actor.IsTrueFinalBoss(), actor.IsOuterTrainBoss())).ToArray(), mode >= 2, combat.TrueFinalBossBattlePreviewHpChange);
                for (int target = 1; target <= actors.Count; target++)
                {
                    bool protection = StatusEffectLifeLinkState.ProtectedByLifeLink(actors[target - 1], managers.GetHeroManager(), managers.GetMonsterManager(), combat);
                    bool prediction = StatusEffectLifeLinkState.ProtectedByLifeLink(actors[target - 1], managers.GetHeroManager(), managers.GetMonsterManager(), combat, true);
                    var damage = new List<object>();
                    foreach (int amount in new[] { 0, 1, 9, 4000, int.MaxValue, int.MinValue })
                    {
                        var input = new StatusEffectState.InputTriggerParams { attacked = actors[target - 1], damage = amount };
                        var output = new StatusEffectState.OutputTriggerParams { damage = -77, damagePreventedByLifeLink = -88 };
                        bool triggered = status.TestTrigger(input, output, managers.GetCoreManagers());
                        damage.Add(new { Input = amount, Actual = new LifeLinkDamageResult(triggered, output.damage, output.damagePreventedByLifeLink) });
                    }
                    rows.Add(new { Label = label, Mask = mask, Mode = mode, Context = context, TargetId = target,
                        Protected = protection, ProtectedForPrediction = prediction, Damage = damage });
                }
                for (int i = 0; i < actors.Count; i++) primary.SetValue(actors[i], originals[i]);
            }
            try
            {
                foreach (int dominion in new[] { 1, 2, 1555 })
                foreach (int savagery in new[] { 1, 2, 2750 })
                foreach (int entropy in new[] { 1, 2, 3500 })
                foreach (int mask in new[] { 31, 28, 0 })
                foreach (int mode in Enumerable.Range(0, 4)) Record(dominion, savagery, entropy, mask, mode, "matrix");
                foreach (int mode in Enumerable.Range(0, 4)) Record(0, 0, 0, 31, mode, "dead-targets");
            }
            finally
            {
                for (int i = 0; i < actors.Count; i++) { primary.SetValue(actors[i], originals[i]); preview.SetValue(actors[i], originalPreviews[i]); }
                previewState.SetValue(combat, originalMode); combat.TrueFinalBossBattlePreviewHpChange = originalChange;
            }
            object afterWorld = world(), afterRng = Rng();
            bool restored = actors.Select((actor, index) => ReferenceEquals(primary.GetValue(actor), originals[index]) &&
                ReferenceEquals(preview.GetValue(actor), originalPreviews[index])).All(value => value) &&
                Equals(previewState.GetValue(combat), originalMode) && combat.TrueFinalBossBattlePreviewHpChange == originalChange;
            bool unchanged = restored && frame == UnityEngine.Time.frameCount &&
                JToken.DeepEquals(JToken.FromObject(beforeWorld), JToken.FromObject(afterWorld)) &&
                JToken.DeepEquals(JToken.FromObject(beforeRng), JToken.FromObject(afterRng));
            if (!unchanged) throw new InvalidOperationException("Life-link calibration changed live state, preview controls, frame or RNG.");
            using (var document = NativeFixtureCapture.Capture(new
            {
                Schema = 1, GameModuleMvid = typeof(CharacterState).Assembly.ManifestModule.ModuleVersionId,
                Boundary = "NativeLifeLinkProtectionAndDamageStage", NativeStateReferencesRestored = restored, ContextUnchanged = unchanged,
                FrameBefore = frame, FrameAfter = UnityEngine.Time.frameCount, RngBefore = beforeRng, RngAfter = afterRng,
                WorldBefore = beforeWorld, WorldAfter = afterWorld, Rows = rows
            }))
            using (var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "true-final-boss-life-link-calibration.mt2f"))) document.Write(output);
        }
    }
}
