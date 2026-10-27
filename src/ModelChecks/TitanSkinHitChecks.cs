using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TitanSkinHitChecks
{
    // The initial request's corruption hypothesis is retained as provenance.
    private const string Reason = "Native end-turn status decay differs after killing a corruption unit.";

    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason, historicalBoundaryMismatch: (old, native) => {
            Require(Boss(old).Status("titanskin")?.Stacks == 40 && Boss(native).Status("titanskin")?.Stacks == 36 &&
                !Boss(old).Triggers.Single().HasTriggered && Boss(native).Triggers.Single().HasTriggered,
                "Preserve the historical40 versus native36 Titanskin witness.");
            Require(old.Spawn.Train.Context!.Statistics!.LastAttackDamageDealt == 49 &&
                native.Spawn.Train.Context!.Statistics!.LastAttackDamageDealt == 52,
                "Preserve the resulting next-floor preview's last-attack statistic.");
            JsonNode node = JsonSerializer.SerializeToNode(old, ModelJson.Options)!;
            JsonNode context = node["Spawn"]!["Train"]!["Context"]!;
            context["Statistics"]!["LastAttackDamageDealt"] = 52;
            foreach (var room in node["Spawn"]!["Train"]!["Rooms"]!.AsArray())
            {
                room!["Context"] = context.DeepClone();
                foreach (var unit in room!["Units"]!.AsArray().Where(unit => unit!["Id"]!.GetValue<int>() == 18))
                {
                    unit!["Triggers"]![0]!["HasTriggered"] = true;
                    foreach (string field in new[] { "Statuses", "StatusRegistry" })
                        foreach (var status in unit![field]!.AsArray().Where(status => status!["Id"]!.GetValue<string>() == "titanskin"))
                            status!["Stacks"] = 36;
                }
            }
            string normalized = NativeBranchObservationChecks.Key(node.Deserialize<BattleTurnState>(ModelJson.Options)!);
            string actual = NativeBranchObservationChecks.Key(native);
            Require(normalized == actual, "Historical mismatch extends beyond Titanskin Revenge: " + ModelJson.Difference(normalized, actual));
        });
        Require(rows.Length == 41 && rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Wrong native Titanskin hit boundary.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var after = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var boss = Boss(before);
        Require(before.Spawn.Turn == 7 && after.Spawn.Turn == 8 && boss.AssetKey == "Boss_Level5_Titanskin" &&
            boss.Health == 900 && Boss(after).Health == 900 && boss.Status("titanskin")?.Stacks == 40 &&
            Boss(after).Status("titanskin")?.Stacks == 36 && boss.Triggers.Count == 1 &&
            boss.Triggers[0].Kind == "OnHit" && boss.Triggers[0].Effects.Single().Type == "CardEffectRemoveStatusEffect",
            "Original Titanskin boss must lose four layers through Revenge without HP damage.");
        string immutable = NativeBranchObservationChecks.Key(before);
        Parallel.For(0, 4, _ => {
            var result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(after),
                "Titanskin hit differs from complete native state/forecasts/RNG.");
        });
        Require(immutable == NativeBranchObservationChecks.Key(before), "Titanskin hit mutated its parent.");

        // Use the exact captured Revenge payload with a single ordinary attack.
        CombatUnit Target(bool revenge) => new(1, boss.AssetKey, CombatTeam.Enemy, 0, 100, 100, false, false, false,
            new[] { boss.Status("titanskin")! }, revenge ? boss.Triggers : Array.Empty<CombatTrigger>(),
            modifiers: new(0, 0, 0, 0, 0, true, false, [], damageBuffFromStatusEffects: 0), nextTriggerId: boss.NextTriggerId);
        foreach (var control in new[] { (Damage: 0, Revenge: true, Skin: 40, Hp: 100),
            (Damage: 5, Revenge: true, Skin: 39, Hp: 100), (Damage: 45, Revenge: true, Skin: 39, Hp: 95),
            (Damage: 5, Revenge: false, Skin: 40, Hp: 100) })
        {
            var attacker = new CombatUnit(2, "control", CombatTeam.Player, control.Damage, 100, 100, true, false, false, [],
                modifiers: new(control.Damage, 0, 0, 0, 0, true, false, [], damageBuffFromStatusEffects: 0));
            var result = RoomCombatModel.Exchange(new RoomCombatState(0, false, new[] { Target(control.Revenge), attacker }, []));
            Require(result.Supported && result.State!.Units.Single(unit => unit.Id == 1) is var target &&
                target.Health == control.Hp && target.Status("titanskin")?.Stacks == control.Skin,
                "Zero hit/full soak/partial soak/passive-only control failed: " + control +
                "; unsupported=" + result.UnsupportedReason + "; target=" + string.Join(",", result.State?.Units.Where(unit => unit.Id == 1)
                    .Select(unit => unit.Health + "/" + unit.Status("titanskin")?.Stacks) ?? []));
        }
        TitanSkinChecks.Run();
        if (replay.HasValue)
        {
            NativeBranchObservationChecks.Replay(replay.Value, Reason);
            NativeBranchObservationChecks.Pair(source, replay.Value, originalPredictionPresent: true);
        }
        Console.WriteLine("TITAN-SKIN-HIT PASS:41 native inputs/full state/forecasts/RNG; four Revenge layers/zero HP damage; four immutable branches and four hit controls; passive shield/armor/piercing/magnitude controls; independentPair=" + replay.HasValue);
    }

    private static CombatUnit Boss(BattleTurnState state) => state.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == 18);
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
