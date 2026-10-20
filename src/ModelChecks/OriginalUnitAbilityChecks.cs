using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalUnitAbilityChecks
{
    internal static void Native(FixtureValue root)
    {
        if (!root.TryGetProperty("OriginalUnitAbilityId", out var value) || value.ValueKind != FixtureKind.String) return;
        string id = value.GetString()!;
        int count = 0;
        foreach (FixtureValue row in root.GetProperty("Actions").EnumerateArray())
        {
            PlayCardAction action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
            if (action.ActivatorUnitId == 0) continue;
            BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!, after = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            CardPlayRule rule = before.PlayRules!.Cards.Single(rule => rule.DataId == id);
            CardInstanceState card = before.Spawn.Train.Context!.CardRegistry.Single(card => card.InstanceId == action.CardInstanceId);
            Require(card.DataId == id && rule.Ability?.Kind == "Unit" && rule.ExternalInteractions.Count == 0, "Missing unchanged original unit skill.");
            if (id == "8d54f68b-5660-4507-84c3-a0cf092bfbf1")
            {
                CardGenerationRule generation = rule.Effects.Single().Generation!;
                CardToken generated = after.Spawn.Train.Context!.Cards.Hand.Single(token => before.Spawn.Train.Context.Cards.Hand.All(old => old.InstanceId != token.InstanceId));
                CardInstanceState created = after.Spawn.Train.Context.CardRegistry.Single(card => card.InstanceId == generated.InstanceId);
                Require(generation.OptionalUpgradeFilters?.Count > 0 && generation.Pool.Any(choice => choice.DataId == created.DataId) &&
                    created.Temporary.Upgrades.Single().AssetKey == "FetchCostReduction" &&
                    created.Temporary.Upgrades.Single().Stats.Cost == -1 && CardBranchMaskModel.Resolve(created).Traits.Contains("CardTraitEphemeral"),
                    "Fetch must generate an original equipment pool member with its filtered temporary discount and Ephemeral trait.");
                count++; continue;
            }
            int sourceRoom = before.Spawn.Train.Rooms.Single(room => room.Units.Any(unit => unit.Id == action.ActivatorUnitId)).RoomIndex;
            CardSourceTrait trait = card.MaskDescriptor!.SourceTraits!.Single(trait => trait.Type == CardCorruptRestrictedModel.Trait);
            int consumedRoom = trait.Bool ? sourceRoom : action.RoomIndex;
            foreach (RoomCapacityState old in before.Spawn.Train.Context.RoomCapacities!)
                Require(after.Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == old.RoomIndex).Corruption ==
                    old.Corruption - (old.RoomIndex == consumedRoom ? trait.Int : 0), "Corrupt Restricted consumed the wrong floor or amount.");
            if (id == "c59d1814-0e0e-414c-b82f-a71cead2951f")
                Require(sourceRoom == 0 && action.RoomIndex == 1 && trait.Bool && trait.Int == 1 &&
                    before.Spawn.Train.Context.RoomCapacities!.Single(room => room.RoomIndex == 1).Corruption == 0 &&
                    after.Spawn.Train.Rooms[1].Units.Any(unit => unit.Id == action.ActivatorUnitId), "Bog Fly must consume source corruption and move to a zero-corruption destination.");
            else if (id == "127b5582-1e59-4ce0-afdb-9ccc2d59f913")
                Require(trait.Int == 4 && !trait.Bool && before.Spawn.Train.Rooms[0].Units.Any(unit => unit.Id == action.TargetUnitId && unit.Health > 0) &&
                    after.Spawn.Train.Rooms[0].Units.All(unit => unit.Id != action.TargetUnitId), "Glugsider must kill a living non-boss target after consuming four corruption.");
            else if (id == "1b18c4ef-9df6-466d-b61e-c338800a14a6")
                Require(trait.Int == 3 && !trait.Bool && after.Spawn.Train.Context!.Cards.Hand.Count == before.Spawn.Train.Context.Cards.Hand.Count + 2 &&
                    after.Spawn.Train.Context.Cards.Draw.Count == before.Spawn.Train.Context.Cards.Draw.Count - 2,
                    "Extract Draw must draw two waiting cards after consuming three corruption.");
            else throw new InvalidOperationException("Missing original skill checker: " + id);
            int nextSourceRoom = after.Spawn.Train.Rooms.Single(room => room.Units.Any(unit => unit.Id == action.ActivatorUnitId)).RoomIndex;
            Require(CardCorruptRestrictedModel.Test(after.Spawn.Train.Context!, card, action.RoomIndex, out bool allowed, nextSourceRoom) == null && !allowed,
                "The depleted skill must fail its modeled corruption gate.");
            var queries = root.GetProperty("OriginalUnitAbilityQueries").EnumerateArray().ToArray();
            Require(queries.Any(query => !query.GetProperty("Allowed").GetBoolean()) && queries.All(query =>
                query.GetProperty("Allowed").GetBoolean() == (query.GetProperty(trait.Bool ? "SourceCorruption" : "SelectedCorruption").GetInt32() >= trait.Int)),
                "Native positive and negative eligibility queries must use the correct floor.");
            count++;
        }
        Require(count == 1, "Expected one positive original unit skill activation.");
        if (id != "8d54f68b-5660-4507-84c3-a0cf092bfbf1")
            Require(root.GetProperty("OriginalUnitAbilityQueries").EnumerateArray().Any(query => query.GetProperty("Allowed").GetBoolean()), "Missing native corruption eligibility query.");
        Console.WriteLine("ORIGINAL-UNIT-ABILITY PASS: " + id + "; unchanged native definition and positive skill effect.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
