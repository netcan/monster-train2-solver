using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalRoomStatusDamageChecks
{
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        for (int index = 0; index < ids.Length; index++)
            if (source.Spawn.Train.Context!.Statistics!.Value(ids[index], "TimesPlayed", "ThisBattle") == 0)
            {
                if (index >= 5 && source.Spawn.Turn < 1) return null;
                int owner = index is 3 or 4 ? 2 : index is 8 or 10 ? 0 : index is 9 or 11 ? 1 : -1;
                int target = owner >= 0 ? source.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == ids[owner]).Id : 0;
                return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] &&
                    play.RoomIndex == (index is 2 or 3 or 4 ? 1 : 0) && (target == 0 || play.TargetUnitId == target));
            }
        return null;
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalRoomStatusDamagePrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original room-status damage witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }) Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("OriginalRoomStatusDamageIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 14 && actions.Length == 14, "Missing zero/positive room-status damage plays.");
        BattleTurnState Before(int index) => actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int index) => actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatUnit Unit(BattleTurnState state, int owner) => state.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == ids[owner]);
        foreach (int index in new[] { 6, 7, 12, 13 })
        {
            BattleTurnState before = Before(index), after = After(index);
            bool ram = index is 6 or 12; string status = ram ? "armor" : "spikes"; int scalar = ram ? 4 : 10;
            CardInstanceState card = before.Spawn.Train.Context!.FindCard(ids[index])!;
            CardSourceTrait raw = card.MaskDescriptor!.SourceTraits!.Single(); ScalingDamageTrait scaling = card.DamageScalingTraits!.Single();
            Require(raw.Type == "CardTraitScalingAddDamage" && raw.Int == scalar && raw.Int2 == 0 && raw.Int3 == 0 && !raw.Bool &&
                raw.Str == "" && raw.Float == 1 && raw.Scaling && raw.TrackedValue == "StatusEffectCountInTargetRoom" && raw.EntryDuration == "ThisTurn" &&
                raw.StatusQueryTeam == "Monsters" && raw.StatusQueryStatuses!.SequenceEqual(new[] { status }) &&
                scaling.Query.Type == raw.TrackedValue && scaling.Query.TargetTeam == "Monsters" && scaling.Query.StatusIds!.SequenceEqual(new[] { status }) &&
                scaling.DamagePerStat == scalar && scaling.Multiplier == 1 && !scaling.AddToDamage, "Native room-status query parameters differ.");
            Require(before.Energy - after.Energy == (ram ? 3 : 2) && after.Spawn.Train.Context!.FindCard(ids[index])!.LastPlayedCost == (ram ? 3 : 2),
                "Original fixed cost changed while scaling damage.");
            CombatUnit[] enemies = before.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units.Where(unit => unit.Team == CombatTeam.Enemy).ToArray();
            Require(enemies.Length == 2 && enemies.Any(unit => unit.Status("spikes")?.Stacks == 7) && (!ram || enemies.Any(unit => unit.Status("armor")?.Stacks > 0)),
                "Missing positive enemy-team exclusion controls.");
            int count = index < 12 ? 0 : ram ? 40 : 20;
            Require(new[] { 0, 1 }.Sum(owner => Unit(before, owner).Status(status)?.Stacks ?? 0) == count &&
                Unit(before, 2).Status("armor")?.Stacks == 20 && Unit(before, 2).Status("spikes")?.Stacks == 10,
                "Missing zero/two-contributor or positive outside-floor controls.");
            int damage = 0;
            foreach (CombatUnit enemy in enemies)
            {
                CombatUnit current = after.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == enemy.Id);
                damage += enemy.Health + (enemy.Status("armor")?.Stacks ?? 0) - current.Health - (current.Status("armor")?.Stacks ?? 0);
            }
            Require(damage == count * scalar, "Damage did not use the selected floor's live friendly status sum.");
            foreach (int owner in new[] { 0, 1, 2 })
                Require(JsonSerializer.Serialize(Unit(before, owner), ModelJson.Options) == JsonSerializer.Serialize(Unit(after, owner), ModelJson.Options),
                    "Room-status damage changed a friendly contributor or outside-floor control.");
            // These inputs cannot be guessed from an old root snapshot or an absent filter.
            CombatContext context = before.Spawn.Train.Context!; RoomCombatState room = before.Spawn.Train.Rooms.Single(value => value.RoomIndex == 0);
            Require(!StatisticQueryModel.Evaluate(context, scaling.Query, ids[index]).Supported &&
                !StatisticQueryModel.Evaluate(context, scaling.Query, ids[index], selectedRoom: before.Spawn.Train.Rooms.Single(value => value.RoomIndex == 1)).Supported &&
                !StatisticQueryModel.Evaluate(context, new CardStatisticQuery("StatusEffectCountInTargetRoom", targetTeam: "Monsters"), ids[index], selectedRoom: room).Supported,
                "Room-status query accepted absent/wrong-floor live units or native status filter.");
        }
        Require(After(12).Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units.Any(unit => unit.Team == CombatTeam.Enemy && unit.Health < 400),
            "Missing positive armor-scaled health damage.");
        RecordedBattleChecks.Native(root);
        Console.WriteLine("ORIGINAL-ROOM-STATUS-DAMAGE PASS: unchanged Battering Ram/Bramble Lash at zero/positive friendly armor/spikes, live two-unit sums, enemy/outside-floor controls, exact source/query masks, missing-input rejection and 16 immutable chains.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
