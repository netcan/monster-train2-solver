using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalBasicDamageChecks
{
    private static readonly string[] Originals = { "682ef9b9-80cb-4e00-8d57-09340d6f4cc1", "fbfda8b0-63e2-47b4-a969-8377b83333ef",
        "0a97eac9-329c-41af-a27e-e6ce37efc632", "5839cfaa-4784-4ec7-99f0-3dda42b939dc", "f1dbd7d5-74fc-4507-aba1-dc1a8ec9393b",
        "3d1a2e1b-7751-4b2c-a549-77d03db715c9" };
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
        if (index < 0 || index >= 3 && source.Spawn.Turn < 1) return null;
        int target = index == 9 ? source.Spawn.Train.Rooms[0].Units.First(unit => unit.Team == CombatTeam.Enemy).Id : 0;
        var action = new PlayCardAction(ids[index], index < 3 ? index : 0, playerPosition: index < 3 ? 0 : -1, targetUnitId: target);
        return BattleActionModel.PlayCard(source, action).Supported ? action : null;
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalBasicDamagePrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original basic damage witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }) Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("OriginalBasicDamageIds").Deserialize<int[]>()!;
        var actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 11 && actions.Length == 11, "Missing six original spells or authored controls.");
        int[] costs = [3, 1, 2, 1, 1, 0], totals = [75, 5, 70, 10, 3];
        for (int i = 5; i < 11; i++)
        {
            BattleTurnState before = actions[i].GetProperty("Before").Deserialize<BattleTurnState>()!, after = actions[i].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            var card = before.Spawn.Train.Context!.FindCard(ids[i])!; var rule = before.PlayRules!.Cards.Single(rule => rule.DataId == Originals[i - 5]);
            Require(card.DataId == Originals[i - 5] && card.MaskDescriptor!.BaseTraits.Count == 0 && rule.ExternalInteractions.Count == 0 &&
                rule.Cost == costs[i - 5] && before.Energy - after.Energy == costs[i - 5], "Original identity, traits, cost or spell projection differs.");
            CombatUnit[] a = before.Spawn.Train.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Enemy).ToArray(),
                b = after.Spawn.Train.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Enemy).ToArray();
            Require(a.Length >= 2 && a.Select(unit => unit.Id).SequenceEqual(b.Select(unit => unit.Id)) && b.All(unit => unit.Health > 0),
                "Multi-target damage requires unchanged living enemy candidates.");
            int Effective(CombatUnit unit) => unit.Health + (unit.Status("armor")?.Stacks ?? 0);
            Require(a.Sum(Effective) - b.Sum(Effective) == (i == 10 ? a.Length : totals[i - 5]), "Original total health/armor damage differs.");
            Require(a.Sum(unit => unit.Health) > b.Sum(unit => unit.Health), "Original spell lacks positive HP damage.");
            if (i == 5) Require(a.Any(unit => unit.Status("armor")?.Stacks > 0) &&
                a.Sum(unit => unit.Status("armor")?.Stacks ?? 0) > b.Sum(unit => unit.Status("armor")?.Stacks ?? 0), "Missing positive armored damage control.");
            int draws = i == 5 ? 3 : i == 6 ? 5 : 0;
            UnityRng rng = before.Spawn.Train.Context!.BattleRng;
            for (int draw = 0; draw < draws; draw++) rng = rng.Next();
            Require(rng.Equals(after.Spawn.Train.Context!.BattleRng), "Original spell consumed an incorrect number of Battle RNG draws.");
            Require(Json(before.Spawn.Train.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Player)) ==
                Json(after.Spawn.Train.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Player)) &&
                Json(before.Spawn.Train.Rooms.Skip(1).Select(room => room.Units)) == Json(after.Spawn.Train.Rooms.Skip(1).Select(room => room.Units)),
                "Original enemy damage changed a friendly or another floor.");
            Require(after.Spawn.Train.Context!.Cards.Discard.Any(token => token.InstanceId == ids[i]), "Original damage spell must discard.");
        }
        RecordedBattleChecks.Native(root);
        Console.WriteLine("ORIGINAL-BASIC-DAMAGE PASS: six unchanged original spells, positive HP/armor damage, front/selected/room targets, three/five random hits with exact Battle RNG, floor controls and 16 immutable full chains.");
    }
    private static string Json<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
