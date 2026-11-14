using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class DevotionDamageBattleChecks
{
    private const string VenerateId = "96c9ed37-5e59-4c4f-a914-ae7040d24755";
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path);
        FixtureValue root = document.RootElement;
        Require(root.GetProperty("GameVersion").GetString() == "2.3.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            root.GetProperty("ModifierScenario").GetString() == "devotion-damage-battle" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            root.GetProperty("TerminalCaptureBoundary").GetString() == "AfterStopCombatLoop", "Missing settled original Devotion damage battle.");
        foreach (string counter in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(counter).GetInt32() == 0, "Incomplete Devotion battle: " + counter);
        FixtureValue evidence = root.GetProperty("DevotionDamageBattle");
        int[] ids = evidence.GetProperty("ControlCardIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(evidence.GetProperty("Prepared").GetBoolean() && evidence.GetProperty("CompletedControlPlays").GetInt32() == 12 &&
            ids.Length == 12 && ids.Distinct().Count() == 12 && actions.Length == 12 && actions.Select(row => row.GetProperty("Action")
                .GetProperty("CardInstanceId").GetInt32()).SequenceEqual(ids), "Missing twelve ordered native controls.");
        BattleTurnState Before(int index) => actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int index) => actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatContext initial = Before(0).Spawn.Train.Context!;
        CombatRelicState owner = initial.Relics!.Single(item => item.DataId == RelicDevotionOnDamageModel.Id);
        Require(RelicDevotionOnDamageModel.Validate(owner, owner.ResourceEffects!.Single()) == null &&
            initial.QueryFrame!.Devotion is { Level: 0, Points: 0, FirstThreshold: 3, ThresholdIncrease: 3, PassiveStacksPerLevel: 2 },
            "Missing unchanged original Devotion owner/balance inputs.");
        FixtureValue definition = evidence.GetProperty("OwnerDefinition");
        Require(definition.GetProperty("Id").GetString() == owner.DataId && definition.GetProperty("Key").GetString() == RelicDevotionOnDamageModel.Key &&
            !definition.GetProperty("Placement").GetBoolean() && definition.GetProperty("Type").GetString() == RelicDevotionOnDamageModel.Effect &&
            definition.GetProperty("Int").GetInt32() == 1 && definition.GetProperty("Int2").GetInt32() == 10 &&
            definition.GetProperty("Team").GetString() == "None" && definition.GetProperty("Trigger").GetString() == "OnDeath" &&
            definition.GetProperty("Conditions").GetInt32() == 1, "Changed original owner metadata.");
        FixtureValue venerate = evidence.GetProperty("VenerateDefinition");
        Require(venerate.GetProperty("Id").GetString() == VenerateId && venerate.GetProperty("Key").GetString() == "Venerate" &&
            venerate.GetProperty("Cost").GetInt32() == 3 && venerate.GetProperty("Type").GetString() == "CardEffectGainDevotionPoints" &&
            venerate.GetProperty("Int").GetInt32() == 6 && venerate.GetProperty("PerLevel").GetInt32() == 0 &&
            venerate.GetProperty("Traits").EnumerateArray().Select(item => item.GetString()).SequenceEqual(["CardTraitExhaustState"]),
            "Changed original Venerate definition.");
        int[] damage = [0, 0, 0, 3, 9, 10, 11, 11, 36, 40, 50, 50];
        int[] points = [0, 0, 0, 0, 0, 1, 1, 1, 2, 2, 0, 0];
        int[] levels = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 2];
        for (int index = 0; index < actions.Length; index++)
        {
            CombatContext after = After(index).Spawn.Train.Context!;
            Require(after.Statistics!.MonsterDamage!.FriendlyThisBattle == damage[index] &&
                after.QueryFrame!.Devotion!.Level == levels[index] && after.QueryFrame.Devotion.Points == points[index],
                "Native threshold/remainder, single big-hit award or heal control differs at " + index);
        }
        Require(Before(11).Energy - After(11).Energy == 3 &&
            Before(11).Spawn.Train.Context!.Cards.Hand.Single(card => card.InstanceId == ids[11]).DataId == VenerateId &&
            !After(11).Spawn.Train.Context!.Cards.Hand.Any(card => card.InstanceId == ids[11]) &&
            After(11).Spawn.Train.Context.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(card => card.InstanceId == ids[11]),
            "Original Venerate cost, hand routing, Consume or gain-six level crossing differs.");

        int awards = 0, below = 0, wrong = 0, enemies = 0, later = 0, levelCrossings = 0;
        bool guardsChecked = false;
        FixtureValue[] callbacks = root.GetProperty("DevotionDamageCallbacks").EnumerateArray().ToArray();
        foreach (FixtureValue row in callbacks)
        {
            Require(row.GetProperty("Completed").GetBoolean() && row.GetProperty("Actual").ValueKind == FixtureKind.Object &&
                row.GetProperty("CaptureError").ValueKind == FixtureKind.Null && row.GetProperty("UnsupportedReason").ValueKind == FixtureKind.Null &&
                row.GetProperty("Difference").ValueKind == FixtureKind.Null, "Incomplete native Devotion manager callback.");
            CombatContext before = row.GetProperty("Before").Deserialize<CombatContext>()!;
            CombatContext actual = row.GetProperty("Actual").Deserialize<CombatContext>()!;
            CombatUnit actor = row.GetProperty("Actor").Deserialize<CombatUnit>()!;
            string trigger = row.GetProperty("Trigger").GetString()!;
            DevotionResult result = RelicDevotionOnDamageModel.Apply(before, actor, trigger, row.GetProperty("Placement").GetBoolean());
            Require(result.Supported, result.UnsupportedReason ?? "Devotion callback unsupported.");
            Equal(result.Context, actual, "complete native manager context");
            RelicConditionState condition = before.Relics!.Single(item => item.DataId == owner.DataId).ResourceEffects!.Single().Conditions.Single();
            bool eligible = trigger == "OnHit" && actor.Team == CombatTeam.Player && !actor.IsPyre;
            bool positive = eligible && before.Statistics!.MonsterDamage!.FriendlyThisBattle - condition.ValueAtLastTrigger >= 10;
            Require(Total(actual.QueryFrame!.Devotion!) - Total(before.QueryFrame!.Devotion!) == (positive ? 1 : 0),
                "Native Devotion callback awarded more than once or missed its delta threshold.");
            if (positive)
            {
                awards++; if (before.QueryFrame.Turn > 0) later++;
                if (actual.QueryFrame.Devotion!.Level > before.QueryFrame.Devotion!.Level) levelCrossings++;
                RelicConditionState recorded = actual.Relics!.Single(item => item.DataId == owner.DataId).ResourceEffects!.Single().Conditions.Single();
                Require(recorded.Triggered && recorded.DurationTriggerCount == unchecked(condition.DurationTriggerCount + 2) &&
                    recorded.ValueAtLastTrigger == before.Statistics.MonsterDamage.FriendlyThisBattle,
                    "Native body/manager notifications did not record the current battle damage twice.");
                if (!guardsChecked) { Guards(before, actor, result); guardsChecked = true; }
            }
            else if (eligible) below++;
            else if (actor.Team == CombatTeam.Enemy) enemies++;
            else wrong++;
        }
        Require(awards >= 4 && below > 0 && wrong > 0 && enemies > 0 && later > 0 && levelCrossings > 0 && guardsChecked,
            $"Missing native controls: awards={awards}, below={below}, wrong={wrong}, enemies={enemies}, later={later}, levels={levelCrossings}.");
        FixtureValue[] transitions = evidence.GetProperty("Transitions").EnumerateArray().ToArray();
        Require(transitions.Length > 0 && transitions.All(row => row.GetProperty("Completed").GetBoolean()), "Incomplete native trigger execution.");
        FixtureValue[] previews = transitions.Where(row => row.GetProperty("Preview").GetBoolean()).ToArray();
        Require(previews.Length > 0, "Missing real native preview controls.");
        foreach (FixtureValue row in previews) Equal(row.GetProperty("DevotionBefore").ToObjectGraph(), row.GetProperty("DevotionAfter").ToObjectGraph(), "preview Devotion");
        FixtureValue[] notifications = evidence.GetProperty("Notifications").EnumerateArray().ToArray();
        Require(notifications.Length >= 4 && notifications.Length % 2 == 0, "Missing original relic/Venerate level notifications.");
        for (int index = 0; index < notifications.Length; index += 2)
        {
            Require(notifications[index].GetProperty("Kind").GetString() == "Cards" && notifications[index + 1].GetProperty("Kind").GetString() == "Monsters" &&
                !notifications[index].GetProperty("Preview").GetBoolean() && !notifications[index + 1].GetProperty("Preview").GetBoolean(),
                "Native level notifications did not reach cards then monsters outside preview.");
            Equal(notifications[index].GetProperty("Devotion").ToObjectGraph(), notifications[index + 1].GetProperty("Devotion").ToObjectGraph(), "notification resource payload");
        }
        RoomCombatChecks.NativeRoomOnly(path); TrainCombatChecks.Native(root); RecordedBattleChecks.Native(root);
        Console.WriteLine($"DEVOTION-DAMAGE-BATTLE PASS: unchanged original owner and Venerate cost/Consume, 9/10/11 and 36/40/50, single big-hit awards, {awards} awards ({later} in later turns), {callbacks.Length} full manager callbacks, {previews.Length} preview controls, complete battle/RNG and 16 immutable branches.");
    }
    private static long Total(DevotionState state) => state.Points + 3L * state.Level * (state.Level + 1L) / 2;
    private static void Guards(CombatContext source, CombatUnit actor, DevotionResult expected)
    {
        string parent = JsonSerializer.Serialize(source, ModelJson.Options);
        foreach (var item in new[] { (actor, "OnHeal", false), (actor, "OnHit", true),
            (new CombatUnit(9998, "enemy", CombatTeam.Enemy, 0, 10, 10, false, false, false, []), "OnHit", false),
            (new CombatUnit(9999, "pyre", CombatTeam.Player, 0, 10, 10, false, true, false, []), "OnHit", false) })
        {
            DevotionResult result = RelicDevotionOnDamageModel.Apply(source, item.Item1, item.Item2, preview: item.Item3);
            Require(result.Supported && !result.LeveledUp, "Wrong-trigger/team/Pyre/preview guard failed.");
            Equal(result.Context, source, "guard context");
        }
        Require(!RelicDevotionOnDamageModel.Apply(source.WithStatistics(null), actor, "OnHit").Supported &&
            !RelicDevotionOnDamageModel.Apply(source.WithQueryFrame(new StatisticQueryFrame()), actor, "OnHit").Supported,
            "Missing damage or Devotion inputs were inferred.");
        Parallel.For(0, 32, _ => {
            DevotionResult result = RelicDevotionOnDamageModel.Apply(source, actor, "OnHit");
            Require(result.Supported && result.LeveledUp == expected.LeveledUp, "Parallel Devotion branch differs.");
            Equal(result.Context, expected.Context, "parallel Devotion context");
        });
        Require(parent == JsonSerializer.Serialize(source, ModelJson.Options), "Devotion branches mutated their parent.");
    }
    private static void Equal(object? a, object? b, string label)
    {
        string? difference = ModelJson.Difference(JsonSerializer.Serialize(a, ModelJson.Options), JsonSerializer.Serialize(b, ModelJson.Options));
        Require(difference == null, label + ": " + difference);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
