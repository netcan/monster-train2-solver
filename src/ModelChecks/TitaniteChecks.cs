using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TitaniteChecks
{
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("ContextUnchanged").GetBoolean() &&
            root.GetProperty("FrameBefore").ContentEquals(root.GetProperty("FrameAfter")) &&
            root.GetProperty("RngBefore").ContentEquals(root.GetProperty("RngAfter")), "Native decision/RNG/frame changed.");
        Require(root.GetProperty("Stage").GetString() == "OnPreAttackedDamageShield", "Unexpected native Titanite stage.");
        int count = 0, reductions = 0, piercingControls = 0;
        foreach (var row in root.GetProperty("Rows").EnumerateArray())
        {
            int damage = row.GetProperty("Damage").GetInt32();
            bool piercing = row.GetProperty("Piercing").GetBoolean();
            bool triggered = row.GetProperty("Triggered").GetBoolean();
            Require(TitaniteModel.Adjust(damage, piercing) == row.GetProperty("Actual").GetInt32() &&
                triggered == (damage > 0 && !piercing), "Native Titanite damage mismatch.");
            count++; if (triggered) reductions++; if (piercing) piercingControls++;
        }
        Require(count == 24 && reductions == 10 && piercingControls == 12, "Incomplete native Titanite matrix.");
        Console.WriteLine("TITANITE PASS: 24 native damage operations, odd/even/zero/negative/float-rounding boundaries and card piercing; live decision/RNG/frame unchanged.");
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}
