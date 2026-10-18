using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalMonsterHandCostsChecks
{
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
        return index < 0 ? null : BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == (index == 3 ? 1 : 0));
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalMonsterHandCostsPrepared").GetBoolean() && root.GetProperty("OriginalMonsterHandCostsQueriesUnchanged").GetBoolean() &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original Monster cost witness or query restoration.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }) Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("OriginalMonsterHandCostsIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray(), samples = root.GetProperty("OriginalMonsterHandCostsSamples").EnumerateArray().ToArray(),
            deployment = root.GetProperty("OriginalMonsterHandCostsDeploymentQueries").EnumerateArray().ToArray();
        Require(ids.Length == 6 && actions.Length == 6 && samples.Length == 7 && deployment.Length == 8, "Missing native Monster cost/filter/eligibility controls.");
        int[] payments = [2, 1, 1, 1, 1, 0];
        for (int index = 0; index < actions.Length; index++)
        {
            BattleTurnState before = actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!, after = actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            Require(actions[index].GetProperty("Action").GetProperty("CardInstanceId").GetInt32() == ids[index] && before.Energy - after.Energy == payments[index] &&
                after.Spawn.Train.Context!.FindCard(ids[index])!.LastPlayedCost == payments[index], "Native payment or played-cost statistic differs at " + index);
        }
        foreach (int index in new[] { 2, 4 })
        {
            CardInstanceState card = actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!.Spawn.Train.Context!.FindCard(ids[index])!;
            CardSourceTrait[] traits = card.MaskDescriptor!.SourceTraits!.ToArray();
            Require(card.DataId == "a27a19b7-9c21-48c3-85b7-5e4b643740be" && traits.Select(trait => trait.Type).SequenceEqual([
                CardHandCostModel.Trait, "CardTraitUnpurgable", "CardTraitDrawInDeploymentPhase"]) && traits[0].Int == 1 &&
                traits[0].HandCostTargetType == "Monster" && traits[0].TrackedValue == "AnyCharacter" &&
                traits.Skip(1).All(trait => trait.Int == 0 && trait.TrackedValue == "SubtypeInDeck") &&
                traits.All(trait => trait.Int2 == 0 && trait.Int3 == 0 && !trait.Bool && trait.Str == "" && trait.Float == 0 && !trait.Scaling && trait.EntryDuration == "ThisTurn") &&
                traits[2].DeploymentDrawFlag == false, "Original Titan Corruption traits or raw deployment provenance differ.");
            Require(actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!.Spawn.Train.Context!.Cards.Discard.Any(value => value.InstanceId == ids[index]),
                "Played Titan Corruption must leave hand and discard.");
        }
        int count = 0;
        foreach (FixtureValue sample in samples)
        {
            CombatContext context = sample.GetProperty("Context").Deserialize<CombatContext>()!;
            string parent = JsonSerializer.Serialize(context, ModelJson.Options);
            foreach (FixtureValue row in sample.GetProperty("Costs").EnumerateArray())
            {
                CardInstanceState card = context.FindCard(row.GetProperty("CardId").GetInt32())!;
                Require(CardHandCostModel.Resolve(context, card, card.MaskDescriptor!.BaseCost, card.MaskDescriptor.Definition.CardType) == row.GetProperty("Cost").GetInt32(),
                    "Native Monster-only hand cost query differs.");
                count++;
            }
            Require(parent == JsonSerializer.Serialize(context, ModelJson.Options), "Cost query mutated its immutable parent.");
        }
        int Cost(int sample, int id) => samples[sample].GetProperty("Costs").EnumerateArray().Single(row => row.GetProperty("CardId").GetInt32() == id).GetProperty("Cost").GetInt32();
        Require(count == 56 && Cost(0, ids[5]) == 2 && Cost(3, ids[5]) == 1 && Cost(5, ids[5]) == 0 &&
            Cost(0, ids[1]) == 1 && Cost(0, ids[2]) == 1 && Cost(0, ids[4]) == 1, "Stacking/removal or Blight exclusions differ.");
        CombatContext initial = samples[0].GetProperty("Context").Deserialize<CombatContext>()!;
        foreach (FixtureValue row in deployment)
        {
            int id = row.GetProperty("CardId").GetInt32(); CardInstanceState card = initial.FindCard(id)!;
            bool expected = id == ids[2] || id == ids[4];
            Require(row.GetProperty("Deployable").GetBoolean() == expected && card.Permanent.Upgrades.Count == 0 &&
                card.MaskDescriptor!.BaseTraits.Any(trait => trait.RuntimeType == "CardTraitDrawInDeploymentPhase") == expected,
                "Native deployment eligibility or unupgraded source control differs.");
            if (card.MaskDescriptor.Definition.CardType is "Spell" or "TrainRoomAttachment")
                Require(Enumerable.Range(0, samples.Length).All(index => Cost(index, id) == card.MaskDescriptor.BaseCost), "Monster-only surcharge leaked to Spell/room controls.");
        }
        RecordedBattleChecks.Native(root);
        Console.WriteLine("ORIGINAL-MONSTER-HAND-COSTS PASS: two original Titan Corruptions, Monster-only stacking/removal, Spell/room/Blight exclusions, 56 native costs, eight restored deployment eligibility queries and 16 immutable battle chains. Initial deployment draw ordering is outside this witness.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
