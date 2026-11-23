using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class PlacementMutatorChecks
{
    internal static void Native(FixtureValue root)
    {
        if (root.GetProperty("Schema").GetInt32() != 1 ||
            root.GetProperty("Boundary").GetString() != "NativePlacementFloorAndUnitCardSize" ||
            root.GetProperty("GameVersion").GetString() != Game231FixtureCatalog.Version ||
            root.GetProperty("GameModuleMvid").GetString() != Game231FixtureCatalog.Module)
            throw new InvalidDataException("Expected the original 2.3.1 deployment component.");

        FixtureValue[] owners = root.GetProperty("Owners").EnumerateArray().ToArray();
        if (owners.Length is < 1 or > 2 || !Owner(0, RelicModel.DeploymentSummonBlockOwnerId,
                RelicModel.DeploymentSummonBlockOwnerKey, RelicModel.DeploymentSummonBlock, RelicModel.DeploymentSummonBlockMask) ||
            owners.Length == 2 && !Owner(1, "af623737-a208-4b85-bce3-e1066b4f4bcd", "BigMonsters", "RelicEffectModifyMonsterCapacity", 1))
            throw new InvalidDataException("Expected the scoped original authored effects.");

        bool sizeChanged = owners.Length == 2;
        FixtureValue deployment = root.GetProperty("Deployment"), ordinary = root.GetProperty("Ordinary");
        CheckDecision(deployment, 0);
        CheckDecision(ordinary, 1);
        if (sizeChanged)
        {
            Console.WriteLine("NATIVE-PLACEMENT-MUTATORS PASS floor masks and original +1 unit card size.");
            return;
        }

        BattleTurnState before = deployment.GetProperty("BattleState").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = ordinary.GetProperty("BattleState").Deserialize<BattleTurnState>()!;
        if (before.PlayRules == null || actual.PlayRules == null ||
            !MatchRooms(before.PlayRules.Rooms, deployment.GetProperty("Rooms").Deserialize<RoomPlayRule[]>()!) ||
            !MatchRooms(actual.PlayRules.Rooms, ordinary.GetProperty("Rooms").Deserialize<RoomPlayRule[]>()!) ||
            before.Spawn.Turn != 0 || actual.Spawn.Turn != 1 ||
            before.Spawn.Train.Context!.Relics!.Count(RelicModel.IsOriginalDeploymentSummonBlock) != 1 ||
            actual.Spawn.Train.Context!.Relics!.Count(RelicModel.IsOriginalDeploymentSummonBlock) != 1)
            throw new InvalidDataException("Expected full native deployment / ordinary decisions and matching room rules.");
        BattleTurnResult predicted = BattleTurnModel.EndTurn(before);
        if (!predicted.Supported || predicted.Outcome != RoomOutcome.Exchanged)
            throw new InvalidDataException("Original deployment EndTurn failed: " + predicted.UnsupportedReason);
        RecordedBattleChecks.Compare(predicted.State!, actual, false, "original deployment summon restriction");
        Console.WriteLine("NATIVE-PLACEMENT-SUMMON-BLOCK PASS complete native deployment EndTurn and reopened floors.");

        bool Owner(int index, string id, string key, string type, int parameter)
        {
            FixtureValue owner = owners[index];
            return owner.GetProperty("Id").GetString() == id && owner.GetProperty("Key").GetString() == key &&
                owner.GetProperty("Type").GetString() == type && owner.GetProperty("Parameter").GetInt32() == parameter &&
                owner.GetProperty("Flag").GetBoolean();
        }

        void CheckDecision(FixtureValue decision, int turn)
        {
            RoomPlayRule[] rooms = decision.GetProperty("Rooms").Deserialize<RoomPlayRule[]>()!;
            if (decision.GetProperty("Turn").GetInt32() != turn || decision.GetProperty("IsPlacementPhase").GetBoolean() != (turn == 0) ||
                !rooms.Where(room => !room.IsPyre).Select(room => room.RoomIndex).SequenceEqual(new[] { 0, 1, 2 }) ||
                rooms.Any(room => room.SummonBlocked != (turn == 0 && RelicModel.IsDeploymentBlockedRoom(room))))
                throw new InvalidDataException("Native floor mask differs at decision " + turn);
            FixtureValue[] cards = decision.GetProperty("CardSizes").EnumerateArray().ToArray();
            if (cards.Length == 0 || sizeChanged && !cards.Any(row => row.GetProperty("OriginalSize").GetInt32() < 6))
                throw new InvalidDataException("Expected at least one applicable original unit card.");
            foreach (FixtureValue row in cards)
            {
                int original = row.GetProperty("OriginalSize").GetInt32();
                if (!Guid.TryParse(row.GetProperty("DataId").GetString(), out _) || original is < 1 or > 6 ||
                    row.GetProperty("EffectiveSize").GetInt32() != Math.Min(6, original + (sizeChanged ? 1 : 0)))
                    throw new InvalidDataException("Native unit card size differs at decision " + turn);
            }
        }
    }

    private static bool MatchRooms(IReadOnlyList<RoomPlayRule> a, IReadOnlyList<RoomPlayRule> b) =>
        a.Count == b.Count && a.Zip(b).All(pair => pair.First.RoomIndex == pair.Second.RoomIndex &&
            pair.First.SummonBlocked == pair.Second.SummonBlocked && pair.First.IsPyre == pair.Second.IsPyre &&
            pair.First.Enabled == pair.Second.Enabled && pair.First.PlayerCapacity == pair.Second.PlayerCapacity &&
            pair.First.PlayerSlots == pair.Second.PlayerSlots && pair.First.EnemyCapacity == pair.Second.EnemyCapacity);
}
