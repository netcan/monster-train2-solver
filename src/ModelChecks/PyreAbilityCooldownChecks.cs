using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class PyreAbilityCooldownChecks
{
    internal static void Native(FixtureValue positive, FixtureValue negative)
    {
        (TrainCombatState train, RoomCombatState room, CombatUnit pyre) = Read(positive, true);
        (TrainCombatState control, _, CombatUnit baseline) = Read(negative, false);
        CombatUnit beforeBirth = AbilityCooldownModel.Copy(pyre, pyre.Ability!.WithCooldown(baseline.Ability!.Cooldown));
        RoomCombatState beforeRoom = room.WithUnits(room.Units.Select(unit => unit.Id == pyre.Id ? beforeBirth : unit).ToArray());
        var before = new TrainCombatState(train.Rooms.Select(frame => frame.RoomIndex == room.RoomIndex ? beforeRoom : frame).ToArray(),
            train.Movement, train.EnemySlotsPerRoom, train.Context);
        using var keys = new BattleStateKeys();
        string key = keys.SnapshotKey(before);
        RelicBirthResult modeled = RelicBirthModel.CharacterAdded(before, pyre.Id, 0, false, true, []);
        Require(modeled.Supported && modeled.Queued.Count == 0 && modeled.Dispatched.Count == 0,
            "Original Pyre birth did not settle: " + modeled.UnsupportedReason);
        CombatUnit result = modeled.State!.Rooms.Single(frame => frame.RoomIndex == room.RoomIndex).Units.Single(unit => unit.Id == pyre.Id);
        Require(JsonSerializer.Serialize(result, ModelJson.Options) == JsonSerializer.Serialize(pyre, ModelJson.Options) &&
            modeled.State.Context!.BattleRng.Equals(before.Context!.BattleRng) && keys.SnapshotKey(before) == key,
            "The cooldown mutator must change only the original Pyre's after-activation interval.");

        RelicBirthResult unchanged = RelicBirthModel.CharacterAdded(control, baseline.Id, 0, false, true, []);
        Require(unchanged.Supported && unchanged.State!.Rooms.SelectMany(frame => frame.Units).Single(unit => unit.Id == baseline.Id)
            .Ability?.Cooldown == baseline.Ability.Cooldown, "Control birth changed cooldown without the native owner.");
        Console.WriteLine("NATIVE-PYRE-ABILITY-COOLDOWN PASS: original callback 99 to 2, control stays 99, initial and remaining cooldown stay zero.");
    }

    private static (TrainCombatState Train, RoomCombatState Room, CombatUnit Pyre) Read(FixtureValue root, bool withOwner)
    {
        const string pyreId = "0ff61363-ab58-4873-9a26-18db51eacd46";
        const string abilityId = "4dbfc527-50b2-4068-aa36-9770d4444a57";
        Require(root.GetProperty("Schema").GetInt32() == 1 && root.GetProperty("Boundary").GetString() == "OriginalPyreAbilityCooldownBirth" &&
            root.GetProperty("GameVersion").GetString() == Game231FixtureCatalog.Version && root.GetProperty("GameModuleMvid").GetString() == Game231FixtureCatalog.Module &&
            root.GetProperty("OwnerId").GetString() == RelicPyreAbilityCooldownModel.OwnerId && root.GetProperty("OwnerKey").GetString() == RelicPyreAbilityCooldownModel.OwnerKey &&
            root.GetProperty("Effect").GetString() == RelicPyreAbilityCooldownModel.Effect && root.GetProperty("ParamInt").GetInt32() == 2 &&
            root.GetProperty("ParamInt2").GetInt32() == 2 && root.GetProperty("HasOwner").GetBoolean() == withOwner &&
            root.GetProperty("PyreId").GetString() == pyreId && root.GetProperty("AbilityId").GetString() == abilityId,
            "Expected original Time Pyre, cooldown owner and callback parameters.");
        FixtureValue before = root.GetProperty("CallbackBefore"), after = root.GetProperty("CallbackAfter");
        Require(withOwner ? IsSample(before, 99) && IsSample(after, 2) : before.ValueKind == FixtureKind.Null && after.ValueKind == FixtureKind.Null,
            "Expected the original birth callback only with the owner.");

        int cooldown = withOwner ? 2 : 99;
        var decision = root.GetProperty("Decision").Deserialize<BattleTurnState>()!;
        TrainCombatState train = decision.Spawn.Train;
        int id = root.GetProperty("UnitId").GetInt32();
        RoomCombatState room = train.Rooms.Single(frame => frame.Units.Any(unit => unit.Id == id));
        CombatUnit pyre = room.Units.Single(unit => unit.Id == id);
        Require(IsSample(root.GetProperty("Settled"), cooldown) && decision.Spawn.Turn == 0 && pyre.IsPyre && pyre.SpawnerCardId == 0 &&
            pyre.Ability is { CooldownAtSpawn: 0 } ability && ability.DataId == abilityId && ability.Cooldown == cooldown &&
            (pyre.Status("cooldown")?.Stacks ?? 0) == 0 &&
            train.Context!.Relics?.Any(owner => owner.EffectTypes.Contains(RelicPyreAbilityCooldownModel.Effect) &&
                RelicPyreAbilityCooldownModel.IsOriginal(owner)) == withOwner,
            "Settled deployment did not preserve the original Pyre ability boundary.");
        return (train, room, pyre);
    }

    private static bool IsSample(FixtureValue value, int cooldown) => value.ValueKind == FixtureKind.Object &&
        value.GetProperty("AfterActivation").GetInt32() == cooldown && value.GetProperty("AtSpawn").GetInt32() == 0 &&
        value.GetProperty("Remaining").GetInt32() == 0;

    private static void Require(bool valid, string message)
    {
        if (!valid) throw new InvalidDataException(message);
    }
}
