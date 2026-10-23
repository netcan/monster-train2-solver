using MonsterTrain2Poju.Model;

internal static class TrueFinalBossCovenantMatrixChecks
{
    internal static void Native(IEnumerable<string> paths)
    {
        string[] assets = { "Covenant00_FTUE", "Covenant01_StrongerEnemiesMoreCards",
            "Covenant04_StrongerBosses", "Covenant08_StrongerEnemies" };
        var observed = new HashSet<int>();
        foreach (string path in paths)
        {
            using var fixture = ModelJson.ReadFixture(path);
            TrueFinalBossSpawnChecks.Native(fixture.RootElement);
            TrueFinalBossSpawnState before = fixture.RootElement.GetProperty("Before").Deserialize<TrueFinalBossSpawnState>()!;
            CombatRelicState[] owners = before.Battle.Spawn.Train.Context!.Relics!.Where(owner => owner.IsCovenant == true).ToArray();
            int mask = 0;
            foreach (CombatRelicState owner in owners)
            {
                int index = Array.IndexOf(assets, owner.AssetKey);
                TrueFinalBossChecks.Require(index >= 0, "Matrix contains an unknown Covenant.");
                mask |= 1 << index;
            }
            TrueFinalBossChecks.Require(observed.Add(mask), "Matrix repeats a Covenant subset.");
        }
        TrueFinalBossChecks.Require(observed.SetEquals(Enumerable.Range(1, 15)),
            "Matrix must include every nonempty subset of the four calibrated original Covenants.");
        Console.WriteLine("TRUE-FINAL-BOSS-COVENANT-MATRIX PASS: all 15 nonempty subsets, pure native birth/state/RNG, 960 immutable branches and rejected missing/spoofed/unknown/duplicate definitions. Full battle acceptance is separate.");
    }
}
