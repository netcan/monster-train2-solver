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
        private const string ExtraFeedBoundary = "NativeOriginalExtraFeedTriggerMeal";
        private const string Steward = "38e70e6a-20f3-4e36-8d31-c5110be07f87";
        private const string Construct = "3db9cb2c-3495-47a7-983e-d717a206460c";
        private const string Miner = "536e6de0-7ccf-412a-9a4e-ec55ab3988e5";
        private const string Feast = "236215da-c1dd-40b7-8793-27ce327a7e78";
        private const string Primordium = OriginalPrimordiumScenario.CardDataId;
        private const int Floor = 2;
        private static int[] deploymentCards = Array.Empty<int>();
        private static int[] forcedCards = Array.Empty<int>();
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool WithExtraFeedTrigger => Enabled &&
            Environment.GetEnvironmentVariable("MT2_PROBE_SOURCE_BACKED_EXTRA_FEED") == "1";
        internal static bool Prepared => deploymentCards.Length > 0;

        internal static void Prepare(AllGameManagers managers)
        {
            if (typeof(CardState).Assembly.ManifestModule.ModuleVersionId != new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d") ||
                managers.GetCombatManager()!.GetTurnCount() != 0)
                throw new InvalidOperationException("Original source-backed feeding requires deployment in the pinned 2.3.1 runtime.");
            if (WithExtraFeedTrigger && !OriginalExtraFeedTriggerScenario.Acquired)
                throw new InvalidOperationException("ExtraFeedTrigger must already be active before the original feeder and its meal deploy.");
            if (WithExtraFeedTrigger)
            {
                deploymentCards = AddOriginals(managers, (Construct, "AlloyedConstruct"), (Miner, "MorselMiner"));
                return;
            }
            deploymentCards = AddOriginals(managers, (Steward, "TrainStewardSpear"), (Miner, "MorselMiner"),
                (Primordium, "ChampionPrimordium"));
        }

        internal static bool Observe(AllGameManagers managers, ManualLogSource log)
        {
            int turn = managers.GetCombatManager()!.GetTurnCount();
            if (turn < 2) return false;
            if (WithExtraFeedTrigger)
            {
                ObserveExtraFeed(log);
                return true;
            }
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

        private static void ObserveExtraFeed(ManualLogSource log)
        {
            var meal = OriginalOpeningMorselsScenario.Foods.SingleOrDefault(row => row.Completed && row.Actual != null &&
                row.RoomIndex == Floor && row.Team == CombatTeam.Player && row.Before.Context?.QueryFrame?.Turn == 1 &&
                row.Before.Rooms[Floor].Units.Any(unit => unit.SpawnerCardId == deploymentCards[1]) &&
                row.Actual.Rooms[Floor].Units.All(unit => unit.SpawnerCardId != deploymentCards[1]));
            if (meal == null) throw new InvalidOperationException("The original Miner did not reach an ordinary feeding callback.");
            CombatUnit feeder = meal.Before.Rooms[Floor].Units.Single(unit => unit.SpawnerCardId == deploymentCards[0]);
            CombatUnit miner = meal.Before.Rooms[Floor].Units.Single(unit => unit.SpawnerCardId == deploymentCards[1]);
            TrainCombatState actual = meal.Actual!;
            CombatUnit after = actual.Rooms[Floor].Units.Single(unit => unit.Id == feeder.Id);
            CombatTrigger onFeed = feeder.Triggers.Single(trigger => trigger.Kind == "OnFeed");
            int fuelBefore = feeder.Status("spark")?.Stacks ?? 0;
            int fuelAfter = after.Status("spark")?.Stacks ?? 0;
            using var document = NativeFixtureCapture.Capture(new
            {
                Schema = 1, Boundary = ExtraFeedBoundary, GameVersion = UnityEngine.Application.version,
                GameModuleMvid = typeof(CardState).Assembly.ManifestModule.ModuleVersionId,
                Floor, Construct, Miner, DeploymentCards = deploymentCards, Definition = OriginalExtraFeedTriggerScenario.Evidence,
                FeederId = feeder.Id, FoodId = miner.Id, NativeFireCount = onFeed.FireCount, FuelBefore = fuelBefore,
                FuelAfter = fuelAfter, Ordinary = meal
            });
            using (var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "extra-feed-trigger-meal.mt2f")))
                document.Write(stream);
            if (meal.Error != null || meal.UnsupportedReason != null || meal.Difference != null ||
                !meal.FoodIds.SequenceEqual(new[] { miner.Id }) || meal.FoodStatAfter - meal.FoodStatBefore != 1 ||
                onFeed.FireCount != 2 || fuelAfter - fuelBefore != 2 ||
                actual.Rooms[Floor].Units.Any(unit => unit.Id == miner.Id) ||
                actual.Context?.OtherPiles?.Any(pile => pile.Name == "Eaten" &&
                    pile.Cards.Any(card => card.InstanceId == deploymentCards[1])) != true)
                throw new InvalidOperationException("The original meal did not produce two exact OnFeed fuel increments and one eaten Miner.");
            log.LogInfo("ORIGINAL-EXTRA-FEED-MEAL PASS one original Miner, one consumed source card, two OnFeed fuel increments.");
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
