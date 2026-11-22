using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalRoomStatusSpellsChecks
{
    private static readonly (string Key, string Id, int Cost, string Status, int Stacks, int Chance)[] Spells =
    [
        ("LavaFlow", "479ceb1b-fc8c-420f-8fa0-17683cee4511", 0, "pyregel", 4, 100),
        ("Gooeyflask", "47df0371-7bd9-4bc2-b8e1-de61cc0060c7", 1, "debuff", 2, 0),
        ("EchoSnare", "75ce1442-757b-4e9f-826d-8325f3dc7d9e", 1, "corrupt_poison", 5, 0),
        ("JarOfFrost", "768a53f0-2743-4298-82c3-8541cbecf935", 0, "poison", 8, 0),
        ("MagicJuice", "9ea70171-8afd-4cd1-997e-8a7bb789c766", 0, "spell weakness", 2, 0)
    ];

    internal static void Check(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 1 && root.GetProperty("Boundary").GetString() == "OriginalRoomStatusSpells" &&
            root.GetProperty("GameVersion").GetString() == "2.3.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d",
            "Expected original 2.3.1 room-status spell capture.");
        var plays = root.GetProperty("Plays").EnumerateArray().ToArray();
        Require(plays.Length == 1 + Spells.Length && plays[0].GetProperty("Card").GetString() == "TrainStewardShield",
            "Missing original deployment or room-status spells.");
        for (int i = 0; i < plays.Length; i++)
        {
            var play = plays[i];
            BattleTurnState before = State(play, "Before"), after = State(play, "After");
            var action = play.GetProperty("Action").Deserialize<PlayCardAction>()!;
            Require(before.MoonPhaseTrackingActive == false && after.MoonPhaseTrackingActive == false &&
                action.RoomIndex == 0 && action.TargetUnitId == 0, "Original status witness lost its neutral room boundary.");
            var modeled = BattleActionModel.PlayCard(before, action);
            Require(modeled.Supported, "Original status play unsupported: " + modeled.Reason);
            Match(modeled.State!, after, play.GetProperty("Card").GetString()!);
            if (i == 0) continue;

            var expected = Spells[i - 1];
            var token = before.Spawn.Train.Context!.Cards.Hand.Single(card => card.InstanceId == action.CardInstanceId);
            var rule = before.PlayRules!.Cards.Single(card => card.DataId == token.DataId);
            Require(token.DataId == expected.Id && rule.AssetKey == expected.Key && rule.Cost == expected.Cost &&
                before.Spawn.Turn == 1 && after.Spawn.Turn == 1 && before.Energy - after.Energy == expected.Cost &&
                rule.Effects is [{ Type: "AddStatus", Target: "Room", AllowEnemy: true, AllowPlayer: false, Statuses: [var status] } effect] &&
                effect.Value == expected.Chance && status.Id == expected.Status && status.Stacks == expected.Stacks &&
                after.OtherPiles.Single(pile => pile.Name == "Exhausted").Cards.Any(card => card.InstanceId == token.InstanceId),
                "Original spell definition, energy payment, or Consume routing differs: " + expected.Key);

            var targets = before.Spawn.Train.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Enemy).ToArray();
            Require(targets.Length >= 2, "Expected two original enemies on the original room-status floor.");
            foreach (var target in targets)
            {
                var observed = after.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == target.Id);
                Require(Stacks(observed, expected.Status) - Stacks(target, expected.Status) == expected.Stacks && observed.Health == target.Health,
                    "Original target stack or direct health differs: " + expected.Key + " on " + target.AssetKey);
            }
        }

        var exchange = root.GetProperty("Exchange");
        BattleTurnState exchangeBefore = State(exchange, "Before"), exchangeAfter = State(exchange, "After");
        var turn = BattleTurnModel.EndTurn(exchangeBefore);
        Require(exchangeBefore.Spawn.Turn == 1 && exchangeAfter.Spawn.Turn == 2 && turn.Outcome == RoomOutcome.Exchanged && turn.Supported,
            "Original first exchange incomplete: " + turn.UnsupportedReason);
        Match(turn.State!, exchangeAfter, "EndTurn");
        Console.WriteLine("ORIGINAL-ROOM-STATUS-SPELLS PASS: five paid direct room casts and first native exchange matched full state and RNG.");
    }

    private static BattleTurnState State(FixtureValue row, string key) => row.GetProperty(key).Deserialize<BattleTurnState>()!;
    private static int Stacks(CombatUnit unit, string status) => unit.Statuses.FirstOrDefault(item => item.Id == status)?.Stacks ?? 0;
    private static void Match(BattleTurnState expected, BattleTurnState actual, string action)
    {
        string? difference = ModelJson.Difference(JsonSerializer.Serialize(expected, ModelJson.Options), JsonSerializer.Serialize(actual, ModelJson.Options));
        Require(difference == null, action + " successor differs: " + difference);
    }
    private static void Require(bool condition, string error)
    {
        if (!condition) throw new InvalidDataException(error);
    }
}
