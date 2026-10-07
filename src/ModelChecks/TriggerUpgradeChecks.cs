using System.Text.Json;
using MonsterTrain2Poju.Model;
using MonsterTrain2Poju.Fixtures;

internal static class TriggerUpgradeChecks
{
    internal static void Run()
    {
        var rng = UnityRng.Seed(903);
        CombatTrigger Trigger(string kind, int amount, bool equipped = false) => new(kind, false, false, true, 1,
            [new CombatEffect("CardEffectRewardGold", amount, 0, "", 0, [], false)], false, 0, new CombatTriggerOrigin("", 0, false, equipped));
        CardUpgradeModifier Upgrade(string id) => new(id, id, new CardStatModifier(), [], false, false, false, 0, 0, [],
            triggerUpgrades: [Trigger("OnTurnBegin", 2)]);
        var modifiers = new UnitModifiers(0, 0, 0, 1, 2, true, false, []);
        var context = new CombatContext(new CardCycleState([], [], [], rng, 0, []), rng, 0, 1, 10, cardInstances: []);
        var host = new CombatUnit(1, "host", CombatTeam.Player, 0, 30, 30, false, false, false, [],
            [Trigger("OnTurnBegin", 1), Trigger("PostCombat", 7, true)], modifiers: modifiers, equipmentCards: []);
        var root = new RoomCombatState(0, false, [host], [], context);
        string parent = JsonSerializer.Serialize(root, ModelJson.Options);
        var upgrade = Upgrade("shared-upgrade");
        var left = UnitModifierModel.ApplyDirect(root, 1, upgrade, upgradeId: "left").State!;
        var both = UnitModifierModel.ApplyDirect(left, 1, upgrade, upgradeId: "right").State!;
        var removed = UnitModifierModel.ApplyDirect(both, 1, upgrade, true, "right").State!;
        Require(removed.Units[0].Triggers.Count == 3 && removed.Units[0].Triggers.Any(trigger => trigger.Origin?.UpgradeId == "left") &&
            !removed.Units[0].Triggers.Any(trigger => trigger.Origin?.UpgradeId == "right"), "Direct removal mixed applied-upgrade identity with trigger origin.");
        var all = UnitModifierModel.ApplyDirect(both, 1, upgrade, true).State!;
        Require(all.Units[0].Triggers.Count == 1 && all.Units[0].Triggers[0].Kind == "PostCombat",
            "Empty trigger origin did not remove every matching kind, including the base trigger.");
        var turn = RoomCombatModel.ApplyUnitTurn(both, 1);
        Require(turn.Supported && turn.State!.Context!.Gold == 15, "Granted turn triggers did not execute in addition to the base trigger with native minimum rewards.");
        var post = RoomCombatModel.ApplyUnitPostCombat(root, [], []);
        Require(post.Supported && post.State!.Context!.Gold == 0 && !post.State.Units[0].Triggers[1].HasTriggered,
            "An equipped-only trigger fired on an empty equipment list.");
        var preview = UnitModifierModel.ApplyDirect(new RoomCombatState(0, false, [host], [], context, true), 1, upgrade);
        Require(preview.Supported && preview.State!.Units[0].Triggers.Count == host.Triggers.Count, "Preview incorrectly added native upgrade triggers.");
        var inherited = CardModifierModel.Resolve(new CardPlayRule("unit", "unit", 0, "Spawn", "Standby", host, []),
            new CardInstanceState(42, "unit", new CardModifiers(new CardStatModifier(), [upgrade], 0, []), CardModifiers.Empty(), 0, 0, 0, []));
        Require(inherited.SpawnUnit!.Triggers.Count == 3 && inherited.SpawnUnit.Triggers.Last().Origin!.UpgradeId == "" &&
            !inherited.SpawnUnit.Triggers.Last().Origin!.IsFromEquipment, "Spawner card upgrades did not initialize ordinary, unbound triggers.");
        string expected = JsonSerializer.Serialize(removed, ModelJson.Options);
        Parallel.For(0, 32, _ => Require(JsonSerializer.Serialize(UnitModifierModel.ApplyDirect(both, 1, upgrade, true, "right").State,
            ModelJson.Options) == expected, "Parallel trigger removal differed."));
        Require(JsonSerializer.Serialize(root, ModelJson.Options) == parent, "Trigger upgrades mutated their parent.");
        Console.WriteLine("TRIGGER-UPGRADE-CHECKS PASS: attributed/empty removal, base trigger preservation, equipped condition, preview, spawner inheritance and 32 parallel branches.");
    }
    private static void Require(bool passed, string message) { if (!passed) throw new InvalidOperationException(message); }
    internal static void Native(FixtureValue fixture)
    {
        if (!fixture.TryGetProperty("TriggerMutations", out var records)) return;
        var samples = records.EnumerateArray().ToArray();
        foreach (var sample in samples) Verify(sample);
        if (fixture.GetProperty("ModifierScenario").GetString() != "trigger-mutation") return;
        Require(samples.Length == 3 && samples.Select(sample => sample.GetProperty("Label").GetString()).ToHashSet()
            .SetEquals(["append-same-phase", "remove-earlier-skips-next", "self-removal-finishes-effects"]), "Missing native trigger mutation cases.");
        var cases = samples.ToDictionary(sample => sample.GetProperty("Label").GetString()!);
        CombatUnit Host(FixtureValue sample, string field) => sample.GetProperty(field).Deserialize<RoomCombatState>()!.Units
            .Single(unit => unit.Id == sample.GetProperty("UnitId").GetInt32());
        var append = cases["append-same-phase"];
        Require(Host(append, "After").Triggers.Count == Host(append, "Before").Triggers.Count + 1 &&
            Host(append, "After").Triggers.Last().HasTriggered && Host(append, "After").Triggers.Last().Origin!.UpgradeId.EndsWith("000000000001"),
            "A newly appended same-phase trigger did not fire with its native definition ID.");
        var shifted = Host(cases["remove-earlier-skips-next"], "After");
        Require(shifted.Triggers.Count == 3 && shifted.Triggers[0].HasTriggered && !shifted.Triggers[1].HasTriggered && shifted.Triggers[2].HasTriggered,
            "Native list shifting did not skip the next trigger while retaining the running trigger's effects.");
        var self = cases["self-removal-finishes-effects"];
        var before = self.GetProperty("Before").Deserialize<RoomCombatState>()!;
        var after = self.GetProperty("After").Deserialize<RoomCombatState>()!;
        Require(Host(self, "After").Triggers.Count == 0 && after.Context!.Gold - before.Context!.Gold == 10,
            "A removed trigger did not finish its own effects or incorrectly executed its removed sibling.");
        Parallel.For(0, 32, _ => { foreach (var sample in samples) Verify(sample); });
        Console.WriteLine("NATIVE-TRIGGER-MUTATION-CHECKS PASS: three independently compared append, shifted-index and detached-running transitions in 32 parallel branches.");
    }
    private static void Verify(FixtureValue sample)
    {
        var before = sample.GetProperty("Before").Deserialize<RoomCombatState>()!;
        var after = sample.GetProperty("After").Deserialize<RoomCombatState>()!;
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        var result = RoomCombatModel.ApplyPreCombat(before, sample.GetProperty("UnitId").GetInt32());
        Require(result.Supported, "Native trigger mutation unsupported: " + result.UnsupportedReason);
        string? difference = ModelJson.Difference(JsonSerializer.Serialize(result.State, ModelJson.Options), JsonSerializer.Serialize(after, ModelJson.Options));
        Require(difference == null, sample.GetProperty("Label").GetString() + ": " + difference);
        Require(JsonSerializer.Serialize(before, ModelJson.Options) == parent, "Trigger mutation changed its parent.");
    }
}
