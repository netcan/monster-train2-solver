using System;
using System.Linq;
using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalExtraFeedTriggerChecks
{
    private const string RelicId = "b6613fe4-13c6-4462-8a0b-9af42850a851";
    private const string RelicKey = "ExtraFeedTrigger";
    private const string Effect = "RelicEffectModifyTriggerCount";
    private const string OnFeed = "OnFeed";

    internal static void Run()
    {
        CombatContext context = Context();
        CombatUnit player = Unit(1, CombatTeam.Player);
        CombatUnit enemy = Unit(2, CombatTeam.Enemy);
        Require(RelicModel.Validate(context) == null, "The captured ExtraFeedTrigger owner was rejected.");
        Require(Query(context, player, OnFeed) == 2 && Query(context, player, "OnDeath") == 1,
            "The original owner did not add exactly one Monsters OnFeed count.");
        Require(Query(context, enemy, OnFeed) == 1,
            "The Monsters-only owner changed an enemy OnFeed count.");
        Require(Query(context, player, OnFeed, blocked: true) == 1,
            "A trigger that blocks count modifiers received the relic bonus.");

        TriggerCountState cache = context.TriggerCounts!;
        Require(cache.Modifiers.Count == 1 && cache.Modifiers[0].Kind == OnFeed && cache.Modifiers[0].Value == 1 &&
            cache.EnemyAllowedKinds.Count == 0 && cache.ExcludedCardTriggers.Count == 0,
            "The registration cache does not match the exact original owner.");
        string before = JsonSerializer.Serialize(context, ModelJson.Options);
        Parallel.For(0, 32, _ =>
        {
            Require(Query(context, player, OnFeed) == 2 && Query(context, enemy, OnFeed) == 1,
                "Parallel immutable ExtraFeedTrigger queries differed.");
        });
        Require(JsonSerializer.Serialize(context, ModelJson.Options) == before,
            "ExtraFeedTrigger queries mutated their parent context.");
        Console.WriteLine("ORIGINAL-EXTRA-FEED-TRIGGER-CHECKS PASS: Monsters OnFeed +1, non-target trigger/team and blocked-count controls, exact registration cache and immutable parallel queries.");
    }

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 199 &&
            root.GetProperty("ModifierScenario").GetString() == "original-extra-feed-trigger" &&
            root.GetProperty("OriginalExtraFeedTriggerPrepared").GetBoolean() &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }
                .All(name => root.GetProperty(name).GetInt32() == 0),
            "The original ExtraFeedTrigger battle is incomplete or has native/model failures.");

        FixtureValue evidence = root.GetProperty("OriginalExtraFeedTriggerEvidence");
        Require(evidence.GetProperty("OwnerId").GetString() == RelicId &&
            evidence.GetProperty("AssetKey").GetString() == RelicKey &&
            evidence.GetProperty("Effect").GetString() == Effect &&
            evidence.GetProperty("SourceTeam").GetString() == "Monsters" &&
            evidence.GetProperty("RequiredSubtype").GetString() == "SubtypesData_None" &&
            evidence.GetProperty("Trigger").GetString() == OnFeed &&
            evidence.GetProperty("CountModifier").GetInt32() == 1 &&
            !evidence.GetProperty("AllTriggers").GetBoolean() &&
            !evidence.GetProperty("AppliesToHeroes").GetBoolean() &&
            evidence.GetProperty("ExcludedCardTriggers").GetArrayLength() == 0 &&
            evidence.GetProperty("ConditionCount").GetInt32() == 0 &&
            evidence.GetProperty("PlayerUnitId").GetInt32() != 0 &&
            evidence.GetProperty("PlayerOnFeedCount").GetInt32() == 2 &&
            evidence.GetProperty("PlayerOnDeathCount").GetInt32() == 1,
            "The acquired ExtraFeedTrigger owner or direct native count-query controls changed.");

        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(actions.Length > 0, "The native ExtraFeedTrigger fixture has no battle decisions.");
        BattleTurnState first = actions[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        CombatContext context = first.Spawn.Train.Context!;
        ValidateContext(context);

        int stages = 0;
        foreach (FixtureValue item in root.GetProperty("Stages").EnumerateArray())
        {
            RoomCombatState before = item.GetProperty("Before").Deserialize<RoomCombatState>()!;
            RoomCombatState actual = item.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            ValidateContext(before.Context!);
            RoomCombatResult predicted = item.GetProperty("Kind").GetString() == "Exchange"
                ? RoomCombatModel.Exchange(before) : RoomCombatModel.Resolve(before);
            Require(predicted.Supported && Serialize(predicted.State) == Serialize(actual),
                "Independent ExtraFeedTrigger combat-stage replay differs: " + predicted.UnsupportedReason);
            stages++;
        }
        Require(stages > 0, "The native ExtraFeedTrigger fixture has no independently replayed combat stages.");
        BattleActionChecks.Native(root);
        Console.WriteLine($"NATIVE-EXTRA-FEED-TRIGGER PASS: exact owner/cache, direct native Monsters OnFeed 2 vs OnDeath 1, {stages} matching combat stages; no natural OnFeed dispatch was captured.");
    }

    private static void ValidateContext(CombatContext context)
    {
        Require(RelicModel.Validate(context) == null && context.Relics!.Count(relic => relic.DataId == RelicId &&
            relic.AssetKey == RelicKey && relic.EffectTypes.SequenceEqual([Effect])) == 1 &&
            context.TriggerCounts!.Modifiers.Count == 1 && context.TriggerCounts.Modifiers[0].Kind == OnFeed &&
            context.TriggerCounts.Modifiers[0].Value == 1 && !context.TriggerCounts.EnemyAllowedKinds.Contains(OnFeed) &&
            context.TriggerCounts.ExcludedCardTriggers.Count == 0,
            "Captured ExtraFeedTrigger owner or registration cache differs from the original catalog.");
        Require(Query(context, Unit(900, CombatTeam.Player), OnFeed) == 2 &&
            Query(context, Unit(901, CombatTeam.Player), "OnDeath") == 1 &&
            Query(context, Unit(902, CombatTeam.Enemy), OnFeed) == 1,
            "The captured trigger cache does not preserve team and trigger gates.");
    }

    private static CombatContext Context()
    {
        UnityRng rng = UnityRng.Seed(17);
        return new(new([], [], [], rng, 0, []), rng, 0, 1, 10,
            relics: [new(RelicId, RelicKey, [Effect])],
            triggerCounts: new([new(OnFeed, 1)], [], [], []));
    }

    private static CombatUnit Unit(int id, CombatTeam team) => new(id, "actor", team, 0, 20, 20,
        false, false, false, [], []);

    private static int Query(CombatContext context, CombatUnit unit, string kind, bool blocked = false)
    {
        CombatTrigger trigger = new(kind, false, false, true, 1, [], false, noCountModifiersAllowed: blocked);
        int count = TriggerCountModel.Query(context, unit, trigger, out string? error);
        Require(error == null, "Trigger count query failed: " + error);
        return count;
    }

    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
