using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalScaledBuffChecks
{
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
        if (index < 0 || index >= 2 && source.Spawn.Turn < 1) return null;
        return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == (index == 1 ? 1 : 0));
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalScaledBuffPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original Pyre Plating witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }) Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("OriginalScaledBuffIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray(), samples = root.GetProperty("OriginalScaledBuffSamples").EnumerateArray().ToArray(),
            queries = root.GetProperty("OriginalScaledBuffQueries").EnumerateArray().ToArray();
        Require(ids.Length == 6 && actions.Length == 6 && samples.Select(row => row.GetProperty("ActualDamage").GetInt32()).SequenceEqual([12, 0, 40]) &&
            queries.Length == 6 && queries.Count(row => row.GetProperty("InputDamage").GetInt32() == 99) == 3, "Missing positive/zero/large X or incoming-damage override controls.");
        foreach (FixtureValue row in samples.Concat(queries))
        {
            CombatContext context = row.GetProperty("Context").Deserialize<CombatContext>()!;
            int id = row.GetProperty("CardId").GetInt32(), incoming = row.GetProperty("InputDamage").GetInt32(), native = row.GetProperty("ActualDamage").GetInt32();
            string parent = JsonSerializer.Serialize(context, ModelJson.Options);
            DamageScalingResult result = CardBuffDamageModel.Apply(context, id, incoming, true);
            CardInstanceState card = context.FindCard(id)!; CardMaskDescriptor descriptor = card.MaskDescriptor!;
            CardSourceTrait source = descriptor.SourceTraits!.Single(trait => trait.Type == CardBuffDamageModel.Trait);
            Require(result.Supported && result.Damage == native && native == 4 * context.Statistics!.PlayedCosts.Single(cost => cost.CardId == id).Cost &&
                (!row.TryGetProperty("StateUnchanged", out FixtureValue unchanged) || unchanged.GetBoolean()) &&
                source.Int == 4 && source.Int2 == 0 && source.Int3 == 0 && !source.Bool && source.Str == "" && source.Float == 0 && !source.Scaling &&
                source.TrackedValue == "PlayedCost" && source.EntryDuration == "ThisTurn", "Native attack buff replacement or exact query/source parameters differ.");
            CardInstanceState WithSource(IReadOnlyList<CardSourceTrait>? sources) => card.WithMaskDescriptor(new CardMaskDescriptor(descriptor.Definition, descriptor.BaseCost,
                descriptor.BaseTraits, descriptor.AuthoredCastEffects, descriptor.Purified, descriptor.PermanentGraft, descriptor.TemporaryTraits, descriptor.PermanentReplacements, sources));
            CardSourceTrait changed = new(source.Type, source.Int, source.Int2, source.Int3, source.Bool, source.Str, source.Float, source.Scaling, source.TrackedValue, "ThisBattle");
            Require(!CardBuffDamageModel.Apply(context.WithCard(WithSource(null)), id, incoming, true).Supported &&
                !CardBuffDamageModel.Apply(context.WithCard(WithSource(descriptor.SourceTraits.Select(trait => trait.Type == CardBuffDamageModel.Trait ? changed : trait).ToArray())), id, incoming, true).Supported &&
                !CardBuffDamageModel.Apply(context, id, incoming, false).Supported, "Missing source, changed duration or non-Pyre target must remain guarded.");
            Require(parent == JsonSerializer.Serialize(context, ModelJson.Options), "Attack buff query or guard mutated its parent.");
        }
        foreach (int index in new[] { 2, 3, 5 })
        {
            BattleTurnState before = actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!, after = actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            int paid = index == 2 ? 3 : index == 3 ? 0 : 10;
            CardInstanceState card = before.Spawn.Train.Context!.FindCard(ids[index])!;
            CardPlayRule rule = before.PlayRules!.Cards.Single(item => item.DataId == card.DataId);
            CombatUnit old = before.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre), changed = after.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre);
            CardSourceTrait status = card.MaskDescriptor!.SourceTraits!.Single(trait => trait.Type == "CardTraitScalingAddStatusEffect");
            Require(card.DataId == "73289203-d973-4552-9500-4aaa66a6b621" && rule.Cost == 0 && rule.CostType == "ConsumeRemainingEnergy" &&
                rule.ExternalInteractions.Count == 0 && rule.Effects.Select(effect => effect.Type).SequenceEqual(["BuffAttack", "AddStatus"]) &&
                rule.Effects.All(effect => effect.Target == "Pyre" && effect.Value == 0) && status.Int == 2 && status.Int2 == 0 && status.StatusEffectIds!.SequenceEqual(["armor"]) &&
                before.Energy == paid && after.Energy == 0 && changed.Attack - old.Attack == 4 * paid && changed.Modifiers!.DamageBuff - old.Modifiers!.DamageBuff == 4 * paid &&
                (changed.Status("armor")?.Stacks ?? 0) - (old.Status("armor")?.Stacks ?? 0) == 2 * paid &&
                old.Status("immune")!.Stacks == changed.Status("immune")!.Stacks && changed.Health == old.Health && changed.MaxHealth == old.MaxHealth &&
                after.OtherPiles.Single(pile => pile.Name == "Exhausted").Cards.Any(token => token.InstanceId == ids[index]) &&
                after.Spawn.Train.Context!.Statistics!.Value(ids[index], "TimesExhausted", "ThisBattle") == 2,
                "Original identity, ordered Pyre effects, paid X, attack/armor increments, native immunity override, unchanged health or Consume differs.");
            Require(JsonSerializer.Serialize(before.Spawn.Train.Rooms.SelectMany(room => room.Units).Where(unit => !unit.IsPyre), ModelJson.Options) ==
                JsonSerializer.Serialize(after.Spawn.Train.Rooms.SelectMany(room => room.Units).Where(unit => !unit.IsPyre), ModelJson.Options), "Pyre effects changed another unit.");
        }
        RecordedBattleChecks.Native(root);
        Console.WriteLine("ORIGINAL-SCALED-BUFF PASS: original Pyre Plating, X three/zero/ten gives attack twelve/zero/forty and armor six/zero/twenty, six pure incoming-override queries, Pyre immunity override, unit/health controls, Consume, source/target guards and 16 immutable chains.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
