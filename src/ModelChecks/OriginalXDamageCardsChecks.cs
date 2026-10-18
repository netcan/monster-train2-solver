using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalXDamageCardsChecks
{
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        for (int index = 0; index < ids.Length; index++)
            if (source.Spawn.Train.Context!.Statistics!.Value(ids[index], "TimesPlayed", "ThisBattle") == 0)
            {
                if (index >= 2 && source.Spawn.Turn < 1) return null;
                int room = index == 1 ? 1 : index == 2 ? 2 : 0;
                return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == room);
            }
        return null;
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalXDamageCardsPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original X-damage witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }) Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("OriginalXDamageCardsIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 9 && actions.Length == 9, "Missing original X spells or controls.");
        BattleTurnState Before(int index) => actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int index) => actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatUnit[] Enemies(BattleTurnState state) => state.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units.Where(unit => unit.Team == CombatTeam.Enemy).ToArray();
        CombatUnit Guard(BattleTurnState state) => Enemies(state).Single(unit => unit.AssetKey == "ShieldGuardT1_BasicNonAttack");
        int Armor(CombatUnit unit) => unit.Status("armor")?.Stacks ?? 0;
        foreach (int index in new[] { 3, 4, 6, 8 })
        {
            BattleTurnState before = Before(index), after = After(index);
            string dataId = index is 3 or 6 ? "a54a1fa9-8cf3-4be2-a9c6-d0e50bb82c7d" : "126a45ce-1bde-42c1-bc69-f04ea4901655";
            CardPlayRule rule = before.PlayRules!.Cards.Single(card => card.DataId == dataId);
            CardInstanceState card = before.Spawn.Train.Context!.FindCard(ids[index])!;
            CardSourceTrait raw = card.MaskDescriptor!.SourceTraits!.Single();
            ScalingDamageTrait scaling = card.DamageScalingTraits!.Single();
            Require(rule.Cost == 0 && rule.CostType == "ConsumeRemainingEnergy" && rule.ExternalInteractions.Count == 0 &&
                raw.Type == "CardTraitScalingAddDamage" && raw.Int == (index is 3 or 6 ? 5 : 80) && raw.Int2 == 0 && raw.Int3 == 0 &&
                !raw.Bool && raw.Float == 1 && raw.Scaling && raw.TrackedValue == "PlayedCost" && raw.EntryDuration == "ThisTurn" &&
                scaling.Query.Type == raw.TrackedValue && scaling.DamagePerStat == raw.Int && scaling.Multiplier == 1 && !scaling.AddToDamage,
                "Original X trait source parameters or scaling query differs.");
            Require(after.Energy == 0 && after.Spawn.Train.Context!.FindCard(ids[index])!.LastPlayedCost == before.Energy &&
                before.Energy == (index == 6 ? 3 : index == 8 ? 2 : 0), "X payment or played-cost ownership differs.");
            Require(JsonSerializer.Serialize(card.MaskDescriptor.SourceTraits, ModelJson.Options) ==
                JsonSerializer.Serialize(after.Spawn.Train.Context.FindCard(ids[index])!.MaskDescriptor!.SourceTraits, ModelJson.Options),
                "Playing/removing Freeze lost original trait provenance.");
            foreach (CombatUnit friendly in before.Spawn.Train.Rooms.SelectMany(room => room.Units).Where(unit => unit.Team == CombatTeam.Player))
                Require(after.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == friendly.Id).Health == friendly.Health,
                    "Enemy X damage changed friendly or outside-floor health.");
        }
        foreach (int index in new[] { 3, 4 })
        {
            Require(Enemies(Before(index)).Length >= 2 && Enemies(After(index)).Length == Enemies(Before(index)).Length &&
                Enemies(Before(index)).All(enemy => Enemies(After(index)).Any(actual => actual.Id == enemy.Id && actual.Health == enemy.Health && Armor(actual) == Armor(enemy))),
                "Enemy-present zero X must leave health and armor unchanged.");
        }
        Require(Enemies(Before(6)).Length == 2 && Enemies(After(6)).Length == 1 && Guard(After(6)).Health == Guard(Before(6)).Health &&
            Armor(Guard(Before(6))) - Armor(Guard(After(6))) == 15, "Vent X=3 must hit the whole enemy room for fifteen, including lethal cleanup.");
        Require(Guard(Before(8)).Health + Armor(Guard(Before(8))) - Guard(After(8)).Health - Armor(Guard(After(8))) == 160,
            "Forever Consumed X=2 front damage/armor differs.");
        BattleTurnState retained = root.GetProperty("Turns")[0].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(ids.Skip(2).All(id => retained.Spawn.Train.Context!.Cards.Hand.Any(card => card.InstanceId == id)) &&
            Before(2).Spawn.Turn == 1 && Before(2).Energy - After(2).Energy == 3,
            "Frozen hand retention or independent Spell Sanctum energy drain differs.");
        RecordedBattleChecks.Native(root);
        Console.WriteLine("ORIGINAL-X-DAMAGE-CARDS PASS: unchanged Vent/Forever Consumed, enemy-present zero X, positive room/front damage, armor/lethal cleanup, exact source traits across Freeze and 16 immutable chains.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
