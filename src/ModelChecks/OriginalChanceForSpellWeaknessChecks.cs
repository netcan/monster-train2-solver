using System;
using System.Linq;
using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalChanceForSpellWeaknessChecks
{
    private const string RelicId = "98989cbb-c729-4f87-8240-08f448db96d8";
    private const string RelicKey = "ChanceForSpellWeakness";

    internal static void Run()
    {
        CombatRelicState relic = Relic();
        Require(RelicModel.Validate([relic]) == null, "The captured original chance relic was rejected.");
        Func<CombatRelicState, CombatRelicState>[] updates =
        [
            state => state.WithSoloUnitUpgrades([]), state => state.WithSpawnStatuses([]),
            state => state.WithRoomEntryStatuses([]), state => state.WithCardModifiers([]),
            state => state.WithTriggerAdditions([]), state => state.WithStatusStackAdditions([]),
            state => state.WithStatusMagnitudes([]), state => state.WithResourceEffects([]),
            state => state.WithMagicPowerModifiers([]), state => state.WithCardPlayMagicPowerModifiers([]),
            state => state.WithUnitUpgradeTriggers([]), state => state.WithForgePointModifiers([]),
            state => state.WithCardHealthModifiers([]), state => state.WithCardAttackModifiers([]),
            state => state.WithCardDrawOnCardPlayModifiers([]), state => state.WithStatusAttackModifiers([]),
            state => state.WithCardPlayRoomCapacityModifiers([]), state => state.WithMoonPhaseHealRules([]),
            state => state.WithRoomAttachmentReplications([]), state => state.WithSummonDamageRules([]),
            state => state.WithCheaperSpellUpgradeModifiers([]), state => state.WithUnstableRebalances([]),
            state => state.WithCopyCardOnCardPlayModifiers([]), state => state.WithMonsterSpellTargetingModifiers([]),
            state => state.WithCardPlayCorruptionModifiers([])
        ];
        string payload = JsonSerializer.Serialize(relic.AfterSpawnChanceRules);
        foreach (Func<CombatRelicState, CombatRelicState> update in updates)
        {
            CombatRelicState updated = update(relic);
            Require(RelicModel.Validate([updated]) == null &&
                JsonSerializer.Serialize(updated.AfterSpawnChanceRules) == payload,
                "An immutable relic update discarded or changed its after-spawn chance payload.");
        }
        CombatStatus expectedStatus = RelicAfterSpawnChanceStatusModel.SpellWeakness(1);
        Require(RelicAfterSpawnChanceStatusModel.IsOriginalSpellWeakness(expectedStatus, 1),
            "The native Spell Weakness status definition was not retained.");

        int hitSeed = FindSeed(hit: true);
        int missSeed = FindSeed(hit: false);
        RoomCombatState hitBefore = Room(relic, Unit(1, CombatTeam.Enemy), hitSeed);
        RngDraw hitDraw = hitBefore.Context!.BattleRng.Range(0, 100);
        RoomCombatResult hit = Fire(hitBefore, "AfterSpawnEnchant");
        CombatUnit hitActor = hit.State!.Units.Single(unit => unit.Id == 1);
        Require(hit.Supported && hitDraw.Value < 50 && hit.State.Context!.BattleRng.Equals(hitDraw.State) &&
            hitActor.Status("spell weakness")?.Stacks == 1 &&
            hitActor.RegisteredStatus("spell weakness") is CombatStatus registered &&
            RelicAfterSpawnChanceStatusModel.IsOriginalSpellWeakness(registered, 1),
            "A successful native 50% roll did not add one fully defined Spell Weakness stack.");

        RoomCombatState missBefore = Room(relic, Unit(1, CombatTeam.Enemy), missSeed);
        RngDraw missDraw = missBefore.Context!.BattleRng.Range(0, 100);
        RoomCombatResult miss = Fire(missBefore, "AfterSpawnEnchant");
        Require(miss.Supported && missDraw.Value >= 50 && miss.State!.Context!.BattleRng.Equals(missDraw.State) &&
            miss.State.Units.Single().Status("spell weakness") == null,
            "A failed native 50% roll changed status state or consumed the wrong RNG amount.");

        Skipped("other trigger", Room(relic, Unit(1, CombatTeam.Enemy), 29), "OnSpawn");
        Skipped("friendly Monster", Room(relic, Unit(1, CombatTeam.Player), 29), "AfterSpawnEnchant");
        Skipped("preview", Room(relic, Unit(1, CombatTeam.Enemy), 29, preview: true), "AfterSpawnEnchant");
        Skipped("Untouchable", Room(relic, Unit(1, CombatTeam.Enemy, new CombatStatus("untouchable", 1)), 29), "AfterSpawnEnchant");
        Skipped("destroyed", Room(relic, Unit(1, CombatTeam.Enemy, Array.Empty<CombatStatus>(),
            new UnitDeathState(false, false, false, isDestroyed: true)), 29), "AfterSpawnEnchant");
        Skipped("removed owner", Room(null, Unit(1, CombatTeam.Enemy), 29), "AfterSpawnEnchant");

        CombatRelicState missing = new(RelicId, RelicKey, [RelicAfterSpawnChanceStatusModel.Effect],
            isCovenant: false, disallowedInPlacementPhase: false);
        CombatRelicState otherOwner = new("00000000-0000-0000-0000-000000000001", "ChanceToCleanse",
            [RelicAfterSpawnChanceStatusModel.Effect], isCovenant: false, disallowedInPlacementPhase: false);
        Require(RelicModel.Validate([missing]) != null && RelicModel.Validate([otherOwner]) != null &&
            RelicModel.Validate([Relic(targetCardType: "Invalid")]) != null &&
            RelicModel.Validate([Relic(multiplier: 1f)]) != null &&
            RelicModel.Validate([Relic(status: new CombatStatus("spell weakness", 1))]) != null,
            "A missing, altered, or other-owner after-spawn chance payload was silently accepted.");

        string parent = JsonSerializer.Serialize(hitBefore);
        Parallel.For(0, 16, _ =>
        {
            RoomCombatResult branch = Fire(hitBefore, "AfterSpawnEnchant");
            Require(branch.Supported && JsonSerializer.Serialize(branch.State) == JsonSerializer.Serialize(hit.State),
                "Parallel immutable after-spawn chance branch differed.");
        });
        Require(JsonSerializer.Serialize(hitBefore) == parent, "After-spawn chance mutated its parent state.");
        Console.WriteLine($"ORIGINAL-CHANCE-FOR-SPELL-WEAKNESS-CHECKS PASS: deterministic 50% hit/miss, exact Spell Weakness metadata, one Battle RNG draw, trigger/team/preview/Untouchable/destroyed/removed-owner controls, unknown-owner rejection, all {updates.Length} relic-copy updates and 16 immutable branches.");
    }

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 196 &&
            root.GetProperty("ModifierScenario").GetString() == "original-chance-for-spell-weakness" &&
            root.GetProperty("OriginalChanceForSpellWeaknessPrepared").GetBoolean() &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }
                .All(name => root.GetProperty(name).GetInt32() == 0),
            "The original ChanceForSpellWeakness battle is incomplete or has native/model failures.");

        FixtureValue definition = root.GetProperty("OriginalChanceForSpellWeaknessDefinition");
        Require(definition.GetProperty("OwnerId").GetString() == RelicId &&
            definition.GetProperty("AssetKey").GetString() == RelicKey &&
            definition.GetProperty("Effect").GetString() == RelicAfterSpawnChanceStatusModel.Effect &&
            definition.GetProperty("ChancePercent").GetInt32() == 50 &&
            definition.GetProperty("RoomRestriction").GetInt32() == 0 &&
            definition.GetProperty("TargetTeam").GetString() == "Heroes" &&
            definition.GetProperty("RequiredSubtype").GetString() == "SubtypesData_None" &&
            definition.GetProperty("ExcludedSubtypes").GetArrayLength() == 0 &&
            definition.GetProperty("ConditionCount").GetInt32() == 0 &&
            !definition.GetProperty("CanApplyInPreviewMode").GetBoolean() &&
            definition.GetProperty("CardEffect").GetString() == "CardEffectAddStatusEffect" &&
            definition.GetProperty("CardEffectTarget").GetString() == "Self" &&
            definition.GetProperty("CardEffectTeam").GetString() == "Heroes" &&
            definition.GetProperty("StatusId").GetString() == "spell weakness" &&
            definition.GetProperty("StatusStacks").GetInt32() == 1,
            "The native ChanceForSpellWeakness owner or its nested effect payload changed.");

        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(actions.Length > 0, "The native chance relic fixture has no captured battle decisions.");
        CombatContext context = actions[0].GetProperty("Before").GetProperty("Spawn").GetProperty("Train")
            .GetProperty("Context").Deserialize<CombatContext>()!;
        CombatRelicState relic = context.Relics!.Single(item => item.DataId == RelicId);
        RelicAfterSpawnChanceRule rule = relic.AfterSpawnChanceRules!.Single();
        Require(relic.AssetKey == RelicKey && relic.EffectTypes.SequenceEqual([RelicAfterSpawnChanceStatusModel.Effect]) &&
            RelicModel.Validate([relic]) == null && rule.EffectIndex == 0 && rule.ChancePercent == 50 &&
            rule.RoomRestriction == 0 && rule.TargetTeam == "Heroes" && rule.HasRequiredSubtype &&
            rule.RequiredSubtypeIsNone && rule.ExcludedSubtypes.Count == 0 && rule.ConditionCount == 0 &&
            rule.RequiredSubtype == "SubtypesData_None" && !rule.CanApplyInPreviewMode && rule.CardEffectHasOnlyCapturedPayload &&
            rule.CardEffectPayloadIssues.Count == 0 && rule.CardEffectTargetCardType == "Spell" &&
            rule.CardEffectParamMultiplier == 0f &&
            rule.StatusEffects.Single().Id == "spell weakness" && rule.StatusEffects.Single().Stacks == 1,
            "The immutable native relic payload or full status definition was not captured for replay.");

        FixtureValue[] tests = root.GetProperty("RelicAfterSpawnChanceTests").EnumerateArray().ToArray();
        FixtureValue[] eligible = tests.Where(IsEligible).ToArray();
        int hits = 0, misses = 0, gated = 0;
        foreach (FixtureValue row in tests)
        {
            bool canRoll = IsEligible(row);
            uint[] beforeWords = Words(row, "BattleRngBefore");
            uint[] afterWords = Words(row, "BattleRngAfter");
            Require(row.GetProperty("SpellWeaknessAfter").GetInt32() == row.GetProperty("SpellWeaknessBefore").GetInt32(),
                "Native Test mutated Spell Weakness before its Apply coroutine.");
            if (canRoll)
            {
                RngDraw draw = ToRng(beforeWords).Range(0, 100);
                Require(afterWords.SequenceEqual(Words(draw.State)) &&
                    row.GetProperty("Result").GetBoolean() == (draw.Value < 50),
                    "Native eligible AfterSpawnEnchant did not consume one matching Battle RNG roll.");
                if (draw.Value < 50) hits++; else misses++;
            }
            else
            {
                Require(beforeWords.SequenceEqual(afterWords) && !row.GetProperty("Result").GetBoolean(),
                    "Native nonmatching callback returned true or consumed Battle RNG.");
                gated++;
            }
        }
        Require(eligible.Length == 7 && hits == 4 && misses == 3 && gated > 0,
            "The natural spawn capture lacks both hit and miss witnesses or callback gates.");

        FixtureValue[] applications = root.GetProperty("RelicAfterSpawnChanceApplications").EnumerateArray().ToArray();
        FixtureValue[] successful = eligible.Where(row => row.GetProperty("Result").GetBoolean()).ToArray();
        Require(applications.Length == successful.Length && applications.Length == 4,
            "Native successful chance tests and completed Apply coroutines do not correspond one-to-one.");
        foreach (FixtureValue application in applications)
        {
            FixtureValue test = successful.Single(row => row.GetProperty("UnitId").GetInt32() == application.GetProperty("UnitId").GetInt32() &&
                row.GetProperty("RoomIndex").GetInt32() == application.GetProperty("RoomIndex").GetInt32());
            int before = application.GetProperty("SpellWeaknessBefore").GetInt32();
            Require(application.GetProperty("Completed").GetBoolean() &&
                before == test.GetProperty("SpellWeaknessBefore").GetInt32() &&
                application.GetProperty("SpellWeaknessAfter").GetInt32() == before + 1 &&
                Words(application, "BattleRngBefore").SequenceEqual(Words(test, "BattleRngAfter")) &&
                Words(application, "BattleRngAfter").SequenceEqual(Words(application, "BattleRngBefore")),
                "A successful native chance callback failed to add exactly one stack or changed RNG during Apply.");
        }

        BattleActionChecks.Native(root);
        Console.WriteLine($"NATIVE-CHANCE-FOR-SPELL-WEAKNESS PASS: unchanged owner, {eligible.Length} eligible rolls ({hits} hits/{misses} misses), {gated} gated tests with no RNG, four exact self-applications and zero battle differences.");
    }

    private static bool IsEligible(FixtureValue row) =>
        row.GetProperty("Trigger").GetString() == "AfterSpawnEnchant" &&
        row.GetProperty("Team").GetString() == "Heroes" &&
        !row.GetProperty("IsDestroyed").GetBoolean() && !row.GetProperty("Untouchable").GetBoolean();

    private static uint[] Words(FixtureValue row, string key)
    {
        uint[] words = row.GetProperty(key).Deserialize<uint[]>()!;
        Require(words.Length == 4, "Native Battle RNG did not capture four state words.");
        return words;
    }

    private static uint[] Words(UnityRng rng) => [rng.S0, rng.S1, rng.S2, rng.S3];
    private static UnityRng ToRng(uint[] words) => new(words[0], words[1], words[2], words[3]);

    private static int FindSeed(bool hit)
    {
        for (int seed = 1; seed < 10000; seed++)
            if ((UnityRng.Seed(seed).Range(0, 100).Value < 50) == hit) return seed;
        throw new InvalidOperationException("Could not find a deterministic chance test seed.");
    }

    private static RoomCombatResult Fire(RoomCombatState room, string trigger) =>
        RoomCombatModel.ApplyQueuedCharacterTrigger(room,
            new RoomCombatModel.QueuedCharacterTrigger(room.RoomIndex, room.Units.Single(), trigger), _ => { });

    private static void Skipped(string label, RoomCombatState room, string trigger)
    {
        RoomCombatResult result = Fire(room, trigger);
        Require(result.Supported && result.State!.Context!.BattleRng.Equals(room.Context!.BattleRng) &&
            result.State.Units.All(unit => unit.Status("spell weakness") == null),
            "The " + label + " gate applied Spell Weakness or consumed Battle RNG.");
    }

    private static RoomCombatState Room(CombatRelicState? relic, CombatUnit actor, int seed, bool preview = false)
    {
        UnityRng rng = UnityRng.Seed(seed);
        CombatRelicState[] relics = relic == null ? [] : [relic];
        var context = new CombatContext(new CardCycleState([], [], [], rng, 0, []), rng, 0, 0, 10, relics: relics);
        return new RoomCombatState(0, false, [actor], [], context, preview: preview);
    }

    private static CombatUnit Unit(int id, CombatTeam team, params CombatStatus[] statuses) =>
        Unit(id, team, statuses, null);

    private static CombatUnit Unit(int id, CombatTeam team, CombatStatus[] statuses, UnitDeathState? deathState) =>
        new(id, "chance-probe-" + id, team, 0, 20, 20, false, false, false, statuses,
            deathState: deathState, statusRegistry: statuses, statusDictionary: new StatusDictionaryState([], []));

    private static CombatRelicState Relic(string targetCardType = "Spell", float multiplier = 0f,
        CombatStatus? status = null) => new(RelicId, RelicKey,
        [RelicAfterSpawnChanceStatusModel.Effect], isCovenant: false, disallowedInPlacementPhase: false,
        afterSpawnChanceRules: [new RelicAfterSpawnChanceRule(0, 50, 0, "Heroes", true, true, "SubtypesData_None",
            [], 0, false, 1, true, [], "CardEffectAddStatusEffect", "Self", "Heroes", targetCardType, multiplier, 0, 0,
            false, false, false, [status ?? RelicAfterSpawnChanceStatusModel.SpellWeakness(1)])]);

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
