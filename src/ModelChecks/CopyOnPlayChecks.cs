using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class CopyOnPlayChecks
{
    internal static void Native(FixtureValue root, bool natural = false)
    {
        Require(root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("ModifierScenario").GetString() == (natural ? "" : "copy-on-play") &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing settled original copy-on-play calibration.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        var created = new HashSet<int>();
        int purgedPlays = 0, plainCopies = 0, consumeCopies = 0;
        foreach (var row in root.GetProperty("Actions").EnumerateArray())
        {
            var before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            var after = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            var action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
            var context = before.Spawn.Train.Context!;
            var actual = after.Spawn.Train.Context!;
            var card = context.FindCard(action.CardInstanceId)!;
            if (CardCopyOnPlayModel.Present(card))
            {
                Require(card.Permanent.Upgrades.Any(upgrade => upgrade.DataId == CardCopyOnPlayModel.UpgradeId) &&
                    card.CanBeCopied == true && actual.NextCardId == context.NextCardId + 1,
                    "Original played upgrade did not create exactly one native identity.");
                var copy = actual.FindCard(context.NextCardId)!;
                Require(copy.DataId == card.DataId && actual.Cards.Hand.Any(item => item.InstanceId == copy.InstanceId) &&
                    copy.Temporary.Upgrades.Any(upgrade => upgrade.AssetKey == "CopyOnPlayTraitUpgrade") &&
                    !copy.Permanent.Upgrades.Concat(copy.Temporary.Upgrades).Any(upgrade => upgrade.DataId == CardCopyOnPlayModel.UpgradeId) &&
                    CardBranchMaskModel.Resolve(copy).Traits.Contains("CardTraitEphemeral") &&
                    !CardBranchMaskModel.Resolve(copy).Traits.Contains(CardCopyOnPlayModel.Trait),
                    "Native copy lost its purge upgrade, excluded original upgrade or hand routing.");
                created.Add(copy.InstanceId);
                if (CardBranchMaskModel.Resolve(card).Traits.Contains("CardTraitExhaustState")) consumeCopies++;
                else plainCopies++;
            }
            if (created.Contains(card.InstanceId))
            {
                Require(actual.NextCardId == context.NextCardId && !actual.Cards.Hand.Any(item => item.InstanceId == card.InstanceId) &&
                    actual.OtherPiles!.Single(pile => pile.Name == "Purged").Cards.Count(item => item.InstanceId == card.InstanceId) == 2,
                    "Playing an original temporary copy recursed or missed duplicate native purge references.");
                purgedPlays++;
            }
        }
        Require(natural ? consumeCopies > 0 && purgedPlays > 0 : plainCopies > 0 && consumeCopies > 0 && purgedPlays >= 2,
            "Missing required plain/Consume copy creation or played-copy purge witnesses.");
        RecordedBattleChecks.Native(root);
        Console.WriteLine($"COPY-ON-PLAY PASS natural={natural} copies={created.Count} plain={plainCopies} consume={consumeCopies} purgedPlays={purgedPlays}: original upgrade exclusion, temporary purge, complete immutable battle/RNG chains.");
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}
