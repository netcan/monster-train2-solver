using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class FhyraGoldChecks
{
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 1 &&
            root.GetProperty("Boundary").GetString() == "OriginalFhyraGoldOnPyreDamage" &&
            root.GetProperty("GameVersion").GetString() == Game231FixtureCatalog.Version &&
            root.GetProperty("GameModuleMvid").GetString() == Game231FixtureCatalog.Module &&
            root.GetProperty("OwnerId").GetString() == RelicResourceModel.FhyraOwnerId &&
            root.GetProperty("OwnerKey").GetString() == RelicResourceModel.FhyraOwnerKey &&
            root.GetProperty("PyreId").GetString() == "6cccb808-8af1-4ca9-b7ba-7ffafb06abc4" &&
            root.GetProperty("Reward").GetInt32() == 10, "Expected the original Fhyra Pyre and ten-gold artifact.");

        int pyreId = root.GetProperty("PyreUnitId").GetInt32(), attackerId = root.GetProperty("AttackerId").GetInt32();
        FixtureValue[] rows = root.GetProperty("Rows").EnumerateArray().ToArray();
        string[] kinds = ["ZeroFlagged", "UnflaggedLoss", "NativeAttackTower"];
        Require(rows.Select(row => row.GetProperty("Kind").GetString()).SequenceEqual(kinds), "Native Fhyra controls are missing or reordered.");
        BattleTurnState? previous = null;
        for (int index = 0; index < rows.Length; index++)
        {
            FixtureValue row = rows[index], beforeValue = row.GetProperty("Before"), afterValue = row.GetProperty("After");
            string kind = kinds[index];
            var before = Read(beforeValue);
            var after = Read(afterValue);
            int damage = row.GetProperty("Damage").GetInt32();
            int reward = index == 2 ? 10 : 0;
            Require(damage == (index == 0 ? 0 : 1) && before.Health - after.Health == damage &&
                after.Gold - before.Gold == reward, kind + " has the wrong health and gold transition.");
            Require(beforeValue.GetProperty("Decision").GetProperty("RngStreams").ContentEquals(
                afterValue.GetProperty("Decision").GetProperty("RngStreams")) &&
                before.Room.Context!.BattleRng.Equals(after.Room.Context!.BattleRng),
                kind + " changed the captured gameplay randomness.");
            if (previous != null) RecordedBattleChecks.Compare(previous, before.Decision, terminal: false, "Before " + kind);
            previous = after.Decision;

            Callback entry = new("Adjust.Enter", -damage, index != 1, before.Health, before.Gold);
            Callback exit = new("Adjust.Exit", -damage, index != 1, after.Health, after.Gold);
            Callback[] expected = index != 2 ? [entry, exit] :
            [
                entry,
                new("Dispatcher.Enter", damage, null, after.Health, before.Gold),
                new("Fhyra.Enter", damage, null, after.Health, before.Gold),
                new("Fhyra.Exit", damage, null, after.Health, after.Gold),
                new("Dispatcher.Exit", damage, null, after.Health, after.Gold),
                exit
            ];
            Require(row.GetProperty("Calls").EnumerateArray().Select(ReadCall).SequenceEqual(expected),
                kind + " changed the native tower HP, dispatcher or Fhyra callback order.");

            if (index == 0) RecordedBattleChecks.Compare(before.Decision, after.Decision, terminal: false, kind);
            if (index == 2)
            {
                Require(before.Decision.Spawn.Train.Rooms.SelectMany(room => room.Units)
                    .Any(unit => unit.Id == attackerId && unit.Team == CombatTeam.Enemy && unit.Health > 0),
                    "Native tower attack did not identify a living original enemy.");
                RoomCombatResult modeled = RoomCombatModel.ApplyCardDamage(before.Room, pyreId, damage);
                Require(modeled.State != null && modeled.Outcome != RoomOutcome.Unsupported,
                    "The existing combat damage pipeline rejected the native Pyre hit: " + modeled.UnsupportedReason);
                Require(modeled.State!.Units.Single(unit => unit.Id == pyreId).Health == after.Health &&
                    modeled.State.Context!.Gold == after.Gold, "Modeled tower damage differs from the native Fhyra HP/gold transition.");
                CompareContext(modeled.State.Context, after.Room.Context!, "Native tower hit");
            }
        }
        Console.WriteLine("NATIVE-FHYRA-GOLD PASS: zero flagged loss, unflagged loss, ordered native damage callback and ten-gold combat successor with identical gameplay RNG.");

        (BattleTurnState Decision, RoomCombatState Room, int Health, int Gold) Read(FixtureValue snapshot)
        {
            BattleTurnState decision = snapshot.GetProperty("Decision").Deserialize<BattleTurnState>()!;
            RoomCombatState room = decision.Spawn.Train.Rooms.Single(candidate => candidate.Units.Any(unit => unit.Id == pyreId));
            CombatUnit pyre = room.Units.Single(unit => unit.Id == pyreId);
            int health = snapshot.GetProperty("Health").GetInt32(), gold = snapshot.GetProperty("Gold").GetInt32();
            CombatRelicState owner = decision.Spawn.Train.Context!.Relics!.Single(item => item.DataId == RelicResourceModel.FhyraOwnerId);
            Require(decision.Spawn.Turn == 1 && !room.Preview && pyre.IsPyre && pyre.AssetKey == "PyreHeartFhyra" &&
                pyre.Health == health && room.Context?.Gold == gold && decision.Spawn.Train.Context.Gold == gold &&
                owner.ResourceEffects is [RelicResourceEffect effect] && RelicResourceModel.IsFhyraGold(owner, effect),
                "Native tower HP, gold or exact Fhyra payload differs from the captured combat state.");
            return (decision, room, health, gold);
        }
    }

    private static Callback ReadCall(FixtureValue call)
    {
        FixtureValue trigger = call.GetProperty("TriggerDamageTakenRelicEffects");
        return new(call.GetProperty("Stage").GetString()!, call.GetProperty("Amount").GetInt32(),
            trigger.ValueKind == FixtureKind.Null ? null : trigger.GetBoolean(),
            call.GetProperty("Health").GetInt32(), call.GetProperty("Gold").GetInt32());
    }

    private static void CompareContext(CombatContext predicted, CombatContext actual, string label)
    {
        string? difference = ModelJson.Difference(JsonSerializer.Serialize(BattleTurnChecks.ComparableContext(predicted), ModelJson.Options),
            JsonSerializer.Serialize(BattleTurnChecks.ComparableContext(actual), ModelJson.Options));
        Require(difference == null, label + " context differs: " + difference);
    }

    private readonly record struct Callback(string Stage, int Amount, bool? Triggers, int Health, int Gold);

    private static void Require(bool valid, string message)
    {
        if (!valid) throw new InvalidDataException(message);
    }
}
