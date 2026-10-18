using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalEffectApplicationsChecks
{
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
        if (index < 0 || source.Spawn.Turn < 1) return null;
        return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == (index == 1 ? 1 : 0));
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalEffectApplicationsPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original Moonbeam witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }) Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("OriginalEffectApplicationsIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray(), counts = root.GetProperty("OriginalEffectApplicationsCounts").EnumerateArray().ToArray(),
            applications = root.GetProperty("OriginalEffectApplicationsSamples").EnumerateArray().ToArray();
        Require(ids.Length == 8 && actions.Length == 8 && counts.Select(row => row.GetProperty("ActualCount").GetInt32()).SequenceEqual([0, 1, 3]) &&
            applications.Length == 4, "Missing zero/one/three applications or unexpected native damage invocation count.");
        foreach (FixtureValue row in counts)
        {
            CombatContext context = row.GetProperty("Context").Deserialize<CombatContext>()!;
            int id = row.GetProperty("CardId").GetInt32(), count = row.GetProperty("ActualCount").GetInt32();
            CardInstanceState card = context.FindCard(id)!; CardMaskDescriptor descriptor = card.MaskDescriptor!;
            CardSourceTrait source = descriptor.SourceTraits!.Single(trait => trait.Type == CardEffectApplicationsModel.Trait);
            var effect = new CardActionEffect("Damage", "FrontInRoom", 20, true, false, Array.Empty<CombatStatus>());
            string parent = JsonSerializer.Serialize(context, ModelJson.Options);
            StatisticQueryResult actual = CardEffectApplicationsModel.Count(context, id, effect);
            Require(actual.Supported && actual.Value == count && row.GetProperty("DirectCount").GetInt32() == count && row.GetProperty("StateUnchanged").GetBoolean() &&
                context.Statistics!.PlayedCosts.Single(cost => cost.CardId == id).Cost == count && source.Int == 1 && source.Int2 == 0 && source.Int3 == 0 &&
                !source.Bool && source.Str == "" && source.Float == 0 && !source.Scaling && source.TrackedValue == "PlayedCost" && source.EntryDuration == "ThisTurn",
                "Native count, pure trait query, paid cost or exact source parameters differ.");
            CardInstanceState WithSource(IReadOnlyList<CardSourceTrait>? sources) => card.WithMaskDescriptor(new CardMaskDescriptor(descriptor.Definition, descriptor.BaseCost,
                descriptor.BaseTraits, descriptor.AuthoredCastEffects, descriptor.Purified, descriptor.PermanentGraft, descriptor.TemporaryTraits, descriptor.PermanentReplacements, sources));
            CardSourceTrait changed = new(source.Type, source.Int, source.Int2, source.Int3, source.Bool, source.Str, source.Float, source.Scaling, source.TrackedValue, "ThisBattle");
            Require(!CardEffectApplicationsModel.Count(context.WithCard(WithSource(null)), id, effect).Supported &&
                !CardEffectApplicationsModel.Count(context.WithCard(WithSource([changed])), id, effect).Supported &&
                !CardEffectApplicationsModel.Count(context, id, new CardActionEffect("Damage", "Room", 20, true, false, Array.Empty<CombatStatus>())).Supported,
                "Missing source, changed query duration or uncalibrated targeting must remain guarded.");
            Require(parent == JsonSerializer.Serialize(context, ModelJson.Options), "Application query or guard mutated its parent.");
        }
        foreach (int index in new[] { 3, 5, 7 })
        {
            BattleTurnState before = actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!, after = actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            CardInstanceState card = before.Spawn.Train.Context!.FindCard(ids[index])!;
            CardPlayRule rule = before.PlayRules!.Cards.Single(item => item.DataId == card.DataId);
            int paid = index == 3 ? 0 : index == 5 ? 1 : 3;
            Require(card.DataId == "8916c550-474d-49db-89bf-75bacdc95db4" && rule.Cost == 0 && rule.CostType == "ConsumeRemainingEnergy" &&
                rule.ExternalInteractions.Count == 0 && rule.Effects.Count == 1 && rule.Effects[0].Type == "Damage" && rule.Effects[0].Target == "FrontInRoom" &&
                rule.Effects[0].Value == 20 && before.Energy == paid && after.Energy == 0 &&
                before.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units.Any(unit => unit.Team == CombatTeam.Enemy) &&
                after.Spawn.Train.Context!.Cards.Discard.Any(token => token.InstanceId == ids[index]),
                "Original identity, fixed front damage, X payment, enemy-present zero or discard routing differs.");
            Require(JsonSerializer.Serialize(before.Spawn.Train.Rooms.Where(room => room.RoomIndex != 0).SelectMany(room => room.Units), ModelJson.Options) ==
                JsonSerializer.Serialize(after.Spawn.Train.Rooms.Where(room => room.RoomIndex != 0).SelectMany(room => room.Units), ModelJson.Options), "Repeat damage changed another floor.");
            Require(JsonSerializer.Serialize(before.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units.Where(unit => unit.Team == CombatTeam.Player), ModelJson.Options) ==
                JsonSerializer.Serialize(after.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units.Where(unit => unit.Team == CombatTeam.Player), ModelJson.Options), "Repeat damage changed a friendly unit.");
            if (paid == 0) Require(JsonSerializer.Serialize(before.Spawn.Train.Rooms.SelectMany(room => room.Units), ModelJson.Options) ==
                JsonSerializer.Serialize(after.Spawn.Train.Rooms.SelectMany(room => room.Units), ModelJson.Options), "Zero X must apply zero hits.");
            if (paid == 1)
            {
                CombatUnit guard = before.Spawn.Train.Rooms[0].Units.Single(unit => unit.AssetKey == "ShieldGuardT1_BasicNonAttack");
                CombatUnit changed = after.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == guard.Id);
                Require(guard.Health == 15 && changed.Health == 15 && guard.Status("armor")!.Stacks == 25 && changed.Status("armor")!.Stacks == 5, "Single application armor control differs.");
            }
            if (paid == 3) Require(after.Spawn.Train.Rooms[0].Units.All(unit => unit.Team != CombatTeam.Enemy), "Three applications must kill the front, retarget the mage and tolerate an empty tail.");
        }
        int[][] targets = applications.Select(row => row.GetProperty("Targets").Deserialize<int[]>()!).ToArray();
        Require(targets.Take(3).All(target => target.Length == 1) && targets[0][0] == targets[1][0] && targets[1][0] != targets[2][0] && targets[3].Length == 0 &&
            applications.All(row => row.GetProperty("Completed").GetBoolean()) && applications[0].GetProperty("CardId").GetInt32() == ids[5] &&
            applications.Skip(1).All(row => row.GetProperty("CardId").GetInt32() == ids[7]), "Actual native applications must retarget after lethal damage and preserve the empty application.");
        TrainCombatState emptyBefore = applications[3].GetProperty("Before").Deserialize<TrainCombatState>()!, emptyAfter = applications[3].GetProperty("After").Deserialize<TrainCombatState>()!;
        Require(JsonSerializer.Serialize(emptyBefore, ModelJson.Options) == JsonSerializer.Serialize(emptyAfter, ModelJson.Options), "Empty native damage application must leave the complete train unchanged.");
        RecordedBattleChecks.Native(root);
        Console.WriteLine("ORIGINAL-EFFECT-APPLICATIONS PASS: original Moonbeam, X zero/one/three, pure native count queries, zero hits with enemies present, armor, lethal front retarget, empty tail, floor/team controls, source guards and 16 immutable chains.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
