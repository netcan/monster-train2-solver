using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalSpellCompositionsChecks
{
    private static readonly string[] Originals = ["a6c8ce76-3c82-47ca-a6e0-8a20e84558dd", "6a6c4a72-3e32-4995-8070-68d9240de0d5",
        "4bf98c5f-73c5-42ed-af98-9dc8cf77c1ac", "d5dd7d58-dd1b-4c46-9308-77179c39c915", "25787bff-d2fb-4d9a-a636-d96a5863d559",
        "f7839c43-c139-4671-9f53-9347673e3b0b", "64beabe3-0260-44e2-a47a-b07192b52f25"];
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
        if (index < 0 || index >= 2 && source.Spawn.Turn < 1) return null;
        int target = index >= 3 && index != 6 ? source.Spawn.Train.Rooms[0].Units.Single(unit => unit.SpawnerCardId == ids[0]).Id : 0;
        return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == (index == 1 ? 1 : 0) && play.TargetUnitId == target);
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalSpellCompositionsPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed spell compositions witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }) Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("OriginalSpellCompositionsIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 10 && actions.Length == 10, "Missing seven original spell plays.");
        BattleTurnState Before(int i) => actions[i].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int i) => actions[i].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatUnit Friendly(BattleTurnState state) => state.Spawn.Train.Rooms[0].Units.Single(unit => unit.SpawnerCardId == ids[0]);
        int Stacks(CombatUnit unit, string id) => unit.Status(id)?.Stacks ?? 0;
        foreach (int i in Enumerable.Range(3, 7))
        {
            BattleTurnState before = Before(i), after = After(i); CombatUnit a = Friendly(before), b = Friendly(after);
            CardInstanceState card = before.Spawn.Train.Context!.FindCard(ids[i])!;
            CardPlayRule rule = before.PlayRules!.Cards.Single(rule => rule.DataId == Originals[i - 3]);
            Require(card.DataId == Originals[i - 3] && card.MaskDescriptor!.SourceTraits!.Count == 0 && rule.ExternalInteractions.Count == 0 &&
                before.Energy - after.Energy == rule.Cost - (i == 5 ? 1 : 0) &&
                after.Spawn.Train.Context!.Cards.Discard.Any(token => token.InstanceId == ids[i]), "Original identity, cost, source or ordinary discard differs.");
            Require(Json(before.Spawn.Train.Rooms[1].Units) == Json(after.Spawn.Train.Rooms[1].Units), "Outside-floor unit changed.");
            Require(a.MaxHealth == b.MaxHealth, "Heal or damage changed maximum health.");
            if (i == 3) Require(a.Health - b.Health == 3 && Stacks(b, "poison") - Stacks(a, "poison") == 8, "Flash Freeze must damage before adding eight Frostbite.");
            if (i == 4) Require(a.Health - b.Health == 5 && Stacks(b, "buff") - Stacks(a, "buff") == 7 &&
                Json(before.Spawn.Train.Context.BattleRng.Range(0, 100).State) == Json(after.Spawn.Train.Context.BattleRng), "Dark Deal damage, seven Rage or guaranteed-chance RNG differs.");
            if (i == 5) Require(b.Health - a.Health == 1 && after.Spawn.Train.Context.Cards.DrawModifier - before.Spawn.Train.Context.Cards.DrawModifier == 1,
                "Engraft must positively heal, schedule one draw and return its one ember.");
            if (i == 6)
            {
                Require(b.Health - a.Health == 3, "Glimmer must positively heal a friendly unit.");
                CombatUnit[] enemies = before.Spawn.Train.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Enemy && unit.Health > 0).ToArray();
                Require(enemies.Length > 0 && enemies.Any(unit => unit.Health + Stacks(unit, "armor") > 3) && enemies.All(unit =>
                {
                    CombatUnit? hit = after.Spawn.Train.Rooms[0].Units.SingleOrDefault(hit => hit.Id == unit.Id);
                    int remaining = hit == null ? 0 : hit.Health + Stacks(hit, "armor");
                    int original = unit.Health + Stacks(unit, "armor");
                    return original - remaining == Math.Min(3, original);
                }),
                    "Glimmer must deal three damage to every present enemy.");
            }
            if (i == 7) Require(a.Health < a.MaxHealth && b.Health == b.MaxHealth &&
                after.Spawn.Train.Context.Cards.DrawModifier - before.Spawn.Train.Context.Cards.DrawModifier == 2, "Focused Growth must clamp its ordered heals and schedule two draws.");
            if (i == 8) Require(a.Health == a.MaxHealth && b.Health == a.Health && Stacks(b, "armor") - Stacks(a, "armor") == 5, "Fae's Blessing must convert five overheal into armor.");
            if (i == 9) Require(Stacks(b, "buff") - Stacks(a, "buff") == 6 && Stacks(b, "armor") - Stacks(a, "armor") == 10 && b.Health == a.Health,
                "Enflame must add six Rage and ten armor to the same target.");
        }
        Require(root.GetProperty("CardCycles").EnumerateArray().Any(row => row.GetProperty("Kind").GetString() == "Draw" &&
            row.GetProperty("Before").Deserialize<CardCycleState>()!.DrawModifier == 3 && row.GetProperty("Actual").Deserialize<CardCycleState>()!.Hand.Count == 8 &&
            row.GetProperty("Actual").Deserialize<CardCycleState>()!.DrawModifier == 0), "Native next turn must draw eight and reset the one-turn draw modifier.");
        BonusDrawChecks.Native(root);
        RecordedBattleChecks.Native(root);
        AttackBuffDamageTargetChecks.Native(root);
        Console.WriteLine("ORIGINAL-SPELL-COMPOSITIONS PASS: seven unchanged original spells, positive damage/healing/overheal/status compositions, exact costs/discard, guaranteed chance RNG, floor/team controls, eight-card future draw/reset and 16 immutable chains. Other target/upgrade/relic combinations need separate witnesses.");
    }
    private static string Json<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
