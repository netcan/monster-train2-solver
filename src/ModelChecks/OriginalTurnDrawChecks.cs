using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalTurnDrawChecks
{
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("ModifierScenario").GetString() == "original-turn-draw-relics" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }.All(name => root.GetProperty(name).GetInt32() == 0), "Turn draw capture failed acceptance.");
        int opening = 0, health = 0, partial = 0, full = 0, zero = 0, natural = 0;
        foreach (var row in root.GetProperty("RelicTurnDraw").EnumerateArray())
        {
            Require(row.GetProperty("Completed").GetBoolean(), "Unfinished native draw callback.");
            var before = row.GetProperty("Before").Deserialize<CombatContext>()!; var actual = row.GetProperty("Actual").Deserialize<CombatContext>()!;
            var openingOwner = before.Relics!.Single(owner => owner.DataId == "2058bafa-a15c-4787-9b9e-4dc7fdf09b74");
            var healthOwner = before.Relics.Single(owner => owner.DataId == "44aae53e-ee70-4b2c-8d25-a02f977349ee");
            Require(openingOwner.AssetKey == "DrawToMaxStartOfCombat" && openingOwner.DisallowedInPlacementPhase == true &&
                openingOwner.EffectTypes.SequenceEqual([RelicTurnDrawModel.Opening]) && openingOwner.ResourceEffects!.Single().ParamInt == 10 &&
                healthOwner.AssetKey == "PyrePiping" && healthOwner.DisallowedInPlacementPhase == false &&
                healthOwner.EffectTypes.SequenceEqual([RelicTurnDrawModel.FullHealth]) && healthOwner.ResourceEffects!.Single().ParamInt == 1 &&
                before.Relics.Where(owner => owner == openingOwner || owner == healthOwner).All(owner => owner.IsCovenant == false && owner.ResourceEffects!.All(effect => effect.Conditions.Count == 0 && !effect.CanApplyInPreviewMode)),
                "Original turn draw owners changed.");
            bool placement = row.GetProperty("Placement").GetBoolean();
            RelicResourceResult predicted;
            if (row.GetProperty("Kind").GetString() == "PostOpening")
            {
                Require(!placement && before.QueryFrame?.Turn == 1, "Post-opening callback ran outside initial combat turn.");
                predicted = RelicTurnDrawModel.PostOpening(before, placement);
                Require(predicted.Supported && predicted.Value > 0, "Missing positive native post-opening draw."); opening++;
                Equal(RelicTurnDrawModel.PostOpening(before, placement: true).Context, before, "deployment gate");
                Equal(RelicTurnDrawModel.PostOpening(before, preview: true).Context, before, "preview gate");
            }
            else
            {
                int hp = row.GetProperty("Health").GetInt32(); var maximum = RelicResourceModel.GetPyreMaxHealth(before);
                predicted = RelicTurnDrawModel.BeforeDraw(before, hp, placement);
                Require(!row.GetProperty("Returned").GetBoolean(), "Native full-health Test should return false after updating its value.");
                int expectedDraw = Math.Clamp(before.Resources!.BaseDraw + (hp == maximum.Value ? 1 : 0), 1, before.MaxHandSize);
                Require(predicted.Supported && RelicResourceModel.GetDrawCount(predicted.Context!).Value == expectedDraw, "Full-health mutable draw value differs.");
                if (hp == maximum.Value) full++; else if (hp == 0) zero++; else partial++;
                if (row.GetProperty("Phase").GetString() == "natural") natural++;
                Equal(RelicTurnDrawModel.BeforeDraw(before, hp, preview: true).Context, before, "health preview gate"); health++;
            }
            Require(predicted.Supported, "Turn draw unsupported: " + predicted.UnsupportedReason); Equal(predicted.Context, actual, "native callback");
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            Parallel.For(0, 16, _ => {
                var branch = row.GetProperty("Kind").GetString() == "PostOpening" ? RelicTurnDrawModel.PostOpening(before, placement) :
                    RelicTurnDrawModel.BeforeDraw(before, row.GetProperty("Health").GetInt32(), placement);
                Require(branch.Supported, "Parallel draw branch unsupported."); Equal(branch.Context, actual, "parallel callback");
            });
            Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Draw callback mutated its parent.");
            var absent = before.WithRelics([]);
            Equal(RelicTurnDrawModel.PostOpening(absent).Context, absent, "removed opening owner");
            Equal(RelicTurnDrawModel.BeforeDraw(absent, 0).Context, absent, "removed health owner");
        }
        Require(opening == 1 && health >= 8 && partial >= 2 && full >= 3 && zero >= 2 && natural >= 1, "Missing original draw/control evidence.");
        BattleActionChecks.Native(root);
        Console.WriteLine($"ORIGINAL-TURN-DRAW-NATIVE PASS: two unchanged owners, {opening} natural post-opening and {health} full-health Test callbacks, partial/zero/full controls, ordered mutable draw values, preview/removed-owner controls and 16 parallel branches; independent full policy replay.");
    }
    private static void Equal<T>(T predicted, T actual, string label)
    {
        string a = JsonSerializer.Serialize(predicted, ModelJson.Options), b = JsonSerializer.Serialize(actual, ModelJson.Options);
        Require(a == b, "Turn draw " + label + " differs: " + ModelJson.Difference(a, b));
    }
    private static void Require(bool pass, string error) { if (!pass) throw new InvalidOperationException(error); }
}
