using System.Security.Cryptography;
using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class LoseMoneyOnHitChecks
{
    private const string HistoricalReceiptHash = "7C28B98F66E3D7A10C2CF1CC134C86646D717E948F5673B94797E282FFB08B55";

    internal static void Native(string path)
    {
        bool historical = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))) == HistoricalReceiptHash;
        using var archive = ModelJson.ReadFixture(path);
        FixtureValue root = archive.RootElement;
        Require(Int(root, "Schema") == 1 && root.GetProperty("Boundary").GetString() == "NativeLoseMoneyOnHit" &&
            root.GetProperty("GameVersion").GetString() == "2.3.1" &&
            root.GetProperty("GameModuleMvid").GetString() == Game231FixtureCatalog.Module &&
            root.GetProperty("OwnerId").GetString() == RelicGoldLossOnHitModel.OwnerId &&
            root.GetProperty("OwnerKey").GetString() == RelicGoldLossOnHitModel.OwnerKey, "Wrong native LoseMoneyOnHit provenance.");
        int actorId = Int(root, "ActorId"), floor = Int(root, "RoomIndex"), damage = Int(root, "Damage");
        var rows = root.GetProperty("Rows").EnumerateArray().ToArray();
        Require(rows.Length == 3 && damage == 1, "Expected three original one-point hits.");
        for (int index = 0; index < rows.Length; index++)
        {
            FixtureValue row = rows[index];
            var before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            var actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            TrainCombatState source = Admission(before.Spawn.Train, historical);
            TrainCombatState expected = Admission(actual.Spawn.Train, historical);
            RoomCombatState input = source.Rooms.Single(room => room.RoomIndex == floor);
            CombatUnit actor = input.Units.Single(unit => unit.Id == actorId);
            CombatTrigger trigger = actor.Triggers.Single(item => item.Kind == "OnHit" &&
                item.Effects.Any(effect => effect.Type == RelicGoldLossOnHitModel.Effect));
            int gold = Math.Max(0, 2 - index), remaining = Math.Max(0, gold - 1);
            Require(RelicGoldLossOnHitModel.OwnsTrigger(input.Context, actor, trigger) &&
                input.Context!.Gold == gold && actual.Spawn.Train.Context!.Gold == remaining &&
                Int(row, "GoldBefore") == gold && Int(row, "GoldAfter") == remaining &&
                Int(row, "HealthBefore") == actor.Health && Int(row, "HealthAfter") == actor.Health - damage &&
                actual.Spawn.Train.Rooms.Single(room => room.RoomIndex == floor).Units.Single(unit => unit.Id == actorId).Health == actor.Health - damage,
                "Original gold-loss or health boundary changed at hit " + index);
            if (index > 0) Require(rows[index - 1].GetProperty("Actual").ContentEquals(row.GetProperty("Before")),
                "Original repeated hit chain changed between settled snapshots.");

            string parent = Json(before);
            RoomCombatResult result = RoomCombatModel.ApplyCardDamage(input, actorId, damage, captureAllDispatches: true);
            Require(result.State != null && result.Outcome == RoomOutcome.Exchanged, result.UnsupportedReason ?? "Did not replay native damage.");
            TrainCombatState train = CardSpellModel.ReplaceRoom(source, result.State!);
            if (before.CanonicalPhysicalReferences) train = TrainCombatModel.CompleteFrameRemovals(train, Array.Empty<int>());
            else if (before.CanonicalDecisionReferences) train = TrainCombatModel.ProcessRemovals(train);
            string? difference = ModelJson.Difference(ComparableTrain(train), ComparableTrain(expected));
            Require(difference == null, "Native gold-loss successor differs at hit " + index + ": " + difference);
            Require(Json(before.RngStreams) == Json(actual.RngStreams) && Json(before) == parent,
                "Native hit changed deterministic RNG or the model mutated its input.");
            if (index == 0)
            {
                CombatContext withoutOwner = input.Context.WithRelics(input.Context.Relics!
                    .Where(relic => relic.DataId != RelicGoldLossOnHitModel.OwnerId).ToArray());
                RoomCombatResult stale = RoomCombatModel.ApplyCardDamage(input.WithContext(withoutOwner), actorId, damage);
                Require(stale.State == null && stale.UnsupportedReason == "Conditional gold costs are not modeled.",
                    "An unbound negative gold trigger was admitted.");
            }
        }
        Console.WriteLine("NATIVE-LOSE-MONEY-ON-HIT PASS: 2→1→0→0, three physical hits, complete successors and RNG.");
    }

    private static TrainCombatState Admission(TrainCombatState train, bool historical)
    {
        if (!historical) return train;
        // The pinned first receipt predates recognition of this original mutator.
        // Preserve the original binary and compare every other captured field.
        Require(train.Rooms.All(room => room.ExternalInteractions.Count(item => item == "Mutator effects") == 1),
            "Historical room must retain its original mutator marker.");
        var rooms = train.Rooms.Select(room => new RoomCombatState(room.RoomIndex, room.Deployment, room.Units,
            room.ExternalInteractions.Where(item => item != "Mutator effects").ToArray(),
            room.Context, room.Preview, room.ActiveCompanionBossIds)).ToArray();
        return new TrainCombatState(rooms, train.Movement, train.EnemySlotsPerRoom, train.Context);
    }

    private static string ComparableTrain(TrainCombatState train) => Json(new
    {
        Rooms = train.Rooms.Select(room => room.WithContext(null)), train.Movement, train.EnemySlotsPerRoom,
        Context = BattleTurnChecks.ComparableContext(train.Context)
    });

    private static int Int(FixtureValue row, string name) => row.GetProperty(name).GetInt32();
    private static string Json<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
