#if DEVELOPMENT_PROBE
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalPlacementMutatorsWitness
    {
        private const string SizeId = "af623737-a208-4b85-bce3-e1066b4f4bcd";
        internal const string ArchiveName = "placement-mutators.mt2f";
        internal static bool BlockOnly => Environment.GetEnvironmentVariable("MT2_PROBE_PLACEMENT_MUTATORS_WITNESS") == "block";
        internal static bool Enabled => BlockOnly || Environment.GetEnvironmentVariable("MT2_PROBE_PLACEMENT_MUTATORS_WITNESS") == "1";
        internal static bool Acquired { get; private set; }
        private static object? originalOwners;
        private static object? deployment;

        internal static void Acquire(SaveManager save)
        {
            if (Application.version != "2.3.1" ||
                typeof(SaveManager).Module.ModuleVersionId != new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d") ||
                save.PreviewMode || save.InTestScenario || save.GetGameSequence() == SaveData.GameSequence.InBattle ||
                save.GetCurrentDistance() != 0 || save.GetCovenants().Count != 0 || save.GetMutators().Count != 0)
                throw new InvalidOperationException("Expected an isolated original 2.3.1 opening battle without mutators.");

            var catalog = save.GetAllGameData();
            var block = catalog.FindMutatorData(RelicModel.DeploymentSummonBlockOwnerId)
                ?? throw new InvalidOperationException("Missing original deployment floor restriction.");
            RelicEffectData blockRule = Definition(block, RelicModel.DeploymentSummonBlockOwnerKey, RelicModel.DeploymentSummonBlock);
            if (blockRule.GetParamInt() != RelicModel.DeploymentSummonBlockMask || !blockRule.GetParamBool())
                throw new InvalidOperationException("Original deployment floor mask changed.");
            var owners = new List<object> { Describe(block, blockRule) };
            save.AddMutator(block);
            if (!BlockOnly)
            {
                var size = catalog.FindMutatorData(SizeId) ?? throw new InvalidOperationException("Missing original larger unit cards.");
                RelicEffectData sizeRule = Definition(size, "BigMonsters", nameof(RelicEffectModifyMonsterCapacity));
                if (sizeRule.GetParamInt() != 1 || !sizeRule.GetParamBool() || !string.IsNullOrEmpty(sizeRule.GetParamString()) ||
                    sizeRule.GetParamCharacterSubtype()?.IsNone != true)
                    throw new InvalidOperationException("Original unit card size modifier changed.");
                owners.Add(Describe(size, sizeRule));
                save.AddMutator(size);
            }
            var active = save.GetMutators();
            if (active.Count != owners.Count ||
                active.Single(owner => owner.GetRelicDataID() == RelicModel.DeploymentSummonBlockOwnerId).GetEffects() is not [RelicEffectBlockRoomSummon] ||
                !BlockOnly && active.Single(owner => owner.GetRelicDataID() == SizeId).GetEffects() is not [RelicEffectModifyMonsterCapacity])
                throw new InvalidOperationException("Native runtime did not activate the expected original mutator definitions.");

            originalOwners = owners;
            Acquired = true;

            RelicEffectData Definition(MutatorData owner, string key, string type)
            {
                RelicEffectData effect = owner.GetEffects().Single();
                if (owner.name != key || owner.DisallowInDeploymentPhase || effect.GetEffectClassName() != type ||
                    effect.GetEffectConditions().Count != 0 || !save.IsDlcAvailableWhenStartingRun(owner.GetRequiredDLC()))
                    throw new InvalidOperationException("Unavailable or changed original mutator: " + key);
                return effect;
            }

            static object Describe(MutatorData owner, RelicEffectData effect) => new
            {
                Id = owner.GetID(), Key = owner.name, Type = effect.GetEffectClassName(),
                Parameter = effect.GetParamInt(), Flag = effect.GetParamBool(),
                RequiredDlc = owner.GetRequiredDLC().ToString()
            };
        }

        // Called only at the ordinary driver settlement point on each side of the original deployment EndTurn.
        internal static bool CaptureDecision(AllGameManagers managers, FullBattleTrace trace)
        {
            CombatManager combat = managers.GetCombatManager();
            int turn = combat.GetTurnCount();
            bool placement = combat.IsPlacementPhase;
            if (!Acquired || turn != (deployment == null ? 0 : 1) || placement != (deployment == null))
                throw new InvalidOperationException("Expected the settled original placement and first ordinary decision.");
            IReadOnlyList<RoomPlayRule> rooms = BattleActionProbe.CaptureRoomRules();
            var floors = rooms.Where(room => !room.IsPyre).OrderBy(room => room.RoomIndex).ToArray();
            if (floors.Length != 3 || floors.Any(room => !room.Enabled) ||
                !floors.Select(room => room.SummonBlocked).SequenceEqual(new[] { placement, placement, false }))
                throw new InvalidOperationException("The native room summon restrictions did not follow the original deployment phase.");

            CardManager cards = managers.GetCardManager();
            var hand = cards.GetHand();
            var sizes = hand
                .Where(card => card.GetEffectStates().Any(effect => effect.GetCardEffect() is CardEffectSpawnMonster))
                .Select(card => new
                {
                    DataId = card.GetCardDataID(),
                    OriginalSize = card.GetSize(ignoreTempUpgrade: true), EffectiveSize = card.GetSize(),
                    TemporarySize = card.GetTemporaryCardStateModifiers().GetAdditionalSize().ToArray()
                }).ToArray();
            if ((!BlockOnly && !sizes.Any(row => row.EffectiveSize > row.OriginalSize)) ||
                sizes.Length == 0 || sizes.Any(row => row.EffectiveSize != Math.Min(6, row.OriginalSize + (BlockOnly ? 0 : 1))))
                throw new InvalidOperationException("The original +1 unit card size was not observed on turn " + turn +
                    ": " + string.Join(", ", sizes.Select(row => row.DataId + "=" + row.OriginalSize + "->" + row.EffectiveSize)));

            object decision = new { Turn = turn, IsPlacementPhase = placement, Rooms = rooms, CardSizes = sizes,
                BattleState = BlockOnly ? trace.CaptureDecision() : null };
            if (deployment == null)
            {
                deployment = decision;
                return false;
            }

            using var archive = NativeFixtureCapture.Capture(new
            {
                Schema = 1, Boundary = "NativePlacementFloorAndUnitCardSize", GameVersion = Application.version,
                GameModuleMvid = typeof(SaveManager).Module.ModuleVersionId,
                Owners = originalOwners, Deployment = deployment, Ordinary = decision
            });
            using var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, ArchiveName));
            archive.Write(stream);
            return true;
        }
    }
}
#endif
