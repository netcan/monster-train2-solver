using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class StatusAttackDamageChecks
{
    internal static void Run()
    {
        var duringFirstTurn = new RelicConditionState(new("TurnCount"), false, 2, 1, true);
        CombatRelicState conditional = Relic("conditional-rage", new(0, 2, "buff", 1, [duringFirstTurn]));
        RoomCombatState root = Room(Unit(), Context([conditional]));
        string parent = Serialize(root);

        RoomCombatResult History(RoomCombatState source)
        {
            RoomCombatResult added = Add(source, new("buff", 2, 2));
            CombatUnit actor = Actor(added);
            Require(actor.Modifiers!.DamageBuffFromStatusEffects == 6 && actor.Attack == 9 &&
                added.PendingCallbacks[0].Kind == "OnStatusEffectChanged" &&
                added.PendingCallbacks[0].Unit.Modifiers!.DamageBuffFromStatusEffects == 6,
                "Native buff callback did not retain the newly accumulated attack.");
            CombatContext later = added.State!.Context!.WithQueryFrame(added.State.Context.QueryFrame!.With(turn: 2));
            RoomCombatResult removed = StatusRemovalModel.Remove(WithContext(added.State, later), 1, "buff", -1);
            actor = Actor(removed);
            Require(actor.Status("buff") == null && actor.RegisteredStatus("buff")!.Stacks == 0 &&
                actor.Modifiers!.DamageBuffFromStatusEffects == 2 && actor.Attack == 5,
                "Removing old buff stacks recomputed history instead of retaining the native attack remainder.");
            RoomCombatState combat = new(0, false, [actor, Unit(2, CombatTeam.Enemy, attack: 0, canAttack: false)], [], removed.State!.Context);
            RoomCombatResult exchanged = RoomCombatModel.Exchange(combat);
            Require(exchanged.Supported && exchanged.State!.Units.Single(unit => unit.Id == 2).Health == 95 &&
                Actor(exchanged).Modifiers!.DamageBuffFromStatusEffects == 2,
                exchanged.UnsupportedReason ?? "Actual strikes or room copies lost attack accumulated by earlier status applications.");
            return exchanged;
        }
        RoomCombatResult history = History(root);

        RoomCombatResult mixed = Add(Room(Unit(), Context()), new("buff", 2, 2));
        mixed = Add(mixed.State!, new("debuff", 3, 1));
        mixed = Add(mixed.State!, new("valor", 2, 5));
        Require(Actor(mixed).Modifiers!.DamageBuffFromStatusEffects == 11 && Actor(mixed).Attack == 14,
            "Buff, debuff and valor did not accumulate their independently signed native attack amounts.");
        mixed = StatusRemovalModel.Remove(mixed.State!, 1, "buff", -1);
        Require(Actor(mixed).Modifiers!.DamageBuffFromStatusEffects == 7, "Buff removal did not subtract its applied attack.");
        mixed = StatusRemovalModel.Remove(mixed.State!, 1, "debuff", -1);
        Require(Actor(mixed).Modifiers!.DamageBuffFromStatusEffects == 10, "Debuff removal did not restore its applied attack.");
        mixed = StatusRemovalModel.Remove(mixed.State!, 1, "valor", -1);
        Require(Actor(mixed).Modifiers!.DamageBuffFromStatusEffects == 0 && Actor(mixed).Attack == 3,
            "Valor removal failed to restore the original attack accumulator.");

        foreach (int magnitude in new[] { 0, -1, int.MaxValue })
        {
            RoomCombatResult ignored = Add(Room(Unit(), Context()), new("buff", 2, magnitude));
            Require(Actor(ignored).Status("buff")!.Stacks == 2 && Actor(ignored).Modifiers!.DamageBuffFromStatusEffects == 0,
                "Native BuffDamage accepted a nonpositive magnitude or a product that overflowed to a negative value.");
        }
        RoomCombatResult totalOverflow = Add(Room(Unit(cache: int.MaxValue), Context()), new("buff", 1, 1));
        Require(Actor(totalOverflow).Modifiers!.DamageBuffFromStatusEffects == int.MinValue && Actor(totalOverflow).Attack == 0,
            "Positive status attack accumulation clamped native signed overflow.");
        RoomCombatResult reducedByAddition = Add(Room(Unit(statuses: [new("buff", 3, 2)], cache: 6), Context()), new("buff", -5, 99));
        Require(Actor(reducedByAddition).Status("buff") == null && Actor(reducedByAddition).Modifiers!.DamageBuffFromStatusEffects == 6,
            "A nonpositive AddStatus delta incorrectly invoked the native removal attack callback.");
        reducedByAddition = Add(reducedByAddition.State!, new("buff", 2, 99));
        Require(Actor(reducedByAddition).Status("buff")!.ParamInt == 2 &&
            Actor(reducedByAddition).Modifiers!.DamageBuffFromStatusEffects == 10,
            "A retained zero-stack definition or its historical attack accumulator was replaced on reapplication.");

        CombatStatus[] starting = [new("buff", 2, 2), new("debuff", 1, 1), new("valor", 2, 1)];
        foreach (bool withRelic in new[] { false, true })
        {
            CombatContext context = Context(withRelic ? [Relic("birth-rage", new(0, 2, "buff", 1))] : []);
            RoomCombatState birth = Room(Unit(statuses: starting, cache: 999), context);
            string birthParent = Serialize(birth);
            RoomCombatResult initialized = RoomCombatModel.ApplySpawnTriggers(birth, 1, fromCard: false, startingApplications: starting);
            int expected = withRelic ? 7 : 5;
            Require(Actor(initialized).Modifiers!.DamageBuffFromStatusEffects == expected && Actor(initialized).Attack == expected + 3 &&
                Actor(initialized).Status("buff")!.Stacks == 2 && Actor(initialized).Status("debuff")!.Stacks == 1,
                "Native birth did not clear the old attack cache and rebuild it from starting statuses.");
            RoomCombatResult copied = Add(initialized.State!, new("armor", 0, 1));
            Require(Actor(copied).Modifiers!.DamageBuffFromStatusEffects == expected && Serialize(birth) == birthParent,
                "A subsequent unrelated status copy discarded the birth accumulator or changed its parent.");
        }

        var rage = new CombatStatus("buff", 3, 2, removeStackAtEnd: true);
        RoomCombatResult decaying = Add(Room(Unit(), Context([Relic("rage", new(0, 2, "buff", 1))])), rage);
        for (int remaining = 2; remaining >= 0; remaining--)
        {
            decaying = RoomCombatModel.Resolve(decaying.State!);
            Require((Actor(decaying).Status("buff")?.Stacks ?? 0) == remaining &&
                Actor(decaying).Modifiers!.DamageBuffFromStatusEffects == remaining * 3,
                "End-of-turn rage decay changed the stack count without applying its native attack-removal callback.");
        }
        Require(Actor(decaying).RegisteredStatus("buff") == null, "Final rage decay retained the expired native status definition.");

        RoomCombatState missing = Room(Unit(cache: null), Context([conditional]));
        RoomCombatResult missingApplication = StatusApplicationModel.Apply(missing, 1, new("buff", 1, 2));
        Require(!missingApplication.Supported && missingApplication.UnsupportedReason!.Contains("status attack accumulator", StringComparison.Ordinal),
            "An uncaptured status attack accumulator was inferred while a magnitude relic was active.");
        RoomCombatResult missingRemoval = StatusRemovalModel.Remove(Room(Unit(statuses: [new("buff", 2, 2)], cache: null),
            Context([conditional])), 1, "buff", -1);
        Require(!missingRemoval.Supported, "Status removal guessed an uncaptured attack accumulator.");
        Require(!RoomCombatModel.Exchange(missing).Supported,
            "Room combat accepted uncaptured attack history even when the exchange did not add or remove status stacks.");

        Parallel.For(0, 32, _ => Require(Serialize(History(root).State) == Serialize(history.State),
            "Parallel status-attack histories diverged."));
        Require(Serialize(root) == parent && history.State!.Context!.BattleRng.Equals(root.Context!.BattleRng),
            "Status attack histories mutated their parent or consumed battle RNG.");
        Console.WriteLine("STATUS-ATTACK-DAMAGE-CHECKS PASS: dynamic magnitude history, signed status changes, ignored nonpositive products, overflow, birth rebuild/copying, rage decay, missing cache guards and 32 immutable branches.");
    }

    private static CombatRelicState Relic(string id, RelicStatusMagnitude rule) =>
        new(id, id, [RelicModel.ModifyStatusMagnitude], statusMagnitudes: [rule]);
    private static CombatContext Context(IReadOnlyList<CombatRelicState>? relics = null) =>
        new(new([], [], [], UnityRng.Seed(13), 0, []), UnityRng.Seed(13), 0, 1, 10,
            statusRules: [new("armor", 0, 1)], statistics: BattleStatistics.Empty(deckCards: []), otherPiles: [],
            queryFrame: new(turn: 1), relics: relics ?? []);
    private static CombatUnit Unit(int id = 1, CombatTeam team = CombatTeam.Player,
        IReadOnlyList<CombatStatus>? statuses = null, int? cache = 0, int attack = 3, bool canAttack = true) =>
        new(id, "status-attack", team, attack, 100, 100, canAttack, false, false, statuses ?? [], [],
            modifiers: new(attack, 0, 0, 1, 1, true, false, [], damageBuffFromStatusEffects: cache),
            isBoss: false, lastAttackerId: 0, statusRegistry: statuses ?? []);
    private static RoomCombatState Room(CombatUnit actor, CombatContext context) => new(0, false, [actor], [], context);
    private static RoomCombatState WithContext(RoomCombatState room, CombatContext context) =>
        new(room.RoomIndex, room.Deployment, room.Units, room.ExternalInteractions, context, room.Preview);
    private static RoomCombatResult Add(RoomCombatState room, CombatStatus status)
    {
        RoomCombatResult result = StatusApplicationModel.Apply(room, 1, status);
        Require(result.Supported, result.UnsupportedReason ?? "Status attack application failed.");
        return result;
    }
    private static CombatUnit Actor(RoomCombatResult result)
    {
        Require(result.Supported, result.UnsupportedReason ?? "Status attack action failed.");
        return result.State!.Units.Single(unit => unit.Id == 1);
    }
    private static string Serialize(object? value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
