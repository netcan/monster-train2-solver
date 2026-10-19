using System.Linq;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalSpawningPodsChecks
{
    private const string CardId = "7c772030-474d-414c-939d-9581c6a70741";
    private const string EagerConscriptId = "0c8ff108-478c-4c1c-9c49-f6a297b4e0d8";

    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids) => OriginalSpawningPodsScenarioChooser.Choose(source, ids);

    internal static int[] CardIds(FixtureValue root) => root.GetProperty("Actions").EnumerateArray()
        .Select(action => action.GetProperty("Action").GetProperty("CardInstanceId").GetInt32()).ToArray();

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 103 && root.GetProperty("ModifierScenario").GetString() == "original-spawning-pods" &&
            root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original Spawning Pods witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);

        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        int[] ids = actions.Select(action => action.GetProperty("Action").GetProperty("CardInstanceId").GetInt32()).ToArray();
        Require(ids.Length == 3 && actions.Length == 3 && actions.Select(action =>
                action.GetProperty("Action").GetProperty("CardInstanceId").GetInt32()).SequenceEqual(ids),
            "The native policy must summon its anchor, seed a Horde with Eager Conscript, then play Spawning Pods.");

        FixtureValue setupEntry = actions[1];
        BattleTurnState setupBefore = setupEntry.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState setupAfter = setupEntry.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CardPlayRule setupRule = setupBefore.PlayRules!.Cards.Single(card => card.DataId == EagerConscriptId);
        CombatUnit seeded = setupAfter.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units.Single(unit => unit.AssetKey == "MonsterFunguy");
        Require(setupRule.Effects.Single().Type == "SpawnOrStatus" && setupRule.Effects[0].Statuses.Single() is { Id: "horde", Stacks: 1 } &&
            setupRule.Effects[0].Filters?.Subtype == "SubtypesData_Horde" && setupRule.Effects[0].SpawnFallbackUnit?.AssetKey == "MonsterFunguy" &&
            seeded.Status("horde")?.Stacks == 1, "The control play did not create exactly one Horde target with its original status.");

        FixtureValue entry = actions[2];
        BattleTurnState before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState after = entry.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatContext beforeContext = before.Spawn.Train.Context!;
        CombatContext afterContext = after.Spawn.Train.Context!;
        CardPlayRule rule = before.PlayRules!.Cards.Single(card => card.DataId == CardId);
        CombatUnit beforeFunguy = before.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units.Single(unit => unit.AssetKey == "MonsterFunguy");
        CombatUnit afterFunguy = after.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units.Single(unit => unit.Id == beforeFunguy.Id);
        CardTraitValue trait = beforeContext.FindCard(ids[2])!.MaskDescriptor!.BaseTraits.Single();
        Require(rule.AssetKey == "SpawningPods" && rule.Cost == 2 && rule.Effects.Count == 1 &&
            rule.Effects[0].Type == "SpawnOrStatus" && rule.Effects[0].Target == "Room" && rule.Effects[0].Value == 4 &&
            rule.Effects[0].Statuses.Single() is { Id: "horde", Stacks: 4 } && rule.Effects[0].Filters?.Subtype == "SubtypesData_Horde" &&
            rule.Effects[0].SpawnFallbackUnit?.AssetKey == "MonsterFunguy" && rule.Effects[0].AllowEnemy && !rule.Effects[0].AllowPlayer &&
            trait.RuntimeType == "CardTraitExhaustState" && trait.DeclaredName == trait.RuntimeType && trait.ParamInt == 0 &&
            beforeFunguy.Status("horde")?.Stacks == 1 && afterFunguy.Status("horde")?.Stacks == 5 &&
            beforeFunguy.Id == afterFunguy.Id && after.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units.Count ==
                before.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units.Count,
            "Spawning Pods must select the existing room Horde, add four stacks without spawning a replacement, and retain its original Consume trait.");
        Require(before.Energy - after.Energy == 2 && afterContext.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(card => card.InstanceId == ids[2]) &&
            afterContext.Cards.Discard.Any(card => card.InstanceId == ids[1]), "Spawning Pods did not pay two Ember, Consume itself and discard the control spell.");

        RecordedBattleChecks.Native(root);
        System.Console.WriteLine("ORIGINAL-SPAWNING-PODS PASS: unchanged 2-cost original adds Horde 4 to the existing filtered Monster, then Consumes itself.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new System.InvalidOperationException(message); }
}

internal static class OriginalSpawningPodsScenarioChooser
{
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
        bool hordeReady = source.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units.Any(unit =>
            unit.Team == CombatTeam.Player && unit.AssetKey == "MonsterFunguy" && (unit.Status("horde")?.Stacks ?? 0) == 1);
        if (hordeReady && context.Statistics.Value(ids[2], "TimesPlayed", "ThisBattle") == 0 && context.Cards.Hand.Any(card => card.InstanceId == ids[2]))
        {
            var cast = new PlayCardAction(ids[2], 0);
            if (BattleActionModel.PlayCard(source, cast).Supported) return cast;
        }
        return null;
    }
}
