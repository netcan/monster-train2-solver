using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class UnitSummonChecks
{
    internal static void Native(FixtureValue fixture)
    {
        if (!fixture.TryGetProperty("ModifierScenario", out var scenario) || scenario.GetString()?.StartsWith("multi-summon", StringComparison.Ordinal) != true) return;
        if (scenario.GetString()!.StartsWith("multi-summon-fresh", StringComparison.Ordinal))
        { FreshSummonChecks.Native(fixture); return; }
        var births = fixture.GetProperty("UnitBirths").EnumerateArray().ToArray();
        var clones = fixture.GetProperty("DetachedCardClones").EnumerateArray().ToArray();
        var phases = fixture.GetProperty("RallyPhases").EnumerateArray().ToArray();
        var triggers = fixture.GetProperty("RallyTriggers").EnumerateArray().ToArray();
        Require(births.Length > 0 && clones.Length >= 5, "Native repeated summons or detached clones were not recorded.");
        foreach (var sample in births) VerifyBirth(sample);
        foreach (var sample in clones) VerifyClone(sample);
        foreach (var sample in phases) RallyChecks.VerifyPhase(sample);
        foreach (var sample in triggers) RallyChecks.VerifyTrigger(sample);
        var extra = fixture.TryGetProperty("SpawnUpgrades", out var extraRecords) ? extraRecords.EnumerateArray().ToArray() : [];
        foreach (var sample in extra) VerifyExtra(sample);
        if (scenario.GetString()!.StartsWith("multi-summon-upgrade", StringComparison.Ordinal))
        {
            Require(extra.Length >= 4 && extra.Any(sample => sample.GetProperty("SourceAdded").GetBoolean()), "Extra spawn upgrades were not exercised.");
            Require(clones.Any(sample => sample.GetProperty("Before").Deserialize<CombatContext>()!.FindCard(sample.GetProperty("SourceCardId").GetInt32())!
                .Temporary.Upgrades.Any(upgrade => upgrade.AssetKey == "PojuMultiSummonExtra")), "Later births did not copy earlier extra spawn upgrades.");
            if (scenario.GetString() == "multi-summon-upgrade-unique")
                Require(extra.Any(sample => !sample.GetProperty("SourceAdded").GetBoolean()), "Unique duplicate source rejection was not exercised.");
            if (scenario.GetString() == "multi-summon-upgrade-restricted")
                Require(extra.Any(sample => sample.GetProperty("SourceAdded").GetBoolean() &&
                    Serialize(sample.GetProperty("Before").Deserialize<RoomCombatState>()!.Units) ==
                    Serialize(sample.GetProperty("AfterDirect").Deserialize<RoomCombatState>()!.Units)),
                    "Restricted unit rejection followed by a successful source write was not exercised.");
        }
        var cloneIds = clones.Select(sample => sample.GetProperty("CloneCardId").GetInt32()).ToHashSet();
        Require(births.Where(sample => sample.GetProperty("IsCardless").GetBoolean()).All(sample =>
            cloneIds.Contains(sample.GetProperty("SpawnerCardId").GetInt32())), "Cardless extra births lost their copied source reference.");
        Require(clones.Any(sample => sample.GetProperty("Before").Deserialize<CombatContext>()!.CardRegistry!
            .Single(card => card.InstanceId == sample.GetProperty("SourceCardId").GetInt32()).Temporary.Upgrades
            .Any(upgrade => upgrade.AssetKey == "PojuMultiSummonLiveSource")), "Later copies never observed an upgrade added during an earlier birth.");
        Require(phases.Any(sample => sample.GetProperty("Team").GetInt32() == (int)CombatTeam.Player &&
            sample.GetProperty("CachedUnitIds").GetArrayLength() >= 3), "Birth callbacks did not refresh the resolving card's paid Rally cache.");
        if (scenario.GetString() == "multi-summon-zero")
        {
            var zero = triggers.Single(sample => sample.GetProperty("Label").GetString() == "multi-summon-zero-dispatch");
            var before = zero.GetProperty("Before").Deserialize<RoomCombatState>()!;
            var after = zero.GetProperty("After").Deserialize<RoomCombatState>()!;
            var actor = zero.GetProperty("Actor").Deserialize<CombatUnit>()!;
            var afterActor = zero.GetProperty("AfterActor").Deserialize<CombatUnit>()!;
            var oldCache = before.Context!.CardRegistry!.Single(card => card.InstanceId == actor.SpawnerCardId).PlayedRoomUnitIds!;
            var newCache = after.Context!.CardRegistry!.Single(card => card.InstanceId == actor.SpawnerCardId).PlayedRoomUnitIds!;
            Require(zero.GetProperty("TriggerCount").GetInt32() == 0 && !oldCache.Contains(actor.Id) && oldCache.SequenceEqual(newCache) &&
                !actor.Triggers.Single(trigger => trigger.Kind == "CardMonsterPlayed").HasTriggered &&
                afterActor.Triggers.Single(trigger => trigger.Kind == "CardMonsterPlayed").HasTriggered && before.Context.Gold == after.Context.Gold,
                "Zero dispatch refreshed source caches, ran effects or failed to mark the trigger.");
        }
        bool truncated = false, overCapacity = false;
        foreach (var sample in fixture.GetProperty("Actions").EnumerateArray())
        {
            var before = sample.GetProperty("Before").Deserialize<BattleTurnState>()!;
            var actual = sample.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            var action = sample.GetProperty("Action").Deserialize<PlayCardAction>()!;
            string dataId = before.Spawn.Train.Context!.Cards.Hand.Single(card => card.InstanceId == action.CardInstanceId).DataId;
            var rule = before.PlayRules!.Cards.Single(card => card.DataId == dataId);
            if (rule.Summon == null) continue;
            var oldRoom = before.Spawn.Train.Rooms[action.RoomIndex];
            var newRoom = actual.Spawn.Train.Rooms[action.RoomIndex];
            int newCount = newRoom.Units.Count(unit => unit.Team == CombatTeam.Player) - oldRoom.Units.Count(unit => unit.Team == CombatTeam.Player);
            truncated |= newCount > 0 && newCount < rule.Summon.Count;
            overCapacity |= newRoom.Units.Where(unit => unit.Team == CombatTeam.Player).Sum(unit => unit.Size) > before.PlayRules.Rooms[action.RoomIndex].PlayerCapacity;
        }
        // Restricted size growth fills capacity before all physical slots. The next
        // real card must use another floor; slot truncation belongs to the other scenes.
        Require(overCapacity, "Native extra-birth capacity overflow was not exercised.");
        if (scenario.GetString() != "multi-summon-upgrade-restricted")
            Require(truncated, "Native physical-slot truncation was not exercised.");
        Parallel.For(0, 32, _ =>
        {
            foreach (var sample in births) VerifyBirth(sample);
            foreach (var sample in clones) VerifyClone(sample);
            foreach (var sample in phases) RallyChecks.VerifyPhase(sample);
            foreach (var sample in triggers) RallyChecks.VerifyTrigger(sample);
            foreach (var sample in extra) VerifyExtra(sample);
        });
        Console.WriteLine($"NATIVE-MULTI-SUMMON-CHECKS PASS: {births.Length} complete births, {clones.Length} detached clones, {extra.Length} extra upgrades, {phases.Length} paid phases, {triggers.Length} Rally dispatches, live source upgrades, retained clone flags, cardless/source timing, slot truncation={truncated}, capacity overflow and 32 branches.");
    }
    internal static void VerifyExtra(FixtureValue sample)
    {
        Require(sample.GetProperty("Completed").GetBoolean(), "Incomplete native extra spawn upgrade.");
        var before = sample.GetProperty("Before").Deserialize<RoomCombatState>()!;
        var directExpected = sample.GetProperty("AfterDirect").Deserialize<RoomCombatState>()!;
        var expected = sample.GetProperty("After").Deserialize<RoomCombatState>()!;
        var upgrade = sample.GetProperty("Upgrade").Deserialize<CardUpgradeModifier>()!;
        int unitId = sample.GetProperty("UnitId").GetInt32(), sourceId = sample.GetProperty("SpawnerCardId").GetInt32();
        string parent = Serialize(before);
        var direct = UnitModifierModel.ApplyDirect(before, unitId, upgrade);
        var result = SpawnUpgradeModel.Apply(before, unitId, sourceId, upgrade);
        Require(direct.Supported && result.Supported, "Independent extra spawn upgrade failed: " + (result.UnsupportedReason ?? direct.UnsupportedReason));
        Require(Serialize(direct.State) == Serialize(directExpected), "Extra spawn direct API differs from native.");
        Require(Serialize(result.State) == Serialize(expected), "Extra spawn source write differs from native.");
        Require(Serialize(before) == parent, "Extra spawn upgrade mutated its parent.");
        Require(Serialize(before.Context!.FindCard(sourceId)) == Serialize(directExpected.Context!.FindCard(sourceId)),
            "The direct API unexpectedly wrote back to the source card before SpawnMonster did.");
        int oldCount = directExpected.Context.FindCard(sourceId)!.Temporary.Upgrades.Count;
        int newCount = expected.Context!.FindCard(sourceId)!.Temporary.Upgrades.Count;
        Require(newCount - oldCount == (sample.GetProperty("SourceAdded").GetBoolean() ? 1 : 0), "Source duplicate result or descriptor count differs.");
    }
    internal static void VerifyBirth(FixtureValue sample)
    {
        Require(sample.GetProperty("Completed").GetBoolean(), "Incomplete native birth.");
        var before = sample.GetProperty("Before").Deserialize<RoomCombatState>()!;
        var definition = sample.GetProperty("Definition").Deserialize<CardPlayRule>()!;
        var expected = sample.GetProperty("After").Deserialize<RoomCombatState>()!;
        int id = sample.GetProperty("SpawnerCardId").GetInt32();
        bool cardless = sample.GetProperty("IsCardless").GetBoolean();
        string parent = Serialize(before);
        var result = UnitBirthModel.Spawn(before, definition, id, sample.GetProperty("Position").GetInt32(), cardless,
            sample.GetProperty("CardlessStatus").Deserialize<CombatStatus>()!);
        Require(result.Supported && result.UnitId == sample.GetProperty("UnitId").GetInt32(), "Independent birth failed: " + result.Result.UnsupportedReason);
        Require(Serialize(result.Result.State) == Serialize(expected), "Independent complete birth differs from native.");
        Require(Serialize(before) == parent, "Birth simulation mutated its parent.");
        if (definition.Summon != null)
        {
            var born = expected.Units.Single(unit => unit.Id == result.UnitId);
            Require(born.IsSpawning == false && (born.Status("cardless")?.Stacks > 0) == cardless &&
                born.StatusImmunities.Contains("endless") == cardless, "Birth flag, cardless marker or Endless immunity differs.");
            Require(expected.Context!.Gold - before.Context!.Gold == 10 + (cardless ? 5 * before.Units.Count(unit => unit.Team == CombatTeam.Player) : 0),
                "Copied sources incorrectly fired OnSpawnNotFromCard or Rally reward count changed.");
        }
    }
    private static void VerifyClone(FixtureValue sample)
    {
        Require(sample.GetProperty("Completed").GetBoolean(), "Incomplete native detached clone.");
        var before = sample.GetProperty("Before").Deserialize<CombatContext>()!;
        var expected = sample.GetProperty("After").Deserialize<CombatContext>()!;
        string parent = Serialize(before);
        var result = CardGenerationModel.CloneDetached(before, sample.GetProperty("Creation").Deserialize<CardCreationRule>()!,
            sample.GetProperty("SourceCardId").GetInt32());
        Require(result.Supported, "Independent clone failed: " + result.UnsupportedReason);
        Require(Serialize(result.Context) == Serialize(expected) && result.AddedCards.Single().InstanceId == sample.GetProperty("CloneCardId").GetInt32(),
            "Independent detached clone differs from native.");
        int id = result.AddedCards.Single().InstanceId;
        var clone = expected.CardRegistry!.Single(card => card.InstanceId == id);
        Require(!expected.CardInstances!.Any(card => card.InstanceId == id) && clone.PlayCount == 0 && clone.LastPlayedCost == 0 &&
            clone.Permanent.Upgrades.Any(upgrade => upgrade.ExcludeFromClones) && clone.Temporary.Upgrades.Any(upgrade => upgrade.ExcludeFromClones) &&
            Serialize(before.CardInstances) == Serialize(expected.CardInstances) && Serialize(before.Statistics) == Serialize(expected.Statistics) &&
            Serialize(before.NextAddedTemporaryUpgrades) == Serialize(expected.NextAddedTemporaryUpgrades),
            "Detached copies gained ownership/history, lost source upgrades or consumed unrelated generation state.");
        Require(Serialize(before) == parent, "Detached cloning mutated its parent.");
    }
    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value);
    private static void Require(bool value, string error) { if (!value) throw new InvalidOperationException(error); }
}
