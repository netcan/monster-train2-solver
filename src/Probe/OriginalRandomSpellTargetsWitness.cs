#if DEVELOPMENT_PROBE
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalRandomSpellTargetsWitness
    {
        private const string OwnerId = "a83bc048-7924-4beb-87bd-00419bda6be0";
        private const string OwnerKey = "AllSpellsRandomTargets";
        private const string TorchId = "f1dbd7d5-74fc-4507-aba1-dc1a8ec9393b";
        private static readonly Guid Module = new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d");
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_RANDOM_SPELL_TARGETS_WITNESS") == "1";
        internal static bool FullBattle => Environment.GetEnvironmentVariable("MT2_PROBE_RANDOM_SPELL_TARGETS_BATTLE") == "1";
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        private static object[]? cases;
        private static object? callbackRngBefore, callbackRngAfter;
        private static CardState? torch;
        private static BattleTurnState? beforeCast;
        private static PlayCardAction? action;
        private static int[]? candidates, actualTargets;
        private static bool addedTorch;

        internal static void Acquire(SaveManager save)
        {
            var definition = save.GetAllGameData().FindMutatorData(OwnerId);
            if (Application.version != "2.3.1" || typeof(CardState).Module.ModuleVersionId != Module ||
                save.PreviewMode || save.GetMutators().Count != 0 ||
                definition == null || definition.name != OwnerKey || definition.DisallowInDeploymentPhase ||
                !definition.GetEffects().Select(effect => effect.GetEffectClassName()).SequenceEqual(new[]
                    { nameof(RelicEffectModifyTargetsRandom), nameof(RelicEffectDisableUndo) }) ||
                definition.GetEffects().Any(effect => effect.GetParamCardFilter()?.name != "OnlySpells" ||
                    effect.GetEffectConditions().Count != 0))
                throw new InvalidOperationException("Original random spell targets mutator definition differs.");
            save.AddMutator(definition);
            if (save.GetMutators().Single().GetRelicDataID() != OwnerId ||
                save.GetMutators().Single().GetEffects() is not [RelicEffectModifyTargetsRandom, RelicEffectDisableUndo])
                throw new InvalidOperationException("Original random spell targets mutator did not initialize.");
            Acquired = true;
        }

        internal static void Prepare(AllGameManagers managers)
        {
            var save = managers.GetSaveManager();
            var all = save.GetAllGameData();
            var relics = managers.GetRelicManager();
            if (!Acquired || managers.GetCombatManager().GetTurnCount() != 0 || save.PreviewMode)
                throw new InvalidOperationException("Expected a settled live deployment with the original mutator.");
            callbackRngBefore = HalfHealthLifecycleCalibration.Rng();
            cases = new[]
            {
                Observe(TorchId, TargetMode.DropTargetCharacter, TargetMode.RandomInRoom),
                Observe("5839cfaa-4784-4ec7-99f0-3dda42b939dc", TargetMode.FrontInRoom, TargetMode.RandomInRoom),
                Observe("fa394e19-4279-4e43-b0b3-75da045fc1eb", TargetMode.Exhaust, TargetMode.Exhaust)
            };
            callbackRngAfter = HalfHealthLifecycleCalibration.Rng();
            if (HalfHealthLifecycleCalibration.Difference(callbackRngBefore, callbackRngAfter) != null)
                throw new InvalidOperationException("Original hand callback advanced native RNG.");
            Prepared = true;

            object Observe(string id, TargetMode authored, TargetMode expected)
            {
                CardData data = all.FindCardData(id) ?? throw new InvalidOperationException("Missing original card " + id);
                var card = new CardState(data, save);
                var before = card.GetEffectStates().Select(effect => effect.GetTargetMode()).ToArray();
                bool wasTargetless = card.GetTargetless();
                relics.ApplyCardAddedToHandRelicEffects(card);
                var after = card.GetEffectStates().Select(effect => effect.GetTargetMode()).ToArray();
                bool targetless = card.GetTargetless();
                if (card.IsSpawnerCard() || card.GetCardType() != CardType.Spell || !before.SequenceEqual(new[] { authored }) ||
                    !after.SequenceEqual(new[] { expected }) || targetless != (authored == TargetMode.Exhaust ? wasTargetless : true))
                    throw new InvalidOperationException("Original hand callback differs for " + data.name);
                return new { DataId = id, AssetKey = data.name, Before = before.Select(mode => mode.ToString()).ToArray(),
                    After = after.Select(mode => mode.ToString()).ToArray(), WasTargetless = wasTargetless, Targetless = targetless };
            }
        }

        internal static bool Tick(FullBattleTrace trace, AllGameManagers managers, ManualLogSource log)
        {
            if (managers.GetCombatManager().GetTurnCount() != 1)
                throw new InvalidOperationException("Expected the first live draw for the original random Torch witness.");
            var save = managers.GetSaveManager();
            var cards = managers.GetCardManager();
            var rooms = managers.GetRoomManager();
            if (torch == null)
            {
                torch = cards.GetHand().FirstOrDefault(card => card.GetCardDataID() == TorchId);
                if (torch == null)
                {
                    CardData definition = save.GetAllGameData().FindCardData(TorchId)
                        ?? throw new InvalidOperationException("Original Torch definition is missing.");
                    torch = cards.AddNewCard(definition, CardPile.HandPile, false, false, animate: false)
                        ?? throw new InvalidOperationException("Original Torch native hand addition failed.");
                    addedTorch = true;
                    return false;
                }
            }
            if (beforeCast == null)
            {
                int roomIndex = rooms.GetSelectedRoom();
                var enemies = new List<CharacterState>();
                rooms.GetRoom(roomIndex).AddCharactersToList(enemies, Team.Type.Heroes);
                candidates = enemies.Where(enemy => enemy.IsAlive && !enemy.IsDestroyed).Select(trace.UnitId).ToArray();
                if (candidates.Length < 2 || !torch.GetTargetless() ||
                    torch.GetEffectStates().Single().GetTargetMode() != TargetMode.RandomInRoom)
                    throw new InvalidOperationException("Expected one native randomized Torch and at least two live targets.");
                beforeCast = trace.CaptureDecision();
                action = new PlayCardAction(trace.CardId(torch), roomIndex);
                int handIndex = cards.GetHand().IndexOf(torch);
                if (handIndex < 0 || !cards.CanPlayHandCard(torch, roomIndex, null, null, null, out var error))
                    throw new InvalidOperationException("Native targetless Torch cannot be played.");
                if (!cards.PlayCard(handIndex, null, ref error))
                    throw new InvalidOperationException("Native targetless Torch was rejected: " + error);
                return false;
            }

            if (actualTargets is not [int target] || !candidates!.Contains(target))
                throw new InvalidOperationException("Original Torch did not report exactly one actual damage target.");
            BattleTurnState afterCast = trace.CaptureDecision();
            if (beforeCast.Spawn.Train.Context!.BattleRng.Equals(afterCast.Spawn.Train.Context!.BattleRng))
                throw new InvalidOperationException("Original random Torch cast did not advance Battle RNG.");
            using var archive = NativeFixtureCapture.Capture(new
            {
                Schema = 1, Boundary = "OriginalRandomSpellTargets", GameVersion = Application.version, GameModuleMvid = Module,
                OwnerId, OwnerKey, Cases = cases, CallbackRngBefore = callbackRngBefore, CallbackRngAfter = callbackRngAfter,
                Cast = new { AddedTorch = addedTorch, Before = beforeCast, Action = action, After = afterCast, Candidates = candidates, ActualTargets = actualTargets }
            });
            using var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "random-spell-targets.mt2f"));
            archive.Write(stream);
            log.LogInfo("ORIGINAL-RANDOM-SPELL-TARGETS PASS cases=" + cases!.Length + " candidates=" + candidates.Length + " target=" + target);
            return true;
        }

        [HarmonyPatch(typeof(CardEffectDamage), nameof(CardEffectDamage.ApplyEffect))]
        private static class DamagePatch
        {
            private static bool Prepare() => Enabled;

            private static void Prefix(CardEffectParams cardEffectParams)
            {
                if (beforeCast == null || !ReferenceEquals(cardEffectParams.playedCard, torch) ||
                    AllGameManagers.Instance.GetSaveManager().PreviewMode) return;
                if (actualTargets != null) throw new InvalidOperationException("Original Torch damage callback repeated.");
                actualTargets = cardEffectParams.targets.Select(FullBattleTrace.Active!.UnitId).ToArray();
            }
        }
    }
}
#endif
