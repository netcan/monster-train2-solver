using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalPermafrostCardsChecks
{
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids, int junkId)
    {
        CombatContext context = source.Spawn.Train.Context!;
        int index = Array.FindIndex(ids, id => context.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
        if (index < 0)
        {
            if (context.Statistics!.Value(ids[4], "TimesPlayed", "ThisBattle") >= 2 || source.Spawn.Turn < 2) return null;
            index = 4;
        }
        if (index >= 2 && source.Spawn.Turn < 1) return null;
        if (index == 2) return new PlayCardAction(ids[index], 0, recursionSelections: [new CardRecursionSelection(0, [junkId])]);
        int room = index < 2 ? index : source.Spawn.Train.Rooms.Where(value => value.RoomIndex < 3 &&
            value.Units.Any(unit => unit.Team == CombatTeam.Enemy && unit.Health > 0)).Select(value => value.RoomIndex).DefaultIfEmpty(-1).Min();
        return room < 0 ? null : BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == room);
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalPermafrostCardsPrepared").GetBoolean() && root.GetProperty("OriginalPermafrostCardsQueriesUnchanged").GetBoolean() &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original Permafrost witness or idempotent restoration.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }) Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("OriginalPermafrostCardsIds").Deserialize<int[]>()!;
        int junkId = root.GetProperty("OriginalPermafrostCardsJunkId").GetInt32();
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        BattleTurnState initial = root.GetProperty("OriginalPermafrostCardsInitial").Deserialize<BattleTurnState>()!;
        Require(ids.Length == 5 && actions.Length == 6, "Missing original Permafrost play/redraw controls.");
        int Frozen(CardInstanceState card) => card.MaskDescriptor!.TemporaryTraits.Count(trait => trait.RuntimeType == "CardTraitFreeze");
        foreach (int id in ids.Skip(2))
        {
            CardInstanceState card = initial.Spawn.Train.Context!.FindCard(id)!;
            Require(card.Permanent.Upgrades.Count == 0 && card.Temporary.Upgrades.Count == 0 && Frozen(card) == 1 &&
                card.MaskDescriptor!.SourceTraits!.All(trait => trait.Int == 0 && trait.Int2 == 0 && trait.Int3 == 0 && !trait.Bool && trait.Str == "" &&
                    trait.Float == 0 && !trait.Scaling && trait.TrackedValue == "SubtypeInDeck" && trait.EntryDuration == "ThisTurn") &&
                card.MaskDescriptor.SourceTraits.Select(trait => trait.Type).SequenceEqual(id == ids[3] ?
                    ["CardTraitPermafrost", "CardTraitExhaustState"] : ["CardTraitPermafrost"]), "Original Permafrost source or automatic setup Freeze differs.");
        }
        int controls = 0, refreezes = 0;
        foreach (FixtureValue row in root.GetProperty("OriginalPermafrostCardsDraws").EnumerateArray())
        {
            CardInstanceState before = row.GetProperty("Before").Deserialize<CardInstanceState>()!, after = row.GetProperty("After").Deserialize<CardInstanceState>()!;
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            CardInstanceState predicted = RelicExpensiveCardUpgradeModel.Freeze(before);
            Require(JsonSerializer.Serialize(predicted, ModelJson.Options) == JsonSerializer.Serialize(after, ModelJson.Options) && Frozen(after) == 1,
                "Original native Permafrost callback differs or duplicated Freeze.");
            if (row.GetProperty("Control").GetBoolean()) { controls++; Require(Frozen(before) == 1 && parent == JsonSerializer.Serialize(after, ModelJson.Options), "Already frozen draw callback must leave the card unchanged."); }
            else if (Frozen(before) == 0) refreezes++;
            Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Permafrost callback replay mutated its parent.");
        }
        Require(controls == 6 && refreezes >= 2, "Missing repeated idempotence or natural redraw re-freeze evidence.");
        BattleTurnState retained = root.GetProperty("Turns")[0].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        foreach (int id in ids.Skip(2))
            Require(retained.Spawn.Train.Context!.Cards.Hand.Any(card => card.InstanceId == id) && Frozen(retained.Spawn.Train.Context.FindCard(id)!) == 1 &&
                retained.Spawn.Train.Context.Statistics!.Value(id, "TimesDiscarded", "ThisBattle") == 0, "Unplayed Permafrost card must stay frozen in hand.");
        int damagePlays = 0;
        foreach (FixtureValue row in actions.Skip(2))
        {
            BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!, after = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            PlayCardAction action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
            CardInstanceState card = before.Spawn.Train.Context!.FindCard(action.CardInstanceId)!, played = after.Spawn.Train.Context!.FindCard(action.CardInstanceId)!;
            CardPlayRule rule = before.PlayRules!.Cards.Single(value => value.DataId == card.DataId);
            Require(Frozen(card) == 1 && Frozen(played) == 0 && rule.ExternalInteractions.Count == 0 && rule.UpgradeInteractions.Count == 0 && before.Energy - after.Energy == rule.Cost,
                "Playing original Permafrost must pay its cost and remove temporary Freeze.");
            if (action.CardInstanceId == ids[2]) Require(card.DataId == "3ca50b9d-9c52-49c8-884f-f4409a4489c9" &&
                rule.Cost == 0 && Frozen(after.Spawn.Train.Context.FindCard(junkId)!) == 1, "Original Preserve must freeze its selected unfrozen Junk.");
            else if (action.CardInstanceId == ids[3])
            {
                Require(card.DataId == "561f7a74-93a3-438f-9bfd-fbc84d979bdd" && rule.Cost == 0 &&
                    after.Spawn.Train.Context.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(value => value.InstanceId == action.CardInstanceId) &&
                    after.Spawn.Train.Rooms.Single(room => room.RoomIndex == action.RoomIndex).Units.Any(unit => unit.Team == CombatTeam.Enemy && unit.Status("muted")?.Stacks == 3),
                    "Original Tome must apply three Silence to enemies and Consume.");
            }
            else
            {
                damagePlays++;
                Require(card.DataId == "d4b4e903-0d62-4a3b-bb05-002a358fcba1" && rule.Cost == 3 && rule.Effects.Count == 3 &&
                    rule.Effects.All(effect => effect.Type == "Damage" && effect.Target == "RandomInRoom" && effect.Value == 30 && effect.AllowEnemy && !effect.AllowPlayer),
                    "Original Ice Tornado must retain all three random thirty-damage effects.");
                CombatUnit[] targets = before.Spawn.Train.Rooms.Single(room => room.RoomIndex == action.RoomIndex).Units.Where(unit => unit.Team == CombatTeam.Enemy && unit.Health > 0).ToArray();
                Require(targets.Length > 0 && targets.Any(target => !after.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit => unit.Id == target.Id) ||
                    after.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == target.Id).Health < target.Health ||
                    (after.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == target.Id).Status("armor")?.Stacks ?? 0) < (target.Status("armor")?.Stacks ?? 0)),
                    "Original Ice Tornado needs positive enemy damage before and after natural redraw.");
            }
        }
        Require(damagePlays == 2 && Frozen(root.GetProperty("Turns")[1].GetProperty("Actual").Deserialize<BattleTurnState>()!.Spawn.Train.Context!.FindCard(junkId)!) == 1,
            "Missing natural Ice Tornado replay or retained Preserve-selected Junk.");
        RecordedBattleChecks.Native(root);
        Console.WriteLine($"ORIGINAL-PERMAFROST-CARDS PASS: three original definitions, automatic Freeze, six restored idempotence controls, {refreezes} natural re-freezes, retention, chosen Junk Freeze, Tome Silence/Consume, two positive Ice Tornado plays and 16 immutable full chains.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
