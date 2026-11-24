#if DEVELOPMENT_PROBE
using System;
using System.IO;
using System.Linq;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal static partial class PlayerAdvisorProbe
    {
        internal const string RejectedDeploymentPass = "Player Advisor preserved its search after a rejected native deployment.";
        internal static bool ObserveRejectedDeployment =>
            Environment.GetEnvironmentVariable("MT2_PROBE_ADVISOR_REJECTED_DEPLOYMENT") == "1";

        private sealed class RejectedDeploymentBoundary
        {
            public BattleTurnState Root { get; }
            public long Generation { get; } = GetGeneration();
            public long Searches { get; } = SearchCount;
            public long CardInputs { get; } = Get<long>("DiagnosticCardInputs");
            internal RejectedDeploymentBoundary(BattleTurnState root) => Root = root;
        }

        private static RejectedDeploymentBoundary? rejectedDeploymentBefore;
        private static object? rejectedDeploymentInput;

        internal static bool TryCaptureRejectedDeployment(AllGameManagers managers, FullBattleTrace trace)
        {
            Resolve();
            if (!TryReadRestartPublication(out var root, out var advice, expectedTurn: 0)) return false;
            Require(managers.GetCombatManager().IsPlacementPhase && trace.ObservedTurns.Count == 0 &&
                trace.ObservedActionCount == 0 && Get<long>("DiagnosticCardInputs") == 0,
                "rejected deployment requires the untouched original placement decision");
            using var keys = new BattleStateKeys();
            if (rejectedDeploymentBefore == null)
            {
                var cards = managers.GetCardManager();
                var card = cards.GetHand().First(candidate => candidate.GetCardType() == CardType.Monster);
                var rooms = managers.GetRoomManager();
                int room = rooms.GetSelectedRoom();
                var enemyPoint = rooms.GetRoom(room).GetHeroPoint(0);
                rejectedDeploymentBefore = new RejectedDeploymentBoundary(root!);
                bool played = cards.PlayAnyCard(card, out var error, dropLocation: enemyPoint, roomIndexOverride: room);
                Require(!played, "native deployment unexpectedly accepted an enemy position");
                Require(GetGeneration() == rejectedDeploymentBefore.Generation && SearchCount == rejectedDeploymentBefore.Searches &&
                    ReferenceEquals(advice, Advice) && !CurrentIsNull(),
                    "the rejected deployment retired verified advice or restarted its search");
                rejectedDeploymentInput = new { CardDataId = card.GetCardDataID(), Room = room, DropTeam = enemyPoint.GetTeam().ToString(),
                    Played = played, Error = error.ToString(), Generation = GetGeneration(), Searches = SearchCount };
                return false;
            }
            Require(SearchCount == rejectedDeploymentBefore.Searches && keys.Key(root!) == keys.Key(rejectedDeploymentBefore.Root),
                "settling the rejected deployment searched again or changed the native decision and RNG");
            using var document = NativeFixtureCapture.Capture(new
            {
                Schema = 1, EvidenceKind = "NativeAdvisorRejectedDeployment", GameVersion = Application.version,
                GameModuleMvid = typeof(CardState).Module.ModuleVersionId, Assembly = type!.Assembly.GetName().Name,
                Before = rejectedDeploymentBefore, Input = rejectedDeploymentInput, After = new RejectedDeploymentBoundary(root!)
            });
            using var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!,
                "player-advisor-rejected-deployment.mt2f"));
            document.Write(output);
            return true;
        }
    }
}
#endif
