using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class OriginalDevoteTreasureChecks
{
    private const string UnitId = "0bf610e9-b1a0-4f02-a75c-71baca900fd1";
    private const string SpellId = "690ba1bf-378e-4db3-a2ab-c37dc36df04b";
    private static void Require(bool condition, string reason) => TrueFinalBossChecks.Require(condition, reason);
    private static string Json<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path); var root = document.RootElement;
        Require(root.GetProperty("OriginalDevoteTreasurePrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Incomplete original Treasure Devote battle.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero native " + key);
        int[] setup = root.GetProperty("OriginalDevoteTreasureSetupIds").Deserialize<int[]>()!;
        int[] treasures = root.GetProperty("OriginalDevoteTreasureIds").Deserialize<int[]>()!;
        var actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(setup.Length == 3 && treasures.Length == 5 && setup.Concat(treasures).Distinct().Count() == 8 && actions.Length == 8,
            "Missing setup or five original Treasure inputs.");
        int births = 0, pyreRefusals = 0, spells = 0, capacityOverflows = 0;
        var chosen = new HashSet<int>();
        for (int index = 3; index < actions.Length; index++)
        {
            var row = actions[index]; var before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            var after = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            var action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
            var from = before.Spawn.Train.Context!; var to = after.Spawn.Train.Context!;
            int[] eligible = DevoteModel.EligibleCards(from, out string? error)!;
            Require(error == null && eligible.Length == 8 - index && eligible.All(treasures.Contains), "Uncontrolled or protected cards entered the random pool.");
            var random = from.BattleRng.Range(0, eligible.Length); int selected = eligible[random.Value];
            Require(chosen.Add(selected) && action.Operation == DevoteModel.Operation && action.CardInstanceId == 0 &&
                before.UiRngIsolated && before.Energy - after.Energy == 1 &&
                to.QueryFrame!.Devotion is { Level: 1 } devotion && devotion.Points == index - 3 &&
                to.Cards.Hand.Select(card => card.InstanceId).SequenceEqual(from.Cards.Hand.Where(card => card.InstanceId != selected).Select(card => card.InstanceId)) &&
                Json(to.BattleRng) == Json(random.State), "Native random selection, one-energy cost, early Devotion award, hand removal or RNG differs.");
            var sourceCard = from.FindCard(selected)!; var card = to.FindCard(selected)!;
            var rule = before.PlayRules!.Cards.Single(rule => rule.DataId == card.DataId);
            Require(card.MaskDescriptor!.SourceTraits!.Single().Type == IndirectSpellModel.TreasureTrait &&
                to.Statistics!.Value(selected, "TimesDiscarded", "ThisBattle") == 1, "Original Treasure trait or discard statistic changed.");
            Require(sourceCard.MaskDescriptor!.TemporaryTraits.Single().RuntimeType == "CardTraitFreeze" &&
                card.MaskDescriptor.TemporaryTraits.Any(trait => trait.RuntimeType == "CardTraitFreeze") == (card.DataId == UnitId && action.RoomIndex == 3) &&
                card.LastPlayedCost == sourceCard.LastPlayedCost,
                "Successful indirect plays must run Freeze cleanup; Pyre refusal and ignored cost must preserve their source state.");
            var actor = after.Spawn.Train.Rooms.SelectMany(room => room.Units).SingleOrDefault(unit => unit.SpawnerCardId == selected);
            if (card.DataId == UnitId)
            {
                Require(rule.Cost == 1 && rule.SpawnUnit is { BaseAttack: 15, Health: 25, Size: 2 }, "Original Dark Initiate definition changed.");
                if (action.RoomIndex == 3)
                {
                    pyreRefusals++;
                    Require(actor == null && card.PlayCount == sourceCard.PlayCount && to.Statistics.Value(selected, "TimesPlayed", "ThisBattle") == 0 &&
                        to.Cards.Discard.Any(token => token.InstanceId == selected), "Pyre must suppress the Treasure cast and play-count promotion.");
                }
                else
                {
                    births++;
                    Require(action.RoomIndex == 0 && actor is { Attack: 30, Health: 30, MaxHealth: 30 } &&
                        actor.Modifiers!.Upgrades.Count == 1 && card.Temporary.Upgrades.Count == 1 &&
                        card.PlayCount == sourceCard.PlayCount + 1 && to.Statistics.Value(selected, "TimesPlayed", "ThisBattle") == 1,
                        "Devotion must rise before the free unit's OnSpawn upgrade and play accounting.");
                    var standby = to.OtherPiles!.Single(pile => pile.Name == "Standby");
                    Require(standby.Cards.Any(token => token.InstanceId == selected) && standby.UnitConditions!.Any(binding =>
                        binding.CardId == selected && binding.HostUnitId == actor!.Id && !binding.Ready) &&
                        to.Cards.Discard.All(token => token.InstanceId != selected) &&
                        to.OtherPiles.Single(pile => pile.Name == "DiscardBuffer").Cards.All(token => token.InstanceId != selected),
                        "Summoned source must leave discard/buffer and retain its exact live-unit standby binding.");
                    var floor = after.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0);
                    int capacity = RoomCapacityModel.Maximum(to, 0, CombatTeam.Player) ?? before.PlayRules.Rooms.Single(room => room.RoomIndex == 0).PlayerCapacity;
                    if (floor.Units.Where(unit => unit.Team == CombatTeam.Player).Sum(unit => unit.Size) > capacity) capacityOverflows++;
                }
            }
            else
            {
                spells++;
                Require(card.DataId == SpellId && rule.Cost == 3 && rule.Effects.Single() is
                    { Type: "AddStatus", Target: "Room", AllowPlayer: false, AllowEnemy: true } effect &&
                    effect.Statuses.Single() is { Id: "debuff", Stacks: 2 } && actor == null &&
                    card.PlayCount == sourceCard.PlayCount + 1 && to.Statistics.Value(selected, "TimesPlayed", "ThisBattle") == 1,
                    "Original Guardian Amulet must cast for the Devote cost alone.");
                var enemies = before.Spawn.Train.Rooms.Single(room => room.RoomIndex == action.RoomIndex).Units.Where(unit => unit.Team == CombatTeam.Enemy).ToArray();
                Require(enemies.Length > 0 && enemies.All(enemy => after.Spawn.Train.Rooms.SelectMany(room => room.Units)
                    .Single(unit => unit.Id == enemy.Id).Status("debuff")!.Stacks == (enemy.Status("debuff")?.Stacks ?? 0) + 2), "Original spell's enemy statuses differ.");
            }
        }
        Require(births == 3 && capacityOverflows == 1 && pyreRefusals == 1 && spells == 1 && chosen.SetEquals(treasures),
            "Missing successful units, over-capacity birth, Pyre refusal or spell control.");
        RecordedBattleChecks.Native(root);
        BattleSearchChecks.FrontierDiagnostics(root);
        BattleSearchChecks.Native(root);
        Console.WriteLine("ORIGINAL-DEVOTE-TREASURE PASS: five native random choices, early level-up, free unit/spell plays, independent play/discard counters, over-capacity birth, Pyre refusal, exact standby sources, full winning chain/16 branches and serial/parallel search.");
    }
}
