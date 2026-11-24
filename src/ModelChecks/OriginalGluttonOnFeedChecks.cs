using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalGluttonOnFeedChecks
{
    private const string Glutton = "aa94ef0c-0fd8-4d42-992f-25345d3717b5";
    private const string Torch = "f1dbd7d5-74fc-4507-aba1-dc1a8ec9393b";
    private const string Miner = "536e6de0-7ccf-412a-9a4e-ec55ab3988e5";
    private const string Feast = "236215da-c1dd-40b7-8793-27ce327a7e78";

    internal static void Native(FixtureValue receipt)
    {
        Require(Number("Schema") == 1 && Field("Boundary") == "OriginalGluttonOnFeed" &&
            Field("GameVersion") == Game231FixtureCatalog.Version && Field("GameModuleMvid") == Game231FixtureCatalog.Module &&
            Field("GluttonId") == Glutton && Field("TorchId") == Torch && Field("MinerId") == Miner && Field("FeastId") == Feast,
            "Expected the pinned native MonsterGlutton OnFeed receipt.");
        var steps = receipt.GetProperty("Steps").EnumerateArray().ToArray();
        Require(steps.Select(step => step.GetProperty("Label").GetString()).SequenceEqual(["Torch", "Miner", "Feast"]),
            "Expected the original damage, food deployment and Feast in order.");
        string[] cardIds = [Torch, Miner, Feast];
        int feederId = Number("GluttonUnitId"), shieldId = Number("ShieldGuardId"), minerId = Number("MinerCardInstanceId");
        using var keys = new BattleStateKeys();
        BattleTurnState? settled = null;
        for (int index = 0; index < steps.Length; index++)
        {
            var before = Read(steps[index], "Before");
            var actual = Read(steps[index], "Actual");
            var action = steps[index].GetProperty("Action").Deserialize<PlayCardAction>()!;
            string parent = keys.Key(before);
            Require(before.Spawn.Turn == 1 && actual.Spawn.Turn == 1 && action.RoomIndex == 0 &&
                before.Spawn.Train.Context!.FindCard(action.CardInstanceId)?.DataId == cardIds[index] &&
                (settled == null || keys.Key(settled) == parent), "The native action chain changed its root or original card identity.");
            var replay = BattleActionModel.PlayCard(before, action);
            Require(replay.Supported && replay.Outcome == RoomOutcome.Exchanged && keys.Key(replay.State!) == keys.Key(actual),
                "The original " + cardIds[index] + " did not replay its complete state and RNG. " + replay.Reason);
            var frontier = BattleActionModel.EnumerateActions(before, action.CardInstanceId, includeAbilities: false);
            Require(frontier.UnsupportedAttempts.Count == 0 && frontier.SupportedCandidates.Any(candidate =>
                candidate.Action.RoomIndex == action.RoomIndex && candidate.Action.TargetUnitId == action.TargetUnitId &&
                candidate.Action.PlayerPosition == action.PlayerPosition && keys.Key(candidate.Result.State!) == keys.Key(actual)),
                "The recorded original feeding setup action was omitted from the solver frontier.");
            using var independent = new BattleStateKeys();
            Require(independent.Key(before) == parent, "Replaying or enumerating the action changed its parent.");
            settled = actual;
        }

        BattleTurnState first = Read(steps[0], "Before");
        BattleTurnState meal = Read(steps[2], "Before");
        BattleTurnState after = settled!;
        CombatUnit virgin = Unit(first, feederId), hungry = Unit(meal, feederId), fed = Unit(after, feederId);
        CombatUnit guard = Unit(meal, shieldId), hit = Unit(after, shieldId);
        CombatUnit miner = meal.Spawn.Train.Rooms[0].Units.Single(unit => unit.SpawnerCardId == minerId);
        CombatTrigger onFeed = hungry.Triggers.Single(trigger => trigger.Kind == "OnFeed");
        Require(hungry.AssetKey == "MonsterGlutton" && hungry.SpawnerCardId == Number("GluttonCardInstanceId") &&
            virgin.Health == 50 && hungry.Health == 47 && hungry.MaxHealth == 50 &&
            onFeed.Effects is [{ Action: { Type: "Heal", Target: "Self", Value: 10 } },
                              { Action: { Type: "Damage", Target: "Room", Value: 20 } }] &&
            fed.Health == 59 && fed.MaxHealth == 59 && fed.Health - hungry.Health == 12 &&
            guard.Team == CombatTeam.Enemy && guard.Health == 5 && hit.Health == guard.Health &&
            guard.Status("armor")?.Stacks == 25 && hit.Status("armor")?.Stacks == 5 &&
            after.Spawn.Train.Rooms[0].Units.All(unit => unit.Id != miner.Id) &&
            after.OtherPiles?.Any(pile => pile.Name == "Eaten" && pile.Cards.Any(card => card.InstanceId == minerId)) == true,
            "Expected the original meal to restore missing HP, damage the same-floor guard and consume the source-backed Miner.");
        Console.WriteLine("ORIGINAL-GLUTTON-ONFEED PASS: exact paid action chain and RNG, self heal, 20 same-floor damage and original Miner consumption.");

        int Number(string name) => receipt.GetProperty(name).GetInt32();
        string? Field(string name) => receipt.GetProperty(name).GetString();
        static BattleTurnState Read(FixtureValue step, string name) => step.GetProperty(name).Deserialize<BattleTurnState>()!;
        static CombatUnit Unit(BattleTurnState state, int id) => state.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == id);
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
