using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalStatusCompositionsChecks
{
    private static readonly string[] Originals = ["537f6adf-0fd2-4c6b-88a8-e6cefeb52a7e", "818357bd-4753-4079-a668-96e9dbd01094",
        "82684961-63e8-4a64-8da4-5c20120243a9", "2260a35e-0570-4e22-a3e0-4c39acfeb933", "a7207665-b6d1-4cd1-997e-8a7bb789c766",
        "2ffb1e93-357a-4d82-800a-f41524766981", "c65e6eec-50c7-4ca1-b135-dfbe46e61213"];
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
        if (index < 0 || index >= 2 && source.Spawn.Turn < 1) return null;
        int target = index == 4 ? source.Spawn.Train.Rooms[0].Units.Single(unit => unit.SpawnerCardId == ids[0]).Id :
            index is 6 or 7 ? source.Spawn.Train.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Enemy && unit.Health > 0).OrderByDescending(unit => unit.Health).First().Id : 0;
        var action = new PlayCardAction(ids[index], index == 1 ? 1 : 0, playerPosition: index < 2 ? 0 : -1, targetUnitId: target);
        return BattleActionModel.PlayCard(source, action).Supported ? action : null;
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalStatusCompositionsPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original status compositions witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }) Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("OriginalStatusCompositionsIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 10 && actions.Length == 10, "Missing seven unchanged original spell plays.");
        int Stacks(CombatUnit unit, string status) => unit.Status(status)?.Stacks ?? 0;
        foreach (int i in Enumerable.Range(3, 7))
        {
            BattleTurnState before = actions[i].GetProperty("Before").Deserialize<BattleTurnState>()!, after = actions[i].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            CombatUnit a = before.Spawn.Train.Rooms[0].Units.Single(unit => unit.SpawnerCardId == ids[0]), b = after.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == a.Id);
            CardInstanceState card = before.Spawn.Train.Context!.FindCard(ids[i])!;
            CardPlayRule rule = before.PlayRules!.Cards.Single(rule => rule.DataId == Originals[i - 3]);
            Require(card.DataId == Originals[i - 3] && card.MaskDescriptor!.SourceTraits!.Count == 0 && rule.ExternalInteractions.Count == 0 &&
                before.Energy - after.Energy == rule.Cost && after.Spawn.Train.Context!.Cards.Discard.Any(token => token.InstanceId == ids[i]),
                "Original definition, cost, source traits or ordinary discard differs.");
            Require(Json(before.Spawn.Train.Rooms[1].Units) == Json(after.Spawn.Train.Rooms[1].Units), "Outside-floor unit changed.");
            Require(i >= 6 ? Json(a) == Json(b) : Json(before.Spawn.Train.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Enemy)) ==
                Json(after.Spawn.Train.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Enemy)), "Untargeted team changed.");
            if (i == 3) Require(Stacks(b, "valor") - Stacks(a, "valor") == 2 && Stacks(b, "damage shield") - Stacks(a, "damage shield") == 1,
                "Benediction must grant two Valor then one Damage Shield to present allies.");
            if (i == 4) Require(Stacks(b, "valor") - Stacks(a, "valor") == 10, "Divinity must grant ten Valor to the selected ally.");
            if (i == 5) Require(Stacks(b, "burst") - Stacks(a, "burst") == 1 && b.Attacks == a.Attacks + 1, "Full Throttle must grant one Burst and one additional attack.");
            if (i >= 6)
            {
                CombatUnit enemy = before.Spawn.Train.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Enemy && unit.Health > 0).OrderByDescending(unit => unit.Health).First();
                CombatUnit actual = after.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == enemy.Id);
                if (i == 6) Require(Stacks(actual, "rooted") - Stacks(enemy, "rooted") == 1 &&
                    after.Spawn.Train.Context.Cards.DrawModifier - before.Spawn.Train.Context.Cards.DrawModifier == 1,
                    "Ensnare must root its enemy and schedule one next-turn draw.");
                if (i == 7) Require(Stacks(actual, "spell weakness") - Stacks(enemy, "spell weakness") == 1,
                    "Energy Siphon must add one Spell Weakness to the selected enemy.");
                if (i == 8) Require(rule.Effects.Select(effect => effect.Type).SequenceEqual(["AddStatus", "Damage"]) && rule.Effects[1].Value == 1 &&
                    Stacks(actual, "unstable") - Stacks(enemy, "unstable") == 10 && Stacks(enemy, "spell weakness") == 1 && Stacks(actual, "spell weakness") == 0 &&
                    enemy.Health + Stacks(enemy, "armor") - actual.Health - Stacks(actual, "armor") == 12 &&
                    before.Spawn.Train.Rooms[0].Units.Any(unit => unit.Team == CombatTeam.Enemy && unit.Health == 1 &&
                        after.Spawn.Train.Rooms[0].Units.All(alive => alive.Id != unit.Id)),
                    "Acid Shower must consume weakness for two damage and trigger the one-health enemy's ten-point Unstable explosion.");
                if (i == 9) Require(rule.Effects.Select(effect => effect.Type).SequenceEqual(["AddStatus", "Damage"]) && rule.Effects[1].Value == 10 &&
                    Stacks(actual, "pyregel") - Stacks(enemy, "pyregel") == 10 &&
                    enemy.Health + Stacks(enemy, "armor") - actual.Health - Stacks(actual, "armor") == 20,
                    "Dragon's Breath must add ten Pyregel before damage so that the same cast receives its bonus.");
            }
        }
        Require(root.GetProperty("CardCycles").EnumerateArray().Any(row => row.GetProperty("Kind").GetString() == "Draw" &&
            row.GetProperty("Before").Deserialize<CardCycleState>()!.DrawModifier == 1 && row.GetProperty("Actual").Deserialize<CardCycleState>()!.Hand.Count == 6 &&
            row.GetProperty("Actual").Deserialize<CardCycleState>()!.DrawModifier == 0), "Ensnare's next turn must draw six and clear its bonus.");
        BonusDrawChecks.Native(root);
        RecordedBattleChecks.Native(root);
        Console.WriteLine("ORIGINAL-STATUS-COMPOSITIONS PASS: seven unchanged original spells; positive Valor/Shield/Burst, Rooted/Spell Weakness/Unstable/Pyregel, ordered damage, floor controls, six-card future draw/reset and 16 immutable full chains.");
    }
    private static string Json<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
