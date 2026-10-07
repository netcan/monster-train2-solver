using System.Text.Json;
using MonsterTrain2Poju.Model;

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
}
