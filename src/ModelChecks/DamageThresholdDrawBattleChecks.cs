using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class DamageThresholdDrawBattleChecks
{
    private const string RevengeId = "2adea00a-87e9-4a45-9822-de04104ae108";
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path);
        FixtureValue root = document.RootElement;
        Require(root.GetProperty("GameVersion").GetString() == "2.3.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            root.GetProperty("ModifierScenario").GetString() == "damage-threshold-draw-battle" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            root.GetProperty("TerminalCaptureBoundary").GetString() == "AfterStopCombatLoop", "Missing native versioned threshold battle terminal.");
        foreach (string counter in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(counter).GetInt32() == 0, "Incomplete threshold battle: " + counter);
        FixtureValue evidence = root.GetProperty("DamageThresholdDrawBattle");
        int[] cards = evidence.GetProperty("ControlCardIds").EnumerateArray().Select(item => item.GetInt32()).ToArray();
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(evidence.GetProperty("Prepared").GetBoolean() && evidence.GetProperty("CompletedControlPlays").GetInt32() == 9 &&
            cards.Length == 9 && cards.Distinct().Count() == 9 && actions.Length == 9 && actions.Select(row => row.GetProperty("Action")
                .GetProperty("CardInstanceId").GetInt32()).SequenceEqual(cards), "Missing controlled native threshold plays.");
        CombatContext initial = actions[0].GetProperty("Before").Deserialize<BattleTurnState>()!.Spawn.Train.Context!;
        CombatRelicState owner = initial.Relics!.Single(item => item.DataId == RelicCardDrawOnHealModel.DamageThresholdId);
        Require(RelicCardDrawOnHealModel.Validate(owner, owner.ResourceEffects!.Single()) == null &&
            initial.Relics!.Single(item => item.DataId == RevengeId).EffectTypes.SequenceEqual(new[] { "RelicEffectModifyTriggerCount" }) &&
            initial.TriggerCounts!.Modifiers.Single(item => item.Kind == "OnHit").Value == 1 &&
            initial.TriggerCounts.Modifiers.All(item => item.Kind == "OnHit" || item.Value == 0), "Wrong original owner/count cache.");
        FixtureValue[] definitions = evidence.GetProperty("OwnerDefinitions").EnumerateArray().ToArray();
        Require(definitions.Length == 2, "Expected both original relic definitions.");
        FixtureValue revenge = definitions.Single(row => row.GetProperty("Id").GetString() == RevengeId);
        Require(revenge.GetProperty("Key").GetString() == "RevengeTriggerTwice" && !revenge.GetProperty("Placement").GetBoolean() &&
            revenge.GetProperty("Type").GetString() == "RelicEffectModifyTriggerCount" && revenge.GetProperty("Int").GetInt32() == 1 &&
            revenge.GetProperty("Int2").GetInt32() == 0 && revenge.GetProperty("Trigger").GetString() == "OnHit" &&
            revenge.GetProperty("Team").GetString() == "Monsters" && !revenge.GetProperty("AllTriggers").GetBoolean() &&
            !revenge.GetProperty("Heroes").GetBoolean() && revenge.GetProperty("Conditions").GetInt32() == 0 &&
            !revenge.GetProperty("Excluded").EnumerateArray().Any(), "Revenge owner metadata changed.");
        int[] damage = { 0, 0, 0, 3, 9, 10, 11, 11, 21 };
        for (int index = 0; index < actions.Length; index++)
        {
            BattleTurnState before = actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
            BattleTurnState after = actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            Require(after.Spawn.Train.Context!.Statistics!.MonsterDamage!.FriendlyThisTurn == damage[index], "Controlled friendly damage differs at " + index);
            int added = after.Spawn.Train.Context.Cards.Hand.Count(card => !before.Spawn.Train.Context!.Cards.Hand.Any(old => old.InstanceId == card.InstanceId));
            Require(added == (index == 5 ? 1 : 0), "Threshold/continued damage/heal/recross draw differs at " + index);
            // Native positive gold rewards have a five-gold minimum per effect.
            if (index >= 3) Require(after.Spawn.Train.Context.Gold - before.Spawn.Train.Context!.Gold == (index == 7 ? 5 : 10),
                "Executed OnHit twice/OnHeal once differs at " + index);
        }
        BattleTurnState deployed = actions[2].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        foreach (CombatUnit unit in deployed.Spawn.Train.Rooms[2].Units.Where(unit => cards.Take(3).Contains(unit.SpawnerCardId)))
            Require(unit.Triggers.Single(trigger => trigger.Kind == "OnHit").FireCount == 2 &&
                unit.Triggers.Single(trigger => trigger.Kind == "OnHeal").FireCount == 1 &&
                unit.Triggers.Single(trigger => trigger.Kind == "OnTurnBegin").FireCount == 1, "Hit-only count registration differs.");
        int draws = 0, suppressed = 0, below = 0, wrong = 0, enemies = 0;
        var drawTurns = new HashSet<int>(); bool guardsChecked = false;
        FixtureValue[] callbacks = root.GetProperty("DamageThresholdDrawCallbacks").EnumerateArray().ToArray();
        foreach (FixtureValue row in callbacks)
        {
            Require(row.GetProperty("Completed").GetBoolean() && row.GetProperty("Actual").ValueKind == FixtureKind.Object &&
                row.GetProperty("CaptureError").ValueKind == FixtureKind.Null && row.GetProperty("UnsupportedReason").ValueKind == FixtureKind.Null &&
                row.GetProperty("Difference").ValueKind == FixtureKind.Null, "Incomplete native threshold callback.");
            CombatContext before = row.GetProperty("Before").Deserialize<CombatContext>()!;
            CombatContext actual = row.GetProperty("Actual").Deserialize<CombatContext>()!;
            CombatUnit actor = row.GetProperty("Actor").Deserialize<CombatUnit>()!;
            string kind = row.GetProperty("Trigger").GetString()!;
            RelicCardDrawOnHealResult result = RelicCardDrawOnHealModel.Apply(before, actor, kind, row.GetProperty("Placement").GetBoolean());
            Require(result.Supported, result.UnsupportedReason ?? "Threshold callback unsupported.");
            Equal(result.Context, actual, "complete threshold callback");
            RelicConditionState condition = before.Relics!.Single(item => item.DataId == owner.DataId).ResourceEffects!.Single().Conditions.Single();
            bool eligible = kind == "OnHit" && actor.Team == CombatTeam.Player && !actor.IsPyre;
            bool positive = eligible && !condition.Triggered && before.Statistics!.MonsterDamage!.FriendlyThisTurn >= 10;
            Require(result.DrawnCardIds.Count == (positive ? 1 : 0), "Unexpected native draw admission.");
            if (positive)
            {
                draws++; Require(drawTurns.Add(before.QueryFrame!.Turn ?? -1), "Repeated draw within the same turn.");
                RelicConditionState recorded = actual.Relics!.Single(item => item.DataId == owner.DataId).ResourceEffects!.Single().Conditions.Single();
                Require(recorded.Triggered && recorded.DurationTriggerCount == 2 && recorded.ValueAtLastTrigger == before.Statistics!.MonsterDamage!.FriendlyThisTurn,
                    "Original effect/manager double notification differs.");
                if (!guardsChecked) { Guards(before, actor, result); guardsChecked = true; }
            }
            else if (eligible && condition.Triggered) suppressed++;
            else if (eligible) below++;
            else if (actor.Team == CombatTeam.Enemy) enemies++;
            else wrong++;
        }
        Require(draws >= 3 && drawTurns.Any(turn => turn > 0) && below > 0 && suppressed > 0 && wrong > 0 && enemies > 0 && guardsChecked,
            $"Missing threshold controls: draws={draws}, below={below}, suppressed={suppressed}, wrong={wrong}, enemy={enemies}.");
        FixtureValue[] transitions = evidence.GetProperty("Transitions").EnumerateArray().ToArray();
        Require(transitions.All(row => row.GetProperty("Completed").GetBoolean() && row.GetProperty("After").ValueKind == FixtureKind.Object),
            "Incomplete native trigger execution.");
        int Delta(FixtureValue row) => row.GetProperty("GoldAfter").GetInt32() - row.GetProperty("GoldBefore").GetInt32();
        bool Carrier(FixtureValue row) => cards.Take(3).Contains(row.GetProperty("Before").GetProperty("SpawnerCardId").GetInt32());
        Require(transitions.Any(row => Carrier(row) && !row.GetProperty("Preview").GetBoolean() && row.GetProperty("Kind").GetString() == "OnHit" && Delta(row) == 10) &&
            transitions.Any(row => Carrier(row) && !row.GetProperty("Preview").GetBoolean() && row.GetProperty("Kind").GetString() == "OnHeal" && Delta(row) == 5) &&
            transitions.Any(row => Carrier(row) && row.GetProperty("Preview").GetBoolean() && row.GetProperty("Kind").GetString() == "OnHit" && Delta(row) == 0),
            "Missing executed double-Hit, single-Heal or preview reward control.");
        Require(transitions.Where(row => row.GetProperty("Preview").GetBoolean()).All(row => Delta(row) == 0), "Preview changed gold.");
        RoomCombatChecks.NativeRoomOnly(path); TrainCombatChecks.Native(root); RecordedBattleChecks.Native(root);
        Console.WriteLine($"DAMAGE-THRESHOLD-DRAW-BATTLE PASS: two unchanged new relics, shared 9/10/11 threshold, heal/recross, {draws} draws in distinct turns, {suppressed} repeats suppressed, native double-Hit/single-Heal, complete battle/RNG and 16 immutable branches.");
    }
    private static void Guards(CombatContext source, CombatUnit actor, RelicCardDrawOnHealResult expected)
    {
        string parent = JsonSerializer.Serialize(source, ModelJson.Options);
        foreach (var item in new[] { (actor, "OnHeal", false), (actor, "OnHit", true),
            (new CombatUnit(9998, "enemy", CombatTeam.Enemy, 0, 10, 10, false, false, false, []), "OnHit", false),
            (new CombatUnit(9999, "pyre", CombatTeam.Player, 0, 10, 10, false, true, false, []), "OnHit", false) })
        {
            RelicCardDrawOnHealResult result = RelicCardDrawOnHealModel.Apply(source, item.Item1, item.Item2, preview: item.Item3);
            Require(result.Supported && result.DrawnCardIds.Count == 0, "Wrong-trigger/team/Pyre/preview guard failed.");
            Equal(result.Context, source, "guard state");
        }
        Require(!RelicCardDrawOnHealModel.Apply(source.WithStatistics(null), actor, "OnHit").Supported, "Unversioned damage threshold was accepted.");
        Parallel.For(0, 16, _ => {
            RelicCardDrawOnHealResult result = RelicCardDrawOnHealModel.Apply(source, actor, "OnHit");
            Require(result.Supported && result.DrawnCardIds.SequenceEqual(expected.DrawnCardIds), "Parallel draw choice differs.");
            Equal(result.Context, expected.Context, "parallel threshold draw");
        });
        Require(parent == JsonSerializer.Serialize(source, ModelJson.Options), "Threshold branches mutated their parent.");
    }
    private static void Equal<T>(T a, T b, string label)
    {
        string? difference = ModelJson.Difference(JsonSerializer.Serialize(a, ModelJson.Options), JsonSerializer.Serialize(b, ModelJson.Options));
        Require(difference == null, label + ": " + difference);
    }
    private static void Require(bool condition, string error) { if (!condition) throw new InvalidOperationException(error); }
}
