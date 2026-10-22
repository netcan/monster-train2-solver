using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal static class TrueFinalBossDeathCalibration
    {
        internal static string Target => Environment.GetEnvironmentVariable("MT2_PROBE_TRUE_FINAL_BOSS_DEATH") ?? "";
        internal static bool Enabled => Target.Length > 0;
        internal static bool Completed { get; private set; }
        internal static string? Error { get; private set; }
        private static bool started;
        private static float startedAt;
        private static bool observing;
        private static readonly List<object> roots = new List<object>();
        private static readonly List<object> steps = new List<object>();
        private static IReadOnlyList<CharacterState> actors = Array.Empty<CharacterState>();
        private static object? original;
        private static int Id(CharacterState actor) => FullBattleTrace.Active!.UnitId(actor);
        private static object Rng() => Enum.GetValues(typeof(RngId)).Cast<RngId>().Select(id =>
            new { Id = id.ToString(), Words = RngCalibration.Words(RandomManager.GetState(id)) }).ToArray();
        private static object Actors(IEnumerable<CharacterState> source) => source.Select(actor => new
        {
            Id = Id(actor), Room = actor.GetCurrentRoomIndex(), Health = actor.GetHP(), MaxHealth = actor.GetMaxHP(),
            AssetKey = actor.GetSourceCharacterData().GetAssetKey(), TrueFinalBoss = actor.IsTrueFinalBoss(), OuterBoss = actor.IsOuterTrainBoss(),
            LinkedId = actor.GetLinkedCharacterState() == null ? 0 : Id(actor.GetLinkedCharacterState())
        }).ToArray();
        internal static void Arm(IReadOnlyList<CharacterState> created)
        { if (Enabled) { actors = created.ToArray(); original = Actors(actors); } }
        internal static void Tick()
        {
            if (!Enabled || Completed || Error != null) return;
            AllGameManagers managers = AllGameManagers.Instance;
            CombatManager combat = managers.GetCombatManager();
            if (started) { if (Time.realtimeSinceStartup - startedAt > 60f) Error = "Native controlled death coroutine did not finish."; return; }
            if (actors.Count != 5 || combat.GetCombatPhase() != CombatManager.Phase.MonsterTurn ||
                !combat.HaveCardsBeenDrawnForCurrentTurn() || combat.IsRunningTriggerQueue || !CardAnimationSettlement.Ready()) return;
            started = true; startedAt = Time.realtimeSinceStartup;
            combat.StartCoroutine(Run());
        }
        private static IEnumerator Run()
        {
            AllGameManagers managers = AllGameManagers.Instance;
            CombatManager combat = managers.GetCombatManager();
            int index = Array.IndexOf(new[] { "Dominion", "Savagery", "EntropyBot", "EntropyMid", "EntropyTop" }, Target);
            if (index < 0) { Error = "Unknown native controlled death target."; yield break; }
            object rngBefore = Rng();
            // Explicit nonterminal control on original actors; no resource or
            // character definition replacement and no naturally earned victory.
            foreach (CharacterState actor in actors) actor.DebuffHP(actor.GetHP() - 1);
            object controlled = Actors(actors);
            bool protectedTarget = StatusEffectLifeLinkState.ProtectedByLifeLink(actors[index], managers.GetHeroManager(), managers.GetMonsterManager(), combat);
            if (protectedTarget || actors.Any(actor => actor.GetHP() != 1)) { Error = "Controlled terminal setup did not remove life-link protection."; yield break; }
            observing = true;
            yield return combat.ApplyDamageToTarget(1, actors[index], new CombatManager.ApplyDamageToTargetParameters
                { damageType = Damage.Type.Default, finalEffectInSequence = true });
            object afterDamage = new { Actors = Actors(actors),
                FinishedDying = actors.Where(actor => actor.HasFinishedDying).Select(Id).ToArray(),
                Active = managers.GetHeroManager().GetCharactersReadOnly().Select(Id).ToArray(), Roots = roots.Count, Steps = steps.Count,
                AllScenarioBossesDead = combat.AllScenarioBossesDead };
            yield return combat.RemoveDeadCharacters();
            observing = false;
            if (roots.Count != 1 || steps.Count != 5 || managers.GetHeroManager().GetCharactersReadOnly().Any(actor => actor.IsTrueFinalBoss()))
            { Error = "Controlled death did not complete exactly one native five-actor removal group."; yield break; }
            using (var document = NativeFixtureCapture.Capture(new
            {
                Schema = 1, GameModuleMvid = typeof(CharacterState).Assembly.ManifestModule.ModuleVersionId,
                Boundary = "ControlledOriginalTrueFinalBossLethalDamageAndRemoval", Setup = "OriginalDebuffHPToOne",
                Target = Target, TargetId = Id(actors[index]), Original = original, Controlled = controlled,
                TargetProtected = protectedTarget, NativeDamageReturned = true, AfterDamageBeforeRemoval = afterDamage,
                NativeRemovalReturned = true, Roots = roots, Steps = steps,
                ActiveAfter = managers.GetHeroManager().GetCharactersReadOnly().Select(Id).ToArray(),
                AllScenarioBossesDead = combat.AllScenarioBossesDead, NativeWon = FullBattleTrace.Active!.NativeWon,
                PyreHealth = managers.GetPlayerManager().GetTowerHP(), RngBeforeControl = rngBefore, RngAfterDamage = Rng()
            }))
            using (var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "true-final-boss-death-" + Target.ToLowerInvariant() + ".mt2f"))) document.Write(output);
            Completed = true;
        }
        private static IEnumerator Root(IEnumerator native, CharacterState actor, List<CharacterState> activeList,
            bool death, bool vanishUnit, bool isLastDeathInSet, bool ignoreTriggers, bool doNotFullyRemove, bool fireRemovedSignal)
        {
            int start = steps.Count;
            object before = Actors(activeList);
            bool returned = false;
            try { while (native.MoveNext()) yield return native.Current; returned = true; }
            finally
            {
                (native as IDisposable)?.Dispose();
                roots.Add(new { TargetId = Id(actor), Before = before, Death = death, Vanish = vanishUnit,
                    LastDeath = isLastDeathInSet, IgnoreTriggers = ignoreTriggers, DoNotFullyRemove = doNotFullyRemove,
                    FireRemovedSignal = fireRemovedSignal, NativeReturned = returned, StartStep = start, EndStep = steps.Count,
                    ActiveAfter = activeList.Select(Id).ToArray() });
            }
        }
        [HarmonyPatch(typeof(CharacterState), "RemoveTrueFinalBoss")]
        private static class RootPatch
        {
            private static void Postfix(ref IEnumerator __result, CharacterState __instance, List<CharacterState> activeList,
                bool death, bool vanishUnit, bool isLastDeathInSet, bool ignoreTriggers, bool doNotFullyRemove, bool fireRemovedSignal)
            {
                if (observing) __result = Root(__result, __instance, activeList, death, vanishUnit, isLastDeathInSet, ignoreTriggers, doNotFullyRemove, fireRemovedSignal);
            }
        }
        [HarmonyPatch(typeof(CharacterState), "RemoveCharacterInner")]
        private static class InnerPatch
        {
            private static void Prefix(CharacterState __instance, List<CharacterState> activeList, bool death, bool vanishUnit,
                bool isLastDeathInSet, bool ignoreTriggers, bool doNotFullyRemove, bool fireRemovedSignal)
            {
                if (!observing) return;
                steps.Add(new { Id = Id(__instance), Room = __instance.GetCurrentRoomIndex(), ActiveBefore = activeList.Select(Id).ToArray(),
                    Death = death, Vanish = vanishUnit, LastDeath = isLastDeathInSet, IgnoreTriggers = ignoreTriggers,
                    DoNotFullyRemove = doNotFullyRemove, FireRemovedSignal = fireRemovedSignal });
            }
        }
    }
}
