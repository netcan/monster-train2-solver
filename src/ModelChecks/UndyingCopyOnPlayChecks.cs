using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class UndyingCopyOnPlayChecks
{
    internal const string Reason = "Copy-on-play requires no authored traits or one ordinary authored Consume, optionally with one ordinary temporary Freeze.";

    internal static void Native(FixtureValue fixture)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture, Reason);
        var before = rows[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var card = before.Spawn.Train.Context!.FindCard(24)!;
        Require(rows.Length == 1 && card.DataId == CardCopyOnPlayModel.UndyingSpellId &&
            before.Spawn.Train.Context.Cards.Draw.Any(token => token.InstanceId == card.InstanceId) &&
            card.StatusScalingTraits is { Count: 1 } && card.StatusScalingTraits[0].Query.Type == "PlayedCost" &&
            card.StatusScalingTraits[0].StatusIds.SequenceEqual(new[] { "undying" }) &&
            card.Permanent.Upgrades.Single().DataId == CardCopyOnPlayModel.UpgradeId &&
            CardCopyOnPlayModel.Validate(card) == null, "Missing original X-cost Undying copy signature in draw pile.");
        // Keep uncalibrated neighboring source signatures rejected.
        foreach (string field in new[] { "Int", "Int2", "TrackedValue", "StatusEffectIds" })
        {
            var json = JsonSerializer.SerializeToNode(card, ModelJson.Options)!;
            var source = json["MaskDescriptor"]!["SourceTraits"]![1]!;
            if (field == "StatusEffectIds") source[field] = JsonSerializer.SerializeToNode(new[] { "poison" });
            else if (field == "TrackedValue") source[field] = "TimesPlayed";
            else source[field] = 7;
            Require(CardCopyOnPlayModel.Validate(json.Deserialize<CardInstanceState>(ModelJson.Options)!) == Reason,
                "Uncalibrated source parameter was accepted: " + field);
        }
        Console.WriteLine("UNDYING-COPY-DEPLOYMENT PASS: original zero-prefix rejection; complete native deployment successor/RNG; exact authored signature and four rejection controls. Actual copied spell execution checked separately.");
    }

    internal static void Pair(FixtureValue source, FixtureValue replay)
    {
        Native(source); Native(replay);
        NativeBranchObservationChecks.Pair(source, replay);
        Console.WriteLine("UNDYING-COPY-DEPLOYMENT-PAIR PASS: independent original/repaired deployment; no spell-effect coverage inferred.");
    }

    internal static void Effects(FixtureValue fixture)
    {
        bool branch = fixture.TryGetProperty("Request", out _);
        var rows = branch ? NativeBranchObservationChecks.Replay(fixture,
            "Original X-cost Undying copy retains authored scaling and native purge callbacks.").Where(row => !row.GetProperty("IsEndTurn").GetBoolean()) :
            fixture.GetProperty("Actions").EnumerateArray();
        int originals = 0, copies = 0, positive = 0, zero = 0;
        var created = new HashSet<int>();
        foreach (var row in rows)
        {
            var before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            var actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            var action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
            var context = before.Spawn.Train.Context!;
            var after = actual.Spawn.Train.Context!;
            var card = context.FindCard(action.CardInstanceId)!;
            if (card.DataId != CardCopyOnPlayModel.UndyingSpellId) continue;
            Require(action.TargetUnitId > 0, "Undying spell has no target.");
            int target = action.TargetUnitId;
            var prior = before.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == target);
            var next = actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == target);
            int spent = after.FindCard(card.InstanceId)!.LastPlayedCost;
            Require((next.Status("undying")?.Stacks ?? 0) - (prior.Status("undying")?.Stacks ?? 0) == spent,
                "X-cost Undying status differs from actual played cost.");
            if (spent > 0) positive++; else zero++;
            if (CardCopyOnPlayModel.Present(card))
            {
                originals++;
                var copy = after.FindCard(context.NextCardId)!;
                Require(after.NextCardId == context.NextCardId + 1 && copy.DataId == card.DataId &&
                    after.Cards.Hand.Any(token => token.InstanceId == copy.InstanceId) &&
                    copy.MaskDescriptor!.BaseTraits.Select(trait => trait.RuntimeType).SequenceEqual(new[] {
                        "CardTraitExhaustState", "CardTraitScalingAddStatusEffect" }) &&
                    copy.StatusScalingTraits is { Count: 1 } && copy.StatusScalingTraits[0].Query.Type == "PlayedCost" &&
                    !CardCopyOnPlayModel.Present(copy) &&
                    copy.Temporary.Upgrades.Any(upgrade => upgrade.AssetKey == "CopyOnPlayTraitUpgrade") &&
                    after.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(token => token.InstanceId == card.InstanceId),
                    "Copy dropped authored scaling/Consume, upgrade exclusion, identity or hand/exhaust routing.");
                created.Add(copy.InstanceId);
            }
            else
            {
                Require(created.Contains(card.InstanceId) && after.NextCardId == context.NextCardId &&
                    after.OtherPiles!.Single(pile => pile.Name == "Purged").Cards.Count(token => token.InstanceId == card.InstanceId) == 2,
                    "Played copy recursed or lost native duplicate purge references.");
                copies++;
            }
        }
        Require(originals > 0 && copies > 0 && positive > 0 && zero > 0,
            $"Missing native positive/zero-cost original/copy witnesses: originals={originals} copies={copies} positive={positive} zero={zero}.");
        if (!branch) RecordedBattleChecks.Native(fixture);
        Console.WriteLine($"UNDYING-COPY-EFFECTS PASS: originals={originals} copies={copies} positive={positive} zero={zero}; full immutable state/forecast/RNG chains.");
    }

    internal static void EffectsPair(FixtureValue source, FixtureValue replay)
    {
        Effects(source); Effects(replay);
        Require(source.GetProperty("Request").ContentEquals(replay.GetProperty("Request")), "Copied-spell replay changed its frozen request.");
        var first = source.GetProperty("Rows").EnumerateArray().ToArray();
        var second = replay.GetProperty("Rows").EnumerateArray().ToArray();
        Require(first.Length == second.Length, "Copied-spell replay changed input count.");
        for (int index = 0; index < first.Length; index++)
        {
            foreach (string field in new[] { "Before", "Actual" })
                Require(NativeBranchObservationChecks.Key(first[index].GetProperty(field).Deserialize<BattleTurnState>()!) ==
                    NativeBranchObservationChecks.Key(second[index].GetProperty(field).Deserialize<BattleTurnState>()!),
                    "Independent copied-spell process changed " + field + " at " + index);
            foreach (string field in new[] { "Action", "IsEndTurn", "Boundary" })
                Require(first[index].GetProperty(field).ContentEquals(second[index].GetProperty(field)),
                    "Independent copied-spell input changed at " + index);
        }
        Console.WriteLine("UNDYING-COPY-EFFECTS-PAIR PASS: independent eleven-input frozen original/copy execution.");
    }

    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
