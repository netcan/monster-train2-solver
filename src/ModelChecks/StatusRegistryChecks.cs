using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class StatusRegistryChecks
{
    internal static void Run()
    {
        CombatStatus[] registry =
        [
            new("regen", 0, 1, removeStackAtEnd: true, hidden: false, displayCategory: "Positive"),
            new("silenced", 0, removeAllAtEnd: true, hidden: false, displayCategory: "Negative"),
            new("damage shield", 1, removeWhenTriggered: true, hidden: false, displayCategory: "Positive"),
            new("immobile", 1, hidden: true, displayCategory: "Persistent")
        ];
        var unit = new CombatUnit(1, "retained", CombatTeam.Player, 0, 100, 100, false, false, false, registry,
            isBoss: false, lastAttackerId: 0, statusRegistry: registry);
        Require(unit.Statuses.Count == 2 && unit.CountUniqueStatusDefinitions() == 4 && unit.CountUniqueVisibleStatuses() == 3,
            "Zero-stack definitions did not contribute to native presence queries, or affected active statuses.");
        var enemy = new CombatUnit(2, "enemy", CombatTeam.Enemy, 6, 100, 100, true, false, false, [], isBoss: false, lastAttackerId: 0, statusRegistry: []);
        var root = new RoomCombatState(0, false, [enemy, unit], []);
        string original = JsonSerializer.Serialize(root);
        var exchange = RoomCombatModel.Exchange(root);
        CombatUnit afterHit = exchange.State!.Units.Single(value => value.Id == 1);
        Require(exchange.Supported && afterHit.Health == 100 && afterHit.Status("damage shield") == null &&
            afterHit.StatusRegistry!.Single(status => status.Id == "damage shield").Stacks == 0 && afterHit.CountUniqueVisibleStatuses() == 3,
            "Consumption discarded the shield definition or exposed zero-stack shield as active.");
        var clear = RoomCombatModel.Resolve(root);
        CombatUnit afterClear = clear.State!.Units.Single(value => value.Id == 1);
        Require(clear.Supported && afterClear.StatusRegistry!.Select(status => status.Id).SequenceEqual(["damage shield", "immobile"]) &&
            afterClear.CountUniqueStatusDefinitions() == 2 && afterClear.CountUniqueVisibleStatuses() == 1,
            "End-of-turn cleanup retained expired zero entries or removed persistent consumed definitions.");
        var restored = StatusApplicationModel.Apply(root, 1, new("regen", 1, 99, hidden: true, displayCategory: "Persistent"));
        Require(restored.Supported && restored.State!.Units[1].Status("regen")!.ParamInt == 1 &&
            restored.State.Units[1].StatusRegistry!.Select(status => status.Id).SequenceEqual(registry.Select(status => status.Id)) &&
            restored.State.Units[1].CountUniqueVisibleStatuses() == 3,
            "Readding an existing zero definition replaced its native rule or insertion position.");
        var zeroAdd = StatusApplicationModel.Apply(root, 1, new("armor", 0, 1, hidden: false, displayCategory: "Positive"));
        Require(zeroAdd.Supported && zeroAdd.State!.Units[1].Status("armor") == null &&
            zeroAdd.State.Units[1].StatusRegistry!.Last().Id == "armor" && zeroAdd.State.Units[1].CountUniqueVisibleStatuses() == 4,
            "A first zero addition failed to create the native dictionary entry.");
        var legacy = new CombatUnit(3, "legacy", CombatTeam.Player, 0, 1, 1, false, false, false, registry);
        Require(legacy.CountUniqueStatusDefinitions() == null && legacy.CountUniqueVisibleStatuses() == null,
            "Legacy active-only captures silently approximated historical presence queries.");
        Parallel.For(0, 32, _ => Require(RoomCombatModel.Resolve(root).Supported, "A parallel registry branch failed."));
        Require(JsonSerializer.Serialize(root) == original, "Registry mutation leaked into a parent or sibling.");
        Console.WriteLine("STATUS-REGISTRY-CHECKS PASS: zero presence, hidden/category rules, retained consumption, ordered cleanup/readdition, missing legacy evidence and 32 isolated branches.");
    }

    internal static void Native(FixtureValue fixture)
    {
        if (!fixture.TryGetProperty("StatusRegistryCalibration", out var samples)) return;
        int observed = 0, zeroSnapshots = 0, zeroVisible = 0, hidden = 0;
        foreach (var sample in samples.EnumerateArray())
        {
            CombatStatus[] registry = sample.GetProperty("Registry").Deserialize<CombatStatus[]>()!;
            var unit = new CombatUnit(sample.GetProperty("UnitId").GetInt32(), "query", CombatTeam.Player, 0, 1, 1,
                false, false, false, registry, statusRegistry: registry);
            Require(unit.CountUniqueStatusDefinitions() == sample.GetProperty("NativeTotal").GetInt32() &&
                unit.CountUniqueVisibleStatuses() == sample.GetProperty("NativeVisible").GetInt32(), "Native status-presence query differs.");
            if (registry.Any(status => status.Stacks == 0)) zeroSnapshots++;
            zeroVisible += registry.Count(status => status.Stacks == 0 && status.Hidden == false && status.DisplayCategory is "Positive" or "Negative");
            hidden += registry.Count(status => status.Hidden == true);
            observed++;
        }
        Require(observed > 0, "Native status registry contains no observations.");
        if (fixture.TryGetProperty("ModifierScenario", out var scenario) && scenario.GetString() == "triggered-status")
            Require(zeroSnapshots > 0 && zeroVisible > 0 && hidden > 0,
                "Retained-status native calibration does not cover zero visible entries and hidden definitions.");
        Console.WriteLine($"NATIVE-STATUS-REGISTRY PASS: {observed} exact native queries, {zeroSnapshots} zero-containing snapshots, {zeroVisible} zero visible entries and {hidden} hidden definitions.");
    }
    private static void Require(bool pass, string message) { if (!pass) throw new InvalidOperationException(message); }
}
