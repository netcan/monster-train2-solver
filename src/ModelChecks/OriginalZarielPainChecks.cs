using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalZarielPainChecks
{
    private const string ChampionId = "d10bb18c-49e9-4895-a1c5-e4ecebbf61a0";
    private static readonly (string UpgradeId, string AbilityId, int ActorHealth, int TargetHealth, int TargetArmor)[] Ranks =
    {
        ("dd5e71e2-6e0d-44fc-a6d9-00fee69cf093", "26aafce5-a9c7-4244-84f2-b24b99a92f87", 40, 5, 15),
        ("63cc5b53-8269-49bf-8966-4621fe81cf34", "1638d032-3ec9-402c-85ce-195aaf56e17d", 60, 5, 5),
        ("1b2f3a61-a56b-4769-8048-fe3bede6c6c8", "ec73275b-2578-4e2b-babb-ced3b23886c3", 80, 0, 0)
    };

    internal static void Native(FixtureValue witness)
    {
        int tier = witness.TryGetProperty("Tier", out var recordedTier) ? recordedTier.GetInt32() : 1;
        Require(tier >= 1 && tier <= Ranks.Length, "Expected a recorded original Zariel tier.");
        var expected = Ranks[tier - 1];
        string label = $"Original Zariel Tier {tier}";
        FixtureValue authored = witness.GetProperty("Authored");
        FixtureValue effect = authored.GetProperty("Damage");
        Require(Number(witness, "Schema") == 1 && Text(witness, "Boundary") == "OriginalZarielPain" &&
            Text(witness, "GameVersion") == Game231FixtureCatalog.Version &&
            Text(witness, "GameModuleMvid") == Game231FixtureCatalog.Module &&
            Text(authored, "ChampionId") == ChampionId && Text(authored, "UpgradeId") == expected.UpgradeId &&
            Text(authored, "AbilityId") == expected.AbilityId && Number(authored, "Cost") == 0 &&
            Number(authored, "CooldownAtSpawn") == 0 && Number(authored, "CooldownAfterUse") == 1 &&
            Text(effect, "Type") == "CardEffectDamagePerStatusEffect" &&
            Text(effect, "Target") == "DropTargetCharacter" && Text(effect, "Team") == "Heroes" &&
            Number(effect, "Int") == tier && !Flag(effect, "UseRange") && Flag(effect, "HasUpgrade"),
            label + " must retain its authored native effect.");
        FixtureValue status = effect.GetProperty("Statuses").EnumerateArray().Single();
        Require(Text(status, "statusId") == "pain" && Number(status, "count") == 1,
            "Expected the original skill to read the activating unit's Pain.");

        BattleTurnState before = witness.GetProperty("Before").Deserialize<BattleTurnState>()!;
        PlayCardAction action = witness.GetProperty("Action").Deserialize<PlayCardAction>()!;
        RoomCombatState room = before.Spawn.Train.Rooms.Single(candidate => candidate.RoomIndex == action.RoomIndex);
        CombatUnit actor = room.Units.Single(unit => unit.Id == action.ActivatorUnitId);
        CombatUnit target = room.Units.Single(unit => unit.Id == action.TargetUnitId);
        CardPlayRule rule = before.PlayRules!.Cards.Single(card => card.DataId == expected.AbilityId);
        Require(before.Spawn.Turn == 1 && actor.AssetKey == "MonsterChampionPunished2" &&
            actor.Health == expected.ActorHealth && actor.MaxHealth == expected.ActorHealth &&
            actor.Ability is { FromEquipment: false } && actor.Ability.DataId == expected.AbilityId &&
            before.Spawn.Train.Context!.FindCard(action.CardInstanceId)?.DataId == expected.AbilityId &&
            rule.Cost == 0 && rule.ExternalInteractions.Count == 0 &&
            rule.Effects.Single() is { Type: MissingHealthStatusModel.CasterDamage, Target: "DropTargetCharacter",
                AllowEnemy: true, AllowPlayer: false, Range: null } projected && projected.Value == tier &&
            projected.Statuses.Single() is { Id: "pain", Stacks: 1 } && target.AssetKey == "ShieldGuardT1_BasicNonAttack",
            "Original Zariel must reach the shared targeted status-damage projection.");

        FixtureValue damage = witness.GetProperty("Damage");
        int pain = Stacks(actor, "pain");
        // The damage callback sees zero HP before the killed unit is removed from the room.
        Require(Flag(witness, "Paid") && Number(witness, "PaidCost") == 0 &&
            Flag(witness, "ObservedScaling") && Flag(witness, "ObservedDamage") && Flag(damage, "Completed") &&
            !Flag(damage, "TargetDestroyed") && Number(damage, "ActorId") == actor.Id && Number(damage, "TargetId") == target.Id &&
            pain == 10 && Number(witness, "StartingPain") == pain && Number(damage, "Pain") == pain &&
            Number(damage, "Coefficient") == tier && Number(damage, "Requested") == tier * pain &&
            target.Health == 5 && Number(damage, "TargetHealthBefore") == target.Health &&
            Number(damage, "TargetHealthAfter") == expected.TargetHealth && Stacks(target, "armor") == 25 &&
            Number(damage, "TargetArmorBefore") == Stacks(target, "armor") && Number(damage, "TargetArmorAfter") == expected.TargetArmor,
            label + " must apply its Pain-scaled damage through the original shield guard's Armor.");

        BattleActionResult played = BattleActionModel.PlayCard(before, action);
        Require(played.Supported, played.Reason ?? label + " paid activation must be supported.");
        BattleTurnState after = RecordedBattleChecks.RecordedBoundary(played.State!, witness.GetProperty("After"),
            terminal: false, label + " paid ability", trackPreviewPosition: true);
        RoomCombatState afterRoom = after.Spawn.Train.Rooms.Single(candidate => candidate.RoomIndex == action.RoomIndex);
        CombatUnit afterActor = afterRoom.Units.Single(unit => unit.Id == actor.Id);
        CombatUnit? afterTarget = afterRoom.Units.SingleOrDefault(unit => unit.Id == target.Id);
        Require(after.Energy == before.Energy && afterActor.Health == actor.Health &&
            Stacks(afterActor, "pain") == pain && Stacks(afterActor, "cooldown") == 1,
            "Original Zariel activation must leave Ember, health and Pain intact and start its native cooldown.");
        Require(expected.TargetHealth == 0 ? afterTarget == null :
            afterTarget != null && afterTarget.Health == expected.TargetHealth && Stacks(afterTarget, "armor") == expected.TargetArmor,
            label + " must preserve or remove the shield guard after the native damage callback settles.");

        PlayCardAction deployment = witness.GetProperty("Deployment").Deserialize<PlayCardAction>()!;
        BattleTurnState beforeDeployment = witness.GetProperty("DeploymentBefore").Deserialize<BattleTurnState>()!;
        Require(actor.SpawnerCardId == deployment.CardInstanceId &&
            beforeDeployment.Spawn.Train.Context!.FindCard(deployment.CardInstanceId)?.DataId == ChampionId,
            "The deployed champion must retain the original paid card identity.");
        BattleActionResult deployed = BattleActionModel.PlayCard(beforeDeployment, deployment);
        Require(deployed.Supported, deployed.Reason ?? "The original upgraded Zariel deployment must be supported.");
        RecordedBattleChecks.RecordedBoundary(deployed.State!, witness.GetProperty("DeploymentAfter"),
            terminal: false, label + " deployment", trackPreviewPosition: true);
        Console.WriteLine($"NATIVE-ZARIEL-PAIN PASS tier={tier}: exact original deployment and paid successor, {tier * pain} damage against Armor.");
    }

    private static int Stacks(CombatUnit unit, string status) => unit.Status(status)?.Stacks ?? 0;
    private static int Number(FixtureValue value, string property) => value.GetProperty(property).GetInt32();
    private static string? Text(FixtureValue value, string property) => value.GetProperty(property).GetString();
    private static bool Flag(FixtureValue value, string property) => value.GetProperty(property).GetBoolean();
    private static void Require(bool condition, string message) => TrueFinalBossChecks.Require(condition, message);
}
