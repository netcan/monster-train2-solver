using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalZarielDevotionChecks
{
    internal static void Native(FixtureValue witness)
    {
        FixtureValue authored = witness.GetProperty("Authored");
        FixtureValue effect = authored.GetProperty("Effect");
        Require(Text(witness, "Boundary") == "OriginalZarielDevotion" &&
            Text(witness, "GameVersion") == Game231FixtureCatalog.Version &&
            Text(witness, "GameModuleMvid") == Game231FixtureCatalog.Module &&
            Text(authored, "Champion") == "PunishedChamp2" && Text(authored, "Upgrade") == "Punished2Path1a" &&
            Text(authored, "Trigger") == "CardSpellPlayed" &&
            Text(effect, "Type") == DevotionModel.NativeDamage && Text(effect, "Target") == "FrontInRoom" &&
            Text(effect, "Team") == "Heroes" && Number(effect, "Int") == 6 && Flag(effect, "Bool") && !Flag(effect, "UseRange"),
            "Expected the original added-base Zariel spell-play payload.");

        var deployment = witness.GetProperty("Deployment").Deserialize<PlayCardAction>()!;
        var placement = witness.GetProperty("DeploymentBefore").Deserialize<BattleTurnState>()!;
        BattleActionResult placed = BattleActionModel.PlayCard(placement, deployment);
        Require(placed.Supported, placed.Reason ?? "Original Zariel deployment was rejected.");
        RecordedBattleChecks.RecordedBoundary(placed.State!, witness.GetProperty("DeploymentAfter"),
            terminal: false, "Zariel Devotion deployment", trackPreviewPosition: true);

        var action = witness.GetProperty("Action").Deserialize<PlayCardAction>()!;
        var before = witness.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var context = before.Spawn.Train.Context!;
        var room = before.Spawn.Train.Rooms.Single(candidate => candidate.RoomIndex == action.RoomIndex);
        var champion = room.Units.Single(unit => unit.SpawnerCardId == deployment.CardInstanceId);
        var incant = champion.Triggers.Single(candidate => candidate.Kind == "CardSpellPlayed");
        Require(incant.Effects.Single() is { Type: DevotionModel.NativeDamage, Value: 6,
            Action: { Type: "Damage", Target: "FrontInRoom", Value: 6, AllowEnemy: true, AllowPlayer: false, Range: null } },
            "Settled Zariel must project the native trigger into ordinary damage.");
        Require(context.QueryFrame?.Devotion is { Level: 0, Points: 0 } &&
            before.PlayRules!.Cards.Single(card => card.DataId == Text(authored, "SpellId")) is { Cost: 3 } venerate &&
            venerate.Effects.Single() is { Type: DevotionModel.Effect, Value: 6 }, "Expected the original paid Venerate at zero Devotion.");

        FixtureValue trigger = witness.GetProperty("Trigger");
        int level = Number(trigger, "Level");
        int amount = 6 * (level + 1);
        var requests = trigger.GetProperty("Requests").EnumerateArray().ToArray();
        var targets = trigger.GetProperty("Targets").EnumerateArray().Select(target => target.GetInt32()).ToArray();
        Require(Flag(trigger, "Completed") && Flag(trigger, "IncludesBase") && Number(trigger, "Actor") == champion.Id &&
            Number(trigger, "SourceCardId") == deployment.CardInstanceId && Number(trigger, "Coefficient") == 6 &&
            level == 1 && targets.Length == 1 && requests.Select(request => Number(request, "TargetId")).SequenceEqual(targets) &&
            requests.All(request => Flag(request, "Completed") && Number(request, "Amount") == amount &&
                Number(request, "SourceCardId") == deployment.CardInstanceId),
            "The native front target must receive exactly twelve damage attributed to Zariel after Venerate awards Devotion.");
        FixtureValue request = requests.Single();
        var target = room.Units.Single(unit => unit.Id == targets[0]);
        Require(target.AssetKey == "ShieldGuardT1_BasicNonAttack" && target.Health == 5 && target.Status("armor")?.Stacks == 25 &&
            Number(request, "HealthBefore") == 5 && Number(request, "HealthAfter") == 5 &&
            Number(request, "ArmorBefore") == 25 && Number(request, "ArmorAfter") == 13,
            "The original front guard must keep its health while Armor absorbs the full hit.");

        BattleActionResult played = BattleActionModel.PlayCard(before, action);
        Require(played.Supported, played.Reason ?? "Original Venerate was rejected.");
        var after = RecordedBattleChecks.RecordedBoundary(played.State!, witness.GetProperty("After"),
            terminal: false, "Zariel Devotion paid spell", trackPreviewPosition: true);
        Require(after.Energy == before.Energy - 3 && Number(witness, "PaidCost") == 3 &&
            after.Spawn.Train.Context!.QueryFrame?.Devotion?.Level == Number(witness, "LevelAfter"),
            "The paid successor must preserve native Ember and Devotion.");
        Console.WriteLine($"NATIVE-ZARIEL-DEVOTION PASS: deployment and paid successor, level {level}, {amount} damage from original Zariel.");
    }

    private static int Number(FixtureValue value, string property) => value.GetProperty(property).GetInt32();
    private static bool Flag(FixtureValue value, string property) => value.GetProperty(property).GetBoolean();
    private static string? Text(FixtureValue value, string property) => value.GetProperty(property).GetString();
    private static void Require(bool condition, string message) => TrueFinalBossChecks.Require(condition, message);
}
