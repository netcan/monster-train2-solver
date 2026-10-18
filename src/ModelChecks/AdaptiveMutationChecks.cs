using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class AdaptiveMutationChecks
{
    private const string DataId = "6f23bf7b-49c3-4a30-aa84-542ab9f939d0";
    private static string ControlId(int index) => "f15d0005-0000-4000-8000-00000000000" + index;
    internal static PlayCardAction? Choose(BattleTurnState source, int[] spells)
    {
        CombatContext context = source.Spawn.Train.Context!; var plays = BattleActionModel.EnumerateSupportedPlays(source);
        foreach (int index in new[] { 1, 2, 3, 4, 5 })
        {
            CombatUnit? normal = source.Spawn.Train.Rooms[0].Units.FirstOrDefault(unit => unit.SpawnerCardId > 0 && context.FindCard(unit.SpawnerCardId)?.DataId == ControlId(1));
            var action = plays.FirstOrDefault(play => play.RoomIndex == 0 && context.FindCard(play.CardInstanceId)!.DataId == ControlId(index) &&
                (index < 4 || play.TargetUnitId == normal?.Id));
            if (action != null) return action;
        }
        for (int index = 0; index < spells.Length; index++)
        {
            if (context.Statistics!.Value(spells[index], "TimesPlayed", "ThisBattle") > 0) continue;
            CombatUnit? target = source.Spawn.Train.Rooms[0].Units.FirstOrDefault(unit => unit.SpawnerCardId > 0 && context.FindCard(unit.SpawnerCardId)?.DataId == ControlId(index + 1));
            return plays.FirstOrDefault(play => play.CardInstanceId == spells[index] && play.TargetUnitId == target?.Id);
        }
        return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("AdaptiveMutationPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed stat-swap witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int count = 0, healing = 0, immune = 0, healer = 0;
        foreach (FixtureValue row in root.GetProperty("AdaptiveMutationRecords").EnumerateArray())
        {
            Require(row.GetProperty("Completed").GetBoolean(), "Incomplete native swap effect.");
            TrainCombatState before = row.GetProperty("Before").Deserialize<TrainCombatState>()!, after = row.GetProperty("After").Deserialize<TrainCombatState>()!;
            int id = row.GetProperty("Targets")[0].GetInt32(), source = row.GetProperty("CardId").GetInt32();
            CombatUnit target = before.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == id);
            CombatUnit changed = after.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == id);
            CardActionEffect effect = row.GetProperty("Effect").Deserialize<CardActionEffect>()!;
            Require(before.Context!.FindCard(source)!.DataId == DataId && effect.Type == "SwapStats", "Original swap card changed.");
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            void Replay()
            {
                var result = UnitStatSwapModel.Apply(before.Rooms.Single(room => room.RoomIndex == 0), id);
                Require(result.Supported, result.UnsupportedReason ?? "Stat swap unsupported.");
                var train = new TrainCombatState(before.Rooms.Select(room => room.RoomIndex == 0 ? result.State! : room).ToArray(),
                    before.Movement, before.EnemySlotsPerRoom, result.State!.Context);
                Equal(CardSpellModel.WithContext(train, result.State.Context!), after, "complete native stat-swap state/RNG");
            }
            Replay(); Parallel.For(0, 16, _ => Replay());
            Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Stat swap mutated its parent.");
            Require(after.Context!.Gold == before.Context.Gold, "Internal healing fired OnHeal.");
            CardUpgradeModifier upgrade = changed.Modifiers!.Upgrades.Last();
            Require(after.Context.FindCard(changed.SpawnerCardId)!.Temporary.Upgrades.Any(item =>
                item.DataId.Length == 0 && item.Stats.Damage == upgrade.Stats.Damage && item.Stats.Health == upgrade.Stats.Health && item.DamageBuff == upgrade.DamageBuff),
                "Anonymous unit upgrade was not written to the temporary spawner state.");
            if (target.Modifiers!.DamageBuff == 7)
            {
                Require(target.Health == 1499 && target.MaxHealth == 2000 && changed.Health == 47 && changed.MaxHealth == 47 &&
                    changed.Attack == 2000 && changed.Modifiers.DamageBuff == 0 && upgrade.DamageBuff == -7,
                    "Injured/damage-buff swap did not complete silent healing and cancel only the damage buff."); healing++;
            }
            else if (target.Status("heal immunity") != null)
            {
                Require(target.Health == 70 && changed.Health == 5 && changed.MaxHealth == 35 && changed.Attack == 70,
                    "Healing immunity or negative maximum-health adjustment differs."); immune++;
            }
            else
            {
                Require(target.Triggers.Any(trigger => trigger.Kind == "PostCombatHealing") && changed.MaxHealth == 17 && changed.Attack == 100,
                    "Healer swap did not use its first healing scalar as the new maximum health."); healer++;
            }
            count++;
        }
        Require(count == 3 && healing == 1 && immune == 1 && healer == 1, "Missing normal, immune or healer native control.");
        RecordedBattleChecks.Native(root);
        Console.WriteLine("ADAPTIVE-MUTATION PASS: three unchanged originals, silent internal healing, damage-buff cancellation, immunity/healer controls and temporary spawner upgrades; full state/RNG and 16 immutable branches.");
    }
    private static void Equal<T>(T predicted, T actual, string label)
    {
        string? difference = ModelJson.Difference(JsonSerializer.Serialize(predicted, ModelJson.Options), JsonSerializer.Serialize(actual, ModelJson.Options));
        Require(difference == null, label + ": " + difference);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
