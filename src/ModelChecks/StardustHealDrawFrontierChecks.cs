using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class StardustHealDrawFrontierChecks
{
    private const string StardustDataId = "135cf22d-6d1b-47ee-b48e-e8f7b47e5750";

    internal static void Native(FixtureValue archive)
    {
        Require(archive.GetProperty("Schema").GetInt32() == 1 &&
            archive.GetProperty("Boundary").GetString() == "OriginalStardustHealDraw" &&
            archive.GetProperty("GameVersion").GetString() == "2.3.1" &&
            archive.GetProperty("GameModuleMvid").GetString() == Game231FixtureCatalog.Module,
            "Expected the original 2.3.1 Stardust heal-and-draw witness.");
        var before = archive.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var recorded = archive.GetProperty("Action").Deserialize<PlayCardAction>()!;
        var actual = archive.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var beforeDuplication = archive.GetProperty("BeforeDuplication").Deserialize<CombatContext>()!;
        int drawn = archive.GetProperty("DrawnRecipientId").GetInt32();
        int held = archive.GetProperty("ExistingRecipientId").GetInt32();
        int copy = archive.GetProperty("CopyCardId").GetInt32();
        int carrier = archive.GetProperty("CarrierUnitId").GetInt32();
        int[] offered = archive.GetProperty("OfferedRecipientIds").Deserialize<int[]>()!;
        var context = before.Spawn.Train.Context!;
        var after = actual.Spawn.Train.Context!;
        var selection = recorded.RecursionSelections.Single();
        var rule = before.PlayRules!.Cards.Single(card => card.DataId == StardustDataId);
        var wounded = before.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == carrier);
        var healed = actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == carrier);
        Require(context.FindCard(recorded.CardInstanceId)?.DataId == StardustDataId &&
            rule.Effects.Select(effect => effect.Type).SequenceEqual(["AdvanceMoonPhase", "DuplicateCard"]) &&
            selection.EffectIndex == 1 && selection.CardInstanceIds.SequenceEqual([drawn]) &&
            archive.GetProperty("NativeSelectedId").GetInt32() == drawn &&
            context.Relics!.Any(relic => relic.AssetKey == "MooncycleRestoreFriendlyHealth" &&
                relic.DataId == "6d977136-5aac-44fd-9843-54cf2a0bd01d") &&
            context.Relics!.Any(relic => relic.AssetKey == "CardDrawOnHeal" &&
                relic.DataId == "9a38370b-150c-45bd-9e3e-93a688a04850"),
            "The original paid effect order or heal-and-draw relics changed.");
        Require(context.Cards.Hand.Any(card => card.InstanceId == held) &&
            context.Cards.Hand.All(card => card.InstanceId != drawn) &&
            context.Cards.Draw.Last().InstanceId == drawn &&
            beforeDuplication.Cards.Hand.Any(card => card.InstanceId == drawn) &&
            before.MoonPhase != actual.MoonPhase && wounded.Health < wounded.MaxHealth &&
            healed.Health == wounded.MaxHealth &&
            after.Cards.Hand.Any(card => card.InstanceId == drawn) &&
            after.Cards.Draw.All(card => card.InstanceId != drawn) &&
            copy == context.NextCardId && after.Cards.Hand.Any(card => card.InstanceId == copy &&
                card.DataId == context.FindCard(drawn)!.DataId),
            "The paid native cast must move the selected spell from deck to hand and duplicate it.");

        using var keys = new BattleStateKeys();
        string parentKey = keys.Key(before);
        var frontier = BattleActionModel.EnumerateActions(before, recorded.CardInstanceId, includeAbilities: false);
        Require(frontier.UnsupportedAttempts.Count == 0, "Stardust candidate search produced unsupported attempts.");
        var choices = frontier.SupportedCandidates.Where(candidate => candidate.Action.RoomIndex == recorded.RoomIndex)
            .ToDictionary(candidate => candidate.Action.RecursionSelections.Single().CardInstanceIds.Single());
        Require(choices.ContainsKey(drawn) && choices.ContainsKey(held) &&
            choices.Keys.Order().SequenceEqual(offered.Order()),
            "The targeted floor must expose exactly the in-cast choices offered by the native filter.");
        var direct = BattleActionModel.PlayCard(before, recorded);
        string actualKey = keys.Key(actual);
        Require(direct.Supported && keys.Key(direct.State!) == actualKey &&
            keys.Key(choices[drawn].Result.State!) == actualKey,
            "Direct and enumerated Stardust plays must match the complete native successor and RNG.");
        var alternate = choices[held];
        var replay = BattleActionModel.PlayCard(before, alternate.Action);
        Require(replay.Supported && replay.Outcome == alternate.Result.Outcome &&
            keys.Key(replay.State!) == keys.Key(alternate.Result.State!) &&
            replay.State!.Spawn.Train.Context!.FindCard(context.NextCardId)?.DataId == context.FindCard(held)!.DataId,
            "The previously held choice must replay exactly and produce its own copy.");
        using var independent = new BattleStateKeys();
        Require(independent.Key(before) == parentKey, "Enumerating Stardust choices changed the parent battle or RNG.");
        Console.WriteLine("STARDUST-HEAL-DRAW-FRONTIER PASS: original settled draw and duplicate, complete native successor and RNG, exact replay of the previously held alternative.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
