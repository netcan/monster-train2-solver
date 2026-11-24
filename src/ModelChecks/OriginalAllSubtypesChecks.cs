using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalAllSubtypesChecks
{
    private const string Fungus = "SubtypesData_FunGuy";
    private const string Horde = "SubtypesData_Horde";

    private sealed record ActorQuery(string Key, bool IsChampion, bool IsPyre, bool Actual);
    private sealed record ActorRow(string Phase, string Role, int ActorId, bool Alive, bool IsPyre, string[] RawSubtypes, ActorQuery[] Queried);
    private sealed record DeckRow(string Phase, string Key, bool IsChampion, int[] Matched);
    private sealed record MaskRow(string Phase, string Kind, string Mode, int SubjectId, CardUpgradeMaskRule Mask, bool Actual);
    private sealed record SpawnRow(string Phase, string Duration, string Key, int SubtypeCount, int AllCount);

    internal static void Battle(string path)
    {
        using var document = ModelJson.ReadFixture(path);
        FixtureValue root = document.RootElement;
        var turns = root.GetProperty("Turns").EnumerateArray().ToArray();
        Require(turns.Length >= 5, "The native mutator archive must reach a regular terminal battle.");
        foreach (FixtureValue turn in turns)
        {
            var before = turn.GetProperty("Before").Deserialize<BattleTurnState>()!;
            var context = before.Spawn.Train.Context!;
            Require(context.Relics?.Count(OriginalAllSubtypesModel.IsOwner) == 1 &&
                OriginalAllSubtypesModel.Active(context, before.Spawn.Turn == 0) && RelicModel.Validate(context) == null,
                "The exact AllUnitsAllSubtypes owner must be active from native deployment through each combat turn.");
        }
        if (root.GetProperty("OriginalKillCopyPrepared").GetBoolean())
            OriginalKillCopyChecks.Native(root, allSubtypes: true);
        SolverStageChecks.Native(path);
    }

    internal static void Native(FixtureValue root)
    {
        FixtureValue owner = root.GetProperty("Owner");
        Require(root.GetProperty("Schema").GetInt32() == 1 &&
            root.GetProperty("Boundary").GetString() == "OriginalAllUnitsAllSubtypesQueries" &&
            root.GetProperty("GameVersion").GetString() == Game231FixtureCatalog.Version &&
            root.GetProperty("GameModuleMvid").GetString() == Game231FixtureCatalog.Module &&
            owner.GetProperty("Id").GetString() == OriginalAllSubtypesModel.OwnerId &&
            owner.GetProperty("AssetKey").GetString() == "AllUnitsAllSubtypes" &&
            owner.GetProperty("Effect").GetString() == OriginalAllSubtypesModel.Effect && owner.GetProperty("Conditions").GetInt32() == 0,
            "Wrong original subtype query witness.");

        var before = root.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var active = root.GetProperty("Active").Deserialize<BattleTurnState>()!;
        CombatContext Context(string phase) => (phase == "Before" ? before : active).Spawn.Train.Context!;
        CombatUnit Actor(string phase, int id) => (phase == "Before" ? before : active).Spawn.Train.Rooms
            .SelectMany(room => room.Units).Single(unit => unit.Id == id);
        bool Active(string phase) => OriginalAllSubtypesModel.Active(Context(phase), deployment: false);
        var activeContext = Context("Active");
        Require(before.Spawn.Turn == 1 && active.Spawn.Turn == 1 && !Active("Before") && Active("Active") &&
            activeContext.Relics!.Count(OriginalAllSubtypesModel.IsOwner) == 1 &&
            activeContext.Relics!.Single(OriginalAllSubtypesModel.IsOwner).DisallowedInPlacementPhase == false &&
            RelicModel.Validate(Context("Before")) == null && RelicModel.Validate(activeContext) == null &&
            root.GetProperty("Before").GetProperty("RngStreams").ContentEquals(root.GetProperty("Active").GetProperty("RngStreams")),
            "Native owner, settled decision, gameplay RNG or production admission differs.");
        var exactOwner = activeContext.Relics!.Single(OriginalAllSubtypesModel.IsOwner);
        Require(RelicModel.Validate([exactOwner, exactOwner]) != null &&
            RelicModel.Validate([new CombatRelicState(exactOwner.DataId, "changed", exactOwner.EffectTypes,
                isCovenant: false, disallowedInPlacementPhase: false)]) != null &&
            RelicModel.Validate([new CombatRelicState(exactOwner.DataId, exactOwner.AssetKey, exactOwner.EffectTypes,
                isCovenant: false, disallowedInPlacementPhase: true)]) != null,
            "Duplicate, changed or deployment-disallowed all-subtypes owner was admitted.");

        ActorRow[] actors = root.GetProperty("Actors").Deserialize<ActorRow[]>()!;
        Require(actors.Select(row => (row.Phase, row.Role)).SequenceEqual(new[] {
            ("Before", "Friendly"), ("Before", "Foe"), ("Before", "Pyre"), ("Active", "Friendly"), ("Active", "Foe"),
            ("Active", "Pyre"), ("Dying", "Friendly") }), "Missing native live and dying actor controls.");
        string[] keys = actors[0].Queried.Select(query => query.Key).ToArray();
        Require(keys.Length == 6 && keys.Distinct().Count() == 6 && keys.Contains(Fungus) && keys.Contains(Horde) &&
            actors[0].Queried.Count(query => query.IsChampion) == 1 && actors[0].Queried.Count(query => query.IsPyre) == 1,
            "Missing distinct native subtype query keys.");
        foreach (ActorRow row in actors)
        {
            CombatUnit actor = Actor(row.Phase, row.ActorId);
            Require(actor.Subtypes.SequenceEqual(row.RawSubtypes) && actor.IsPyre == row.IsPyre &&
                actor.Team == (row.Role == "Foe" ? CombatTeam.Enemy : CombatTeam.Player) && row.Alive == (row.Phase != "Dying") &&
                row.Queried.Select(query => query.Key).SequenceEqual(keys), "Wrong actor/query identity at " + row.Phase + "/" + row.Role);
            foreach (ActorQuery query in row.Queried)
                Require(OriginalAllSubtypesModel.MatchesCharacter(actor.Team, actor.IsPyre, actor.Subtypes, query.Key, query.IsPyre, Active(row.Phase)) == query.Actual,
                    "Native character subtype query differs at " + row.Phase + "/" + row.Role + "/" + query.Key);
        }
        Require(actors[0].ActorId == actors[3].ActorId && actors[3].ActorId == actors[6].ActorId &&
            !actors[0].Queried.Single(query => query.Key == Fungus).Actual && actors[6].Queried.Single(query => query.Key == Fungus).Actual &&
            !actors[6].Queried.Single(query => query.Key == Horde).Actual, "Missing dying ordinary-monster wildcard or original Horde control.");
        foreach (ActorRow row in actors.Where(row => row.Role == "Friendly" && row.Alive))
        {
            TrainCombatState train = (row.Phase == "Before" ? before : active).Spawn.Train;
            RoomCombatState room = train.Rooms.Single(floor => floor.Units.Any(unit => unit.Id == row.ActorId));
            int position = Array.FindIndex(room.Units.Where(unit => unit.Team == CombatTeam.Player).ToArray(), unit => unit.Id == row.ActorId);
            bool wildcard = row.Queried.Single(query => query.Key == Fungus).Actual;
            CardActionEffect Effect(string mode, string required, IReadOnlyList<string> excluded) => new("Damage", mode,
                1, true, true, [], filters: new CardTargetFilters("Both", [], [], false, required, excluded));
            var requiredFungus = CardTargetModel.Collect(room, Effect("DropTargetCharacter", Fungus, []), [], CombatTeam.Player, position);
            var excludedFungus = CardTargetModel.Collect(train, room.RoomIndex, Effect("Tower", "", [Fungus]), []);
            var requiredHorde = CardTargetModel.Collect(room, Effect("Room", Horde, []), []);
            var sacrifice = new CardActionEffect("Sacrifice", "RandomInRoom", 1, false, true, [], targetSubtype: Fungus);
            var damageByKills = new CardActionEffect("DamageByUnitsKilled", "Room", 1, true, true, [], targetSubtype: Fungus);
            var sacrificeCandidates = CardTargetModel.RandomCandidates(train, room.RoomIndex, sacrifice);
            Require(requiredFungus.Supported && requiredFungus.UnitIds.Contains(row.ActorId) == wildcard &&
                excludedFungus.Supported && excludedFungus.UnitIds.Contains(row.ActorId) != wildcard &&
                requiredHorde.Supported && !requiredHorde.UnitIds.Contains(row.ActorId) &&
                sacrificeCandidates.Supported && sacrificeCandidates.UnitIds.Contains(row.ActorId) == wildcard &&
                CardSpellModel.TestCount(train, damageByKills, new CardTargets([row.ActorId])) == (wildcard ? 1 : 0),
                "Target filters and sacrifice callbacks must apply the captured native character subtype matching.");

            var healthRule = new RelicPercentageStat(0, 100, true, "Monsters", Fungus, multiplier: 2f);
            var health = RelicPercentageStatModel.Apply(room, row.ActorId, healthRule);
            CombatUnit original = Actor(row.Phase, row.ActorId);
            var deathTrigger = new CombatTrigger("OnDeath", false, false, false, 1, []);
            var triggerRule = new RelicCharacterTriggers(0, true, false, [deathTrigger], false, Fungus, subtypeIsNone: false);
            // This older component receipt predates the whole-battle capture gate.
            var triggerRoom = new RoomCombatState(room.RoomIndex, room.Deployment, room.Units,
                room.ExternalInteractions.Where(reason => reason != "Mutator effects" &&
                    reason != $"Unmodeled relic effect {OriginalAllSubtypesModel.Effect} on {OriginalAllSubtypesModel.OwnerKey}.").ToArray(),
                room.Context, room.Preview, room.ActiveCompanionBossIds);
            var trigger = RelicCharacterTriggerModel.Apply(triggerRoom, row.ActorId, triggerRule);
            Require(health.Supported && health.State!.Units.Single(unit => unit.Id == row.ActorId).MaxHealth ==
                (wildcard ? original.MaxHealth * 2 : original.MaxHealth), "Percentage stats did not use the native subtype query on birth.");
            Require(trigger.Supported && trigger.State!.Units.Single(unit => unit.Id == row.ActorId).Triggers.Count ==
                original.Triggers.Count + (wildcard ? 1 : 0),
                "Trigger installation did not use the native subtype query on birth: " + trigger.UnsupportedReason);
        }

        DeckRow[] deck = root.GetProperty("Deck").Deserialize<DeckRow[]>()!;
        Require(deck.Length == 10 && deck.Select(row => (row.Phase, row.Key)).Distinct().Count() == 10, "Missing paired deck results.");
        foreach (DeckRow row in deck)
        {
            CombatContext context = Context(row.Phase);
            var cards = context.CardInstances!.Where(card => card.MaskDescriptor != null).ToArray();
            int[] matched = cards.Where(card => OriginalAllSubtypesModel.MatchesDeckCard(card.MaskDescriptor!.Definition, row.Key, Active(row.Phase)))
                .Select(card => card.InstanceId).OrderBy(id => id).ToArray();
            var query = new CardStatisticQuery("SubtypeInDeck", subtypeIsNone: false, subtype: row.Key,
                subtypeMatchDataIds: cards.Where(card => OriginalAllSubtypesModel.MatchesDeckCard(card.MaskDescriptor!.Definition, row.Key, false))
                    .Select(card => card.DataId).ToArray());
            ExpectStatistic(context, query, row.Matched.Length);
            Require(matched.SequenceEqual(row.Matched), "Native deck subtype membership differs at " + row.Phase + "/" + row.Key);
        }
        Require(deck.Single(row => row.Phase == "Active" && row.IsChampion).Matched.SequenceEqual(
            deck.Single(row => row.Phase == "Before" && row.IsChampion).Matched) &&
            deck.Single(row => row.Phase == "Active" && row.Key == Horde).Matched.Length > 1,
            "Missing Champion and Horde deck controls.");

        MaskRow[] masks = root.GetProperty("Masks").Deserialize<MaskRow[]>()!;
        Require(masks.Length == 8 && masks.Select(row => (row.Phase, row.Kind, row.Mode)).Distinct().Count() == 8,
            "Missing two required/excluded mask pairs for card and character.");
        foreach (MaskRow row in masks)
        {
            CombatUnit actor = Actor(row.Phase, actors[0].ActorId);
            bool predicted = row.Kind == "Card" ? CardUpgradeMaskModel.FilterCard(row.Mask,
                CardBranchMaskModel.Resolve(Context(row.Phase).FindCard(row.SubjectId)!), Active(row.Phase)) :
                CardUpgradeMaskModel.FilterCharacter(row.Mask, new CardUpgradeMaskCharacter(actor.Subtypes,
                    actor.Statuses.Select(status => status.Id).ToArray(), actor.Size), Active(row.Phase));
            Require(predicted == row.Actual && row.Actual == Active(row.Phase), "Native subtype mask differs at " + row.Phase + "/" + row.Kind + "/" + row.Mode);
        }

        SpawnRow[] counts = root.GetProperty("SpawnCounts").Deserialize<SpawnRow[]>()!;
        Require(counts.Length == 20 && counts.Select(row => (row.Phase, row.Duration, row.Key)).Distinct().Count() == 20 &&
            counts.Any(row => row.Phase == "Before" && row.Key == Horde && row.AllCount > row.SubtypeCount), "Missing distinct native spawn count controls.");
        foreach (SpawnRow row in counts)
        {
            CombatContext context = Context(row.Phase);
            ExpectStatistic(context, new CardStatisticQuery("MonsterSubtypePlayed", row.Duration, subtype: row.Key), row.SubtypeCount);
            ExpectStatistic(context, new CardStatisticQuery("AnyMonsterSpawned", row.Duration), row.AllCount);
        }
        Console.WriteLine("ORIGINAL-ALL-UNIT-SUBTYPES PASS: 42 native actor matches, 10 original deck filters, eight mask checks, 20 spawn-query pairs and unchanged gameplay RNG.");

        static void ExpectStatistic(CombatContext context, CardStatisticQuery query, int actual)
        {
            StatisticQueryResult result = StatisticQueryModel.Evaluate(context, query, 0);
            Require(result.Supported && result.Value == actual, "Native statistic differs for " + query.Type + "/" + query.Duration + "/" + query.Subtype +
                ": " + (result.Supported ? result.Value + " versus " + actual : result.UnsupportedReason));
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
