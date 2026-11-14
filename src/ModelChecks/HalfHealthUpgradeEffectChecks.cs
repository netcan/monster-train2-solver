using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class HalfHealthUpgradeEffectChecks
{
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path);
        FixtureValue root = document.RootElement;
        FixtureValue[] samples = root.GetProperty("Samples").EnumerateArray().ToArray();
        Require(root.GetProperty("Schema").GetInt32() == 1 && root.GetProperty("GameVersion").GetString() == "2.3.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            root.GetProperty("Boundary").GetString() == "NativeHalfHealthUnitUpgradeEffects" &&
            root.GetProperty("LiveContextUnchanged").GetBoolean() && root.GetProperty("EffectsDrained").GetBoolean() &&
            root.GetProperty("RestorationDifference").ValueKind == FixtureKind.Null && root.GetProperty("Mismatches").GetInt32() == 0 &&
            samples.Length == 28 && samples.All(sample => sample.GetProperty("Completed").GetBoolean() &&
                sample.GetProperty("Difference").ValueKind == FixtureKind.Null && sample.GetProperty("GameplayRngUnchanged").GetBoolean() &&
                sample.GetProperty("PendingAfterDrain").GetInt32() == 0), "Incomplete native half-health upgrade-effect capture.");
        Require(Difference(root.GetProperty("RngBefore"), root.GetProperty("RngAfter")) == null, "Upgrade effects did not restore every RNG stream.");
        foreach (FixtureValue sample in samples) Verify(sample);
        var byLabel = samples.ToDictionary(sample => sample.GetProperty("Label").GetString()!);
        Expect("temporary-add-healed-loss", 12, 22, 0, 5, 5, 1, 1);
        Expect("permanent-add-healed-loss", 12, 22, 0, 5, 5, 1, 0, 1);
        Expect("death-only-add-healed-loss", 12, 22, 0, 5, 5, 1, 0);
        Expect("temporary-add-unhealed-gain", 11, 22, 0, 5, 5, 1, 1);
        Expect("temporary-add-two-phase-loss-gain", 12, 26, 0, 10, 10, 1, 1);
        Expect("temporary-add-negative-gain", 9, 18, 0, 5, 5, 1, 1);
        Expect("temporary-add-gain-heals", 12, 22, 0, 15, 15, 1, 1);
        Expect("temporary-add-loss-removes-before-write", 12, 20, 0, 5, 5, 0, 1);
        Expect("permanent-add-loss-removes-before-write", 12, 20, 0, 5, 5, 0, 0, 1);
        Expect("temporary-add-gain-buffs-max", 12, 23, 0, 10, 10, 1, 1, healthOffset: 1);
        Expect("temporary-remove-healed-loss", 10, 18, 2, 0, 5, 0, 0);
        Expect("permanent-remove-keeps-permanent-card-record", 10, 18, 2, 0, 5, 0, 0, 1);
        Expect("temporary-remove-two-copies", 10, 16, 2, 0, 5, 0, 0);
        Expect("temporary-remove-two-negative-copies", 14, 24, 0, 5, 5, 0, 0);
        Expect("temporary-remove-unhealed-gain", 11, 22, 2, 0, 5, 0, 0);
        Expect("temporary-remove-loss-readds-after-write", 10, 18, 2, 0, 5, 0, 0,
            finalHp: 12, finalMax: 20, finalUnitRecords: 1, finalTemporary: 1);
        Expect("temporary-remove-restoration-readds-before-write", 10, 20, 0, 10, 10, 1, 0);
        Expect("unique-repeat-stale-half", 10, 20, 0, 0, 0, 1, 1);
        Expect("excluded-clone-noop", 10, 20, 0, 0, 0, 0, 0);
        Expect("unique-add-loss", 12, 22, 0, 5, 5, 1, 1);
        Expect("remove-missing-unit-clears-card", 10, 20, 0, 0, 0, 0, 0);
        Expect("temporary-add-cannotheal", 11, 22, 0, 5, 5, 1, 1);
        Expect("temporary-add-heal-multiplier", 14, 23, 0, 5, 5, 1, 1);
        Expect("temporary-add-ignores-heal-immunity", 12, 22, 0, 5, 5, 1, 1);
        Expect("temporary-remove-zero-stale-half", 10, 20, 0, 0, 0, 0, 0);
        Expect("death-only-remove-loss", 10, 18, 2, 0, 5, 0, 0);
        Expect("temporary-add-loss-damages-before-second", 11, 26, 0, 15, 15, 1, 1);
        Expect("temporary-remove-ledger-gain-loss", 11, 18, 4, 0, 10, 0, 0);
        string[] damageKinds = Kinds("temporary-add-loss-damages-before-second");
        Require(damageKinds.SequenceEqual(new[] { "OnDeathwishLost", "OnDeathwishLost", "OnBloodshed", "OnHit", "OnPunish", "OnDeathwish", "OnDeathwish" }),
            "Native friendly self-damage callback ordering differs.");
        FixtureValue hit = byLabel["temporary-add-loss-damages-before-second"];
        Require(hit.GetProperty("Dispatches").EnumerateArray().Single(item => item.GetProperty("Kind").GetString() == "OnBloodshed")
            .GetProperty("OverrideTargetId").GetInt32() == hit.GetProperty("ActorId").GetInt32(), "Bloodshed lost its damaged-character target.");
        RoomCombatState guarded = samples[0].GetProperty("Before").Deserialize<RoomCombatState>()!;
        Require(RoomCombatModel.Validate(guarded) == null, "Calibrated half-health battle scheduling was rejected.");
        int actorId = samples[0].GetProperty("ActorId").GetInt32();
        CombatUnit actor = guarded.Units.Single(unit => unit.Id == actorId);
        foreach (string kind in new[] { "OnBloodshed", "OnPunish" })
        {
            var trigger = new CombatTrigger(kind, false, false, false, 1, Array.Empty<CombatEffect>(), skipDuringDeployment: false,
                stateId: actor.NextTriggerId);
            CombatUnit changed = actor.WithTriggerAllocation(actor.Triggers.Concat(new[] { trigger }).ToArray(), actor.NextTriggerId!.Value + 1);
            RoomCombatState state = guarded.WithUnits(guarded.Units.Select(unit => unit.Id == actorId ? changed : unit).ToArray());
            Require(RoomCombatModel.Validate(state, halfHealthLifecycle: true) == null,
                "Calibrated authored " + kind + " was rejected in the versioned damage lifecycle.");
        }
        Parallel.For(0, 32, _ => { foreach (FixtureValue sample in samples) Verify(sample); });
        Console.WriteLine("HALF-HEALTH-UPGRADE-EFFECT PASS: 28 native effects at API/queue-drain boundaries, all three lifetimes, duplicate removal, unique/clone controls, nested upgrade removal/readdition across spawner writes, complete dispatch/RNG/state and 32 immutable branches. Trigger installation and lethal upgrades remain guarded; whole-card/battle acceptance is separate.");

        void Expect(string label, int hp, int max, int queued, int apiGold, int finalGold, int unitRecords, int temporary, int permanent = 0,
            int healthOffset = 0, int? finalHp = null, int? finalMax = null, int? finalUnitRecords = null, int? finalTemporary = null)
        {
            FixtureValue sample = byLabel[label]; int id = sample.GetProperty("ActorId").GetInt32(), cardId = sample.GetProperty("SpawnerId").GetInt32();
            string upgradeId = sample.GetProperty("Upgrade").GetProperty("DataId").GetString()!;
            RoomCombatState before = sample.GetProperty("Before").Deserialize<RoomCombatState>()!;
            RoomCombatState api = sample.GetProperty("ApiActual").Deserialize<RoomCombatState>()!;
            RoomCombatState actual = sample.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            CombatUnit unit = api.Units.Single(item => item.Id == id), final = actual.Units.Single(item => item.Id == id);
            CardInstanceState card = api.Context!.FindCard(cardId)!, finalCard = actual.Context!.FindCard(cardId)!;
            Require(unit.SpawnerCardId == cardId && unit.Team == CombatTeam.Player && unit.Health == hp && unit.MaxHealth == max &&
                final.Health == (finalHp ?? hp) && final.MaxHealth == (finalMax ?? max) && sample.GetProperty("ApiCallbacks").GetArrayLength() == queued &&
                api.Context.Gold - before.Context!.Gold == apiGold && actual.Context.Gold - before.Context.Gold == finalGold &&
                unit.Modifiers!.Upgrades.Count(item => item.DataId == upgradeId) == unitRecords &&
                final.Modifiers!.Upgrades.Count(item => item.DataId == upgradeId) == (finalUnitRecords ?? unitRecords) &&
                card.Temporary.Upgrades.Count(item => item.DataId == upgradeId) == temporary &&
                finalCard.Temporary.Upgrades.Count(item => item.DataId == upgradeId) == (finalTemporary ?? temporary) &&
                card.Permanent.Upgrades.Count(item => item.DataId == upgradeId) == permanent &&
                finalCard.Permanent.Upgrades.Count(item => item.DataId == upgradeId) == permanent &&
                card.Temporary.Offsets.Health - before.Context.FindCard(cardId)!.Temporary.Offsets.Health == healthOffset,
                "Missing native upgrade effect/card-write control: " + label);
        }
        string[] Kinds(string label) => byLabel[label].GetProperty("Dispatches").EnumerateArray().Select(item => item.GetProperty("Kind").GetString()!).ToArray();
    }

    private static void Verify(FixtureValue sample)
    {
        string label = sample.GetProperty("Label").GetString()!;
        RoomCombatState before = sample.GetProperty("Before").Deserialize<RoomCombatState>()!;
        CardUpgradeModifier upgrade = sample.GetProperty("Upgrade").Deserialize<CardUpgradeModifier>()!;
        FixtureValue input = sample.GetProperty("Input"); string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        RoomCombatResult api = UnitModifierModel.Apply(before, sample.GetProperty("ActorId").GetInt32(), upgrade,
            input.GetProperty("Lifetime").GetString()!, input.GetProperty("Remove").GetBoolean(), captureAllDispatches: true);
        Require(api.Supported, label + ": " + api.UnsupportedReason);
        Require(Difference(api.State, sample.GetProperty("ApiActual")) == null, label + " API state: " + Difference(api.State, sample.GetProperty("ApiActual")));
        Require(Difference(api.Dispatches, sample.GetProperty("ApiDispatches")) == null, label + ": API dispatch payload/order differs.");
        Require(Difference(api.PendingCallbacks.Select(EnchantmentCallback.From).ToArray(), sample.GetProperty("ApiCallbacks")) == null, label + ": API queue differs.");
        string intermediate = JsonSerializer.Serialize(api.State, ModelJson.Options);
        RoomCombatResult drained = RoomCombatModel.DrainCharacterCallbacks(api.State!, api.PendingCallbacks, true);
        Require(drained.Supported, label + ": " + drained.UnsupportedReason);
        Require(Difference(drained.State, sample.GetProperty("Actual")) == null, label + " drained state: " + Difference(drained.State, sample.GetProperty("Actual")));
        Require(Difference(api.Dispatches.Concat(drained.Dispatches).ToArray(), sample.GetProperty("Dispatches")) == null, label + ": drained dispatch payload/order differs.");
        foreach (string phase in new[] { "RngAfterApi", "RngAfter" })
            Require(ModelJson.Difference(HalfHealthLifecycleChecks.GameplayRng(sample.GetProperty("RngBefore")),
                HalfHealthLifecycleChecks.GameplayRng(sample.GetProperty(phase))) == null, label + ": gameplay or Unity RNG changed at " + phase);
        Require(JsonSerializer.Serialize(before, ModelJson.Options) == parent && JsonSerializer.Serialize(api.State, ModelJson.Options) == intermediate,
            label + ": upgrade effect or drain mutated a parent state.");
    }
    private static string? Difference(object? predicted, FixtureValue actual) => ModelJson.Difference(JsonSerializer.Serialize(predicted, ModelJson.Options), JsonSerializer.Serialize(actual.ToObjectGraph()));
    private static string? Difference(RoomCombatState? predicted, FixtureValue actual) => ModelJson.Difference(JsonSerializer.Serialize(predicted, ModelJson.Options), JsonSerializer.Serialize(actual.Deserialize<RoomCombatState>(), ModelJson.Options));
    private static string? Difference(FixtureValue left, FixtureValue right) => ModelJson.Difference(JsonSerializer.Serialize(left.ToObjectGraph()), JsonSerializer.Serialize(right.ToObjectGraph()));
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
