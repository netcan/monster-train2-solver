using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class GoldmanceChecks
{
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        CombatContext context = source.Spawn.Train.Context!;
        var plays = BattleActionModel.EnumerateSupportedPlays(source);
        for (int index = 0; index < 4; index++)
            if (context.Statistics!.Value(ids[index], "TimesPlayed", "ThisBattle") == 0)
                return plays.FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == index % 2 && play.PlayerPosition == (index < 2 ? 0 : 1));
        CombatUnit[] actors = ids.Skip(2).Take(2).Select(id => source.Spawn.Train.Rooms.SelectMany(room => room.Units)
            .Single(unit => unit.SpawnerCardId == id)).ToArray();
        int used = actors.Count(unit => unit.Triggers.Any(trigger => trigger.Kind == "OnOwnAbilityActivated" && trigger.HasTriggered));
        if (used == 1 && context.Statistics!.Value(ids[4], "TimesPlayed", "ThisBattle") == 0)
            return plays.FirstOrDefault(play => play.CardInstanceId == ids[4] && play.RoomIndex == 0);
        return used >= 2 ? null : plays.FirstOrDefault(play => play.ActivatorUnitId == actors[used].Id);
    }

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("GoldmancePrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original Goldmance witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("GoldmanceCardIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 5 && actions.Length == 7, "Missing zero/positive Hoard activations and adjustment.");
        BattleTurnState Before(int index) => actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int index) => actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction Action(int index) => actions[index].GetProperty("Action").Deserialize<PlayCardAction>()!;
        CombatUnit Actor(BattleTurnState state, int source) => state.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == source);
        const string abilityId = "ac258de8-8765-4856-87c2-03868ebbf27e";
        CombatContext initial = Before(4).Spawn.Train.Context!, zero = After(4).Spawn.Train.Context!, positive = After(6).Spawn.Train.Context!;
        Require(Before(4).DragonsHoard == 0 && After(4).DragonsHoard == 0 && zero.Gold == initial.Gold,
            "Zero Hoard must allow activation without rewarding gold.");
        Require(Before(6).DragonsHoard == 3 && After(6).DragonsHoard == 3 && positive.Gold - Before(6).Spawn.Train.Context!.Gold == 15,
            "Original Goldmance must reward five gold per Hoard without consuming Hoard.");
        Require(Action(4).CardInstanceId == Action(6).CardInstanceId && Action(4).ActivatorUnitId == Actor(Before(4), ids[2]).Id &&
            Action(6).ActivatorUnitId == Actor(Before(6), ids[3]).Id && Action(4).ActivatorUnitId != Action(6).ActivatorUnitId &&
            positive.FindCard(Action(6).CardInstanceId)!.DataId == abilityId, "Distinct original Whelps must share the original ability cache card.");
        foreach (int index in new[] { 4, 6 })
        {
            CombatUnit actor = Actor(After(index), ids[index == 4 ? 2 : 3]);
            Require(actor.Status("cooldown")?.Stacks == 2 && actor.Triggers.Single(trigger => trigger.Kind == "OnOwnAbilityActivated").FireCount == 1,
                "Each activation must apply its own cooldown and original activation trigger once.");
        }
        Require(Actor(After(4), ids[3]).Status("cooldown") == null && Before(6).Energy == After(6).Energy,
            "Zero activation altered the other Whelp's cooldown or zero-cost activation spent energy.");
        RecordedBattleChecks.Native(root);
        Console.WriteLine("GOLDMANCE PASS: unchanged original Whelps/ability, legal zero reward, positive 5x3 gold, shared cache, separate cooldowns and 16 immutable full chains.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
