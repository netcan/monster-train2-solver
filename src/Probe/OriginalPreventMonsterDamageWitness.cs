#if DEVELOPMENT_PROBE
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalPreventMonsterDamageWitness
    {
        internal const string OwnerId = RelicModel.PreventMonsterDamageOwnerId;
        internal const string OwnerKey = RelicModel.PreventMonsterDamageOwnerKey;
        internal const string FileName = "prevent-monster-damage.mt2f";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_PREVENT_MONSTER_DAMAGE_WITNESS") == "1";
        internal static bool FullBattle => Environment.GetEnvironmentVariable("MT2_PROBE_PREVENT_MONSTER_DAMAGE_BATTLE") == "1";
        internal static bool Acquired { get; private set; }
        internal static bool Started { get; private set; }
        internal static bool Completed { get; private set; }

        internal static RelicEffectData Acquire(SaveManager save)
        {
            if (save.PreviewMode || save.GetMutators().Count != 0)
                throw new InvalidOperationException("Expected an original live run without active mutators.");
            MutatorData definition = save.GetAllGameData().FindMutatorData(OwnerId)
                ?? throw new InvalidOperationException("Original monster damage prevention owner is missing.");
            RelicEffectData effect = definition.GetEffects().Single();
            if (definition.name != OwnerKey || definition.DisallowInDeploymentPhase ||
                effect.GetEffectClassName() != nameof(RelicEffectPreventMonstersDamagingHeroes) || effect.GetEffectConditions().Count != 0)
                throw new InvalidOperationException("Original monster damage prevention owner changed.");
            save.AddMutator(definition);
            var active = save.GetMutators().Single();
            if (active.GetRelicDataID() != OwnerId || active.GetEffects() is not [RelicEffectPreventMonstersDamagingHeroes])
                throw new InvalidOperationException("Native mutator did not initialize the original effect.");
            Acquired = true;
            return effect;
        }

        internal static void TryStart(AllGameManagers managers, ManualLogSource log)
        {
            if (Started) return;
            var rooms = managers.GetRoomManager();
            var actors = new List<CharacterState>();
            for (int room = 0; room < rooms.GetNumRooms(); room++)
                rooms.GetRoom(room).AddCharactersToList(actors, Team.Type.Monsters | Team.Type.Heroes);
            CharacterState? friendly = actors.FirstOrDefault(unit => unit.IsAlive && !unit.IsDestroyed &&
                unit.GetTeamType() == Team.Type.Monsters && !unit.IsPyreHeart() && unit.GetSpawnerCard() != null);
            CharacterState? enemy = actors.FirstOrDefault(unit => unit.IsAlive && !unit.IsDestroyed &&
                unit.GetTeamType() == Team.Type.Heroes && unit.GetHP() > 2 &&
                unit.GetDamageToTarget(1, null, null, out _) == 1);
            int turn = managers.GetCombatManager().GetTurnCount();
            if (friendly == null || enemy == null)
            {
                if (turn >= 3) throw new InvalidOperationException("Expected live original friendly and enemy units by turn three.");
                return;
            }
            CharacterState pyre = rooms.GetPyreRoom().GetPyreHeart();
            var save = managers.GetSaveManager();
            Guid module = typeof(RelicEffectPreventMonstersDamagingHeroes).Module.ModuleVersionId;
            if (Application.version != "2.3.1" || module != new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d") ||
                save.PreviewMode || save.GetMutators().Count != 0 || pyre == null || !pyre.IsPyreHeart())
                throw new InvalidOperationException("Expected the original 2.3.1 live battle without active mutators.");
            int frame = Time.frameCount;
            object rngBefore = HalfHealthLifecycleCalibration.Rng();
            int[] healthBefore = { friendly.GetHP(), enemy.GetHP(), pyre.GetHP() };
            var relics = managers.GetRelicManager();
            var pairs = new (string Kind, CharacterState? Attacker, CharacterState Target)[]
            {
                ("FriendlyToEnemy", friendly, enemy), ("EnemyToFriendly", enemy, friendly),
                ("PyreToEnemy", pyre, enemy), ("NoAttackerToEnemy", null, enemy), ("FriendlyToFriendly", friendly, friendly)
            };
            var baseline = Measure();
            RelicEffectData effect = Acquire(save);
            var adjusted = Measure();
            object rngAfter = HalfHealthLifecycleCalibration.Rng();
            int frameAfter = Time.frameCount;
            int[] healthAfter = { friendly.GetHP(), enemy.GetHP(), pyre.GetHP() };
            if (frame != frameAfter || HalfHealthLifecycleCalibration.Difference(rngBefore, rngAfter) != null ||
                !healthBefore.SequenceEqual(healthAfter))
                throw new InvalidOperationException("Native damage adjustment query changed actor health, frame or RNG.");
            var rows = baseline.Zip(adjusted, (before, after) => new
            {
                before.Kind, before.Requested, BeforeOwner = before.Adjusted, WithOwner = after.Adjusted,
                AfterStatusHook = after.AfterStatusHook
            }).ToArray();
            if (rows.Any(row => row.BeforeOwner != row.Requested ||
                row.WithOwner != (row.Kind == "FriendlyToEnemy" ? 0 : row.Requested) ||
                row.AfterStatusHook != row.Requested))
                throw new InvalidOperationException("Native signed damage adjustment differs from the scoped original controls.");

            Started = true;
            save.StartCoroutine(RecordLiveDamage());

            IEnumerator RecordLiveDamage()
            {
                var combat = managers.GetCombatManager();
                int health = enemy.GetHP();
                yield return Hit(enemy, friendly);
                int preventedHealth = enemy.GetHP();
                yield return Hit(enemy, null);
                int attackerlessHealth = enemy.GetHP();
                if (preventedHealth != health || attackerlessHealth != health - 1)
                    throw new InvalidOperationException("Original live friendly and attackerless damage controls changed.");

                object? naturalPyregel = null;
                CharacterState? marked = actors.FirstOrDefault(unit => unit.GetTeamType() == Team.Type.Heroes &&
                    unit.IsAlive && !unit.IsDestroyed && unit.GetStatusEffectStacks("pyregel") > 0);
                if (marked != null)
                {
                    int stacks = marked.GetStatusEffectStacks("pyregel"), markedHealth = marked.GetHP();
                    int previewDamage = marked.GetDamageToTarget(1, friendly, null, out _);
                    yield return Hit(marked, friendly);
                    if (marked.GetHP() != markedHealth)
                        throw new InvalidOperationException("Original live friendly damage to the Pyregel target was not prevented.");
                    naturalPyregel = new { Stacks = stacks, PreviewDamage = previewDamage, HealthBefore = markedHealth, HealthAfter = marked.GetHP() };
                }

                using var archive = NativeFixtureCapture.Capture(new
                {
                    Schema = 1, Boundary = "NativeDamageRelicManagerAdjustment", GameVersion = Application.version,
                    GameModuleMvid = module, OwnerId, OwnerKey, Effect = effect.GetEffectClassName(), Turn = turn,
                    FrameBefore = frame, FrameAfter = frameAfter, RngBefore = rngBefore, RngAfter = rngAfter,
                    HealthBefore = healthBefore, HealthAfter = healthAfter,
                    Actors = new[] { friendly, enemy, pyre }.Select(unit => new { DataId = unit.GetSourceCharacterData().GetID(),
                        AssetKey = unit.GetSourceCharacterData().GetAssetKey(), Team = unit.GetTeamType().ToString(), Pyre = unit.IsPyreHeart() }).ToArray(),
                    Rows = rows, LiveDamage = new { Health = health, AfterFriendly = preventedHealth, AfterAttackerless = attackerlessHealth },
                    NaturalPyregel = naturalPyregel
                });
                using var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, FileName));
                archive.Write(stream);
                Completed = true;
                log.LogInfo("ORIGINAL-PREVENT-MONSTER-DAMAGE PASS turn=" + turn + " rows=" + rows.Length);

                IEnumerator Hit(CharacterState target, CharacterState? attacker)
                {
                    yield return combat.ApplyDamageToTarget(1, target, new CombatManager.ApplyDamageToTargetParameters
                    { selfTarget = attacker! });
                    yield return combat.RunTriggerQueue();
                }
            }

            (string Kind, int Requested, int Adjusted, int AfterStatusHook)[] Measure() =>
                (from pair in pairs
                 from damage in new[] { 7, -3 }
                 let before = Adjust(pair.Attacker, pair.Target, damage, beforeStatuses: true)
                 let after = Adjust(pair.Attacker, pair.Target, damage, beforeStatuses: false)
                 select (pair.Kind, damage, before, after)).ToArray();

            int Adjust(CharacterState? attacker, CharacterState target, int damage, bool beforeStatuses)
            {
                var input = new StatusEffectState.InputTriggerParams { attacker = attacker!, attacked = target,
                    damage = damage, unmodifiedDamage = damage };
                var output = new StatusEffectState.OutputTriggerParams { damage = damage };
                if (beforeStatuses) relics.ApplyDamageAdjustmentBeforeStatusEffects(input, output);
                else relics.ApplyDamageAdjustmentAfterStatusEffects(input, output);
                return output.damage;
            }
        }
    }
}
#endif
