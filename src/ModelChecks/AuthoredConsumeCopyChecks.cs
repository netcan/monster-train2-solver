using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class AuthoredConsumeCopyChecks
{
    internal const string InitialReason = "Copy-on-play requires the original single-copy spell upgrade and captured purge/replacement definition.";
    internal const string CallbackReason = "Copy-on-play combined discard callbacks require native ordering calibration.";
    private const string BoxId = "f0ff6411-5c58-4b69-b025-0f6e4a52fca7";

    internal static void Native(FixtureValue fixture)
    {
        string reason = fixture.GetProperty("Request").GetProperty("Reason").GetString()!;
        Require(reason is InitialReason or CallbackReason, "Unexpected authored-Consume/copy boundary.");
        FixtureValue[] rows = NativeBranchObservationChecks.Replay(fixture, reason);
        FixtureValue last = rows[^1];
        BattleTurnState before = last.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = last.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction action = last.GetProperty("Action").Deserialize<PlayCardAction>()!;
        CombatContext old = before.Spawn.Train.Context!, next = actual.Spawn.Train.Context!;
        CardInstanceState card = old.FindCard(action.CardInstanceId)!;
        Require(card.DataId == BoxId && card.MaskDescriptor!.SourceTraits!.Single().Type == "CardTraitExhaustState" &&
            card.MaskDescriptor.BaseTraits[0].RuntimeType == "CardTraitExhaustState" &&
            card.Permanent.Upgrades.Any(upgrade => upgrade.DataId == CardCopyOnPlayModel.UpgradeId),
            "Missing the original authored Consume and merchant copy upgrade.");
        if (reason == InitialReason)
        {
            Require(rows.Length == 12 && action.CardInstanceId == 9 && before.Spawn.Turn == 2 &&
                old.NextCardId == 29 && next.NextCardId == 34, "Missing the first five-card creation boundary.");
            CardInstanceState recursive = next.FindCard(32)!, copy = next.FindCard(33)!;
            Require(recursive.DataId == BoxId && copy.DataId == BoxId && recursive.CanBeCopied == true && copy.CanBeCopied == true &&
                recursive.Permanent.Upgrades.Any(upgrade => upgrade.DataId == CardCopyOnPlayModel.UpgradeId) &&
                recursive.Temporary.Upgrades.Single().AssetKey == "AddEphemeral" &&
                copy.Permanent.Upgrades.All(upgrade => upgrade.DataId != CardCopyOnPlayModel.UpgradeId) &&
                copy.Temporary.Upgrades.Single().AssetKey == "CopyOnPlayTraitUpgrade" &&
                recursive.MaskDescriptor!.SourceTraits!.Single().Type == "CardTraitExhaustState" &&
                copy.MaskDescriptor!.SourceTraits!.Single().Type == "CardTraitExhaustState" &&
                copy.MaskDescriptor.BaseTraits.Single().RuntimeType == "CardTraitExhaustState" &&
                recursive.Permanent.BoxedCards!.Count == 3 && copy.Permanent.BoxedCards!.Count == 3,
                "On-cast and on-discard copies lost their distinct modifiers, authored Consume or boxed contents.");
            CombatRelicState owner = old.Relics!.Single(relic => relic.DataId == "0f4a9e72-cd34-4667-95f6-6a0ca69832f8");
            RelicCopyCardOnCardPlayModifier effect = owner.CopyCardOnCardPlayModifiers!.Single();
            CardMaskDescriptor normalized = RelicCopyCardOnCardPlayModel.CreationMask(owner, effect, card);
            Require(!ReferenceEquals(normalized, card.MaskDescriptor) && normalized.BaseTraits.Count == 1 &&
                normalized.BaseTraits[0].RuntimeType == "CardTraitExhaustState", "The observed copied card retained installed traits as authored traits.");
            foreach (string mutation in new[] { "owner", "card", "upgrade", "source", "traits", "callback" })
            {
                JsonNode ownerNode = JsonSerializer.SerializeToNode(owner, ModelJson.Options)!;
                JsonNode effectNode = JsonSerializer.SerializeToNode(effect, ModelJson.Options)!;
                JsonNode cardNode = JsonSerializer.SerializeToNode(card, ModelJson.Options)!;
                switch (mutation)
                {
                    case "owner": ownerNode["DataId"] = "unknown"; break;
                    case "card": cardNode["DataId"] = "unknown"; break;
                    case "upgrade": effectNode["Upgrade"]!["DataId"] = "unknown"; break;
                    case "source": cardNode["MaskDescriptor"]!["SourceTraits"]![0]!["Type"] = "unknown"; break;
                    case "traits": cardNode["MaskDescriptor"]!["BaseTraits"]![1]!["RuntimeType"] = "unknown"; break;
                    case "callback": cardNode["MaskDescriptor"]!["BaseTraits"]![1]!["CopyOnPlayUpgrade"]!["DataId"] = "unknown"; break;
                }
                CardInstanceState changed = cardNode.Deserialize<CardInstanceState>(ModelJson.Options)!;
                Require(ReferenceEquals(RelicCopyCardOnCardPlayModel.CreationMask(
                    ownerNode.Deserialize<CombatRelicState>(ModelJson.Options)!,
                    effectNode.Deserialize<RelicCopyCardOnCardPlayModifier>(ModelJson.Options)!, changed), changed.MaskDescriptor),
                    "Uncalibrated copy source was normalized: " + mutation);
            }
        }
        else if (rows.Length == 14)
        {
            Require(rows.Length == 14 && action.CardInstanceId == 32 && old.Cards.Hand.Count == 8 &&
                next.Cards.Hand.Count == next.MaxHandSize && next.NextCardId == old.NextCardId + 3 &&
                card.Temporary.Upgrades.Single().AssetKey == "AddEphemeral" &&
                next.OtherPiles!.Single(pile => pile.Name == "Purged").Cards.Count(token => token.InstanceId == card.InstanceId) == 2 &&
                next.OtherPiles.Single(pile => pile.Name is "Standby").Cards.All(token => token.InstanceId != card.InstanceId) &&
                next.OtherPiles.Single(pile => pile.Name is "Exhausted").Cards.All(token => token.InstanceId != card.InstanceId),
                "The full-hand control must unbox three cards, suppress new copies and purge its original twice.");
        }
        else
        {
            CardInstanceState copy = next.FindCard(40)!;
            Require(rows.Length == 18 && action.CardInstanceId == 32 && old.Cards.Hand.Count == 7 &&
                next.Cards.Hand.Count == next.MaxHandSize && next.NextCardId == old.NextCardId + 4 &&
                copy.DataId == BoxId && copy.Permanent.Upgrades.All(upgrade => upgrade.DataId != CardCopyOnPlayModel.UpgradeId) &&
                copy.Temporary.Upgrades.Select(upgrade => upgrade.AssetKey).SequenceEqual(["CopyOnPlayTraitUpgrade", "AddEphemeral"]) &&
                copy.MaskDescriptor!.BaseTraits.Single().RuntimeType == "CardTraitExhaustState" && copy.Permanent.BoxedCards!.Count == 3 &&
                next.OtherPiles!.Single(pile => pile.Name == "Purged").Cards.Count(token => token.InstanceId == 32) == 2,
                "With one slot after unboxing, the discard copy must inherit Consume, boxed contents and both temporary upgrades.");
        }
        Require(next.OtherPiles!.SelectMany(pile => pile.Cards).Any(token => token.InstanceId == card.InstanceId),
            "The consumed/purged original disappeared from all native piles.");
        Console.WriteLine($"AUTHORED-CONSUME-COPY PASS inputs={rows.Length} callback={reason == CallbackReason}: full native state/RNG and immutable roots; component observation only.");
    }

    internal static void Pair(FixtureValue source, FixtureValue replay)
    {
        Native(source); Native(replay); NativeBranchObservationChecks.Pair(source, replay);
        Console.WriteLine("AUTHORED-CONSUME-COPY-PAIR PASS: old rejection retained and fresh supported prediction matches every native input/state/RNG.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
