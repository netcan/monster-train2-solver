#if DEVELOPMENT_PROBE
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalHandAndEmberWitness
    {
        private const string HandId = "2dd0e035-96a9-46f3-8c52-eacbe82e871b";
        private const string EmberId = "0421d885-09a3-48ec-ad5e-762127ac336e";
        private const string TorchId = "f1dbd7d5-74fc-4507-aba1-dc1a8ec9393b";
        private static readonly Guid Module = new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d");
        private static readonly string? Mode = Environment.GetEnvironmentVariable("MT2_PROBE_HAND_AND_EMBER_WITNESS");
        internal static bool Enabled => Mode is "Combined" or "Stock";
        internal static bool Combined => Mode == "Combined";
        private static bool prepared;
        private static int? endingTurn;
        private static readonly List<PhaseEvent> phases = new List<PhaseEvent>();
        private static Snapshot? deployment, firstDraw, beforePlay, afterPlay;
        private static Input? input;
        private static PlayCardAction? action;

        internal enum Step { Wait, EndTurn, Complete }

        internal static void Select(RunSetupScreen setup, SaveManager save)
        {
            if (Application.version != "2.3.1" || typeof(SaveManager).Module.ModuleVersionId != Module || save.PreviewMode)
                throw new InvalidOperationException("Expected pinned native 2.3.1 Run Setup.");

            var hand = Original(HandId, "HandDoesntDiscard", nameof(RelicEffectChangeStartingHandSize), nameof(RelicEffectSuppressHandDiscard));
            var ember = Original(EmberId, "EmberAtStartOfTurnOnly", nameof(RelicEffectModifyEnergyStartOfCombat),
                nameof(RelicEffectConserveEnergyStartOfTurn), nameof(RelicEffectAddRelicStartOfRun));
            var grant = ember.GetEffects()[2].GetParamRelic();
            if (hand.GetEffects()[0].GetParamInt() != -2 || ember.GetEffects()[0].GetParamInt() != 20 ||
                grant?.GetID() != OriginalConserveEmberScenario.DataId || grant.name != OriginalConserveEmberScenario.AssetKey ||
                grant.GetEffects().Single().GetEffectClassName() != nameof(RelicEffectConserveEnergyEndOfTurn))
                throw new InvalidOperationException("The original hand or Ember mutator payload changed.");

            IReadOnlyList<string> selected = Combined ? new[] { HandId, EmberId } : Array.Empty<string>();
            var select = AccessTools.DeclaredMethod(typeof(RunSetupScreen), "HandleMutatorDialogClosed")
                ?? throw new MissingMethodException(nameof(RunSetupScreen), "HandleMutatorDialogClosed");
            select.Invoke(setup, new object[] { selected });
            prepared = true;

            MutatorData Original(string id, string key, params string[] effects)
            {
                var definition = save.GetAllGameData().FindMutatorData(id);
                if (definition == null || definition.GetID() != id || definition.name != key || definition.DisallowInDeploymentPhase ||
                    !definition.GetEffects().Select(effect => effect.GetEffectClassName()).SequenceEqual(effects) ||
                    definition.GetEffects().Any(effect => effect.GetEffectConditions().Count != 0))
                    throw new InvalidOperationException("Original Run Setup mutator changed: " + key);
                return definition;
            }
        }

        internal static Step Tick(FullBattleTrace trace, AllGameManagers managers)
        {
            var save = managers.GetSaveManager();
            var combat = managers.GetCombatManager()!;
            var rooms = managers.GetRoomManager()!;
            var cards = managers.GetCardManager()!;
            int turn = combat.GetTurnCount();
            endingTurn = null;
            string[] mutators = save.GetMutators().Select(owner => owner.GetRelicDataID()).ToArray();
            string[] relics = save.GetCollectedRelics().Select(owner => owner.GetRelicDataID()).ToArray();
            if (!prepared || save.PreviewMode || save.GetCurrentDistance() != 0 ||
                save.GetRunType() != (Combined ? RunType.Custom : RunType.Class) ||
                !mutators.SequenceEqual(Combined ? new[] { HandId, EmberId } : Array.Empty<string>()) ||
                relics.Count(id => id == OriginalConserveEmberScenario.DataId) != (Combined ? 1 : 0))
                throw new InvalidOperationException("Original Run Setup did not settle the expected mutators and ConserveEmber grant.");

            switch (turn)
            {
                case 0:
                    deployment = Capture();
                    save.MarkOneTimeMessageComplete(OneTimeMessage.EndTurnWithoutPlayingChampion);
                    return Advance();
                case 1 when action == null:
                    firstDraw ??= Capture();
                    input ??= FindInput();
                    if (rooms.GetSelectedRoom() != input.Room)
                    {
                        save.StartCoroutine(rooms.GetRoomUI().SetSelectedRoom(input.Room));
                        return Step.Wait;
                    }
                    int index = cards.GetHand().IndexOf(input.Card);
                    if (index < 0 || !cards.CanPlayHandCard(input.Card, input.Room, input.Point, null, null, out var error))
                        throw new InvalidOperationException("Original paid control is unavailable after room selection.");
                    beforePlay = Capture();
                    action = new PlayCardAction(trace.CardId(input.Card), input.Room,
                        input.Card.IsSpawnerCard() ? input.Point!.GetIndexInRoom() : -1,
                        targetUnitId: input.Target == null ? 0 : trace.UnitId(input.Target));
                    if (!cards.PlayCard(index, input.Point, ref error))
                        throw new InvalidOperationException("Original paid control was rejected: " + error);
                    return Step.Wait;
                case 1:
                    afterPlay = Capture();
                    return Advance();
                case 2:
                    Snapshot secondDraw = Capture();
                    var balance = save.GetBalanceData();
                    var starting = save.GetStartingConditions();
                    int baseEnergy = balance.GetStartOfTurnEnergy(), deploymentExtra = balance.GetStartOfDeploymentPhaseEnergy();
                    int baseDraw = balance.GetStartOfTurnCards(), maximum = balance.GetMaxEnergy();
                    int paid = input!.Card.GetLastPlayedCost();
                    using (var archive = NativeFixtureCapture.Capture(new
                    {
                        Schema = 1, Boundary = "OriginalHandAndEmberNewRun", GameVersion = Application.version,
                        GameModuleMvid = Module, Mode, RunType = save.GetRunType().ToString(),
                        Scenario = save.GetCurrentScenarioData()?.name, starting.Seed, starting.ClassId, starting.SubclassId,
                        Mutators = mutators, Relics = relics,
                        BaseEnergy = baseEnergy, DeploymentExtraEnergy = deploymentExtra, BaseDraw = baseDraw, MaxEnergy = maximum,
                        Deployment = deployment, FirstDraw = firstDraw, BeforePlay = beforePlay, Action = action,
                        PaidCard = input.Card.GetCardDataID(), Paid = paid, AfterPlay = afterPlay, SecondDraw = secondDraw, Phases = phases
                    }))
                    using (var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "hand-and-ember.mt2f")))
                        archive.Write(stream);

                    int expectedDraw = baseDraw - (Combined ? 2 : 0);
                    int expectedDeployment = Math.Min(maximum, baseEnergy + deploymentExtra + (Combined ? 20 : 0));
                    int[] survivorIds = afterPlay!.HandIds;
                    if (paid <= 0 || beforePlay!.Decision.Energy - afterPlay.Decision.Energy != paid ||
                        afterPlay.Decision.Energy >= maximum || survivorIds.Length == 0 ||
                        deployment!.Decision.Energy != expectedDeployment || firstDraw!.Decision.Energy != (Combined ? expectedDeployment : baseEnergy) ||
                        firstDraw.HandIds.Length != expectedDraw || secondDraw.HandIds.Length != expectedDraw + (Combined ? survivorIds.Length : 0) ||
                        (Combined ? !secondDraw.HandIds.Skip(expectedDraw).SequenceEqual(survivorIds) :
                            secondDraw.HandIds.Intersect(survivorIds).Any()) ||
                        secondDraw.Decision.Energy != (Combined ? afterPlay.Decision.Energy : baseEnergy) ||
                        !CheckEndTurn(0, discarded: true) || !CheckEndTurn(1, discarded: !Combined))
                        throw new InvalidOperationException($"Native hand/Ember boundary differs. mode={Mode} paid={paid} " +
                            $"deployment={deployment.Decision.Energy} first={firstDraw.Decision.Energy} after={afterPlay.Decision.Energy} " +
                            $"second={secondDraw.Decision.Energy} hand={firstDraw.HandIds.Length}/{survivorIds.Length}/{secondDraw.HandIds.Length} " +
                            $"phases={string.Join(",", phases.Select(row => $"{row.Turn}:{row.Kind}:{row.Amount}"))}");
                    return Step.Complete;
                default:
                    throw new InvalidOperationException("Missed the original hand and Ember observation window.");
            }

            Snapshot Capture() => new Snapshot(trace.CaptureDecision(), cards.GetHand().Select(trace.CardId).ToArray());

            Step Advance()
            {
                endingTurn = turn;
                return Step.EndTurn;
            }

            bool CheckEndTurn(int originalTurn, bool discarded)
            {
                PhaseEvent[] events = phases.Where(row => row.Turn == originalTurn).ToArray();
                return events.Select(row => row.Kind).SequenceEqual(discarded
                    ? new[] { "ShouldDiscard", "DiscardHand", "RemoveEphemeral", "EnergyRemoval" }
                    : new[] { "ShouldDiscard", "RemoveEphemeral", "EnergyRemoval" }) &&
                    events[0].Amount == (Combined ? 0 : 1) && events.Last().Amount == (Combined ? 0 : events.Last().Energy);
            }

            Input FindInput()
            {
                var nativeStats = managers.GetCardStatistics();
                var nativeMonsters = managers.GetMonsterManager();
                var nativeRelics = managers.GetRelicManager();
                foreach (CardState card in cards.GetHand().Where(card => !card.IsAnyAbility() && !card.IsConsumeRemainingEnergyCostType())
                    .OrderByDescending(card => card.GetCardDataID() == TorchId))
                {
                    for (int index = 0; index < rooms.GetNumRooms(); index++)
                    {
                        RoomState room = rooms.GetRoom(index);
                        if (!room.IsRoomEnabled() || room.GetIsPyreRoom() || card.GetCost(nativeStats, nativeMonsters, nativeRelics, room) <= 0)
                            continue;
                        if (card.IsSpawnerCard())
                        {
                            SpawnPoint? point = room.GetFirstEmptyMonsterPoint();
                            if (point != null && cards.CanPlayHandCard(card, index, point, null, null, out _))
                                return new Input(card, index, point, null);
                            continue;
                        }
                        if (card.GetCardType() != CardType.Spell) continue;
                        bool requiresUnit = card.GetEffectStates().Any(effect => effect.GetTargetMode() == TargetMode.DropTargetCharacter);
                        if (!requiresUnit && (card.GetTargetless() || card.GetTargetsRoom()) &&
                            cards.CanPlayHandCard(card, index, null, null, null, out _))
                            return new Input(card, index, null, null);
                        var actors = new List<CharacterState>();
                        room.AddCharactersToList(actors, Team.Type.Heroes | Team.Type.Monsters, allowIsSpawning: false);
                        foreach (CharacterState actor in actors)
                            if (actor.GetSpawnPoint() is SpawnPoint point && cards.CanPlayHandCard(card, index, point, null, null, out _))
                                return new Input(card, index, point, actor);
                    }
                }
                throw new InvalidOperationException("No positive-cost stock turn-one action: " +
                    string.Join(",", cards.GetHand().Select(card => card.GetAssetName())));
            }
        }

        internal static void ConfirmMutatorShowcase(RewardScreen screen)
        {
            if (AccessTools.Field(typeof(RewardScreen), "currentRewards").GetValue(screen) is not IReadOnlyList<IRewardDisplayable> rewards ||
                rewards.Count == 0 || rewards.Any(reward => reward is not RelicDisplayedAsReward ||
                    reward.ReplayDataId != HandId && reward.ReplayDataId != EmberId)) return;
            ReplayManager.TryClickButton(screen.GetCollectButton());
        }

        private sealed class Snapshot(BattleTurnState decision, int[] handIds)
        {
            public BattleTurnState Decision { get; } = decision;
            public int[] HandIds { get; } = handIds;
        }

        private sealed class PhaseEvent(int turn, string kind, int amount, int energy)
        {
            public int Turn { get; } = turn;
            public string Kind { get; } = kind;
            public int Amount { get; } = amount;
            public int Energy { get; } = energy;
        }

        private sealed class Input(CardState card, int room, SpawnPoint? point, CharacterState? target)
        {
            internal CardState Card { get; } = card;
            internal int Room { get; } = room;
            internal SpawnPoint? Point { get; } = point;
            internal CharacterState? Target { get; } = target;
        }

        private static void Observe(string kind, int value = 0)
        {
            if (endingTurn is not int turn || AllGameManagers.Instance is not AllGameManagers managers ||
                managers.GetSaveManager().PreviewMode || managers.GetCombatManager()?.GetTurnCount() != turn) return;
            phases.Add(new PhaseEvent(turn, kind, value, managers.GetPlayerManager().GetEnergy()));
        }

        [HarmonyPatch(typeof(RelicManager), nameof(RelicManager.GetShouldDiscardHand))]
        private static class ShouldDiscardPatch
        {
            private static bool Prepare() => Enabled;
            private static void Postfix(bool __result) => Observe("ShouldDiscard", __result ? 1 : 0);
        }

        [HarmonyPatch(typeof(CardManager), nameof(CardManager.DiscardHand))]
        private static class DiscardPatch
        {
            private static bool Prepare() => Enabled;
            private static void Prefix() => Observe("DiscardHand");
        }

        [HarmonyPatch(typeof(CardManager), nameof(CardManager.RemoveEphemeralCardsFromHand))]
        private static class EphemeralPatch
        {
            private static bool Prepare() => Enabled;
            private static void Prefix() => Observe("RemoveEphemeral");
        }

        [HarmonyPatch(typeof(RelicManager), nameof(RelicManager.GetEnergyToRemoveAtTurnEnd))]
        private static class EnergyRemovalPatch
        {
            private static bool Prepare() => Enabled;
            private static void Postfix(int __result) => Observe("EnergyRemoval", __result);
        }
    }
}
#endif
