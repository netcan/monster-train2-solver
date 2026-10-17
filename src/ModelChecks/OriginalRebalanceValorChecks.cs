using System;
using System.Linq;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalRebalanceValorChecks
{
    private const string RelicId = "76511782-d2c1-43d1-9878-e465758017d8";
    private const string RelicKey = "RebalanceValor";
    private const string Effect = "RelicEffectRebalanceValor";

    internal static void Run()
    {
        CombatRelicState owner = Owner();
        Require(RelicModel.Validate([owner]) == null, "Captured original RebalanceValor payload was rejected.");

        RoomCombatResult Control() => Resolve(turn: 1, deployment: false, relics: [], Unit(1));
        RoomCombatResult Rebalanced(params CombatUnit[] units) => Resolve(turn: 1, deployment: false, relics: [owner], units);
        RoomCombatResult control = Control();
        RoomCombatResult powered = Rebalanced(Unit(1));
        Require(control.Supported && control.State!.Units.Single().Status("armor")?.Stacks == 2 &&
            powered.Supported && powered.State!.Units.Single().Status("armor")?.Stacks == 4,
            "RebalanceValor did not add exactly one armor per Valor stack over the vanilla target.");

        RoomCombatResult partialArmor = Rebalanced(Unit(1, armor: 3));
        Require(partialArmor.Supported && partialArmor.State!.Units.Single().Status("armor")?.Stacks == 4,
            "RebalanceValor did not fill existing armor to the native target instead of adding its full target.");
        RoomCombatResult frontOnly = Rebalanced(Unit(1), Unit(2));
        Require(frontOnly.Supported && frontOnly.State!.Units.Single(unit => unit.Id == 1).Status("armor")?.Stacks == 4 &&
            frontOnly.State.Units.Single(unit => unit.Id == 2).Status("armor") == null,
            "RebalanceValor armor was not restricted to the living room front.");

        RoomCombatResult turnZero = Resolve(turn: 0, deployment: false, relics: [owner], Unit(1));
        RoomCombatResult deployment = Resolve(turn: 1, deployment: true, relics: [owner], Unit(1));
        Require(turnZero.Supported && turnZero.State!.Units.Single().Status("armor") == null &&
            deployment.Supported && deployment.State!.Units.Single().Status("armor") == null,
            "Valor armor fired at native turn zero or during deployment.");

        StatusScalingResult attackControl = RelicStatusMagnitudeModel.GetModifier(Context(1, []), "valor", 2);
        StatusScalingResult armor = RelicStatusMagnitudeModel.GetValorArmorIncrease(Context(1, [owner]));
        Require(attackControl.Supported && attackControl.Stacks == 0 && armor.Supported && armor.Stacks == 1,
            "RebalanceValor attack and armor payloads were confused in their consumers.");
        foreach (CombatRelicState malformed in new[]
        {
            new CombatRelicState(RelicId, RelicKey, [Effect], disallowedInPlacementPhase: false),
            Owner(attack: 1), Owner(armor: 2),
            new CombatRelicState("wrong-id", RelicKey, [Effect], disallowedInPlacementPhase: false,
                statusMagnitudes: [new(0, 0, "valor", 0, valorAttackIncrease: 0, valorArmorIncrease: 1)])
        })
            Require(RelicModel.Validate([malformed]) != null, "A missing or altered RebalanceValor definition passed validation.");

        Console.WriteLine("ORIGINAL-REBALANCE-VALOR-PURE PASS: vanilla/relic target, partial armor, front-only application, turn-zero/deployment gates, and strict payload guards.");
    }

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 206 &&
            root.GetProperty("OriginalRebalanceValorPrepared").GetBoolean() &&
            root.GetProperty("ModifierScenario").GetString() == "original-rebalance-valor" &&
            root.GetProperty("NativeWon").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }
                .All(name => root.GetProperty(name).GetInt32() == 0),
            "The isolated original RebalanceValor battle is incomplete or has native/model failures.");

        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray()
            .Where(entry => entry.GetProperty("ScenarioAction").GetBoolean()).ToArray();
        int[] carrierCards = actions.Select(entry => entry.GetProperty("Action").GetProperty("CardInstanceId").GetInt32())
            .Distinct().ToArray();
        Require(actions.Length == 1 && carrierCards.Length == 1,
            "The RebalanceValor battle must contain exactly one play of its tracked carrier.");

        BattleTurnState before = actions[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        CombatRelicState owner = before.Spawn.Train.Context!.Relics!.Single(relic => relic.DataId == RelicId);
        RelicStatusMagnitude magnitude = owner.StatusMagnitudes!.Single();
        Require(owner.AssetKey == RelicKey && owner.EffectTypes.SequenceEqual([Effect]) &&
            owner.DisallowedInPlacementPhase == false && magnitude.EffectIndex == 0 &&
            magnitude.StatusId == "valor" && magnitude.SourceTeam == 0 && magnitude.Modifier == 0 &&
            magnitude.ValorAttackIncrease == 0 && magnitude.ValorArmorIncrease == 1 && magnitude.Conditions.Count == 0 &&
            RelicModel.Validate([owner]) == null,
            "Captured native RebalanceValor identity or effect parameters differ from the decompiled definition.");

        FixtureValue[] observations = root.GetProperty("OriginalRebalanceValorObservations").EnumerateArray().ToArray();
        Require(observations.Length > 0, "The native battle did not invoke Valor's armor callback for the tracked carrier.");
        foreach (FixtureValue observation in observations)
        {
            int stacks = observation.GetProperty("ValorStacks").GetInt32();
            int baseAttack = observation.GetProperty("BaseAttackPerStack").GetInt32();
            int relicAttack = observation.GetProperty("RelicAttackPerStack").GetInt32();
            int baseArmor = observation.GetProperty("BaseArmorPerStack").GetInt32();
            int relicArmor = observation.GetProperty("RelicArmorPerStack").GetInt32();
            int attackPerStack = observation.GetProperty("AttackPerStack").GetInt32();
            int armorPerStack = observation.GetProperty("ArmorPerStack").GetInt32();
            int targetArmor = observation.GetProperty("TargetArmor").GetInt32();
            Require(observation.GetProperty("Turn").GetInt32() > 0 && stacks == 2 &&
                carrierCards.Contains(observation.GetProperty("CardInstanceId").GetInt32()) &&
                baseAttack == 1 && relicAttack == 0 && attackPerStack == 1 &&
                baseArmor == 1 && relicArmor == 1 && armorPerStack == 2 &&
                targetArmor == stacks * armorPerStack && observation.GetProperty("ActualArmor").GetInt32() >= targetArmor,
                "A native Valor observation differs from the original attack/armor per-stack contract or turn boundary.");
        }

        Console.WriteLine($"ORIGINAL-REBALANCE-VALOR NATIVE PASS: {observations.Length} original callback observations, +1 armor and +0 attack per Valor stack, zero capture/model counters.");
    }

    private static CombatRelicState Owner(int attack = 0, int armor = 1) => new(RelicId, RelicKey, [Effect],
        disallowedInPlacementPhase: false,
        statusMagnitudes: [new RelicStatusMagnitude(0, 0, "valor", 0,
            valorAttackIncrease: attack, valorArmorIncrease: armor)]);

    private static CombatUnit Unit(int id, int armor = 0)
    {
        CombatStatus[] statuses = armor > 0
            ? [new("armor", armor, 1), new("valor", 2, 1)]
            : [new("valor", 2, 1)];
        return new CombatUnit(id, "rebalance-valor-carrier-" + id, CombatTeam.Player, 2, 80, 80,
            true, false, false, statuses,
            modifiers: new(2, 0, 0, 1, 1, true, false, [], damageBuffFromStatusEffects: 2),
            statusRegistry: statuses);
    }

    private static RoomCombatResult Resolve(int turn, bool deployment, IReadOnlyList<CombatRelicState> relics,
        params CombatUnit[] units)
    {
        var rng = UnityRng.Seed(17);
        var context = new CombatContext(new([], [], [], rng, 0, []), rng, 0, 1, 10,
            statusRules: [new("armor", 0, 1)], queryFrame: new(turn: turn), relics: relics);
        return RoomCombatModel.Resolve(new RoomCombatState(0, deployment, units, [], context));
    }

    private static CombatContext Context(int turn, IReadOnlyList<CombatRelicState> relics)
    {
        var rng = UnityRng.Seed(17);
        return new CombatContext(new([], [], [], rng, 0, []), rng, 0, 1, 10,
            queryFrame: new(turn: turn), relics: relics);
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
