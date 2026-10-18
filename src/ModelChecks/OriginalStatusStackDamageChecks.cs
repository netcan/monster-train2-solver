using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalStatusStackDamageChecks
{
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
        if (index < 0 || index >= 3 && source.Spawn.Turn < 1) return null;
        int owner = index == 3 ? 2 : index >= 4 ? 0 : -1;
        int target = owner < 0 ? 0 : source.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == ids[owner]).Id;
        return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] &&
            play.RoomIndex == (index == 1 ? 1 : 0) && play.TargetUnitId == target);
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalStatusStackDamagePrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original Wracking Pain witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }) Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("OriginalStatusStackDamageIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray(), samples = root.GetProperty("OriginalStatusStackDamageSamples").EnumerateArray().ToArray();
        Require(ids.Length == 7 && actions.Length == 7 && samples.Length == 3, "Missing zero, positive and increased target-debuff controls.");
        int[] amounts = new int[3];
        for (int sampleIndex = 0; sampleIndex < samples.Length; sampleIndex++)
        {
            FixtureValue sample = samples[sampleIndex]; CombatContext context = sample.GetProperty("Context").Deserialize<CombatContext>()!;
            CombatUnit target = sample.GetProperty("Target").Deserialize<CombatUnit>()!;
            int id = sample.GetProperty("CardId").GetInt32(), input = sample.GetProperty("InputDamage").GetInt32(), actual = sample.GetProperty("ActualDamage").GetInt32();
            CardInstanceState card = context.FindCard(id)!;
            string parent = JsonSerializer.Serialize(context, ModelJson.Options);
            DamageScalingResult scaled = DamageScalingModel.Apply(context, id, id, input, targetStatuses: target.Statuses);
            int debuffs = target.Statuses.Where(status => status.Id != "horde" && status.Hidden == false && status.DisplayCategory == "Negative").Sum(status => status.Stacks);
            Require(sample.GetProperty("StateUnchanged").GetBoolean() && scaled.Supported && scaled.Damage == actual && input == 0 && actual == debuffs * 3 &&
                card.DataId == "67e742a7-d9b6-46e4-b9dc-37edd4dfb872" && id == ids[sampleIndex == 0 ? 3 : sampleIndex == 1 ? 4 : 6],
                "Native target-debuff trait output, source identity or query restoration differs.");
            amounts[sampleIndex] = actual;
            CardMaskDescriptor descriptor = card.MaskDescriptor!;
            CardInstanceState WithSource(IReadOnlyList<CardSourceTrait>? sources) => card.WithMaskDescriptor(new CardMaskDescriptor(descriptor.Definition, descriptor.BaseCost,
                descriptor.BaseTraits, descriptor.AuthoredCastEffects, descriptor.Purified, descriptor.PermanentGraft, descriptor.TemporaryTraits, descriptor.PermanentReplacements, sources));
            Require(CardStatusDamageModel.Apply(WithSource(null), 0, target.Statuses, out _) != null &&
                CardStatusDamageModel.Apply(card, 0, null, out _) != null &&
                CardStatusDamageModel.Apply(card, 0, [new CombatStatus("debuff", 1)], out _) != null,
                "Missing source/target/visibility metadata must remain guarded.");
            CardSourceTrait source = descriptor.SourceTraits!.Single();
            Require(source.Int == 3 && source.Int2 == 2 && source.Int3 == 0 && !source.Bool && source.Str == "" && source.Float == 2 && !source.Scaling &&
                source.TrackedValue == "MagicPowerInTargetRoom" && source.EntryDuration == "ThisTurn", "Original source parameters changed.");
            CardSourceTrait changed = new CardSourceTrait(source.Type, source.Int, 3, source.Int3, source.Bool, source.Str, source.Float, source.Scaling, source.TrackedValue, source.EntryDuration);
            Require(CardStatusDamageModel.Apply(WithSource([changed]), 0, target.Statuses, out _) != null, "Uncalibrated positive-status mode must remain guarded.");
            Require(parent == JsonSerializer.Serialize(context, ModelJson.Options), "Status-stack evaluation or guard mutated its parent.");
        }
        Require(amounts[0] == 0 && amounts[1] > 0 && amounts[2] == amounts[1] + 6, "Zero-debuff, positive and added-two-stack damage controls differ.");
        foreach (int index in new[] { 3, 4, 6 })
        {
            BattleTurnState before = actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!, after = actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            PlayCardAction action = actions[index].GetProperty("Action").Deserialize<PlayCardAction>()!;
            CardPlayRule rule = before.PlayRules!.Cards.Single(item => item.DataId == "67e742a7-d9b6-46e4-b9dc-37edd4dfb872");
            CombatUnit target = before.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == action.TargetUnitId),
                actual = after.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == target.Id);
            int amount = amounts[index == 3 ? 0 : index == 4 ? 1 : 2];
            Require(rule.Cost == 1 && rule.Effects.Single().Value == 0 && rule.ExternalInteractions.Count == 0 && rule.UpgradeInteractions.Count == 0 &&
                before.Energy - after.Energy == 1 && target.Statuses.Any(status => status.DisplayCategory == "Positive" && status.Stacks > 0) &&
                target.Health + (target.Status("armor")?.Stacks ?? 0) - actual.Health - (actual.Status("armor")?.Stacks ?? 0) == amount,
                "Original zero base damage, one ember cost, positive-status control or armor ordering differs.");
            Require(JsonSerializer.Serialize(before.Spawn.Train.Rooms.Single(room => room.RoomIndex == 1).Units, ModelJson.Options) ==
                JsonSerializer.Serialize(after.Spawn.Train.Rooms.Single(room => room.RoomIndex == 1).Units, ModelJson.Options), "Target status damage changed the outside floor.");
        }
        RecordedBattleChecks.Native(root);
        Console.WriteLine($"ORIGINAL-STATUS-STACK-DAMAGE PASS: original Wracking Pain, three unchanged native callbacks, zero/positive/added-stack controls ({string.Join('/', amounts)}), visibility/source guards, armor ordering and 16 immutable full chains. Attacker/list/buff modes and added/replaced traits remain guarded.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
