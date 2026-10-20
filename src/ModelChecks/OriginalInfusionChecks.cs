using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalInfusionChecks
{
    internal static void Native(FixtureValue root, IReadOnlyCollection<string> originals)
    {
        string[] expected = ["3cf25a9b-daa1-42cd-8174-eb0f59cd590f", "b4029b77-12b3-4d88-b10b-c6c0e6b62db8", "3f0d7968-ba33-4ad7-94d7-b0f867de837a", "af13aa32-5442-408a-b491-229362c634f8"];
        if (!expected.All(originals.Contains)) return;
        int[] ids = root.GetProperty("OriginalSupportSpellsIds").Deserialize<int[]>()!.Skip(4).ToArray();
        foreach (FixtureValue row in root.GetProperty("Actions").EnumerateArray().Skip(4))
        {
            PlayCardAction action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
            BattleTurnState after = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            if (!after.OtherPiles.Single(pile => pile.Name == "DiscardBuffer").Cards.Any(token => token.InstanceId == action.CardInstanceId) ||
                after.Spawn.Train.Context!.Statistics!.Value(action.CardInstanceId, "TimesDiscarded") != 0 ||
                after.OtherPiles.Single(pile => pile.Name == "Purged").Cards.Any(token => token.InstanceId == action.CardInstanceId))
                throw new InvalidOperationException("Played Infusion/Persistent must retain its buffer reference and suppress SelfPurge/discard callbacks.");
        }
        int retained = 0;
        foreach (FixtureValue row in root.GetProperty("Turns").EnumerateArray())
        {
            BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!, after = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            if (after.Spawn.Turn <= before.Spawn.Turn) continue; // Terminal hand cleanup is separate from natural discard.
            int[] controls = before.Spawn.Train.Context!.Cards.Hand.Where(token => expected.Contains(token.DataId) && !ids.Contains(token.InstanceId)).Select(token => token.InstanceId).ToArray();
            if (controls.Length != 4) throw new InvalidOperationException("Missing independent unplayed infusion controls.");
            foreach (int id in controls)
            {
                CardInstanceState card = before.Spawn.Train.Context!.CardRegistry.Single(card => card.InstanceId == id);
                if (card.Temporary.Upgrades.Count != 0 || card.MaskDescriptor!.TemporaryTraits.Count != 0)
                    throw new InvalidOperationException("Infusion controls must retain their original traits without a Freeze shim.");
                if (!after.Spawn.Train.Context!.Cards.Hand.Any(token => token.InstanceId == id) ||
                    after.OtherPiles.Any(pile => pile.Cards.Any(token => token.InstanceId == id)) ||
                    after.Spawn.Train.Context.Statistics!.Value(id, "TimesDiscarded") != before.Spawn.Train.Context.Statistics!.Value(id, "TimesDiscarded"))
                    throw new InvalidOperationException("Infusion/Persistent must retain original null cards before discard and SelfPurge routing.");
                retained++;
            }
        }
        if (retained < 8) throw new InvalidOperationException("Missing repeated native Infusion retention controls.");
        Console.WriteLine("ORIGINAL-INFUSIONS PASS: four unchanged definitions, no Freeze shim and " + retained + " retained end-turn controls.");
    }
}
