using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalEagerConscriptChecks
{
    private const string CardId = "0c8ff108-478c-4c1c-9c49-f6a297b4e0d8";

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 249 &&
            root.GetProperty("ModifierScenario").GetString() == "original-eager-conscript" &&
            root.GetProperty("OriginalEagerConscriptPrepared").GetBoolean() &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean(),
            "Missing completed original Eager Conscript witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);

        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        FixtureValue[] plays = actions.Where(Plays).ToArray();
        Require(plays.Length >= 2, "Expected two direct original Eager Conscript plays.");
        FixtureValue[] firstPlays = plays.Take(2).ToArray();
        Require(firstPlays.Select(action => action.GetProperty("Action").GetProperty("CardInstanceId").GetInt32())
            .Distinct().Count() == 2, "The two original Horde paths must use separate Eager Conscript copies.");
        int? spawnedId = null;
        for (int index = 0; index < firstPlays.Length; index++)
        {
            FixtureValue action = firstPlays[index];
            BattleTurnState before = action.GetProperty("Before").Deserialize<BattleTurnState>()!;
            BattleTurnState after = action.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            int cardId = action.GetProperty("Action").GetProperty("CardInstanceId").GetInt32();
            CardPlayRule rule = before.PlayRules!.Cards.Single(card => card.DataId == CardId);
            CombatUnit[] beforeHorde = before.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units
                .Where(unit => unit.Team == CombatTeam.Player && unit.Status("horde")?.Stacks > 0).ToArray();
            CombatUnit[] afterUnits = after.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units
                .Where(unit => unit.Team == CombatTeam.Player).ToArray();
            CombatUnit fungus = afterUnits.Single(unit => unit.AssetKey == "MonsterFunguy");
            int expectedStacks = index + 1;
            Require(rule.AssetKey == "EagerConscript" && rule.Cost == 1 && rule.Effects.Count == 1,
                "Original Eager Conscript identity, cost or effect count differs.");
            Require(rule.Effects[0].Type == "SpawnOrStatus" && rule.Effects[0].Target == "Room" &&
                rule.Effects[0].Statuses.Single() is { Id: "horde", Stacks: 1 } &&
                rule.Effects[0].Filters?.Subtype == "SubtypesData_Horde" &&
                rule.Effects[0].SpawnFallbackUnit?.AssetKey == "MonsterFunguy" &&
                rule.ExternalInteractions.Count == 0 && rule.UpgradeInteractions!.Count == 0,
                "Original Eager Conscript Horde target/filter, fallback or interaction guards differ.");
            bool paid = before.Energy - after.Energy == 1;
            bool discarded = after.Spawn.Train.Context!.Cards.Discard.Any(card => card.InstanceId == cardId);
            int playsThisBattle = after.Spawn.Train.Context.Statistics!.Value(cardId, "TimesPlayed", "ThisBattle");
            Require(paid && discarded && playsThisBattle >= 1,
                $"Original Eager Conscript payment, discard or per-copy play count differs: paid={paid}, discarded={discarded}, plays={playsThisBattle}, id={cardId}.");
            int actualStacks = fungus.Status("horde")?.Stacks ?? 0;
            Require(actualStacks == expectedStacks,
                $"Original Eager Conscript Horde stack count differs: action={index}, expected={expectedStacks}, actual={actualStacks}, unit={fungus.Id}.");
            Require(index == 0 ? beforeHorde.Length == 0 :
                beforeHorde.Length == 1 && beforeHorde[0].Id == spawnedId && fungus.Id == spawnedId,
                "Original Eager Conscript did not follow the expected fallback/extension branch.");
            if (index == 0) spawnedId = fungus.Id;
        }

        RecordedBattleChecks.Native(root);
        Console.WriteLine($"ORIGINAL-EAGER-CONSCRIPT PASS: unchanged 1-cost original spawns a Horde Funguy, then adds Horde to that same unit; {plays.Length} native actions replay.");
    }

    private static bool Plays(FixtureValue action)
    {
        int instanceId = action.GetProperty("Action").GetProperty("CardInstanceId").GetInt32();
        return action.GetProperty("Before").GetProperty("Spawn").GetProperty("Train").GetProperty("Context")
            .GetProperty("CardRegistry").EnumerateArray().Any(card =>
                card.GetProperty("InstanceId").GetInt32() == instanceId && card.GetProperty("DataId").GetString() == CardId);
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
