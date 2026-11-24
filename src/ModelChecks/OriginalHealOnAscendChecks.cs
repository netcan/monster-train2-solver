using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalHealOnAscendChecks
{
    internal static void Check(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 1 &&
            root.GetProperty("Boundary").GetString() == "OriginalHealOnAscend" &&
            root.GetProperty("GameVersion").GetString() == "2.3.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            root.GetProperty("OwnerId").GetString() == RelicAscensionHealModel.DataId &&
            root.GetProperty("HealAmount").GetInt32() == 150 &&
            root.GetProperty("ExcludedSubtypes").EnumerateArray().Select(item => item.GetString())
                .SequenceEqual([RelicAscensionHealModel.Boss, RelicAscensionHealModel.TreasureCollector]),
            "Unexpected native Ascend trial receipt.");
        BattleTurnState before = root.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = root.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatRelicState owner = before.Spawn.Train.Context!.Relics!.Single(item => item.DataId == RelicAscensionHealModel.DataId);
        Require(owner.ResourceEffects is [{ } rule] && RelicAscensionHealModel.Validate(owner, rule) == null,
            "Native trial definition did not reach the battle model.");
        var observations = root.GetProperty("Observations").EnumerateArray().ToArray();
        int woundedActor = root.GetProperty("WoundedActor").GetInt32();
        FixtureValue wounded = observations.Single(item => item.GetProperty("ActorId").GetInt32() == woundedActor);
        Require(observations.Length == 2 && wounded.GetProperty("Team").GetString() == "Heroes" &&
            wounded.GetProperty("Room").GetInt32() == 1 && wounded.GetProperty("Before").GetInt32() == 44 &&
            wounded.GetProperty("After").GetInt32() == 45 && wounded.GetProperty("Maximum").GetInt32() == 45 &&
            observations.Any(row => row.GetProperty("Before").GetInt32() == 1 && row.GetProperty("After").GetInt32() == 1),
            "The native callbacks did not cover the wounded and full-health controls.");
        TrainCombatState train = before.Spawn.Train;
        TrainCombatResult moved = TrainCombatModel.Ascend(train);
        Require(moved.Supported && moved.Outcome == RoomOutcome.Cleared,
            "Ascend trial movement failed. " + moved.UnsupportedReason);
        var healEvents = moved.RoomResults.SelectMany(result => result.Events)
            .Where(item => item.Kind == "RelicAscensionHeal").ToArray();
        foreach (FixtureValue row in observations)
        {
            int actorId = row.GetProperty("ActorId").GetInt32();
            int room = row.GetProperty("Room").GetInt32();
            int previousHealth = row.GetProperty("Before").GetInt32();
            int health = row.GetProperty("After").GetInt32();
            Require(train.Rooms.SelectMany(item => item.Units).Single(unit => unit.Id == actorId).Health == previousHealth &&
                UnitHealth(moved.State!, room, actorId) == health && UnitHealth(actual.Spawn.Train, room, actorId) == health &&
                healEvents.Count(item => item.Actor == actorId && item.Target == actorId && item.Amount == health - previousHealth) == 1,
                "Ascend trial movement changed the native actor or its health.");
        }
        Require(healEvents.Length == observations.Length, "The modeled movement healed an extra actor.");
        CombatContext context = train.Context!;
        TrainCombatState withoutOwner = CardSpellModel.WithContext(train,
            context.WithRelics(context.Relics!.Where(item => item.DataId != owner.DataId).ToArray()));
        TrainCombatResult control = TrainCombatModel.Ascend(withoutOwner);
        Require(control.Supported && UnitHealth(control.State!, 1, woundedActor) == 44 &&
            control.State!.Context!.BattleRng.Equals(moved.State!.Context!.BattleRng),
            "Ascend trial client ablation changed movement or consumed gameplay RNG.");
        Console.WriteLine("ORIGINAL-HEAL-ON-ASCEND PASS: native 44→45 and 1→1 post-ascension callbacks, modeled movement and independent owner ablation.");
    }

    private static int UnitHealth(TrainCombatState train, int room, int actorId) =>
        train.Rooms.Single(item => item.RoomIndex == room).Units.Single(unit => unit.Id == actorId).Health;

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
