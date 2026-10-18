using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalMoreCapacityScenario
    {
        internal const string Name = "original-more-capacity";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        private static int step;
        private static bool actionMark;
        internal static readonly List<Record> Records = new List<Record>();
        private static Record? active;
        internal static object Evidence => new { OwnerId = RelicSingleFloorCapacityModel.DataId,
            AssetKey = RelicSingleFloorCapacityModel.AssetKey, Floor = 1, Amount = 2, CardIds };
        internal sealed class Snapshot
        {
            public RoomCapacityState[] Rooms { get; set; } = null!;
            public CombatRelicState[] Relics { get; set; } = null!;
            public BattleResourceState Resources { get; set; } = null!;
            public UnityRng BattleRng { get; set; }
            public int SelectedRoom { get; set; }
            public int Turn { get; set; }
            public bool Preview { get; set; }
            internal CombatContext Context() => new(new([], [], [], BattleRng, 0, []), BattleRng, 0, 1,
                Resources.MaxHandSize, relics: Relics, resources: Resources, roomCapacities: Rooms,
                queryFrame: new(turn: Turn, selectedRoomIndex: SelectedRoom),
                energyState: new(Resources.MaxEnergy, 0, 0, "MonsterTurn", true));
        }
        internal sealed class Record
        {
            public string Kind { get; set; } = "";
            public Snapshot Before { get; set; } = null!;
            public Snapshot? After { get; set; }
            public bool Result { get; set; }
            public bool Completed { get; set; }
            public int Notifications { get; set; }
            public string? Difference { get; set; }
            public string? UnsupportedReason { get; set; }
            public string? Error { get; set; }
        }
        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            if (Acquired || save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("MoreCapacity requires one pre-battle acquisition.");
            AllGameData data = save.GetAllGameData();
            CollectableRelicData relic = data.GetAllCollectableRelicData().Single(item => item.GetID() == RelicSingleFloorCapacityModel.DataId);
            Check(relic);
            if (save.GetCollectedRelics().Any(owner => owner.GetRelicDataID() == relic.GetID()))
                throw new InvalidOperationException("MoreCapacity already acquired.");
            CardData steward = save.GetDeckState().Select(card => data.FindCardData(card.GetCardDataID()))
                .First(card => card?.GetSpawnCharacterData()?.name.StartsWith("TrainSteward", StringComparison.Ordinal) == true)!;
            Set(steward.GetSpawnCharacterData()!, "health", 80); Set(steward.GetSpawnCharacterData()!, "attackDamage", 30);
            Set(steward.GetSpawnCharacterData()!, "size", 1); Set(steward, "cost", 0);
            save.GetDeckState().Clear();
            for (int i = 0; i < 8; i++) save.AddCardToDeck(steward, applyExistingRelicModifiers: false, showAnimation: false, notifyDeckCount: false);
            save.AddRelic(relic); Acquired = true;
            log.LogInfo("ORIGINAL-MORE-CAPACITY-ACQUIRED unchanged floor-one +2 owner; eight size-one Steward carriers.");
        }
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (Prepared) return;
            Check(managers.GetSaveManager().GetAllGameData().GetAllCollectableRelicData().Single(item => item.GetID() == RelicSingleFloorCapacityModel.DataId));
            if (!Acquired || managers.GetCombatManager()!.GetTurnCount() != 0 ||
                Records.Count(row => row.Kind == "Apply" && row.Completed) != 1 ||
                managers.GetRoomManager()!.GetRoom(1).GetCapacityInfo(Team.Type.Monsters).max != 7)
                throw new InvalidOperationException("MoreCapacity natural deployment callback is missing.");
            CardManager cards = managers.GetCardManager()!;
            foreach (CardState card in cards.GetDrawPile().Where(card => card.GetCardType() == CardType.Monster).ToArray())
                if (!cards.DrawSpecificCard(card, useEffectDelay: false)) throw new InvalidOperationException("Capacity carrier draw failed.");
            CardIds = cards.GetHand().Where(card => card.GetCardType() == CardType.Monster)
                .Take(8).Select(FullBattleTrace.Active!.CardId).ToArray();
            if (CardIds.Length != 8) throw new InvalidOperationException("Missing eight opening capacity carriers.");
            Prepared = true;
            log.LogInfo("ORIGINAL-MORE-CAPACITY-PREPARED natural middle-floor 5->7 capacity; seven middle-floor plays and one ordinary control play.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source, PlayCardAction? fallback)
        {
            if (step >= 7) return fallback;
            PlayCardAction? play = BattleActionModel.EnumerateSupportedPlays(source)
                .FirstOrDefault(item => item.CardInstanceId == CardIds[step] && item.RoomIndex == 1 && item.PlayerPosition == step);
            if (play == null) throw new InvalidOperationException("The next middle-floor capacity witness is unavailable.");
            step++; actionMark = true; return play;
        }
        internal static bool IsScenarioAction(PlayCardAction action)
        {
            if (!actionMark || !CardIds.Take(7).Contains(action.CardInstanceId)) return false;
            actionMark = false; return true;
        }
        private static Snapshot Capture()
        {
            AllGameManagers managers = AllGameManagers.Instance!;
            uint[] words = RngCalibration.Words(RandomManager.GetState(RngId.Battle));
            return new Snapshot { Rooms = RoomCapacityProbe.Capture(managers.GetRoomManager()!), Relics = RelicProbe.Capture(managers),
                Resources = RelicResourceProbe.CaptureResources(managers)!, BattleRng = new(words[0], words[1], words[2], words[3]),
                SelectedRoom = managers.GetRoomManager()!.GetSelectedRoom(), Turn = managers.GetCombatManager()!.GetTurnCount(),
                Preview = managers.GetSaveManager().PreviewMode };
        }
        private static bool Active => Enabled && Acquired && !AllGameManagers.Instance!.GetSaveManager().PreviewMode;
        [HarmonyPatch(typeof(RelicEffectChangeSingleFloorCapacity), nameof(RelicEffectChangeSingleFloorCapacity.TestEffectTurnPhaseTiming))]
        private static class TestPatch
        {
            private static void Prefix(out Record? __state)
            {
                __state = null; if (!Active) return;
                var row = new Record { Kind = "Test" }; __state = row; Records.Add(row);
                try { row.Before = Capture(); } catch (Exception error) { row.Error = error.ToString(); }
            }
            private static void Postfix(bool __result, Record? __state)
            {
                if (__state == null || __state.Error != null) return;
                try {
                    __state.Result = __result; __state.After = Capture(); __state.Completed = true;
                    if (__result != RelicSingleFloorCapacityModel.Test(__state.Before.Context(), __state.Before.Turn) ||
                        !JToken.DeepEquals(JToken.FromObject(__state.Before), JToken.FromObject(__state.After)))
                        __state.Difference = "Native single-floor capacity Test differs.";
                } catch (Exception error) { __state.Error = error.ToString(); }
            }
        }
        [HarmonyPatch(typeof(RelicEffectChangeSingleFloorCapacity), nameof(RelicEffectChangeSingleFloorCapacity.ApplyEffectTurnPhaseTiming))]
        private static class ApplyPatch
        {
            private static void Postfix(ref IEnumerator __result) { if (Active) __result = Wrap(__result); }
        }
        private static IEnumerator Wrap(IEnumerator native)
        {
            var row = new Record { Kind = "Apply" }; Records.Add(row); active = row;
            try { row.Before = Capture(); } catch (Exception error) { row.Error = error.ToString(); }
            try { while (native.MoveNext()) yield return native.Current; row.Completed = true; }
            finally {
                active = null; (native as IDisposable)?.Dispose();
                try {
                    row.After = Capture();
                    if (row.Error == null) {
                        RoomCapacityResult predicted = RelicSingleFloorCapacityModel.Apply(row.Before.Context(), row.Before.Turn);
                        row.UnsupportedReason = predicted.UnsupportedReason;
                        if (predicted.Supported && (!JToken.DeepEquals(JToken.FromObject(predicted.Context!.RoomCapacities!), JToken.FromObject(row.After.Rooms)) ||
                            predicted.Context.QueryFrame!.SelectedRoomIndex != row.After.SelectedRoom ||
                            !row.Before.BattleRng.Equals(row.After.BattleRng) ||
                            !JToken.DeepEquals(JToken.FromObject(row.Before.Resources), JToken.FromObject(row.After.Resources)) ||
                            !JToken.DeepEquals(JToken.FromObject(row.Before.Relics), JToken.FromObject(row.After.Relics))))
                            row.Difference = "Native single-floor capacity Apply differs.";
                    }
                } catch (Exception error) { row.Error = error.ToString(); }
            }
        }
        [HarmonyPatch(typeof(RelicManager), nameof(RelicManager.NotifyRelicTriggered))]
        private static class NotifyPatch
        {
            private static void Prefix(IRelicEffect triggeredEffect) { if (active != null && triggeredEffect is RelicEffectChangeSingleFloorCapacity) active.Notifications++; }
        }
        private static void Check(CollectableRelicData relic)
        {
            RelicEffectData effect = relic.GetEffects().Single();
            if (relic.name != RelicSingleFloorCapacityModel.AssetKey || relic.DisallowInDeploymentPhase ||
                effect.GetEffectClassName() != RelicSingleFloorCapacityModel.Effect || effect.GetParamInt() != 2 ||
                effect.GetParamFloat() != 1f || effect.GetParamInt2() != 0 || effect.GetEffectConditions().Count != 0)
                throw new InvalidOperationException("Original MoreCapacity payload changed.");
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field)!.SetValue(target, value);
    }
}
