using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RelicForgeConservationChecks
{
    private const string DataId = "b2868935-103c-4d27-ae48-a96d947c05ed";
    internal static void Run()
    {
        CombatRelicState Owner(string id, int cap, int mode = 0, RelicConditionState[]? conditions = null) =>
            new(id, id, [RelicResourceModel.ConserveForgePoints], disallowedInPlacementPhase: true,
                resourceEffects: [new(0, RelicResourceModel.ConserveForgePoints, cap, mode,
                    mode == 0 ? Math.Max(0, cap) : Math.Clamp(cap, 0, 100), false, conditions: conditions)]);
        CombatContext Context(int points, params CombatRelicState[] relics)
        {
            var rng = UnityRng.Seed(77);
            return new(new([], [], [], rng, 0, []), rng, 0, 1, 10,
                statistics: BattleStatistics.Empty(deckCards: []), queryFrame: new(forgePoints: points, energy: 0, runningCombat: true),
                energyState: new(10, 0, 0, "MonsterTurn", true), relics: relics,
                resources: new(3, 1, 5, 10, 10, "pyre", 20, 80, 0, 0, [], [], []));
        }
        foreach (int points in new[] { 0, 5, 10, 17 })
        {
            var source = Context(points, Owner("cap", 10)); string frozen = JsonSerializer.Serialize(source);
            var result = RelicResourceModel.FinishCombatForgePoints(source);
            Require(result.Supported && result.Value == Math.Min(points, 10) && result.Events.Single().Notified &&
                JsonSerializer.Serialize(source) == frozen, "Forge cap, zero notification or immutable input differs.");
            Require(RelicResourceModel.FinishCombatForgePoints(source, preview: true).Value == points &&
                RelicResourceModel.FinishCombatForgePoints(source, preview: true).Events.Count == 0 &&
                RelicResourceModel.FinishCombatForgePoints(Context(points)).Value == 0,
                "Preview skip or no-owner combat-end reset differs.");
        }
        Require(RelicResourceModel.FinishCombatForgePoints(Context(17, Owner("first", 5), Owner("second", 10))).Value == 5 &&
            RelicResourceModel.FinishCombatForgePoints(Context(17, Owner("negative", -4))).Value == 0 &&
            RelicResourceModel.FinishCombatForgePoints(Context(17, Owner("percent", 50, 1))).Value == 8 &&
            RelicResourceModel.FinishCombatForgePoints(Context(17, Owner("percent-cap", 150, 1))).Value == 17 &&
            RelicResourceModel.FinishCombatForgePoints(Context(17, Owner("unknown-mode", 10, 2))).Value == 0,
            "First-eligible semantics or initialized flat/percentage/unknown mode differs.");
        var once = new RelicConditionState(new("AnyCardPlayed"), true, 6, 1, false);
        var fired = RelicResourceModel.FinishCombatForgePoints(Context(17, Owner("conditional", 5, conditions: [once]), Owner("fallback", 10)));
        var repeated = RelicResourceModel.FinishCombatForgePoints(fired.Context!);
        Require(fired.Supported && fired.Value == 5 && fired.Events.Single().RelicId == "conditional" &&
            fired.Context!.Relics![0].ResourceEffects![0].Conditions.Single().Triggered &&
            repeated.Supported && repeated.Events.Single().RelicId == "fallback",
            "Forge conservation did not notify/consume the first condition and advance to the next eligible owner.");
        Console.WriteLine("RELIC-FORGE-CONSERVATION-CHECKS PASS: combat-end lifecycle, flat/percentage bounds, first eligible owner, zero notification, conditions, preview, ablation and immutable inputs.");
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("ModifierScenario").GetString() == "original-forge-conservation-relic" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            root.GetProperty("GameVersion").GetString() == "2.2.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }.All(name => root.GetProperty(name).GetInt32() == 0),
            "Forge-conservation battle failed acceptance.");
        var rows = root.GetProperty("ForgeCombatEndOperations").EnumerateArray().ToArray();
        Require(rows.Length == 6 && rows.Select(row => row.GetProperty("Kind").GetString()).SequenceEqual(
            ["matrix-0", "matrix-5", "matrix-10", "matrix-17", "ablation", "natural"]), "Missing forge lifecycle matrix/ablation/natural callbacks.");
        foreach (var row in rows)
        {
            var before = row.GetProperty("Before").Deserialize<CombatContext>()!;
            var after = row.GetProperty("After").Deserialize<CombatContext>()!;
            bool ablation = row.GetProperty("Kind").GetString() == "ablation";
            if (!ablation)
            {
                var owner = before.Relics!.Single(relic => relic.DataId == DataId);
                Require(owner.AssetKey == "SteelStockpile" && owner.IsCovenant == false && owner.DisallowedInPlacementPhase == false &&
                    owner.EffectTypes.SequenceEqual([RelicResourceModel.ConserveForgePoints]), "Original forge owner changed.");
                var effect = owner.ResourceEffects!.Single();
                Require(effect.EffectIndex == 0 && effect.Type == RelicResourceModel.ConserveForgePoints &&
                    effect.ParamInt == 10 && effect.ParamInt2 == 0 && effect.Amount == 10 && effect.Conditions.Count == 0 &&
                    !effect.CanApplyInPreviewMode && effect.RequiredMutatorId == null, "Original forge conservation payload changed.");
            }
            else Require(before.Relics!.All(relic => relic.DataId != DataId), "Ablated owner is still installed.");
            string frozen = JsonSerializer.Serialize(before);
            var predicted = RelicResourceModel.FinishCombatForgePoints(before);
            Require(predicted.Supported, "Native forge combat-end unsupported: " + predicted.UnsupportedReason);
            Require(predicted.Value == after.QueryFrame!.ForgePoints &&
                predicted.Events.Count(item => item.Notified) == row.GetProperty("Notifications").GetInt32() &&
                row.GetProperty("MoonPhaseAfter").GetString() == "New", "Native forge amount/notification/moon reset differs.");
            Require(JsonSerializer.Serialize(predicted.Context) == JsonSerializer.Serialize(after), "Complete forge combat-end context differs.");
            Require(JsonSerializer.Serialize(before) == frozen, "Forge replay mutated its input.");
        }
        var natural = rows[^1];
        Require(natural.GetProperty("Before").Deserialize<CombatContext>()!.QueryFrame!.ForgePoints == 17 &&
            natural.GetProperty("After").Deserialize<CombatContext>()!.QueryFrame!.ForgePoints == 10,
            "Natural post-victory forge conservation lacks a positive capped witness.");
        foreach (var turn in root.GetProperty("Turns").EnumerateArray())
        {
            Require(turn.GetProperty("Before").Deserialize<BattleTurnState>()!.ForgePoints == 17 &&
                turn.GetProperty("Actual").Deserialize<BattleTurnState>()!.ForgePoints == 17,
                "Combat-end forge conservation was incorrectly applied at a per-turn boundary.");
        }
        Console.WriteLine("ORIGINAL-FORGE-CONSERVATION-NATIVE PASS: four exact combat-end matrix contexts, removed-owner control, natural post-victory 17-to-10, notification order and unchanged per-turn forge state.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
