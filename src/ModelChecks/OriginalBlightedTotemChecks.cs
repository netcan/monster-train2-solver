using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalBlightedTotemChecks
{
    private const string Totem = "21ad4f07-1568-4461-8be4-6fb2670bd90d";
    private const string Torch = "f1dbd7d5-74fc-4507-aba1-dc1a8ec9393b";

    internal static void Native(FixtureValue receipt)
    {
        Require(Number("Schema") == 1 && Field("Boundary") == "OriginalBlightedTotemCardSpellPlayed" &&
            Field("GameVersion") == Game231FixtureCatalog.Version && Field("GameModuleMvid") == Game231FixtureCatalog.Module &&
            Field("TotemId") == Totem && Field("TorchId") == Torch, "Expected the pinned native Blighted Totem receipt.");
        var before = receipt.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = receipt.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = receipt.GetProperty("Action").Deserialize<PlayCardAction>()!;
        int totemId = Number("TotemUnitId"), totemCardId = Number("TotemCardInstanceId"), guardId = Number("ShieldGuardId");
        RoomCombatState oldRoom = before.Spawn.Train.Rooms.Single(room => room.RoomIndex == action.RoomIndex);
        RoomCombatState newRoom = actual.Spawn.Train.Rooms.Single(room => room.RoomIndex == action.RoomIndex);
        CombatUnit totem = oldRoom.Units.Single(unit => unit.Id == totemId);
        CombatUnit settledTotem = newRoom.Units.Single(unit => unit.Id == totemId);
        CombatUnit guard = oldRoom.Units.Single(unit => unit.Id == guardId);
        CombatUnit hit = newRoom.Units.Single(unit => unit.Id == guardId);
        var context = before.Spawn.Train.Context!;
        CombatTrigger trigger = totem.Triggers.Single(item => item.Kind == "CardSpellPlayed");
        Require(before.Spawn.Turn == 1 && actual.Spawn.Turn == 1 && action.RoomIndex == 0 && action.TargetUnitId == guardId &&
            before.Energy - actual.Energy == 1 && context.FindCard(action.CardInstanceId)?.DataId == Torch &&
            context.FindCard(totemCardId)?.DataId == Totem && totem.SpawnerCardId == totemCardId &&
            totem.AssetKey == "MonsterBlightedTotem" && totem.Team == CombatTeam.Player &&
            !trigger.HasTriggered && settledTotem.Triggers.Single(item => item.Kind == "CardSpellPlayed").HasTriggered &&
            trigger.Effects is [{ Action: { Type: "AddStatus", Target: "Room", AllowEnemy: true, AllowPlayer: false } poison }] &&
            poison.Statuses is [{ Id: "poison", Stacks: 2 }] && guard.Health == 5 && hit.Health == 5 &&
            guard.Status("armor")?.Stacks == 25 && hit.Status("armor")?.Stacks == 22,
            "Expected an original paid Torch against the natural shield and the Totem's two-Poison payload.");
        foreach (CombatUnit unit in oldRoom.Units.Where(unit => unit.Health > 0))
        {
            int oldPoison = unit.Status("poison")?.Stacks ?? 0;
            int newPoison = newRoom.Units.Single(candidate => candidate.Id == unit.Id).Status("poison")?.Stacks ?? 0;
            Require(newPoison - oldPoison == (unit.Team == CombatTeam.Enemy ? 2 : 0),
                "The original trigger did not apply Poison to the expected same-floor enemies.");
        }

        using var keys = new BattleStateKeys();
        string parent = keys.Key(before);
        string expected = keys.Key(actual);
        var replay = BattleActionModel.PlayCard(before, action);
        Require(replay.Supported && replay.Outcome == RoomOutcome.Exchanged && keys.Key(replay.State!) == expected,
            "The native Totem cast differs from its full model successor or RNG. " + replay.Reason);
        var frontier = BattleActionModel.EnumerateActions(before, action.CardInstanceId, includeAbilities: false);
        Require(frontier.UnsupportedAttempts.Count == 0 && frontier.SupportedCandidates.Any(candidate =>
            candidate.Action.RoomIndex == action.RoomIndex && candidate.Action.TargetUnitId == guardId && keys.Key(candidate.Result.State!) == expected),
            "The exact native Totem cast is missing from the solver's candidates.");
        using var independent = new BattleStateKeys();
        Require(independent.Key(before) == parent, "The native Totem cast replay mutated its parent.");
        Console.WriteLine("ORIGINAL-BLIGHTED-TOTEM PASS: original same-floor enemy Poison, exact successor, RNG and solver candidate.");

        int Number(string name) => receipt.GetProperty(name).GetInt32();
        string? Field(string name) => receipt.GetProperty(name).GetString();
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
