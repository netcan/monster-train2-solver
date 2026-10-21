using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    // Component capture only; never installed or started by the player entry.
    internal static class RoomMoonCallbackCalibration
    {
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_ROOM_MOON_CALLBACKS") == "1";
        internal static bool Shade => Enabled && Environment.GetEnvironmentVariable("MT2_PROBE_ROOM_MOON_SHADE") == "1";
        internal static bool Movement => Enabled && Environment.GetEnvironmentVariable("MT2_PROBE_ROOM_MOON_MOVEMENT") == "1";
        internal static bool Started, Completed;
        internal static string? Error;
        private static AllGameManagers Managers => AllGameManagers.Instance!;
        private static FullBattleTrace Trace => FullBattleTrace.Active!;
        private static readonly List<object> rows = new List<object>();
        private static object Rng() => Enum.GetValues(typeof(RngId)).Cast<RngId>()
            .Where(id => id != RngId.NonDeterministic && id != RngId.Chatter && id != RngId.BattleTest)
            .Select(id => new { Id = id.ToString(), Words = RngCalibration.Words(RandomManager.GetState(id)) }).ToArray();
        private static object Snapshot() => new { Train = Trace.CaptureTrain(), Rng = Rng() };
        internal static void Start(ManualLogSource log)
        {
            Started = true;
            Managers.GetCombatManager().SuppressCombatPreviewUpdates = true;
            Managers.GetSaveManager().StartCoroutine(Guard(Run(log), log));
        }
        private static IEnumerator Guard(IEnumerator native, ManualLogSource log)
        {
            var stack = new Stack<IEnumerator>(); stack.Push(native);
            while (stack.Count > 0)
            {
                bool more = false; object? current = null;
                try { more = stack.Peek().MoveNext(); if (more) current = stack.Peek().Current; }
                catch (Exception error) { Error = error.ToString(); log.LogError(Error); }
                if (Error != null) yield break;
                if (!more) { (stack.Pop() as IDisposable)?.Dispose(); continue; }
                if (current is IEnumerator nested) stack.Push(nested);
                else yield return current;
            }
        }
        private static IEnumerator Record(string label, string operation, bool characters, int phase, IEnumerator native,
            int unitId = 0, int amount = 0)
        {
            RoomPlayRule[]? rules = unitId == 0 ? null : Trace.CaptureDecision().PlayRules!.Rooms.ToArray();
            object before = Snapshot();
            yield return native;
            yield return Managers.GetCombatManager().RunTriggerQueue();
            yield return null;
            object actual = Snapshot();
            rows.Add(new { Label = label, Operation = operation, Characters = characters, Phase = phase,
                UnitId = unitId, Amount = amount, Rooms = rules, Before = before, Actual = actual });
        }
        private static IEnumerator Move(string label, CharacterState unit, int amount) =>
            Record(label, "Bump", false, 0, CardEffectBump.Bump(null,
                new CardEffectParams { targets = new List<CharacterState> { unit }, selectedRoom = Managers.GetRoomManager().CurrentSelectedRoom },
                Managers.GetCoreManagers(), amount), Trace.UnitId(unit), amount);
        private static IEnumerator Refresh(string label, CardEffectData definition)
        {
            var state = new CardEffectState(); state.Setup(definition, Team.Type.Monsters, Managers.GetSaveManager());
            yield return Record(label, "Refresh", state.GetParamBool(), 0,
                state.GetCardEffect().ApplyEffect(state, new CardEffectParams(), Managers.GetCoreManagers(), Managers.GetSystemManagers()));
        }
        private static IEnumerator Run(ManualLogSource log)
        {
            var data = Managers.GetAllGameData(); var cards = Managers.GetCardManager();
            var rooms = Managers.GetRoomManager(); var player = Managers.GetPlayerManager();
            cards.AddNewCard(data.FindCardData(MoonPhaseCardScenario.MoonriseId)!, CardPile.HandPile, false, false, animate: false);
            yield return player.HandleMoonPhaseResetMidBattle();
            yield return Managers.GetCombatManager().RunTriggerQueue();
            var actors = new List<CharacterState>();
            // Deliberately create outside-floor actors first: callback order is
            // native creation order rather than room enumeration order.
            foreach (int floor in new[] { 1, 0, 2 })
                yield return Managers.GetMonsterManager().CreateMonsterState(
                    data.FindCardData("2b7a187f-da0b-4c34-b8c5-b18755911b43")!.GetSpawnCharacterData(), null!, floor,
                    unit => { actors.Add(unit); Trace.UnitId(unit); }, SpawnMode.SelectedSlot,
                    rooms.GetRoom(floor).GetMonsterPoint(0), isCardless: true);
            if (Shade)
                foreach (int floor in new[] { 1, 0, 2 })
                    yield return Managers.GetMonsterManager().CreateMonsterState(
                        data.FindCardData("7775c5b5-eed9-4864-9976-9c86896becc1")!.GetSpawnCharacterData(), null!, floor,
                        unit => { actors.Add(unit); Trace.UnitId(unit); }, SpawnMode.SelectedSlot,
                        rooms.GetRoom(floor).GetMonsterPoint(1), isCardless: true);
            yield return player.SetMoonPhase(PlayerManager.MoonPhase.Full);
            yield return Managers.GetCombatManager().RunTriggerQueue();
            CardData eclipseData = data.FindCardData(EclipseChamberScenario.DataId)!;
            CardEffectData refresh = eclipseData.GetEffects().Single(effect => effect.GetEffectStateName() == "CardEffectUpdateMoonPhaseResource");
            CardUpgradeData upgrade = eclipseData.GetEffects().Single(effect => effect.GetEffectStateName() == "CardEffectAttachTrainRoomAttachment").GetParamCardUpgradeData();
            CardEffectData resourceOnly = upgrade.GetRoomModifierUpgrades().Single(modifier =>
                modifier.GetRoomStateModifierClassName() == "RoomStateNewMoonAlwaysActiveModifier").GetParamCardData().GetEffects().Single();
            yield return Refresh("full-noop", refresh);
            yield return player.SetMoonPhase(PlayerManager.MoonPhase.New, shouldTriggerShift: false);
            yield return Refresh("unmasked-full-lost", refresh);
            yield return player.SetMoonPhase(PlayerManager.MoonPhase.Full, shouldTriggerShift: false);
            yield return Refresh("resource-only-preserves-understanding", resourceOnly);
            yield return Refresh("unmasked-full-gained", refresh);
            CardState eclipse = cards.AddNewCard(eclipseData, CardPile.HandPile, false, false, animate: false);
            Trace.CardId(eclipse);
            IEnumerator attach = rooms.GetRoom(0).AddTrainRoomAttachment("poju-room-moon-calibration", eclipse, upgrade, null!,
                Team.Type.Monsters, TrainRoomAttachmentType.Primary, Managers.GetCoreManagers(), skipTargeting: true);
            if (Shade) yield return Record("attach-original-eclipse", "Attach", false, 0, attach);
            else yield return attach;
            yield return Managers.GetCombatManager().RunTriggerQueue();
            yield return Refresh("attached-full-gains-new", refresh);
            yield return Refresh("attached-full-noop", refresh);
            yield return player.SetMoonPhase(PlayerManager.MoonPhase.New, shouldTriggerShift: false);
            yield return Refresh("attached-new-loses-full", refresh);
            yield return Record("masked-global-full", "SetMoonPhase", false, 2, player.SetMoonPhase(PlayerManager.MoonPhase.Full));
            yield return Record("masked-global-new", "SetMoonPhase", false, 1, player.SetMoonPhase(PlayerManager.MoonPhase.New));
            yield return Record("masked-global-same", "SetMoonPhase", false, 1, player.SetMoonPhase(PlayerManager.MoonPhase.New));
            yield return Record("masked-global-full-again", "SetMoonPhase", false, 2, player.SetMoonPhase(PlayerManager.MoonPhase.Full));
            IEnumerator detach = rooms.GetRoom(0).RemoveTrainRoomAttachment(rooms.GetRoom(0).Attachments.Single(), Managers.GetCoreManagers());
            if (Shade) yield return Record("detach-original-eclipse", "Detach", false, 0, detach);
            else yield return detach;
            yield return Managers.GetCombatManager().RunTriggerQueue();
            yield return Refresh("detached-full-loses-new", refresh);
            yield return Refresh("detached-full-noop", refresh);
            if (Movement)
            {
                yield return Record("movement-attach", "Attach", false, 0,
                    rooms.GetRoom(0).AddTrainRoomAttachment("poju-room-moon-movement", eclipse, upgrade, null!,
                        Team.Type.Monsters, TrainRoomAttachmentType.Primary, Managers.GetCoreManagers(), skipTargeting: true));
                yield return Move("full-enters-mask", actors[0], -1);
                yield return Move("full-leaves-mask", actors[0], 1);
                yield return Move("new-enters-via-bump", actors[3], -1);
                yield return Refresh("enter-character-refresh-is-noop", refresh);
                yield return Move("new-leaves-via-bump", actors[3], 1);
                yield return Refresh("resource-refresh-after-bump-is-noop", resourceOnly);
                yield return Refresh("leave-character-refresh-is-noop", refresh);
                yield return Move("native-new-holder-leaves-mask", actors[4], 1);
                yield return Refresh("holder-leave-refresh-is-noop", refresh);
                yield return Move("native-new-holder-reenters-mask", actors[4], -1);
                yield return Refresh("holder-return-refresh-is-noop", refresh);
                yield return player.SetMoonPhase(PlayerManager.MoonPhase.New, shouldTriggerShift: false);
                yield return Managers.GetCombatManager().RunTriggerQueue();
                yield return Move("only-moved-character-refreshes-stale-phase", actors[0], -1);
                yield return Record("restore-full-after-target-only", "SetMoonPhase", false, 2, player.SetMoonPhase(PlayerManager.MoonPhase.Full));
                yield return Move("target-only-return", actors[0], 1);
                yield return Record("movement-global-new", "SetMoonPhase", false, 1, player.SetMoonPhase(PlayerManager.MoonPhase.New));
                yield return Move("global-new-unmasked-control", actors[2], -1);
                yield return Move("global-new-enters-mask-control", actors[2], -1);
                yield return Move("global-new-two-floor-return", actors[2], 2);
                yield return Record("movement-global-full", "SetMoonPhase", false, 2, player.SetMoonPhase(PlayerManager.MoonPhase.Full));
                yield return Record("movement-detach", "Detach", false, 0,
                    rooms.GetRoom(0).RemoveTrainRoomAttachment(rooms.GetRoom(0).Attachments.Single(), Managers.GetCoreManagers()));
                yield return Refresh("movement-reset-full-noop", refresh);
            }
            bool rngUnchanged = rows.All(row => JToken.DeepEquals(JObject.FromObject(row)["Before"]!["Rng"], JObject.FromObject(row)["Actual"]!["Rng"]));
            using (var document = NativeFixtureCapture.Capture(new { Schema = 1, GameVersion = "2.2.1",
                GameModuleMvid = typeof(CardState).Assembly.ManifestModule.ModuleVersionId,
                NativeCompleted = true, RngUnchanged = rngUnchanged, PreviewUpdatesSuppressed = Managers.GetCombatManager().SuppressCombatPreviewUpdates,
                Boundary = "AfterOriginalCoroutineAndTriggerQueue",
                ShadeCallbacks = Shade, MovementControls = Movement, AttachmentEffect = BattleActionProbe.Definition(eclipseData).Effects[0],
                ActorIds = actors.Select(Trace.UnitId).ToArray(), Rows = rows }))
            using (var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!,
                Movement ? "room-moon-movement-calibration.mt2f" : "room-moon-callback-calibration.mt2f"))) document.Write(stream);
            Completed = true; log.LogInfo("ROOM-MOON-CALLBACK-NATIVE PASS rows=" + rows.Count + " gameplayRngUnchanged=" + rngUnchanged);
        }
    }
}
