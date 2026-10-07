using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;
using System.Text.Json;

internal static class TerminalSpellChecks
{
    internal static void Run()
    {
        var rng = UnityRng.Seed(1);
        foreach (bool generated in new[] { false, true })
        {
            var context = new CombatContext(new CardCycleState([new(8, "spell")], [new(9, "junk")], [], rng, 0, []), rng, 0, 10, 10,
                statistics: BattleStatistics.Empty(deckCards: generated ? [] : [8]).TrackCards([8, 9]),
                cardInstances: [CardInstanceState.Empty(8, "spell"), CardInstanceState.Empty(9, "junk")]);
            var boss = new CombatUnit(1, "boss", CombatTeam.Enemy, 7, 1, 125, true, false, true, []);
            var pyre = new CombatUnit(2, "pyre", CombatTeam.Player, 45, 80, 80, true, true, false, []);
            var train = new TrainCombatState([new(0, false, [boss], [], context), new(1, false, [pyre], [], context)],
                [new(1, 1, false, false)], 5, context);
            var spawn = new EnemySpawnState(train, [], [], 0, false, rng, 3, [], 0, false, 0, 0, 0, []);
            var root = new BattleTurnState(spawn, 1, 3, 5, 0, 0, "Full", [],
                [new("Standby", []), new("Exhausted", []), new("Purged", []), new("Eaten", [])], [],
                new([new(0, 5, 5, true, false, false), new(1, 5, 5, true, false, true)],
                    [new("spell", "spell", 1, "Spell", "Discard", null, [], [new("Damage", "DropTargetCharacter", 5, true, false, [])])]));
            var killed = BattleActionModel.PlayCard(root, new(8, 0, targetUnitId: 1));
            Require(killed.Supported && killed.Outcome == RoomOutcome.BattleWon, "A terminal spell did not finish the battle: " + killed.Reason);
            BattleStatistics statistics = killed.State!.Spawn.Train.Context!.Statistics!;
            Require(statistics.Value(8, "TimesPlayed") == (generated ? 0 : 1) && statistics.Value(8, "TimesDiscarded") == 1 &&
                statistics.CardsPlayedThisTurn.SequenceEqual([8]) && statistics.PlayedCosts.Count == 0 &&
                statistics.TrackedCards.SequenceEqual([8]) && statistics.Value(8, "HeroesKilled") == (generated ? 0 : 1) &&
                killed.State.Spawn.Train.Context!.Cards.Discard.Single().InstanceId == 8 &&
                killed.State.Spawn.Train.Context.CardInstances!.Single().PlayCount == 1,
                "Settled terminal spells lost played/discard callbacks, retained costs, or did not restore the resolving card.");
        }
        Console.WriteLine("TERMINAL-SPELL-CHECKS PASS: settled callbacks, restored card instance, cleared cost and permanent/generated deck statistic membership.");
    }
    internal static void Native(FixtureValue entry)
    {
        BattleTurnState before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = entry.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction action = entry.GetProperty("Action").Deserialize<PlayCardAction>()!;
        CombatContext old = before.Spawn.Train.Context!, settled = actual.Spawn.Train.Context!;
        CardInstanceState instance = old.CardInstances!.Single(card => card.InstanceId == action.CardInstanceId);
        CardPlayRule rule = CardModifierModel.Resolve(before.PlayRules!.Cards.Single(card => card.DataId == instance.DataId), instance);
        bool permanent = old.Statistics!.DeckCards!.Contains(instance.InstanceId);
        Require(before.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit => unit.EndsBattleOnDeath) &&
            settled.Cards.Discard.Single().InstanceId == instance.InstanceId && settled.CardInstances!.Single().PlayCount == instance.PlayCount + 1 &&
            settled.Statistics!.Value(instance.InstanceId, "TimesPlayed") == (permanent ? old.Statistics.Value(instance.InstanceId, "TimesPlayed") + 1 : 0) &&
            settled.Statistics.Value(instance.InstanceId, "TimesDiscarded") == (permanent ? old.Statistics.Value(instance.InstanceId, "TimesDiscarded") + 1 : 1) &&
            settled.Statistics.PlayedCosts.Count == 0 && settled.Statistics.CardsPlayedThisTurn.Contains(instance.InstanceId) &&
            actual.Energy == before.Energy - (rule.CostType == "ConsumeRemainingEnergy" ? before.Energy : rule.Cost),
            "The settled terminal oracle lacks cast/discard callbacks, restored card state or paid energy.");
        if (old.CardRegistry != null)
            Require(settled.CardRegistry != null && old.CardRegistry.All(card => settled.CardRegistry.Any(next => next.InstanceId == card.InstanceId)) &&
                settled.CardRegistry.Single(card => card.InstanceId == instance.InstanceId).PlayCount == instance.PlayCount + 1 &&
                settled.CardRegistry.Count > settled.CardInstances!.Count,
                "The settled terminal oracle lost unowned card references or their completed discard state.");
        Console.WriteLine("NATIVE-TERMINAL-SPELL-COVERAGE PASS: native boss kill, completed callbacks, paid energy, cleared cost and restored resolving card.");
        if (rule.Effects.Any(effect => effect.Upgrade?.AssetKey == "PojuPostKillPermanent"))
        {
            CombatUnit[] oldRoom = before.Spawn.Train.Rooms.Single(room => room.RoomIndex == action.RoomIndex).Units.ToArray();
            CombatUnit[] newRoom = actual.Spawn.Train.Rooms.Single(room => room.RoomIndex == action.RoomIndex).Units.ToArray();
            Require(old.AllScenarioBossesDead == false && settled.AllScenarioBossesDead == true &&
                oldRoom.Count(unit => unit.Team == CombatTeam.Enemy) >= 2 && newRoom.All(unit => unit.Team != CombatTeam.Enemy),
                "The post-kill oracle lacks a boss death followed by a remaining enemy death.");
            var players = oldRoom.Where(unit => unit.Team == CombatTeam.Player).ToArray();
            Require(players.Length >= 2 && rule.Effects.Where(effect => effect.Type == "HandUpgrade")
                .All(effect => effect.Tests!.CanPlayAfterBossDead == false) &&
                rule.Effects.Any(effect => effect.Type == "HandUpgrade" && effect.Tests!.CancelSubsequent),
                "The post-kill oracle lacks multiple live targets or the native hand-effect cancellation gate.");
            foreach (CombatUnit unit in players)
            {
                CombatUnit next = newRoom.Single(item => item.Id == unit.Id);
                CardInstanceState oldCard = old.CardRegistry!.Single(card => card.InstanceId == unit.SpawnerCardId);
                CardInstanceState retained = settled.CardRegistry!.Single(card => card.InstanceId == unit.SpawnerCardId);
                int Count(IReadOnlyList<CardUpgradeModifier> list, string name) => list.Count(upgrade => upgrade.AssetKey == "PojuPostKill" + name);
                Require(next.BaseAttack == unit.BaseAttack + 1 && next.MaxHealth == unit.MaxHealth + 4 && next.Health == next.MaxHealth &&
                    next.Statuses.All(status => status.Id != "armor") &&
                    Count(retained.Permanent.Upgrades, "Permanent") == Count(oldCard.Permanent.Upgrades, "Permanent") + 1 &&
                    Count(retained.Temporary.Upgrades, "Temporary") == Count(oldCard.Temporary.Upgrades, "Temporary") &&
                    Count(next.Modifiers!.Upgrades, "CanceledTail") == Count(unit.Modifiers!.Upgrades, "CanceledTail") &&
                    settled.CardInstances!.All(card => card.InstanceId != retained.InstanceId),
                    "Post-kill damage/status/healing, detached upgrades/removal or cancellation differs.");
            }
            Console.WriteLine("NATIVE-POST-KILL-COVERAGE PASS: remaining enemy death, two surviving friendly targets, status consumption, permanent spawner upgrades, temporary removal, healing and canceled tail.");
        }
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
