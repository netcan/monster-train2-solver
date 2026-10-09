using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class RetainedCallbackChecks
{
    internal static void Run()
    {
        AutomaticRetainedAuraCallback(false);
        AutomaticRetainedAuraCallback(true);
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
        Require(queue.Single(item => item.Kind == "OnHit").ParamString == "",
            "Triggered damage queued a null string where native OnHit supplies an empty FireTriggersData string.");
        var healer = new CombatUnit(5, "unplaced", CombatTeam.Enemy, 0, 10, 20, false, false, false, [],
            [new("OnArmorAdded", true, false, true, 1,
                [new("CardEffectHeal", 1, 0, "", 0, [], false, action: new("Heal", "Self", 1, true, false, []))], false)],
            modifiers: actor.Modifiers, isBoss: false, lastAttackerId: 0, statusRegistry: []);
        var healingQueue = new List<RoomCombatModel.QueuedCharacterTrigger>();
        var healingCallback = new RoomCombatModel.QueuedCharacterTrigger(0, healer, "OnArmorAdded");
        var healed = RoomCombatModel.ApplyQueuedCharacterTrigger(room, healingCallback, healingQueue.Add);
        Require(healed.Supported && healingCallback.Unit.Health == 11 &&
            healingQueue.Single(item => item.Kind == "OnHeal").ParamInt == 1 &&
            healingQueue.Single(item => item.Kind == "OnHeal").ParamString == "",
            "Retained self healing lost the native OnHeal amount or empty-string payload.");
        Console.WriteLine("RETAINED-CALLBACK-CHECKS PASS: unplaced living actors, self/room target membership, deferred payloads and parent isolation.");
    }

    private static void AutomaticRetainedAuraCallback(bool temporaryBossPreview)
    {
        var rng = UnityRng.Seed(81);
        var armor = new CombatStatus("armor", 2, 1, stackable: true, hidden: false, displayCategory: "Positive");
        var buff = new CombatStatus("buff", 1, 1, stackable: true, hidden: false, displayCategory: "Positive");
        var aura = new EnchantmentRule(new("Enchant", "Room", 0, true, true, []), [armor],
            new EnchantmentState(primaryTargets: [new(5, true, 0)], previewTargets: [new(5, true, 0)]), bound: true);
        var secondAura = new EnchantmentRule(new("Enchant", "Room", 0, true, true, []), [buff],
            new EnchantmentState(primaryTargets: [new(5, true, 0)], previewTargets: [new(5, true, 0)]), bound: true);
        var sourceUnit = new CombatUnit(1, "aura", CombatTeam.Enemy, 0, 1, 1, false, false, false, [],
            [new("OnSpawn", false, false, false, 1, [new("CardEffectEnchant", 0, 0, "", 0, [], false, enchantment: aura),
                new("CardEffectEnchant", 0, 0, "", 0, [], false, enchantment: secondAura)], false)],
            isBoss: temporaryBossPreview, lastAttackerId: 0, statusRegistry: []);
        var defender = new CombatUnit(2, "defender", CombatTeam.Player, 1, 20, 20, true, false, false, [],
            isBoss: false, lastAttackerId: 0, statusRegistry: []);
        var retained = new CombatUnit(5, "retained-preview", CombatTeam.Player, 0, 20, 20, false, false, false, [armor, buff],
            [new("OnStatusEffectChanged", true, false, false, 1, [new("CardEffectRewardGold", 7, 0, "", 0, [], false)], false)],
            isBoss: false, lastAttackerId: 0, statusRegistry: [armor, buff]);
        var room = new RoomCombatState(0, false, [sourceUnit, defender], [], preview: !temporaryBossPreview);
        var context = new CombatContext(new([], [], [], rng, 0, []), rng, 10, 1, 10, nextUnitId: 6,
            statistics: temporaryBossPreview ? BattleStatistics.Empty() : null,
            enchantments: new([room], [], 7, [new(retained, 0, true)], [1], true, false, !temporaryBossPreview, rng, true));
        room = new RoomCombatState(0, false, room.Units, [], context, !temporaryBossPreview);
        string parent = JsonSerializer.Serialize(room);
        void Verify()
        {
            var result = RoomCombatModel.Exchange(room);
            Require(result.Supported, result.UnsupportedReason ?? "Retained aura callback was rejected.");
            var actor = result.State!.Context!.Enchantments!.RetainedUnits.Single(unit => unit.Unit.Id == 5);
            Require(result.State.Units.Select(unit => unit.Id).SequenceEqual(new[] { 2 }) && actor.RoomIndex == 0 && actor.Preview &&
                actor.Unit.RegisteredStatus("armor")?.Stacks == 0 && actor.Unit.RegisteredStatus("buff")?.Stacks == 0 && actor.Unit.Triggers[0].HasTriggered &&
                result.State.Context.Gold == 10 && result.State.Context.NextUnitId == 6,
                $"Retained aura callback: units={string.Join(',', result.State.Units.Select(unit => unit.Id))}, room={actor.RoomIndex}, preview={actor.Preview}, armor={actor.Unit.RegisteredStatus("armor")?.Stacks}, buff={actor.Unit.RegisteredStatus("buff")?.Stacks}, once={actor.Unit.Triggers[0].HasTriggered}, gold={result.State.Context.Gold}, next={result.State.Context.NextUnitId}.");
        }
        Verify(); Parallel.For(0, 32, _ => Verify());
        Require(JsonSerializer.Serialize(room) == parent, "Retained aura callbacks mutated their root.");
        Console.WriteLine($"RETAINED-AURA-CALLBACK-CHECKS PASS: {(temporaryBossPreview ? "temporary Boss preview restoration" : "local preview callbacks")}, two withdrawn statuses, once state, preview gold, stable identity and 32 immutable branches.");
    }
    private static void Require(bool pass, string message) { if (!pass) throw new InvalidOperationException(message); }
}
