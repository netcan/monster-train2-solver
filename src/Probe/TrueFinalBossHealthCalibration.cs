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
    internal static class TrueFinalBossHealthCalibration
    {
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_TRUE_FINAL_BOSS_HEALTH_UPDATES") == "1";
        internal static void Capture(IReadOnlyList<CharacterState> actors, Func<object> world)
        {
            if (!Enabled) return;
            if (actors.Count != 5 || actors.Any(actor => actor.PreviewMode || actor.TemporaryPreviewsEnabled || !actor.HasStatusEffect("lifelink")))
                throw new InvalidOperationException("Health-update controls require five original non-preview TFB actors.");
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
            var dirty = AccessTools.Field(stateType, "hpDirtied");
            var rawHp = AccessTools.Field(stateType, "hp");
            var rawMaxHp = AccessTools.Field(stateType, "maxHp");
            var statuses = AccessTools.Field(stateType, "statusEffects");
            var updateHp = AccessTools.Method(typeof(CharacterState), "UpdateHp");
            var previewState = AccessTools.Property(typeof(CombatManager), "CombatPreviewState");
            object originalMode = previewState.GetValue(combat);
            int originalChange = combat.TrueFinalBossBattlePreviewHpChange;
            bool originalDead = combat.AllScenarioBossesDead;
            int Id(CharacterState? actor) => actor == null ? 0 : Enumerable.Range(0, actors.Count).Single(index => ReferenceEquals(actors[index], actor)) + 1;
            object Rng() => new { Streams = Enum.GetValues(typeof(RngId)).Cast<RngId>()
                .Select(id => new { Id = id.ToString(), Words = RngCalibration.Words(RandomManager.GetState(id)) }).ToArray(),
                Unity = RngCalibration.Words(UnityEngine.Random.state) };
            object beforeWorld = world(), beforeRng = Rng();
            int frame = UnityEngine.Time.frameCount;
            object[] shadows = Array.Empty<object>();
            var rows = new List<object>();
            var events = new List<HealthChangedEvent>();
            StatusEffectLifeLinkState status = (StatusEffectLifeLinkState)managers.GetStatusEffectManager().Create("lifelink", actors[0]);
            TrueFinalBossHealthState Snapshot() => new TrueFinalBossHealthState(new LinkedHealthState(actors.Select((actor, index) =>
                new LinkedHealthActor(index + 1, Id(actor.GetLinkedCharacterState()),
                    Convert.ToInt32(((ObfuscatedNumber)rawHp.GetValue(shadows[index])).Value),
                    Convert.ToInt32(((ObfuscatedNumber)rawMaxHp.GetValue(shadows[index])).Value), (bool)dirty.GetValue(shadows[index]))).ToArray()),
                new LifeLinkContext(actors.Select((actor, index) => new LifeLinkActor(index + 1, Id(actor.GetLinkedCharacterState()),
                    actor.GetHP(), actor.PreviewHP(), actor.Initialized, actor.HasStatusEffect("lifelink"), actor.IsTrueFinalBoss(), actor.IsOuterTrainBoss())).ToArray(),
                    combat.CombatPreviewState == CharacterState.CombatPreviewState.Calculating, combat.TrueFinalBossBattlePreviewHpChange));
            void Reset(int mode)
            {
                for (int i = 0; i < actors.Count; i++) primary.SetValue(actors[i], originals[i]);
                shadows = actors.Select((actor, index) =>
                {
                    object state = Activator.CreateInstance(stateType, true)!;
                    hp.SetValue(state, actor.GetHP()); maxHp.SetValue(state, actor.GetMaxHP());
                    link.SetValue(state, actor.GetLinkedCharacterState()); dirty.SetValue(state, false);
                    statuses.SetValue(state, new Dictionary<string, CharacterState.StatusEffectStack>(
                        (Dictionary<string, CharacterState.StatusEffectStack>)statuses.GetValue(originals[index])));
                    object previewCopy = Activator.CreateInstance(stateType, true)!;
                    hp.SetValue(previewCopy, mode == 0 ? actor.GetHP() : 1);
                    preview.SetValue(actor, previewCopy);
                    return state;
                }).ToArray();
                for (int i = 0; i < actors.Count; i++)
                {
                    primary.SetValue(actors[i], shadows[i]);
                    actors[i].AddHpChangedListener((actor, value) => events.Add(new HealthChangedEvent(Id(actor), value)));
                }
                previewState.SetValue(combat, mode == 0 ? CharacterState.CombatPreviewState.Off : CharacterState.CombatPreviewState.Calculating);
                combat.TrueFinalBossBattlePreviewHpChange = 0;
            }
            void Record(string label, int mode, int target, string operation, int value)
            {
                TrueFinalBossHealthState before = Snapshot();
                events.Clear();
                LifeLinkDamageResult? damage = null;
                int amount = value;
                if (operation == "LifeLinkAndDebuffHP")
                {
                    var input = new StatusEffectState.InputTriggerParams { attacked = actors[target - 1], damage = value };
                    var output = new StatusEffectState.OutputTriggerParams();
                    bool triggered = status.TestTrigger(input, output, managers.GetCoreManagers());
                    damage = new LifeLinkDamageResult(triggered, output.damage, output.damagePreventedByLifeLink);
                    amount = output.damage;
                }
                int next = operation == "UpdateHp" ? amount : amount > 0 ? Math.Max(0, unchecked(actors[target - 1].GetHP() - amount)) : actors[target - 1].GetHP();
                if (next <= 0) throw new InvalidOperationException("Health-update control would invoke uncalibrated native terminal callbacks.");
                if (operation == "UpdateHp") updateHp.Invoke(actors[target - 1], new object[] { amount });
                else actors[target - 1].DebuffHP(amount);
                if (combat.AllScenarioBossesDead != originalDead) throw new InvalidOperationException("Health-update control changed terminal state.");
                rows.Add(new { Label = label, Mode = mode, TargetId = target, Operation = operation, Value = value, Before = before,
                    Actual = new TrueFinalBossHealthResult(Snapshot(), events.ToArray(), damage) });
            }
            try
            {
                foreach (int mode in new[] { 0, 1 })
                foreach (int target in Enumerable.Range(1, 5))
                foreach (string operation in new[] { "DebuffHP", "UpdateHp", "LifeLinkAndDebuffHP" })
                foreach (int value in operation == "DebuffHP" ? new[] { int.MinValue, -1, 0, 1, 9 } : operation == "UpdateHp" ?
                    new[] { 1, 2, 17, 99999, 100000, int.MaxValue } : new[] { int.MinValue, -1, 0, 1, 9, 4000, int.MaxValue })
                { Reset(mode); Record("matrix", mode, target, operation, value); }
                foreach (int mode in new[] { 0, 1 })
                {
                    Reset(mode);
                    Record("chain-mid-lethal", mode, 4, "LifeLinkAndDebuffHP", 4000);
                    Record("chain-top-protected", mode, 5, "LifeLinkAndDebuffHP", 9);
                    Record("chain-savagery-lethal", mode, 2, "LifeLinkAndDebuffHP", 4000);
                    Record("chain-dominion-hit", mode, 1, "LifeLinkAndDebuffHP", 1000);
                    Record("chain-all-groups-one", mode, 1, "LifeLinkAndDebuffHP", 554);
                    Record("chain-top-recover", mode, 5, "UpdateHp", 77);
                    Record("chain-dominion-recover", mode, 1, "UpdateHp", 1555);
                    Record("chain-savagery-protected", mode, 2, "LifeLinkAndDebuffHP", 4000);
                    Record("chain-mid-debuff", mode, 4, "DebuffHP", 1);
                    Record("chain-top-upper-cap", mode, 5, "UpdateHp", 100000);
                }
            }
            finally
            {
                for (int i = 0; i < actors.Count; i++) { primary.SetValue(actors[i], originals[i]); preview.SetValue(actors[i], originalPreviews[i]); }
                previewState.SetValue(combat, originalMode); combat.TrueFinalBossBattlePreviewHpChange = originalChange;
            }
            object afterWorld = world(), afterRng = Rng();
            bool restored = actors.Select((actor, index) => ReferenceEquals(primary.GetValue(actor), originals[index]) &&
                ReferenceEquals(preview.GetValue(actor), originalPreviews[index])).All(value => value) &&
                Equals(previewState.GetValue(combat), originalMode) && combat.TrueFinalBossBattlePreviewHpChange == originalChange &&
                combat.AllScenarioBossesDead == originalDead;
            bool unchanged = restored && frame == UnityEngine.Time.frameCount &&
                JToken.DeepEquals(JToken.FromObject(beforeWorld), JToken.FromObject(afterWorld)) &&
                JToken.DeepEquals(JToken.FromObject(beforeRng), JToken.FromObject(afterRng));
            if (!unchanged) throw new InvalidOperationException("Health-update control changed original state, world, frame or RNG.");
            using (var document = NativeFixtureCapture.Capture(new
            {
                Schema = 1, GameModuleMvid = typeof(CharacterState).Assembly.ManifestModule.ModuleVersionId,
                Boundary = "NativeLifeLinkAndNonterminalHealthUpdates", NativeStateReferencesRestored = restored, ContextUnchanged = unchanged,
                CharacterPreviewModesOff = actors.All(actor => !actor.PreviewMode), AllScenarioBossesDeadUnchanged = combat.AllScenarioBossesDead == originalDead,
                FrameBefore = frame, FrameAfter = UnityEngine.Time.frameCount, RngBefore = beforeRng, RngAfter = afterRng,
                WorldBefore = beforeWorld, WorldAfter = afterWorld, Rows = rows
            }))
            using (var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "true-final-boss-health-updates-calibration.mt2f"))) document.Write(output);
        }
    }
}
