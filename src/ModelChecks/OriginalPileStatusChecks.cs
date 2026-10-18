using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalPileStatusChecks
{
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
        if (index < 0 || index >= 2 && source.Spawn.Turn < 1) return null;
        return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == (index < 2 ? index + 1 : 0));
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalPileStatusPrepared").GetBoolean() && root.GetProperty("OriginalPileStatusQueriesUnchanged").GetBoolean() &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed Dig Deep witness or restored queries.");
        foreach (FixtureValue action in root.GetProperty("Actions").EnumerateArray())
        {
            if (action.GetProperty("Difference").ValueKind == FixtureKind.Null) continue;
            BattleActionResult replay = BattleActionModel.PlayCard(action.GetProperty("Before").Deserialize<BattleTurnState>()!, action.GetProperty("Action").Deserialize<PlayCardAction>()!);
            string? difference = replay.Supported ? ModelJson.Difference(JsonSerializer.Serialize(replay.State, ModelJson.Options),
                JsonSerializer.Serialize(action.GetProperty("Actual").Deserialize<BattleTurnState>(), ModelJson.Options)) : replay.Reason;
            if (difference != null) throw new InvalidOperationException("Native Dig Deep mismatch: " + difference);
        }
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }) Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("OriginalPileStatusIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray(), queries = root.GetProperty("OriginalPileStatusQueries").EnumerateArray().ToArray(),
            samples = root.GetProperty("OriginalPileStatusSamples").EnumerateArray().ToArray();
        if (actions.Length != 5)
        {
            BattleTurnState decision = actions.Last().GetProperty("Actual").Deserialize<BattleTurnState>()!;
            BattleActionResult attempted = BattleActionModel.PlayCard(decision, new PlayCardAction(ids[3], 0));
            throw new InvalidOperationException("Missing native Dig Deep play: " + attempted.Rejection + ": " + attempted.Reason);
        }
        Require(ids.Length == 5 && actions.Length == 5 && queries.Length == 24 && samples.Length == 2 &&
            queries.Select(row => row.GetProperty("VisibleDeckCount").GetInt32()).Distinct().SequenceEqual([0, 9, 10, 19, 20, 30]), "Missing original plays or threshold/status/incoming controls.");
        foreach (FixtureValue row in queries.Concat(samples))
        {
            CombatContext context = row.GetProperty("Context").Deserialize<CombatContext>()!;
            int id = row.GetProperty("CardId").GetInt32(), native = row.GetProperty("ActualBonus").GetInt32();
            string status = row.GetProperty("StatusId").GetString()!;
            string parent = JsonSerializer.Serialize(context, ModelJson.Options);
            StatusScalingResult actual = CardPileStatusModel.Bonus(context, id, status);
            CardInstanceState card = context.FindCard(id)!; CardMaskDescriptor descriptor = card.MaskDescriptor!;
            CardSourceTrait source = descriptor.SourceTraits!.Single(trait => trait.Type == CardPileStatusModel.Trait);
            Require(actual.Supported && actual.Stacks == native && native == (status == "horde" ? context.Resources!.VisibleDeckCount / 10 : 0) &&
                (!row.TryGetProperty("StateUnchanged", out FixtureValue unchanged) || unchanged.GetBoolean()) && context.Cards.Draw.Count == 0 &&
                source.Int == 1 && source.Int2 == 0 && source.Int3 == 10 && source.Bool && source.Str == "" && source.Float == 0 && !source.Scaling &&
                source.TrackedValue == "SubtypeInDeck" && source.EntryDuration == "ThisTurn" && source.StatusEffectIds!.SequenceEqual(["horde"]),
                "Native visible deck count, runtime-pile control, bonus or exact source parameters differ.");
            CardInstanceState WithSource(IReadOnlyList<CardSourceTrait>? sources) => card.WithMaskDescriptor(new CardMaskDescriptor(descriptor.Definition, descriptor.BaseCost,
                descriptor.BaseTraits, descriptor.AuthoredCastEffects, descriptor.Purified, descriptor.PermanentGraft, descriptor.TemporaryTraits, descriptor.PermanentReplacements, sources));
            CardSourceTrait changed = new(source.Type, source.Int, source.Int2, 9, source.Bool, source.Str, source.Float, source.Scaling, source.TrackedValue,
                source.EntryDuration, statusEffectIds: source.StatusEffectIds);
            Require(!CardPileStatusModel.Bonus(context.WithCard(WithSource(null)), id, "horde").Supported &&
                !CardPileStatusModel.Bonus(context.WithCard(WithSource([changed])), id, "horde").Supported &&
                !CardPileStatusModel.Bonus(context.WithResources(null), id, "horde").Supported, "Missing source/resource metadata or changed threshold must remain guarded.");
            Require(parent == JsonSerializer.Serialize(context, ModelJson.Options), "Pile status query or guard mutated its parent.");
        }
        for (int index = 3; index <= 4; index++)
        {
            BattleTurnState before = actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!, after = actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            CardPlayRule rule = before.PlayRules!.Cards.Single(card => card.DataId == "3d6898b0-2e34-41a4-a6ff-e301b0d46d5e");
            CombatUnit[] old = before.Spawn.Train.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Player).ToArray();
            CombatUnit unit = after.Spawn.Train.Rooms[0].Units.Single(unit => unit.Team == CombatTeam.Player);
            int count = index == 3 ? 4 : 8;
            Require(rule.Cost == 2 && rule.Effects.Single().Type == "SpawnOrStatus" && rule.Effects[0].Statuses.Single().Id == "horde" &&
                rule.Effects[0].Statuses[0].Stacks == 1 && rule.ExternalInteractions.Count == 0 && rule.UpgradeInteractions!.Count == 0 &&
                before.Energy - after.Energy == 2 && after.Spawn.Train.Context!.Cards.Discard.Any(token => token.InstanceId == ids[index]) &&
                after.Spawn.Train.Context.Statistics!.Value(ids[index], "AnyStatusEffectStacksAdded", "ThisBattle") == 4 &&
                unit.AssetKey == "MonsterFunguy" && unit.Status("horde")!.Stacks == count && unit.Attack == count * 3 && unit.Health == count * 4 && unit.MaxHealth == count * 4 &&
                (index == 3 ? old.Length == 0 : old.Length == 1 && old[0].Id == unit.Id && old[0].Status("horde")!.Stacks == 4),
                "Original cost, base Horde amount, discard, first birth or existing-Horde extension differs.");
            Require(JsonSerializer.Serialize(before.Spawn.Train.Rooms.Skip(1).SelectMany(room => room.Units), ModelJson.Options) ==
                JsonSerializer.Serialize(after.Spawn.Train.Rooms.Skip(1).SelectMany(room => room.Units), ModelJson.Options), "Horde effect changed another floor.");
        }
        RecordedBattleChecks.Native(root);
        Console.WriteLine("ORIGINAL-PILE-STATUS PASS: original Dig Deep, 24 restored visible-deck threshold/status/incoming queries, two native callbacks, new four-stack Horde then extension to eight, costs/discard/floor controls, source/resource guards and 16 immutable chains.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
