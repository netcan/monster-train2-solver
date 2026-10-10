using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal sealed class BattleTurnProbe
    {
        private readonly FullBattleTrace trace;
        private readonly EnemySpawningProbe spawning;
        private readonly CardCycleProbe cardCycles;
        private readonly UnitPlayModelProbe projection;
        private readonly ManualLogSource log;
        private readonly List<Record> records = new List<Record>();
        internal IReadOnlyList<Record> Records => records;
        internal int Mismatches => records.Count(record => record.Difference != null);
        internal int Unsupported => records.Count(record => !record.Predicted.Supported);
        internal BattleTurnProbe(FullBattleTrace trace, EnemySpawningProbe spawning, CardCycleProbe cardCycles,
            UnitPlayModelProbe projection, ManualLogSource log)
        { this.trace = trace; this.spawning = spawning; this.cardCycles = cardCycles; this.projection = projection; this.log = log; }

        internal BattleTurnState Capture()
            => trace.CaptureDecision(CaptureState);

        private BattleTurnState CaptureState()
        {
            AllGameManagers managers = AllGameManagers.Instance!;
            SaveManager save = managers.GetSaveManager();
            CardManager cards = managers.GetCardManager()!;
            CombatManager combat = managers.GetCombatManager()!;
            PlayerManager player = managers.GetPlayerManager();
            var standby = (Dictionary<CardState, RemoveFromStandByCondition>)AccessTools.Field(typeof(CardManager), "pileStandBy").GetValue(cards);
            var otherPiles = new[]
            {
                StandbyPileProbe.Capture(standby, projection),
                new CardPileState("DiscardBuffer", projection.CaptureCards(cards.GetDiscardBufferPile())),
                new CardPileState("Exhausted", projection.CaptureCards(cards.GetExhaustedPile())),
                new CardPileState("Purged", projection.CaptureCards(cards.GetPurgedPile())),
                new CardPileState("Eaten", projection.CaptureCards(cards.GetEatenPile()))
            };
            var interactions = new List<string>();
            interactions.AddRange(cardCycles.Capture(cards, "Draw").ExternalInteractions);
            interactions.AddRange(cardCycles.Capture(cards, "Discard").ExternalInteractions);
            foreach (CardState card in cards.GetHand().Concat(cards.GetDrawPile()).Concat(cards.GetDiscardPile()))
            {
                if (card.TriggersOnUnplayed()) interactions.Add("Unplayed card triggers: " + card.GetCardDataID());
                if (card.GetTraitStates().Any(trait => trait.GetType().GetMethod("OnPreOpeningHand",
                    BindingFlags.Public | BindingFlags.Instance)?.DeclaringType != typeof(CardTraitState)))
                    interactions.Add("Opening hand trait callback");
            }
            if (player.GetHasMoonPhaseCardEffect()) interactions.Add("Moon phase unit/card effects");
            var streams = new List<BattleRngStream>();
            foreach (RngId id in Enum.GetValues(typeof(RngId)))
            {
                // BattleTest is used by card legality/UI previews, never live target selection.
                if (id == RngId.NonDeterministic || id == RngId.Chatter || id == RngId.BattleTest) continue;
                var native = (HadesRNG)AccessTools.Method(typeof(RandomManager), "GetRng").Invoke(null, new object[] { id });
                uint[] words = RngCalibration.Words(native.GetState());
                streams.Add(new BattleRngStream(id.ToString(), native.GetSeed(),
                    new UnityRng(words[0], words[1], words[2], words[3])));
            }
            EnemySpawnState spawn = spawning.Capture();
            return new BattleTurnState(spawn, player.GetEnergy(), save.GetBalanceData().GetStartOfTurnEnergy(),
                combat.GetStartOfTurnCards(), save.GetForgePoints(), save.GetDragonsHoardAmount(), player.CurrentMoonPhase.ToString(),
                streams, otherPiles, interactions.Distinct().OrderBy(value => value, StringComparer.Ordinal).ToArray(), trace.CapturePlayRules(spawn),
                save.GetBattlePreviewEnabled(), UiRngIsolation.Enabled, canonicalDecisionReferences: true,
                canonicalPhysicalReferences: BattleSpawnPointProbe.Enabled, selectedRoom: managers.GetRoomManager()!.GetSelectedRoom());
        }

        internal void Begin()
        {
            try
            {
                if (records.Any(record => record.Actual == null)) throw new InvalidOperationException("Previous EndTurn is still pending.");
                BattleTurnState before = Capture();
                var record = new Record { Index = records.Count, Before = before, Predicted = BattleTurnModel.EndTurn(before) };
                records.Add(record);
            }
            catch (Exception error) { trace.CaptureFailure(error); }
        }
        internal void Complete(RoomOutcome outcome)
        {
            Record? record = records.LastOrDefault(item => item.Actual == null);
            if (record == null) return;
            try
            {
                record.Actual = Capture(); record.ActualOutcome = outcome;
                if (record.Predicted.Supported)
                {
                    record.Difference = record.Predicted.Outcome != outcome ? "EndTurn outcome differs" :
                        JToken.DeepEquals(Comparable(record.Predicted.State!), Comparable(record.Actual))
                            ? null : "Decision state, resources, card piles or gameplay RNG differ";
                    log.LogInfo("TURN-MODEL-" + (record.Difference == null ? "MATCH" : "MISMATCH") + " index=" + record.Index);
                }
                else log.LogInfo("TURN-MODEL-UNSUPPORTED " + record.Predicted.UnsupportedReason);
            }
            catch (Exception error) { trace.CaptureFailure(error); }
        }
        internal static JToken Comparable(BattleTurnState state) => JToken.FromObject(new
        {
            Rooms = state.Spawn.Train.Rooms.Select(room => new { room.RoomIndex, room.Deployment, room.Units }).ToArray(),
            Movement = state.Spawn.Train.Movement.OrderBy(rule => rule.UnitId).ToArray(), state.Spawn.Train.Context,
            state.Spawn.Phase, state.Spawn.SelectedGroups, state.Spawn.Rng, state.Spawn.NextUnitId,
            state.Spawn.TreasuresRemaining, state.Spawn.Turn, state.Energy, state.ForgePoints, state.DragonsHoard,
            state.MoonPhase, state.RngStreams, state.OtherPiles, state.PlayRules, state.BattlePreviewEnabled, state.UiRngIsolated,
            state.CanonicalPhysicalReferences, state.SelectedRoom
        });
        internal sealed class Record
        {
            public int Index { get; set; }
            public BattleTurnState Before { get; set; } = null!;
            public BattleTurnResult Predicted { get; set; } = null!;
            public BattleTurnState? Actual { get; set; }
            public RoomOutcome ActualOutcome { get; set; }
            public string? Difference { get; set; }
        }
    }
}
