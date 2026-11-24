#if DEVELOPMENT_PROBE
using System;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using MonsterTrain2Poju.Capture;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalSkullCollectorsWitness
    {
        private const string OwnerId = "32d52c9d-cc1d-4f01-931a-8c232b687a35";
        private const string OwnerKey = "SkullCollectors";
        private static readonly Guid Module = new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d");
        private static readonly int Tier = Environment.GetEnvironmentVariable("MT2_PROBE_SKULL_COLLECTORS") switch
        {
            "1" => 1, "2" => 2, "3" => 3, _ => 0
        };
        internal static bool Enabled => Tier != 0;
        private static int SettledTurn => Tier == 1 ? 2 : 3;
        private static (int Distance, string Scenario, string StockKey) ExpectedSource => Tier switch
        {
            1 => (1, "Level2BattleJunker", "TreasureBagLvl1Gold"),
            2 => (3, "Level4BattleHunterAttackers", "TreasureBagLvl2Gold"),
            3 => (6, "Level7BattleHordeSilence", "TreasureBagLvl3Gold"),
            _ => throw new InvalidOperationException("Missing native SkullCollectors witness tier.")
        };
        private static object? ownerDefinition;
        internal static bool Acquired => ownerDefinition != null;
        private static CharacterSubstitution? substitutions;
        private static Birth? birth;
        private static EnemySpawningProbe.Record? spawn;

        internal static void Acquire(SaveManager save)
        {
            var expected = ExpectedSource;
            if (Application.version != "2.3.1" || typeof(HeroManager).Module.ModuleVersionId != Module ||
                save.PreviewMode || save.InTestScenario || save.IsRegionRun || save.GetCurrentDistance() != expected.Distance ||
                save.GetCurrentScenarioData()?.name != expected.Scenario ||
                save.GetCovenants().Count != 0 || save.GetMutators().Count != 0)
                throw new InvalidOperationException($"Expected an untouched original rank-zero Ring {expected.Distance + 1} {expected.Scenario} BattleIntro.");
            var definition = save.GetAllGameData().FindMutatorData(OwnerId)
                ?? throw new InvalidOperationException("Missing original SkullCollectors mutator.");
            var effects = definition.GetEffects();
            if (definition.name != OwnerKey || definition.DisallowInDeploymentPhase ||
                !effects.Select(effect => effect.GetEffectClassName()).SequenceEqual(new[]
                {
                    nameof(RelicEffectSubstituteTreasureUnitOnSpawn),
                    nameof(RelicEffectModifyCharacterMaxHealth),
                    nameof(RelicEffectModifyCharacterMaxHealth),
                    nameof(RelicEffectModifyCharacterMaxHealth),
                    nameof(RelicEffectModifyCharacterAttackDamage),
                    nameof(RelicEffectModifyCharacterAttackDamage),
                    nameof(RelicEffectModifyCharacterAttackDamage)
                }) || effects.Any(effect => effect.GetEffectConditions().Count != 0))
                throw new InvalidOperationException("Original SkullCollectors payload changed.");
            substitutions = effects[0].GetParamCharacterSubstitution()
                ?? throw new InvalidOperationException("Original SkullCollectors has no native substitution definition.");
            var orderedMappings = substitutions.Entries.Select(entry => new
            {
                Before = Character(entry.UnitBefore),
                After = Character(entry.UnitAfter)
            }).ToArray();
            if (orderedMappings.Length == 0)
                throw new InvalidOperationException("Original SkullCollectors has no native substitution entries.");
            save.AddMutator(definition);
            var active = save.GetMutators().Single();
            if (active.GetRelicDataID() != OwnerId ||
                !active.GetEffects().Select(effect => effect.GetType().Name)
                    .SequenceEqual(effects.Select(effect => effect.GetEffectClassName())))
                throw new InvalidOperationException("Native SkullCollectors owner did not initialize.");
            ownerDefinition = new
            {
                Id = OwnerId, AssetKey = OwnerKey, definition.DisallowInDeploymentPhase, OrderedMappings = orderedMappings,
                Effects = effects.Select(effect => new { Type = effect.GetEffectClassName(), Amount = effect.GetParamInt(),
                    Team = effect.GetParamSourceTeam().ToString(), Target = effect.GetTargetMode().ToString(),
                    Subtype = effect.GetParamCharacterSubtype()?.Key }).ToArray()
            };
        }

        internal static void ObserveCreation(int actorId, CharacterData requested, CharacterData? original, CharacterState actor, CharacterData resolved)
        {
            if (!Acquired || birth != null || original == null || ReferenceEquals(original, requested) ||
                original.GetAssetKey() != ExpectedSource.StockKey ||
                !substitutions!.Entries.Any(entry => ReferenceEquals(entry.UnitBefore, original) &&
                    ReferenceEquals(entry.UnitAfter, requested))) return;
            birth = new Birth(actorId, original, requested, resolved, actor.GetOriginalCharacterData(), actor.GetSourceCharacterData());
        }

        internal static void ObserveSpawn(EnemySpawningProbe.Record record)
        {
            if (birth == null || spawn != null || !record.IncludeTreasure || record.Before.Turn != SettledTurn - 1 ||
                record.Actual == null || record.Before.TreasuresRemaining != record.Actual.TreasuresRemaining + 1 ||
                record.Before.Train.Rooms.Any(room => room.Units.Any(unit => unit.Id == birth.ActorId)) ||
                !record.Actual.Train.Rooms.Any(room => room.Units.Any(unit => unit.Id == birth.ActorId))) return;
            spawn = record;
        }

        internal static bool Tick(FullBattleTrace trace, AllGameManagers managers, ManualLogSource log, Action advanceTurn)
        {
            if (!Acquired) throw new InvalidOperationException("Original SkullCollectors was missing at the first native decision.");
            int turn = managers.GetCombatManager().GetTurnCount();
            if (turn < SettledTurn)
            {
                advanceTurn();
                return false;
            }
            if (turn != SettledTurn || birth == null || spawn?.Actual == null ||
                !ReferenceEquals(birth.Original, birth.ActorOriginal) ||
                !ReferenceEquals(birth.Requested, birth.Resolved) ||
                !ReferenceEquals(birth.Resolved, birth.ActorSource))
                throw new InvalidOperationException("Expected one native substituted first treasure and its settled spawn state.");
            using var archive = NativeFixtureCapture.Capture(new
            {
                Schema = 1, Boundary = "OriginalSkullCollectorsTreasureBirth", GameVersion = Application.version,
                GameModuleMvid = Module, Owner = ownerDefinition,
                Birth = new { birth.ActorId, Original = Character(birth.Original), Requested = Character(birth.Requested),
                    Used = Character(birth.Resolved), ActorOriginal = Character(birth.ActorOriginal), ActorSource = Character(birth.ActorSource) },
                Spawn = new { spawn.IncludeTreasure, spawn.Rooms, spawn.Before, spawn.Actual },
                FirstDecision = trace.CaptureDecision()
            });
            using var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "skull-collectors.mt2f"));
            archive.Write(stream);
            log.LogInfo("ORIGINAL-SKULL-COLLECTORS OBSERVED " + birth.Original.GetAssetKey() + " -> " +
                birth.Resolved.GetAssetKey() + " actor=" + birth.ActorId);
            return true;
        }

        private static object Character(CharacterData data) => new { Id = data.GetID(), AssetKey = data.GetAssetKey(),
            Attack = data.GetAttackDamage(), Health = data.GetHealth(), Size = data.GetSize() };

        private sealed class Birth
        {
            internal Birth(int actorId, CharacterData original, CharacterData requested, CharacterData resolved,
                CharacterData actorOriginal, CharacterData actorSource) =>
                (ActorId, Original, Requested, Resolved, ActorOriginal, ActorSource) =
                (actorId, original, requested, resolved, actorOriginal, actorSource);
            internal int ActorId { get; }
            internal CharacterData Original { get; }
            internal CharacterData Requested { get; }
            internal CharacterData Resolved { get; }
            internal CharacterData ActorOriginal { get; }
            internal CharacterData ActorSource { get; }
        }
    }
}
#endif
