using System.Diagnostics;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class BattleStateKeyChecks
{
    internal static void Run(FixtureValue fixture)
    {
        // Frozen with the old .NET 8 implementation, from the accepted original
        // Ring7 default-loss capture. These cover deployment, combat and boss
        // turns in strict, preview-allocator and floor-browsing key modes.
        // Keys include closed generic runtime type names and are only compared
        // within one runtime; these .NET 8 golden values are not native keys.
        int[] indexes = [0, 7, 14, 24];
        string[][] expected = [
            ["46B396323564180166D8E2E2B9030AF3B201F44BC928F15DAC13C35CEA374241", "279A13AF1EA05D9306C4DDE5F5E406C6AA7B016880BD3E4A0BBD2DF96223E85B", "EB7E8C32A268DB72385F6D95FC31D061E5E4619C11871BB47F4587B1F01DD2DE"],
            ["2F0A03E81B67B0395ECF2C0B5F2D3EBC627C5111FA3F1AA3658271EE73D66C41", "67E5FF70E5FF0F163A9942BA4BD986E22FE5A9053246090D50B780B6E16E8280", "E89CBDADA896C5D70344ECAC554ADD35DCDF93155AEF327322470F870A7FDA2A"],
            ["2D8162C0F90F02C1FB0202CC3E6939D413DFCBE04E1D08698122FD704F5E5E97", "1FCC6137000675F051492A4DE2FE6A3ED82C7E0545F56C412C9A0017C2B2EAA1", "B4219E3D265D412763FBA2EB5195E85F5ADF8DF87351392483B8418DD5FF3C10"],
            ["9D55FACECEC67BE300E4ACB1E59E64C136AF089534DE7CDF4CB7B2FECC1B6E04", "271262220BE16671D8256BC967DAE6E2B1C7E15183BD66BDA95D4D9FA0C81B99", "8FB050582B129E20CB020827294514FE2613E6B24EDD41FAB6FB9C2A9C3B8B01"]
        ];
        BattleTurnState[] states = indexes.Select(index => fixture.GetProperty("RollingDecisions")[index]
            .GetProperty("Root").Deserialize<BattleTurnState>()!).ToArray();
        Parallel.For(0, states.Length * 3 * 2, new ParallelOptions { MaxDegreeOfParallelism = 2 }, sample =>
        {
            int index = sample / 3 % states.Length, mode = sample % 3;
            using var keys = new BattleStateKeys(mode > 0, mode == 2);
            string actual = keys.Key(states[index]);
            if (actual != expected[index][mode])
                throw new InvalidOperationException($"State key changed its frozen format: sample={index}, mode={mode}, actual={actual}.");
        });
        var watch = Stopwatch.StartNew();
        for (int index = 0; index < 20; index++)
        {
            using var keys = new BattleStateKeys();
            if (keys.Key(states[0]) != expected[0][0]) throw new InvalidOperationException("A repeated capture changed its key.");
        }
        Console.WriteLine($"STATE-KEY PASS: 12 frozen pre-optimization keys, concurrent independent instances, all three modes; 20 warm fresh-root keys={watch.Elapsed.TotalMilliseconds:F1}ms.");
    }
}
