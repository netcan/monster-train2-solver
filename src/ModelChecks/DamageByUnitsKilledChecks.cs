using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class DamageByUnitsKilledChecks
{
    internal static void Native(FixtureValue fixture)
    {
        bool forwardsSource = fixture.GetProperty("GameVersion").GetString() == Game231FixtureCatalog.Version;
        bool higherRanks = fixture.GetProperty("ModifierScenario").GetString() == "damage-by-units-killed-higher-ranks";
        var spellDamage = higherRanks
            ? new Dictionary<string, int> { ["fb0d1638-347d-4b4a-98c9-96bff42a968e"] = 40, ["c807999e-f9b9-423e-9af1-d52666127082"] = 60 }
            : new Dictionary<string, int> { ["d9c0c510-ccb5-4a89-9f3b-99799e765540"] = 20 };
        if (higherRanks)
            Require(forwardsSource && fixture.GetProperty("GameModuleMvid").GetString() == Game231FixtureCatalog.Module,
                "Expected the pinned original Shardtail higher-rank battle.");
        var observed = new HashSet<string>();
        int matched = 0;
        int wildcardChampionCasts = 0;
        int wildcardMixedCasts = 0;
        foreach (FixtureValue entry in fixture.GetProperty("Actions").EnumerateArray())
        {
            BattleTurnState before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
            PlayCardAction action = entry.GetProperty("Action").Deserialize<PlayCardAction>()!;
            CardToken? spell = before.Spawn.Train.Context!.Cards.Hand.FirstOrDefault(card =>
                card.InstanceId == action.CardInstanceId && spellDamage.ContainsKey(card.DataId));
            if (spell == null) continue;
            observed.Add(spell.DataId);
            RoomCombatState room = before.Spawn.Train.Rooms.Single(item => item.RoomIndex == action.RoomIndex);
            const string impSubtype = "SubtypesData_Imp_0f9b989f-15b5-4b16-8378-5d8ed8691e7c";
            int[] imps = room.Units.Where(unit => unit.Team == CombatTeam.Player && unit.Subtypes.Contains(impSubtype))
                .Select(unit => unit.Id).ToArray();
            int[] heroes = room.Units.Where(unit => unit.Team == CombatTeam.Enemy).Select(unit => unit.Id).ToArray();
            BattleActionResult result = BattleActionModel.PlayCard(before, action);
            BattleTurnState actual = entry.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            RoomCombatState after = actual.Spawn.Train.Rooms.Single(item => item.RoomIndex == action.RoomIndex);
            if (OriginalAllSubtypesModel.Active(before.Spawn.Train.Context, room.Deployment))
            {
                Require(result.Supported && ModelJson.Difference(BattleTurnChecks.Comparable(result.State!),
                    BattleTurnChecks.Comparable(actual)) == null, "Shardtail wildcard cast differs from the native successor.");
                CombatUnit? champion = room.Units.SingleOrDefault(unit => unit.Modifiers?.IsChampion == true);
                CombatUnit[] nonChampions = room.Units.Where(unit => unit.Team == CombatTeam.Player && unit.Id != champion?.Id).ToArray();
                Require(nonChampions.Any(unit => !imps.Contains(unit.Id)) &&
                    nonChampions.All(unit => after.Units.All(retained => retained.Id != unit.Id)) &&
                    heroes.Length > 0 && after.Units.All(unit => !heroes.Contains(unit.Id)),
                    "The original wildcard must sacrifice a non-Imp and damage the enemies.");
                if (champion != null)
                {
                    Require(nonChampions.Length == 1 && imps.Length == 0 && heroes.Length == 2 &&
                        after.Units.Any(unit => unit.Id == champion.Id), "The original wildcard must preserve the champion.");
                    wildcardChampionCasts++;
                }
                else
                {
                    Require(nonChampions.Length == 2 && imps.Length == 1, "Expected a native Imp and non-Imp sacrifice together.");
                    wildcardMixedCasts++;
                }
                matched++;
                continue;
            }
            Require(result.Supported && imps.Length > 0 && heroes.Length > 0 &&
                imps.All(id => after.Units.All(unit => unit.Id != id)) &&
                heroes.Any(id => room.Units.Single(unit => unit.Id == id).Health >
                    (after.Units.FirstOrDefault(unit => unit.Id == id)?.Health ?? 0)),
                "Original Shardtail action did not sacrifice an Imp and damage an enemy.");
            if (forwardsSource)
            {
                var effect = before.PlayRules!.Cards.Single(rule => rule.DataId == spell.DataId).Effects.Single();
                var actualStats = actual.Spawn.Train.Context!.Statistics!;
                var beforeStats = before.Spawn.Train.Context.Statistics!;
                int kills = heroes.Count(id => after.Units.All(unit => unit.Id != id));
                Require(effect.UseCardTraits && effect.Type == "DamageByUnitsKilled" &&
                    effect.TargetSubtype == impSubtype && effect.Value == spellDamage[spell.DataId] &&
                    actualStats.Value(spell.InstanceId, "HeroesKilled", "ThisTurn") -
                    beforeStats.Value(spell.InstanceId, "HeroesKilled", "ThisTurn") == kills,
                    "The original Shardtail damage multiplier or source-card kill credit differs.");
                if (higherRanks)
                {
                    int damage = checked(imps.Length * effect.Value);
                    Require(room.Units.Where(unit => heroes.Contains(unit.Id)).All(unit =>
                        Math.Max(0, unit.Health - damage) == (after.Units.FirstOrDefault(actor => actor.Id == unit.Id)?.Health ?? 0)) &&
                        room.Units.Any(unit => unit.Team == CombatTeam.Player && !imps.Contains(unit.Id) &&
                            after.Units.Any(actor => actor.Id == unit.Id && actor.Health == unit.Health)),
                        "Higher-rank Shardtail damage did not pass unchanged to the enemies or harmed the friendly control.");
                }
                else
                {
                    Require(room.Units.Any(unit => heroes.Contains(unit.Id) &&
                        unit.Status("armor")?.Stacks >= imps.Length * effect.Value && after.Units.All(actor => actor.Id != unit.Id)) &&
                        kills >= 2,
                        "The pinned Shardtail action must pierce armor and credit the source card for its kills.");
                    var train = before.Spawn.Train;
                    var allSubtypes = new CombatRelicState(OriginalAllSubtypesModel.OwnerId, OriginalAllSubtypesModel.OwnerKey,
                        [OriginalAllSubtypesModel.Effect], isCovenant: false, disallowedInPlacementPhase: false);
                    var active = CardSpellModel.WithContext(train, train.Context!.WithRelics(train.Context.Relics!.Append(allSubtypes).ToArray()));
                    CombatUnit champion = train.Rooms.SelectMany(floor => floor.Units).Single(unit => unit.Modifiers?.IsChampion == true);
                    CombatUnit steward = room.Units.Single(unit => unit.Team == CombatTeam.Player && !imps.Contains(unit.Id));
                    Require(OriginalAllSubtypesModel.MatchesCharacter(champion, impSubtype, allSubtypes: true) &&
                        CardSpellModel.TestCount(active, effect, new CardTargets([champion.Id])) == 0 &&
                        CardSpellModel.TestCount(active, effect, new CardTargets([steward.Id])) == 1,
                        "The native subtype wildcard must exclude champions from sacrifice and admit ordinary friendly units.");
                }
            }
            string? difference = ModelJson.Difference(BattleTurnChecks.Comparable(result.State!),
                BattleTurnChecks.Comparable(actual));
            Require(difference == null, "Shardtail action differs: " + difference);
            matched++;
        }
        Require(observed.SetEquals(spellDamage.Keys), "Missing an original Shardtail rank.");
        Require(wildcardChampionCasts + wildcardMixedCasts == 0 || (wildcardChampionCasts, wildcardMixedCasts) == (1, 1),
            "Missing original wildcard champion and mixed-Imp controls.");
        Console.WriteLine("NATIVE-DAMAGE-BY-UNITS-KILLED PASS: " + matched + " original Shardtail actions matched.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
