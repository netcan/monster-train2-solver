#if DEVELOPMENT_PROBE
using System;
using System.Linq;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static partial class PlayerAdvisorProbe
    {
        internal static bool ObserveDevote => Environment.GetEnvironmentVariable("MT2_PROBE_ADVISOR_DEVOTE_OBSERVATION") == "1";
        private static object? devoteBefore, devoteAccepted, devoteAfter;
        private static string? devoteBeforeKey;
        private static long devoteBeforeGeneration, devoteBeforeInputs;
        private static int devoteBeforePublications, devoteTargetRoom = -1;
        private static bool devoteCompleted;

        internal static object? DevoteEvidence => !ObserveDevote ? null : new
        {
            Scope = "controlled-native-helper-api-only", Passed = devoteAfter != null,
            Before = devoteBefore, Accepted = devoteAccepted, After = devoteAfter
        };

        internal static bool AwaitDevoteObservation(BattleTurnState decision)
        {
            if (!ObserveDevote) return false;
            if (devoteBefore == null && decision.Spawn.Turn == 1)
            {
                if (!ReadyFor()) return true;
                BattleTurnState player = Capture();
                BattleAdvice advice = Advice;
                BattleActionEnumeration candidates = BattleActionModel.EnumerateActions(player);
                int offered = candidates.SupportedCandidates.Count(item => item.Action.Operation == DevoteModel.Operation) +
                    candidates.UnsupportedAttempts.Count(item => item.Action?.Operation == DevoteModel.Operation);
                Require(player.PlayRules?.DevoteEnabled != true && offered == 0 &&
                    !advice.Plan.Steps.Any(step => step.Action?.Operation == DevoteModel.Operation),
                    "non-Punished donor exposed Devote in the player advisor");
                Require(SameResources(player, decision), "player and native pre-Devote resources or Battle RNG differ");
                using var keys = new BattleStateKeys();
                devoteBeforeKey = keys.Key(player);
                devoteBeforeGeneration = GetGeneration(); devoteBeforePublications = PublicationCount;
                devoteBeforeInputs = Get<long>("DiagnosticDevoteInputs");
                devoteBefore = new { Turn = player.Spawn.Turn, RootKey = devoteBeforeKey,
                    AdviceRootKey = advice.RootKey, Generation = devoteBeforeGeneration,
                    Publications = devoteBeforePublications, DevoteInputs = devoteBeforeInputs,
                    PlayerDevoteEnabled = player.PlayRules?.DevoteEnabled, DevoteCandidates = offered,
                    Energy = player.Energy, Hand = player.Spawn.Train.Context!.Cards.Hand.Count,
                    Devotion = player.Spawn.Train.Context.QueryFrame?.Devotion,
                    BattleRng = player.Spawn.Train.Context.BattleRng };
                log!.LogInfo("PLAYER-ADVISOR-DEVOTE-BEFORE generation=" + devoteBeforeGeneration +
                    " publications=" + devoteBeforePublications + " candidates=" + offered);
            }
            if (devoteAccepted != null && devoteAfter == null)
            {
                Require(devoteCompleted, "native Devote acceptance was observed before coroutine completion");
                if (!ReadyFor()) return true;
                BattleTurnState player = Capture();
                using var keys = new BattleStateKeys();
                string key = keys.Key(player);
                Require(key != devoteBeforeKey && Advice.RootKey == key && GetGeneration() > devoteBeforeGeneration &&
                    PublicationCount > devoteBeforePublications && Get<long>("DiagnosticDevoteInputs") == devoteBeforeInputs + 1,
                    "settled Devote did not publish a fresh recommendation for the new root");
                Require(SameResources(player, decision), "player and native settled Devote resources or Battle RNG differ");
                devoteAfter = new { Turn = player.Spawn.Turn, RootKey = key, AdviceRootKey = Advice.RootKey,
                    Generation = GetGeneration(), Publications = PublicationCount,
                    DevoteInputs = Get<long>("DiagnosticDevoteInputs"),
                    Energy = player.Energy, Hand = player.Spawn.Train.Context!.Cards.Hand.Count,
                    Devotion = player.Spawn.Train.Context.QueryFrame?.Devotion,
                    BattleRng = player.Spawn.Train.Context.BattleRng,
                    NativeResourcesMatch = true, FreshRoot = true };
                log!.LogInfo("PLAYER-ADVISOR-DEVOTE-AFTER generation=" + GetGeneration() +
                    " publications=" + PublicationCount + " room=" + devoteTargetRoom);
            }
            return false;
        }

        internal static void DevoteStarted(int room)
        {
            if (!ObserveDevote || devoteAccepted != null) return;
            Require(devoteBefore != null, "native Devote started before settled player advice");
            devoteTargetRoom = room;
        }

        internal static void DevoteProgress()
        {
            if (!ObserveDevote || devoteAccepted != null || devoteBefore == null) return;
            long inputs = Get<long>("DiagnosticDevoteInputs");
            if (inputs == devoteBeforeInputs) return;
            Require(inputs == devoteBeforeInputs + 1 && CurrentIsNull() && Field<bool>("captureNeeded") &&
                GetGeneration() > devoteBeforeGeneration && PublicationCount == devoteBeforePublications &&
                DevotionProbe.IsDevoteInProgress,
                "accepted native Devote did not retire the pre-input recommendation synchronously");
            devoteAccepted = new { TargetRoom = devoteTargetRoom, DevoteInputs = inputs,
                Generation = GetGeneration(), Publications = PublicationCount,
                CurrentAdviceAbsent = true, CapturePending = true,
                NativeDevoteInProgress = true };
            log!.LogInfo("PLAYER-ADVISOR-DEVOTE-ACCEPTED room=" + devoteTargetRoom +
                " generation=" + GetGeneration() + " inputs=" + inputs);
        }

        internal static void DevoteCompleted()
        {
            if (!ObserveDevote || devoteAfter != null) return;
            Require(devoteAccepted != null, "native Devote completed without an accepted input journal event");
            devoteCompleted = true;
        }

        private static bool SameResources(BattleTurnState player, BattleTurnState native)
        {
            var left = player.Spawn.Train.Context;
            var right = native.Spawn.Train.Context;
            var leftDevotion = left?.QueryFrame?.Devotion;
            var rightDevotion = right?.QueryFrame?.Devotion;
            return left != null && right != null && leftDevotion != null && rightDevotion != null &&
                player.Energy == native.Energy &&
                left.Cards.Hand.Count == right.Cards.Hand.Count &&
                JToken.DeepEquals(JToken.FromObject(leftDevotion), JToken.FromObject(rightDevotion)) &&
                JToken.DeepEquals(JToken.FromObject(left.BattleRng), JToken.FromObject(right.BattleRng));
        }
    }
}
#endif
