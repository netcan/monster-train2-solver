using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalLazarusDevourChecks
{
    private const string AssistantSubtype = "SubtypesData_Assistant";
    private static readonly string[] AbilityIds =
    [
        "db98983f-fc62-4b7c-9be0-05a1bc270fe1",
        "c2bec532-6a5b-44fa-9c46-0e12510914eb",
        "3eba8cc7-ff26-4ed2-9f59-e7a23d977547"
    ];

    internal static void Native(FixtureValue receipt)
    {
        int rank = Number("Rank");
        Require(Number("Schema") == 1 && Field("Boundary") == "OriginalLazarusDevourAbility" &&
            Field("GameVersion") == Game231FixtureCatalog.Version && Field("GameModuleMvid") == Game231FixtureCatalog.Module &&
            rank is >= 1 and <= 3 && Field("AbilityDataId") == AbilityIds[rank - 1], "Expected the pinned ranked native Devour receipt.");

        var before = receipt.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = receipt.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = receipt.GetProperty("Action").Deserialize<PlayCardAction>()!;
        FixtureValue[] upgrades = receipt.GetProperty("Upgrades").EnumerateArray().ToArray();
        Require(upgrades.Select(tier => tier.GetProperty("AbilityDataId").GetString()).SequenceEqual(AbilityIds.Take(rank)) &&
            upgrades.Select(tier => tier.GetProperty("Multiplier").GetInt32()).SequenceEqual(Enumerable.Range(1, rank)),
            "Native champion upgrade order or Devour multipliers changed.");
        var context = before.Spawn.Train.Context!;
        var championCard = context.FindCard(Number("ChampionCardInstanceId"));
        string abilityId = AbilityIds[rank - 1];
        CardPlayRule ability = before.PlayRules!.Cards.Single(rule => rule.DataId == abilityId);
        Require(championCard?.DataId == Field("ChampionDataId") && championCard.Permanent.Upgrades.Single().DataId ==
            upgrades[^1].GetProperty("Id").GetString() && context.AbilityCardCache?.Any(card =>
                card.InstanceId == action.CardInstanceId && card.DataId == abilityId) == true &&
            ability.Cost == 0 && ability.Effects is
                [{ Type: "TransferAttackFromUnit", Target: "DropTargetCharacter", Value: 1 } transfer,
                 { Type: "Sacrifice", Target: "LastTargetedCharacters", TargetSubtype: AssistantSubtype } sacrifice] &&
            transfer.TransferAttackMultiplier == rank && transfer.Filters?.Subtype == AssistantSubtype &&
            sacrifice.Filters?.Subtype == AssistantSubtype,
            "The native skill does not match its original champion's installed rank and selected-target effects.");

        int actorId = Number("ActorId"), donorId = Number("DonorId"), donorCardId = Number("DonorCardInstanceId");
        var inputRoom = before.Spawn.Train.Rooms.Single(room => room.RoomIndex == action.RoomIndex);
        var outputRoom = actual.Spawn.Train.Rooms.Single(room => room.RoomIndex == action.RoomIndex);
        CombatUnit actor = inputRoom.Units.Single(unit => unit.Id == actorId);
        CombatUnit donor = inputRoom.Units.Single(unit => unit.Id == donorId);
        CombatUnit changed = outputRoom.Units.Single(unit => unit.Id == actorId);
        CombatUnit retained = outputRoom.Units.Single(unit => unit.Id == donorId);
        Require(before.Spawn.Turn == 1 && actual.Spawn.Turn == 1 && before.Energy == actual.Energy &&
            action.ActivatorUnitId == actorId && action.TargetUnitId == donorId && actorId != donorId &&
            actor.Team == CombatTeam.Player && donor.Team == CombatTeam.Player && actor.SpawnerCardId == Number("ChampionCardInstanceId") &&
            donor.SpawnerCardId == donorCardId && donor.Subtypes.Contains(AssistantSubtype) &&
            donor.Attack > 0 && donor.Attack == Number("DonorAttack") &&
            changed.Attack - actor.Attack == rank * donor.Attack && donor.Status("undying")?.Stacks == 1 &&
            retained.Health == 1 && retained.Status("undying") == null && retained.SpawnerCardId == donorCardId &&
            actual.OtherPiles?.Any(pile => pile.Name == "Standby" && pile.Cards.Any(card => card.InstanceId == donorCardId)) == true,
            "The native skill must transfer a positive amount before the sacrifice consumes the original donor's Undying.");

        using var keys = new BattleStateKeys();
        string parent = keys.Key(before);
        var frontier = UnitAbilityModel.EnumerateActivations(before);
        var act = frontier.SupportedCandidates.Single(candidate => candidate.Action.ActivatorUnitId == actorId);
        Require(frontier.UnsupportedAttempts.Count == 0 && act.Action.CardInstanceId == action.CardInstanceId &&
            act.Action.RoomIndex == action.RoomIndex && act.Action.TargetUnitId == donorId,
            "The solver must offer the original Assistant and exclude the champion from this skill's targets.");
        var replay = BattleActionModel.PlayCard(before, action);
        Require(replay.Supported && keys.Key(replay.State!) == keys.Key(actual) && keys.Key(act.Result.State!) == keys.Key(actual),
            "Devour's native successor or its solver candidate differs in full state or RNG. " + replay.Reason);
        using var independent = new BattleStateKeys();
        Require(independent.Key(before) == parent, "Devour mutated the parent decision.");
        Console.WriteLine($"ORIGINAL-LAZARUS-DEVOUR PASS rank={rank} donorAttack={donor.Attack} attack={actor.Attack}->{changed.Attack}, Undying consumed, exact native successor and solver candidate.");

        int Number(string name) => receipt.GetProperty(name).GetInt32();
        string? Field(string name) => receipt.GetProperty(name).GetString();
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
