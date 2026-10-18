using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalScaledMoonPhaseChecks
{
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
        if (index < 0 || index >= 2 && source.Spawn.Turn < 1) return null;
        int target = index is 2 or 3 or 5 ? source.Spawn.Train.Rooms[0].Units.Single(unit => unit.SpawnerCardId == ids[0]).Id : 0;
        return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] &&
            play.RoomIndex == (index == 1 ? 1 : 0) && (target == 0 || play.TargetUnitId == target));
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalScaledMoonPhasePrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original Lunacy witness.");
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        foreach (FixtureValue action in actions)
        {
            if (action.GetProperty("Difference").ValueKind == FixtureKind.Null) continue;
            BattleActionResult replay = BattleActionModel.PlayCard(action.GetProperty("Before").Deserialize<BattleTurnState>()!, action.GetProperty("Action").Deserialize<PlayCardAction>()!);
            string? difference = replay.Supported ? ModelJson.Difference(JsonSerializer.Serialize(replay.State, ModelJson.Options),
                JsonSerializer.Serialize(action.GetProperty("Actual").Deserialize<BattleTurnState>(), ModelJson.Options)) : replay.Reason;
            if (difference != null) throw new InvalidOperationException("Native Lunacy mismatch: " + difference);
        }
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }) Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("OriginalScaledMoonPhaseIds").Deserialize<int[]>()!;
        if (actions.Length != 6)
        {
            BattleTurnState decision = actions.Length > 0 ? actions.Last().GetProperty("Actual").Deserialize<BattleTurnState>()! :
                root.GetProperty("Turns").EnumerateArray().First().GetProperty("Before").Deserialize<BattleTurnState>()!;
            int index = Array.FindIndex(ids, id => decision.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            int target = index is 2 or 3 or 5 ? decision.Spawn.Train.Rooms[0].Units.Single(unit => unit.SpawnerCardId == ids[0]).Id : 0;
            BattleActionResult attempted = BattleActionModel.PlayCard(decision, new PlayCardAction(ids[index], index == 1 ? 1 : 0, targetUnitId: target));
            throw new InvalidOperationException("Missing native Lunacy play: " + attempted.Rejection + ": " + attempted.Reason);
        }
        FixtureValue[] counts = root.GetProperty("OriginalScaledMoonPhaseCounts").EnumerateArray().ToArray(), queries = root.GetProperty("OriginalScaledMoonPhaseQueries").EnumerateArray().ToArray(),
            samples = root.GetProperty("OriginalScaledMoonPhaseStatusSamples").EnumerateArray().ToArray(), phases = root.GetProperty("OriginalScaledMoonPhasePhases").EnumerateArray().ToArray();
        Require(ids.Length == 6 && counts.Select(row => row.GetProperty("ActualCount").GetInt32()).SequenceEqual([3, 0, 10]) && queries.Length == 12 && samples.Length == 3 && phases.Length == 3,
            "Missing positive/zero/large X, status controls or phase-effect boundaries.");
        foreach (FixtureValue row in counts)
        {
            CombatContext context = row.GetProperty("Context").Deserialize<CombatContext>()!; int id = row.GetProperty("CardId").GetInt32();
            string parent = JsonSerializer.Serialize(context, ModelJson.Options);
            int native = row.GetProperty("ActualCount").GetInt32(); StatisticQueryResult actual = CardMoonAdvanceScalingModel.Adjust(context, id, 0);
            CardInstanceState card = context.FindCard(id)!; CardMaskDescriptor descriptor = card.MaskDescriptor!;
            CardSourceTrait source = descriptor.SourceTraits!.Single(trait => trait.Type == CardMoonAdvanceScalingModel.Trait);
            Require(actual.Supported && actual.Value == native && native == context.Statistics!.PlayedCosts.Single(cost => cost.CardId == id).Cost &&
                CardMoonAdvanceScalingModel.Adjust(context, id, 2).Value == native + 2 && source.Int == 0 && source.Int2 == 0 && source.Int3 == 0 &&
                !source.Bool && source.Str == "" && source.Float == 1 && !source.Scaling && source.TrackedValue == "PlayedCost" && source.EntryDuration == "ThisTurn",
                "Native moon-advance bonus, additive effect amount or exact original source parameters differ.");
            CardInstanceState WithSource(IReadOnlyList<CardSourceTrait>? sources) => card.WithMaskDescriptor(new CardMaskDescriptor(descriptor.Definition, descriptor.BaseCost,
                descriptor.BaseTraits, descriptor.AuthoredCastEffects, descriptor.Purified, descriptor.PermanentGraft, descriptor.TemporaryTraits, descriptor.PermanentReplacements, sources));
            CardSourceTrait changed = new(source.Type, source.Int, source.Int2, source.Int3, source.Bool, source.Str, 2, source.Scaling, source.TrackedValue, source.EntryDuration);
            Require(!CardMoonAdvanceScalingModel.Adjust(context.WithCard(WithSource(null)), id, 0).Supported &&
                !CardMoonAdvanceScalingModel.Adjust(context.WithCard(WithSource(descriptor.SourceTraits.Select(trait => trait.Type == source.Type ? changed : trait).ToArray())), id, 0).Supported,
                "Missing source or another moon-advance multiplier must remain guarded.");
            Require(parent == JsonSerializer.Serialize(context, ModelJson.Options), "Moon-advance query or guard mutated its parent.");
        }
        foreach (FixtureValue row in queries.Concat(samples))
        {
            CombatContext context = row.GetProperty("Context").Deserialize<CombatContext>()!; int id = row.GetProperty("CardId").GetInt32(), input = row.GetProperty("InputStacks").GetInt32();
            string status = row.GetProperty("StatusId").GetString()!;
            StatusScalingResult result = StatusScalingModel.Apply(context, id, CombatTeam.Player, status, input);
            CardSourceTrait source = context.FindCard(id)!.MaskDescriptor!.SourceTraits!.Single(trait => trait.Type == "CardTraitScalingAddStatusEffect");
            int bonus = status == "conduit" ? context.Statistics!.PlayedCosts.Single(cost => cost.CardId == id).Cost : 0;
            Require(result.Supported && result.Stacks == input + bonus && row.GetProperty("ActualBonus").GetInt32() == bonus &&
                source.Int == 1 && source.Int2 == 0 && source.Int3 == 0 && !source.Bool && source.Str == "" && source.Float == 1 && !source.Scaling &&
                source.TrackedValue == "PlayedCost" && source.EntryDuration == "ThisTurn" && source.StatusEffectIds!.SequenceEqual(["conduit"]) &&
                (!row.TryGetProperty("StateUnchanged", out FixtureValue unchanged) || unchanged.GetBoolean()), "Native Conduit bonus, unmatched control or exact status source differs.");
        }
        foreach (FixtureValue row in phases)
        {
            TrainCombatState before = row.GetProperty("Before").Deserialize<TrainCombatState>()!, after = row.GetProperty("After").Deserialize<TrainCombatState>()!;
            MoonPhaseTransitionResult predicted = MoonPhaseTransitionModel.Apply(before, new CardActionEffect("AdvanceMoonPhase", "Room", 0, false, false, []), row.GetProperty("CardId").GetInt32());
            string? difference = predicted.Supported ? ModelJson.Difference(JsonSerializer.Serialize(predicted.State, ModelJson.Options), JsonSerializer.Serialize(after, ModelJson.Options)) : predicted.UnsupportedReason;
            Require(row.GetProperty("Completed").GetBoolean() && difference == null, "Native complete moon-advance train boundary differs: " + difference);
        }
        foreach (int index in new[] { 2, 3, 5 })
        {
            BattleTurnState before = actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!, after = actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            CardPlayRule rule = before.PlayRules!.Cards.Single(card => card.DataId == "e9fa09e5-13bd-4826-a8e5-969332cbfbbf");
            CombatUnit old = before.Spawn.Train.Rooms[0].Units.Single(unit => unit.SpawnerCardId == ids[0]), changed = after.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == old.Id);
            int paid = index == 2 ? 3 : index == 3 ? 0 : 10, phase = before.Spawn.Train.Context!.QueryFrame!.MoonPhase!.Value;
            Require(rule.Cost == 0 && rule.CostType == "ConsumeRemainingEnergy" && rule.ExternalInteractions.Count == 0 && rule.UpgradeInteractions!.Count == 0 &&
                rule.Effects.Select(effect => effect.Type).SequenceEqual(["AdvanceMoonPhase", "AddStatus"]) && rule.Effects.All(effect => effect.Value == 0) &&
                before.Energy == paid && after.Energy == 0 && after.Spawn.Train.Context!.QueryFrame!.MoonPhase == (paid % 2 == 0 ? phase : phase == 1 ? 2 : 1) &&
                (changed.Status("conduit")?.Stacks ?? 0) - (old.Status("conduit")?.Stacks ?? 0) == paid && changed.Attack == old.Attack && changed.Health == old.Health &&
                after.Spawn.Train.Rooms[1].Units.All(unit => unit.Status("conduit") == null) &&
                after.Spawn.Train.Context.Cards.Discard.Any(token => token.InstanceId == ids[index]), "Original effects/order, paid X, phase parity, Conduit target/floor control, health/attack or discard differs.");
        }
        RecordedBattleChecks.Native(root);
        Console.WriteLine("ORIGINAL-SCALED-MOON-PHASE PASS: original Lunacy X three/zero/ten, exact advance counts and complete phase train contexts before Conduit, twelve unchanged matched/unmatched status queries, target/floor/health controls, discard, source guards and 16 immutable chains.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
