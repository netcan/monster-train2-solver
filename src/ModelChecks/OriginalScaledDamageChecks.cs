using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalScaledDamageChecks
{
    private static readonly int[] Indices = [9, 10, 11, 12, 13, 15];
    private static readonly string[] Originals = ["97d18bbe-6fa9-45a4-90a9-5a1d0ea24ca3", "f6434819-3b62-44e4-a82c-e344dc4c1aa0",
        "e8e593f4-7e2d-4791-a7d4-8a6155f1a50f", "f2bed1d3-6b52-43d5-8b97-bfe5c90dce53", "acc7bdd1-8f5b-4738-8c4a-4542792e7388", "97d18bbe-6fa9-45a4-90a9-5a1d0ea24ca3"];
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
        if (index < 0 || index >= 6 && source.Spawn.Turn < 1) return null;
        int target = index == 10 ? source.Spawn.Train.Rooms[0].Units.Single(unit => unit.SpawnerCardId == ids[3]).Id : 0;
        var action = new PlayCardAction(ids[index], index is 1 or 2 ? index : 0,
            playerPosition: index < 3 ? 0 : index < 6 ? index - 2 : -1, targetUnitId: target);
        return BattleActionModel.PlayCard(source, action).Supported ? action : null;
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalScaledDamagePrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original scaled damage witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }) Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("OriginalScaledDamageIds").Deserialize<int[]>()!;
        var actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 17 && actions.Length == 17, "Missing original scaling spells or authored positive/zero controls.");
        int consumed = 0;
        foreach (int i in Indices)
        {
            BattleTurnState before = actions[i].GetProperty("Before").Deserialize<BattleTurnState>()!, after = actions[i].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            var context = before.Spawn.Train.Context!; var changed = after.Spawn.Train.Context!;
            var card = context.FindCard(ids[i])!; string original = Originals[Array.IndexOf(Indices, i)];
            var rule = before.PlayRules!.Cards.Single(rule => rule.DataId == original);
            Require(card.DataId == original && rule.ExternalInteractions.Count == 0 && before.Energy - after.Energy == rule.Cost,
                "Original scaling spell identity, cost or projection differs.");
            CombatUnit[] a = before.Spawn.Train.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Enemy).ToArray(),
                b = after.Spawn.Train.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Enemy).ToArray();
            Require(a.Length == 2 && a.Select(unit => unit.Id).SequenceEqual(b.Select(unit => unit.Id)) && b.All(unit => unit.Health > 0), "Scaling controls require two surviving enemies.");
            int Effective(CombatUnit unit) => unit.Health + (unit.Status("armor")?.Stacks ?? 0);
            int expected = i switch { 9 => 0, 10 => 255, 11 => 120, 12 => 48, 13 => 60, 15 => 12, _ => throw new InvalidOperationException() };
            Require(a.Sum(Effective) - b.Sum(Effective) == expected, "Exact resource/HP/attack/death/deck-type scaled damage differs at action " + i);
            if (i == 10)
            {
                CombatUnit support = before.Spawn.Train.Rooms[0].Units.Single(unit => unit.SpawnerCardId == ids[3]);
                Require(support.Health == 80 && support.Status("armor")?.Stacks == 5 && rule.Effects![0].DamagePerHpIncludeArmor,
                    "Deadly Plunge must use three times 80 HP plus five armor.");
            }
            if (i == 12) Require(context.RoomCapacities!.Single(room => room.RoomIndex == 0).Corruption == 3, "Ancient Resonance requires positive three-Corruption scaling.");
            if (i == 13) Require(card.MaskDescriptor!.SourceTraits!.Single().QueryCardType == "Spell" &&
                rule.Effects!.Count == 3 && context.CardRegistry!.Count(state => state.MaskDescriptor?.Definition.CardType == "Spell" && ids.Contains(state.InstanceId)) == 11,
                "Ancient Synergy requires captured Spell query metadata and eleven created spells; the resolving source leaves the native deck query.");
            if (i == 15) Require(before.Spawn.Train.Rooms[0].Units.All(unit => unit.SpawnerCardId != ids[4] && unit.SpawnerCardId != ids[5]),
                "Positive death-count damage must follow both authored allied deaths.");
            Require(Json(before.Spawn.Train.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Player)) ==
                Json(after.Spawn.Train.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Player)) &&
                Json(before.Spawn.Train.Rooms.Skip(1).Select(room => room.Units)) == Json(after.Spawn.Train.Rooms.Skip(1).Select(room => room.Units)),
                "Original scaled spell changed a friendly or neighboring floor.");
            bool consume = CardExhaustModel.HasConsume(card); if (consume) consumed++;
            Require(consume ? changed.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(token => token.InstanceId == ids[i]) :
                changed.Cards.Discard.Any(token => token.InstanceId == ids[i]), "Original scaled spell Consume/discard route differs.");
            Require(context.BattleRng.Equals(changed.BattleRng), "Fixed scaled spells must not consume Battle RNG.");
        }
        BattleTurnState mementoBefore = actions[16].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState mementoAfter = actions[16].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatContext mementoContext = mementoBefore.Spawn.Train.Context!;
        CombatContext mementoChanged = mementoAfter.Spawn.Train.Context!;
        CardInstanceState memento = mementoContext.FindCard(ids[16])!;
        CardPlayRule mementoRule = mementoBefore.PlayRules!.Cards.Single(rule => rule.DataId == memento.DataId);
        ScalingDamageTrait mementoScaling = memento.DamageScalingTraits!.Single();
        CardSourceTrait mementoSource = memento.MaskDescriptor!.SourceTraits!.Single();
        CombatUnit[] mementoEnemies = mementoBefore.Spawn.Train.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Enemy).ToArray();
        CombatUnit frontBefore = mementoEnemies.First();
        CombatUnit frontAfter = mementoAfter.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == frontBefore.Id);
        DamageScalingResult mementoExpected = DamageScalingModel.Apply(mementoContext, ids[16], ids[16], mementoRule.Effects!.Single().Value);
        Require(memento.DataId == "c04c359c-9991-42bc-9040-bff7d3441248" && mementoRule.AssetKey == "MementoMori" &&
            mementoRule.Cost == 3 && mementoRule.CostType == "Default" && mementoRule.Destination == "Discard" &&
            mementoRule.Effects.Single().Type == "Damage" && mementoRule.Effects[0].Target == "FrontInRoom" &&
            mementoRule.Effects[0].Value == 0 && mementoRule.Effects[0].AllowEnemy && !mementoRule.Effects[0].AllowPlayer &&
            mementoRule.ExternalInteractions.Count == 0 && mementoRule.UpgradeInteractions!.Count == 0 &&
            mementoSource.Type == "CardTraitScalingAddDamage" && mementoSource.Int == 10 && mementoSource.Int2 == 0 && mementoSource.Int3 == 0 &&
            !mementoSource.Bool && mementoSource.Str == "" && mementoSource.Float == 1 && mementoSource.Scaling &&
            mementoSource.TrackedValue == "AnyMonsterDeath" && mementoSource.EntryDuration == "ThisBattle" &&
            mementoScaling.Query.Type == "AnyMonsterDeath" && mementoScaling.Query.Duration == "ThisBattle" &&
            mementoScaling.DamagePerStat == 10 && mementoScaling.Multiplier == 1 && !mementoScaling.AddToDamage &&
            mementoContext.Statistics!.MonstersDeadThisBattle == 2 && mementoContext.Statistics.Value(ids[16], "AnyMonsterDeath", "ThisBattle") == 2 &&
            mementoExpected.Supported && mementoExpected.Damage == 20 && frontBefore.Health - frontAfter.Health == 20 &&
            mementoBefore.Energy - mementoAfter.Energy == 3 && mementoChanged.Cards.Discard.Any(card => card.InstanceId == ids[16]) &&
            mementoContext.BattleRng.Equals(mementoChanged.BattleRng) &&
            Json(mementoBefore.Spawn.Train.Rooms[0].Units.Where(unit => unit.Id != frontBefore.Id)) ==
                Json(mementoAfter.Spawn.Train.Rooms[0].Units.Where(unit => unit.Id != frontBefore.Id)) &&
            Json(mementoBefore.Spawn.Train.Rooms.Skip(1).Select(room => room.Units)) == Json(mementoAfter.Spawn.Train.Rooms.Skip(1).Select(room => room.Units)),
            "Original Memento Mori AnyMonsterDeath scaling, target damage, payment or discard differs.");
        Require(consumed == 1, "Deadly Plunge must be the only original Consume spell in this witness.");
        RecordedBattleChecks.Native(root);
        Console.WriteLine("ORIGINAL-SCALED-DAMAGE PASS: six unchanged originals/seven plays, exact armor-inclusive HP and front attack damage, three Corruption, ten runtime Spell members after source removal, zero/two allied deaths, Memento Mori +20 from AnyMonsterDeath and 17 immutable full chains.");
    }
    private static string Json<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
