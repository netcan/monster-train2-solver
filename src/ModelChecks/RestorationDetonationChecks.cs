using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RestorationDetonationChecks
{
    private const string DataId = "5f4f426e-3ca6-4a04-bede-3eab9a3723a9";
    private static string UnitId(int index) => "f15d0000-0000-4000-8000-00000000000" + index;
    internal static PlayCardAction? Choose(BattleTurnState source, int[] healCards)
    {
        CombatContext context = source.Spawn.Train.Context!;
        var plays = BattleActionModel.EnumerateSupportedPlays(source);
        foreach (int index in new[] { 1, 2, 3 })
        {
            var unit = plays.FirstOrDefault(play => play.RoomIndex == 0 && context.FindCard(play.CardInstanceId)!.DataId == UnitId(index));
            if (unit != null) return unit;
        }
        CombatUnit? Target(int index) => source.Spawn.Train.Rooms[0].Units.FirstOrDefault(unit => unit.SpawnerCardId > 0 && context.FindCard(unit.SpawnerCardId)?.DataId == UnitId(index));
        PlayCardAction? Heal(int index, int target) => context.Statistics!.Value(healCards[index], "TimesPlayed", "ThisBattle") == 0
            ? plays.FirstOrDefault(play => play.CardInstanceId == healCards[index] && play.TargetUnitId == target) : null;
        var normal = Target(1); var immune = Target(2); var doubled = Target(3);
        if (normal == null || immune == null || doubled == null) return null;
        var full = Heal(0, normal.Id); if (full != null && normal.Health == normal.MaxHealth) return full;
        var injury = plays.FirstOrDefault(play => play.RoomIndex == 0 && context.FindCard(play.CardInstanceId)!.DataId == UnitId(4));
        if (injury != null) return injury;
        var blocked = Heal(1, immune.Id); if (blocked != null) return blocked;
        if (!source.Spawn.Train.Rooms[0].Deployment && source.Spawn.Train.Rooms[0].Units.Any(unit => unit.Team == CombatTeam.Enemy))
            return Heal(2, normal.Id) ?? Heal(3, doubled.Id);
        return null;
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("RestorationDetonationPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed restoration witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int full = 0, immune = 0, positive = 0, doubled = 0;
        foreach (FixtureValue row in root.GetProperty("RestorationDetonationRecords").EnumerateArray())
        {
            Require(row.GetProperty("Completed").GetBoolean(), "Incomplete native heal-relative effect.");
            TrainCombatState before = row.GetProperty("Before").Deserialize<TrainCombatState>()!, after = row.GetProperty("After").Deserialize<TrainCombatState>()!;
            int id = row.GetProperty("Targets")[0].GetInt32(), source = row.GetProperty("CardId").GetInt32();
            CombatUnit target = before.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == id);
            CombatUnit restored = after.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == id);
            int healed = restored.Health - target.Health;
            CardActionEffect effect = row.GetProperty("Effect").Deserialize<CardActionEffect>()!;
            BattlePlayRules definitions = row.GetProperty("Definitions").Deserialize<BattlePlayRules>()!;
            Require(effect.Type == "HealRelative" && effect.Value == 15 && effect.HealDamageMultiplier == 5,
                "Original restoration parameters changed.");
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            void Replay()
            {
                var result = CardSpellModel.ApplyForCardPlay(before, 0, [effect], id, source, definitions, before.Context!.OtherPiles);
                Require(result.Supported, result.UnsupportedReason ?? "Heal-relative effect unsupported.");
                Equal(result.State, CardSpellModel.WithContext(after, after.Context!.AfterCardEffects()), "complete native effect state/RNG");
            }
            Replay(); Parallel.For(0, 16, _ => Replay());
            Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Heal-relative effect mutated its parent.");
            var damages = row.GetProperty("Damages").EnumerateArray().ToArray();
            if (target.Health == target.MaxHealth)
            {
                Require(healed == 0 && damages.Length == 0, "Full-health restoration dealt damage."); full++;
            }
            else if (target.Status("heal immunity") != null)
            {
                Require(healed == 0 && damages.Length == 0, "Healing immunity failed to suppress relative damage."); immune++;
            }
            else
            {
                Require(healed > 0 && damages.Length == 1 && damages[0].GetProperty("Amount").GetInt32() == healed * 5,
                    "Native damage is not five times actual restored HP.");
                Require(damages[0].GetProperty("Gold").GetInt32() - before.Context!.Gold == 5,
                    "Native OnHeal did not settle before relative damage.");
                var victim = before.Rooms[0].Units.First(unit => unit.Team == CombatTeam.Enemy && unit.Health > 0);
                Require(damages[0].GetProperty("Target").GetInt32() == victim.Id, "Relative damage did not select the opposing front unit.");
                if (target.Status("heal multiplier") != null) { Require(healed == 30, "Double healing did not restore thirty HP."); doubled++; }
                else Require(healed == 15, "Normal restoration did not restore fifteen HP.");
                positive++;
            }
        }
        Require(full == 1 && immune == 1 && positive == 2 && doubled == 1, "Missing full-HP, immune, normal or doubled-healing evidence.");
        int originals = root.GetProperty("Actions").EnumerateArray().Count(row =>
        {
            BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            int id = row.GetProperty("Action").GetProperty("CardInstanceId").GetInt32();
            return before.Spawn.Train.Context!.FindCard(id)!.DataId == DataId;
        });
        Require(originals == 4, "Missing four successful original card plays.");
        RecordedBattleChecks.Native(root);
        Console.WriteLine("RESTORATION-DETONATION PASS: four original casts, full-HP and immunity controls, 75/150 native damage, OnHeal before opposing-front damage; full state/RNG and 16 immutable branches.");
    }
    private static void Equal<T>(T predicted, T actual, string label)
    {
        string? difference = ModelJson.Difference(JsonSerializer.Serialize(predicted, ModelJson.Options), JsonSerializer.Serialize(actual, ModelJson.Options));
        Require(difference == null, label + ": " + difference);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
