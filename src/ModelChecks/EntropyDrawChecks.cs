using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class EntropyDrawChecks
{
    private const string BlightId = "21038774-c0a4-413c-ab7e-27e458228e5f";

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 1 &&
            root.GetProperty("Boundary").GetString() == "OriginalEntropyDrawOwnedBlight" &&
            root.GetProperty("GameVersion").GetString() == Game231FixtureCatalog.Version &&
            root.GetProperty("GameModuleMvid").GetString() == Game231FixtureCatalog.Module &&
            root.GetProperty("OwnerId").GetString() == RelicEntropyDrawModel.OwnerId &&
            root.GetProperty("OwnerKey").GetString() == RelicEntropyDrawModel.OwnerKey &&
            root.GetProperty("PyreId").GetString() == "cd00b31d-d15a-4871-ae5f-6d9451f121f8" &&
            root.GetProperty("BlightId").GetString() == BlightId &&
            root.GetProperty("CardsPerDraw").GetInt32() == 2,
            "Expected the original Entropy Pyre and SelfPurgeBlight under the pinned module.");

        DeckChange[] callbacks = root.GetProperty("DeckChanged").EnumerateArray().Select(row => new DeckChange(
            row.GetProperty("DuringPurge").GetBoolean(), row.GetProperty("BlightCount").GetInt32(),
            row.GetProperty("Before").GetSingle(), row.GetProperty("After").GetSingle())).ToArray();
        Require(callbacks.SequenceEqual([new(false, 1, 0, 0), new(false, 2, 0, 1), new(true, 1, 1, 0)]),
            "Expected one native deck notification per owned addition and one on the paid purge.");

        Snapshot deployment = Read("Deployment", 0, 2, 6);
        Snapshot first = Read("FirstDraw", 1, 2, 6);
        Snapshot before = Read("BeforePurge", 1, 2, 6);
        Snapshot after = Read("AfterPurge", 1, 1, 5);
        Snapshot second = Read("SecondDraw", 2, 1, 5);
        PlayCardAction action = root.GetProperty("Action").Deserialize<PlayCardAction>()!;
        int cardId = action.CardInstanceId;
        CardPlayRule rule = before.Decision.PlayRules!.Cards.Single(item => item.DataId == BlightId);
        Require(!root.GetProperty("DrawnForSetup").GetBoolean() && first.Hand.Length == 6 && second.Hand.Length == 5 &&
            before.Hand.Contains(cardId) && !after.Hand.Contains(cardId) && before.Owned.Contains(cardId) &&
            after.Owned.SequenceEqual(before.Owned.Where(id => id != cardId)) &&
            deployment.Owned.SequenceEqual(first.Owned) && first.Owned.SequenceEqual(before.Owned) &&
            after.Owned.SequenceEqual(second.Owned) && action.ActivatorUnitId == 0 && action.TargetUnitId == 0 &&
            rule.IsAnyAbility == false && rule.Cost == 3 && before.Decision.Energy - after.Decision.Energy == rule.Cost &&
            before.Decision.Spawn.Train.Context!.FindCard(cardId)?.DataId == BlightId,
            "The trace must contain the actual drawn hands and one ordinary paid purge of an owned Blight.");

        string parent = JsonSerializer.Serialize(deployment.Decision, ModelJson.Options);
        BattleTurnState replay = EndTurn(deployment.Decision, first.Raw, "first draw");
        replay = Match(replay, before.Raw, "before the paid purge");
        BattleActionResult play = BattleActionModel.PlayCard(replay, action);
        Require(play.State != null && play.Outcome == RoomOutcome.Exchanged, "Entropy paid purge: " + play.Reason);
        replay = Match(play.State!, after.Raw, "after the paid purge");
        EndTurn(replay, second.Raw, "next draw after purge");
        Require(parent == JsonSerializer.Serialize(deployment.Decision, ModelJson.Options),
            "Entropy replay mutated the recorded deployment parent.");
        Console.WriteLine("NATIVE-ENTROPY-DRAW PASS: two owned Blights, native paid purge, two exact EndTurns and RNG, native hands 6 and 5.");

        Snapshot Read(string name, int turn, int count, int draw)
        {
            var row = root.GetProperty(name);
            FixtureValue raw = row.GetProperty("Decision");
            BattleTurnState decision = raw.Deserialize<BattleTurnState>()!;
            CombatContext context = decision.Spawn.Train.Context!;
            CombatRelicState owner = context.Relics!.Single(item => item.DataId == RelicEntropyDrawModel.OwnerId);
            RelicResourceEffect effect = owner.ResourceEffects!.Single();
            int[] saved = context.Statistics!.DeckCards!.Where(id =>
                context.FindCard(id)?.MaskDescriptor?.Definition.CardType == "Blight").Order().ToArray();
            int[] owned = row.GetProperty("OwnedIds").Deserialize<int[]>()!;
            int[] hand = row.GetProperty("HandIds").Deserialize<int[]>()!;
            Require(decision.Spawn.Turn == turn && row.GetProperty("BlightCount").GetInt32() == count &&
                decision.Spawn.Train.Rooms.SelectMany(room => room.Units)
                    .Any(unit => unit.IsPyre && unit.AssetKey == "PyreHeartEntropy") &&
                row.GetProperty("Value").GetSingle() == count / 2 &&
                row.GetProperty("StartingHandSize").GetInt32() == draw && decision.DrawPerTurn == draw &&
                effect.Type == RelicEntropyDrawModel.Effect && RelicEntropyDrawModel.Validate(owner, effect) == null &&
                effect.DrawValue == count / 2 && owned.Length == count && owned.Order().SequenceEqual(saved) &&
                owned.All(id => context.FindCard(id)?.DataId == BlightId) &&
                hand.SequenceEqual(context.Cards.Hand.Select(card => card.InstanceId)),
                "Native Entropy count, draw projection or ownership differs at " + name + ".");
            return new Snapshot(raw, decision, owned, hand);
        }
    }

    private static BattleTurnState EndTurn(BattleTurnState state, FixtureValue expected, string label)
    {
        BattleTurnResult next = BattleTurnModel.EndTurn(state);
        Require(next.State != null && next.Outcome == RoomOutcome.Exchanged, "Entropy " + label + ": " + next.UnsupportedReason);
        return Match(next.State!, expected, label);
    }

    private static BattleTurnState Match(BattleTurnState state, FixtureValue expected, string label) =>
        RecordedBattleChecks.RecordedBoundary(state, expected, terminal: false, "Entropy " + label);

    private sealed record Snapshot(FixtureValue Raw, BattleTurnState Decision, int[] Owned, int[] Hand);
    private readonly record struct DeckChange(bool DuringPurge, int Count, float Before, float After);

    private static void Require(bool valid, string message)
    {
        if (!valid) throw new InvalidDataException(message);
    }
}
