using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalBattleSetupChecks
{
    internal static void Pair(FixtureValue stock, FixtureValue modified)
    {
        string[] owners = [RelicModel.ChampionReplacementId, RelicModel.ForceTrialId];
        Validate(stock, "Stock", []);
        Validate(modified, "Modified", owners);
        foreach (string field in new[] { "Seed", "Scenario", "ScenarioId", "ClassId", "SubclassId", "ChampionPool" })
            Require(stock.GetProperty(field).ContentEquals(modified.GetProperty(field)), $"Original setup pair changed {field}.");

        string primary = Text(stock, "ClassId")!;
        string secondary = Text(stock, "SubclassId")!;
        string defaultChampion = Text(stock.GetProperty("DeckChampion"), "Id")!;
        FixtureValue replaced = modified.GetProperty("DeckChampion");
        string replacementId = Text(replaced, "Id")!;
        string replacementClass = Text(replaced, "ClassId")!;
        FixtureValue[] pool = modified.GetProperty("ChampionPool").EnumerateArray().ToArray();
        Require(Text(stock.GetProperty("DeckChampion"), "ClassId") == primary &&
            string.IsNullOrEmpty(Text(stock, "ReplacementClassId")) &&
            replacementId != defaultChampion && replacementClass != primary && replacementClass != secondary &&
            Text(modified, "ReplacementClassId") == replacementClass &&
            pool.Length > 0 && pool.All(row => HasId(Text(row, "Id")) && HasId(Text(row, "ClassId"))) &&
            pool.Any(row => Text(row, "Id") == replacementId && Text(row, "ClassId") == replacementClass),
            "The settled champion is not an original replacement from outside the two chosen classes.");

        FixtureValue initialTrial = stock.GetProperty("TrialBefore");
        foreach (FixtureValue witness in new[] { stock, modified })
            foreach (string field in new[] { "Id", "SinId" })
                Require(initialTrial.GetProperty(field).ContentEquals(witness.GetProperty("TrialBefore").GetProperty(field)) &&
                    initialTrial.GetProperty(field).ContentEquals(witness.GetProperty("TrialIntro").GetProperty(field)),
                    $"The pair changed the prefetched native trial {field}.");
        Console.WriteLine("ORIGINAL-BATTLE-SETUP PASS: matched seed and native trial, original replacement champion has legal deployment, forced trial is locked with four draws.");
    }

    private static void Validate(FixtureValue witness, string mode, string[] owners)
    {
        Require(witness.TryGetProperty("Boundary", out var boundary) && boundary.GetString() == "OriginalBattleSetup" &&
            witness.GetProperty("Schema").GetInt32() == 1 &&
            Text(witness, "GameVersion") == Game231FixtureCatalog.Version &&
            Text(witness, "GameModuleMvid") == Game231FixtureCatalog.Module && Text(witness, "Mode") == mode &&
            !string.IsNullOrEmpty(Text(witness, "Scenario")) && HasId(Text(witness, "ScenarioId")) &&
            HasId(Text(witness, "ClassId")) && HasId(Text(witness, "SubclassId")) &&
            Text(witness, "ClassId") != Text(witness, "SubclassId") &&
            witness.GetProperty("Mutators").EnumerateArray().Select(item => item.GetString()).SequenceEqual(owners),
            $"Expected pinned original {mode} setup with the exact active mutator order.");
        bool forced = owners.Length > 0;
        FixtureValue trialBefore = witness.GetProperty("TrialBefore");
        FixtureValue intro = witness.GetProperty("TrialIntro");
        string? sin = Text(intro, "SinId");
        Require(HasId(Text(trialBefore, "Id")) && HasId(sin) && !Flag(trialBefore, "Enabled") &&
            Flag(intro, "Enabled") == forced && Flag(intro, "Locked") == forced &&
            (forced ? Text(witness, "ActiveTrialSinId") == sin : string.IsNullOrEmpty(Text(witness, "ActiveTrialSinId"))),
            $"The {mode} trial toggle, lock or settled trial sin differs.");

        BattleTurnState state = witness.GetProperty("Deployment").Deserialize<BattleTurnState>()!;
        CombatContext context = state.Spawn.Train.Context!;
        var relics = context.Relics ?? [];
        Require(state.Spawn.Turn == 0 && state.Spawn.Train.Rooms.Any(room => room.Deployment) &&
            state.DrawPerTurn == (forced ? 4 : 5) &&
            relics.Where(relic => relic.DataId is RelicModel.ChampionReplacementId or RelicModel.ForceTrialId)
                .Select(relic => relic.DataId).SequenceEqual(owners) &&
            relics.Where(relic => owners.Contains(relic.DataId)).All(RelicModel.IsOriginalBattleSetupOwner) &&
            relics.Any(relic => relic.DataId == sin && relic.AssetKey == "Enemy_LowerDraw") == forced,
            $"The {mode} deployment did not retain the original setup owners and trial sin.");
        FixtureValue champion = witness.GetProperty("DeckChampion");
        int instance = champion.GetProperty("InstanceId").GetInt32();
        string? id = Text(champion, "Id");
        string? key = Text(champion, "Key");
        CardInstanceState? card = context.FindCard(instance);
        Require(instance > 0 && HasId(id) && !string.IsNullOrEmpty(key) &&
            card is { MaskDescriptor.Definition.IsChampionCard: true } && card.DataId == id &&
            card.MaskDescriptor.Definition.LinkedClan == Text(champion, "ClassId") &&
            context.Cards.Hand.Count(token => token.InstanceId == instance && token.DataId == id) == 1 &&
            state.PlayRules?.Cards.Any(rule => rule.DataId == id && rule.AssetKey == key) == true &&
            card.Permanent.Upgrades.Select(upgrade => upgrade.DataId).SequenceEqual(
                champion.GetProperty("Upgrades").EnumerateArray().Select(item => item.GetString())),
            $"The {mode} champion does not match the original deployment card and its ordered upgrades.");
        var candidates = BattleActionModel.EnumerateActions(state, instance, includeAbilities: false);
        Require(!candidates.HasUnsupportedAttempts && candidates.SupportedActions.Count > 0,
            $"The {mode} champion must have a modeled deployment: " +
            string.Join(" | ", candidates.UnsupportedAttempts.Select(attempt => attempt.Reason).Distinct()));
    }

    private static bool HasId(string? value) => Guid.TryParseExact(value, "D", out Guid id) && id != Guid.Empty;
    private static string? Text(FixtureValue value, string field) => value.GetProperty(field).GetString();
    private static bool Flag(FixtureValue value, string field) => value.GetProperty(field).GetBoolean();
    private static void Require(bool valid, string message) => TrueFinalBossChecks.Require(valid, message);
}
