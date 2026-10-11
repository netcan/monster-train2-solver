using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalStatusRelicBatchChecks
{
    private static readonly (string Key, string Id, string Team, string Status, int Stacks, int Condition)[] Artifacts =
    [
        ("ApplyExtraPyregel_DH", "f3e1c306-964c-4373-96c3-61e606379f43", "None", "pyregel", 1, 1),
        ("IncreasePyregelStacks", "f4c76f11-37d9-4bb5-867a-4d99b7cfa270", "Heroes", "pyregel", 6, 2),
        ("ExtraValorOnValor", "74c025fa-ddcb-494b-a790-dcadc5b5772b", "Monsters", "valor", 1, 0),
        ("IncreaseArmorStacks", "ba26070b-4f6b-4af0-a1ec-2af024b6af87", "Monsters", "armor", 4, 1)
    ];
    private static readonly string[] ExcludedSources =
    ["CardEffectMultiplyStatusEffect", "CardEffectTransferAllStatusEffects", "CardEffectTransferAllStatusEffectsFromUnit"];

    internal static IReadOnlyDictionary<string, int>? Native(FixtureValue root, bool required = false)
    {
        bool eligible = root.TryGetProperty("ModifierScenario", out var scenario) && scenario.GetString() == "original-status-relic-batch";
        Require(eligible || !required, "Fixture lacks the original status-stack relic batch.");
        if (!eligible) return null;
        Require(root.GetProperty("Schema").GetInt32() == 113 &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4",
            "Native status-stack relic schema or build changed.");
        Require(root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalCaptureBoundary").GetString() == "AfterStopCombatLoop" &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            root.GetProperty("Actions").GetArrayLength() > 0 && root.GetProperty("Stages").GetArrayLength() > 0 &&
            root.GetProperty("Turns").GetArrayLength() > 0, "Native status-stack relic battle is incomplete.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Native status-stack relic capture has nonzero " + key + ".");

        Query[] rows = root.GetProperty("RelicStatusStackQueries").EnumerateArray().Select(Read).ToArray();
        Require(rows.Length > 0, "Native status-stack relic queries are missing.");
        foreach (Query row in rows) CheckDefinitions(row.Relics);
        string parent = Serialize(rows);
        void CheckQueries()
        {
            foreach (Query row in rows)
                Require(Get(row, row.Relics) == row.Actual, "Original relic native status-stack query differs: " + Label(row));
        }
        CheckQueries();
        Parallel.For(0, 32, _ => CheckQueries());
        Require(Serialize(rows) == parent, "Parallel status-stack queries mutated their parent relics.");
        CheckMatrix(rows);

        var witnesses = Artifacts.ToDictionary(item => item.Key, _ => 0);
        int actualQueries = 0;
        foreach (Query row in rows.Where(row => !row.Matrix))
        {
            actualQueries++;
            foreach (var item in Artifacts)
            {
                int without = Get(row, row.Relics.Where(relic => relic.DataId != item.Id).ToArray());
                if (without == row.Actual) continue;
                Require(unchecked(row.Actual - without) == item.Stacks,
                    "Removing an original status-stack relic changed another payload: " + item.Key + " " + Label(row));
                witnesses[item.Key]++;
            }
        }
        Require(actualQueries > 0 && witnesses.Values.All(count => count > 0),
            "Some original status relics lack actual applications: " +
            string.Join(", ", witnesses.Select(item => item.Key + "=" + item.Value)));
        Console.WriteLine("ORIGINAL-STATUS-RELIC-WITNESSES " + string.Join(", ", witnesses.Select(item => item.Key + "=" + item.Value)));
        Console.WriteLine($"NATIVE-ORIGINAL-STATUS-RELICS PASS: four unchanged originals, {actualQueries} actual/{rows.Length - actualQueries} matrix queries; exact team, nullable target, positive/first-application, overflow and exclusion gates; permanent/deferred value-query gates only; per-relic actual effect ablations and 32 immutable branches.");
        return witnesses;
    }

    private static void CheckMatrix(Query[] allRows)
    {
        Query[] rows = allRows.Where(row => row.Matrix).ToArray();
        Require(rows.Length > 0, "Native status-stack boundary matrix is missing.");
        foreach (CombatTeam? team in new CombatTeam?[] { CombatTeam.Player, CombatTeam.Enemy, null })
        foreach (string id in new[] { "armor", "valor", "pyregel", "regen" })
        {
            bool Has(int incoming, bool permanent = false, bool deferred = false, bool includeAdders = true, string? type = null) =>
                rows.Any(row => row.TargetTeam == team && row.StatusId == id && row.Incoming == incoming &&
                    row.Permanent == permanent && row.Deferred == deferred && row.IncludeAdders == includeAdders && row.EffectType == type);
            Require(new[] { -1, 0, 1, int.MaxValue }.All(count => Has(count)) &&
                Has(1, permanent: true) && Has(0, permanent: true, deferred: true) &&
                Has(1, permanent: true, deferred: true) && Has(1, deferred: true) && Has(1, includeAdders: false) &&
                ExcludedSources.All(type => Has(1, type: type)),
                "Native status-stack matrix lacks a boundary for " + id + "/" + team + ".");
        }
        void Expect(string id, int incoming, CombatTeam? team, int actual, bool? has = null,
            bool permanent = false, bool deferred = false, bool includeAdders = true, string? type = null)
        {
            Query[] matching = rows.Where(row => row.StatusId == id && row.Incoming == incoming && row.TargetTeam == team &&
                (!has.HasValue || row.HasStatus == has) && row.Permanent == permanent && row.Deferred == deferred &&
                row.IncludeAdders == includeAdders && row.EffectType == type).ToArray();
            Require(matching.Length > 0 && matching.All(row => row.Actual == actual),
                "Native status-stack boundary changed or is absent: " + id + "/" + incoming + "/" + team +
                "/has=" + has + "/permanent=" + permanent + "/deferred=" + deferred + "/source=" + type + ".");
        }
        Expect("armor", 1, CombatTeam.Player, 5); Expect("armor", 1, CombatTeam.Enemy, 1); Expect("armor", 1, null, 5);
        Expect("armor", 0, CombatTeam.Player, 0); Expect("armor", -1, CombatTeam.Player, -1);
        Expect("valor", 0, CombatTeam.Player, 1); Expect("valor", -1, CombatTeam.Player, 0);
        Expect("valor", 0, CombatTeam.Enemy, 0); Expect("valor", 0, null, 1);
        // Enemy OnTurnBegin may apply its first Pyregel before a decision
        // matrix runs. Use that real query together with later matrix queries
        // to prove the first/repeated gate, without changing native state.
        Query[] firstApplications = allRows.Where(row => row.StatusId == "pyregel" && row.TargetTeam == CombatTeam.Enemy &&
            !row.Permanent && !row.Deferred && row.IncludeAdders && !ExcludedSources.Contains(row.EffectType ?? "")).ToArray();
        Require(firstApplications.Any(row => row.Incoming == 1 && !row.HasStatus && row.Actual == 8) &&
            firstApplications.Any(row => row.Incoming == 1 && row.HasStatus && row.Actual == 2),
            "Native first/repeated Pyregel application evidence is missing.");
        foreach (Query row in firstApplications.Where(row => !row.HasStatus && row.Incoming is -1 or 0))
            Require(row.Actual == row.Incoming + 6, "Native first application incorrectly requires positive incoming stacks.");
        Expect("pyregel", 1, CombatTeam.Player, 2); Expect("pyregel", 1, null, 2);
        Expect("armor", int.MaxValue, CombatTeam.Player, unchecked(int.MaxValue + 4));
        Expect("armor", 1, CombatTeam.Player, 1, permanent: true);
        Expect("armor", 0, CombatTeam.Player, 4, permanent: true, deferred: true);
        Expect("pyregel", 0, CombatTeam.Enemy, 7, permanent: true, deferred: true);
        Expect("pyregel", 0, null, 7, permanent: true, deferred: true);
        Expect("armor", 1, CombatTeam.Player, 1, deferred: true);
        Expect("armor", 1, CombatTeam.Player, 1, includeAdders: false);
        foreach (string type in ExcludedSources) Expect("armor", 1, CombatTeam.Player, 1, type: type);
        foreach (Query row in rows.Where(row => row.StatusId == "regen" || !row.IncludeAdders ||
            ExcludedSources.Contains(row.EffectType ?? "") || row.Permanent != row.Deferred))
            Require(row.Actual == row.Incoming, "An excluded, unrelated or nonmatching permanent/deferred query received extra stacks.");
    }

    private static void CheckDefinitions(IReadOnlyList<CombatRelicState> relics)
    {
        foreach (var item in Artifacts)
        {
            CombatRelicState[] matches = relics.Where(relic => relic.DataId == item.Id).ToArray();
            Require(matches.Length == 1, "Missing or duplicated original status relic: " + item.Key);
            CombatRelicState relic = matches[0];
            Require(relic.AssetKey == item.Key && relic.EffectTypes.SequenceEqual([RelicModel.AddStatusOnApplied]) &&
                relic.IsCovenant == false && relic.DisallowedInPlacementPhase == false && relic.StatusStackAdditions?.Count == 1,
                "Original status relic identity or effect type changed: " + item.Key);
            RelicStatusStacks rule = relic.StatusStackAdditions![0];
            Require(rule.EffectIndex == 0 && rule.SourceTeam == item.Team && rule.StatusId == item.Status &&
                rule.AdditionalStacks == item.Stacks && rule.AddCondition == item.Condition && rule.Conditions.Count == 0,
                "Original status relic payload changed: " + item.Key);
        }
    }
    private sealed record Query(string StatusId, int Incoming, CombatTeam? TargetTeam, bool HasStatus,
        bool Permanent, bool Deferred, bool IncludeAdders, string? EffectType, CombatRelicState[] Relics, int Actual, bool Matrix);
    private static Query Read(FixtureValue row) => new(row.GetProperty("StatusId").GetString()!, row.GetProperty("Incoming").GetInt32(),
        row.GetProperty("TargetTeam").Deserialize<CombatTeam?>(), row.GetProperty("HasStatus").GetBoolean(),
        row.GetProperty("Permanent").GetBoolean(), row.GetProperty("Deferred").GetBoolean(), row.GetProperty("IncludeAdders").GetBoolean(),
        row.GetProperty("EffectType").GetString(), row.GetProperty("Relics").Deserialize<CombatRelicState[]>()!,
        row.GetProperty("Actual").GetInt32(), row.GetProperty("Matrix").GetBoolean());
    private static int Get(Query row, IReadOnlyList<CombatRelicState> relics) => RelicStatusStackModel.GetStacks(
        row.IncludeAdders ? relics : null, row.StatusId, row.Incoming, row.TargetTeam, row.HasStatus, row.Permanent, row.Deferred, row.EffectType);
    private static string Label(Query row) => row.StatusId + "/" + row.Incoming + "/" + row.TargetTeam + "/has=" + row.HasStatus +
        "/permanent=" + row.Permanent + "/deferred=" + row.Deferred + "/adders=" + row.IncludeAdders + "/source=" + row.EffectType;
    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool value, string message) { if (!value) throw new InvalidDataException(message); }
}
