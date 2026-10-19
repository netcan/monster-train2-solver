using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalXStatusCardsChecks
{
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        for (int index = 0; index < ids.Length; index++)
            if (source.Spawn.Train.Context!.Statistics!.Value(ids[index], "TimesPlayed", "ThisBattle") == 0)
            {
                int target = index >= 3 ? source.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == ids[0]).Id : 0;
                return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] &&
                    play.RoomIndex == (index == 2 ? 1 : 0) && (target == 0 || play.TargetUnitId == target));
            }
        return null;
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalXStatusCardsPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original X-status witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }) Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("OriginalXStatusCardsIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 10 && actions.Length == 10, "Missing original positive/zero X plays or controls.");
        BattleTurnState Before(int index) => actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int index) => actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatUnit Unit(BattleTurnState state, int owner) => state.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == ids[owner]);
        foreach (int index in new[] { 3, 4 })
        {
            BattleTurnState before = Before(index), after = After(index);
            CardPlayRule rule = before.PlayRules!.Cards.Single(card => card.DataId == "dc7b0d83-2a0c-460b-ae92-42ee144005a5");
            CardInstanceState card = before.Spawn.Train.Context!.FindCard(ids[index])!;
            CardSourceTrait[] raw = card.MaskDescriptor!.SourceTraits!.ToArray();
            ScalingStatusTrait scaling = card.StatusScalingTraits!.Single();
            Require(raw.Length == 2 && raw[0].Type == "CardTraitExhaustState" && raw[1].Type == "CardTraitScalingAddStatusEffect" &&
                raw[1].Int == 2 && raw[1].Int2 == 1 && raw[1].Int3 == 0 && !raw[1].Bool && !raw[1].Scaling && raw[1].Float == 0 &&
                raw[1].TrackedValue == "PlayedCost" && raw[1].EntryDuration == "ThisTurn" && scaling.Query.Type == "PlayedCost" &&
                scaling.StacksPerStat == 2 && scaling.Filter == 1 && !scaling.OnlyWhenSourceZero,
                "Original Consume/propagatable scaling trait pair differs.");
            Require(rule.Cost == 0 && rule.CostType == "ConsumeRemainingEnergy" && rule.ExternalInteractions.Count == 0 &&
                rule.Effects.Single().Type == "AdjustAllStatusEffects" && rule.Effects.Single().StatusAdjustmentFilter == 1 &&
                after.Energy == 0 && before.Energy == (index == 3 ? 4 : 0) &&
                after.Spawn.Train.Context!.FindCard(ids[index])!.LastPlayedCost == before.Energy,
                "Original X payment, propagation filter or played-cost ownership differs.");
            Require(Unit(after, 0).Status("buff")?.Stacks == 11 && Unit(after, 0).Status("regen")?.Stacks == 10 &&
                Unit(after, 0).Status("armor")?.Stacks == 4, "Positive two-times-X growth, zero-X control or nonpropagatable armor differs.");
            foreach (int owner in new[] { 1, 2 })
                Require(JsonSerializer.Serialize(Unit(before, owner), ModelJson.Options) == JsonSerializer.Serialize(Unit(after, owner), ModelJson.Options),
                    "Targeted X propagation changed its neighbor or outside-floor control.");
            Require(after.Spawn.Train.Context!.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(token => token.InstanceId == ids[index]) &&
                after.Spawn.Train.Context.Statistics!.Value(ids[index], "TimesExhausted", "ThisBattle") == 2,
                "Original X=4/X=0 Consume must both enter Exhausted with native statistics.");
        }
        Require(Unit(Before(3), 0).Status("buff")?.Stacks == 3 && Unit(Before(3), 0).Status("regen")?.Stacks == 2,
            "Missing positive source statuses.");
        foreach (int index in new[] { 6, 8 })
        {
            BattleTurnState refillBefore = Before(index), refillAfter = After(index);
            CardInstanceState refill = refillBefore.Spawn.Train.Context!.FindCard(ids[index])!;
            CardPlayRule refillRule = refillBefore.PlayRules!.Cards.Single(card => card.DataId == refill.DataId);
            Require(refillRule.AssetKey == "PojuXStatusEnergyRefill" && refillRule.Cost == 0 && refillRule.Effects!.Count == 1 &&
                refillRule.Effects[0].Type == "GainEnergy" && refillRule.Effects[0].Target == "Room" && refillRule.Effects[0].Value == 4 &&
                refillBefore.Energy == 0 && refillAfter.Energy == 4,
                "Synthetic energy control must restore exactly four Ember between X=0 and X=4 witnesses.");
        }
        foreach (int index in new[] { 5, 7 })
        {
            BattleTurnState before = Before(index), after = After(index);
            CardInstanceState card = before.Spawn.Train.Context!.FindCard(ids[index])!;
            CardPlayRule rule = before.PlayRules!.Cards.Single(candidate => candidate.DataId == card.DataId);
            CardSourceTrait[] raw = card.MaskDescriptor!.SourceTraits!.ToArray();
            ScalingStatusTrait scaling = card.StatusScalingTraits!.Single();
            int expected = index == 5 ? 0 : 4;
            CombatUnit target = Unit(before, 0), targetAfter = Unit(after, 0);
            Require(card.DataId == "9f4dd6b6-4a03-4e2d-9a52-8a17b1dd2c93" && rule.AssetKey == "PrismalDust" &&
                rule.Cost == 0 && rule.CostType == "ConsumeRemainingEnergy" && rule.Effects!.Count == 1 &&
                rule.Effects[0].Type == "AddStatus" && rule.Effects[0].Target == "DropTargetCharacter" &&
                rule.Effects[0].AllowEnemy && rule.Effects[0].AllowPlayer && rule.Effects[0].Statuses.Single().Id == "damage shield" &&
                rule.Effects[0].Statuses[0].Stacks == 0 && raw.Length == 2 && raw[0].Type == "CardTraitExhaustState" &&
                raw[1].Type == "CardTraitScalingAddStatusEffect" && raw[1].Int == 1 && raw[1].Int2 == 0 && raw[1].Int3 == 0 &&
                !raw[1].Bool && raw[1].Str == "" && raw[1].Float == 1 && raw[1].Scaling &&
                raw[1].TrackedValue == "PlayedCost" && raw[1].EntryDuration == "ThisTurn" &&
                raw[1].StatusEffectIds!.SequenceEqual(new[] { "damage shield" }) && scaling.Query.Type == "PlayedCost" &&
                scaling.StacksPerStat == 1 && scaling.Filter == 0 && scaling.StatusIds.SequenceEqual(new[] { "damage shield" }),
                "Original Prismal Dust definition, target or PlayedCost scaling metadata differs.");
            int shieldBefore = target.Status("damage shield")?.Stacks ?? 0;
            int shieldAfter = targetAfter.Status("damage shield")?.Stacks ?? 0;
            int lastCost = after.Spawn.Train.Context!.FindCard(ids[index])!.LastPlayedCost;
            bool targetControls = targetAfter.Health == target.Health && targetAfter.Attack == target.Attack &&
                targetAfter.Status("buff")?.Stacks == target.Status("buff")?.Stacks &&
                targetAfter.Status("regen")?.Stacks == target.Status("regen")?.Stacks &&
                targetAfter.Status("armor")?.Stacks == target.Status("armor")?.Stacks;
            bool otherUnitsUnchanged = JsonSerializer.Serialize(before.Spawn.Train.Rooms[0].Units.Where(unit => unit.Id != target.Id), ModelJson.Options) ==
                JsonSerializer.Serialize(after.Spawn.Train.Rooms[0].Units.Where(unit => unit.Id != target.Id), ModelJson.Options);
            bool otherFloorsUnchanged = JsonSerializer.Serialize(before.Spawn.Train.Rooms.Skip(1).Select(room => room.Units), ModelJson.Options) ==
                JsonSerializer.Serialize(after.Spawn.Train.Rooms.Skip(1).Select(room => room.Units), ModelJson.Options);
            bool exhausted = after.Spawn.Train.Context!.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(token => token.InstanceId == ids[index]);
            Require(before.Energy == expected && after.Energy == 0 && lastCost == expected && shieldAfter - shieldBefore == expected &&
                targetControls && exhausted && otherUnitsUnchanged && otherFloorsUnchanged,
                "Prismal Dust X payment/scaling, Consume routing, target isolation or neighboring-floor state differs.");
        }
        {
            const int index = 9;
            BattleTurnState before = Before(index), after = After(index);
            CardInstanceState card = before.Spawn.Train.Context!.FindCard(ids[index])!;
            CardPlayRule rule = before.PlayRules!.Cards.Single(candidate => candidate.DataId == card.DataId);
            CardSourceTrait[] raw = card.MaskDescriptor!.SourceTraits!.ToArray();
            ScalingStatusTrait[] scaling = card.StatusScalingTraits!.ToArray();
            const int expected = 4;
            CombatUnit target = Unit(before, 0), targetAfter = Unit(after, 0);
            Require(card.DataId == "7684a346-ac5f-41a4-8c88-1691784a185e" && rule.AssetKey == "SpikeOfTheHellhorned" &&
                rule.Cost == 0 && rule.CostType == "ConsumeRemainingEnergy" && rule.Effects!.Count == 2 &&
                rule.Effects[0].Type == "AddStatus" && rule.Effects[0].Target == "DropTargetCharacter" &&
                rule.Effects[0].Statuses.Single().Id == "buff" && rule.Effects[0].Statuses[0].Stacks == 0 &&
                rule.Effects[1].Type == "AddStatus" && rule.Effects[1].Target == "LastTargetedCharacters" &&
                rule.Effects[1].Statuses.Single().Id == "armor" && rule.Effects[1].Statuses[0].Stacks == 0 &&
                raw.Length == 3 && raw[0].Type == "CardTraitScalingAddStatusEffect" && raw[1].Type == "CardTraitScalingAddStatusEffect" &&
                raw[2].Type == "CardTraitExhaustState" && raw.Take(2).All(trait => trait.Int == 4 && trait.Int2 == 0 && trait.Int3 == 0 &&
                    !trait.Bool && trait.Str == "" && trait.Float == 1 && trait.Scaling && trait.TrackedValue == "PlayedCost" &&
                    trait.EntryDuration == "ThisBattle") && raw[0].StatusEffectIds!.SequenceEqual(new[] { "buff" }) &&
                raw[1].StatusEffectIds!.SequenceEqual(new[] { "armor" }) && scaling.Length == 2 &&
                scaling.All(trait => trait.Query.Type == "PlayedCost" && trait.StacksPerStat == 4 && trait.Filter == 0) &&
                scaling[0].StatusIds.SequenceEqual(new[] { "buff" }) && scaling[1].StatusIds.SequenceEqual(new[] { "armor" }),
                "Original Spike of the Hellhorned effects or paired X-status source traits differ.");
            int lastCost = after.Spawn.Train.Context!.FindCard(ids[index])!.LastPlayedCost;
            int rageDelta = (targetAfter.Status("buff")?.Stacks ?? 0) - (target.Status("buff")?.Stacks ?? 0);
            int armorDelta = (targetAfter.Status("armor")?.Stacks ?? 0) - (target.Status("armor")?.Stacks ?? 0);
            int attackDelta = targetAfter.Attack - target.Attack;
            bool targetHealthAndStatuses = targetAfter.Health == target.Health &&
                targetAfter.Status("regen")?.Stacks == target.Status("regen")?.Stacks &&
                targetAfter.Status("damage shield")?.Stacks == target.Status("damage shield")?.Stacks;
            bool otherUnitsUnchanged = JsonSerializer.Serialize(before.Spawn.Train.Rooms[0].Units.Where(unit => unit.Id != target.Id), ModelJson.Options) ==
                JsonSerializer.Serialize(after.Spawn.Train.Rooms[0].Units.Where(unit => unit.Id != target.Id), ModelJson.Options);
            bool otherFloorsUnchanged = JsonSerializer.Serialize(before.Spawn.Train.Rooms.Skip(1).Select(room => room.Units), ModelJson.Options) ==
                JsonSerializer.Serialize(after.Spawn.Train.Rooms.Skip(1).Select(room => room.Units), ModelJson.Options);
            bool exhausted = after.Spawn.Train.Context.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(token => token.InstanceId == ids[index]);
            Require(before.Energy == expected && after.Energy == 0 && lastCost == expected && rageDelta == expected * 4 && armorDelta == expected * 4 &&
                attackDelta == rageDelta * 2 &&
                targetHealthAndStatuses && exhausted && otherUnitsUnchanged && otherFloorsUnchanged,
                "Spike of the Hellhorned X=4 payment/scaling, Consume routing, target isolation or neighboring-floor state differs.");
        }
        RecordedBattleChecks.Native(root);
        Console.WriteLine("ORIGINAL-X-STATUS-CARDS PASS: unchanged Spike of the Legion and Prismal Dust at X=4/0 plus Spike of the Hellhorned at X=4, paired traits, target/floor controls and Consume paths.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
