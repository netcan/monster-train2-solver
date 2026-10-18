using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalStatusRemovalScalingChecks
{
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
        if (index < 0 || index >= 2 && source.Spawn.Turn < 1) return null;
        int target = index is 2 or 3 or 5 ? source.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == ids[0]).Id : 0;
        return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == (index == 1 ? 1 : 0) && play.TargetUnitId == target);
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalStatusRemovalScalingPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original Pyreborne Brainspike witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }) Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("OriginalStatusRemovalScalingIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray(), samples = root.GetProperty("OriginalStatusRemovalScalingSamples").EnumerateArray().ToArray(),
            queries = root.GetProperty("OriginalStatusRemovalScalingQueries").EnumerateArray().ToArray();
        Require(ids.Length == 6 && actions.Length == 6 && samples.Length == 3 && queries.Length == 12, "Missing positive/zero/over-request removal or matched/unmatched status controls.");
        foreach (FixtureValue row in samples.Concat(queries))
        {
            CombatContext context = row.GetProperty("Context").Deserialize<CombatContext>()!;
            int id = row.GetProperty("CardId").GetInt32(), incoming = row.GetProperty("InputStacks").GetInt32(), bonus = row.GetProperty("ActualBonus").GetInt32();
            string status = row.GetProperty("StatusId").GetString()!;
            string parent = JsonSerializer.Serialize(context, ModelJson.Options);
            StatusScalingResult scaled = CardStatusRemovalScalingModel.Apply(context, id, status, incoming);
            int paid = context.Statistics!.PlayedCosts.Single(cost => cost.CardId == id).Cost;
            Require(scaled.Supported && scaled.Stacks == incoming + bonus && bonus == (status == "cooldown" ? paid : 0) &&
                (!row.TryGetProperty("StateUnchanged", out FixtureValue unchanged) || unchanged.GetBoolean()), "Native removal trait bonus or unchanged query context differs.");
            CardInstanceState card = context.FindCard(id)!; CardMaskDescriptor descriptor = card.MaskDescriptor!;
            CardSourceTrait source = descriptor.SourceTraits!.Single(trait => trait.Type == CardStatusRemovalScalingModel.Trait);
            Require(card.DataId == "0cdeed34-c8f5-436d-ad7d-774286333dbe" && source.Int == 1 && source.Int2 == 0 && source.Int3 == 0 &&
                !source.Bool && source.Str == "" && source.Float == 0 && !source.Scaling && source.TrackedValue == "PlayedCost" && source.EntryDuration == "ThisBattle" &&
                source.StatusEffectIds!.SequenceEqual(["cooldown"]) && descriptor.SourceTraits.Single(trait => trait.Type == "CardTraitScalingAddStatusEffect").StatusEffectIds!.SequenceEqual(["avarice"]),
                "Original removal/addition source status masks or scalar parameters differ.");
            CardInstanceState WithSource(IReadOnlyList<CardSourceTrait>? sources) => card.WithMaskDescriptor(new CardMaskDescriptor(descriptor.Definition, descriptor.BaseCost,
                descriptor.BaseTraits, descriptor.AuthoredCastEffects, descriptor.Purified, descriptor.PermanentGraft, descriptor.TemporaryTraits, descriptor.PermanentReplacements, sources));
            CardSourceTrait missing = new CardSourceTrait(source.Type, source.Int, source.Int2, source.Int3, source.Bool, source.Str, source.Float, source.Scaling, source.TrackedValue, source.EntryDuration);
            CardSourceTrait[] changed = descriptor.SourceTraits.Select(trait => trait.Type == CardStatusRemovalScalingModel.Trait ? missing : trait).ToArray();
            Require(!CardStatusRemovalScalingModel.Apply(context.WithCard(WithSource(null)), id, status, incoming).Supported &&
                !CardStatusRemovalScalingModel.Apply(context.WithCard(WithSource(changed)), id, status, incoming).Supported &&
                CardStatusRemovalScalingModel.Apply(context.WithCard(WithSource(null)), id, status, -1).Stacks == -1,
                "Missing source/status-mask metadata must remain guarded; native remove-all must bypass scaling.");
            Require(parent == JsonSerializer.Serialize(context, ModelJson.Options), "Removal query or guard mutated its parent.");
        }
        Require(samples.Select(sample => sample.GetProperty("ActualBonus").GetInt32()).SequenceEqual([3, 0, 10]) &&
            queries.Count(query => query.GetProperty("StatusId").GetString() == "armor" && query.GetProperty("ActualBonus").GetInt32() == 0) == 6 &&
            queries.Count(query => query.GetProperty("InputStacks").GetInt32() == 9) == 6, "Missing zero, positive, unmatched-status or incoming-stack independence controls.");
        foreach (int index in new[] { 2, 3, 5 })
        {
            BattleTurnState before = actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!, after = actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            CombatUnit Friendly(BattleTurnState state) => state.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == ids[0]);
            CombatUnit target = Friendly(before), actual = Friendly(after);
            int paid = index == 2 ? 3 : index == 3 ? 0 : 10, old = target.Status("cooldown")?.Stacks ?? 0, remaining = actual.Status("cooldown")?.Stacks ?? 0;
            CardPlayRule rule = before.PlayRules!.Cards.Single(item => item.DataId == "0cdeed34-c8f5-436d-ad7d-774286333dbe");
            Require(rule.Cost == 0 && rule.CostType == "ConsumeRemainingEnergy" && rule.ExternalInteractions.Count == 0 && rule.UpgradeInteractions.Count == 0 &&
                before.Energy == paid && after.Energy == 0 && old > 0 && remaining == Math.Max(0, old - paid) && (index != 5 || paid > old && remaining == 0) &&
                actual.Status("avarice")!.Stacks - target.Status("avarice")!.Stacks == paid * 2 &&
                after.Spawn.Train.Context!.Statistics!.Value(ids[index], "AnyStatusEffectStacksRemoved", "ThisBattle") == Math.Min(paid, old) &&
                after.Spawn.Train.Context.Statistics.Value(ids[index], "TimesExhausted", "ThisBattle") == 2 &&
                after.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(token => token.InstanceId == ids[index]) &&
                target.Health == actual.Health && target.Status("armor")!.Stacks == actual.Status("armor")!.Stacks,
                "Original X payment, clamped cooldown removal/attribution, paired Avarice, Consume or armor control differs.");
            Require(JsonSerializer.Serialize(before.Spawn.Train.Rooms.Single(room => room.RoomIndex == 1).Units, ModelJson.Options) ==
                JsonSerializer.Serialize(after.Spawn.Train.Rooms.Single(room => room.RoomIndex == 1).Units, ModelJson.Options), "Removal spell changed the outside floor.");
        }
        RecordedBattleChecks.Native(root);
        Console.WriteLine("ORIGINAL-STATUS-REMOVAL-SCALING PASS: original Pyreborne Brainspike, X three/zero/ten, positive/zero/clamped cooldown removal, paired Avarice, twelve unchanged matched/unmatched/incoming-stack queries, exact source masks, removal attribution, Consume and 16 immutable chains.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
