#if DEVELOPMENT_PROBE
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using BepInEx.Logging;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    // Development-only driver: observe a rejected candidate without inventing
    // its expected successor or requiring a full terminal battle.
    internal static class UnsupportedBranchObservation
    {
        internal static bool Enabled => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MT2_PROBE_UNSUPPORTED_BRANCH"));
        internal static bool Completed { get; private set; }
        private static FixtureValue request;
        private static FixtureValue[]? prefix;
        internal static bool ObserveTriggeredCorruption => prefix != null && request.GetProperty("Reason").GetString() ==
            "Battle preview floor 2: CorruptionAdded unit callbacks are not modeled.";
        internal static bool ObserveOrdinaryBossAuraPreview => prefix != null && request.GetProperty("Reason").GetString() ==
            "Native ordinary boss aura action outside the temporary preview roster.";
        internal static bool ObserveStandbyOrdering => waiting && rows.Count > 0 && rows.Last().Boundary &&
            request.GetProperty("Reason").GetString() == "Native multi-equipment host sacrifice recycle order.";
        private static BattleTurnState expected = null!;
        private static readonly List<Row> rows = new List<Row>();
        private static readonly List<object> roomAttachmentMovement = new List<object>();
        private static object? abilityCacheWarm;
        private static string? nativeSelectionRejection;
        private static bool waiting;
        private static int readyFrame = -1;
        internal static bool AwaitingSettlement()
        {
            if (!waiting) return false;
            // Unity defers OnDestroy and enchantment unbinding beyond the first
            // ready combat frame. Observe the settled state, as the player does.
            if (readyFrame < 0) readyFrame = UnityEngine.Time.frameCount;
            return UnityEngine.Time.frameCount <= readyFrame + 1;
        }
        private static string Comparable(BattleTurnState state)
        {
            var value = (JObject)BattleTurnProbe.Comparable(state);
            // Older observation requests omitted inactive understanding masks.
            // Their native default is MoonPhase.None (4). Keep raw records intact.
            foreach (JProperty property in value.Descendants().OfType<JProperty>()
                .Where(property => property.Name == "MoonPhaseMask" && property.Value.Type == JTokenType.Null))
                property.Value = (int)PlayerManager.MoonPhase.None;
            return value.ToString(Newtonsoft.Json.Formatting.None);
        }
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

        private static void Initialize()
        {
            if (prefix != null) return;
            using var document = FixtureDocument.Read(Environment.GetEnvironmentVariable("MT2_PROBE_UNSUPPORTED_BRANCH")!);
            request = document.RootElement;
            Require(request.GetProperty("Schema").GetInt32() == 1 &&
                request.GetProperty("EvidenceKind").GetString() == "ModelUnsupportedBranch", "Expected a rejected model branch.");
            prefix = request.GetProperty("Prefix").EnumerateArray().ToArray();
            Require(prefix.Length <= 256, "Unsupported branch prefix exceeds the input bound.");
            expected = Read<BattleTurnState>(request.GetProperty("Root"));
            Require(expected.Spawn.Turn == 0, "Branch observation must start at native deployment.");
        }

        internal static void Observe(BattleTurnState actual)
        {
            Initialize();
            if (!waiting) return;
            Row row = rows.Last();
            row.Actual = actual; waiting = false;
            if (row.Boundary) row.RoomAttachmentsAfter = CaptureRoomAttachments();
            if (!row.Boundary)
            {
                Require(Comparable(expected) == Comparable(actual), "Native branch prefix successor differs at " + rows.Count);
                return;
            }
            object? equipmentPreviewClear = null;
            object? skillPresentation = null;
            if (request.GetProperty("Reason").GetString() == "Native skill presentation must not create ability cards.")
                skillPresentation = PlayerAdvisorProbe.VerifySkillPresentation(actual,
                    request.GetProperty("SkillPresentationSteps").EnumerateArray().Select(value =>
                        new BattleSearchStep(Read<PlayCardAction?>(value.GetProperty("Action")),
                            value.GetProperty("IsEndTurn").GetBoolean())).ToArray());
            if (request.GetProperty("Reason").GetString() == "Native equipment preview clears the card-side owner while unit membership persists.")
            {
                FullBattleTrace trace = FullBattleTrace.Active!;
                CardState card = trace.KnownCards.Single(item => trace.CardId(item) == row.Action!.CardInstanceId);
                CharacterState owner = trace.KnownUnits.Single(item => item.GetNonPreviewEquipment().Contains(card));
                int rawBefore = card.EquippedCharacter == null ? 0 : trace.UnitId(card.EquippedCharacter);
                // Reproduce the original UI cleanup through the game's API,
                // after the complete natural action prefix has settled.
                AllGameManagers.Instance.GetCombatManager().PreviewCardPlay(false, card, owner.GetCurrentRoom(), null, null, null, null);
                BattleTurnState after = PlayerAdvisorProbe.Capture();
                equipmentPreviewClear = new { Before = actual, After = after, RawOwnerBefore = rawBefore,
                    RawOwnerAfter = card.EquippedCharacter == null ? 0 : trace.UnitId(card.EquippedCharacter),
                    EquipmentOwnerId = trace.UnitId(owner), StillAttached = owner.GetNonPreviewEquipment().Contains(card) };
                Require(Comparable(actual) == Comparable(after), "Original equipment preview cleanup changed the captured decision.");
            }
            using var document = NativeFixtureCapture.Capture(new { Schema = 1, EvidenceKind = "NativeUnsupportedBranchObservation",
                GameModuleMvid = typeof(CardState).Module.ModuleVersionId.ToString(),
                Request = Read<JObject>(request), NativeCompleted = true, Rows = rows,
                NativeSelectionRejection = nativeSelectionRejection,
                PlayerAdvisor = PlayerAdvisorProbe.Evidence,
                EquipmentOwnerRecoveries = FullBattleTrace.Active!.EquipmentOwnerRecoveries.ToArray(),
                EquipmentPreviewClear = equipmentPreviewClear,
                SkillPresentation = skillPresentation,
                AbilityCacheWarm = abilityCacheWarm,
                StandbyOrdering = StandbyOrderingProbe.Records,
                EnemySummonSignatures = CaptureEnemySummonSignatures(),
                TripleJuiceCalibration = JuiceTraitProbe.TripleSamples,
                FloorTransitionPiercing = CaptureFloorTransitionPiercing(),
                RoomAttachmentMovement = roomAttachmentMovement.ToArray(),
                TriggeredCorruption = TriggeredCorruptionProbe.Records.ToArray(),
                RoomPreCombatRecords = RoomPreCombatProbe.Records.ToArray(),
                CorruptRestrictedState = CorruptRestrictedStateCalibration.Evidence,
                BattleTestRng = BattleTestRngProbe.Records.ToArray(),
                TerminalSpawnPoints = BattleSpawnPointProbe.Decisions.Where(sample => sample.Terminal).ToArray() });
            using var stream = new FileStream(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!,
                "unsupported-branch-native.mt2f"), FileMode.CreateNew, FileAccess.Write);
            document.Write(stream);
            Completed = true;
        }

        // Keep authored targeting alongside new enemy-birth observations. The
        // native effect can ignore some common targeting fields; the model
        // still needs the exact signature before widening its capture guard.
        private static object[] CaptureEnemySummonSignatures() => FullBattleTrace.Active!.KnownUnits
            .Where(unit => unit != null && !unit.IsDestroyed && unit.IsAlive)
            .Select(unit => unit.GetSourceCharacterData()).Where(data => data != null)
            .GroupBy(data => data.GetID()).Select(group => group.First())
            .SelectMany(data => data.GetTriggers().SelectMany(trigger => trigger.GetEffects()
                .Where(effect => effect.GetEffectStateName() == "CardEffectSpawnHero")
                .Select(effect => (object)new {
                    AssetKey = data.GetAssetKey(), Trigger = trigger.GetTrigger().ToString(),
                    TargetMode = effect.GetTargetMode().ToString(), TargetTeam = effect.GetTargetTeamType().ToString(),
                    Character = effect.GetParamCharacterData()?.GetAssetKey(), CharacterId = effect.GetParamCharacterData()?.GetID(),
                    Pool = (effect.GetParamCharacterDataPool() ?? new List<CharacterData>()).Select(item => item.GetAssetKey()).ToArray(),
                    ParamInt = effect.GetParamInt(), ParamBool = effect.GetParamBool(),
                    ShouldTest = effect.GetShouldTest(), FailToCast = effect.GetShouldFailToCastIfTestFails(),
                    CancelSubsequent = effect.GetShouldCancelSubsequentEffectsIfTestFails()
                }))).ToArray();

        internal static bool TryObserveRejection(PlayCardAction action, string error)
        {
            if (!Enabled || !waiting || rows.Count == 0 || !rows.Last().Boundary ||
                !request.TryGetProperty("ExpectedNativeRejection", out var expectedError)) return false;
            Require(expectedError.GetString() == error, "Native rejected the candidate for a different reason: " + error);
            Row row = rows.Last();
            Require(!row.IsEndTurn && row.Action?.CardInstanceId == action.CardInstanceId &&
                row.Action.RoomIndex == action.RoomIndex && action.ActivatorUnitId == 0 && !action.ActivatorRoomIndex.HasValue,
                "Only the requested final hand-card rejection may complete this observation.");
            nativeSelectionRejection = error;
            Observe(PlayerAdvisorProbe.Capture());
            return true;
        }

        private static object? CaptureFloorTransitionPiercing()
        {
            if (request.GetProperty("Reason").GetString() != "Native floor-transition relic damage bypasses Titanskin.") return null;
            AllGameManagers managers = AllGameManagers.Instance!;
            var relics = new List<RelicState>();
            AccessTools.Method(typeof(RelicManager), "GetCurrentRelics").Invoke(managers.GetRelicManager(), new object[] { relics });
            return new {
                BalanceEffects = managers.GetAllGameData().GetBalanceData().GetRelicEffectStateNamesThatDoPiercingDamage().ToArray(),
                Ordinary = DamageHelper.IsPiercingDamage(null, null, managers.GetCoreManagers()),
                Owners = relics.Where(relic => relic.GetEffects().Any(effect => effect is RelicEffectDamageEnemyOnAscend || effect is RelicEffectDamageEnemyOnDescend))
                    .Select(relic => new { DataId = relic.GetRelicDataID(), AssetKey = relic.GetAssetName(),
                        Effects = relic.GetEffects().Select(effect => effect.GetType().Name).ToArray(),
                        Piercing = DamageHelper.IsPiercingDamage(null, relic, managers.GetCoreManagers()) }).ToArray()
            };
        }

        private static object? CaptureRoomAttachments()
        {
            if (request.GetProperty("Reason").GetString() != "Native Pyre trigger registry differs after rolling input52") return null;
            RoomManager rooms = AllGameManagers.Instance!.GetRoomManager()!;
            FullBattleTrace trace = FullBattleTrace.Active!;
            return Enumerable.Range(0, rooms.GetNumRooms()).Select(index => {
                RoomState room = rooms.GetRoom(index);
                return new { RoomIndex = index, room.PreviewMode,
                    Updating = AccessTools.Field(typeof(RoomState), "isUpdatingTrainRoomAttachments").GetValue(room),
                    Heroes = ((List<SpawnPoint>)AccessTools.Property(typeof(SpawnPointGroup), "SpawnPoints").GetValue(
                        AccessTools.Field(typeof(RoomState), "heroSpawnPointGroup").GetValue(room)))
                        .Select(point => point.GetCharacterState()).Where(unit => unit != null).Select(unit => new { Id = trace.UnitId(unit),
                        unit.PreviewMode, RoomIndex = unit.GetCurrentRoomIndex(), unit.IsAlive }).ToArray(),
                    Attachments = room.Attachments.Select(attachment => new { attachment.Guid,
                        Impacted = ((List<CharacterState>)AccessTools.Field(typeof(TrainRoomAttachmentState), "_charactersImpacted")
                            .GetValue(attachment)).Select(unit => trace.UnitId(unit)).ToArray() }).ToArray() };
            }).ToArray();
        }

        internal static IEnumerator TraceRoomAttachmentMovement(RoomState room, IEnumerator operation)
        {
            bool enabled = waiting && rows.Count > 0 && rows.Last().Boundary &&
                request.GetProperty("Reason").GetString() == "Native Pyre trigger registry differs after rolling input52";
            if (enabled) roomAttachmentMovement.Add(new { Phase = "Before", Room = room.GetRoomIndex(), State = CaptureRoomAttachments() });
            while (operation.MoveNext()) yield return operation.Current;
            if (enabled) roomAttachmentMovement.Add(new { Phase = "After", Room = room.GetRoomIndex(), State = CaptureRoomAttachments() });
        }

        internal static PlayCardAction? Choose(BattleTurnState root, ManualLogSource log)
        {
            Initialize();
            Require(!waiting && !Completed, "Branch observation consumed an unsettled input.");
            if (abilityCacheWarm == null && request.TryGetProperty("WarmAbilityBeforeInput", out FixtureValue warmIndex) &&
                rows.Count == warmIndex.GetInt32())
            {
                BattleTurnState before = root;
                string id = request.GetProperty("WarmAbilityDataId").GetString()!;
                AllGameManagers managers = AllGameManagers.Instance!;
                CardData definition = managers.GetAllGameData().FindCardData(id)!;
                Require(definition != null && AbilityCardProbe.Existing(definition) == null, "Expected original cold ability definition.");
                UnitOrRoomAbilityCardStateCache.Instance.Get(definition, managers.GetRelicManager(), managers.GetSaveManager());
                root = PlayerAdvisorProbe.Capture();
                Require(AbilityCacheAdviceReuse.TryWarm(before, root, out BattleTurnState warmed) &&
                    Comparable(warmed) == Comparable(root), "Native cache warming differs from the modeled transition.");
                expected = warmed;
                abilityCacheWarm = new { BeforeInput = rows.Count, Before = before, After = root, DataId = id };
            }
            bool boundary = rows.Count == prefix!.Length;
            Require(rows.Count <= prefix.Length, "Branch input overflow.");
            Require(Comparable(root) == Comparable(expected), "Native branch prefix root differs at " + rows.Count + ": " +
                UnitPlayModelProbe.FirstDifference(BattleTurnProbe.Comparable(expected), BattleTurnProbe.Comparable(root), "$"));
            if (boundary)
            {
                var failedRoot = Read<BattleTurnState>(request.GetProperty("FailedRoot"));
                Require(Comparable(root) == Comparable(failedRoot), "Native branch did not reach the recorded failure root: " +
                    UnitPlayModelProbe.FirstDifference(BattleTurnProbe.Comparable(failedRoot), BattleTurnProbe.Comparable(root), "$"));
            }
            FixtureValue value = boundary ? request : prefix[rows.Count];
            PlayCardAction? action = Read<PlayCardAction?>(value.GetProperty("Action"));
            bool end = value.TryGetProperty("IsEndTurn", out FixtureValue endValue) && endValue.GetBoolean();
            Require(end == (action == null), "Invalid branch input.");
            if (boundary) CorruptRestrictedStateCalibration.Capture(action);
            BattleTurnState? predicted;
            string? reason;
            RoomOutcome outcome;
            if (end)
            {
                BattleTurnResult result = BattleTurnModel.EndTurn(root);
                predicted = result.State; reason = result.UnsupportedReason; outcome = result.Outcome;
            }
            else
            {
                try
                {
                    BattleActionResult result = BattleActionModel.PlayCard(root, action!);
                    predicted = result.State; reason = result.Reason; outcome = result.Outcome;
                    Require(result.Rejection != ActionRejection.Illegal || boundary &&
                        request.TryGetProperty("ExpectedNativeRejection", out var nativeError) && nativeError.ValueKind == FixtureKind.String,
                        "Native diagnostic action is illegal: " + reason);
                }
                catch (InvalidOperationException error) when (boundary &&
                    error.Message == "Triple Juice with this card or ordered scaling traits requires native calibration.")
                {
                    // This composition can be discovered partway through a room
                    // focus callback. Preserve its original model refusal while
                    // observing only the requested final native input. Prefixes
                    // and unrelated exceptions still fail the probe.
                    predicted = null; reason = error.Message; outcome = RoomOutcome.Unsupported;
                }
            }
            Require(boundary || predicted != null && outcome == RoomOutcome.Exchanged,
                "Branch prefix is unsupported or terminal: " + reason);
            string[] targetDeathOnceCallbacks = Array.Empty<string>();
            if (boundary && action?.TargetUnitId > 0)
            {
                FullBattleTrace trace = FullBattleTrace.Active!;
                CharacterState target = trace.KnownUnits.Single(unit => trace.UnitId(unit) == action.TargetUnitId);
                object primary = AccessTools.Property(typeof(CharacterState), "PrimaryStateInformation").GetValue(target);
                object signal = AccessTools.Field(primary.GetType(), "deathSignal").GetValue(primary);
                var once = (Delegate?)AccessTools.Field(signal.GetType(), "OnceListener").GetValue(signal);
                targetDeathOnceCallbacks = (once?.GetInvocationList() ?? Array.Empty<Delegate>())
                    .Select(callback => callback.Method.DeclaringType!.Name + "." + callback.Method.Name).ToArray();
            }
            rows.Add(new Row { Before = root, Action = action, IsEndTurn = end, Boundary = boundary,
                Expected = predicted, UnsupportedReason = reason, Outcome = outcome, TargetDeathOnceCallbacks = targetDeathOnceCallbacks,
                RoomAttachmentsBefore = boundary ? CaptureRoomAttachments() : null });
            if (predicted != null) expected = predicted;
            waiting = true; readyFrame = -1;
            log.LogInfo("UNSUPPORTED-BRANCH-INPUT index=" + rows.Count + " boundary=" + boundary + " reason=" + reason);
            return PlayerAdvisorProbe.NativeAction(action);
        }

        private static T Read<T>(FixtureValue value) => PlannerReplayScenario.Read<T>(value);
        internal static IEnumerator ObserveTroop(RoomStateTroopUnitUpgradeModifier modifier, RoomState room, IEnumerator operation)
        {
            bool observing = waiting && rows.Count > 0 && rows.Last().Boundary &&
                request.GetProperty("Reason").GetString() is
                    "Battle preview floor 1: Combined troop upgrades and enchantment source ordering require native calibration." or
                    "Battle preview floor 2: Combined troop upgrades and enchantment source ordering require native calibration." or
                    "Train combat floor 1: Combined troop upgrades and enchantment source ordering require native calibration." or
                    "Train combat floor 2: Combined troop upgrades and enchantment source ordering require native calibration." or
                    "Combined troop upgrades and enchantment source ordering require native calibration.";
            object? before = observing ? TroopSnapshot() : null;
            while (operation.MoveNext()) yield return operation.Current;
            if (observing) rows.Last().TroopUpdates.Add(new { Before = before, After = TroopSnapshot() });

            object TroopSnapshot()
            {
                var trace = FullBattleTrace.Active!;
                var units = new List<CharacterState>();
                room.AddCharactersToList(units, Team.Type.Monsters);
                return new { Preview = AllGameManagers.Instance!.GetSaveManager().PreviewMode, Room = room.GetRoomIndex(),
                    Cache = ((List<CharacterState>)AccessTools.Field(modifier.GetType(), "cachedAffectedTargets")
                        .GetValue(modifier)).Select(trace.UnitId).ToArray(),
                    Units = units.Select(unit => new { Id = trace.UnitId(unit), Asset = unit.GetSourceCharacterData().GetAssetKey(),
                        Attack = unit.GetAttackDamageWithoutStatusEffectBuffs(), Horde = unit.GetStatusEffectStacks("horde"),
                        Health = unit.GetHP(), MaxHealth = unit.GetMaxHP(),
                        Trample = unit.GetStatusEffectStacks("trample"),
                        Upgrades = unit.GetAppliedCardUpgrades().Select(upgrade => upgrade.GetCardUpgradeDataId()).ToArray() }).ToArray() };
            }
        }
        internal static IEnumerator ObserveDamage(CardEffectState effect, CardEffectParams args, IEnumerator operation)
        {
            if (waiting && rows.Count > 0 && rows.Last().Boundary &&
                request.GetProperty("Reason").GetString() == "Native copied Moonbeam damage differs at input27." &&
                !AllGameManagers.Instance!.GetSaveManager().PreviewMode)
            {
                FullBattleTrace trace = FullBattleTrace.Active!;
                CardState card = args.playedCard;
                CardState parent = effect.GetParentCardState();
                rows.Last().DamageEffects.Add(new { CardId = trace.CardId(card), ParentCardId = trace.CardId(parent),
                    Unmodified = effect.GetUnmodifiedParamInt(), Resolved = effect.GetParamInt(),
                    PermanentDamage = card.GetCardStateModifiers().GetCardUpgrades().Select(upgrade =>
                        new { Id = upgrade.GetCardUpgradeDataId(), Damage = upgrade.GetAttackDamage() }).ToArray(),
                    TemporaryDamage = card.GetTemporaryCardStateModifiers().GetCardUpgrades().Select(upgrade =>
                        new { Id = upgrade.GetCardUpgradeDataId(), Damage = upgrade.GetAttackDamage() }).ToArray(),
                    Targets = args.targets.Select(unit => new { Id = trace.UnitId(unit), Health = unit.GetHP() }).ToArray() });
            }
            try { while (operation.MoveNext()) yield return operation.Current; }
            finally { (operation as IDisposable)?.Dispose(); }
        }
        private sealed class Row
        {
            public BattleTurnState Before { get; set; } = null!;
            public PlayCardAction? Action { get; set; }
            public bool IsEndTurn { get; set; }
            public bool Boundary { get; set; }
            public string[] TargetDeathOnceCallbacks { get; set; } = Array.Empty<string>();
            public BattleTurnState? Expected { get; set; }
            public string? UnsupportedReason { get; set; }
            public RoomOutcome Outcome { get; set; }
            public BattleTurnState? Actual { get; set; }
            public object? RoomAttachmentsBefore { get; set; }
            public object? RoomAttachmentsAfter { get; set; }
            public List<object> DamageEffects { get; } = new List<object>();
            public List<object> TroopUpdates { get; } = new List<object>();
        }
    }

    [HarmonyPatch(typeof(RoomState), nameof(RoomState.UpdateTrainRoomAttachments))]
    internal static class RoomAttachmentMovementObservation
    {
        private static void Postfix(RoomState __instance, ref IEnumerator __result)
            => __result = UnsupportedBranchObservation.TraceRoomAttachmentMovement(__instance, __result);
    }
    [HarmonyPatch(typeof(CardEffectDamage), nameof(CardEffectDamage.ApplyEffect))]
    internal static class UnsupportedBranchDamageObservation
    {
        private static void Postfix(CardEffectState cardEffectState, CardEffectParams cardEffectParams, ref IEnumerator __result)
        {
            if (UnsupportedBranchObservation.Enabled)
                __result = UnsupportedBranchObservation.ObserveDamage(cardEffectState, cardEffectParams, __result);
        }
    }
    [HarmonyPatch(typeof(RoomStateTroopUnitUpgradeModifier), nameof(RoomStateTroopUnitUpgradeModifier.OnUnitOrderPossiblyChanged))]
    internal static class UnsupportedBranchTroopObservation
    {
        private static void Postfix(RoomStateTroopUnitUpgradeModifier __instance, RoomState room, ref IEnumerator __result)
        {
            if (UnsupportedBranchObservation.Enabled)
                __result = UnsupportedBranchObservation.ObserveTroop(__instance, room, __result);
        }
    }
}
#endif
