using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class AngelDuoMoonBirthChecks
{
    internal static void Native(FixtureValue source, FixtureValue birth, FixtureValue? replay = null, FixtureValue? replayBirth = null)
    {
        const string reason = "Outer-boss instantiation requires the calibrated original definition without creation modifiers or moon tracking.";
        var rows = NativeBranchObservationChecks.Replay(source, reason);
        Require(rows.Length == 2 && rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Missing natural champion/first end-turn prefix.");
        OuterBossBirthChecks.Native(birth);
        var input = birth.GetProperty("Births")[0].GetProperty("Instantiation").Deserialize<OuterBossInstantiationState>()!;
        var created = birth.GetProperty("Births")[0].GetProperty("Before").Deserialize<OuterBossCreationState>()!;
        Require(input.Battle.MoonPhaseTrackingActive == true && input.Battle.MoonPhase == "Full" &&
            created.CreatedUnit is { Id: 5, AssetKey: "Boss_Level6_AngelDuo_Entropic_Variant1_ResolveHealth", MoonPhaseMask: 6,
                EndsBattleOnDeath: false } && created.CreatedUnit.BumpRules!.PairedBossUnitId == 3 &&
            created.CreatedUnit.StatusRegistry!.Select(status => status.Id).SequenceEqual(["titanskin", "fullmoon"]) &&
            created.CreatedUnit.Status("titanskin")?.Stacks == 10 && created.CreatedUnit.Status("fullmoon")?.Stacks == 1,
            "Lost original moon initialization before pairing, status registration order or companion identity.");
        foreach (string control in new[] { "Moon", "Variant", "MoonTrigger" })
        {
            var node = JsonSerializer.SerializeToNode(input, ModelJson.Options)!;
            if (control == "Moon") node["Battle"]!["Spawn"]!["Train"]!["Context"]!["QueryFrame"]!["MoonPhase"] = 1;
            if (control == "Variant") node["Definition"]!["Actor"]!["Unit"]!["AssetKey"] = "Boss_Level6_AngelDuo_Entropic_Variant2_IncantHealth";
            if (control == "MoonTrigger") node["Definition"]!["Actor"]!["Unit"]!["Triggers"]![0]!["Kind"] = "OnMoonLit";
            Require(!OuterBossInstantiationModel.Apply(node.Deserialize<OuterBossInstantiationState>(ModelJson.Options)!).Supported,
                "Uncalibrated angel moon birth accepted: " + control);
        }
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var context = actual.Spawn.Train.Context!;
        Require(context.Cards.Hand.Any(token => CardMoonUpgradeModel.Present(context.FindCard(token.InstanceId)!)),
            "Expected naturally drawn moon-upgrade card to exercise the relic compatibility gate.");
        foreach (string control in new[] { "TriggerOrder", "Propagation", "CardSequence" })
        {
            var node = JsonSerializer.SerializeToNode(context, ModelJson.Options)!;
            var owners = node["Relics"]!.AsArray();
            string asset = control == "TriggerOrder" ? "Enemy_Boss_Level6_AngelDuo_LoopAttackResolveHealth" :
                control == "Propagation" ? "StartOfTurnPropagate" : "GainBonusOnThreeCardsPlayed";
            var owner = owners.Single(item => item!["AssetKey"]!.GetValue<string>() == asset)!;
            if (control == "TriggerOrder") owner["TriggerAdditions"]![0]!["EffectIndex"] = 1;
            if (control == "Propagation") owner["ResourceEffects"]![0]!["StartTurnRoomEffect"]!["Action"]!["Value"] = 2;
            if (control == "CardSequence") owner["ResourceEffects"]![0]!["ParamInt"] = 2;
            CardMoonUpgradeModel.RefreshHand(node.Deserialize<CombatContext>(ModelJson.Options)!, out string? error);
            Require(error != null, "Moon refresh accepted malformed independent relic: " + control);
        }
        if (replay.HasValue)
        {
            NativeBranchObservationChecks.Replay(replay.Value, reason);
            NativeBranchObservationChecks.Pair(source, replay.Value);
            Require(replayBirth.HasValue, "Independent birth witness required.");
            OuterBossBirthChecks.Native(replayBirth.Value);
        }
        Console.WriteLine("ANGEL-DUO-MOON-BIRTH PASS:2 native inputs/full state/RNG, off-train full-moon status and mask6, " +
            "titan-skin registration then paired identities5/3, six birth/relic rejection controls; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
