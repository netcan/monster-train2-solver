using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalOverflowCardsChecks
{
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
        if (index < 0 || index >= 4 && source.Spawn.Turn < 1) return null;
        int room = index == 1 ? 1 : 0;
        if (index >= 5 && !source.Spawn.Train.Rooms.Single(value => value.RoomIndex == room).Units.Any(unit => unit.Team == CombatTeam.Enemy && unit.Health > 0)) return null;
        int owner = index == 5 ? 0 : index == 6 ? 2 : index == 7 ? 3 : -1;
        int target = owner < 0 ? 0 : source.Spawn.Train.Rooms.SelectMany(value => value.Units).Single(unit => unit.SpawnerCardId == ids[owner]).Id;
        return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == room && play.TargetUnitId == target);
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalOverflowCardsPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original overflow witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }) Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("OriginalOverflowCardsIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 8 && actions.Length == 8, "Missing nonlethal, both positive spills or outside-floor controls.");
        for (int index = 5; index < actions.Length; index++)
        {
            BattleTurnState before = actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!, after = actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            PlayCardAction action = actions[index].GetProperty("Action").Deserialize<PlayCardAction>()!;
            CardInstanceState card = before.Spawn.Train.Context!.FindCard(action.CardInstanceId)!;
            CardPlayRule rule = before.PlayRules!.Cards.Single(value => value.DataId == card.DataId);
            CardSourceTrait[] traits = card.MaskDescriptor!.SourceTraits!.ToArray();
            Require(card.DataId == (index == 7 ? "817d1245-13e6-4a4c-9c90-26d2622a860e" : "b1129fb0-6bd0-4a9c-aab4-ef7b977c6fb2") &&
                traits.Select(trait => trait.Type).SequenceEqual(index == 7 ? ["CardTraitStrongerMagicPower", "CardTraitDamageOverflow"] : ["CardTraitDamageOverflow"]) &&
                traits.All(trait => trait.Int == (trait.Type == "CardTraitDamageOverflow" ? 2 : 0) && trait.Int2 == 0 && trait.Int3 == 0 && !trait.Bool && trait.Str == "" &&
                    trait.Float == 0 && !trait.Scaling && trait.TrackedValue == "SubtypeInDeck" && trait.EntryDuration == "ThisTurn") &&
                rule.Cost == (index == 7 ? 4 : 2) && rule.Effects.Single().Value == (index == 7 ? 150 : 20) && rule.ExternalInteractions.Count == 0 && rule.UpgradeInteractions.Count == 0 &&
                before.Energy - after.Energy == rule.Cost, "Original overflow parameters, paired trait or payment differs.");
            CombatUnit[] units = before.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units.ToArray(), enemies = units.Where(unit => unit.Team == CombatTeam.Enemy && unit.Health > 0).ToArray();
            CombatUnit primary = units.Single(unit => unit.Id == action.TargetUnitId);
            Require(primary.Team == CombatTeam.Player && enemies.Length > 0, "Overflow needs a friendly primary and positive enemy controls.");
            CombatUnit? Find(int id) => after.Spawn.Train.Rooms.SelectMany(room => room.Units).FirstOrDefault(unit => unit.Id == id);
            if (index == 5)
            {
                Require(primary.Health > 20 && primary.Health - Find(primary.Id)!.Health == 20 &&
                    enemies.All(enemy => JsonSerializer.Serialize(enemy, ModelJson.Options) == JsonSerializer.Serialize(Find(enemy.Id), ModelJson.Options)),
                    "Nonlethal original Pyreball must not spill to enemies.");
            }
            else
            {
                int spill = index == 7 ? 145 : 15;
                Require(primary.Health == 5 && (Find(primary.Id) == null || Find(primary.Id)!.Health <= 0), "Positive spill must kill the five-health friendly primary.");
                Require(enemies.All(enemy =>
                {
                    CombatUnit? actual = Find(enemy.Id);
                    int protection = enemy.Status("armor")?.Stacks ?? 0;
                    int remaining = unchecked(enemy.Health + protection - spill);
                    return remaining <= 0 ? actual == null || actual.Health <= 0 : actual != null &&
                        actual.Health == enemy.Health - Math.Max(0, spill - protection) && (actual.Status("armor")?.Stacks ?? 0) == Math.Max(0, protection - spill);
                }), "Hero-team overflow must independently hit every enemy for the original spill amount.");
                foreach (CombatUnit friendly in units.Where(unit => unit.Team == CombatTeam.Player && unit.Id != primary.Id))
                    Require(Find(friendly.Id)?.Health == friendly.Health, "Hero-team overflow leaked to a friendly sibling.");
            }
            Require(JsonSerializer.Serialize(before.Spawn.Train.Rooms.Single(room => room.RoomIndex == 1).Units, ModelJson.Options) ==
                JsonSerializer.Serialize(after.Spawn.Train.Rooms.Single(room => room.RoomIndex == 1).Units, ModelJson.Options), "Original overflow leaked outside the selected floor.");
        }
        RecordedBattleChecks.Native(root);
        Console.WriteLine("ORIGINAL-OVERFLOW-CARDS PASS: original Pyreball/Pyreclasm, nonlethal no-spill, friendly primary deaths with fifteen/145 enemy-only spill, armor, every enemy sibling, friendly/outside-floor controls and 16 immutable full chains.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
