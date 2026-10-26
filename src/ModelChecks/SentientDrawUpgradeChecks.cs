using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class SentientDrawUpgradeChecks
{
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, CardCorruptionAttackUpgradeModel.Guard);
        Require(rows.Length == 1 && rows[0].GetProperty("Action").GetProperty("CardInstanceId").GetInt32() == 3,
            "Wrong original Sentient deployment boundary.");
        var before = rows[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[0].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var upgrade = before.Spawn.Train.Context!.FindCard(3)!.Permanent.Upgrades.Single();
        var unit = actual.Spawn.Train.Rooms[2].Units.Single(item => item.SpawnerCardId == 3);
        Require(CardSentientDrawUpgradeModel.Known(upgrade) && unit is { AssetKey: "MonsterChampionAwokenHollow", Health: 40, MaxHealth: 40 } &&
            unit.Modifiers is { IsChampion: true, RoomResources.Count: 1 } &&
            unit.Modifiers.RoomResources[0] is { Type: UnitRoomResourceModel.Draw, Amount: 1, DisallowedInDeployment: false } &&
            unit.Triggers.Single().Kind == "OnHit", "Native champion birth lost its draw aura, health or revenge trigger.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            var result = BattleActionModel.PlayCard(before, new PlayCardAction(3, 2, 0));
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Sentient birth differs in an independent branch.");
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Sentient birth mutated its parent.");
        Require(!CardSentientDrawUpgradeModel.Known(upgrade.WithStats(new CardStatModifier(health: 31))) &&
            upgrade.ExternalInteractions.SequenceEqual([CardCorruptionAttackUpgradeModel.Guard]),
            "Changed original signatures or live-unit APIs lost their guard.");
        if (replay.HasValue)
        {
            Native(replay.Value);
            NativeBranchObservationChecks.Pair(source, replay.Value);
        }
        Console.WriteLine("SENTIENT-DRAW-UPGRADE PASS: original champion birth, HP40, draw1 aura and revenge trigger; full state/forecast/RNG; four immutable branches; changed stats guarded; independentPair=" + replay.HasValue);
    }

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
