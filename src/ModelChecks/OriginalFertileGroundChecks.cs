using System.Linq;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalFertileGroundChecks
{
    private const string CardId = "0c718572-ef91-4152-b6a9-0fdf907a9b06";
    private const string EagerConscriptId = "0c8ff108-478c-4c1c-9c49-f6a297b4e0d8";
    private const string SpawningPodsId = "7c772030-474d-414c-939d-9581c6a70741";
    private const string StewardId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";

    internal static int[] CardIds(FixtureValue root) => root.GetProperty("Actions").EnumerateArray()
        .Select(action => action.GetProperty("Action").GetProperty("CardInstanceId").GetInt32()).ToArray();

    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        CombatContext context = source.Spawn.Train.Context!;
        if (context.Statistics!.Value(ids[0], "TimesPlayed", "ThisBattle") == 0)
        {
            var summon = new PlayCardAction(ids[0], 0, playerPosition: 0);
            if (BattleActionModel.PlayCard(source, summon).Supported) return summon;
        }
        if (context.Statistics.Value(ids[1], "TimesPlayed", "ThisBattle") == 0 && context.Cards.Hand.Any(card => card.InstanceId == ids[1]))
        {
            var seed = new PlayCardAction(ids[1], 0);
            if (BattleActionModel.PlayCard(source, seed).Supported) return seed;
        }

        CombatUnit? horde = source.Spawn.Train.Rooms[0].Units.SingleOrDefault(unit =>
            unit.Team == CombatTeam.Player && unit.AssetKey == "MonsterFunguy");
        if (horde?.Status("horde")?.Stacks == 1 && context.Statistics.Value(ids[2], "TimesPlayed", "ThisBattle") == 0 &&
            context.Cards.Hand.Any(card => card.InstanceId == ids[2]))
        {
            var pods = new PlayCardAction(ids[2], 0);
            if (BattleActionModel.PlayCard(source, pods).Supported) return pods;
        }

        bool spellExhausted = context.OtherPiles?.Single(pile => pile.Name == "Exhausted").Cards.Any(card => card.InstanceId == ids[2]) == true;
        if (horde?.Status("horde")?.Stacks == 5 && spellExhausted && source.Spawn.Turn >= 1 &&
            context.Statistics.Value(ids[3], "TimesPlayed", "ThisBattle") == 0 && context.Cards.Hand.Any(card => card.InstanceId == ids[3]))
        {
            var cast = new PlayCardAction(ids[3], 0);
            if (BattleActionModel.PlayCard(source, cast).Supported) return cast;
        }
        return null;
    }

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 103 && root.GetProperty("ModifierScenario").GetString() == "original-fertile-ground" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean(),
            "Missing completed original Fertile Ground witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);

        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        int[] ids = CardIds(root);
        Require(actions.Length == 4 && ids.Length == 4, "Expected one anchor, two Horde setup cards and the original Fertile Ground play.");
        BattleTurnState Before(int index) => actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int index) => actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatUnit Funguy(TrainCombatState state) => state.Rooms[0].Units.Single(unit => unit.AssetKey == "MonsterFunguy");

        BattleTurnState eagerBefore = Before(1);
        BattleTurnState eagerAfter = After(1);
        CardPlayRule eagerRule = eagerBefore.PlayRules!.Cards.Single(card => card.DataId == EagerConscriptId);
        Require(eagerBefore.Spawn.Train.Context!.FindCard(ids[1])!.DataId == EagerConscriptId &&
            eagerRule.Effects.Single().Type == "SpawnOrStatus" && eagerRule.Effects[0].Statuses.Single() is { Id: "horde", Stacks: 1 } &&
            eagerRule.Effects[0].Filters?.Subtype == "SubtypesData_Horde" && eagerRule.Effects[0].SpawnFallbackUnit?.AssetKey == "MonsterFunguy" &&
            Funguy(eagerAfter.Spawn.Train).Status("horde")?.Stacks == 1,
            "Eager Conscript did not seed exactly one original Horde target.");

        BattleTurnState podsBefore = Before(2);
        BattleTurnState podsAfter = After(2);
        CardPlayRule podsRule = podsBefore.PlayRules!.Cards.Single(card => card.DataId == SpawningPodsId);
        CombatUnit seeded = Funguy(podsBefore.Spawn.Train);
        CombatUnit podded = Funguy(podsAfter.Spawn.Train);
        Require(seeded.Status("horde")?.Stacks == 1 && podsRule.Effects.Single().Type == "SpawnOrStatus" &&
            podsRule.Effects[0].Value == 4 && podsRule.Effects[0].Statuses.Single() is { Id: "horde", Stacks: 4 } &&
            podded.Id == seeded.Id && podded.Status("horde")?.Stacks == 5 &&
            podsAfter.Spawn.Train.Context!.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(card => card.InstanceId == ids[2]),
            "Spawning Pods failed to build a five-stack Horde or exhaust the scaling control spell.");

        BattleTurnState before = Before(3);
        BattleTurnState after = After(3);
        CombatContext input = before.Spawn.Train.Context!;
        CombatContext output = after.Spawn.Train.Context!;
        CombatUnit hordeBefore = Funguy(before.Spawn.Train);
        CombatUnit hordeAfter = Funguy(after.Spawn.Train);
        CardInstanceState card = input.FindCard(ids[3])!;
        CardPlayRule rule = before.PlayRules!.Cards.Single(item => item.DataId == CardId);
        CardSourceTrait[] sourceTraits = card.MaskDescriptor!.SourceTraits!.ToArray();
        CardSourceTrait source = sourceTraits.Single(trait => trait.Type == "CardTraitScalingAddStatusEffect");
        ScalingStatusTrait scaling = card.StatusScalingTraits!.Single();
        StatisticQueryResult query = StatisticQueryModel.Evaluate(input, scaling.Query, ids[3]);
        StatusScalingResult scaled = StatusScalingModel.Apply(input, ids[3], CombatTeam.Player, "horde", 1);
        PlayCardAction cast = actions[3].GetProperty("Action").Deserialize<PlayCardAction>()!;

        Require(card.DataId == CardId && rule.AssetKey == "FertileGround" && rule.CardType == "Spell" && rule.Cost == 2 &&
            rule.ExternalInteractions.Count == 0 && rule.Effects.Single().Type == "SpawnOrStatus" && rule.Effects[0].Target == "Room" &&
            rule.Effects[0].Value == 1 && rule.Effects[0].Filters?.Subtype == "SubtypesData_Horde" &&
            rule.Effects[0].Statuses.Single() is { Id: "horde", Stacks: 1 } && rule.Effects[0].SpawnFallbackUnit?.AssetKey == "MonsterFunguy",
            "Original Fertile Ground identity, base Horde effect or fallback unit differs.");
        Require(sourceTraits.Select(trait => trait.Type).SequenceEqual(["CardTraitScalingAddStatusEffect", "CardTraitExhaustState"]) &&
            source.Int == 1 && source.Int2 == 2 && source.Int3 == 1 && source.TrackedValue == "TypeInExhaustPile" &&
            source.EntryDuration == "ThisBattle" && scaling.StacksPerStat == 1 && scaling.Filter == 2 &&
            scaling.Query.Type == "TypeInExhaustPile" && scaling.Query.Duration == "ThisBattle",
            "Original Fertile Ground source traits or native scaling query differ.");
        Require(input.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Select(item => item.InstanceId).SequenceEqual([ids[2]]) &&
            query.Supported && query.Value == 1 && scaled.Supported && scaled.Stacks == 2,
            "Fertile Ground did not count the one exhausted Spawning Pods spell as one Horde bonus.");
        Require(hordeBefore.Id == hordeAfter.Id && hordeBefore.Status("horde")?.Stacks == 5 && hordeAfter.Status("horde")?.Stacks == 7 &&
            cast.CardInstanceId == ids[3] && cast.RoomIndex == 0 && before.Spawn.Turn >= 1 && before.Energy - after.Energy == 2 &&
            output.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(item => item.InstanceId == ids[2]) &&
            output.OtherPiles.Single(pile => pile.Name == "Exhausted").Cards.Any(item => item.InstanceId == ids[3]) &&
            output.Cards.Discard.All(item => item.InstanceId != ids[3]) &&
            after.Spawn.Train.Context!.Statistics!.Value(ids[3], "TimesPlayed", "ThisBattle") == 1,
            "Fertile Ground did not apply the predicted Horde stacks, pay two Ember or Exhaust itself.");

        RecordedBattleChecks.Native(root);
        System.Console.WriteLine("ORIGINAL-FERTILE-GROUND PASS: native TypeInExhaustPile scaling sees Spawning Pods, adding Horde 2 (base 1 + scaled 1) to the existing unit.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new System.InvalidOperationException(message); }
}
