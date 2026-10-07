using MonsterTrain2Poju.Fixtures;
using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class UnitAttackChecks
{
    internal static void Run()
    {
        var rng = UnityRng.Seed(92);
        var card = CardInstanceState.Empty(8, "unit");
        var context = new CombatContext(new CardCycleState([], [], [], rng, 0, []), rng, 0, 9, 10, cardInstances: [card]);
        CombatUnit Unit(int id, bool canAttack, int attack = 8, CombatStatus[]? statuses = null) =>
            new(id, "unit" + id, CombatTeam.Player, attack, 10, 10, canAttack, false, false, statuses ?? [], spawnerCardId: 8,
                modifiers: new(attack, 0, 0, 1, 1, true, false, []));
        var root = new RoomCombatState(0, false, [Unit(1, true), Unit(2, false), Unit(3, true, 0)], [], context);
        CardActionEffect Effect(string type, int value, string target = "Room", CardEffectTests? tests = null) =>
            new(type, target, value, false, true, [], tests: tests);
        string parent = JsonSerializer.Serialize(root);
        var debt = UnitAttackModel.Apply(root, 1, 20, true);
        Require(debt.Supported && debt.State!.Units[0].BaseAttack == 0 && debt.State.Units[0].Modifiers!.DamageBuff == -20 &&
            UnitAttackModel.Apply(debt.State, 1, 11).State!.Units[0].BaseAttack == 0 &&
            UnitAttackModel.Apply(debt.State, 1, 13).State!.Units[0].BaseAttack == 1,
            "Attack display clamping destroyed the native negative buff balance.");
        var added = CardSpellModel.Apply(root, [Effect("BuffAttack", 3), Effect("DebuffAttack", 1), Effect("BuffAttack", -5)], 0);
        Require(added.Supported && added.State!.Units[0].BaseAttack == 10 && added.State.Units[1].BaseAttack == 8 &&
            added.State.Units[2].BaseAttack == 2 && added.State.Context!.CardInstances!.Single().Permanent.Upgrades.Count == 0 &&
            added.State.Context.BattleRng.Equals(rng), "Attack capability, zero-attack recovery, negative no-op, spawner ownership or RNG differs.");
        Require(!CardSpellModel.TestPlay(new RoomCombatState(0, false, [Unit(2, false)], [], context), [Effect("BuffAttack", 2)], 0).CanPlay &&
            CardSpellModel.TestPlay(root, [Effect("BuffAttack", 0)], 0).CanPlay,
            "Buff/debuff casting ignored capability or rejected a valid zero-value effect.");
        Require(!CardSpellModel.TestPlay(root, [Effect("BuffAttack", 1, "RandomInRoom")], 0).Supported,
            "Mixed-capability auxiliary random tests returned a modeled cast.");
        var mandatory = new CardEffectTests(true, true, false, false);
        var randomHistory = new[] { Effect("Damage", 0, "RandomInRoom"), Effect("BuffAttack", 1, "LastTargetedCharacters", mandatory) };
        Require(!CardSpellModel.TestPlay(root, randomHistory, 0).Supported && !CardSpellModel.Apply(root, randomHistory, 0).Supported,
            "Mandatory attack tests after random damage accepted auxiliary-stream-dependent target history.");
        var skippedDrop = new CardEffectTests(false, false, false, false);
        Require(!CardSpellModel.TestPlay(root, [randomHistory[0], Effect("Damage", 0, "DropTargetCharacter", skippedDrop), randomHistory[1]], 1).Supported,
            "An untested drop target incorrectly reset casting-test random history.");
        Require(CardSpellModel.TestPlay(root, [randomHistory[0], Effect("Damage", 0, "DropTargetCharacter"), randomHistory[1]], 1).CanPlay,
            "A tested explicit drop target did not reset casting history.");
        var incapable = new RoomCombatState(0, false, [Unit(2, false)], [], context);
        var canceled = CardSpellModel.Apply(incapable, [Effect("BuffAttack", 1, tests: new(true, false, true, false)),
            new("AddStatus", "Room", 0, false, true, [new("armor", 1)])], 0);
        Require(canceled.Supported && canceled.State!.Units.Single().Statuses.Count == 0,
            "Failed attack capability tests did not cancel subsequent runtime effects.");
        var upgrade = new CardUpgradeModifier("attack-upgrade", "attack-upgrade", new(damage: 5), [], false, false, false, 0, 0, []);
        var upgradedDebt = UnitModifierModel.Apply(debt.State!, 1, upgrade, "TemporaryUntilUnitDeath");
        Require(upgradedDebt.Supported && upgradedDebt.State!.Units[0].Modifiers!.DamageBuff == -20 &&
            upgradedDebt.State.Units[0].BaseAttack == 0 && UnitAttackModel.Apply(upgradedDebt.State, 1, 8).State!.Units[0].BaseAttack == 1 &&
            UnitModifierModel.Apply(upgradedDebt.State, 1, upgrade, "", true).State!.Units[0].Modifiers!.DamageBuff == -20,
            "Unit upgrade/removal discarded a raw attack deficit.");
        var train = new TrainCombatState([root, new(1, false, [Unit(4, true)], [], context), new(2, false, [], [], context)], [], 5, context);
        var remote = CardSpellModel.Apply(train, 0, [Effect("BuffAttack", 2, "Tower"), Effect("DebuffAttack", 1, "RandomFromAnyRoom")], 0);
        Require(!remote.Supported, "A mixed-capability random runtime gate returned a partial search child.");
        var valid = CardSpellModel.Apply(train, 0, [Effect("BuffAttack", 2, "Tower")], 0);
        Require(valid.Supported && valid.State!.Rooms[1].Units.Single().BaseAttack == 10 &&
            valid.State.Context!.CardInstances!.Single().Temporary.Upgrades.Count == 0, "Remote attack buffs changed a source card or lost a room.");
        Parallel.For(0, 32, _ => Require(JsonSerializer.Serialize(CardSpellModel.Apply(root,
            [Effect("BuffAttack", 3), Effect("DebuffAttack", 1), Effect("BuffAttack", -5)], 0).State) == JsonSerializer.Serialize(added.State),
            "Parallel attack changes differ."));
        Require(JsonSerializer.Serialize(root) == parent, "Attack changes mutated their parent.");
        Console.WriteLine("UNIT-ATTACK-CHECKS PASS: raw negative balance, capability, zero/negative values, casting, auxiliary gates, source ownership, remote state and parallel branches.");
    }

    internal static void Native(FixtureValue actions)
    {
        int plays = 0, remote = 0, incapable = 0, zeroAttack = 0, rawDebtRecovered = 0;
        foreach (FixtureValue entry in actions.EnumerateArray())
        {
            BattleTurnState before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
            PlayCardAction action = entry.GetProperty("Action").Deserialize<PlayCardAction>()!;
            CombatContext context = before.Spawn.Train.Context!;
            string dataId = context.Cards.Hand.Single(card => card.InstanceId == action.CardInstanceId).DataId;
            CardPlayRule rule = before.PlayRules!.Cards.Single(card => card.DataId == dataId);
            if (!rule.Effects.Any(effect => effect.Type == "BuffAttack")) continue;
            plays++;
            Require(rule.Effects[0].Target == "Tower" && rule.Effects.Any(effect => effect.Type == "DebuffAttack" && effect.Value < 0) &&
                rule.Effects.Any(effect => effect.Type == "BuffAttack" && effect.Value == 0), "Attack oracle lacks global incapable targets or no-op values.");
            BattleTurnState actual = entry.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            CombatUnit[] after = actual.Spawn.Train.Rooms.SelectMany(room => room.Units).ToArray();
            int randomRecipients = 0;
            foreach (RoomCombatState room in before.Spawn.Train.Rooms)
                foreach (CombatUnit old in room.Units)
                {
                    CombatUnit next = after.Single(unit => unit.Id == old.Id);
                    if (old.IsPyre)
                    {
                        Require(JsonSerializer.Serialize(old) == JsonSerializer.Serialize(next), "Tower attack buffs changed the Pyre.");
                        continue;
                    }
                    UnitModifiers oldModifiers = old.Modifiers!, modifiers = next.Modifiers!;
                    if (!old.CanAttack)
                    {
                        Require(modifiers.DamageBuff == oldModifiers.DamageBuff, "An incapable native unit received an attack change.");
                        incapable++;
                        continue;
                    }
                    int delta = modifiers.DamageBuff - oldModifiers.DamageBuff;
                    if (old.Team == CombatTeam.Player)
                    {
                        Require(delta is 3 or 4 && modifiers.AttackDamage == oldModifiers.AttackDamage + 1 &&
                            modifiers.Upgrades.Count == oldModifiers.Upgrades.Count + 1,
                            "Native friendly recovery, raw balance or later until-death upgrade differs.");
                        if (delta == 4) randomRecipients++;
                        if (room.RoomIndex != action.RoomIndex) remote++;
                        CardInstanceState oldCard = context.CardInstances!.Single(card => card.InstanceId == old.SpawnerCardId);
                        CardInstanceState nextCard = actual.Spawn.Train.Context!.CardInstances!.Single(card => card.InstanceId == old.SpawnerCardId);
                        Require(JsonSerializer.Serialize(oldCard) == JsonSerializer.Serialize(nextCard), "Attack changes modified a source unit card.");
                    }
                    else Require(delta == 3 && modifiers.AttackDamage == oldModifiers.AttackDamage,
                        "Native global enemy buff/debuff recovery differs.");
                    Require(next.BaseAttack == Math.Max(0, modifiers.AttackDamage + modifiers.DamageBuff), "Native attack clamp differs.");
                    if (old.BaseAttack == 0 && next.BaseAttack > 0) zeroAttack++;
                    rawDebtRecovered++;
                }
            Require(randomRecipients == 1 && actual.Spawn.Train.Context!.BattleRng.Equals(context.BattleRng.Next()),
                "A random friendly attack buff did not consume exactly one Battle draw and choose one target.");
        }
        Require(plays > 0 && remote > 0 && incapable > 0 && zeroAttack > 0,
            "Attack oracle lacks spells, remote targets, incapable units or zero-attack recovery.");
        Console.WriteLine($"NATIVE-UNIT-ATTACK-COVERAGE PASS: {plays} plays, {rawDebtRecovered} raw recoveries, {remote} remote friendlies, {incapable} incapable targets and {zeroAttack} zero-attack recoveries.");
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
