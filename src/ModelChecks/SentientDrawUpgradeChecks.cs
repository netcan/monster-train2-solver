using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class SentientDrawUpgradeChecks
{
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, CardCorruptionAttackUpgradeModel.Guard);
        int cardId = rows[0].GetProperty("Action").GetProperty("CardInstanceId").GetInt32();
        Require(rows.Length == 1 && cardId is 3 or 6,
            "Wrong original Sentient deployment boundary.");
        var before = rows[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[0].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var upgrade = before.Spawn.Train.Context!.FindCard(cardId)!.Permanent.Upgrades.Single();
        bool second = upgrade.DataId == CardSentientDrawUpgradeModel.SecondDataId;
        int health = second ? 80 : 40;
        var unit = actual.Spawn.Train.Rooms[2].Units.Single(item => item.SpawnerCardId == cardId);
        Require(cardId == (second ? 6 : 3) && CardSentientDrawUpgradeModel.Known(upgrade) &&
            unit is { AssetKey: "MonsterChampionAwokenHollow" } && unit.Health == health && unit.MaxHealth == health &&
            unit.Modifiers is { IsChampion: true, RoomResources.Count: 1 } &&
            unit.Modifiers.RoomResources[0] is { Type: UnitRoomResourceModel.Draw, Amount: 1, DisallowedInDeployment: false } &&
            unit.Triggers.Single().Kind == "OnHit", "Native champion birth lost its draw aura, health or revenge trigger.");
        var buff = unit.Triggers.Single().Effects.Single().UnitUpgrade;
        Require(buff is { Target: "Weakest", AllowPlayer: true, AllowEnemy: false } &&
            buff.Upgrade!.Stats.Damage == (second ? 3 : 2) && buff.Upgrade.Stats.Health == (second ? 3 : 2),
            "Native tier-specific revenge upgrade lost its exact target or stats.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            var result = BattleActionModel.PlayCard(before, new PlayCardAction(cardId, 2, 0));
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Sentient birth differs in an independent branch.");
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Sentient birth mutated its parent.");
        Require(!CardSentientDrawUpgradeModel.Known(upgrade.WithStats(new CardStatModifier(health: upgrade.Stats.Health + 1))) &&
            upgrade.ExternalInteractions.SequenceEqual([CardCorruptionAttackUpgradeModel.Guard]),
            "Changed original signatures or live-unit APIs lost their guard.");
        Require(!CardSentientDrawUpgradeModel.KnownIdentity(upgrade.DataId, second ?
            CardSentientDrawUpgradeModel.AssetKey : CardSentientDrawUpgradeModel.SecondAssetKey),
            "Crossed tier identities must remain guarded.");
        if (replay.HasValue)
        {
            Native(replay.Value);
            NativeBranchObservationChecks.Pair(source, replay.Value);
        }
        Console.WriteLine($"SENTIENT-DRAW-UPGRADE PASS: original champion tier{(second ? 2 : 1)} birth, HP{health}, draw1 aura and revenge trigger; full state/forecast/RNG; four immutable branches; changed stats guarded; independentPair=" + replay.HasValue);
    }

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
