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
            if (id == "f7126e4a-ef22-4621-b1f7-31a53e2e5870")
            {
                CombatUnit actor = before.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == action.ActivatorUnitId);
                CombatUnit victim = before.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == action.TargetUnitId);
                CombatUnit changed = after.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == actor.Id);
                Require(victim.Id != actor.Id && victim.Team == CombatTeam.Player &&
                    after.Spawn.Train.Rooms[0].Units.All(unit => unit.Id != victim.Id) &&
                    after.Spawn.Train.Context!.Gold == before.Spawn.Train.Context!.Gold + 5 && changed.Attack > actor.Attack,
                    "Chomp must remove a distinct friendly target, fire its activator's positive OnFeed control and then upgrade itself.");
                bool food = victim.Triggers.Any(trigger => trigger.Kind == "OnEaten");
                string pile = food ? "Eaten" : "Exhausted";
                Require(after.Spawn.Train.Context!.OtherPiles!.Any(value => value.Name == pile && value.Cards.Any(token => token.InstanceId == victim.SpawnerCardId)),
                    "Chomp must preserve the distinct food/sacrifice destination: " + pile);
                count++; continue;
            }
            if (id == "4dbfc527-50b2-4068-aa36-9770d4444a57")
            {
                CombatContext old = before.Spawn.Train.Context!, next = after.Spawn.Train.Context!;
                int newlyFrozen = 0, alreadyFrozen = 0;
                foreach (CardToken token in old.Cards.Hand)
                {
                    CardInstanceState original = old.CardRegistry.Single(card => card.InstanceId == token.InstanceId);
                    CardInstanceState frozen = next.CardRegistry.Single(card => card.InstanceId == token.InstanceId);
                    Require(CardBranchMaskModel.Resolve(frozen).Traits.Contains("CardTraitFreeze"), "Time Freeze must freeze every hand card.");
                    Require(frozen.MaskDescriptor!.TemporaryTraits.Count(trait => trait.RuntimeType == "CardTraitFreeze") ==
                        original.MaskDescriptor!.TemporaryTraits.Count(trait => trait.RuntimeType == "CardTraitFreeze") + 1,
                        "Whole-hand freeze must append a new temporary trait even to an already frozen card.");
                    if (CardBranchMaskModel.Resolve(original).Traits.Contains("CardTraitFreeze")) alreadyFrozen++;
                    else newlyFrozen++;
                }
                Require(newlyFrozen >= 2 && alreadyFrozen > 0 &&
                    after.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == action.ActivatorUnitId).Ability?.HasAbility != true,
                    "Time Freeze requires positive ordinary cards, an already frozen control and removal of its own skill.");
                count++; continue;
            }
            if (id is "4252e8ed-4f8f-474f-80be-622eb6dae7fa" or "2479c736-4463-4346-8464-d517dae23f40" or "be65a126-831e-43c1-b987-698c37edead2")
            {
                UnitStatusAbilityChecks.Native(before, after, action, id);
                count++; continue;
            }
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
        if (id is "c59d1814-0e0e-414c-b82f-a71cead2951f" or "127b5582-1e59-4ce0-afdb-9ccc2d59f913" or "1b18c4ef-9df6-466d-b61e-c338800a14a6")
            Require(root.GetProperty("OriginalUnitAbilityQueries").EnumerateArray().Any(query => query.GetProperty("Allowed").GetBoolean()), "Missing native corruption eligibility query.");
        Console.WriteLine("ORIGINAL-UNIT-ABILITY PASS: " + id + "; unchanged native definition and positive skill effect.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
