using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class MoonCardUpgradeChecks
{
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("DecisionUnchanged").GetBoolean() &&
            root.GetProperty("FrameBefore").GetInt32() == root.GetProperty("FrameAfter").GetInt32() &&
            root.GetProperty("RngBefore").ContentEquals(root.GetProperty("RngAfter")), "Native moon calibration changed live state/frame/RNG.");
        FixtureValue[] rows = root.GetProperty("Rows").EnumerateArray().ToArray();
        Require(rows.Length == 135 && rows.Select(row => row.GetProperty("CardDataId").GetString()).Distinct().Count() == 5 &&
            rows.Select(row => row.GetProperty("Variant").GetString()).Distinct().Count() == 3, "Incomplete original moon upgrade controls.");
        foreach (FixtureValue row in rows)
        {
            CardInstanceState source = row.GetProperty("Before").Deserialize<CardInstanceState>()!;
            CardInstanceState actual = row.GetProperty("Actual").Deserialize<CardInstanceState>()!;
            string parent = Json(source), operation = row.GetProperty("Operation").GetString()!;
            int phase = row.GetProperty("Phase").GetInt32(); bool preview = row.GetProperty("Preview").GetBoolean();
            CardMoonUpgradeResult result = CardMoonUpgradeModel.Update(source, phase, reset: operation == "Discard", preview: preview);
            string? difference = result.Supported ? ModelJson.Difference(Json(result.Card), Json(actual)) : result.UnsupportedReason;
            Require(difference == null, source.DataId + "/" + row.GetProperty("Variant").GetString() + "/" + operation + "/" + phase + ": " + difference);
            Require(Json(source) == parent, "Moon upgrade changed its immutable parent.");
            if (preview) Require(Json(actual) == parent, "Preview resource callback must be a native no-op.");
            if (operation == "Discard") Require(actual.MaskDescriptor!.BaseTraits.Where(trait => trait.RuntimeType == CardMoonUpgradeModel.Trait)
                .All(trait => trait.MoonPhaseUpgradeState is { Active: false, Cached: true }), "Discard must reset every cached moon trait.");
            Parallel.For(0, 16, _ =>
            {
                CardMoonUpgradeResult branch = CardMoonUpgradeModel.Update(source, phase, reset: operation == "Discard", preview: preview);
                Require(branch.Supported && Json(branch.Card) == Json(result.Card) && Json(source) == parent, "Parallel moon upgrade branches differ or share mutable state.");
            });
        }
        CardInstanceState guard = rows[0].GetProperty("Before").Deserialize<CardInstanceState>()!;
        CardMaskDescriptor descriptor = guard.MaskDescriptor!;
        CardInstanceState missing = guard.WithMaskDescriptor(new CardMaskDescriptor(descriptor.Definition, descriptor.BaseCost, descriptor.BaseTraits,
            descriptor.AuthoredCastEffects, descriptor.Purified, descriptor.PermanentGraft, descriptor.TemporaryTraits, descriptor.PermanentReplacements));
        Require(!CardMoonUpgradeModel.Update(missing, 2).Supported, "Missing moon source metadata must remain unsupported.");
        Require(!CardMoonUpgradeModel.Update(guard, 0).Supported, "Unknown moon phase must remain unsupported.");
        Console.WriteLine("MOON-CARD-UPGRADE PASS: five original spells, seven upgrade definitions, 135 complete native card states, phase reversal/repeat, preview no-op, draw/discard reset, magic-power rescaling, existing-trait protection and 16 immutable branches. Whole-battle callbacks require separate integration witnesses.");
    }
    private static string Json(object? value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool condition, string reason) { if (!condition) throw new InvalidOperationException(reason); }
}
