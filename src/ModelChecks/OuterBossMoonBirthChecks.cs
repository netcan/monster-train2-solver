using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OuterBossMoonBirthChecks
{
    internal static void Native(FixtureValue source, FixtureValue birth, FixtureValue? replay = null, FixtureValue? replayBirth = null)
    {
        const string reason = "Outer-boss instantiation requires the calibrated original definition without creation modifiers or moon tracking.";
        var rows = NativeBranchObservationChecks.Replay(source, reason);
        Require(rows.Length == 2 && rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Missing natural champion deployment/first end-turn boundary.");
        OuterBossBirthChecks.Native(birth);
        var before = birth.GetProperty("Births")[0].GetProperty("Before").Deserialize<OuterBossCreationState>()!;
        Require(before.Battle.MoonPhaseTrackingActive == true && before.Battle.MoonPhase == "Full" &&
            before.CreatedUnit is { Id: 5, AssetKey: "Boss_Level3_Fleshweaver_Corruption", MoonPhaseMask: 6 } &&
            before.CreatedUnit.Status("fullmoon")?.Stacks == 1 && before.CreatedUnit.Status("mass")?.Stacks == 5 &&
            before.CreatedUnit.StatusRegistry!.Select(status => status.Id).SequenceEqual(["mass", "fullmoon"]),
            "Lost natural off-train moon initialization, authored mass, shared identity or status registration order.");
        if (replay.HasValue)
        {
            NativeBranchObservationChecks.Replay(replay.Value, reason);
            NativeBranchObservationChecks.Pair(source, replay.Value);
            Require(replayBirth.HasValue, "Independent birth capture required.");
            OuterBossBirthChecks.Native(replayBirth!.Value);
        }
        Console.WriteLine("OUTER-BOSS-MOON-BIRTH PASS: two native inputs/full state/RNG, full-moon creation before initial action, " +
            "identity5 and mass/moon registration order; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
