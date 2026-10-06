using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class RngChecks
{
    internal static void Run(string path)
    {
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
        if (!document.RootElement.GetProperty("GlobalStateRestored").GetBoolean())
            throw new InvalidOperationException("Calibration changed the Unity RNG state.");
        int count = 0;
        foreach (JsonElement sample in document.RootElement.GetProperty("Samples").EnumerateArray())
        {
            UnityRng seed = UnityRng.Seed(sample.GetProperty("Seed").GetInt32());
            if (!seed.Equals(State(sample.GetProperty("Initial"))))
                throw new InvalidOperationException("Unity RNG initialization differs.");
            foreach (JsonElement draw in sample.GetProperty("Draws").EnumerateArray())
            {
                RngDraw predicted = State(draw.GetProperty("Before")).Range(draw.GetProperty("Min").GetInt32(),
                    draw.GetProperty("Max").GetInt32());
                if (predicted.Value != draw.GetProperty("Value").GetInt32() ||
                    !predicted.State.Equals(State(draw.GetProperty("After"))))
                    throw new InvalidOperationException("Unity RNG differs at native sample " + count);
                count++;
            }
        }
        Console.WriteLine("NATIVE-RNG-CHECKS PASS: " + count + " integer draws and their complete states.");
    }

    private static UnityRng State(JsonElement state) => new(state[0].GetUInt32(), state[1].GetUInt32(),
        state[2].GetUInt32(), state[3].GetUInt32());
}
