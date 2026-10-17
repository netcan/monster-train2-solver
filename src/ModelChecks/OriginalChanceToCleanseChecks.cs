using System;
using System.Linq;
using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalChanceToCleanseChecks
{
    private const string RelicId = "1e972d81-fc2f-4d4a-ade9-46edeb246dbd";
    private const string RelicKey = "ChanceToCleanse";
    private const string Effect = "RelicEffectChanceAddEffectAfterSpawn";

    internal static void Run()
    {
        CombatRelicState owner = Relic();
        Require(RelicModel.Validate([owner]) == null, "The captured ChanceToCleanse owner was rejected.");
        CombatStatus armor = new("armor", 4, stackable: true, hidden: false, displayCategory: "Positive");
        CombatStatus weakness = RelicAfterSpawnChanceStatusModel.SpellWeakness(2);
        UnityRng hitRng = Seed(true);
        RoomCombatState hitBefore = Room(owner, Unit(1, CombatTeam.Enemy, [armor, weakness]), hitRng);
        RoomCombatResult hit = Fire(hitBefore, "AfterSpawnEnchant");
        Require(hit.Supported && hit.State!.Context!.BattleRng.Equals(hitRng.Range(0, 100).State) &&
            hit.State.Units.Single().Status("armor") == null && hit.State.Units.Single().Status("spell weakness")?.Stacks == 2,
            "A 50% hit did not remove visible Positive statuses while retaining Negative statuses.");

        UnityRng missRng = Seed(false);
        RoomCombatState missBefore = Room(owner, Unit(2, CombatTeam.Enemy, [armor, weakness]), missRng);
        RoomCombatResult miss = Fire(missBefore, "AfterSpawnEnchant");
        Require(miss.Supported && miss.State!.Context!.BattleRng.Equals(missRng.Range(0, 100).State) &&
            miss.State.Units.Single().Status("armor")?.Stacks == 4,
            "A 50% miss failed to preserve statuses or consume the native Battle RNG draw.");

        Skipped("wrong trigger", Room(owner, Unit(3, CombatTeam.Enemy, [armor]), Seed(true)), "AfterAscension");
        Skipped("wrong team", Room(owner, Unit(4, CombatTeam.Player, [armor]), Seed(true)), "AfterSpawnEnchant");
        Skipped("preview", Room(owner, Unit(5, CombatTeam.Enemy, [armor]), Seed(true), preview: true), "AfterSpawnEnchant");
        Skipped("untouchable", Room(owner, Unit(6, CombatTeam.Enemy, [armor, new("untouchable", 1)]), Seed(true)), "AfterSpawnEnchant");
        Skipped("destroyed", Room(owner, Unit(7, CombatTeam.Enemy, [armor], destroyed: true), Seed(true)), "AfterSpawnEnchant");
        var unknown = new CombatRelicState("00000000-0000-0000-0000-000000000001", "OtherOwner", [Effect],
            isCovenant: false, disallowedInPlacementPhase: false,
            afterSpawnChanceRules: [owner.AfterSpawnChanceRules!.Single()]);
        Require(RelicModel.Validate([unknown]) != null, "An unknown owner with the cleanse effect was silently accepted.");

        string parent = JsonSerializer.Serialize(hitBefore);
        string expected = JsonSerializer.Serialize(hit.State);
        Parallel.For(0, 16, _ => Require(JsonSerializer.Serialize(Fire(hitBefore, "AfterSpawnEnchant").State) == expected,
            "Parallel immutable ChanceToCleanse branches differed."));
        Require(JsonSerializer.Serialize(hitBefore) == parent, "ChanceToCleanse mutated its parent state.");
        Console.WriteLine("ORIGINAL-CHANCE-TO-CLEANSE-CHECKS PASS: exact positive-only removal, deterministic 50% hit/miss and RNG, trigger/team/preview/Untouchable/destroyed/unknown-owner controls, immutable branches.");
    }

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 198 &&
            root.GetProperty("ModifierScenario").GetString() == "original-chance-to-cleanse" &&
            root.GetProperty("OriginalChanceToCleansePrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }
                .All(name => root.GetProperty(name).GetInt32() == 0),
            "The original ChanceToCleanse battle is incomplete or has native/model failures.");

        FixtureValue definition = root.GetProperty("OriginalChanceToCleanseDefinition");
        Require(definition.GetProperty("OwnerId").GetString() == RelicId &&
            definition.GetProperty("AssetKey").GetString() == RelicKey && definition.GetProperty("Effect").GetString() == Effect &&
            definition.GetProperty("ChancePercent").GetInt32() == 50 && definition.GetProperty("RoomRestriction").GetInt32() == 0 &&
            definition.GetProperty("TargetTeam").GetString() == "Heroes" &&
            definition.GetProperty("RequiredSubtype").GetString() == "SubtypesData_None" &&
            definition.GetProperty("ExcludedSubtypes").GetArrayLength() == 0 && definition.GetProperty("ConditionCount").GetInt32() == 0 &&
            !definition.GetProperty("CanApplyInPreviewMode").GetBoolean() &&
            definition.GetProperty("CardEffect").GetString() == "CardEffectRemoveAllStatusEffects" &&
            definition.GetProperty("CardEffectTarget").GetString() == "Self" && definition.GetProperty("CardEffectTeam").GetString() == "Heroes" &&
            definition.GetProperty("CardEffectCategory").GetInt32() == 0 && !definition.GetProperty("RemoveHalf").GetBoolean(),
            "The native ChanceToCleanse owner or its nested effect payload changed.");

        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(actions.Length > 0, "The native ChanceToCleanse fixture has no battle decisions.");
        CombatContext context = actions[0].GetProperty("Before").GetProperty("Spawn").GetProperty("Train")
            .GetProperty("Context").Deserialize<CombatContext>()!;
        CombatRelicState relic = context.Relics!.Single(item => item.DataId == RelicId);
        RelicAfterSpawnChanceRule rule = relic.AfterSpawnChanceRules!.Single();
        Require(relic.AssetKey == RelicKey && relic.EffectTypes.SequenceEqual([Effect]) && RelicModel.Validate([relic]) == null &&
            rule.ChancePercent == 50 && rule.CardEffect == "CardEffectRemoveAllStatusEffects" && rule.CardEffectParamInt == 0 &&
            !rule.CardEffectParamBool && rule.StatusEffects.Count == 0,
            "The captured ChanceToCleanse rule does not preserve its exact original payload.");

        FixtureValue[] tests = root.GetProperty("RelicAfterSpawnChanceTests").EnumerateArray().ToArray();
        FixtureValue[] eligible = tests.Where(IsEligible).ToArray();
        int hits = 0, misses = 0, gated = 0;
        foreach (FixtureValue row in tests)
        {
            uint[] before = Words(row, "BattleRngBefore");
            uint[] after = Words(row, "BattleRngAfter");
            Require(Serialize(row.GetProperty("StatusesBefore")) == Serialize(row.GetProperty("StatusesAfter")),
                "Native ChanceToCleanse Test mutated statuses before its Apply coroutine.");
            if (IsEligible(row))
            {
                RngDraw draw = ToRng(before).Range(0, 100);
                Require(after.SequenceEqual(Words(draw.State)) && row.GetProperty("Result").GetBoolean() == (draw.Value < 50),
                    "Native eligible ChanceToCleanse test did not consume one matching Battle RNG roll.");
                if (draw.Value < 50) hits++; else misses++;
            }
            else
            {
                Require(before.SequenceEqual(after) && !row.GetProperty("Result").GetBoolean(),
                    "A gated ChanceToCleanse test returned true or consumed RNG.");
                gated++;
            }
        }
        Require(eligible.Length > 0 && hits > 0 && misses > 0 && gated > 0,
            "The native capture lacks eligible hit/miss and gated witnesses.");

        FixtureValue[] applications = root.GetProperty("RelicAfterSpawnChanceApplications").EnumerateArray().ToArray();
        FixtureValue[] successful = eligible.Where(row => row.GetProperty("Result").GetBoolean()).ToArray();
        Require(applications.Length == successful.Length && applications.Length == hits,
            "Native successful ChanceToCleanse tests and completed applications do not correspond one-to-one.");
        int positiveRemovals = 0;
        foreach (FixtureValue application in applications)
        {
            FixtureValue test = successful.Single(row => row.GetProperty("UnitId").GetInt32() == application.GetProperty("UnitId").GetInt32() &&
                row.GetProperty("RoomIndex").GetInt32() == application.GetProperty("RoomIndex").GetInt32());
            CombatStatus[] before = application.GetProperty("StatusesBefore").Deserialize<CombatStatus[]>()!;
            CombatStatus[] after = application.GetProperty("StatusesAfter").Deserialize<CombatStatus[]>()!;
            Require(application.GetProperty("Completed").GetBoolean() &&
                Words(application, "BattleRngBefore").SequenceEqual(Words(test, "BattleRngAfter")) &&
                Words(application, "BattleRngAfter").SequenceEqual(Words(application, "BattleRngBefore")),
                "ChanceToCleanse Apply did not complete without additional Battle RNG.");
            positiveRemovals += Positive(before).Sum(status => status.Stacks) - Positive(after).Sum(status => status.Stacks);
        }
        Require(positiveRemovals > 0, "No successful native ChanceToCleanse application removed a visible Positive status.");

        foreach (FixtureValue stage in root.GetProperty("Stages").EnumerateArray())
        {
            RoomCombatState before = stage.GetProperty("Before").Deserialize<RoomCombatState>()!;
            RoomCombatState actual = stage.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            RoomCombatResult predicted = stage.GetProperty("Kind").GetString() == "Exchange"
                ? RoomCombatModel.Exchange(before) : RoomCombatModel.Resolve(before);
            Require(predicted.Supported && Serialize(predicted.State) == Serialize(actual),
                "Independent ChanceToCleanse combat-stage replay differed: " + predicted.UnsupportedReason);
        }
        BattleActionChecks.Native(root);
        Console.WriteLine($"NATIVE-CHANCE-TO-CLEANSE PASS: {eligible.Length} eligible 50% rolls ({hits} hits/{misses} misses), {gated} gated tests, {positiveRemovals} positive status stacks removed and zero battle differences.");
    }

    private static CombatRelicState Relic() => new(RelicId, RelicKey, [Effect], isCovenant: false,
        disallowedInPlacementPhase: false, afterSpawnChanceRules: [new RelicAfterSpawnChanceRule(0, 50, 0,
            "Heroes", true, true, "SubtypesData_None", [], 0, false, 1, true, [],
            "CardEffectRemoveAllStatusEffects", "Self", "Heroes", "Spell", 0f, 0, 0, false, false, false, [])]);

    private static RoomCombatState Room(CombatRelicState relic, CombatUnit actor, UnityRng rng, bool preview = false)
    {
        var context = new CombatContext(new([], [], [], rng, 0, []), rng, 0, 20, 10, relics: [relic]);
        return new RoomCombatState(0, false, [actor], [], context, preview: preview);
    }

    private static CombatUnit Unit(int id, CombatTeam team, CombatStatus[] statuses, bool destroyed = false) =>
        new(id, "chance-cleanse-check-" + id, team, 0, 20, 20, false, false, false, statuses,
            deathState: destroyed ? new UnitDeathState(false, false, false, isDestroyed: true) : null,
            statusRegistry: statuses, statusDictionary: new StatusDictionaryState(statuses.Select(status => status.Id).ToArray(), []));

    private static RoomCombatResult Fire(RoomCombatState room, string trigger) =>
        RoomCombatModel.ApplyQueuedCharacterTrigger(room,
            new RoomCombatModel.QueuedCharacterTrigger(room.RoomIndex, room.Units.Single(), trigger), _ => { });

    private static UnityRng Seed(bool hit)
    {
        for (int seed = 1; seed < 10000; seed++)
            if ((UnityRng.Seed(seed).Range(0, 100).Value < 50) == hit) return UnityRng.Seed(seed);
        throw new InvalidOperationException("Could not find a deterministic cleanse chance test seed.");
    }

    private static void Skipped(string label, RoomCombatState room, string trigger)
    {
        RoomCombatResult result = Fire(room, trigger);
        CombatUnit? after = result.State?.Units.FirstOrDefault(unit => unit.Id == room.Units.Single().Id);
        Require(result.Supported && result.State!.Context!.BattleRng.Equals(room.Context!.BattleRng) &&
            (after == null || after.Statuses.SequenceEqual(room.Units.Single().Statuses)),
            "The " + label + " gate applied a cleanse or consumed Battle RNG.");
    }

    private static bool IsEligible(FixtureValue row) => row.GetProperty("Trigger").GetString() == "AfterSpawnEnchant" &&
        row.GetProperty("Team").GetString() == "Heroes" && !row.GetProperty("IsDestroyed").GetBoolean() &&
        !row.GetProperty("Untouchable").GetBoolean();

    private static CombatStatus[] Positive(CombatStatus[] statuses) => statuses.Where(status => status.Stacks > 0 &&
        status.Hidden == false && status.DisplayCategory == "Positive").ToArray();

    private static uint[] Words(FixtureValue row, string key) =>
        row.GetProperty(key).Deserialize<uint[]>() ?? throw new InvalidOperationException("Missing captured RNG words.");
    private static uint[] Words(UnityRng rng) => [rng.S0, rng.S1, rng.S2, rng.S3];
    private static UnityRng ToRng(uint[] words) => new(words[0], words[1], words[2], words[3]);
    private static string Serialize(object? value) => JsonSerializer.Serialize(value);

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
