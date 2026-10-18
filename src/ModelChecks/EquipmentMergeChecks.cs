using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class EquipmentMergeChecks
{
    private const string OutputId = "71d29f23-3ce3-468b-9ca9-61a681d9370a";
    internal static PlayCardAction? Choose(BattleTurnState source, int[] spells, int[] inputs, int[] units)
    {
        CombatContext context = source.Spawn.Train.Context!; var plays = BattleActionModel.EnumerateSupportedPlays(source);
        foreach (int id in units)
            if (context.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0)
                return plays.FirstOrDefault(play => play.CardInstanceId == id && play.RoomIndex == 0);
        for (int index = 0; index < spells.Length; index++)
            if (context.Statistics!.Value(spells[index], "TimesPlayed", "ThisBattle") == 0)
            {
                int[] selected = index == 0 ? Array.Empty<int>() : inputs.Skip((index - 1) * 2).Take(2).ToArray();
                return plays.FirstOrDefault(play => play.CardInstanceId == spells[index] && play.RoomIndex == 0 &&
                    play.RecursionSelections.Single().CardInstanceIds.SequenceEqual(selected));
            }
        return plays.FirstOrDefault(play => context.FindCard(play.CardInstanceId)!.DataId == OutputId && play.RoomIndex == 0 &&
            source.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit => unit.Id == play.TargetUnitId && unit.EquipmentCards?.Count == 0));
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("EquipmentMergePrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original merge witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        var tests = new List<bool>();
        foreach (FixtureValue row in root.GetProperty("EquipmentMergeTests").EnumerateArray())
        {
            var context = row.GetProperty("Context").Deserialize<CombatContext>()!;
            var rule = row.GetProperty("Rule").Deserialize<EquipmentMergeRule>()!;
            var eligible = EquipmentMergeModel.Eligible(context, rule, out string? error);
            bool actual = row.GetProperty("Returned").GetBoolean();
            Require(error == null && (eligible.Count >= 2) == actual, "Native merge Test differs: " + error);
            if (tests.Count == 2)
            {
                var choices = EquipmentMergeModel.Choices(context, rule, out error);
                Require(error == null && choices.Count == 1 && choices[0].Count == 0, "Same-definition cards must allow cancellation only.");
            }
            tests.Add(actual);
        }
        Require(tests.SequenceEqual([false, false, true, true]), "Missing zero/one/same-definition/valid-pair Tests.");
        int index = 0;
        foreach (FixtureValue row in root.GetProperty("EquipmentMergeRecords").EnumerateArray())
        {
            Require(row.GetProperty("Completed").GetBoolean(), "Incomplete native merge.");
            var before = row.GetProperty("Before").Deserialize<CombatContext>()!;
            var after = row.GetProperty("After").Deserialize<CombatContext>()!;
            var rule = row.GetProperty("Rule").Deserialize<EquipmentMergeRule>()!;
            var definitions = row.GetProperty("Definitions").Deserialize<BattlePlayRules>()!;
            var optional = row.GetProperty("Upgrade").Deserialize<CardUpgradeModifier>();
            int[] selected = row.GetProperty("Selected").Deserialize<int[]>()!;
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            void Replay()
            {
                var result = EquipmentMergeModel.Apply(before, rule, selected, definitions, optional);
                Require(result.Supported, result.UnsupportedReason ?? "Merge unsupported.");
                Equal(result.Context, after, "native merge complete context/RNG");
            }
            Replay(); Parallel.For(0, 16, _ => Replay());
            Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Merge mutated its parent.");
            if (index == 0) Equal(before, after, "Cancelled merge preserves every context field");
            else
            {
                CardInstanceState created = after.FindCard(before.NextCardId)!;
                Require(optional?.Stats.Cost == -1 && optional.Stats.XCost == 1 &&
                    CardModifierModel.Resolve(definitions.Cards.Single(card => card.DataId == OutputId), created).Cost == (index == 1 ? 3 : 2),
                    "Original merge discount or source current/permanent cost composition differs.");
                Require(after.NextCardId == before.NextCardId + 1 && after.BattleRng.Equals(before.BattleRng) &&
                    created.Permanent.MergedEquipment!.Sources.Select(source => source.CardId).SequenceEqual(selected) &&
                    created.Permanent.Upgrades.Count == 1 && created.Temporary.Upgrades.Count == (optional == null ? 0 : 1),
                    "Merge lost source order/permanent upgrade or copied a temporary source upgrade.");
                Require(selected.All(id => after.OtherPiles!.Single(pile => pile.Name == "Standby").Cards.Any(card => card.InstanceId == id)), "Merge failed to retain source cards in Standby.");
                if (index == 1) Require(after.Cards.Hand[0].InstanceId == created.InstanceId && after.Statistics!.Value(created.InstanceId, "TimesDrawn", "ThisBattle") == 1,
                    "A merge with a hand source must draw its output.");
                else Require(after.Cards.Draw.Last().InstanceId == created.InstanceId && !after.Cards.Hand.Any(card => card.InstanceId == created.InstanceId),
                    "A merge without hand sources must leave output at draw top.");
                CardInstanceState input = before.FindCard(selected[0])!;
                CombatContext unsupportedOffsets = before.WithCard(input.WithModifiers(new CardModifiers(new CardStatModifier(damage: 1),
                    input.Permanent.Upgrades, input.Permanent.PersistentHealth, input.Permanent.ExternalInteractions), input.Temporary));
                Require(!EquipmentMergeModel.Apply(unsupportedOffsets, rule, selected, definitions, optional).Supported,
                    "Uncalibrated permanent offsets must not be silently dropped by merging.");
            }
            Require(!EquipmentMergeModel.Apply(before, rule, null, definitions, optional).Supported &&
                !EquipmentMergeModel.Apply(before, rule, [int.MaxValue, int.MaxValue], definitions, optional).Supported,
                "Missing explicit/missing-identity merge guards.");
            index++;
        }
        Require(index == 3, "Missing original cancellation and both source-pile merges.");
        int equipped = 0;
        foreach (FixtureValue row in root.GetProperty("Actions").EnumerateArray())
        {
            var before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            var action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
            if (before.Spawn.Train.Context!.FindCard(action.CardInstanceId)!.DataId != OutputId) continue;
            var after = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            CombatUnit old = before.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == action.TargetUnitId);
            CombatUnit updated = after.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == old.Id);
            CardInstanceState card = before.Spawn.Train.Context.FindCard(action.CardInstanceId)!;
            CardUpgradeModifier permanent = card.Permanent.Upgrades.Single();
            Require(updated.BaseAttack - old.BaseAttack == 30 + permanent.Stats.Damage &&
                updated.MaxHealth - old.MaxHealth == 30 + permanent.Stats.Health && updated.EquipmentCards!.Contains(card.InstanceId),
                "Merged attachment did not apply both base equipment payloads and its permanent modifier once.");
            equipped++;
        }
        Require(equipped == 2, "Both distinct merged outputs must be equipped.");
        RecordedBattleChecks.Native(root);
        Console.WriteLine("FUSED-WEAPONRY PASS: original cancellation, ordered hand/discard and draw/discard merges, native same-definition Test boundary, permanent-only inheritance, both merged attachments and immutable action chains.");
    }
    private static void Equal<T>(T predicted, T actual, string label)
    {
        string? difference = ModelJson.Difference(JsonSerializer.Serialize(predicted, ModelJson.Options), JsonSerializer.Serialize(actual, ModelJson.Options));
        Require(difference == null, label + ": " + difference);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
