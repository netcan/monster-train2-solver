using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class StrongerCorruptionCopyChecks
{
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, UndyingCopyOnPlayChecks.Reason);
        Require(rows.Length == 1 && source.GetProperty("Request").GetProperty("Prefix").GetArrayLength() == 0,
            "Expected the original deployment rejection.");
        BattleTurnState before = rows[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        CardInstanceState card = before.Spawn.Train.Context!.FindCard(14)!;
        Require(card.DataId == CardCopyOnPlayModel.StrongerCorruptionSpellId &&
            CardCopyOnPlayModel.Present(card) && CardCopyOnPlayModel.Validate(card) == null &&
            before.Spawn.Train.Context.Cards.Draw.Any(token => token.InstanceId == card.InstanceId),
            "Missing the native stronger-magic/corruption spell with merchant copy upgrade.");
        foreach (string control in new[] { "magic", "corruption", "runtime", "card", "XCost", "status" })
        {
            var json = JsonSerializer.SerializeToNode(card, ModelJson.Options)!;
            var descriptor = json["MaskDescriptor"]!;
            if (control == "magic") descriptor["SourceTraits"]![0]!["Int"] = 6;
            else if (control == "corruption") descriptor["SourceTraits"]![1]!["Int"] = 3;
            else if (control == "runtime") descriptor["BaseTraits"]![0]!["ParamInt"] = 6;
            else if (control == "card") descriptor["Definition"]!["DataId"] = "uncalibrated-card";
            else if (control == "XCost") descriptor["Definition"]!["XCost"] = true;
            else descriptor["SourceTraits"]![0]!["StatusEffectIds"] = JsonSerializer.SerializeToNode(new[] { "poison" });
            Require(CardCopyOnPlayModel.Validate(json.Deserialize<CardInstanceState>(ModelJson.Options)!) == UndyingCopyOnPlayChecks.Reason,
                "Uncalibrated source signature accepted: " + control);
        }
        if (replay is FixtureValue second)
        {
            NativeBranchObservationChecks.Replay(second, UndyingCopyOnPlayChecks.Reason);
            NativeBranchObservationChecks.Pair(source, second);
        }
        Console.WriteLine("STRONGER-CORRUPTION-COPY PASS: native deployment/full state/RNG; exact5x magic/corruption2 source, six rejection controls; spell-copy execution requires separate native evidence; independentPair=" + replay.HasValue);
    }

    internal static void Effects(FixtureValue fixture)
    {
        int originals = 0, copies = 0;
        var created = new HashSet<int>();
        foreach (FixtureValue row in fixture.GetProperty("Actions").EnumerateArray())
        {
            BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            BattleTurnState actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            PlayCardAction action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
            CombatContext context = before.Spawn.Train.Context!, after = actual.Spawn.Train.Context!;
            CardInstanceState card = context.FindCard(action.CardInstanceId)!;
            if (card.DataId != CardCopyOnPlayModel.StrongerCorruptionSpellId) continue;
            Require(card.MaskDescriptor!.SourceTraits!.Select(trait => (trait.Type, trait.Int)).SequenceEqual(new[] {
                ("CardTraitStrongerMagicPower", 5), ("CardTraitCorruptRestricted", 2) }), "Authored spell traits changed.");
            if (CardCopyOnPlayModel.Present(card))
            {
                originals++;
                CardInstanceState copy = after.FindCard(context.NextCardId)!;
                Require(after.NextCardId == context.NextCardId + 1 && copy.DataId == card.DataId &&
                    after.Cards.Hand.Any(token => token.InstanceId == copy.InstanceId) && !CardCopyOnPlayModel.Present(copy) &&
                    copy.MaskDescriptor!.BaseTraits.Take(2).Select(trait => (trait.RuntimeType, trait.ParamInt)).SequenceEqual(new[] {
                        ("CardTraitStrongerMagicPower", 5), ("CardTraitCorruptRestricted", 2) }) &&
                    copy.Permanent.Upgrades.Any(upgrade => upgrade.AssetKey == "ChaosScepter" &&
                        upgrade.Stats.Damage == 100 && upgrade.CloneDamageBase == 20) &&
                    copy.Temporary.Upgrades.Any(upgrade => upgrade.AssetKey == "CopyOnPlayTraitUpgrade") &&
                    after.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(token => token.InstanceId == card.InstanceId),
                    "Copy lost its source traits,20 magic upgrade,Consume/hand routing or excluded-copy upgrade.");
                created.Add(copy.InstanceId);
            }
            else if (created.Contains(card.InstanceId))
            {
                copies++;
                Require(after.NextCardId == context.NextCardId &&
                    after.OtherPiles!.Single(pile => pile.Name == "Purged").Cards.Count(token => token.InstanceId == card.InstanceId) == 2,
                    "Played ephemeral copy recursed or missed native duplicate purge references.");
            }
        }
        Require(originals == 1 && copies == 1, "Missing the original and its played copy in this native route.");
        Console.WriteLine("STRONGER-CORRUPTION-COPY EFFECTS PASS: original/copy retain5x magic,corruption2 and20 magic upgrade; one new identity,Consume,excluded-copy upgrade and native duplicate purge witnesses.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
