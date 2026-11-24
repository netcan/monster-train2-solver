using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalZarielPainChecks
{
    private const string ChampionId = "d10bb18c-49e9-4895-a1c5-e4ecebbf61a0";
    private const string UpgradeId = "dd5e71e2-6e0d-44fc-a6d9-00fee69cf093";
    private const string AbilityId = "26aafce5-a9c7-4244-84f2-b24b99a92f87";

    internal static void Native(FixtureValue witness)
    {
        FixtureValue authored = witness.GetProperty("Authored");
        FixtureValue effect = authored.GetProperty("Damage");
        Require(Number(witness, "Schema") == 1 && Text(witness, "Boundary") == "OriginalZarielPain" &&
            Text(witness, "GameVersion") == Game231FixtureCatalog.Version &&
            Text(witness, "GameModuleMvid") == Game231FixtureCatalog.Module &&
            Text(authored, "ChampionId") == ChampionId && Text(authored, "UpgradeId") == UpgradeId &&
            Text(authored, "AbilityId") == AbilityId && Number(authored, "Cost") == 0 &&
            Number(authored, "CooldownAtSpawn") == 0 && Number(authored, "CooldownAfterUse") == 1 &&
            Text(effect, "Type") == "CardEffectDamagePerStatusEffect" &&
            Text(effect, "Target") == "DropTargetCharacter" && Text(effect, "Team") == "Heroes" &&
            Number(effect, "Int") == 1 && !Flag(effect, "UseRange") && Flag(effect, "HasUpgrade"),
            "Expected the pinned original Zariel Tier I ability and its authored native effect.");
        FixtureValue status = effect.GetProperty("Statuses").EnumerateArray().Single();
        Require(Text(status, "statusId") == "pain" && Number(status, "count") == 1,
            "Expected the original skill to read the activating unit's Pain.");

        BattleTurnState before = witness.GetProperty("Before").Deserialize<BattleTurnState>()!;
        PlayCardAction action = witness.GetProperty("Action").Deserialize<PlayCardAction>()!;
        RoomCombatState room = before.Spawn.Train.Rooms.Single(candidate => candidate.RoomIndex == action.RoomIndex);
        CombatUnit actor = room.Units.Single(unit => unit.Id == action.ActivatorUnitId);
        CombatUnit target = room.Units.Single(unit => unit.Id == action.TargetUnitId);
        CardPlayRule rule = before.PlayRules!.Cards.Single(card => card.DataId == AbilityId);
        Require(before.Spawn.Turn == 1 && actor.AssetKey == "MonsterChampionPunished2" &&
            actor.Ability is { DataId: AbilityId, FromEquipment: false } &&
            before.Spawn.Train.Context!.FindCard(action.CardInstanceId)?.DataId == AbilityId &&
            rule.Cost == 0 && rule.ExternalInteractions.Count == 0 &&
            rule.Effects.Single() is { Type: MissingHealthStatusModel.CasterDamage, Target: "DropTargetCharacter",
                Value: 1, AllowEnemy: true, AllowPlayer: false, Range: null } projected &&
            projected.Statuses.Single() is { Id: "pain", Stacks: 1 } && target.AssetKey == "ShieldGuardT1_BasicNonAttack",
            "Original Zariel must reach the shared targeted status-damage projection.");

        FixtureValue damage = witness.GetProperty("Damage");
        int pain = Stacks(actor, "pain");
        Require(Flag(witness, "Paid") && Number(witness, "PaidCost") == 0 &&
            Flag(witness, "ObservedScaling") && Flag(witness, "ObservedDamage") && Flag(damage, "Completed") &&
            !Flag(damage, "TargetDestroyed") && Number(damage, "ActorId") == actor.Id && Number(damage, "TargetId") == target.Id &&
            pain == 10 && Number(witness, "StartingPain") == pain && Number(damage, "Pain") == pain &&
            Number(damage, "Coefficient") == 1 && Number(damage, "Requested") == pain &&
            target.Health == 5 && Number(damage, "TargetHealthBefore") == target.Health &&
            Number(damage, "TargetHealthAfter") == target.Health && Stacks(target, "armor") == 25 &&
            Number(damage, "TargetArmorBefore") == Stacks(target, "armor") && Number(damage, "TargetArmorAfter") == 15,
            "Expected a native request of 10, absorbed by the original shield guard's Armor.");

        BattleActionResult played = BattleActionModel.PlayCard(before, action);
        Require(played.Supported, played.Reason ?? "Original Zariel's paid Tier I activation must be supported.");
        BattleTurnState after = RecordedBattleChecks.RecordedBoundary(played.State!, witness.GetProperty("After"),
            terminal: false, "Original Zariel Tier I paid ability", trackPreviewPosition: true);
        CombatUnit afterActor = after.Spawn.Train.Rooms.Single(candidate => candidate.RoomIndex == action.RoomIndex)
            .Units.Single(unit => unit.Id == actor.Id);
        Require(after.Energy == before.Energy && afterActor.Health == actor.Health &&
            Stacks(afterActor, "pain") == pain && Stacks(afterActor, "cooldown") == 1,
            "Original Zariel activation must leave Ember, health and Pain intact and start its native cooldown.");

        PlayCardAction deployment = witness.GetProperty("Deployment").Deserialize<PlayCardAction>()!;
        BattleTurnState beforeDeployment = witness.GetProperty("DeploymentBefore").Deserialize<BattleTurnState>()!;
        Require(actor.SpawnerCardId == deployment.CardInstanceId &&
            beforeDeployment.Spawn.Train.Context!.FindCard(deployment.CardInstanceId)?.DataId == ChampionId,
            "The deployed champion must retain the original paid card identity.");
        BattleActionResult deployed = BattleActionModel.PlayCard(beforeDeployment, deployment);
        Require(deployed.Supported, deployed.Reason ?? "The original upgraded Zariel deployment must be supported.");
        RecordedBattleChecks.RecordedBoundary(deployed.State!, witness.GetProperty("DeploymentAfter"),
            terminal: false, "Original Zariel Tier I deployment", trackPreviewPosition: true);
        Console.WriteLine("NATIVE-ZARIEL-PAIN PASS: exact original deployment and paid Tier I successor, ten Pain damage absorbed by Armor.");
    }

    private static int Stacks(CombatUnit unit, string status) => unit.Status(status)?.Stacks ?? 0;
    private static int Number(FixtureValue value, string property) => value.GetProperty(property).GetInt32();
    private static string? Text(FixtureValue value, string property) => value.GetProperty(property).GetString();
    private static bool Flag(FixtureValue value, string property) => value.GetProperty(property).GetBoolean();
    private static void Require(bool condition, string message) => TrueFinalBossChecks.Require(condition, message);
}
