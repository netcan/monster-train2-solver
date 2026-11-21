using System;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalSourceBackedFeeding
    {
        internal const string Name = "original-source-backed-feeding";
        internal const string Boundary = "NativeOriginalSourceBackedFeeding";
        private const string Steward = "38e70e6a-20f3-4e36-8d31-c5110be07f87";
        private const string Miner = "536e6de0-7ccf-412a-9a4e-ec55ab3988e5";
        private const string Feast = "236215da-c1dd-40b7-8793-27ce327a7e78";
        private const string Primordium = OriginalPrimordiumScenario.CardDataId;
        private const int Floor = 2;
        private static int[] deploymentCards = Array.Empty<int>();
        private static int[] forcedCards = Array.Empty<int>();
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared => deploymentCards.Length > 0;

        internal static void Prepare(AllGameManagers managers)
        {
            if (typeof(CardState).Assembly.ManifestModule.ModuleVersionId != new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d") ||
                managers.GetCombatManager()!.GetTurnCount() != 0)
                throw new InvalidOperationException("Original source-backed feeding requires deployment in the pinned 2.3.1 runtime.");
            deploymentCards = AddOriginals(managers, (Steward, "TrainStewardSpear"), (Miner, "MorselMiner"),
                (Primordium, "ChampionPrimordium"));
        }

        internal static bool Observe(AllGameManagers managers, ManualLogSource log)
        {
            int turn = managers.GetCombatManager()!.GetTurnCount();
            if (turn < 2) return false;
            var ordinary = OriginalOpeningMorselsScenario.Foods.SingleOrDefault(row => row.Completed &&
                row.RoomIndex == Floor && row.Team == CombatTeam.Player && row.Actual != null &&
                Positive(row.Before, row.Actual, deploymentCards[1], 3));
            if (ordinary == null)
                throw new InvalidOperationException("Native ordinary meal did not consume the Miner and preserve Primordium at two stacks.");

            if (forcedCards.Length == 0)
            {
                forcedCards = AddOriginals(managers, (Miner, "MorselMiner"), (Feast, "Feast"));
                return false;
            }

            var forced = ForcedFeedingScenario.Records.SingleOrDefault(row => row.Completed &&
                row.CardId == forcedCards[1] && row.Actual != null);
            if (forced == null)
            {
                if (turn > 2) throw new InvalidOperationException("Feast did not resolve in the first post-meal player turn.");
                return false;
            }
            if (!Positive(forced.Before, forced.Actual!, forcedCards[0], 2))
                throw new InvalidOperationException("Native Feast did not consume the Miner and preserve Primordium at one stack.");

            using var document = NativeFixtureCapture.Capture(new
            {
                Schema = 1, Boundary, GameVersion = UnityEngine.Application.version,
                GameModuleMvid = typeof(CardState).Assembly.ManifestModule.ModuleVersionId,
                Floor, Primordium, Miner, Feast, Steward, DeploymentCards = deploymentCards, ForcedCards = forcedCards,
                Ordinary = ordinary, Forced = forced
            });
            using var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "source-backed-feeding.mt2f"));
            document.Write(stream);
            log.LogInfo("ORIGINAL-SOURCE-BACKED-FEEDING PASS ordinary and Feast observed with original Miner removal and Primordium survival.");
            return true;
        }

        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            int[] instances = state.Spawn.Turn < 2 ? deploymentCards : forcedCards;
            var hand = state.Spawn.Train.Context!.Cards.Hand;
            int index = Array.FindIndex(instances, id => hand.Any(card => card.InstanceId == id));
            return index < 0 ? null : new PlayCardAction(instances[index], Floor,
                playerPosition: state.Spawn.Turn < 2 || index == 0 ? (state.Spawn.Turn < 2 ? index : 1) : -1);
        }

        private static int[] AddOriginals(AllGameManagers managers, params (string Id, string Key)[] originals)
        {
            CardManager cards = managers.GetCardManager()!;
            var all = managers.GetSaveManager().GetAllGameData();
            return originals.Select(original =>
            {
                CardData definition = all.FindCardData(original.Id);
                if (definition == null || definition.name != original.Key || !cards.GetBelowHandSize())
                    throw new InvalidOperationException("Could not deal unchanged original " + original.Key + ".");
                CardState card = cards.AddNewCard(definition, CardPile.HandPile, false, false, animate: false);
                if (!cards.GetHand().Contains(card)) throw new InvalidOperationException("Original card did not enter the native hand.");
                return FullBattleTrace.Active!.CardId(card);
            }).ToArray();
        }

        private static bool Positive(TrainCombatState before, TrainCombatState actual, int minerCard, int oldStacks)
        {
            CombatUnit? feeder = before.Rooms[Floor].Units.SingleOrDefault(unit => unit.SpawnerCardId == deploymentCards[0]);
            CombatUnit? repeated = before.Rooms[Floor].Units.SingleOrDefault(unit => unit.SpawnerCardId == deploymentCards[2]);
            CombatUnit? miner = before.Rooms[Floor].Units.SingleOrDefault(unit => unit.SpawnerCardId == minerCard);
            if (feeder == null || repeated == null || miner == null || repeated.Status("eatmany")?.Stacks != oldStacks ||
                before.Context?.SpawnPoints == null || actual.Context?.SpawnPoints == null) return false;
            CombatUnit? surviving = actual.Rooms[Floor].Units.SingleOrDefault(unit => unit.Id == repeated.Id);
            return surviving?.Status("eatmany")?.Stacks == oldStacks - 1 && surviving.LastFeederId == feeder.Id &&
                actual.Rooms[Floor].Units.All(unit => unit.Id != miner.Id) &&
                before.Context.SpawnPoints.Units.Any(point => point.UnitId == repeated.Id && point.Current != null) &&
                actual.Context.SpawnPoints.Units.Any(point => point.UnitId == repeated.Id && point.Current != null) &&
                actual.Context.OtherPiles?.Any(pile => pile.Name == "Eaten" && pile.Cards.Any(card => card.InstanceId == minerCard)) == true;
        }
    }
}
