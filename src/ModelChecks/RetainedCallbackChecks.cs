using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class RetainedCallbackChecks
{
    internal static void Run()
    {
        var rng = UnityRng.Seed(81);
        var context = new CombatContext(new([], [], [], rng, 0, []), rng, 10, 1, 10);
        var effects = new CombatEffect[] {
            new("CardEffectAddStatusEffect", 0, 0, "", 0, [], false,
                action: new("AddStatus", "Self", 0, true, false, [new("regen", 1, 1, hidden: false, displayCategory: "Positive")])),
            new("CardEffectHeal", 1, 0, "", 0, [], false, action: new("Heal", "Room", 1, true, false, [])),
            new("CardEffectDamage", 1, 0, "", 0, [], false, action: new("Damage", "Room", 1, false, true, [])) };
        var actor = new CombatUnit(5, "unplaced", CombatTeam.Enemy, 0, 10, 20, false, false, false, [],
            [new("OnArmorAdded", true, false, true, 1, effects, false)],
            modifiers: new(0, 0, 0, 1, 1, true, false, []), isBoss: false, lastAttackerId: 0, statusRegistry: []);
        var player = new CombatUnit(2, "player", CombatTeam.Player, 0, 20, 20, false, false, false, [],
            modifiers: new(0, 0, 0, 1, 1, true, false, []), isBoss: false, lastAttackerId: 0, statusRegistry: []);
        var room = new RoomCombatState(0, false, [player], [], context);
        string parent = JsonSerializer.Serialize(room);
        var queue = new List<RoomCombatModel.QueuedCharacterTrigger>();
        var callback = new RoomCombatModel.QueuedCharacterTrigger(0, actor, "OnArmorAdded");
        var result = RoomCombatModel.ApplyQueuedCharacterTrigger(room, callback, queue.Add);
        Require(result.Supported && result.State!.Units.Count == 1 && result.State.Units[0].Health == 19 &&
            callback.Unit.Health == 10 && callback.Unit.Status("regen")!.Stacks == 1 &&
            queue.All(item => item.Kind != "OnHeal") && callback.Unit.Triggers[0].HasTriggered &&
            JsonSerializer.Serialize(room) == parent,
            "A live unplaced callback actor joined the room or room healing targeted its retained reference.");
        Console.WriteLine("RETAINED-CALLBACK-CHECKS PASS: unplaced living actors, self/room target membership, deferred payloads and parent isolation.");
    }
    private static void Require(bool pass, string message) { if (!pass) throw new InvalidOperationException(message); }
}
