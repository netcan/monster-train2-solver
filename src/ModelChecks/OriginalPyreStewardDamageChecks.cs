using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalPyreStewardDamageChecks
{
    internal static void Native(FixtureValue witness)
    {
        FixtureValue definition = witness.GetProperty("Definition");
        Require(witness.GetProperty("Schema").GetInt32() == 1 &&
            Text(witness, "Boundary") == "OriginalPyreStewardDamage" &&
            Text(witness, "GameVersion") == Game231FixtureCatalog.Version &&
            Text(witness, "GameModuleMvid") == Game231FixtureCatalog.Module &&
            Text(definition, "StewardId") == "e90fc0e0-b383-48a9-bb10-d8aaa1518531" &&
            Text(definition, "AbilityId") == CardSpellModel.PyreStewardAbilityId &&
            Number(definition, "Cost") == 0 && Number(definition, "CooldownAfterUse") == 3,
            "Expected the pinned original Pyre Steward ability.");
        FixtureValue[] authored = definition.GetProperty("Effects").EnumerateArray().ToArray();
        Require(authored.Length == 2 && Text(authored[0], "Target") == "Pyre" && Number(authored[0], "ParamInt") == 3 &&
            Text(authored[1], "Type") == "CardEffectDamagePerPyreAttack" && Text(authored[1], "Target") == "Room" &&
            Text(authored[1], "Team") == "Heroes" && Number(authored[1], "ParamInt") == 1 &&
            Number(authored[1], "AdditionalParamInt") == 0 && !authored[1].GetProperty("UsesRange").GetBoolean(),
            "The native Pyre Steward damage sequence differs.");

        var before = witness.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var action = witness.GetProperty("Action").Deserialize<PlayCardAction>()!;
        CombatContext context = before.Spawn.Train.Context!;
        RoomCombatState pyreRoom = before.Spawn.Train.Rooms.Single(room => room.Units.Any(unit => unit.IsPyre));
        CombatUnit pyre = pyreRoom.Units.Single(unit => unit.IsPyre);
        RoomCombatState targetRoom = before.Spawn.Train.Rooms.Single(room => room.RoomIndex == action.RoomIndex);
        CombatUnit actor = targetRoom.Units.Single(unit => unit.Id == action.ActivatorUnitId);
        Require(before.Spawn.Turn == 1 && actor.AssetKey == "MonsterPyreSteward" &&
            context.FindCard(action.CardInstanceId)?.DataId == CardSpellModel.PyreStewardAbilityId,
            "Expected the original first-turn activating unit and ability card.");

        FixtureValue scaling = witness.GetProperty("Scaling");
        int displayed = Number(scaling, "DisplayedPyreAttack");
        Require(scaling.GetProperty("Completed").GetBoolean() && displayed > 0 &&
            scaling.GetProperty("BeforeRng").ContentEquals(scaling.GetProperty("AfterRng")) &&
            context.Resources?.PyreBaseAttack == Number(scaling, "AuthoredPyreAttack") &&
            context.Resources.PyreModifiedBaseAttack == Number(scaling, "SavedAttackOffset") &&
            RelicResourceModel.AttackDamage(pyre, context, pyreRoom) == displayed,
            "Live and saved Pyre attack inputs differ from the native ability query.");
        int extra = 7;
        CombatContext changed = context.WithResources(context.Resources!.WithPyreModifiedBaseAttack(
            context.Resources.PyreModifiedBaseAttack + extra));
        Require(RelicResourceModel.AttackDamage(pyre, changed, pyreRoom.WithContext(changed)) == displayed + extra &&
            RelicResourceModel.AttackDamage(pyre, context, pyreRoom) == displayed,
            "A branch's saved attack offset must participate exactly once without mutating its parent.");

        FixtureValue[] requests = witness.GetProperty("DamageRequests").EnumerateArray().ToArray();
        Require(requests.Length == 3 && !requests[0].GetProperty("Scaled").GetBoolean(),
            "Expected the fixed Pyre damage followed by two scaled requests.");
        FixtureValue pyreHit = requests[0].GetProperty("Damage");
        Require(Number(pyreHit, "TargetId") == pyre.Id && Number(pyreHit, "RoomIndex") == pyreRoom.RoomIndex &&
            Number(pyreHit, "Amount") == 3 && Number(pyreHit, "HealthBefore") == pyre.Health &&
            Number(pyreHit, "HealthAfter") == pyre.Health - 3 && pyreHit.GetProperty("TargetsLivePyre").GetBoolean() &&
            !pyreHit.GetProperty("TargetPreview").GetBoolean(), "The original first effect must damage the live Pyre by three.");
        VerifyPyreHealth(pyreHit.GetProperty("PyreBefore"), pyre.Health);
        int pyreHealth = pyre.Health - 3;
        VerifyPyreHealth(pyreHit.GetProperty("PyreAfter"), pyreHealth);
        VerifyPyreHealth(scaling.GetProperty("PyreBefore"), pyreHealth);
        FixtureValue[] scaled = scaling.GetProperty("Requests").EnumerateArray().ToArray();
        int[] targets = scaling.GetProperty("Targets").Deserialize<int[]>()!;
        Require(scaled.Length == 2 && targets.Length == 2 && targets.Distinct().Count() == 2 &&
            targets.SequenceEqual(scaled.Select(request => Number(request, "TargetId"))), "Scaled targets and requests differ.");
        for (int index = 0; index < targets.Length; ++index)
        {
            FixtureValue request = scaled[index];
            var enemy = targetRoom.Units.Single(unit => unit.Id == targets[index]);
            Require(requests[index + 1].GetProperty("Scaled").GetBoolean() &&
                request.ContentEquals(requests[index + 1].GetProperty("Damage")) &&
                enemy.Team == CombatTeam.Enemy && Text(request, "Team") == "Heroes" &&
                Number(request, "RoomIndex") == targetRoom.RoomIndex &&
                Number(request, "Amount") == displayed && Number(request, "HealthBefore") == enemy.Health &&
                Number(request, "HealthAfter") == 0 && !request.GetProperty("TargetPreview").GetBoolean() &&
                !request.GetProperty("TargetsLivePyre").GetBoolean(), "Original scaled damage differs for " + enemy.AssetKey);
            VerifyPyreHealth(request.GetProperty("PyreBefore"), pyreHealth);
            pyreHealth = Math.Min(pyre.MaxHealth, pyreHealth + 2);
            VerifyPyreHealth(request.GetProperty("PyreAfter"), pyreHealth);
        }
        VerifyPyreHealth(scaling.GetProperty("PyreAfter"), pyreHealth);
        VerifyPyreHealth(witness.GetProperty("SettledPyre"), pyreHealth);

        var cast = BattleActionModel.PlayCard(before, action);
        Require(cast.Supported, cast.Reason ?? "The original Pyre Steward ability must be supported.");
        BattleTurnState after = RecordedBattleChecks.RecordedBoundary(cast.State!, witness.GetProperty("After"),
            terminal: false, "Original Pyre Steward paid ability", trackPreviewPosition: true);
        Require(after.Energy == before.Energy, "The original ability is zero Ember.");
        Console.WriteLine("NATIVE-PYRE-STEWARD-DAMAGE PASS: exact paid ability successor, three native damage requests, two Slay heals and saved attack offset control.");
    }

    private static void VerifyPyreHealth(FixtureValue sample, int health) =>
        Require(Number(sample, "SavedHealth") == health && Number(sample, "LiveHealth") == health &&
            !sample.GetProperty("SavePreview").GetBoolean() && !sample.GetProperty("LivePreview").GetBoolean(),
            "Expected matching saved and live Pyre health after native damage and Slay healing.");

    private static int Number(FixtureValue value, string property) => value.GetProperty(property).GetInt32();
    private static string? Text(FixtureValue value, string property) => value.GetProperty(property).GetString();
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
