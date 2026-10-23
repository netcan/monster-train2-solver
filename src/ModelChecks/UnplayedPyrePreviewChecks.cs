using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class UnplayedPyrePreviewChecks
{
    internal static void Native(FixtureValue fixture)
    {
        int witnesses = 0, actualStops = 0;
        var lethalHands = new HashSet<string>();
        string HandKey(BattleTurnState state) => state.Spawn.Turn + ":" + string.Join(",", state.Spawn.Train.Context!.Cards.Hand.Select(card => card.InstanceId));
        foreach (FixtureValue row in fixture.GetProperty("RollingDecisions").EnumerateArray())
        {
            if (!row.GetProperty("IsEndTurn").GetBoolean() || row.GetProperty("Actual").ValueKind == FixtureKind.Null) continue;
            var before = row.GetProperty("Root").Deserialize<BattleTurnState>()!;
            var actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            CombatContext context = actual.Spawn.Train.Context!;
            if (context.LifeLinks != null) continue;
            CombatUnit? pyre = actual.Spawn.Train.Rooms.SelectMany(room => room.Units).SingleOrDefault(unit => unit.IsPyre);
            if (pyre == null || pyre.Health == 0)
            {
                CombatUnit? live = before.Spawn.Train.Rooms.SelectMany(room => room.Units).SingleOrDefault(unit => unit.IsPyre);
                if (live?.Health > 0 && lethalHands.Contains(HandKey(before)) && before.Spawn.Train.Context!.Cards.Hand.Count >= 4 &&
                    before.Spawn.Train.Context.Cards.Hand.Count == context.Cards.Hand.Count && context.KillCamActivated == false &&
                    actual.Spawn.Turn == before.Spawn.Turn)
                {
                    string immutableRoot = System.Text.Json.JsonSerializer.Serialize(before, ModelJson.Options);
                    string terminal = BattleTurnChecks.Comparable(actual, terminal: true);
                    Parallel.For(0, 16, _ => {
                        BattleTurnResult stopped = BattleTurnModel.EndTurn(before);
                        Require(stopped.Supported && stopped.Outcome == RoomOutcome.PlayerDefeated &&
                            stopped.State!.Spawn.Train.Context!.KillCamActivated == false &&
                            stopped.State.Spawn.Train.Context.Cards.Hand.Count == context.Cards.Hand.Count &&
                            BattleTurnChecks.Comparable(stopped.State, terminal: true) == terminal &&
                            System.Text.Json.JsonSerializer.Serialize(before, ModelJson.Options) == immutableRoot,
                            "Actual unplayed Pyre death must retain its native hand and stop before kill-camera cleanup.");
                    });
                    actualStops++;
                }
            }
            if (pyre == null || pyre.Health <= 0) continue;
            int damage = 0, cards = 0;
            foreach (CardToken card in context.Cards.Hand)
            {
                CardPlayRule? original = actual.PlayRules?.Cards.FirstOrDefault(rule => rule.DataId == card.DataId);
                CardInstanceState? instance = context.FindCard(card.InstanceId);
                if (original == null || instance == null) continue;
                CardPlayRule rule = CardModifierModel.Resolve(original, instance);
                if (rule.OnUnplayedNegativeEffects.Count == 0) continue;
                Require(!rule.HasUnsupportedUnplayedTriggers && rule.OnUnplayedNegativeEffects.All(effect =>
                    effect.Type == "Damage" && effect.Target == "Pyre" && effect.Range == null && effect.Value > 0),
                    "Ordinary lethal preview witness must retain the original fixed negative Pyre damage.");
                damage += rule.OnUnplayedNegativeEffects.Sum(effect => effect.Value); cards++;
            }
            if (damage < pyre.Health) continue;
            FixtureValue phase = fixture.GetProperty("TrainPhases").EnumerateArray().Single(entry =>
                entry.GetProperty("Kind").GetString() == "Ascend" &&
                entry.GetProperty("Before").Deserialize<TrainCombatState>()!.Context!.QueryFrame!.Turn == before.Spawn.Turn);
            int prior = phase.GetProperty("Actual").Deserialize<TrainCombatState>()!.Context!.Statistics!.LastAttackDamageDealt;
            Require(context.Statistics!.LastAttackDamageDealt == prior && prior == pyre.Attack && cards >= 4,
                "Lethal ordinary unplayed preview must stop before room attacks and preserve the native prior attack statistic.");
            string immutable = System.Text.Json.JsonSerializer.Serialize(before, ModelJson.Options);
            string observed = BattleTurnChecks.Comparable(actual);
            Parallel.For(0, 16, _ => {
                BattleTurnResult next = BattleTurnModel.EndTurn(before);
                Require(next.Supported && BattleTurnChecks.Comparable(next.State!) == observed &&
                    System.Text.Json.JsonSerializer.Serialize(before, ModelJson.Options) == immutable,
                    "Lethal ordinary preview differs from the complete native state/RNG or changed a shared branch root.");
            });
            witnesses++;
            lethalHands.Add(HandKey(actual));
        }
        Require(witnesses > 0 && actualStops > 0, "Missing ordinary-battle lethal preview and actual unplayed-death witnesses.");
        Console.WriteLine($"UNPLAYED-PYRE-PREVIEW PASS: {witnesses} ordinary native lethal forecasts preserve prior attack damage; {actualStops} actual unplayed deaths retain the native hand; sixteen immutable full-turn branches each.");
    }
    private static void Require(bool condition, string reason) { if (!condition) throw new InvalidOperationException(reason); }
}
