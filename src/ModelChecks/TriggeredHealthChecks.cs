using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class TriggeredHealthChecks
{
    internal static void Run()
    {
        RoomCombatState Root(string type, int amount, int hp = 25, int maximum = 25, string lifetime = "")
        {
            string action = type == "CardEffectAdjustMaxHealth" ? "AdjustMaxHealth" : "BuffHealth";
            var effect = new CombatEffect(type, amount, 0, "", 0, [], false,
                action: new(action, "Self", amount, false, false, [], lifetime: lifetime));
            var unit = new CombatUnit(1, "health-actor", CombatTeam.Player, 5, hp, maximum, true, false, false,
                [new("heal immunity", 1), new("heal multiplier", 1, 2)],
                [new("OnSpawn", false, false, false, 1, [effect], false),
                    new("OnHeal", false, false, false, 1, [new("CardEffectRewardGold", 3, 0, "", 0, [], false)], false)], 8,
                modifiers: new(5, 0, 0, 1, 1, true, false, []));
            var rng = UnityRng.Seed(113); var card = CardInstanceState.Empty(8, "health-actor");
            var context = new CombatContext(new([], [], [], rng, 0, []), rng, 0, 9, 10,
                cardInstances: [card], cardRegistry: [card]);
            return new(0, false, [unit], [], context);
        }
        RoomCombatResult Fire(RoomCombatState state) => RoomCombatModel.ApplyQueuedCharacterTrigger(state, new(0, state.Units[0], "OnSpawn"), _ => { });
        foreach (var test in new[] { (Amount: 60, Expected: 85), (Amount: 100000, Expected: 99999), (Amount: 0, Expected: 25) })
        {
            var root = Root("CardEffectAdjustMaxHealth", test.Amount); string parent = JsonSerializer.Serialize(root);
            var result = Fire(root);
            Require(result.Supported && result.State!.Units[0].Health == 25 && result.State.Units[0].MaxHealth == test.Expected &&
                result.State.Context!.FindCard(8)!.Temporary.Offsets.Health == 0 && result.State.Context.Gold == 0 &&
                result.State.Context.BattleRng.Equals(root.Context!.BattleRng), result.UnsupportedReason ?? "Raw maximum adjustment healed, fired OnHeal, recorded a card buff or lost the native upper cap.");
            string expected = JsonSerializer.Serialize(result.State);
            Parallel.For(0, 32, _ => Require(JsonSerializer.Serialize(Fire(root).State) == expected, "Maximum-health branches diverged."));
            Require(JsonSerializer.Serialize(root) == parent, "Maximum-health callback mutated its parent.");
        }
        var signed = Fire(Root("CardEffectAdjustMaxHealth", -3, 5, 10));
        Require(signed.Supported && signed.State!.Units[0].Health == 5 && signed.State.Units[0].MaxHealth == 7,
            "Signed maximum-only adjustment changed current HP.");
        Require(!Fire(Root("CardEffectAdjustMaxHealth", -8, 5, 10)).Supported &&
            !Fire(Root("CardEffectAdjustMaxHealth", 3, lifetime: "ParentUpgrade")).Supported,
            "Invalid-health or parent-upgrade attribution was guessed.");
        var buff = Fire(Root("CardEffectBuffMaxHealth", 4, 5, 10, "TemporaryUntilEndOfBattle"));
        Require(buff.Supported && buff.State!.Units[0].MaxHealth == 14 && buff.State.Units[0].Health == 13 &&
            buff.State.Context!.FindCard(8)!.Temporary.Offsets.Health == 4 && buff.State.Context.Gold == 0,
            buff.UnsupportedReason ?? "Triggered buff lost raw offsets, multiplier/immunity handling or OnHeal suppression.");
        var negative = Fire(Root("CardEffectBuffMaxHealth", -7, 5, 10, "TemporaryUntilEndOfBattle"));
        Require(negative.Supported && negative.State!.Units[0].Health == 5 && negative.State.Units[0].MaxHealth == 10 &&
            negative.State.Context!.FindCard(8)!.Temporary.Offsets.Health == -7, "Negative health buff lost the signed card offset.");
        UnitHealthChecks.Run();
        Console.WriteLine("TRIGGERED-HEALTH PASS: maximum-only/current HP separation, native cap, signed offsets, multiplier/immunity, no OnHeal, attribution/invalid-health guards and 32 immutable branches.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
