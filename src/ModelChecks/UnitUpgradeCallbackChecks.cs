using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class UnitUpgradeCallbackChecks
{
    internal static void Run()
    {
        var rng = UnityRng.Seed(774);
        CombatTrigger EffectTrigger(string kind, CombatEffect effect, bool once = false) => new(kind, once, false, true, 1, [effect], false);
        var marker = new CardUpgradeModifier("writeback", "writeback", new(damage: 1),
            [new CombatStatus("armor", 1, 1, hidden: false, displayCategory: "Positive")], false, false, false, 0, 0, []);
        var copy = new CombatEffect("CardEffectAddBattleCard", 0, 0, "DiscardPile", 1, ["callback-card"], false,
            generation: new("DiscardPile", 1, [new("callback-card", CardModifiers.Empty(), [], [])], copyModifiers: true));
        var writebackActor = new CombatUnit(8, "writeback", CombatTeam.Player, 0, 20, 20, false, false, false, [],
            [EffectTrigger("OnArmorAdded", copy, once: true)], spawnerCardId: 1,
            modifiers: new(0, 0, 0, 1, 1, true, false, []), isBoss: false, lastAttackerId: 0, statusRegistry: []);
        var writebackContext = new CombatContext(new([], [], [], rng, 0, []), rng, 10, 2, 10,
            cardInstances: [CardInstanceState.Empty(1, "callback-card")], cardRegistry: []);
        var writebackRoom = new RoomCombatState(0, false, [writebackActor], [], writebackContext);
        string writebackParent = JsonSerializer.Serialize(writebackRoom);
        var standalone = UnitModifierModel.Apply(writebackRoom, 8, marker, "Permanent");
        Require(standalone.Supported && standalone.State!.Context!.FindCard(1)!.Permanent.Upgrades.Count == 1 &&
            standalone.State.Context.FindCard(2)!.Permanent.Upgrades.Count == 0 && standalone.State.Units.Single().BaseAttack == 1,
            "An upgrade callback copied its source after the native card writeback boundary: " +
            (standalone.UnsupportedReason ?? JsonSerializer.Serialize(standalone.State)));
        var upgradingTrigger = EffectTrigger("PreCombat", new("CardEffectAddCardUpgradeToUnits", 0, 0, "", 0, [], false,
            unitUpgrade: new("UnitUpgrade", "Self", 0, true, false, [], upgrade: marker, lifetime: "Permanent")));
        var insideQueue = new CombatUnit(8, "writeback", CombatTeam.Player, 0, 20, 20, false, false, false, [],
            [upgradingTrigger, EffectTrigger("OnArmorAdded", copy, once: true)], spawnerCardId: 1,
            modifiers: writebackActor.Modifiers, isBoss: false, lastAttackerId: 0, statusRegistry: []);
        var deferred = RoomCombatModel.ApplyPreCombat(new RoomCombatState(0, false, [insideQueue], [], writebackContext), 8);
        Require(deferred.Supported && deferred.State!.Context!.FindCard(1)!.Permanent.Upgrades.Count == 1 &&
            deferred.State.Context.FindCard(2)!.Permanent.Upgrades.Count == 1,
            "A running trigger queue drained its child callback before source card writeback.");
        Require(JsonSerializer.Serialize(writebackRoom) == writebackParent, "Upgrade callbacks mutated their parent card state.");
        Parallel.For(0, 32, _ => Require(UnitModifierModel.Apply(writebackRoom, 8, marker, "Permanent").State!.Context!.FindCard(2)!.Permanent.Upgrades.Count == 0,
            "Parallel upgrade callbacks crossed a sibling source writeback boundary."));
        Console.WriteLine("UNIT-UPGRADE-CALLBACK-CHECKS PASS: standalone and running-queue source-copy boundaries, final source writeback and 32 isolated branches.");
    }
    private static void Require(bool pass, string message) { if (!pass) throw new InvalidOperationException(message); }
}
