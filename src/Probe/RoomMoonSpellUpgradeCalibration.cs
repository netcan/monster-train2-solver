using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    // Isolated native component; the original five spell definitions and room
    // modifier execute unchanged, including their private cached upgrades.
    internal static class RoomMoonSpellUpgradeCalibration
    {
        private static AllGameManagers Managers => AllGameManagers.Instance!;
        private static FullBattleTrace Trace => FullBattleTrace.Active!;
        private static readonly List<object> rows = new List<object>();
        private static object Rng() => Enum.GetValues(typeof(RngId)).Cast<RngId>()
            .Where(id => id != RngId.NonDeterministic && id != RngId.Chatter && id != RngId.BattleTest)
            .Select(id => new { Id = id.ToString(), Words = RngCalibration.Words(RandomManager.GetState(id)) }).ToArray();
        private static object Snapshot() => new { Train = Trace.CaptureTrain(), Rng = Rng() };
        private static IEnumerator Record(string label, string operation, int value, IEnumerator native)
        {
            object before = Snapshot();
            yield return native;
            yield return Managers.GetCombatManager()!.RunTriggerQueue();
            yield return null;
            rows.Add(new { Label = label, Operation = operation, Value = value, Before = before, Actual = Snapshot() });
        }
        private static IEnumerator Select(int room)
        {
            AccessTools.Method(typeof(RoomUI), "InnerSetSelectedRoom").Invoke(Managers.GetRoomManager()!.GetRoomUI(), new object[] { room, false });
            yield break;
        }
        private static IEnumerator Refresh(CardEffectData definition)
        {
            var state = new CardEffectState(); state.Setup(definition, Team.Type.Monsters, Managers.GetSaveManager());
            yield return state.GetCardEffect().ApplyEffect(state, new CardEffectParams(), Managers.GetCoreManagers(), Managers.GetSystemManagers());
        }
        internal static IEnumerator Run(ManualLogSource log)
        {
            AllGameData data = Managers.GetAllGameData(); CardManager cards = Managers.GetCardManager()!;
            RoomManager rooms = Managers.GetRoomManager()!; PlayerManager player = Managers.GetPlayerManager();
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            cards.AddNewCard(data.FindCardData(MoonPhaseCardScenario.MoonriseId)!, CardPile.HandPile, false, false, animate: false);
            yield return player.HandleMoonPhaseResetMidBattle();
            yield return player.SetMoonPhase(PlayerManager.MoonPhase.Full);
            yield return Managers.GetCombatManager()!.RunTriggerQueue();
            string[] originals = { "108b9153-0236-4ba2-ad20-d03a991bbd57", "753f20a2-c625-47b3-be4b-8600b673841e",
                "8757357b-c36f-476d-88de-b9a481d89fcb", "87e45427-921e-45d3-b4c4-9c156a83cedf", "cbf4d65d-afdb-4fd8-a7ce-216d58a6b486" };
            int[] ids = originals.Select(id => Trace.CardId(cards.AddNewCard(data.FindCardData(id)!, CardPile.HandPile, false, false, animate: false))).ToArray();
            if (RoomMoonCallbackCalibration.SpellAdvance)
                ids = ids.Concat(new[] { Trace.CardId(cards.AddNewCard(data.FindCardData("e9fa09e5-13bd-4826-a8e5-969332cbfbbf")!, CardPile.HandPile, false, false, animate: false)) }).ToArray();
            int[] costIds = RoomMoonCallbackCalibration.SpellCostRelic ?
                new[] { OriginalCardTypeCostScenario.BlightId, OriginalCardTypeCostScenario.JunkId }
                    .Select(id => Trace.CardId(cards.AddNewCard(data.FindCardData(id)!, CardPile.HandPile, false, false, animate: false))).ToArray() : Array.Empty<int>();
            int[] attackIds = RoomMoonCallbackCalibration.SpellAttackRelic ? new[] {
                Trace.CardId(cards.AddNewCard(data.FindCardData(MagneticRelayScenario.StewardId)!, CardPile.HandPile, false, false, animate: false)) } : Array.Empty<int>();
            CardState storm = cards.GetHand().Single(card => Trace.CardId(card) == ids[0]);
            CardUpgradeData numeric = data.GetAllCardUpgradeData().First(item => item.GetBonusDamage() > 0 && item.GetTraitDataUpgrades().Count == 0 &&
                item.GetCardTriggerUpgrades().Count == 0 && item.GetStatusEffectUpgrades().Count == 0);
            var added = new CardUpgradeState(); added.Setup(numeric); storm.ApplyPermanentUpgrade(added, Managers.GetSaveManager(), ignoreUpgradeAnimation: true);
            storm.ReapplyMagicPowerScalingFromTraitsToAllExistingUpgrades(Managers.GetCoreManagers());
            CardData eclipseData = data.FindCardData(EclipseChamberScenario.DataId)!;
            CardState eclipse = cards.AddNewCard(eclipseData, CardPile.HandPile, false, false, animate: false); Trace.CardId(eclipse);
            CardUpgradeData upgrade = eclipseData.GetEffects().Single(effect => effect.GetEffectStateName() == "CardEffectAttachTrainRoomAttachment").GetParamCardUpgradeData();
            CardEffectData resource = upgrade.GetRoomModifierUpgrades().Single(modifier => modifier.GetRoomStateModifierClassName() == "RoomStateNewMoonAlwaysActiveModifier")
                .GetParamCardData()!.GetEffects().Single();
            yield return Select(0);
            yield return Managers.GetCombatManager()!.RunTriggerQueue();
            yield return Record("unmasked-full", "Refresh", 0, Refresh(resource));
            yield return Record("attach-selected-full", "Attach", 0, rooms.GetRoom(0).AddTrainRoomAttachment("poju-spell-room-moon", eclipse,
                upgrade, null!, Team.Type.Monsters, TrainRoomAttachmentType.Primary, Managers.GetCoreManagers(), skipTargeting: true));
            yield return Record("masked-full-resource", "Refresh", 0, Refresh(resource));
            yield return Record("leave-mask-full", "Select", 1, Select(1));
            yield return Record("reenter-mask-full", "Select", 0, Select(0));
            yield return Record("same-selected-mask", "Select", 0, Select(0));
            yield return Record("masked-global-new", "SetMoonPhase", 1, player.SetMoonPhase(PlayerManager.MoonPhase.New));
            yield return Record("leave-mask-new", "Select", 1, Select(1));
            yield return Record("outside-global-full", "SetMoonPhase", 2, player.SetMoonPhase(PlayerManager.MoonPhase.Full));
            yield return Record("reenter-full-again", "Select", 0, Select(0));
            yield return Record("detach-selected-full", "Detach", 0, rooms.GetRoom(0).RemoveTrainRoomAttachment(rooms.GetRoom(0).Attachments.Single(), Managers.GetCoreManagers()));
            yield return Record("detached-resource", "Refresh", 0, Refresh(resource));
            yield return Record("unmasked-global-new", "SetMoonPhase", 1, player.SetMoonPhase(PlayerManager.MoonPhase.New));
            yield return Record("unmasked-selection-control", "Select", 2, Select(2));
            yield return Record("unmasked-global-full", "SetMoonPhase", 2, player.SetMoonPhase(PlayerManager.MoonPhase.Full));
            bool unchanged = rows.All(row => JToken.DeepEquals(JObject.FromObject(row)["Before"]!["Rng"], JObject.FromObject(row)["Actual"]!["Rng"]));
            using (var document = NativeFixtureCapture.Capture(new { Schema = 1, GameVersion = "2.2.1",
                GameModuleMvid = typeof(CardState).Assembly.ManifestModule.ModuleVersionId, NativeCompleted = true, RngUnchanged = unchanged,
                PreviewUpdatesSuppressed = Managers.GetCombatManager()!.SuppressCombatPreviewUpdates, Boundary = "AfterOriginalCoroutineAndTriggerQueue",
                SpellUpgradeControls = true, SpellAdvanceControls = RoomMoonCallbackCalibration.SpellAdvance,
                CostRelicControls = RoomMoonCallbackCalibration.SpellCostRelic, CostControlIds = costIds,
                AttackRelicControls = RoomMoonCallbackCalibration.SpellAttackRelic, AttackControlIds = attackIds,
                ShadeCallbacks = false, MovementControls = false, EnemyControls = false,
                ActorIds = Array.Empty<int>(), CardIds = ids, AttachmentEffect = BattleActionProbe.Definition(eclipseData).Effects[0], Rows = rows }))
            using (var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!,
                RoomMoonCallbackCalibration.SpellAttackRelic ? "room-moon-spell-attack-relic-calibration.mt2f" :
                RoomMoonCallbackCalibration.SpellCostRelic ? "room-moon-spell-cost-relic-calibration.mt2f" :
                RoomMoonCallbackCalibration.SpellAdvance ? "room-moon-spell-advance-calibration.mt2f" : "room-moon-spell-upgrade-calibration.mt2f"))) document.Write(output);
            log.LogInfo("ROOM-MOON-CALLBACK-NATIVE PASS spellUpgrades=true rows=" + rows.Count + " gameplayRngUnchanged=" + unchanged);
        }
    }
}
