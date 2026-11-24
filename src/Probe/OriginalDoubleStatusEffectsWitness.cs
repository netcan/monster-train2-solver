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
    internal static class OriginalDoubleStatusEffectsWitness
    {
        internal const string FileName = "double-status-effects.mt2f";
        private const string OwnerId = "459c7ec3-9c35-4298-8873-bda051605210";
        private const string OwnerKey = "DoubleStatusEffects";
        private const string StewardId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";
        private const string PotionId = "ec0980cf-9f88-4d38-9704-b89042f2f625";
        private const string Status = "spikes";
        private static readonly Guid Module = new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d");
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_DOUBLE_STATUS_EFFECTS") == "1";
        private static object? ownerDefinition;
        internal static bool Acquired => ownerDefinition != null;
        private static CardState? steward;
        private static CardState? potion;
        private static CharacterState? target;
        private static bool addedPotion;
        private static bool observing;
        private static BattleTurnState? before;
        private static PlayCardAction? action;
        private static object[]? queryControls;
        private static int stacksBefore;
        private static int sequence;
        private static readonly List<object> StackQueries = new();
        private static readonly List<ApplicationSample> Applications = new();

        internal static void Acquire(SaveManager save)
        {
            if (Application.version != "2.3.1" || typeof(CardState).Module.ModuleVersionId != Module ||
                save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle ||
                save.GetCurrentDistance() != 0 || save.GetCovenants().Count != 0 || save.GetMutators().Count != 0)
                throw new InvalidOperationException("Expected the pinned original rank-zero Ring 1 run before combat.");
            MutatorData definition = save.GetAllGameData().FindMutatorData(OwnerId)
                ?? throw new InvalidOperationException("Original DoubleStatusEffects mutator was missing.");
            var data = definition.GetEffects().Single();
            if (definition.name != OwnerKey || definition.DisallowInDeploymentPhase ||
                data.GetEffectClassName() != nameof(RelicEffectAddStatusEffectMultiplier) ||
                data.GetParamInt() != 200 || data.GetParamSourceTeam() != Team.Type.None ||
                data.GetTargetMode() != TargetMode.Room || data.GetEffectConditions().Count != 0 ||
                data.GetParamCharacterSubtype()?.IsNone != true || data.GetParamStatusEffects().Any())
                throw new InvalidOperationException("Original DoubleStatusEffects authored payload changed.");
            save.AddMutator(definition);
            var owner = save.GetMutators().Single();
            var effect = owner.GetEffects().Single();
            if (owner.GetRelicDataID() != OwnerId || effect is not RelicEffectAddStatusEffectMultiplier)
                throw new InvalidOperationException("Native DoubleStatusEffects effect did not initialize.");
            ownerDefinition = new
            {
                Id = OwnerId, AssetKey = OwnerKey, Type = effect.GetType().Name, EffectIndex = 0,
                Percent = data.GetParamInt(), SourceTeam = data.GetParamSourceTeam().ToString(),
                TargetMode = data.GetTargetMode().ToString(), RequiredSubtype = data.GetParamCharacterSubtype()?.Key,
                Conditions = data.GetEffectConditions().Count, definition.DisallowInDeploymentPhase, effect.CanApplyInPreviewMode
            };
        }

        internal static bool Tick(FullBattleTrace trace, AllGameManagers managers, ManualLogSource log, Action advanceTurn)
        {
            var save = managers.GetSaveManager();
            var combat = managers.GetCombatManager();
            var rooms = managers.GetRoomManager();
            var cards = managers.GetCardManager();
            if (!Acquired || save.PreviewMode)
                throw new InvalidOperationException("Expected a settled live decision with the original DoubleStatusEffects mutator.");
            if (before == null && rooms.GetSelectedRoom() != 0)
            {
                save.StartCoroutine(rooms.GetRoomUI().SetSelectedRoom(0));
                return false;
            }

            var room = rooms.GetRoom(0);
            if (combat.IsPlacementPhase)
            {
                if (steward == null)
                {
                    steward = cards.GetHand().FirstOrDefault(card => card.GetCardDataID() == StewardId)
                        ?? throw new InvalidOperationException("No original Train Steward is in the native placement hand.");
                    var drop = room.GetFirstEmptyMonsterPoint()
                        ?? throw new InvalidOperationException("No native floor-zero placement point.");
                    Play(cards, steward, drop);
                }
                else
                    advanceTurn();
                return false;
            }
            if (combat.GetTurnCount() != 1 || steward == null)
                throw new InvalidOperationException("Expected the first ordinary decision with the original deployed Train Steward.");
            if (potion == null)
            {
                CardData definition = save.GetAllGameData().FindCardData(PotionId)
                    ?? throw new InvalidOperationException("Original SpikePotion definition was missing.");
                var effect = definition.GetEffects().Single();
                var status = effect.GetParamStatusEffects().Single();
                if (definition.name != "SpikePotion" || definition.GetCardType() != CardType.Spell || definition.GetCost() != 1 ||
                    effect.GetEffectStateName() != nameof(CardEffectAddStatusEffect) || effect.GetTargetMode() != TargetMode.DropTargetCharacter ||
                    status.statusId != Status || status.count != 10)
                    throw new InvalidOperationException("Original SpikePotion authored payload changed.");
                potion = cards.GetHand().FirstOrDefault(card => card.GetCardDataID() == PotionId);
                if (potion == null)
                {
                    potion = cards.AddNewCard(definition, CardPile.HandPile, false, false, animate: false)
                        ?? throw new InvalidOperationException("Original SpikePotion could not reach the native hand.");
                    addedPotion = true;
                    return false;
                }
            }
            if (before == null)
            {
                var allies = new List<CharacterState>();
                room.AddCharactersToList(allies, Team.Type.Monsters);
                target = allies.Single(unit => unit.IsAlive && !unit.IsDestroyed &&
                    unit.GetSourceCharacterData().GetAssetKey() == "TrainStewardBig" &&
                    ReferenceEquals(unit.GetSpawnerCard(), steward));
                var drop = target.GetSpawnPoint() ?? throw new InvalidOperationException("No native SpikePotion target point.");
                stacksBefore = target.GetStatusEffectStacks(Status);
                if (stacksBefore != 0) throw new InvalidOperationException("Expected a live Train Steward with no prior Spikes.");
                queryControls = ObserveQueryControls(managers.GetRelicManager(), target);
                before = trace.CaptureDecision();
                action = new PlayCardAction(trace.CardId(potion), 0, -1, targetUnitId: trace.UnitId(target));
                observing = true;
                Play(cards, potion, drop);
                return false;
            }

            observing = false;
            var after = trace.CaptureDecision();
            int stacksAfter = target!.GetStatusEffectStacks(Status);
            if (cards.GetHand().Contains(potion) || before.Energy - after.Energy != 1 ||
                !Applications.Any(row => !row.Preview && !row.TargetPreview && row.Requested == 10 &&
                    row.BeforeStacks == stacksBefore && row.AfterStacks == stacksAfter) ||
                StackQueries.Count == 0)
                throw new InvalidOperationException("Native SpikePotion did not settle with a paid cast and observed stack callbacks.");
            using var document = NativeFixtureCapture.Capture(new
            {
                Schema = 1, Boundary = "OriginalDoubleStatusEffects", GameVersion = Application.version, GameModuleMvid = Module,
                Owner = ownerDefinition, AddedPotionToHand = addedPotion, PotionId, Status, StacksBefore = stacksBefore,
                StacksAfter = stacksAfter, Before = before, Action = action, After = after, QueryControls = queryControls,
                StackQueries, Applications
            });
            using var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, FileName));
            document.Write(stream);
            log.LogInfo("ORIGINAL-DOUBLE-STATUS-EFFECTS OBSERVED spikes=" + stacksBefore + "->" + stacksAfter +
                " queries=" + StackQueries.Count + " applications=" + Applications.Count);
            return true;
        }

        private static void Play(CardManager cards, CardState card, SpawnPoint point)
        {
            int index = cards.GetHand().IndexOf(card);
            if (index < 0 || !cards.CanPlayHandCard(card, 0, point, null, null, out var error))
                throw new InvalidOperationException("Original controlled card is unavailable: " + card.GetCardDataID());
            if (!cards.PlayCard(index, point, ref error))
                throw new InvalidOperationException("Original controlled card was rejected: " + error);
        }

        private static object[] ObserveQueryControls(RelicManager relics, CharacterState actor)
        {
            var rng = HalfHealthLifecycleCalibration.Rng();
            object[] rows =
            {
                Query("cooldown", null),
                Query("buff", null),
                Query(Status, null),
                Query(Status, actor, permanent: true),
                Query(Status, actor, permanent: true, deferred: true),
                Query(Status, actor, type: typeof(CardEffectTransferAllStatusEffects)),
                Query(Status, actor, adders: false, type: typeof(CardEffectTransferAllStatusEffectsFromUnit))
            };
            if (HalfHealthLifecycleCalibration.Difference(rng, HalfHealthLifecycleCalibration.Rng()) != null ||
                actor.GetStatusEffectStacks(Status) != stacksBefore)
                throw new InvalidOperationException("The original stack query controls changed native RNG or applied Spikes.");
            return rows;

            object Query(string id, CharacterState? recipient, bool permanent = false, bool deferred = false,
                bool adders = true, Type? type = null)
            {
                const int incoming = 3;
                var data = new StatusEffectStackData { statusId = id, count = incoming, fromPermanentUpgrade = permanent };
                int actual = relics.GetStatusEffectStacksModifiedByRelics(data, recipient, type, adders, deferred);
                return new { Status = id, Incoming = incoming, Actual = actual, HasRecipient = recipient != null,
                    Permanent = permanent, Deferred = deferred, IncludeAdders = adders, EffectType = type?.Name };
            }
        }

        internal static void ObserveStackQuery(StatusEffectStackData data, CharacterState? onCharacter, Type? fromEffectType,
            bool includeAdders, bool deferred, int result)
        {
            if (!observing || data.statusId != Status || (onCharacter != null && !OnTarget(onCharacter))) return;
            StackQueries.Add(new
            {
                Order = ++sequence, StatusId = data.statusId, Incoming = data.count, Actual = result, IncludeAdders = includeAdders,
                Deferred = deferred, Permanent = data.fromPermanentUpgrade, EffectType = fromEffectType?.Name,
                Preview = AllGameManagers.Instance.GetSaveManager().PreviewMode, TargetPreview = onCharacter?.PreviewMode,
                TargetTeam = onCharacter?.GetTeamType().ToString(), SameActor = ReferenceEquals(onCharacter, target),
                ExistingStacks = onCharacter?.GetStatusEffectStacks(Status), NativeFrame = Time.frameCount
            });
        }

        private static bool OnTarget(CharacterState? actor) => actor != null &&
            (ReferenceEquals(actor, target) || ReferenceEquals(actor.GetSpawnerCard(), steward));

        private sealed class ApplicationSample
        {
            public int Order { get; set; }
            public int NativeFrame { get; set; }
            public int Requested { get; set; }
            public bool AllowModification { get; set; }
            public bool OverrideImmunity { get; set; }
            public bool Preview { get; set; }
            public bool TargetPreview { get; set; }
            public int BeforeStacks { get; set; }
            public int? AfterStacks { get; set; }
            public int? AfterOrder { get; set; }
        }

        [HarmonyPatch(typeof(CharacterState), nameof(CharacterState.AddStatusEffect), new[] { typeof(string), typeof(int),
            typeof(CharacterState.AddStatusEffectParams), typeof(CharacterState), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool) })]
        private static class ApplyPatch
        {
            private static bool Prepare() => Enabled;

            private static void Prefix(CharacterState __instance, string statusId, int numStacks,
                CharacterState.AddStatusEffectParams addStatusEffectParams, bool allowModification, out ApplicationSample? __state)
            {
                __state = null;
                if (!observing || statusId != Status || !OnTarget(__instance) ||
                    !ReferenceEquals(addStatusEffectParams?.sourceCardState, potion)) return;
                __state = new ApplicationSample
                {
                    Order = ++sequence, NativeFrame = Time.frameCount, Requested = numStacks, AllowModification = allowModification,
                    OverrideImmunity = addStatusEffectParams!.overrideImmunity,
                    Preview = AllGameManagers.Instance.GetSaveManager().PreviewMode,
                    TargetPreview = __instance.PreviewMode, BeforeStacks = __instance.GetStatusEffectStacks(Status)
                };
                Applications.Add(__state);
            }

            private static void Postfix(CharacterState __instance, ApplicationSample? __state)
            {
                if (__state == null) return;
                __state.AfterStacks = __instance.GetStatusEffectStacks(Status);
                __state.AfterOrder = ++sequence;
            }
        }
    }
}
#endif
